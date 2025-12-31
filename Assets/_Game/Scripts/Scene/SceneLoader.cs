// SceneLoader.cs

using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneLoader
{
    public static void Load(E_Scene scene)
    {
        string sceneName = SceneMap.Get(scene);

        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("[SceneLoader] sceneName is null or empty");
            return;
        }

        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }
}
