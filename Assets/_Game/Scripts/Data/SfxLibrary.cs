// SfxLibrary.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public enum E_SfxId
{
    None = 0,

    Rewind_Enter,
    Rewind_Prev,
    Rewind_Next,

    NoRewind_ChildFail,

    Move_Father,

    ChildBlocked_Wall,

    GapFiller_Push,

    Switch_On,
    Switch_Off,

    UI_Hover,
    UI_Click,

    Rewind_Loop,
    Rewind_Exit,
}

[Serializable]
public struct SfxClipEntry
{
    public E_SfxId Id;

    [Header("Clip")]
    public AudioClip Clip;

    [Header("Mix")]
    [Range(0f, 1f)] public float Volume;
    [Range(-3f, 3f)] public float PitchMin;
    [Range(-3f, 3f)] public float PitchMax;

    [Tooltip("0 = 2D, 1 = 3D")]
    [Range(0f, 1f)] public float SpatialBlend;

    [Header("De-dup Rules")]
    [Tooltip("Same id cannot play again until cooldown passes.")]
    [Min(0f)] public float CooldownSeconds;

    [Tooltip("Max simultaneous plays allowed for this id.")]
    [Min(0)] public int MaxVoices;

    [Header("Loop Blend")]
    [Tooltip("Loop 재생 시 끝과 시작을 겹쳐서 자연스럽게 연결한다. (0 = 사용 안함)")]
    [Min(0f)] public float LoopCrossfadeSeconds;

    public void Sanitize()
    {
        if (Volume <= 0f) Volume = 1f;
        if (Mathf.Approximately(PitchMin, 0f)) PitchMin = 1f;
        if (Mathf.Approximately(PitchMax, 0f)) PitchMax = 1f;

        if (PitchMin > PitchMax)
        {
            float t = PitchMin;
            PitchMin = PitchMax;
            PitchMax = t;
        }

        if (MaxVoices <= 0) MaxVoices = 1;
        SpatialBlend = Mathf.Clamp01(SpatialBlend);
        CooldownSeconds = Mathf.Max(0f, CooldownSeconds);
        LoopCrossfadeSeconds = Mathf.Max(0f, LoopCrossfadeSeconds);
    }
}

[CreateAssetMenu(menuName = "Game/Audio/SfxLibrary", fileName = "SfxLibrary")]
public class SfxLibrary : ScriptableObject
{
    [SerializeField] private List<SfxClipEntry> _entries = new();

    private readonly Dictionary<E_SfxId, SfxClipEntry> _cache = new();

    private void OnEnable()
    {
        BuildCache(logWarnings: false);
    }

    [ContextMenu("Validate Audio Libraries")]
    private void ValidateAndLog()
    {
        BuildCache(logWarnings: true);
    }

    private void BuildCache(bool logWarnings)
    {
        _cache.Clear();

        if (_entries == null) return;

        for (int i = 0; i < _entries.Count; i++)
        {
            var e = _entries[i];
            e.Sanitize();
            _entries[i] = e;

            if (_cache.ContainsKey(e.Id))
            {
                if (logWarnings)
                    Debug.LogWarning($"[SfxLibrary] Duplicate id detected. id={e.Id} (fallback: last wins)", this);
            }

            if (e.Clip == null && logWarnings)
                Debug.LogWarning($"[SfxLibrary] Clip is null. id={e.Id} (SFX will not play)", this);

            _cache[e.Id] = e;
        }
    }

    public bool TryGet(E_SfxId id, out SfxClipEntry entry)
        => _cache.TryGetValue(id, out entry);
}
