using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Drags a desk object across a DeskSurface with the EventSystem (its
/// Collider2D proxy is found by the Physics2DRaycaster): the pointer is
/// projected onto the desk plane and the object's centre stays inside the desk
/// rectangle. The proxy is off during the drag, so the release is never over
/// the object itself (no click follows a drag) and the drop is decided by the
/// projected pointer (DragEnded). While disabled by its owner the EventSystem
/// sends it nothing. Generic: papers use it now, decoration later.
/// </summary>
public sealed class DeskDraggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    /// <summary>The collider the raycaster finds (off while dragged).</summary>
    [SerializeField] private Collider2D proxy;

    private DeskSurface _surface;
    private Vector3 _grabOffset;

    /// <summary>Raised when a drag starts.</summary>
    public event Action<DeskDraggable> DragBegan;

    /// <summary>Raised when a drag ends, with the pointer projected onto the desk (or the object's position when the projection fails).</summary>
    public event Action<DeskDraggable, Vector3> DragEnded;

    /// <summary>Where the object was when the last drag started.</summary>
    public Vector3 PickUpPosition { get; private set; }

    /// <summary>Sets the desk this object moves on.</summary>
    public void Init(DeskSurface surface) => _surface = surface;

    /// <summary>Records the pick-up position and the grab offset, and turns the proxy off.</summary>
    public void OnBeginDrag(PointerEventData eventData)
    {
        PickUpPosition = transform.position;
        _grabOffset = Vector3.zero;
        if (_surface != null && _surface.TryProject(eventData.pressEventCamera, eventData.position, out Vector3 point))
            _grabOffset = transform.position - point;

        if (proxy != null)
            proxy.enabled = false;
        DragBegan?.Invoke(this);
    }

    /// <summary>Follows the projected pointer, the centre clamped to the desk.</summary>
    public void OnDrag(PointerEventData eventData)
    {
        if (_surface != null && _surface.TryProject(eventData.pressEventCamera, eventData.position, out Vector3 point))
            transform.position = _surface.Clamp(point + _grabOffset);
    }

    /// <summary>Turns the proxy back on and reports where the pointer was released.</summary>
    public void OnEndDrag(PointerEventData eventData)
    {
        if (proxy != null)
            proxy.enabled = true;

        Vector3 released = transform.position;
        if (_surface != null && _surface.TryProject(eventData.pressEventCamera, eventData.position, out Vector3 point))
            released = point;
        DragEnded?.Invoke(this, released);
    }
}
