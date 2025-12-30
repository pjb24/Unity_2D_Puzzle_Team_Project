///
/// 런타임 Grid 모델(순수 C#)
/// 좌표: Vector2Int (x,y)
/// 인덱스: idx = y * width + x
/// 저장:
/// _cells[idx] : E_CellType
/// _occ[idx] : E_Occupant(None / Father / Child / Blocker 등)
///

using UnityEngine;

public enum E_Occupant
{
    None,
    Father,
    Child,
    Blocker, // 필요하면
}

public class BoardGrid
{
    public readonly int _w;
    public readonly int _h;

    private readonly E_CellType[] _cells; // from StageDefinition.Cells
    private readonly E_Occupant[] _occ;

    public BoardGrid(int w, int h, E_CellType[] cells)
    {
        _w = Mathf.Max(1, w);
        _h = Mathf.Max(1, h);

        int total = _w * _h;
        _cells = new E_CellType[total];
        _occ = new E_Occupant[total];

        if (cells != null)
        {
            int n = Mathf.Min(cells.Length, total);
            for (int i = 0; i < n; i++) _cells[i] = cells[i];
        }
    }

    public bool IsInBounds(Vector2Int c) => (uint)c.x < (uint)_w && (uint)c.y < (uint)_h;

    public int ToIndex(Vector2Int c) => c.y * _w + c.x;

    public E_CellType GetCell(Vector2Int c) => _cells[ToIndex(c)];
    public E_Occupant GetOcc(Vector2Int c) => _occ[ToIndex(c)];

    public void SetOcc(Vector2Int c, E_Occupant occ) => _occ[ToIndex(c)] = occ;

    public bool IsBlockedCell(E_CellType t) => (t == E_CellType.Wall || t == E_CellType.Obstacle);

    public bool CanEnter(Vector2Int c)
    {
        if (!IsInBounds(c)) return false;
        if (IsBlockedCell(GetCell(c))) return false;
        if (GetOcc(c) != E_Occupant.None) return false;
        return true;
    }
}
