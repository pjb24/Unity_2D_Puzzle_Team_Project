// TurnPhase_End.cs
/// <summary>
/// End → Input
/// 
/// End에서 Turn 데이터 정리, 다음 턴 준비.
/// </summary>

public class TurnPhase_End : ITurnPhase
{
    public E_TurnPhase Phase => E_TurnPhase.End;

    private readonly TurnStateMachine _sm;
    public TurnPhase_End(TurnStateMachine sm) { _sm = sm; }

    public void Enter(TurnContext ctx)
    {
        ctx.SetInputLocked(false);
        ctx.ClearAcceptedInput();

        // 실패/클리어면 자동턴 없음
        if (ctx.TurnFailed || ctx.TurnCleared)
        {
            ctx.PendingAutoTurns = 0;
            _sm.Change(E_TurnPhase.Input);
            return;
        }

        // “턴 비용(2턴)” 처리: 입력 없이 자동 실행
        if (ctx.PendingAutoTurns > 0)
        {
            ctx.PendingAutoTurns--;

            ctx.BeginAutoTurn();
            ctx.InvokeTurnBegin();

            // 자동 턴은 ChildStep → Resolve → Snapshot만 수행
            _sm.Change(E_TurnPhase.ChildStep);
            return;
        }

        _sm.Change(E_TurnPhase.Input);
    }

    public void Tick(TurnContext ctx) { }
    public void Exit(TurnContext ctx) { }
}
