public interface IGameFlowState
{
    E_GameFlowState Id { get; }
    void Enter(GameFlowContext ctx);
    void Tick(GameFlowContext ctx);
    void Exit(GameFlowContext ctx);
}
