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

    // ===== Runtime/Config (나중에 실제 타입으로 교체) =====
    // 지금은 뼈대 단계라 object로 두고, SO 구축 끝나면 GameConfig/StageDefinition 등으로 바꿔라.
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
}
