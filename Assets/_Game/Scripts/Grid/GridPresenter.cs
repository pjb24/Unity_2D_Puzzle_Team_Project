// GridPresenter.cs
///
/// 표현 레이어: “월드 좌표 ↔ 셀” 변환 규칙 고정
/// DummyStageLoader가 타일을 “중앙 정렬 + tileSize”로 깔고 있다.
///

using UnityEngine;

public class GridPresenter
{
    public readonly float _tileSize;
    public readonly Vector3 _originLocal; // root 기준 원점(셀 0,0의 중심)
    public readonly Transform _root;      // StageRuntime root
    private readonly int _w;
    private readonly int _h;

    private readonly SpriteRenderer[] _tileRenderers;

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

    public void BuildTiles(BoardGrid grid)
    {
        if (grid == null)
        {
            Debug.LogWarning("[GridPresenter] BuildTiles fallback: grid is null.");
            return;
        }

        var tilesRoot = new GameObject("[Tiles2D]");
        tilesRoot.transform.SetParent(_root, false);

        for (int y = 0; y < _h; y++)
        {
            for (int x = 0; x < _w; x++)
            {
                var cell = new Vector2Int(x, y);
                int idx = y * _w + x;

                var go = Proto2DVisual.CreateSpriteObject(
                    name: $"Tile({x},{y})",
                    parent: tilesRoot.transform,
                    sortingOrder: (int)E_ProtoSort.Tile,
                    color: Proto2DVisual.TileFloor,
                    localScale: new Vector3(_tileSize, _tileSize, 1f)
                );

                go.transform.position = CellToWorld(cell);

                _tileRenderers[idx] = go.GetComponent<SpriteRenderer>();

                RefreshTile(grid, cell);
            }
        }
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
}
