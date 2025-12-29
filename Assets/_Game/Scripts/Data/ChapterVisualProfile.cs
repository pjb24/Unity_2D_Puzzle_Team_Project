using UnityEngine;

[CreateAssetMenu(menuName = "Puzzle/Data/Chapter Visual Profile")]
public class ChapterVisualProfile : ScriptableObject
{
    [Header("Prefabs / Sprites")]
    [SerializeField] private GameObject _fatherPrefab;
    [SerializeField] private GameObject _childPrefab;
    [SerializeField] private Sprite _fatherSprite;
    [SerializeField] private Sprite _childSprite;

    [Header("Audio")]
    [SerializeField] private AudioClip _bgm;
    [SerializeField] private AudioClip _sfxStageEnter;

    [Header("Anim Params (Prototype)")]
    [SerializeField] private float _fatherMoveSpeed = 1.0f;
    [SerializeField] private float _childStepSpeed = 1.0f;

    public GameObject FatherPrefab => _fatherPrefab;
    public GameObject ChildPrefab => _childPrefab;
    public Sprite FatherSprite => _fatherSprite;
    public Sprite ChildSprite => _childSprite;
    public AudioClip Bgm => _bgm;
    public AudioClip SfxStageEnter => _sfxStageEnter;
    public float FatherMoveSpeed => _fatherMoveSpeed;
    public float ChildStepSpeed => _childStepSpeed;

    private void OnValidate()
    {
        // 최소: 둘 중 하나(프리팹 or 스프라이트)는 있어야 한다는 식의 규칙도 가능
        if (_fatherPrefab == null && _fatherSprite == null)
            Debug.LogWarning($"[ChapterVisualProfile] Father visual missing: {name}", this);

        if (_childPrefab == null && _childSprite == null)
            Debug.LogWarning($"[ChapterVisualProfile] Child visual missing: {name}", this);

        if (_fatherMoveSpeed <= 0f) _fatherMoveSpeed = 0.01f;
        if (_childStepSpeed <= 0f) _childStepSpeed = 0.01f;
    }
}
