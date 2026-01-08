// FatherController.cs
///
/// FatherController 최소 통합(4방향 스냅 + 점유)
/// _lastResult 프로퍼티로 TurnContext/Resolve가 조회하게 한다
/// (콜백에 ctx를 못 넣는 구조와 잘 맞음).
///
using System;
using System.Collections;
using UnityEngine;

public enum E_FatherActionResultCode
{
    None,
    Moved,

    Blocked_OutOfBounds,
    Blocked_Cell,
    Blocked_Occupied,

    // InnerBase 밖 이동 차단
    Blocked_InnerBase,
}

public readonly struct FatherActionResult
{
    public readonly E_FatherActionResultCode Code;  // 이동 성공/실패 원인(벽/장애물/바운더리/점유)
    public readonly Vector2Int From;
    public readonly Vector2Int To;
    public readonly bool TriggerGoal;   // 트리거(Goal / 스위치 등)

    // 2턴(늪) 지원: 기본 1
    public readonly int ConsumedTurns;

    public bool IsSuccess => Code == E_FatherActionResultCode.Moved;

    public FatherActionResult(
        E_FatherActionResultCode code,
        Vector2Int from,
        Vector2Int to,
        bool triggerGoal,
        int consumedTurns = 1)
    {
        Code = code;
        From = from;
        To = to;
        TriggerGoal = triggerGoal;
        ConsumedTurns = Mathf.Max(1, consumedTurns);
    }
}

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

    // ===== GapFiller =====
    private GapFillerBlockRegistry _gapFillerRegistry;
    public void BindGapFillerRegistry(GapFillerBlockRegistry registry) => _gapFillerRegistry = registry;
    public void UnbindGapFillerRegistry() => _gapFillerRegistry = null;

    // ===== InnerBase bounds =====
    private RectInt _moveBounds;
    private bool _hasMoveBounds;

    [Header("Move FX (Lerp)")]
    [SerializeField] private bool _useLerp = true;
    [SerializeField] private float _moveDuration = 0.12f;

    private Coroutine _moveCo;

    // ===== Move Animation =====
    private FatherAnimDriver _animDriver;
    public void BindAnimDriver(FatherAnimDriver driver) => _animDriver = driver;
    public void UnbindAnimDriver() => _animDriver = null;

    private VisualMoveAgent _visualMove;
    private bool _useRewindRestoreLerp;
    private float _rewindRestoreMoveDuration;

    private bool _warnedRestoreMissingMoveAgent;
    private bool _warnedRestoreInvalidDuration;

    public void BindVisualMoveAgent(VisualMoveAgent agent) => _visualMove = agent;
    public void UnbindVisualMoveAgent() => _visualMove = null;

    public void Initialize(
        BoardGrid grid,
        GridPresenter presenter,
        Vector2Int spawnCell,
        RectInt moveBounds,
        bool useRewindRestoreLerp,
        float rewindRestoreMoveDuration)
    {
        _grid = grid;
        _presenter = presenter;

        _useRewindRestoreLerp = useRewindRestoreLerp;
        _rewindRestoreMoveDuration = rewindRestoreMoveDuration;

        if (_animDriver == null)
            _animDriver = GetComponent<FatherAnimDriver>();

        Cell = spawnCell;

        if (_grid == null || _presenter == null)
        {
            Debug.LogWarning("[FatherController] Initialize fallback: grid/presenter is null.");
            return;
        }

        // bounds 세팅 (유효하지 않으면 보드 전체로 폴백 + Warning)
        if (moveBounds.width <= 0 || moveBounds.height <= 0)
        {
            Debug.LogWarning($"[FatherController] MoveBounds invalid. fallback to full board. raw={moveBounds}");
            _moveBounds = new RectInt(0, 0, _grid._w, _grid._h);
            _hasMoveBounds = true;
        }
        else
        {
            _moveBounds = ClampRectToGrid(moveBounds, _grid._w, _grid._h);
            if (_moveBounds != moveBounds)
                Debug.LogWarning($"[FatherController] MoveBounds clamped. raw={moveBounds} clamped={_moveBounds}");
            _hasMoveBounds = true;
        }

        // 보드 bounds 보정
        if (!_grid.IsInBounds(Cell))
        {
            Debug.LogWarning($"[FatherController] Initialize fallback: spawn out of bounds. spawn={Cell}");
            Cell = new Vector2Int(
                Mathf.Clamp(Cell.x, 0, _grid._w - 1),
                Mathf.Clamp(Cell.y, 0, _grid._h - 1));
        }

        // InnerBase bounds 보정
        if (_hasMoveBounds && !_moveBounds.Contains(Cell))
        {
            var clamped = ClampCellToRect(Cell, _moveBounds);
            Debug.LogWarning($"[FatherController] Initialize fallback: spawn out of move bounds. spawn={Cell} -> clamp={clamped} rect={_moveBounds}");
            Cell = clamped;
        }

        // 점유 등록
        _grid.SetOcc(Cell, E_Occupant.Father);

        StopMoveFxIfAny();

        // 위치 스냅
        transform.position = _presenter.CellToWorld(Cell);
        ApplyFacingVisual();
    }

    public void RequestAction(TurnCommand cmd)
    {
        if (cmd.Type == E_TurnCommandType.Interact)
        {
            if (_interactPort == null)
                Debug.LogWarning("[FatherController] Interact fallback: interact port is null.");

            _interactPort?.RequestInteract(Cell, Facing);

            // Interact는 이동이 아니므로 애니 트리거 없음
            _lastResult = new FatherActionResult(E_FatherActionResultCode.None, Cell, Cell, false);
            _onActionCompleted?.Invoke();
            return;
        }

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
            Debug.LogWarning($"[FatherController] RequestAction fallback: invalid move cmd. cmd={cmd.Type}");
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

        // 1) 보드 bounds
        if (!_grid.IsInBounds(to))
        {
            _lastResult = new FatherActionResult(E_FatherActionResultCode.Blocked_OutOfBounds, from, from, false);
            return;
        }

        // 2) InnerBase bounds
        if (_hasMoveBounds && !_moveBounds.Contains(to))
        {
            _lastResult = new FatherActionResult(E_FatherActionResultCode.Blocked_InnerBase, from, from, false);
            return;
        }

        // wall/blocked cell
        var cellType = _grid.GetCell(to);
        if (_grid.IsBlockedCell(cellType))
        {
            _lastResult = new FatherActionResult(E_FatherActionResultCode.Blocked_Cell, from, from, false);
            return;
        }

        // 기믹 메타(Hole) 진입 불가
        if (_grid.GetMeta(to).IsHole)
        {
            _lastResult = new FatherActionResult(E_FatherActionResultCode.Blocked_Cell, from, from, false, consumedTurns: 1);
            return;
        }

        // ===== 앞칸이 블록이면 “밀기 시도” =====
        var occ = _grid.GetOcc(to);
        if (occ != E_Occupant.None)
        {
            if (occ == E_Occupant.GapFillerBlock)
            {
                if (_gapFillerRegistry == null)
                {
                    Debug.LogWarning("[FatherController] Push fallback: GapFillerBlockRegistry is null.");
                    _lastResult = new FatherActionResult(E_FatherActionResultCode.Blocked_Occupied, from, from, false);
                    return;
                }

                if (!_gapFillerRegistry.TryGet(to, out var block) || block == null)
                {
                    Debug.LogWarning($"[FatherController] Push fallback: block not found in registry. cell={to}");
                    _lastResult = new FatherActionResult(E_FatherActionResultCode.Blocked_Occupied, from, from, false);
                    return;
                }

                if (!block.TryPush(dir))
                {
                    _lastResult = new FatherActionResult(E_FatherActionResultCode.Blocked_Occupied, from, from, false);
                    return;
                }

                // 밀기 성공 후, Father가 들어갈 칸(to)은 비어 있어야 함
                if (_grid.GetOcc(to) != E_Occupant.None)
                {
                    Debug.LogWarning($"[FatherController] Push fallback: to still occupied after push. cell={to}");
                    _lastResult = new FatherActionResult(E_FatherActionResultCode.Blocked_Occupied, from, from, false);
                    return;
                }
            }
            else
            {
                _lastResult = new FatherActionResult(E_FatherActionResultCode.Blocked_Occupied, from, from, false);
                return;
            }
        }

        // ===== 논리 이동, 점유 갱신 =====
        _grid.SetOcc(from, E_Occupant.None);
        _grid.SetOcc(to, E_Occupant.Father);
        Cell = to;

        bool triggerGoal = (cellType == E_CellType.Goal);

        // “턴 비용(2턴)” (늪 이탈 시 2)
        int consumedTurns = 1;
        var fromMeta = _grid.GetMeta(from);
        var toMeta = _grid.GetMeta(to);
        if (fromMeta.IsSwamp && !toMeta.IsSwamp)
            consumedTurns = 2;

        _lastResult = new FatherActionResult(E_FatherActionResultCode.Moved, from, to, triggerGoal, consumedTurns);

        // ===== 연출 이동(애니 + Lerp) =====
        // 이동 성공 시에만 Move 애니메이션 재생
        _animDriver?.PlayMove(Facing);

        // === Father 이동 SFX: 이동 성공 확정 + VisualMove 시작 직전 1회 ===
        AudioHub.Ensure().PlaySfx(E_SfxId.Move_Father);

        StartMoveFx(
            fromWorld: _presenter.CellToWorld(from),
            toWorld: _presenter.CellToWorld(to),
            onDone: () => _onActionCompleted?.Invoke());
    }

    private void ApplyFacingVisual()
    {
        // Animator 사용 가능하면 회전으로 방향 표시하지 않는다(프리팹 애니/리깅 훼손 방지)
        if (_animDriver != null && _animDriver.IsUsable)
        {
            _animDriver.SetFacing(Facing);
            return;
        }

        // 프로토타입: 회전으로 방향 표시
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

    private static RectInt ClampRectToGrid(RectInt r, int w, int h)
    {
        int xMin = Mathf.Clamp(r.xMin, 0, w - 1);
        int yMin = Mathf.Clamp(r.yMin, 0, h - 1);
        int xMax = Mathf.Clamp(r.xMax, xMin + 1, w);
        int yMax = Mathf.Clamp(r.yMax, yMin + 1, h);
        return new RectInt(xMin, yMin, xMax - xMin, yMax - yMin);
    }

    private static Vector2Int ClampCellToRect(Vector2Int c, RectInt r)
    {
        int x = Mathf.Clamp(c.x, r.xMin, r.xMax - 1);
        int y = Mathf.Clamp(c.y, r.yMin, r.yMax - 1);
        return new Vector2Int(x, y);
    }

    private void StartMoveFx(Vector3 fromWorld, Vector3 toWorld, Action onDone)
    {
        // 중복 이동 정리
        StopMoveFxIfAny();

        if (!_useLerp)
        {
            transform.position = toWorld;
            onDone?.Invoke();
            return;
        }

        if (_moveDuration <= 0f)
        {
            Debug.LogWarning($"[FatherController] MoveFX fallback: invalid duration({_moveDuration}). (snap)");
            transform.position = toWorld;
            onDone?.Invoke();
            return;
        }

        _moveCo = StartCoroutine(CoMove(toWorld, _moveDuration, () =>
        {
            _moveCo = null;
            onDone?.Invoke();
        }));
    }

    private void StopMoveFxIfAny()
    {
        if (_moveCo != null)
        {
            StopCoroutine(_moveCo);
            _moveCo = null;
        }
    }

    private IEnumerator CoMove(Vector3 to, float dur, Action onDone)
    {
        // 현재 위치가 from과 다를 수 있으니(리와인드/중단 등) 실제 시작점은 transform 기준으로 잡는다.
        Vector3 from = transform.position;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / dur;
            transform.position = Vector3.Lerp(from, to, Mathf.Clamp01(t));
            yield return null;
        }

        transform.position = to;
        onDone?.Invoke();
    }
}
