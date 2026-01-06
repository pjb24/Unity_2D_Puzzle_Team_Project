// AudioHub.cs
using UnityEngine;

[DisallowMultipleComponent]
public class AudioHub : MonoBehaviour
{
    private static AudioHub _instance;

    [Header("Sources")]
    [SerializeField] private AudioSource _bgm;
    [SerializeField] private AudioSource _sfx;

    [Header("Auto Create Sources")]
    [SerializeField] private bool _autoCreateSources = true;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Debug.LogWarning("[AudioHub] Duplicate instance detected. Destroying new one (fallback).");
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureSources();
    }

    private void EnsureSources()
    {
        if (!_autoCreateSources) return;

        if (_bgm == null)
        {
            var go = new GameObject("BGM");
            go.transform.SetParent(transform, false);

            _bgm = go.AddComponent<AudioSource>();
            _bgm.loop = true;
            _bgm.playOnAwake = false;
        }

        if (_sfx == null)
        {
            var go = new GameObject("SFX");
            go.transform.SetParent(transform, false);

            _sfx = go.AddComponent<AudioSource>();
            _sfx.loop = false;
            _sfx.playOnAwake = false;
        }
    }

    public static AudioHub Ensure()
    {
        if (_instance != null) return _instance;

        var found = FindFirstObjectByType<AudioHub>();
        if (found != null)
        {
            _instance = found;
            _instance.EnsureSources();
            return _instance;
        }

        var root = new GameObject("AudioHub");
        var hub = root.AddComponent<AudioHub>();
        // Awake에서 DontDestroyOnLoad + Source 생성됨
        return hub;
    }

    // 기존 호출 호환용
    public void PlayBgm(AudioClip clip, float volume = 1f)
        => PlayBgmIfChanged(clip, volume);

    // 요구사항: 파일(clip)이 다르면 새로 시작, 같으면 유지
    public void PlayBgmIfChanged(AudioClip clip, float volume = 1f)
    {
        if (_bgm == null)
        {
            Debug.LogError("[AudioHub] BGM source is null. Cannot play BGM.");
            return;
        }

        if (clip == null)
        {
            Debug.LogWarning("[AudioHub] PlayBgmIfChanged skipped: clip is null. Keep current BGM (fallback).");
            return;
        }

        // 같은 clip이면 유지
        if (_bgm.clip == clip && _bgm.isPlaying)
            return;

        _bgm.Stop();
        _bgm.clip = clip;
        _bgm.volume = Mathf.Clamp01(volume);
        _bgm.Play();
    }

    public void StopBgm()
    {
        if (_bgm == null) return;
        _bgm.Stop();
        _bgm.clip = null;
    }

    public void PlaySfx(AudioClip clip, float volumeScale = 1f)
    {
        if (_sfx == null) return;

        if (clip == null)
        {
            Debug.LogWarning("[AudioHub] PlaySfx skipped: clip is null (fallback).");
            return;
        }

        _sfx.PlayOneShot(clip);
    }
}
