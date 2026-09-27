using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// The Investigation app's keys, focus ring, clipboard, pins, recent items
/// and zoom (redesign phase 20; the PC redesign KB1-KB5, CP1-CP3, PR1-PR2,
/// section 3.4). The desktop's keyboard poller (DesktopKeyboard) runs the
/// app's commands here. Ctrl+F opens or restores the app and focuses its
/// search field; Ctrl+1…6 show the tab at that position, Ctrl+Tab and
/// Ctrl+Shift+Tab (and ← → on the tab strip) the next or previous one;
/// Ctrl+B hides or shows the sidebar (the pane widens; saved per player).
/// Tab and Shift+Tab walk the regions (AppFocus: search, the tab strip, the
/// pane header, the pane's content, the sidebar, the dock, Accept/Deny) with
/// the focus ring; inside a region the arrows, Home, End, PgUp and PgDn move
/// the ring over its items (the content's rows in reading order, turning the
/// page at a page's end), Enter presses the focused item (a chip, the pin
/// button, a pin or recent item, the dock's clear, Accept or Deny), Space
/// picks the focused row, Ctrl+C copies its value as shown and Ctrl+Shift+C
/// "Label: value" (the one clipboard, AppClipboard, and the system
/// clipboard), Ctrl+P pins the focused row, a sidebar pin (unpinned) or the
/// pane's item. Pins (PinBoard, at most DesktopConfigSO.pinsMax) and recent
/// items (RecentList: the items the player opens or jumps to) show in the
/// sidebar; a click on one jumps (its tab, its item, its row with the ring
/// on it); the case's are dropped when a case starts or ends, the day's when
/// the day starts. Ctrl+= and Ctrl+- zoom the panes' content (PaneZoom) to
/// the next level (100, 125, 150 %), Ctrl+0 back to Settings' Text size.
/// A mouse press hides the ring. With the two panes (phase 18) the keys act on
/// the active pane: Alt+← and Alt+→ walk its history, F6 makes the other pane
/// active, Ctrl+\ splits or joins, Ctrl+Shift+PgUp/PgDn move the active tab
/// in the shared order, Ctrl+1…6 follow that order, Tab also visits the other
/// pane's content, and Enter on a row with a smart link follows it in the same
/// pane (Ctrl+Enter: the other pane). The steps come with phase 21.
/// </summary>
public sealed partial class InvestigationApp
{
    [Header("Keys, clipboard, pins, zoom (redesign phase 20)")]
    /// <summary>The toolbar's search field (Ctrl+F; a paste target).</summary>
    [SerializeField] private TMP_InputField searchField;

    /// <summary>The search field's chip for a pasted untranslated line.</summary>
    [SerializeField] private SearchFieldChip searchChip;

    /// <summary>The panes' area beside the sidebar (it widens while the sidebar is hidden).</summary>
    [SerializeField] private RectTransform panes;

    /// <summary>The sidebar's Pinned list.</summary>
    [SerializeField] private SidebarEntryList pinsList;

    /// <summary>The sidebar's Recent list.</summary>
    [SerializeField] private SidebarEntryList recentList;

    /// <summary>Each pane header's pin button (it pins the active pane's item; a press on it makes its pane the active one first).</summary>
    [SerializeField] private Button[] pinButtons = new Button[0];

    /// <summary>The keyboard focus ring.</summary>
    [SerializeField] private AppFocusRing focusRing;

    /// <summary>Each pane's zoom.</summary>
    [SerializeField] private PaneZoom[] zooms = new PaneZoom[0];

    /// <summary>The header's Accept (the Decision region).</summary>
    [SerializeField] private Button acceptButton;

    /// <summary>The header's Deny (the Decision region).</summary>
    [SerializeField] private Button denyButton;

    /// <summary>The compare dock's clear (the Dock region, while a pair shows).</summary>
    [SerializeField] private Button dockClearButton;

    /// <summary>The compare dock (the Dock region's frame while no pair shows).</summary>
    [SerializeField] private RectTransform compareDock;

