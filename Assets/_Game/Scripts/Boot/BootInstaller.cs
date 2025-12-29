using UnityEngine;

[DisallowMultipleComponent]
public class BootInstaller : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private E_Scene _nextScene = E_Scene.MainMenu;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);

        // TODO: 이후 GameConfig / Save / Service 초기화 지점
        SceneLoader.Load(_nextScene);
    }
}
