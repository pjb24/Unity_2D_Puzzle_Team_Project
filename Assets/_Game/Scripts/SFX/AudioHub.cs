// AudioHub.cs
using UnityEngine;

[DisallowMultipleComponent]
public class AudioHub : MonoBehaviour
{
    [Header("Sources")]
    [SerializeField] private AudioSource _bgm;
    [SerializeField] private AudioSource _sfx;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);

        if (_bgm == null)
        {
            Debug.LogWarning("[AudioHub] _bgm is null. Created AudioSource at runtime (fallback).");
            _bgm = gameObject.AddComponent<AudioSource>();
            _bgm.loop = true;
            _bgm.playOnAwake = false;
        }

        if (_sfx == null)
        {
            Debug.LogWarning("[AudioHub] _sfx is null. Created AudioSource at runtime (fallback).");
            _sfx = gameObject.AddComponent<AudioSource>();
            _sfx.loop = false;
            _sfx.playOnAwake = false;
        }
    }

    public void PlayBgm(AudioClip clip)
    {
        if (_bgm == null) return;

        if (clip == null)
        {
            Debug.LogWarning("[AudioHub] PlayBgm skipped: clip is null (fallback).");
            return;
        }

        if (_bgm.clip == clip && _bgm.isPlaying)
            return;

        _bgm.clip = clip;
        _bgm.loop = true;
        _bgm.Play();
    }

    public void StopBgm()
    {
        if (_bgm == null) return;
        _bgm.Stop();
        _bgm.clip = null;
    }

    public void PlaySfx(AudioClip clip)
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
