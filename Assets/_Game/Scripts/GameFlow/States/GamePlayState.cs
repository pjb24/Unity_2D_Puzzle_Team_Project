// GamePlayState.cs
///
/// 요구: Play → StageClear
/// 
/// 퍼즐 목표 달성 이벤트로 교체
/// 또는 C 키(또는 UI 버튼)로 StageClear 강제 트리거
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
    private DifficultyProfile _profile;

    public E_GameFlowState Id => E_GameFlowState.Play;

    private GameplayUIRoot _uiRoot;

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

        // DifficultyProfile 결정
        _profile = null;
        if (_ctx._gameConfig == null)
        {
            Debug.LogWarning("[GamePlayState] GameConfig is null. DifficultyProfile is null (fallback).");
            ctx.SetFailStreakLimit(0);
        }
        else
        {
            _profile = _ctx._gameConfig.GetProfile(_ctx._gameConfig.DefaultDifficulty);
            if (_profile == null)
            {
                Debug.LogWarning("[GamePlayState] DifficultyProfile not found. Using null profile (fallback).");
                ctx.SetFailStreakLimit(0);
            }
            else
            {
                ctx.SetFailStreakLimit(_profile.FailStreakToReturnChapterStart);
            }
        }

        // ===== (5) Stage Start Reset 고정 =====
        // StageLoad에서 "ClearAll + Capture(0)"를 이미 수행함
        // 폴백: 누락된 경우에만 여기서 보정
        EnsureStageCreatedSnapshot(snapshot);

        _rewind?.ResetForStageStart(_profile != null ? _profile.RewindMax : 0);

        int goalStep = (_ctx != null && _ctx._stageDefinition != null) ? _ctx._stageDefinition.ChildGoalPathStep : -1;

        _turnDriver.Bind(
            rt._fatherController,
            rt._childController,
            snapshot,
            router,
            _profile,
            rt._turnSystems,
            childGoalPathStep: goalStep);

        _uiRoot = Object.FindFirstObjectByType<GameplayUIRoot>();
        if (_uiRoot == null)
        {
            Debug.LogWarning("[GamePlayState] GameplayUIRoot not found (fallback).");
        }
        else
        {
            _uiRoot.Bind(_ctx, _turnDriver, _rewind, _profile);
        }

        // ExitPort 바인딩
        if (_rewind != null)
        {
            if (_stageLoad == null)
            {
                Debug.LogWarning("[GamePlayState] StageLoad state is null. Rewind exhaustion restart may fail (fallback).");
            }
            else
            {
                _rewind.BindExitPort(new RewindExitPort_GameFlow(_sm, _ctx, _stageLoad));
            }
        }

        // 구독 (Bind 이후에 수행: TurnDriver 존재/초기화 보장)
        _turnDriver.AddListenerOnResolved(OnTurnResolved);
    }

    private void EnsureStageCreatedSnapshot(TurnSnapshotRecorder snapshot)
    {
        if (snapshot == null)
            return;

        if (snapshot.Count > 0)
            return;

        Debug.LogWarning("[GamePlayState] Snapshot is empty on Enter. Fallback: capture stage-created snapshot now (turnIndex=0).");
        snapshot.ClearAll();
        snapshot.Capture(0);
    }

    public void Exit(GameFlowContext ctx)
    {
        _ctx = null;

        if (_uiRoot != null)
        {
            _uiRoot.Unbind();
            _uiRoot = null;
        }

        if (_turnDriver != null)
        {
            _turnDriver.RemoveListenerOnResolved(OnTurnResolved);
            _turnDriver.Unbind();
        }

        _turnDriver = null;
        _rewind = null;
        _profile = null;
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
                {
                    if (_next == null)
                    {
                        Debug.LogWarning("[GamePlayState] StageCleared but StageClear state is null. Fallback to StageLoad.");
                        ChangeToStageLoad();
                        return;
                    }
                    else
                    {
                        _ctx.ResetFailStreak();
                        _sm.ChangeState(_ctx, _next);
                    }
                    break;
                }

            case E_TurnResolveOutcome.StageFailed_Reset:
                {
                    HandleStageFailedReset();
                    break;
                }

            case E_TurnResolveOutcome.StageFailed_Rewind:
                {
                    // Normal: 자동 Rewind 진입
                    if (_rewind != null)
                    {
                        _rewind.EnterRewindDeferredFailureAuto();
                    }
                    else
                    {
                        // Rewind 시스템이 없으면 폴백
                        Debug.LogWarning("[GamePlayState] RewindController is null. Normal rewind will fallback to StageLoad.");
                        ChangeToStageLoad();
                    }
                    break;
                }

            case E_TurnResolveOutcome.Continue:
            default:
                break;
        }
    }

    private void HandleStageFailedReset()
    {
        // C) Hard + Ironman -> 즉시 1-1 복귀
        bool hardReset = _profile != null && _profile.HardResetStage;
        bool ironman = _ctx._gameConfig != null && _ctx._gameConfig.IronmanHardReturnToChapterStart;

        if (hardReset && ironman)
        {
            Debug.LogWarning("[GamePlayState] Hard fail with Ironman -> return to chapter start (1-1).");
            _ctx.ResetToChapterStart();
            ChangeToStageLoad();
            return;
        }

        // B) “스테이지 재시작” 누적 -> 임계 도달 시 챕터 복귀
        bool returnToChapterStart = _ctx.RecordFailAndShouldReturnChapterStart();
        if (returnToChapterStart)
        {
            Debug.LogWarning("[GamePlayState] FailStreak reached -> return to chapter start.");
            _ctx.ResetToChapterStart();
        }

        ChangeToStageLoad();
    }

    private void ChangeToStageLoad()
    {
        if (_stageLoad == null)
        {
            Debug.LogWarning("[GamePlayState] StageLoad state is null. Cannot change state (fallback).");
            return;
        }

        _sm.ChangeState(_ctx, _stageLoad);
    }
}
