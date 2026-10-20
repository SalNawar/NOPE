using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// One case's papers on the desk, Papers, Please's way (Saleh 2026-10-06:
/// "copy the controls of Papers, Please 1:1"): papers handed over slide from
/// the traveller's side onto the counter (DeskCounter: a row of spots), small,
/// one after another, each landing with a slap (the staged hand-over,
/// BUG-49b: DeskConfigSO.handOverSeconds, DeskEvents.DocumentLanded on the
/// landing frame for FEEDBACK's sound; one paper bound per frame at most, from
/// a pool of clones made between cases, BUG-65g); left-press and drag moves a
/// document (DeskDraggable), the only way to move it, in either view. A paper
/// has one world size per zone, whatever the view (DOC-HANDLE, BUG-1; Saleh
/// 2026-10-20, D1: "we are doing the camera tilt no need to enlarge the
/// documents when you click them"; PaperSizes): small on the counter, its
/// reading size on the desk; a held paper changes size only as its pointer
/// crosses the counter's edge, under the cursor (a desk paper keeps its desk
/// size over the counter until the passport carries its verdict: it could
/// not stay there). A drop never changes its size, and it rests exactly where
/// it was released (DropResolver, BUG-6: its footprint at its final size,
/// kept wholly inside what the reading view shows and clear of every
/// obstacle in DeskObstacles and of the area the scanner hides, in one joint
/// solve: the least move, never one push after another). A document dropped
/// on the desk brings the reading view by itself (DeskView.TiltIn, the one
/// camera move a drop makes: Saleh's D1); a click on a paper only brings it
/// to the top of the stack (R6: no move, no size, no tilt). Each drop is
/// decided through DeskPapers: it stays in the zone it was dropped in, it
/// scans on the scanner (the dock under its footprint's centre, not the
/// pointer), it waits by the busy scanner (drop and go), it is refused (a
/// machine that does not take it, or a paper from the desk dropped on the
/// counter before the passport carries its verdict, with the note "Stamp the
/// passport first"): a refusal shakes in place and rests just clear of what
/// refused it, never flying back; or (any paper dropped on the counter once
/// the passport carries its verdict) the papers are handed back
/// (DeskStampTray.HandBack: the case is decided). Every paper lies upright
/// toward the chair (DeskView.LevelForward); dropped on the scanner it eases
/// square onto its bed (DeskScanner.BedFacing), a passport on the chip
/// reader's pad lies upright to the player sized to fit the pad (DockFit), and
/// each keeps its docked size until it is clear of the machine again; the dock
/// state is the case's (MachineOccupancy: cleared when a case begins and
/// ends) and no queue takes a paper out of a machine (BUG-24). In inspect mode
/// (SetInspecting) a click on a value picks it for comparison (FieldPicked)
/// and nothing else does. A right-click, Esc or a view key mid-drag cancels the
/// drag (the hold registry, ControlRules: the paper slides back to where it
/// was picked up). Papers stack by height (each place in the stack lifts a
/// sheet one step, the top one a little more so the order reads at a glance,
/// BUG-40b; a reorder shows in the same frame and raises DeskEvents.
/// StackChanged with each paper's rank for DOC-LOOK's contact shadow; the
/// rulebook takes a place in the stack too). A held paper or the held folder
/// is drawn above every prop (HeldOnTop). A document takes input only while
/// BoothCoordinator allows the papers, DeskPapers lets it be dragged and it is
/// not sliding; papers not allowed take no raycasts at all (spec R38). The
/// day's scanner upgrades (ScannerDay, from the day-start snapshot: BeginDay)
/// change the scan (the PC redesign SC2-SC4): with the Auto-Feed a click on
/// its tray puts every paper not scanned yet in DeskPapers' queue (FeedStack)
/// and each, once landed and not dragged nor in a machine, slides onto the
/// scanner by itself, in hand-over order, one at a time (FeedScanner); with
/// the Analysis a scan by hand takes the analysis pass (the longer scan), once
/// per document (DeskPapers.Pass), and ScanFinished says which pass it was. The
/// placeholder scanner shows its tray and lamp for the owned upgrades (SC6). At
/// the decision every paper goes back to the traveller (DeskEvents.CaseEnded).
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

    /// <summary>The desk tuning (scan time, slide and hand-over times, the stack's heights, the zones' sizes, the photo's tint, the note).</summary>
    [SerializeField] private DeskConfigSO config;

    /// <summary>The counter (optional: without it every paper lies on the desk and none is handed back by a drop).</summary>
    [SerializeField] private DeskCounter counter;

    /// <summary>The reading view (optional): a document dropped on the desk tilts into it, and a resting paper lies wholly inside what it shows.</summary>
    [SerializeField] private DeskView deskView;

    /// <summary>The stamps (optional): the passport's verdict, and the hand-back when the stamped passport is dropped on the counter.</summary>
    [SerializeField] private DeskStampTray stamps;

    /// <summary>The rulebook (optional): it lies in the papers' stack, over or under each paper as they were last touched, a dragged one above them all (Saleh 2026-10-06: "documents on desk like the folder with the rules are clipping with the desk").</summary>
    [SerializeField] private DeskRulebook rulebook;

    /// <summary>The desk machines (optional; the desk machines spec §1): the chip reader a paper is dropped on, the UV torch, each paper's UV layer.</summary>
    [SerializeField] private DeskHardware hardware;

    /// <summary>The desk machines (null when none are wired).</summary>
    public DeskHardware Hardware => hardware;

    /// <summary>Which machine holds which paper this case (the scanner's bed, the chip reader): cleared when a case begins and ends (BUG-24).</summary>
    private readonly MachineOccupancy _docks = new MachineOccupancy();

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
    private readonly HashSet<int> _waiting = new HashSet<int>();

    /// <summary>The arrival papers still to be handed over (one per hand-over beat: HandOverNext), and when the next may come (Time.time).</summary>
    private readonly Queue<int> _arrivals = new Queue<int>();
    private float _nextHandOverAt;

    /// <summary>Clones of the paper template made ready between cases (one per frame), bound when a paper is handed over.</summary>
    private readonly List<DeskDocument> _spare = new List<DeskDocument>();

    /// <summary>Each paper's last rank in the stack (DeskEvents.StackChanged raises only on a change).</summary>
    private readonly Dictionary<int, int> _ranks = new Dictionary<int, int>();

    /// <summary>The paper being dragged, or -1.</summary>
    private int _dragged = -1;

    /// <summary>True while the dragged paper's pointer is over the counter (its strip lights).</summary>
    private bool _overCounter;

    /// <summary>The office view's level right and forward on the desk (SetScannerView): the frame every rest is judged in (DropResolver).</summary>
    private Vector3 _viewRight = Vector3.right, _viewForward = Vector3.forward;

    /// <summary>How deep behind the scanner its body hides a paper from the office camera, in metres (SetScannerView).</summary>
    private float _scannerShadow;

    /// <summary>The obstacles' ids this desk registers (DeskObstacles): the scanner, the chip reader, the UV torch's cradle (until TOOLS registers its own).</summary>
    private const string ScannerObstacle = "desk.scanner", ReaderObstacle = "desk.chipReader", TorchObstacle = "desk.uvTorchCradle";

    /// <summary>Reused buffers: the registry's footprints (world), the keep-outs in the view's frame, a paper's four corners.</summary>
    private readonly List<DeskRect> _footprints = new List<DeskRect>();
    private readonly List<DeskRect> _keepOut = new List<DeskRect>();
    private readonly Vector3[] _corners = new Vector3[4];

    /// <summary>True when the desk and all its parts are wired; otherwise documents reach the PC when handed over (InvestigationUIController).</summary>
    public bool IsReachable =>
        surface != null && scanner != null && paperTemplate != null && paperRoot != null && handOverPoint != null && config != null;

    /// <summary>True while a document is being dragged (a right-click or Esc cancels the drag first: ControlRules).</summary>
    public bool IsDragging => _dragged >= 0;

    /// <summary>Raised when a scan finishes, with the paper's index and its pass (ScanPass: the analysis pass for a first scan by hand with the Analysis Scanner): its scanned copy reaches the PC (the Investigation app's Documents tab), which marks the papers after an analysis.</summary>
    public event Action<int, ScanPass> ScanFinished;

    /// <summary>Raised when a value of a document is picked in inspect mode: the paper's index, the field's row and where it lights up.</summary>
    public event Action<int, DocumentRow, ICompareHighlight> FieldPicked;

    /// <summary>Raised when a document is dropped on the desk to be read, with its index (the steps checklist's "read").</summary>
    public event Action<int> PaperExamined;

    private void Awake()
    {
        _landed = Landed;
        if (scanHint != null && config != null)
            scanHint.text = UiText.Get(config.scanHintKey);
        if (stamps != null)
            stamps.Changed += ShowCounter;
        if (rulebook != null && config != null)
        {
            rulebook.ShowDepth(config);
            rulebook.UseConfig(config);
        }
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
        if (rulebook != null && rulebook.Drag != null)
        {
            rulebook.Drag.DragBegan -= RulebookLifted;
            rulebook.Drag.Dragged -= RulebookDragged;
            rulebook.Drag.DragEnded -= RulebookDropped;
            rulebook.Drag.DragCancelled -= RulebookLifted;
        }
        DeskObstacles.Desk.Unregister(ScannerObstacle);
        DeskObstacles.Desk.Unregister(ReaderObstacle);
        DeskObstacles.Desk.Unregister(TorchObstacle);
    }

    /// <summary>A paper's rotation lying upright toward the chair: its top away from it, along the office view's level forward.</summary>
    private Quaternion Upright => Quaternion.LookRotation(deskView != null ? deskView.LevelForward : Vector3.forward, Vector3.up);

    /// <summary>The stack with no case paper: the rulebook alone.</summary>
    private void ResetStack()
    {
        _stack.Clear();
        _stack.Add(RulebookId);
        _ranks.Clear();
    }

    /// <summary>The rulebook's drag begins (lifted above the stack, drawn above every prop) or is cut short (back on top of it).</summary>
    private void RulebookLifted(DeskDraggable drag)
    {
        _rulebookDragged = drag.IsDragging;
        if (_rulebookDragged)
            MeasureMachines();
        else
            rulebook.ShowHeld(null, 1f);
        _stack.BringToFront(RulebookId);
        if (config != null)
            ApplyStack();
    }

    /// <summary>The dragged rulebook is drawn above every prop (HeldOnTop), as a held paper is.</summary>
    private void RulebookDragged(DeskDraggable drag, Vector3 point)
    {
        if (config != null)
            rulebook.ShowHeld(drag.DragCamera, HeldRatio(drag.DragCamera, rulebook.transform.position.y));
    }

    /// <summary>
    /// The rulebook dropped: it lies on top of the stack, where it was
    /// released (DropResolver: wholly in view, clear of every obstacle);
    /// dropped on a machine (the scanner or the chip reader: neither takes it,
    /// MachineIntake) the machine says no, the folder shakes in place and
    /// lies just clear of it (Saleh's 1018a playtest: "if is the wrong
    /// document it shouldnt go through the machience"); dropped on the counter
    /// it shakes its "no" (DeskRulebook) and lies just off it (BUG-6b: never a
    /// teleport back to where it was picked up).
    /// </summary>
    private void RulebookDropped(DeskDraggable drag, Vector3 released)
    {
        _rulebookDragged = false;
        rulebook.ShowHeld(null, 1f);
        if (config != null && surface != null && TryFootprint(rulebook.transform, out DeskRect folder))
        {
            Vector3 at = rulebook.transform.position;
            IPaperDock dock = DockAt(at);
            bool refused = dock != null && !MachineIntake.Accepts(dock.Machine, MachineFeed.Folder);
            Vector3 rest = RestOf(folder, at, counter != null && counter.Contains(released));
            List<Vector3> way = (rest - at).sqrMagnitude > 1e-8f ? Way(at, rest, RulebookGrow()) : null;
            float seconds = way != null ? WaySeconds(at, way) : 0f;
            if (refused)
            {
                dock.Refuse();
                rulebook.Refused(way, seconds);
            }
            else
                rulebook.RestAt(way, seconds);
        }
        RulebookLifted(drag);
    }

    /// <summary>Half the rulebook's diagonal on the desk (metres): how far its edges reach from its root.</summary>
    private float RulebookGrow() => TryFootprint(rulebook.transform, out DeskRect folder) ? Mathf.Sqrt(folder.Width * folder.Width + folder.Height * folder.Height) / 2f : 0.2f;

    /// <summary>Makes a spare paper ready (between hand-overs), hands the next arrival over on its beat, then advances a running scan (a finished paper slides back to where it was picked up, keeping its docked size until clear of the scanner) or feeds the idle scanner its next queued paper (the Auto-Feed Scanner).</summary>
    private void Update()
    {
        if (!HandOverNext())
            PrepareSpare();
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
        CarryOut(paper, Rest(paper, paper.Drag.PickUpPosition, false));
        scanner.Pulse();
        _scansToday++;
        RefreshHint();
        ScanFinished?.Invoke(done, pass);
    }

    /// <summary>
    /// The feed: while the scanner is idle, the next queued paper that lies
    /// still on the desk, in no machine, slides onto the bed and scans
    /// (DeskPapers.FeedNext: queue order, one at a time): a paper the Auto-Feed
    /// Scanner (SC3) took in goes back to where it lay, one dropped on the busy
    /// scanner (drop and go, the scanner app spec §1) back to where it was
    /// picked up.
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
        _stack.BringToFront(next);
        StartScan(_papers[next]);
    }

    /// <summary>True when paper <paramref name="i"/> lies still on the desk: handed over, landed, not being dragged and in no machine (a queue never takes a paper from the chip reader: BUG-24).</summary>
    private bool Landed(int i) => _papers[i] != null && !_papers[i].IsSliding && _dragged != i && _docks.FreeToFeed(i);

    /// <summary>
    /// The office view the scanner hides papers from (the office binder, from
    /// the art office's camera): its level forward on the desk and the depth
    /// behind the scanner its body hides (ScannerClearance.Shadow). From then
    /// on no paper rests in the scanner's blocked area (the desk-first
    /// redesign, item 4), and the chip reader lays a passport upright to the
    /// player.
    /// </summary>
    public void SetScannerView(Vector3 levelForward, float shadow)
    {
        levelForward = Vector3.ProjectOnPlane(levelForward, Vector3.up);
        if (levelForward.sqrMagnitude < 1e-6f)
            return;
        _viewForward = levelForward.normalized;
        _viewRight = Vector3.Cross(Vector3.up, _viewForward);
        _scannerShadow = Mathf.Max(0f, shadow);
        if (hardware != null && hardware.Reader != null)
            hardware.Reader.FaceThePlayer(_viewForward);
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

    /// <summary>Starts a case's papers, each printing its form (<paramref name="forms"/>, by paper; a photo document shows <paramref name="look"/>); the documents handed over on arrival slide onto the counter one after another (HandOverNext); the first of them is the passport (the stamps' verdict is its). No paper is in any machine.</summary>
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
        _docks.Clear();
        _handedOver = 0;
        _dragged = -1;

        int passport = _state.ArrivalIndices.Count > 0 ? _state.ArrivalIndices[0] : -1;
        _arrivals.Clear();
        foreach (int i in _state.ArrivalIndices)
            _arrivals.Enqueue(i);
        _nextHandOverAt = Time.time;
        if (stamps != null)
            stamps.BeginCase(passport);
        ShowCounter();
        RefreshHint();
    }

    /// <summary>Hands paper <paramref name="i"/> over now (a paper the desk asked for); nothing unless DeskPapers agrees (a paper is handed over once).</summary>
    public void HandOver(int i) => HandOverNow(i);

    /// <summary>The next arrival paper is handed over on its beat (one per frame at most, DeskConfigSO.handOverStagger apart); true when one was.</summary>
    private bool HandOverNext()
    {
        if (_arrivals.Count == 0 || Time.time < _nextHandOverAt)
            return false;
        HandOverNow(_arrivals.Dequeue());
        _nextHandOverAt = Time.time + (config != null ? config.handOverStagger : 0f);
        return true;
    }

    /// <summary>Makes one clone of the paper template ready while fewer than DeskConfigSO.paperPool wait (one per frame, never on a hand-over's frame): call-next binds a ready paper instead of building one (BUG-65g).</summary>
    private void PrepareSpare()
    {
        if (config == null || paperTemplate == null || paperRoot == null || _spare.Count >= config.paperPool)
            return;
        DeskDocument spare = Instantiate(paperTemplate, paperRoot);
        spare.gameObject.SetActive(false);
        _spare.Add(spare);
    }

    /// <summary>A clone of the paper template: a ready one, else a new one.</summary>
    private DeskDocument TakeSpare()
    {
        if (_spare.Count == 0)
            return Instantiate(paperTemplate, paperRoot);
        DeskDocument paper = _spare[_spare.Count - 1];
        _spare.RemoveAt(_spare.Count - 1);
        return paper;
    }

    /// <summary>
    /// Hands paper <paramref name="i"/> over: a ready clone of the template
    /// appears at the hand-over point, small, on top of the stack, and slides
    /// to the next spot on the counter (reused in turn) in
    /// DeskConfigSO.handOverSeconds, landing with a slap (DeskDocument.Land,
    /// DeskEvents.DocumentLanded on that frame). Nothing happens unless
    /// DeskPapers agrees (a paper is handed over once).
    /// </summary>
    private void HandOverNow(int i)
    {
        if (_state == null || !_state.HandOver(i))
            return;

        DeskDocument paper = TakeSpare();
        paper.transform.SetPositionAndRotation(handOverPoint.position, Upright);
        paper.gameObject.SetActive(true);
        paper.Bind(i, _documents[i], i < _forms.Count ? _forms[i] : null, config);
        DocumentForm form = i < _forms.Count ? _forms[i] : null;
        bool photo = _documents[i] != null && (_documents[i].showsPhoto || (form != null && ArtLayout.ShowsPhoto(form.Spec, form.Data)));
        paper.ShowPhoto(photo ? _look : null, _art, config.travellerTint);
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
        target = Rest(paper, target, false);
        Vector3 from = paper.CarriedFrom;
        paper.Carry(Way(from, target, Grow(paper)), paper.transform.rotation, config.handOverSeconds, () =>
        {
            ApplyLive(paper);
            paper.Land();
            DeskEvents.RaiseDocumentLanded(paper);
        });
        ApplyLive(paper);
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

    /// <summary>Every paper on the desk goes back to the traveller (the state's papers all returned; no paper is left in a machine, the scanner's lid lets go), inked with <paramref name="ink"/> (None: no ink), inert, sliding to the hand-over point, then destroyed; DeskEvents.CaseEnded says so.</summary>
    private void SendBack(DeskStamp ink)
    {
        _arrivals.Clear();
        _state.ReturnAll();
        _waiting.Clear();
        _docks.Clear();
        if (hardware != null)
            hardware.EndCase();
        if (scanner != null)
        {
            scanner.Sweep(-1f);
            scanner.HoldOpen(false);
            scanner.SetNear(false);
        }
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
        DeskEvents.RaiseCaseEnded();
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

    /// <summary>A right-click or Esc mid-drag (ControlRules.BackOut, when the hold registry holds nothing else): the drag is cancelled and the paper slides back to where it was picked up; false when nothing is dragged.</summary>
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

    /// <summary>A left-click on a document (PaperClicks): in inspect mode a value is picked; otherwise the document comes to the top of the stack, in that frame, and nothing else (Saleh's D1 and R6: no move, no size, no tilt).</summary>
    private void HandlePaperClicked(DeskDocument paper, int slot)
    {
        if (_state == null || !_state.CanDrag(paper.Index))
            return;

        switch (PaperClicks.Decide(_inspecting, slot >= 0 && slot < paper.SlotCount))
        {
            case PaperClickAction.Pick:
                FieldPicked?.Invoke(paper.Index, paper.FieldAt(slot), paper.SlotHighlight(slot));
                break;
            case PaperClickAction.Front:
                _stack.BringToFront(paper.Index);
                ApplyStack();
                break;
        }
    }

    /// <summary>A document dropped on the desk: it counts as read (the day-1 note goes; the steps' "read"), and the reading view comes (DeskView.TiltIn, Saleh's D1: the one camera move a drop makes; none when it is already on).</summary>
    private void Read(DeskDocument paper)
    {
        _readsToday++;
        RefreshHint();
        PaperExamined?.Invoke(paper.Index);
        if (deskView != null && !deskView.IsOn)
            deskView.TiltIn();
    }

    /// <summary>A drag begins: the dragged paper lifts above the stack (and is drawn above every prop: DeskDocument, HeldOnTop).</summary>
    private void HandleDragBegan(DeskDraggable drag)
    {
        DeskDocument paper = drag.GetComponent<DeskDocument>();
        _dragged = paper.Index;
        MeasureMachines();
        ApplyStack();
    }

    /// <summary>
    /// The dragged paper follows the pointer. Its size changes only as the
    /// pointer crosses the counter's edge, under the cursor (PaperSizes): it
    /// takes the counter's size over the counter when it could lie there (a
    /// counter paper, or any paper once the passport carries its verdict: a
    /// drop there hands the papers back), else it keeps its desk size (a drop
    /// there is refused). Over the counter the counter's strip lights; near the
    /// scanner its lid opens.
    /// </summary>
    private void HandleDragged(DeskDraggable drag, Vector3 point)
    {
        DeskZone under = ZoneAt(point);
        // The scanner's lid opens for a paper dragged near it (ScannerLid).
        scanner.SetNear(!_scanners.Hidden && scanner.Near(point));
        DeskDocument paper = drag.GetComponent<DeskDocument>();
        if (paper != null && _state != null)
        {
            bool canLie = under == DeskZone.Desk || _state.ZoneOf(paper.Index) == DeskZone.Counter || (stamps != null && stamps.HasVerdict);
            DeskZone sized = canLie ? under : DeskZone.Desk;
            if (paper.Zone != sized)
                paper.SetZone(sized, false);
        }
        if (_overCounter != (under == DeskZone.Counter))
        {
            _overCounter = under == DeskZone.Counter;
            ShowCounter();
        }
    }

    /// <summary>
    /// DeskPapers decides the drop. The dock is the one under the paper's
    /// footprint's centre (not the pointer: BUG-6): the chip reader reads a
    /// paper it takes; the scanner scans it (or, busy, it waits beside it).
    /// Refused (a machine that does not take it, or before the verdict a desk
    /// paper on the counter, with the note "Stamp the passport first"), it
    /// shakes in place and rests just clear of what refused it. Any paper on
    /// the counter once the passport carries its verdict hands the papers back.
    /// Otherwise it stays where it was released, in the zone it was dropped in
    /// (DropResolver: moved only if part of it would lie out of view or under
    /// a prop); on the desk it is read and the reading view comes. It goes on
    /// top either way, its lift settling there.
    /// </summary>
    private void HandleDragEnded(DeskDraggable drag, Vector3 released)
    {
        DeskDocument paper = drag.GetComponent<DeskDocument>();
        int index = paper.Index;
        _dragged = -1;
        _overCounter = false;
        scanner.SetNear(false);
        ShowCounter();
        DeskZone zone = ZoneAt(released);
        bool verdict = stamps != null && stamps.HasVerdict;
        _stack.BringToFront(index);
        Vector3 at = paper.transform.position;

        // A machine takes only its documents (MachineIntake; Saleh's 1018a playtest): the chip reader a chip card
        // in its slot or a passport on its pad, the scanner any case paper on its bed; anything else is refused.
        IPaperDock dock = DockAt(at);
        if (dock != null && dock.Machine == DeskMachine.ChipReader)
        {
            MachineDock where = MachineIntake.Dock(dock.Machine, FeedOf(paper));
            if (where == MachineDock.Refused || hardware.ReaderBusy || _docks.Busy(DeskMachine.ChipReader))
                RefuseAt(paper, dock, at);
            else
                ReadOnReader(paper, dock, where, drag.PickUpPosition);
            ApplyStack(index);
            return;
        }

        DropOutcome outcome = _state.Drop(index, dock != null && OnScanner(at), zone, verdict);
        if (outcome != DropOutcome.Queued)
            _waiting.Remove(index);
        switch (outcome)
        {
            case DropOutcome.HandsBack:
                ApplyStack(index);
                stamps.HandBack();
                return;
            case DropOutcome.Scanning:
                paper.SetZone(_state.ZoneOf(index), false);
                StartScan(paper);
                break;
            case DropOutcome.Queued:
                // Drop and go: it waits beside the busy scanner and feeds through at its turn.
                _waiting.Add(index);
                paper.SetZone(_state.ZoneOf(index), false);
                MoveTo(paper, Rest(paper, at, false));
                break;
            case DropOutcome.NotStamped:
                if (stamps != null)
                    stamps.Note("stamp.refused.handBackFirst");
                paper.SetZone(_state.ZoneOf(index), false);
                paper.Shake();
                MoveTo(paper, Rest(paper, at, true));
                break;
            case DropOutcome.Refused:
                paper.SetZone(_state.ZoneOf(index), false);
                paper.Shake();
                MoveTo(paper, Rest(paper, at, false));
                break;
            default:
                zone = _state.ZoneOf(index);
                paper.SetZone(zone, false);
                MoveTo(paper, Rest(paper, at, false));
                if (zone == DeskZone.Desk)
                    Read(paper);
                break;
        }
        ApplyStack(index);
        RefreshHint();
    }

    /// <summary>
    /// A paper dropped on the chip reader that takes it (<paramref name="where"/>:
    /// a card into the slot, a passport onto the pad): inert meanwhile and the
    /// reader's (MachineOccupancy), it shrinks to its docked size (a passport
    /// to fit the pad: DockFit) as it is carried over the casing and docked at
    /// the dock's height and angle (DockPath; upright to the player on the
    /// pad), read (ChipReader's cycle), and carried back out (out of the slot,
    /// or up off the pad) over the casing to where it rests near
    /// <paramref name="back"/> (where it was picked up), keeping its docked
    /// size until it is clear (BUG-24).
    /// </summary>
    private void ReadOnReader(DeskDocument paper, IPaperDock dock, MachineDock where, Vector3 back)
    {
        int index = paper.Index;
        CaseDocument doc = index >= 0 && index < _documents.Count ? _documents[index] : null;
        _docks.Dock(index, DeskMachine.ChipReader);
        paper.SetDocked(true, DockScale(paper, where));
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
                if (!_papers.Contains(paper))
                    return;
                Vector3 home = Rest(paper, back, false);
                Vector3 at = paper.CarriedFrom;
                List<Vector3> outWay = where == MachineDock.Slot
                    ? Points(DockPath.OutOf(Tuple(mouth), slot.way.x, slot.way.z, half, inside, dock.Casing, Tuple(home), Casings(), Grow(paper), config.dockClearance))
                    : Way(at, home, Grow(paper));
                paper.Carry(outWay, Upright, WaySeconds(at, outWay), () => Undock(paper), () => _docks.Leave(index));
                ApplyLive(paper);
            }
            if (doc == null || !hardware.Read(paper, doc.fields, Out))
                Out();
        }, () => _docks.Leave(index));
    }

    /// <summary>The scale <paramref name="paper"/> lies at docked <paramref name="where"/>: its counter size, a passport on the chip reader's pad no wider or deeper than the pad (DockFit, DeskConfigSO.padOverhang).</summary>
    private float DockScale(DeskDocument paper, MachineDock where)
    {
        if (where != MachineDock.Pad || hardware == null || hardware.Reader == null)
            return paper.DockScale;
        Vector2 pad = hardware.Reader.PadExtents(_viewRight, _viewForward);
        return DockFit.Scale(paper.Size.x, paper.Size.y, pad.x, pad.y, paper.DockScale, config.padOverhang);
    }

    /// <summary>
    /// A paper the machine under it will not take (MachineIntake: a paper
    /// with no chip at the chip reader, or the reader busy with another):
    /// the machine says no (IPaperDock.Refuse: the reader's FAULT lamp blinks
    /// with its fault buzz), the paper shakes in place and rests just clear of
    /// the machine (DropResolver, the machine among the obstacles), never
    /// flying back and never through the casing.
    /// </summary>
    private void RefuseAt(DeskDocument paper, IPaperDock dock, Vector3 at)
    {
        dock.Refuse();
        paper.Shake();
        paper.SetZone(_state.ZoneOf(paper.Index), false);
        MoveTo(paper, Rest(paper, at, false));
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

    /// <summary>The machine a thing whose footprint is centred at <paramref name="centre"/> drops on (the chip reader first: it stands clear of the scanner), or null: the footprint's centre decides, not the pointer (DropResolver.DockOf's rule).</summary>
    private IPaperDock DockAt(Vector3 centre)
    {
        IPaperDock found = null;
        foreach (IPaperDock dock in Docks())
            if (dock.Contains(centre) && (found == null || dock.Machine == DeskMachine.ChipReader))
                found = dock;
        return found;
    }

    /// <summary>The machines' casings today, measured for every way across the desk.</summary>
    private readonly List<MachineBox> _casings = new List<MachineBox>();

    /// <summary>
    /// Measures the machines now (Docks): their casings for the ways over them,
    /// and their footprints in the desk's obstacle registry (DeskObstacles:
    /// the scanner as high as its open lid's sweep, the chip reader; the UV
    /// torch's cradle until TOOLS registers its own): what a resting paper
    /// keeps clear of and a held one is drawn above.
    /// </summary>
    private void MeasureMachines()
    {
        _casings.Clear();
        float desk = surface != null ? surface.transform.position.y : 0f;
        bool scannerOn = false, readerOn = false;
        foreach (IPaperDock dock in Docks())
        {
            MachineBox box = dock.Casing;
            _casings.Add(box);
            if (dock.Machine == DeskMachine.Scanner)
            {
                scannerOn = true;
                DeskObstacles.Desk.Register(ScannerObstacle, WorldFootprint(Corners(box, desk)), scanner.LidSweepTop - desk);
            }
            else
            {
                readerOn = true;
                DeskObstacles.Desk.Register(ReaderObstacle, WorldFootprint(Corners(box, desk)), box.Top - desk);
            }
        }
        if (!scannerOn)
            DeskObstacles.Desk.Unregister(ScannerObstacle);
        if (!readerOn)
            DeskObstacles.Desk.Unregister(ReaderObstacle);
        if (hardware != null && hardware.TryTorchFootprint(out Vector3[] cradle))
            DeskObstacles.Desk.Register(TorchObstacle, WorldFootprint(cradle), 0f);
        else
            DeskObstacles.Desk.Unregister(TorchObstacle);
    }

    /// <summary>The casings, measured now.</summary>
    private List<MachineBox> Casings()
    {
        MeasureMachines();
        return _casings;
    }

    /// <summary>The world x, z bounds of <paramref name="corners"/> (DeskObstacles' frame).</summary>
    private static DeskRect WorldFootprint(IEnumerable<Vector3> corners)
    {
        float minX = float.MaxValue, minZ = float.MaxValue, maxX = float.MinValue, maxZ = float.MinValue;
        foreach (Vector3 c in corners)
        {
            minX = Mathf.Min(minX, c.x);
            maxX = Mathf.Max(maxX, c.x);
            minZ = Mathf.Min(minZ, c.z);
            maxZ = Mathf.Max(maxZ, c.z);
        }
        return new DeskRect((minX + maxX) / 2f, (minZ + maxZ) / 2f, maxX - minX, maxZ - minZ);
    }

    /// <summary>The held ratio for a thing on the desk at height <paramref name="deskY"/> seen through <paramref name="eye"/> (HeldOnTop.Ratio: in front of every registered obstacle, at least DeskConfigSO.heldClearance high).</summary>
    private float HeldRatio(Camera eye, float deskY)
    {
        if (eye == null || config == null)
            return 1f;
        float clear = Mathf.Max(DeskObstacles.Desk.MaxHeight, config.heldClearance);
        return HeldOnTop.Ratio(eye.transform.position.y - deskY, clear, config.heldLeastRatio);
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
    /// docked: ApplyStack).
    /// </summary>
    private static Vector3 DockedRoot(DeskDocument paper, Vector3 point, MachineDock where) =>
        where == MachineDock.Slot ? point - Vector3.up * (paper.Thickness / 2f + DockGap) : point;

    /// <summary>How far a docked paper's bottom lies over its bed (metres): clear of it for the depth buffer.</summary>
    private const float DockGap = 0.0005f;

    /// <summary>The paper is clear of its machine again (its carry out has landed): it leaves the machine's hold, eases back to its zone's size, comes to the top of the stack and takes input.</summary>
    private void Undock(DeskDocument paper)
    {
        _docks.Leave(paper.Index);
        paper.SetDocked(false);
        _stack.BringToFront(paper.Index);
        ApplyStack();
        ApplyLive(paper);
    }

    /// <summary>A scanned paper comes off the bed to <paramref name="rest"/>, upright, over the casing, keeping its docked size until it lands clear of the scanner (Undock).</summary>
    private void CarryOut(DeskDocument paper, Vector3 rest)
    {
        int index = paper.Index;
        Vector3 from = paper.CarriedFrom;
        List<Vector3> way = Way(from, rest, Grow(paper));
        paper.Carry(way, Upright, WaySeconds(from, way), () => Undock(paper), () => _docks.Leave(index));
        ApplyLive(paper);
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
    /// A drag cut short (cancelled by a right-click, Esc or a view key, the
    /// focus lost, or the paper disabled mid-drag: its input taken away, or the
    /// case torn down, so no release comes): a paper still on the desk takes
    /// its zone's size again and slides back to where it was picked up, on
    /// top; a paper already given back is left to the teardown. Nothing is
    /// committed (no hand-back, no scan).
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
        if (paper == null || _state == null || !_state.CanDrag(paper.Index))
            return;

        paper.SetZone(_state.ZoneOf(paper.Index), false);
        _stack.BringToFront(paper.Index);
        MoveTo(paper, Rest(paper, drag.PickUpPosition, false));
        ApplyStack(paper.Index);
    }

    /// <summary>The zone a point on the desk lies in: the counter's strip, else the desk.</summary>
    private DeskZone ZoneAt(Vector3 point) => counter != null && counter.Contains(point) ? DeskZone.Counter : DeskZone.Desk;

    /// <summary>The counter shows while a traveller's papers are on the desk, "▲ HAND BACK ▲" once the passport carries its verdict, lit while a dragged paper's pointer is over it.</summary>
    private void ShowCounter()
    {
        if (counter != null)
            counter.Show(_papers.Count > 0, stamps != null && stamps.HasVerdict, _overCounter);
    }

    /// <summary>
    /// Where <paramref name="paper"/>, about to lie with its root at
    /// <paramref name="at"/> at its zone's size, rests (DropResolver.Rest, in
    /// the office view's frame on the desk): <paramref name="at"/> itself
    /// unless part of it would lie outside what the reading view shows (a
    /// desk paper) or the desk, over a registered obstacle (DeskObstacles), in
    /// the area the scanner hides from the camera, or (<paramref name="offCounter"/>:
    /// a desk paper refused there) on the counter; then the least move clear
    /// of all of them at once.
    /// </summary>
    private Vector3 Rest(DeskDocument paper, Vector3 at, bool offCounter)
    {
        if (surface == null)
            return at;
        paper.RestCorners(new Vector3(at.x, surface.transform.position.y, at.z), _corners);
        Vector3 rest = RestOf(InViewFrame(_corners), at, offCounter, paper.Zone == DeskZone.Desk);
        return new Vector3(rest.x, at.y, rest.z);
    }

    /// <summary>Where a thing whose footprint is <paramref name="footprint"/> (the view's frame) with its root at <paramref name="at"/> rests (DropResolver.Rest, as Rest; the rulebook folder too): kept in what the reading view shows (a thing on the desk), clear of every obstacle, the scanner's hidden area and, <paramref name="offCounter"/>, the counter.</summary>
    private Vector3 RestOf(DeskRect footprint, Vector3 at, bool offCounter, bool inView = true)
    {
        MeasureMachines();
        DeskObstacles.Desk.Footprints(_footprints);
        _keepOut.Clear();
        foreach (DeskRect f in _footprints)
            _keepOut.Add(InViewFrame(WorldCorners(f, surface.transform.position.y)));
        if (scanner != null && !_scanners.Hidden && scanner.OnDesk)
            _keepOut.Add(ScannerClearance.Blocked(InViewFrame(scanner.Corners()), _scannerShadow));
        if (offCounter && counter != null && counter.TryNearEdge(out float near))
            _keepOut.Add(new DeskRect(0f, near + CounterKeepOut / 2f, CounterKeepOut, CounterKeepOut));
        DeskRect area = RestArea(footprint, inView);
        (float x, float y) = DropResolver.Rest(footprint, area, _keepOut);
        if (Mathf.Approximately(x, footprint.CentreX) && Mathf.Approximately(y, footprint.CentreY))
            return at;
        return at + _viewRight * (x - footprint.CentreX) + _viewForward * (y - footprint.CentreY);
    }

    /// <summary>How large the counter's keep-out is (metres, every way past its near edge): far more than the desk.</summary>
    private const float CounterKeepOut = 100f;

    /// <summary>The area a resting thing (its <paramref name="footprint"/>, the view's frame) lies wholly inside: the desk's, its centre anywhere a drag may take it (DeskSurface's clamp area, grown by its size), and on the desk (<paramref name="inView"/>) what the reading view shows (DeskConfigSO.restViewport: BUG-6, the whole paper stays in view; the desk's alone without the reading view, or where they do not meet).</summary>
    private DeskRect RestArea(DeskRect footprint, bool inView)
    {
        DeskRect clamp = InViewFrame(surface.Corners());
        var desk = new DeskRect(clamp.CentreX, clamp.CentreY, clamp.Width + footprint.Width, clamp.Height + footprint.Height);
        if (!inView || deskView == null)
            return desk;
        Rect v = config.restViewport;
        float h = surface.transform.position.y;
        if (!deskView.TryViewPoint(new Vector2(v.xMin, v.yMin), h, out Vector3 bl) || !deskView.TryViewPoint(new Vector2(v.xMax, v.yMin), h, out Vector3 br)
            || !deskView.TryViewPoint(new Vector2(v.xMin, v.yMax), h, out Vector3 tl) || !deskView.TryViewPoint(new Vector2(v.xMax, v.yMax), h, out Vector3 tr))
            return desk;
        DeskRect a = InViewFrame(new[] { bl }), b = InViewFrame(new[] { br }), c = InViewFrame(new[] { tl }), d = InViewFrame(new[] { tr });
        float left = Mathf.Max(Mathf.Max(a.CentreX, c.CentreX), desk.CentreX - desk.Width / 2f);
        float right = Mathf.Min(Mathf.Min(b.CentreX, d.CentreX), desk.CentreX + desk.Width / 2f);
        float nearEdge = Mathf.Max(Mathf.Max(a.CentreY, b.CentreY), desk.CentreY - desk.Height / 2f);
        float farEdge = Mathf.Min(Mathf.Min(c.CentreY, d.CentreY), desk.CentreY + desk.Height / 2f);
        if (right <= left || farEdge <= nearEdge)
            return desk;
        return new DeskRect((left + right) / 2f, (nearEdge + farEdge) / 2f, right - left, farEdge - nearEdge);
    }

    /// <summary>The four corners of a world x, z footprint at height <paramref name="y"/>.</summary>
    private static Vector3[] WorldCorners(DeskRect footprint, float y)
    {
        float hx = footprint.Width / 2f, hz = footprint.Height / 2f;
        return new[]
        {
            new Vector3(footprint.CentreX - hx, y, footprint.CentreY - hz), new Vector3(footprint.CentreX + hx, y, footprint.CentreY - hz),
            new Vector3(footprint.CentreX - hx, y, footprint.CentreY + hz), new Vector3(footprint.CentreX + hx, y, footprint.CentreY + hz),
        };
    }

    /// <summary>The footprint on the desk (the view's frame) of everything drawn under <paramref name="root"/>.</summary>
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
        float y = surface.transform.position.y;
        footprint = InViewFrame(new[] { new Vector3(min.x, y, min.z), new Vector3(max.x, y, min.z), new Vector3(min.x, y, max.z), new Vector3(max.x, y, max.z) });
        return true;
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

    /// <summary>A dropped paper goes to <paramref name="rest"/> (DropResolver): nowhere when it already lies there (it rests exactly where it was released); else the short way, over any casing.</summary>
    private void MoveTo(DeskDocument paper, Vector3 rest)
    {
        if ((rest - paper.transform.position).sqrMagnitude > 1e-8f)
            Slide(paper, rest, paper.transform.rotation);
        else
            ApplyLive(paper);
    }

    /// <summary>Slides a paper, turning it to lie at <paramref name="facing"/> on the way (eased with the slide): from where it shows, over every machine's casing it comes over (DockPath.Over: never through one); it is inert while sliding and its liveness is re-applied when it lands.</summary>
    private void Slide(DeskDocument paper, Vector3 target, Quaternion facing)
    {
        Vector3 from = paper.CarriedFrom;
        List<Vector3> way = Way(from, target, Grow(paper));
        paper.Carry(way, facing, WaySeconds(from, way), () => ApplyLive(paper));
        ApplyLive(paper);
    }

    /// <summary>A paper on the desk takes input while the papers are allowed, DeskPapers lets it be dragged, it is not sliding and it is in no machine, and is in the raycast while the papers are allowed.</summary>
    private void ApplyLive(DeskDocument paper)
    {
        bool live = _live && !paper.IsSliding && _state != null && _state.CanDrag(paper.Index) && !_docks.IsDocked(paper.Index);
        paper.SetLive(live, live, _live);
    }

    /// <summary>
    /// Stack heights (PaperLayers.Lifts): each paper and the rulebook lifted
    /// by its place in the stack and the thickness of all under it, the
    /// lowest clear of the desk top (Saleh's 1007d playtest: papers clipped
    /// into the table), the top one DeskConfigSO.topLift higher (BUG-40b: the
    /// order reads at a glance); the dragged paper or rulebook lifted above
    /// the whole stack (the pick-up pop); a docked paper its own thickness over
    /// its dock. A reorder shows in the same frame (every resting paper's lift
    /// is set at once); only <paramref name="settling"/> (a paper just let go)
    /// and the dragged paper ease on their springs. Each paper whose rank
    /// changed raises DeskEvents.StackChanged (DOC-LOOK's contact shadow).
    /// </summary>
    private void ApplyStack(int settling = int.MinValue)
    {
        // One stacking rule (PaperLayers): each one's bottom lies over every part of the one under it, so the paper on top hides all of each paper under it.
        _stackThickness.Clear();
        for (int i = 0; i < _stack.Count; i++)
            _stackThickness.Add(Thickness(_stack[i]));
        float[] lifts = PaperLayers.Lifts(_stackThickness, config.paperStackStep);
        if (lifts.Length > 0)
            lifts[lifts.Length - 1] += config.topLift;
        float top = lifts.Length > 0 ? lifts[lifts.Length - 1] + PaperLayers.PartsDepth : PaperLayers.StackStep(config.paperStackStep);
        foreach (DeskDocument paper in _papers)
        {
            if (paper == null)
                continue;
            int index = paper.Index;
            if (paper.IsDocked)
                paper.SetLift(paper.Thickness + DockGap);
            else if (index == _dragged)
                paper.SetLift(top + paper.Thickness + config.dragLift);
            else
                paper.SetLift(LiftOf(index, lifts), index != settling);
            int rank = _stack.IndexOf(index);
            if (!_ranks.TryGetValue(index, out int was) || was != rank)
            {
                _ranks[index] = rank;
                DeskEvents.RaiseStackChanged(paper, rank);
            }
        }
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

    /// <summary>A paper slides onto the bed and its scan starts: it is the scanner's (MachineOccupancy) at its docked size, the lid held open until it lies there (cut short: the lid lets go), the relay click (scanner_start), the glowing bar crossing the glass as the scan runs (DeskScanner.Sweep); the done beep is DeskScanner.Pulse's.</summary>
    private void StartScan(DeskDocument paper)
    {
        // over the casing and down onto the glass, square to the bed, the lid held open until it lies there (the scan waits: Update)
        _docks.Dock(paper.Index, DeskMachine.Scanner);
        paper.SetDocked(true, paper.DockScale);
        ApplyStack();
        scanner.HoldOpen(true);
        Vector3 from = paper.CarriedFrom;
        List<Vector3> way = Points(DockPath.OnTo(Tuple(from), Tuple(scanner.DockPoint(MachineDock.Bed)), Casings(), Grow(paper), config.dockClearance));
        paper.Carry(way, scanner.DockFacing(MachineDock.Bed), WaySeconds(from, way), () =>
        {
            scanner.HoldOpen(false);
            ApplyLive(paper);
        }, () => scanner.HoldOpen(false));
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
