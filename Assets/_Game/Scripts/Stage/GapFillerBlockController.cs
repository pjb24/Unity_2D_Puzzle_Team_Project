// GapFillerBlockController.cs
using System;
using UnityEngine;

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

    private SpriteRenderer _sr;

    public Vector2Int Cell => _cell;
    public bool IsAlive => _isAlive;

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

        _sr = Proto2DVisual.EnsureSpriteRenderer(gameObject, (int)E_ProtoSort.Actor, Proto2DVisual.GapBlock);

        _cell = spawnCell;

        if (!_grid.IsInBounds(_cell))
        {
            Debug.LogWarning($"[GapFillerBlock] Initialize fallback: spawn out of bounds. spawn={_cell}");
            _cell = new Vector2Int(
                Mathf.Clamp(_cell.x, 0, _grid._w - 1),
                Mathf.Clamp(_cell.y, 0, _grid._h - 1));
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

        // 정적 지형 막힘
        var cellType = _grid.GetCell(to);
        if (_grid.IsBlockedCell(cellType))
            return false;

        // 다른 점유(블록/캐릭터 등)면 밀기 불가
        var occ = _grid.GetOcc(to);
        if (occ != E_Occupant.None)
            return false;

        // Hole이면: 블록 소멸 + Hole -> Floor(타일색 갱신)
        var meta = _grid.GetMeta(to);
        if (meta.IsHole)
        {
            _grid.SetOcc(from, E_Occupant.None);

            meta._surface = E_CellSurface.Normal;
            _grid.SetMeta(to, meta, notify: true);

            _presenter.RefreshTile(_grid, to); // 색 즉시 반영

            _registry?.Unregister(from, this);

            _isAlive = false;
            gameObject.SetActive(false);
            return true;
        }

        // 일반 이동
        _grid.SetOcc(from, E_Occupant.None);
        _grid.SetOcc(to, E_Occupant.GapFillerBlock);

        _registry?.Move(from, to, this);

        _cell = to;
        SnapToCell();
        return true;
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

        // 현재 등록/점유 해제(살아있는 경우만)
        if (_isAlive)
        {
            _grid.SetOcc(_cell, E_Occupant.None);
            _registry?.Unregister(_cell, this);
        }

        var newCell = new Vector2Int(s._x, s._y);
        if (!_grid.IsInBounds(newCell))
        {
            Debug.LogWarning($"[GapFillerBlock] RestoreState fallback: out of bounds. cell={newCell}");
            newCell = new Vector2Int(
                Mathf.Clamp(newCell.x, 0, _grid._w - 1),
                Mathf.Clamp(newCell.y, 0, _grid._h - 1));
        }

        _cell = newCell;
        _isAlive = s._isAlive;

        _sr = Proto2DVisual.EnsureSpriteRenderer(gameObject, (int)E_ProtoSort.Actor, Proto2DVisual.GapBlock);

        if (_isAlive)
        {
            _grid.SetOcc(_cell, E_Occupant.GapFillerBlock);
            _registry?.Register(this, _cell);
            gameObject.SetActive(true);
            SnapToCell();
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
