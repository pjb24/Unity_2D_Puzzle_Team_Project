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

        // 이동 연출 중이면 중단 (무음 금지 아님: 정상 동작이므로 Warning 불필요)
        // (StopMoveFxIfAny는 FatherController.cs에 있음)
        StopMoveFxIfAny();
        _visualMove?.StopMove();

        Vector2Int from = Cell;
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

        ApplyFacingVisual();

        bool moved = from != to;
        Vector3 toWorld = _presenter.CellToWorld(Cell);

        if (moved)
            _animDriver?.PlayMove(Facing);

        if (_useRewindRestoreLerp && moved)
        {
            if (_visualMove == null)
            {
                if (!_warnedRestoreMissingMoveAgent)
                {
                    _warnedRestoreMissingMoveAgent = true;
                    Debug.LogWarning("[FatherController] RestoreState fallback: VisualMoveAgent missing. (snap restore)");
                }
                transform.position = toWorld;
                return;
            }

            if (_rewindRestoreMoveDuration <= 0f)
            {
                if (!_warnedRestoreInvalidDuration)
                {
                    _warnedRestoreInvalidDuration = true;
                    Debug.LogWarning($"[FatherController] RestoreState fallback: invalid rewind restore duration={_rewindRestoreMoveDuration}. (snap restore)");
                }
                transform.position = toWorld;
                return;
            }

            _visualMove.MoveTo(toWorld, _rewindRestoreMoveDuration);
            return;
        }

        // Snap (기본 / 옵션 off / 혹은 moved=false)
        transform.position = toWorld;
    }
}
