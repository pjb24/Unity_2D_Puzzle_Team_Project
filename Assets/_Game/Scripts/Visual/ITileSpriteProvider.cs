// ITileSpriteProvider.cs
using UnityEngine;

public interface ITileSpriteProvider
{
    bool TryGetSprite(E_TileVisualKey key, out Sprite sprite);
}
