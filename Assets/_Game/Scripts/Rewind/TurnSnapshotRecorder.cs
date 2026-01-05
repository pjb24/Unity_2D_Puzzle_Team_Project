// TurnSnapshotRecorder.cs
///
/// 작업
/// TurnSnapshotRecorder에 스냅샷 리스트, MaxN(링버퍼), Capture 구현
/// 캡처 시점은 TurnPhase_Snapshot에서 1회 호출됨
///
using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class TurnSnapshotRecorder : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int _maxSnapshots = 64;

    private readonly List<TurnSnapshot> _snapshots = new();

    public int Count => _snapshots.Count;

    public int LatestIndex => _snapshots.Count - 1; // 스냅샷이 0개면 -1

    // ^1은 뒤에서 첫번째를 의미함
    public TurnSnapshot GetLatest() => (_snapshots.Count > 0) ? _snapshots[^1] : null;

    public TurnSnapshot GetByTurnIndex(int turnIndex)
    {
        for (int i = _snapshots.Count - 1; i >= 0; i--)
            if (_snapshots[i]._turnIndex == turnIndex) return _snapshots[i];
        return null;
    }

    public TurnSnapshot GetAt(int index)
    {
        if ((uint)index >= (uint)_snapshots.Count)
            return null;

        return _snapshots[index];
    }

    public bool TryGetAt(int index, out TurnSnapshot snapshot)
    {
        snapshot = null;

        if ((uint)index >= (uint)_snapshots.Count)
            return false;

        snapshot = _snapshots[index];
        return true;
    }

    public int ClampIndex(int index)
    {
        if (_snapshots.Count == 0) return -1;
        if (index < 0) return 0;
        if (index >= _snapshots.Count) return _snapshots.Count - 1;
        return index;
    }

    public void ClearAll() => _snapshots.Clear();

    public void Capture(int turnIndex)
    {
        // 1) Rewind 대상 전수 조사
        var rewindables = FindRewindables();

        // 2) Snapshot 생성
        var snap = new TurnSnapshot { _turnIndex = turnIndex };

        for (int i = 0; i < rewindables.Count; i++)
        {
            var (key, rw) = rewindables[i];

            object stateObj = rw.CaptureState();
            if (stateObj == null) continue;

            Type t = stateObj.GetType();
            string json = JsonUtility.ToJson(stateObj);

            snap._entries.Add(new TurnSnapshot.Entry
            {
                _keyGuid = key,
                _typeName = t.AssemblyQualifiedName,
                _json = json
            });
        }

        // 3) 보관(링버퍼)
        _snapshots.Add(snap);

        while (_maxSnapshots > 0 && _snapshots.Count > _maxSnapshots)
        {
            _snapshots.RemoveAt(0);
        }

        Debug.Log($"[Rewind] Capture Snapshot turnIndex={turnIndex}, entries={snap._entries.Count}");
    }

    public void Restore(TurnSnapshot snapshot)
    {
        if (snapshot == null)
        {
            Debug.LogWarning("[Rewind] Restore fallback: snapshot is null.");
            return;
        }

        // 현재 씬의 rewindables 맵 구성
        var map = BuildRewindableMap();

        for (int i = 0; i < snapshot._entries.Count; i++)
        {
            var e = snapshot._entries[i];

            if (!map.TryGetValue(e._keyGuid, out IRewindable rw))
            {
                Debug.LogWarning($"[Rewind] Restore fallback: rewindable not found. key={e._keyGuid}");
                continue;
            }

            var type = Type.GetType(e._typeName);
            if (type == null)
            {
                Debug.LogWarning($"[Rewind] Restore fallback: type not found. type={e._typeName}");
                continue;
            }

            object stateObj;
            try
            {
                stateObj = JsonUtility.FromJson(e._json, type);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Rewind] Restore fallback: json parse failed. type={e._typeName} ex={ex.Message}");
                continue;
            }

            if (stateObj == null)
            {
                Debug.LogWarning($"[Rewind] Restore fallback: stateObj is null. type={e._typeName}");
                continue;
            }

            rw.RestoreState(stateObj);
        }

        Debug.Log($"[Rewind] Restore Snapshot turnIndex={snapshot._turnIndex}");
    }

    public void DiscardAfterIndex(int index)
    {
        if (_snapshots.Count <= 0)
        {
            Debug.LogWarning("[Rewind] DiscardAfterIndex fallback: no snapshots.");
            return;
        }

        if (index < 0 || index >= _snapshots.Count)
        {
            Debug.LogWarning($"[Rewind] DiscardAfterIndex fallback: index out of range. index={index}, count={_snapshots.Count}");
            return;
        }

        int removeStart = index + 1;
        int removeCount = _snapshots.Count - removeStart;
        if (removeCount <= 0) return;

        _snapshots.RemoveRange(removeStart, removeCount);
        Debug.Log($"[Rewind] DiscardAfterIndex index={index}, removed={removeCount}, remain={_snapshots.Count}");
    }

    // ----- helpers -----

    private List<(Guid key, IRewindable rw)> FindRewindables()
    {
        var result = new List<(Guid, IRewindable)>();

        // 프로토타입: 씬 전수 조사로 충분 (최적화는 나중)
        var behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is not IRewindable rw) continue;

            var key = behaviours[i].GetComponent<RewindKey>();
            if (key == null || !IsValid(key.Guid)) continue;

            result.Add((key.Guid, rw));
        }

        return result;
    }

    private Dictionary<Guid, IRewindable> BuildRewindableMap()
    {
        var dict = new Dictionary<Guid, IRewindable>();

        var behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is not IRewindable rw) continue;

            var key = behaviours[i].GetComponent<RewindKey>();
            if (key == null || !IsValid(key.Guid)) continue;

            // 중복 키는 무시(또는 경고)
            if (!dict.ContainsKey(key.Guid))
                dict.Add(key.Guid, rw);
        }

        return dict;
    }

    public static bool IsValid(Guid value)
    {
        return value != Guid.Empty;
    }
}
