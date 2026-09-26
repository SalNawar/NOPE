using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// One pane of the Investigation app (the PC redesign AP2-AP5, AP8, AP9): its
/// tab strip (one tab per source, laid out in the app's shared TabOrder, each
/// with a badge and a tooltip naming it; on a strip too narrow for every
/// label the inactive tabs show their glyphs: AppPanes.TabsNarrow), its
/// header (the active view's chips: a click shows that item) and its content
/// (the active tab's view; between travellers a case source shows the
/// no-case state, "Waiting for the next traveller", instead). The app has two
/// panes, each with its own views and its own back/forward history
/// (NavHistory of LinkTargets: every tab switch, item switch, lookup and link
/// is recorded; Back and Forward walk it without recording). A link followed
/// from one of its rows (FollowLink) goes to the app, which sends it to the
/// other pane (Ctrl held: this one). A tab is dragged along the strip past a
/// neighbour's middle, or moved from its right-click menu; the app reorders
/// both strips. A tab is shown only by the player or by the app's own rules
/// (a new case shows Documents in the left pane, AP8); a view never switches
/// the tab. The active pane wears a 3 u accent frame while the app is split.
/// </summary>
public sealed class AppPane : MonoBehaviour
{
    /// <summary>The tabs' buttons, indexed by the tab's value (AppTab).</summary>
    [SerializeField] private Button[] tabButtons = new Button[0];

    /// <summary>Each tab's active look (drawn over the button while it is the active tab), indexed by the tab's value.</summary>
    [SerializeField] private GameObject[] tabActive = new GameObject[0];

    /// <summary>Each tab's badge, indexed by the tab's value.</summary>
    [SerializeField] private GameObject[] tabBadges = new GameObject[0];

    /// <summary>Each tab's name on its button, indexed by the tab's value.</summary>
    [SerializeField] private GameObject[] tabLabels = new GameObject[0];

    /// <summary>Each tab's glyph, shown instead of its name on a narrow strip, indexed by the tab's value.</summary>
    [SerializeField] private GameObject[] tabGlyphs = new GameObject[0];

    /// <summary>The tab strip (its width decides whether the inactive tabs show glyphs).</summary>
    [SerializeField] private RectTransform tabStrip;

    /// <summary>The views, one per tab (their Tab says which).</summary>
    [SerializeField] private AppView[] views = new AppView[0];

    /// <summary>The header's chip row.</summary>
    [SerializeField] private RectTransform chipStrip;

    /// <summary>A chip (inactive), cloned per item of the active view.</summary>
    [SerializeField] private Button chipTemplate;

    /// <summary>The no-case state over the content (a case source's tab between travellers).</summary>
    [SerializeField] private GameObject noCase;

    /// <summary>The 3 u accent frame of the active pane (shown while the app is split).</summary>
    [SerializeField] private GameObject activeFrame;

    /// <summary>The tab the pane shows first.</summary>
    [SerializeField] private AppTab startTab = AppTab.Documents;

    /// <summary>The desktop's knobs: the tabs' label and glyph widths, the history's length.</summary>
    [SerializeField] private DesktopConfigSO config;

    /// <summary>The chosen chip's tint (pressed).</summary>
    [SerializeField] private Color chosenTint = new Color(0.72f, 0.72f, 0.72f, 1f);

    /// <summary>An unavailable item's chip tint (dimmed; it still shows why when clicked).</summary>
    [SerializeField] private Color unavailableTint = new Color(1f, 1f, 1f, 0.55f);

    private readonly Dictionary<AppTab, IAppView> _views = new Dictionary<AppTab, IAppView>();
    private readonly List<Button> _chips = new List<Button>();
    private readonly List<float> _middles = new List<float>();
    private NavHistory<LinkTarget> _history;
    private AppTab _active;
    private bool _caseOn;
    private bool _ready;

