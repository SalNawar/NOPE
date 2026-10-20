using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// One case's papers on the desk, Papers, Please's way (Saleh 2026-10-06:
/// "copy the controls of Papers, Please 1:1"): papers handed over slide from
/// the traveller's side onto the counter (DeskCounter: a row of spots), small;
/// left-press and drag moves a document (DeskDraggable), the only way to move
/// it, in either view; on the counter it is small, on the desk full size
/// (DeskDocument.SetZone), but a paper keeps its size: a grab and a carry
/// never change it, and it shows at full size only on the desk while the
/// reading view is on, easing back to its normal size (the counter's) when
/// the view returns (DeskPapers.ShownFullSize; Saleh's 1016a playtest:
/// "documents should stay their size, if we are zooming then it should be
/// only when you are focused on the table"): the slim counter only lights
/// while the pointer is over it (DeskCounter.Show's hover). Every paper lies
/// upright toward the chair (DeskView.LevelForward); dropped on the scanner
/// it eases square onto its bed (DeskScanner.BedFacing) and, scanned, back
/// upright as it slides off (Saleh's 1016a playtest: "documents have weird
/// orientation when you scan them"). A document dropped on the desk brings the reading view by itself
/// (DeskView.TiltIn). Each drop is decided through DeskPapers: it stays in
/// the zone it was dropped in, it scans on the scanner, it slides back to
/// where it was picked up (a busy scanner; or a paper from the desk dropped
/// on the counter before the passport carries its verdict, with the note
/// "Stamp the passport first": documents can only be returned after
/// stamping), or (any paper dropped on the counter once the passport carries
/// its verdict) the papers are handed back (DeskStampTray.HandBack: the case
/// is decided). A
/// left-click on a document routes through PaperClicks: on the counter it
/// goes to the desk to be read (as if dragged there), on the desk it comes to
/// the top; in inspect mode (SetInspecting) a click on a value picks it for
/// comparison (FieldPicked) and nothing else does. A right-click or Esc
/// mid-drag cancels the drag (CancelDrag, ControlRules: the paper slides back
/// to where it was picked up). Papers stack by height (each place in the
/// stack lifts a sheet one step, a dragged paper above them all; the
/// rulebook takes a place in the stack too, so nothing lies at the desk's
/// own height and nothing prints through what lies on it). A document
/// takes input only while BoothCoordinator allows the papers, DeskPapers lets
/// it be dragged and it is not sliding; papers not allowed take no raycasts
/// at all (spec R38). The day's scanner upgrades (ScannerDay, from the
/// day-start snapshot: BeginDay) change the scan (the PC redesign SC2-SC4):
/// with the Auto-Feed a click on its tray puts every paper not scanned yet in
/// DeskPapers' queue (FeedStack) and each, once landed and not dragged,
/// slides onto the scanner by itself, in hand-over order, one at a time
/// (FeedScanner); with the Analysis a scan by hand
/// takes the analysis pass (the longer scan), once per document
/// (DeskPapers.Pass), and ScanFinished says which pass it was. No paper is
/// left behind the scanner (ScannerClearance, the desk-first redesign, item
/// 4). The placeholder scanner shows its tray and lamp for the owned upgrades
/// (SC6). At the decision every paper goes back to the traveller.
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

    /// <summary>The desk tuning (scan time, slide time, the stack's heights, the zones' sizes, the photo's tint, the note).</summary>
    [SerializeField] private DeskConfigSO config;

    /// <summary>The counter (optional: without it every paper lies on the desk and none is handed back by a drop).</summary>
    [SerializeField] private DeskCounter counter;

    /// <summary>The reading view (optional): a document dropped on the desk tilts into it, and a document sent to the desk by a click lands where it shows.</summary>
    [SerializeField] private DeskView deskView;

    /// <summary>The stamps (optional): the passport's verdict, and the hand-back when the stamped passport is dropped on the counter.</summary>
    [SerializeField] private DeskStampTray stamps;

    /// <summary>The rulebook (optional): it lies in the papers' stack, over or under each paper as they were last touched, a dragged one above them all (Saleh 2026-10-06: "documents on desk like the folder with the rules are clipping with the desk").</summary>
    [SerializeField] private DeskRulebook rulebook;

    /// <summary>The desk machines (optional; the desk machines spec §1): the chip reader a paper is dropped on, the UV torch, each paper's UV layer.</summary>
    [SerializeField] private DeskHardware hardware;

    /// <summary>The desk machines (null when none are wired).</summary>
    public DeskHardware Hardware => hardware;

    /// <summary>The papers in the chip reader (inert until it gives them back).</summary>
    private readonly HashSet<int> _inReader = new HashSet<int>();

    /// <summary>The rulebook's place in the stack (papers are 0 and up).</summary>
    private const int RulebookId = -1;

    /// <summary>True while the rulebook is dragged (it lifts above the stack).</summary>
    private bool _rulebookDragged;

    /// <summary>The case's papers by index (null until handed over).</summary>
    private readonly List<DeskDocument> _papers = new List<DeskDocument>();

    private readonly PaperStack _stack = new PaperStack();
    private IReadOnlyList<CaseDocument> _documents = Array.Empty<CaseDocument>();
    private IReadOnlyList<DocumentForm> _forms = Array.Empty<DocumentForm>();
    private TravellerLook _look;
    private CharacterArt _art;
    private DeskPapers _state;
    private bool _live;
    private bool _inspecting;
    private int _day;
    private int _scansToday;
    private int _readsToday;
    private int _handedOver;

    /// <summary>The day's scanner upgrades (BeginDay).</summary>
    private ScannerDay _scanners;

    /// <summary>The pair the case's last analysis pass marked (FlagAnalysis), which the beacon's click shows; null before one.</summary>
    private AnalysisMark? _flagged;

    /// <summary>The Auto-Feed's readiness rule, made once (no delegate per frame).</summary>
    private Func<int, bool> _landed;

    /// <summary>The papers dropped on the busy scanner, waiting by the glass (drop and go): once scanned they go back to where they were picked up, not to where they waited.</summary>
    private readonly System.Collections.Generic.HashSet<int> _waiting = new System.Collections.Generic.HashSet<int>();

    /// <summary>The paper being dragged, or -1.</summary>
    private int _dragged = -1;

    /// <summary>True while the dragged paper's pointer is over the counter (its strip lights).</summary>
    private bool _overCounter;

    /// <summary>The office view's level right and forward on the desk (SetScannerView): the frame ScannerClearance works in.</summary>
    private Vector3 _viewRight = Vector3.right, _viewForward = Vector3.forward;

    /// <summary>How deep behind the scanner its body hides a paper from the office camera, in metres (SetScannerView).</summary>
    private float _scannerShadow;

    /// <summary>The reading spots across the reading view (viewport x), each next paper sent to the desk by a click taking the next: right of the rulebook folder's page, which fills the view's left.</summary>
    private static readonly float[] ReadingSpots = { 0.62f, 0.78f, 0.7f };

    /// <summary>The reading spots' height on the screen in the reading view (viewport y).</summary>
    private const float ReadingSpotY = 0.42f;

    /// <summary>True when the desk and all its parts are wired; otherwise documents reach the PC when handed over (InvestigationUIController).</summary>
    public bool IsReachable =>
        surface != null && scanner != null && paperTemplate != null && paperRoot != null && handOverPoint != null && config != null;

    /// <summary>True while a document is being dragged (a right-click or Esc cancels the drag first: ControlRules).</summary>
    public bool IsDragging => _dragged >= 0;

    /// <summary>Raised when a scan finishes, with the paper's index and its pass (ScanPass: the analysis pass for a first scan by hand with the Analysis Scanner): its scanned copy reaches the PC (the Investigation app's Documents tab), which marks the papers after an analysis.</summary>
    public event Action<int, ScanPass> ScanFinished;

    /// <summary>Raised when a value of a document is picked in inspect mode: the paper's index, the field's row and where it lights up.</summary>
    public event Action<int, DocumentRow, ICompareHighlight> FieldPicked;

    /// <summary>Raised when a document lands on the desk, full size, to be read (dragged there or sent by a click), with its index (the steps checklist's "read").</summary>
    public event Action<int> PaperExamined;

    private void Awake()
    {
        _landed = Landed;
        if (scanHint != null && config != null)
            scanHint.text = UiText.Get(config.scanHintKey);
        if (stamps != null)
            stamps.Changed += ShowCounter;
        if (deskView != null)
            deskView.Changed += Focus;
        if (rulebook != null && config != null)
            rulebook.ShowDepth(config);
        if (rulebook != null && rulebook.Drag != null)
        {
            rulebook.Drag.DragBegan += RulebookLifted;
            rulebook.Drag.Dragged += RulebookDragged;
            rulebook.Drag.DragEnded += RulebookDropped;
            rulebook.Drag.DragCancelled += RulebookLifted;
        }
        ResetStack();
        if (config != null)
            ApplyStack();
        RefreshHint();
        if (scanner != null)
        {
            scanner.TrayClicked += TrayClicked;
            scanner.BeaconClicked += BeaconClicked;
        }
    }

    private void TrayClicked() => FeedStack();

    private void BeaconClicked() => ShowFlagged();

    private void OnDestroy()
    {
        if (scanner != null)
        {
            scanner.TrayClicked -= TrayClicked;
            scanner.BeaconClicked -= BeaconClicked;
        }
        if (stamps != null)
            stamps.Changed -= ShowCounter;
        if (deskView != null)
            deskView.Changed -= Focus;
        if (rulebook != null && rulebook.Drag != null)
        {
            rulebook.Drag.DragBegan -= RulebookLifted;
            rulebook.Drag.Dragged -= RulebookDragged;
            rulebook.Drag.DragEnded -= RulebookDropped;
            rulebook.Drag.DragCancelled -= RulebookLifted;
        }
    }

    /// <summary>The reading view turned on or off: every paper on the desk eases to its reading size or back to its normal size (a carried one keeps its size).</summary>
    private void Focus()
    {
        foreach (DeskDocument paper in _papers)
            if (paper != null)
                paper.SetFocused(deskView.IsOn);
    }

    /// <summary>A paper's rotation lying upright toward the chair: its top away from it, along the office view's level forward.</summary>
    private Quaternion Upright => Quaternion.LookRotation(deskView != null ? deskView.LevelForward : Vector3.forward, Vector3.up);

    /// <summary>The stack with no case paper: the rulebook alone.</summary>
    private void ResetStack()
    {
        _stack.Clear();
        _stack.Add(RulebookId);
    }

    /// <summary>The rulebook's drag begins (lifted above the stack) or is cut short (back on top of it).</summary>
    private void RulebookLifted(DeskDraggable drag)
    {
        _rulebookDragged = drag.IsDragging;
        if (_rulebookDragged)
            MeasureCasings();
        else
            rulebook.SetCarryFloor(0f);
        _stack.BringToFront(RulebookId);
        if (config != null)
            ApplyStack();
    }

    /// <summary>The dragged rulebook keeps over any machine's casing under it (CarryFloor).</summary>
    private void RulebookDragged(DeskDraggable drag, Vector3 point)
    {
        if (config != null)
            rulebook.SetCarryFloor(CarryFloor(rulebook.transform.position, RulebookGrow(), rulebook.Thickness));
    }

    /// <summary>
    /// The rulebook dropped: it lies on top of the stack; dropped on a machine
    /// (the scanner or the chip reader: neither takes it, MachineIntake), the
    /// machine says no and the folder goes back over its casing to where it
    /// was picked up (Saleh's 1018a playtest: "if is the wrong document it
    /// shouldnt go through the machience").
    /// </summary>
    private void RulebookDropped(DeskDraggable drag, Vector3 released)
    {
        _rulebookDragged = false;
        IPaperDock dock = DockAt(released);
        if (config != null && dock != null && !MachineIntake.Accepts(dock.Machine, MachineFeed.Folder))
        {
            dock.Refuse();
            Vector3 from = rulebook.CarriedFrom;
            List<Vector3> way = Way(from, drag.PickUpPosition, RulebookGrow());
            rulebook.Refused(way, WaySeconds(from, way), config.refuseShake);
        }
        RulebookLifted(drag);
    }

    /// <summary>Half the rulebook's diagonal on the desk (metres): how far its edges reach from its root.</summary>
    private float RulebookGrow() => TryFootprint(rulebook.transform, out DeskRect folder) ? Mathf.Sqrt(folder.Width * folder.Width + folder.Height * folder.Height) / 2f : 0.2f;

    /// <summary>Advances a running scan (a finished paper slides back to where it was picked up) or feeds the idle scanner its next queued paper (the Auto-Feed Scanner).</summary>
    private void Update()
    {
        if (_state == null)
            return;
        if (!_state.ScannerBusy)
        {
            scanner.Sweep(-1f);
            FeedScanner();
            return;
        }

        // the scan waits, the lid open, until its paper lies on the bed
        int onBed = _state.Scanning;
        if (onBed >= 0 && onBed < _papers.Count && _papers[onBed] != null && _papers[onBed].IsSliding)
        {
            scanner.Sweep(-1f);
            return;
        }
        ScanPass pass = _state.Pass;
        int done = _state.Tick(Time.deltaTime);
        scanner.Sweep(_state.ScannerBusy ? _state.Progress : -1f);
        if (done < 0)
            return;

        DeskDocument paper = _papers[done];
        Undock(paper);
        Slide(paper, ClearOfBlockers(paper, paper.Drag.PickUpPosition), Upright);
        scanner.Pulse();
        _scansToday++;
        RefreshHint();
        ScanFinished?.Invoke(done, pass);
    }

    /// <summary>
    /// The feed: while the scanner is idle, the next queued paper that lies
    /// still on the desk slides onto the bed and scans (DeskPapers.FeedNext:
    /// queue order, one at a time): a paper the Auto-Feed Scanner (SC3) took
    /// in goes back to where it lay, one dropped on the busy scanner (drop and
    /// go, the scanner app spec §1) back to where it was picked up.
    /// </summary>
    private void FeedScanner()
    {
        int next = _state.FeedNext(_landed);
        if (next < 0)
            return;

        // An Auto-Fed paper goes back to where it lay once scanned, as a dragged paper goes back to where it was picked up.
        if (!_waiting.Remove(next))
        {
            _papers[next].Drag.RememberPosition();
            if (_scanners.AutoFeed && scanner != null)
                scanner.Fed(); // the Auto-Feed tray's rollers take it in
        }
        StartScan(_papers[next]);
        _stack.BringToFront(next);
        ApplyStack();
    }

    /// <summary>True when paper <paramref name="i"/> lies still on the desk: handed over, landed and not being dragged.</summary>
    private bool Landed(int i) => _papers[i] != null && !_papers[i].IsSliding && _dragged != i;

    /// <summary>
    /// The office view the scanner hides papers from (the office binder, from
    /// the art office's camera): its level forward on the desk and the depth
    /// behind the scanner its body hides (ScannerClearance.Shadow). From then
    /// on no paper is left in the scanner's blocked area (the desk-first
    /// redesign, item 4).
    /// </summary>
    public void SetScannerView(Vector3 levelForward, float shadow)
    {
        levelForward = Vector3.ProjectOnPlane(levelForward, Vector3.up);
        if (levelForward.sqrMagnitude < 1e-6f)
            return;
        _viewForward = levelForward.normalized;
        _viewRight = Vector3.Cross(Vector3.up, _viewForward);
        _scannerShadow = Mathf.Max(0f, shadow);
    }

    /// <summary>An analysis pass ended (CaseDocumentsPresenter) with the pair it marked (null: none): the Analysis beacon on the scanner pops, and its click shows that pair (ShowFlagged; a pass that marks nothing keeps the case's last pair).</summary>
    public void FlagAnalysis(AnalysisMark? mark)
    {
        if (mark.HasValue)
            _flagged = mark;
        if (scanner != null)
            scanner.Flag(mark.HasValue);
    }

    /// <summary>
    /// The Auto-Feed tray clicked (Saleh 2026-10-18, demo 1018a note 14: "feed a
    /// stack in one go"; the desk machines spec §10): every paper handed over
    /// and not scanned yet this case feeds through the scanner by itself, one
    /// after another in hand-over order (DeskPapers.FeedStack), each back
    /// where it lay; the tray whirrs either way. Returns how many papers it
    /// took (0: nothing left to scan). Nothing without the Auto-Feed Scanner
    /// installed today.
    /// </summary>
    public int FeedStack()
    {
        if (_state == null || !_scanners.AutoFeed)
            return 0;
        int fed = _state.FeedStack().Count;
        if (scanner != null)
            scanner.Stacked(fed);
        return fed;
    }

    /// <summary>
    /// The Analysis beacon clicked (the desk machines spec §10: "click it to
    /// jump to the flagged pair"): the two papers of the pair the case's last
    /// analysis marked come to the top of the stack, each marked box tinted
    /// (DeskConfigSO.analysisFlagTint, for the case, under a logged
    /// difference's mark, which it never covers), and the beacon pops lit
    /// again. Nothing is picked or logged: the clerk still compares the two.
    /// False (the beacon pops unlit) before any pair is marked or with neither
    /// paper on the desk.
    /// </summary>
    public bool ShowFlagged()
    {
        bool shown = false;
        if (_state != null && _flagged.HasValue)
        {
            AnalysisMark m = _flagged.Value;
            foreach ((int doc, int field) in new[] { (m.DocA, m.FieldA), (m.DocB, m.FieldB) })
            {
                DeskDocument paper = doc >= 0 && doc < _papers.Count ? _papers[doc] : null;
                if (paper == null || !_state.CanDrag(doc))
                    continue;
                int slot = paper.SlotOfRow(field);
                if (slot >= 0 && !paper.Marked(slot) && config != null)
                    paper.SetMarked(slot, config.analysisFlagTint);
                _stack.BringToFront(doc);
                shown = true;
            }
            if (shown)
                ApplyStack();
        }
        if (scanner != null)
            scanner.Flag(shown);
        return shown;
    }

    /// <summary>Starts a day with its scanner upgrades (<paramref name="scanners"/>: the day-start snapshot's, so a scanner bought tonight works tomorrow): nothing read or scanned yet (the scan note may show again on its days); the placeholder scanner shows the owned upgrades' parts.</summary>
    public void BeginDay(int day, ScannerDay scanners)
    {
        _day = day;
        _scanners = scanners;
        _scansToday = 0;
        _readsToday = 0;
        if (scanner != null)
            scanner.ShowUpgrades(scanners);
        RefreshHint();
    }

    /// <summary>Starts a case's papers, each printing its form (<paramref name="forms"/>, by paper; a photo document shows <paramref name="look"/>); the documents handed over on arrival slide onto the counter; the first of them is the passport (the stamps' verdict is its).</summary>
    public void BeginCase(IReadOnlyList<CaseDocument> docs, IReadOnlyList<DocumentForm> forms, TravellerLook look, CharacterArt art)
    {
        Sounds.Play(SoundCues.CallNext);
        _documents = docs ?? Array.Empty<CaseDocument>();
        _forms = forms ?? Array.Empty<DocumentForm>();
        _look = look;
        _art = art;
        _state = new DeskPapers(_documents, config.scanSeconds, config.analysisScanSeconds, _scanners);
        _flagged = null;
        _papers.Clear();
        for (int i = 0; i < _state.Count; i++)
            _papers.Add(null);
        ResetStack();
        _handedOver = 0;
        _dragged = -1;

        int passport = _state.ArrivalIndices.Count > 0 ? _state.ArrivalIndices[0] : -1;
        foreach (int i in _state.ArrivalIndices)
            HandOver(i);
        if (stamps != null)
            stamps.BeginCase(passport);
        ShowCounter();
        RefreshHint();
    }

    /// <summary>
    /// Hands paper <paramref name="i"/> over: a clone of the template appears at
    /// the hand-over point, small, on top of the stack, and slides to the next
    /// spot on the counter (reused in turn). Nothing happens unless DeskPapers
    /// agrees (a paper is handed over once).
    /// </summary>
    public void HandOver(int i)
    {
        if (_state == null || !_state.HandOver(i))
            return;

        DeskDocument paper = Instantiate(paperTemplate, paperRoot);
        paper.transform.SetPositionAndRotation(handOverPoint.position, Upright);
        paper.gameObject.SetActive(true);
        paper.Bind(i, _documents[i], i < _forms.Count ? _forms[i] : null, config);
        DocumentForm form = i < _forms.Count ? _forms[i] : null;
        bool photo = _documents[i] != null && (_documents[i].showsPhoto || (form != null && ArtLayout.ShowsPhoto(form.Spec, form.Data)));
        paper.ShowPhoto(photo ? _look : null, _art, config.travellerTint);
        paper.SetFocused(deskView != null && deskView.IsOn);
        paper.SetZone(DeskZone.Counter, true);
        paper.SetInspecting(_inspecting);

        DeskDraggable drag = paper.Drag;
        drag.Init(surface);
        drag.DragBegan += HandleDragBegan;
        drag.Dragged += HandleDragged;
        drag.DragEnded += HandleDragEnded;
        drag.DragCancelled += HandleDragCancelled;
        paper.Clicked += HandlePaperClicked;

        _papers[i] = paper;
        _stack.Add(i);
        ApplyStack();
        if (hardware != null)
            hardware.PaperAdded(paper, _documents[i] != null ? _documents[i].formNumber : null);

        Vector3 target = counter != null ? counter.Spot(_handedOver) : surface.transform.position;
        _handedOver++;
        Slide(paper, ClearOfBlockers(paper, target));
        RefreshHint();
    }

    /// <summary>The decision (<paramref name="decision"/>): a drag is cancelled, then every paper still on the desk goes back (a running scan is cancelled) wearing the verdict's ink mark (DeskDocument.ShowVerdict, APPROVED or DENIED; not when the player stamped the passport: their mark is the verdict's; none for a detention), slides inert and out of the raycast to the traveller's side and is destroyed.</summary>
    public void EndCase(DeskStamp decision)
    {
        if (_state == null)
            return;
        bool stamped = stamps != null && stamps.HasVerdict;
        if (stamps != null)
            stamps.EndCase();
        SendBack(stamped || decision == DeskStamp.Detained ? DeskStamp.None : decision);
    }

    /// <summary>Every paper on the desk goes back to the traveller (the state's papers all returned), inked with <paramref name="ink"/> (None: no ink), inert, sliding to the hand-over point, then destroyed.</summary>
    private void SendBack(DeskStamp ink)
    {
        _state.ReturnAll();
        _waiting.Clear();
        _inReader.Clear();
        if (hardware != null)
            hardware.EndCase();
        if (scanner != null)
            scanner.Sweep(-1f);
        foreach (DeskDocument paper in _papers)
        {
            if (paper == null)
                continue;

            DeskDocument leaving = paper;
            leaving.Clicked -= HandlePaperClicked;
            leaving.SetLive(false, false, false);
            leaving.SetZone(DeskZone.Counter, false);
            if (ink != DeskStamp.None && stamps != null)
                leaving.ShowVerdict(stamps.Impression(ink == DeskStamp.Approved));
            leaving.Carry(Way(leaving.CarriedFrom, handOverPoint.position, Grow(leaving)), leaving.transform.rotation, config.paperSlideSeconds, () => Destroy(leaving.gameObject));
        }

        _papers.Clear();
        ResetStack();
        _dragged = -1;
        ShowCounter();
        RefreshHint();
    }

    /// <summary>Allows the papers on the desk input or not (BoothCoordinator: BoothRules.PapersLive); remembered for papers handed over later. Papers not allowed take no raycasts (spec R38); a drag running when they lose it is cut short (DeskDraggable: the paper slides back).</summary>
    public void SetPapersLive(bool live)
    {
        _live = live;
        foreach (DeskDocument paper in _papers)
            if (paper != null)
                ApplyLive(paper);
    }

    /// <summary>Inspect mode on or off (BoothCoordinator, from DeskInspect): every value on the papers tints as comparable and a left-click picks one; off, a click never compares.</summary>
    public void SetInspecting(bool inspecting)
    {
        _inspecting = inspecting;
        if (hardware != null)
            hardware.SetInspecting(inspecting);
        foreach (DeskDocument paper in _papers)
            if (paper != null)
                paper.SetInspecting(inspecting);
    }

    /// <summary>A right-click or Esc mid-drag (ControlRules.BackOut): the drag is cancelled and the paper slides back to where it was picked up; false when nothing is dragged.</summary>
    public bool CancelDrag()
    {
        if (_dragged < 0 || _dragged >= _papers.Count || _papers[_dragged] == null)
            return false;
        _papers[_dragged].Drag.Cancel();
        return true;
    }

    /// <summary>Where the box of document <paramref name="document"/>'s row <paramref name="rowIndex"/> is on its paper, as world bounds (a match line meets it there); false when the paper is not on the desk or prints no such box.</summary>
    public bool TryFieldBounds(int document, int rowIndex, out Bounds bounds)
    {
        bounds = default;
        DeskDocument paper = document >= 0 && document < _papers.Count ? _papers[document] : null;
        return paper != null && paper.TryBoundsOf(paper.SlotOfRow(rowIndex), out bounds);
    }

    /// <summary>Marks the box of document <paramref name="document"/>'s row <paramref name="rowIndex"/> as a clear mistake found, in <paramref name="colour"/>, for the rest of the case (the desk-first redesign, item 11: "highlight clear mistakes"); nothing when the paper is not on the desk.</summary>
    public void MarkField(int document, int rowIndex, Color colour)
    {
        DeskDocument paper = document >= 0 && document < _papers.Count ? _papers[document] : null;
        if (paper != null)
            paper.SetMarked(paper.SlotOfRow(rowIndex), colour);
    }

    /// <summary>A left-click on a document (PaperClicks): in inspect mode a value is picked; otherwise a document on the counter goes to the desk to be read, one on the desk comes to the top.</summary>
    private void HandlePaperClicked(DeskDocument paper, int slot)
    {
        if (_state == null || !_state.CanDrag(paper.Index))
            return;

        switch (PaperClicks.Decide(_inspecting, slot >= 0 && slot < paper.SlotCount, _state.ZoneOf(paper.Index)))
        {
            case PaperClickAction.Pick:
                FieldPicked?.Invoke(paper.Index, paper.FieldAt(slot), paper.SlotHighlight(slot));
                break;
            case PaperClickAction.Read:
                ToDesk(paper);
                break;
            case PaperClickAction.Front:
                _stack.BringToFront(paper.Index);
                ApplyStack();
                break;
        }
    }

    /// <summary>A document on the counter clicked: it slides to the next reading spot (where the reading view shows the desk, else the desk's centre), full size, comes to the top and is read (as if dragged there).</summary>
    private void ToDesk(DeskDocument paper)
    {
        if (_state.Drop(paper.Index, false, DeskZone.Desk) != DropOutcome.Stays)
            return;
        int onDesk = 0;
        foreach (DeskDocument other in _papers)
            if (other != null && other != paper && _state.ZoneOf(other.Index) == DeskZone.Desk)
                onDesk++;
        Vector3 spot = surface.transform.position;
        if (deskView != null && deskView.TryViewPoint(new Vector2(ReadingSpots[onDesk % ReadingSpots.Length], ReadingSpotY), surface.transform.position.y, out Vector3 shown))
            spot = surface.Clamp(shown);
        spot = Spread(paper, spot);
        _stack.BringToFront(paper.Index);
        ApplyStack();
        paper.SetZone(DeskZone.Desk, false);
        spot = ClearOfBlockers(paper, spot);
        _restAt[paper.Index] = spot;
        Slide(paper, spot);
        Read(paper);
    }

    /// <summary>Where each paper sent to the desk by a click was laid (its spread's spot), while it slides there.</summary>
    private readonly Dictionary<int, Vector3> _restAt = new Dictionary<int, Vector3>();

    /// <summary>What covering the rulebook folder costs to the spread (a little more than a paper: its page is read beside the papers, but the reading view has no room to keep it clear of them all).</summary>
    private const float RulebookSpreadWeight = 1.5f;

    /// <summary>What lying under the stamp rack costs to the spread (far more than a paper: the rack stands over the papers' top right in the reading view, the desk layout spec §2, and a paper under it hides under the daters).</summary>
    private const float RackSpreadWeight = 4f;

    /// <summary>
    /// Where <paramref name="paper"/>, sent to the desk, lies (PaperSpread;
    /// Saleh's playtest 2026-10-07: "documents overlap"): inside the reading
    /// view's free area (DeskConfigSO.readingArea), covering as little as it can
    /// of the papers on the desk (their headers, photos and visa boxes least of
    /// all), the rulebook folder and the stamp rack, nearest <paramref name="prefer"/>
    /// (the reading spots' fan) among equal places; all in the papers' own frame
    /// on the desk (their sheets' right and top, as the reading view shows them).
    /// <paramref name="prefer"/> itself without the reading view.
    /// </summary>
    private Vector3 Spread(DeskDocument paper, Vector3 prefer)
    {
        if (surface == null || deskView == null || paper.Sheet == null)
            return prefer;
        _spreadRight = Vector3.ProjectOnPlane(paper.Sheet.right, Vector3.up).normalized;
        _spreadTop = Vector3.ProjectOnPlane(paper.Sheet.up, Vector3.up).normalized;
        if (_spreadRight == Vector3.zero || _spreadTop == Vector3.zero || !TryReadingArea(out DeskRect area))
            return prefer;
        Vector2 size = paper.SizeIn(DeskZone.Desk);
        var taken = new List<SpreadTaken>();
        foreach (DeskDocument other in _papers)
        {
            if (other == null || other == paper || _state == null || _state.ZoneOf(other.Index) != DeskZone.Desk)
                continue;
            Vector3 at = other.IsSliding && _restAt.TryGetValue(other.Index, out Vector3 rest) ? rest : other.transform.position;
            taken.Add(Taken(other, at, 1f));
        }
        if (rulebook != null && rulebook.gameObject.activeInHierarchy && TryFootprint(rulebook.transform, out DeskRect folder))
            taken.Add(new SpreadTaken(folder, null, RulebookSpreadWeight));
        if (stamps != null && stamps.Rack != null && stamps.Rack.gameObject.activeInHierarchy && TryFootprint(stamps.Rack, out DeskRect rack))
            taken.Add(new SpreadTaken(rack, null, RackSpreadWeight));
        (float px, float py) = SpreadFrame(prefer);
        (float x, float y) = PaperSpread.Place(area, size.x, size.y, taken, px, py);
        LastSpread = $"area {area.CentreX:0.000},{area.CentreY:0.000} {area.Width:0.000}x{area.Height:0.000}; paper {size.x:0.000}x{size.y:0.000} at {x:0.000},{y:0.000}; taken {taken.Count}, cost {PaperSpread.Cost(new DeskRect(x, y, size.x, size.y), taken):0.0000}";
        return surface.Clamp(surface.transform.position + _spreadRight * x + _spreadTop * y);
    }

    /// <summary>The last spread's numbers (the probes report them).</summary>
    public string LastSpread { get; private set; }

    /// <summary>The spread's frame on the desk: the papers' right and top, level.</summary>
    private Vector3 _spreadRight = Vector3.right, _spreadTop = Vector3.forward;

    /// <summary>A world point in the spread's frame (metres from the desk's centre).</summary>
    private (float x, float y) SpreadFrame(Vector3 world)
    {
        Vector3 d = world - surface.transform.position;
        return (Vector3.Dot(d, _spreadRight), Vector3.Dot(d, _spreadTop));
    }

    /// <summary>A paper lying at <paramref name="at"/> as the spread sees it: its footprint at its zone's size, its header, photo and visa box, covering it costing <paramref name="weight"/>.</summary>
    private SpreadTaken Taken(DeskDocument paper, Vector3 at, float weight)
    {
        Vector2 size = paper.SizeIn(paper.Zone);
        (float x, float y) = SpreadFrame(at);
        var shares = new List<(float, float, float, float)>();
        foreach (Rect r in paper.KeyShares)
            shares.Add((r.x, r.y, r.width, r.height));
        return new SpreadTaken(new DeskRect(x, y, size.x, size.y), PaperSpread.Keys(x, y, size.x, size.y, shares), weight);
    }

    /// <summary>The footprint on the desk (the spread's frame) of everything drawn under <paramref name="root"/>.</summary>
    private bool TryFootprint(Transform root, out DeskRect footprint)
    {
        footprint = default;
        bool any = false;
        Bounds all = default;
        foreach (Renderer r in root.GetComponentsInChildren<Renderer>(false))
        {
            if (!r.enabled)
                continue;
            if (!any)
                all = r.bounds;
            else
                all.Encapsulate(r.bounds);
            any = true;
        }
        if (!any)
            return false;
        Vector3 min = all.min, max = all.max;
        float l = float.MaxValue, rr = float.MinValue, n = float.MaxValue, f = float.MinValue;
        foreach (Vector3 c in new[] { new Vector3(min.x, 0f, min.z), new Vector3(max.x, 0f, min.z), new Vector3(min.x, 0f, max.z), new Vector3(max.x, 0f, max.z) })
        {
            (float x, float y) = SpreadFrame(new Vector3(c.x, surface.transform.position.y, c.z));
            l = Mathf.Min(l, x);
            rr = Mathf.Max(rr, x);
            n = Mathf.Min(n, y);
            f = Mathf.Max(f, y);
        }
        footprint = new DeskRect((l + rr) / 2f, (n + f) / 2f, rr - l, f - n);
        return true;
    }

    /// <summary>The reading view's free area on the desk (DeskConfigSO.readingArea's viewport corners on the desk plane, in the spread's frame: the largest rectangle inside them); false without the reading view.</summary>
    private bool TryReadingArea(out DeskRect area)
    {
        area = default;
        Rect v = config.readingArea;
        float h = surface.transform.position.y;
        if (!deskView.TryViewPoint(new Vector2(v.xMin, v.yMin), h, out Vector3 bl) || !deskView.TryViewPoint(new Vector2(v.xMax, v.yMin), h, out Vector3 br)
            || !deskView.TryViewPoint(new Vector2(v.xMin, v.yMax), h, out Vector3 tl) || !deskView.TryViewPoint(new Vector2(v.xMax, v.yMax), h, out Vector3 tr))
            return false;
        (float blx, float bly) = SpreadFrame(bl);
        (float brx, float bry) = SpreadFrame(br);
        (float tlx, float tly) = SpreadFrame(tl);
        (float trx, float tr_y) = SpreadFrame(tr);
        float left = Mathf.Max(blx, tlx), right = Mathf.Min(brx, trx), near = Mathf.Max(bly, bry), far = Mathf.Min(tly, tr_y);
        if (right <= left || far <= near)
            return false;
        area = new DeskRect((left + right) / 2f, (near + far) / 2f, right - left, far - near);
        return true;
    }

    /// <summary>A document landed on the desk, full size: it counts as read (the day-1 note goes; the steps' "read"), and the reading view comes (DeskView.TiltIn: only while the desk takes input).</summary>
    private void Read(DeskDocument paper)
    {
        _readsToday++;
        RefreshHint();
        PaperExamined?.Invoke(paper.Index);
        if (deskView != null && !deskView.IsOn)
            deskView.TiltIn();
    }

    /// <summary>A drag begins: the dragged paper lifts above the stack.</summary>
    private void HandleDragBegan(DeskDraggable drag)
    {
        DeskDocument paper = drag.GetComponent<DeskDocument>();
        _dragged = paper.Index;
        MeasureCasings();
        ApplyStack();
    }

    /// <summary>The dragged paper follows the pointer, keeping its size (the drop and the view decide it: DeskPapers.ShownFullSize); over the counter the counter's strip lights.</summary>
    private void HandleDragged(DeskDraggable drag, Vector3 point)
    {
        DeskZone under = ZoneAt(point);
        // The scanner's lid opens for a paper dragged near it (ScannerLid).
        scanner.SetNear(!_scanners.Hidden && scanner.Near(point));
        // Carried over a machine, it keeps over its casing (never through it).
        DeskDocument paper = drag.GetComponent<DeskDocument>();
        if (paper != null)
            paper.SetCarryFloor(CarryFloor(paper.transform.position, Grow(paper), paper.Thickness));
        if (_overCounter != (under == DeskZone.Counter))
        {
            _overCounter = under == DeskZone.Counter;
            ShowCounter();
        }
    }

    /// <summary>DeskPapers decides the drop: the paper slides to the bed (scanning) or back to its pick-up point in the zone it lay in (refused; or bounced off the counter before the verdict, with the note "Stamp the passport first"), the papers are handed back (any paper on the counter once the passport carries its verdict), or it stays in the zone it was dropped in (the desk: it is read and the reading view comes); it goes on top either way.</summary>
    private void HandleDragEnded(DeskDraggable drag, Vector3 released)
    {
        DeskDocument paper = drag.GetComponent<DeskDocument>();
        _dragged = -1;
        _overCounter = false;
        scanner.SetNear(false);
        ShowCounter();
        DeskZone zone = ZoneAt(released);
        bool verdict = stamps != null && stamps.HasVerdict;
        // it settles to its place on top from where it hangs (the way it is carried starts there)
        paper.SetCarryFloor(0f);
        _stack.BringToFront(paper.Index);
        ApplyStack();

        // A machine takes only its documents (MachineIntake; Saleh's 1018a playtest): the chip reader a chip card
        // in its slot or a passport on its pad, the scanner any case paper on its bed; anything else bounces back.
        IPaperDock dock = DockAt(released);
        if (dock != null && dock.Machine == DeskMachine.ChipReader)
        {
            MachineDock where = MachineIntake.Dock(dock.Machine, FeedOf(paper));
            if (where == MachineDock.Refused || hardware.ReaderBusy || _inReader.Count > 0)
                RefuseAt(paper, dock, drag.PickUpPosition);
            else
                ReadOnReader(paper, dock, where, drag.PickUpPosition);
            return;
        }

        DropOutcome outcome = _state.Drop(paper.Index, OnScanner(released), zone, verdict);
        if (outcome != DropOutcome.Queued)
            _waiting.Remove(paper.Index);
        switch (outcome)
        {
            case DropOutcome.HandsBack:
                stamps.HandBack();
                return;
            case DropOutcome.Scanning:
                paper.SetZone(_state.ZoneOf(paper.Index), false);
                StartScan(paper);
                break;
            case DropOutcome.Queued:
                // Drop and go: it waits by the glass, clear of the scanner's body, and feeds through at its turn.
                _waiting.Add(paper.Index);
                paper.SetZone(_state.ZoneOf(paper.Index), false);
                Vector3 byTheGlass = scanner.BedPoint;
                byTheGlass.y = surface.transform.position.y; // it waits on the desk, not at the glass's height
                Slide(paper, ClearOfBlockers(paper, byTheGlass));
                break;
            case DropOutcome.NotStamped:
                if (stamps != null)
                    stamps.Note("stamp.refused.handBackFirst");
                paper.SetZone(_state.ZoneOf(paper.Index), false);
                Slide(paper, ClearOfBlockers(paper, drag.PickUpPosition));
                break;
            case DropOutcome.Refused:
                paper.SetZone(_state.ZoneOf(paper.Index), false);
                Slide(paper, ClearOfBlockers(paper, drag.PickUpPosition));
                break;
            default:
                zone = _state.ZoneOf(paper.Index);
                paper.SetZone(zone, false);
                Vector3 clear = ClearOfBlockers(paper, paper.transform.position);
                if ((clear - paper.transform.position).sqrMagnitude > 1e-8f)
                    Slide(paper, clear);
                if (zone == DeskZone.Desk)
                    Read(paper);
                break;
        }

        RefreshHint();
    }

    /// <summary>
    /// A paper dropped on the chip reader that takes it (<paramref name="where"/>:
    /// a card into the slot, a passport onto the pad): inert meanwhile, it is
    /// carried over the casing and docked at the dock's height and angle
    /// (DockPath), read (ChipReader's cycle), and carried back out (out of the
    /// slot, or up off the pad) over the casing to <paramref name="back"/>
    /// (where it was picked up).
    /// </summary>
    private void ReadOnReader(DeskDocument paper, IPaperDock dock, MachineDock where, Vector3 back)
    {
        int index = paper.Index;
        CaseDocument doc = index >= 0 && index < _documents.Count ? _documents[index] : null;
        _inReader.Add(index);
        Dock(paper);
        ApplyLive(paper);
        (Vector3 way, float depth) slot = dock.Slot;
        float half = paper.DockedSize.y / 2f, inside = Mathf.Min(slot.depth, 2f * half * config.slotInsertShare);
        Vector3 mouth = DockedRoot(paper, dock.DockPoint(where), where);
        Vector3 from = paper.CarriedFrom;
        List<Vector3> path = where == MachineDock.Slot
            ? Points(DockPath.Into(Tuple(from), Tuple(mouth), slot.way.x, slot.way.z, half, inside, dock.Casing, Casings(), Grow(paper), config.dockClearance))
            : Points(DockPath.OnTo(Tuple(from), Tuple(mouth), Casings(), Grow(paper), config.dockClearance));
        paper.Carry(path, dock.DockFacing(where), WaySeconds(from, path), () =>
        {
            void Out()
            {
                _inReader.Remove(index);
                if (!_papers.Contains(paper))
                    return;
                Undock(paper);
                Vector3 home = ClearOfBlockers(paper, back);
                Vector3 at = paper.CarriedFrom;
                List<Vector3> outWay = where == MachineDock.Slot
                    ? Points(DockPath.OutOf(Tuple(mouth), slot.way.x, slot.way.z, half, inside, dock.Casing, Tuple(home), Casings(), Grow(paper), config.dockClearance))
                    : Way(at, home, Grow(paper));
                paper.Carry(outWay, Upright, WaySeconds(at, outWay), () => ApplyLive(paper));
                ApplyLive(paper);
            }
            if (doc == null || !hardware.Read(paper, doc.fields, Out))
                Out();
        });
    }

    /// <summary>
    /// A paper the machine under it will not take (MachineIntake: a paper
    /// with no chip at the chip reader, or the reader busy with another):
    /// the machine says no (IPaperDock.Refuse: the reader's FAULT lamp blinks
    /// with its fault buzz), the paper shakes and goes back over the casing to
    /// <paramref name="back"/> (where it was picked up), never through it.
    /// </summary>
    private void RefuseAt(DeskDocument paper, IPaperDock dock, Vector3 back)
    {
        dock.Refuse();
        paper.Shake(config.refuseShake);
        paper.SetZone(_state.ZoneOf(paper.Index), false);
        Slide(paper, ClearOfBlockers(paper, back));
        RefreshHint();
    }

    // ---------------- The machines' docks (Saleh's 1018a playtest: "the correct elevation and orintation") ----------------

    /// <summary>The machines a paper can be fed to today (IPaperDock): the scanner, from its day; the chip reader, from its.</summary>
    private IEnumerable<IPaperDock> Docks()
    {
        if (scanner != null && !_scanners.Hidden && scanner.OnDesk)
            yield return scanner;
        IPaperDock reader = hardware != null ? hardware.ReaderDock : null;
        if (reader != null && reader.OnDesk)
            yield return reader;
    }

    /// <summary>The machine a thing released at <paramref name="point"/> drops on (the chip reader first: it stands clear of the scanner), or null.</summary>
    private IPaperDock DockAt(Vector3 point)
    {
        IPaperDock found = null;
        foreach (IPaperDock dock in Docks())
            if (dock.Contains(point) && (found == null || dock.Machine == DeskMachine.ChipReader))
                found = dock;
        return found;
    }

    /// <summary>The machines' casings today, measured at the start of a drag (the machines stand still while it runs) and for every way across the desk.</summary>
    private readonly List<MachineBox> _casings = new List<MachineBox>();

    /// <summary>Measures the machines' casings now (Docks).</summary>
    private void MeasureCasings()
    {
        _casings.Clear();
        foreach (IPaperDock dock in Docks())
            _casings.Add(dock.Casing);
    }

    /// <summary>The casings, measured now.</summary>
    private List<MachineBox> Casings()
    {
        MeasureCasings();
        return _casings;
    }

    /// <summary>How high over its place (metres: its face's lift) a thing <paramref name="thickness"/> thick dragged with its root at <paramref name="root"/> keeps, its edges reaching <paramref name="grow"/> from it: its bottom over each casing under it by the clearance; 0 over none.</summary>
    private float CarryFloor(Vector3 root, float grow, float thickness)
    {
        float floor = 0f;
        foreach (MachineBox box in _casings)
            if (box.Over(root.x, root.z, grow))
                floor = Mathf.Max(floor, box.Top + config.dockClearance + thickness - root.y);
        return floor;
    }

    /// <summary>Half <paramref name="paper"/>'s diagonal as it shows (metres): how far its corners reach from its root.</summary>
    private static float Grow(DeskDocument paper) => paper.ShownSize.magnitude / 2f;

    /// <summary>What <paramref name="paper"/> is to a machine (MachineIntake): a case paper, carrying a chip or not, a card or not.</summary>
    private MachineFeed FeedOf(DeskDocument paper) =>
        new MachineFeed(true, hardware != null && hardware.CarriesChip(paper.Index), paper.Kind == PaperKind.Card);

    /// <summary>
    /// Where <paramref name="paper"/>'s root lies docked at
    /// <paramref name="point"/> (the bed's or the pad's top, or the slot's
    /// mouth): its bottom on the bed or the pad, or its middle at the slot's
    /// height (its face lies its thickness and a hair over its root while
    /// docked: Dock).
    /// </summary>
    private static Vector3 DockedRoot(DeskDocument paper, Vector3 point, MachineDock where) =>
        where == MachineDock.Slot ? point - Vector3.up * (paper.Thickness / 2f + DockGap) : point;

    /// <summary>How far a docked paper's bottom lies over its bed (metres): clear of it for the depth buffer.</summary>
    private const float DockGap = 0.0005f;

    /// <summary>The papers docked in a machine (on the scanner's bed, in the chip reader): their lift is their own thickness over the dock, not the stack's.</summary>
    private readonly HashSet<int> _docked = new HashSet<int>();

    /// <summary>The paper goes into a machine: its lift becomes its thickness over the dock, at its normal size.</summary>
    private void Dock(DeskDocument paper)
    {
        _docked.Add(paper.Index);
        paper.SetDocked(true);
        ApplyStack();
    }

    /// <summary>The paper comes out of its machine: back to the stack's lift and its zone's size.</summary>
    private void Undock(DeskDocument paper)
    {
        _docked.Remove(paper.Index);
        paper.SetDocked(false);
        _stack.BringToFront(paper.Index);
        ApplyStack();
    }

    /// <summary>The way from <paramref name="from"/> to <paramref name="to"/> over every casing it comes over (DockPath.Over), as world points after the start.</summary>
    private List<Vector3> Way(Vector3 from, Vector3 to, float grow) => Points(DockPath.Over(Tuple(from), Tuple(to), Casings(), grow, config.dockClearance));

    /// <summary>A way's seconds: the desk's slide for a straight way, else its length at dockSpeed (never quicker than the slide).</summary>
    private float WaySeconds(Vector3 from, List<Vector3> way)
    {
        if (way.Count <= 1)
            return config.paperSlideSeconds;
        float length = 0f;
        Vector3 at = from;
        foreach (Vector3 p in way)
        {
            length += Vector3.Distance(at, p);
            at = p;
        }
        return DeskCarry.Seconds(length, config.dockSpeed, config.paperSlideSeconds);
    }

    private static (float x, float y, float z) Tuple(Vector3 v) => (v.x, v.y, v.z);

    /// <summary>A way's points after its start, as world points.</summary>
    private static List<Vector3> Points(List<(float x, float y, float z)> way)
    {
        var points = new List<Vector3>(Mathf.Max(0, way.Count - 1));
        for (int i = 1; i < way.Count; i++)
            points.Add(new Vector3(way[i].x, way[i].y, way[i].z));
        return points;
    }

    /// <summary>
    /// A drag cut short (cancelled by a right-click or Esc, or the paper
    /// disabled mid-drag: its input taken away, or the case torn down, so no
    /// release comes): a paper still on the desk slides back to where it was
    /// picked up, in the zone it lay in, on top, as a refused drop does; a
    /// paper already given back is left to the teardown.
    /// </summary>
    private void HandleDragCancelled(DeskDraggable drag)
    {
        DeskDocument paper = drag.GetComponent<DeskDocument>();
        _dragged = -1;
        scanner.SetNear(false);
        if (_overCounter)
        {
            _overCounter = false;
            ShowCounter();
        }
        if (paper == null)
            return;
        paper.SetCarryFloor(0f);
        if (_state == null || !_state.CanDrag(paper.Index))
            return;

        paper.SetZone(_state.ZoneOf(paper.Index), false);
        _stack.BringToFront(paper.Index);
        ApplyStack();
        Slide(paper, ClearOfBlockers(paper, drag.PickUpPosition));
    }

    /// <summary>The zone a point on the desk lies in: the counter's strip, else the desk.</summary>
    private DeskZone ZoneAt(Vector3 point) => counter != null && counter.Contains(point) ? DeskZone.Counter : DeskZone.Desk;

    /// <summary>The counter shows while a traveller's papers are on the desk, "▲ HAND BACK ▲" once the passport carries its verdict, lit while a dragged paper's pointer is over it.</summary>
    private void ShowCounter()
    {
        if (counter != null)
            counter.Show(_papers.Count > 0, stamps != null && stamps.HasVerdict, _overCounter);
    }

    /// <summary>The corners of a paper lying with its root at <paramref name="at"/> (every paper lies as the template does: the new paper's sheet gives the offsets; its size is its look's at its zone's scale).</summary>
    private Vector3[] Footprint(DeskDocument paper, Vector3 at)
    {
        Vector2 size = paper.Size;
        Transform sheet = paper.Sheet;
        Vector3 root = paper.transform.position;
        return new[]
        {
            at + sheet.TransformPoint(new Vector3(-size.x / 2f, -size.y / 2f, 0f)) - root,
            at + sheet.TransformPoint(new Vector3(size.x / 2f, -size.y / 2f, 0f)) - root,
            at + sheet.TransformPoint(new Vector3(-size.x / 2f, size.y / 2f, 0f)) - root,
            at + sheet.TransformPoint(new Vector3(size.x / 2f, size.y / 2f, 0f)) - root,
        };
    }

    /// <summary>
    /// Where <paramref name="paper"/>, about to lie with its root at
    /// <paramref name="at"/>, lies instead so the scanner does not hide it
    /// (ScannerClearance.Clear, in the office view's frame on the desk: its
    /// footprint and the shadow its body casts away from the camera) while it
    /// is on the desk; it moves the shortest way out to the left, right or
    /// front, or to the eject spot. <paramref name="at"/> itself when nothing
    /// is in the way.
    /// </summary>
    private Vector3 ClearOfBlockers(DeskDocument paper, Vector3 at)
    {
        if (surface == null || scanner == null || !scanner.gameObject.activeInHierarchy)
            return at;
        DeskRect desk = InViewFrame(surface.Corners());
        DeskRect sheet = InViewFrame(Footprint(paper, at));
        (float x, float y) = ScannerClearance.Clear(sheet, InViewFrame(scanner.Corners()), _scannerShadow, desk);
        // The UV torch's cradle on the mat's corner (the desk machines spec §1): no paper lies over it either.
        if (hardware != null && hardware.TryTorchFootprint(out Vector3[] cradle))
            (x, y) = ScannerClearance.ClearProp(new DeskRect(x, y, sheet.Width, sheet.Height), InViewFrame(cradle), desk);
        // Nor over the chip reader's casing (a paper lies beside it, never in it).
        IPaperDock reader = hardware != null ? hardware.ReaderDock : null;
        if (reader != null && reader.OnDesk)
            (x, y) = ScannerClearance.ClearProp(new DeskRect(x, y, sheet.Width, sheet.Height), InViewFrame(Corners(reader.Casing, surface.transform.position.y)), desk);
        if (Mathf.Approximately(x, sheet.CentreX) && Mathf.Approximately(y, sheet.CentreY))
            return at;
        Vector3 moved = at + _viewRight * (x - sheet.CentreX) + _viewForward * (y - sheet.CentreY);
        return surface.Clamp(moved) + Vector3.up * (at.y - surface.transform.position.y);
    }

    /// <summary>The four corners of <paramref name="box"/>'s footprint at height <paramref name="y"/>.</summary>
    private static Vector3[] Corners(MachineBox box, float y)
    {
        Vector3 c = new Vector3(box.CentreX, y, box.CentreZ), a = new Vector3(box.AxisX, 0f, box.AxisZ) * box.HalfWidth, b = new Vector3(-box.AxisZ, 0f, box.AxisX) * box.HalfDepth;
        return new[] { c - a - b, c + a - b, c - a + b, c + a + b };
    }

    /// <summary>The rectangle enclosing <paramref name="corners"/> in the office view's frame on the desk (x right, y away from the camera, from the desk's centre).</summary>
    private DeskRect InViewFrame(IEnumerable<Vector3> corners)
    {
        Vector3 origin = surface.transform.position;
        float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
        foreach (Vector3 c in corners)
        {
            float x = Vector3.Dot(c - origin, _viewRight), y = Vector3.Dot(c - origin, _viewForward);
            minX = Mathf.Min(minX, x);
            maxX = Mathf.Max(maxX, x);
            minY = Mathf.Min(minY, y);
            maxY = Mathf.Max(maxY, y);
        }
        return new DeskRect((minX + maxX) / 2f, (minY + maxY) / 2f, maxX - minX, maxY - minY);
    }

    /// <summary>Slides a paper; it is inert while sliding, and its liveness is re-applied when it lands.</summary>
    private void Slide(DeskDocument paper, Vector3 target) => Slide(paper, target, paper.transform.rotation);

    /// <summary>Slides a paper, turning it to lie at <paramref name="facing"/> on the way (eased with the slide): from where it shows, over every machine's casing it comes over (DockPath.Over: never through one).</summary>
    private void Slide(DeskDocument paper, Vector3 target, Quaternion facing)
    {
        Vector3 from = paper.CarriedFrom;
        List<Vector3> way = Way(from, target, Grow(paper));
        paper.Carry(way, facing, WaySeconds(from, way), () => ApplyLive(paper));
        ApplyLive(paper);
    }

    /// <summary>A paper on the desk takes input while the papers are allowed, DeskPapers lets it be dragged and it is not sliding, and is in the raycast while the papers are allowed.</summary>
    private void ApplyLive(DeskDocument paper)
    {
        bool live = _live && !paper.IsSliding && _state != null && _state.CanDrag(paper.Index) && !_inReader.Contains(paper.Index);
        paper.SetLive(live, live, _live);
    }

    /// <summary>
    /// Stack heights (PaperLayers.Lifts): each paper and the rulebook lifted
    /// by its place in the stack and the thickness of all under it, the
    /// lowest clear of the desk top (Saleh's 1007d playtest: papers clipped
    /// into the table); the dragged paper or rulebook lifted above the whole stack.
    /// </summary>
    private void ApplyStack()
    {
        // One stacking rule (PaperLayers): each one's bottom lies over every part of the one under it, so the paper on top hides all of each paper under it.
        _stackThickness.Clear();
        for (int i = 0; i < _stack.Count; i++)
            _stackThickness.Add(Thickness(_stack[i]));
        float[] lifts = PaperLayers.Lifts(_stackThickness, config.paperStackStep);
        float top = lifts.Length > 0 ? lifts[lifts.Length - 1] + PaperLayers.PartsDepth : PaperLayers.StackStep(config.paperStackStep);
        foreach (DeskDocument paper in _papers)
            if (paper != null)
                paper.SetLift(_docked.Contains(paper.Index) ? paper.Thickness + DockGap
                              : paper.Index == _dragged ? top + paper.Thickness + config.dragLift : LiftOf(paper.Index, lifts));
        if (rulebook != null)
            rulebook.SetLift(_rulebookDragged ? top + rulebook.Thickness + config.dragLift : LiftOf(RulebookId, lifts));
    }

    /// <summary>The thicknesses of the stack, bottom first (reused).</summary>
    private readonly List<float> _stackThickness = new List<float>();

    /// <summary>The thickness of stack id <paramref name="id"/>: the rulebook's, or a paper's.</summary>
    private float Thickness(int id)
    {
        if (id == RulebookId)
            return rulebook != null ? rulebook.Thickness : 0f;
        return id >= 0 && id < _papers.Count && _papers[id] != null ? _papers[id].Thickness : config.paperThickness;
    }

    /// <summary>Stack id <paramref name="id"/>'s lift from <paramref name="lifts"/> (a paper not in the stack lies at the first place).</summary>
    private float LiftOf(int id, float[] lifts)
    {
        int at = _stack.IndexOf(id);
        return at >= 0 && at < lifts.Length ? lifts[at] : PaperLayers.Lifts(new[] { Thickness(id) }, config.paperStackStep)[0];
    }

    /// <summary>
    /// True when the camera <paramref name="camera"/> sees paper
    /// <paramref name="document"/>'s face at <paramref name="world"/>: no other
    /// paper or the rulebook lies over it there (the first of them a
    /// ray from the camera meets is that paper; nothing of them met counts as
    /// shown). The guide's arrow points only at what shows (GuideDirector).
    /// </summary>
    public bool Shows(int document, Vector3 world, Camera camera)
    {
        if (camera == null)
            return true;
        Vector3 from = camera.transform.position;
        Vector3 way = world - from;
        float distance = way.magnitude;
        if (distance <= 0f)
            return true;
        // The nearest paper or rulebook on the line of sight decides; no allocation (the guide's arrow asks every frame).
        int count = Physics.RaycastNonAlloc(from, way / distance, ShowsHits, distance + 0.05f, 1 << OfficeLayers.InteractableLayer, QueryTriggerInteraction.Collide);
        float nearest = float.MaxValue;
        DeskDocument top = null;
        bool book = false;
        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = ShowsHits[i];
            if (hit.distance >= nearest)
                continue;
            DeskDocument paper = hit.collider.GetComponentInParent<DeskDocument>();
            if (paper != null)
            {
                nearest = hit.distance;
                top = paper;
                book = false;
            }
            else if (hit.collider.GetComponentInParent<DeskRulebook>() != null)
            {
                nearest = hit.distance;
                top = null;
                book = true;
            }
        }
        if (book)
            return false;
        if (top != null)
            return top.Index == document && _papers.Contains(top);
        return true;
    }

    /// <summary>Shows' reused hit buffer (more colliders than the desk ever stacks on one line of sight).</summary>
    private static readonly RaycastHit[] ShowsHits = new RaycastHit[64];

    /// <summary>A paper slides onto the bed and its scan starts: the relay click (scanner_start), the glowing bar crossing the glass as the scan runs (DeskScanner.Sweep); the done beep is DeskScanner.Pulse's.</summary>
    private void StartScan(DeskDocument paper)
    {
        // over the casing and down onto the glass, square to the bed, the lid held open until it lies there (the scan waits: Update)
        Dock(paper);
        scanner.HoldOpen(true);
        Vector3 from = paper.CarriedFrom;
        List<Vector3> way = Points(DockPath.OnTo(Tuple(from), Tuple(scanner.DockPoint(MachineDock.Bed)), Casings(), Grow(paper), config.dockClearance));
        paper.Carry(way, scanner.DockFacing(MachineDock.Bed), WaySeconds(from, way), () =>
        {
            scanner.HoldOpen(false);
            ApplyLive(paper);
        });
        ApplyLive(paper);
        Sounds.Play(SoundCues.ScannerStart);
    }

    /// <summary>True when <paramref name="point"/> lies on the scanner's bed and the scanner is on the desk today (ScannerDay.Hidden: not before it is introduced).</summary>
    private bool OnScanner(Vector3 point) => !_scanners.Hidden && scanner.Contains(point);

    /// <summary>The day-1 scan note (DeskHints): re-evaluated at every hand-over, read, drop, finished scan and case end; it goes after the day's first read or scan.</summary>
    private void RefreshHint()
    {
        if (scanHint == null)
            return;

        bool paperOnDesk = _state != null && _state.OnDeskCount > 0;
        scanHint.gameObject.SetActive(config != null && !_scanners.Hidden &&
                                      DeskHints.ScanHintVisible(config.scanHintKey, _day, config.scanHintUntilDay, _scansToday + _readsToday, paperOnDesk));
    }
}
