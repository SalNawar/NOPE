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

    /// <summary>The document's fields: the rows its paper shows (DocumentRows).</summary>
    public IReadOnlyList<DocumentField> fields;

    /// <summary>When the traveller hands it over.</summary>
    public DocumentHandOver handOver;

    /// <summary>True when the paper carries the traveller's photo (DocumentTemplateSO.showsPhoto).</summary>
    public bool showsPhoto;

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

/// <summary>Where a paper held in the hand sits (piece 10): the two examine slots low beside the screen's centre.</summary>
public enum ExamineSlot
{
    /// <summary>Not held.</summary>
    None,

    /// <summary>The left slot.</summary>
    Left,

    /// <summary>The right slot.</summary>
    Right
}

/// <summary>What DeskPapers.Hold did: whether the paper is now held, its slot, and the paper sent back to the desk to free it (-1 for none).</summary>
public readonly struct HoldResult
{
    /// <summary>A hold's outcome.</summary>
    public HoldResult(bool held, ExamineSlot slot, int evicted)
    {
        Held = held;
        Slot = slot;
        Evicted = evicted;
    }

    /// <summary>True when the paper is now held.</summary>
    public bool Held { get; }

    /// <summary>The slot it took (None when not held).</summary>
    public ExamineSlot Slot { get; }

    /// <summary>The paper held longest, put back on the desk to free its slot, or -1.</summary>
    public int Evicted { get; }
}

