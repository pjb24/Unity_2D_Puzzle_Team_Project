// BoardGrid.cs
///
/// 런타임 Grid 모델(순수 C#)
/// 좌표: Vector2Int (x,y)
/// 인덱스: idx = y * width + x
/// 저장:
/// _cells[idx] : E_CellType (정적 지형)
/// _meta[idx]  : CellMeta  (기믹/동적 속성)
/// _occ[idx]   : E_Occupant(None / Father / Child / Blocker 등)
///
using System;
using UnityEngine;

public enum E_Occupant
{
    None,
    Father,
    Child,
    Blocker,
    GapFillerBlock,
}

public class BoardGrid
{
    public readonly int _w;
    public readonly int _h;

    private readonly E_CellType[] _cellsBase;
    private readonly E_CellType[] _cellsOverlay01;
    private readonly E_CellType[] _cellsOverlay02;

    private readonly E_Occupant[] _occ;

    public BoardGrid(int w, int h, StageDefinition stageDef)
    {
        _w = Mathf.Max(1, w);
        _h = Mathf.Max(1, h);

        int total = _w * _h;

        _cellsBase = new E_CellType[total];
        _cellsOverlay01 = new E_CellType[total];
        _cellsOverlay02 = new E_CellType[total];
        _occ = new E_Occupant[total];

        if (stageDef != null)
        {

            for (int i = 0; i < total; i++)
            {
                // cellsBase
                // Empty, Floor, Wall
                if (stageDef.Cells[i] == E_CellType.Empty)
                {
                    _cellsBase[i] = E_CellType.Empty;
                }
                else if (stageDef.Cells[i] == E_CellType.Floor
                    || stageDef.Cells[i] == E_CellType.Wall)
                {
                    _cellsBase[i] = E_CellType.Floor;
                }

                // cellsOverlay01
                // Wall
                if (stageDef.Cells[i] == E_CellType.Wall)
                {
                    _cellsOverlay01[i] = E_CellType.Wall;
                }
            }

            // cellsOverlay01
            // Wall, Hole, Switch
            // Door, Goal
            var hole = stageDef.GetHoleCells_Runtime();
            foreach (var cell in hole)
            {
                int idx = ToIndex(cell);
                _cellsOverlay01[idx] = E_CellType.Hole;
            }

            var switchSpawn = stageDef.ToggleSwitchSpawns;
            foreach (var item in switchSpawn)
            {
                int idx = ToIndex(item._cell);
                if (item._startOn)
                {
                    _cellsOverlay01[idx] = E_CellType.SwitchOn;
                }
                else
                {
                    _cellsOverlay01[idx] = E_CellType.SwitchOff;
                }
            }

            var door = stageDef.DoorSpawns;
            foreach (var item in door)
            {
                int idx = ToIndex(item._cell);
                _cellsOverlay01[idx] = E_CellType.Door;
            }

            var goal = stageDef.ChildGoalPathStep;
            _cellsOverlay01[goal] = E_CellType.Goal;

            // cellsOverlay02
            // FillerBlock
            var fillerBlock = stageDef.GetGapFillerBlockCells_Runtime();
            foreach (var cell in fillerBlock)
            {
                int idx = ToIndex(cell);
                _cellsOverlay02[idx] = E_CellType.FillerBlock;
            }
        }
    }

    public bool IsInBounds(Vector2Int c) => (uint)c.x < (uint)_w && (uint)c.y < (uint)_h;
    public int ToIndex(Vector2Int c) => c.y * _w + c.x;

    public E_CellType GetCell(Vector2Int c)
    {
        if (!IsInBounds(c))
        {
            Debug.LogWarning($"[BoardGrid] GetCell fallback: out of bounds. c={c}");
            return E_CellType.Wall; // 안전: “막힘”으로 취급
        }

        int idx = ToIndex(c);

        if (_cellsOverlay02[idx] != E_CellType.Empty)
        {
            return _cellsOverlay02[idx];
        }

        if (_cellsOverlay01[idx] != E_CellType.Empty)
        {
            return _cellsOverlay01[idx];
        }

        return _cellsBase[ToIndex(c)];
    }

    public E_Occupant GetOcc(Vector2Int c)
    {
        if (!IsInBounds(c))
        {
            Debug.LogWarning($"[BoardGrid] GetOcc fallback: out of bounds. c={c}");
            return E_Occupant.Blocker; // 안전: 점유/막힘으로 취급
        }

        return _occ[ToIndex(c)];
    }

    public void SetOcc(Vector2Int c, E_Occupant occ)
    {
        if (!IsInBounds(c))
        {
            Debug.LogWarning($"[BoardGrid] SetOcc fallback: out of bounds. c={c} occ={occ}");
            return;
        }

        _occ[ToIndex(c)] = occ;
    }

    public bool IsBlockedCell(E_CellType t) => (t == E_CellType.Wall || t == E_CellType.Hole);

    public E_Occupant[] CopyOccArray()
    {
        var copy = new E_Occupant[_occ.Length];
        Array.Copy(_occ, copy, _occ.Length);
        return copy;
    }

    public void RestoreOccArray(E_Occupant[] occArray)
    {
        if (occArray == null || occArray.Length != _occ.Length)
        {
            Debug.LogWarning("[BoardGrid] RestoreOccArray fallback: occArray is null or size mismatch.");
            return;
        }

        Array.Copy(occArray, _occ, _occ.Length);
    }

    public bool CanEnter(Vector2Int c)
    {
        if (!IsInBounds(c)) return false;
        if (IsBlockedCell(GetCell(c))) return false;

        if (GetOcc(c) != E_Occupant.None) return false;
        return true;
    }

    public E_CellType GetCellOverlay01(Vector2Int cell)
    {
        int idx = ToIndex(cell);
        return _cellsOverlay01[idx];
    }

    public void SetCellOverlay01(Vector2Int cell, E_CellType type)
    {
        int idx = ToIndex(cell);
        _cellsOverlay01[idx] = type;
    }
}
