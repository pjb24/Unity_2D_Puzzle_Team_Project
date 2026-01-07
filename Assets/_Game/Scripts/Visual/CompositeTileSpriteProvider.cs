// CompositeTileSpriteProvider.cs
using System.Collections.Generic;
using UnityEngine;

public class CompositeTileSpriteProvider : ITileSpriteProvider
{
    private readonly string _stageId;
    private readonly TileVisualProfile _baseProfile;
    private readonly StageVisualOverride _overrideProfile;

    private readonly HashSet<E_TileVisualKey> _warnedMissing = new();

    public CompositeTileSpriteProvider(string stageId, TileVisualProfile baseProfile, StageVisualOverride overrideProfile)
    {
        _stageId = string.IsNullOrWhiteSpace(stageId) ? "UNKNOWN_STAGE" : stageId;
        _baseProfile = baseProfile;
        _overrideProfile = overrideProfile;
    }

    public bool TryGetSprite(E_TileVisualKey key, out Sprite sprite)
    {
        sprite = null;

        if (_overrideProfile != null && _overrideProfile.TryGetTileSpriteOverride(key, out sprite))
            return true;

        if (_baseProfile != null && _baseProfile.TryGetSprite(key, out sprite))
            return true;

        WarnMissingOnce(key);
        return false;
    }

    private void WarnMissingOnce(E_TileVisualKey key)
    {
        if (_warnedMissing.Contains(key))
            return;

        _warnedMissing.Add(key);
        Debug.LogWarning($"[Visual] Sprite missing (keep previous). stageId={_stageId} key={key}");
    }
}
