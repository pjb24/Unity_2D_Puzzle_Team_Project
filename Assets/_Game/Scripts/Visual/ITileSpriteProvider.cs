// ITileSpriteProvider.cs
using UnityEngine;

public interface ITileSpriteProvider
{
    /// <summary>
    /// 스프라이트가 "설정되어 있을 때만" true.
    /// sprite가 null이면 반드시 false.
    /// </summary>
    bool TryGetSprite(in TileSelector selector, out Sprite sprite);
}
