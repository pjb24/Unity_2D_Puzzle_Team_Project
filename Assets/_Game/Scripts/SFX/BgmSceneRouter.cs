// BgmSceneRouter.cs
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class BgmSceneRouter : MonoBehaviour
{
    private static BgmSceneRouter _instance;

    private const string _gameConfigPath = "Configs/GameConfig";               // Resources/Configs/GameConfig.asset
    private const string _mainMenuBgmProfilePath = "Configs/MainMenuBgmProfile"; // Resources/Configs/MainMenuBgmProfile.asset

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Debug.LogWarning("[BgmSceneRouter] Duplicate instance detected. Destroying new one (fallback).");
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (_instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public static BgmSceneRouter Ensure()
    {
        if (_instance != null) return _instance;

        var found = FindFirstObjectByType<BgmSceneRouter>();
        if (found != null)
        {
            _instance = found;
            return _instance;
        }

        var go = new GameObject("BgmSceneRouter");
        return go.AddComponent<BgmSceneRouter>();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // MainMenu 씬에서부터 시작
        if (scene.name != SceneMap.Get(E_Scene.MainMenu))
            return;

        var hub = AudioHub.Ensure();
        var clip = ResolveMainMenuBgm();

        if (clip == null)
        {
            Debug.LogWarning("[BgmSceneRouter] MainMenu BGM clip is null. BGM not started (fallback).");
            return;
        }

        hub.PlayBgmIfChanged(clip);
    }

    private AudioClip ResolveMainMenuBgm()
    {
        // 1) 전용 프로필 우선
        var profile = Resources.Load<MainMenuBgmProfile>(_mainMenuBgmProfilePath);
        if (profile != null && profile.Bgm != null)
            return profile.Bgm;

        // 2) 없으면 GameConfig의 1챕터 BGM으로 폴백
        var cfg = Resources.Load<GameConfig>(_gameConfigPath);
        if (cfg == null)
        {
            Debug.LogWarning($"[BgmSceneRouter] GameConfig not found at Resources/{_gameConfigPath}.asset (fallback).");
            return null;
        }

        if (cfg.Chapters == null || cfg.Chapters.Count <= 0)
        {
            Debug.LogWarning("[BgmSceneRouter] GameConfig.Chapters is empty. Cannot resolve fallback BGM.");
            return null;
        }

        var ch0 = cfg.Chapters[0];
        var vp = ch0 != null ? ch0.VisualProfile : null;
        if (vp == null || vp.Bgm == null)
        {
            Debug.LogWarning("[BgmSceneRouter] Chapter[0] VisualProfile/Bgm is null. Cannot resolve fallback BGM.");
            return null;
        }

        return vp.Bgm;
    }
}
