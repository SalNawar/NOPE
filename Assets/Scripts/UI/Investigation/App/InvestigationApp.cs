using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// The Investigation app (the PC redesign AP1, AP4, AP8, AP9, LK2, WN5, DK2;
/// the PC UX redesign IA1-IA9): one desktop window, "Investigation" (· the
/// traveller's name while one is at the desk), that fills the desktop the
/// first time it opens. Its toolbar holds Back and Forward (the active
/// pane's history), the search field (the palette: InvestigationApp.Search)
/// and the PC's Accept and Deny (the façade wires them). Its navigator
/// (AppNav) tops the sidebar with the case summary (the counters: papers
/// received and scanned, deviations logged; "Waiting for the next
/// traveller" between travellers; it prints no claim, which the traveller
/// only says: the personalities spec's B1-B3), then every case source in
/// full words in the saved order (TabOrder: dragged or moved from a source's
/// menu, saved per player in DesktopPreferences.AppTabs), the active pane's
/// source selected with its items under it, and ends with the checklist
/// (StepsPanel, phase 21). The navigator drives the active pane: a source's
/// click shows it there (Ctrl+click: in the other pane, opening the split
/// when it fits), an item's click shows that item. Two panes side by side
/// (Open beside in the left pane's header, Close in the right one's, Ctrl+\:
/// saved per player, possible only while each pane gets a readable width:
/// AppPanes.CanSplit, so a restored window has one pane and Open beside says
/// why). The keys, the focus ring, copy and paste, pins, recent items and
/// zoom are in InvestigationApp.Keys (redesign phase 20). The active pane is
/// the last one pressed (the desktop's press, DesktopWindowManager.Pressed)
/// or sent somewhere; its header wears the accent underline. A row's smart
/// link goes to the other pane (Ctrl held, or one pane: the same pane, and
/// Back returns); the compare dock's sides, the toast, Mail's memo and a
/// step's label open in the active pane (Open). The steps checklist reads
/// what the player sees (Sees, CopiesSeen: the showing panes' sources and
/// scanned copies). Nothing steals the view: something new for a source
/// badges its navigator entry unless a showing pane shows it (AppBadges),
/// and dots the desktop's Investigation icon while the app is closed or
/// minimised; a scan (ScanArrival) opens the app only when it is closed,
/// shows the paper only in a Papers view showing none, and toasts ("…
/// scanned", Open shows it). A new case shows Papers in the left pane (the
/// right one keeps its source), drops the last traveller's places from both
/// histories and clears the badges; at the decision the case sources show
/// the no-case state. InvestigationUIController drives it.
/// </summary>
public sealed partial class InvestigationApp : MonoBehaviour
{
    /// <summary>The app's window (maximised on the first open).</summary>
    [SerializeField] private DesktopWindow window;

    /// <summary>The left pane (the one pane while the split is off).</summary>
    [SerializeField] private AppPane leftPane;

    /// <summary>The right pane (shown while the split is on).</summary>
    [SerializeField] private AppPane rightPane;

    /// <summary>The window's body (its width decides whether two panes fit).</summary>
    [SerializeField] private RectTransform body;

    /// <summary>The sidebar at the body's left (its width counts against the panes).</summary>
    [SerializeField] private RectTransform sidebar;

    /// <summary>The navigator: the sources, the active one's items.</summary>
    [SerializeField] private AppNav nav;

    [Header("Case summary")]
    /// <summary>The counters: papers received and scanned, deviations logged; between travellers the idle line.</summary>
    [SerializeField] private TMP_Text countersText;

    [Header("Toolbar and pane headers")]
    /// <summary>Back in the active pane.</summary>
    [SerializeField] private Button backButton;

    /// <summary>Forward in the active pane.</summary>
    [SerializeField] private Button forwardButton;

    /// <summary>Open beside, in the left pane's header: the right pane opens.</summary>
    [SerializeField] private Button splitButton;

    /// <summary>Open beside's hover hint: what it does, or why it cannot.</summary>
    [SerializeField] private TMP_Text splitHint;

    /// <summary>Close, in the right pane's header: back to one pane.</summary>
    [SerializeField] private Button closeSplitButton;

