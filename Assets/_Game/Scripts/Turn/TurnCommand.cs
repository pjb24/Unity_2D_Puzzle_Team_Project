public enum E_TurnCommandType
{
    None,
    MoveUp,
    MoveDown,
    MoveLeft,
    MoveRight,
    Interact,
    Cancel,
    RewindEnter,
    RewindPrev,
    RewindNext,
    RewindCommit,
    RewindCancel,
}

public readonly struct TurnCommand
{
    public readonly E_TurnCommandType Type;
    public TurnCommand(E_TurnCommandType type) { Type = type; }
    public override string ToString() => Type.ToString();
}
