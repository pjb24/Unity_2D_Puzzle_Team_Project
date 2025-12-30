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

    public TurnContext(FatherController father, ChildController child, TurnSnapshotRecorder snapshotRecorder)
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
}
