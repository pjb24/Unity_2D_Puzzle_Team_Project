// GridPresenter.cs
///
/// 표현 레이어: “월드 좌표 ↔ 셀” 변환 규칙 고정
/// DummyStageLoader가 타일을 “중앙 정렬 + tileSize”로 깔고 있다.
/// 타일 생성 파이프라인을 GridPresenter.BuildTiles() 하나로 통일
/// 
/// - 타일 생성 루프에서 "스프라이트가 없으면" GameObject/SpriteRenderer를 만들지 않는다.
/// - 이미 만들어진 타일이더라도, 이후 TryGet 실패/열린 Hole 등으로 "표시 불가"가 되면 즉시 Destroy.
///
using System.Collections.Generic;
using UnityEngine;

public class GridPresenter
{
    public readonly float _tileScale;
    public readonly float _cellPitch;
    public readonly Vector3 _originLocal; // root 기준 원점(셀 0,0의 중심)
    public readonly Transform _root;      // StageRuntime root

    private readonly int _w;
    private readonly int _h;

    private Transform _tilesRoot;
    private BoardGrid _boundGrid;
    private System.Action<Vector2Int, CellMeta> _metaListener;

    private ITileSpriteProvider _tileSpriteProvider;
    private bool _warnedNoProvider;

    private RectInt _innerBaseRect;
    private bool _hasInnerBaseRect;

    // idx = y * w + x
    private SpriteRenderer[] _tileRenderers;
    private GameObject[] _tileObjects;

    public GridPresenter(Transform root, int w, int h, float tileScale, float tileGap)
    {
        _root = root;
        _w = Mathf.Max(1, w);
        _h = Mathf.Max(1, h);

        _tileScale = Mathf.Max(0.01f, tileScale);
        float gap = Mathf.Max(0f, tileGap);
        _cellPitch = Mathf.Max(0.01f, _tileScale + gap);

        // DummyStageLoader와 동일한 중앙정렬 규칙(단, pitch 기준)
        _originLocal = new Vector3(
            -(w - 1) * 0.5f * _cellPitch,
            -(h - 1) * 0.5f * _cellPitch,
            0f);
    }

    public void SetTileSpriteProvider(ITileSpriteProvider provider)
    {
        _tileSpriteProvider = provider;

        if (_tileSpriteProvider == null && !_warnedNoProvider)
        {
            _warnedNoProvider = true;
            Debug.LogWarning("[GridPresenter] TileSpriteProvider is null. Tiles will not be created.");
        }

        // 이미 타일이 만들어져 있으면 즉시 재적용
        if (_boundGrid != null && _tilesRoot != null)
        {
            for (int y = 0; y < _h; y++)
                for (int x = 0; x < _w; x++)
                    RefreshTile(_boundGrid, new Vector2Int(x, y));
        }
    }

    public void SetInnerBaseRect(RectInt rect)
    {
        if (rect.width <= 0 || rect.height <= 0)
        {
            Debug.LogWarning($"[GridPresenter] SetInnerBaseRect fallback: invalid rect. rect={rect} (gap tile disabled)");
            _hasInnerBaseRect = false;
        }
        else
        {
            _innerBaseRect = rect;
            _hasInnerBaseRect = true;
        }

        // 즉시 재적용
        if (_boundGrid != null && _tilesRoot != null)
        {
            for (int y = 0; y < _h; y++)
                for (int x = 0; x < _w; x++)
                    RefreshTile(_boundGrid, new Vector2Int(x, y));
        }
    }

    public Vector3 CellToWorld(Vector2Int c)
    {
        // 2D 탑뷰: (x,y) 그대로 매핑
        Vector3 local = _originLocal + new Vector3(c.x * _cellPitch, c.y * _cellPitch, 0f);
        return _root.TransformPoint(local);
    }

    public Transform BuildTiles(BoardGrid grid, List<Transform> outTiles = null)
    {
        if (_root == null)
        {
            Debug.LogWarning("[GridPresenter] BuildTiles fallback: root is null.");
            return null;
        }

        if (grid == null)
        {
            Debug.LogWarning("[GridPresenter] BuildTiles fallback: grid is null.");
            return null;
        }

        // 기존 바인딩 정리
        UnbindMetaListener();

        // 기존 타일 루트가 있으면 제거(중복 방지)
        if (_tilesRoot != null)
        {
            Debug.LogWarning("[GridPresenter] BuildTiles: previous tilesRoot exists. Destroy and rebuild.");
            Object.Destroy(_tilesRoot.gameObject);
            _tilesRoot = null;
        }

        int total = _w * _h;
        _tileRenderers = new SpriteRenderer[total];
        _tileObjects = new GameObject[total];

        var goRoot = new GameObject("[Tiles]");
        goRoot.transform.SetParent(_root, false);
        _tilesRoot = goRoot.transform;

        if (outTiles != null) outTiles.Clear();

        // 루프에서 TryGet 실패면 "생성 스킵"
        for (int y = 0; y < _h; y++)
        {
            for (int x = 0; x < _w; x++)
            {
                var cell = new Vector2Int(x, y);
                RefreshTile(grid, cell);

                int idx = ToIndex(cell);
                if (outTiles != null && _tileRenderers[idx] != null)
                    outTiles.Add(_tileRenderers[idx].transform);
            }
        }

        BindMetaListener(grid);
        return _tilesRoot;
    }

    public void ApplyCellChange(Vector2Int cell)
    {
        if (_boundGrid == null)
        {
            Debug.LogWarning($"[GridPresenter] ApplyCellChange fallback: bound grid is null. cell={cell}");
            return;
        }

        RefreshTile(_boundGrid, cell);
    }

