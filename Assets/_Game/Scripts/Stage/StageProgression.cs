// StageProgression.cs
using UnityEngine;

public class StageProgression : IStageProgression
{
    public E_StageAdvanceResult EvaluateNext(GameFlowContext ctx)
    {
        var cfg = ctx._config.LoadGameConfig();
        if (cfg == null)
        {
            Debug.LogError("[Progression] GameConfig is null. Treat as Ending.");
            return E_StageAdvanceResult.Ending;
        }

        if (ctx._chapterIndex < 0 || ctx._chapterIndex >= cfg.Chapters.Count)
        {
            Debug.LogError($"[Progression] Invalid chapterIndex={ctx._chapterIndex}. Treat as Ending.");
            return E_StageAdvanceResult.Ending;
        }

        var chapter = cfg.Chapters[ctx._chapterIndex];
        int stageCount = chapter.Stages.Count;

        if (stageCount <= 0)
        {
            Debug.LogError($"[Progression] Chapter has no stages. chapterIndex={ctx._chapterIndex}. Treat as Ending.");
            return E_StageAdvanceResult.Ending;
        }

        // NextStage
        if (ctx._stageIndex + 1 < stageCount)
            return E_StageAdvanceResult.NextStage;

        // NextChapter
        if (ctx._chapterIndex + 1 < cfg.Chapters.Count)
            return E_StageAdvanceResult.NextChapter;

        // Ending
        return E_StageAdvanceResult.Ending;
    }
}
