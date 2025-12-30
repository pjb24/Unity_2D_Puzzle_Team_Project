using System;
using System.Collections.Generic;
using UnityEngine;

public class ChildPathRuntime
{
    public IReadOnlyList<Vector3> Points => _points;
    public int Count => _points.Count;

    private readonly List<Vector3> _points = new();

    public ChildPathRuntime(BoardGrid grid, GridPresenter presenter)
    {
        if (grid == null) throw new ArgumentNullException(nameof(grid));
        if (presenter == null) throw new ArgumentNullException(nameof(presenter));

        int w = grid._w;
        int h = grid._h;
        var indices = PerimeterPathBuilder.Build(w, h);

        _points.Clear();
        for (int i = 0; i < indices.Count; i++)
        {
            int idx = indices[i];
            int x = idx % w;
            int y = idx / w;

            var c = new Vector2Int(x, y);
            // 범위 밖이면 일단 클램프/스킵 중 택1. 프로토타입은 스킵 권장.
            if (!grid.IsInBounds(c)) continue;

            Vector3 p = presenter.CellToWorld(c) + Vector3.up * 0.9f; // 캡슐 높이 보정(기존 Father와 동일 컨셉)
            _points.Add(p);
        }
    }
}
