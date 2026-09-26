using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// One desktop icon (the PC redesign DK2-DK6; replaces DesktopIcon): its
/// app's glyph over its label on translucent plates, a selection plate and a
/// badge. A press selects it; a click opens it when it completes a double
/// click (ClickTiming, the desktop's knobs) or, with Settings' "Single
/// click", at once; a right-click opens the icon's context menu (Open). Past
/// the EventSystem's drag threshold it drags: it follows the pointer keeping
/// the grab offset, drawn above the other icons and kept inside the icon
/// area, and on release DesktopIcons drops it (DesktopLayout.Drop) and saves
/// the layout; Escape cancels the drag (IDesktopDrag), the icon back where it
/// started. The pointer is read in the icon area's space through the press
/// camera, so a drag is right through the PC frame. The glyph is the app's
/// art when delivered (Assets/Art/UI/Resources/Desktop/icon_&lt;id&gt;.png,
/// ArtSlots.DesktopIcon); without it the icon draws DesktopIconPlaceholder's
/// (kept in memory, destroyed with it).
/// No upgrade gate and no reach into the run (audit R4-020): every icon is
/// always there and always live. Its RectTransform is anchored and pivoted
/// at the icon area's top-left.
/// </summary>
public sealed class DesktopIconView : MonoBehaviour, IPointerDownHandler, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDesktopDrag
{
    /// <summary>The app this icon opens (DesktopAppIds).</summary>
    [SerializeField] private string appId;

    /// <summary>The desktop's icons (selection, opening, the drop, the layout).</summary>
    [SerializeField] private DesktopIcons board;

    /// <summary>The glyph (tinted by the theme; the placeholder is drawn when it has no sprite).</summary>
    [SerializeField] private Image glyph;

    /// <summary>The selection plate (the IconSelection role), shown while the icon is selected.</summary>
    [SerializeField] private GameObject selection;

    /// <summary>The badge (the Badge role's circle), shown while it has something to say.</summary>
    [SerializeField] private GameObject badge;

    /// <summary>The badge's count ("" for a dot).</summary>
    [SerializeField] private TMP_Text badgeText;

    private readonly DoubleClick _clicks = new DoubleClick();
    private Texture2D _placeholderTexture;
    private Sprite _placeholderSprite;
    private Vector2 _grab;
    private Vector2 _start;
    private bool _dragging;

    /// <summary>The app this icon opens.</summary>
    public string AppId => appId;

    /// <summary>Where the icon sits now (its cell's top-left in the icon area, y down).</summary>
    public IconPlace Place => new IconPlace(appId, Rect.anchoredPosition.x, -Rect.anchoredPosition.y);

    private RectTransform Rect => (RectTransform)transform;

