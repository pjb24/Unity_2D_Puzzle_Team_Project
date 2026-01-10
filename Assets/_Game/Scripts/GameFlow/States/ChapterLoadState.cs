// ChapterLoadState.cs
using UnityEngine;

public class ChapterLoadState : IGameFlowState
{
    private readonly GameFlowStateMachine _sm;
    private IGameFlowState _next;

    public ChapterLoadState(GameFlowStateMachine sm)
    {
        _sm = sm;
    }

    public void SetNext(IGameFlowState next)
    {
        _next = next;
    }

    public E_GameFlowState Id => E_GameFlowState.ChapterLoad;

    public void Enter(GameFlowContext ctx)
    {
        if (ctx == null)
        {
            Debug.LogError("[ChapterLoadState] ctx is null.");
            return;
        }

        // GameConfig 캐시
        if (ctx._gameConfig == null)
            ctx._gameConfig = ctx._config?.LoadGameConfig();

        if (ctx._gameConfig == null)
        {
            Debug.LogWarning("[ChapterLoadState] GameConfig is null. ChapterVisualProfile cleared (fallback).");
            _sm.ChangeState(ctx, _next);
            return;
        }

        if (ctx._chapterIndex < 0 || ctx._chapterIndex >= ctx._gameConfig.Chapters.Count)
        {
            Debug.LogWarning($"[ChapterLoadState] Invalid chapterIndex={ctx._chapterIndex}. Force 0 (fallback).");
            ctx._chapterIndex = 0;
        }

        var chapter = ctx._gameConfig.Chapters[ctx._chapterIndex];

        // 오디오(있으면 적용)
        EnsureAudioHub(ctx);

        if (ctx._audioHub != null)
            ctx._audioHub.PlayBgmIfChanged(chapter.BgmId);

        _sm.ChangeState(ctx, _next);
    }

    public void Tick(GameFlowContext ctx) { }
    public void Exit(GameFlowContext ctx) { }

    private void EnsureAudioHub(GameFlowContext ctx)
    {
        if (ctx._audioHub != null)
            return;

        ctx._audioHub = Object.FindFirstObjectByType<AudioHub>();
        if (ctx._audioHub == null)
        {
            Debug.LogWarning("[ChapterLoadState] AudioHub not found. BGM/SFX skipped (fallback).");
        }
    }
}
