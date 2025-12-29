/// <summary>
/// 요구 파이프라인: SO 로드 → 런타임 생성 → 스폰 → UI 초기화(최소는 생략 가능)
/// StageLoader가 이미 “더미 보드/경로/스폰”을 해주므로, 상태는 호출만.
/// </summary>

public class StageLoadState : IGameFlowState
{
    private readonly GameFlowStateMachine _sm;
    private IGameFlowState _next;

    public StageLoadState(GameFlowStateMachine sm)
    {
        _sm = sm;
    }

    public void SetNext(IGameFlowState next)
    {
        _next = next;
    }

    public E_GameFlowState Id => E_GameFlowState.StageLoad;

    public void Enter(GameFlowContext ctx)
    {
        ctx._stageLoader.LoadStage(ctx, () =>
        {
            _sm.ChangeState(ctx, _next);
        });
    }

    public void Tick(GameFlowContext ctx) { }
    public void Exit(GameFlowContext ctx) { }
}
