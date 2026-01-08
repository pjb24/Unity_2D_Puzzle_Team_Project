// AudioHub.cs
///
/// BGM/SFX는 ID로만 재생.
/// SFX는 E_SfxId 단위 싱글 인스턴스: 같은 id 재생 시 이전 것 Stop 후 재생
/// - 재생 종료/중단 시 bookkeeping 정리
///
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

[DisallowMultipleComponent]
public class AudioHub : MonoBehaviour
{
    private struct ActiveSfx
    {
        public E_SfxId Id;
        public AudioSource Source;
        public float EndTime;
    }

    private static AudioHub _instance;

    [Header("BGM")]
    [SerializeField] private AudioSource _bgm;

    [Header("BGM Library")]
    [SerializeField] private BgmLibrary _bgmLibrary;
    [Header("SFX Library")]
    [SerializeField] private SfxLibrary _sfxLibrary;

    [Tooltip("Optional fallback load path under Resources. Example: Configs/SfxLibrary")]
    [SerializeField] private string _defaultSfxLibraryResourcesPath = "Configs/SfxLibrary";
    [SerializeField] private string _defaultBgmLibraryResourcesPath = "Configs/BgmLibrary";

    [Header("SFX Output")]
    [SerializeField] private AudioMixerGroup _defaultSfxMixerGroup;
    [SerializeField] private AudioMixerGroup _bgmMixerGroup;

    [Header("SFX Pool")]
    [SerializeField] private bool _autoCreateSources = true;
    [SerializeField] private int _sfxPoolSize = 12;

    private readonly List<AudioSource> _sfxPool = new();
    private readonly List<ActiveSfx> _activeSfx = new();

    private readonly Dictionary<E_SfxId, float> _lastPlayedAt = new();
    private readonly Dictionary<E_SfxId, int> _activeVoicesById = new();

    // 싱글 인스턴스: 현재 재생 중인 id -> source
    private readonly Dictionary<E_SfxId, AudioSource> _playingById = new();

    public static AudioHub Ensure()
    {
        if (_instance != null) return _instance;

        var found = FindFirstObjectByType<AudioHub>();
        if (found != null)
        {
            _instance = found;
            _instance.BootstrapIfNeeded();
            return _instance;
        }

        var root = new GameObject("AudioHub");
        _instance = root.AddComponent<AudioHub>();
        _instance.BootstrapIfNeeded();
        return _instance;
    }

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
        
