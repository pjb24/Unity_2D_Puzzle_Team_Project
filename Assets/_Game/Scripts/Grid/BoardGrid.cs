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

// 기믹용 “동적/속성” 레이어
public enum E_CellSurface
{
    Normal = 0,
    Hole = 1,
    Swamp = 2,
}

public enum E_CellDir
{
    None = 0,
    Up = 1,
    Down = 2,
    Left = 3,
    Right = 4,
}

[Flags]
public enum E_CellTrapMask
{
    None = 0,
    Rhythm = 1 << 0,
    // 확장: Spike, Laser 등
}

[Serializable]
public struct CellMeta
{
    public E_CellSurface _surface;
    public E_CellDir _dir;
    public int _regionId;
    public E_CellTrapMask _trapMask;

    public static CellMeta Default => new CellMeta
    {
        _surface = E_CellSurface.Normal,
        _dir = E_CellDir.None,
        _regionId = 0,
        _trapMask = E_CellTrapMask.None,
    };

    public bool IsHole => _surface == E_CellSurface.Hole;
    public bool IsSwamp => _surface == E_CellSurface.Swamp;
}

public enum E_CellChangeKind
{
    SetMeta = 0,

    // “좌표가 바뀌는” 변화(회전/이동) 같은 경우.
    // 공통 인프라에서는 정책 플래그만 제공하고 실제 연산은 기믹에서 수행.
    TopologyChanged = 10,
}

public readonly struct CellChange
{
    public readonly Vector2Int Cell;
    public readonly CellMeta Meta;
    public readonly E_CellChangeKind Kind;

    public CellChange(Vector2Int cell, CellMeta meta, E_CellChangeKind kind = E_CellChangeKind.SetMeta)
    {
        Cell = cell;
        Meta = meta;
        Kind = kind;
    }
}

public class BoardGrid
{
    public readonly int _w;
    public readonly int _h;

    private readonly E_CellType[] _cells; // 정적 지형
    private readonly CellMeta[] _meta;    // 동적/기믹 속성
    private readonly E_Occupant[] _occ;

    private event Action<Vector2Int, CellMeta> _onMetaChanged;

    public void AddListenerOnMetaChanged(Action<Vector2Int, CellMeta> cb) => _onMetaChanged += cb;
    public void RemoveListenerOnMetaChanged(Action<Vector2Int, CellMeta> cb) => _onMetaChanged -= cb;

    public BoardGrid(int w, int h, E_CellType[] cells)
    {
        _w = Mathf.Max(1, w);
        _h = Mathf.Max(1, h);

        int total = _w * _h;

        _cells = new E_CellType[total];
        _meta = new CellMeta[total];
        _occ = new E_Occupant[total];

        for (int i = 0; i < total; i++)
            _meta[i] = CellMeta.Default;

        if (cells != null)
        {
            int n = Mathf.Min(cells.Length, total);
            for (int i = 0; i < n; i++)
                _cells[i] = cells[i];
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

        return _cells[ToIndex(c)];
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

    public bool IsBlockedCell(E_CellType t) => (t == E_CellType.Wall || t == E_CellType.Obstacle);

    // ---- Meta ----

    public CellMeta GetMeta(Vector2Int c)
    {
        if (!IsInBounds(c))
        {
            Debug.LogWarning($"[BoardGrid] GetMeta fallback: out of bounds. c={c}");
            return CellMeta.Default;
        }
        return _meta[ToIndex(c)];
    }

    public void SetMeta(Vector2Int c, CellMeta meta, bool notify = true)
    {
        if (!IsInBounds(c))
        {
            Debug.LogWarning($"[BoardGrid] SetMeta fallback: out of bounds. c={c}");
            return;
        }

        _meta[ToIndex(c)] = meta;

        if (notify)
            _onMetaChanged?.Invoke(c, meta);
    }

    // 보드 전체 메타 복원/백업용(리와인드)
    public CellMeta[] CopyMetaArray()
    {
        var copy = new CellMeta[_meta.Length];
        Array.Copy(_meta, copy, _meta.Length);
        return copy;
    }

    public void RestoreMetaArray(CellMeta[] metaArray, bool notifyAll = true)
    {
        if (metaArray == null || metaArray.Length != _meta.Length)
        {
            Debug.LogWarning("[BoardGrid] RestoreMetaArray fallback: metaArray is null or size mismatch.");
            return;
        }

        Array.Copy(metaArray, _meta, _meta.Length);

        if (notifyAll)
        {
            for (int y = 0; y < _h; y++)
                for (int x = 0; x < _w; x++)
                {
                    var c = new Vector2Int(x, y);
                    _onMetaChanged?.Invoke(c, GetMeta(c));
                }
        }
    }

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

    /// 동적 변화 적용 API
    /// 반환: (anyChanged, requiresRegistryRebuild)
    public (bool changed, bool requiresRegistryRebuild) ApplyCellChange(CellChange change, bool notify = true)
    {
        if (!IsInBounds(change.Cell))
        {
            Debug.LogWarning($"[BoardGrid] ApplyCellChange fallback: out of bounds. c={change.Cell}");
            return (false, false);
        }

        int idx = ToIndex(change.Cell);
        bool changed = !Equals(_meta[idx], change.Meta);

        _meta[idx] = change.Meta;

        if (notify)
            _onMetaChanged?.Invoke(change.Cell, change.Meta);

        bool requiresRebuild = (change.Kind == E_CellChangeKind.TopologyChanged);
        return (changed, requiresRebuild);
    }

    public (bool changed, bool requiresRegistryRebuild) ApplyCellChanges(CellChange[] changes, bool notify = true)
    {
        if (changes == null || changes.Length == 0)
            return (false, false);

        bool anyChanged = false;
        bool requiresRebuild = false;

        for (int i = 0; i < changes.Length; i++)
        {
            var (c, r) = ApplyCellChange(changes[i], notify);
            anyChanged |= c;
            requiresRebuild |= r;
        }

        return (anyChanged, requiresRebuild);
    }

    public bool CanEnter(Vector2Int c)
    {
        if (!IsInBounds(c)) return false;
        if (IsBlockedCell(GetCell(c))) return false;

        // 기믹 메타: Hole은 “진입 불가”
        if (GetMeta(c).IsHole) return false;

        if (GetOcc(c) != E_Occupant.None) return false;
        return true;
    }
}
