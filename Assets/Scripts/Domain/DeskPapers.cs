using System;
using System.Collections.Generic;

/// <summary>When the traveller hands a document over. Serialized on DocumentTemplateSO: append only.</summary>
public enum DocumentHandOver
{
    /// <summary>When the desk asks for it (a "Request" choice on the traveller wheel). 0, so existing templates read as OnRequest.</summary>
    OnRequest,

    /// <summary>When the traveller steps up to the desk.</summary>
    OnArrival,

    /// <summary>With the arrival papers, unless the traveller forgets it (Saleh 2026-10-17: "they should forget to give it to you some time"; a seeded chance, their personality's forgetPaper): forgotten, the desk asks for it.</summary>
    Forgettable
}

/// <summary>The one rule over DocumentHandOver.</summary>
public static class DocumentHandOvers
{
    /// <summary>True when a document with this hand-over may wait for the desk to ask (it then gets a request on the papers menu and counts toward the hub's size): on request, or a paper a traveller can forget.</summary>
    public static bool IsRequested(DocumentHandOver handOver) => handOver == DocumentHandOver.OnRequest || handOver == DocumentHandOver.Forgettable;

    /// <summary>One traveller's hand-over of a paper whose template says <paramref name="handOver"/>: a forgettable paper comes with the arrival papers, or on request when they <paramref name="forgot"/> it; any other as its template says.</summary>
    public static DocumentHandOver ForTraveller(DocumentHandOver handOver, bool forgot) =>
        handOver != DocumentHandOver.Forgettable ? handOver : forgot ? DocumentHandOver.OnRequest : DocumentHandOver.OnArrival;

    /// <summary>
    /// Whether a traveller forgets their forgettable papers: one draw of
    /// <paramref name="rng"/> (their own stream, Seeds.ForForgetting) against
    /// <paramref name="chance"/> (their personality's forgetPaper), made
    /// whatever they carry, so it never depends on the papers.
    /// </summary>
    public static bool Forgets(float chance, IRandomSource rng)
    {
        float u = rng != null ? rng.Value() : 1f;
        return u < chance;
    }
}

/// <summary>One of the current traveller's documents, as the desk and the interview see it.</summary>
public sealed class CaseDocument
{
    /// <summary>The document's display name ("Travel Passport"), in the reading language: the label a view shows on its own.</summary>
    public string name;

    /// <summary>The document's English name (its template's display name), or blank: a content sentence names the paper in English, as its words are (Saleh 2026-10-16: only labels translate).</summary>
    public string englishName;

    /// <summary>The name a content sentence ("Your {document} says {value}.") says: the English name, else the display name.</summary>
    public string SentenceName => string.IsNullOrEmpty(englishName) ? name : englishName;

    /// <summary>The document's fields: the boxes its paper's form shows.</summary>
    public IReadOnlyList<DocumentField> fields;

    /// <summary>When this traveller hands it over (DocumentHandOvers.ForTraveller: a forgettable paper on arrival, or on request when forgotten).</summary>
    public DocumentHandOver handOver;

    /// <summary>True when the paper carries the traveller's photo (DocumentTemplateSO.showsPhoto).</summary>
    public bool showsPhoto;

    /// <summary>The agency form number ("TC-310"; DocumentTemplateSO.formNumber): a request names it.</summary>
    public string formNumber;

    /// <summary>The request group it belongs to (DocumentTemplateSO.askGroup), or blank: a group's request hands over the one form of the group the traveller carries.</summary>
    public string askGroup;

    /// <summary>True when the document is handed over only on request (it then gets a hub request).</summary>
    public bool Requested => DocumentHandOvers.IsRequested(handOver);
}

/// <summary>Rules over one traveller's documents.</summary>
public static class CaseDocuments
{
    /// <summary>The documents handed over on arrival, in paper order: the papers the desk receives at presentation, or the windows that open then where no desk is wired. Null entries are skipped; a null list gives none.</summary>
    public static IReadOnlyList<int> ArrivalIndices(IReadOnlyList<CaseDocument> documents)
    {
        var arrivals = new List<int>();
        int count = documents != null ? documents.Count : 0;
        for (int i = 0; i < count; i++)
            if (documents[i] != null && !documents[i].Requested)
                arrivals.Add(i);
        return arrivals;
    }
}

