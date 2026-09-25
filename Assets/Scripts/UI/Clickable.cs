using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

/// <summary>
/// Makes an office object clickable: a collider on it or on a child (on the
/// Interactable layer, found by the office camera's PhysicsRaycaster) raises
/// <see cref="onClick"/> on a pointer click while <see cref="Interactable"/>
/// is true. The hover outline draws <see cref="Outline"/>'s meshes (the
/// paper's sheet; for an art prop the office binder hands over the prop's own
/// renderers), or nothing but the hand cursor when the list is empty (a hit
/// zone). Designers wire the response in the inspector.
/// </summary>
public sealed class Clickable : MonoBehaviour, IPointerClickHandler
{
    /// <summary>Raised on a click while interactable.</summary>
    public UnityEvent onClick = new UnityEvent();

    /// <summary>When false, clicks are ignored.</summary>
    [SerializeField] private bool interactable = true;

    /// <summary>The renderers the hover outline draws (empty: the hand cursor only).</summary>
    [SerializeField] private Renderer[] outline = System.Array.Empty<Renderer>();

    /// <summary>Gets or sets whether clicks are accepted.</summary>
    public bool Interactable { get => interactable; set => interactable = value; }

    /// <summary>The renderers the hover outline draws.</summary>
    public IReadOnlyList<Renderer> Outline => outline ?? System.Array.Empty<Renderer>();

    /// <summary>Sets what the hover outline draws (the office binder: an art prop's renderers).</summary>
    public void SetOutline(Renderer[] renderers) => outline = renderers ?? System.Array.Empty<Renderer>();

    /// <summary>Handles a pointer click from the EventSystem.</summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (interactable)
            onClick.Invoke();
    }
}
