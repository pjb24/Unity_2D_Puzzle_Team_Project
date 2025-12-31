// InteractPort_Registry.cs
using UnityEngine;

public class InteractPort_Registry : IInteractPort
{
    private readonly InteractRegistry _registry;

    public InteractPort_Registry(InteractRegistry registry)
    {
        _registry = registry;
    }

    public void RequestInteract(Vector2Int fatherCell, E_Facing facing)
    {
        if (_registry == null)
        {
            Debug.LogWarning("[InteractPort] Interact fallback: registry is null.");
            return;
        }

        Vector2Int dir = facing switch
        {
            E_Facing.Up => Vector2Int.up,
            E_Facing.Down => Vector2Int.down,
            E_Facing.Left => Vector2Int.left,
            E_Facing.Right => Vector2Int.right,
            _ => Vector2Int.zero
        };

        Vector2Int target = fatherCell + dir;
        var args = new FatherInteractArgs(fatherCell, facing, target);

        // 1) 타겟 셀 Interactable 먼저
        var list = _registry.GetAt(target);
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].TryInteract(args))
                return;
        }

        // 2) 필요하면 본인 셀도 허용(발판 스위치 같은 타입)
        list = _registry.GetAt(fatherCell);
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].TryInteract(args))
                return;
        }
    }
}
