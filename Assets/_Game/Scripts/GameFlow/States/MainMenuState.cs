// MainMenuState.cs
/// <summary>
/// 요구: MainMenu → ChapterLoad (그리고 Gameplay 씬 로드)
/// 핵심:
/// UI 버튼은 IStartGamePort만 호출
/// Start가 오면 Gameplay 로드 후 ChapterLoad로 전환
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
        // UI 바인딩은 Orchestrator가 SceneLoaded 이후에 수행
    }

    public void Tick(GameFlowContext ctx) { }
    public void Exit(GameFlowContext ctx)
    {
        _ctx = null;
    }

    public void RequestStartGame()
    {
        if (_ctx == null) return;

        // 진행값 초기화
        _ctx._chapterIndex = 0;
        _ctx._stageIndex = 0;
        _ctx._isEnding = false;
        _ctx._stageDefinition = null;

        _ctx._scene.LoadGameplay(() =>
        {
            // MainMenu -> ChapterLoad -> StageLoad
            // 다음은 ChapterLoad로 보내서 VisualProfile 적용 후 StageLoad로 감
            _sm.ChangeState(_ctx, _next);
        });
    }
}
