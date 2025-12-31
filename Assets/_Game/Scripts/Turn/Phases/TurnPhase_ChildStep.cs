// TurnPhase_ChildStep.cs
/// <summary>
/// ChildStep → Resolve
/// 
/// ChildController가 1스텝 전진 시도 후 결과(막힘 여부)를 저장하고 완료 이벤트 호출.
/// </summary>

public class TurnPhase_ChildStep : ITurnPhase
{
    public E_TurnPhase Phase => E_TurnPhase.ChildStep;

    private readonly TurnStateMachine _sm;
    private TurnContext _ctx;

    public TurnPhase_ChildStep(TurnStateMachine sm) { _sm = sm; }

    public void Enter(TurnContext ctx)
    {
        _ctx = ctx;
        ctx.Child.AddListenerOnStepCompleted(OnChildStepDone);
        ctx.Child.RequestStep();
    }

    public void Exit(TurnContext ctx)
    {
        ctx.Child.RemoveListenerOnStepCompleted(OnChildStepDone);
        if (_ctx == ctx) _ctx = null;
    }

    public void Tick(TurnContext ctx) { }

    private void OnChildStepDone(bool blocked)
    {
        if (_ctx != null) _ctx.ChildBlocked = blocked;

        _sm.Change(E_TurnPhase.Resolve);
    }
}
