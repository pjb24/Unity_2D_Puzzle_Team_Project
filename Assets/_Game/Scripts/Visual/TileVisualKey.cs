// TileVisualKey.cs
using System;

public enum E_TileVisualLayer
{
    Base = 0,       // 가장 아래
    Overlay_1 = 1,  // 중간
    Overlay_2 = 2,  // 가장 위
}

public enum E_TileVisualType
{
    Floor = 0,
    Wall = 1,

    SwitchOn = 2,
    SwitchOff = 3,

    Hole = 4,
    FilledHole = 5,
    GapFillerBlock = 6,

    DoorOn = 7,
    DoorOff = 8,

    Goal = 9,
    ChildPathOuterBorder = 10,
}

public enum E_Dir4
{
    None = 0,
    Up = 1,
    Right = 2,
    Down = 3,
    Left = 4,
}

public readonly struct TileVisualKey : IEquatable<TileVisualKey>
{
    private readonly E_TileVisualLayer _layer;
    private readonly E_TileVisualType _type;
    private readonly E_Dir4 _dir; // Goal / ChildPathOuterBorder 에서만 사용

    public E_TileVisualLayer Layer => _layer;
    public E_TileVisualType Type => _type;
    public E_Dir4 Dir => _dir;

    public TileVisualKey(E_TileVisualLayer layer, E_TileVisualType type, E_Dir4 dir)
    {
        _layer = layer;
        _type = type;
        _dir = dir;
    }

    public static bool IsDirectionalType(E_TileVisualType type)
    {
        return type == E_TileVisualType.Goal ||
               type == E_TileVisualType.ChildPathOuterBorder;
    }

    /// <summary>
    /// 규칙:
    /// - Goal / ChildPathOuterBorder => dir는 None이 아니어야 함 (Runtime에서 결정)
    /// - 그 외 타입 => dir는 반드시 None
    /// </summary>
    public static bool TryCreate(
        E_TileVisualLayer layer,
        E_TileVisualType type,
        E_Dir4 dir,
        out TileVisualKey key,
        out string error)
    {
        bool directional = IsDirectionalType(type);

        if (directional && dir == E_Dir4.None)
        {
            key = default;
            error = $"Directional type requires dir. type={type}, layer={layer}";
            return false;
        }

        if (!directional && dir != E_Dir4.None)
        {
            key = default;
            error = $"Non-directional type must use dir=None. type={type}, dir={dir}, layer={layer}";
            return false;
        }

        key = new TileVisualKey(layer, type, dir);
        error = null;
        return true;
    }

    // 편의 생성기: 방향 없는 키
    public static TileVisualKey Create(E_TileVisualLayer layer, E_TileVisualType type)
    {
        return new TileVisualKey(layer, type, E_Dir4.None);
    }

    // 편의 생성기: 방향 있는 키(Goal/Border 전용)
    public static TileVisualKey CreateDirectional(E_TileVisualLayer layer, E_TileVisualType type, E_Dir4 dir)
    {
        return new TileVisualKey(layer, type, dir);
    }

    public bool Equals(TileVisualKey other)
    {
        return _layer == other._layer && _type == other._type && _dir == other._dir;
    }

    public override bool Equals(object obj)
    {
        return obj is TileVisualKey other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = (hash * 31) + (int)_layer;
            hash = (hash * 31) + (int)_type;
            hash = (hash * 31) + (int)_dir;
            return hash;
        }
    }

    public static bool operator ==(TileVisualKey a, TileVisualKey b) => a.Equals(b);
    public static bool operator !=(TileVisualKey a, TileVisualKey b) => !a.Equals(b);

    public override string ToString()
    {
        if (IsDirectionalType(_type))
            return $"{_layer}:{_type}:{_dir}";
        return $"{_layer}:{_type}";
    }
}

// TODO: 참조 함수들 정리 후 제거 필요
public enum E_TileVisualKey
{
    Floor = 0,
    Wall = 1,

    SwitchOn = 2,
    SwitchOff = 3,

    Hole = 4,
    FilledHole = 5,
    GapFillerBlock = 6,

    DoorOpen = 7,
    DoorClosed = 8,

    Goal = 9,
    ChildPathOuterBorder = 10,

    Obstacle,
    Path,
    InnerOuterGap,
    HoleFilled,
}

public enum E_SortingOrder
{
    Hole,

}
