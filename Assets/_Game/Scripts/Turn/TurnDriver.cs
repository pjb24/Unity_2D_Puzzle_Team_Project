// TurnDriver.cs
///
/// Phase들이 TurnStateMachine을 참조하니, 팩토리 패턴으로 한 번에 조립한다.
///

using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class TurnDriver : MonoBehaviour
{
    private bool _isBound;

    private TurnStateMachine _sm;
    private TurnContext _ctx;
    private TurnInputBuffer _input;

    private TurnInputRouter _router;
    private TurnSignalBus _signals = new TurnSignalBus();

    public bool IsInputLocked => _ctx != null && _ctx.IsInputLocked;
    public int TurnIndex => _ctx != null ? _ctx.TurnIndex : 0;

    public void AddListenerOnResolved(Action<E_TurnResolveOutcome, E_StageFailReason, int> cb)
    => _signals.AddListenerOnResolved(cb);

    public void RemoveListenerOnResolved(Action<E_TurnResolveOutcome, E_StageFailReason, int> cb)
        => _signals.RemoveListenerOnResolved(cb);

    public void Bind(FatherController father,
        ChildController child,
        TurnSnapshotRecorder snapshot,
        TurnInputRouter router,
        DifficultyProfile profile,
        IReadOnlyList<ITurnTickable> turnSystems = null)
    {
        if (_isBound) return;

        _router = router;

        _input = new TurnInputBuffer();
        _ctx = new TurnContext(father, child, snapshot);

        // Inject
        _ctx.InjectDifficulty(profile);
        _ctx.InjectSignals(_signals);
        _ctx.InjectTurnSystems(turnSystems);

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
}
