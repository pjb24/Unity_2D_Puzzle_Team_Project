// HoleVisualRegistry.cs
using System.Collections.Generic;
using UnityEngine;

public class HoleVisualRegistry : MonoBehaviour
{
    private readonly Dictionary<Vector2Int, GameObject> _holes = new();
    private GameObject _filledHolePrefab;

    public void Configure(GameObject filledHolePrefab)
    {
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

        _holes[cell] = holeGo;
    }

    public void FillHole(Vector2Int cell)
    {
        if (!_holes.TryGetValue(cell, out var holeGo) || holeGo == null)
        {
            Debug.LogWarning($"[HoleVisualRegistry] FillHole fallback: hole not found. cell={cell}");
            return;
        }

        if (_filledHolePrefab == null)
        {
            Debug.LogWarning($"[HoleVisualRegistry] FillHole fallback: filledHole prefab is null. cell={cell}");
            return;
        }

        var parent = holeGo.transform.parent;
        var filledGo = Instantiate(_filledHolePrefab, parent);
        filledGo.name = _filledHolePrefab.name;
        filledGo.transform.localPosition = holeGo.transform.localPosition;
        filledGo.transform.localRotation = holeGo.transform.localRotation;
        filledGo.transform.localScale = holeGo.transform.localScale;
        holeGo.SetActive(false);
        _holes[cell] = filledGo;
    }
}