        BootstrapIfNeeded();
    }

    private void BootstrapIfNeeded()
    {
        EnsureBgmSource();
        EnsureLibraries();
        EnsureSfxPool();
    }

    private void Update()
    {
        float now = Time.unscaledTime;

        for (int i = _activeSfx.Count - 1; i >= 0; i--)
        {
            var a = _activeSfx[i];
            if (a.Source == null)
            {
                DecreaseVoiceCount(a.Id);
                CleanupPlayingByIdIfMatches(a.Id, null);
                _activeSfx.RemoveAt(i);
                continue;
            }

            if (!a.Source.isPlaying || now >= a.EndTime)
            {
                a.Source.Stop();
                a.Source.clip = null;

                DecreaseVoiceCount(a.Id);
                CleanupPlayingByIdIfMatches(a.Id, a.Source);

                _activeSfx.RemoveAt(i);
            }
        }
    }

    // ===== 씬/초기화 연결용 API =====
    public void ApplyConfig(AudioConfig cfg)
    {
        if (cfg == null)
        {
            Debug.LogWarning("[AudioHub] ApplyConfig fallback: config is null.");
            return;
        }

        if (cfg.SfxLibrary != null) _sfxLibrary = cfg.SfxLibrary;
        if (cfg.BgmLibrary != null) _bgmLibrary = cfg.BgmLibrary;

        if (!string.IsNullOrEmpty(cfg.SfxLibraryResourcesPath)) _defaultSfxLibraryResourcesPath = cfg.SfxLibraryResourcesPath;
        if (!string.IsNullOrEmpty(cfg.BgmLibraryResourcesPath)) _defaultBgmLibraryResourcesPath = cfg.BgmLibraryResourcesPath;

        if (cfg.DefaultSfxMixerGroup != null) _defaultSfxMixerGroup = cfg.DefaultSfxMixerGroup;
        if (cfg.BgmMixerGroup != null) _bgmMixerGroup = cfg.BgmMixerGroup;

        _sfxPoolSize = Mathf.Max(1, cfg.SfxPoolSize);

        if (_bgm != null && _bgmMixerGroup != null)
            _bgm.outputAudioMixerGroup = _bgmMixerGroup;

        EnsureLibraries();
        EnsureSfxPool();
    }

    // ===== BGM ID 기반 =====
    public void PlayBgmIfChanged(E_BgmId id, float volumeScale = 1f)
    {
        if (_bgmLibrary == null)
        {
            Debug.LogWarning($"[AudioHub] BgmLibrary is null. id={id} (fallback: skip)");
            return;
        }

        if (!_bgmLibrary.TryGet(id, out var def))
        {
            Debug.LogWarning($"[AudioHub] BGM id not registered in library. id={id} (fallback: skip)");
            return;
        }

        if (def.Clip == null)
        {
            Debug.LogWarning($"[AudioHub] BGM clip is null. id={id} (fallback: skip)");
            return;
        }

        PlayBgmClipIfChanged(def.Clip, def.Volume * Mathf.Clamp01(volumeScale), def.MixerGroupOverride);
    }

    public void StopBgm()
    {
        if (_bgm == null) return;
        _bgm.Stop();
        _bgm.clip = null;
    }

    // ===== SFX (ID 기반) =====
    public void PlaySfx(E_SfxId id, float volumeScale = 1f)
        => PlaySfxInternal(id, null, volumeScale);

    public void PlaySfxAt(E_SfxId id, Vector3 worldPos, float volumeScale = 1f)
        => PlaySfxInternal(id, worldPos, volumeScale);

    // ===== PRIVATE: CLIP API (외부 노출 금지) =====
    private void PlayBgmClipIfChanged(AudioClip clip, float volume, AudioMixerGroup mixerOverride)
    {
        if (_bgm == null)
        {
            Debug.LogError("[AudioHub] BGM source is null. Cannot play BGM.");
            return;
        }

        if (clip == null)
        {
            Debug.LogWarning("[AudioHub] PlayBgmClipIfChanged skipped: clip is null (fallback).");
            return;
        }

        _bgm.outputAudioMixerGroup = mixerOverride != null ? mixerOverride : _bgmMixerGroup;

        if (_bgm.clip == clip && _bgm.isPlaying)
            return;

        _bgm.Stop();
        _bgm.clip = clip;
        _bgm.volume = Mathf.Clamp01(volume);
        _bgm.Play();
    }

    private void PlaySfxInternal(E_SfxId id, Vector3? worldPos, float volumeScale)
    {
        if (_sfxLibrary == null)
        {
            Debug.LogWarning($"[AudioHub] SfxLibrary is null. id={id} (fallback: skip)");
            return;
        }

        if (!_sfxLibrary.TryGet(id, out var def))
        {
            Debug.LogWarning($"[AudioHub] SFX id not registered. id={id} (fallback: skip)");
            return;
        }

        if (def.Clip == null)
        {
            Debug.LogWarning($"[AudioHub] SFX clip is null. id={id} (fallback: skip)");
            return;
        }

        // 싱글 인스턴스: 같은 id면 이전 것 즉시 Stop
        StopPreviousSameIdIfPlaying(id);

        float now = Time.unscaledTime;

        // cooldown
        if (def.CooldownSeconds > 0f &&
            _lastPlayedAt.TryGetValue(id, out float lastAt) &&
            now - lastAt < def.CooldownSeconds)
        {
            return;
        }

        // max voices (싱글 인스턴스면 사실상 1이지만 방어)
        if (def.MaxVoices > 0 &&
            _activeVoicesById.TryGetValue(id, out int voices) &&
            voices >= def.MaxVoices)
        {
            return;
        }

        var src = GetFreeSfxSource();
        if (src == null)
        {
            Debug.LogWarning($"[AudioHub] SFX pool exhausted. id={id} pool={_sfxPool.Count} (fallback: skip)");
            return;
        }

        float pitch = Random.Range(def.PitchMin, def.PitchMax);
        if (Mathf.Approximately(pitch, 0f)) pitch = 1f;

        src.outputAudioMixerGroup = def.MixerGroupOverride != null ? def.MixerGroupOverride : _defaultSfxMixerGroup;
        src.spatialBlend = def.SpatialBlend;
        src.pitch = pitch;
        src.volume = Mathf.Clamp01(def.Volume * Mathf.Clamp01(volumeScale));
        src.loop = false;
        src.transform.position = worldPos ?? Vector3.zero;

        src.clip = def.Clip;
        src.Play();

        _lastPlayedAt[id] = now;
        IncreaseVoiceCount(id);

        _playingById[id] = src;

        float length = def.Clip.length / Mathf.Max(0.01f, Mathf.Abs(pitch));

        _activeSfx.Add(new ActiveSfx
        {
            Id = id,
            Source = src,
            EndTime = now + length
        });
    }

    private void StopPreviousSameIdIfPlaying(E_SfxId id)
    {
        if (!_playingById.TryGetValue(id, out var prev) || prev == null)
            return;

        if (!prev.isPlaying)
        {
            _playingById.Remove(id);
            return;
        }

        for (int i = _activeSfx.Count - 1; i >= 0; i--)
        {
            var a = _activeSfx[i];
            if (a.Source != prev) continue;

            prev.Stop();
            prev.clip = null;

            DecreaseVoiceCount(a.Id);
            _activeSfx.RemoveAt(i);
            break;
        }

        _playingById.Remove(id);
    }

    private void CleanupPlayingByIdIfMatches(E_SfxId id, AudioSource src)
    {
        if (_playingById.TryGetValue(id, out var cur))
        {
            if (cur == null || cur == src)
                _playingById.Remove(id);
        }
    }

    private void EnsureBgmSource()
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

        if (_bgmMixerGroup != null)
            _bgm.outputAudioMixerGroup = _bgmMixerGroup;
    }

    private void EnsureLibraries()
    {
        if (_bgmLibrary == null)
        {
            if (!string.IsNullOrEmpty(_defaultBgmLibraryResourcesPath))
            {
                _bgmLibrary = Resources.Load<BgmLibrary>(_defaultBgmLibraryResourcesPath);
                if (_bgmLibrary == null)
                    Debug.LogWarning($"[AudioHub] BgmLibrary not found at Resources/{_defaultBgmLibraryResourcesPath}.asset (fallback: BGM id play disabled)");
            }
            else
            {
                Debug.LogWarning("[AudioHub] BgmLibrary is null and default path is empty (fallback: BGM id play disabled)");
            }
        }

        if (_sfxLibrary == null)
        {
            if (!string.IsNullOrEmpty(_defaultSfxLibraryResourcesPath))
            {
                _sfxLibrary = Resources.Load<SfxLibrary>(_defaultSfxLibraryResourcesPath);
                if (_sfxLibrary == null)
                    Debug.LogWarning($"[AudioHub] SfxLibrary not found at Resources/{_defaultSfxLibraryResourcesPath}.asset (fallback: SFX id play disabled)");
            }
            else
            {
                Debug.LogWarning("[AudioHub] SfxLibrary is null and default path is empty (fallback: SFX id play disabled)");
            }
        }
    }

    private void EnsureSfxPool()
    {
        if (!_autoCreateSources) return;

        _sfxPoolSize = Mathf.Max(1, _sfxPoolSize);

        // already built
        if (_sfxPool.Count >= _sfxPoolSize) return;

        var poolRoot = transform.Find("SFX_Pool");
        if (poolRoot == null)
        {
            var go = new GameObject("SFX_Pool");
            go.transform.SetParent(transform, false);
            poolRoot = go.transform;
        }

        while (_sfxPool.Count < _sfxPoolSize)
        {
            var go = new GameObject($"SFX_{_sfxPool.Count:00}");
            go.transform.SetParent(poolRoot, false);

            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = false;
            src.spatialBlend = 0f;
            src.outputAudioMixerGroup = _defaultSfxMixerGroup;

            _sfxPool.Add(src);
        }
    }

    private AudioSource GetFreeSfxSource()
    {
        for (int i = 0; i < _sfxPool.Count; i++)
        {
            var s = _sfxPool[i];
            if (s == null) continue;
            if (!s.isPlaying) return s;
        }
        return null;
    }

    private void IncreaseVoiceCount(E_SfxId id)
    {
        if (_activeVoicesById.TryGetValue(id, out int v))
            _activeVoicesById[id] = v + 1;
        else
            _activeVoicesById[id] = 1;
    }

    private void DecreaseVoiceCount(E_SfxId id)
    {
        if (!_activeVoicesById.TryGetValue(id, out int v)) return;

        v = Mathf.Max(0, v - 1);
        if (v == 0) _activeVoicesById.Remove(id);
        else _activeVoicesById[id] = v;
    }
}
