using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The Investigation app as the Lens case screen (the PC Lens spec,
/// docs/superpowers/specs/2026-11-12-pc-lens-case-screen.md; before it the PC
/// workbench spec, lessons 1, 2, D5, D6, D10, the desk-first redesign, items 7
/// and 8): one desktop window, "Investigation", that covers the whole PC
/// desktop (the taskbar too) while it is open. It has no title bar: its
/// header holds the Home key (it minimises the window), the traveller at the
/// desk (the name and role, never what they ask for; or that the desk waits),
/// the findings chip (a click drops the findings list under it), the Inspect
/// key (Space) and the time. Under it are two sides (AppPane: the left and the
/// right, each with a slim tag strip; a click on a strip selects the side, F6
/// too; the selected side is where the next paper opens), each holding one
/// document (LensSides, the pure place rule; InvestigationApp.Lens applies it:
/// a side with no document shows the empty state). The dock under the sides
/// (LensDock) lists the case's and the day's documents (the traveller's papers,
/// the transcript and the papers not handed over, to flag missing; then the
/// Registry, the rules, the calendar and each reference book), each shown from
/// the day it is introduced; a click on one opens it on the selected side, a
/// drag drops it on a side. The workbench (MatchBoard) draws the dashed line
/// between two compared values over the sides and says the answer on the
/// answer plate between the sides and the dock. A value is picked only while
/// inspect is on (PcInspect). The guided steps are gone: the PC does not choose
/// the documents for the clerk, and the verdict is the stamp on the passport at
/// the desk. The keys, the focus ring, copy and paste and zoom are in
/// InvestigationApp.Keys; the inspect mode, the sides and the drags in
/// InvestigationApp.Lens. Nothing steals the view: something new for a source
/// dots its dock item until it is seen (AppBadges), and dots the desktop's
/// Investigation icon while the app is closed or minimised; a scan
/// (ScanArrival) opens the app only when it is closed, puts the paper on the
/// selected side only when that side is empty, and toasts ("... scanned", Open
/// shows it). A new case takes the last traveller's papers off the sides and
/// clears the dots; once the traveller is decided at the desk the case sources
/// show the no-case state. InvestigationUIController drives it.
/// </summary>
public sealed partial class InvestigationApp : MonoBehaviour
{
    /// <summary>The app's window (maximised on the first open: the whole desktop).</summary>
    [SerializeField] private DesktopWindow window;

    /// <summary>The left side.</summary>
    [SerializeField] private AppPane leftPane;

    /// <summary>The right side.</summary>
    [SerializeField] private AppPane rightPane;

    [Header("The workbench")]
    /// <summary>The guided steps (headless: the PC does not put documents up for the clerk).</summary>
    [SerializeField] private GuideBar guide;

    /// <summary>The dock: the documents in one row (the Lens screen).</summary>
    [SerializeField] private LensDock menus;

    /// <summary>The click-and-match, the findings and the answer plate.</summary>
    [SerializeField] private MatchBoard board;

    /// <summary>The notice (a paper scanned, a copy, "Press Space to inspect."): it shows while the app does.</summary>
    [SerializeField] private AppToast toast;

    [Header("The header")]
    /// <summary>The traveller at the desk (the name plate), or that the desk waits.</summary>
    [SerializeField] private TMP_Text travellerText;

    /// <summary>Home: minimises the window.</summary>
    [SerializeField] private Button homeButton;

    /// <summary>The findings chip ("No findings", "1 finding", "3 findings"): a click opens or closes the findings list.</summary>
    [SerializeField] private Button findingsChip;

    /// <summary>The chip's text.</summary>
    [SerializeField] private TMP_Text findingsChipText;

    /// <summary>The findings list under the chip (closed until the chip is clicked).</summary>
    [SerializeField] private RectTransform findingsColumn;

    /// <summary>The Inspect key.</summary>
    [SerializeField] private Button inspectButton;

    /// <summary>The Inspect key's filled face (shown while inspect is on).</summary>
    [SerializeField] private GameObject inspectOn;

    [Header("Desktop")]

    /// <summary>The desktop's knobs (the toast's time).</summary>
    [SerializeField] private DesktopConfigSO config;

    /// <summary>The desktop's icons (the Investigation icon's dot).</summary>
    [SerializeField] private DesktopIcons icons;

    private readonly AppBadges _badges = new AppBadges();
    private readonly List<AppTab> _shown = new List<AppTab>(2);
    private readonly HashSet<string> _opened = new HashSet<string>();
    private readonly List<ShelfItem> _items = new List<ShelfItem>();
    private readonly List<AppRow> _rowScratch = new List<AppRow>();
    private CasePapers _papers;
    private DesktopWindowManager _manager;
    private string _traveller;
    private bool _ready;

    /// <summary>True while the app shows (open and not minimised).</summary>
    public bool IsShowing => window != null && window.gameObject.activeSelf;

    /// <summary>True when the sides host a view for the source.</summary>
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

    /// <summary>The app shows (opened or restored): the sides are drawn, the shown documents are seen, and the icon's dot goes.</summary>
    private void OnEnable()
    {
        if (leftPane == null)
            return;
        Init();
        ApplySides();
        MarkShelf();
        if (icons != null)
            icons.SetBadge(DesktopAppIds.Investigation, 0);
    }

    /// <summary>The app goes (the window closed or minimised): inspect turns off, with what is held.</summary>
    private void OnDisable()
    {
        if (_ready)
            SetInspect(false);
    }

    private void OnDestroy()
    {
        if (_manager != null)
            _manager.Pressed -= Pressed;
    }

    /// <summary>
    /// A traveller is presented: the header names who stands at the desk
    /// (never what they ask for), the last traveller's papers leave the sides,
    /// the dots and the icon's dot are cleared, inspect is off, the toast gone;
    /// the façade then starts the workbench (MatchBoard.BeginCase) and the
    /// steps (GuideBar.BeginCase; nothing is put up).
    /// </summary>
    public void BeginCase(string travellerName)
    {
        Init();
        ResetSearchCase();
        CancelDrag();
        _traveller = travellerName ?? string.Empty;
        ShowWho();
        _badges.Clear();
        _opened.Clear();
        foreach (AppPane pane in Panes())
        {
            pane.SetCase(true);
            pane.DropCaseHistory();
        }
        SetInspect(false);
        _sides.RemoveWhere(t => TabOrder.IsCaseSource(t.Tab));
        ApplySides();
        if (icons != null)
            icons.SetBadge(DesktopAppIds.Investigation, 0);
        if (toast != null)
            toast.Hide();
        RefreshShelf();
        KeysBeginCase(travellerName);
    }

    /// <summary>The traveller was decided at the desk: the case's papers leave the sides, the case sources show the no-case state; the header waits for the next traveller; search forgets the case; the workbench empties.</summary>
    public void EndCase()
    {
        Init();
        CancelDrag();
        _papers = null;
        _traveller = null;
        ResetSearchCase();
        _missing = MissingPapers.None;
        _missingPapers = null;
        SetInspect(false);
        foreach (AppPane pane in Panes())
            pane.SetCase(false);
        _sides.RemoveWhere(t => TabOrder.IsCaseSource(t.Tab));
        ApplySides();
        if (toast != null)
            toast.Hide();
        if (board != null)
            board.EndCase();
        ShowWho();
        RefreshShelf();
        KeysEndCase();
    }

    /// <summary>Remembers the case's papers (kept: SmartLinks.ForKey reads them for a finding's place).</summary>
    public void SetCounters(CasePapers papers)
    {
        _papers = papers;
    }

    /// <summary>The header's traveller line: who is at the desk ("<b>Yichen (Soldier)</b>"), else "Waiting for the next traveller".</summary>
    private void ShowWho()
    {
        if (travellerText != null)
            travellerText.text = _traveller == null ? UiText.Pc.Get("idle.waiting") : UiText.Pc.Format("app.atDesk.name", _traveller);
    }

    /// <summary>Something new for the source (a transcript line, a logged deviation): its dock item dotted unless a side shows it; the icon dotted while the app is down.</summary>
    public void Arrived(AppTab tab)
    {
        Init();
        _badges.Arrived(tab, ShownTabs());
        MarkShelf();
        if (!IsShowing && icons != null)
            icons.SetBadge(DesktopAppIds.Investigation, IconBadge.Dot);
    }

    /// <summary>Paper <paramref name="paper"/> was scanned (ScanArrival): the app opens when closed, the paper goes on the selected side only when that side is empty, Papers dotted unless seen, a toast names it.</summary>
    public void Scanned(int paper, string paperName)
    {
        Init();
        bool open = window != null && window.IsOpen, minimised = window != null && window.IsMinimised;
        ScanArrival arrival = ScanArrival.Decide(open, minimised, ShownTabs(), !_sides[_sides.Selected].IsNone);
        if (arrival.OpenApp)
            OpenWindow();
        if (arrival.ShowPaper)
        {
            LensSide at = _sides.Selected; // the paper goes on the empty selected side; a minimised app stays down
            _sides.Put(LinkTarget.ToTab(AppTab.Documents, paper));
            ApplySides(at);
        }
        if (arrival.BadgeDocuments)
            Arrived(AppTab.Documents);
        if (arrival.Toast && toast != null)
            toast.Show(UiText.Pc.Format("app.toast.scanned", paperName), config != null ? config.toastSeconds : 4f,
                () => OpenOnTarget(LinkTarget.ToTab(AppTab.Documents, paper)));
    }

    /// <summary>Opens the app (a minimised one restores) on the source on the selected side: Mail's directive memo shows today's rules.</summary>
    public void ShowTab(AppTab tab) => OpenOnTarget(LinkTarget.ToTab(tab));

    /// <summary>
    /// Opens the app (a minimised one restores) at <paramref name="target"/>
    /// on the selected side (a dock item, the toast, Mail); the document open
    /// on the other side leaves it, so one document is never open twice
    /// (LensSides.Put). False when the target is gone (its view says so; the
    /// source shows all the same).
    /// </summary>
    public bool OpenOnTarget(LinkTarget target)
    {
        Init();
        if (target.IsNone)
            return false;
        OpenWindow();
        LensSide at = _sides.Selected;
        _sides.Put(Carry(target));
        return ApplySides(at);
    }

    /// <summary>Puts <paramref name="left"/> up on the left and <paramref name="right"/> on the right (a finding revisited); the right side is selected afterwards. It opens no window.</summary>
    public void OpenPair(LinkTarget left, LinkTarget right)
    {
        Init();
        if (LensSides.Same(left, right)) // both picks on one paper: it goes on the selected side only, the other side keeps its paper
        {
            LensSide at = _sides.Selected;
            _sides.Place(left, at);
            ApplySides(at);
            return;
        }
        if (!left.IsNone)
            _sides.Place(left, LensSide.Left);
        if (!right.IsNone)
            _sides.Place(right, LensSide.Right);
        _sides.Select(LensSide.Right);
        ApplySides(left.IsNone ? (LensSide?)null : LensSide.Left, right.IsNone ? (LensSide?)null : LensSide.Right);
    }

    /// <summary>Where a pick was picked (SmartLinks.ForKey over the case's papers).</summary>
    public LinkTarget LinkFor(string pickKey) => SmartLinks.ForKey(pickKey, _papers);

    /// <summary>The box of the row with <paramref name="key"/> in a showing side's view (the workbench's line), or null; <paramref name="scratch"/> is reused.</summary>
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

    /// <summary>Wires the sides, the steps, the dock, the header and the desktop's press (once; the app is driven while its window is closed).</summary>
    private void Init()
    {
        if (_ready)
            return;
        _ready = true;

        foreach (AppPane pane in Panes())
        {
            pane.Shown += PaneShown;
            pane.Changed += PaneChanged;
            pane.HeaderClicked += PaneClicked;
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
            menus.Dragging += DockDragging;
            menus.Dropped += DockDropped;
        }
        if (guide != null)
            guide.StageShown += StageShown;
        if (board != null)
            board.Changed += BoardChanged;
        if (homeButton != null)
            homeButton.onClick.AddListener(Home);
        if (inspectButton != null)
            inspectButton.onClick.AddListener(ToggleInspect);
        if (findingsChip != null)
            findingsChip.onClick.AddListener(() => ApplyFindings(!_findingsShown));

        _manager = window != null ? window.Manager : null;
        if (_manager != null)
            _manager.Pressed += Pressed;
        InitKeys();
        InitSearch();
        InitLens();
        RefreshShelf();
        ShowWho();
    }

    /// <summary>Home: the window is minimised and the desktop shows again.</summary>
    public void Home()
    {
        if (window != null)
            window.Minimise();
    }

    /// <summary>A step was gone to: nothing is put up. The PC does not choose the documents for the clerk (the Lens screen).</summary>
    private void StageShown(GuideStage stage)
    {
    }

    /// <summary>The findings or what is held changed: the findings chip counts them.</summary>
    private void BoardChanged() => RefreshChip();

    /// <summary>Both sides (each keeps its case state and its views current).</summary>
    private IEnumerable<AppPane> Panes()
    {
        if (leftPane != null)
            yield return leftPane;
        if (rightPane != null)
            yield return rightPane;
    }

    /// <summary>The sides that show: both (the Lens screen has no one-pane mode).</summary>
    private IEnumerable<AppPane> ShowingPanes() => Panes();

    /// <summary>The sources the player sees: those of the sides that hold a document (none while the app is down).</summary>
    private IReadOnlyList<AppTab> ShownTabs()
    {
        _shown.Clear();
        if (IsShowing)
            foreach (LensSide side in Sides)
                if (!_sides[side].IsNone)
                    _shown.Add(_sides[side].Tab);
        return _shown;
    }

    /// <summary>A press on the desktop (DesktopWindowManager.Pressed): outside the search drawer's panel, the drawer closes; outside the findings list and its chip, the list closes.</summary>
    private void Pressed(GameObject top)
    {
        PressedForSearch(top);
        if (_findingsShown && !InFindings(top))
            ApplyFindings(false);
    }

    /// <summary>True when <paramref name="pressed"/> is the findings list or its chip (a press elsewhere closes the list).</summary>
    private bool InFindings(GameObject pressed)
    {
        for (Transform t = pressed != null ? pressed.transform : null; t != null; t = t.parent)
            if ((findingsColumn != null && t == findingsColumn) || (findingsChip != null && t == findingsChip.transform))
                return true;
        return false;
    }

    /// <summary>True while the findings list is open (Escape closes it first, as a menu).</summary>
    public bool MenuOpen => _findingsShown;

    /// <summary>Escape's CloseMenu for the open findings list.</summary>
    public void CloseMenu() => ApplyFindings(false);

    /// <summary>Raised when the Papers menu flags a paper missing (its request id; the facade unlocks its request on the wheel).</summary>
    public event System.Action<string> MissingFlagged;

    /// <summary>The papers the clerk can flag missing for the traveller at the desk.</summary>
    private MissingPapers _missing = MissingPapers.None;

    /// <summary>Where each of the traveller's papers is (what is still not handed over).</summary>
    private CasePapers _missingPapers;

    /// <summary>The papers to flag missing (<paramref name="missing"/>, those not handed over by <paramref name="papers"/>) and their flags: the dock redraws.</summary>
    public void SetMissing(MissingPapers missing, CasePapers papers)
    {
        _missing = missing ?? MissingPapers.None;
        _missingPapers = papers;
        RefreshShelf();
    }

    /// <summary>A side showed a source: it is seen when the app shows.</summary>
    private void PaneShown(AppPane pane, AppTab tab)
    {
        if (IsShowing)
            _badges.Seen(tab);
    }

    /// <summary>A side's document changed: it is opened this case, and the dock marks the sides.</summary>
    private void PaneChanged(AppPane pane)
    {
        if (!_ready)
            return;
        (AppTab tab, int item) = pane.Document;
        if (!pane.Blocked)
            _opened.Add(DocKey(tab, item));
        AdoptDrags();
        MarkShelf();
    }

    /// <summary>
    /// The day's introductions (the desk-first redesign, items 3 and 9): the
    /// dock lists only the agency documents introduced (Citizen records,
    /// today's rules, the calendar: Feature.Records, Rulebook, Calendar) and
    /// the desktop shows only the apps introduced (Feature.App), this app
    /// itself opening only once it is (OnPc); a side left on the
    /// books before any is introduced is emptied; set at the
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
        // A side left on a book no day has introduced yet is emptied (through the sides, so they agree with the panes).
        if (!_items.Exists(x => x.Group == ShelfGroup.Books))
        {
            _sides.RemoveWhere(t => t.Tab == AppTab.Reference);
            ApplySides();
        }
    }

    /// <summary>What the day has introduced (SetIntroductions).</summary>
    private Introductions _introductions = Introductions.None;

    /// <summary>
    /// True while the app is on the PC: from the day its key is introduced
    /// (Feature.App(DesktopAppIds.Investigation), day 5 with the scanner:
    /// Saleh 2026-10-06, "hide the application if it is not used and unlock
    /// it when the scanner is available or it is needed"); true until a day
    /// says (SetIntroductions). While false nothing opens the app: no icon
    /// (DesktopIcons.ShowApps), no link (Mail's Rules link hides), no dock
    /// jump.
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
    /// The dock's items from the left side's views, in three groups: the
    /// traveller's (each paper handed over, by its name, not readable yet:
    /// dimmed; the transcript; then the papers not handed over, to flag
    /// missing), the agency's (Citizen records, today's rules and the calendar,
    /// each from the day it is introduced) and the books (each by its name,
    /// from the day it is introduced: ReferenceView.OnShelf); drawn again only
    /// when they change.
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
                if (_missingPapers == null || _missingPapers.State(i) != PaperState.NotHandedOver) // a paper not handed over is shown by its request (it tells nothing of what they carry)
                    items.Add(new ShelfItem(ShelfGroup.Traveller, documents.Chips[i].Label, LinkTarget.ToTab(AppTab.Documents, i), documents.Chips[i].Available));
        if (_traveller != null && Hosts(AppTab.Transcript))
            items.Add(new ShelfItem(ShelfGroup.Traveller, UiText.Pc.Get("app.tab.transcript"), LinkTarget.ToTab(AppTab.Transcript), true));
        if (_traveller != null)
            foreach (FormRequest request in _missing.Open(_missingPapers))
                items.Add(ShelfItem.MissingPaper(request.Id, request.Label, _missing.State(request.Id)));
        if (Hosts(AppTab.Records) && Introduced(Feature.Records))
            items.Add(new ShelfItem(ShelfGroup.Agency, UiText.Pc.Get("app.tab.records"), LinkTarget.ToTab(AppTab.Records), true));
        if (Hosts(AppTab.Rules) && Introduced(Feature.Rulebook))
            items.Add(new ShelfItem(ShelfGroup.Agency, UiText.Pc.Get("app.tab.rules"), LinkTarget.ToTab(AppTab.Rules), true));
        if (Hosts(AppTab.Calendar) && Introduced(Feature.Calendar))
            items.Add(new ShelfItem(ShelfGroup.Agency, UiText.Pc.Get("app.tab.calendar"), LinkTarget.ToTab(AppTab.Calendar), true));
        IAppView books = leftPane.View(AppTab.Reference);
        for (int i = 0; books != null && i < books.Chips.Count; i++)
            if (!(books is ReferenceView reference) || reference.OnShelf(i))
                items.Add(new ShelfItem(ShelfGroup.Books, books.Chips[i].Label, LinkTarget.ToTab(AppTab.Reference, i), books.Chips[i].Available));

        if (!SameItems(items))
        {
            _items.Clear();
            _items.AddRange(items);
            CancelDrag(); // the items a drag started on are replaced
            menus.Show(_items);
        }
        MarkShelf();
    }

    /// <summary>True when <paramref name="items"/> are the dock's items already (ShelfItem.Same, in order).</summary>
    private bool SameItems(List<ShelfItem> items)
    {
        if (items.Count != _items.Count)
            return false;
        for (int i = 0; i < items.Count; i++)
            if (!items[i].Same(_items[i]))
                return false;
        return true;
    }

    /// <summary>The dock's side tags (where each document is open) and dots (the transcript with a new line, or something new in a source).</summary>
    private void MarkShelf()
    {
        if (menus == null || leftPane == null)
            return;
        menus.Mark(SideOf, Unread);
    }

    /// <summary>"L" or "R" (short so the dock keeps one row) for the side a dock document is open on, else null.</summary>
    private string SideOf(ShelfItem item)
    {
        if (item.Action != ShelfAction.Open)
            return null;
        if (LensSides.Same(_sides.Left, item.Target))
            return UiText.Pc.Get("shelf.side.left");
        return LensSides.Same(_sides.Right, item.Target) ? UiText.Pc.Get("shelf.side.right") : null;
    }

    /// <summary>True for a source with something new (a transcript line). A paper not opened yet has no dot: the clerk decides what to read.</summary>
    private bool Unread(ShelfItem item)
    {
        if (!item.Available || _traveller == null)
            return false;
        return item.Target.Item < 0 && _badges.IsBadged(item.Target.Tab);
    }

    /// <summary>A document's key for the opened set ("Documents:1", "Records:-1").</summary>
    private static string DocKey(AppTab tab, int item) => tab + ":" + (item < 0 ? -1 : item);
}
