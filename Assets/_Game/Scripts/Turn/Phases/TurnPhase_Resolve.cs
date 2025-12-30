/// <summary>
/// Resolve → Snapshot
/// 
/// 난이도 정책까지는 나중에 붙이고,
/// 지금은 ChildBlocked만 판정해서 TurnFailed/TurnCleared를 세팅.
/// Resolve 종료 시점에 입력 잠금 OFF.
/// 
/// Resolve에서 최소로 쓸만한 포인트는 2개:
/// Goal 트리거면 TurnCleared = true
/// 이동 실패면 로그/피드백 훅(나중에 UI/사운드)
/// </summary>

public class TurnPhase_Resolve : ITurnPhase
{
    public E_TurnPhase Phase => E_TurnPhase.Resolve;

    private readonly TurnStateMachine _sm;

    public TurnPhase_Resolve(TurnStateMachine sm) { _sm = sm; }

    public void Enter(TurnContext ctx)
    {
        // 1) Goal 체크
        if (ctx.FatherResult.TriggerGoal)
            ctx.TurnCleared = true;

        // 2) ChildBlocked 정책은 기존대로(나중에 난이도 적용)
        if (ctx.ChildBlocked)
            ctx.TurnFailed = true;

        ctx.SetInputLocked(false); // 잠금 OFF (규칙: Resolve 종료)
        _sm.Change(E_TurnPhase.Snapshot);
    }

    public void Tick(TurnContext ctx) { }
    public void Exit(TurnContext ctx) { }
}
