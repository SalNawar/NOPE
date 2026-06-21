using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

/// <summary>
/// Makes a world-space sprite clickable. Requires a Collider2D on the same
/// object and a Physics2DRaycaster on the camera. Raises <see cref="onClick"/>
/// on a pointer click when <see cref="Interactable"/> is true. Designers wire
/// the response in the inspector; swap the sprite freely without touching code.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public sealed class Clickable : MonoBehaviour, IPointerClickHandler
{
    /// <summary>Raised on a click while interactable.</summary>
    public UnityEvent onClick = new UnityEvent();

    /// <summary>When false, clicks are ignored.</summary>
    [SerializeField] private bool interactable = true;

    /// <summary>Gets or sets whether clicks are accepted.</summary>
    public bool Interactable { get => interactable; set => interactable = value; }

    /// <summary>Handles a pointer click from the EventSystem.</summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (interactable)
            onClick.Invoke();
    }
}
