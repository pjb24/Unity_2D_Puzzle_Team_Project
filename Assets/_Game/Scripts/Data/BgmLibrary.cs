// BgmLibrary.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public enum E_BgmId
{
    None = 0,

    MainMenu = 10,

    Chapter_01 = 100,
    Chapter_02 = 200,

    // 필요하면 Stage 전용도 추가
    Stage_01_01 = 1001,
    Stage_01_02 = 1002,
    Stage_01_03 = 1003,
    Stage_01_04 = 1004,
}

[Serializable]
public struct BgmClipEntry
{
    public E_BgmId Id;

    [Header("Clip")]
    public AudioClip Clip;

    [Header("Mix")]
    [Range(0f, 1f)] public float Volume;

    [Tooltip("Optional. If null, AudioHub.BgmMixerGroup is used.")]
    public AudioMixerGroup MixerGroupOverride;

    public void Sanitize()
    {
        if (Volume <= 0f) Volume = 1f;
    }
}

[CreateAssetMenu(menuName = "Game/Audio/BgmLibrary", fileName = "BgmLibrary")]
public class BgmLibrary : ScriptableObject
{
    [SerializeField] private List<BgmClipEntry> _entries = new();

    private readonly Dictionary<E_BgmId, BgmClipEntry> _cache = new();

    private void OnEnable()
    {
        BuildCache(logWarnings: false);
    }

    private void OnValidate()
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
                    Debug.LogWarning($"[BgmLibrary] Duplicate id detected. id={e.Id} (fallback: last wins)", this);
            }

            if (e.Clip == null && logWarnings && e.Id != E_BgmId.None)
                Debug.LogWarning($"[BgmLibrary] Clip is null. id={e.Id} (BGM will not play)", this);

            _cache[e.Id] = e;
        }
    }

    public bool TryGet(E_BgmId id, out BgmClipEntry entry)
        => _cache.TryGetValue(id, out entry);
}
