// SceneMap.cs
using System;

public enum E_Scene
{
    Boot,
    MainMenu,
    Gameplay,
}

public static class SceneMap
{
    public static string Get(E_Scene scene)
    {
        return scene switch
        {
            E_Scene.Boot => "Boot",
            E_Scene.MainMenu => "MainMenu",
            E_Scene.Gameplay => "Gameplay",
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}
