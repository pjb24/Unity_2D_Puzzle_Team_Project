// ChildPathBlockerRegistry.cs
using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class ChildPathBlockerRegistry
{
    private readonly HashSet<int> _blocked = new HashSet<int>();
    private int _pathCount;

    public int PathCount => _pathCount;

    public ChildPathBlockerRegistry(IReadOnlyList<int> initialBlockedSteps, int pathCount)
    {
        Reset(initialBlockedSteps, pathCount);
    }

    public void Reset(IReadOnlyList<int> initialBlockedSteps, int pathCount)
    {
        _blocked.Clear();

        _pathCount = pathCount < 0 ? 0 : pathCount;

        if (initialBlockedSteps == null) return;

        for (int i = 0; i < initialBlockedSteps.Count; i++)
        {
            int s = initialBlockedSteps[i];
            if (!IsInRange(s))
            {
                Debug.LogWarning($"[ChildPathBlockerRegistry] Reset fallback: step out of range. step={s} pathCount={_pathCount}");
                continue;
            }
            _blocked.Add(s);
        }
    }

    public bool IsBlocked(int step)
    {
        if (!IsInRange(step))
        {
            Debug.LogWarning($"[ChildPathBlockerRegistry] IsBlocked fallback: step out of range. step={step} pathCount={_pathCount}");
            return true; // 안전: 범위 밖은 막힘 처리
        }

        return _blocked.Contains(step);
    }

    public void SetBlocked(int step, bool blocked, string reason)
    {
        if (!IsInRange(step))
        {
            Debug.LogWarning($"[ChildPathBlockerRegistry] SetBlocked fallback: step out of range. step={step} pathCount={_pathCount} reason={reason}");
            return;
        }

        if (blocked) _blocked.Add(step);
        else _blocked.Remove(step);
    }

    private bool IsInRange(int step) => step >= 0 && step < _pathCount;
}
