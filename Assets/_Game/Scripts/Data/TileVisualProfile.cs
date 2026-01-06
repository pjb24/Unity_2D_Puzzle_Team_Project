// TileVisualProfile.cs
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Puzzle/Visual/Tile Visual Profile")]
public class TileVisualProfile : ScriptableObject
{
    [Serializable]
    public struct Entry
    {
        public E_TileVisualKey _key;
        public Sprite _sprite;
    }

    [Header("Fallback")]
    [SerializeField] private Sprite _fallbackSprite;

    [Header("Sprites")]
    [SerializeField] private Entry[] _entries;

    private readonly Dictionary<E_TileVisualKey, Sprite> _map = new Dictionary<E_TileVisualKey, Sprite>(32);
    private readonly HashSet<E_TileVisualKey> _warnedMissing = new HashSet<E_TileVisualKey>();

    private void OnEnable()
    {
        RebuildCache();
    }

    private void OnValidate()
    {
        RebuildCache();
    }

    private void RebuildCache()
    {
        _map.Clear();

        if (_entries == null)
            return;

        for (int i = 0; i < _entries.Length; i++)
        {
            var e = _entries[i];
            if (_map.ContainsKey(e._key))
            {
                Debug.LogWarning($"[TileVisualProfile] Duplicate key ignored. profile={name} key={e._key}", this);
                continue;
            }

            _map.Add(e._key, e._sprite);
        }
    }

    public bool TryGetSprite(E_TileVisualKey key, out Sprite sprite)
    {
        if (_map.TryGetValue(key, out sprite) && sprite != null)
            return true;

        sprite = null;
        return false;
    }

    public Sprite GetSpriteOrFallback(E_TileVisualKey key)
    {
        if (TryGetSprite(key, out var sprite))
            return sprite;

        if (!_warnedMissing.Contains(key))
        {
            _warnedMissing.Add(key);
            Debug.LogWarning($"[TileVisualProfile] Sprite missing -> fallback used. profile={name} key={key}", this);
        }

        return _fallbackSprite;
    }
}
