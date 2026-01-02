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

        // 전환 타입 확보(연출 시작 전에 필요)
        var def = ctx._config?.GetStageDefinition(ctx._chapterIndex, ctx._stageIndex);
        if (def == null)
        {
            Debug.LogWarning("[StageLoadState] StageDefinition not found. Transition=Fade (fallback).");
        }
        else
        {
            ctx._stageDefinition = def;
        }

        E_StageTransitionType t = (def != null) ? def.TransitionType : E_StageTransitionType.Fade;

        // TransitionFx 확보
        if (ctx._transitionFx == null)
        {
            ctx._transitionFx = Object.FindFirstObjectByType<StageTransitionFx>();
            if (ctx._transitionFx == null)
                Debug.LogWarning("[StageLoadState] StageTransitionFx not found. Transition skipped (fallback).");
        }

        // AudioHub 확보(SFX)
        if (ctx._audioHub == null)
        {
            ctx._audioHub = Object.FindFirstObjectByType<AudioHub>();
            if (ctx._audioHub == null)
                Debug.LogWarning("[StageLoadState] AudioHub not found. SFX skipped (fallback).");
        }

        if (ctx._transitionFx == null || t == E_StageTransitionType.None)
        {
            // 폴백: 즉시 로드
            ctx._stageLoader.LoadStage(ctx, () =>
            {
                PlayStageEnterSfx(ctx);
                _sm.ChangeState(ctx, _next);
            });
            return;
        }

        // 정상: Out -> (LoadStage at midpoint) -> In
        ctx._transitionFx.Play(ctx, t,
            onMidpointAsync: (continueAfterLoad) =>
            {
                ctx._stageLoader.LoadStage(ctx, () =>
                {
                    PlayStageEnterSfx(ctx);
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

    private void PlayStageEnterSfx(GameFlowContext ctx)
    {
        if (ctx._audioHub == null)
            return;

        var profile = ctx._chapterVisualProfile;
        if (profile == null)
            return;

        ctx._audioHub.PlaySfx(profile.SfxStageEnter);
    }
}
