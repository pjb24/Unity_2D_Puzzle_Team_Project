// MainMenuBgmProfile.cs
using UnityEngine;

[CreateAssetMenu(menuName = "Puzzle/Audio/MainMenu BGM Profile")]
public class MainMenuBgmProfile : ScriptableObject
{
    [SerializeField] private AudioClip _bgm;

    public AudioClip Bgm => _bgm;
}
