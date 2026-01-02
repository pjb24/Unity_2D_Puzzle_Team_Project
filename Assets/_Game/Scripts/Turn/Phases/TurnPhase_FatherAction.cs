// TurnPhase_FatherAction.cs
/// <summary>
/// FatherAction → ChildStep
/// 
/// FatherController가 “행동 완료”를 알리면 전이.
/// 
/// 주의: TurnPhase_FatherAction이 TurnStateMachine 참조를 필요로 하니,
/// 생성 순서가 꼬이면 팩토리로 조립하면 된다.
/// 
/// Father 완료 이벤트가 오면 FatherController.LastResult를 읽어서 ctx에 저장 후 ChildStep으로 전이
/// </summary>

using UnityEngine;

public class TurnPhase_FatherAction : ITurnPhase
{
    public E_TurnPhase Phase => E_TurnPhase.FatherAction;

    private readonly TurnStateMachine _sm;
    private TurnContext _ctx;

    public TurnPhase_FatherAction(TurnStateMachine sm) { _sm = sm; }

    public void Enter(TurnContext ctx)
    {
        _ctx = ctx;
        ctx.Father.AddListenerOnActionCompleted(OnFatherDone);
    }

    public void Exit(TurnContext ctx)
    {
        ctx.Father.RemoveListenerOnActionCompleted(OnFatherDone);
        if (_ctx == ctx) _ctx = null;
    }

    public void Tick(TurnContext ctx) { }

    private void OnFatherDone()
    {
        if (_ctx == null) return;

        // FatherResult 저장(Resolve/디버그/UI 확장 포인트)
        _ctx.FatherResult = _ctx.Father.LastResult;

        // ===== 이동 입력인데 이동 실패면 턴 진행 금지 =====
        if (IsMoveCommand(_ctx.AcceptedCommand) && !_ctx.FatherResult.IsSuccess)
        {
            // 벽/범위/점유 등으로 막힘: 턴 소모 X, 다음 턴으로 진행 X
            Debug.Log($"[Turn] Father move blocked ({_ctx.FatherResult.Code}) -> stay in Input (no turn advance)");

            _ctx.RollbackTurnBecauseFatherBlocked();
            _ctx.SetInputLocked(false);       // 입력 잠금 해제
            _sm.Change(E_TurnPhase.Input);    // Input으로 복귀
            return;
        }

        // 여기부터 “실제 턴 소비”가 발생한 시점
        _ctx.InvokeTurnBegin();

        // 정상 턴 진행
        _sm.Change(E_TurnPhase.ChildStep);
    }

    private bool IsMoveCommand(TurnCommand cmd)
    {
        return cmd.Type == E_TurnCommandType.MoveUp
            || cmd.Type == E_TurnCommandType.MoveDown
            || cmd.Type == E_TurnCommandType.MoveLeft
            || cmd.Type == E_TurnCommandType.MoveRight;
    }
}
