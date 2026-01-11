// AudioHub.cs
///
/// BGM/SFX는 ID로만 재생.
/// SFX는 E_SfxId 단위 싱글 인스턴스: 같은 id 재생 시 이전 것 Stop 후 재생
/// - 재생 종료/중단 시 bookkeeping 정리
///
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class AudioHub : MonoBehaviour
{
    private struct ActiveSfx
    {
        public E_SfxId Id;
        public uint Serial;
        public AudioSource Source;
        public AudioSource SecondarySource;
        public float EndTime;
        public bool IsLoop;
        public bool UsesSeamlessLoop;
        public float BaseVolume;
        public float LoopCrossfade;
        public float ClipLength;
        public double CurrentStartDsp;
        public double NextStartDsp;
    }

    // StopSfx(token) 용도 (외부에서만 사용)
    public readonly struct SfxToken
    {
        public readonly E_SfxId Id;
        public readonly uint Serial;

        // OneShot: 실제 사용된 길이(피치 반영) / Loop: 0
        public readonly float DurationSeconds;

        public bool IsValid => Serial != 0;

        public SfxToken(E_SfxId id, uint serial, float durationSeconds)
        {
            Id = id;
            Serial = serial;
            DurationSeconds = durationSeconds;
        }

        public static SfxToken Invalid => new SfxToken(E_SfxId.None, 0, 0f);
    }

    private static AudioHub _instance;

    [Header("BGM")]
    [SerializeField] private AudioSource _bgm;

    [Header("BGM Library")]
    [SerializeField] private BgmLibrary _bgmLibrary;
    [Header("SFX Library")]
    [SerializeField] private SfxLibrary _sfxLibrary;

    [Header("SFX Pool")]
    [SerializeField] private bool _autoCreateSources = true;
    [SerializeField] private int _sfxPoolSize = 12;

    private readonly List<AudioSource> _sfxPool = new();
    private readonly List<ActiveSfx> _activeSfx = new();

    private readonly Dictionary<E_SfxId, float> _lastPlayedAt = new();
    private readonly Dictionary<E_SfxId, int> _activeVoicesById = new();

    // 싱글 인스턴스: 현재 재생 중인 id -> source
    private readonly Dictionary<E_SfxId, AudioSource> _playingById = new();

    // 토큰 기반 stop
    private uint _sfxSerialCounter = 1; // 0은 invalid 예약
    private readonly Dictionary<uint, AudioSource> _playingByToken = new();

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
        double dspNow = AudioSettings.dspTime;

        for (int i = _activeSfx.Count - 1; i >= 0; i--)
        {
            var a = _activeSfx[i];
            if (a.Source == null || (a.UsesSeamlessLoop && a.SecondarySource == null))
            {
                if (a.Source != null)
                {
                    a.Source.Stop();
                    a.Source.clip = null;
                }
                if (a.SecondarySource != null)
                {
                    a.SecondarySource.Stop();
                    a.SecondarySource.clip = null;
                }
                DecreaseVoiceCount(a.Id);
                CleanupPlayingByIdIfMatches(a.Id, null);
                CleanupPlayingByTokenIfMatches(a.Serial, null);
                _activeSfx.RemoveAt(i);
                continue;
            }

            if (a.UsesSeamlessLoop)
            {
                UpdateSeamlessLoop(ref a, dspNow);
                _activeSfx[i] = a;
                continue;
            }

            if (!a.Source.isPlaying || now >= a.EndTime)
            {
                a.Source.Stop();
                a.Source.clip = null;

                DecreaseVoiceCount(a.Id);
                CleanupPlayingByIdIfMatches(a.Id, a.Source);
                CleanupPlayingByTokenIfMatches(a.Serial, a.Source);

                _activeSfx.RemoveAt(i);
            }
        }
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

        PlayBgmClipIfChanged(def.Clip, def.Volume * Mathf.Clamp01(volumeScale));
    }

    public void StopBgm()
    {
        if (_bgm == null) return;
        _bgm.Stop();
        _bgm.clip = null;
    }

    // ===== SFX (ID 기반) =====
    public void PlaySfx(E_SfxId id, float volumeScale = 1f)
        => PlaySfxInternal(id, null, volumeScale, false);

    public void PlaySfxAt(E_SfxId id, Vector3 worldPos, float volumeScale = 1f)
        => PlaySfxInternal(id, worldPos, volumeScale, false);

    // ===== SFX TOKEN API =====
    public SfxToken PlaySfxOneShot(E_SfxId id, float volumeScale = 1f)
        => PlaySfxInternal(id, null, volumeScale, false);

    public SfxToken PlaySfxOneShotAt(E_SfxId id, Vector3 worldPos, float volumeScale = 1f)
        => PlaySfxInternal(id, worldPos, volumeScale, false);

    public SfxToken PlaySfxLoop(E_SfxId id, float volumeScale = 1f)
        => PlaySfxInternal(id, null, volumeScale, true);

    public SfxToken PlaySfxLoopAt(E_SfxId id, Vector3 worldPos, float volumeScale = 1f)
        => PlaySfxInternal(id, worldPos, volumeScale, true);

    public void StopSfx(SfxToken token)
    {
        if (!token.IsValid) return;

        if (!_playingByToken.TryGetValue(token.Serial, out var src) || src == null)
        {
            _playingByToken.Remove(token.Serial);
            return;
        }

        for (int i = _activeSfx.Count - 1; i >= 0; i--)
        {
            var a = _activeSfx[i];
            if (a.Serial != token.Serial) continue;

            if (a.Source != null)
            {
                a.Source.Stop();
                a.Source.clip = null;
            }
            if (a.UsesSeamlessLoop && a.SecondarySource != null)
            {
                a.SecondarySource.Stop();
                a.SecondarySource.clip = null;
            }

            DecreaseVoiceCount(a.Id);
            CleanupPlayingByIdIfMatches(a.Id, src);
            CleanupPlayingByTokenIfMatches(a.Serial, src);

            _activeSfx.RemoveAt(i);
            return;
        }

        // active 리스트에 없더라도 토큰 맵은 정리
        src.Stop();
        src.clip = null;
        CleanupPlayingByIdIfMatches(token.Id, src);
        CleanupPlayingByTokenIfMatches(token.Serial, src);
    }

    // ===== PRIVATE: CLIP API (외부 노출 금지) =====
    private void PlayBgmClipIfChanged(AudioClip clip, float volume)
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

        if (_bgm.clip == clip && _bgm.isPlaying)
            return;

        _bgm.Stop();
        _bgm.clip = clip;
        _bgm.volume = Mathf.Clamp01(volume);
        _bgm.Play();
    }

    private static bool IsRewindBlendSfx(E_SfxId id)
        => id == E_SfxId.Rewind_Enter || id == E_SfxId.Rewind_Loop || id == E_SfxId.Rewind_Exit;

    private SfxToken PlaySfxInternal(E_SfxId id, Vector3? worldPos, float volumeScale, bool loop)
    {
        if (_sfxLibrary == null)
        {
            Debug.LogWarning($"[AudioHub] SfxLibrary is null. id={id} (fallback: skip)");
            return SfxToken.Invalid;
        }

        if (!_sfxLibrary.TryGet(id, out var def))
        {
            Debug.LogWarning($"[AudioHub] SFX id not registered. id={id} (fallback: skip)");
            return SfxToken.Invalid;
        }

        if (def.Clip == null)
        {
            Debug.LogWarning($"[AudioHub] SFX clip is null. id={id} (fallback: skip)");
            return SfxToken.Invalid;
        }

        // 싱글 인스턴스: 같은 id면 이전 것 즉시 Stop
        StopPreviousSameIdIfPlaying(id);

        float now = Time.unscaledTime;

        // cooldown
        if (def.CooldownSeconds > 0f &&
            _lastPlayedAt.TryGetValue(id, out float lastAt) &&
            now - lastAt < def.CooldownSeconds)
        {
            return SfxToken.Invalid;
        }

        // max voices (싱글 인스턴스면 사실상 1이지만 방어)
        if (def.MaxVoices > 0 &&
            _activeVoicesById.TryGetValue(id, out int voices) &&
            voices >= def.MaxVoices)
        {
            return SfxToken.Invalid;
        }

        var src = GetFreeSfxSource();
        if (src == null)
        {
            Debug.LogWarning($"[AudioHub] SFX pool exhausted. id={id} pool={_sfxPool.Count} (fallback: skip)");
            return SfxToken.Invalid;
        }

        float pitch = Random.Range(def.PitchMin, def.PitchMax);
        if (Mathf.Approximately(pitch, 0f)) pitch = 1f;

        // Rewind Enter/Loop/Exit는 시퀀스 연결 품질이 중요해서 랜덤 피치를 금지한다.
        if (IsRewindBlendSfx(id)) pitch = 1f;

        src.spatialBlend = def.SpatialBlend;
        src.pitch = pitch;
        float baseVolume = Mathf.Clamp01(def.Volume * Mathf.Clamp01(volumeScale));
        src.volume = baseVolume;
        src.transform.position = worldPos ?? Vector3.zero;

        bool useSeamlessLoop = loop && def.LoopCrossfadeSeconds > 0f;
        AudioSource secondarySrc = null;
        float clipLength = def.Clip.length / Mathf.Max(0.01f, Mathf.Abs(pitch));
        float loopCrossfade = useSeamlessLoop ? Mathf.Clamp(def.LoopCrossfadeSeconds, 0f, clipLength * 0.5f) : 0f;

        if (useSeamlessLoop)
        {
            secondarySrc = GetFreeSfxSource(src);
            if (secondarySrc == null)
            {
                useSeamlessLoop = false;
            }
        }

        src.loop = loop && !useSeamlessLoop;
        if (secondarySrc != null)
        {
            secondarySrc.spatialBlend = def.SpatialBlend;
            secondarySrc.pitch = pitch;
            secondarySrc.volume = 0f;
            secondarySrc.loop = false;
            secondarySrc.transform.position = worldPos ?? Vector3.zero;
        }

        src.clip = def.Clip;

        if (useSeamlessLoop && secondarySrc != null)
        {
            secondarySrc.clip = def.Clip;
            double startDsp = AudioSettings.dspTime;
            double nextStart = startDsp + clipLength - loopCrossfade;

            src.PlayScheduled(startDsp);
            secondarySrc.PlayScheduled(nextStart);

            _activeSfx.Add(new ActiveSfx
            {
                Id = id,
                Serial = 0, // placeholder, overwritten below
                Source = src,
                SecondarySource = secondarySrc,
                EndTime = float.PositiveInfinity,
                IsLoop = true,
                UsesSeamlessLoop = true,
                BaseVolume = baseVolume,
                LoopCrossfade = loopCrossfade,
                ClipLength = clipLength,
                CurrentStartDsp = startDsp,
                NextStartDsp = nextStart
            });
        }
        else
        {
            src.Play();
        }

        uint serial = NextSfxSerial();
        _playingByToken[serial] = src;

        _lastPlayedAt[id] = now;
        IncreaseVoiceCount(id);

        _playingById[id] = src;

        float duration = loop ? 0f : clipLength;

        if (useSeamlessLoop && secondarySrc != null)
        {
            var last = _activeSfx.Count - 1;
            var seamless = _activeSfx[last];
            seamless.Serial = serial;
            _activeSfx[last] = seamless;
        }
        else
        {
            _activeSfx.Add(new ActiveSfx
            {
                Id = id,
                Serial = serial,
                Source = src,
                EndTime = loop ? float.PositiveInfinity : now + clipLength,
                IsLoop = loop,
                UsesSeamlessLoop = false,
                BaseVolume = baseVolume,
                LoopCrossfade = 0f,
                ClipLength = clipLength,
                CurrentStartDsp = 0,
                NextStartDsp = 0,
                SecondarySource = null
            });
        }

        return new SfxToken(id, serial, duration);
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
            if (a.UsesSeamlessLoop && a.SecondarySource != null)
            {
                a.SecondarySource.Stop();
                a.SecondarySource.clip = null;
            }

            DecreaseVoiceCount(a.Id);
            CleanupPlayingByTokenIfMatches(a.Serial, a.Source);
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

    private uint NextSfxSerial()
    {
        uint s = _sfxSerialCounter++;
        if (_sfxSerialCounter == 0) _sfxSerialCounter = 1; // overflow 대비
        if (s == 0) s = _sfxSerialCounter++;
        return s;
    }

    private void CleanupPlayingByTokenIfMatches(uint serial, AudioSource src)
    {
        if (serial == 0) return;

        if (_playingByToken.TryGetValue(serial, out var cur))
        {
            if (cur == null || cur == src)
                _playingByToken.Remove(serial);
        }
        else
        {
            if (src == null)
                _playingByToken.Remove(serial);
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
    }

    private void EnsureLibraries()
    {
        if (_bgmLibrary == null)
        {
            Debug.LogWarning("[AudioHub] BgmLibrary is null (fallback: BGM id play disabled)");
        }

        if (_sfxLibrary == null)
        {
            Debug.LogWarning("[AudioHub] SfxLibrary is null (fallback: SFX id play disabled)");
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

            _sfxPool.Add(src);
        }
    }

    private AudioSource GetFreeSfxSource()
    {
        for (int i = 0; i < _sfxPool.Count; i++)
        {
            var s = _sfxPool[i];
            if (s == null) continue;
            if (s.isPlaying) continue;
            if (IsSourceReserved(s)) continue;
            return s;
        }
        return null;
    }

    private AudioSource GetFreeSfxSource(AudioSource exclude)
    {
        if (exclude == null) return GetFreeSfxSource();

        for (int i = 0; i < _sfxPool.Count; i++)
        {
            var s = _sfxPool[i];
            if (s == null || s == exclude) continue;
            if (s.isPlaying) continue;
            if (IsSourceReserved(s)) continue;
            return s;
        }

        return null;
    }

    private bool IsSourceReserved(AudioSource source)
    {
        for (int i = 0; i < _activeSfx.Count; i++)
        {
            var a = _activeSfx[i];
            if (a.Source == source || a.SecondarySource == source)
                return true;
        }

        return false;
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

    private void UpdateSeamlessLoop(ref ActiveSfx a, double dspNow)
    {
        if (a.LoopCrossfade > 0f)
        {
            double fadeStart = a.CurrentStartDsp + a.ClipLength - a.LoopCrossfade;
            if (dspNow >= fadeStart)
            {
                float t = (float)((dspNow - fadeStart) / a.LoopCrossfade);
                t = Mathf.Clamp01(t);
                a.Source.volume = a.BaseVolume * (1f - t);
                a.SecondarySource.volume = a.BaseVolume * t;
            }
            else
            {
                a.Source.volume = a.BaseVolume;
                a.SecondarySource.volume = 0f;
            }
        }

        double currentEnd = a.CurrentStartDsp + a.ClipLength;
        if (dspNow >= currentEnd)
        {
            var oldCurrent = a.Source;
            a.Source = a.SecondarySource;
            a.CurrentStartDsp = a.NextStartDsp;
            a.SecondarySource = oldCurrent;

            _playingById[a.Id] = a.Source;

            a.SecondarySource.Stop();
            a.SecondarySource.clip = a.Source.clip;
            a.SecondarySource.volume = 0f;

            a.NextStartDsp = a.CurrentStartDsp + a.ClipLength - a.LoopCrossfade;
            a.SecondarySource.PlayScheduled(a.NextStartDsp);
        }
    }
}
