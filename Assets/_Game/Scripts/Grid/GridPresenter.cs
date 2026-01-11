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

    public Vector3 CellToLocal(Vector2Int c)
    {
        if (_grid == null)
        {
            Debug.LogWarning("[GridPresenter] CellToLocal fallback: grid is null");
            return Vector3.zero;
        }

        return _originLocal + new Vector3(c.x * _tileSize, c.y * _tileSize, 0f);
    }

    public Vector3 CellToWorld(Vector2Int c)
    {
        if (_grid == null || _root == null)
        {
            Debug.LogWarning("[GridPresenter] CellToWorld fallback: grid/root is null");
            return CellToLocal(c);
        }

        return _root.TransformPoint(CellToLocal(c));
    }
}
