using UnityEngine;

public static class StageScalePolicy
{
    public const int ReferenceBoardSize = 9;   // 9x9 기준
    public const float ReferenceTileSize = 1f; // 1x1 타일 기준

    public static float CalcUniformScale(int width, int height)
    {
        int max = Mathf.Max(width, height);
        if (max <= 0)
        {
            Debug.LogWarning("[StageScale] invalid board size. fallback scale=1");
            return 1f;
        }

        return (float)ReferenceBoardSize / max;
    }
}
