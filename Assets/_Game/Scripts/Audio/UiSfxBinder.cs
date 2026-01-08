// UiSfxBinder.cs
// Hover/Click SFX. 버튼 비활성(interactable=false)이면 재생 금지.
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class UiSfxBinder : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    private Selectable _selectable;

    private void Awake()
    {
        _selectable = GetComponent<Selectable>();
        if (_selectable == null)
            Debug.LogWarning("[UiSfxBinder] Selectable not found. This binder should be attached to a UI Selectable.");
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!IsInteractableNow()) return;
        AudioHub.Ensure().PlaySfx(E_SfxId.UI_Hover);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!IsInteractableNow()) return;
        AudioHub.Ensure().PlaySfx(E_SfxId.UI_Click);
    }

    private bool IsInteractableNow()
    {
        if (!isActiveAndEnabled) return false;

        if (_selectable == null)
            return false;

        if (!_selectable.IsActive() || !_selectable.IsInteractable())
            return false;

        return true;
    }
}
