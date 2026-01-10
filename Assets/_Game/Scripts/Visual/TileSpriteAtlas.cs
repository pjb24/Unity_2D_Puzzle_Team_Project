// TileSpriteAtlas.cs
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Visual/Tile Sprite Atlas", fileName = "TileSpriteAtlas_Default")]
public sealed class TileSpriteAtlas : ScriptableObject, ITileSpriteLookup
{
    [Serializable]
    public struct Entry
    {
        public E_TileVisualLayer Layer;
        public E_TileVisualType Type;

        // - Atlas에는 Goal/ChildPathOuterBorder도 "1장만" 넣는다.
        // - 방향은 런타임에서 회전으로 처리하므로 Dir은 None이어야 한다.
        public E_Dir4 Dir;
        public Sprite Sprite;
    }

    [SerializeField] private List<Entry> _entries = new();

    private Dictionary<TileVisualKey, Sprite> _cache;

    public IReadOnlyList<Entry> Entries => _entries;

    private void OnEnable()
    {
        BuildCacheIfNeeded();
    }

    public bool TryGetSprite(in TileVisualKey key, out Sprite sprite)
    {
        BuildCacheIfNeeded();

        if (_cache.TryGetValue(key, out sprite))
            return sprite != null;

        sprite = null;
        return false;
    }

    private void BuildCacheIfNeeded()
    {
        if (_cache != null)
            return;

        _cache = new Dictionary<TileVisualKey, Sprite>(_entries.Count);

        for (int i = 0; i < _entries.Count; i++)
        {
            Entry e = _entries[i];

            // 엔트리 유효성 검사(방향 규칙)
            bool isDirectional = TileVisualKey.IsDirectionalType(e.Type);

            if (isDirectional && e.Dir != E_Dir4.None)
            {
                Debug.LogWarning($"[TileSpriteAtlas] Invalid entry: directional type must use dir=None in atlas. index={i}, type={e.Type}, dir={e.Dir}", this);
                continue;
            }

            if (!isDirectional && e.Dir != E_Dir4.None)
            {
                Debug.LogWarning($"[TileSpriteAtlas] Invalid entry: non-directional type must use dir=None. index={i}, type={e.Type}, dir={e.Dir}", this);
                continue;
            }

            var key = new TileVisualKey(e.Layer, e.Type, E_Dir4.None);

            // 중복 키 방지: "첫 항목 고정" + Warning
            if (_cache.ContainsKey(key))
            {
                Debug.LogWarning($"[TileSpriteAtlas] Duplicate key ignored. index={i}, key={key}", this);
                continue;
            }

            if (e.Sprite == null)
            {
                Debug.LogWarning($"[TileSpriteAtlas] Null sprite entry. index={i}, key={key}", this);
                continue;
            }

            _cache.Add(key, e.Sprite);
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Validate Entries")]
    private void ValidateEntries()
    {
        _cache = null;
        BuildCacheIfNeeded();
        Debug.Log($"[TileSpriteAtlas] Validate done. entries={_entries.Count}, cache={_cache.Count}", this);
    }
#endif
}
