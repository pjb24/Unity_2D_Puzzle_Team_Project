// IInteractable.cs
using UnityEngine;

public readonly struct FatherInteractArgs
{
    public readonly Vector2Int FatherCell;
    public readonly E_Facing Facing;
    public readonly Vector2Int TargetCell;

    public FatherInteractArgs(Vector2Int fatherCell, E_Facing facing, Vector2Int targetCell)
    {
        FatherCell = fatherCell;
        Facing = facing;
        TargetCell = targetCell;
    }
}

public interface IInteractable
{
    Vector2Int Cell { get; }

    /// <summary>
    /// 상호작용을 처리했으면 true 반환(소비), 아니면 false.
    /// </summary>
    bool TryInteract(in FatherInteractArgs args);
}
