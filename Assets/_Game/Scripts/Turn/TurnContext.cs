// TurnContext.cs
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

    // 의존 참조
    public FatherController Father { get; }
    public ChildController Child { get; }
    public TurnSnapshotRecorder SnapshotRecorder { get; }

    public FatherActionResult FatherResult { get; set; }

    public DifficultyProfile _profile { get; private set; }
    public TurnSignalBus _signals { get; private set; }

    public void InjectDifficulty(DifficultyProfile profile) => _profile = profile;
    public void InjectSignals(TurnSignalBus signals) => _signals = signals;

    public TurnContext(FatherController father,
        ChildController child,
        TurnSnapshotRecorder snapshotRecorder)
    {
        Father = father;
        Child = child;
        SnapshotRecorder = snapshotRecorder;
    }

    public void BeginNewTurn(TurnCommand cmd)
    {
        TurnIndex++;
        HasAcceptedInput = true;
        AcceptedCommand = cmd;
        ChildBlocked = false;
        TurnFailed = false;
        TurnCleared = false;
        Debug.Log($"[Turn] Tick TurnIndex={TurnIndex}, Cmd={cmd}");
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

    public void RollbackTurnBecauseFatherBlocked()
    {
        // BeginNewTurn에서 TurnIndex++ 했던 걸 되돌림
        if (TurnIndex > 0)
            TurnIndex--;

        // 이번 턴은 없었던 것으로 처리
        HasAcceptedInput = false;
        AcceptedCommand = default;

        ChildBlocked = false;
        TurnFailed = false;
        TurnCleared = false;

        FatherResult = default;
    }

    public void SetTurnIndexFromRewind(int turnIndex)
    {
        TurnIndex = Mathf.Max(0, turnIndex);

        IsInputLocked = false;
        HasAcceptedInput = false;
        AcceptedCommand = default;

        ChildBlocked = false;
        TurnFailed = false;
        TurnCleared = false;

        FatherResult = default;
    }
}
