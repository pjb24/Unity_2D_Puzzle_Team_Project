// PathFadeFx.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PathFadeFx : MonoBehaviour
{
    [SerializeField] private bool _autoFadeInOnStart = true;
    [SerializeField] private float _defaultDuration = 0.25f;

    private readonly List<Renderer> _renderers = new();
    private readonly List<int> _colorPropIds = new();
    private readonly List<Color> _baseColors = new();

    private MaterialPropertyBlock _mpb;
    private Coroutine _co;

    private void Awake()
    {
        _mpb = new MaterialPropertyBlock();

        _renderers.Clear();
        _colorPropIds.Clear();
        _baseColors.Clear();

        GetComponentsInChildren(true, _renderers);

        if (_renderers.Count == 0)
        {
            Debug.LogWarning("[PathFadeFx] No Renderer found under Path root. Fade disabled (fallback).");
            return;
        }

        for (int i = 0; i < _renderers.Count; i++)
        {
            var r = _renderers[i];
            if (r == null || r.sharedMaterial == null)
            {
                _colorPropIds.Add(0);
                _baseColors.Add(Color.white);
                continue;
            }

            var mat = r.sharedMaterial;

            int id = 0;
            if (mat.HasProperty("_BaseColor")) id = Shader.PropertyToID("_BaseColor");
            else if (mat.HasProperty("_Color")) id = Shader.PropertyToID("_Color");

            if (id == 0)
            {
                Debug.LogWarning("[PathFadeFx] Material has no _BaseColor/_Color. Alpha change skipped for some renderers (fallback).");
                _colorPropIds.Add(0);
                _baseColors.Add(Color.white);
                continue;
            }

            _colorPropIds.Add(id);
            _baseColors.Add(mat.GetColor(id));
        }

        if (_autoFadeInOnStart)
            SetAlphaImmediate(0f);
    }

    private void Start()
    {
        if (_autoFadeInOnStart)
            FadeIn(_defaultDuration);
    }

    public void FadeIn(float duration = -1f)
    {
        if (_renderers.Count == 0) return;
        if (duration <= 0f) duration = _defaultDuration;
        StartFade(1f, duration);
    }

    public void FadeOut(float duration = -1f)
    {
        if (_renderers.Count == 0) return;
        if (duration <= 0f) duration = _defaultDuration;
        StartFade(0f, duration);
    }

    private void StartFade(float to, float duration)
    {
        if (_co != null) StopCoroutine(_co);
        _co = StartCoroutine(CoFade(to, duration));
    }

    private IEnumerator CoFade(float to, float duration)
    {
        duration = Mathf.Max(0.01f, duration);

        float from = ReadAlphaFallback();
        float t = 0f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(t / duration);
            float v = Mathf.Lerp(from, to, a);
            SetAlphaImmediate(v);
            yield return null;
        }

        SetAlphaImmediate(to);
        _co = null;
    }

    private float ReadAlphaFallback()
    {
        // 프로토타입: 마지막 SetAlphaImmediate 기반으로만 운용.
        // 초기값은 Awake에서 0으로 세팅되므로 페이드 흐름에는 문제 없음.
        return 0f;
    }

    private void SetAlphaImmediate(float a)
    {
        a = Mathf.Clamp01(a);

        for (int i = 0; i < _renderers.Count; i++)
        {
            var r = _renderers[i];
            if (r == null) continue;

            int id = _colorPropIds[i];
            if (id == 0) continue;

            var baseC = _baseColors[i];
            baseC.a = a;

            r.GetPropertyBlock(_mpb);
            _mpb.SetColor(id, baseC);
            r.SetPropertyBlock(_mpb);
        }
    }
}
