///
/// 요구: Play → StageClear
/// 프로토타입 최소:
/// C 키(또는 UI 버튼)로 StageClear 강제 트리거
/// 나중에 퍼즐 목표 달성 이벤트로 교체
///

using UnityEngine;

public class GamePlayState : IGameFlowState
{
    private readonly GameFlowStateMachine _sm;
    private IGameFlowState _next;

    public GamePlayState(GameFlowStateMachine sm)
    {
        _sm = sm;
    }

    public void SetNext(IGameFlowState next)
    {
        _next = next;
    }

    public E_GameFlowState Id => E_GameFlowState.Play;

    public void Enter(GameFlowContext ctx)
    {
        // 입력 활성, 턴 시스템 시작 등 (지금은 생략)
    }

    public void Exit(GameFlowContext ctx)
    {
        // 입력 잠금, 턴 시스템 정지 등 (지금은 생략)
    }

    public void Tick(GameFlowContext ctx)
    {
        // 프로토타입: C키로 강제 클리어
        if (Input.GetKeyDown(KeyCode.C))
        {
            _sm.ChangeState(ctx, _next);
        }
    }
}
