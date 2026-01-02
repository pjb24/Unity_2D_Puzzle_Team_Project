// StageTransitionFx.cs
using System;
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class StageTransitionFx : MonoBehaviour
{
    [Header("Fade Overlay (optional)")]
    [SerializeField] private CanvasGroup _fadeOverlay;

    [Header("Slide (optional)")]
    [SerializeField] private float _slideDistance = 12f;

    [Header("Timing")]
    [SerializeField] private float _outDuration = 0.25f;
    [SerializeField] private float _inDuration = 0.25f;

    private bool _isPlaying;

    /// <summary>
    /// 코루틴 1개로 Out -> (midpoint) -> In 수행.
    /// midpoint는 "로딩 완료 시 continueAfterLoad 호출" 규칙만 지키면 된다.
    /// </summary>
    public void Play(
        GameFlowContext ctx,
        E_StageTransitionType type,
        Func<Action, bool> onMidpointAsync,
        Action onDone)
    {
        if (_isPlaying)
        {
            Debug.LogWarning("[StageTransitionFx] Play called while playing. Run without transition (fallback).");
            RunMidpointNoFx(onMidpointAsync, onDone);
            return;
        }

        StartCoroutine(CoPlay(ctx, type, onMidpointAsync, onDone));
    }

    private void RunMidpointNoFx(Func<Action, bool> onMidpointAsync, Action onDone)
    {
        bool done = false;

        if (onMidpointAsync == null)
        {
            Debug.LogWarning("[StageTransitionFx] onMidpointAsync is null. Continue immediately (fallback).");
            done = true;
        }
        else
        {
            bool started = onMidpointAsync.Invoke(() => done = true);
            if (!started)
                done = true;
        }

        if (done)
            onDone?.Invoke();
        else
            StartCoroutine(CoWait(() => done, onDone));
    }

    private IEnumerator CoWait(Func<bool> pred, Action onDone)
    {
        while (!pred()) yield return null;
        onDone?.Invoke();
    }

    private IEnumerator CoPlay(
        GameFlowContext ctx,
        E_StageTransitionType type,
        Func<Action, bool> onMidpointAsync,
        Action onDone)
    {
        _isPlaying = true;

        if (type == E_StageTransitionType.None)
        {
            yield return CoMidpoint(onMidpointAsync);
            onDone?.Invoke();
            _isPlaying = false;
            yield break;
        }

        // Out
        yield return CoOut(ctx, type);

        // Midpoint (Load)
        yield return CoMidpoint(onMidpointAsync);

        // In
        yield return CoIn(ctx, type);

        onDone?.Invoke();
        _isPlaying = false;
    }

    private IEnumerator CoMidpoint(Func<Action, bool> onMidpointAsync)
    {
        bool done = false;
        Action cont = () => done = true;

        if (onMidpointAsync == null)
        {
            Debug.LogWarning("[StageTransitionFx] onMidpointAsync is null. Continue immediately (fallback).");
            done = true;
        }
        else
        {
            bool started = onMidpointAsync.Invoke(cont);
            if (!started)
                done = true;
        }

        while (!done) yield return null;
    }

    private IEnumerator CoOut(GameFlowContext ctx, E_StageTransitionType type)
    {
        if (type == E_StageTransitionType.Fade)
        {
            if (_fadeOverlay == null)
            {
                Debug.LogWarning("[StageTransitionFx] Fade requested but _fadeOverlay is null. Skip (fallback).");
                yield break;
            }

            _fadeOverlay.gameObject.SetActive(true);
            _fadeOverlay.blocksRaycasts = true;
            yield return CoFade(0f, 1f, _outDuration);
            yield break;
        }

        if (type == E_StageTransitionType.Slide)
        {
            var target = GetSlideTarget(ctx);
            if (target == null)
            {
                Debug.LogWarning("[StageTransitionFx] Slide target missing. Skip (fallback).");
                yield break;
            }

            FadePath(ctx, false);

            Vector3 start = target.localPosition;
            Vector3 end = start + Vector3.left * _slideDistance;
            yield return CoMoveLocal(target, start, end, _outDuration);
        }
    }

    private IEnumerator CoIn(GameFlowContext ctx, E_StageTransitionType type)
    {
        if (type == E_StageTransitionType.Fade)
        {
            if (_fadeOverlay == null)
                yield break;

            yield return CoFade(1f, 0f, _inDuration);
            _fadeOverlay.blocksRaycasts = false;
            _fadeOverlay.gameObject.SetActive(false);

            FadePath(ctx, true);
            yield break;
        }

        if (type == E_StageTransitionType.Slide)
        {
            var target = GetSlideTarget(ctx);
            if (target == null)
            {
                Debug.LogWarning("[StageTransitionFx] Slide target missing (after). Skip (fallback).");
                yield break;
            }

            Vector3 end = target.localPosition;
            Vector3 start = end + Vector3.right * _slideDistance;

            target.localPosition = start;

            FadePath(ctx, true);

            yield return CoMoveLocal(target, start, end, _inDuration);
        }
    }

    private IEnumerator CoFade(float from, float to, float dur)
    {
        dur = Mathf.Max(0.01f, dur);

        float t = 0f;
        _fadeOverlay.alpha = from;

        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(t / dur);
            _fadeOverlay.alpha = Mathf.Lerp(from, to, a);
            yield return null;
        }

        _fadeOverlay.alpha = to;
    }

    private IEnumerator CoMoveLocal(Transform tr, Vector3 from, Vector3 to, float dur)
    {
        dur = Mathf.Max(0.01f, dur);

        float t = 0f;
        tr.localPosition = from;

        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(t / dur);
            tr.localPosition = Vector3.Lerp(from, to, a);
            yield return null;
        }

        tr.localPosition = to;
    }

    private Transform GetSlideTarget(GameFlowContext ctx)
    {
        if (ctx == null || ctx._stageRuntime == null)
            return null;

        if (ctx._stageRuntime._tilesRoot != null)
            return ctx._stageRuntime._tilesRoot;

        if (ctx._stageRuntime._root != null)
            return ctx._stageRuntime._root.transform;

        return null;
    }

    private void FadePath(GameFlowContext ctx, bool isIn)
    {
        if (ctx == null || ctx._stageRuntime == null || ctx._stageRuntime._pathRoot == null)
            return;

        var fx = ctx._stageRuntime._pathRoot.GetComponent<PathFadeFx>();
        if (fx == null)
        {
            Debug.LogWarning("[StageTransitionFx] PathFadeFx missing on pathRoot. Path fade skipped (fallback).");
            return;
        }

        if (isIn) fx.FadeIn();
        else fx.FadeOut();
    }
}
