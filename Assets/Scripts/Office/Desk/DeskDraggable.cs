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
/// that I physically move"). A paper keeps the offset it was grabbed at
/// (times GrabScale: a paper resized under the cursor at the counter's edge
/// keeps the held point under it); a stamp is carried by its anchor (Init's
/// anchor, its die): the anchor stays straight above the point the pointer
/// shows (Init's aim: on the paper under the pointer, not the bare desk plane
/// under it, which an oblique view sees millimetres away), wherever the stamp
/// was grabbed (Saleh 2026-10-06: "when I stamp it doesn't stamp where I'm
/// pressing"). Every drag is a hold (DOC-HANDLE, BUG-55): it Begins in the
/// desk's HoldRegistry and Ends there; the registry's Cancel cancels it
/// (CancelHold). The held object follows the pointer every frame, re-projected
/// through the moving camera during any blend (LateUpdate), not only when the
/// pointer moves; and a release that comes while the game has lost the focus
/// (Alt+Tab: the Input System lets go of the button) cancels the drag
/// instead of dropping it, so nothing is committed.
/// </summary>
public sealed class DeskDraggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IHoldable
{
    /// <summary>The collider the raycaster finds (off while dragged, and while out of the raycast).</summary>
    [SerializeField] private Collider proxy;

    private DeskSurface _surface;
    private float _height;
    private Transform _anchor;
    private Func<Ray, Vector3?> _aim;
    private Vector3 _grabOffset;
    private bool _dragging;
    private bool _raycastable = true;

    /// <summary>The camera and the screen point of the drag's last pointer move: the drag re-projects them every frame.</summary>
    private Camera _camera;
    private Vector2 _pointer;

    /// <summary>The camera's pose and the grab scale at the last follow: a frame with neither changed (nor the pointer) needs no new follow.</summary>
    private Vector3 _followedFrom;
    private Quaternion _followedTurn;
    private float _followedScale = 1f;
    private bool _moved;

    /// <summary>Raised when a drag starts.</summary>
    public event Action<DeskDraggable> DragBegan;

    /// <summary>Raised when a drag ends, with the pointer projected onto the desk (or the object's position when the projection fails).</summary>
    public event Action<DeskDraggable, Vector3> DragEnded;

    /// <summary>Raised when a drag is cut short: the object was disabled mid-drag (its owner took its input away, or it is being destroyed), the drag was cancelled (a right-click, Esc or a view key: the hold registry), or its release came while the game had lost the focus, so no drop will come.</summary>
    public event Action<DeskDraggable> DragCancelled;

    /// <summary>Raised as the object follows the pointer, with the pointer projected onto the desk (the desk sizes a document by the zone it is over).</summary>
    public event Action<DeskDraggable, Vector3> Dragged;

    /// <summary>True while a drag runs.</summary>
    public bool IsDragging => _dragging;

    /// <summary>Where the object was when the last drag started, or when RememberPosition last took note.</summary>
    public Vector3 PickUpPosition { get; private set; }

    /// <summary>How much the grab offset is scaled now (1 at the grab): its owner sets it while the held thing changes size under the cursor (a paper crossing the counter's edge), so the held point stays under the cursor.</summary>
    public float GrabScale { get; set; } = 1f;

    /// <summary>The held object's origin from the pointer's point on the desk now (world, on the desk plane; the grab offset times GrabScale): the held point is the origin minus this.</summary>
    public Vector3 GrabOffset => _grabOffset * GrabScale;

    /// <summary>The camera the drag is seen through (the press's; null before a drag).</summary>
    public Camera DragCamera => _camera;

    /// <summary>Takes note of where the object lies now as its pick-up point: the scanner feeding itself a paper (the Auto-Feed Scanner) sends it back here after the scan, as a dragged paper goes back to where it was picked up.</summary>
    public void RememberPosition() => PickUpPosition = transform.position;

