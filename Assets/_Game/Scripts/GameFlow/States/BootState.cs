// BootState.cs
///
/// 요구: Boot → MainMenu
/// 핵심:
/// GameConfig 로드 성공 확인
/// MainMenu 씬 로드
/// 상태 전환(메뉴 상태로)
///

using UnityEngine;

public class BootState : IGameFlowState
{
    private readonly GameFlowStateMachine _sm;
    private IGameFlowState _next;

    public BootState(GameFlowStateMachine sm)
    {
        _sm = sm;
    }

    public void SetNext(IGameFlowState next)
    {
        _next = next;
    }

    public E_GameFlowState Id => E_GameFlowState.Boot;

    public void Enter(GameFlowContext ctx)
    {
        var cfg = ctx._config.LoadGameConfig();
        if (cfg == null)
        {
            Debug.LogError("[Boot] GameConfig load failed. Flow stopped.");
            return;
        }

        // 여기서 GameConfig 로드 같은 초기화도 진행 가능 (지금은 생략)
        ctx._scene.LoadMainMenu();

        _sm.ChangeState(ctx, _next);
    }

    public void Tick(GameFlowContext ctx) { }
    public void Exit(GameFlowContext ctx) { }
}
