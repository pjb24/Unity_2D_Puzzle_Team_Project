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
    private IGameFlowState _mainMenu;

    public StageClearState(GameFlowStateMachine sm)
    {
        _sm = sm;
    }

    public void SetStagedLoadState(IGameFlowState stageLoad)
    {
        _stageLoad = stageLoad;
    }

    public void SetMainMenuState(IGameFlowState mainMenu)
    {
        _mainMenu = mainMenu;
    }

    public E_GameFlowState Id => E_GameFlowState.StageClear;

    public void Enter(GameFlowContext ctx)
    {
        // 1) 현재 스테이지 정리
        ctx._stageLoader.UnloadStage(ctx);

        // 2) 다음 분기 판단
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
                    _sm.ChangeState(ctx, _stageLoad); // ChapterLoad를 따로 쓸 거면 거기로 전환
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

    public void Exit(GameFlowContext ctx)
    {

    }

    public void Tick(GameFlowContext ctx)
    {

    }
}
