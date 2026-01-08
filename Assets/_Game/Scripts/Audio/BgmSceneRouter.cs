// BgmSceneRouter.cs
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class BgmSceneRouter : MonoBehaviour
{
    private static BgmSceneRouter _instance;

    [Header("MainMenu BGM (ID Only)")]
    [SerializeField] private E_BgmId _mainMenuBgmId = E_BgmId.MainMenu;

    public static BgmSceneRouter Ensure()
    {
        if (_instance != null) return _instance;

        var found = FindFirstObjectByType<BgmSceneRouter>();
        if (found != null)
        {
            _instance = found;
            return _instance;
        }

        var go = new GameObject(nameof(BgmSceneRouter));
        _instance = go.AddComponent<BgmSceneRouter>();
        return _instance;
    }

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

        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (_instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // MainMenu 씬에서부터 시작
        if (scene.name != SceneMap.Get(E_Scene.MainMenu))
            return;

        if (_mainMenuBgmId == E_BgmId.None)
        {
            Debug.LogWarning("[BgmSceneRouter] MainMenuBgmId is None. Play skipped.");
            return;
        }

        // AudioHub가 내부에서 BgmLibrary로 조회 후 재생한다.
        // (BgmSceneRouter가 BgmLibrary를 직접 만지지 않는다 = 책임 분리)
        AudioHub.Ensure().PlayBgmIfChanged(_mainMenuBgmId);
    }
}
