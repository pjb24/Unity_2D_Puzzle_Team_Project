// RewindController.cs
///
/// Enter: 최신 스냅샷을 기준점으로 Restore + 커서 세팅 + 입력버퍼 클리어
/// Prev: 커서 - 1 → Restore
/// Commit: 되감기 종료 + 횟수 1 소모 + 입력버퍼 클리어
/// Next도 같이 넣어둠(라우터가 이미 갖고 있어서)
///
using UnityEngine;
using System.Collections;

public enum E_RewindEnterSource
{
    Player = 0,
    FailureAuto = 1,
}

[DisallowMultipleComponent]
public class RewindController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private TurnSnapshotRecorder _recorder;
    [SerializeField] private TurnDriver _turnDriver;

    [Header("Stage Runtime")]
    [SerializeField] private Transform _stageRoot;
    [SerializeField] private InteractRegistry _interactRegistry;

    [Header("Settings")]
    [SerializeField, Min(0)] private int _rewindMax = 10;

    private IRewindExitPort _exitPort;

    private bool _isRewindActive;

    private int _cursorIndex = -1;
    private int _enterIndex = -1; // 진입 시점 인덱스

    private int _rewindRemaining;

    public bool IsRewindActive => _isRewindActive;
    public int RewindRemaining => _rewindRemaining;

    private Coroutine _deferredEnterCo;

    private void Reset()
    {
        _recorder = FindAnyObjectByType<TurnSnapshotRecorder>();
        _turnDriver = FindAnyObjectByType<TurnDriver>();
    }

    private void Awake()
    {
        _rewindRemaining = _rewindMax;
    }

    public void BindExitPort(IRewindExitPort exitPort)
    {
        _exitPort = exitPort;
    }

    public void BindStageRuntime(Transform stageRoot, InteractRegistry registry, TurnSnapshotRecorder recorder = null)
    {
        _stageRoot = stageRoot;
        _interactRegistry = registry;

        if (recorder != null)
            _recorder = recorder;

        if (_recorder != null)
            _recorder.BindStageRoot(_stageRoot);
        else
            Debug.LogWarning("[RewindController] BindStageRuntime fallback: recorder is null.");
    }

    /// <summary>
    /// 스테이지 시작/재시작 시 호출 권장.
    /// - rewindMax: 난이도 프로필 값
    /// - resetFailStreak: 챕터 복귀 후 true 권장
    /// </summary>
    public void ResetForStageStart(int rewindMax)
    {
        _rewindMax = Mathf.Max(0, rewindMax);
        _rewindRemaining = _rewindMax;

        _isRewindActive = false;
        _cursorIndex = -1;
        _enterIndex = -1;
    }

    public void EnterRewind(E_RewindEnterSource source)
    {
        if (_recorder == null)
        {
            Debug.LogWarning("[RewindController] EnterRewind fallback: recorder is null.");
            return;
        }

        if (_recorder.Count <= 0)
        {
            Debug.LogWarning("[RewindController] EnterRewind fallback: no snapshots.");
            return;
        }

        // remaining=0 처리 정책
        if (_rewindRemaining <= 0)
        {
            if (source == E_RewindEnterSource.FailureAuto)
            {
                // ChildBlocked 등 실패로 들어오려는 경우: 즉시 재시작
                if (_exitPort == null)
                {
                    Debug.LogWarning("[RewindController] FailureAuto: remaining=0 but ExitPort is null. Cannot restart stage.");
                    return;
                }

                Debug.Log("[RewindController] FailureAuto: remaining=0 -> restart stage.");
                _exitPort.RequestRestartStage();
                return;
            }

            // 플레이어 능동 진입: 재시작 금지, 로그만
            Debug.Log("[RewindController] Player EnterRewind denied: remaining=0 (rewind not available).");
            return;
        }

        _isRewindActive = true;

        _cursorIndex = _recorder.LatestIndex; // 0개면 -1 이지만 위에서 방지됨
        _enterIndex = _cursorIndex; // 진입 시점 고정

        // 안전하게 최신 상태를 복원(되감기 UI 기준점)
        RestoreAndSync(_cursorIndex);

        ClearTurnInputBuffer();
        Debug.Log($"[RewindController] EnterRewind cursor={_cursorIndex} remaining={_rewindRemaining}");
    }

    public void RequestPrevTurn()
    {
        if (!_isRewindActive) return;
        if (_recorder == null) return;
        if (_recorder.Count <= 0) return;

        int prev = _cursorIndex - 1;
        if (prev < 0) prev = 0;
        if (prev == _cursorIndex) return;

        _cursorIndex = prev;

        RestoreAndSync(_cursorIndex);

        ClearTurnInputBuffer();
        Debug.Log($"[RewindController] Prev cursor={_cursorIndex}");
    }

    public void RequestNextTurn()
    {
        if (!_isRewindActive) return;
        if (_recorder == null) return;
        if (_recorder.Count <= 0) return;

        int max = _recorder.LatestIndex;
        int next = _cursorIndex + 1;
        if (next > max) next = max;
        if (next == _cursorIndex) return;

        _cursorIndex = next;

        RestoreAndSync(_cursorIndex);

        ClearTurnInputBuffer();
        Debug.Log($"[RewindController] Next cursor={_cursorIndex}");
    }

    public void RequestCommit()
    {
        if (!_isRewindActive) return;
        if (_recorder == null)
        {
            Debug.LogWarning("[RewindController] Commit fallback: recorder is null. Cannot discard snapshots.");
            return;
        }

        if (_cursorIndex < 0 || _cursorIndex > _recorder.LatestIndex)
        {
            Debug.LogWarning($"[RewindController] Commit fallback: invalid cursor. cursor={_cursorIndex}");
            return;
        }

        // === Commit 시점에 “미래” 스냅샷 삭제 ===
        _recorder.DiscardAfterIndex(_cursorIndex);

        _isRewindActive = false;

        _rewindRemaining = Mathf.Max(0, _rewindRemaining - 1);

        ClearTurnInputBuffer();
        Debug.Log($"[RewindController] Commit cursor={_cursorIndex} remaining={_rewindRemaining}");
    }

    public void RequestCancel()
    {
        if (!_isRewindActive) return;
        if (_recorder == null) return;

        if (_enterIndex < 0 || _enterIndex > _recorder.LatestIndex)
        {
            Debug.LogWarning($"[RewindController] Cancel fallback: invalid enterIndex. enterIndex={_enterIndex}");
            return;
        }

        // 진입 시점 상태로 복귀

        _cursorIndex = _enterIndex;
        RestoreAndSync(_cursorIndex);

        _isRewindActive = false;

        ClearTurnInputBuffer();
        Debug.Log("[RewindController] Cancel -> restored enter snapshot and exit rewind.");
    }

    private void RestoreAndSync(int index)
    {
        if (_recorder == null)
        {
            Debug.LogWarning("[RewindController] RestoreAndSync fallback: recorder is null.");
            return;
        }

        if (_stageRoot == null)
            Debug.LogWarning("[RewindController] RestoreAndSync fallback: stageRoot is null. (BindStageRuntime is recommended)");

        _recorder.BindStageRoot(_stageRoot);

        var snap = _recorder.GetAt(index);
        if (snap == null)
        {
            Debug.LogWarning($"[RewindController] RestoreAndSync fallback: snapshot is null. index={index}");
            return;
        }

        _recorder.Restore(snap);

        // 복원 후 1회 리빌드(스테이지 루트 범위만)
        if (_interactRegistry != null && _stageRoot != null)
            _interactRegistry.RebuildFromRoot(_stageRoot);
        else
            Debug.LogWarning("[RewindController] InteractRegistry rebuild skipped (fallback): registry/root is null.");

        if (_turnDriver == null)
        {
            Debug.LogWarning("[RewindController] RestoreAndSync fallback: turnDriver is null.");
            return;
        }

        _turnDriver.SyncTurnIndexFromSnapshot(snap._turnIndex);
    }

    private void ClearTurnInputBuffer()
    {
        if (_turnDriver == null) return;

        _turnDriver.ClearInputBuffer();
    }

    public void EnterRewindDeferredFailureAuto()
    {
        if (_deferredEnterCo != null)
        {
            StopCoroutine(_deferredEnterCo);
            _deferredEnterCo = null;
        }

        _deferredEnterCo = StartCoroutine(CoEnterRewindDeferredFailureAuto());
    }

    private IEnumerator CoEnterRewindDeferredFailureAuto()
    {
        // “막힘 비주얼”이 실제로 화면에 반영된 뒤 진입
        yield return new WaitForEndOfFrame();

        EnterRewind(E_RewindEnterSource.FailureAuto);
        _deferredEnterCo = null;
    }
}
