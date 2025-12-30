///
/// FatherAction 결과 모델(확장 포인트)
/// 이동 성공/실패
/// 실패 원인(벽/장애물/바운더리/점유)
/// 트리거(Goal / 스위치 등)
///

using UnityEngine;

public enum E_FatherActionResultCode
{
    None,
    Moved,
    Blocked_OutOfBounds,
    Blocked_Cell,
    Blocked_Occupied,
}

public readonly struct FatherActionResult
{
    public readonly E_FatherActionResultCode Code;
    public readonly Vector2Int From;
    public readonly Vector2Int To;
    public readonly bool TriggerGoal;

    public bool IsSuccess => Code == E_FatherActionResultCode.Moved;

    public FatherActionResult(
        E_FatherActionResultCode code,
        Vector2Int from,
        Vector2Int to,
        bool triggerGoal)
    {
        Code = code;
        From = from;
        To = to;
        TriggerGoal = triggerGoal;
    }
}
