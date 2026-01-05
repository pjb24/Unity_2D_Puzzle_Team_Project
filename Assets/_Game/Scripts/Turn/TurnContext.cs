// TurnContext.cs
using System;
using System.Collections.Generic;
using UnityEngine;

public class TurnContext
{
    public int TurnIndex { get; private set; } = 0;

    public bool IsInputLocked { get; private set; } = false;
    public bool HasAcceptedInput { get; private set; } = false;

    public TurnCommand AcceptedCommand { get; private set; }

    // 결과(Resolve에서 채움)
    public bool ChildBlocked { get; set; }
    public bool TurnFailed { get; set; }
    public bool TurnCleared { get; set; }

    // 2턴(늪) 지원: 입력 없는 자동 턴 예약
    public int PendingAutoTurns { get; set; }

    // 의존 참조
    public FatherController Father { get; }
    public ChildController Child { get; }
    public TurnSnapshotRecorder SnapshotRecorder { get; }

    public FatherActionResult FatherResult { get; set; }

    public DifficultyProfile _profile { get; private set; }
    public TurnSignalBus _signals { get; private set; }

    private List<ITurnTickable> _turnSystems = new List<ITurnTickable>(8);

    private int _lastBeginTurnIndex = int.MinValue;
    private int _lastEndTurnIndex = int.MinValue;

    public E_TurnResolveOutcome PendingOutcome { get; private set; } = E_TurnResolveOutcome.Continue;
    public E_StageFailReason PendingFailReason { get; private set; } = E_StageFailReason.None;
    public bool HasPendingOutcome { get; private set; } = false;

    public int ChildGoalPathStep { get; private set; } = -1;

    public void InjectDifficulty(DifficultyProfile profile) => _profile = profile;
    public void InjectSignals(TurnSignalBus signals) => _signals = signals;

    public void InjectTurnSystems(IReadOnlyList<ITurnTickable> systems)
    {
        _turnSystems.Clear();
        if (systems == null) return;

        for (int i = 0; i < systems.Count; i++)
            if (systems[i] != null)
                _turnSystems.Add(systems[i]);
    }

    public void InjectChildGoalPathStep(int step) => ChildGoalPathStep = step;

    public TurnContext(FatherController father,
        ChildController child,
        TurnSnapshotRecorder snapshotRecorder)
    {
        Father = father;
        Child = child;
        SnapshotRecorder = snapshotRecorder;

        TurnIndex = 0;
        PendingAutoTurns = 0;
    }

    public void BeginNewTurn(TurnCommand cmd)
    {
        TurnIndex++;
        HasAcceptedInput = true;
        AcceptedCommand = cmd;

        ClearTurnResults();

        // 예약 자동 턴은 Resolve에서 세팅한다(턴 결과 확정 후)
        PendingAutoTurns = 0;

        Debug.Log($"[Turn] Tick TurnIndex={TurnIndex}, Cmd={cmd.Type}");
    }

    public void BeginAutoTurn()
    {
        TurnIndex++;

        ClearAcceptedInput();

        ClearTurnResults();

        // 자동 턴은 FatherResult를 재사용하면 안 됨
        FatherResult = default;

        Debug.Log($"[Turn] AutoTick TurnIndex={TurnIndex}");
    }

    public void SetInputLocked(bool locked)
    {
        IsInputLocked = locked;
        Debug.Log($"[Turn] InputLocked={(locked ? "ON" : "OFF")} (TurnIndex={TurnIndex})");
    }

    public void ClearAcceptedInput()
    {
        HasAcceptedInput = false;
        AcceptedCommand = default;
    }

    private void ClearTurnResults()
    {
        ChildBlocked = false;
        TurnFailed = false;
        TurnCleared = false;
    }

    public void RollbackTurnBecauseFatherBlocked()
    {
        // BeginNewTurn에서 TurnIndex++ 했던 걸 되돌림
        if (TurnIndex > 0)
            TurnIndex--;

        // 이번 턴은 없었던 것으로 처리
        ClearAcceptedInput();

        ClearTurnResults();
        PendingAutoTurns = 0;

        // 훅 중복 방지 인덱스도 롤백
        _lastBeginTurnIndex = int.MinValue;
        _lastEndTurnIndex = int.MinValue;

        FatherResult = default;

        Debug.Log($"[Turn] Rollback because Father blocked. TurnIndex={TurnIndex}");
    }

    // 턴 훅 호출(중복 방지)
    public void InvokeTurnBegin()
    {
        if (_lastBeginTurnIndex == TurnIndex) return;
        _lastBeginTurnIndex = TurnIndex;

        for (int i = 0; i < _turnSystems.Count; i++)
        {
            try { _turnSystems[i].OnTurnBegin(TurnIndex); }
            catch (Exception ex) { Debug.LogWarning($"[Turn] OnTurnBegin system failed. ex={ex.Message}"); }
        }
    }

    public void InvokeTurnEnd()
    {
        if (_lastEndTurnIndex == TurnIndex) return;
        _lastEndTurnIndex = TurnIndex;

        for (int i = 0; i < _turnSystems.Count; i++)
        {
            try { _turnSystems[i].OnTurnEnd(TurnIndex); }
            catch (Exception ex) { Debug.LogWarning($"[Turn] OnTurnEnd system failed. ex={ex.Message}"); }
        }
    }

    public void SetTurnIndexFromRewind(int turnIndex)
    {
        TurnIndex = Mathf.Max(0, turnIndex);

        IsInputLocked = false;

        ClearTurnResults();

        // 훅 중복 방지 인덱스도 리셋
        _lastBeginTurnIndex = int.MinValue;
        _lastEndTurnIndex = int.MinValue;

        PendingAutoTurns = 0;
        ClearAcceptedInput();

        FatherResult = default;

        Debug.Log($"[Turn] Sync from snapshot. TurnIndex={TurnIndex}");
    }

    public void SetPendingOutcome(E_TurnResolveOutcome outcome, E_StageFailReason reason)
    {
        HasPendingOutcome = true;
        PendingOutcome = outcome;
        PendingFailReason = reason;
    }

    public bool TryConsumePendingOutcome(out E_TurnResolveOutcome outcome, out E_StageFailReason reason, out int turnIndex)
    {
        outcome = PendingOutcome;
        reason = PendingFailReason;
        turnIndex = TurnIndex;

        if (!HasPendingOutcome)
            return false;

        HasPendingOutcome = false;
        PendingOutcome = E_TurnResolveOutcome.Continue;
        PendingFailReason = E_StageFailReason.None;
        return true;
    }
}
