// StageVisualOverride.cs
///
/// Resources 경로 규칙:
/// Resources/Visual/StageOverrides/< stageId >.asset
/// < stageId > 는 StageDefinition.StageId 사용
///
using UnityEngine;

public enum E_Dir4
{
    Up,
    Right,
    Down,
    Left,
}

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

    [Header("Layout (Optional)")]
    [SerializeField] private bool _useLayoutOverride = false;

    [Tooltip("월드 스케일. 1.0이면 1유닛 크기(프로토 기준). 0 이하이면 무시됨.")]
    [SerializeField] private float _tileSize = 1f;

    [Tooltip("타일 간격. 0이면 붙음. 음수는 0으로 클램프.")]
    [SerializeField] private float _tileGap = 0f;

    [Header("Child Path Outer Border (Optional)")]
    [SerializeField] private bool _useChildPathOuterBorder = false;
    [Tooltip("테두리 스프라이트 1종(기본 방향=Right 기준).")]
    [SerializeField] private Sprite _childPathOuterBorderSprite;
    [Tooltip("두께(셀 단위). 1이면 '1칸'. 0 이하는 1로 폴백. 테두리 두께 = tileSize * scale")]
    [SerializeField] private float _childPathOuterBorderThicknessCells = 1f;
    [Tooltip("경로 바깥쪽으로 얼마나 밀지(셀 단위). 0.5면 바깥 한 칸 중심으로 이동.")]
    [SerializeField] private float _childPathOuterBorderOffsetCells = 0.5f;

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

    public bool UseLayoutOverride => _useLayoutOverride;
    public float TileSize => _tileSize;
    public float TileGap => _tileGap;

    public bool UseChildPathOuterBorder => _useChildPathOuterBorder;
    public Sprite ChildPathOuterBorderSprite => _childPathOuterBorderSprite;
    public float ChildPathOuterBorderThicknessCells => _childPathOuterBorderThicknessCells;
    public float ChildPathOuterBorderOffsetCells => _childPathOuterBorderOffsetCells;

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

    private void OnValidate()
    {
        if (_useLayoutOverride)
        {
            if (_tileSize <= 0f)
            {
                Debug.LogWarning("[StageVisualOverride] LayoutOverride fallback: TileSize <= 0. Force 1.");
                _tileSize = 1f;
            }

            if (_tileGap < 0f)
            {
                Debug.LogWarning("[StageVisualOverride] LayoutOverride fallback: TileGap < 0. Clamp 0.");
                _tileGap = 0f;
            }
        }
    }

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
