/// <summary>
/// FatherAction → ChildStep
/// 
/// FatherController가 “행동 완료”를 알리면 전이.
/// 
/// 주의: TurnPhase_FatherAction이 TurnStateMachine 참조를 필요로 하니,
/// 생성 순서가 꼬이면 팩토리로 조립하면 된다.
/// </summary>

public class TurnPhase_FatherAction : ITurnPhase
{
    public E_TurnPhase Phase => E_TurnPhase.FatherAction;

    private readonly TurnStateMachine _sm;

    public TurnPhase_FatherAction(TurnStateMachine sm) { _sm = sm; }

    public void Enter(TurnContext ctx)
    {
        ctx.Father.AddListenerOnActionCompleted(OnFatherDone);
    }

    public void Exit(TurnContext ctx)
    {
        ctx.Father.RemoveListenerOnActionCompleted(OnFatherDone);
    }

    public void Tick(TurnContext ctx) { }

    private void OnFatherDone()
    {
        _sm.Change(E_TurnPhase.ChildStep);
    }
}
