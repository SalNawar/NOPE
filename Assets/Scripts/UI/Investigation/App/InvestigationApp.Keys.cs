using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// The Investigation app's keys, focus ring, clipboard, pins, recent items
/// and zoom (redesign phase 20; the PC redesign KB1-KB5, CP1-CP3, PR1-PR2;
/// the PC workbench spec section 7). The desktop's keyboard poller
/// (DesktopKeyboard) runs the app's commands here. Ctrl+K and Ctrl+F open or
/// restore the app and open its search drawer, the field focused; Ctrl+1…5
/// go to a guided step, Ctrl+Tab and Ctrl+Shift+Tab (and the arrows on the
/// steps) the next or previous one; F6 makes the other side the target;
/// Ctrl+\ joins or splits the panes; Ctrl+B hides or shows the findings
/// column (the panes take its width; saved per player); Ctrl+Shift+S the
/// steps' hints; Alt+← and Alt+→ walk the target pane's history. Tab and
/// Shift+Tab walk the regions (AppFocus: with the drawer open its field and
/// its hits (or its pins and recent items before anything is typed);
/// otherwise the steps, the shelf, the target pane's values, the other
/// pane's, the findings, the held value's Cancel, Accept and Deny at the
/// decision) with the focus ring, which scrolls a list to what it is on; ↓ in
/// the field takes it to the first hit, ↑ on the first hit back to the field;
/// inside a region the arrows, Home, End, PgUp and PgDn move the ring over its
/// items (the values in reading order, turning the page at a page's end),
/// Enter presses the focused item (a step, a chip, a finding, Cancel, a hit,
/// a pin, Accept or Deny) or follows the focused value's smart link in its
/// own pane (Ctrl+Enter: the other one), Space picks the focused value (as a
/// click does: held, then matched), Ctrl+C copies its value as shown and
/// Ctrl+Shift+C "Label: value" (the one clipboard, AppClipboard, and the
/// system clipboard), Ctrl+P pins the focused value or the target pane's
/// document. Pins (PinBoard, at most DesktopConfigSO.pinsMax) and recent
/// items (RecentList: the documents the player opens or jumps to) show in
/// the search drawer before anything is typed; a click on one jumps (its
/// document on the target side, its row with the ring on it); the case's are
/// dropped when a case starts or ends, the day's when the day starts. Ctrl+=
/// and Ctrl+- zoom the panes' content (PaneZoom) to the next level (100,
/// 125, 150 %), Ctrl+0 back to Settings' Text size. A mouse press hides the
/// ring.
/// </summary>
public sealed partial class InvestigationApp
{
    [Header("Keys, clipboard, pins, zoom (redesign phase 20)")]
    /// <summary>The search drawer's field (Ctrl+K, Ctrl+F; a paste target).</summary>
    [SerializeField] private TMP_InputField searchField;

    /// <summary>The search field's chip for a pasted untranslated line.</summary>
    [SerializeField] private SearchFieldChip searchChip;

    /// <summary>The search drawer's Pinned list.</summary>
    [SerializeField] private SidebarEntryList pinsList;

    /// <summary>The search drawer's Recent list.</summary>
    [SerializeField] private SidebarEntryList recentList;

    /// <summary>The keyboard focus ring.</summary>
    [SerializeField] private AppFocusRing focusRing;

    /// <summary>Each pane's zoom.</summary>
    [SerializeField] private PaneZoom[] zooms = new PaneZoom[0];

    /// <summary>The decision's Accept (the Decision region).</summary>
    [SerializeField] private Button acceptButton;

    /// <summary>The decision's Deny (the Decision region).</summary>
    [SerializeField] private Button denyButton;

    /// <summary>The desktop's context menu (a row's Copy value, Copy row, Pin, Pick for compare).</summary>
    [SerializeField] private DesktopContextMenu rowMenu;

    private readonly AppClipboard _clipboard = new AppClipboard();
    private readonly List<RectTransform> _targets = new List<RectTransform>();
    private readonly List<FormPage> _pagesScratch = new List<FormPage>();
    private readonly Dictionary<AppTab, string> _recentOpened = new Dictionary<AppTab, string>();
    private PinBoard _pins = new PinBoard(1);
    private RecentList _recent = new RecentList(1);
    private AppRegion _region = AppRegion.PaneContent;
    private int _item;
    private bool _ringOn;
    private bool _caseOn;
    private bool _findingsShown = true;
    private bool _railShown;
    private float _findingsWidth;
    private float _findingsGap;
    private int _zoom = AppZoom.Normal;
    private string _clipTraveller = string.Empty;

