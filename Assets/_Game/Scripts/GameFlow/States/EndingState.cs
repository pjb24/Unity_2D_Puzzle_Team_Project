using UnityEngine;

public class EndingState : IGameFlowState
{
    private readonly GameFlowStateMachine _sm;
    private IGameFlowState _mainMenu;

    public EndingState(GameFlowStateMachine sm)
    {
        _sm = sm;
    }

    public void SetMainMenu(IGameFlowState mainMenu)
    {
        _mainMenu = mainMenu;
    }

    public E_GameFlowState Id => E_GameFlowState.Ending;

    public void Enter(GameFlowContext ctx)
    {
        ctx._isEnding = true;

        Debug.Log("[Ending] Reached final ending. Return MainMenu.");

        ctx._scene.LoadMainMenu(() =>
        {
            _sm.ChangeState(ctx, _mainMenu);
        });
    }

    public void Tick(GameFlowContext ctx) { }
    public void Exit(GameFlowContext ctx) { }
}
