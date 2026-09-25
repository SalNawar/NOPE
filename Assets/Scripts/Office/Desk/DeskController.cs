using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// One case's papers on the desk: hands papers over from the traveller's side
/// to the spawn slots, lets the player drag them (DeskDraggable), decides each
/// drop through DeskPapers (it stays, it scans on the scanner, or it slides back
/// to where it was picked up), runs the scan timer and raises ScanFinished,
/// stacks papers by sorting order, returns them at the decision, and shows the
/// day-1 scan note. A paper takes input only while BoothCoordinator allows
/// papers, DeskPapers lets it be dragged and it is not sliding.
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

    /// <summary>The desk tuning (scan time, spawn slots, slide time, paper orders, the note).</summary>
    [SerializeField] private DeskConfigSO config;

    /// <summary>The case's papers by index (null until handed over).</summary>
    private readonly List<DeskDocument> _papers = new List<DeskDocument>();

    private readonly PaperStack _stack = new PaperStack();
    private IReadOnlyList<CaseDocument> _documents = Array.Empty<CaseDocument>();
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
            scanHint.text = config.scanHint;
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

    /// <summary>Starts a case's papers; the documents handed over on arrival slide onto the desk.</summary>
    public void BeginCase(IReadOnlyList<CaseDocument> docs)
    {
        _documents = docs ?? Array.Empty<CaseDocument>();
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
        paper.Bind(i, _documents[i]);

        DeskDraggable drag = paper.GetComponent<DeskDraggable>();
        drag.Init(surface);
        drag.DragBegan += HandleDragBegan;
        drag.DragEnded += HandleDragEnded;
        paper.GetComponent<Clickable>().onClick.AddListener(() => BringToFront(paper));

        _papers[i] = paper;
        _stack.Add(i);
        ApplyOrders();

        Vector2[] slots = config.paperSpawnSlots;
        Vector3 target = slots != null && slots.Length > 0 ? surface.PointAt(slots[_handedOver % slots.Length]) : surface.transform.position;
        _handedOver++;
        Slide(paper, target);
        RefreshHint();
    }

    /// <summary>The decision: every paper goes back (a running scan is cancelled), slides inert to the traveller's side and is destroyed.</summary>
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
            leaving.SetLive(false);
            leaving.SlideTo(handOverPoint.position, config.paperSlideSeconds, () => Destroy(leaving.gameObject));
        }

        _papers.Clear();
        _stack.Clear();
        _held = -1;
        RefreshHint();
    }

    /// <summary>Allows the papers input or not (BoothCoordinator); remembered for papers handed over later.</summary>
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
        paper.SetOrder(config.heldPaperOrder);
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
        ApplyOrders();
        RefreshHint();
    }

    /// <summary>A click without a drag brings the paper to the top.</summary>
    private void BringToFront(DeskDocument paper)
    {
        _stack.BringToFront(paper.Index);
        ApplyOrders();
    }

    /// <summary>Slides a paper; it is inert while sliding, and its liveness is re-applied when it lands.</summary>
    private void Slide(DeskDocument paper, Vector3 target)
    {
        paper.SlideTo(target, config.paperSlideSeconds, () => ApplyLive(paper));
        ApplyLive(paper);
    }

    /// <summary>A paper takes input while papers are allowed, DeskPapers lets it be dragged and it is not sliding.</summary>
    private void ApplyLive(DeskDocument paper) =>
        paper.SetLive(_live && _state != null && _state.CanDrag(paper.Index) && !paper.IsSliding);

    /// <summary>Stack order: the base order plus the paper's place; the held paper above them all.</summary>
    private void ApplyOrders()
    {
        foreach (DeskDocument paper in _papers)
            if (paper != null)
                paper.SetOrder(paper.Index == _held ? config.heldPaperOrder : config.paperBaseOrder + _stack.IndexOf(paper.Index));
    }

    /// <summary>The day-1 scan note (DeskHints): re-evaluated at every hand-over, drop, finished scan and case end.</summary>
    private void RefreshHint()
    {
        if (scanHint == null)
            return;

        bool paperOnDesk = _state != null && _state.OnDeskCount > 0;
        scanHint.gameObject.SetActive(config != null &&
                                      DeskHints.ScanHintVisible(config.scanHint, _day, config.scanHintUntilDay, _scansToday, paperOnDesk));
    }
}
