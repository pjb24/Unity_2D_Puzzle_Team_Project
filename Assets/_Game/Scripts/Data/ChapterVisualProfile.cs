// ChapterVisualProfile.cs
using UnityEngine;

[CreateAssetMenu(menuName = "Puzzle/Data/Chapter Visual Profile")]
public class ChapterVisualProfile : ScriptableObject
{
    [Header("Prefabs / Sprites")]
    [SerializeField] private GameObject _fatherPrefab;
    [SerializeField] private GameObject _childPrefab;
    [SerializeField] private Sprite _fatherSprite;
    [SerializeField] private Sprite _childSprite;

    [Header("Tiles")]
    [SerializeField] private TileVisualProfile _tileVisualProfile;

    [Header("Audio (ID Only)")]
    [SerializeField] private E_BgmId _bgmId = E_BgmId.Chapter_01;
    [Header("Optional SFX (ID Only)")]
    [SerializeField] private E_SfxId _stageEnterSfxId;

    [Header("Anim Params (Prototype)")]
    [SerializeField] private float _fatherMoveSpeed = 1.0f;
    [SerializeField] private float _childStepSpeed = 1.0f;

    public GameObject FatherPrefab => _fatherPrefab;
    public GameObject ChildPrefab => _childPrefab;
    public Sprite FatherSprite => _fatherSprite;
    public Sprite ChildSprite => _childSprite;
    public TileVisualProfile TileVisualProfile => _tileVisualProfile;

    public E_BgmId BgmId => _bgmId;
    public E_SfxId StageEnterSfxId => _stageEnterSfxId;

    public float FatherMoveSpeed => _fatherMoveSpeed;
    public float ChildStepSpeed => _childStepSpeed;

    private void OnValidate()
    {
        // 최소: 둘 중 하나(프리팹 or 스프라이트)는 있어야 한다는 식의 규칙도 가능
        if (_fatherPrefab == null && _fatherSprite == null)
            Debug.LogWarning($"[ChapterVisualProfile] Father visual missing: {name}", this);

        if (_childPrefab == null && _childSprite == null)
            Debug.LogWarning($"[ChapterVisualProfile] Child visual missing: {name}", this);

        if (_tileVisualProfile == null)
            Debug.LogWarning($"[ChapterVisualProfile] TileVisualProfile missing: {name} (tiles will use proto fallback)", this);

        if (_bgmId == E_BgmId.None)
            Debug.LogWarning($"[ChapterVisualProfile] BgmId is None. BGM may not play. name={name}", this);

        if (_fatherMoveSpeed <= 0f) _fatherMoveSpeed = 0.01f;
        if (_childStepSpeed <= 0f) _childStepSpeed = 0.01f;
    }
}
