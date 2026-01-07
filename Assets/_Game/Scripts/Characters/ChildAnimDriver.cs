// ChildAnimDriver.cs
using UnityEngine;

[DisallowMultipleComponent]
public class ChildAnimDriver : MonoBehaviour
{
    private const string ParamMove = "Move";
    private static readonly int HashMove = Animator.StringToHash(ParamMove);

    [SerializeField] private Animator _anim;

    private bool _hasMoveTrigger;

    private bool _warnedMissingAnimator;
    private bool _warnedMissingParams;

    public bool IsUsable => _anim != null && _hasMoveTrigger;

    private void Awake()
    {
        if (_anim == null)
            _anim = GetComponent<Animator>();

        CacheParams();
    }

    public void ApplyAnimatorOverrideOrWarn(AnimatorOverrideController aoc, string stageId)
    {
        if (aoc == null)
        {
            Debug.LogWarning($"[ChildAnim] Apply override fallback: aoc is null. stageId={stageId}");
            return;
        }

        if (_anim == null)
        {
            Debug.LogWarning($"[ChildAnim] Apply override fallback: Animator missing. stageId={stageId}");
            return;
        }

        _anim.runtimeAnimatorController = aoc;
        CacheParams();
    }

    private void CacheParams()
    {
        _hasMoveTrigger = false;

        if (_anim == null)
            return;

        var ps = _anim.parameters;
        for (int i = 0; i < ps.Length; i++)
        {
            var p = ps[i];
            if (p.nameHash == HashMove && p.type == AnimatorControllerParameterType.Trigger)
            {
                _hasMoveTrigger = true;
            }
        }
    }

    public void PlayMove()
    {
        if (!EnsureReady())
            return;

        // 연타 안전
        _anim.ResetTrigger(HashMove);
        _anim.SetTrigger(HashMove);
    }

    private bool EnsureReady()
    {
        if (_anim == null)
        {
            if (!_warnedMissingAnimator)
            {
                _warnedMissingAnimator = true;
                Debug.LogWarning("[ChildAnim] Animator missing. (fallback: skip animation)");
            }
            return false;
        }

        if (!_hasMoveTrigger)
        {
            if (!_warnedMissingParams)
            {
                _warnedMissingParams = true;
                Debug.LogWarning($"[ChildAnim] Animator param missing: Trigger({ParamMove}). (fallback: skip animation)");
            }
            return false;
        }

        return true;
    }
}
