///
/// GameConfig는 1회 로드 후 캐시
/// StageDefinition은
/// GameConfig에 참조가 있으면 그걸 사용
/// 없으면 Resources 경로 규칙으로 로드
///

using UnityEngine;

public class ResourcesConfigProvider : IConfigProvider
{
    private const string _gameConfigPath = "Configs/GameConfig"; // Resources/Configs/GameConfig.asset
    private GameConfig _cachedGameConfig;

    public GameConfig LoadGameConfig()
    {
        if (_cachedGameConfig != null) return _cachedGameConfig;

        _cachedGameConfig = Resources.Load<GameConfig>(_gameConfigPath);
        if (_cachedGameConfig == null)
        {
            Debug.LogError($"[Config] GameConfig not found at Resources/{_gameConfigPath}.asset");
        }

        return _cachedGameConfig;
    }

    public StageDefinition GetStageDefinition(int chapterIndex, int stageIndex)
    {
        var cfg = LoadGameConfig();
        if (cfg == null) return null;

        // 인덱스 검증
        if (chapterIndex < 0 || chapterIndex >= cfg.Chapters.Count)
        {
            Debug.LogError($"[Config] Invalid chapterIndex={chapterIndex}, chapters={cfg.Chapters.Count}");
            return null;
        }

        var chapter = cfg.Chapters[chapterIndex];
        if (stageIndex < 0 || stageIndex >= chapter.Stages.Count)
        {
            Debug.LogError($"[Config] Invalid stageIndex={stageIndex}, stages={chapter.Stages.Count} (chapterIndex={chapterIndex})");
            return null;
        }

        // 1) GameConfig가 StageDefinition 레퍼런스를 들고 있으면 그걸 우선 사용
        var def = chapter.Stages[stageIndex];
        if (def != null) return def;

        // 2) 비어있으면 파일명 규칙으로 Resources에서 로드
        // 규칙: Resources/Stages/ChapterXX_StageYY.asset
        string chapterId = chapter.ChapterId; // "Chapter01"
        string stageId = $"Stage{(stageIndex + 1):00}"; // "Stage01"
        string path = $"Stages/{chapterId}_{stageId}";

        def = Resources.Load<StageDefinition>(path);
        if (def == null)
        {
            Debug.LogError($"[Config] StageDefinition not found. Resources/{path}.asset");
        }

        return def;
    }
}