    /// <summary>Raised when a tab is shown (the app clears its badge).</summary>
    public event Action<AppPane, AppTab> Shown;

    /// <summary>Raised when the history changes (the toolbar's Back and Forward follow the active pane's).</summary>
    public event Action<AppPane> HistoryChanged;

    /// <summary>Raised when a row's link is followed here: the target, and true to stay in this pane (Ctrl held).</summary>
    public event Action<AppPane, LinkTarget, bool> LinkFollowed;

    /// <summary>Raised when a tab is dragged past a neighbour's middle: the tab and the position it belongs at.</summary>
    public event Action<AppTab, int> TabDragged;

    /// <summary>Raised when a tab is right-clicked: the tab and the click (its menu opens there).</summary>
    public event Action<AppTab, PointerEventData> TabMenuRequested;

    /// <summary>The active tab.</summary>
    public AppTab ActiveTab
    {
        get
        {
            Init();
            return _active;
        }
    }

    /// <summary>True when Back has somewhere to go.</summary>
    public bool CanBack => _history != null && _history.CanBack;

    /// <summary>True when Forward has somewhere to go.</summary>
    public bool CanForward => _history != null && _history.CanForward;

    /// <summary>The tab's view, or null when the pane has none.</summary>
    public IAppView View(AppTab tab)
    {
        Init();
        return _views.TryGetValue(tab, out IAppView view) ? view : null;
    }

    /// <summary>True when the pane hosts a view for the tab (read from the wired views, before the pane first shows).</summary>
    public bool Hosts(AppTab tab)
    {
        foreach (AppView view in views)
            if (view != null && view.Tab == tab)
                return true;
        return false;
    }

    /// <summary>The player shows the tab (a click on it): its view with the item it shows, recorded in the history.</summary>
    public void Show(AppTab tab) => Navigate(LinkTarget.ToTab(tab), true);

    /// <summary>Goes to <paramref name="target"/> (a link, a dock side, the toast's paper, a jump): its tab, item and row, recorded in the history.</summary>
    public void Go(LinkTarget target)
    {
        if (!target.IsNone)
            Navigate(target, true);
    }

    /// <summary>Back through the history (not recorded again).</summary>
    public void Back()
    {
        if (_history != null && _history.Back(out LinkTarget at))
            Navigate(at, false);
        HistoryChanged?.Invoke(this);
    }

    /// <summary>Forward through the history (not recorded again).</summary>
    public void Forward()
    {
        if (_history != null && _history.Forward(out LinkTarget at))
            Navigate(at, false);
        HistoryChanged?.Invoke(this);
    }

    /// <summary>A row of this pane follows its link (LK2): the app sends it to the other pane, or keeps it here while Ctrl is held.</summary>
    public void FollowLink(LinkTarget target)
    {
        Keyboard keys = Keyboard.current;
        if (!target.IsNone)
            LinkFollowed?.Invoke(this, target, keys != null && keys.ctrlKey.isPressed);
    }

    /// <summary>A traveller is at the desk (a case source's view shows) or not (it shows the no-case state).</summary>
    public void SetCase(bool on)
    {
        Init();
        _caseOn = on;
        Apply();
    }

    /// <summary>
    /// A new case: the history forgets the last traveller's papers, lines and
    /// deviations (the case sources' places; the day sources' stay) and
    /// records where the pane is now.
    /// </summary>
    public void DropCaseHistory()
    {
        Init();
        _history.RemoveAll(t => TabOrder.IsCaseSource(t.Tab));
        _history.Go(Spot());
        HistoryChanged?.Invoke(this);
    }

    /// <summary>Shows or hides the tab's badge.</summary>
    public void SetBadge(AppTab tab, bool on)
    {
        GameObject badge = At(tabBadges, tab);
        if (badge != null && badge.activeSelf != on)
            badge.SetActive(on);
    }

