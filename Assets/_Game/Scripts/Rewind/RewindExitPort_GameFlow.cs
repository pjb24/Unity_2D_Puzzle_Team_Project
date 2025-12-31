// RewindExitPort_GameFlow.cs
using UnityEngine;

public class RewindExitPort_GameFlow : IRewindExitPort
{
    private readonly GameFlowStateMachine _sm;
    private readonly GameFlowContext _ctx;
    private readonly IGameFlowState _stageLoad;

    public RewindExitPort_GameFlow(GameFlowStateMachine sm, GameFlowContext ctx, IGameFlowState stageLoad)
    {
        _sm = sm;
        _ctx = ctx;
        _stageLoad = stageLoad;
    }

    public void RequestRestartStage()
    {
        if (_ctx == null || _sm == null || _stageLoad == null)
        {
            Debug.LogWarning("[RewindExitPort_GameFlow] RequestRestartStage failed: missing refs (fallback).");
            return;
        }

        bool returnToChapterStart = _ctx.RecordFailAndShouldReturnChapterStart();
        if (returnToChapterStart)
        {
            Debug.LogWarning("[RewindExitPort_GameFlow] FailStreak reached on rewind exhaustion -> return to chapter start.");
            _ctx.ResetToChapterStart();
        }

        _sm.ChangeState(_ctx, _stageLoad);
    }

    public void RequestReturnToChapterStart()
    {
        if (_ctx == null || _sm == null || _stageLoad == null)
        {
            Debug.LogWarning("[RewindExitPort_GameFlow] RequestReturnToChapterStart failed: missing refs (fallback).");
            return;
        }

        // 챕터1 1스테이지로 진행 인덱스 리셋 후 StageLoad
        _ctx.ResetToChapterStart();
        _sm.ChangeState(_ctx, _stageLoad);
    }
}
