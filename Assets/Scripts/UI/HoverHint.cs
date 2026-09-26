using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// A small tooltip under an overlay control, shown while the pointer is over
/// it (the desk view's "▲ Back": "Esc / right-click", the other ways back).
/// The hint takes no raycasts; it hides when the control hides.
/// </summary>
public sealed class HoverHint : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    /// <summary>The hint shown while hovered (a child of this control, inactive otherwise).</summary>
    [SerializeField] private GameObject hint;

    private void Awake() => Show(false);

    private void OnDisable() => Show(false);

    /// <summary>The pointer is over the control: the hint shows.</summary>
    public void OnPointerEnter(PointerEventData eventData) => Show(true);

    /// <summary>The pointer left the control: the hint hides.</summary>
    public void OnPointerExit(PointerEventData eventData) => Show(false);

    private void Show(bool on)
    {
        if (hint != null && hint.activeSelf != on)
            hint.SetActive(on);
    }
}
