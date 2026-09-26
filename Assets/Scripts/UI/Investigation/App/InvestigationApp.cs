using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// The Investigation app (the PC redesign AP1-AP4, AP8, AP9, LK2, WN5, DK2):
/// one desktop window, "Investigation" (· the traveller's name while one is at
/// the desk), that fills the desktop the first time it opens. Its case header
/// holds the claim, the counters ("Papers 2 of 3 received · 1 scanned ·
/// Deviations 1") and the PC's Accept and Deny (the façade wires them); its
/// toolbar holds Back and Forward (the active pane's history), the search
/// field, Steps, Split (two panes side by side, saved per player, possible
/// only while each pane gets a readable width: AppPanes.CanSplit, so a
/// restored window has one pane and the button says why) and Keys (the search
/// field is live since phase 19, InvestigationApp.Search, and Keys since phase
/// 20; Steps is shown but not live until phase 21); the sidebar holds Steps (a placeholder until phase 21), Pinned
/// and Recent. The keys, the focus ring, copy and paste, pins, recent items
/// and zoom are in InvestigationApp.Keys (redesign phase 20). Two
/// panes share one tab order (TabOrder: dragged or moved from a tab's menu,
/// saved per player in DesktopPreferences.AppTabs). The active pane is the
/// last one pressed (the desktop's press, DesktopWindowManager.Pressed) or
/// sent somewhere; it wears the accent frame. A row's smart link goes to the
/// other pane (Ctrl held, or one pane: the same pane, and Back returns);
/// the compare dock's sides, the toast and Mail's memo open in the active
/// pane (Open). Nothing steals the view: something new for a tab badges it
/// unless a showing pane shows it (AppBadges), and dots the desktop's
/// Investigation icon while the app is closed or minimised; a scan
/// (ScanArrival) opens the app only when it is closed, shows the paper only in
/// a Documents view showing none, and toasts ("… scanned", Open shows it). A
/// new case shows Documents in the left pane (the right one keeps its
/// source), drops the last traveller's places from both histories and clears
/// the badges; at the decision the case sources show the no-case state.
/// InvestigationUIController drives it.
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

    [Header("Case header")]
    /// <summary>The claim (the claim banner's text, which the office claim tag also shows).</summary>
    [SerializeField] private TMP_Text claimText;

    /// <summary>The counters: papers received and scanned, deviations logged.</summary>
    [SerializeField] private TMP_Text countersText;

    [Header("Toolbar")]
    /// <summary>Back in the active pane.</summary>
    [SerializeField] private Button backButton;

    /// <summary>Forward in the active pane.</summary>
    [SerializeField] private Button forwardButton;

    /// <summary>Split: two panes or one.</summary>
    [SerializeField] private Button splitButton;

    /// <summary>The Split button's hover hint: what it does, or why it cannot.</summary>
    [SerializeField] private TMP_Text splitHint;

    /// <summary>Steps: shown, not live until its phase (21).</summary>
    [SerializeField] private Selectable[] notYetLive = new Selectable[0];

    /// <summary>The Split button's tint while the split is on (pressed).</summary>
    [SerializeField] private Color splitOnTint = new Color(0.72f, 0.72f, 0.72f, 1f);

    [Header("Desktop")]
    /// <summary>The scan toast (on the desktop, above the windows: it shows while the app is down too).</summary>
    [SerializeField] private AppToast toast;

    /// <summary>The desktop's knobs (the toast's time, the panes' widths).</summary>
    [SerializeField] private DesktopConfigSO config;

    /// <summary>The desktop's icons (the Investigation icon's dot).</summary>
    [SerializeField] private DesktopIcons icons;

    /// <summary>The desktop's right-click menu (a tab's Move left, Move right, Reset tab order).</summary>
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

    /// <summary>True when the panes host a view for the tab.</summary>
    public bool Hosts(AppTab tab) => leftPane != null && leftPane.Hosts(tab);
    /// <summary>The app's window (the keyboard poller's "app focused").</summary>
    public DesktopWindow Window => window;


    /// <summary>The first open: the app fills the desktop (P spec WN4).</summary>
    private void Start()
    {
        if (window != null && !window.IsMaximised)
            window.ToggleMaximise();
    }

    /// <summary>The app shows (opened or restored): the shown tabs are seen, the icon's dot goes, and the panes fit the window.</summary>
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

    /// <summary>A traveller is presented: the title, the claim, the left pane on Documents, the histories without the last traveller, the badges and the icon's dot cleared, the toast gone, search's case layer empty.</summary>
    public void BeginCase(string claim, string travellerName)
    {
        Init();
        ResetSearchCase();
        if (window != null)
            window.SetTitle(UiText.Format("app.titleCase", travellerName));
        if (claimText != null)
            claimText.text = claim;
        _badges.Clear();
        foreach (AppPane pane in Panes())
        {
            foreach (AppTab tab in TabOrder.Default)
                pane.SetBadge(tab, false);
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

    /// <summary>The decision: the case sources show the no-case state; the header waits for the next traveller; search forgets the case.</summary>
    public void EndCase()
    {
        Init();
        _papers = null;
        ResetSearchCase();
        if (window != null)
            window.SetTitle(UiText.Get("app.title"));
        if (claimText != null)
            claimText.text = UiText.Get("idle.waiting");
        if (countersText != null)
            countersText.text = string.Empty;
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

    /// <summary>Something new for the tab (a transcript line, a logged deviation): badged unless a showing pane shows it; the icon dotted while the app is down.</summary>
    public void Arrived(AppTab tab)
    {
        Init();
        if (_badges.Arrived(tab, ShownTabs()))
            foreach (AppPane pane in Panes())
                pane.SetBadge(tab, true);
        if (!IsShowing && icons != null)
            icons.SetBadge(DesktopAppIds.Investigation, IconBadge.Dot);
    }

    /// <summary>Paper <paramref name="paper"/> was scanned (ScanArrival): the app opens when closed, the paper shows in each Documents view showing none, Documents is badged unless seen, a toast names it.</summary>
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

    /// <summary>Opens the app (a minimised one restores) on the tab in the active pane: Mail's directive memo shows the Rules.</summary>
    public void ShowTab(AppTab tab) => Open(LinkTarget.ToTab(tab), false);

    /// <summary>
    /// Opens the app (a minimised one restores) at <paramref name="target"/>:
    /// in the active pane, or the other one (<paramref name="otherPane"/>, while
    /// split), which then becomes active. The compare dock's sides, the toast
    /// and Mail come here; phases 19 and 20 (a search hit, a pin, a recent)
    /// come here too.
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

    /// <summary>Moves a tab by <paramref name="delta"/> positions in both strips and saves the order (a tab's Move left and Move right).</summary>
    public void MoveTab(AppTab tab, int delta)
    {
        Init();
        int from = _order.PositionOf(tab);
        if (_order.Move(from, from + delta))
            OrderChanged();
    }

    /// <summary>Back to the default tab order in both strips, saved ("Reset tab order").</summary>
    public void ResetTabOrder()
    {
        Init();
        if (_order.Reset())
            OrderChanged();
    }

    /// <summary>Wires the panes, the toolbar and the desktop's press, reads the saved order and split, and turns the toolbar's not-yet-live controls off (once; the app is driven while its window is closed).</summary>
    private void Init()
    {
        if (_ready)
            return;
        _ready = true;
        _order = TabOrder.Parse(DesktopPreferences.AppTabs);
        _splitWanted = DesktopPreferences.AppSplit;
        _active = leftPane;

        foreach (AppPane pane in new[] { leftPane, rightPane })
        {
            if (pane == null)
                continue;
            pane.Shown += PaneShown;
            pane.HistoryChanged += HistoryChanged;
            pane.LinkFollowed += Follow;
            pane.TabDragged += (tab, position) => MoveTabTo(tab, position);
            pane.TabMenuRequested += TabMenu;
            pane.ApplyOrder(_order);
        }

        if (backButton != null)
            backButton.onClick.AddListener(() => _active.Back());
        if (forwardButton != null)
            forwardButton.onClick.AddListener(() => _active.Forward());
        if (splitButton != null)
            splitButton.onClick.AddListener(ToggleSplit);
        foreach (Selectable control in notYetLive)
            if (control != null)
                control.interactable = false;

        _manager = window != null ? window.Manager : null;
        if (_manager != null)
            _manager.Pressed += Pressed;
        InitKeys();
        InitSearch();
        Layout();
        RefreshHistoryButtons();
    }

    /// <summary>The Split button: two panes or one, saved per player.</summary>
    private void ToggleSplit()
    {
        _splitWanted = !_splitWanted;
        DesktopPreferences.AppSplit = _splitWanted;
        Layout();
    }

    /// <summary>
    /// Two panes when the player wants them and the body holds them
    /// (AppPanes.CanSplit), else the left one alone (the right one's history
    /// and view stay for the next split); the Split button greys, with its
    /// hint saying why, when they cannot fit.
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
        leftPane.SetFrame(_split && _active == leftPane);
        if (rightPane != null)
            rightPane.SetFrame(_split && _active == rightPane);

        if (splitButton != null)
        {
            splitButton.interactable = fits;
            ColorBlock colours = splitButton.colors;
            colours.normalColor = _split ? splitOnTint : Color.white;
            colours.selectedColor = colours.normalColor;
            splitButton.colors = colours;
        }
        if (splitHint != null)
            splitHint.text = UiText.Get(fits ? "app.split.hint" : "app.split.tooNarrow");
        RefreshHistoryButtons();
    }

    /// <summary>Both panes (the right one keeps its case state, badges and views current while it is hidden, for the next split).</summary>
    private IEnumerable<AppPane> Panes()
    {
        if (leftPane != null)
            yield return leftPane;
        if (rightPane != null)
            yield return rightPane;
    }

    /// <summary>The tabs the player sees: the showing panes' active tabs (none while the app is down).</summary>
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

    /// <summary>The pane becomes the active one (its frame, the toolbar's history).</summary>
    private void Activate(AppPane pane)
    {
        if (pane == null || (!_split && pane == rightPane))
            return;
        _active = pane;
        leftPane.SetFrame(_split && _active == leftPane);
        if (rightPane != null)
            rightPane.SetFrame(_split && _active == rightPane);
        RefreshHistoryButtons();
    }

    /// <summary>A press on the desktop (DesktopWindowManager.Pressed): inside a pane, that pane is active.</summary>
    private void Pressed(GameObject top)
    {
        if (top == null)
            return;
        foreach (AppPane pane in Panes())
            if (pane.gameObject.activeInHierarchy && top.transform.IsChildOf(pane.transform))
                Activate(pane);
    }

    /// <summary>A pane showed a tab: the tab is seen when the pane shows.</summary>
    private void PaneShown(AppPane pane, AppTab tab)
    {
        if (IsShowing && (pane == leftPane || _split))
            Seen(tab);
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

    /// <summary>A tab dragged past a neighbour's middle: to its new position in both strips, saved.</summary>
    private void MoveTabTo(AppTab tab, int position)
    {
        if (_order.Move(_order.PositionOf(tab), position))
            OrderChanged();
    }

    /// <summary>A tab's right-click: its menu.</summary>
    private void TabMenu(AppTab tab, PointerEventData eventData)
    {
        if (contextMenu != null)
            contextMenu.ShowForTab(this, tab, eventData);
    }

    /// <summary>The order changed: both strips follow, and it is saved.</summary>
    private void OrderChanged()
    {
        foreach (AppPane pane in Panes())
            pane.ApplyOrder(_order);
        DesktopPreferences.AppTabs = _order.Save();
    }

    /// <summary>The player sees the tab: its badge goes.</summary>
    private void Seen(AppTab tab)
    {
        _badges.Seen(tab);
        foreach (AppPane pane in Panes())
            pane.SetBadge(tab, false);
    }
}
