// StageAudioProfile.cs
using UnityEngine;

[CreateAssetMenu(menuName = "Puzzle/Data/Stage Audio Profile")]
public class StageAudioProfile : ScriptableObject
{
    [Header("BGM Override")]
    [SerializeField] private bool _useBgmOverride = false;
    [SerializeField] private E_BgmId _bgmId = E_BgmId.None;

    [Header("BGM Volume Scale")]
    [SerializeField, Range(0f, 2f)] private float _bgmVolumeScale = 1f;

    [Header("Stage Enter SFX Override (optional)")]
    [SerializeField] private bool _useStageEnterSfxOverride = false;
    [SerializeField] private E_SfxId _stageEnterSfxId = E_SfxId.None;

    public bool UseBgmOverride => _useBgmOverride;
    public E_BgmId BgmId => _bgmId;
    public float BgmVolumeScale => _bgmVolumeScale;

    public bool UseStageEnterSfxOverride => _useStageEnterSfxOverride;
    public E_SfxId StageEnterSfxId => _stageEnterSfxId;

    private void OnValidate()
    {
        if (_bgmVolumeScale <= 0f) _bgmVolumeScale = 0.01f;

        if (_useBgmOverride && _bgmId == E_BgmId.None)
            Debug.LogWarning($"[StageAudioProfile] UseBgmOverride is true but BgmId is None. name={name}", this);

        if (_useStageEnterSfxOverride && _stageEnterSfxId == E_SfxId.None)
            Debug.LogWarning($"[StageAudioProfile] UseStageEnterSfxOverride is true but StageEnterSfxId is None. name={name}", this);
    }
}
