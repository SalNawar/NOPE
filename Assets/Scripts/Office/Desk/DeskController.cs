using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// One case's papers on the desk: hands papers over from the traveller's side
/// to the spawn slots, lets the player drag them (DeskDraggable), decides each
/// drop through DeskPapers (it stays, it scans on the scanner, or it slides back
/// to where it was picked up), runs the scan timer and raises ScanFinished,
/// stacks papers by height (each place in the stack lifts a sheet one step, a
/// held paper above them all), returns them at the decision, and shows the
/// day-1 scan note. A paper takes input only while BoothCoordinator allows
/// papers, DeskPapers lets it be dragged and it is not sliding; while the
/// office does not allow papers they take no raycasts at all (spec R38).
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
    private int _day;
    private int _scansToday;
    private int _handedOver;

    /// <summary>The paper being dragged, or -1.</summary>
    private int _held = -1;

    /// <summary>True when the desk and all its parts are wired; otherwise documents go straight to their windows (InvestigationUIController).</summary>
    public bool IsReachable =>
        surface != null && scanner != null && paperTemplate != null && paperRoot != null && handOverPoint != null && config != null;

    /// <summary>Raised when a scan finishes, with the paper's index (its window opens).</summary>
    public event Action<int> ScanFinished;

    private void Awake()
    {
        if (scanHint != null && config != null)
            scanHint.text = UiText.Get(config.scanHintKey);
        RefreshHint();
    }

    /// <summary>Only while a scan runs: advances it; a finished paper slides back to where it was picked up.</summary>
    private void Update()
    {
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

    /// <summary>Starts a day: nothing scanned yet (the scan note may show again on its days).</summary>
    public void BeginDay(int day)
    {
        _day = day;
        _scansToday = 0;
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
        _held = -1;

        foreach (int i in _state.ArrivalIndices)
            HandOver(i);
        RefreshHint();
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
        paper.GetComponent<Clickable>().onClick.AddListener(() => BringToFront(paper));

        _papers[i] = paper;
        _stack.Add(i);
        ApplyStack();

        Vector2[] slots = config.paperSpawnSlots;
        Vector3 target = slots != null && slots.Length > 0 ? surface.PointAt(slots[_handedOver % slots.Length]) : surface.transform.position;
        _handedOver++;
        Slide(paper, target);
        RefreshHint();
    }

    /// <summary>The decision: every paper goes back (a running scan is cancelled), slides inert and out of the raycast to the traveller's side and is destroyed.</summary>
    public void EndCase()
    {
        if (_state == null)
            return;

        _state.ReturnAll();
        foreach (DeskDocument paper in _papers)
        {
            if (paper == null)
                continue;

            DeskDocument leaving = paper;
            leaving.SetLive(false, false);
            leaving.SlideTo(handOverPoint.position, config.paperSlideSeconds, () => Destroy(leaving.gameObject));
        }

        _papers.Clear();
        _stack.Clear();
        _held = -1;
        RefreshHint();
    }

    /// <summary>Allows the papers input or not (BoothCoordinator); remembered for papers handed over later. Papers not allowed take no raycasts (spec R38).</summary>
    public void SetPapersLive(bool live)
    {
        _live = live;
        foreach (DeskDocument paper in _papers)
            if (paper != null)
                ApplyLive(paper);
    }

    private void HandleDragBegan(DeskDraggable drag)
    {
        DeskDocument paper = drag.GetComponent<DeskDocument>();
        _held = paper.Index;
        ApplyStack();
    }

    /// <summary>DeskPapers decides the drop; the paper slides to the bed (scanning), back to its pick-up point (refused), or stays; it goes on top either way.</summary>
    private void HandleDragEnded(DeskDraggable drag, Vector3 released)
    {
        DeskDocument paper = drag.GetComponent<DeskDocument>();
        _held = -1;

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

    /// <summary>A click without a drag brings the paper to the top.</summary>
    private void BringToFront(DeskDocument paper)
    {
        _stack.BringToFront(paper.Index);
        ApplyStack();
    }

    /// <summary>Slides a paper; it is inert while sliding, and its liveness is re-applied when it lands.</summary>
    private void Slide(DeskDocument paper, Vector3 target)
    {
        paper.SlideTo(target, config.paperSlideSeconds, () => ApplyLive(paper));
        ApplyLive(paper);
    }

    /// <summary>A paper takes input while papers are allowed, DeskPapers lets it be dragged and it is not sliding; it is in the raycast while papers are allowed.</summary>
    private void ApplyLive(DeskDocument paper) =>
        paper.SetLive(_live && _state != null && _state.CanDrag(paper.Index) && !paper.IsSliding, _live);

    /// <summary>Stack heights: one step per place from the desk (the bottom paper one step up); the held paper lifted above the whole stack.</summary>
    private void ApplyStack()
    {
        float top = _papers.Count * config.paperStackStep;
        foreach (DeskDocument paper in _papers)
            if (paper != null)
                paper.SetLift(paper.Index == _held ? top + config.heldPaperLift : (_stack.IndexOf(paper.Index) + 1) * config.paperStackStep);
    }

    /// <summary>The day-1 scan note (DeskHints): re-evaluated at every hand-over, drop, finished scan and case end.</summary>
    private void RefreshHint()
    {
        if (scanHint == null)
            return;

        bool paperOnDesk = _state != null && _state.OnDeskCount > 0;
        scanHint.gameObject.SetActive(config != null &&
                                      DeskHints.ScanHintVisible(config.scanHintKey, _day, config.scanHintUntilDay, _scansToday, paperOnDesk));
    }
}
