using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// The desktop's right-click menu (the PC redesign DK5, AP4, section 3.2):
/// one themed panel whose builder-made entries are shown per target: on the
/// empty desktop "Arrange icons" (DesktopIcons.Arrange), on an icon "Open"
/// (its app), on a tab of the Investigation app "Move left", "Move right" and
/// "Reset tab order" (InvestigationApp.MoveTab, ResetTabOrder). It opens with its top-left at the pointer, kept inside the desktop,
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

    /// <summary>"Move left" (a tab's entry).</summary>
    [SerializeField] private Button moveLeftEntry;

    /// <summary>"Move right" (a tab's entry).</summary>
    [SerializeField] private Button moveRightEntry;

    /// <summary>"Reset tab order" (a tab's entry).</summary>
    [SerializeField] private Button resetTabsEntry;

    /// <summary>The desktop's icons (Arrange, Open).</summary>
    [SerializeField] private DesktopIcons icons;

    /// <summary>What the menu was opened on: the empty desktop, an icon or a tab.</summary>
    private enum Target
    {
        Desktop,
        Icon,
        Tab
    }

    private DesktopIconView _target;
    private InvestigationApp _app;
    private AppTab _tab;
    private bool _wired;

    /// <summary>True while the menu shows.</summary>
    public bool IsOpen => gameObject.activeSelf;

    /// <summary>True for the menu and its entries (a press there leaves the menu to them).</summary>
    public bool IsPart(GameObject go) => go != null && go.transform.IsChildOf(transform);

    /// <summary>Opens the empty desktop's menu (Arrange icons) at the pointer.</summary>
    public void ShowForDesktop(PointerEventData eventData) => Show(Target.Desktop, eventData);

    /// <summary>Opens an icon's menu (Open) at the pointer.</summary>
    public void ShowForIcon(DesktopIconView icon, PointerEventData eventData)
    {
        _target = icon;
        Show(Target.Icon, eventData);
    }

    /// <summary>Opens a tab's menu (Move left, Move right, Reset tab order) at the pointer.</summary>
    public void ShowForTab(InvestigationApp app, AppTab tab, PointerEventData eventData)
    {
        _app = app;
        _tab = tab;
        Show(Target.Tab, eventData);
    }

    /// <summary>Closes the menu.</summary>
    public void Close()
    {
        _target = null;
        _app = null;
        gameObject.SetActive(false);
    }

    private void Show(Target target, PointerEventData eventData)
    {
        Wire();
        if (target != Target.Icon)
            _target = null;
        if (target != Target.Tab)
            _app = null;
        Entry(arrangeEntry, target == Target.Desktop);
        Entry(openEntry, target == Target.Icon);
        Entry(moveLeftEntry, target == Target.Tab);
        Entry(moveRightEntry, target == Target.Tab);
        Entry(resetTabsEntry, target == Target.Tab);
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

    /// <summary>Shows or hides an entry (a missing one is skipped).</summary>
    private static void Entry(Button entry, bool on)
    {
        if (entry != null)
            entry.gameObject.SetActive(on);
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
        if (moveLeftEntry != null)
            moveLeftEntry.onClick.AddListener(() => OnTab((app, tab) => app.MoveTab(tab, -1)));
        if (moveRightEntry != null)
            moveRightEntry.onClick.AddListener(() => OnTab((app, tab) => app.MoveTab(tab, 1)));
        if (resetTabsEntry != null)
            resetTabsEntry.onClick.AddListener(() => OnTab((app, _) => app.ResetTabOrder()));
    }

    /// <summary>A tab's entry: the menu closes, then the app moves its tabs.</summary>
    private void OnTab(System.Action<InvestigationApp, AppTab> act)
    {
        InvestigationApp app = _app;
        AppTab tab = _tab;
        Close();
        if (app != null)
            act(app, tab);
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
