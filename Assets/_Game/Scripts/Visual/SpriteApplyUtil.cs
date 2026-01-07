// SpriteApplyUtil.cs
using UnityEngine;

public static class SpriteApplyUtil
{
    public static void TryApplySpriteKeepPrevious(SpriteRenderer sr, Sprite sprite, string warnContextIfNoRenderer)
    {
        if (sr == null)
        {
            Debug.LogWarning($"[Visual] ApplySprite fallback: SpriteRenderer missing. ctx={warnContextIfNoRenderer}");
            return;
        }

        if (sprite == null)
            return; // 규칙: 변경 대상 없으면 이전 유지

        sr.sprite = sprite;
        sr.color = Color.white;
    }

    public static SpriteRenderer FindSpriteRendererOrNull(GameObject go)
    {
        if (go == null) return null;
        return go.GetComponentInChildren<SpriteRenderer>(includeInactive: true);
    }
}
