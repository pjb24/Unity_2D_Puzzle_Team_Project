// TurnDriver.cs
///
/// Phase들이 TurnStateMachine을 참조하니, 팩토리 패턴으로 한 번에 조립한다.
///
using System;
using System.Collections.Generic;
using UnityEngine;

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

public enum E_ChildBlockedCause
{
    None = 0,
    WallOrBlockedCell = 1,
    DoorOrGimmick = 2,
    Unknown = 99,
}

[DisallowMultipleComponent]
public class TurnDriver : MonoBehaviour
{
    private bool _isBound;

    private TurnStateMachine _sm;
    private TurnContext _ctx;
    private TurnInputBuffer _input;

    private TurnInputRouter _router;

    public bool IsInputLocked => _ctx != null && _ctx.IsInputLocked;
    public int TurnIndex => _ctx != null ? _ctx.TurnIndex : 0;

    private Action<E_TurnResolveOutcome, E_StageFailReason, int> _onResolved;
    private Action<E_TurnResolveOutcome, E_StageFailReason, E_ChildBlockedCause, int> _onResolvedDetailed;

    private ChildController _child;
    private FatherController _father;

    public void AddListenerOnResolved(Action<E_TurnResolveOutcome, E_StageFailReason, int> cb) => _onResolved += cb;
    public void RemoveListenerOnResolved(Action<E_TurnResolveOutcome, E_StageFailReason, int> cb) => _onResolved -= cb;

    public void AddListenerOnResolvedDetailed(Action<E_TurnResolveOutcome, E_StageFailReason, E_ChildBlockedCause, int> cb) => _onResolvedDetailed += cb;
    public void RemoveListenerOnResolvedDetailed(Action<E_TurnResolveOutcome, E_StageFailReason, E_ChildBlockedCause, int> cb) => _onResolvedDetailed -= cb;

    public void Bind(
        FatherController father,
        ChildController child,
        TurnSnapshotRecorder snapshot,
        TurnInputRouter router,
        DifficultyProfile profile,
        IReadOnlyList<ITurnTickable> turnSystems = null,
        int childGoalPathStep = -1)
    {
        if (_isBound) return;

        _router = router;

        _input = new TurnInputBuffer();
        _ctx = new TurnContext(father, child, snapshot);

        _father = father;
        _child = child;

        // Inject
        _ctx.InjectDifficulty(profile);
        _ctx.InjectSignals(this);
        _ctx.InjectTurnSystems(turnSystems);
        _ctx.InjectChildGoalPathStep(childGoalPathStep);

        _sm = new TurnStateMachine(_ctx);

        // Router에 버퍼 주입
        if (_router != null)
            _router.Initialize(_input);

        // Phase 생성 (이제 sm이 이미 존재하므로 주입 가능)
        var phases = new ITurnPhase[]
        {
            new TurnPhase_Input(_input, _sm),
            new TurnPhase_FatherAction(_sm),
            new TurnPhase_ChildStep(_sm),
            new TurnPhase_Resolve(_sm),
            new TurnPhase_Snapshot(_sm),
            new TurnPhase_End(_sm),
        };

        _sm.SetPhases(phases);

        _sm.Start();
        _isBound = true;
    }

    public void Unbind()
    {
        if (!_isBound) return;

        // 입력 버퍼 비우기(턴 꼬임 방지)
        _input?.Clear();

        // 입력 차단용으로 라우터 끊기
        if (_router != null)
            _router.Initialize(null);

        _sm = null;
        _ctx = null;
        _input = null;
        _router = null;

        _isBound = false;
    }

    private void Update()
    {
        if (!_isBound) return;
        _sm.Tick();
    }

    public void ClearInputBuffer()
    {
        _input?.Clear();
    }

    public void SyncTurnIndexFromSnapshot(int turnIndex)
    {
        if (_ctx == null) return;

        _ctx.SetTurnIndexFromRewind(turnIndex);

        // 안전하게 Input 상태로 돌리고 버퍼 비움
        _input?.Clear();
        _sm?.Change(E_TurnPhase.Input);
    }

    public void RaiseResolved(E_TurnResolveOutcome outcome, E_StageFailReason reason, int turnIndex)
    {
        E_ChildBlockedCause cause = E_ChildBlockedCause.None;

        if (reason == E_StageFailReason.ChildBlocked)
        {
            if (_child == null)
            {
                Debug.LogWarning("[TurnDriver] ChildBlocked cause fallback: child is null.");
                cause = E_ChildBlockedCause.Unknown;
            }
            else
            {
                cause = _child.LastBlockedCause;
                if (cause == E_ChildBlockedCause.None)
                    cause = E_ChildBlockedCause.Unknown;
            }
        }

        _onResolved?.Invoke(outcome, reason, turnIndex);
        _onResolvedDetailed?.Invoke(outcome, reason, cause, turnIndex);
    }
}
