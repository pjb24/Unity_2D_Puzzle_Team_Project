// StageAudioSelector.cs
using UnityEngine;

public static class StageAudioSelector
{
    public static E_BgmId SelectBgmId(StageDefinition stage, ChapterVisualProfile chapter, out float volumeScale)
    {
        volumeScale = 1f;

        StageAudioProfile p = stage != null ? stage.AudioProfile : null;

        if (p != null && p.UseBgmOverride)
        {
            if (p.BgmId == E_BgmId.None)
            {
                Debug.LogWarning($"[StageAudioSelector] StageAudioProfile override enabled but BgmId is None. fallback to chapter. stageId={stage?.StageId}");
            }
            else
            {
                volumeScale = p.BgmVolumeScale;
                return p.BgmId;
            }
        }

        if (chapter == null)
        {
            Debug.LogWarning("[StageAudioSelector] ChapterVisualProfile is null. BGM skipped.");
            return E_BgmId.None;
        }

        return chapter.BgmId;
    }

    public static E_SfxId SelectStageEnterSfx(StageDefinition stage, ChapterVisualProfile chapter)
    {
        StageAudioProfile p = stage != null ? stage.AudioProfile : null;

        if (p != null && p.UseStageEnterSfxOverride)
        {
            if (p.StageEnterSfxId == E_SfxId.None)
            {
                Debug.LogWarning($"[StageAudioSelector] StageEnterSfx override enabled but id is None. fallback to chapter. stageId={stage?.StageId}");
            }
            else
            {
                return p.StageEnterSfxId;
            }
        }

        if (chapter == null)
        {
            Debug.LogWarning("[StageAudioSelector] ChapterVisualProfile is null. StageEnter SFX skipped.");
            return E_SfxId.None;
        }

        return chapter.StageEnterSfxId;
    }
}
