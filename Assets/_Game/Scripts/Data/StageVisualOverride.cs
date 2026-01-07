// StageVisualOverride.cs
///
/// Resources 경로 규칙:
/// Resources/Visual/StageOverrides/< stageId >.asset
/// < stageId > 는 StageDefinition.StageId 사용
///
using UnityEngine;

[CreateAssetMenu(menuName = "Puzzle/Visual/Stage Visual Override")]
public class StageVisualOverride : ScriptableObject
{
    [Header("Actors (Optional)")]
    [SerializeField] private Sprite _fatherSpriteOverride;
    [SerializeField] private Sprite _childSpriteOverride;

    [Header("Tiles / Gimmicks (Optional)")]
    [SerializeField] private Sprite _floor;
    [SerializeField] private Sprite _hole;
    [SerializeField] private Sprite _goal;
    [SerializeField] private Sprite _path;
    [SerializeField] private Sprite _doorOpen;
    [SerializeField] private Sprite _doorClosed;
    [SerializeField] private Sprite _switch;

    public Sprite FatherSpriteOverride => _fatherSpriteOverride;
    public Sprite ChildSpriteOverride => _childSpriteOverride;

    public bool TryGetTileSpriteOverride(E_TileVisualKey key, out Sprite sprite)
    {
        sprite = key switch
        {
            E_TileVisualKey.Floor => _floor,
            E_TileVisualKey.Hole => _hole,
            E_TileVisualKey.Goal => _goal,
            E_TileVisualKey.Path => _path,
            E_TileVisualKey.DoorOpen => _doorOpen,
            E_TileVisualKey.DoorClosed => _doorClosed,
            E_TileVisualKey.Switch => _switch,
            _ => null
        };

        return sprite != null;
    }
}
