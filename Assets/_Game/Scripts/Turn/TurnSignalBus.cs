// TurnSignalBus.cs
using System;

public enum E_TurnResolveOutcome
{
    // 턴의 최종 결론
    None,
    Continue,          // Easy에서 막혀도 계속
    StageCleared,      // 목표 도달
    StageFailed_Rewind,// Normal 실패 → 리와인드로
    StageFailed_Reset, // Hard 실패 → 즉시 리셋
}

public enum E_StageFailReason
{
    // 실패 원인
    None,
    ChildBlocked,
}

public class TurnSignalBus
{
    private Action<E_TurnResolveOutcome, E_StageFailReason, int> _onResolved;

    public void AddListenerOnResolved(Action<E_TurnResolveOutcome, E_StageFailReason, int> cb)
        => _onResolved += cb;

    public void RemoveListenerOnResolved(Action<E_TurnResolveOutcome, E_StageFailReason, int> cb)
        => _onResolved -= cb;

    public void RaiseResolved(E_TurnResolveOutcome outcome, E_StageFailReason reason, int turnIndex)
        => _onResolved?.Invoke(outcome, reason, turnIndex);
}
