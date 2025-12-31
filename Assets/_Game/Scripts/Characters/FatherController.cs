// FatherController.cs
///
/// FatherController 최소 통합(4방향 스냅 + 점유)
/// _lastResult 프로퍼티로 TurnContext/Resolve가 조회하게 한다
/// (콜백에 ctx를 못 넣는 구조와 잘 맞음).
///

using System;
using UnityEngine;

[DisallowMultipleComponent]
public partial class FatherController : MonoBehaviour
{
    private event Action _onActionCompleted;

    public void AddListenerOnActionCompleted(Action cb) => _onActionCompleted += cb;
    public void RemoveListenerOnActionCompleted(Action cb) => _onActionCompleted -= cb;

    public Vector2Int Cell { get; private set; }
    public E_Facing Facing { get; private set; } = E_Facing.Down;

    public FatherActionResult LastResult => _lastResult;

    private FatherActionResult _lastResult;

    private BoardGrid _grid;
    private GridPresenter _presenter;

    private IInteractPort _interactPort;

    public void BindInteractPort(IInteractPort port) => _interactPort = port;
    public void UnbindInteractPort() => _interactPort = null;

    public void Initialize(BoardGrid grid, GridPresenter presenter, Vector2Int spawnCell)
    {
        _grid = grid;
        _presenter = presenter;

        Cell = spawnCell;

        if (_grid == null || _presenter == null)
        {
            Debug.LogWarning("[FatherController] Initialize fallback: grid/presenter is null.");
            return;
        }

        if (!_grid.IsInBounds(Cell))
        {
            Debug.LogWarning($"[FatherController] Initialize fallback: spawn out of bounds. spawn={Cell}");
            Cell = new Vector2Int(
                Mathf.Clamp(Cell.x, 0, _grid._w - 1),
                Mathf.Clamp(Cell.y, 0, _grid._h - 1));
        }

        // 점유 등록
        _grid.SetOcc(Cell, E_Occupant.Father);

        // 위치 스냅
        transform.position = _presenter.CellToWorld(Cell) + Vector3.up * 0.9f; // 더미 캡슐 높이 보정
        ApplyFacingVisual();
    }

    public void RequestAction(TurnCommand cmd)
    {
        if (cmd.Type == E_TurnCommandType.Interact)
        {
            if (_interactPort == null)
                Debug.LogWarning("[FatherController] Interact fallback: interact port is null.");

            _interactPort?.RequestInteract(Cell, Facing);

            _lastResult = new FatherActionResult(E_FatherActionResultCode.None, Cell, Cell, false);
            _onActionCompleted?.Invoke();
            return;
        }

        // Move 외에는 일단 패스(확장 가능)
        Vector2Int dir = cmd.Type switch
        {
            E_TurnCommandType.MoveUp => Vector2Int.up,
            E_TurnCommandType.MoveDown => Vector2Int.down,
            E_TurnCommandType.MoveLeft => Vector2Int.left,
            E_TurnCommandType.MoveRight => Vector2Int.right,
            _ => Vector2Int.zero
        };

        if (dir == Vector2Int.zero)
        {
            _lastResult = new FatherActionResult(E_FatherActionResultCode.None, Cell, Cell, false);
            _onActionCompleted?.Invoke();
            return;
        }

        // 방향은 “입력” 기준으로 갱신 (이동 성공/실패와 무관)
        Facing = cmd.Type switch
        {
            E_TurnCommandType.MoveUp => E_Facing.Up,
            E_TurnCommandType.MoveDown => E_Facing.Down,
            E_TurnCommandType.MoveLeft => E_Facing.Left,
            E_TurnCommandType.MoveRight => E_Facing.Right,
            _ => Facing
        };
        ApplyFacingVisual();

        TryMove(dir);
        _onActionCompleted?.Invoke();
    }

    private void TryMove(Vector2Int dir)
    {
        if (_grid == null || _presenter == null)
        {
            Debug.LogWarning("[FatherController] TryMove fallback: grid/presenter is null.");
            _lastResult = new FatherActionResult(E_FatherActionResultCode.None, Cell, Cell, false);
            return;
        }

        Vector2Int from = Cell;
        Vector2Int to = from + dir;

        if (!_grid.IsInBounds(to))
        {
            _lastResult = new FatherActionResult(E_FatherActionResultCode.Blocked_OutOfBounds, from, from, false);
            return;
        }

        var cellType = _grid.GetCell(to);
        if (_grid.IsBlockedCell(cellType))
        {
            _lastResult = new FatherActionResult(E_FatherActionResultCode.Blocked_Cell, from, from, false);
            return;
        }

        if (_grid.GetOcc(to) != E_Occupant.None)
        {
            _lastResult = new FatherActionResult(E_FatherActionResultCode.Blocked_Occupied, from, from, false);
            return;
        }

        // 점유 갱신
        _grid.SetOcc(from, E_Occupant.None);
        _grid.SetOcc(to, E_Occupant.Father);
        Cell = to;

        // 월드 이동 스냅(프로토타입)
        transform.position = _presenter.CellToWorld(Cell) + Vector3.up * 0.9f;

        bool triggerGoal = (cellType == E_CellType.Goal);
        _lastResult = new FatherActionResult(E_FatherActionResultCode.Moved, from, to, triggerGoal);
    }

    private void ApplyFacingVisual()
    {
        // 프로토타입: 캡슐 회전으로 방향 표시
        float z = Facing switch
        {
            E_Facing.Up => 0f,
            E_Facing.Right => -90f,
            E_Facing.Down => 180f,
            E_Facing.Left => 90f,
            _ => 0f
        };
        transform.rotation = Quaternion.Euler(0f, 0f, z);
    }
}
