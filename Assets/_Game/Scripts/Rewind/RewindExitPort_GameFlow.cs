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
        _sm.ChangeState(_ctx, _stageLoad);
    }

    public void RequestReturnToChapterStart()
    {
        // 챕터1 1스테이지로 진행 인덱스 리셋 후 StageLoad
        _ctx.ResetToChapterStart();
        _sm.ChangeState(_ctx, _stageLoad);
    }
}
