using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Makes a UI window draggable by a header bar, brings it to the front when
/// touched, and supports stow/restore. Attach to the HEADER object (which needs
/// a raycast-target Graphic, e.g. an Image) and point <see cref="windowRoot"/>
/// at the window panel that should move. The pointer is read in the window's
/// parent space (RectTransformUtility with the press camera), so a drag is
/// right on an overlay canvas and on the CRT's world-space desktop alike, and
/// the window is kept inside its parent while dragged (one taller than the
/// parent keeps its title bar visible). Investigation documents and reference
/// books use this so the player can arrange papers like a Papers, Please booth.
/// </summary>
public sealed class DraggableWindow : MonoBehaviour, IBeginDragHandler, IDragHandler, IPointerDownHandler
{
    /// <summary>The panel that actually moves / is brought to front.</summary>
    [SerializeField] private RectTransform windowRoot;

    private RectTransform _self;
    private Vector2 _pointerStart;
    private Vector2 _windowStart;

    /// <summary>The moved panel (defaults to this object's parent).</summary>
    public RectTransform WindowRoot => windowRoot;

    private void Awake()
    {
        _self = (RectTransform)transform;

        if (windowRoot == null)
            windowRoot = _self.parent as RectTransform ?? _self;
    }

    /// <summary>Brings the window above its siblings.</summary>
    public void BringToFront()
    {
        if (windowRoot != null)
            windowRoot.SetAsLastSibling();
    }

    /// <summary>A press anywhere on the header brings the window to the front.</summary>
    public void OnPointerDown(PointerEventData eventData) => BringToFront();

    /// <summary>Starts a drag: remembers where the window and the pointer were.</summary>
    public void OnBeginDrag(PointerEventData eventData)
    {
        BringToFront();

        if (windowRoot == null)
            return;

        _windowStart = windowRoot.anchoredPosition;
        TryPointer(eventData, out _pointerStart);
    }

    /// <summary>Moves the window with the pointer, kept inside its parent.</summary>
    public void OnDrag(PointerEventData eventData)
    {
        if (windowRoot == null || !TryPointer(eventData, out Vector2 pointer))
            return;

        windowRoot.anchoredPosition = _windowStart + (pointer - _pointerStart);
        KeepInsideParent();
    }

    /// <summary>Shows the window and brings it forward.</summary>
    public void Open()
    {
        if (windowRoot == null)
            return;

        windowRoot.gameObject.SetActive(true);
        BringToFront();
    }

    /// <summary>Hides the window (stow to the shelf/tray).</summary>
    public void Stow()
    {
        if (windowRoot != null)
            windowRoot.gameObject.SetActive(false);
    }

    /// <summary>Toggles between open and stowed.</summary>
    public void Toggle()
    {
        if (windowRoot == null)
            return;

        if (windowRoot.gameObject.activeSelf)
            Stow();
        else
            Open();
    }

    /// <summary>The pointer in the window's parent space (the press camera: null on an overlay canvas, the world camera on the CRT).</summary>
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
