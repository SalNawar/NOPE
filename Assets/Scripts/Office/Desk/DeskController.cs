using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// One case's papers on the desk: hands papers over from the traveller's side
/// to the spawn slots, lets the player drag them (DeskDraggable), decides each
/// drop through DeskPapers (it stays, it scans on the scanner, or it slides back
/// to where it was picked up), runs the scan timer and raises ScanFinished,
/// stacks papers by height (each place in the stack lifts a sheet one step, a
/// dragged paper above them all), returns them at the decision, and shows the
/// day-1 scan note. A click on a paper routes through PaperClicks (piece 10):
/// it lifts a paper on the desk into the hand (DeskPapers.Hold, posed by the
/// PaperExaminer; PaperExamined; on the side that hides no other paper when
/// it can, and a paper handed over while papers are held lands where none
/// covers it: HeldCover), picks a held paper's row (FieldPicked) or
/// puts it back where it lay, on top of the stack; a click on the desk (the
/// desk catcher) or Escape puts every held paper back, and dragging a held
/// paper drops it back onto the desk under the pointer and on. A paper on the
/// desk takes input only while BoothCoordinator allows papers, DeskPapers lets
/// it be dragged and it is not sliding; a held paper while held papers are
/// allowed; papers not allowed take no raycasts at all (spec R38).
/// </summary>
public sealed class DeskController : MonoBehaviour
{
    /// <summary>The desk the papers lie on.</summary>
    [SerializeField] private DeskSurface surface;

    /// <summary>The desk scanner.</summary>
    [SerializeField] private DeskScanner scanner;

    /// <summary>The inactive paper cloned per handed-over document.</summary>
    [SerializeField] private DeskDocument paperTemplate;

    /// <summary>Where paper clones live.</summary>
    [SerializeField] private Transform paperRoot;

    /// <summary>Where papers slide in from and back to: the traveller's side of the desk.</summary>
    [SerializeField] private Transform handOverPoint;

    /// <summary>The day-1 note on the scanner tray (its text comes from the config).</summary>
    [SerializeField] private TMP_Text scanHint;

    /// <summary>The desk tuning (scan time, spawn slots, slide time, the stack's heights, the photo's tint, the note).</summary>
    [SerializeField] private DeskConfigSO config;

    /// <summary>Poses the papers held in the hand (piece 10; optional: without it a click on a paper does nothing).</summary>
    [SerializeField] private PaperExaminer examiner;

    /// <summary>The desk catcher (piece 10; optional): a click on the desk puts every held paper back; active only while BoothCoordinator allows it.</summary>
    [SerializeField] private ClickCatcher deskCatcher;

    /// <summary>The case's papers by index (null until handed over).</summary>
    private readonly List<DeskDocument> _papers = new List<DeskDocument>();

    private readonly PaperStack _stack = new PaperStack();
    private IReadOnlyList<CaseDocument> _documents = Array.Empty<CaseDocument>();
    private CaseTranslation _translation = CaseTranslation.None;
    private IReadOnlyList<RevealClock> _clocks = Array.Empty<RevealClock>();
    private TravellerLook _look;
    private CharacterArt _art;
    private DeskPapers _state;
    private bool _live;
    private bool _heldLive;
    private bool _escapeLive;

    /// <summary>The frame Escape became able to put papers back (an Escape that closed the frame, the wheel or the tray in the same frame is not taken again).</summary>
    private int _escapeLiveSince;
    private int _day;
    private int _scansToday;
    private int _readsToday;
    private int _handedOver;

    /// <summary>The paper being dragged, or -1.</summary>
    private int _dragged = -1;

    /// <summary>True when the desk and all its parts are wired; otherwise documents go straight to their windows (InvestigationUIController).</summary>
    public bool IsReachable =>
        surface != null && scanner != null && paperTemplate != null && paperRoot != null && handOverPoint != null && config != null;

    /// <summary>How many papers are held in the hand.</summary>
    public int HeldCount => _state != null ? _state.HeldCount : 0;

    /// <summary>Raised when a scan finishes, with the paper's index (its window opens).</summary>
    public event Action<int> ScanFinished;

    /// <summary>Raised when a paper is lifted into the hand, with its index (the first time is its sighting: its translation's reveal).</summary>
    public event Action<int> PaperExamined;

    /// <summary>Raised when a held paper's row is picked for comparison: the paper's index, the row and where it lights up.</summary>
    public event Action<int, DocumentRow, ICompareHighlight> FieldPicked;

