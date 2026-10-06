using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Drags a desk object across a DeskSurface with the EventSystem (its collider
/// proxy is found by the office camera's PhysicsRaycaster): the pointer is
/// projected onto the desk plane and the object's centre stays inside the desk
/// area. The proxy is off during the drag, so the release is never over the
/// object itself (no click follows a drag) and the drop is decided by the
/// projected pointer (DragEnded). While disabled by its owner the EventSystem
/// sends it nothing, so disabling it mid-drag ends the drag at once
/// (DragCancelled; audit R5-002: the drag used to stay open for good, the proxy
/// off); the moves and the release of that press are ignored if it is enabled
/// again before the button comes up. While its owner takes it out of the
/// raycast (SetRaycastable) the proxy is off too, so clicks reach what lies
/// under it. Only the left button drags (Papers, Please's one mouse button:
/// left-press and drag moves a document; a right-click backs out, and
/// mid-drag it cancels the drag: Cancel, ControlRules). Generic: the papers
/// and the rulebook use it on the desk plane, the stamps carried a height
/// above it (Init's height: Saleh 2026-10-06, "the stamp should be two stamps
/// that I physically move").
/// </summary>
public sealed class DeskDraggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    /// <summary>The collider the raycaster finds (off while dragged, and while out of the raycast).</summary>
    [SerializeField] private Collider proxy;

    private DeskSurface _surface;
    private float _height;
    private Vector3 _grabOffset;
    private bool _dragging;
    private bool _raycastable = true;

    /// <summary>Raised when a drag starts.</summary>
    public event Action<DeskDraggable> DragBegan;

    /// <summary>Raised when a drag ends, with the pointer projected onto the desk (or the object's position when the projection fails).</summary>
    public event Action<DeskDraggable, Vector3> DragEnded;

    /// <summary>Raised when a drag is cut short: the object was disabled mid-drag (its owner took its input away, or it is being destroyed), or the drag was cancelled (a right-click or Esc), so no release will come.</summary>
    public event Action<DeskDraggable> DragCancelled;

    /// <summary>Raised as the object follows the pointer, with the pointer projected onto the desk (the desk sizes a document by the zone it is over).</summary>
    public event Action<DeskDraggable, Vector3> Dragged;

    /// <summary>True while a drag runs.</summary>
    public bool IsDragging => _dragging;

    /// <summary>Where the object was when the last drag started, or when RememberPosition last took note.</summary>
    public Vector3 PickUpPosition { get; private set; }

    /// <summary>Takes note of where the object lies now as its pick-up point: the scanner feeding itself a paper (the Auto-Feed Scanner) sends it back here after the scan, as a dragged paper goes back to where it was picked up.</summary>
    public void RememberPosition() => PickUpPosition = transform.position;

    /// <summary>Sets the desk this object moves on, and how far above its plane the object's origin is carried (<paramref name="height"/>, metres: 0 for a paper or the rulebook, which lie on it; a stamp's foot hangs over the papers).</summary>
    public void Init(DeskSurface surface, float height = 0f)
    {
        _surface = surface;
        _height = height;
    }

    /// <summary>
    /// Puts the object in the raycast or takes it out (DeskDocument: a paper
    /// the office has put away, so a click on it reaches what lies under it).
    /// The proxy stays off during a drag either way, and follows this when the
    /// drag ends.
    /// </summary>
    public void SetRaycastable(bool raycastable)
    {
        _raycastable = raycastable;
        if (proxy != null && !_dragging)
            proxy.enabled = raycastable;
    }

    /// <summary>Records the pick-up position and the grab offset, and turns the proxy off (the left button only).</summary>
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;
        _dragging = true;
        PickUpPosition = transform.position;
        _grabOffset = Vector3.zero;
        if (_surface != null && _surface.TryProject(eventData.pressEventCamera, eventData.position, out Vector3 point))
            _grabOffset = Vector3.ProjectOnPlane(transform.position - point, _surface.transform.up);
        if (proxy != null)
            proxy.enabled = false;
        DragBegan?.Invoke(this);
    }

    /// <summary>Follows the projected pointer, the centre clamped to the desk and carried at its height above it (nothing once the drag was cut short).</summary>
    public void OnDrag(PointerEventData eventData)
    {
        if (!_dragging)
            return;
        if (_surface != null && _surface.TryProject(eventData.pressEventCamera, eventData.position, out Vector3 point))
        {
            transform.position = _surface.Clamp(point + _grabOffset) + _surface.transform.up * _height;
            Dragged?.Invoke(this, point);
        }
    }

    /// <summary>Turns the proxy back on (unless the object is out of the raycast) and reports where the pointer was released (nothing once the drag was cut short).</summary>
    public void OnEndDrag(PointerEventData eventData)
    {
        if (!_dragging)
            return;
        _dragging = false;
        if (proxy != null)
            proxy.enabled = _raycastable;
        Vector3 released = transform.position;
        if (_surface != null && _surface.TryProject(eventData.pressEventCamera, eventData.position, out Vector3 point))
            released = point;
        DragEnded?.Invoke(this, released);
    }

    /// <summary>Cancels a running drag (a right-click or Esc backs out of it, ControlRules.BackOut): the moves and the release of this press are ignored, the proxy follows the raycast again and DragCancelled says so (the owner puts the object back where it was picked up).</summary>
    public void Cancel() => OnDisable();

    /// <summary>A drag cut short: disabled (or cancelled), the object gets no release from the EventSystem, so the drag ends here, the proxy follows the raycast again and DragCancelled says so.</summary>
    private void OnDisable()
    {
        if (!_dragging)
            return;
        _dragging = false;
        if (proxy != null)
            proxy.enabled = _raycastable;
        DragCancelled?.Invoke(this);
    }
}
