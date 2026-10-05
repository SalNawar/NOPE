using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The Investigation app as a workbench (the PC workbench spec,
/// docs/superpowers/specs/2026-09-30-pc-workbench-design.md; lessons 1, 2,
/// D5, D6, D10; polished in wave 5 A3): one desktop window, "Investigation"
/// (the header names the traveller, so the title does not repeat it), that
/// fills the desktop the first time it opens. Its header says who is at the
/// desk (their face: the person, drawn from their look, never the paper's
/// photo; their name; one short line of counters; no claim, which the
/// traveller only says) beside the guided steps as one segmented control
/// (GuideBar: a step says what to do and puts a pair of documents up; it
/// never locks). Under it the shelf (ShelfView) holds the case's and the
/// day's documents on one row (the traveller's papers and transcript; the
/// agency's Citizen records, rules and calendar; the books behind one Books
/// menu); a chip opens its document on the target side, and a document
/// already open on the other side swaps (OpenOnTarget). The work area holds
/// the status line (the step's title and sentence, or what is held, or the
/// last result) and two panes (AppPane: the left and the right, a gutter
/// between them where a line's label sits; a click on a header makes it the
/// target, F6 too; the target is where the keys act and the history walks),
/// over which the workbench (MatchBoard) draws the line between two compared
/// values; at the decision step the panes give way to the decision
/// (DecisionView). The findings column sits at the work area's right: a slim
/// rail while nothing is logged, so the documents take the width (Ctrl+B
/// hides it; remembered per player). Two panes show while the body holds
/// them at their least width and the gutter (AppPanes.CanSplit; Ctrl+\ joins
/// or splits them, remembered per player): the restored window has one,
/// always the target. The keys, the focus ring, copy and paste,
/// pins, recent items and zoom are in InvestigationApp.Keys; the search
/// drawer in InvestigationApp.Search. Nothing steals the view: something new
/// for a source dots its chip until it is seen (AppBadges; a document not
/// opened yet this case is dotted too), and dots the desktop's Investigation
/// icon while the app is closed or minimised; a scan (ScanArrival) opens the
/// app only when it is closed, shows the paper only in a Papers view showing
/// none, and toasts ("… scanned", Open shows it). A new case goes to the
/// first step (its pair up), drops the last traveller's places from both
/// histories and clears the dots; at the decision the case sources show the
/// no-case state. InvestigationUIController drives it.
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

    /// <summary>The main column (the lead, the status line, the panes or the decision), left of the findings.</summary>
    [SerializeField] private RectTransform mainColumn;

    [Header("Header")]
    /// <summary>The traveller's face (the person at the desk, from their look).</summary>
    [SerializeField] private TravellerPortraitView face;

    /// <summary>The traveller's name.</summary>
    [SerializeField] private TMP_Text nameText;

    /// <summary>The counters: papers received and scanned; between travellers the idle line.</summary>
    [SerializeField] private TMP_Text countersText;

    [Header("The workbench")]
    /// <summary>The guided steps, the lead and the foot.</summary>
    [SerializeField] private GuideBar guide;

    /// <summary>The document shelf.</summary>
    [SerializeField] private ShelfView shelf;

    /// <summary>The click-and-match and the findings.</summary>
    [SerializeField] private MatchBoard board;

    /// <summary>The decision step (shown in place of the panes and the status line).</summary>
    [SerializeField] private DecisionView decision;

    /// <summary>The two panes' area (hidden at the decision).</summary>
    [SerializeField] private GameObject panesArea;

    /// <summary>The status line (hidden at the decision).</summary>
    [SerializeField] private GameObject statusLine;

    [Header("Desktop")]
    /// <summary>The scan toast (on the desktop, above the windows: it shows while the app is down too).</summary>
    [SerializeField] private AppToast toast;

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
    private bool _splitWanted = true;
    private bool _split;
    private bool _deciding;
    private bool _ready;

    /// <summary>True while the app shows (open and not minimised).</summary>
    public bool IsShowing => window != null && window.gameObject.activeSelf;

    /// <summary>True when the panes host a view for the source.</summary>
    public bool Hosts(AppTab tab) => leftPane != null && leftPane.Hosts(tab);

    /// <summary>The workbench (the façade gates Deny on its findings).</summary>
    public MatchBoard Board => board;

    /// <summary>The guided steps (the façade feeds them the case's events).</summary>
    public GuideBar Guide => guide;

    /// <summary>True when a showing pane shows the source (the steps: the Rules read while the player looks at the PC).</summary>
    public bool Sees(AppTab tab)
    {
        foreach (AppTab shown in ShownTabs())
            if (shown == tab)
                return true;
        return false;
    }

    /// <summary>Fills <paramref name="into"/> with the papers whose scanned copies the showing panes show on Papers (the steps: a paper read on the PC).</summary>
    public void CopiesSeen(List<int> into)
    {
        into.Clear();
        if (!IsShowing || _deciding)
            return;
        foreach (AppPane pane in ShowingPanes())
            if (pane.ActiveTab == AppTab.Documents && pane.View(AppTab.Documents) is DocumentsView documents && documents.ShowsCopy)
                into.Add(documents.Selected);
    }

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
    /// A traveller is presented: the header (their face from
    /// <paramref name="look"/> and <paramref name="art"/>, their name; who
    /// stands at the desk, never what they ask for), the histories without the
    /// last traveller, the dots and the icon's dot cleared, the toast gone,
    /// search's case layer empty; the façade then starts the workbench
    /// (MatchBoard.BeginCase) and the steps (GuideBar.BeginCase puts the first
    /// pair up).
    /// </summary>
    public void BeginCase(string travellerName, TravellerLook look, CharacterArt art)
    {
        Init();
        ResetSearchCase();
        _traveller = travellerName ?? string.Empty;
        if (nameText != null)
            nameText.text = _traveller;
        if (face != null)
            face.Show(look, art);
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

    /// <summary>The decision: the case sources show the no-case state; the header waits for the next traveller; search forgets the case; the workbench empties.</summary>
    public void EndCase()
    {
        Init();
        _papers = null;
        _traveller = null;
        ResetSearchCase();
        if (nameText != null)
            nameText.text = UiText.Get("app.title");
        if (countersText != null)
            countersText.text = UiText.Get("idle.waiting");
        if (face != null)
            face.Clear();
        foreach (AppPane pane in Panes())
            pane.SetCase(false);
        if (toast != null)
            toast.Hide();
        if (board != null)
            board.EndCase();
        ShowDecision(false);
        RefreshShelf();
        KeysEndCase();
    }

    /// <summary>Writes the counters from the case's papers (kept: a link to a paper opens only once it is scanned).</summary>
    public void SetCounters(CasePapers papers)
    {
        _papers = papers;
        if (countersText != null && papers != null)
            countersText.text = UiText.Format("app.counters", papers.Received, papers.Count, papers.Scanned);
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
        if (arrival.OpenApp && window != null)
            window.Open();
        if (arrival.BadgeDocuments)
            Arrived(AppTab.Documents);
        if (arrival.Toast && toast != null)
            toast.Show(UiText.Format("app.toast.scanned", paperName), config != null ? config.toastSeconds : 4f,
                       () => OpenOnTarget(LinkTarget.ToTab(AppTab.Documents, paper)));
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
        if (window != null)
            window.Open();
        if (_deciding && guide != null)
            guide.Step(-1);
        AppPane to = TargetPane, other = _split ? Other(to) : null;
        if (other != null && SameDocument(other, target))
            other.Go(to.Current);
        return to.Go(target);
    }

    /// <summary>Puts <paramref name="left"/> up on the left and <paramref name="right"/> on the right (a step's pair, a finding revisited); one pane: the left one alone (the right one when the left is none). The right side becomes the target. It opens no window (a new case's first pair waits in a closed app: nothing steals the view).</summary>
    public void OpenPair(LinkTarget left, LinkTarget right)
    {
        Init();
        ShowDecision(false);
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
        if (string.IsNullOrEmpty(key) || !IsShowing || _deciding)
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
        if (shelf != null)
            shelf.Opened += item => OpenOnTarget(item.Target);
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

    /// <summary>A step was gone to: its pair of documents put up, the right side the target; the decision shows in place of the panes.</summary>
    private void StageShown(GuideStage stage)
    {
        if (stage == GuideStage.Decision)
        {
            ShowDecision(true);
            return;
        }
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

    /// <summary>The decision step shows (in place of the panes and the status line) or goes.</summary>
    private void ShowDecision(bool on)
    {
        _deciding = on;
        if (panesArea != null && panesArea.activeSelf == on)
            panesArea.SetActive(!on);
        if (statusLine != null && statusLine.activeSelf == on)
            statusLine.SetActive(!on);
        if (decision != null)
        {
            if (decision.gameObject.activeSelf != on)
                decision.gameObject.SetActive(on);
            if (on)
                decision.Show(board != null ? board.Log : null, _traveller);
        }
        if (on && board != null)
            board.Release();
        MarkShelf();
        Refocus();
    }

    /// <summary>The findings or what is held changed: the decision redraws, and the findings column opens from its rail at the first finding (or folds back at a new case).</summary>
    private void BoardChanged()
    {
        if (_deciding && decision != null)
            decision.Show(board != null ? board.Log : null, _traveller);
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

    /// <summary>The sources the player sees: the showing panes' (none while the app is down or the decision shows).</summary>
    private IReadOnlyList<AppTab> ShownTabs()
    {
        _shown.Clear();
        if (IsShowing && !_deciding)
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

    /// <summary>A press on the desktop (DesktopWindowManager.Pressed): outside the search drawer's panel, the drawer closes; outside the Books menu (and its chip), the menu closes.</summary>
    private void Pressed(GameObject top)
    {
        PressedForSearch(top);
        if (shelf != null && shelf.BooksOpen && !shelf.IsPart(top))
            shelf.ShowBooks(false);
    }

    /// <summary>True while the shelf's Books menu is open (Escape closes it first, as a menu).</summary>
    public bool BooksMenuOpen => shelf != null && shelf.BooksOpen;

    /// <summary>Escape's CloseMenu for the Books menu.</summary>
    public void CloseBooksMenu()
    {
        if (shelf != null)
            shelf.ShowBooks(false);
    }

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
    /// the desktop shows only the apps introduced (Feature.App); a pane on
    /// the books before any is introduced turns to today's rules; set at the
    /// day's start.
    /// </summary>
    public void SetIntroductions(Introductions introductions, int day)
    {
        _introductions = introductions ?? Introductions.None;
        _day = day;
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

    /// <summary>Today's day number (SetIntroductions).</summary>
    private int _day;

    /// <summary>
    /// The shelf's documents from the left pane's views, in three groups:
    /// the traveller's (each paper the day issues by its chip's name, not
    /// readable yet: dimmed; the transcript), the agency's (Citizen records,
    /// today's rules, the calendar, each from the day it is introduced) and
    /// the books (each by its name, in the Books menu, from the day it is
    /// introduced: ReferenceView.OnShelf); drawn again only when they change.
    /// </summary>
    private void RefreshShelf()
    {
        if (shelf == null || leftPane == null)
            return;
        bool Introduced(string feature) => _introductions.Has(_day, feature);
        var items = new List<ShelfItem>();
        IAppView documents = leftPane.View(AppTab.Documents);
        if (documents != null && _traveller != null)
            for (int i = 0; i < documents.Chips.Count; i++)
                items.Add(new ShelfItem(ShelfGroup.Traveller, documents.Chips[i].Label, LinkTarget.ToTab(AppTab.Documents, i), documents.Chips[i].Available));
        if (_traveller != null && Hosts(AppTab.Transcript))
            items.Add(new ShelfItem(ShelfGroup.Traveller, UiText.Get("app.tab.transcript"), LinkTarget.ToTab(AppTab.Transcript), true));
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

        if (!SameItems(items))
        {
            _items.Clear();
            _items.AddRange(items);
            shelf.Show(_items);
        }
        MarkShelf();
    }

    /// <summary>True when <paramref name="items"/> are the shelf's already (the same names, places and readability).</summary>
    private bool SameItems(List<ShelfItem> items)
    {
        if (items.Count != _items.Count)
            return false;
        for (int i = 0; i < items.Count; i++)
            if (items[i].Label != _items[i].Label || !items[i].Target.Equals(_items[i].Target) || items[i].Available != _items[i].Available ||
                items[i].Group != _items[i].Group)
                return false;
        return true;
    }

    /// <summary>The chips' side tags (where each document is open), their dots (a paper or the transcript not opened yet this case, or something new in a source) and the Books chip's words.</summary>
    private void MarkShelf()
    {
        if (shelf == null || leftPane == null)
            return;
        shelf.Mark(SideOf, Unread, BooksLabel);
    }

    /// <summary>The Books chip's words: "Books", or "Books · Costume Guide" while that book is open on a side.</summary>
    private static string BooksLabel(string openBook) => openBook == null ? UiText.Get("shelf.books") : UiText.Format("shelf.booksOpen", openBook);

    /// <summary>"L" or "R" (the pane headers' "Left" and "Right", short so the shelf keeps one row) for the side a shelf document is open on, else null.</summary>
    private string SideOf(ShelfItem item)
    {
        if (_deciding)
            return null;
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
