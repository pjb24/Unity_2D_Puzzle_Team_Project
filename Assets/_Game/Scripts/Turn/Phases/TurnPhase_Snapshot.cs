// TurnPhase_Snapshot.cs
///
/// Snapshot → End
/// 
/// Snapshot은 “항상 턴 종료 직전 1회 저장” 원칙.
/// 저장 후 End로 전이.
///
using UnityEngine;

public class TurnPhase_Snapshot : ITurnPhase
{
    public E_TurnPhase Phase => E_TurnPhase.Snapshot;
    private readonly TurnStateMachine _sm;

    public TurnPhase_Snapshot(TurnStateMachine sm) { _sm = sm; }

    public void Enter(TurnContext ctx)
    {
        // 턴 종료 훅(스냅샷 저장 전에 수행)
        ctx.InvokeTurnEnd();

        if (ctx.SnapshotRecorder == null)
        {
            Debug.LogWarning("[TurnPhase_Snapshot] fallback: SnapshotRecorder is null.");
        }
        else
        {
            ctx.SnapshotRecorder.Capture(ctx.TurnIndex);
        }

        // 여기서 StageFailed / StageCleared 신호를 송출 (Resolve에서 미룸)
        if (ctx.TryConsumePendingOutcome(out var outcome, out var reason, out var turnIndex))
        {
            ctx._signals?.RaiseResolved(outcome, reason, turnIndex);
        }

        _sm.Change(E_TurnPhase.End);
    }

    public void Tick(TurnContext ctx) { }
    public void Exit(TurnContext ctx) { }
}