/// <summary>What happens to a document released on the desk: it stays where it was dropped, it starts scanning, it goes back to where it was picked up, the papers are handed back (a paper dropped on the counter once the passport carries its verdict), or it bounces back to the desk (dropped on the counter before that). Not serialized.</summary>
public enum DropOutcome
{
    /// <summary>It stays where it was dropped (not over the scanner), in the zone it was dropped in.</summary>
    Stays,

    /// <summary>It starts scanning (over the idle scanner).</summary>
    Scanning,

    /// <summary>The paper could not be dropped (it is not on the desk, or out of range): nothing changes.</summary>
    Refused,

    /// <summary>A paper was dropped on the counter once the passport carries its verdict: the papers go back to the traveller with it (Papers, Please's hand-back).</summary>
    HandsBack,

    /// <summary>A paper from the desk was dropped on the counter before the passport carries a verdict: it goes back to where it was picked up, on the desk, with the note "Stamp the passport first" (Saleh 2026-10-06: "documents can only be returned after stamping").</summary>
    NotStamped,

    /// <summary>It was dropped on the busy scanner: it waits its turn beside the glass, on the desk (it takes the desk's zone), and feeds through by itself (drop and go, the scanner app spec §1: "a stack feeds through one paper at a time").</summary>
    Queued,

    /// <summary>A machine under it would not take it (the chip reader: a paper with no chip, or the reader busy; MachineIntake): it shakes in place and lies on the desk just beside the machine, in the desk's zone. The desk decides it (DeskPapers.Drop never returns it).</summary>
    MachineRefused
}

/// <summary>The desk's two zones (Papers, Please's, Saleh 2026-10-06): where a document lies decides its size, one world size per zone whatever the view (DeskZones.SheetScale; Saleh's D1).</summary>
public enum DeskZone
{
    /// <summary>The counter: the strip on the traveller's side, where papers arrive and go back; a document there is small (DeskConfigSO.counterScale).</summary>
    Counter,

    /// <summary>The desk: the reading area; a document there is full size (its reading size), in the office view and the reading view alike.</summary>
    Desk
}

/// <summary>
/// One case's papers: each is handed over once (from the traveller onto the
/// counter), can be dragged while on the desk, and is scanned by dropping it
/// on the scanner, one scan at a time, for a fixed duration; at the decision
/// every paper goes back and a running scan is cancelled. Each paper lies in
/// a zone (Papers, Please's counter and desk, Saleh 2026-10-06): it arrives on
/// the counter and takes the zone it is dropped in, except that a paper from
/// the desk goes back onto the counter only to hand the papers back: once the
/// passport carries its verdict any paper dropped there hands them back
/// (HandsBack); before that it bounces back to the desk (NotStamped:
/// "documents can only be returned after stamping", Saleh 2026-10-06). With the
/// Auto-Feed Scanner (the PC redesign SC3) handed-over papers join a queue
/// and scan themselves in hand-over order, one at a time (FeedNext), an
/// unready paper skipped until it lies still; a scan by hand (a drop) takes
/// its own duration (the Analysis Scanner's pass, SC4). Pure, so every
/// state and outcome is tested headless; DeskController animates them.
/// </summary>
public sealed class DeskPapers
{
    /// <summary>The shortest scan (a shorter duration is raised to it).</summary>
    private const float MinScanSeconds = 0.01f;

    /// <summary>Where a paper is.</summary>
    private enum PaperState
    {
        /// <summary>Not handed over yet.</summary>
        WithTraveller,

        /// <summary>On the desk (the counter or the reading area), draggable.</summary>
        OnDesk,

        /// <summary>On the scanner's bed, being scanned.</summary>
        Scanning,

        /// <summary>Given back at the decision.</summary>
        Returned
    }

    private readonly PaperState[] _states;

    /// <summary>Each paper's zone (the counter until it is dropped on the desk).</summary>
    private readonly DeskZone[] _zones;

    /// <summary>Seconds a scan the scanner feeds itself takes.</summary>
    private readonly float _scanSeconds;

