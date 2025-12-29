using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class MainMenuController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Button _btnStart;

    [Header("Config")]
    [SerializeField] private E_Scene _gameplayScene = E_Scene.Gameplay;

    private void Awake()
    {
        if (_btnStart != null)
            _btnStart.onClick.AddListener(OnClickStart);
    }

    private void OnDestroy()
    {
        if (_btnStart != null)
            _btnStart.onClick.RemoveListener(OnClickStart);
    }

    private void OnClickStart()
    {
        SceneLoader.Load(_gameplayScene);
    }
}
