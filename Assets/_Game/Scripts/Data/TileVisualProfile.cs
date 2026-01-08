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
        public TileSelector _selector;
        public Sprite _sprite;
    }

    [Header("Mapping")]
    [SerializeField] private Entry[] _entries;

    private readonly Dictionary<TileSelector, Sprite> _map = new();
    private readonly HashSet<TileSelector> _warnedNullSprite = new();
    private readonly HashSet<TileSelector> _warnedDuplicate = new();

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
        _warnedNullSprite.Clear();
        _warnedDuplicate.Clear();

        if (_entries == null)
            return;

        for (int i = 0; i < _entries.Length; i++)
        {
            var e = _entries[i];

            // null 스프라이트는 캐시에 넣지 않는다 (TryGetSprite는 false)
            if (e._sprite == null)
                continue;

            if (_map.ContainsKey(e._selector))
            {
                if (!_warnedDuplicate.Contains(e._selector))
                {
                    _warnedDuplicate.Add(e._selector);
                    Debug.LogWarning($"[TileVisualProfile] Duplicate selector detected. last wins. profile={name} selector=({e._selector})", this);
                }
            }

            _map[e._selector] = e._sprite;
        }
    }

    public bool TryGetSprite(in TileSelector selector, out Sprite sprite)
    {
        if (_map.TryGetValue(selector, out sprite) && sprite != null)
            return true;

        // 인스펙터 데이터 실수 디버깅(키는 있는데 스프라이트가 null) 1회 경고
        if (_entries != null)
        {
            for (int i = 0; i < _entries.Length; i++)
            {
                if (!_entries[i]._selector.Equals(selector))
                    continue;

                if (_entries[i]._sprite == null && !_warnedNullSprite.Contains(selector))
                {
                    _warnedNullSprite.Add(selector);
                    Debug.LogWarning($"[TileVisualProfile] Sprite is null. profile={name} selector=({selector}) (treated as missing)", this);
                }

                break;
            }
        }

        sprite = null;
        return false;
    }
}