    /// <summary>Seconds the Analysis Scanner's pass takes.</summary>
    private readonly float _analysisSeconds;

    /// <summary>The day's scanner upgrades (which pass a scan takes: ScannerDay.PassFor).</summary>
    private readonly ScannerDay _scanners;

    /// <summary>Each paper whose analysis pass has ended (the pass works once per document).</summary>
    private readonly bool[] _analysed;

    /// <summary>Each paper scanned at least once this case (the Auto-Feed's stack skips it).</summary>
    private readonly bool[] _scanned;

    /// <summary>The papers in the order they were handed over (the Auto-Feed's stack goes through in it).</summary>
    private readonly List<int> _handed = new List<int>();

    /// <summary>The running scan's duration.</summary>
    private float _running;

    /// <summary>The Auto-Feed queue: the papers waiting to scan themselves, in hand-over order.</summary>
    private readonly List<int> _queue = new List<int>();

    /// <summary>The queued papers dropped on the scanner (drop and go): they scan by hand when their turn comes.</summary>
    private readonly HashSet<int> _dropped = new HashSet<int>();

    /// <summary>The zone each paper dropped on the busy scanner comes back to once scanned (the desk carries it back to where it was picked up), while it waits beside the scanner on the desk; forgotten when it is lifted again (Lift: its pick-up position moves) or dropped elsewhere (LeaveFeed).</summary>
    private readonly Dictionary<int, DeskZone> _comesBackTo = new Dictionary<int, DeskZone>();

    /// <summary>The paper being scanned, or -1.</summary>
    private int _scanning = -1;

    /// <summary>Seconds the running scan has taken.</summary>
    private float _elapsed;

    /// <summary>Every paper starts with the traveller; a null list means no papers; a scan shorter than 0.01 s is raised to it; no scanner upgrade.</summary>
    public DeskPapers(IReadOnlyList<CaseDocument> documents, float scanSeconds) : this(documents, scanSeconds, scanSeconds, default)
    {
    }

    /// <summary>Every paper starts with the traveller; a null list means no papers; a plain scan takes <paramref name="scanSeconds"/> and the analysis pass <paramref name="analysisSeconds"/> (each raised to 0.01 s when shorter); <paramref name="scanners"/> are the day's upgrades (which pass a scan takes).</summary>
    public DeskPapers(IReadOnlyList<CaseDocument> documents, float scanSeconds, float analysisSeconds, ScannerDay scanners)
    {
        _states = new PaperState[documents != null ? documents.Count : 0];
        _zones = new DeskZone[_states.Length];
        _analysed = new bool[_states.Length];
        _scanned = new bool[_states.Length];
        ArrivalIndices = CaseDocuments.ArrivalIndices(documents);
        _scanSeconds = scanSeconds > MinScanSeconds ? scanSeconds : MinScanSeconds;
        _analysisSeconds = analysisSeconds > MinScanSeconds ? analysisSeconds : MinScanSeconds;
        _scanners = scanners;
    }

    /// <summary>How many papers the case has.</summary>
    public int Count => _states.Length;

    /// <summary>True while a scan runs.</summary>
    public bool ScannerBusy => _scanning >= 0;

    /// <summary>The paper on the scanner's bed (-1: none): the desk holds its scan while it is still on its way onto the bed.</summary>
    public int Scanning => _scanning;

    /// <summary>The running scan's pass (ScannerDay.PassFor: the analysis pass for a paper dropped on the scanner with the Analysis Scanner, once per document); Plain while the scanner fed itself or is idle.</summary>
    public ScanPass Pass { get; private set; }

    /// <summary>True when paper <paramref name="i"/>'s analysis pass has ended (a re-scan of it is AlreadyAnalysed).</summary>
    public bool WasAnalysed(int i) => InRange(i) && _analysed[i];

    /// <summary>True once paper <paramref name="i"/> has been scanned this case.</summary>
    public bool WasScanned(int i) => InRange(i) && _scanned[i];

    /// <summary>The papers waiting to scan (the Auto-Feed's and those dropped on the busy scanner).</summary>
    public int QueuedCount => _queue.Count;