/// <summary>
/// One case's papers: each is handed over once (from the traveller onto the
/// desk), can be dragged while on the desk, and is scanned by dropping it on
/// the scanner, one scan at a time, for a fixed duration; at the decision
/// every paper goes back and a running scan is cancelled. A paper on the desk
/// can be held in the hand (piece 10), two at a time: it takes the slot on its
/// side when free, else the other; with both full, the paper held longest goes
/// back to the desk and the new one takes its slot. A held paper can be dragged
/// (out of the hand) but must be put back before it drops. Pure, so every
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

        /// <summary>Held in the hand (an examine slot); still draggable, and still on the desk for the day-1 note.</summary>
        Held,

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

    /// <summary>Each paper's examine slot (None unless held).</summary>
    private readonly ExamineSlot[] _slots;

    /// <summary>The held papers, held longest first.</summary>
    private readonly List<int> _holdOrder = new List<int>();

    /// <summary>Every paper starts with the traveller; a null list means no papers; a scan shorter than 0.01 s is raised to it.</summary>
    public DeskPapers(IReadOnlyList<CaseDocument> documents, float scanSeconds)
    {
        _states = new PaperState[documents != null ? documents.Count : 0];
        _slots = new ExamineSlot[_states.Length];
        ArrivalIndices = CaseDocuments.ArrivalIndices(documents);
        _scanSeconds = scanSeconds > MinScanSeconds ? scanSeconds : MinScanSeconds;
    }

    /// <summary>How many papers the case has.</summary>
    public int Count => _states.Length;

    /// <summary>True while a scan runs.</summary>
    public bool ScannerBusy => _scanning >= 0;

    /// <summary>The papers handed over on arrival, in paper order (CaseDocuments.ArrivalIndices).</summary>
    public IReadOnlyList<int> ArrivalIndices { get; }

    /// <summary>Papers on the desk, the one being scanned and those held in the hand included.</summary>
    public int OnDeskCount
    {
        get
        {
            int n = 0;
            for (int i = 0; i < _states.Length; i++)
                if (StateOf(i) == PaperState.OnDesk || StateOf(i) == PaperState.Scanning || StateOf(i) == PaperState.Held)
                    n++;
            return n;
        }
    }

    /// <summary>How many papers are held in the hand (0 to 2).</summary>
    public int HeldCount => _holdOrder.Count;

    /// <summary>The paper held longest (the one a third paper sends back), or -1 when none is held.</summary>
    public int HeldLongest => _holdOrder.Count > 0 ? _holdOrder[0] : -1;

    /// <summary>Hands a paper over from the traveller onto the desk; false for any other state or an index out of range.</summary>
    public bool HandOver(int i)
    {
        if (!InRange(i) || StateOf(i) != PaperState.WithTraveller)
            return false;

        _states[i] = PaperState.OnDesk;
        return true;
    }

    /// <summary>True while the paper is on the desk or held in the hand (the drag-out); false out of range.</summary>
    public bool CanDrag(int i) => InRange(i) && (StateOf(i) == PaperState.OnDesk || StateOf(i) == PaperState.Held);

    /// <summary>True while the paper is held in the hand (false out of range).</summary>
    public bool IsHeld(int i) => InRange(i) && StateOf(i) == PaperState.Held;

    /// <summary>
    /// Takes a paper on the desk into the hand (anything else, or an index out
    /// of range, is not held and nothing changes): the slot on its side
    /// (<paramref name="preferRight"/>) when free, else the other free one;
    /// with both taken, the paper held longest goes back on the desk and the
    /// new one takes its slot.
    /// </summary>
    public HoldResult Hold(int i, bool preferRight)
    {
        if (!InRange(i) || StateOf(i) != PaperState.OnDesk)
            return new HoldResult(false, ExamineSlot.None, -1);

        ExamineSlot preferred = preferRight ? ExamineSlot.Right : ExamineSlot.Left;
        ExamineSlot other = preferRight ? ExamineSlot.Left : ExamineSlot.Right;
        int evicted = -1;
        ExamineSlot slot;
        if (HolderOf(preferred) < 0)
        {
            slot = preferred;
        }
        else if (HolderOf(other) < 0)
        {
            slot = other;
        }
        else
        {
            evicted = _holdOrder[0];
            slot = _slots[evicted];
            PutBack(evicted);
        }

        _states[i] = PaperState.Held;
        _slots[i] = slot;
        _holdOrder.Add(i);
        return new HoldResult(true, slot, evicted);
    }

    /// <summary>Puts a held paper back on the desk; false (nothing changes) when it is not held.</summary>
    public bool PutBack(int i)
    {
        if (!IsHeld(i))
            return false;

        _states[i] = PaperState.OnDesk;
        _slots[i] = ExamineSlot.None;
        _holdOrder.Remove(i);
        return true;
    }

    /// <summary>Puts every held paper back on the desk and returns them in slot order, left first.</summary>
    public IReadOnlyList<int> PutBackAll()
    {
        var back = new List<int>();
        foreach (ExamineSlot slot in new[] { ExamineSlot.Left, ExamineSlot.Right })
        {
            int i = HolderOf(slot);
            if (i >= 0 && PutBack(i))
                back.Add(i);
        }
        return back;
    }

    /// <summary>
    /// Decides a released paper: a paper that is not on the desk (held in the
    /// hand, or an index out of range) is Refused and nothing changes; a paper on the desk stays
    /// where it was dropped unless it is over the scanner, where it starts
    /// scanning while the scanner is idle and is Refused while it is busy. A
    /// scanned paper may be scanned again.
    /// </summary>
    public DropOutcome Drop(int i, bool overScanner)
    {
        if (!InRange(i) || StateOf(i) != PaperState.OnDesk)
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

    /// <summary>Every paper goes back to the traveller's side for good (none stays held); a running scan is cancelled and never finishes.</summary>
    public void ReturnAll()
    {
        for (int i = 0; i < _states.Length; i++)
        {
            _states[i] = PaperState.Returned;
            _slots[i] = ExamineSlot.None;
        }
        _holdOrder.Clear();
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

    /// <summary>The paper held in a slot, or -1.</summary>
    private int HolderOf(ExamineSlot slot)
    {
        foreach (int i in _holdOrder)
            if (_slots[i] == slot)
                return i;
        return -1;
    }

    /// <summary>True for a valid paper index.</summary>
    private bool InRange(int i) => i >= 0 && i < _states.Length;
}

/// <summary>
/// Where papers go while papers are held in the hand (piece 10): the two
/// examine slots cover most of the desk's mat, so a paper lifted into the hand
/// takes the side that hides no other paper on the desk when the side it lies
/// on would hide one, and a paper handed over while papers are held lands on
/// the first spawn slot that no held paper covers (under them only when every
/// slot is covered).
/// </summary>
public static class HeldCover
{
    /// <summary>The side a lifted paper prefers: the side it lies on (<paramref name="liesRight"/>), unless that slot would hide papers on the desk and the other would hide none.</summary>
    public static bool PreferRight(bool liesRight, int hiddenLeft, int hiddenRight)
    {
        int own = liesRight ? hiddenRight : hiddenLeft;
        int other = liesRight ? hiddenLeft : hiddenRight;
        return own > 0 && other == 0 ? !liesRight : liesRight;
    }

    /// <summary>The spawn slot a handed-over paper lands on: the next in turn (<paramref name="next"/>, wrapped round the slots) unless a held paper covers it, else the first uncovered slot after it; the next in turn when every slot is covered or nothing is known (0 with no slots).</summary>
    public static int LandingSlot(int next, IReadOnlyList<bool> covered)
    {
        int count = covered != null ? covered.Count : 0;
        if (count == 0)
            return covered == null ? next : 0;

        int first = ((next % count) + count) % count;
        for (int k = 0; k < count; k++)
        {
            int slot = (first + k) % count;
            if (!covered[slot])
                return slot;
        }
        return first;
    }
}

/// <summary>What a click on a paper does (piece 10).</summary>
public enum PaperClickAction
{
    /// <summary>Nothing.</summary>
    None,

    /// <summary>Takes a paper on the desk into the hand.</summary>
    Examine,

    /// <summary>Puts a held paper back where it lay.</summary>
    PutBack,

    /// <summary>Picks the clicked row of a held paper for comparison.</summary>
    Pick
}

/// <summary>How a click on a paper routes (piece 10).</summary>
public static class PaperClicks
{
    /// <summary>
    /// On a paper on the desk, a left click examines it and a right click does
    /// nothing; on a held paper, a left click on a row picks the row, a left
    /// click off every row (the title, the photo, a margin) puts it back, and
    /// a right click puts it back.
    /// </summary>
    public static PaperClickAction Decide(bool held, bool secondary, bool onRow)
    {
        if (!held)
            return secondary ? PaperClickAction.None : PaperClickAction.Examine;
        if (secondary)
            return PaperClickAction.PutBack;
        return onRow ? PaperClickAction.Pick : PaperClickAction.PutBack;
    }
}

/// <summary>When the day-1 desk notes show (their text and last day are DeskConfigSO knobs).</summary>
public static class DeskHints
{
    /// <summary>The scanner note: a non-blank hint, on or before its last day, while a paper is on the desk and no paper was read (examined) or scanned yet today (<paramref name="usesToday"/>: papers read or scanned today).</summary>
    public static bool ScanHintVisible(string hint, int day, int untilDay, int usesToday, bool paperOnDesk) =>
        !string.IsNullOrWhiteSpace(hint) && day <= untilDay && usesToday == 0 && paperOnDesk;

    /// <summary>The wheel note: a non-blank hint, on or before its last day, while a traveller is at the desk and the wheel was not opened yet today.</summary>
    public static bool WheelHintVisible(string hint, int day, int untilDay, bool wheelOpenedToday, bool travellerAtDesk) =>
        !string.IsNullOrWhiteSpace(hint) && day <= untilDay && !wheelOpenedToday && travellerAtDesk;
}
