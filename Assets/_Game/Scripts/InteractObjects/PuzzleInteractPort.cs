using UnityEngine;

public class PuzzleInteractPort : IInteractPort
{
    private readonly SwitchController _switch;

    public PuzzleInteractPort(SwitchController sw)
    {
        _switch = sw;
    }

    public void RequestInteract(Vector2Int fatherCell, E_Facing facing)
    {
        if (_switch == null)
        {
            Debug.LogWarning("[PuzzleInteractPort] Interact fallback: switch is null.");
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

        if (target == _switch.Cell || fatherCell == _switch.Cell)
        {
            _switch.Toggle();
        }
    }
}