    /// <summary>How far the running scan is, 0 to 1 (0 while idle): the sweep's place on the glass.</summary>
    public float Progress => ScannerBusy && _running > 0f ? Math.Min(1f, _elapsed / _running) : 0f;

    /// <summary>True when paper <paramref name="i"/> waits in the feed.</summary>
    public bool IsQueued(int i) => _queue.Contains(i);

    /// <summary>The papers handed over on arrival, in paper order (CaseDocuments.ArrivalIndices).</summary>
    public IReadOnlyList<int> ArrivalIndices { get; }

    /// <summary>Papers on the desk, the one being scanned included.</summary>
    public int OnDeskCount
    {
        get
        {
            int n = 0;
            for (int i = 0; i < _states.Length; i++)
                if (StateOf(i) == PaperState.OnDesk || StateOf(i) == PaperState.Scanning)
                    n++;
            return n;
        }
    }

    /// <summary>The zone paper <paramref name="i"/> lies in (the counter until it is dropped on the desk; the counter out of range).</summary>
    public DeskZone ZoneOf(int i) => InRange(i) ? _zones[i] : DeskZone.Counter;

    /// <summary>Hands a paper over from the traveller onto the counter; false for any other state or an index out of range.</summary>
    public bool HandOver(int i)
    {
        if (!InRange(i) || StateOf(i) != PaperState.WithTraveller)
            return false;

        _states[i] = PaperState.OnDesk;
        _zones[i] = DeskZone.Counter;
        _handed.Add(i);
        return true;
    }

    /// <summary>
    /// Queues a handed-over paper to scan itself (the Auto-Feed Scanner's
    /// stack, FeedStack); false when it is not on the desk, is already
    /// queued, or is out of range.
    /// </summary>
    public bool Enqueue(int i)
    {
        if (!CanDrag(i) || _queue.Contains(i))
            return false;

        _queue.Add(i);
        return true;
    }

    /// <summary>
    /// The Auto-Feed tray takes the stack (Saleh 2026-10-18, demo 1018a note
    /// 14: "feed a stack in one go"; the desk machines spec §10): every paper
    /// handed over, on the desk and never scanned this case, not queued
    /// yet, joins the feed in hand-over order, to scan itself one after
    /// another (FeedNext). Returns the papers queued, in that order (none:
    /// nothing left to scan).
    /// </summary>
    public List<int> FeedStack()
    {
        var fed = new List<int>();
        foreach (int i in _handed)
            if (!_scanned[i] && Enqueue(i))
                fed.Add(i);
        return fed;
    }

    /// <summary>
    /// Feeds the scanner its next paper while it is idle: the first queued
    /// paper that lies on the desk and that <paramref name="ready"/> admits
    /// (null: every paper; the controller: landed and not being dragged)
    /// leaves the queue and starts scanning, not by hand, and its index is
    /// returned; -1 while the scanner is busy or no queued paper is ready. A
    /// paper skipped keeps its place for the next turn.
    /// </summary>
    public int FeedNext(Func<int, bool> ready)
    {
        if (ScannerBusy)
            return -1;

        foreach (int i in _queue)
        {
            if (StateOf(i) != PaperState.OnDesk || (ready != null && !ready(i)))
                continue;

            BeginScan(i, _dropped.Contains(i));
            return i;
        }
        return -1;
    }

    /// <summary>True while the paper is on the desk (not scanning); false out of range.</summary>
    public bool CanDrag(int i) => InRange(i) && StateOf(i) == PaperState.OnDesk;

    /// <summary>
    /// Paper <paramref name="i"/>, on the desk, comes to lie in
    /// <paramref name="zone"/> where the desk laid it after a drop it decided
    /// itself (a machine that refused it: it rests on the desk beside the
    /// machine, PaperSizes.ZoneAfterDrop's MachineRefused): its zone, and so
    /// its size, is the zone it lies in. False (nothing changes) for a paper
    /// not on the desk or out of range.
    /// </summary>
    public bool Lay(int i, DeskZone zone)
    {
        if (!CanDrag(i))
            return false;
        _zones[i] = zone;
        return true;
    }