    [Header("Desktop")]
    /// <summary>The scan toast (on the desktop, above the windows: it shows while the app is down too).</summary>
    [SerializeField] private AppToast toast;

    /// <summary>The desktop's knobs (the toast's time, the panes' widths).</summary>
    [SerializeField] private DesktopConfigSO config;

    /// <summary>The desktop's icons (the Investigation icon's dot).</summary>
    [SerializeField] private DesktopIcons icons;

    /// <summary>The desktop's right-click menu (a source's Move up, Move down, Reset order).</summary>
    [SerializeField] private DesktopContextMenu contextMenu;

    private readonly AppBadges _badges = new AppBadges();
    private readonly List<AppTab> _shown = new List<AppTab>(2);
    private TabOrder _order = new TabOrder();
    private AppPane _active;
    private CasePapers _papers;
    private DesktopWindowManager _manager;
    private bool _splitWanted = true;
    private bool _split;
    private bool _ready;

    /// <summary>True while the app shows (open and not minimised).</summary>
    public bool IsShowing => window != null && window.gameObject.activeSelf;

    /// <summary>True when the panes host a view for the source.</summary>
    public bool Hosts(AppTab tab) => leftPane != null && leftPane.Hosts(tab);

    /// <summary>True when a showing pane shows the source (the steps checklist: the Rules read while the player looks at the PC).</summary>
    public bool Sees(AppTab tab)
    {
        foreach (AppTab shown in ShownTabs())
            if (shown == tab)
                return true;
        return false;
    }

    /// <summary>Fills <paramref name="into"/> with the papers whose scanned copies the showing panes show on Papers (the steps checklist: a paper read on the PC).</summary>
    public void CopiesSeen(List<int> into)
    {
        into.Clear();
        if (!IsShowing)
            return;
        foreach (AppPane pane in Panes())
            if ((pane == leftPane || _split) && pane.ActiveTab == AppTab.Documents && pane.View(AppTab.Documents) is DocumentsView documents && documents.ShowsCopy)
                into.Add(documents.Selected);
    }

    /// <summary>The app's window (the keyboard poller's "app focused").</summary>
    public DesktopWindow Window => window;

    /// <summary>The first open: the app fills the desktop (P spec WN4).</summary>
    private void Start()
    {
        if (window != null && !window.IsMaximised)
            window.ToggleMaximise();
    }

    /// <summary>The app shows (opened or restored): the shown sources are seen, the icon's dot goes, and the panes fit the window.</summary>
    private void OnEnable()
    {
        if (leftPane == null)
            return;
        Init();
        Layout();
        foreach (AppTab tab in ShownTabs())
            Seen(tab);
        if (icons != null)
            icons.SetBadge(DesktopAppIds.Investigation, 0);
    }

    private void OnDestroy()
    {
        if (_manager != null)
            _manager.Pressed -= Pressed;
    }

    /// <summary>The window was maximised or restored: two panes or one.</summary>
    private void OnRectTransformDimensionsChange()
    {
        if (_ready)
            Layout();
    }

    /// <summary>A traveller is presented: the title (who stands at the desk, never what they ask for), the left pane on Papers, the histories without the last traveller, the badges and the icon's dot cleared, the toast gone, search's case layer empty.</summary>
    public void BeginCase(string travellerName)
    {
        Init();
        ResetSearchCase();
        if (window != null)
            window.SetTitle(UiText.Format("app.titleCase", travellerName));
        _badges.Clear();
        foreach (AppTab tab in TabOrder.Default)
            SetBadge(tab, false);
        foreach (AppPane pane in Panes())
        {
            pane.SetCase(true);
            pane.DropCaseHistory();
        }
        if (icons != null)
            icons.SetBadge(DesktopAppIds.Investigation, 0);
        leftPane.Show(AppTab.Documents);
        if (toast != null)
            toast.Hide();
        KeysBeginCase(travellerName);
    }