    /// <summary>The desktop's context menu (a row's Copy value, Copy row, Pin, Pick for compare).</summary>
    [SerializeField] private DesktopContextMenu rowMenu;

    private readonly AppClipboard _clipboard = new AppClipboard();
    private readonly List<RectTransform> _targets = new List<RectTransform>();
    private readonly List<AppRow> _rowScratch = new List<AppRow>();
    private readonly List<IPagedRows> _pagesScratch = new List<IPagedRows>();
    private readonly Dictionary<AppTab, string> _opened = new Dictionary<AppTab, string>();
    private PinBoard _pins = new PinBoard(1);
    private RecentList _recent = new RecentList(1);
    private AppRegion _region = AppRegion.PaneContent;
    private int _item;
    private bool _ringOn;
    private bool _caseOn;
    private bool _sidebarShown = true;
    private float _panesInset;
    private int _zoom = AppZoom.Normal;
    private string _traveller = string.Empty;

    /// <summary>The desktop's one clipboard (Notes and the search field read its clip).</summary>
    public AppClipboard Clipboard => _clipboard;

    /// <summary>True while the focus ring is on a list's item (the rows, the chips, the sidebar, the dock, Accept/Deny).</summary>
    public bool ListFocused => _ringOn && _region != AppRegion.Search && _region != AppRegion.TabStrip;

    /// <summary>True while the focus ring is on the tab strip.</summary>
    public bool TabStripFocused => _ringOn && _region == AppRegion.TabStrip;

    /// <summary>True when the search field holds text or a pasted chip (Escape clears it before leaving it).</summary>
    public bool SearchHasText => searchField != null && (searchField.text.Length > 0 || (searchChip != null && searchChip.Chip != null));

    /// <summary>True for the app's search field.</summary>
    public bool IsSearchField(TMP_InputField field) => field != null && field == searchField;

    /// <summary>The zoom levels (DesktopConfigSO).</summary>
    private IReadOnlyList<int> Levels => config != null ? config.zoomLevels : null;

    /// <summary>The player's default zoom (Settings' Text size).</summary>
    private int DefaultZoom => AppZoom.Parse(DesktopPreferences.DefaultZoom, Levels);

    /// <summary>The pins and recent items with their knobs, the views' items followed, the sidebar and the zoom as the player left them (once, from Init).</summary>
    private void InitKeys()
    {
        _pins = new PinBoard(config != null ? config.pinsMax : 1);
        _recent = new RecentList(config != null ? config.recentItems : 1);
        if (panes != null)
            _panesInset = panes.offsetMin.x;
        foreach (AppPane pane in Panes())
            foreach (AppTab tab in TabOrder.Default)
            {
                IAppView view = pane.View(tab);
                if (view != null)
                    view.ChipsChanged += () => ItemChanged(view);
            }
        foreach (Button pin in pinButtons)
            if (pin != null)
                pin.onClick.AddListener(PinPaneItem);
        ApplySidebar(DesktopPreferences.SidebarShown);
        SetZoom(DefaultZoom);
        DrawLists();
    }

    /// <summary>The day starts: every pin and recent item goes (PR2).</summary>
    public void BeginDay()
    {
        Init();
        _pins.EndDay();
        _recent.EndDay();
        _opened.Clear();
        DrawLists();
    }

    /// <summary>A traveller is presented: the last case's pins and recent items go; clips are theirs from now on.</summary>
    private void KeysBeginCase(string travellerName)
    {
        _traveller = travellerName ?? string.Empty;
        _caseOn = true;
        DropCaseItems();
    }

    /// <summary>The decision: the case's pins and recent items go; the dock and the decision leave the regions.</summary>
    private void KeysEndCase()
    {
        _traveller = string.Empty;
        _caseOn = false;
        DropCaseItems();
        if (_region == AppRegion.Dock || _region == AppRegion.Decision)
            SetRegion(AppRegion.PaneContent, 0, _ringOn);
    }

    private void DropCaseItems()
    {
        _pins.EndCase();
        _recent.EndCase();
        foreach (AppTab tab in TabOrder.Default)
            if (TabOrder.IsCaseSource(tab))
                _opened.Remove(tab);
        DrawLists();
    }