    /// <summary>
    /// A drag begins on paper <paramref name="i"/> (DeskController's
    /// HandleDragBegan): the desk's pick-up position becomes where it lies
    /// now, so that is where a scan carries it back to. A paper waiting beside
    /// the busy scanner therefore forgets the zone it was first picked up in;
    /// scanned, it comes back to where it was lifted from last, in the zone it
    /// lies in there (the desk's, beside the scanner). It keeps its place in
    /// the feed: a cancelled drag lays it back where it waited, and only a
    /// drop changes the feed. False (nothing changes) for a paper not on the
    /// desk or out of range.
    /// </summary>
    public bool Lift(int i)
    {
        if (!CanDrag(i))
            return false;
        _comesBackTo.Remove(i);
        return true;
    }

    /// <summary>
    /// Paper <paramref name="i"/> was dropped off the scanner somewhere
    /// DeskPapers does not decide (the chip reader, which reads it or refuses
    /// it: DeskController): as with any drop off the scanner (Drop), a paper
    /// waiting its turn by hand leaves the feed (an Auto-Fed paper keeps its
    /// place) and no scan carries it back anywhere. False (nothing changes)
    /// for a paper not on the desk or out of range.
    /// </summary>
    public bool LeaveFeed(int i)
    {
        if (!CanDrag(i))
            return false;
        if (_dropped.Remove(i))
            _queue.Remove(i);
        _comesBackTo.Remove(i);
        return true;
    }

    /// <summary>
    /// Decides a released paper: a paper that is not on the desk (or an index
    /// out of range) is Refused and nothing changes; over the scanner it
    /// starts scanning by hand while the scanner is idle (a queued paper
    /// leaves the queue) and, while it is busy, joins the feed to scan by hand
    /// at its turn (Queued: drop and go, the scanner app spec §1; it waits
    /// beside the scanner, on the desk, so it takes the desk's zone); off the
    /// scanner a waiting paper leaves the feed (LeaveFeed); on the counter, once
    /// the passport carries its verdict (<paramref name="verdict"/>), any
    /// paper hands the papers back (HandsBack: the controller then ends the
    /// case), and before that a paper from the desk bounces back to it
    /// (NotStamped: its zone unchanged) while one still on the counter stays
    /// there; elsewhere it lies in <paramref name="zone"/> (Stays). A scanned
    /// paper may be scanned again; a scan keeps its zone.
    /// </summary>
    public DropOutcome Drop(int i, bool overScanner, DeskZone zone = DeskZone.Desk, bool verdict = false)
    {
        if (!InRange(i) || StateOf(i) != PaperState.OnDesk)
            return DropOutcome.Refused;
        if (overScanner)
        {
            if (ScannerBusy)
            {
                if (!_queue.Contains(i))
                    _queue.Add(i);
                _dropped.Add(i);
                // it waits beside the scanner, on the desk (PaperSizes.ZoneAfterDrop), and once scanned comes back to where it was picked up, in that zone (Tick)
                _comesBackTo[i] = _zones[i];
                _zones[i] = PaperSizes.ZoneAfterDrop(DropOutcome.Queued, zone, _zones[i]);
                return DropOutcome.Queued;
            }
            _comesBackTo.Remove(i); // picked up from where it waited: it comes back there, in the zone it lies in
            BeginScan(i, true);
            return DropOutcome.Scanning;
        }
        LeaveFeed(i);
        if (zone == DeskZone.Counter)
        {
            if (verdict)
                return DropOutcome.HandsBack;
            if (_zones[i] == DeskZone.Desk)
                return DropOutcome.NotStamped;
        }
        _zones[i] = zone;
        return DropOutcome.Stays;
    }

