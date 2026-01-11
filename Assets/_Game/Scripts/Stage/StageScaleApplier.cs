// StageScaleApplier.cs
using UnityEngine;

public class StageScaleApplier
{
    private readonly Transform _stageRoot;

    public StageScaleApplier(Transform stageRoot)
    {
        _stageRoot = stageRoot;
    }

    public float Apply(int width, int height)
    {
        if (_stageRoot == null)
        {
            Debug.LogWarning("[StageScale] stageRoot is null. fallback scale=1");
            return 1f;
        }

        float s = StageScalePolicy.CalcUniformScale(width, height);
        _stageRoot.localScale = new Vector3(s, s, 1f); // 2D 기준
        return s;
    }
}
