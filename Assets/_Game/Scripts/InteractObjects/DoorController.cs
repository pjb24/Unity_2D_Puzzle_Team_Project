// DoorController.cs
using System;
using UnityEngine;

[DisallowMultipleComponent]
public class DoorController : MonoBehaviour, IRewindable
{
    [Serializable]
    public struct DoorState
    {
        public bool _isOpen;
    }

    private BoardGrid _grid;
    private GridPresenter _presenter;
    private Vector2Int _cell;

    private bool _isOpen;

    public Vector2Int Cell => _cell;
    public bool IsOpen => _isOpen;

    public void Initialize(BoardGrid grid, GridPresenter presenter, Vector2Int cell, bool startOpen)
    {
        _grid = grid;
        _presenter = presenter;
        _cell = cell;

        if (_grid == null || _presenter == null)
        {
            Debug.LogWarning("[DoorController] Initialize fallback: grid/presenter is null.");
            return;
        }

        if (!_grid.IsInBounds(_cell))
        {
            Debug.LogWarning($"[DoorController] Initialize fallback: out of bounds. cell={_cell}");
            _cell = new Vector2Int(
                Mathf.Clamp(_cell.x, 0, _grid._w - 1),
                Mathf.Clamp(_cell.y, 0, _grid._h - 1));
        }

        transform.position = _presenter.CellToWorld(_cell) + Vector3.up * 0.55f;
        SetOpen(startOpen, notify: false);
    }

    public void SetOpen(bool open) => SetOpen(open, notify: true);

    private void SetOpen(bool open, bool notify)
    {
        if (_grid == null)
        {
            Debug.LogWarning("[DoorController] SetOpen fallback: grid is null.");
            _isOpen = open;
            ApplyVisual();
            return;
        }

        _isOpen = open;

        // 닫힘이면 Blocker 점유로 막기, 열림이면 해제
        if (_grid.IsInBounds(_cell))
            _grid.SetOcc(_cell, _isOpen ? E_Occupant.None : E_Occupant.Blocker);
        else
            Debug.LogWarning($"[DoorController] SetOpen fallback: cell out of bounds. cell={_cell}");

        ApplyVisual();
    }

    private void ApplyVisual()
    {
        // 프로토타입: 열리면 숨김(스케일 0), 닫히면 보임
        transform.localScale = _isOpen ? Vector3.zero : new Vector3(0.9f, 0.9f, 0.2f);
    }

    public object CaptureState()
    {
        return new DoorState { _isOpen = _isOpen };
    }

    public void RestoreState(object state)
    {
        if (state is not DoorState s)
        {
            Debug.LogWarning("[DoorController] RestoreState fallback: invalid state type.");
            return;
        }

        if (_grid == null || _presenter == null)
        {
            Debug.LogWarning("[DoorController] RestoreState fallback: grid/presenter is null.");
            return;
        }

        SetOpen(s._isOpen, notify: false);
    }
}
