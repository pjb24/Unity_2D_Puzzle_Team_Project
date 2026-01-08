// InnerBaseBackgroundBuilder.cs
using UnityEngine;

public static class InnerBaseBackgroundBuilder
{
    private const string GoName = "[InnerBaseBackground]";

    public static GameObject BuildOrNull(StageRuntimeRefs refs, StageDefinition stageDef)
    {
        if (refs == null)
        {
            Debug.LogWarning("[InnerBaseBackground] BuildOrNull fallback: refs is null.");
            return null;
        }

        StageVisualOverride ov = refs._stageVisualOverride;

        if (ov == null || !ov.UseInnerBaseBackground)
        {
            // 설정이 꺼져있으면 기존 오브젝트가 있더라도 유지하지 않음(잔존 방지)
            if (refs._innerBaseBackground != null)
            {
                Object.Destroy(refs._innerBaseBackground);
                refs._innerBaseBackground = null;
            }
            return null;
        }

        if (ov.InnerBaseBackgroundSprite == null)
        {
            Debug.LogWarning($"[InnerBaseBackground] Enabled but sprite is null. skip. stageId={refs._stageId}");
            return null;
        }

        if (refs._gridPresenter == null)
        {
            Debug.LogWarning($"[InnerBaseBackground] BuildOrNull fallback: GridPresenter is null. skip. stageId={refs._stageId}");
            return null;
        }

        if (stageDef == null)
        {
            Debug.LogWarning($"[InnerBaseBackground] FatherMoveRect fallback: stageDef is null. skip. stageId={refs._stageId}");
            return null;
        }

        RectInt rect = stageDef.FatherMoveRect;
        if (rect.width <= 0 || rect.height <= 0)
        {
            Debug.LogWarning($"[InnerBaseBackground] Invalid FatherMoveRect. skip. rect={rect} stageId={refs._stageId}");
            return null;
        }

        float ts = refs._gridPresenter._tileScale;

        Vector2 pad = ov.InnerBaseBackgroundPaddingCells;
        float wCells = rect.width + pad.x * 2f;
        float hCells = rect.height + pad.y * 2f;

        if (wCells <= 0f || hCells <= 0f)
        {
            Debug.LogWarning($"[InnerBaseBackground] Padding made invalid size. skip. pad={pad} rect={rect} stageId={refs._stageId}");
            return null;
        }

        Vector2 size = new Vector2(wCells * ts, hCells * ts);

        // GridPresenter 로컬 규칙과 동일(루트 기준 로컬)
        Vector3 originLocal = refs._gridPresenter._originLocal;
        Vector3 minCellCenterLocal = originLocal + new Vector3(rect.x * ts, rect.y * ts, 0f);
        Vector3 rectCenterLocal = minCellCenterLocal + new Vector3((rect.width - 1) * 0.5f * ts, (rect.height - 1) * 0.5f * ts, 0f);

        Transform parent = refs._root != null ? refs._root.transform : refs._gridPresenter._root;
        if (parent == null)
        {
            Debug.LogWarning($"[InnerBaseBackground] Parent root is null. skip. stageId={refs._stageId}");
            return null;
        }

        GameObject go = refs._innerBaseBackground;
        if (go == null)
        {
            go = new GameObject(GoName);
            go.transform.SetParent(parent, false);
            refs._innerBaseBackground = go;
        }
        else if (go.transform.parent != parent)
        {
            go.transform.SetParent(parent, false);
        }

        go.transform.localPosition = rectCenterLocal;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;

        var sr = go.GetComponent<SpriteRenderer>();
        if (sr == null) sr = go.AddComponent<SpriteRenderer>();

        sr.sprite = ov.InnerBaseBackgroundSprite;
        sr.color = Color.white;
        sr.sortingOrder = ov.InnerBaseBackgroundSortingOrder;

        ApplyDrawMode(sr, ov, size, refs._stageId);

        return go;
    }

    private static void ApplyDrawMode(SpriteRenderer sr, StageVisualOverride ov, Vector2 size, string stageId)
    {
        if (sr == null || ov == null)
        {
            Debug.LogWarning("[InnerBaseBackground] ApplyDrawMode fallback: sr/ov is null.");
            return;
        }

        switch (ov.InnerBaseBackgroundDrawMode)
        {
            case E_InnerBaseBackgroundDrawMode.Tiled:
                sr.drawMode = SpriteDrawMode.Tiled;
                sr.size = size;

                // WrapMode가 Repeat가 아니면 타일링이 의도대로 안 나올 수 있음
                var tex = sr.sprite != null ? sr.sprite.texture : null;
                if (tex != null && tex.wrapMode != TextureWrapMode.Repeat)
                {
                    Debug.LogWarning($"[InnerBaseBackground] Tiled selected but texture.wrapMode is not Repeat. (import setting) stageId={stageId}");
                }
                break;

            case E_InnerBaseBackgroundDrawMode.Sliced:
                sr.drawMode = SpriteDrawMode.Sliced;
                sr.size = size;

                if (sr.sprite != null && sr.sprite.border == Vector4.zero)
                {
                    Debug.LogWarning($"[InnerBaseBackground] Sliced selected but sprite border is zero. (9-slice not set) stageId={stageId}");
                }
                break;

            default:
            case E_InnerBaseBackgroundDrawMode.Simple:
                // 폴백: Simple + scale
                sr.drawMode = SpriteDrawMode.Simple;

                if (sr.sprite == null)
                {
                    Debug.LogWarning($"[InnerBaseBackground] Simple fallback: sprite is null. stageId={stageId}");
                    return;
                }

                Vector2 spriteSize = sr.sprite.bounds.size;
                if (spriteSize.x <= 0f || spriteSize.y <= 0f)
                {
                    Debug.LogWarning($"[InnerBaseBackground] Simple fallback: invalid sprite bounds. stageId={stageId}");
                    return;
                }

                float sx = size.x / spriteSize.x;
                float sy = size.y / spriteSize.y;

                sr.transform.localScale = new Vector3(sx, sy, 1f);
                Debug.LogWarning($"[InnerBaseBackground] Using Simple(scale) fallback. stageId={stageId}");
                break;
        }
    }
}
