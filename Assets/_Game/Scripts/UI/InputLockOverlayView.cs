// InputLockOverlayView.cs
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class InputLockOverlayView : MonoBehaviour
{
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private TMP_Text _label;

    public void Refresh(bool locked)
    {
        if (_canvasGroup == null)
        {
            Debug.LogWarning("[InputLockOverlayView] CanvasGroup is null (fallback).");
            return;
        }

        if (_label != null)
            _label.text = locked ? "INPUT LOCKED" : string.Empty;

        _canvasGroup.alpha = locked ? 1f : 0f;
        _canvasGroup.interactable = locked;
        _canvasGroup.blocksRaycasts = locked;
    }
}
