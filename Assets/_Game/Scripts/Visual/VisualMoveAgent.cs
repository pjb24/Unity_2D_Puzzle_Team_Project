// VisualMoveAgent.cs
using System;
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class VisualMoveAgent : MonoBehaviour
{
    private Coroutine _co;

    public bool IsMoving => _co != null;

    public void StopMove()
    {
        if (_co == null)
            return;

        StopCoroutine(_co);
        _co = null;
    }

    public void MoveTo(Vector3 to, float duration, Action onDone = null)
    {
        StopMove();

        if (duration <= 0f)
        {
            transform.position = to;
            onDone?.Invoke();
            return;
        }

        _co = StartCoroutine(CoMove(to, duration, onDone));
    }

    private IEnumerator CoMove(Vector3 to, float duration, Action onDone)
    {
        Vector3 from = transform.position;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / duration);
            transform.position = Vector3.Lerp(from, to, u);
            yield return null;
        }

        transform.position = to;
        _co = null;
        onDone?.Invoke();
    }
}
