/// <summary>
/// Resolve → Snapshot
/// 
/// 난이도 정책까지는 나중에 붙이고,
/// 지금은 ChildBlocked만 판정해서 TurnFailed/TurnCleared를 세팅.
/// Resolve 종료 시점에 입력 잠금 OFF.
/// </summary>

public class TurnPhase_Resolve : ITurnPhase
{
    public E_TurnPhase Phase => E_TurnPhase.Resolve;

    private readonly TurnStateMachine _sm;

    public TurnPhase_Resolve(TurnStateMachine sm) { _sm = sm; }

    public void Enter(TurnContext ctx)
    {
        // TODO: 실제 정책(Easy/Normal/Hard) 적용
        if (ctx.ChildBlocked)
        {
            ctx.TurnFailed = true;
        }

        ctx.SetInputLocked(false); // 잠금 OFF (규칙: Resolve 종료)
        _sm.Change(E_TurnPhase.Snapshot);
    }

    public void Tick(TurnContext ctx) { }
    public void Exit(TurnContext ctx) { }
}
