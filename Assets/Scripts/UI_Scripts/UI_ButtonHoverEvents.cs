using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Adds hover (and optional keyboard/controller select) UnityEvents to a UI element,
/// since Unity's Button only exposes OnClick.
///
/// Put this on the Button (or any UI object that is a raycast target) and wire the
/// events - e.g. onHoverEnter -> AudioManager.PlayButtonPress, or a scale/tween.
///
/// Requires an EventSystem in the scene (Buttons already need one).
/// </summary>
public class UI_ButtonHoverEvents : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    ISelectHandler, IDeselectHandler
{
    [Header("Mouse hover")]
    public UnityEvent onHoverEnter;
    public UnityEvent onHoverExit;

    [Header("Keyboard / controller selection (optional)")]
    [Tooltip("Fires when this element becomes the selected object (gamepad/keyboard navigation). Hover does NOT fire for those inputs.")]
    public UnityEvent onSelected;
    public UnityEvent onDeselected;

    [Header("Options")]
    [Tooltip("Ignore hover/select events while the Button/Selectable is not interactable.")]
    [SerializeField] private bool respectInteractable = true;

    [Tooltip("Optional. Auto-filled from this GameObject; used only for the interactable check.")]
    [SerializeField] private Selectable selectable;

    private void Awake()
    {
        if (selectable == null)
        {
            selectable = GetComponent<Selectable>();
        }
    }

    private bool Blocked()
    {
        return respectInteractable && selectable != null && !selectable.interactable;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (Blocked()) return;
        onHoverEnter?.Invoke();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (Blocked()) return;
        onHoverExit?.Invoke();
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (Blocked()) return;
        onSelected?.Invoke();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        onDeselected?.Invoke();
    }
}
