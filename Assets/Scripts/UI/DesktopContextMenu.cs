using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// The desktop's right-click menu (the PC redesign DK5, section 3.2): one
/// themed panel whose builder-made entries are shown per target: on the empty
/// desktop "Arrange icons" (DesktopIcons.Arrange), on an icon "Open" (its
/// app). It opens with its top-left at the pointer, kept inside the desktop,
/// above every window; an entry closes it, and so do a press outside it and
/// Escape (the window manager: DesktopEscape.CloseMenu, first in the chain).
/// On the menu's panel, a child of the desktop canvas.
/// </summary>
public sealed class DesktopContextMenu : MonoBehaviour
{
    /// <summary>"Arrange icons" (the empty desktop's entry).</summary>
    [SerializeField] private Button arrangeEntry;

    /// <summary>"Open" (an icon's entry).</summary>
    [SerializeField] private Button openEntry;

    /// <summary>The desktop's icons (Arrange, Open).</summary>
    [SerializeField] private DesktopIcons icons;

    private DesktopIconView _target;
    private bool _wired;

    /// <summary>True while the menu shows.</summary>
    public bool IsOpen => gameObject.activeSelf;

    /// <summary>True for the menu and its entries (a press there leaves the menu to them).</summary>
    public bool IsPart(GameObject go) => go != null && go.transform.IsChildOf(transform);

    /// <summary>Opens the empty desktop's menu (Arrange icons) at the pointer.</summary>
    public void ShowForDesktop(PointerEventData eventData) => Show(null, eventData);

    /// <summary>Opens an icon's menu (Open) at the pointer.</summary>
    public void ShowForIcon(DesktopIconView icon, PointerEventData eventData) => Show(icon, eventData);

    /// <summary>Closes the menu.</summary>
    public void Close()
    {
        _target = null;
        gameObject.SetActive(false);
    }

    private void Show(DesktopIconView icon, PointerEventData eventData)
    {
        Wire();
        _target = icon;
        if (arrangeEntry != null)
            arrangeEntry.gameObject.SetActive(icon == null);
        if (openEntry != null)
            openEntry.gameObject.SetActive(icon != null);
        gameObject.SetActive(true);
        transform.SetAsLastSibling();

        var menu = (RectTransform)transform;
        LayoutRebuilder.ForceRebuildLayoutImmediate(menu);
        if (!(menu.parent is RectTransform parent) ||
            !RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, eventData.position, eventData.pressEventCamera, out Vector2 at))
            return;

        // Top-left at the pointer, moved back inside the desktop.
        Rect bounds = parent.rect;
        Vector2 size = menu.rect.size;
        float x = Mathf.Clamp(at.x, bounds.xMin, bounds.xMax - size.x);
        float y = Mathf.Clamp(at.y, bounds.yMin + size.y, bounds.yMax);
        menu.pivot = new Vector2(0f, 1f);
        menu.anchorMin = menu.anchorMax = new Vector2(0.5f, 0.5f);
        menu.anchoredPosition = new Vector2(x, y) - (Vector2)bounds.center;
    }

    /// <summary>The entries' clicks, once (the menu starts hidden, so this runs on the first show).</summary>
    private void Wire()
    {
        if (_wired)
            return;
        _wired = true;
        if (arrangeEntry != null)
            arrangeEntry.onClick.AddListener(Arrange);
        if (openEntry != null)
            openEntry.onClick.AddListener(OpenTarget);
    }

    private void Arrange()
    {
        Close();
        if (icons != null)
            icons.Arrange();
    }

    private void OpenTarget()
    {
        DesktopIconView target = _target;
        Close();
        if (icons != null && target != null)
            icons.Open(target);
    }
}
