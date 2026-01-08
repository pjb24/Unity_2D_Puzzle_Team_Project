// GridPresenter.cs
///
/// 표현 레이어: “월드 좌표 ↔ 셀” 변환 규칙 고정
/// DummyStageLoader가 타일을 “중앙 정렬 + tileSize”로 깔고 있다.
/// 타일 생성 파이프라인을 GridPresenter.BuildTiles() 하나로 통일
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

    private readonly List<SpriteRenderer> _tileRenderers = new List<SpriteRenderer>(256);

    private Transform _tilesRoot;
    private BoardGrid _boundGrid;
    private System.Action<Vector2Int, CellMeta> _metaListener;

    private ITileSpriteProvider _tileSpriteProvider;
    private bool _warnedNoProvider;

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

        _tileRenderers.Clear();

        var goRoot = new GameObject("[Tiles]");
        goRoot.transform.SetParent(_root, false);
        _tilesRoot = goRoot.transform;

        if (outTiles != null) outTiles.Clear();

        for (int y = 0; y < _h; y++)
        {
            for (int x = 0; x < _w; x++)
            {
                var cell = new Vector2Int(x, y);

                var go = Proto2DVisual.CreateSpriteObject(
                    name: $"Tile({x},{y})",
                    parent: _tilesRoot,
                    sortingOrder: (int)E_ProtoSort.Tile,
                    color: Proto2DVisual.TileFloor,
                    localScale: new Vector3(_tileScale, _tileScale, 1f)
                );

                go.transform.position = CellToWorld(cell);

                var sr = go.GetComponent<SpriteRenderer>();
                _tileRenderers.Add(sr);
                outTiles?.Add(sr.transform);

                RefreshTile(grid, cell);
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

        int idx = cell.y * _w + cell.x;
        var sr = _tileRenderers[idx];
        if (sr == null)
        {
            Debug.LogWarning($"[GridPresenter] RefreshTile fallback: tile renderer missing. cell={cell}");
            return;
        }

        // Hole은 메타 우선(검정)
        var meta = grid.GetMeta(cell);
        // 정적 셀 타입 색
        var cellType = grid.GetCell(cell);

        // ---- resolve key ----
        E_TileVisualKey key;
        if (meta.IsHole) key = E_TileVisualKey.Hole;
        else
        {
            key = cellType switch
            {
                E_CellType.Wall => E_TileVisualKey.Wall,
                E_CellType.Obstacle => E_TileVisualKey.Obstacle,
                E_CellType.Goal => E_TileVisualKey.Goal,
                _ => E_TileVisualKey.Floor
            };
        }

        // 1) provider sprite가 있으면 적용 (스프라이트 교체)
        if (_tileSpriteProvider != null && _tileSpriteProvider.TryGetSprite(key, out var sprite) && sprite != null)
        {
            sr.sprite = sprite;
            sr.color = Color.white;
            return;
        }

        // 2) 없으면 "이전 스프라이트 유지"가 원칙
        // 단, 현재가 프로토 스프라이트 상태일 때만 색상 폴백으로 상태 표현
        if (sr.sprite == null || sr.sprite == Proto2DVisual.Sprite)
        {
            sr.sprite = Proto2DVisual.Sprite;

            Color baseColor = cellType switch
            {
                E_CellType.Wall => Proto2DVisual.TileWall,
                E_CellType.Obstacle => Proto2DVisual.TileObstacle,
                E_CellType.Goal => Proto2DVisual.TileGoal,
                _ => Proto2DVisual.TileFloor
            };

            sr.color = meta.IsHole ? Proto2DVisual.TileHole : baseColor;
        }
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

    public void SetTileSpriteProvider(ITileSpriteProvider provider)
    {
        _tileSpriteProvider = provider;

        if (_tileSpriteProvider == null && !_warnedNoProvider)
        {
            _warnedNoProvider = true;
            Debug.LogWarning("[GridPresenter] TileSpriteProvider is null. Tiles will keep proto visuals.");
        }

        // 이미 타일이 만들어져 있으면 즉시 재적용
        if (_boundGrid != null && _tileRenderers.Count == _w * _h)
        {
            for (int y = 0; y < _h; y++)
                for (int x = 0; x < _w; x++)
                    RefreshTile(_boundGrid, new Vector2Int(x, y));
        }
    }
}