    /// <summary>Runs a shortcut's command (DesktopKeyboard resolved it for the focused app).</summary>
    public void Run(AppCommand command)
    {
        Init();
        switch (command)
        {
            case AppCommand.FocusSearch:
                FocusSearch();
                break;
            case AppCommand.Tab1:
            case AppCommand.Tab2:
            case AppCommand.Tab3:
            case AppCommand.Tab4:
            case AppCommand.Tab5:
            case AppCommand.Tab6:
                ShowTabAt(ShortcutMap.TabPosition(command) - 1);
                break;
            case AppCommand.NextTab:
                StepTab(1);
                break;
            case AppCommand.PrevTab:
                StepTab(-1);
                break;
            case AppCommand.ToggleSidebar:
                ApplySidebar(!_sidebarShown);
                DesktopPreferences.SidebarShown = _sidebarShown;
                break;
            case AppCommand.NextRegion:
                MoveRegion(1);
                break;
            case AppCommand.PrevRegion:
                MoveRegion(-1);
                break;
            case AppCommand.RowUp:
                MoveItem(-1);
                break;
            case AppCommand.RowDown:
                MoveItem(1);
                break;
            case AppCommand.RowFirst:
                MoveItemTo(0);
                break;
            case AppCommand.RowLast:
                MoveItemTo(int.MaxValue);
                break;
            case AppCommand.PageUp:
                TurnPage(-1);
                break;
            case AppCommand.PageDown:
                TurnPage(1);
                break;
            case AppCommand.Follow:
                PressFocused(true);
                break;
            case AppCommand.FollowOther:
                PressFocused(false);
                break;
            case AppCommand.Back:
                ActivePane.Back();
                break;
            case AppCommand.Forward:
                ActivePane.Forward();
                break;
            case AppCommand.OtherPane:
                if (_split)
                    Activate(Other(ActivePane));
                Refocus();
                break;
            case AppCommand.ToggleSplit:
                ToggleSplit();
                Refocus();
                break;
            case AppCommand.MoveTabLeft:
                MoveTab(ActivePane.ActiveTab, -1);
                break;
            case AppCommand.MoveTabRight:
                MoveTab(ActivePane.ActiveTab, 1);
                break;
            case AppCommand.Pick:
                FocusedRow()?.Pick();
                break;
            case AppCommand.Copy:
                Copy(FocusedRow(), false);
                break;
            case AppCommand.CopyRow:
                Copy(FocusedRow(), true);
                break;
            case AppCommand.Pin:
                PinFocused();
                break;
            case AppCommand.ZoomIn:
                SetZoom(AppZoom.Step(Levels, _zoom, 1));
                break;
            case AppCommand.ZoomOut:
                SetZoom(AppZoom.Step(Levels, _zoom, -1));
                break;
            case AppCommand.ZoomReset:
                SetZoom(DefaultZoom);
                break;
            // ToggleSteps comes with the steps (phase 21).
        }
    }

    /// <summary>Settings' Text size: the player's default zoom, saved and shown now.</summary>
    public void SetZoomDefault(int level)
    {
        Init();
        DesktopPreferences.DefaultZoom = level.ToString(CultureInfo.InvariantCulture);
        SetZoom(level);
    }

    /// <summary>Escape's ClearSearch: the search field's text and chip go (the field keeps the keyboard).</summary>
    public void ClearSearch()
    {
        if (searchChip != null)
            searchChip.Clear();
        else if (searchField != null)
            searchField.text = string.Empty;
    }

    /// <summary>Escape left <paramref name="field"/>: a field of the app hands the keyboard back to its pane.</summary>
    public void FieldLeft(TMP_InputField field)
    {
        if (field != null && field.transform.IsChildOf(transform))
            SetRegion(AppRegion.PaneContent, 0, true);
    }

    /// <summary>A mouse press on the desktop: the pointer leads again, the focus ring hides.</summary>
    public void PointerPressed()
    {
        _ringOn = false;
        if (focusRing != null)
            focusRing.Hide();
    }

