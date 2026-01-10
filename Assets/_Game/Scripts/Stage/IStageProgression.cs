// IStageProgression.cs
public enum E_StageAdvanceResult
{
    NextStage,
    NextChapter,
    Ending,
}

public interface IStageProgression
{
    E_StageAdvanceResult EvaluateNext(GameFlowContext ctx);
}
