// InteractRegistry.cs
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class InteractRegistry : MonoBehaviour
{
    private readonly Dictionary<Vector2Int, List<IInteractable>> _map = new();

    public void Clear()
    {
        _map.Clear();
    }

    // 기존 API 유지(호환용). 씬 전체 스캔은 "언로드 직후 잔존 오브젝트"를 다시 등록시키는 원인이라 Warning.
    public void RebuildFromScene()
    {
        Debug.LogWarning("[InteractRegistry] RebuildFromScene is deprecated for stage runtime. Use RebuildFromRoot(root).");
        _map.Clear();

        var behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] == null) continue;
            if (behaviours[i] is IInteractable ia)
                Register(ia);
        }
    }

    // 스테이지 루트 하위만 스캔 (UnloadStage 문제 해결 핵심)
    public void RebuildFromRoot(Transform root)
    {
        _map.Clear();

        if (root == null)
        {
            Debug.LogWarning("[InteractRegistry] RebuildFromRoot fallback: root is null.");
            return;
        }

        var behaviours = root.GetComponentsInChildren<MonoBehaviour>(includeInactive: true);
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] == null) continue;
            if (behaviours[i] is IInteractable ia)
                Register(ia);
        }
    }

    public void Register(IInteractable interactable)
    {
        if (interactable == null)
        {
            Debug.LogWarning("[InteractRegistry] Register fallback: interactable is null.");
            return;
        }

        Vector2Int cell = interactable.Cell;

        if (!_map.TryGetValue(cell, out var list))
        {
            list = new List<IInteractable>(2);
            _map.Add(cell, list);
        }

        if (!list.Contains(interactable))
            list.Add(interactable);
    }

    public void Unregister(IInteractable interactable)
    {
        if (interactable == null)
        {
            Debug.LogWarning("[InteractRegistry] Unregister fallback: interactable is null.");
            return;
        }

        Vector2Int cell = interactable.Cell;
        if (!_map.TryGetValue(cell, out var list))
            return;

        list.Remove(interactable);
        if (list.Count == 0)
            _map.Remove(cell);
    }

    public IReadOnlyList<IInteractable> GetAt(Vector2Int cell)
    {
        if (_map.TryGetValue(cell, out var list))
            return list;

        return System.Array.Empty<IInteractable>();
    }
}