    public void RefreshTile(BoardGrid grid, Vector2Int cell)
    {
        if (grid == null)
        {
            Debug.LogWarning("[GridPresenter] RefreshTile fallback: grid is null.");
            return;
        }

        if (!grid.IsInBounds(cell))
        {
            Debug.LogWarning($"[GridPresenter] RefreshTile fallback: out of bounds. cell={cell}");
            return;
        }

        if (_tileRenderers == null || _tileObjects == null)
        {
            Debug.LogWarning("[GridPresenter] RefreshTile fallback: tiles not built yet.");
            return;
        }

        int idx = ToIndex(cell);

        var meta = grid.GetMeta(cell);
        var cellType = grid.GetCell(cell);

        // ===== Overlay 모드: Hole은 Base 숨김, Goal은 Base=Floor =====
        if (meta.IsOpenHole)
        {
            DestroyTileAt(idx); // Hole 스프라이트가 있으면 Proto2D가 절대 보이면 안 됨
            return;
        }

        // Provider 없으면 생성 금지(= 스킵). 기존 타일이 있으면 제거.
        if (_tileSpriteProvider == null)
        {
            DestroyTileAt(idx);
            return;
        }

        // Base selector 계산
        if (!TryBuildBaseSelector(grid, cell, cellType, meta, out var selector))
        {
            DestroyTileAt(idx);
            return;
        }

        // 스프라이트 없으면 생성/유지 안 함
        if (!_tileSpriteProvider.TryGetSprite(in selector, out var sprite) || sprite == null)
        {
            DestroyTileAt(idx);
            return;
        }

        // 여기부터는 반드시 생성/유지
        var sr = EnsureTileAt(idx, cell);
        if (sr == null)
        {
            Debug.LogWarning($"[GridPresenter] EnsureTileAt fallback: SpriteRenderer missing. cell={cell}");
            return;
        }

        sr.sprite = sprite;
        sr.color = Color.white;
        sr.enabled = true;
    }

    // ----- Internals -----

    private int ToIndex(Vector2Int c) => c.y * _w + c.x;

    private bool TryBuildBaseSelector(BoardGrid grid, Vector2Int cell, E_CellType cellType, CellMeta meta, out TileSelector selector)
    {
        // 정적 막힘은 항상 그 키를 사용
        E_TileVisualKey baseKey;

        if (cellType == E_CellType.Wall) baseKey = E_TileVisualKey.Wall;
        else if (cellType == E_CellType.Obstacle) baseKey = E_TileVisualKey.Obstacle;
        else
        {
            // Gap 표시는 meta.IsGap 또는 InnerBaseRect 외부로 결정
            bool isGap = meta.IsGap || (_hasInnerBaseRect && !_innerBaseRect.Contains(cell));

            if (IsPerimeter(cell))
                baseKey = E_TileVisualKey.Path;
            else if (isGap)
                baseKey = E_TileVisualKey.InnerOuterGap;
            else
                baseKey = E_TileVisualKey.Floor; // Goal 포함: Base는 항상 Floor
        }

        var layer = ResolveLayerForBaseKey(baseKey);
        selector = TileSelector.Make(layer, baseKey);
        return true;
    }

    private static E_TileLayer ResolveLayerForBaseKey(E_TileVisualKey key)
    {
        switch (key)
        {
            case E_TileVisualKey.Path:
            case E_TileVisualKey.InnerOuterGap:
            case E_TileVisualKey.DoorOpen:
            case E_TileVisualKey.DoorClosed:
            case E_TileVisualKey.Goal:
                return E_TileLayer.Ring;

            case E_TileVisualKey.GapFillerBlock:
                return E_TileLayer.Block;

            default:
                return E_TileLayer.InnerBase;
        }
    }

    private bool IsPerimeter(Vector2Int c)
    {
        return c.x == 0 || c.y == 0 || c.x == _w - 1 || c.y == _h - 1;
    }

    private SpriteRenderer EnsureTileAt(int idx, Vector2Int cell)
    {
        var existing = _tileRenderers[idx];
        if (existing != null)
        {
            // 위치 보정(타일 피치/오리진 변경 등 대응)
            existing.transform.position = CellToWorld(cell);
            return existing;
        }

        if (_tilesRoot == null)
        {
            Debug.LogWarning("[GridPresenter] EnsureTileAt fallback: tilesRoot is null.");
            return null;
        }

        var go = new GameObject($"Tile({cell.x},{cell.y})");
        go.transform.SetParent(_tilesRoot, false);
        go.transform.localScale = new Vector3(_tileScale, _tileScale, 1f);
        go.transform.position = CellToWorld(cell);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = (int)E_ProtoSort.Tile; // 기존 규칙 유지
        sr.color = Color.white;

        _tileObjects[idx] = go;
        _tileRenderers[idx] = sr;
        return sr;
    }

    private void DestroyTileAt(int idx)
    {
        var go = _tileObjects[idx];
        if (go != null)
        {
            Object.Destroy(go);
        }

        _tileObjects[idx] = null;
        _tileRenderers[idx] = null;
    }

    private void BindMetaListener(BoardGrid grid)
    {
        if (_boundGrid == grid) return;

        _boundGrid = grid;

        _metaListener = (cell, meta) =>
        {
            // cell만 갱신
            RefreshTile(_boundGrid, cell);
        };

        _boundGrid.AddListenerOnMetaChanged(_metaListener);
    }

    private void UnbindMetaListener()
    {
        if (_boundGrid != null && _metaListener != null)
            _boundGrid.RemoveListenerOnMetaChanged(_metaListener);

        _boundGrid = null;
        _metaListener = null;
    }
}
