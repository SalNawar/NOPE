using System;
using System.Collections.Generic;

/// <summary>When the traveller hands a document over. Serialized on DocumentTemplateSO: append only.</summary>
public enum DocumentHandOver
{
    /// <summary>When the desk asks for it (a "Request" choice on the traveller wheel). 0, so existing templates read as OnRequest.</summary>
    OnRequest,

    /// <summary>When the traveller steps up to the desk.</summary>
    OnArrival
}

/// <summary>The one rule over DocumentHandOver.</summary>
public static class DocumentHandOvers
{
    /// <summary>True when a document with this hand-over waits for the desk to ask (it then gets a hub request and counts toward the hub's size).</summary>
    public static bool IsRequested(DocumentHandOver handOver) => handOver == DocumentHandOver.OnRequest;
}

/// <summary>One of the current traveller's documents, as the desk and the interview see it.</summary>
public sealed class CaseDocument
{
    /// <summary>The document's display name ("Travel Passport").</summary>
    public string name;

    /// <summary>The document's fields: the boxes its paper's form shows.</summary>
    public IReadOnlyList<DocumentField> fields;

    /// <summary>When the traveller hands it over.</summary>
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

    /// <summary>It goes back to where it was picked up (the scanner is busy, or the paper could not be dropped).</summary>
    Refused,

    /// <summary>A paper was dropped on the counter once the passport carries its verdict: the papers go back to the traveller with it (Papers, Please's hand-back).</summary>
    HandsBack,

    /// <summary>A paper from the desk was dropped on the counter before the passport carries a verdict: it goes back to where it was picked up, on the desk, with the note "Stamp the passport first" (Saleh 2026-10-06: "documents can only be returned after stamping").</summary>
    NotStamped
}

/// <summary>The desk's two zones (Papers, Please's, Saleh 2026-10-06): where a document lies decides its size.</summary>
public enum DeskZone
{
    /// <summary>The counter: the strip on the traveller's side, where papers arrive and go back; a document there is small.</summary>
    Counter,

    /// <summary>The desk: the reading area; a document there is full size.</summary>
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

    /// <summary>The running scan's duration.</summary>
    private float _running;

    /// <summary>The Auto-Feed queue: the papers waiting to scan themselves, in hand-over order.</summary>
    private readonly List<int> _queue = new List<int>();

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
        ArrivalIndices = CaseDocuments.ArrivalIndices(documents);
        _scanSeconds = scanSeconds > MinScanSeconds ? scanSeconds : MinScanSeconds;
        _analysisSeconds = analysisSeconds > MinScanSeconds ? analysisSeconds : MinScanSeconds;
        _scanners = scanners;
    }

    /// <summary>How many papers the case has.</summary>
    public int Count => _states.Length;

    /// <summary>True while a scan runs.</summary>
    public bool ScannerBusy => _scanning >= 0;

    /// <summary>The running scan's pass (ScannerDay.PassFor: the analysis pass for a paper dropped on the scanner with the Analysis Scanner, once per document); Plain while the scanner fed itself or is idle.</summary>
    public ScanPass Pass { get; private set; }

    /// <summary>True when paper <paramref name="i"/>'s analysis pass has ended (a re-scan of it is AlreadyAnalysed).</summary>
    public bool WasAnalysed(int i) => InRange(i) && _analysed[i];

    /// <summary>The papers waiting to scan themselves (the Auto-Feed queue).</summary>
    public int QueuedCount => _queue.Count;

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

    /// <summary>
    /// What size a dragged document shows while the pointer is over
    /// <paramref name="under"/>, having shown <paramref name="shown"/>: over
    /// the desk it grows to full size at once; over the counter it keeps the
    /// size it has (Saleh 2026-10-06: "it keeps shrinking the documents as I
    /// try to stamp"), so a document from the desk shrinks only once it is
    /// handed back.
    /// </summary>
    public static DeskZone ShownWhileDragged(DeskZone shown, DeskZone under) => under == DeskZone.Desk ? DeskZone.Desk : shown;

    /// <summary>The zone paper <paramref name="i"/> lies in (the counter until it is dropped on the desk; the counter out of range).</summary>
    public DeskZone ZoneOf(int i) => InRange(i) ? _zones[i] : DeskZone.Counter;

    /// <summary>Hands a paper over from the traveller onto the counter; false for any other state or an index out of range.</summary>
    public bool HandOver(int i)
    {
        if (!InRange(i) || StateOf(i) != PaperState.WithTraveller)
            return false;

        _states[i] = PaperState.OnDesk;
        _zones[i] = DeskZone.Counter;
        return true;
    }

    /// <summary>
    /// Queues a handed-over paper to scan itself (the Auto-Feed Scanner: the
    /// controller enqueues each hand-over while the upgrade is owned); false
    /// when it is not on the desk, is already queued, or is out of range.
    /// </summary>
    public bool Enqueue(int i)
    {
        if (!CanDrag(i) || _queue.Contains(i))
            return false;

        _queue.Add(i);
        return true;
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

            BeginScan(i, false);
            return i;
        }
        return -1;
    }

    /// <summary>True while the paper is on the desk (not scanning); false out of range.</summary>
    public bool CanDrag(int i) => InRange(i) && StateOf(i) == PaperState.OnDesk;

    /// <summary>
    /// Decides a released paper: a paper that is not on the desk (or an index
    /// out of range) is Refused and nothing changes; over the scanner it
    /// starts scanning by hand while the scanner is idle (a queued paper
    /// leaves the queue) and is Refused while it is busy; on the counter, once
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
                return DropOutcome.Refused;
            BeginScan(i, true);
            return DropOutcome.Scanning;
        }
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
    /// (analysed, after an analysis pass) and its index is returned;
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

    /// <summary>A document on the counter goes to the desk, full size, to be read (as if dragged there).</summary>
    Read,

    /// <summary>A document on the desk comes to the top of the pile.</summary>
    Front,

    /// <summary>In inspect mode, the value under the pointer is picked for comparison.</summary>
    Pick
}

/// <summary>How a left-click on a document routes (the one input model: only left-clicks act; a right-click backs out, ControlRules).</summary>
public static class PaperClicks
{
    /// <summary>In inspect mode (<paramref name="inspecting"/>) a click on a value (<paramref name="onRow"/>) picks it and a click off every value does nothing (outside inspect mode clicks never compare); otherwise a document on the counter goes to the desk to be read and one on the desk comes to the top.</summary>
    public static PaperClickAction Decide(bool inspecting, bool onRow, DeskZone zone)
    {
        if (inspecting)
            return onRow ? PaperClickAction.Pick : PaperClickAction.None;
        return zone == DeskZone.Counter ? PaperClickAction.Read : PaperClickAction.Front;
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
