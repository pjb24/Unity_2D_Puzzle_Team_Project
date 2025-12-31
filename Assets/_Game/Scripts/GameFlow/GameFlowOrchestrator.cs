// GameFlowOrchestrator.cs
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class GameFlowOrchestrator : MonoBehaviour
{
    private GameFlowContext _ctx;
    private GameFlowStateMachine _sm;

    // 캐싱
    private BootState _bootState;
    private MainMenuState _mainMenuState;
    private StageLoadState _stageLoadState;
    private GamePlayState _playState;
    private StageClearState _stageClearState;
    private EndingState _endingState;
    

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);

        _ctx = new GameFlowContext
        {
            _signals = new GameFlowSignalBus(),
            _scene = new SceneGateway(),
            _config = new ResourcesConfigProvider(),
            _stageLoader = new DummyStageLoader(),
            _progression = new StageProgression(),
            _chapterIndex = 0,
            _stageIndex = 0
        };
        _ctx._gameConfig = _ctx._config.LoadGameConfig();

        _sm = new GameFlowStateMachine();

        _bootState = new BootState(_sm);
        _mainMenuState = new MainMenuState(_sm);
        _stageLoadState = new StageLoadState(_sm);
        _playState = new GamePlayState(_sm);
        _stageClearState = new StageClearState(_sm);
        _endingState = new EndingState(_sm);

        _bootState.SetNext(_mainMenuState);
        _mainMenuState.SetNext(_stageLoadState);
        _stageLoadState.SetNext(_playState);
        _playState.SetNext(_stageClearState);
        _playState.SetStageLoad(_stageLoadState);
        _stageClearState.SetMainMenuState(_mainMenuState);
        _stageClearState.SetStagedLoadState(_stageLoadState);
        _endingState.SetMainMenu(_mainMenuState);

        // 상태 변경 로그 테스트(구독/해제 패턴 확인)
        _ctx._signals.AddListenerOnFlowStateChanged(OnFlowStateChanged);

        SceneManager.sceneLoaded += OnSceneLoaded;

        _sm.ChangeState(_ctx, _bootState);
    }

    private void Update()
    {
        _sm.Tick(_ctx);
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        _ctx?._signals?.RemoveListenerOnFlowStateChanged(OnFlowStateChanged);
    }

    private void OnFlowStateChanged(E_GameFlowState state)
    {
        Debug.Log("[GameFlowOrchestrator] OnFlowStateChanged current state: " + _sm._currentId.ToString());
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // MainMenu 씬이 로드된 이후에만 바인딩
        if (scene.name == SceneMap.Get(E_Scene.MainMenu))
        {
            BindMainMenuUI();
        }
    }

    private void BindMainMenuUI()
    {
        // MainMenuStartButton은 MainMenu 씬에 존재해야 한다.
        var startBtn = FindFirstObjectByType<MainMenuStartButton>();
        if (startBtn == null) return;

        // 현재 상태가 MainMenuState인지 확인 후 포트로 바인딩
        // (간단히: MainMenuState 인스턴스를 Orchestrator가 들고 있어도 된다)
        // 여기서는 "현재 상태를 Port로 제공"하는 구조가 필요하므로
        // 가장 단순하게 MainMenuState 인스턴스를 Orchestrator가 생성/보관한다.

        // 권장 구조: 상태 인스턴스를 필드로 보관
        // -> Step 8에서 개선안 제공
        startBtn.Bind(_mainMenuState);
    }
}