    /// <summary>The desktop's one clipboard (Notes and the search field read its clip).</summary>
    public AppClipboard Clipboard => _clipboard;

    /// <summary>True while the focus ring is on a list's item (every region but the search field and the steps).</summary>
    public bool ListFocused => _ringOn && _region != AppRegion.Search && _region != AppRegion.Steps;

    /// <summary>True while the focus ring is on the guided steps (the arrows walk them).</summary>
    public bool StepsFocused => _ringOn && _region == AppRegion.Steps;

    /// <summary>True when the search field holds text or a pasted chip (Escape clears it before closing the drawer).</summary>
    public bool SearchHasText => searchField != null && (searchField.text.Length > 0 || (searchChip != null && searchChip.Chip != null));

    /// <summary>True for the app's search field.</summary>
    public bool IsSearchField(TMP_InputField field) => field != null && field == searchField;

    /// <summary>True while the workbench holds a value, a rule or the date (Escape lets go).</summary>
    public bool Holding => board != null && board.IsHolding;

    /// <summary>Escape's ReleaseHold: the held value is let go.</summary>
    public void ReleaseHold()
    {
        if (board != null)
            board.Release();
    }

    /// <summary>The zoom levels (DesktopConfigSO).</summary>
    private IReadOnlyList<int> Levels => config != null ? config.zoomLevels : null;

    /// <summary>The player's default zoom (Settings' Text size).</summary>
    private int DefaultZoom => AppZoom.Parse(DesktopPreferences.DefaultZoom, Levels);

    /// <summary>What the regions read of the app now.</summary>
    private AppFocusState FocusState => new AppFocusState(SearchOpen, ResultsListed || QuickRowsListed, _split, _findingsShown, _caseOn, Holding, _deciding);

    /// <summary>The pins and recent items with their knobs, the views' items followed, the findings column and the zoom as the player left them (once, from Init).</summary>
    private void InitKeys()
    {
        _pins = new PinBoard(config != null ? config.pinsMax : 1);
        _recent = new RecentList(config != null ? config.recentItems : 1);
        if (findingsColumn != null && mainColumn != null)
        {
            _findingsWidth = -findingsColumn.offsetMin.x;
            _findingsGap = -mainColumn.offsetMax.x - _findingsWidth;
        }
        foreach (AppPane pane in Panes())
            foreach (AppTab tab in TabOrder.Default)
            {
                IAppView view = pane.View(tab);
                if (view != null)
                    view.ChipsChanged += () => ItemChanged(view);
            }
        ApplyFindings(DesktopPreferences.SidebarShown);
        SetZoom(DefaultZoom);
        DrawLists();
    }

    /// <summary>The day starts: every pin and recent item goes (PR2).</summary>
    public void BeginDay()
    {
        Init();
        _pins.EndDay();
        _recent.EndDay();
        _recentOpened.Clear();
        DrawLists();
    }

    /// <summary>A traveller is presented: the last case's pins and recent items go; clips are theirs from now on.</summary>
    private void KeysBeginCase(string travellerName)
    {
        _clipTraveller = travellerName ?? string.Empty;
        _caseOn = true;
        DropCaseItems();
    }

    /// <summary>The decision: the case's pins and recent items go; Cancel and the decision leave the regions.</summary>
    private void KeysEndCase()
    {
        _clipTraveller = string.Empty;
        _caseOn = false;
        DropCaseItems();
        if (_region == AppRegion.Holding || _region == AppRegion.Decision)
            SetRegion(AppFocus.Home(FocusState), 0, _ringOn);
    }

