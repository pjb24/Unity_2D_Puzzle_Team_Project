// CompositeTileSpriteProvider.cs
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 합성 규칙(고정):
/// 1) StageVisualOverride에서 성공하면 그걸 사용
/// 2) Chapter TileVisualProfile에서 성공하면 그걸 사용
/// 3) 둘 다 실패면 false + Warning(키 단위 1회)
/// </summary>
public class CompositeTileSpriteProvider : ITileSpriteProvider
{
    private readonly string _stageId;
    private readonly TileVisualProfile _baseProfile;
    private readonly StageVisualOverride _overrideProfile;

    private readonly HashSet<TileSelector> _warnedMissing = new();

    public CompositeTileSpriteProvider(string stageId, TileVisualProfile baseProfile, StageVisualOverride overrideProfile)
    {
        _stageId = string.IsNullOrWhiteSpace(stageId) ? "UNKNOWN_STAGE" : stageId;
        _baseProfile = baseProfile;
        _overrideProfile = overrideProfile;
    }

    public bool TryGetSprite(in TileSelector selector, out Sprite sprite)
    {
        // 1) Stage override 우선
        if (_overrideProfile != null && _overrideProfile.TryGetTileSpriteOverride(in selector, out sprite) && sprite != null)
            return true;

        // 2) Chapter(base) profile
        if (_baseProfile != null && _baseProfile.TryGetSprite(in selector, out sprite) && sprite != null)
            return true;

        // 3) 없음 (폴백 발생: “생성 스킵”을 위한 false)
        sprite = null;
        WarnMissingOnce(selector);
        return false;
    }

    private void WarnMissingOnce(in TileSelector selector)
    {
        if (_warnedMissing.Contains(selector))
            return;

        _warnedMissing.Add(selector);
        Debug.LogWarning($"[Visual] Missing sprite -> no creation. stageId={_stageId} selector=({selector})");
    }
}
