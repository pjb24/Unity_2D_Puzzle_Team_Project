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

    private TurnDriver _turnDriver;

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
        // 1) TurnDriver 찾기 (Gameplay 씬에 존재)
        _turnDriver = Object.FindFirstObjectByType<TurnDriver>();
        if (_turnDriver == null)
        {
            Debug.LogError("[GamePlayState] TurnDriver not found in Gameplay scene.");
            return;
        }

        // 2) Router/Snapshot 찾기 (같은 씬 고정 배치)
        var router = Object.FindFirstObjectByType<TurnInputRouter>();
        var snapshot = Object.FindFirstObjectByType<TurnSnapshotRecorder>();

        // 3) StageRuntime에서 Father/Child 컨트롤러 꺼내서 바인딩
        var rt = ctx._stageRuntime;
        //if (rt == null || rt._fatherController == null || rt._childController == null)
        if (rt == null || rt._fatherController == null)
        {
            Debug.LogError("[GamePlayState] StageRuntimeRefs or controllers missing.");
            return;
        }

        _turnDriver.Bind(rt._fatherController, rt._childController, snapshot, router);

        // (기존 프로토타입 C키 클리어는 유지 가능)
    }

    public void Exit(GameFlowContext ctx)
    {
        if (_turnDriver != null)
            _turnDriver.Unbind();

        _turnDriver = null;
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
