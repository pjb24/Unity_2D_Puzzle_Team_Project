// MainMenuState.cs
/// <summary>
/// 요구: MainMenu → StageLoad (그리고 Gameplay 씬 로드)
/// 핵심:
/// UI 버튼은 IStartGamePort만 호출
/// Start가 오면 Gameplay 로드 후 StageLoad로 전환
/// </summary>

public class MainMenuState : IGameFlowState, IStartGamePort
{
    private readonly GameFlowStateMachine _sm;
    private IGameFlowState _next;
    private GameFlowContext _ctx;

    public MainMenuState(GameFlowStateMachine sm)
    {
        _sm = sm;
    }

    public void SetNext(IGameFlowState next)
    {
        _next = next;
    }

    public E_GameFlowState Id => E_GameFlowState.MainMenu;

    public void Enter(GameFlowContext ctx)
    {
        _ctx = ctx;
    }

    public void Tick(GameFlowContext ctx) { }
    public void Exit(GameFlowContext ctx)
    {
        if (_ctx == ctx) _ctx = null;
    }

    public void RequestStartGame()
    {
        if (_ctx == null) return;

        // 진행값 초기화(프로토타입: 0,0에서 시작)
        _ctx._chapterIndex = 0;
        _ctx._stageIndex = 0;

        _ctx._scene.LoadGameplay(() =>
        {
            _sm.ChangeState(_ctx, _next);   // _next == StageLoadState
        });
    }
}
