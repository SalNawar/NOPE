using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// The desktop's six icons (the PC redesign DK1-DK6), on the icon layer: the
/// icon area above the compare dock and the taskbar, under the case chrome
/// and every window. At start the icons take the player's saved layout
/// (DesktopPreferences, DesktopLayout.Restore: unknown ids dropped, a new id
/// in the first free spot, off-screen places clamped), else the default
/// arrangement. One icon at a time is selected (a press on it); a press on
/// the empty icon area deselects, and a right-click there opens the
/// desktop's context menu ("Arrange icons"). An icon opens its app through
/// DesktopApps.OpenApp on a double click (a single one with Settings'
/// choice); a drop goes through DesktopLayout.Drop and saves the layout;
/// Arrange (the context menu, the Start menu, Settings' "Reset icon
/// positions") lays the icons out column-first in the default order and
/// saves. While the desktop takes input (its raycaster is on) and no window
/// or text field has the keyboard, the arrow keys move the selection to the
/// nearest icon that way and Enter opens it. Badges: an app's count or dot
/// (SetBadge): Mail's is the Mail feed's unread count, redrawn whenever the
/// feed changes; until the Investigation app exists (phase 16), a scan
/// finishing while the interim Investigation window is hidden dots its icon,
/// and the dot goes when the window shows.
/// </summary>
public sealed class DesktopIcons : MonoBehaviour, IPointerDownHandler, IPointerClickHandler
{
    /// <summary>The icon knobs (the cell, the arrange grid, the default order, the drop's overlap share, the double click).</summary>
    [SerializeField] private DesktopConfigSO config;

    /// <summary>Opens an icon's app.</summary>
    [SerializeField] private DesktopApps apps;

    /// <summary>The window manager: an icon drag is the one Escape may cancel, and a focused window takes the keyboard.</summary>
    [SerializeField] private DesktopWindowManager manager;

    /// <summary>The desktop's right-click menu.</summary>
    [SerializeField] private DesktopContextMenu contextMenu;

    /// <summary>The desktop canvas's raycaster (on only while the desktop takes input: the frame open, the screen on).</summary>
    [SerializeField] private GraphicRaycaster raycaster;

    /// <summary>The icons, one per app.</summary>
    [SerializeField] private DesktopIconView[] icons = new DesktopIconView[0];

    /// <summary>Interim until the Investigation app (phase 16): the desk whose finished scans dot the Investigation icon while its window is hidden.</summary>
    [SerializeField] private DeskController desk;

    /// <summary>Interim until the Investigation app (phase 16): the Investigation window whose showing clears the dot.</summary>
    [SerializeField] private DesktopWindow investigationWindow;

    /// <summary>The Mail feed: its unread count is the Mail icon's badge.</summary>
    [SerializeField] private MailFeed mail;

    private readonly Dictionary<string, int> _badges = new Dictionary<string, int>();
    private readonly List<string> _order = new List<string>();
    private readonly List<IconPlace> _others = new List<IconPlace>();
    private IconGrid _grid;
    private DesktopIconView _selected;

    /// <summary>The icon area and the arrange grid (the icon layer's size and the knobs).</summary>
    public IconGrid Grid => _grid ??= MakeGrid();

    /// <summary>True when one click opens an icon (Settings' choice).</summary>
    public bool OpensOnSingleClick => DesktopPreferences.OpenIconsWithSingleClick;

    /// <summary>The double click's time, in seconds.</summary>
    public float DoubleClickSeconds => config != null ? config.doubleClickSeconds : 0f;

    /// <summary>The double click's distance, in desktop units.</summary>
    public float DoubleClickDistance => config != null ? config.doubleClickDistance : 0f;

    private void Awake()
    {
        if (config == null)
            Debug.LogWarning("[DesktopIcons] No DesktopConfigSO wired: the icons keep their built places. Run Tools > TimeDesk > Build Office UI.", this);
        if (desk != null)
            desk.ScanFinished += HandleScanFinished;
        if (mail != null)
            mail.Changed += ShowUnreadMail;
    }

    private void OnDestroy()
    {
        if (desk != null)
            desk.ScanFinished -= HandleScanFinished;
        if (mail != null)
            mail.Changed -= ShowUnreadMail;
    }

    /// <summary>The icons take the player's saved layout (or the default arrangement).</summary>
    private void Start()
    {
        if (config != null)
            Apply(DesktopLayout.Restore(DesktopPreferences.IconPositions, Order(), Grid));
        foreach (DesktopIconView icon in icons)
            if (icon != null)
            {
                icon.SetSelected(false);
                icon.SetBadge(0);
            }
        ShowUnreadMail();
    }

    /// <summary>The Mail icon's badge: the feed's unread count.</summary>
    private void ShowUnreadMail()
    {
        if (mail != null)
            SetBadge(DesktopAppIds.Mail, mail.Unread);
    }

    /// <summary>Selects one icon (the others lose their plate).</summary>
    public void Select(DesktopIconView icon)
    {
        _selected = icon;
        foreach (DesktopIconView i in icons)
            if (i != null)
                i.SetSelected(i == icon);
    }

    /// <summary>Opens the icon's app.</summary>
    public void Open(DesktopIconView icon)
    {
        if (icon != null && apps != null)
            apps.OpenApp(icon.AppId);
    }

    /// <summary>Lays the icons out column-first in the default order and saves the layout (the context menu, the Start menu, Settings' reset).</summary>
    public void Arrange()
    {
        if (config == null)
            return;
        Apply(DesktopLayout.Arrange(Order(), Grid));
        Save();
    }

