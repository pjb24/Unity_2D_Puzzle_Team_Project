// ISceneGateway.cs
using System;

public interface ISceneGateway
{
    void LoadBoot(Action onLoaded = null);
    void LoadMainMenu(Action onLoaded = null);
    void LoadGameplay(Action onLoaded = null);
}
