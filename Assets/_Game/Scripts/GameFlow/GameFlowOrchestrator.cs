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
    private ChapterLoadState _chapterLoadState;
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
            _stageIndex = 0,
        };
        _ctx._gameConfig = _ctx._config.LoadGameConfig();

        _sm = new GameFlowStateMachine();

        _bootState = new BootState(_sm);
        _mainMenuState = new MainMenuState(_sm);
        _chapterLoadState = new ChapterLoadState(_sm);
        _stageLoadState = new StageLoadState(_sm);
        _playState = new GamePlayState(_sm);
        _stageClearState = new StageClearState(_sm);
        _endingState = new EndingState(_sm);

        _bootState.SetNext(_mainMenuState);
        _mainMenuState.SetNext(_chapterLoadState);
        _chapterLoadState.SetNext(_stageLoadState);
        _stageLoadState.SetNext(_playState);
        _playState.SetNext(_stageClearState);
        _playState.SetStageLoad(_stageLoadState);
        _stageClearState.SetMainMenuState(_mainMenuState);
        _stageClearState.SetStagedLoadState(_stageLoadState);
        _stageClearState.SetChapterLoadState(_chapterLoadState);
        _endingState.SetMainMenu(_mainMenuState);

        // 상태 변경 로그
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
        Debug.Log("[GameFlowOrchestrator] OnFlowStateChanged current state: " + state.ToString());
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // MainMenu 씬이 로드된 이후에만 바인딩
        if (scene.name == SceneMap.Get(E_Scene.MainMenu))
        {
            BindMainMenuUI();
            return;
        }
        
        if (scene.name == SceneMap.Get(E_Scene.Gameplay))
        {
            BindGameplayFx();
            return;
        }
    }

    private void BindMainMenuUI()
    {
        // MainMenuStartButton은 MainMenu 씬에 존재해야 한다.
        var startBtn = FindFirstObjectByType<MainMenuStartButton>();
        if (startBtn == null)
        {
            Debug.LogWarning("[GameFlowOrchestrator] MainMenuStartButton not found. UI bind skipped (fallback).");
            return;
        }

        // 현재 상태가 MainMenuState인지 확인 후 포트로 바인딩
        // 여기서는 "현재 상태를 Port로 제공"하는 구조가 필요하므로
        // 가장 단순하게 MainMenuState 인스턴스를 Orchestrator가 생성/보관한다.
        startBtn.Bind(_mainMenuState);
    }

    private void BindGameplayFx()
    {
        // TransitionFx / AudioHub는 씬에 배치하는 방식이 정석.
        // 없으면 폴백 경고 후 그냥 진행됨.
        _ctx._transitionFx = FindFirstObjectByType<StageTransitionFx>();
        if (_ctx._transitionFx == null)
            Debug.LogWarning("[GameFlowOrchestrator] StageTransitionFx not found in Gameplay scene (fallback).");

        _ctx._audioHub = FindFirstObjectByType<AudioHub>();
        if (_ctx._audioHub == null)
            Debug.LogWarning("[GameFlowOrchestrator] AudioHub not found (fallback).");
    }
}
