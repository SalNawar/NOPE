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
/// with a badge and a tooltip naming it; the active tab wears its active
/// look, the paper plate with the ink bar the builder made; on a strip too
/// narrow for every name at its size the inactive tabs collapse to their
/// glyphs and their badges, sharing what the active tab's name leaves:
/// AppPanes.TabsNarrow), its header (the active view's chips in a row that
/// scrolls sideways when they do not fit, ChipRow: a click shows that item;
/// the chosen chip wears its "Chosen" accent look and is scrolled into view,
/// one not readable yet is dimmed) and
/// its content (the active tab's view; between
/// travellers a case source shows the no-case state, "Waiting for the next
/// traveller", instead). The views are IAppView components, each drawing its
/// page as a form (FormPage). The app has two
/// panes, each with its own views and its own back/forward history
/// (NavHistory of LinkTargets: every tab switch, item switch, lookup and link
/// is recorded; Back and Forward walk it without recording). A link followed
/// from one of its rows (FollowLink) goes to the app, which sends it to the
/// other pane (Ctrl held: this one). A tab is dragged along the strip past a
/// neighbour's middle, or moved from its right-click menu; the app reorders
/// both strips. A tab is shown only by the player or by the app's own rules
/// (a new case shows Documents in the left pane, AP8); a view never switches
/// the tab. The active pane wears a 3 u accent frame while the app is split.
/// The keys' focus ring walks its tabs and chips (AppPane.Keys, phase 20).
/// </summary>
public sealed partial class AppPane : MonoBehaviour
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

    /// <summary>The header's chips' parent (the chip row's scrolling content).</summary>
    [SerializeField] private RectTransform chipStrip;

    /// <summary>The header's chip row: it scrolls sideways when the chips do not fit, and shows the chosen one.</summary>
    [SerializeField] private ChipRow chipRow;

    /// <summary>A chip (inactive), cloned per item of the active view.</summary>
    [SerializeField] private Button chipTemplate;

    /// <summary>The no-case state over the content (a case source's tab between travellers).</summary>
    [SerializeField] private GameObject noCase;

    /// <summary>The 3 u accent frame of the active pane (shown while the app is split).</summary>
    [SerializeField] private GameObject activeFrame;

    /// <summary>The tab the pane shows first.</summary>
    [SerializeField] private AppTab startTab = AppTab.Documents;

    /// <summary>The desktop's knobs: the tabs' glyph width, the history's length.</summary>
    [SerializeField] private DesktopConfigSO config;

    /// <summary>An unavailable item's chip tint (dimmed; it still shows why when clicked).</summary>
    [SerializeField] private Color unavailableTint = new Color(1f, 1f, 1f, 0.55f);

    private readonly Dictionary<AppTab, IAppView> _views = new Dictionary<AppTab, IAppView>();
    private readonly List<Button> _chips = new List<Button>();
    private readonly List<float> _middles = new List<float>();
    private readonly List<float> _nameTabs = new List<float>();
    private readonly Dictionary<AppTab, float> _nameWidths = new Dictionary<AppTab, float>();
    private NavHistory<LinkTarget> _history;
    private AppTab _active;
    private bool _caseOn;
    private bool _ready;
    private float _tabMinWidth = -1f;
    private int _tabPadding = -1;

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

    /// <summary>Goes to <paramref name="target"/> (a link, a dock side, the toast's paper, a pin's or a recent item's jump): its tab, item and row, recorded in the history. False when the target is gone (None, or its view says so); the tab shows all the same.</summary>
    public bool Go(LinkTarget target) => !target.IsNone && Navigate(target, true);

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

    /// <summary>Shows the target's tab and sends its view there (true when the view finds the target); recorded (<paramref name="record"/>) as the target when it names a row or a lookup, else as where the view is.</summary>
    private bool Navigate(LinkTarget target, bool record)
    {
        Init();
        _active = target.Tab;
        Apply();
        IAppView view = View(_active);
        bool there = view == null || view.Reveal(target);
        if (record)
        {
            _history.Go(target.Key != null || target.Query != null ? target : Spot());
            HistoryChanged?.Invoke(this);
        }
        Shown?.Invoke(this, _active);
        return there;
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

    /// <summary>
    /// A strip that cannot hold every tab with its name at its size
    /// (AppPanes.TabsNarrow over each tab's measured width: NameTabWidth)
    /// collapses its inactive tabs to their glyphs and badges (the plate's
    /// padding gone, the glyph and the badge's slot centred), at least the
    /// glyph width each and sharing what the active tab, which keeps its name
    /// at its own width, leaves of the strip; the glyph shrinks to fit its room
    /// (its theme tag's fit), so it never draws over a neighbour. Otherwise
    /// every tab is as wide as its name, on the plate the builder made (its
    /// padding and narrowest width, read once before the first collapse), so
    /// no name ever shrinks under its size.
    /// </summary>
    private void LayoutTabs()
    {
        if (tabStrip == null || config == null)
            return;
        _nameTabs.Clear();
        foreach (AppTab tab in TabOrder.Default)
        {
            float width = NameTabWidth(tab);
            if (width > 0f)
                _nameTabs.Add(width);
        }
        HorizontalLayoutGroup strip = tabStrip.GetComponent<HorizontalLayoutGroup>();
        bool narrow = AppPanes.TabsNarrow(tabStrip.rect.width, _nameTabs, strip != null ? strip.padding.horizontal : 0f, strip != null ? strip.spacing : 0f);
        foreach (AppTab tab in TabOrder.Default)
        {
            bool glyph = narrow && tab != _active;
            GameObject label = At(tabLabels, tab), mark = At(tabGlyphs, tab);
            if (label != null && label.activeSelf == glyph)
                label.SetActive(!glyph);
            if (mark != null && mark.activeSelf != glyph)
                mark.SetActive(glyph);
            Button button = At(tabButtons, tab);
            if (button == null || !button.TryGetComponent(out LayoutElement size) || !button.TryGetComponent(out HorizontalLayoutGroup plate))
                continue;
            size.minWidth = glyph ? config.tabGlyphWidth : _tabMinWidth;
            size.preferredWidth = glyph ? config.tabGlyphWidth : -1f;
            size.flexibleWidth = glyph ? 1f : 0f;
            int padding = glyph ? 0 : _tabPadding;
            if (plate.padding.left != padding)
            {
                plate.padding.left = padding;
                plate.padding.right = padding;
                LayoutRebuilder.MarkLayoutForRebuild((RectTransform)button.transform);
            }
        }
    }

    /// <summary>
    /// The tab's width with its name at the label size (the name's preferred
    /// width, a fitting label's largest; the plate's padding either side; the
    /// gap and the badge's slot), never under the plate's narrowest; 0 while
    /// it is not known. It is measured whenever TextMeshPro has set the label
    /// up (a label never shown yet has no material to measure with: every
    /// name shows when the pane first does), and the last measure stands while
    /// the tab shows its glyph. The plate's padding and narrowest width are
    /// read once, before the first collapse.
    /// </summary>
    private float NameTabWidth(AppTab tab)
    {
        Button button = At(tabButtons, tab);
        GameObject label = At(tabLabels, tab), badge = At(tabBadges, tab);
        if (button == null || label == null || !label.TryGetComponent(out TMP_Text name) ||
            !button.TryGetComponent(out LayoutElement size) || !button.TryGetComponent(out HorizontalLayoutGroup plate))
            return 0f;
        if (_tabMinWidth < 0f)
        {
            _tabMinWidth = size.minWidth;
            _tabPadding = plate.padding.left;
        }
        if (name.fontSharedMaterial != null)
        {
            float slot = badge != null && badge.transform.parent is RectTransform room ? LayoutUtility.GetPreferredWidth(room) + plate.spacing : 0f;
            _nameWidths[tab] = 2f * _tabPadding + name.GetPreferredValues(name.text).x + slot;
        }
        return _nameWidths.TryGetValue(tab, out float width) ? Mathf.Max(_tabMinWidth, width) : 0f;
    }

    /// <summary>The active view's chips (none while the no-case state shows): the chosen one wears its accent look and is scrolled into view (ChipRow), an unavailable one is dimmed.</summary>
    private void DrawChips()
    {
        foreach (Button chip in _chips)
            if (chip != null)
            {
                chip.gameObject.SetActive(false); // out of the row's layout now; destroyed at the frame's end
                Destroy(chip.gameObject);
            }
        _chips.Clear();

        if (chipTemplate == null || chipStrip == null || (!_caseOn && TabOrder.IsCaseSource(_active)) || !_views.TryGetValue(_active, out IAppView view))
            return;

        IReadOnlyList<AppChip> items = view.Chips;
        for (int i = 0; i < items.Count; i++)
        {
            Button chip = Instantiate(chipTemplate, chipStrip);
            chip.gameObject.name = "Chip_" + i;
            chip.gameObject.SetActive(true);
            foreach (TMP_Text label in chip.GetComponentsInChildren<TMP_Text>(true))
                label.text = items[i].Label;
            Transform chosen = chip.transform.Find("Chosen");
            if (chosen != null && chosen.gameObject.activeSelf != (i == view.Selected))
                chosen.gameObject.SetActive(i == view.Selected);
            ColorBlock colours = chip.colors;
            colours.normalColor = items[i].Available ? Color.white : unavailableTint;
            colours.selectedColor = colours.normalColor;
            chip.colors = colours;
            AppTab tab = view.Tab;
            int index = i;
            chip.onClick.AddListener(() => Navigate(LinkTarget.ToTab(tab, index), true));
            _chips.Add(chip);
        }
        if (chipRow != null)
            chipRow.Refresh(view.Selected >= 0 && view.Selected < _chips.Count ? (RectTransform)_chips[view.Selected].transform : null);
    }

    /// <summary>The tab's entry in an array indexed by the tab's value, or null.</summary>
    private static T At<T>(T[] byTab, AppTab tab) where T : class
    {
        int i = (int)tab;
        return byTab != null && i >= 0 && i < byTab.Length ? byTab[i] : null;
    }
}
