// ResourcesConfigProvider.cs
///
/// GameConfig는 1회 로드 후 캐시
/// StageDefinition은
/// GameConfig에 참조가 있으면 그걸 사용
/// 없으면 Resources 경로 규칙으로 로드
///
using System.Collections.Generic;
using UnityEngine;

public class ResourcesConfigProvider
{
    private const string _gameConfigPath = "Configs/GameConfig"; // Resources/Configs/GameConfig.asset
    private GameConfig _cachedGameConfig;

    // JSON -> StageDefinition 캐시
    private readonly Dictionary<string, StageDefinition> _jsonStageCache = new();

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

    // 챕터/스테이지 인덱스로 StageDefinition 접근
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

        // TODO: JSON 파일을 SO로 가지고 있는 방향으로 변경 //
        // TODO: 파일 이름을 {N}chapter_{M}stage.json으로 변경 //
        // 3) JSON 폴백 규칙: Resources/Stages/{N}stage.json
        // - 요구사항: 1stage.json~4stage.json
        string jsonKey = $"{(stageIndex + 1)}stage";
        if (_jsonStageCache.TryGetValue(jsonKey, out var cached) && cached != null)
            return cached;

        var textAsset = Resources.Load<TextAsset>($"Stages/{jsonKey}");
        if (textAsset == null)
        {
            Debug.LogError($"[Config] StageDefinition not found. json=Resources/Stages/{jsonKey}.json");
            return null;
        }

        var runtimeStage = StageJsonStageFactory.BuildOrNull(jsonKey, textAsset.text);
        if (runtimeStage == null)
        {
            Debug.LogError($"[Config] JSON stage build failed. key={jsonKey}");
            return null;
        }

        _jsonStageCache[jsonKey] = runtimeStage;
        return runtimeStage;
    }
}
