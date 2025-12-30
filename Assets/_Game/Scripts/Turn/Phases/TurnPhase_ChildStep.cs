/// <summary>
/// ChildStep → Resolve
/// 
/// ChildController가 1스텝 전진 시도 후 결과(막힘 여부)를 저장하고 완료 이벤트 호출.
/// 
/// 구현 시: OnChildStepDone에서 ctx.ChildBlocked = blocked;를 해야 하는데,
/// 콜백 시그니처에 ctx가 없으니 필드로 ctx를 잡아두거나,
/// ChildController가 “마지막 Step 결과” 프로퍼티를 들고 있게 해라.
/// 가장 단순: TurnPhase_ChildStep이 Enter에서 _ctx = ctx 캐시.
/// </summary>

public class TurnPhase_ChildStep : ITurnPhase
{
    public E_TurnPhase Phase => E_TurnPhase.ChildStep;

    private readonly TurnStateMachine _sm;

    public TurnPhase_ChildStep(TurnStateMachine sm) { _sm = sm; }

    public void Enter(TurnContext ctx)
    {
        ctx.Child.AddListenerOnStepCompleted(OnChildStepDone);
        ctx.Child.RequestStep();
    }

    public void Exit(TurnContext ctx)
    {
        ctx.Child.RemoveListenerOnStepCompleted(OnChildStepDone);
    }

    public void Tick(TurnContext ctx) { }

    private void OnChildStepDone(bool blocked)
    {
        // blocked는 ctx에 저장하는 편이 추적/테스트가 쉽다
        // (ctx는 현재 Turn 데이터 컨테이너)
        // 다음 Phase에서 참조
        _sm.Change(E_TurnPhase.Resolve);
    }
}
