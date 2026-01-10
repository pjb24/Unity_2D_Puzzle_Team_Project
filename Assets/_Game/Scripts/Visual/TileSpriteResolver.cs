// TileSpriteResolver.cs
using UnityEngine;

public interface ITileSpriteLookup
{
    bool TryGetSprite(in TileVisualKey key, out Sprite sprite);
}

/// <summary>
/// 런타임 해석기(단일 진입점)
/// - override -> base(atlas) 순서로 조회
/// - 실패 시 Warning
/// - 임시 스프라이트 생성 금지(호출부에서 "표시 스킵"만)
/// </summary>
public class TileSpriteResolver
{
    private readonly ITileSpriteLookup _overrideLookup; // optional
    private readonly ITileSpriteLookup _baseLookup;     // required
    private readonly Object _logContext;                // optional(UnityEngine.Object)

    public TileSpriteResolver(ITileSpriteLookup baseLookup, ITileSpriteLookup overrideLookup = null, Object logContext = null)
    {
        _baseLookup = baseLookup;
        _overrideLookup = overrideLookup;
        _logContext = logContext;
    }

    public bool TryGetSprite(in TileVisualKey key, out Sprite sprite)
    {
        // Atlas/Override는 Goal/Border를 dir=None으로만 저장하므로, 조회용 키를 정규화한다.
        TileVisualKey lookupKey = NormalizeForLookup(key);

        if (_overrideLookup != null && _overrideLookup.TryGetSprite(lookupKey, out sprite))
            return sprite != null;

        if (_baseLookup != null && _baseLookup.TryGetSprite(lookupKey, out sprite))
            return sprite != null;

        sprite = null;

        if (_logContext != null)
            Debug.LogWarning("[TileSpriteResolver] Missing sprite: " + key, _logContext);
        else
            Debug.LogWarning("[TileSpriteResolver] Missing sprite: " + key);

        return false;
    }

    private static TileVisualKey NormalizeForLookup(in TileVisualKey key)
    {
        if (TileVisualKey.IsDirectionalType(key.Type))
            return new TileVisualKey(key.Layer, key.Type, E_Dir4.None);

        return key;
    }
}
