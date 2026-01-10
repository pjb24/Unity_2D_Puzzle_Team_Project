// GridPresenter.cs
using UnityEngine;

public class GridPresenter
{
    [Header("Tuning")]
    public float _tileSize = 1f;

    private BoardGrid _grid;

    public Vector3 _originLocal; // root 기준 원점(셀 0,0의 중심)
    public Transform _root;      // StageRuntime root

    public void Initialize(Transform root, BoardGrid grid)
    {
        _root = root;
        _grid = grid;

        if (_grid == null)
        {
            Debug.LogWarning("[GridPresenter] Initialize failed: grid is null");
            return;
        }

        _originLocal = new Vector3(
            -(_grid._w - 1) * 0.5f * _tileSize,
            -(_grid._h - 1) * 0.5f * _tileSize,
            0f);
    }

    private bool IsReady()
    {
        return _grid != null;
    }

    public Vector3 CellToWorld(Vector2Int c)
    {
        // 2D 탑뷰: (x,y) 그대로 매핑
        Vector3 local = _originLocal + new Vector3(c.x * _tileSize, c.y * _tileSize, 0f);
        return _root.TransformPoint(local);
    }
}
