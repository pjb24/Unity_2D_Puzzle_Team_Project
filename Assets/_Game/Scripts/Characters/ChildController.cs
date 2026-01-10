// ChildController.cs
///
/// 핵심 규칙
/// _pathPos: 현재 위치(스텝 인덱스)
/// 다음 스텝: next = _pathPos + 1
/// next가 Blocked면 막힘(인덱스 유지)
/// 성공이면 _pathPos = next 후 위치 갱신(스냅 또는 코루틴 Lerp)
///
using System;
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public partial class ChildController : MonoBehaviour
{
    private event Action<bool> _onStepCompleted; // bool = blocked
    public void AddListenerOnStepCompleted(Action<bool> cb) => _onStepCompleted += cb;
    public void RemoveListenerOnStepCompleted(Action<bool> cb) => _onStepCompleted -= cb;

    public int PathPos => _pathPos;
    public bool LastStepBlocked => _lastStepBlocked;

    private ChildPathRuntime _path;
    private ChildPathBlockerRegistry _pathBlockers;

    private int _pathPos;
    private bool _lastStepBlocked;

    private Coroutine _moveCo;

    [Header("Move FX (Lerp)")]
    [SerializeField] private bool _useLerp = true;
    [SerializeField] private float _lerpDuration = 0.12f;

    // ===== Move Animation (Optional) =====
    private ChildAnimDriver _animDriver;
    public void BindAnimDriver(ChildAnimDriver driver) => _animDriver = driver;
    public void UnbindAnimDriver() => _animDriver = null;

    private VisualMoveAgent _visualMove;
    private bool _useRewindRestoreLerp;
    private float _rewindRestoreMoveDuration;

    private bool _warnedRestoreMissingMoveAgent;
    private bool _warnedRestoreInvalidDuration;

    public void BindVisualMoveAgent(VisualMoveAgent agent) => _visualMove = agent;
    public void UnbindVisualMoveAgent() => _visualMove = null;

    public E_ChildBlockedCause LastBlockedCause { get; private set; } = E_ChildBlockedCause.None;

    public void Initialize(
        ChildPathRuntime path,
        ChildPathBlockerRegistry blockers,
        int startPos = 0,
        bool useRewindRestoreLerp = false,
        float rewindRestoreMoveDuration = 0f)
    {
        _path = path;
        _pathBlockers = blockers;

        _useRewindRestoreLerp = useRewindRestoreLerp;
        _rewindRestoreMoveDuration = rewindRestoreMoveDuration;

        if (_animDriver == null)
            _animDriver = GetComponent<ChildAnimDriver>();

        int count = _path?.Count ?? 0;
        if (_pathBlockers == null)
        {
            Debug.LogWarning("[ChildController] Initialize fallback: blockers is null. Child will never be blocked by path steps.");
        }
        else if (_pathBlockers.PathCount != count)
        {
            Debug.LogWarning($"[ChildController] Initialize fallback: blockers.PathCount mismatch. blockers={_pathBlockers.PathCount} path={count}");
        }

        _pathPos = Mathf.Clamp(startPos, 0, (count <= 0 ? 0 : count - 1));
        _lastStepBlocked = false;

        if (_path != null && _path.Count > 0)
        {
            StopMoveFxIfAny();

            transform.position = _path.Points[_pathPos];

            if (_path.Count >= 2)
            {
                int next = _pathPos + 1;
                if (next >= _path.Count) next = 0;
                UpdateFacingByNextStepWorld(_path.Points[_pathPos], _path.Points[next]);
            }
            else
            {
                ApplyFacingVisual();
            }
        }
    }

    public void RequestStep()
    {
        if (_path == null || _path.Count <= 0)
        {
            _lastStepBlocked = true;
            _onStepCompleted?.Invoke(true);
            return;
        }

        int next = _pathPos + 1;
        if (next >= _path.Count)
        {
            next = 0; // 루프: 끝이면 처음으로
        }

        // 특정 인덱스가 Blocked면 막힘
        if (_pathBlockers != null && _pathBlockers.IsBlocked(next))
        {
            Debug.Log("[ChildController] Blocked by ChildPathBlockerRegistry");
            _lastStepBlocked = true;
            _onStepCompleted?.Invoke(true);
            return;
        }

        // 코너 포함: “다음 이동 벡터”로 Facing 선 갱신
        Vector3 from = _path.Points[_pathPos];
        Vector3 toNext = _path.Points[next];
        UpdateFacingByNextStepWorld(from, toNext);

        // 성공: 인덱스 갱신 + 이동
        _pathPos = next;
        _lastStepBlocked = false;

        // ===== 연출 이동(애니 + Lerp) =====
        // 이동 성공 시에만 이동 애니메이션 1회 재생
        _animDriver?.PlayMove();

        Vector3 to = _path.Points[_pathPos];

        StartMoveFx(
            toWorld: to,
            onDone: () => _onStepCompleted?.Invoke(false));
    }

    private void StartMoveFx(Vector3 toWorld, Action onDone)
    {
        StopMoveFxIfAny();

        if (!_useLerp)
        {
            transform.position = toWorld;
            onDone?.Invoke();
            return;
        }

        if (_lerpDuration <= 0f)
        {
            Debug.LogWarning($"[ChildController] MoveFX fallback: invalid duration({_lerpDuration}). (snap)");
            transform.position = toWorld;
            onDone?.Invoke();
            return;
        }

        _moveCo = StartCoroutine(CoMove(toWorld, _lerpDuration, () =>
        {
            _moveCo = null;
            onDone?.Invoke();
        }));
    }

    private void StopMoveFxIfAny()
    {
        if (_moveCo != null)
        {
            StopCoroutine(_moveCo);
            _moveCo = null;
        }
    }

    private IEnumerator CoMove(Vector3 to, float dur, Action onDone)
    {
        Vector3 from = transform.position;
        if (dur <= 0f)
        {
            transform.position = to;
            onDone?.Invoke();
            yield break;
        }

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / dur;
            transform.position = Vector3.Lerp(from, to, Mathf.Clamp01(t));
            yield return null;
        }

        transform.position = to;
        onDone?.Invoke();
    }
}