    /// <summary>Lays the strip out in <paramref name="order"/> (the app's shared order).</summary>
    public void ApplyOrder(TabOrder order)
    {
        for (int position = 0; position < order.Tabs.Count; position++)
        {
            Button button = At(tabButtons, order.Tabs[position]);
            if (button != null)
                button.transform.SetSiblingIndex(position);
        }
    }

    /// <summary>Shows or hides the active pane's accent frame.</summary>
    public void SetFrame(bool on)
    {
        if (activeFrame != null && activeFrame.activeSelf != on)
            activeFrame.SetActive(on);
    }

    /// <summary>A tab is dragged to <paramref name="eventData"/>'s pointer (AppTabHandle): past a neighbour's middle it asks to move there (TabOrder.DragTarget).</summary>
    public void DragTab(AppTab tab, PointerEventData eventData)
    {
        Button dragged = At(tabButtons, tab);
        if (tabStrip == null || dragged == null ||
            !RectTransformUtility.ScreenPointToLocalPointInRectangle(tabStrip, eventData.position, eventData.pressEventCamera, out Vector2 pointer))
            return;

        _middles.Clear();
        int from = -1;
        for (int i = 0; i < tabStrip.childCount; i++)
        {
            var child = (RectTransform)tabStrip.GetChild(i);
            if (!child.gameObject.activeSelf || child.GetComponent<Button>() == null)
                continue;
            if (child == dragged.transform)
                from = _middles.Count;
            _middles.Add(child.localPosition.x + (0.5f - child.pivot.x) * child.rect.width);
        }
        int to = TabOrder.DragTarget(_middles, from, pointer.x);
        if (from >= 0 && to != from)
            TabDragged?.Invoke(tab, to);
    }

    /// <summary>A tab is right-clicked (AppTabHandle): its menu (Move left, Move right, Reset tab order).</summary>
    public void RequestTabMenu(AppTab tab, PointerEventData eventData) => TabMenuRequested?.Invoke(tab, eventData);

    /// <summary>The pane shows (the app split, the window opened): it is set up and shows its tab.</summary>
    private void OnEnable() => Init();

    /// <summary>The strip's width changed (the split, the window restored): the tabs re-decide between name and glyph.</summary>
    private void OnRectTransformDimensionsChange()
    {
        if (_ready)
            LayoutTabs();
    }

    /// <summary>Maps the views, wires the tabs and the views' chips and moves, and shows the first tab (once; the pane may be driven while its window is still closed).</summary>
    private void Init()
    {
        if (_ready)
            return;
        _ready = true;
        _history = new NavHistory<LinkTarget>(config != null ? config.paneHistory : 30);
        _active = startTab;

        foreach (AppView view in views)
            if (view != null)
            {
                _views[view.Tab] = view;
                view.ChipsChanged += () => ChipsChanged(view);
                view.Moved += () => ViewMoved(view);
            }

        foreach (AppTab tab in TabOrder.Default)
        {
            Button button = At(tabButtons, tab);
            if (button != null)
                button.onClick.AddListener(() => Show(tab));
        }

        if (chipTemplate != null)
            chipTemplate.gameObject.SetActive(false);
        Navigate(LinkTarget.ToTab(startTab), true);
    }

    /// <summary>Shows the target's tab and sends its view there; recorded (<paramref name="record"/>) as the target when it names a row or a lookup, else as where the view is.</summary>
    private void Navigate(LinkTarget target, bool record)
    {
        Init();
        _active = target.Tab;
        Apply();
        IAppView view = View(_active);
        if (view != null)
            view.Reveal(target);
        if (record)
        {
            _history.Go(target.Key != null || target.Query != null ? target : Spot());
            HistoryChanged?.Invoke(this);
        }
        Shown?.Invoke(this, _active);
    }

    /// <summary>Where the pane is: the active view's spot.</summary>
    private LinkTarget Spot()
    {
        IAppView view = _views.TryGetValue(_active, out IAppView v) ? v : null;
        return view != null ? view.Spot : LinkTarget.ToTab(_active);
    }