    /// <summary>Raised when the papers held in the hand change (the booth's input rules read HeldCount).</summary>
    public event Action HoldsChanged;

    private void Awake()
    {
        if (scanHint != null && config != null)
            scanHint.text = UiText.Get(config.scanHintKey);
        if (deskCatcher != null)
        {
            deskCatcher.onClick.AddListener(() => PutBackAll(false));
            deskCatcher.gameObject.SetActive(false);
        }
        RefreshHint();
    }

    /// <summary>Advances a running scan (a finished paper slides back to where it was picked up); Escape puts every held paper back while that is allowed.</summary>
    private void Update()
    {
        if (_escapeLive && _escapeLiveSince < Time.frameCount && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            PutBackAll(false);

        if (_state == null || !_state.ScannerBusy)
            return;

        int done = _state.Tick(Time.deltaTime);
        if (done < 0)
            return;

        DeskDocument paper = _papers[done];
        Slide(paper, paper.GetComponent<DeskDraggable>().PickUpPosition);
        scanner.Pulse();
        _scansToday++;
        RefreshHint();
        ScanFinished?.Invoke(done);
    }

    /// <summary>Starts a day: nothing read or scanned yet (the scan note may show again on its days).</summary>
    public void BeginDay(int day)
    {
        _day = day;
        _scansToday = 0;
        _readsToday = 0;
        RefreshHint();
    }

    /// <summary>Starts a case's papers (a photo document shows <paramref name="look"/>; each paper's values show in the traveller's translation on its document's reveal clock, shared with the scanned copy); the documents handed over on arrival slide onto the desk.</summary>
    public void BeginCase(IReadOnlyList<CaseDocument> docs, TravellerLook look, CharacterArt art, CaseTranslation translation, IReadOnlyList<RevealClock> clocks)
    {
        _documents = docs ?? Array.Empty<CaseDocument>();
        _translation = translation ?? CaseTranslation.None;
        _clocks = clocks ?? Array.Empty<RevealClock>();
        _look = look;
        _art = art;
        _state = new DeskPapers(_documents, config.scanSeconds);
        _papers.Clear();
        for (int i = 0; i < _state.Count; i++)
            _papers.Add(null);
        _stack.Clear();
        _handedOver = 0;
        _dragged = -1;

        foreach (int i in _state.ArrivalIndices)
            HandOver(i);
        RefreshHint();
        HoldsChanged?.Invoke();
    }

    /// <summary>
    /// Hands paper <paramref name="i"/> over: a clone of the template appears at
    /// the hand-over point, on top of the stack, and slides to the next spawn
    /// slot (slots are reused in order). Nothing happens unless DeskPapers
    /// agrees (a paper is handed over once).
    /// </summary>
    public void HandOver(int i)
    {
        if (_state == null || !_state.HandOver(i))
            return;

        DeskDocument paper = Instantiate(paperTemplate, paperRoot);
        paper.transform.position = handOverPoint.position;
        paper.gameObject.SetActive(true);
        paper.Bind(i, _documents[i], _translation, i < _clocks.Count ? _clocks[i] : null, config);
        paper.ShowPhoto(_documents[i] != null && _documents[i].showsPhoto ? _look : null, _art, config.travellerTint);

        DeskDraggable drag = paper.GetComponent<DeskDraggable>();
        drag.Init(surface);
        drag.DragBegan += HandleDragBegan;
        drag.DragEnded += HandleDragEnded;
        paper.Clicked += HandlePaperClicked;

        _papers[i] = paper;
        _stack.Add(i);
        ApplyStack();

        Vector3 target = LandingPoint();
        _handedOver++;
        Slide(paper, target);
        RefreshHint();
    }

    /// <summary>Rewrites a paper's values from its document's reveal clock (a sighting on the PC started it); nothing for a paper not handed over.</summary>
    public void RefreshPaper(int i)
    {
        if (i >= 0 && i < _papers.Count && _papers[i] != null)
            _papers[i].Refresh();
    }

    /// <summary>The decision: held papers drop back at once, then every paper goes back (a running scan is cancelled), slides inert and out of the raycast to the traveller's side and is destroyed.</summary>
    public void EndCase()
    {
        if (_state == null)
            return;

        foreach (int i in _state.PutBackAll())
            Release(_papers[i], true);
        if (examiner != null)
            examiner.Clear();

        _state.ReturnAll();
        foreach (DeskDocument paper in _papers)
        {
            if (paper == null)
                continue;

            DeskDocument leaving = paper;
            leaving.SetExamined(false);
            leaving.SetLive(false, false);
            leaving.SlideTo(handOverPoint.position, config.paperSlideSeconds, () => Destroy(leaving.gameObject));
        }

        _papers.Clear();
        _stack.Clear();
        _dragged = -1;
        RefreshHint();
        HoldsChanged?.Invoke();
    }

    /// <summary>Allows the papers on the desk input or not (BoothCoordinator); remembered for papers handed over later. Papers not allowed take no raycasts (spec R38).</summary>
    public void SetPapersLive(bool live)
    {
        _live = live;
        ApplyLiveAll();
    }

    /// <summary>Allows the papers held in the hand input or not (BoothCoordinator: BoothRules.HeldPapersLive).</summary>
    public void SetHeldLive(bool live)
    {
        _heldLive = live;
        ApplyLiveAll();
    }

    /// <summary>Shows the desk catcher (a click on the desk puts every held paper back) or hides it (BoothCoordinator: BoothRules.DeskCatcherLive).</summary>
    public void SetDeskCatcherLive(bool live)
    {
        if (deskCatcher != null && deskCatcher.gameObject.activeSelf != live)
            deskCatcher.gameObject.SetActive(live);
    }

    /// <summary>Lets Escape put every held paper back, from the next frame on (BoothCoordinator: BoothRules.ExamineEscapeLive).</summary>
    public void SetExamineEscapeLive(bool live)
    {
        if (live && !_escapeLive)
            _escapeLiveSince = Time.frameCount;
        _escapeLive = live;
    }

    /// <summary>A click on a paper (PaperClicks): examine a paper on the desk, pick a held paper's row, or put a held paper back.</summary>
    private void HandlePaperClicked(DeskDocument paper, bool secondary, int row)
    {
        if (_state == null)
            return;

        switch (PaperClicks.Decide(_state.IsHeld(paper.Index), secondary, row >= 0 && row < paper.RowCount))
        {
            case PaperClickAction.Examine:
                Examine(paper);
                break;
            case PaperClickAction.Pick:
                FieldPicked?.Invoke(paper.Index, paper.FieldRow(row), paper.RowHighlight(row));
                break;
            case PaperClickAction.PutBack:
                if (_state.PutBack(paper.Index))
                {
                    Release(paper, false);
                    HoldsChanged?.Invoke();
                }
                break;
        }
    }

    /// <summary>
    /// Where the next handed-over paper lands: the next spawn slot in turn
    /// (slots are reused in order), or, while papers are held, the first slot
    /// after it that no held paper covers (HeldCover.LandingSlot).
    /// </summary>
    private Vector3 LandingPoint()
    {
        Vector2[] slots = config.paperSpawnSlots;
        if (slots == null || slots.Length == 0)
            return surface.transform.position;

        var covered = new bool[slots.Length];
        if (examiner != null && _state.HeldCount > 0)
            for (int k = 0; k < slots.Length; k++)
                covered[k] = examiner.HeldCovers(surface.PointAt(slots[k]));
        return surface.PointAt(slots[HeldCover.LandingSlot(_handedOver, covered)]);
    }

    /// <summary>
    /// Lifts a paper on the desk into the hand, into the slot on its side of
    /// the screen unless that slot would hide another paper on the desk and the
    /// other would not (HeldCover.PreferRight); the paper held longest goes back
    /// when both are taken.
    /// </summary>
    private void Examine(DeskDocument paper)
    {
        if (examiner == null || paper.IsSliding)
            return;

        int hiddenLeft = 0, hiddenRight = 0;
        foreach (DeskDocument other in _papers)
        {
            if (other == null || other == paper || _state.IsHeld(other.Index) || !_state.CanDrag(other.Index))
                continue;
            if (examiner.SlotCovers(false, other.Sheet.position))
                hiddenLeft++;
            if (examiner.SlotCovers(true, other.Sheet.position))
                hiddenRight++;
        }

        bool preferRight = HeldCover.PreferRight(examiner.RightOfCentre(paper.Sheet.position), hiddenLeft, hiddenRight);
        HoldResult hold = _state.Hold(paper.Index, preferRight);
        if (!hold.Held)
            return;

        if (hold.Evicted >= 0 && _papers[hold.Evicted] != null)
            Release(_papers[hold.Evicted], false);

        paper.SetExamined(true);
        paper.GetComponent<DeskDraggable>().GrabAtCentre = true;
        examiner.Hold(paper, hold.Slot);
        ApplyLive(paper);
        _readsToday++;
        RefreshHint();
        PaperExamined?.Invoke(paper.Index);
        HoldsChanged?.Invoke();
    }

    /// <summary>Every held paper goes back where it lay (the desk catcher, Escape).</summary>
    private void PutBackAll(bool instant)
    {
        if (_state == null)
            return;

        IReadOnlyList<int> back = _state.PutBackAll();
        foreach (int i in back)
            Release(_papers[i], instant);
        if (back.Count > 0)
            HoldsChanged?.Invoke();
    }

    /// <summary>A paper DeskPapers put back returns to where it lay (at once when <paramref name="instant"/>) and lands on top of the stack.</summary>
    private void Release(DeskDocument paper, bool instant)
    {
        if (paper == null)
            return;

        paper.GetComponent<DeskDraggable>().GrabAtCentre = false;
        void Landed()
        {
            paper.SetExamined(false);
            _stack.BringToFront(paper.Index);
            ApplyStack();
            ApplyLive(paper);
        }

        if (examiner != null)
            examiner.Release(paper, instant, Landed);
        else
            Landed();
        ApplyLive(paper);
    }

    /// <summary>A drag begins: a held paper first drops back onto the desk at once (it then follows the pointer by its centre); the dragged paper lifts above the stack.</summary>
    private void HandleDragBegan(DeskDraggable drag)
    {
        DeskDocument paper = drag.GetComponent<DeskDocument>();
        if (_state.PutBack(paper.Index))
        {
            Release(paper, true);
            HoldsChanged?.Invoke();
        }

        _dragged = paper.Index;
        ApplyStack();
    }

    /// <summary>DeskPapers decides the drop; the paper slides to the bed (scanning), back to its pick-up point (refused), or stays; it goes on top either way.</summary>
    private void HandleDragEnded(DeskDraggable drag, Vector3 released)
    {
        DeskDocument paper = drag.GetComponent<DeskDocument>();
        _dragged = -1;

        switch (_state.Drop(paper.Index, scanner.Contains(released)))
        {
            case DropOutcome.Scanning:
                Slide(paper, scanner.BedPoint);
                break;
            case DropOutcome.Refused:
                Slide(paper, drag.PickUpPosition);
                break;
        }

        _stack.BringToFront(paper.Index);
        ApplyStack();
        RefreshHint();
    }

    /// <summary>Slides a paper; it is inert while sliding, and its liveness is re-applied when it lands.</summary>
    private void Slide(DeskDocument paper, Vector3 target)
    {
        paper.SlideTo(target, config.paperSlideSeconds, () => ApplyLive(paper));
        ApplyLive(paper);
    }

    private void ApplyLiveAll()
    {
        foreach (DeskDocument paper in _papers)
            if (paper != null)
                ApplyLive(paper);
    }

    /// <summary>A held paper takes input (and raycasts) while held papers are allowed; a paper on the desk takes input while papers are allowed, DeskPapers lets it be dragged and it is not sliding, and is in the raycast while papers are allowed.</summary>
    private void ApplyLive(DeskDocument paper)
    {
        if (_state != null && _state.IsHeld(paper.Index))
            paper.SetLive(_heldLive, _heldLive);
        else
            paper.SetLive(_live && _state != null && _state.CanDrag(paper.Index) && !paper.IsSliding, _live);
    }

    /// <summary>Stack heights: one step per place from the desk (the bottom paper one step up); the dragged paper lifted above the whole stack (a held paper's sheet is the examiner's).</summary>
    private void ApplyStack()
    {
        float top = _papers.Count * config.paperStackStep;
        foreach (DeskDocument paper in _papers)
            if (paper != null)
                paper.SetLift(paper.Index == _dragged ? top + config.heldPaperLift : (_stack.IndexOf(paper.Index) + 1) * config.paperStackStep);
    }

    /// <summary>The day-1 scan note (DeskHints): re-evaluated at every hand-over, read, drop, finished scan and case end; it goes after the day's first read or scan.</summary>
    private void RefreshHint()
    {
        if (scanHint == null)
            return;

        bool paperOnDesk = _state != null && _state.OnDeskCount > 0;
        scanHint.gameObject.SetActive(config != null &&
                                      DeskHints.ScanHintVisible(config.scanHintKey, _day, config.scanHintUntilDay, _scansToday + _readsToday, paperOnDesk));
    }
}