    /// <summary>Shows an app's badge: a count, IconBadge.Dot, or 0 for none.</summary>
    public void SetBadge(string appId, int count)
    {
        _badges[appId] = count;
        foreach (DesktopIconView icon in icons)
            if (icon != null && icon.AppId == appId)
                icon.SetBadge(count);
    }

    /// <summary>An icon's drag started (the window manager lets Escape cancel it).</summary>
    public void BeginDrag(DesktopIconView icon)
    {
        if (manager != null)
            manager.BeginDrag(icon);
    }

    /// <summary>An icon's drag ended.</summary>
    public void EndDrag(DesktopIconView icon)
    {
        if (manager != null)
            manager.EndDrag(icon);
    }

    /// <summary>An icon was dropped: DesktopLayout.Drop settles it (clamped; moved to the nearest free spot when it covers another icon), then the layout is saved.</summary>
    public void Dropped(DesktopIconView icon)
    {
        if (config == null || icon == null)
            return;

        _others.Clear();
        foreach (DesktopIconView other in icons)
            if (other != null && other != icon)
                _others.Add(other.Place);
        IconPlace at = icon.Place;
        icon.MoveTo(DesktopLayout.Drop(icon.AppId, at.X, at.Y, _others, Grid, config.iconDropOverlap));
        Save();
    }

    /// <summary>A right-click on an icon: its context menu (Open).</summary>
    public void ShowMenu(DesktopIconView icon, PointerEventData eventData)
    {
        if (contextMenu != null)
            contextMenu.ShowForIcon(icon, eventData);
    }

    /// <summary>A press on the empty icon area deselects the icon.</summary>
    public void OnPointerDown(PointerEventData eventData) => Select(null);

    /// <summary>A right-click on the empty icon area opens the desktop's context menu.</summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right && contextMenu != null)
            contextMenu.ShowForDesktop(eventData);
    }

    /// <summary>The keys (arrows, Enter) while the desktop takes input, and the interim Investigation dot.</summary>
    private void Update()
    {
        ClearShownDots();
        if (raycaster == null || !raycaster.isActiveAndEnabled || (manager != null && manager.WindowFocused) || FieldFocused())
            return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (keyboard.upArrowKey.wasPressedThisFrame)
            Step(0, -1);
        else if (keyboard.downArrowKey.wasPressedThisFrame)
            Step(0, 1);
        else if (keyboard.leftArrowKey.wasPressedThisFrame)
            Step(-1, 0);
        else if (keyboard.rightArrowKey.wasPressedThisFrame)
            Step(1, 0);
        else if ((keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame) && _selected != null)
            Open(_selected);
    }

    /// <summary>An arrow key: the nearest icon that way becomes the selection (the first icon when none is selected).</summary>
    private void Step(int dx, int dy)
    {
        if (_selected == null)
        {
            List<string> order = Order();
            Select(order.Count > 0 ? IconOf(order[0]) : null);
            return;
        }

        _others.Clear();
        foreach (DesktopIconView icon in icons)
            if (icon != null)
                _others.Add(icon.Place);
        string next = DesktopLayout.Nearest(_selected.AppId, dx, dy, _others);
        if (next != null)
            Select(IconOf(next));
    }

    /// <summary>Interim (phase 16's app takes it over): a finished scan dots the Investigation icon while its window is hidden.</summary>
    private void HandleScanFinished(int document)
    {
        if (!InvestigationShows)
            SetBadge(DesktopAppIds.Investigation, IconBadge.Dot);
    }

    /// <summary>Interim: the Investigation icon's dot goes once its window shows.</summary>
    private void ClearShownDots()
    {
        if (InvestigationShows && _badges.TryGetValue(DesktopAppIds.Investigation, out int count) && count == IconBadge.Dot)
            SetBadge(DesktopAppIds.Investigation, 0);
    }

    /// <summary>True while the interim Investigation window shows (open and not minimised).</summary>
    private bool InvestigationShows => investigationWindow != null && investigationWindow.gameObject.activeInHierarchy;

    /// <summary>The ids in the default order (the knob's, then any icon it does not name).</summary>
    private List<string> Order()
    {
        _order.Clear();
        if (config != null && config.iconOrder != null)
            foreach (string id in config.iconOrder)
                if (IconOf(id) != null && !_order.Contains(id))
                    _order.Add(id);
        foreach (DesktopIconView icon in icons)
            if (icon != null && !_order.Contains(icon.AppId))
                _order.Add(icon.AppId);
        return _order;
    }

    private void Apply(IReadOnlyList<IconPlace> places)
    {
        foreach (IconPlace place in places)
        {
            DesktopIconView icon = IconOf(place.Id);
            if (icon != null)
                icon.MoveTo(place);
        }
    }

    private void Save()
    {
        var places = new List<IconPlace>();
        foreach (string id in Order())
            places.Add(IconOf(id).Place);
        DesktopPreferences.IconPositions = DesktopLayout.Save(places);
    }

    private DesktopIconView IconOf(string id)
    {
        foreach (DesktopIconView icon in icons)
            if (icon != null && icon.AppId == id)
                return icon;
        return null;
    }

    private IconGrid MakeGrid()
    {
        Rect area = ((RectTransform)transform).rect;
        Vector2 cell = config != null ? config.iconCellSize : new Vector2(120f, 132f);
        Vector2 origin = config != null ? config.iconOrigin : Vector2.zero;
        return new IconGrid(area.width, area.height, cell.x, cell.y, origin.x, origin.y,
                            config != null ? config.iconColumnStep : cell.x, config != null ? config.iconRowStep : cell.y);
    }

    /// <summary>True while a desktop text field has the keyboard (typing is not a shortcut).</summary>
    private static bool FieldFocused()
    {
        GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        return selected != null && selected.TryGetComponent(out TMP_InputField field) && field.isFocused;
    }
}
