// StageVisualOverride.cs
///
/// Resources 경로 규칙:
/// Resources/Visual/StageOverrides/< stageId >.asset
/// < stageId > 는 StageDefinition.StageId 사용
///
using UnityEngine;

public enum E_InnerBaseBackgroundDrawMode
{
    Tiled,
    Sliced,
    Simple, // 폴백용(Transform scale)
}

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

    [Header("InnerBase Background (Optional)")]
    [SerializeField] private bool _useInnerBaseBackground = false;
    [SerializeField] private Sprite _innerBaseBackgroundSprite;
    [SerializeField] private E_InnerBaseBackgroundDrawMode _innerBaseBackgroundDrawMode = E_InnerBaseBackgroundDrawMode.Tiled;

    [Tooltip("InnerBase rect(w,h)에 더해지는 셀 단위 여백. (x=좌/우, y=상/하)")]
    [SerializeField] private Vector2 _innerBaseBackgroundPaddingCells = Vector2.zero;

    [Tooltip("타일보다 낮게(뒤) 깔아야 배경처럼 보임. 기본 -10 권장.")]
    [SerializeField] private int _innerBaseBackgroundSortingOrder = -10;

    [Header("Move Animation Override (AnimatorOverrideController)")]
    [SerializeField] private bool _useMoveAnimOverride = false;
    [SerializeField] private AnimatorOverrideController _fatherMoveAnimatorOverride;
    [SerializeField] private AnimatorOverrideController _childMoveAnimatorOverride;

    [Header("Rewind Restore Move FX")]
    [SerializeField] private bool _useRewindRestoreLerp = true;

    [Tooltip("UseRewindRestoreLerp가 true일 때만 사용. 0 이하이면 복원 시 Snap 폴백.")]
    [SerializeField] private float _rewindRestoreMoveDuration = 0.12f;

    public Sprite FatherSpriteOverride => _fatherSpriteOverride;
    public Sprite ChildSpriteOverride => _childSpriteOverride;

    public bool UseInnerBaseBackground => _useInnerBaseBackground;
    public Sprite InnerBaseBackgroundSprite => _innerBaseBackgroundSprite;
    public E_InnerBaseBackgroundDrawMode InnerBaseBackgroundDrawMode => _innerBaseBackgroundDrawMode;
    public Vector2 InnerBaseBackgroundPaddingCells => _innerBaseBackgroundPaddingCells;
    public int InnerBaseBackgroundSortingOrder => _innerBaseBackgroundSortingOrder;

    public bool UseMoveAnimOverride => _useMoveAnimOverride;
    public AnimatorOverrideController FatherMoveAnimatorOverride => _fatherMoveAnimatorOverride;
    public AnimatorOverrideController ChildMoveAnimatorOverride => _childMoveAnimatorOverride;

    public bool UseRewindRestoreLerp => _useRewindRestoreLerp;
    public float RewindRestoreMoveDuration => _rewindRestoreMoveDuration;

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
