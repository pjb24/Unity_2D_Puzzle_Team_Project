// TurnPhase_Resolve.cs
/// <summary>
/// Resolve → Snapshot
/// 
/// ChildBlocked를 판정해서 TurnFailed/TurnCleared를 세팅.
/// Resolve 종료 시점에 입력 잠금 OFF.
/// 
/// Resolve에서 최소로 쓸만한 포인트는 2개:
/// Goal 트리거면 TurnCleared = true
/// 이동 실패면 로그/피드백 훅(나중에 UI/사운드)
/// </summary>
using UnityEngine;

public class TurnPhase_Resolve : ITurnPhase
{
    public E_TurnPhase Phase => E_TurnPhase.Resolve;

    private readonly TurnStateMachine _sm;

    public TurnPhase_Resolve(TurnStateMachine sm) { _sm = sm; }

    public void Enter(TurnContext ctx)
    {
        // 1) Clear 우선, Goal 체크
        if (ctx != null && ctx.Child != null && !ctx.ChildBlocked && ctx.ChildGoalPathStep >= 0)
        {
            if (ctx.Child.PathPos == ctx.ChildGoalPathStep)
            {
                ctx.TurnCleared = true;
                ctx.TurnFailed = false;

                ctx.SetPendingOutcome(E_TurnResolveOutcome.StageCleared, E_StageFailReason.None);

                _sm.Change(E_TurnPhase.Snapshot);
                return;
            }
        }
        else
        {
            // 골 정보가 없으면 기존 Father TriggerGoal로 폴백(무음 금지)
            if (ctx != null && ctx.ChildGoalPathStep < 0 && ctx.FatherResult.TriggerGoal)
            {
                Debug.LogWarning("[TurnPhase_Resolve] ChildGoalPathStep is not set. Fallback to FatherResult.TriggerGoal.");
                ctx.TurnCleared = true;
                ctx.TurnFailed = false;

                ctx.SetPendingOutcome(E_TurnResolveOutcome.StageCleared, E_StageFailReason.None);
                _sm.Change(E_TurnPhase.Snapshot);
                return;
            }
        }

        // 2) ChildBlocked 분기
        if (ctx.ChildBlocked)
        {
            bool failOnBlocked = ctx._profile != null && ctx._profile.FailOnChildBlocked;
            if (failOnBlocked)
            {
                ctx.TurnFailed = true;

                // 실패 시에도 막힘 피드백은 찍어야 "막힘 비주얼"이 먼저 나옴
                if (ctx.Child != null)
                    ctx.Child.RequestBlockedFeedback();
                else
                    Debug.LogWarning("[TurnPhase_Resolve] ChildBlocked feedback skipped: ctx.Child is null (fallback).");

                bool hardReset = ctx._profile != null && ctx._profile.HardResetStage;

                var outcome = hardReset
                    ? E_TurnResolveOutcome.StageFailed_Reset
                    : E_TurnResolveOutcome.StageFailed_Rewind;

                // 여기서 즉시 RaiseResolved 하지 않음 (Snapshot 이후로 지연)
                ctx.SetPendingOutcome(outcome, E_StageFailReason.ChildBlocked);
            }
            else
            {
                // Easy: 실패 없음(턴은 정상 종료)
                ctx.TurnFailed = false;

                if (ctx.Child != null)
                    ctx.Child.RequestBlockedFeedback();
                else
                    Debug.LogWarning("[TurnPhase_Resolve] ChildBlocked feedback skipped: ctx.Child is null (fallback).");

                // Continue는 기존처럼 즉시 알림 유지
                ctx._signals?.RaiseResolved(E_TurnResolveOutcome.Continue, E_StageFailReason.ChildBlocked, ctx.TurnIndex);
            }
            // 실패면 자동턴 없음
            ctx.PendingAutoTurns = 0;
        }
        else
        {
            ctx.TurnFailed = false;

            // 3) 정상 진행: 턴 비용(2턴) 예약
            int extra = Mathf.Max(0, ctx.FatherResult.ConsumedTurns - 1);
            ctx.PendingAutoTurns = extra;

            // Continue는 기존처럼 즉시 알림 유지
            ctx._signals?.RaiseResolved(E_TurnResolveOutcome.Continue, E_StageFailReason.None, ctx.TurnIndex);
        }

        _sm.Change(E_TurnPhase.Snapshot);
    }

    public void Tick(TurnContext ctx) { }
    public void Exit(TurnContext ctx) { }
}
