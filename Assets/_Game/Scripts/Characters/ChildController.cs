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
using System.Collections.Generic;
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

    [Header("Move (Optional)")]
    [SerializeField] private bool _useLerp = true;
    [SerializeField] private float _lerpDuration = 0.12f;

    public void Initialize(ChildPathRuntime path, ChildPathBlockerRegistry blockers, int startPos = 0)
    {
        _path = path;
        _pathBlockers = blockers;

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

        Vector3 to = _path.Points[_pathPos];

        if (_useLerp)
        {
            if (_moveCo != null) StopCoroutine(_moveCo);
            _moveCo = StartCoroutine(CoMove(to, _lerpDuration, () =>
            {
                _moveCo = null;
                _onStepCompleted?.Invoke(false);
            }));
        }
        else
        {
            transform.position = to;
            _onStepCompleted?.Invoke(false);
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