    /// <summary>The decision: the case sources show the no-case state; the summary waits for the next traveller; search forgets the case.</summary>
    public void EndCase()
    {
        Init();
        _papers = null;
        ResetSearchCase();
        if (window != null)
            window.SetTitle(UiText.Get("app.title"));
        if (countersText != null)
            countersText.text = UiText.Get("idle.waiting");
        foreach (AppPane pane in Panes())
            pane.SetCase(false);
        if (toast != null)
            toast.Hide();
        KeysEndCase();
    }

    /// <summary>Writes the counters from the case's papers (kept: a dock side from a paper links only once it is scanned) and the logged deviations.</summary>
    public void SetCounters(CasePapers papers, int deviations)
    {
        _papers = papers;
        if (countersText != null && papers != null)
            countersText.text = UiText.Format("app.counters", papers.Received, papers.Count, papers.Scanned, deviations);
    }

    /// <summary>Something new for the source (a transcript line, a logged deviation): badged unless a showing pane shows it; the icon dotted while the app is down.</summary>
    public void Arrived(AppTab tab)
    {
        Init();
        if (_badges.Arrived(tab, ShownTabs()))
            SetBadge(tab, true);
        if (!IsShowing && icons != null)
            icons.SetBadge(DesktopAppIds.Investigation, IconBadge.Dot);
    }

    /// <summary>Paper <paramref name="paper"/> was scanned (ScanArrival): the app opens when closed, the paper shows in each Papers view showing none, Papers is badged unless seen, a toast names it.</summary>
    public void Scanned(int paper, string paperName)
    {
        Init();
        bool open = window != null && window.IsOpen, minimised = window != null && window.IsMinimised;
        IReadOnlyList<AppTab> shown = ShownTabs();
        ScanArrival arrival = default;
        foreach (AppPane pane in Panes())
        {
            IAppView documents = pane.View(AppTab.Documents);
            arrival = ScanArrival.Decide(open, minimised, shown, documents != null && documents.Selected >= 0);
            if (arrival.ShowPaper && documents != null)
                documents.Select(paper);
        }
        if (arrival.OpenApp && window != null)
            window.Open();
        if (arrival.BadgeDocuments)
            Arrived(AppTab.Documents);
        if (arrival.Toast && toast != null)
            toast.Show(UiText.Format("app.toast.scanned", paperName), config != null ? config.toastSeconds : 4f,
                       () => Open(LinkTarget.ToTab(AppTab.Documents, paper), false));
    }

    /// <summary>Opens the app (a minimised one restores) on the source in the active pane: Mail's directive memo shows today's rules.</summary>
    public void ShowTab(AppTab tab) => Open(LinkTarget.ToTab(tab), false);

    /// <summary>
    /// Opens the app (a minimised one restores) at <paramref name="target"/>:
    /// in the active pane, or the other one (<paramref name="otherPane"/>, while
    /// split), which then becomes active. The compare dock's sides, the toast,
    /// Mail, a search hit, a pin and a recent item come here.
    /// </summary>
    public void Open(LinkTarget target, bool otherPane)
    {
        Init();
        if (target.IsNone)
            return;
        if (window != null)
            window.Open();
        AppPane pane = otherPane && _split ? Other(_active) : _active;
        pane.Go(target);
        Activate(pane);
    }

    /// <summary>Where a pick was picked (SmartLinks.ForKey over the case's papers): the dock's sides.</summary>
    public LinkTarget LinkFor(string pickKey) => SmartLinks.ForKey(pickKey, _papers);

    /// <summary>Moves a source by <paramref name="delta"/> places in the navigator and saves the order (a source's Move up and Move down).</summary>
    public void MoveTab(AppTab tab, int delta)
    {
        Init();
        int from = _order.PositionOf(tab);
        if (_order.Move(from, from + delta))
            OrderChanged();
    }

    /// <summary>Back to the default order of the sources, saved ("Reset order").</summary>
    public void ResetTabOrder()
    {
        Init();
        if (_order.Reset())
            OrderChanged();
    }

