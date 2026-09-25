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

    /// <summary>The name the paper shows: the traveller's registered given name.</summary>
    public string holder;

    /// <summary>When the traveller hands it over.</summary>
    public DocumentHandOver handOver;

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

/// <summary>What happens to a paper released on the desk: it stays where it was dropped, it starts scanning, or it goes back to where it was picked up.</summary>
public enum DropOutcome
{
    /// <summary>It stays where it was dropped (not over the scanner).</summary>
    Stays,

    /// <summary>It starts scanning (over the idle scanner).</summary>
    Scanning,

    /// <summary>It goes back to where it was picked up (the scanner is busy, or the paper could not be dropped).</summary>
    Refused
}

/// <summary>
/// One case's papers: each is handed over once (from the traveller onto the
/// desk), can be dragged while on the desk, and is scanned by dropping it on
/// the scanner, one scan at a time, for a fixed duration; at the decision
/// every paper goes back and a running scan is cancelled. Pure, so every
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

        /// <summary>On the desk, draggable.</summary>
        OnDesk,

        /// <summary>On the scanner's bed, being scanned.</summary>
        Scanning,

        /// <summary>Given back at the decision.</summary>
        Returned
    }

    private readonly PaperState[] _states;
    private readonly float _scanSeconds;

    /// <summary>The paper being scanned, or -1.</summary>
    private int _scanning = -1;

    /// <summary>Seconds the running scan has taken.</summary>
    private float _elapsed;

    /// <summary>Every paper starts with the traveller; a null list means no papers; a scan shorter than 0.01 s is raised to it.</summary>
    public DeskPapers(IReadOnlyList<CaseDocument> documents, float scanSeconds)
    {
        _states = new PaperState[documents != null ? documents.Count : 0];
        ArrivalIndices = CaseDocuments.ArrivalIndices(documents);
        _scanSeconds = scanSeconds > MinScanSeconds ? scanSeconds : MinScanSeconds;
    }

    /// <summary>How many papers the case has.</summary>
    public int Count => _states.Length;

    /// <summary>True while a scan runs.</summary>
    public bool ScannerBusy => _scanning >= 0;

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

    /// <summary>Hands a paper over from the traveller onto the desk; false for any other state or an index out of range.</summary>
    public bool HandOver(int i)
    {
        if (!InRange(i) || StateOf(i) != PaperState.WithTraveller)
            return false;

        _states[i] = PaperState.OnDesk;
        return true;
    }

    /// <summary>True while the paper is on the desk (false out of range).</summary>
    public bool CanDrag(int i) => InRange(i) && StateOf(i) == PaperState.OnDesk;

    /// <summary>
    /// Decides a released paper: a paper that is not on the desk (or an index
    /// out of range) is Refused and nothing changes; a paper on the desk stays
    /// where it was dropped unless it is over the scanner, where it starts
    /// scanning while the scanner is idle and is Refused while it is busy. A
    /// scanned paper may be scanned again.
    /// </summary>
    public DropOutcome Drop(int i, bool overScanner)
    {
        if (!CanDrag(i))
            return DropOutcome.Refused;
        if (!overScanner)
            return DropOutcome.Stays;
        if (ScannerBusy)
            return DropOutcome.Refused;

        BeginScan(i);
        return DropOutcome.Scanning;
    }

    /// <summary>
    /// Advances a running scan by a positive amount (0 and negative amounts
    /// are ignored). When it reaches the scan duration the paper goes back on
    /// the desk and its index is returned; otherwise, or with no scan
    /// running, -1.
    /// </summary>
    public int Tick(float seconds)
    {
        if (!ScannerBusy || !(seconds > 0f))
            return -1;

        _elapsed += seconds;
        if (_elapsed < _scanSeconds)
            return -1;

        int done = _scanning;
        _states[done] = PaperState.OnDesk;
        _scanning = -1;
        _elapsed = 0f;
        return done;
    }

    /// <summary>Every paper goes back to the traveller's side for good; a running scan is cancelled and never finishes.</summary>
    public void ReturnAll()
    {
        for (int i = 0; i < _states.Length; i++)
            _states[i] = PaperState.Returned;
        _scanning = -1;
        _elapsed = 0f;
    }

    /// <summary>Puts a paper on the scanner's bed and starts the timer (Drop is the only caller).</summary>
    private void BeginScan(int i)
    {
        _states[i] = PaperState.Scanning;
        _scanning = i;
        _elapsed = 0f;
    }

    /// <summary>A paper's state (callers check the range).</summary>
    private PaperState StateOf(int i) => _states[i];

    /// <summary>True for a valid paper index.</summary>
    private bool InRange(int i) => i >= 0 && i < _states.Length;
}

/// <summary>When the day-1 desk notes show (their text and last day are DeskConfigSO knobs).</summary>
public static class DeskHints
{
    /// <summary>The scanner note: a non-blank hint, on or before its last day, while a paper is on the desk and nothing was scanned yet today.</summary>
    public static bool ScanHintVisible(string hint, int day, int untilDay, int scansToday, bool paperOnDesk) =>
        !string.IsNullOrWhiteSpace(hint) && day <= untilDay && scansToday == 0 && paperOnDesk;

    /// <summary>The wheel note: a non-blank hint, on or before its last day, while a traveller is at the desk and the wheel was not opened yet today.</summary>
    public static bool WheelHintVisible(string hint, int day, int untilDay, bool wheelOpenedToday, bool travellerAtDesk) =>
        !string.IsNullOrWhiteSpace(hint) && day <= untilDay && !wheelOpenedToday && travellerAtDesk;
}
