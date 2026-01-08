// TileSelector.cs
using System;

public enum E_TileLayer
{
    InnerBase = 0,
    Ring = 1,
    Overlay = 2,
    Block = 3,
    Ui = 4,
}

[Serializable]
public struct TileSelector : IEquatable<TileSelector>
{
    public E_TileLayer _layer;
    public E_TileVisualKey _key;

    // 확장 슬롯(지금 단계에서는 선택적으로만 사용)
    public E_Dir4 _dir;
    public int _variant;

    public static TileSelector Make(E_TileLayer layer, E_TileVisualKey key, E_Dir4 dir = E_Dir4.Right, int variant = 0)
    {
        return new TileSelector
        {
            _layer = layer,
            _key = key,
            _dir = dir,
            _variant = variant
        };
    }

    public bool Equals(TileSelector other)
    {
        return _layer == other._layer
               && _key == other._key
               && _dir == other._dir
               && _variant == other._variant;
    }

    public override bool Equals(object obj)
    {
        return obj is TileSelector other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int h = 17;
            h = h * 31 + (int)_layer;
            h = h * 31 + (int)_key;
            h = h * 31 + (int)_dir;
            h = h * 31 + _variant;
            return h;
        }
    }

    public override string ToString()
    {
        return $"layer={_layer} key={_key} dir={_dir} variant={_variant}";
    }
}