    /// <summary>
    /// Advances a running scan by a positive amount (0 and negative amounts
    /// are ignored). When it reaches the scan's duration (the analysis pass's
    /// for an analysis, else the plain one) the paper goes back on the desk
    /// (analysed, after an analysis pass; one dropped on the busy scanner
    /// takes back the zone it was picked up in, as the desk carries it back
    /// there, unless it was lifted again since: Lift) and its index is returned;
    /// otherwise, or with no scan running, -1.
    /// </summary>
    public int Tick(float seconds)
    {
        if (!ScannerBusy || !(seconds > 0f))
            return -1;

        _elapsed += seconds;
        if (_elapsed < _running)
            return -1;

        int done = _scanning;
        _states[done] = PaperState.OnDesk;
        if (_comesBackTo.TryGetValue(done, out DeskZone back))
        {
            // dropped on the busy scanner, it waited on the desk; it comes back to where it was picked up, in that zone
            _zones[done] = back;
            _comesBackTo.Remove(done);
        }
        _scanned[done] = true;
        if (Pass == ScanPass.Analysis)
            _analysed[done] = true;
        _scanning = -1;
        _elapsed = 0f;
        Pass = ScanPass.Plain;
        return done;
    }

    /// <summary>Every paper goes back to the traveller's side for good (none stays queued); a running scan is cancelled and never finishes.</summary>
    public void ReturnAll()
    {
        for (int i = 0; i < _states.Length; i++)
        {
            _states[i] = PaperState.Returned;
            _zones[i] = DeskZone.Counter;
        }
        _queue.Clear();
        _dropped.Clear();
        _comesBackTo.Clear();
        _scanning = -1;
        _elapsed = 0f;
        Pass = ScanPass.Plain;
    }

    /// <summary>Puts a paper on the scanner's bed and starts the timer for its pass (ScannerDay.PassFor: by hand or fed, analysed before or not; Drop and FeedNext are the callers); a queued paper leaves the queue.</summary>
    private void BeginScan(int i, bool byHand)
    {
        _states[i] = PaperState.Scanning;
        _scanning = i;
        _elapsed = 0f;
        Pass = _scanners.PassFor(byHand, _analysed[i]);
        _running = Pass == ScanPass.Analysis ? _analysisSeconds : _scanSeconds;
        _queue.Remove(i);
        _dropped.Remove(i);
    }

    /// <summary>A paper's state (callers check the range).</summary>
    private PaperState StateOf(int i) => _states[i];

    /// <summary>True for a valid paper index.</summary>
    private bool InRange(int i) => i >= 0 && i < _states.Length;
}

/// <summary>What a left-click on a document does (Papers, Please's controls, Saleh 2026-10-06). Not serialized.</summary>
public enum PaperClickAction
{
    /// <summary>Nothing (in inspect mode, a click off every value).</summary>
    None,

    /// <summary>The document comes to the top of the pile, and nothing else (no move, no change of size, no tilt: Saleh's D1 and R6).</summary>
    Front,

    /// <summary>In inspect mode, the value under the pointer is picked for comparison.</summary>
    Pick
}

/// <summary>
/// A paper's one world size per zone (DOC-HANDLE, BUG-1; Saleh 2026-10-20,
/// D1: "we are doing the camera tilt no need to enlarge the documents when
/// you click them": papers never change size because of the view). A paper
/// has two sizes, the counter's (small) and the desk's (its reading size), in
/// world units, whatever the view: the reading view's tilt only moves the
/// camera. The size changes at most once per crossing, under the cursor, as a
/// held paper's pointer crosses the counter's edge (Papers, Please), the held
/// point staying under the cursor (GrabRatio); a drop, a click and the tilt
/// never change it. Docked in a machine it shows at its docked size
/// (DockFit) until it is clear of it again.
/// </summary>
public static class PaperSizes
{
    /// <summary>The sheet's world scale lying in (or held over) <paramref name="zone"/>: <paramref name="counterScale"/> on the counter, <paramref name="deskScale"/> on the desk. Nothing else (no view, no click) enters it.</summary>
    public static float SheetScale(DeskZone zone, float counterScale, float deskScale) => zone == DeskZone.Desk ? deskScale : counterScale;

    /// <summary>How much a held paper's grab offset (the held point's distance from its centre) grows when its scale goes from <paramref name="from"/> to <paramref name="to"/>: by the same ratio, so the held point stays under the cursor (1 for a scale of 0).</summary>
    public static float GrabRatio(float from, float to) => from > 0f && to > 0f ? to / from : 1f;