    /// <summary>The glyph: the app's art when delivered (ArtSlots.DesktopIcon, redesign phase 27), else the placeholder drawn for its id.</summary>
    private void Awake()
    {
        if (glyph == null || glyph.sprite != null)
            return;

        Sprite art = SlotArt.Sprite(ArtSlots.DesktopIcon(appId));
        if (art != null)
        {
            glyph.sprite = art;
            return;
        }

        byte[] rgba = DesktopIconPlaceholder.Render(appId);
        if (rgba == null)
            return;

        _placeholderTexture = new Texture2D(DesktopIconPlaceholder.Size, DesktopIconPlaceholder.Size, TextureFormat.RGBA32, false)
        {
            name = "icon_" + appId,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        _placeholderTexture.LoadRawTextureData(rgba);
        _placeholderTexture.Apply(false, true);
        _placeholderSprite = Sprite.Create(_placeholderTexture, new Rect(0f, 0f, DesktopIconPlaceholder.Size, DesktopIconPlaceholder.Size), new Vector2(0.5f, 0.5f));
        _placeholderSprite.name = _placeholderTexture.name;
        glyph.sprite = _placeholderSprite;
    }

    private void OnDestroy()
    {
        if (_placeholderSprite != null)
            Destroy(_placeholderSprite);
        if (_placeholderTexture != null)
            Destroy(_placeholderTexture);
    }

    /// <summary>A drag cut short by the icon hiding ends where the icon is.</summary>
    private void OnDisable() => EndDragging();

    /// <summary>Puts the icon's cell at a place.</summary>
    public void MoveTo(IconPlace place) => Rect.anchoredPosition = new Vector2(place.X, -place.Y);

    /// <summary>Shows or hides the selection plate.</summary>
    public void SetSelected(bool selected)
    {
        if (selection != null)
            selection.SetActive(selected);
    }

    /// <summary>Shows the badge for a count (IconBadge: a number, a dot, or nothing).</summary>
    public void SetBadge(int count)
    {
        string label = IconBadge.Label(count);
        if (badge != null)
            badge.SetActive(label != null);
        if (badgeText != null)
            badgeText.text = label ?? string.Empty;
    }

    /// <summary>A press (either button) selects the icon.</summary>
    public void OnPointerDown(PointerEventData eventData)
    {
        if (board != null)
            board.Select(this);
    }

    /// <summary>A left click opens on a double click (or at once with "Single click"); a right click opens the icon's menu. A drag sends no click.</summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (board == null)
            return;

        if (eventData.button == PointerEventData.InputButton.Right)
        {
            board.ShowMenu(this, eventData);
            return;
        }
        if (eventData.button != PointerEventData.InputButton.Left || !TryArea(eventData.position, eventData, out Vector2 at))
            return;

        if (board.OpensOnSingleClick || _clicks.Click(Time.unscaledTime, at.x, at.y, board.DoubleClickSeconds, board.DoubleClickDistance))
            board.Open(this);
    }

    /// <summary>Starts a drag (left button): remembers where the icon was and where on it the pointer grabbed (at the press, before the drag threshold); the icon draws above the others.</summary>
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (board == null || eventData.button != PointerEventData.InputButton.Left || !TryArea(eventData.pressPosition, eventData, out Vector2 at))
            return;

        _start = Rect.anchoredPosition;
        IconPlace place = Place;
        _grab = at - new Vector2(place.X, place.Y);
        _dragging = true;
        transform.SetAsLastSibling();
        board.BeginDrag(this);
    }

    /// <summary>Follows the pointer, keeping the grab offset, inside the icon area.</summary>
    public void OnDrag(PointerEventData eventData)
    {
        if (!_dragging || !TryArea(eventData.position, eventData, out Vector2 at))
            return;

        Vector2 topLeft = at - _grab;
        MoveTo(DesktopLayout.Clamp(new IconPlace(appId, topLeft.x, topLeft.y), board.Grid));
    }

    /// <summary>Drops the icon where it is (DesktopIcons: the overlap rule, then the layout is saved).</summary>
    public void OnEndDrag(PointerEventData eventData)
    {
        if (!_dragging)
            return;

        EndDragging();
        board.Dropped(this);
    }

    /// <summary>Escape during the drag: the icon goes back where it started and nothing is saved.</summary>
    public void CancelDrag()
    {
        if (!_dragging)
            return;

        Rect.anchoredPosition = _start;
        EndDragging();
    }

    private void EndDragging()
    {
        if (!_dragging)
            return;

        _dragging = false;
        if (board != null)
            board.EndDrag(this);
    }

    /// <summary>A screen point in the icon area's space (x right and y down from its top-left) through the press camera (the frame camera on the desktop).</summary>
    private bool TryArea(Vector2 screen, PointerEventData eventData, out Vector2 area)
    {
        area = Vector2.zero;
        if (!(transform.parent is RectTransform parent) ||
            !RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, eventData.pressEventCamera, out Vector2 local))
            return false;

        Rect r = parent.rect;
        area = new Vector2(local.x - r.xMin, r.yMax - local.y);
        return true;
    }
}
