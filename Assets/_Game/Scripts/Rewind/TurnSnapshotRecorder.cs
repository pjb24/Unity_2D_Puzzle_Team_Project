using UnityEngine;

public class TurnSnapshotRecorder : MonoBehaviour
{
    public void Capture(int turnIndex)
    {
        Debug.Log($"[Rewind] Capture Snapshot turnIndex={turnIndex}");
        // TODO: IRewindable 전수 조사 후 TurnSnapshot 저장
    }
}
