// TurnPhase_Resolve.cs
/// <summary>
/// Resolve → Snapshot
/// 
/// 난이도 정책까지는 나중에 붙이고,
/// 지금은 ChildBlocked만 판정해서 TurnFailed/TurnCleared를 세팅.
/// Resolve 종료 시점에 입력 잠금 OFF.
/// 
/// Resolve에서 최소로 쓸만한 포인트는 2개:
/// Goal 트리거면 TurnCleared = true
/// 이동 실패면 로그/피드백 훅(나중에 UI/사운드)
/// </summary>

public class TurnPhase_Resolve : ITurnPhase
{
    public E_TurnPhase Phase => E_TurnPhase.Resolve;

    private readonly TurnStateMachine _sm;

    public TurnPhase_Resolve(TurnStateMachine sm) { _sm = sm; }

    public void Enter(TurnContext ctx)
    {
        // 1) Goal 체크
        // 1) Clear 우선
        if (ctx.FatherResult.TriggerGoal)
        {
            ctx.TurnCleared = true;
            ctx.TurnFailed = false;

            ctx._signals?.RaiseResolved(E_TurnResolveOutcome.StageCleared, E_StageFailReason.None, ctx.TurnIndex);

            ctx.SetInputLocked(false);
            _sm.Change(E_TurnPhase.Snapshot);
            return;
        }

        // 2) ChildBlocked 분기
        if (ctx.ChildBlocked)
        {
            bool failOnBlocked = ctx._profile != null && ctx._profile.FailOnChildBlocked;
            if (failOnBlocked)
            {
                ctx.TurnFailed = true;

                bool hardReset = ctx._profile != null && ctx._profile.HardResetStage;

                var outcome = hardReset
                    ? E_TurnResolveOutcome.StageFailed_Reset
                    : E_TurnResolveOutcome.StageFailed_Rewind;

                ctx._signals?.RaiseResolved(outcome, E_StageFailReason.ChildBlocked, ctx.TurnIndex);
            }
            else
            {
                // Easy: 실패 없음(턴은 정상 종료)
                ctx.TurnFailed = false;
                ctx._signals?.RaiseResolved(E_TurnResolveOutcome.Continue, E_StageFailReason.ChildBlocked, ctx.TurnIndex);
            }
        }
        else
        {
            ctx.TurnFailed = false;
            ctx._signals?.RaiseResolved(E_TurnResolveOutcome.Continue, E_StageFailReason.None, ctx.TurnIndex);
        }

        ctx.SetInputLocked(false); // 잠금 OFF (규칙: Resolve 종료)
        _sm.Change(E_TurnPhase.Snapshot);
    }

    public void Tick(TurnContext ctx) { }
    public void Exit(TurnContext ctx) { }
}
