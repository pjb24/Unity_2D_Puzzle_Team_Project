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
    public readonly float _tileSize;
    public readonly Vector3 _originLocal; // root 기준 원점(셀 0,0의 중심)
    public readonly Transform _root;      // StageRuntime root
    private readonly int _w;
    private readonly int _h;

    private readonly SpriteRenderer[] _tileRenderers;

    private Transform _tilesRoot;
    private BoardGrid _boundGrid;
    private System.Action<Vector2Int, CellMeta> _metaListener;

    public GridPresenter(Transform root, int w, int h, float tileSize)
    {
        _root = root;
        _w = w;
        _h = h;
        _tileSize = tileSize;

        // DummyStageLoader와 동일한 중앙정렬 규칙
        _originLocal = new Vector3(-(w - 1) * 0.5f * tileSize, -(h - 1) * 0.5f * tileSize, 0f);

        _tileRenderers = new SpriteRenderer[_w * _h];
    }

    public Vector3 CellToWorld(Vector2Int c)
    {
        // 2D 탑뷰: (x,y) 그대로 매핑
        Vector3 local = _originLocal + new Vector3(c.x * _tileSize, c.y * _tileSize, 0f);
        return _root.TransformPoint(local);
    }

    public Transform BuildTiles(BoardGrid grid, List<Transform> outTiles = null)
    {
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

        var goRoot = new GameObject("[Tiles]");
        goRoot.transform.SetParent(_root, false);
        _tilesRoot = goRoot.transform;

        if (outTiles != null) outTiles.Clear();

        for (int y = 0; y < _h; y++)
        {
            for (int x = 0; x < _w; x++)
            {
                var cell = new Vector2Int(x, y);
                int idx = y * _w + x;

                var go = Proto2DVisual.CreateSpriteObject(
                    name: $"Tile({x},{y})",
                    parent: _tilesRoot,
                    sortingOrder: (int)E_ProtoSort.Tile,
                    color: Proto2DVisual.TileFloor,
                    localScale: new Vector3(_tileSize, _tileSize, 1f)
                );

                go.transform.position = CellToWorld(cell);

                _tileRenderers[idx] = go.GetComponent<SpriteRenderer>();

                outTiles?.Add(go.transform);

                RefreshTile(grid, cell);
            }
        }

        BindMetaListener(grid);
        return _tilesRoot;
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

        // 정적 셀 타입 색
        var cellType = grid.GetCell(cell);
        Color baseColor = cellType switch
        {
            E_CellType.Wall => Proto2DVisual.TileWall,
            E_CellType.Obstacle => Proto2DVisual.TileObstacle,
            E_CellType.Goal => Proto2DVisual.TileGoal,
            _ => Proto2DVisual.TileFloor
        };

        // Hole은 메타 우선(검정)
        var meta = grid.GetMeta(cell);
        if (meta.IsHole)
            sr.color = Proto2DVisual.TileHole;
        else
            sr.color = baseColor;
    }

    private void BindMetaListener(BoardGrid grid)
    {
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
