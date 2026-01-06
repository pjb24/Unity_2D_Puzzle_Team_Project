// FatherController.Rewind.cs
///
/// 저장: Cell
/// 복구: Grid 점유 갱신 + transform 스냅
///
using System;
using UnityEngine;

public partial class FatherController : IRewindable
{
    [Serializable]
    public struct FatherState
    {
        public int _x;
        public int _y;
        public E_Facing _facing;
    }

    public object CaptureState()
    {
        return new FatherState
        {
            _x = Cell.x,
            _y = Cell.y,
            _facing = Facing
        };
    }

    public void RestoreState(object state)
    {
        if (state is not FatherState s)
        {
            Debug.LogWarning("[FatherController] RestoreState fallback: invalid state type.");
            return;
        }

        if (_grid == null || _presenter == null)
        {
            Debug.LogWarning("[FatherController] RestoreState fallback: grid/presenter is null.");
            return;
        }

        Vector2Int to = new Vector2Int(s._x, s._y);

        if (!_grid.IsInBounds(to))
        {
            Debug.LogWarning($"[FatherController] RestoreState fallback: out of bounds -> clamp. to={to}");
            to = new Vector2Int(
                Mathf.Clamp(to.x, 0, _grid._w - 1),
                Mathf.Clamp(to.y, 0, _grid._h - 1));
        }

        // InnerBase bounds 보정
        if (_hasMoveBounds && !_moveBounds.Contains(to))
        {
            var clamped = ClampCellToRect(to, _moveBounds);
            Debug.LogWarning($"[FatherController] RestoreState fallback: out of move bounds -> clamp. to={to} clamp={clamped} rect={_moveBounds}");
            to = clamped;
        }

        // 점유 리셋
        if (_grid.IsInBounds(Cell))
        {
            // 기존 Cell 비우기
            _grid.SetOcc(Cell, E_Occupant.None);
        }

        // 새 Cell 점유
        _grid.SetOcc(to, E_Occupant.Father);

        Cell = to;
        Facing = s._facing;

        transform.position = _presenter.CellToWorld(Cell);
        ApplyFacingVisual();
    }
}
