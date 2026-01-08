// AudioConfig.cs
using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(menuName = "Game/Audio/AudioConfig", fileName = "AudioConfig")]
public class AudioConfig : ScriptableObject
{
    [Header("Libraries")]
    public SfxLibrary SfxLibrary;
    public BgmLibrary BgmLibrary;

    [Header("Mixer")]
    public AudioMixerGroup DefaultSfxMixerGroup;
    public AudioMixerGroup BgmMixerGroup;

    [Header("Pool")]
    [Min(1)] public int SfxPoolSize = 12;

    [Header("Resources Fallback (optional)")]
    public string SfxLibraryResourcesPath = "Configs/SfxLibrary";
    public string BgmLibraryResourcesPath = "Configs/BgmLibrary";
}
