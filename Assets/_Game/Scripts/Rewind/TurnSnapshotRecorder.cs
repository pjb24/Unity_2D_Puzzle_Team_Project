// TurnSnapshotRecorder.cs
///
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

    [Header("Scope")]
    [SerializeField] private Transform _stageRoot;

    private readonly List<TurnSnapshot> _snapshots = new();
    private bool _warnedNoStageRoot;

    public int Count => _snapshots.Count;
    public int LatestIndex => _snapshots.Count - 1; // 스냅샷이 0개면 -1

    // ^1은 뒤에서 첫번째를 의미함
    public TurnSnapshot GetLatest() => (_snapshots.Count > 0) ? _snapshots[^1] : null;

    public void BindStageRoot(Transform stageRoot)
    {
        _stageRoot = stageRoot;
        _warnedNoStageRoot = false;
    }

    public TurnSnapshot GetByTurnIndex(int turnIndex)
    {
        for (int i = _snapshots.Count - 1; i >= 0; i--)
            if (_snapshots[i]._turnIndex == turnIndex) return _snapshots[i];
        return null;
    }

    public TurnSnapshot GetAt(int index)
    {
        if (index < 0 || index >= _snapshots.Count)
            return null;

        return _snapshots[index];
    }

    public bool TryGetAt(int index, out TurnSnapshot snapshot)
    {
        snapshot = GetAt(index);
        return snapshot != null;
    }

    public int ClampIndex(int index)
    {
        if (_snapshots.Count <= 0) return -1;
        if (index < 0) return 0;
        if (index >= _snapshots.Count) return _snapshots.Count - 1;
        return index;
    }

    public void ClearAll() => _snapshots.Clear();

    public void Capture(int turnIndex)
    {
        // 1) Rewind 대상 전수 조사
        var rewindables = FindRewindablesInScope();

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
        var map = BuildRewindableMapInScope();

        for (int i = 0; i < snapshot._entries.Count; i++)
        {
            var e = snapshot._entries[i];

            if (!map.TryGetValue(e._keyGuid, out IRewindable rw))
            {
                Debug.LogWarning($"[Rewind] Restore fallback: rewindable not found. key={e._keyGuid}");
                continue;
            }

            if (string.IsNullOrEmpty(e._typeName) || string.IsNullOrEmpty(e._json))
                continue;

            var type = Type.GetType(e._typeName);
            if (type == null)
            {
                Debug.LogWarning($"[Rewind] Restore fallback: type not found. type={e._typeName}");
                continue;
            }

            try
            {
                object stateObj = JsonUtility.FromJson(e._json, type);
                rw.RestoreState(stateObj);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Rewind] Restore fallback: exception. key={e._keyGuid} ex={ex.Message}");
            }
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

        int clamped = ClampIndex(index);
        if (clamped < 0) return;
        if (clamped >= _snapshots.Count - 1) return;

        int removeStart = clamped + 1;
        int removeCount = _snapshots.Count - removeStart;

        _snapshots.RemoveRange(removeStart, removeCount);
        Debug.Log($"[Rewind] DiscardAfterIndex index={index}, removed={removeCount}, remain={_snapshots.Count}");
    }

    // ----- helpers -----

    private List<(Guid key, IRewindable rw)> FindRewindablesInScope()
    {
        var result = new List<(Guid, IRewindable)>();

        var behaviours = GetScopedBehaviours();
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] == null) continue;
            if (behaviours[i] is not IRewindable rw) continue;

            var key = behaviours[i].GetComponent<RewindKey>();
            if (key == null || !IsValid(key.Guid)) continue;

            result.Add((key.Guid, rw));
        }

        return result;
    }

    private Dictionary<Guid, IRewindable> BuildRewindableMapInScope()
    {
        var dict = new Dictionary<Guid, IRewindable>();

        var behaviours = GetScopedBehaviours();
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] == null) continue;
            if (behaviours[i] is not IRewindable rw) continue;

            var key = behaviours[i].GetComponent<RewindKey>();
            if (key == null || !IsValid(key.Guid)) continue;

            if (dict.ContainsKey(key.Guid))
            {
                Debug.LogWarning($"[Rewind] BuildMap warning: duplicate key detected. key={key.Guid}");
                continue;
            }

            dict.Add(key.Guid, rw);
        }

        return dict;
    }

    private MonoBehaviour[] GetScopedBehaviours()
    {
        if (_stageRoot != null)
            return _stageRoot.GetComponentsInChildren<MonoBehaviour>(includeInactive: true);

        if (!_warnedNoStageRoot)
        {
            _warnedNoStageRoot = true;
            Debug.LogWarning("[Rewind] Scope fallback: stageRoot is null. Using scene-wide scan. (BindStageRoot is recommended)");
        }

        return FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
    }

    public static bool IsValid(Guid value)
    {
        return value != Guid.Empty;
    }
}
