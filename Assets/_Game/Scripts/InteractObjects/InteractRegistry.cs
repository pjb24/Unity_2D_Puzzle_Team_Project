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

    public void RebuildFromScene()
    {
        _map.Clear();

        // interface는 직접 Find가 안 되므로 MonoBehaviour 전체 스캔 후 캐스팅
        var behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        for (int i = 0; i < behaviours.Length; i++)
        {
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