    /// <summary>Wires the panes, the navigator, the buttons and the desktop's press, and reads the saved order and split (once; the app is driven while its window is closed).</summary>
    private void Init()
    {
        if (_ready)
            return;
        _ready = true;
        _order = TabOrder.Parse(DesktopPreferences.AppTabs);
        _splitWanted = DesktopPreferences.AppSplit;
        _active = leftPane;

        foreach (AppPane pane in Panes())
        {
            pane.Shown += PaneShown;
            pane.Changed += PaneChanged;
            pane.HistoryChanged += HistoryChanged;
            pane.LinkFollowed += Follow;
        }
        if (nav != null)
        {
            nav.SourceClicked += SourceClicked;
            nav.ItemClicked += index => ActivePane.ShowItem(index);
            nav.SourceDragged += MoveTabTo;
            nav.SourceMenuRequested += SourceMenu;
            nav.ApplyOrder(_order);
        }

        if (backButton != null)
            backButton.onClick.AddListener(() => _active.Back());
        if (forwardButton != null)
            forwardButton.onClick.AddListener(() => _active.Forward());
        if (splitButton != null)
            splitButton.onClick.AddListener(ToggleSplit);
        if (closeSplitButton != null)
            closeSplitButton.onClick.AddListener(ToggleSplit);

        _manager = window != null ? window.Manager : null;
        if (_manager != null)
            _manager.Pressed += Pressed;
        InitKeys();
        InitSearch();
        Layout();
        RefreshHistoryButtons();
        RefreshNav();
    }

    /// <summary>Open beside and Close: two panes or one, saved per player.</summary>
    private void ToggleSplit()
    {
        _splitWanted = !_splitWanted;
        DesktopPreferences.AppSplit = _splitWanted;
        Layout();
    }

    /// <summary>
    /// Two panes when the player wants them and the body holds them
    /// (AppPanes.CanSplit), else the left one alone (the right one's history
    /// and view stay for the next split); Open beside shows only while one
    /// pane does and greys, with its hint saying why, when two cannot fit.
    /// </summary>
    private void Layout()
    {
        if (leftPane == null || body == null)
            return;
        float sidebarWidth = sidebar != null && sidebar.gameObject.activeSelf ? sidebar.rect.width : 0f;
        bool fits = rightPane != null && config != null && AppPanes.CanSplit(body.rect.width, sidebarWidth, config.paneMinWidth);
        _split = _splitWanted && fits;

        float gap = config != null ? config.paneGap / 2f : 3f;
        var left = (RectTransform)leftPane.transform;
        left.anchorMax = new Vector2(_split ? 0.5f : 1f, 1f);
        left.offsetMax = new Vector2(_split ? -gap : 0f, left.offsetMax.y);
        if (rightPane != null)
        {
            var right = (RectTransform)rightPane.transform;
            right.anchorMin = new Vector2(0.5f, 0f);
            right.offsetMin = new Vector2(gap, right.offsetMin.y);
            if (rightPane.gameObject.activeSelf != _split)
                rightPane.gameObject.SetActive(_split);
        }
        if (!_split)
            _active = leftPane;
        Frames();

        if (splitButton != null)
        {
            if (splitButton.gameObject.activeSelf == _split)
                splitButton.gameObject.SetActive(!_split);
            splitButton.interactable = fits;
        }
        if (splitHint != null)
            splitHint.text = UiText.Get(fits ? "app.split.hint" : "app.split.tooNarrow");
        RefreshHistoryButtons();
        RefreshNav();
    }

    /// <summary>Both panes (the right one keeps its case state, badges and views current while it is hidden, for the next split).</summary>
    private IEnumerable<AppPane> Panes()
    {
        if (leftPane != null)
            yield return leftPane;
        if (rightPane != null)
            yield return rightPane;
    }

    /// <summary>The sources the player sees: the showing panes' sources (none while the app is down).</summary>
    private IReadOnlyList<AppTab> ShownTabs()
    {
        _shown.Clear();
        if (IsShowing && leftPane != null)
        {
            _shown.Add(leftPane.ActiveTab);
            if (_split)
                _shown.Add(rightPane.ActiveTab);
        }
        return _shown;
    }

    /// <summary>The other pane.</summary>
    private AppPane Other(AppPane pane) => pane == leftPane ? rightPane : leftPane;

