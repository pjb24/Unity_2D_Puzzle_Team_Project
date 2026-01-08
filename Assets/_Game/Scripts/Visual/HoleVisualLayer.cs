// HoleVisualLayer.cs
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class HoleVisualLayer : MonoBehaviour
{
    private BoardGrid _grid;
    private GridPresenter _presenter;
    private ITileSpriteProvider _tileSprites;

    private readonly Dictionary<int, SpriteRenderer> _renderersByIndex = new();
    private System.Action<Vector2Int, CellMeta> _metaCb;

    private bool _warnedNoProvider;
    private bool _warnedNoPresenter;

    public void Initialize(BoardGrid grid, GridPresenter presenter, ITileSpriteProvider tileSprites)
    {
        _grid = grid;
        _presenter = presenter;
        _tileSprites = tileSprites;

        if (_grid == null)
        {
            Debug.LogWarning("[HoleVisualLayer] Initialize fallback: grid is null.");
            return;
        }

        _metaCb ??= OnMetaChanged;
        _grid.AddListenerOnMetaChanged(_metaCb);

        RebuildAll();
    }

    private void OnDestroy()
    {
        if (_grid != null && _metaCb != null)
            _grid.RemoveListenerOnMetaChanged(_metaCb);
    }

    private void RebuildAll()
    {
        if (_grid == null) return;

        // 간단: 필요한 칸만 생성/갱신
        for (int y = 0; y < _grid._h; y++)
            for (int x = 0; x < _grid._w; x++)
                ApplyCell(new Vector2Int(x, y), _grid.GetMeta(new Vector2Int(x, y)));
    }

    private void OnMetaChanged(Vector2Int cell, CellMeta meta)
    {
        ApplyCell(cell, meta);
    }

    private void ApplyCell(Vector2Int cell, CellMeta meta)
    {
        if (_grid == null) return;

        int idx = _grid.ToIndex(cell);

        bool isHoleSurface = meta.IsHole;
        if (!isHoleSurface)
        {
            if (_renderersByIndex.TryGetValue(idx, out var existing) && existing != null)
                existing.enabled = false;
            return;
        }

        if (_presenter == null)
        {
            if (!_warnedNoPresenter)
            {
                _warnedNoPresenter = true;
                Debug.LogWarning("[HoleVisualLayer] presenter is null (fallback: skip hole visuals).");
            }
            return;
        }

        if (!_renderersByIndex.TryGetValue(idx, out var sr) || sr == null)
        {
            var go = Proto2DVisual.CreateSpriteObject(
                name: $"Hole({cell.x},{cell.y})",
                parent: transform,
                sortingOrder: (int)E_ProtoSort.Hole,
                color: Color.white,
                localScale: Vector3.one
            );
            go.transform.position = _presenter.CellToWorld(cell);
            sr = go.GetComponent<SpriteRenderer>();
            _renderersByIndex[idx] = sr;
        }

        sr.enabled = true;

        if (_tileSprites == null)
        {
            if (!_warnedNoProvider)
            {
                _warnedNoProvider = true;
                Debug.LogWarning("[HoleVisualLayer] tile sprite provider is null (fallback: keep current hole sprites).");
            }
            return;
        }

        var key = meta._isFilledHole ? E_TileVisualKey.HoleFilled : E_TileVisualKey.Hole;
        if (!_tileSprites.TryGetSprite(key, out var sprite) || sprite == null)
        {
            Debug.LogWarning($"[HoleVisualLayer] hole sprite missing. key={key} (fallback: keep current).");
            return;
        }

        sr.sprite = sprite;
        sr.color = Color.white;
    }
}
