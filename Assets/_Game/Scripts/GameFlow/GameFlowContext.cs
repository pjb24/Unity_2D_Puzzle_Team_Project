/// <summary>
/// 목적
/// “현재 진행 데이터 + 서비스 참조”를 한 덩어리로 상태들에 전달
/// </summary>

public class GameFlowContext
{
    // ===== Progress =====
    public int _chapterIndex;
    public int _stageIndex;
    public bool _isEnding;

    // ===== Runtime/Config =====
    public GameConfig _gameConfig;
    public ChapterVisualProfile _chapterVisualProfile;
    public StageDefinition _stageDefinition;

    // ===== Services =====
    public GameFlowSignalBus _signals;

    // 추후 확장용 (StageLoader, SceneGateway, UIFacade 등)
    public ISceneGateway _scene;
    public IConfigProvider _config;
    public StageRuntimeRefs _stageRuntime;
    public IStageLoader _stageLoader;
    public IStageProgression _progression;
    public object _ui;

    /// <summary>
    /// 정책: 챕터 1, 스테이지 1(인덱스 0,0)으로 진행도를 리셋.
    /// StageLoad가 이 값을 보고 로드한다.
    /// </summary>
    public void ResetToChapterStart()
    {
        _chapterIndex = 0;
        _stageIndex = 0;
        _isEnding = false;

        // 진행도가 바뀌었으니, 캐시된 정의는 무효화 (StageLoad에서 다시 채움)
        _chapterVisualProfile = null;
        _stageDefinition = null;
    }
}
