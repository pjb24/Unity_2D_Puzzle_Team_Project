// StageLoadState.cs
/// <summary>
/// 요구 파이프라인: SO 로드 → 런타임 생성 → 스폰 → UI 초기화
/// StageLoader가 이미 “보드/경로/스폰”을 해주므로, 상태는 호출만.
/// </summary>
using UnityEngine;

public class StageLoadState : IGameFlowState
{
    private readonly GameFlowStateMachine _sm;
    private IGameFlowState _next;

    public StageLoadState(GameFlowStateMachine sm)
    {
        _sm = sm;
    }

    public void SetNext(IGameFlowState next)
    {
        _next = next;
    }

    public E_GameFlowState Id => E_GameFlowState.StageLoad;

    public void Enter(GameFlowContext ctx)
    {
        if (ctx == null)
        {
            Debug.LogError("[StageLoadState] ctx is null.");
            return;
        }

        if (ctx._stageLoader == null)
        {
            Debug.LogError("[StageLoadState] ctx._stageLoader is null.");
            return;
        }

        // TransitionFx 확보
        if (ctx._transitionFx == null)
        {
            ctx._transitionFx = Object.FindFirstObjectByType<StageTransitionFx>();
            if (ctx._transitionFx == null)
                Debug.LogWarning("[StageLoadState] StageTransitionFx not found. Transition skipped (fallback).");
        }

        // AudioHub 확보
        if (ctx._audioHub == null)
            ctx._audioHub = AudioHub.Ensure();

        if (ctx._transitionFx == null)
        {
            // 폴백: 즉시 로드
            ctx._stageLoader.LoadStage(ctx, () =>
            {
                ApplyStageAudio(ctx);
                _sm.ChangeState(ctx, _next);
            });
            return;
        }

        // 정상: Out -> (LoadStage at midpoint) -> In
        ctx._transitionFx.Play(ctx, E_StageTransitionType.Fade,
            onMidpointAsync: (continueAfterLoad) =>
            {
                ctx._stageLoader.LoadStage(ctx, () =>
                {
                    ApplyStageAudio(ctx);
                    continueAfterLoad?.Invoke();
                });
                return true;
            },
            onDone: () =>
            {
                _sm.ChangeState(ctx, _next);
            });
    }

    public void Tick(GameFlowContext ctx) { }
    public void Exit(GameFlowContext ctx) { }

    private void ApplyStageAudio(GameFlowContext ctx)
    {
        if (ctx == null || ctx._audioHub == null)
            return;

        E_BgmId bgmId = ctx._gameConfig.Chapters[ctx._chapterIndex].Stages[ctx._stageIndex].BgmId;

        if (bgmId == E_BgmId.None)
        {
            Debug.LogWarning("[StageLoadState] Selected BgmId is None. BGM skipped.");
        }
        else
        {
            ctx._audioHub.PlayBgmIfChanged(bgmId);
        }
    }
}
