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

    private IGameFlowState _stageLoad; // 실패 시 이동용

    private TurnDriver _turnDriver;
    private RewindController _rewind;

    private GameFlowContext _ctx;

    public E_GameFlowState Id => E_GameFlowState.Play;

    public GamePlayState(GameFlowStateMachine sm)
    {
        _sm = sm;
    }

    public void SetNext(IGameFlowState next)
    {
        _next = next;
    }

    public void SetStageLoad(IGameFlowState stageLoad)
    {
        _stageLoad = stageLoad;
    }

    public void Enter(GameFlowContext ctx)
    {
        _ctx = ctx;

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
        _rewind = Object.FindFirstObjectByType<RewindController>();

        if (router == null) Debug.LogWarning("[GamePlayState] TurnInputRouter not found.");
        if (snapshot == null) Debug.LogWarning("[GamePlayState] TurnSnapshotRecorder not found.");
        if (_rewind == null) Debug.LogWarning("[GamePlayState] RewindController not found. StageFailed_Rewind will fallback to StageLoad.");

        // 3) StageRuntime에서 Father/Child 컨트롤러 꺼내서 바인딩
        var rt = ctx._stageRuntime;
        if (rt == null || rt._fatherController == null || rt._childController == null)
        {
            Debug.LogError("[GamePlayState] StageRuntimeRefs or controllers missing.");
            return;
        }

        // DifficultyProfile 결정(프로토타입: DefaultDifficulty 사용)
        var cfg = ctx._gameConfig;
        var profile = (cfg != null) ? cfg.GetProfile(cfg.DefaultDifficulty) : null;
        if (profile == null)
            Debug.LogWarning("[GamePlayState] DifficultyProfile is null. Resolve outcome may be wrong.");

        _turnDriver.Bind(rt._fatherController, rt._childController, snapshot, router, profile);

        // ExitPort 바인딩
        if (_rewind != null)
            _rewind.BindExitPort(new RewindExitPort_GameFlow(_sm, ctx, _stageLoad));

        // 구독 (Bind 이후에 수행: TurnDriver 존재/초기화 보장)
        _turnDriver.AddListenerOnResolved(OnTurnResolved);
    }

    public void Exit(GameFlowContext ctx)
    {
        _ctx = null;

        if (_turnDriver != null)
        {
            _turnDriver.RemoveListenerOnResolved(OnTurnResolved);
            _turnDriver.Unbind();
        }

        _turnDriver = null;
        _rewind = null;
    }

    public void Tick(GameFlowContext ctx)
    {
        // 프로토타입: C키로 강제 클리어
        if (Input.GetKeyDown(KeyCode.C))
        {
            _sm.ChangeState(ctx, _next);
        }
    }

    private void OnTurnResolved(E_TurnResolveOutcome outcome, E_StageFailReason reason, int turnIndex)
    {
        if (_ctx == null) return;

        switch (outcome)
        {
            case E_TurnResolveOutcome.StageCleared:
                _sm.ChangeState(_ctx, _next);
                break;

            case E_TurnResolveOutcome.StageFailed_Reset:
                // Hard: 즉시 리셋(= StageLoad로 이동)
                _sm.ChangeState(_ctx, _stageLoad);
                break;

            case E_TurnResolveOutcome.StageFailed_Rewind:
                // Normal: 자동 Rewind 진입
                if (_rewind != null)
                {
                    _rewind.EnterRewind(E_RewindEnterSource.FailureAuto);
                }
                else
                {
                    // Rewind 시스템이 없으면 폴백
                    Debug.LogWarning("[GamePlayState] RewindController is null. Normal rewind will fallback to StageLoad.");
                    _sm.ChangeState(_ctx, _stageLoad);
                }
                break;

            case E_TurnResolveOutcome.Continue:
            default:
                break;
        }
    }
}
