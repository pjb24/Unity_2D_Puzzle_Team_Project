// HoleVisualRegistry.cs
using System;
using System.Collections.Generic;
using UnityEngine;

public class HoleVisualRegistry : MonoBehaviour, IRewindable
{
    [Serializable]
    public struct HoleState
    {
        public int _x;
        public int _y;
        public bool _filled;
    }

    [Serializable]
    public struct RegistryState
    {
        public HoleState[] _holes;
    }

    private class HoleEntry
    {
        public GameObject _holeGo;
        public GameObject _filledGo;
        public bool _isFilled;
    }

    private readonly Dictionary<Vector2Int, HoleEntry> _holes = new();
    private readonly List<Vector2Int> _order = new();
    private BoardGrid _grid;
    private GameObject _filledHolePrefab;

    public void Configure(BoardGrid grid, GameObject filledHolePrefab)
    {
        _grid = grid;
        _filledHolePrefab = filledHolePrefab;
    }

    public void RegisterHole(Vector2Int cell, GameObject holeGo)
    {
        if (holeGo == null)
        {
            Debug.LogWarning($"[HoleVisualRegistry] RegisterHole fallback: holeGo is null. cell={cell}");
            return;
        }

        if (_holes.ContainsKey(cell))
        {
            Debug.LogWarning($"[HoleVisualRegistry] RegisterHole fallback: duplicate cell. cell={cell}");
            return;
        }

        _holes[cell] = new HoleEntry
        {
            _holeGo = holeGo,
            _filledGo = null,
            _isFilled = false
        };
        _order.Add(cell);
    }

    public void FillHole(Vector2Int cell)
    {
        ApplyFillState(cell, filled: true, updateGrid: true);
    }

    public object CaptureState()
    {
        var states = new HoleState[_order.Count];
        for (int i = 0; i < _order.Count; i++)
        {
            var cell = _order[i];
            if (_holes.TryGetValue(cell, out var entry))
            {
                states[i] = new HoleState
                {
                    _x = cell.x,
                    _y = cell.y,
                    _filled = entry._isFilled
                };
            }
        }

        return new RegistryState { _holes = states };
    }

    public void RestoreState(object state)
    {
        if (state is not RegistryState s || s._holes == null)
        {
            Debug.LogWarning("[HoleVisualRegistry] RestoreState fallback: invalid state type.");
            return;
        }

        var seen = new HashSet<Vector2Int>();
        for (int i = 0; i < s._holes.Length; i++)
        {
            var h = s._holes[i];
            var cell = new Vector2Int(h._x, h._y);
            seen.Add(cell);
            ApplyFillState(cell, h._filled, updateGrid: true);
        }

        for (int i = 0; i < _order.Count; i++)
        {
            var cell = _order[i];
            if (!seen.Contains(cell))
                ApplyFillState(cell, filled: false, updateGrid: true);
        }
    }

    private void ApplyFillState(Vector2Int cell, bool filled, bool updateGrid)
    {
        if (!_holes.TryGetValue(cell, out var entry) || entry._holeGo == null)
        {
            Debug.LogWarning($"[HoleVisualRegistry] ApplyFillState fallback: hole not found. cell={cell}");
            return;
        }

        if (filled)
        {
            if (_filledHolePrefab == null)
            {
                Debug.LogWarning($"[HoleVisualRegistry] ApplyFillState fallback: filledHole prefab is null. cell={cell}");
                return;
            }

            if (entry._filledGo == null)
            {
                var parent = entry._holeGo.transform.parent;
                var filledGo = Instantiate(_filledHolePrefab, parent);
                filledGo.name = _filledHolePrefab.name;
                filledGo.transform.localPosition = entry._holeGo.transform.localPosition;
                filledGo.transform.localRotation = entry._holeGo.transform.localRotation;
                filledGo.transform.localScale = entry._holeGo.transform.localScale;
                entry._filledGo = filledGo;
            }

            entry._filledGo.SetActive(true);
            entry._holeGo.SetActive(false);
            entry._isFilled = true;

            if (updateGrid && _grid != null)
                _grid.SetCellOverlay01(cell, E_CellType.FilledHole);
        }
        else
        {
            if (entry._filledGo != null)
                entry._filledGo.SetActive(false);

            entry._holeGo.SetActive(true);
            entry._isFilled = false;

            if (updateGrid && _grid != null)
                _grid.SetCellOverlay01(cell, E_CellType.Hole);
        }
    }
}
