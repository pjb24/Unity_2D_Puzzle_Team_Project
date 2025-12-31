// GameFlowStateMachine.cs
/// <summary>
/// 목적
/// 현재 상태 보관
/// 상태 변경 시 Exit/Enter 호출
/// 상태 변경 신호 브로드캐스트
/// </summary>

public class GameFlowStateMachine
{
    private IGameFlowState _current;

    public E_GameFlowState _currentId => _current?.Id ?? E_GameFlowState.Boot;

    public void ChangeState(GameFlowContext ctx, IGameFlowState next)
    {
        if (next == null) return;

        _current?.Exit(ctx);
        _current = next;

        ctx?._signals?.RaiseFlowStateChanged(_current.Id);

        _current.Enter(ctx);
    }

    public void Tick(GameFlowContext ctx)
    {
        _current?.Tick(ctx);
    }
}
