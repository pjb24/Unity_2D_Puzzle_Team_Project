// IStageLoader.cs
using System;

public interface IStageLoader
{
    void LoadStage(GameFlowContext ctx, Action onComplete);
    void UnloadStage(GameFlowContext ctx);
}
