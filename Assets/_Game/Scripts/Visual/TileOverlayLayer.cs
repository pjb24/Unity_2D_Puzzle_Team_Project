// TileOverlayLayer.cs
///
/// Goal: Floor(Base) + Goal(Overlay)
/// Hole: Overlay만 표시(스프라이트 없으면 "아예 생성하지 않음")
/// FilledHole: Floor(Base) + FilledHole(Overlay) (스프라이트 없으면 생성하지 않음)
///
using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class TileOverlayLayer : MonoBehaviour
{
    private BoardGrid _grid;
    private GridPresenter _presenter;
    private ITileSpriteProvider _tileSprites;

    private readonly Dictionary<int, GameObject> _goByIdx = new Dictionary<int, GameObject>();
    private readonly Dictionary<int, SpriteRenderer> _srByIdx = new Dictionary<int, SpriteRenderer>();
    private readonly HashSet<E_TileVisualKey> _warnedMissingKey = new HashSet<E_TileVisualKey>();

    private Action<Vector2Int, CellMeta> _metaCb;

    private bool _warnedNoPresenter;
    private bool _warnedNoProvider;

    [SerializeField] private float _overlayScaleMul = 1.00f;
    [SerializeField] private int _sortingOrder = (int)E_SortingOrder.Hole;

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

        _metaCb ??= OnMetaChanged;
        _grid.AddListenerOnMetaChanged(_metaCb);

        RebuildAll();
    }

    private void OnDestroy()
    {
        if (_grid != null && _metaCb != null)
            _grid.RemoveListenerOnMetaChanged(_metaCb);

        DestroyAll();
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

        int idx = _grid.ToIndex(cell);

        // 오버레이 필요 여부 결정
        var meta = _grid.GetMeta(cell);
        var cellType = _grid.GetCell(cell);

        bool needsOverlay = (cellType == E_CellType.Goal) || meta.IsHole;
        if (!needsOverlay)
        {
            DestroyOverlay(idx);
            return;
        }

        if (_presenter == null)
        {
            if (!_warnedNoPresenter)
            {
                _warnedNoPresenter = true;
                Debug.LogWarning("[TileOverlayLayer] presenter is null. overlays will not be created.");
            }
            DestroyOverlay(idx);
            return;
        }

        if (_tileSprites == null)
        {
            if (!_warnedNoProvider)
            {
                _warnedNoProvider = true;
                Debug.LogWarning("[TileOverlayLayer] tile sprite provider is null. overlays will not be created.");
            }
            DestroyOverlay(idx);
            return;
        }

        // 키 결정(우선순위: Goal > Hole/FilledHole)
        E_TileVisualKey key;
        if (cellType == E_CellType.Goal)
            key = E_TileVisualKey.Goal;
        else if (meta.IsOpenHole)
            key = E_TileVisualKey.Hole;
        else
            key = E_TileVisualKey.HoleFilled;

        var selector = TileSelector.Make(E_TileLayer.Overlay, key);

        // 스프라이트 없으면 "생성/유지 금지"
        if (!_tileSprites.TryGetSprite(in selector, out var sprite) || sprite == null)
        {
            DestroyOverlay(idx);
            return;
        }

        // 여기부터 생성
        var sr = EnsureOverlay(idx, cell);
        if (sr == null)
        {
            Debug.LogWarning($"[TileOverlayLayer] EnsureOverlay fallback: SpriteRenderer missing. cell={cell}");
            DestroyOverlay(idx);
            return;
        }

        sr.enabled = true;
        sr.sprite = sprite;
        sr.color = Color.white;
    }

    private SpriteRenderer EnsureOverlay(int idx, Vector2Int cell)
    {
        if (_srByIdx.TryGetValue(idx, out var sr) && sr != null)
        {
            sr.transform.position = _presenter.CellToWorld(cell);
            return sr;
        }

        var go = new GameObject($"Overlay({cell.x},{cell.y})");
        go.transform.SetParent(transform, false);
        go.transform.position = _presenter.CellToWorld(cell);

        float s = Mathf.Max(0.01f, _presenter._tileScale) * Mathf.Max(0.01f, _overlayScaleMul);
        go.transform.localScale = new Vector3(s, s, 1f);

        sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = _sortingOrder;
        sr.color = Color.white;

        _goByIdx[idx] = go;
        _srByIdx[idx] = sr;
        return sr;
    }

    private void DestroyOverlay(int idx)
    {
        if (_goByIdx.TryGetValue(idx, out var go) && go != null)
            Destroy(go);

        _goByIdx.Remove(idx);
        _srByIdx.Remove(idx);
    }

    private void DestroyAll()
    {
        foreach (var kv in _goByIdx)
        {
            if (kv.Value != null) Destroy(kv.Value);
        }
        _goByIdx.Clear();
        _srByIdx.Clear();
    }
}
