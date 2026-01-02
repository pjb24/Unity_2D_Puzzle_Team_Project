// StageClearState.cs
///
/// 요구: StageClear → (다음 스테이지 or 다음 챕터 or 종료)
/// 프로토타입 분기 규칙:
/// GameConfig.Chapters[chapterIndex].Stages.Count 기반
/// Stage 끝이면 Chapter++ / Stage=0
/// Chapter 끝이면 MainMenu로 복귀(엔딩은 나중에 확장)
///
using UnityEngine;

public class StageClearState : IGameFlowState
{
    private readonly GameFlowStateMachine _sm;
    private IGameFlowState _stageLoad;
    private IGameFlowState _chapterLoad;
    private IGameFlowState _mainMenu;

    public StageClearState(GameFlowStateMachine sm)
    {
        _sm = sm;
    }

    public void SetStagedLoadState(IGameFlowState stageLoad)
    {
        _stageLoad = stageLoad;
    }

    public void SetChapterLoadState(IGameFlowState chapterLoad)
    {
        _chapterLoad = chapterLoad;
    }

    public void SetMainMenuState(IGameFlowState mainMenu)
    {
        _mainMenu = mainMenu;
    }

    public E_GameFlowState Id => E_GameFlowState.StageClear;

    public void Enter(GameFlowContext ctx)
    {
        if (ctx == null)
        {
            Debug.LogError("[StageClearState] ctx is null.");
            return;
        }

        if (ctx._progression == null)
        {
            Debug.LogWarning("[StageClearState] ctx._progression is null. Go MainMenu (fallback).");
            _sm.ChangeState(ctx, _mainMenu);
            return;
        }

        // 여기서 UnloadStage를 호출하지 않는다.
        // 이유: StageLoad에서 Out 연출 동안 현재 스테이지가 화면에 남아야 한다.

        // 다음 분기 판단
        var result = ctx._progression.EvaluateNext(ctx);

        switch (result)
        {
            case E_StageAdvanceResult.NextStage:
                {
                    ctx._stageIndex++;
                    _sm.ChangeState(ctx, _stageLoad);
                    break;
                }
            case E_StageAdvanceResult.NextChapter:
                {
                    ctx._chapterIndex++;
                    ctx._stageIndex = 0;

                    // 새 챕터/스테이지 재조회 유도
                    ctx._chapterVisualProfile = null;
                    ctx._stageDefinition = null;

                    if (_chapterLoad == null)
                    {
                        Debug.LogWarning("[StageClearState] ChapterLoadState is null. Jump to StageLoad (fallback).");
                        _sm.ChangeState(ctx, _stageLoad);
                    }
                    else
                    {
                        _sm.ChangeState(ctx, _chapterLoad);
                    }

                    break;
                }
            case E_StageAdvanceResult.Ending:
            default:
                {
                    ctx._isEnding = true;

                    // 엔딩 처리(프로토타입: 메인메뉴 복귀)
                    ctx._scene.LoadMainMenu(() =>
                    {
                        _sm.ChangeState(ctx, _mainMenu);
                    });
                    break;
                }
        }
    }

    public void Exit(GameFlowContext ctx) { }

    public void Tick(GameFlowContext ctx) { }
}
