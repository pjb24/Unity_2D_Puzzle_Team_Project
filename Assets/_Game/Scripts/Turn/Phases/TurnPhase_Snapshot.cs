// TurnPhase_Snapshot.cs
///
/// Snapshot → End
/// 
/// Snapshot은 “항상 턴 종료 직전 1회 저장” 원칙.
/// 저장 후 End로 전이.
///

public class TurnPhase_Snapshot : ITurnPhase
{
    public E_TurnPhase Phase => E_TurnPhase.Snapshot;
    private readonly TurnStateMachine _sm;

    public TurnPhase_Snapshot(TurnStateMachine sm) { _sm = sm; }

    public void Enter(TurnContext ctx)
    {
        ctx.SnapshotRecorder.Capture(ctx.TurnIndex);
        _sm.Change(E_TurnPhase.End);
    }

    public void Tick(TurnContext ctx) { }
    public void Exit(TurnContext ctx) { }
}