    /// <summary>A row's right-click: its context menu (Copy value, Copy row, Pin or Unpin, Pick for compare).</summary>
    public void ShowRowMenu(AppRow row, PointerEventData eventData)
    {
        if (rowMenu != null && row != null)
            rowMenu.ShowForRow(this, row, _pins.IsPinned(row.Key), eventData);
    }

    /// <summary>Copies <paramref name="row"/>'s value as shown, or "Label: value" (<paramref name="wholeRow"/>), to the clipboard and the system clipboard.</summary>
    public void Copy(AppRow row, bool wholeRow)
    {
        if (row == null)
            return;
        Clip clip = row.ToClip(wholeRow, _traveller);
        _clipboard.Copy(clip);
        if (_clipboard.Current != clip)
            return;
        GUIUtility.systemCopyBuffer = clip.Text;
        Notice(UiText.Format("app.copied", clip.Text));
    }

    /// <summary>Pins <paramref name="row"/>, or unpins it.</summary>
    public void TogglePin(AppRow row)
    {
        if (row != null)
            TogglePin(row.Key, row.Title);
    }

    /// <summary>Unpins the item with <paramref name="key"/> (a pin's right-click).</summary>
    public void Unpin(string key)
    {
        if (_pins.Unpin(key))
            DrawLists();
    }

    /// <summary>A pin or recent item clicked: its tab, its item and its row shown, the ring on the row, the item first in Recent.</summary>
    public void Jump(EntryItem item)
    {
        Init();
        if (window != null)
            window.Open();
        AppPane pane = ActivePane;
        pane.Show(item.Ref.Source);
        if (!(pane.View(item.Ref.Source) is IAppItems items) || !items.Reveal(item.Ref.Key))
        {
            Notice(UiText.Get("app.jump.gone"));
            return;
        }
        _recent.Touch(item.Ref, item.Label);
        DrawLists();
        FocusRow(item.Ref.Key);
    }

    /// <summary>The active view showed another item (a chip chosen, a record looked up): it goes first in Recent.</summary>
    private void ItemChanged(IAppView view)
    {
        if (!IsShowing || !(view is IAppItems items) || !Showing(view))
            return;
        string key = items.ItemKey;
        if (key == null || (_opened.TryGetValue(view.Tab, out string last) && last == key))
            return;
        _opened[view.Tab] = key;
        if (EntryKeys.TryRef(key, out EntryRef entry))
        {
            _recent.Touch(entry, items.ItemTitle);
            DrawLists();
        }
    }

    /// <summary>Ctrl+F: the app opened or restored, its search field focused (its text selected).</summary>
    private void FocusSearch()
    {
        if (window != null)
            window.Open();
        SetRegion(AppRegion.Search, 0, true);
    }

    /// <summary>The active pane shows the tab at <paramref name="position"/> (0-based, in the shared tab order).</summary>
    private void ShowTabAt(int position)
    {
        if (position < 0 || position >= _order.Tabs.Count)
            return;
        ActivePane.Show(_order.Tabs[position]);
        Refocus();
    }

    /// <summary>The next (1) or previous (-1) tab, round the strip.</summary>
    private void StepTab(int direction)
    {
        int count = _order.Tabs.Count;
        int at = Mathf.Max(0, _order.PositionOf(ActivePane.ActiveTab));
        ShowTabAt(((at + direction) % count + count) % count);
    }

    /// <summary>Tab (1) or Shift+Tab (-1): the next region that is there; the first press starts at the search field (or the last region).</summary>
    private void MoveRegion(int direction)
    {
        AppRegion next = _ringOn
            ? AppFocus.Next(_region, _split, _sidebarShown, _caseOn, direction)
            : direction > 0 ? AppRegion.Search : AppFocus.Next(AppRegion.Search, _split, _sidebarShown, _caseOn, -1);
        SetRegion(next, -1, true);
    }

