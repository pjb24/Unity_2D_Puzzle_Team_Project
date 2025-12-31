// SwitchController.cs
using System;
using UnityEngine;

[DisallowMultipleComponent]
public class SwitchController : MonoBehaviour, IRewindable, IInteractable
{
    [Serializable]
    public struct SwitchState
    {
        public bool _isOn;
    }

    private event Action<bool> _onChanged;
    public void AddListenerOnChanged(Action<bool> cb) => _onChanged += cb;
    public void RemoveListenerOnChanged(Action<bool> cb) => _onChanged -= cb;

    private BoardGrid _grid;
    private GridPresenter _presenter;
    private Vector2Int _cell;

    private bool _isOn;

    private InteractRegistry _registry;

    public Vector2Int Cell => _cell;
    public bool IsOn => _isOn;

    public void Initialize(BoardGrid grid, GridPresenter presenter, Vector2Int cell, bool startOn, InteractRegistry registry)
    {
        _grid = grid;
        _presenter = presenter;
        _cell = cell;
        _registry = registry;

        if (_grid == null || _presenter == null)
        {
            Debug.LogWarning("[SwitchController] Initialize fallback: grid/presenter is null.");
            return;
        }

        if (!_grid.IsInBounds(_cell))
        {
            Debug.LogWarning($"[SwitchController] Initialize fallback: out of bounds. cell={_cell}");
            _cell = new Vector2Int(
                Mathf.Clamp(_cell.x, 0, _grid._w - 1),
                Mathf.Clamp(_cell.y, 0, _grid._h - 1));
        }

        if (_registry == null)
            Debug.LogWarning("[SwitchController] Initialize fallback: registry is null. Interact may not work.");

        _registry?.Register(this);

        transform.position = _presenter.CellToWorld(_cell) + Vector3.up * 0.25f;

        SetOn(startOn, notify: true);
    }

    private void OnDestroy()
    {
        _registry?.Unregister(this);
    }

    public bool TryInteract(in FatherInteractArgs args)
    {
        // 이 스위치는 “셀에 인접해서 Interact”로만 동작
        if (args.TargetCell != _cell && args.FatherCell != _cell)
            return false;

        Toggle();
        return true;
    }

    public void Toggle()
    {
        SetOn(!_isOn, notify: true);
    }

    public void SetOn(bool isOn, bool notify)
    {
        _isOn = isOn;
        ApplyVisual();

        if (notify)
            _onChanged?.Invoke(_isOn);
    }

    private void ApplyVisual()
    {
        // 프로토타입: On이면 조금 커짐
        transform.localScale = _isOn ? Vector3.one * 0.35f : Vector3.one * 0.25f;
    }

    // ===== IRewindable =====
    public object CaptureState()
    {
        return new SwitchState { _isOn = _isOn };
    }

    public void RestoreState(object state)
    {
        if (state is not SwitchState s)
        {
            Debug.LogWarning("[SwitchController] RestoreState fallback: invalid state type.");
            return;
        }

        if (_grid == null || _presenter == null)
        {
            Debug.LogWarning("[SwitchController] RestoreState fallback: grid/presenter is null.");
            return;
        }

        // Restore 중에는 Door를 건드리면 “순서 의존” 생김 -> notify=false
        SetOn(s._isOn, notify: false);
    }
}
