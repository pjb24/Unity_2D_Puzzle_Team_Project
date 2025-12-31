// SceneGateway.cs

using System;
using UnityEngine.SceneManagement;

public class SceneGateway : ISceneGateway
{
    private Action _pending;

    public void LoadBoot(Action onLoaded = null) => Load(E_Scene.Boot, onLoaded);
    public void LoadMainMenu(Action onLoaded = null) => Load(E_Scene.MainMenu, onLoaded);
    public void LoadGameplay(Action onLoaded = null) => Load(E_Scene.Gameplay, onLoaded);

    private void Load(E_Scene scene, Action onLoaded)
    {
        _pending = onLoaded;

        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;

        SceneLoader.Load(scene);    // LoadSceneMode.Single
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        var cb = _pending;
        _pending = null;
        cb?.Invoke();
    }
}
