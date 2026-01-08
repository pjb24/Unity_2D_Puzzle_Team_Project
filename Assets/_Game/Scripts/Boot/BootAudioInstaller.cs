// BootAudioInstaller.cs
using UnityEngine;

[DisallowMultipleComponent]
public class BootAudioInstaller : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private AudioConfig _config;

    [Tooltip("Optional fallback load path under Resources. Example: Configs/AudioConfig")]
    [SerializeField] private string _configResourcesPath = "Configs/AudioConfig";

    private void Awake()
    {
        var hub = AudioHub.Ensure();

        if (_config == null)
        {
            if (!string.IsNullOrEmpty(_configResourcesPath))
            {
                _config = Resources.Load<AudioConfig>(_configResourcesPath);
                if (_config == null)
                    Debug.LogWarning($"[BootAudioInstaller] AudioConfig not found at Resources/{_configResourcesPath}.asset (fallback: use AudioHub defaults)");
            }
            else
            {
                Debug.LogWarning("[BootAudioInstaller] AudioConfig is null and resources path is empty (fallback: use AudioHub defaults)");
            }
        }

        if (_config != null)
            hub.ApplyConfig(_config);
    }
}
