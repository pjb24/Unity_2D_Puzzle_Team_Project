// BootInstaller.cs
using UnityEngine;

[DisallowMultipleComponent]
public class BootInstaller : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private E_Scene _nextScene = E_Scene.MainMenu;

    private void Awake()
    {
        // BGM이 MainMenu 씬 로드 직후부터 바로 시작되도록 사전 준비
        AudioHub.Ensure();
        BgmSceneRouter.Ensure();

        // TODO: 이후 GameConfig / Save / Service 초기화 지점
        SceneLoader.Load(_nextScene);
    }
}
