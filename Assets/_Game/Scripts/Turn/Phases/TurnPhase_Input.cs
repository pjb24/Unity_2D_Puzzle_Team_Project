// TurnPhase_Input.cs
/// <summary>
/// Input → FatherAction
/// 
/// InputPhase는 “유효 입력 수락”만 담당.
/// 수락 시 ctx.BeginNewTurn(cmd) 후 FatherAction으로 전이.
/// 
/// 핵심: Input 상태에서만 Dequeue. 그 외 상태에서는 입력 버퍼를 건드리지 않음.
/// </summary>

public class TurnPhase_Input : ITurnPhase
{
    public E_TurnPhase Phase => E_TurnPhase.Input;

    private readonly TurnInputBuffer _input;
    private readonly TurnStateMachine _sm;

    public TurnPhase_Input(TurnInputBuffer input, TurnStateMachine sm)
    {
        _input = input;
        _sm = sm;
    }

    public void Enter(TurnContext ctx)
    {
        ctx.SetInputLocked(false);
        ctx.ClearAcceptedInput();
    }

    public void Exit(TurnContext ctx) { }

    public void Tick(TurnContext ctx)
    {
        if (ctx.IsInputLocked) return;
        if (_input == null) return;

        if (_input.TryDequeue(out TurnCommand cmd))
        {
            _sm.Change(E_TurnPhase.FatherAction);

            ctx.BeginNewTurn(cmd);
            ctx.SetInputLocked(true);          // 잠금 ON (규칙)
            ctx.Father.RequestAction(cmd);     // FatherAction이 구독/완료로 이어짐
        }
    }
}
