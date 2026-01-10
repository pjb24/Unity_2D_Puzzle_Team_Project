// GameConfig.cs
using System.Collections.Generic;
using UnityEngine;

public enum E_Difficulty
{
    Easy,
    Normal,
    Hard,
}

[System.Serializable]
public class DifficultyProfile
{
    public E_Difficulty _difficulty;

    [Header("Rewind")]
    public int RewindMax = 3;

    [Header("Fail Policy")]
    public bool FailOnChildBlocked = true; // Easy는 false로
    public bool HardResetStage = false;    // Hard는 true로

    [Header("Normal - FailStreak Return ChapterStart")]
    [SerializeField, Min(0)] private int _failStreakToReturnChapterStart = 0;
    public int FailStreakToReturnChapterStart => _failStreakToReturnChapterStart;

    [Header("Tuning")]
    public float ChildStepDelay = 0.0f;    // 연출/턴 템포
}

[System.Serializable]
public class StageDefinitionFiles
{
    public TextAsset _stageAsset;
    public E_BgmId BgmId;
}

[System.Serializable]
public class ChapterDefinition
{
    public string ChapterId = "Chapter_01";
    public E_BgmId BgmId = E_BgmId.Chapter_01;
    public List<StageDefinitionFiles> Stages = new();
}

[CreateAssetMenu(menuName = "Puzzle/Data/Game Config")]
public class GameConfig : ScriptableObject
{
    [Header("Chapters")]
    [SerializeField] private List<ChapterDefinition> _chapters = new();

    [Header("Difficulty Profiles")]
    [SerializeField] private List<DifficultyProfile> _difficultyProfiles = new();

    [Header("Default Params")]
    [SerializeField] private E_Difficulty _defaultDifficulty = E_Difficulty.Normal;

    [Header("Hard - Ironman Option")]
    [SerializeField] private bool _ironmanHardReturnToChapterStart = false;
    public bool IronmanHardReturnToChapterStart => _ironmanHardReturnToChapterStart;

    [Header("Stage Prefabs")]
    [SerializeField] private StagePrefabs _prefabs;

    public IReadOnlyList<ChapterDefinition> Chapters => _chapters;
    public E_Difficulty DefaultDifficulty => _defaultDifficulty;
    public StagePrefabs Prefabs => _prefabs;

    public DifficultyProfile GetProfile(E_Difficulty difficulty)
    {
        for (int i = 0; i < _difficultyProfiles.Count; i++)
        {
            if (_difficultyProfiles[i] != null && _difficultyProfiles[i]._difficulty == difficulty)
                return _difficultyProfiles[i];
        }

        Debug.LogWarning($"[GameConfig] DifficultyProfile not found for {difficulty} (fallback).");
        return null;
    }

    private void OnValidate()
    {
        if (_chapters == null) _chapters = new List<ChapterDefinition>();
        if (_difficultyProfiles == null) _difficultyProfiles = new List<DifficultyProfile>();

        // 챕터/스테이지 null 검증
        for (int c = 0; c < _chapters.Count; c++)
        {
            var ch = _chapters[c];
            if (ch == null) continue;

            if (ch.Stages == null) ch.Stages = new List<StageDefinitionFiles>();
            for (int s = ch.Stages.Count - 1; s >= 0; s--)
            {
                if (ch.Stages[s] == null)
                    Debug.LogWarning($"[GameConfig] Null stage in {ch.ChapterId}", this);
            }
        }

        // 난이도 프로필 중복 방지(최소)
        for (int i = 0; i < _difficultyProfiles.Count; i++)
        {
            for (int j = i + 1; j < _difficultyProfiles.Count; j++)
            {
                if (_difficultyProfiles[i] != null && _difficultyProfiles[j] != null &&
                    _difficultyProfiles[i]._difficulty == _difficultyProfiles[j]._difficulty)
                {
                    Debug.LogWarning($"[GameConfig] Duplicate difficulty profile: {_difficultyProfiles[i]._difficulty}", this);
                }
            }
        }
    }
}
