// GameFlowContext.cs
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

    public ISceneGateway _scene;
    public IConfigProvider _config;
    public StageRuntimeRefs _stageRuntime;
    public IStageLoader _stageLoader;
    public IStageProgression _progression;
    public object _ui;

    // ===== Fail Streak (Normal) =====
    private int _failStreak;
    private int _failStreakLimit; // 0이면 비활성

    public void SetFailStreakLimit(int limit)
    {
        if (limit < 0) limit = 0;
        _failStreakLimit = limit;

        // limit이 꺼지면 누적 의미가 없으니 정리
        if (_failStreakLimit == 0)
            _failStreak = 0;
    }

    public void ResetFailStreak()
    {
        _failStreak = 0;
    }

    /// <summary>
    /// "스테이지 재시작"이 발생했을 때만 호출한다.
    /// true면 챕터 시작으로 복귀해야 한다.
    /// </summary>
    public bool RecordFailAndShouldReturnChapterStart()
    {
        if (_failStreakLimit <= 0)
            return false;

        _failStreak++;

        if (_failStreak < _failStreakLimit)
            return false;

        // 트리거 후 즉시 리셋(루프 방지)
        _failStreak = 0;
        return true;
    }

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

        // 챕터 복귀면 실패 누적도 무조건 리셋
        _failStreak = 0;
    }
}