    /// <summary>
    /// The zone whose size a held paper shows at, its pointer over
    /// <paramref name="under"/> (its zone <paramref name="recorded"/>; the
    /// passport carrying its <paramref name="verdict"/> or not;
    /// <paramref name="overMachine"/>: its footprint's centre over a machine's
    /// drop area, the dock a release there goes to, DeskController.DockAt).
    /// Over a machine, the desk whatever the pointer: the machines stand on
    /// the desk, and a release there docks it or leaves it on the desk beside
    /// the machine (refused or queued), never on the counter. Otherwise the
    /// zone under the pointer where the paper could lie there (the desk; the
    /// counter for a counter paper, or for any paper once the verdict is in: a
    /// drop there hands the papers back), else the desk (a desk paper over the
    /// counter before the verdict: that drop is refused and it stays on the
    /// desk, so it keeps its desk size).
    /// </summary>
    public static DeskZone HeldZone(DeskZone under, DeskZone recorded, bool verdict, bool overMachine = false) =>
        overMachine ? DeskZone.Desk : under == DeskZone.Desk || recorded == DeskZone.Counter || verdict ? under : DeskZone.Desk;

    /// <summary>
    /// The zone a paper lies in once its drop is decided, and so the one size
    /// it takes after the release (DOC-HANDLE, the drop matrix's (c): a drop
    /// never changes a paper's size after the release): released over
    /// <paramref name="released"/> (the pointer's zone), its zone
    /// <paramref name="before"/> the drop.
    /// <list type="bullet">
    /// <item>Stays: the zone it was dropped in.</item>
    /// <item>HandsBack: the counter (it goes back to the traveller).</item>
    /// <item>NotStamped, Queued, MachineRefused: the desk. It lies on the desk, off the counter or just beside the machine (machines stand on the desk), which is the size it was held at there (HeldZone).</item>
    /// <item>Scanning, Refused: <paramref name="before"/>. Docked, it shows at its docked size and comes back out to where it was picked up, in its own zone; refused by DeskPapers it was never on the desk, and nothing changes.</item>
    /// </list>
    /// </summary>
    public static DeskZone ZoneAfterDrop(DropOutcome outcome, DeskZone released, DeskZone before)
    {
        switch (outcome)
        {
            case DropOutcome.Stays:
                return released;
            case DropOutcome.HandsBack:
                return DeskZone.Counter;
            case DropOutcome.NotStamped:
            case DropOutcome.Queued:
            case DropOutcome.MachineRefused:
                return DeskZone.Desk;
            default:
                return before;
        }
    }
}

/// <summary>How a left-click on a document routes (the one input model: only left-clicks act; a right-click backs out, ControlRules).</summary>
public static class PaperClicks
{
    /// <summary>In inspect mode (<paramref name="inspecting"/>) a click on a value (<paramref name="onRow"/>) picks it and a click off every value does nothing (outside inspect mode clicks never compare); otherwise the document comes to the top of the pile wherever it lies and nothing else happens (Saleh 2026-10-20, D1: "no need to enlarge the documents when you click them"; R6: "clicking a desk paper brings it to the top of the stack"): a paper goes to the desk only by a drag.</summary>
    public static PaperClickAction Decide(bool inspecting, bool onRow)
    {
        if (inspecting)
            return onRow ? PaperClickAction.Pick : PaperClickAction.None;
        return PaperClickAction.Front;
    }
}

// <summary>When the day-1 desk notes show (their text and last day are DeskConfigSO knobs).</summary>
public static class DeskHints
{
    /// <summary>The scanner note: a non-blank hint, on or before its last day, while a paper is on the desk and no paper was read (examined) or scanned yet today (<paramref name="usesToday"/>: papers read or scanned today).</summary>
    public static bool ScanHintVisible(string hint, int day, int untilDay, int usesToday, bool paperOnDesk) =>
        !string.IsNullOrWhiteSpace(hint) && day <= untilDay && usesToday == 0 && paperOnDesk;

    /// <summary>The wheel note: a non-blank hint, on or before its last day, while a traveller is at the desk and the wheel was not opened yet today.</summary>
    public static bool WheelHintVisible(string hint, int day, int untilDay, bool wheelOpenedToday, bool travellerAtDesk) =>
        !string.IsNullOrWhiteSpace(hint) && day <= untilDay && !wheelOpenedToday && travellerAtDesk;
}
