// IStageProgression.cs
public interface IStageProgression
{
    E_StageAdvanceResult EvaluateNext(GameFlowContext ctx);
}
