// ChildController.BlockedFeedback.cs
using System.Collections;
using UnityEngine;

public partial class ChildController : MonoBehaviour
{
    [Header("Blocked Feedback (Easy)")]
    [SerializeField] private float _blockedBounceDistance = 0.08f;
    [SerializeField] private float _blockedBounceDuration = 0.08f;

    private Coroutine _blockedFeedbackCo;

    public void RequestBlockedFeedback()
    {
        if (!isActiveAndEnabled)
        {
            Debug.LogWarning("[ChildController] Blocked feedback skipped: component is not active/enabled (fallback).");
            return;
        }

        if (_blockedBounceDistance <= 0f || _blockedBounceDuration <= 0f)
        {
            Debug.LogWarning(
                $"[ChildController] Blocked feedback skipped: invalid config distance={_blockedBounceDistance}, duration={_blockedBounceDuration} (fallback).");
            return;
        }

        Vector3 tangentDir = GetTangentDirOrFallback();

        if (_blockedFeedbackCo != null)
            StopCoroutine(_blockedFeedbackCo);

        _blockedFeedbackCo = StartCoroutine(CoBlockedBounce(tangentDir));
    }

    private Vector3 GetTangentDirOrFallback()
    {
        if (_path == null || _path.Count <= 1)
        {
            Debug.LogWarning("[ChildController] Blocked feedback dir fallback: path is null/too short. Using Vector3.up.");
            return Vector3.up;
        }

        int cur = Mathf.Clamp(_pathPos, 0, _path.Count - 1);
        int next = cur + 1;
        if (next >= _path.Count) next = 0;

        Vector3 from = _path.Points[cur];
        Vector3 to = _path.Points[next];

        Vector3 dir = to - from;

        // tangent이 0이면 역방향으로 한번 더 시도
        if (dir.sqrMagnitude < 1e-6f)
        {
            int prev = cur - 1;
            if (prev < 0) prev = _path.Count - 1;

            dir = from - _path.Points[prev];
        }

        if (dir.sqrMagnitude < 1e-6f)
        {
            Debug.LogWarning("[ChildController] Blocked feedback dir fallback: tangent is zero. Using Vector3.up.");
            return Vector3.up;
        }

        return dir.normalized;
    }

    private IEnumerator CoBlockedBounce(Vector3 tangentDir)
    {
        Vector3 origin = transform.position;

        float half = _blockedBounceDuration * 0.5f;
        if (half <= 0f)
        {
            Debug.LogWarning("[ChildController] Blocked feedback skipped: duration too small (fallback).");
            _blockedFeedbackCo = null;
            yield break;
        }

        // 진행 방향(tangent) 반대로 살짝 밀렸다가 복귀
        Vector3 outPos = origin - tangentDir * _blockedBounceDistance;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / half;
            transform.position = Vector3.Lerp(origin, outPos, Mathf.Clamp01(t));
            yield return null;
        }

        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / half;
            transform.position = Vector3.Lerp(outPos, origin, Mathf.Clamp01(t));
            yield return null;
        }

        transform.position = origin;
        _blockedFeedbackCo = null;
    }
}
