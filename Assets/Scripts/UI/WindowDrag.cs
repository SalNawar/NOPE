using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// A desktop window's title-bar drag (was DraggableWindow, without its dead
/// members: audit R4-016; the PC redesign WN3, section 3.2). Attach to the
/// title bar (it needs a raycast-target Graphic) and point
/// <see cref="windowRoot"/> at the window that moves. The pointer is read in
/// the window's parent space through the press camera, so a drag is right on
/// the frame's world-space desktop, and the window stays inside its parent
/// (one taller than the parent keeps its title bar visible: RectClamp). A
/// maximised window does not drag. A double-click on the bar toggles
/// maximise (ClickTiming, with the desktop's knobs). The window manager
/// knows the drag while it runs, so Escape can cancel it: the window goes
/// back where it started. Focus comes from the manager's own press (WN2).
/// </summary>
public sealed class WindowDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    /// <summary>The window that moves (defaults to this object's parent).</summary>
    [SerializeField] private RectTransform windowRoot;

    private DesktopWindow _window;
    private readonly DoubleClick _clicks = new DoubleClick();
    private Vector2 _pointerStart;
    private Vector2 _windowStart;
    private bool _dragging;

    private void Awake()
    {
        if (windowRoot == null)
            windowRoot = transform.parent as RectTransform ?? (RectTransform)transform;
        _window = windowRoot.GetComponent<DesktopWindow>();
    }

    /// <summary>A window hidden mid-drag ends the drag.</summary>
    private void OnDisable() => EndDragging();

    /// <summary>Starts a drag (not while maximised): remembers where the window and the pointer were.</summary>
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (windowRoot == null || (_window != null && _window.IsMaximised) || !TryPointer(eventData, out _pointerStart))
            return;

        _windowStart = windowRoot.anchoredPosition;
        _dragging = true;
        if (_window != null && _window.Manager != null)
            _window.Manager.BeginDrag(this);
    }

    /// <summary>Moves the window with the pointer, kept inside its parent.</summary>
    public void OnDrag(PointerEventData eventData)
    {
        if (!_dragging || !TryPointer(eventData, out Vector2 pointer))
            return;

        windowRoot.anchoredPosition = _windowStart + (pointer - _pointerStart);
        KeepInsideParent();
    }

    /// <summary>Ends the drag where the window is.</summary>
    public void OnEndDrag(PointerEventData eventData) => EndDragging();

    /// <summary>Escape during the drag (the window manager): the window goes back where it started and the rest of the drag does nothing.</summary>
    public void CancelDrag()
    {
        if (!_dragging)
            return;

        windowRoot.anchoredPosition = _windowStart;
        EndDragging();
    }

    /// <summary>A double-click on the title bar toggles maximise (a drag sends no click).</summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (_window == null || _window.Manager == null || !TryPointer(eventData, out Vector2 at))
            return;

        DesktopWindowManager manager = _window.Manager;
        if (_clicks.Click(Time.unscaledTime, at.x, at.y, manager.DoubleClickSeconds, manager.DoubleClickDistance))
            _window.ToggleMaximise();
    }

    private void EndDragging()
    {
        if (!_dragging)
            return;

        _dragging = false;
        if (_window != null && _window.Manager != null)
            _window.Manager.EndDrag(this);
    }

    /// <summary>The pointer in the window's parent space (the press camera: the frame camera on the desktop).</summary>
    private bool TryPointer(PointerEventData eventData, out Vector2 local)
    {
        local = Vector2.zero;
        return windowRoot.parent is RectTransform parent &&
               RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, eventData.position, eventData.pressEventCamera, out local);
    }

    /// <summary>Moves the window back inside its parent's rect, per axis (RectClamp): a window taller than the parent keeps its top, the title bar, inside.</summary>
    private void KeepInsideParent()
    {
        if (!(windowRoot.parent is RectTransform parent))
            return;

        Rect bounds = parent.rect;
        Rect own = windowRoot.rect;
        Vector3 at = windowRoot.localPosition;
        float dx = RectClamp.Shift(at.x + own.xMin, at.x + own.xMax, bounds.xMin, bounds.xMax, false);
        float dy = RectClamp.Shift(at.y + own.yMin, at.y + own.yMax, bounds.yMin, bounds.yMax, true);
        windowRoot.anchoredPosition += new Vector2(dx, dy);
    }
}
