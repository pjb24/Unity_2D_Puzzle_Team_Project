// ITurnTickable.cs
public interface ITurnTickable
{
    void OnTurnBegin(int turnIndex);
    void OnTurnEnd(int turnIndex);
}