    /// <summary>A view's chips changed: redrawn when it is the active one.</summary>
    private void ChipsChanged(IAppView view)
    {
        if (view.Tab == _active)
            DrawChips();
    }

    /// <summary>The player moved the active view (a lookup): recorded.</summary>
    private void ViewMoved(IAppView view)
    {
        if (view.Tab != _active)
            return;
        _history.Go(view.Spot);
        HistoryChanged?.Invoke(this);
    }

    /// <summary>Which view shows, the no-case state, the tabs' looks and the chips.</summary>
    private void Apply()
    {
        bool blocked = !_caseOn && TabOrder.IsCaseSource(_active);
        foreach (KeyValuePair<AppTab, IAppView> pair in _views)
            pair.Value.SetVisible(pair.Key == _active && !blocked);
        if (noCase != null && noCase.activeSelf != blocked)
            noCase.SetActive(blocked);

        foreach (AppTab tab in TabOrder.Default)
        {
            GameObject look = At(tabActive, tab);
            if (look != null && look.activeSelf != (tab == _active))
                look.SetActive(tab == _active);
        }
        LayoutTabs();
        DrawChips();
    }

    /// <summary>On a narrow strip the inactive tabs show their glyphs at the glyph width, the active one its name; otherwise every tab its name, sharing the strip.</summary>
    private void LayoutTabs()
    {
        if (tabStrip == null || config == null)
            return;
        bool narrow = AppPanes.TabsNarrow(tabStrip.rect.width, TabOrder.Default.Count, config.tabLabelWidth);
        foreach (AppTab tab in TabOrder.Default)
        {
            bool glyph = narrow && tab != _active;
            GameObject label = At(tabLabels, tab), mark = At(tabGlyphs, tab);
            if (label != null && label.activeSelf == glyph)
                label.SetActive(!glyph);
            if (mark != null && mark.activeSelf != glyph)
                mark.SetActive(glyph);
            Button button = At(tabButtons, tab);
            LayoutElement size = button != null ? button.GetComponent<LayoutElement>() : null;
            if (size == null)
                continue;
            size.minWidth = config.tabGlyphWidth;
            size.preferredWidth = glyph ? config.tabGlyphWidth : config.tabLabelWidth;
            size.flexibleWidth = glyph ? 0f : 1f;
        }
    }

    /// <summary>The active view's chips (none while the no-case state shows), the chosen one pressed, an unavailable one dimmed.</summary>
    private void DrawChips()
    {
        foreach (Button chip in _chips)
            if (chip != null)
                Destroy(chip.gameObject);
        _chips.Clear();

        if (chipTemplate == null || chipStrip == null || (!_caseOn && TabOrder.IsCaseSource(_active)) || !_views.TryGetValue(_active, out IAppView view))
            return;

        IReadOnlyList<AppChip> items = view.Chips;
        for (int i = 0; i < items.Count; i++)
        {
            Button chip = Instantiate(chipTemplate, chipStrip);
            chip.gameObject.name = "Chip_" + i;
            chip.gameObject.SetActive(true);
            TMP_Text label = chip.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
                label.text = items[i].Label;
            ColorBlock colours = chip.colors;
            colours.normalColor = i == view.Selected ? chosenTint : items[i].Available ? Color.white : unavailableTint;
            colours.selectedColor = colours.normalColor;
            chip.colors = colours;
            AppTab tab = view.Tab;
            int index = i;
            chip.onClick.AddListener(() => Navigate(LinkTarget.ToTab(tab, index), true));
            _chips.Add(chip);
        }
    }

    /// <summary>The tab's entry in an array indexed by the tab's value, or null.</summary>
    private static T At<T>(T[] byTab, AppTab tab) where T : class
    {
        int i = (int)tab;
        return byTab != null && i >= 0 && i < byTab.Length ? byTab[i] : null;
    }
}
