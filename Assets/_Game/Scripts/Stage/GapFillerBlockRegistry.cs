// GapFillerBlockRegistry.cs
using System.Collections.Generic;
using UnityEngine;

public class GapFillerBlockRegistry : MonoBehaviour
{
    private readonly Dictionary<Vector2Int, GapFillerBlockController> _map = new();

    public bool TryGet(Vector2Int cell, out GapFillerBlockController block) => _map.TryGetValue(cell, out block);

    public void Register(GapFillerBlockController block, Vector2Int cell)
    {
        if (block == null)
        {
            Debug.LogWarning("[GapFillerBlockRegistry] Register fallback: block is null.");
            return;
        }

        if (_map.ContainsKey(cell))
        {
            Debug.LogWarning($"[GapFillerBlockRegistry] Register fallback: duplicated cell. cell={cell}");
            return;
        }

        _map.Add(cell, block);
    }

    public void Unregister(Vector2Int cell, GapFillerBlockController block)
    {
        if (!_map.TryGetValue(cell, out var cur))
            return;

        if (cur != block)
            return;

        _map.Remove(cell);
    }

    public void Move(Vector2Int from, Vector2Int to, GapFillerBlockController block)
    {
        Unregister(from, block);
        Register(block, to);
    }
}
