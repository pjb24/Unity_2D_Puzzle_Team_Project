public enum E_TurnCommandType
{
    None,
    MoveUp,
    MoveDown,
    MoveLeft,
    MoveRight,
    Interact,
    TurnCancel,
    RewindPrev,
    RewindNext,
}

public readonly struct TurnCommand
{
    public readonly E_TurnCommandType Type;
    public TurnCommand(E_TurnCommandType type) { Type = type; }
    public override string ToString() => Type.ToString();
}
