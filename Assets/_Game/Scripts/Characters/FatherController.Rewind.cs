///
/// 저장: Cell
/// 복구: Grid 점유 갱신 + transform 스냅
///
// FatherController.Rewind.cs (partial 추천)
using System;
using UnityEngine;

public partial class FatherController : IRewindable
{
    [Serializable]
    public struct FatherState
    {
        public int _x;
        public int _y;
    }

    public object CaptureState()
    {
        return new FatherState { _x = Cell.x, _y = Cell.y };
    }

    public void RestoreState(object state)
    {
        if (state is not FatherState s) return;

        Vector2Int to = new Vector2Int(s._x, s._y);

        // 점유 리셋(프로토타입: Father만 고려)
        if (_grid != null)
        {
            // 기존 Cell 비우기
            _grid.SetOcc(Cell, E_Occupant.None);
            // 새 Cell 점유
            _grid.SetOcc(to, E_Occupant.Father);
        }

        Cell = to;

        if (_presenter != null)
            transform.position = _presenter.CellToWorld(Cell) + Vector3.up * 0.9f;
    }
}
