// TileOverlayLayer.cs
//
// Goal: Floor(Base) + Goal(Overlay)
// Hole: Overlay만 표시(스프라이트 있으면 Proto2D가 절대 보이면 안 됨 → Base는 GridPresenter가 숨김)
// FilledHole: Floor(Base) + FilledHole(Overlay) (스프라이트 없으면 Warning + Proto2D 폴백)
//
using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class TileOverlayLayer : MonoBehaviour
{
    private BoardGrid _grid;
    private GridPresenter _presenter;
    private ITileSpriteProvider _tileSprites;

    private readonly Dictionary<int, SpriteRenderer> _srByIdx = new Dictionary<int, SpriteRenderer>(128);
    private readonly HashSet<E_TileVisualKey> _warnedMissingKey = new HashSet<E_TileVisualKey>();

    private Action<Vector2Int, CellMeta> _metaCb;

    private bool _warnedNoPresenter;
    private bool _warnedNoProvider;

    [SerializeField] private float _overlayScaleMul = 0.92f;
    [SerializeField] private int _sortingOrder = (int)E_ProtoSort.TileOverlay;

    public void Initialize(BoardGrid grid, GridPresenter presenter, ITileSpriteProvider tileSprites)
    {
        _grid = grid;
        _presenter = presenter;
        _tileSprites = tileSprites;

        if (_grid == null)
        {
            Debug.LogWarning("[TileOverlayLayer] Initialize fallback: grid is null.");
            return;
        }

        if (_presenter == null && !_warnedNoPresenter)
        {
            _warnedNoPresenter = true;
            Debug.LogWarning("[TileOverlayLayer] presenter is null. (skip overlay visuals)");
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

        for (int y = 0; y < _grid._h; y++)
            for (int x = 0; x < _grid._w; x++)
            {
                var c = new Vector2Int(x, y);
                ApplyCell(c);
            }
    }

    private void OnMetaChanged(Vector2Int cell, CellMeta meta)
    {
        ApplyCell(cell);
    }

    private void ApplyCell(Vector2Int cell)
    {
        if (_grid == null) return;
        if (!_grid.IsInBounds(cell)) return;

        var meta = _grid.GetMeta(cell);
        var cellType = _grid.GetCell(cell);

        bool wantsOverlay =
            (cellType == E_CellType.Goal) ||
            meta.IsOpenHole ||
            meta.IsFilledHole;

        int idx = _grid.ToIndex(cell);

        if (!wantsOverlay)
        {
            if (_srByIdx.TryGetValue(idx, out var existing) && existing != null)
                existing.enabled = false;
            return;
        }

        if (_presenter == null)
            return;

        if (!_srByIdx.TryGetValue(idx, out var sr) || sr == null)
        {
            var go = Proto2DVisual.CreateSpriteObject(
                name: $"Overlay({cell.x},{cell.y})",
                parent: transform,
                sortingOrder: _sortingOrder,
                color: Color.white,
                localScale: Vector3.one
            );

            go.transform.position = _presenter.CellToWorld(cell);

            float s = Mathf.Max(0.01f, _presenter._tileScale) * Mathf.Max(0.01f, _overlayScaleMul);
            go.transform.localScale = new Vector3(s, s, 1f);

            sr = go.GetComponent<SpriteRenderer>();
            _srByIdx[idx] = sr;
        }

        sr.enabled = true;

        // 키 결정(우선순위: Goal > Hole/FilledHole)
        E_TileVisualKey key;
        if (cellType == E_CellType.Goal)
            key = E_TileVisualKey.Goal;
        else if (meta.IsOpenHole)
            key = E_TileVisualKey.Hole;
        else
            key = E_TileVisualKey.HoleFilled;

        ApplyOverlaySprite(sr, key);
    }

    private void ApplyOverlaySprite(SpriteRenderer sr, E_TileVisualKey key)
    {
        if (sr == null) return;

        if (_tileSprites == null)
        {
            if (!_warnedNoProvider)
            {
                _warnedNoProvider = true;
                Debug.LogWarning("[TileOverlayLayer] TileSpriteProvider is null. (overlay keeps proto)");
            }

            ApplyProtoFallback(sr, key);
            return;
        }

        if (_tileSprites.TryGetSprite(key, out var sp) && sp != null)
        {
            sr.sprite = sp;
            sr.color = Color.white;
            return;
        }

        if (!_warnedMissingKey.Contains(key))
        {
            _warnedMissingKey.Add(key);
            Debug.LogWarning($"[TileOverlayLayer] Sprite missing. key={key} (fallback: Proto2D)");
        }

        ApplyProtoFallback(sr, key);
    }

    private static void ApplyProtoFallback(SpriteRenderer sr, E_TileVisualKey key)
    {
        sr.sprite = Proto2DVisual.Sprite;

        sr.color = key switch
        {
            E_TileVisualKey.Goal => Proto2DVisual.TileGoal,
            E_TileVisualKey.Hole => Proto2DVisual.TileHole,
            E_TileVisualKey.HoleFilled => new Color(0.15f, 0.15f, 0.15f, 1f),
            _ => Color.white
        };
    }
}