    private void DropCaseItems()
    {
        _pins.EndCase();
        _recent.EndCase();
        foreach (AppTab tab in TabOrder.Default)
            if (TabOrder.IsCaseSource(tab))
                _recentOpened.Remove(tab);
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
            case AppCommand.IntoResults:
                SetRegion(AppRegion.Results, 0, true);
                break;
            case AppCommand.Step1:
            case AppCommand.Step2:
            case AppCommand.Step3:
            case AppCommand.Step4:
            case AppCommand.Step5:
                if (guide != null)
                    guide.GoTo(ShortcutMap.StepPosition(command));
                Refocus();
                break;
            case AppCommand.NextStep:
            case AppCommand.PrevStep:
                if (guide != null)
                    guide.Step(command == AppCommand.NextStep ? 1 : -1);
                Refocus();
                break;
            case AppCommand.ToggleFindings:
                ApplyFindings(!_findingsShown);
                DesktopPreferences.SidebarShown = _findingsShown;
                break;
            case AppCommand.ToggleHints:
                if (guide != null)
                    guide.Toggle();
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
                TargetPane.Back();
                break;
            case AppCommand.Forward:
                TargetPane.Forward();
                break;
            case AppCommand.OtherPane:
                if (_split)
                    SetTarget(Other(TargetPane));
                Refocus();
                break;
            case AppCommand.ToggleSplit:
                ToggleSplit();
                Refocus();
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
        }
    }

    /// <summary>Settings' Text size: the player's default zoom, saved and shown now.</summary>
    public void SetZoomDefault(int level)
    {
        Init();
        DesktopPreferences.DefaultZoom = level.ToString(CultureInfo.InvariantCulture);
        SetZoom(level);
    }

    /// <summary>Escape's ClearSearch: the search field's text and chip go (the field keeps the keyboard, the drawer stays open).</summary>
    public void ClearSearch()
    {
        if (searchChip != null)
            searchChip.Clear();
        else if (searchField != null)
            searchField.text = string.Empty;
    }

    /// <summary>Escape left <paramref name="field"/>: a field of the app hands the keyboard back to the target pane.</summary>
    public void FieldLeft(TMP_InputField field)
    {
        if (field != null && field.transform.IsChildOf(transform))
            SetRegion(AppFocus.Home(FocusState), 0, true);
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

    /// <summary>Copies <paramref name="row"/>'s value as shown, or "Label: value" (<paramref name="wholeRow"/>), to the clipboard and the system clipboard; the toast names an untranslated line by its source (its glyphs are not in the chrome's font).</summary>
    public void Copy(AppRow row, bool wholeRow)
    {
        if (row == null)
            return;
        Clip clip = row.ToClip(wholeRow, _clipTraveller);
        _clipboard.Copy(clip);
        if (_clipboard.Current != clip)
            return;
        GUIUtility.systemCopyBuffer = clip.Text;
        Notice(UiText.Format("app.copied", clip.Foreign ? clip.SourceLabel : clip.Text));
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

    /// <summary>A pin or recent item clicked: its document on the target side (SmartLinks.ForEntry, recorded in the pane's history), the ring on its row, the item first in Recent, the drawer closed; an item that is gone says so.</summary>
    public void Jump(EntryItem item)
    {
        Init();
        CloseResults();
        LinkTarget target = SmartLinks.ForEntry(item.Ref.Key, _papers);
        if (target.IsNone || !OpenOnTarget(target))
        {
            Notice(UiText.Get("app.jump.gone"));
            return;
        }
        _recent.Touch(item.Ref, item.Label);
        DrawLists();
        FocusRow(item.Ref.Key);
    }

    /// <summary>The target view showed another item (a document opened, a record looked up): it goes first in Recent.</summary>
    private void ItemChanged(IAppView view)
    {
        if (!IsShowing || !(view is IAppItems items) || !Showing(view))
            return;
        string key = items.ItemKey;
        if (key == null || (_recentOpened.TryGetValue(view.Tab, out string last) && last == key))
            return;
        _recentOpened[view.Tab] = key;
        if (EntryKeys.TryRef(key, out EntryRef entry))
        {
            _recent.Touch(entry, items.ItemTitle);
            DrawLists();
        }
    }

    /// <summary>Ctrl+K, Ctrl+F: the app opened or restored, the search drawer open, its field focused (its text selected).</summary>
    private void FocusSearch()
    {
        if (window != null)
            window.Open();
        OpenSearch();
        SetRegion(AppRegion.Search, 0, true);
    }

    /// <summary>Tab (1) or Shift+Tab (-1): the next region that is there; the first press starts at the region's home.</summary>
    private void MoveRegion(int direction)
    {
        AppFocusState state = FocusState;
        AppRegion next = _ringOn && AppFocus.Available(_region, state) ? AppFocus.Next(_region, state, direction) : AppFocus.Home(state);
        SetRegion(next, -1, true);
    }

    /// <summary>
    /// Puts the focus in <paramref name="region"/> on item <paramref name="item"/>
    /// (-1: the region's own: the current step, else the first); the search
    /// field takes the keyboard in its region and gives it up outside it.
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

    /// <summary>The item a region starts on: the current step on the steps, else the first.</summary>
    private int RegionItem(AppRegion region) => region == AppRegion.Steps && guide != null ? (int)guide.Current : 0;

    /// <summary>The ring re-collected after the view changed under it (a step, a split): the steps follow the current one, a list keeps its place; a region gone sends it home.</summary>
    private void Refocus()
    {
        if (!_ringOn)
            return;
        AppFocusState state = FocusState;
        if (!AppFocus.Available(_region, state))
            SetRegion(AppFocus.Home(state), 0, true);
        else
            SetRegion(_region, _region == AppRegion.Steps ? -1 : 0, true);
    }

    /// <summary>The ring moves by <paramref name="delta"/> items (the rows scroll into view as it goes); ↑ on the first hit goes back to the search field.</summary>
    private void MoveItem(int delta)
    {
        if (_region == AppRegion.Results && _item == 0 && delta < 0)
        {
            SetRegion(AppRegion.Search, 0, true);
            return;
        }
        Collect();
        _item = Mathf.Clamp(_item + delta, 0, Mathf.Max(0, _targets.Count - 1));
        ShowRing();
    }

    /// <summary>Home (0) or End (int.MaxValue): the first or last item of the region.</summary>
    private void MoveItemTo(int item)
    {
        Collect();
        _item = Mathf.Clamp(item, 0, Mathf.Max(0, _targets.Count - 1));
        ShowRing();
    }

    /// <summary>PgUp (-1) or PgDn (1) in a pane: the view's shown page scrolls by a viewport (FormPage.PageBy; the ring stays on its row); false when the page is at that end.</summary>
    private bool TurnPage(int direction)
    {
        AppPane pane = ContentPane();
        if (pane == null || !(pane.View(pane.ActiveTab) is Component view))
            return false;
        view.GetComponentsInChildren(false, _pagesScratch);
        foreach (FormPage page in _pagesScratch)
            if (page.gameObject.activeInHierarchy)
                return page.PageBy(direction);
        return false;
    }

    /// <summary>Enter: presses the focused item (a step, a chip, a finding, Cancel, a pin or recent item, Accept or Deny), opens the focused search hit, or follows the focused value's smart link, in its own pane (<paramref name="samePane"/>) or the other one (Ctrl+Enter).</summary>
    private void PressFocused(bool samePane)
    {
        Collect();
        if (_item >= _targets.Count)
            return;
        if (_region == AppRegion.Results && ResultsListed)
        {
            if (searchBox != null)
                searchBox.OpenRow(_targets[_item], !samePane);
            return;
        }
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
        else if (target.TryGetComponent(out SidebarEntryRow entry))
            Jump(entry.Item);
        Refocus();
    }

    /// <summary>The value the ring is on in a pane, or null.</summary>
    private AppRow FocusedRow()
    {
        if (!_ringOn || !IsContent(_region))
            return null;
        Collect();
        return _item < _targets.Count ? _targets[_item].GetComponent<AppRow>() : null;
    }

    /// <summary>Ctrl+P: the focused value pinned or unpinned, a focused pin unpinned, else the target pane's document.</summary>
    private void PinFocused()
    {
        Collect();
        RectTransform target = _ringOn && _item < _targets.Count ? _targets[_item] : null;
        if (target != null && IsContent(_region) && target.TryGetComponent(out AppRow row))
            TogglePin(row);
        else if (target != null && _region == AppRegion.Results && target.TryGetComponent(out SidebarEntryRow entry))
            Unpin(entry.Item.Ref.Key);
        else
            PinPaneItem();
    }

    /// <summary>The target pane's document, pinned or unpinned.</summary>
    private void PinPaneItem()
    {
        AppPane pane = TargetPane;
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

    /// <summary>The ring on the target pane's row with <paramref name="key"/> (a jump), else on its first row.</summary>
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
            case AppRegion.Results:
                if (ResultsListed)
                    foreach (Button hit in searchBox.HitRows)
                        Add(hit);
                else
                {
                    AddRows(pinsList);
                    AddRows(recentList);
                }
                break;
            case AppRegion.Steps:
                if (guide != null)
                    foreach (Button pill in guide.Pills)
                        Add(pill);
                break;
            case AppRegion.Shelf:
                if (shelf != null)
                    foreach (Button chip in shelf.Buttons)
                        Add(chip);
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
            case AppRegion.Findings:
                if (board != null && board.Findings != null)
                    foreach (Button finding in board.Findings.Buttons)
                        Add(finding);
                break;
            case AppRegion.Holding:
                if (board != null)
                    Add(board.CancelButton);
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

    /// <summary>True while the drawer's quick-open lists (Pinned, Recent) show rows.</summary>
    private bool QuickRowsListed => SearchOpen && !ResultsListed &&
                                    ((pinsList != null && pinsList.Rows.Count > 0) || (recentList != null && recentList.Rows.Count > 0));

    /// <summary>The ring on the focused item (a pane's value scrolled into view), else on the region's own frame when it has no items.</summary>
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
                    clip = RevealInView(target, (RectTransform)zoom.transform);
                    zoom.Reveal(target);
                }
        if ((_region == AppRegion.Results || _region == AppRegion.Findings) && target != null)
            clip = RevealInView(target, null);
        focusRing.Show(target, clip);
    }

    /// <summary>
    /// A row inside a scroll of its own (a scanned copy's page, the drawer's
    /// results, the findings) is scrolled into that scroll's viewport, which
    /// then cuts the ring; else <paramref name="outer"/> (the pane's viewport)
    /// does. Returns the viewport that cuts the ring.
    /// </summary>
    private static RectTransform RevealInView(RectTransform target, RectTransform outer)
    {
        ScrollRect inner = target.GetComponentInParent<ScrollRect>();
        if (inner == null || inner.transform == outer || inner.content == null)
            return outer;
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

    /// <summary>A region without items shows the ring round it: a pane's content, the findings column; else nothing.</summary>
    private RectTransform RegionFrame()
    {
        AppPane pane = IsContent(_region) ? ContentPane() : null;
        if (pane != null)
            foreach (PaneZoom zoom in zooms)
                if (zoom != null && zoom.transform.IsChildOf(pane.transform))
                    return (RectTransform)zoom.transform;
        if (_region == AppRegion.Findings && findingsColumn != null)
            return findingsColumn;
        return null;
    }

    /// <summary>Shows or hides the findings column (a slim rail while nothing is logged); the main column takes the width it leaves.</summary>
    private void ApplyFindings(bool shown)
    {
        _findingsShown = shown;
        _railShown = FindingsRail;
        float width = !shown ? 0f : _railShown ? findingsRail : _findingsWidth;
        if (findingsColumn != null)
        {
            if (findingsColumn.gameObject.activeSelf != shown)
                findingsColumn.gameObject.SetActive(shown);
            findingsColumn.offsetMin = new Vector2(-width, findingsColumn.offsetMin.y);
        }
        if (board != null && board.Findings != null)
            board.Findings.SetRail(_railShown);
        if (mainColumn != null)
            mainColumn.offsetMax = new Vector2(shown ? -(width + _findingsGap) : 0f, mainColumn.offsetMax.y);
        Layout();
        if (!shown && _region == AppRegion.Findings)
            SetRegion(AppFocus.Home(FocusState), 0, _ringOn);
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

    /// <summary>The drawer's Pinned and Recent lists redrawn (the ring kept on their rows).</summary>
    private void DrawLists()
    {
        if (pinsList != null)
            pinsList.Show(_pins.Items);
        if (recentList != null)
            recentList.Show(_recent.Items);
        if (_ringOn && _region == AppRegion.Results && !ResultsListed)
        {
            Collect();
            _item = Mathf.Clamp(_item, 0, Mathf.Max(0, _targets.Count - 1));
            ShowRing();
        }
    }

    /// <summary>True for a pane region: the target pane's (PaneContent) or, while split, the other one's (OtherPane).</summary>
    private static bool IsContent(AppRegion region) => region == AppRegion.PaneContent || region == AppRegion.OtherPane;

    /// <summary>The pane whose values the ring is in: the target, or the other one in the OtherPane region; null outside the panes.</summary>
    private AppPane ContentPane()
    {
        if (_region == AppRegion.PaneContent)
            return TargetPane;
        return _region == AppRegion.OtherPane && _split ? Other(TargetPane) : null;
    }

    /// <summary>True when <paramref name="view"/> is the active view of a showing pane.</summary>
    private bool Showing(IAppView view)
    {
        foreach (AppPane pane in ShowingPanes())
            if (pane.ActiveTab == view.Tab && pane.View(view.Tab) == view)
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
