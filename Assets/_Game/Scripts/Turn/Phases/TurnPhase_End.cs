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
        // TODO: 실패/클리어면 GameFlow/Play 상위 상태로 신호
        ctx.ClearAcceptedInput();
        _sm.Change(E_TurnPhase.Input);
    }

    public void Tick(TurnContext ctx) { }
    public void Exit(TurnContext ctx) { }
}
