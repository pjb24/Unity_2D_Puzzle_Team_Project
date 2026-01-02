// ResultPopupView.cs
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class ResultPopupView : MonoBehaviour
{
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private TMP_Text _label;

    private bool _isShowing;
    private float _hideAt;

    private void Awake()
    {
        HideImmediate();
    }

    private void Update()
    {
        if (!_isShowing) return;

        if (Time.unscaledTime >= _hideAt)
            HideImmediate();
    }

    public void Show(string message, float durationSeconds)
    {
        if (_canvasGroup == null)
        {
            Debug.LogWarning("[ResultPopupView] CanvasGroup is null (fallback).");
            return;
        }

        if (_label != null) _label.text = message;

        _canvasGroup.alpha = 1f;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;

        _isShowing = true;
        _hideAt = Time.unscaledTime + Mathf.Max(0.01f, durationSeconds);
    }

    private void HideImmediate()
    {
        if (_canvasGroup == null) return;

        _canvasGroup.alpha = 0f;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;

        if (_label != null) _label.text = string.Empty;

        _isShowing = false;
        _hideAt = 0f;
    }
}
