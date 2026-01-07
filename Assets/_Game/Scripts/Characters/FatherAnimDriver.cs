// FatherAnimDriver.cs
using UnityEngine;

[DisallowMultipleComponent]
public class FatherAnimDriver : MonoBehaviour
{
    // Animator 파라미터
    // - Facing (Int): 0=Up, 1=Right, 2=Down, 3=Left
    // - Move (Trigger)
    private const string ParamFacing = "Facing";
    private const string ParamMove = "Move";

    private static readonly int HashFacing = Animator.StringToHash(ParamFacing);
    private static readonly int HashMove = Animator.StringToHash(ParamMove);

    [SerializeField] private Animator _anim;

    private bool _hasFacingInt;
    private bool _hasMoveTrigger;

    private bool _warnedMissingAnimator;
    private bool _warnedMissingParams;

    public bool IsUsable => _anim != null && _hasFacingInt && _hasMoveTrigger;

    private void Awake()
    {
        if (_anim == null)
            _anim = GetComponent<Animator>();

        CacheParams();
    }

    private void CacheParams()
    {
        _hasFacingInt = false;
        _hasMoveTrigger = false;

        if (_anim == null)
            return;

        var ps = _anim.parameters;
        for (int i = 0; i < ps.Length; i++)
        {
            var p = ps[i];
            if (p.nameHash == HashFacing && p.type == AnimatorControllerParameterType.Int)
                _hasFacingInt = true;

            if (p.nameHash == HashMove && p.type == AnimatorControllerParameterType.Trigger)
                _hasMoveTrigger = true;
        }
    }

    public void SetFacing(E_Facing facing)
    {
        if (!EnsureReady(requireMoveTrigger: false))
            return;

        _anim.SetInteger(HashFacing, FacingToAnimatorInt(facing));
    }

    public void PlayMove(E_Facing facing)
    {
        if (!EnsureReady(requireMoveTrigger: true))
            return;

        _anim.SetInteger(HashFacing, FacingToAnimatorInt(facing));

        // 연타 안전
        _anim.ResetTrigger(HashMove);
        _anim.SetTrigger(HashMove);
    }

    private bool EnsureReady(bool requireMoveTrigger)
    {
        if (_anim == null)
        {
            if (!_warnedMissingAnimator)
            {
                _warnedMissingAnimator = true;
                Debug.LogWarning("[FatherAnim] Animator missing. (fallback: skip animation)");
            }
            return false;
        }

        if (!_hasFacingInt || (requireMoveTrigger && !_hasMoveTrigger))
        {
            if (!_warnedMissingParams)
            {
                _warnedMissingParams = true;
                Debug.LogWarning($"[FatherAnim] Animator params missing. required: Int({ParamFacing}), Trigger({ParamMove}). (fallback: skip animation)");
            }
            return false;
        }

        return true;
    }

    private static int FacingToAnimatorInt(E_Facing f)
    {
        // Animator는 이 매핑을 기준으로 세팅하면 됨:
        // Up=0, Right=1, Down=2, Left=3
        return f switch
        {
            E_Facing.Up => 0,
            E_Facing.Right => 1,
            E_Facing.Down => 2,
            E_Facing.Left => 3,
            _ => 2,
        };
    }
}
