using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Makes a UI window draggable by a header bar, brings it to the front when
/// touched, and supports stow/restore. Attach to the HEADER object (which needs
/// a raycast-target Graphic, e.g. an Image) and point <see cref="windowRoot"/>
/// at the window panel that should move. Investigation documents and reference
/// books use this so the player can arrange papers like a Papers, Please booth.
/// </summary>
public sealed class DraggableWindow : MonoBehaviour, IBeginDragHandler, IDragHandler, IPointerDownHandler
{
    /// <summary>The panel that actually moves / is brought to front.</summary>
    [SerializeField] private RectTransform windowRoot;

    private RectTransform _self;
    private Canvas _canvas;
    private Vector2 _pointerStart;
    private Vector2 _windowStart;

    /// <summary>The moved panel (defaults to this object's parent).</summary>
    public RectTransform WindowRoot => windowRoot;

    private void Awake()
    {
        _self = (RectTransform)transform;

        if (windowRoot == null)
            windowRoot = _self.parent as RectTransform ?? _self;

        _canvas = GetComponentInParent<Canvas>();
    }

    /// <summary>Brings the window above its siblings.</summary>
    public void BringToFront()
    {
        if (windowRoot != null)
            windowRoot.SetAsLastSibling();
    }

    public void OnPointerDown(PointerEventData eventData) => BringToFront();

    public void OnBeginDrag(PointerEventData eventData)
    {
        BringToFront();

        if (windowRoot == null)
            return;

        _windowStart = windowRoot.anchoredPosition;
        _pointerStart = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (windowRoot == null)
            return;

        float scale = _canvas != null && _canvas.scaleFactor > 0f ? _canvas.scaleFactor : 1f;
        Vector2 delta = (eventData.position - _pointerStart) / scale;
        windowRoot.anchoredPosition = _windowStart + delta;
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
}
