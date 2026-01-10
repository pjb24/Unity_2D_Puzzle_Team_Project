// HoleVisualLayer.cs
///
/// meta.IsHole인 칸만 표시.
/// 스프라이트 없으면 생성/유지 금지 (Proto2D 폴백 없음)
///
using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class HoleVisualLayer : MonoBehaviour
{
    [SerializeField] private float _scaleMul = 1.00f;
    [SerializeField] private int _sortingOrder = (int)E_SortingOrder.Hole;

    private BoardGrid _grid;
    private GridPresenter _presenter;
    private ITileSpriteProvider _tileSprites;

    private readonly Dictionary<int, GameObject> _goByIdx = new Dictionary<int, GameObject>();
    private readonly Dictionary<int, SpriteRenderer> _srByIdx = new Dictionary<int, SpriteRenderer>();

    private Action<Vector2Int, CellMeta> _metaCb;

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

        DestroyAll();
    }

    private void RebuildAll()
    {
        if (_grid == null) return;

        // 간단: 필요한 칸만 생성/갱신
        for (int y = 0; y < _grid._h; y++)
            for (int x = 0; x < _grid._w; x++)
            {
                var c = new Vector2Int(x, y);
                ApplyCell(c, _grid.GetMeta(c));
            }
    }

    private void OnMetaChanged(Vector2Int cell, CellMeta meta)
    {
        ApplyCell(cell, meta);
    }

    private void ApplyCell(Vector2Int cell, CellMeta meta)
    {
        if (_grid == null) return;

        int idx = _grid.ToIndex(cell);

        if (!meta.IsHole)
        {
            DestroyHole(idx);
            return;
        }

        if (_presenter == null)
        {
            if (!_warnedNoPresenter)
            {
                _warnedNoPresenter = true;
                Debug.LogWarning("[HoleVisualLayer] presenter is null. holes will not be created.");
            }
            DestroyHole(idx);
            return;
        }

        if (_tileSprites == null)
        {
            if (!_warnedNoProvider)
            {
                _warnedNoProvider = true;
                Debug.LogWarning("[HoleVisualLayer] tile sprite provider is null. holes will not be created.");
            }
            DestroyHole(idx);
            return;
        }

        var key = meta._isFilledHole ? E_TileVisualKey.HoleFilled : E_TileVisualKey.Hole;
        var selector = TileSelector.Make(E_TileLayer.Overlay, key);

        if (!_tileSprites.TryGetSprite(in selector, out var sprite) || sprite == null)
        {
            DestroyHole(idx);
            return;
        }

        var sr = EnsureHole(idx, cell);
        if (sr == null)
        {
            Debug.LogWarning($"[HoleVisualLayer] EnsureHole fallback: SpriteRenderer missing. cell={cell}");
            DestroyHole(idx);
            return;
        }

        sr.enabled = true;
        sr.sprite = sprite;
        sr.color = Color.white;
    }

    private SpriteRenderer EnsureHole(int idx, Vector2Int cell)
    {
        if (_srByIdx.TryGetValue(idx, out var sr) && sr != null)
        {
            sr.transform.position = _presenter.CellToWorld(cell);
            return sr;
        }

        var go = new GameObject($"Hole({cell.x},{cell.y})");
        go.transform.SetParent(transform, false);
        go.transform.position = _presenter.CellToWorld(cell);

        float s = Mathf.Max(0.01f, _presenter._tileSize) * Mathf.Max(0.01f, _scaleMul);
        go.transform.localScale = new Vector3(s, s, 1f);

        sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = _sortingOrder;
        sr.color = Color.white;

        _goByIdx[idx] = go;
        _srByIdx[idx] = sr;
        return sr;
    }

    private void DestroyHole(int idx)
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