    /// <summary>Sets the desk this object moves on, how far above its plane the object's origin is carried (<paramref name="height"/>, metres: 0 for a paper or the rulebook, which lie on it; a stamp's foot hangs over the papers), the point carried straight above the pointer (<paramref name="anchor"/>: a stamp's die; null keeps the grab offset, as a paper does), and where the pointer's ray points (<paramref name="aim"/>: the stamps' paper under the pointer; null or no answer: the desk plane).</summary>
    public void Init(DeskSurface surface, float height = 0f, Transform anchor = null, Func<Ray, Vector3?> aim = null)
    {
        _surface = surface;
        _height = height;
        _anchor = anchor;
        _aim = aim;
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

    /// <summary>Records the pick-up position and the grab offset (with an anchor, the offset from the anchor to the object's origin: the anchor goes over the pointer), turns the proxy off and holds it in the desk's hold registry (the left button only; another hold is let go first).</summary>
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;
        _dragging = true;
        PickUpPosition = transform.position;
        _grabOffset = Vector3.zero;
        GrabScale = 1f;
        _camera = eventData.pressEventCamera;
        _pointer = eventData.position;
        if (_surface != null && _anchor != null)
            _grabOffset = Vector3.ProjectOnPlane(transform.position - _anchor.position, _surface.transform.up);
        else if (_surface != null && _surface.TryProject(_camera, _pointer, out Vector3 point))
            _grabOffset = Vector3.ProjectOnPlane(transform.position - point, _surface.transform.up);
        if (proxy != null)
            proxy.enabled = false;
        HoldRegistry.Desk.Begin(this);
        DragBegan?.Invoke(this);
    }

    /// <summary>Takes the pointer's new place and follows it (nothing once the drag was cut short).</summary>
    public void OnDrag(PointerEventData eventData)
    {
        if (!_dragging)
            return;
        _camera = eventData.pressEventCamera;
        _pointer = eventData.position;
        _moved = true;
        Follow();
    }

    /// <summary>While dragged, follows the pointer again whenever the camera moved (a blend, a tilt) or the grab scale changed since the last follow: the held thing stays glued to the cursor every frame (BUG-55).</summary>
    private void LateUpdate()
    {
        if (!_dragging || _camera == null)
            return;
        Transform eye = _camera.transform;
        if (_moved || eye.position != _followedFrom || eye.rotation != _followedTurn || !Mathf.Approximately(GrabScale, _followedScale))
            Follow();
    }

    /// <summary>The object goes where the pointer shows through the camera now, its centre clamped to the desk and carried at its height above it; Dragged says where the pointer is on the desk.</summary>
    private void Follow()
    {
        _moved = false;
        if (_camera != null)
        {
            _followedFrom = _camera.transform.position;
            _followedTurn = _camera.transform.rotation;
        }
        _followedScale = GrabScale;
        if (_surface == null || !_surface.TryProject(_camera, _pointer, out Vector3 point))
            return;
        transform.position = _surface.Clamp(Aimed(point) + GrabOffset) + _surface.transform.up * _height;
        Dragged?.Invoke(this, point);
    }

    /// <summary>Turns the proxy back on (unless the object is out of the raycast), lets go of the hold and reports where the pointer was released (nothing once the drag was cut short). A release while the game has no focus (Alt+Tab) cancels instead: nothing is committed.</summary>
    public void OnEndDrag(PointerEventData eventData)
    {
        if (!_dragging)
            return;
        if (!Application.isFocused)
        {
            OnDisable();
            return;
        }
        _dragging = false;
        HoldRegistry.Desk.End(this);
        if (proxy != null)
            proxy.enabled = _raycastable;
        Vector3 released = transform.position;
        if (_surface != null && _surface.TryProject(eventData.pressEventCamera, eventData.position, out Vector3 point))
            released = point;
        DragEnded?.Invoke(this, released);
    }

    /// <summary>Where the pointer points for the object's place: Init's aim along the pointer's ray when it answers, else <paramref name="onPlane"/> (the pointer on the desk plane).</summary>
    private Vector3 Aimed(Vector3 onPlane)
    {
        Vector3? aimed = _aim != null && _camera != null ? _aim(_camera.ScreenPointToRay(_pointer)) : null;
        return aimed ?? onPlane;
    }

    /// <summary>Cancels a running drag (a right-click, Esc or a view key backs out of it, ControlRules.BackOut): the moves and the release of this press are ignored, the proxy follows the raycast again and DragCancelled says so (the owner puts the object back where it was picked up).</summary>
    public void Cancel() => OnDisable();

    /// <summary>The hold registry lets go of this drag (another hold began, a back-out, a view key, the focus lost): the drag is cancelled.</summary>
    public void CancelHold() => Cancel();

    /// <summary>A drag cut short: disabled (or cancelled), the object gets no release from the EventSystem, so the drag ends here, the hold is let go, the proxy follows the raycast again and DragCancelled says so.</summary>
    private void OnDisable()
    {
        if (!_dragging)
            return;
        _dragging = false;
        HoldRegistry.Desk.End(this);
        if (proxy != null)
            proxy.enabled = _raycastable;
        DragCancelled?.Invoke(this);
    }
}