    /// <summary>
    /// Puts the focus in <paramref name="region"/> on item <paramref name="item"/>
    /// (-1: the region's own: the active tab, the chosen chip, else the first);
    /// the search field takes the keyboard in its region and gives it up outside it.
    /// </summary>
    private void SetRegion(AppRegion region, int item, bool ringOn)
    {
        if (region != AppRegion.Search && searchField != null && searchField.isFocused)
        {
            searchField.DeactivateInputField();
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);
        }
        _region = region;
        _ringOn = ringOn;
        Collect();
        _item = item >= 0 ? item : RegionItem(region);
        _item = Mathf.Clamp(_item, 0, Mathf.Max(0, _targets.Count - 1));
        if (region == AppRegion.Search && searchField != null && searchField.gameObject.activeInHierarchy)
        {
            searchField.Select();
            searchField.ActivateInputField();
        }
        ShowRing();
    }

    /// <summary>The item a region starts on: the active tab on the strip, the chosen chip in the header, else the first.</summary>
    private int RegionItem(AppRegion region)
    {
        if (region == AppRegion.TabStrip)
            return Mathf.Max(0, _order.PositionOf(ActivePane.ActiveTab));
        if (region == AppRegion.PaneHeader)
        {
            IAppView view = ActivePane.View(ActivePane.ActiveTab);
            return view != null && view.Selected >= 0 ? view.Selected : 0;
        }
        return 0;
    }

    /// <summary>The ring re-collected after the view changed under it (a tab shown): the strip follows the active tab, a list keeps its place.</summary>
    private void Refocus()
    {
        if (_ringOn)
            SetRegion(_region, _region == AppRegion.TabStrip ? -1 : 0, true);
    }

    /// <summary>The ring moves by <paramref name="delta"/> items; past a content page's end it turns the page.</summary>
    private void MoveItem(int delta)
    {
        Collect();
        int next = _item + delta;
        if (IsContent(_region) && (next < 0 || next >= _targets.Count) && TurnPage(delta))
            return;
        _item = Mathf.Clamp(next, 0, Mathf.Max(0, _targets.Count - 1));
        ShowRing();
    }

    /// <summary>Home (0) or End (int.MaxValue): the first or last item of the region.</summary>
    private void MoveItemTo(int item)
    {
        Collect();
        _item = Mathf.Clamp(item, 0, Mathf.Max(0, _targets.Count - 1));
        ShowRing();
    }

    /// <summary>PgUp (-1) or PgDn (1) in the content: the view's page turned, the ring on its first (or, going back, last) row; false when there is no page that way.</summary>
    private bool TurnPage(int direction)
    {
        AppPane pane = ContentPane();
        if (pane == null || !(pane.View(pane.ActiveTab) is Component view))
            return false;
        view.GetComponentsInChildren(false, _pagesScratch);
        if (_pagesScratch.Count == 0 || !_pagesScratch[0].TurnPage(direction))
            return false;
        Collect();
        _item = direction > 0 ? 0 : Mathf.Max(0, _targets.Count - 1);
        ShowRing();
        return true;
    }

    /// <summary>Enter: presses the focused item (a chip, the pin button, a pin or recent item, the dock's clear, Accept or Deny) or follows the focused row's smart link in its own pane (<paramref name="samePane"/>) or the other one (Ctrl+Enter).</summary>
    private void PressFocused(bool samePane)
    {
        Collect();
        if (_item >= _targets.Count)
            return;
        if (IsContent(_region))
        {
            AppRow row = _targets[_item].GetComponent<AppRow>();
            if (row != null && !row.Link.IsNone)
                Follow(ContentPane(), row.Link, samePane);
            return;
        }
        RectTransform target = _targets[_item];
        if (target.TryGetComponent(out Button button) && button.interactable)
            button.onClick.Invoke();
        Refocus();
    }

    /// <summary>The row the ring is on in the content, or null.</summary>
    private AppRow FocusedRow()
    {
        if (!_ringOn || !IsContent(_region))
            return null;
        Collect();
        return _item < _targets.Count ? _targets[_item].GetComponent<AppRow>() : null;
    }

    /// <summary>Ctrl+P: the focused row pinned or unpinned, a focused pin unpinned, else the pane's item.</summary>
    private void PinFocused()
    {
        Collect();
        RectTransform target = _ringOn && _item < _targets.Count ? _targets[_item] : null;
        if (target != null && IsContent(_region) && target.TryGetComponent(out AppRow row))
            TogglePin(row);
        else if (target != null && _region == AppRegion.Sidebar && target.TryGetComponent(out SidebarEntryRow entry))
            Unpin(entry.Item.Ref.Key);
        else
            PinPaneItem();
    }

    /// <summary>The pane header's pin (and Ctrl+P elsewhere): the item the pane shows, pinned or unpinned.</summary>
    private void PinPaneItem()
    {
        AppPane pane = ActivePane;
        if (pane.View(pane.ActiveTab) is IAppItems items && items.ItemKey != null)
            TogglePin(items.ItemKey, items.ItemTitle);
    }

    /// <summary>Pins or unpins the item <paramref name="key"/>; a full board says so.</summary>
    private void TogglePin(string key, string label)
    {
        if (!EntryKeys.TryRef(key, out EntryRef entry))
            return;
        if (_pins.Toggle(entry, label) == PinChange.Full)
            Notice(UiText.Get("app.pins.full"));
        DrawLists();
    }

    /// <summary>The ring on the content's row with <paramref name="key"/> (a jump), else on the first row.</summary>
    private void FocusRow(string key)
    {
        _region = AppRegion.PaneContent;
        _ringOn = true;
        Collect();
        _item = 0;
        for (int i = 0; i < _targets.Count; i++)
            if (_targets[i].TryGetComponent(out AppRow row) && row.Key == key)
                _item = i;
        ShowRing();
    }

    /// <summary>The focused region's items, in order (only what shows).</summary>
    private void Collect()
    {
        _targets.Clear();
        switch (_region)
        {
            case AppRegion.Search:
                Add(searchField);
                break;
            case AppRegion.TabStrip:
                foreach (AppTab tab in _order.Tabs)
                    Add(ActivePane.TabButton(tab));
                break;
            case AppRegion.PaneHeader:
                foreach (Button chip in ActivePane.ChipButtons)
                    Add(chip);
                foreach (Button pin in pinButtons)
                    if (pin != null && pin.transform.IsChildOf(ActivePane.transform))
                        Add(pin);
                break;
            case AppRegion.PaneContent:
            case AppRegion.OtherPane:
                AppPane pane = ContentPane();
                if (pane != null && pane.View(pane.ActiveTab) is Component view && view.gameObject.activeInHierarchy)
                {
                    view.GetComponentsInChildren(false, _rowScratch);
                    foreach (AppRow row in _rowScratch)
                        Add(row);
                }
                break;
            case AppRegion.Sidebar:
                AddRows(pinsList);
                AddRows(recentList);
                break;
            case AppRegion.Dock:
                Add(dockClearButton);
                break;
            case AppRegion.Decision:
                Add(acceptButton);
                Add(denyButton);
                break;
        }
    }

    private void AddRows(SidebarEntryList list)
    {
        if (list != null)
            foreach (SidebarEntryRow row in list.Rows)
                Add(row);
    }

    private void Add(Component part)
    {
        if (part != null && part.gameObject.activeInHierarchy)
            _targets.Add((RectTransform)part.transform);
    }

    /// <summary>The ring on the focused item (the content scrolled to it), else on the region's own frame when it has no items.</summary>
    private void ShowRing()
    {
        if (focusRing == null)
            return;
        if (!_ringOn)
        {
            focusRing.Hide();
            return;
        }
        RectTransform target = _item < _targets.Count ? _targets[_item] : RegionFrame();
        RectTransform clip = null;
        if (IsContent(_region) && target != null)
            foreach (PaneZoom zoom in zooms)
                if (zoom != null && target != zoom.transform && target.IsChildOf(zoom.transform))
                {
                    clip = RevealInView(target, zoom);
                    zoom.Reveal(target);
                }
        focusRing.Show(target, clip);
    }

    /// <summary>
    /// A row inside a view's own scroll (a scanned copy's page) is scrolled
    /// into that scroll's viewport, which then cuts the ring; else the pane's
    /// viewport does. Returns the viewport that cuts the ring.
    /// </summary>
    private static RectTransform RevealInView(RectTransform target, PaneZoom zoom)
    {
        ScrollRect inner = target.GetComponentInParent<ScrollRect>();
        if (inner == null || inner.transform == zoom.transform || inner.content == null)
            return (RectTransform)zoom.transform;
        RectTransform viewport = inner.viewport != null ? inner.viewport : (RectTransform)inner.transform;
        Bounds b = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, target);
        Rect view = viewport.rect;
        float shift = b.max.y > view.yMax ? view.yMax - b.max.y : b.min.y < view.yMin ? view.yMin - b.min.y : 0f;
        if (shift != 0f)
        {
            inner.StopMovement();
            inner.content.anchoredPosition += new Vector2(0f, shift);
        }
        return viewport;
    }

    /// <summary>A region without items shows the ring round it: the pane's content, the sidebar, the dock; else nothing.</summary>
    private RectTransform RegionFrame()
    {
        AppPane pane = IsContent(_region) ? ContentPane() : null;
        if (pane != null)
            foreach (PaneZoom zoom in zooms)
                if (zoom != null && zoom.transform.IsChildOf(pane.transform))
                    return (RectTransform)zoom.transform;
        if (_region == AppRegion.Sidebar && sidebar != null)
            return sidebar;
        if (_region == AppRegion.Dock && compareDock != null && compareDock.gameObject.activeInHierarchy)
            return compareDock;
        return null;
    }

    /// <summary>Shows or hides the sidebar; the panes take its width while it is hidden.</summary>
    private void ApplySidebar(bool shown)
    {
        _sidebarShown = shown;
        if (sidebar != null && sidebar.gameObject.activeSelf != shown)
            sidebar.gameObject.SetActive(shown);
        if (panes != null)
            panes.offsetMin = new Vector2(shown ? _panesInset : 0f, panes.offsetMin.y);
        Layout();
        if (!shown && _region == AppRegion.Sidebar)
            SetRegion(AppRegion.PaneContent, 0, _ringOn);
    }

    /// <summary>Every pane's content at <paramref name="level"/> %.</summary>
    private void SetZoom(int level)
    {
        _zoom = level;
        foreach (PaneZoom zoom in zooms)
            if (zoom != null)
                zoom.Apply(AppZoom.Scale(level));
        if (_ringOn)
            ShowRing();
    }

    /// <summary>The sidebar's Pinned and Recent lists redrawn (the ring kept on the sidebar's rows).</summary>
    private void DrawLists()
    {
        if (pinsList != null)
            pinsList.Show(_pins.Items);
        if (recentList != null)
            recentList.Show(_recent.Items);
        if (_ringOn && _region == AppRegion.Sidebar)
        {
            Collect();
            _item = Mathf.Clamp(_item, 0, Mathf.Max(0, _targets.Count - 1));
            ShowRing();
        }
    }

    /// <summary>The pane the keys act on: the active one.</summary>
    private AppPane ActivePane => _active != null ? _active : leftPane;

    /// <summary>True for a content region: the active pane's (PaneContent) or, while split, the other pane's (OtherPane).</summary>
    private static bool IsContent(AppRegion region) => region == AppRegion.PaneContent || region == AppRegion.OtherPane;

    /// <summary>The pane whose content the ring is in: the active one, or the other one in the OtherPane region; null outside the content.</summary>
    private AppPane ContentPane()
    {
        if (_region == AppRegion.PaneContent)
            return ActivePane;
        return _region == AppRegion.OtherPane && _split ? Other(ActivePane) : null;
    }

    /// <summary>True when <paramref name="view"/> is the active view of a showing pane.</summary>
    private bool Showing(IAppView view)
    {
        foreach (AppPane pane in Panes())
            if ((pane == leftPane || _split) && pane.ActiveTab == view.Tab && pane.View(view.Tab) == view)
                return true;
        return false;
    }

    /// <summary>A short line on the app's toast (no Open).</summary>
    private void Notice(string line)
    {
        if (toast != null)
            toast.Show(line, config != null ? config.toastSeconds : 4f, null);
    }
}