    /// <summary>The pane becomes the active one (its underline, the toolbar's history, the navigator).</summary>
    private void Activate(AppPane pane)
    {
        if (pane == null || (!_split && pane == rightPane))
            return;
        _active = pane;
        Frames();
        RefreshHistoryButtons();
        RefreshNav();
    }

    /// <summary>The active pane's underline, while split.</summary>
    private void Frames()
    {
        leftPane.SetFrame(_split && _active == leftPane);
        if (rightPane != null)
            rightPane.SetFrame(_split && _active == rightPane);
    }

    /// <summary>A press on the desktop (DesktopWindowManager.Pressed): inside a pane, that pane is active; outside the palette, it closes.</summary>
    private void Pressed(GameObject top)
    {
        PressedForSearch(top);
        if (top == null)
            return;
        foreach (AppPane pane in Panes())
            if (pane.gameObject.activeInHierarchy && top.transform.IsChildOf(pane.transform) && pane != _active)
                Activate(pane);
    }

    /// <summary>A navigator source clicked: shown in the active pane, or in the other one with Ctrl (the split opening when it fits).</summary>
    private void SourceClicked(AppTab tab, bool otherPane)
    {
        if (otherPane && !_split)
        {
            _splitWanted = true;
            DesktopPreferences.AppSplit = true;
            Layout();
        }
        AppPane pane = otherPane && _split ? Other(ActivePane) : ActivePane;
        pane.Show(tab);
        Activate(pane);
    }

    /// <summary>A pane showed a source: it is seen when the pane shows.</summary>
    private void PaneShown(AppPane pane, AppTab tab)
    {
        if (IsShowing && (pane == leftPane || _split))
            Seen(tab);
    }

    /// <summary>A pane's source, item or items changed: the navigator follows.</summary>
    private void PaneChanged(AppPane pane)
    {
        if (_ready)
            RefreshNav();
    }

    /// <summary>The navigator: the active pane's source and its items, the other pane's source (while split).</summary>
    private void RefreshNav()
    {
        if (nav == null || _active == null)
            return;
        AppPane other = _split ? Other(_active) : null;
        AppTab tab = _active.ActiveTab;
        nav.Show(tab, other != null ? other.ActiveTab : (AppTab?)null, _active.Blocked ? null : _active.View(tab));
    }

    /// <summary>A pane's history changed: the toolbar follows the active pane's.</summary>
    private void HistoryChanged(AppPane pane)
    {
        if (pane == _active)
            RefreshHistoryButtons();
    }

    /// <summary>Back and Forward take the active pane's history.</summary>
    private void RefreshHistoryButtons()
    {
        if (backButton != null)
            backButton.interactable = _active != null && _active.CanBack;
        if (forwardButton != null)
            forwardButton.interactable = _active != null && _active.CanForward;
    }

    /// <summary>A row's link (LK2): into the other pane, or the same one with Ctrl held or one pane; the pane it went to is active.</summary>
    private void Follow(AppPane from, LinkTarget target, bool samePane)
    {
        AppPane to = samePane || !_split ? from : Other(from);
        to.Go(target);
        Activate(to);
    }

    /// <summary>A source dragged past a neighbour's middle: to its new place, saved.</summary>
    private void MoveTabTo(AppTab tab, int position)
    {
        if (_order.Move(_order.PositionOf(tab), position))
            OrderChanged();
    }

    /// <summary>A source's right-click: its menu.</summary>
    private void SourceMenu(AppTab tab, PointerEventData eventData)
    {
        if (contextMenu != null)
            contextMenu.ShowForTab(this, tab, eventData);
    }

    /// <summary>The order changed: the navigator follows, and it is saved.</summary>
    private void OrderChanged()
    {
        if (nav != null)
            nav.ApplyOrder(_order);
        DesktopPreferences.AppTabs = _order.Save();
        RefreshNav();
    }

    /// <summary>A source's badge dot on the navigator.</summary>
    private void SetBadge(AppTab tab, bool on)
    {
        if (nav != null)
            nav.SetBadge(tab, on);
    }

    /// <summary>The player sees the source: its badge goes.</summary>
    private void Seen(AppTab tab)
    {
        _badges.Seen(tab);
        SetBadge(tab, false);
    }
}
