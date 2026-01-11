// GapFillerBlockController.cs
using System;
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class GapFillerBlockController : MonoBehaviour, IRewindable
{
    [Serializable]
    public struct GapFillerState
    {
        public int _x;
        public int _y;
        public bool _isAlive;
    }

    private BoardGrid _grid;
    private GridPresenter _presenter;
    private GapFillerBlockRegistry _registry;

    [SerializeField] private Vector2Int _cell;
    [SerializeField] private bool _isAlive = true;

    [Header("Move FX")]
    [SerializeField] private bool _useLerp = true;
    [SerializeField] private float _moveDuration = 0.12f;

    public Vector2Int Cell => _cell;
    public bool IsAlive => _isAlive;

    private Coroutine _moveCo;

    public void Initialize(BoardGrid grid, GridPresenter presenter, GapFillerBlockRegistry registry, Vector2Int spawnCell)
    {
        _grid = grid;
        _presenter = presenter;
        _registry = registry;

        if (_grid == null || _presenter == null)
        {
            Debug.LogWarning("[GapFillerBlock] Initialize fallback: grid/presenter is null.");
            return;
        }

        _cell = spawnCell;

        if (!_grid.IsInBounds(_cell))
        {
            Debug.LogWarning($"[GapFillerBlock] Initialize fallback: spawn out of bounds. spawn={_cell}");
            _cell = new Vector2Int(
                Mathf.Clamp(_cell.x, 0, _grid._w - 1),
                Mathf.Clamp(_cell.y, 0, _grid._h - 1));
        }

        // InnerBase(=FatherMoveRect) 밖이면 스폰 자체를 Clamp + Warning
        if (_registry != null && !_registry.IsAllowedCell(_cell, _grid))
        {
            Vector2Int clamped = ClampCellToRect(_cell, GetMoveRectOrFullBoard());
            Debug.LogWarning($"[GapFillerBlock] Initialize fallback: out of InnerBase. spawn={_cell} -> clamp={clamped}");
            _cell = clamped;
        }

        _isAlive = true;

        if (_registry != null)
            _registry.Register(this, _cell);

        _grid.SetOcc(_cell, E_Occupant.GapFillerBlock);
        SnapToCell();
        gameObject.SetActive(true);
    }

    public bool TryPush(Vector2Int dir)
    {
        if (!_isAlive)
            return false;

        if (_grid == null || _presenter == null)
        {
            Debug.LogWarning("[GapFillerBlock] TryPush fallback: grid/presenter is null.");
            return false;
        }

        if (dir == Vector2Int.zero)
        {
            Debug.LogWarning("[GapFillerBlock] TryPush fallback: dir is zero.");
            return false;
        }

        Vector2Int from = _cell;
        Vector2Int to = from + dir;

        if (!_grid.IsInBounds(to))
            return false;

        // InnerBase(=FatherMoveRect)로 이동 제한
        if (_registry != null && !_registry.IsAllowedCell(to, _grid))
            return false;

        // 다른 점유(블록/캐릭터 등)면 밀기 불가
        var occ = _grid.GetOcc(to);
        if (occ != E_Occupant.None)
            return false;

        // Hole이면: 블록 소멸 + HoleFilled 상태로 전환(진입 가능 + FilledHole 스프라이트)
        var cellOv1 = _grid.GetCellOverlay01(to);
        if (cellOv1 == E_CellType.Hole)
        {
            _grid.SetOcc(from, E_Occupant.None);
            _grid.SetCellOverlay01(to, E_CellType.FilledHole);

            _registry?.Unregister(from, this);
            _isAlive = false;
            gameObject.SetActive(false);

            return true;
        }

        // 정적 지형 막힘(벽/장애물)
        var cellType = _grid.GetCell(to);
        if (_grid.IsBlockedCell(cellType))
            return false;

        // 일반 이동
        _grid.SetOcc(from, E_Occupant.None);
        _grid.SetOcc(to, E_Occupant.GapFillerBlock);

        _registry?.Move(from, to, this);

        _cell = to;

        // === Push 성공 직후 SFX. 실패에는 재생 없음. ===
        AudioHub.Ensure().PlaySfx(E_SfxId.GapFiller_Push);

        // 비주얼 이동
        StartMoveFx(_presenter.CellToWorld(from), _presenter.CellToWorld(to));
        return true;
    }

    private void StartMoveFx(Vector3 fromWorld, Vector3 toWorld)
    {
        if (_moveCo != null)
        {
            StopCoroutine(_moveCo);
            _moveCo = null;
        }

        if (!_useLerp)
        {
            transform.position = toWorld;
            return;
        }

        if (_moveDuration <= 0f)
        {
            Debug.LogWarning($"[GapFillerBlock] MoveFX fallback: invalid duration({_moveDuration}). (snap)");
            transform.position = toWorld;
            return;
        }

        _moveCo = StartCoroutine(CoMove(fromWorld, toWorld, _moveDuration));
    }

    private IEnumerator CoMove(Vector3 from, Vector3 to, float dur)
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / dur;
            transform.position = Vector3.Lerp(from, to, Mathf.Clamp01(t));
            yield return null;
        }

        transform.position = to;
        _moveCo = null;
    }

    private void SnapToCell()
    {
        if (_presenter == null)
            return;

        transform.position = _presenter.CellToWorld(_cell);
    }

    public object CaptureState()
    {
        return new GapFillerState
        {
            _x = _cell.x,
            _y = _cell.y,
            _isAlive = _isAlive
        };
    }

    public void RestoreState(object state)
    {
        if (state is not GapFillerState s)
        {
            Debug.LogWarning("[GapFillerBlock] RestoreState fallback: invalid state type.");
            return;
        }

        if (_grid == null || _presenter == null)
        {
            Debug.LogWarning("[GapFillerBlock] RestoreState fallback: grid/presenter is null.");
            return;
        }

        // 기존 등록/점유 해제(살아있는 경우만)
        if (_isAlive)
        {
            _grid.SetOcc(_cell, E_Occupant.None);
            _registry?.Unregister(_cell, this);
        }
        var from = _cell;

        var newCell = new Vector2Int(s._x, s._y);
        if (!_grid.IsInBounds(newCell))
        {
            Vector2Int clamped = new Vector2Int(
                Mathf.Clamp(newCell.x, 0, _grid._w - 1),
                Mathf.Clamp(newCell.y, 0, _grid._h - 1));
            Debug.LogWarning($"[GapFillerBlock] RestoreState fallback: out of bounds -> clamp. to={newCell} clamp={clamped}");
            newCell = clamped;
        }
        var to = newCell;

        // InnerBase 밖 복원은 Clamp + Warning (무음 금지)
        if (_registry != null && !_registry.IsAllowedCell(newCell, _grid))
        {
            Vector2Int clamped = ClampCellToRect(newCell, GetMoveRectOrFullBoard());
            Debug.LogWarning($"[GapFillerBlock] RestoreState fallback: out of InnerBase -> clamp. to={newCell} clamp={clamped}");
            newCell = clamped;
        }

        _cell = newCell;
        _isAlive = s._isAlive;

        if (_isAlive)
        {
            var cellOv1 = _grid.GetCellOverlay01(_cell);
            if (cellOv1 == E_CellType.Hole)
            {
                Debug.LogWarning($"[GapFillerBlock] RestoreState fallback: alive block on OpenHole cell. cell={_cell} -> deactivate");
                _isAlive = false;
                gameObject.SetActive(false);

                return;
            }

            _grid.SetOcc(_cell, E_Occupant.GapFillerBlock);
            _registry?.Register(this, _cell);
            gameObject.SetActive(true);
            StartMoveFx(_presenter.CellToWorld(from), _presenter.CellToWorld(to));

        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    // ===== helpers =====

    private RectInt GetMoveRectOrFullBoard()
    {
        // registry가 full-board 폴백으로 들고 있을 수도 있으니 그대로 사용
        // 단, 접근 API를 따로 열지 않았으므로 여기선 “보드 전체”를 반환하고 Clamp만 수행한다.
        return new RectInt(0, 0, _grid._w, _grid._h);
    }

    private static Vector2Int ClampCellToRect(Vector2Int c, RectInt r)
    {
        int x = Mathf.Clamp(c.x, r.xMin, r.xMax - 1);
        int y = Mathf.Clamp(c.y, r.yMin, r.yMax - 1);
        return new Vector2Int(x, y);
    }
}
