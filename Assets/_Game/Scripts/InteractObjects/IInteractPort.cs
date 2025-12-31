// IInteractPort.cs
using UnityEngine;

public interface IInteractPort
{
    void RequestInteract(Vector2Int fatherCell, E_Facing facing);
}
