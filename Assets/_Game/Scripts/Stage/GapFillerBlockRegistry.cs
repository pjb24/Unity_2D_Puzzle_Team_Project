// GapFillerBlockRegistry.cs
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class GapFillerBlockRegistry : MonoBehaviour
{
    private readonly Dictionary<Vector2Int, GapFillerBlockController> _map = new();

    private RectInt _moveBounds;
    private bool _hasMoveBounds;
    private bool _warnedNotConfigured;

    public bool TryGet(Vector2Int cell, out GapFillerBlockController block) => _map.TryGetValue(cell, out block);

    public void ConfigureMoveBounds(RectInt rawMoveBounds, BoardGrid grid)
    {
        if (grid == null)
        {
            Debug.LogWarning("[GapFillerBlockRegistry] ConfigureMoveBounds fallback: grid is null.");
            _hasMoveBounds = false;
            _moveBounds = default;
            return;
        }

        // 유효하지 않으면 “보드 전체”로 폴백 + Warning (무음 금지)
        if (rawMoveBounds.width <= 0 || rawMoveBounds.height <= 0)
        {
            Debug.LogWarning($"[GapFillerBlockRegistry] MoveBounds fallback: invalid rect. raw={rawMoveBounds} -> use full board.");
            _hasMoveBounds = false; // full board == 제한 없음
            _moveBounds = new RectInt(0, 0, grid._w, grid._h);
            return;
        }

        RectInt clamped = ClampRectToGrid(rawMoveBounds, grid._w, grid._h);
        if (clamped != rawMoveBounds)
            Debug.LogWarning($"[GapFillerBlockRegistry] MoveBounds clamped. raw={rawMoveBounds} clamped={clamped}");

        _moveBounds = clamped;
        _hasMoveBounds = true;
        _warnedNotConfigured = false;
    }

    public bool IsAllowedCell(Vector2Int cell, BoardGrid grid)
    {
        if (grid == null)
        {
            Debug.LogWarning("[GapFillerBlockRegistry] IsAllowedCell fallback: grid is null.");
            return false;
        }

        if (!grid.IsInBounds(cell))
            return false;

        if (!_hasMoveBounds)
        {
            if (!_warnedNotConfigured)
            {
                _warnedNotConfigured = true;
                Debug.LogWarning("[GapFillerBlockRegistry] MoveBounds not configured. (GapFillerBlock will use full board bounds)");
            }
            return true;
        }

        return _moveBounds.Contains(cell);
    }

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

    private static RectInt ClampRectToGrid(RectInt r, int w, int h)
    {
        int xMin = Mathf.Clamp(r.xMin, 0, w);
        int yMin = Mathf.Clamp(r.yMin, 0, h);
        int xMax = Mathf.Clamp(r.xMax, 0, w);
        int yMax = Mathf.Clamp(r.yMax, 0, h);

        int width = Mathf.Max(0, xMax - xMin);
        int height = Mathf.Max(0, yMax - yMin);
        return new RectInt(xMin, yMin, width, height);
    }
}
