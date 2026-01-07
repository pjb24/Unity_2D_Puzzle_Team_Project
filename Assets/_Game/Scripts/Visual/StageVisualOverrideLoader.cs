// StageVisualOverrideLoader.cs
using System.Collections.Generic;
using UnityEngine;

public static class StageVisualOverrideLoader
{
    private const string BasePath = "Visual/StageOverrides/";
    private static readonly Dictionary<string, StageVisualOverride> _cache = new();

    public static StageVisualOverride LoadOrNull(string stageId)
    {
        if (string.IsNullOrWhiteSpace(stageId))
        {
            Debug.LogWarning("[StageVisualOverrideLoader] stageId is empty. (no stage override)");
            return null;
        }

        if (_cache.TryGetValue(stageId, out var cached))
        {
            if (cached != null)
            {
                return cached;
            }
        }

        var loaded = Resources.Load<StageVisualOverride>(BasePath + stageId);
        _cache[stageId] = loaded; // null도 캐시

        return loaded;
    }
}
