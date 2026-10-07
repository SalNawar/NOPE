using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The Investigation app as a workbench (the PC workbench spec,
/// docs/superpowers/specs/2026-09-30-pc-workbench-design.md; lessons 1, 2,
/// D5, D6, D10; polished in wave 5 A3; the desk-first redesign, items 7 and
/// 8): one desktop window, "Investigation", that fills the desktop the first
/// time it opens. Its menu bar (MenuBarView) holds the case's and the day's
/// documents in drop-down menus (Papers: the traveller's papers, the
/// transcript and the papers not handed over, to flag missing; Records;
/// Rules; Books; Calendar, with today's date to hold), each shown from the
/// day it is introduced, and at its right Search; a row opens its document
/// on the target side, and a document already open on the other side swaps
/// (OpenOnTarget). The guided steps run headless (GuideBar: a new case and
/// the keys put a pair of documents up); the PC only investigates (the PC
/// clean-up of 2026-10-05: the verdict is the stamp on the passport at the
/// desk). The work area, the window's whole height under the bar, holds the
/// status line and two panes (AppPane: the left and the right, a gutter
/// between them where a line's label sits; a click on a header makes it the
/// target, F6 too; the target is where the keys act and the history walks),
/// over which the workbench (MatchBoard) draws the line between two compared
/// values. The status line says what is held or the last result, else who
/// is at the desk ("<b>Yichen (Soldier)</b> · Papers: 1 of 2 received, 0
/// scanned"; no claim, which the traveller only says) or that the desk waits;
/// a notice (the toast: a paper scanned with Open, a copy, a pin) takes its
/// right end while it shows, so it covers nothing. The findings column sits at the work area's right: a slim
/// rail while nothing is logged, so the documents take the width (Ctrl+B
/// hides it; remembered per player). Two panes show while the body holds
/// them at their least width and the gutter (AppPanes.CanSplit; Ctrl+\ joins
/// or splits them, remembered per player): the restored window has one,
/// always the target. The keys, the focus ring, copy and paste,
/// pins, recent items and zoom are in InvestigationApp.Keys; the search
/// drawer in InvestigationApp.Search. Nothing steals the view: something new
/// for a source dots its menu until it is seen (AppBadges; a document not
/// opened yet this case is dotted too), and dots the desktop's Investigation
/// icon while the app is closed or minimised; a scan (ScanArrival) opens the
/// app only when it is closed, shows the paper only in a Papers view showing
/// none, and toasts ("… scanned", Open shows it). A new case goes to the
/// first step (its pair up), drops the last traveller's places from both
/// histories and clears the dots; once the traveller is decided at the desk
/// the case sources show the no-case state. InvestigationUIController drives it.
/// </summary>
public sealed partial class InvestigationApp : MonoBehaviour
{
    /// <summary>The app's window (maximised on the first open).</summary>
    [SerializeField] private DesktopWindow window;

    /// <summary>The left pane (the one pane while there is room for one).</summary>
    [SerializeField] private AppPane leftPane;

    /// <summary>The right pane (shown while two fit).</summary>
    [SerializeField] private AppPane rightPane;

    /// <summary>The work area under the shelf (its width decides whether two panes fit).</summary>
    [SerializeField] private RectTransform work;

    /// <summary>The findings column at the work area's right (its width counts against the panes; Ctrl+B hides it).</summary>
    [SerializeField] private RectTransform findingsColumn;

    /// <summary>The findings column's width while nothing is logged (a slim rail; the documents take the rest).</summary>
    [SerializeField, Min(0f)] private float findingsRail = 56f;

    /// <summary>The main column (the status line and the panes), left of the findings.</summary>
    [SerializeField] private RectTransform mainColumn;

    [Header("The workbench")]
    /// <summary>The guided steps (headless since the desk-first redesign: the keys' pairs of documents).</summary>
    [SerializeField] private GuideBar guide;

    /// <summary>The menu bar: the documents' menus (the desk-first redesign, item 8).</summary>
    [SerializeField] private MenuBarView menus;

    /// <summary>The click-and-match, the findings and the status line (its idle plate says who is at the desk).</summary>
    [SerializeField] private MatchBoard board;

    /// <summary>The notice (a paper scanned, a copy, a pin) at the status line's right end: it shows while the app does.</summary>
    [SerializeField] private AppToast toast;

    [Header("Desktop")]

    /// <summary>The desktop's knobs (the toast's time, the panes' widths).</summary>
    [SerializeField] private DesktopConfigSO config;

    /// <summary>The desktop's icons (the Investigation icon's dot).</summary>
    [SerializeField] private DesktopIcons icons;

    private readonly AppBadges _badges = new AppBadges();
    private readonly List<AppTab> _shown = new List<AppTab>(2);
    private readonly HashSet<string> _opened = new HashSet<string>();
    private readonly List<ShelfItem> _items = new List<ShelfItem>();
    private readonly List<AppRow> _rowScratch = new List<AppRow>();
    private AppPane _target;
    private CasePapers _papers;
    private DesktopWindowManager _manager;
    private string _traveller;
    private string _counters;
    private bool _splitWanted = true;
    private bool _split;
    private bool _ready;

    /// <summary>True while the app shows (open and not minimised).</summary>
    public bool IsShowing => window != null && window.gameObject.activeSelf;

    /// <summary>True when the panes host a view for the source.</summary>
    public bool Hosts(AppTab tab) => leftPane != null && leftPane.Hosts(tab);

    /// <summary>The workbench (the façade reads its findings: the evidence a denial rests on).</summary>
    public MatchBoard Board => board;

    /// <summary>The guided steps (the façade feeds them the case's events).</summary>
    public GuideBar Guide => guide;

    /// <summary>The app's window (the keyboard poller's "app focused").</summary>
    public DesktopWindow Window => window;

    /// <summary>The first open: the app fills the desktop.</summary>
    private void Start()
    {
        if (window != null && !window.IsMaximised)
            window.ToggleMaximise();
    }

    /// <summary>The app shows (opened or restored): the shown documents are seen, the icon's dot goes, and the panes fit the window.</summary>
    private void OnEnable()
    {
        if (leftPane == null)
            return;
        Init();
        Layout();
        MarkShelf();
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

    /// <summary>
    /// A traveller is presented: the status line names who stands at the desk
    /// (never what they ask for), the histories without the
    /// last traveller, the dots and the icon's dot cleared, the toast gone,
    /// search's case layer empty; the façade then starts the workbench
    /// (MatchBoard.BeginCase) and the steps (GuideBar.BeginCase puts the first
    /// pair up).
    /// </summary>
    public void BeginCase(string travellerName)
    {
        Init();
        ResetSearchCase();
        _traveller = travellerName ?? string.Empty;
        _counters = null;
        ShowWho();
        _badges.Clear();
        _opened.Clear();
        foreach (AppPane pane in Panes())
        {
            pane.SetCase(true);
            pane.DropCaseHistory();
        }
        if (icons != null)
            icons.SetBadge(DesktopAppIds.Investigation, 0);
        if (toast != null)
            toast.Hide();
        RefreshShelf();
        KeysBeginCase(travellerName);
    }

    /// <summary>The traveller was decided at the desk: the case sources show the no-case state; the status line waits for the next traveller; search forgets the case; the workbench empties.</summary>
    public void EndCase()
    {
        Init();
        _papers = null;
        _traveller = null;
        _counters = null;
        ResetSearchCase();
        _missing = MissingPapers.None;
        _missingPapers = null;
        foreach (AppPane pane in Panes())
            pane.SetCase(false);
        if (toast != null)
            toast.Hide();
        if (board != null)
            board.EndCase();
        ShowWho();
        RefreshShelf();
        KeysEndCase();
    }

    /// <summary>Writes the counters from the case's papers (kept: a link to a paper opens only once it is scanned).</summary>
    public void SetCounters(CasePapers papers)
    {
        _papers = papers;
        if (papers != null)
            _counters = UiText.Format("app.counters", papers.Received, papers.Count, papers.Scanned);
        ShowWho();
    }

    /// <summary>The status line's idle plate: who is at the desk with the counters ("<b>Yichen (Soldier)</b> · Papers: 1 of 2 received, 0 scanned"), else "Waiting for the next traveller".</summary>
    private void ShowWho()
    {
        if (board == null)
            return;
        board.SetIdleHint(_traveller == null ? UiText.Get("idle.waiting")
                        : _counters == null ? UiText.Format("app.atDesk.name", _traveller)
                        : UiText.Format("app.atDesk", _traveller, _counters));
    }

    /// <summary>Something new for the source (a transcript line, a logged deviation): its chip dotted unless a showing pane shows it; the icon dotted while the app is down.</summary>
    public void Arrived(AppTab tab)
    {
        Init();
        _badges.Arrived(tab, ShownTabs());
        MarkShelf();
        if (!IsShowing && icons != null)
            icons.SetBadge(DesktopAppIds.Investigation, IconBadge.Dot);
    }

    /// <summary>Paper <paramref name="paper"/> was scanned (ScanArrival): the app opens when closed, the paper shows in each Papers view showing none, Papers dotted unless seen, a toast names it.</summary>
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
        if (arrival.OpenApp)
        {
            OpenWindow();
            OpenBoard(paper);
        }
        if (arrival.BadgeDocuments)
            Arrived(AppTab.Documents);
        if (arrival.Toast && toast != null)
            toast.Show(UiText.Format("app.toast.scanned", paperName), config != null ? config.toastSeconds : 4f, () =>
            {
                OpenWindow();
                OpenBoard(paper);
            });
    }

    /// <summary>
    /// The case board up (the scanner app spec §2: "when a scan lands, the app
    /// opens to that traveller's case board"): the scanned paper on the left
    /// and the board on the right, or the board alone in one pane; the
    /// scanned paper alone where no pane hosts a board.
    /// </summary>
    private void OpenBoard(int paper)
    {
        LinkTarget scanned = LinkTarget.ToTab(AppTab.Documents, paper);
        if (!Hosts(AppTab.Board))
            OpenOnTarget(scanned);
        else
            OpenPair(_split ? scanned : LinkTarget.None, LinkTarget.ToTab(AppTab.Board));
    }

    /// <summary>Opens the app (a minimised one restores) on the source on the target side: Mail's directive memo shows today's rules.</summary>
    public void ShowTab(AppTab tab) => OpenOnTarget(LinkTarget.ToTab(tab));

    /// <summary>
    /// Opens the app (a minimised one restores) at <paramref name="target"/>
    /// on the target side (a shelf chip, the toast, Mail, a search hit, a pin,
    /// a recent item); the document open on the other side swaps to where the
    /// target side was, so one document is never open twice. False when the
    /// target is gone (its view says so; the source shows all the same).
    /// </summary>
    public bool OpenOnTarget(LinkTarget target)
    {
        Init();
        if (target.IsNone)
            return false;
        OpenWindow();
        AppPane to = TargetPane, other = _split ? Other(to) : null;
        if (other != null && SameDocument(other, target))
            other.Go(to.Current);
        return to.Go(target);
    }

    /// <summary>Puts <paramref name="left"/> up on the left and <paramref name="right"/> on the right (a step's pair, a finding revisited); one pane: the left one alone (the right one when the left is none). The right side becomes the target. It opens no window (a new case's first pair waits in a closed app: nothing steals the view).</summary>
    public void OpenPair(LinkTarget left, LinkTarget right)
    {
        Init();
        if (_split)
        {
            if (!left.IsNone)
                leftPane.Go(left);
            if (!right.IsNone)
                rightPane.Go(right);
            SetTarget(rightPane);
        }
        else if (!right.IsNone || !left.IsNone)
            leftPane.Go(left.IsNone ? right : left);
    }

    /// <summary>Where a pick was picked (SmartLinks.ForKey over the case's papers).</summary>
    public LinkTarget LinkFor(string pickKey) => SmartLinks.ForKey(pickKey, _papers);

    /// <summary>The box of the row with <paramref name="key"/> in a showing pane's view (the workbench's line), or null; <paramref name="scratch"/> is reused.</summary>
    public RectTransform FindRow(string key, List<AppRow> scratch)
    {
        if (string.IsNullOrEmpty(key) || !IsShowing)
            return null;
        foreach (AppPane pane in ShowingPanes())
        {
            if (!(pane.View(pane.ActiveTab) is Component view) || !view.gameObject.activeInHierarchy)
                continue;
            view.GetComponentsInChildren(false, scratch);
            foreach (AppRow row in scratch)
                if (row.Key == key)
                    return (RectTransform)row.transform;
        }
        return null;
    }

    /// <summary>Wires the panes, the steps, the shelf and the desktop's press, and reads the saved split (once; the app is driven while its window is closed).</summary>
    private void Init()
    {
        if (_ready)
            return;
        _ready = true;
        _splitWanted = DesktopPreferences.AppSplit;
        _target = rightPane != null ? rightPane : leftPane;

        foreach (AppPane pane in Panes())
        {
            pane.Shown += PaneShown;
            pane.Changed += PaneChanged;
            pane.LinkFollowed += Follow;
            pane.HeaderClicked += SetTarget;
            foreach (AppTab tab in TabOrder.Default)
            {
                IAppView view = pane.View(tab);
                if (view != null && pane == leftPane)
                    view.ChipsChanged += RefreshShelf;
            }
        }
        if (menus != null)
        {
            menus.Opened += item => OpenOnTarget(item.Target);
            menus.Flagged += id => MissingFlagged?.Invoke(id);
            menus.TodayHeld += HoldToday;
        }
        if (guide != null)
            guide.StageShown += StageShown;
        if (board != null)
            board.Changed += BoardChanged;

        _manager = window != null ? window.Manager : null;
        if (_manager != null)
            _manager.Pressed += Pressed;
        InitKeys();
        InitSearch();
        Layout();
        RefreshShelf();
        ShowWho();
    }

    /// <summary>Ctrl+\: two panes or one, saved per player.</summary>
    private void ToggleSplit()
    {
        _splitWanted = !_splitWanted;
        DesktopPreferences.AppSplit = _splitWanted;
        Layout();
    }

    /// <summary>
    /// Two panes when the player wants them and the main column holds them
    /// (AppPanes.CanSplit), else the left one alone, the target (the right
    /// one's history and view stay for the next split).
    /// </summary>
    private void Layout()
    {
        if (leftPane == null || mainColumn == null)
            return;
        bool fits = rightPane != null && config != null && AppPanes.CanSplit(mainColumn.rect.width, config.paneGap, config.paneMinWidth);
        _split = _splitWanted && fits;

        float gap = config != null ? config.paneGap / 2f : 7f;
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
        if (!_split || _target == null)
            _target = _split ? rightPane : leftPane;
        Targets();
        MarkShelf();
    }

    /// <summary>A step was gone to: its pair of documents put up, the right side the target.</summary>
    private void StageShown(GuideStage stage)
    {
        LinkTarget firstPaper = LinkTarget.ToTab(AppTab.Documents, 0);
        switch (stage)
        {
            case GuideStage.Papers:
                OpenPair(firstPaper, PaperCount > 1 ? LinkTarget.ToTab(AppTab.Documents, 1) : LinkTarget.ToTab(AppTab.Transcript));
                break;
            case GuideStage.Records:
                OpenPair(firstPaper, guide.RecordsTarget());
                break;
            case GuideStage.Books:
                OpenPair(firstPaper, guide.BooksTarget());
                break;
            case GuideStage.Rules:
                OpenPair(LinkTarget.ToTab(AppTab.Rules), firstPaper);
                break;
        }
    }

    /// <summary>The papers of the case (the left pane's Papers view's).</summary>
    private int PaperCount => leftPane != null && leftPane.View(AppTab.Documents) != null ? leftPane.View(AppTab.Documents).Chips.Count : 0;

    /// <summary>The findings or what is held changed: the findings column opens from its rail at the first finding (or folds back at a new case).</summary>
    private void BoardChanged()
    {
        if (FindingsRail != _railShown)
            ApplyFindings(_findingsShown);
    }

    /// <summary>True while nothing is logged: the findings column is a slim rail.</summary>
    private bool FindingsRail => board == null || board.Log.Items.Count == 0;

    /// <summary>Both panes (the right one keeps its case state and views current while it is hidden, for the next split).</summary>
    private IEnumerable<AppPane> Panes()
    {
        if (leftPane != null)
            yield return leftPane;
        if (rightPane != null)
            yield return rightPane;
    }

    /// <summary>The panes that show: the left one, and the right one while split.</summary>
    private IEnumerable<AppPane> ShowingPanes()
    {
        if (leftPane != null)
            yield return leftPane;
        if (_split && rightPane != null)
            yield return rightPane;
    }

    /// <summary>The sources the player sees: the showing panes' (none while the app is down).</summary>
    private IReadOnlyList<AppTab> ShownTabs()
    {
        _shown.Clear();
        if (IsShowing)
            foreach (AppPane pane in ShowingPanes())
                _shown.Add(pane.ActiveTab);
        return _shown;
    }

    /// <summary>The other pane.</summary>
    private AppPane Other(AppPane pane) => pane == leftPane ? rightPane : leftPane;

    /// <summary>The pane the shelf opens on and the keys act on.</summary>
    private AppPane TargetPane => _target != null && (_split || _target == leftPane) ? _target : leftPane;

    /// <summary>True when <paramref name="pane"/> shows the document <paramref name="target"/> names (its source, and its item for a source with items).</summary>
    private static bool SameDocument(AppPane pane, LinkTarget target)
    {
        (AppTab tab, int item) = pane.Document;
        if (tab != target.Tab)
            return false;
        return item < 0 || target.Item < 0 || item == target.Item;
    }

    /// <summary>A pane's header clicked (or F6): that side is the target.</summary>
    private void SetTarget(AppPane pane)
    {
        if (pane == null || (!_split && pane == rightPane))
            return;
        _target = pane;
        Targets();
    }

    /// <summary>The target side's header shows it (one pane: always the target).</summary>
    private void Targets()
    {
        leftPane.SetTarget(TargetPane == leftPane);
        if (rightPane != null)
            rightPane.SetTarget(_split && TargetPane == rightPane);
    }

    /// <summary>A press on the desktop (DesktopWindowManager.Pressed): outside the search drawer's panel, the drawer closes; outside the open menu (and the titles), the menu closes.</summary>
    private void Pressed(GameObject top)
    {
        PressedForSearch(top);
        if (menus != null && menus.MenuOpen && !menus.IsPart(top))
            menus.Close();
    }

    /// <summary>True while a menu of the menu bar is open (Escape closes it first, as a menu).</summary>
    public bool MenuOpen => menus != null && menus.MenuOpen;

    /// <summary>Escape's CloseMenu for the menu bar's open menu.</summary>
    public void CloseMenu()
    {
        if (menus != null)
            menus.Close();
    }

    /// <summary>Raised when the Papers menu flags a paper missing (its request id; the facade unlocks its request on the wheel).</summary>
    public event System.Action<string> MissingFlagged;

    /// <summary>The papers the clerk can flag missing for the traveller at the desk.</summary>
    private MissingPapers _missing = MissingPapers.None;

    /// <summary>Where each of the traveller's papers is (what is still not handed over).</summary>
    private CasePapers _missingPapers;

    /// <summary>The papers to flag missing (<paramref name="missing"/>, those not handed over by <paramref name="papers"/>) and their flags: the Papers menu redraws.</summary>
    public void SetMissing(MissingPapers missing, CasePapers papers)
    {
        _missing = missing ?? MissingPapers.None;
        _missingPapers = papers;
        RefreshShelf();
    }

    /// <summary>
    /// Holds today's date on the workbench (the Calendar menu's date, the
    /// taskbar's date; the desk-first redesign: "the date should be on the
    /// PC"), to match with an expiry or a ticket's date; the app opens. Nothing
    /// when the agency's calendar has no readable date, or while the app is
    /// not on the PC yet (OnPc: the desk's calendar is compared in inspect
    /// mode instead).
    /// </summary>
    public void HoldToday()
    {
        Init();
        string today = Today;
        if (today == null || board == null || !OnPc)
            return;
        OpenWindow();
        board.PickToday(today);
    }

    /// <summary>Today's date as the papers print it (the Calendar view's), or null.</summary>
    private string Today => leftPane != null && leftPane.View(AppTab.Calendar) is CalendarView calendar ? calendar.Today : null;

    /// <summary>A pane showed a source: it is seen when the pane shows.</summary>
    private void PaneShown(AppPane pane, AppTab tab)
    {
        if (IsShowing && (pane == leftPane || _split))
            _badges.Seen(tab);
    }

    /// <summary>A pane's document changed: it is opened this case, and the shelf marks the sides.</summary>
    private void PaneChanged(AppPane pane)
    {
        if (!_ready)
            return;
        if (pane == leftPane || _split)
        {
            (AppTab tab, int item) = pane.Document;
            if (!pane.Blocked)
                _opened.Add(DocKey(tab, item));
        }
        MarkShelf();
    }

    /// <summary>A row's link (LK2): into the other pane, or the same one with Ctrl held or one pane.</summary>
    private void Follow(AppPane from, LinkTarget target, bool samePane)
    {
        AppPane to = samePane || !_split ? from : Other(from);
        to.Go(target);
    }

    /// <summary>
    /// The day's introductions (the desk-first redesign, items 3 and 9): the
    /// shelf lists only the agency documents introduced (Citizen records,
    /// today's rules, the calendar: Feature.Records, Rulebook, Calendar) and
    /// the desktop shows only the apps introduced (Feature.App), this app
    /// itself opening only once it is (OnPc); a pane on
    /// the books before any is introduced turns to today's rules; set at the
    /// day's start.
    /// </summary>
    public void SetIntroductions(Introductions introductions, int day)
    {
        _introductions = introductions ?? Introductions.None;
        _day = day;
        OnPc = _introductions.Has(_day, Feature.App(DesktopAppIds.Investigation));
        if (icons != null)
            icons.ShowApps(id => _introductions.Has(_day, Feature.App(id)));
        RefreshShelf();
        // A pane left on a book no day has introduced yet shows today's rules instead (the right pane starts on the books).
        foreach (AppPane pane in new[] { leftPane, rightPane })
            if (pane != null && pane.ActiveTab == AppTab.Reference && !_items.Exists(x => x.Group == ShelfGroup.Books) && pane.Hosts(AppTab.Rules))
                pane.Show(AppTab.Rules);
    }

    /// <summary>What the day has introduced (SetIntroductions).</summary>
    private Introductions _introductions = Introductions.None;

    /// <summary>
    /// True while the app is on the PC: from the day its key is introduced
    /// (Feature.App(DesktopAppIds.Investigation), day 5 with the scanner:
    /// Saleh 2026-10-06, "hide the application if it is not used and unlock
    /// it when the scanner is available or it is needed"); true until a day
    /// says (SetIntroductions). While false nothing opens the app: no icon
    /// (DesktopIcons.ShowApps), no link (Mail's Rules link hides), no shelf
    /// or search jump, no hold of today's date from the taskbar.
    /// </summary>
    public bool OnPc { get; private set; } = true;

    /// <summary>Opens the window (a minimised one restores), only while the app is on the PC (OnPc).</summary>
    private void OpenWindow()
    {
        if (window != null && OnPc)
            window.Open();
    }

    /// <summary>Today's day number (SetIntroductions).</summary>
    private int _day;

    /// <summary>
    /// The menus' rows from the left pane's views, in three groups: the
    /// traveller's (each paper handed over, by its name, not readable yet:
    /// dimmed; the transcript; then the papers not handed over, to flag
    /// missing), the agency's (Citizen records, today's rules, the calendar
    /// and today's date, each from the day it is introduced) and the books
    /// (each by its name, from the day it is introduced:
    /// ReferenceView.OnShelf); drawn again only when they change.
    /// </summary>
    private void RefreshShelf()
    {
        if (menus == null || leftPane == null)
            return;
        bool Introduced(string feature) => _introductions.Has(_day, feature);
        var items = new List<ShelfItem>();
        IAppView documents = leftPane.View(AppTab.Documents);
        if (documents != null && _traveller != null)
            for (int i = 0; i < documents.Chips.Count; i++)
                if (_missingPapers == null || _missingPapers.State(i) != PaperState.NotHandedOver) // a paper not handed over is under "Not handed over", by its request (it tells nothing of what they carry)
                    items.Add(new ShelfItem(ShelfGroup.Traveller, documents.Chips[i].Label, LinkTarget.ToTab(AppTab.Documents, i), documents.Chips[i].Available));
        if (_traveller != null && Hosts(AppTab.Transcript))
            items.Add(new ShelfItem(ShelfGroup.Traveller, UiText.Get("app.tab.transcript"), LinkTarget.ToTab(AppTab.Transcript), true));
        if (_traveller != null && Hosts(AppTab.Board) && Introduced(Feature.Scanner))
            items.Add(new ShelfItem(ShelfGroup.Traveller, UiText.Get("app.tab.board"), LinkTarget.ToTab(AppTab.Board), true));
        if (Hosts(AppTab.Records) && Introduced(Feature.Records))
            items.Add(new ShelfItem(ShelfGroup.Agency, UiText.Get("app.tab.records"), LinkTarget.ToTab(AppTab.Records), true));
        if (Hosts(AppTab.Rules) && Introduced(Feature.Rulebook))
            items.Add(new ShelfItem(ShelfGroup.Agency, UiText.Get("app.tab.rules"), LinkTarget.ToTab(AppTab.Rules), true));
        if (Hosts(AppTab.Calendar) && Introduced(Feature.Calendar))
            items.Add(new ShelfItem(ShelfGroup.Agency, UiText.Get("app.tab.calendar"), LinkTarget.ToTab(AppTab.Calendar), true));
        IAppView books = leftPane.View(AppTab.Reference);
        for (int i = 0; books != null && i < books.Chips.Count; i++)
            if (!(books is ReferenceView reference) || reference.OnShelf(i))
                items.Add(new ShelfItem(ShelfGroup.Books, books.Chips[i].Label, LinkTarget.ToTab(AppTab.Reference, i), books.Chips[i].Available));
        AddMenuExtras(items);

        if (!SameItems(items))
        {
            _items.Clear();
            _items.AddRange(items);
            menus.Show(_items);
        }
        MarkShelf();
    }

    /// <summary>
    /// The menus' rows beyond the documents (the desk-first redesign, items 7
    /// and 8): in Papers, each paper of the day's papers menu the traveller
    /// has not handed over, to flag missing (MissingPapers.Open, the same
    /// list for every traveller); in Calendar, today's date to hold (with the
    /// calendar, from the day it is introduced).
    /// </summary>
    private void AddMenuExtras(List<ShelfItem> items)
    {
        if (_traveller != null)
            foreach (FormRequest request in _missing.Open(_missingPapers))
                items.Add(ShelfItem.MissingPaper(request.Id, request.Label, _missing.State(request.Id)));
        string today = Today;
        if (today != null && items.Exists(x => x.Target.Tab == AppTab.Calendar))
            items.Add(ShelfItem.Today(UiText.Format("menubar.today", today)));
    }

    /// <summary>True when <paramref name="items"/> are the menus' rows already (ShelfItem.Same, in order).</summary>
    private bool SameItems(List<ShelfItem> items)
    {
        if (items.Count != _items.Count)
            return false;
        for (int i = 0; i < items.Count; i++)
            if (!items[i].Same(_items[i]))
                return false;
        return true;
    }

    /// <summary>The menus' side tags (where each document is open) and dots (a paper or the transcript not opened yet this case, or something new in a source).</summary>
    private void MarkShelf()
    {
        if (menus == null || leftPane == null)
            return;
        menus.Mark(SideOf, Unread);
    }

    /// <summary>"L" or "R" (the pane headers' "Left" and "Right", short so the shelf keeps one row) for the side a shelf document is open on, else null.</summary>
    private string SideOf(ShelfItem item)
    {
        if (SameDocument(leftPane, item.Target) && !leftPane.Blocked)
            return UiText.Get("shelf.side.left");
        if (_split && rightPane != null && SameDocument(rightPane, item.Target) && !rightPane.Blocked)
            return UiText.Get("shelf.side.right");
        return null;
    }

    /// <summary>True for the traveller's readable document not opened yet this case, or a source with something new (the agency's documents and the books are reference: no dot for being unopened).</summary>
    private bool Unread(ShelfItem item)
    {
        if (!item.Available || _traveller == null)
            return false;
        if (item.Target.Item < 0 && _badges.IsBadged(item.Target.Tab))
            return true;
        return item.Group == ShelfGroup.Traveller && !_opened.Contains(DocKey(item.Target.Tab, item.Target.Item));
    }

    /// <summary>A document's key for the opened set ("Documents:1", "Records:-1").</summary>
    private static string DocKey(AppTab tab, int item) => tab + ":" + (item < 0 ? -1 : item);
}
