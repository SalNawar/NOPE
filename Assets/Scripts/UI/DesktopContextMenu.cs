using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// The desktop's right-click menu (the PC redesign DK5, CP1, PR1, section
/// 3.2): one themed panel whose builder-made entries are shown per target: on
/// the empty desktop "Arrange icons" (DesktopIcons.Arrange), on an icon
/// "Open" (its app), on a row of the Investigation app "Copy value", "Copy
/// row", "Pin" or "Unpin" and, when the row can be picked, "Pick for
/// compare". It opens with its top-left at the pointer, kept inside the
/// desktop, above every window; an entry closes it, and so do a press
/// outside it and Escape (the keyboard poller: DesktopEscape.CloseMenu,
/// first in the chain). On the menu's panel, a child of the desktop canvas.
/// </summary>
public sealed class DesktopContextMenu : MonoBehaviour
{
    /// <summary>"Arrange icons" (the empty desktop's entry).</summary>
    [SerializeField] private Button arrangeEntry;

    /// <summary>"Open" (an icon's entry).</summary>
    [SerializeField] private Button openEntry;

    /// <summary>"Copy value" (a row's entry).</summary>
    [SerializeField] private Button copyEntry;

    /// <summary>"Copy row" (a row's entry: "Label: value").</summary>
    [SerializeField] private Button copyRowEntry;

    /// <summary>"Pin" or "Unpin" (a row's entry).</summary>
    [SerializeField] private Button pinEntry;

    /// <summary>"Pick for compare" (a pickable row's entry).</summary>
    [SerializeField] private Button pickEntry;

    /// <summary>The desktop's icons (Arrange, Open).</summary>
    [SerializeField] private DesktopIcons icons;

    private DesktopIconView _target;
    private AppRow _row;
    private InvestigationApp _app;
    private bool _wired;

    /// <summary>True while the menu shows.</summary>
    public bool IsOpen => gameObject.activeSelf;

    /// <summary>True for the menu and its entries (a press there leaves the menu to them).</summary>
    public bool IsPart(GameObject go) => go != null && go.transform.IsChildOf(transform);

    /// <summary>Opens the empty desktop's menu (Arrange icons) at the pointer.</summary>
    public void ShowForDesktop(PointerEventData eventData) => Show(null, null, false, eventData);

    /// <summary>Opens an icon's menu (Open) at the pointer.</summary>
    public void ShowForIcon(DesktopIconView icon, PointerEventData eventData) => Show(icon, null, false, eventData);

    /// <summary>Opens a row's menu (Copy value, Copy row, Pin or Unpin as <paramref name="pinned"/> says, Pick for compare when it can be picked) at the pointer.</summary>
    public void ShowForRow(InvestigationApp app, AppRow row, bool pinned, PointerEventData eventData)
    {
        _app = app;
        Show(null, row, pinned, eventData);
    }

    /// <summary>Closes the menu.</summary>
    public void Close()
    {
        _target = null;
        _row = null;
        gameObject.SetActive(false);
    }

    private void Show(DesktopIconView icon, AppRow row, bool pinned, PointerEventData eventData)
    {
        Wire();
        _target = icon;
        _row = row;
        bool desktop = icon == null && row == null;
        SetShown(arrangeEntry, desktop);
        SetShown(openEntry, icon != null);
        SetShown(copyEntry, row != null);
        SetShown(copyRowEntry, row != null);
        SetShown(pinEntry, row != null);
        SetShown(pickEntry, row != null && row.Pickable);
        if (pinEntry != null && row != null)
        {
            TMP_Text label = pinEntry.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
                label.text = UiText.Get(pinned ? "menu.unpin" : "menu.pin");
        }
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

    private static void SetShown(Button entry, bool shown)
    {
        if (entry != null)
            entry.gameObject.SetActive(shown);
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
        if (copyEntry != null)
            copyEntry.onClick.AddListener(() => OnRow((app, row) => app.Copy(row, false)));
        if (copyRowEntry != null)
            copyRowEntry.onClick.AddListener(() => OnRow((app, row) => app.Copy(row, true)));
        if (pinEntry != null)
            pinEntry.onClick.AddListener(() => OnRow((app, row) => app.TogglePin(row)));
        if (pickEntry != null)
            pickEntry.onClick.AddListener(() => OnRow((app, row) => row.Pick()));
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

    /// <summary>A row's entry: the menu closes, then the entry acts on the row (still there).</summary>
    private void OnRow(System.Action<InvestigationApp, AppRow> act)
    {
        AppRow row = _row;
        InvestigationApp app = _app;
        Close();
        if (row != null && app != null)
            act(app, row);
    }
}
