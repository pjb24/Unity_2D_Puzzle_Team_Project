/// <summary>
/// FatherAction → ChildStep
/// 
/// FatherController가 “행동 완료”를 알리면 전이.
/// 
/// 주의: TurnPhase_FatherAction이 TurnStateMachine 참조를 필요로 하니,
/// 생성 순서가 꼬이면 팩토리로 조립하면 된다.
/// 
/// Father 완료 이벤트가 오면 FatherController.LastResult를 읽어서 ctx에 저장 후 ChildStep으로 전이
/// </summary>

public class TurnPhase_FatherAction : ITurnPhase
{
    public E_TurnPhase Phase => E_TurnPhase.FatherAction;

    private readonly TurnStateMachine _sm;
    private TurnContext _ctx;

    public TurnPhase_FatherAction(TurnStateMachine sm) { _sm = sm; }

    public void Enter(TurnContext ctx)
    {
        _ctx = ctx;
        ctx.Father.AddListenerOnActionCompleted(OnFatherDone);
    }

    public void Exit(TurnContext ctx)
    {
        ctx.Father.RemoveListenerOnActionCompleted(OnFatherDone);
        if (_ctx == ctx) _ctx = null;
    }

    public void Tick(TurnContext ctx) { }

    private void OnFatherDone()
    {
        // FatherResult 저장(Resolve/디버그/UI 확장 포인트)
        if (_ctx != null)
            _ctx.FatherResult = _ctx.Father.LastResult;

        _sm.Change(E_TurnPhase.ChildStep);
    }
}
