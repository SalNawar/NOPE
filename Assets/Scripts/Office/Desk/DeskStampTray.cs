using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// The stamp rack with the two daters (the desk machine spec §1-2; Saleh
/// 2026-10-07: "the stamps need to be retro two-click stamps … one saying
/// approved, one denied"; before it, 2026-10-06: Papers, Please's grey tab,
/// TAB, and "two stamps that I physically move ... I should be able to stamp
/// anywhere on the document"). The rack is FIXED (Saleh's D4, the desk plan
/// v2 of 2026-10-18, after his 1018a playtest: "we need a better location for
/// the stamp drawer it is in the way"; he rejected the slide-out bar on
/// 2026-10-08): the brass drawer's box stands on its own home off the paper
/// mat (DeskLayoutSettings.stampRack, laid by the office binder: Lay) and
/// never moves; laid, it gets a drawn contact shadow in the cel kit's ink
/// under its footprint (PaperDepth.Shadow, as the papers have). Its brass
/// mechanism (BrassDrawer: a lever, a rack and pinion, a cradle per dater)
/// stands the daters up the moment a case begins, DENIED then APPROVED, each
/// locking upright with a click, and folds them down as it ends
/// (DrawerSequence's cradles; the drawer's old invisible slide, which held
/// them back about 0.34 s a case, is gone: BUG-73b). The tab on the screen's
/// top edge ("STAMPS" beside "TAB") and the TAB key (OfficeControls) take the
/// player to the stamps: the reading view, which frames the rack, and the
/// daters lift in their cradles to answer (ToggleRack; TAB again goes back;
/// the tab shows in the office view only). A dater takes input once its
/// cradle is locked and while it is not mid-stroke. Without the drawer's art
/// the built rack (a lip with end caps) holds them upright all the time. The
/// rack holds the DENIED dater (left) and the APPROVED dater (right),
/// self-inking daters after Saleh's S-401 reference, built under a prop
/// contract (a root with Body, Frame, Die, Wheels and Button: the art may
/// replace the meshes): a glossy body, deep green on APPROVED and red on
/// DENIED (Saleh 2026-10-07: "easy to tell apart"), with a window on top
/// showing the die's print, a white frame, the date wheels seen through it
/// and a lighter side button. In the rack a dater the pointer is over lifts a
/// few millimetres and, as the hand nears, tips toward it. A dater is moved,
/// not the paper: left-press and drag it (its DeskDraggable; it is the desk's
/// hold while carried, HoldRegistry) and its die is carried straight above
/// the spot the pointer shows on the papers; letting go strokes it on what
/// the player sees under the pointer (StampTarget.Pick: the top-most thing,
/// a folder's edge passed over for the paper under it), then it goes back to
/// its place in the rack on a firm spring and seats with a click. Stamping is
/// drag-only (Saleh's 1016a playtest: "should be dragging"): a click or a
/// held press on a hanging dater strokes nothing. A stroke (all motion on
/// MotionKnobs' springs): the press clack as it starts (the die flipping off
/// the pad: Saleh's "cha-ka", the deep one on DENIED, the pitch ±3 % a press),
/// the dater rises a little (stampLift) and slams down exactly onto the
/// surface it met (StampTarget.StrokeDrop: no sink into a stack or the
/// folder), its body sinks over its frame against the stiff Dater spring and
/// squashes; as it bottoms out the impression lands (DaterImpressionArt: the
/// outline word, the date in red, BY: the clerk's id; its ink fading with the
/// pad over the shift, DaterInk) and the hit comes (FeelDirector: the
/// hit-stop and the camera's impulse); on release the second clack and the
/// body springs up past its rest. The verdict's ink is owed from the press
/// (StampInk): whatever ends the stroke first, it prints exactly once, so
/// the ink always matches the recorded verdict. Each morning the date
/// wheels still show yesterday; the first time a dater is picked up that day
/// they roll to today, one ratchet click per notch (DaterWheels). The side
/// button re-inks the pad (a squish). A stroke prints only on the passport,
/// anywhere on it, once (StampFlow: the other dater, another paper or the
/// rulebook are refused with a thunk, a bounce off the paper and a note);
/// over the bare desk the die thumps the wood (stamp_miss) and the dater goes
/// back. Then the stamped passport dropped on the counter hands the papers
/// back (HandBack), and that commits the verdict (Saleh 2026-10-07: no extra
/// step): APPROVED spins the portal up, DENIED sends the traveller back; the
/// desk's DETAIN button commits the third verdict any time (Commit). A
/// right-click or Esc drops a carried dater back into the rack
/// (ControlRules.BackOut). The daters take input while BoothRules.StampsLive;
/// the tab shows while BoothRules.PropsLive, in the office view. Reduced
/// Motion cuts every stroke and drops the bounce. Build Office UI builds the
/// tab, the hint, the audio source, the rack, its shadow and the daters; the
/// office binder lays the rack.
/// </summary>
public sealed class DeskStampTray : MonoBehaviour
{
    /// <summary>The desk tuning (the daters' hover, the rack's scale, the ink, a note's time, the contact shadow's spread and darkness).</summary>
    [SerializeField] private DeskConfigSO config;

    /// <summary>The reading view (optional): TAB and the tab take the player to it, where the rack is framed (ToggleRack); the tab hides while it is on.</summary>
    [SerializeField] private DeskView deskView;

    /// <summary>The desk plane (the daters are dragged over it).</summary>
    [SerializeField] private DeskSurface surface;

    /// <summary>The rack in the office (the brass drawer or the built lip, the two daters): always shown, fixed on its home.</summary>
    [SerializeField] private Transform rack;

    /// <summary>The brass drawer's mechanism on the rack (optional: the built rack has none; its daters stand all the time).</summary>
    [SerializeField] private BrassDrawer drawer;

    /// <summary>The rack's contact shadow (optional; the papers' shadow material): Lay draws it under the rack's footprint.</summary>
    [SerializeField] private MeshFilter contactShadow;

    /// <summary>The tab on the screen's top edge ("STAMPS" beside its "TAB" key): a click takes the player to the stamps (ToggleRack).</summary>
    [SerializeField] private Button tab;

    /// <summary>The APPROVED dater's click box: its root (the prop contract's Body, Frame, Die, Wheels and Button are its children), with its DeskDraggable.</summary>
    [SerializeField] private Clickable approvedStamp;

    /// <summary>The DENIED dater's click box.</summary>
    [SerializeField] private Clickable deniedStamp;

    /// <summary>The APPROVED dater's die: its centre, at the dater's foot, is where it presses.</summary>
    [SerializeField] private Transform approvedDie;

    /// <summary>The DENIED dater's die.</summary>
    [SerializeField] private Transform deniedDie;

    /// <summary>The hint at the top right (the next step, or a note); its parent is its plate.</summary>
    [SerializeField] private TMP_Text hint;

    /// <summary>Plays the clacks, the wheels' clicks, the squish and a refusal's thunk (optional).</summary>
    [SerializeField] private AudioSource sound;

    /// <summary>The physics layers a dater's aim meets documents on (the Interactable layer).</summary>
    [SerializeField] private LayerMask paperLayers = ~0;

    /// <summary>How near the rulebook folder's edge (metres) a dater's aim may meet it and still land on the paper under that edge (StampTarget.Pick).</summary>
    [SerializeField, Min(0f)] private float folderEdge = 0.02f;

    /// <summary>The papers' style: the daters print its verdict words (FormStyleSO.approvedStamp, deniedStamp).</summary>
    [SerializeField] private FormStyleSO style;

    /// <summary>The face the date and the BY line print in (its SDF atlas readable: GeistMono-Bold SDF).</summary>
    [SerializeField] private TMP_FontAsset daterFont;

    /// <summary>The APPROVED dater's clacks: the press (the die flips off the pad) and the release (Saleh's stamp_real_cha_ka, split at its gap).</summary>
    [SerializeField] private AudioClip approvedPress, approvedRelease;

    /// <summary>The DENIED dater's clacks (the deep cha-ka).</summary>
    [SerializeField] private AudioClip deniedPress, deniedRelease;

    /// <summary>The angle (degrees about a wheel's axis) that turns its first notch to face the chair; notch k is k steps on.</summary>
    [SerializeField] private float wheelFacing;

    /// <summary>How far a refused dater bounces off the paper (metres; the feel bible's B4: 6 mm, Elastic).</summary>
    private const float Bounce = 0.006f;

    /// <summary>The longest ray a dater casts straight down, and the longest pointer ray a carried dater aims along (metres).</summary>
    private const float RayLength = 0.5f, AimLength = 10f;

    /// <summary>How far (metres, on the desk) the point the pointer showed may lie from straight under the die before the press looks straight down instead (the carry clamped at the desk's edge).</summary>
    private const float AimSlack = 0.02f;

    /// <summary>How far a print may turn either way (degrees): a hand's slight twist.</summary>
    private const float PrintTwist = 1.6f;

    /// <summary>How much the press's pitch varies either way (Saleh's sounds: ±3 % a press).</summary>
    private const float PitchSpread = 0.03f;

    /// <summary>The rack's answer to the hand (the feel bible's B3): a dater the pointer is over lifts this far in its cradle (metres), and one the hand nears within TipReach (metres) tips toward it, up to TipDegrees.</summary>
    private const float HoverLift = 0.003f, TipReach = 0.06f, TipDegrees = 5f;

    /// <summary>How far a dater seating in its cradle dips (metres: the cradle's lock bounce), and how far its handle wobbles as the stroke springs up (degrees: its follow-through).</summary>
    private const float SeatDip = 0.0015f, WobbleDegrees = 2f;

    /// <summary>How far above the desk the rack's contact shadow lies (metres): clear of the desk's top for the depth test.</summary>
    private const float ShadowLift = 0.0004f;

    /// <summary>Where a stroke is.</summary>
    private enum Stroke
    {
        None,
        Rise,
        Down,
        Up
    }

    /// <summary>One dater of the rack: what it prints, its parts, its place in the rack and its motion. While carried it is the desk's hold (HoldRegistry): a cancel drops it back into the rack.</summary>
    private sealed class Handle : IHoldable
    {
        public DeskStamp Kind;

        /// <summary>Its lane in the drawer (DrawerSequence.Denied or Approved).</summary>
        public int Lane;
        public Clickable Click;
        public BoxCollider Box;
        public Transform Die;
        public DeskDraggable Drag;
        public Clickable Reink;
        public Transform Body;
        public Vector3 BodyHome;

        /// <summary>The body's foot (its lowest point, body space): it squashes about it.</summary>
        public float BodyFoot;
        public Transform[] Wheels = Array.Empty<Transform>();
        public Quaternion[] WheelBase = Array.Empty<Quaternion>();
        public Spring[] WheelAngle = Array.Empty<Spring>();
        public Renderer Window;
        public Texture2D WindowPrint;

        /// <summary>The wheels' rubber bands (their labels painted for the day's decade).</summary>
        public Texture2D[] Bands = new Texture2D[3];

        /// <summary>Its place and its upright turn in the rack (local).</summary>
        public Vector3 Home;
        public Quaternion HomeTurn = Quaternion.identity;

        /// <summary>Where its stroke started (local): it rises and drops from there.</summary>
        public Vector3 PressedAt;

        /// <summary>When the stroke's rise began.</summary>
        public float RiseFrom;

        /// <summary>How far this stroke drops its die from where it was let go to the surface it met (metres).</summary>
        public float Drop;

        /// <summary>True for a stroke over the bare desk (the wood's thump: no print, no hit).</summary>
        public bool Miss;

        /// <summary>The stroke's depth below PressedAt (metres: the drop, then the body's sink; negative while risen), and the refusal's bounce off the paper.</summary>
        public Spring Depth, Sway;

        /// <summary>The bounce's offset as last applied.</summary>
        public float SwayShown;

        /// <summary>The body's squash on impact (a share of MotionKnobs.stampSquash) and the handle's follow-through wobble (degrees).</summary>
        public Spring Squash, Wobble;

        /// <summary>The hover lift (0 to 1 of HoverLift), the tip toward the hand (degrees) and the seat's dip (metres) in the rack.</summary>
        public Spring Lift, Tip, Seat;

        /// <summary>Which way the hand is, in the rack's plane (local): the dater tips that way.</summary>
        public Vector3 Toward = Vector3.back;

        public Stroke Stroke;
        public bool Contact;
        public float ContactAt;

        /// <summary>The print the stroke owes the passport (StampInk), the paper it lands on and where.</summary>
        public readonly StampInk Ink = new StampInk();
        public DeskDocument PrintOn;
        public Vector2 PrintAt;

        /// <summary>The way back to the rack: 0 to 1 on a spring.</summary>
        public Spring Back;
        public bool Returning;
        public Vector3 ReturnFrom;

        /// <summary>Prints since its pad was last inked, and true once its wheels rolled today.</summary>
        public int Prints;
        public bool Rolled;

        /// <summary>The wheels' clicks still to come in the morning roll (the wheel of each), and when the next one comes.</summary>
        public readonly Queue<int> Clicks = new Queue<int>();
        public float NextClick;

        /// <summary>The hold is cancelled (another hold began, the focus was lost): the drag is cut and the dater goes back to the rack.</summary>
        public void CancelHold()
        {
            if (Drag != null)
                Drag.Cancel();
        }
    }

    private readonly StampFlow _flow = new StampFlow();
    private readonly RaycastHit[] _hits = new RaycastHit[64];
    private readonly List<StampCandidate> _candidates = new List<StampCandidate>();
    private readonly List<(Component thing, Vector3 point)> _met = new List<(Component, Vector3)>();
    private readonly List<BoxCollider> _boxes = new List<BoxCollider>();
    private Handle[] _handles = Array.Empty<Handle>();
    private Handle _carried;
    private int _passport = -1;
    private bool _live;

    /// <summary>The pointer's ray the carried dater last aimed along (its die hangs straight above where it met the papers), and whether there is one.</summary>
    private Ray _aimRay;
    private bool _aimed;

    /// <summary>The cradles' motion (DrawerSequence: up while a case is open).</summary>
    private readonly DrawerSequence _drawer = new DrawerSequence();

    /// <summary>The rack's contact shadow's mesh (made when the rack is laid).</summary>
    private Mesh _shadowMesh;

    /// <summary>True while BoothRules.PropsLive shows the tab (it then shows in the office view only).</summary>
    private bool _tabShown;
    private string _noteKey;
    private float _noteUntil;
    private AudioClip _thunk;
    private bool _dated;
    private DateTime _today;
    private string _dateText = string.Empty, _byLine = string.Empty;
    private int _day;
    private int _presses;

    /// <summary>True while the passport carries a verdict (stamped; handed back or not).</summary>
    public bool HasVerdict => _flow.Verdict != DeskStamp.None;

    /// <summary>True while a traveller stands at the desk with their case, not yet committed (DETAIN can take them).</summary>
    public bool TravellerHere => _flow.TravellerHere && !_flow.Committed;

    /// <summary>True while a dater is dragged over the desk (a right-click or Esc drops it back into the rack: CancelCarry).</summary>
    public bool IsCarrying => _carried != null;

    /// <summary>The rack on the desk (the guide's "stamp_rack" target), or null.</summary>
    public Transform Rack => rack;

    /// <summary>True while a dater strokes, bounces, goes back to the rack, seats or rolls its wheels (the probes wait for it).</summary>
    public bool IsMoving
    {
        get
        {
            foreach (Handle h in _handles)
                if (h.Stroke != Stroke.None || h.Returning || !h.Sway.AtRest || !h.Seat.AtRest || !h.Squash.AtRest || h.Clicks.Count > 0)
                    return true;
            return false;
        }
    }

    /// <summary>What the last press did (the probes read it).</summary>
    public StampPress LastPress { get; private set; }

    /// <summary>Where the last press met a paper, the rulebook or the desk (the surface the die strokes onto; the probes read it).</summary>
    public Vector3 LastPressPoint { get; private set; }

    /// <summary>Raised when the verdict or the case changes (the counter's label, the booth's rules, the DETAIN button).</summary>
    public event Action Changed;

    /// <summary>Raised when the case's verdict is committed (the hand-back APPROVED or DENIED, the DETAIN button DETAINED).</summary>
    public event Action<DeskStamp> Decided;

    private void Awake()
    {
        if (tab != null)
            tab.onClick.AddListener(ToggleRack);
        if (deskView != null)
            deskView.Changed += ShowTab;
        _handles = new[] { MakeHandle(DeskStamp.Approved, approvedStamp, approvedDie), MakeHandle(DeskStamp.Denied, deniedStamp, deniedDie) };
        _thunk = CueSounds.Tone("StampThunk", 70f, 0.16f, 0.7f);
        Show();
    }

    /// <summary>A dater of the rack: its parts by the prop contract's names, its drag (carried stampHover over the desk, its die over the pointer's spot) and its side button (re-ink).</summary>
    private Handle MakeHandle(DeskStamp kind, Clickable click, Transform die)
    {
        var handle = new Handle { Kind = kind, Lane = kind == DeskStamp.Denied ? DrawerSequence.Denied : DrawerSequence.Approved, Click = click, Die = die, Back = Spring.At(1f) };
        if (click == null)
            return handle;
        Transform root = click.transform;
        handle.Home = root.localPosition;
        handle.HomeTurn = root.localRotation;
        handle.Box = click.GetComponent<BoxCollider>();
        handle.Body = root.Find("Body");
        if (handle.Body != null)
        {
            handle.BodyHome = handle.Body.localPosition;
            handle.BodyFoot = Foot(handle.Body);
        }
        Transform window = handle.Body != null ? handle.Body.Find("Window") : null;
        handle.Window = window != null ? window.GetComponent<Renderer>() : null;
        Transform wheels = root.Find("Wheels");
        if (wheels != null)
        {
            handle.Wheels = new[] { wheels.Find("Day"), wheels.Find("Month"), wheels.Find("Year") };
            handle.WheelBase = new Quaternion[3];
            handle.WheelAngle = new Spring[3];
            for (int i = 0; i < 3; i++)
                handle.WheelBase[i] = handle.Wheels[i] != null ? handle.Wheels[i].localRotation : Quaternion.identity;
        }
        Transform button = root.Find("Button");
        handle.Reink = button != null ? button.GetComponent<Clickable>() : null;
        if (handle.Reink != null)
            handle.Reink.onClick.AddListener(() => Reink(handle));
        handle.Drag = click.GetComponent<DeskDraggable>();
        if (handle.Drag != null)
        {
            handle.Drag.Init(surface, config != null ? config.stampHover : 0f, die, Aim);
            handle.Drag.DragBegan += _ => PickUp(handle);
            handle.Drag.DragEnded += (_, _) => LetGo(handle);
            handle.Drag.DragCancelled += _ => PutBack(handle);
        }
        return handle;
    }

    /// <summary>The lowest point of <paramref name="body"/>'s meshes in its own space (0 without any).</summary>
    private static float Foot(Transform body)
    {
        float foot = float.MaxValue;
        foreach (MeshFilter mesh in body.GetComponentsInChildren<MeshFilter>(true))
        {
            if (mesh.sharedMesh == null || mesh.GetComponent<TMP_Text>() != null)
                continue;
            Bounds b = mesh.sharedMesh.bounds;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = new Vector3((i & 1) == 0 ? b.min.x : b.max.x, (i & 2) == 0 ? b.min.y : b.max.y, (i & 4) == 0 ? b.min.z : b.max.z);
                foot = Mathf.Min(foot, body.InverseTransformPoint(mesh.transform.TransformPoint(corner)).y);
            }
        }
        return foot == float.MaxValue ? 0f : foot;
    }

    private void OnDestroy()
    {
        if (deskView != null)
            deskView.Changed -= ShowTab;
        if (_thunk != null)
            Destroy(_thunk);
        if (_shadowMesh != null)
            Destroy(_shadowMesh);
        foreach (Handle handle in _handles)
        {
            HoldRegistry.Desk.End(handle);
            if (handle.WindowPrint != null)
                Destroy(handle.WindowPrint);
            foreach (Texture2D band in handle.Bands)
                if (band != null)
                    Destroy(band);
        }
    }

    /// <summary>
    /// Lays the rack on <paramref name="bay"/> of the desk's plan (the office
    /// binder, once the desk is bound; DeskLayoutSettings.stampRack: its home
    /// off the paper mat): at DeskConfigSO.stampDrawerScale, facing the chair,
    /// its box standing on the desk, its contact shadow under it.
    /// </summary>
    public void Lay(DeskFrame frame, DeskBay bay)
    {
        if (rack == null || config == null || !frame.Place(rack, bay, 0f))
            return;
        rack.localScale = Vector3.one * config.stampDrawerScale;
        rack.position -= Vector3.up * Underside();
        rack.gameObject.SetActive(true);
        LayShadow();
    }

    /// <summary>The meshes of the rack's box: the drawer's Tray part or, for the built rack, every part.</summary>
    private IEnumerable<MeshFilter> BoxMeshes()
    {
        Transform box = drawer != null ? drawer.transform.Find("Tray") : null;
        foreach (MeshFilter mesh in (box != null ? box : rack).GetComponentsInChildren<MeshFilter>(true))
            if (mesh.sharedMesh != null && mesh.GetComponent<TMP_Text>() == null && mesh != contactShadow)
                yield return mesh;
    }

    /// <summary>The rack's box's bounds in the rack's own space (its meshes' corners), and false without any.</summary>
    private bool BoxBounds(out Bounds bounds)
    {
        bounds = default;
        bool any = false;
        foreach (MeshFilter mesh in BoxMeshes())
        {
            Bounds b = mesh.sharedMesh.bounds;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = new Vector3((i & 1) == 0 ? b.min.x : b.max.x, (i & 2) == 0 ? b.min.y : b.max.y, (i & 4) == 0 ? b.min.z : b.max.z);
                Vector3 local = rack.InverseTransformPoint(mesh.transform.TransformPoint(corner));
                if (!any)
                    bounds = new Bounds(local, Vector3.zero);
                else
                    bounds.Encapsulate(local);
                any = true;
            }
        }
        return any;
    }

    /// <summary>How far below the rack's origin its box reaches (metres, as laid), from the meshes' own bounds.</summary>
    private float Underside() => BoxBounds(out Bounds b) ? b.min.y * rack.lossyScale.y : 0f;

    /// <summary>
    /// The rack's contact shadow (BUG-16a: the rack floated): the papers'
    /// drawn shadow (PaperDepth.Shadow, the cel kit's ink, DeskConfigSO's
    /// shadow spread, lean and darkness) under the box's footprint, on the
    /// desk; nothing without a shadow or a box.
    /// </summary>
    private void LayShadow()
    {
        if (contactShadow == null || !BoxBounds(out Bounds box))
            return;
        Transform t = contactShadow.transform;
        float scale = Mathf.Max(1e-4f, rack.lossyScale.x);
        t.localPosition = new Vector3(box.center.x, box.min.y + ShadowLift / scale, box.center.z);
        t.localRotation = Quaternion.Euler(90f, 0f, 0f); // the face's x along the rack's right, its y away from the chair, its z down
        t.localScale = new Vector3(box.size.x, box.size.z, 1f);
        var outline = new List<(float x, float y)> { (-0.5f, -0.5f), (0.5f, -0.5f), (0.5f, 0.5f), (-0.5f, 0.5f) };
        DepthMesh shape = PaperDepth.Shadow(outline, box.size.x * scale, box.size.z * scale, 0f, config.shadowSpread,
                                            config.shadowOffset * PaperDepth.LeanRight, -config.shadowOffset * PaperDepth.LeanNear, config.shadowAlpha);
        _shadowMesh ??= new Mesh { name = "RackShadow" };
        _shadowMesh.Clear();
        _shadowMesh.SetVertices(shape.Vertices.ConvertAll(v => new Vector3(v.x, v.y, v.z)));
        _shadowMesh.SetColors(shape.Colours.ConvertAll(c => new Color(c.R, c.G, c.B, c.A)));
        _shadowMesh.SetTriangles(shape.Triangles, 0);
        _shadowMesh.RecalculateBounds();
        contactShadow.sharedMesh = _shadowMesh;
        contactShadow.gameObject.SetActive(true);
    }

    /// <summary>
    /// A new day at the desk (InvestigationUIController with the day's
    /// registry): today's date from the agency's calendar on shift day
    /// <paramref name="day"/> and the clerk's id (BY: TMW-773) for the
    /// prints; the pads fresh; the wheels back on yesterday's date until the
    /// day's first pick-up rolls them; each top window shows today's print.
    /// </summary>
    public void SetDay(AgencyContent agency, int day)
    {
        _day = day;
        _dated = agency != null && AgencyCalendar.TryToday(agency.firstDate, day, out _today);
        _dateText = _dated ? AgencyCalendar.Write(_today).ToUpperInvariant() : string.Empty;
        string clerk = agency != null && agency.clerk != null ? agency.clerk.citizenId : string.Empty;
        _byLine = UiText.Format("stamp.dater.by", clerk);
        foreach (Handle handle in _handles)
        {
            handle.Prints = 0;
            handle.Rolled = false;
            handle.Clicks.Clear();
            if (_dated)
            {
                WheelSetting shown = DaterWheels.Notches(DaterWheels.Shown(_today));
                SetWheel(handle, 0, shown.Day, DaterWheels.DayNotches);
                SetWheel(handle, 1, shown.Month, DaterWheels.MonthNotches);
                SetWheel(handle, 2, shown.Year, DaterWheels.YearNotches);
                PaintBands(handle, _today.Year);
            }
            ShowWindow(handle);
        }
    }

    /// <summary>A fresh print of the APPROVED (<paramref name="approved"/>) or the DENIED dater at the pad's full density: the verdict's ink on papers leaving unstamped (DeskDocument.ShowVerdict; the paper owns it).</summary>
    public Texture2D Impression(bool approved) =>
        DaterImpressionArt.Paint(Word(approved), Ink(approved), _dateText, _byLine, daterFont, 1f, _day * 977 + _presses++);

    /// <summary>
    /// The grey tab, TAB (Papers, Please's TAB; the rack is fixed, Saleh's
    /// D4): takes the player to the stamps, the reading view, which frames
    /// the rack, and the daters lift in their cradles to answer; from the
    /// reading view, back to the office view. Nothing while the daters take
    /// no input.
    /// </summary>
    public void ToggleRack()
    {
        Deselect();
        if (!_live || deskView == null)
            return;
        CancelCarry();
        if (deskView.IsOn)
            deskView.Return();
        else
        {
            deskView.TiltIn();
            Present();
        }
    }

    /// <summary>The daters' answer to the tab: each usable one lifts in its cradle and settles (a kick of its hover lift; nothing under Reduced Motion).</summary>
    private void Present()
    {
        MotionKnobs knobs = UiMotion.Knobs;
        MotionAmount amount = UiMotion.Amount;
        if (amount.Still)
            return;
        foreach (Handle handle in _handles)
            if (Usable(handle))
                handle.Lift.Kick(knobs.Get(MotionFeel.Balanced).KickFor(amount.Share));
    }

    /// <summary>Drops a carried dater back into the rack without a press (a right-click or Esc mid-drag: ControlRules.BackOut's CancelDrag); false when none is carried.</summary>
    public bool CancelCarry()
    {
        if (_carried == null)
            return false;
        _carried.Drag.Cancel();
        return true;
    }

    /// <summary>Lets the daters take input (<paramref name="live"/>: BoothRules.StampsLive; false drops a carried dater back) and shows the grey tab (<paramref name="shown"/>: BoothRules.PropsLive; in the office view only).</summary>
    public void SetLive(bool live, bool shown)
    {
        _live = live;
        _tabShown = shown;
        if (!live)
            CancelCarry();
        ShowTab();
        Show();
    }

    /// <summary>The grey tab: shown while BoothRules.PropsLive in the office view (the reading view shows the rack itself), clickable while the daters are live.</summary>
    private void ShowTab()
    {
        if (tab == null)
            return;
        bool shown = _tabShown && (deskView == null || !deskView.IsOn);
        if (tab.gameObject.activeSelf != shown)
            tab.gameObject.SetActive(shown);
        tab.interactable = _live;
    }

    /// <summary>The paper that takes the verdict (-1: none, nobody here).</summary>
    public int Passport => _passport;

    /// <summary>A new traveller (DeskController): <paramref name="passport"/> is the paper that takes the verdict (the first paper handed over; -1: none, nobody here). No verdict yet (a print still owed lands first, on the paper it was pressed on); the daters stand up for it (or fold down with none).</summary>
    public void BeginCase(int passport)
    {
        SettleInk();
        _passport = passport;
        _noteKey = null;
        _flow.BeginCase(passport >= 0);
        Raise();
    }

    /// <summary>The decision (DeskController): no passport, no verdict, nobody here.</summary>
    public void EndCase() => BeginCase(-1);

    /// <summary>
    /// The papers handed back with the passport's verdict (a paper dropped on
    /// the counter once the passport is stamped: DeskController): that is the
    /// decision (Saleh 2026-10-07: "no lever, no extra commit step", Papers,
    /// Please's way). A print still owed lands on the passport first, so it
    /// leaves with the verdict's ink. The view lifts to the hall and the
    /// verdict is committed (Decided): an APPROVED passport spins the
    /// traveller's portal up (portal_through and a hit; the hall's ring
    /// flares as they leave through it), a DENIED one sends them back the way
    /// they came. False (nothing done) without a verdict or once handed back.
    /// </summary>
    public bool HandBack()
    {
        if (!_flow.HandBack())
            return false;
        CancelCarry();
        SettleInk();
        Sounds.Play(SoundCues.PaperSlide);
        if (deskView != null)
            deskView.Return();
        DeskStamp verdict = _flow.Verdict;
        if (verdict == DeskStamp.Approved)
        {
            CueSounds.Play(SoundCues.PortalThrough, sound);
            FeelDirector.Hit(UiMotion.Knobs.approveHit);
        }
        Commit(verdict);
        return true;
    }

    /// <summary>Commits <paramref name="verdict"/> (StampFlow.Commit: the hand-back an APPROVED or a DENIED passport, the DETAIN button any traveller at any time, stamp or not): Decided; false (nothing done) otherwise.</summary>
    public bool Commit(DeskStamp verdict)
    {
        if (!_flow.Commit(verdict))
            return false;
        CancelCarry();
        Decided?.Invoke(verdict);
        return true;
    }

    /// <summary>Shows the note <paramref name="key"/> (a UI string) on the hint's plate for DeskConfigSO.stampNoteSeconds (DeskController: a paper bounced off the counter, "Stamp the passport first").</summary>
    public void Note(string key)
    {
        _noteKey = key;
        _noteUntil = UiMotion.Now + (config != null ? config.stampNoteSeconds : 2.5f);
        Show();
    }

    /// <summary>
    /// Presses <paramref name="handle"/>, let go after a drag, on what the
    /// player sees under it (ThingUnder: the top-most document or the
    /// rulebook along the pointer's ray, a folder's edge passed over for the
    /// paper under it): StampFlow decides; an accepted press owes its print
    /// and strokes the dater onto the passport (the print lands as it
    /// bottoms out); a refused one thunks, bounces the dater off the paper
    /// and says why; over the bare desk the die thumps the wood.
    /// </summary>
    private void Press(Handle handle)
    {
        Deselect();
        if (!_live || handle.Click == null || handle.Stroke != Stroke.None)
        {
            Return(handle);
            return;
        }
        Roll(handle);
        Component under = ThingUnder(handle.Die, out Vector3 point);
        var paper = under as DeskDocument;
        LastPress = _flow.Press(handle.Kind, under != null, paper != null && paper.Index == _passport);
        if (LastPress == StampPress.Nothing)
        {
            LastPressPoint = handle.Die != null ? handle.Die.position - Up * Height(handle.Die.position) : point;
            BeginStroke(handle, LastPressPoint, true);
        }
        else if (LastPress == StampPress.Stamped)
        {
            LastPressPoint = point;
            handle.PrintOn = paper;
            handle.PrintAt = paper.PagePoint(point);
            handle.Ink.Owe();
            BeginStroke(handle, point, false);
        }
        else
        {
            LastPressPoint = point;
            Note(LastPress == StampPress.NotPassport ? "stamp.refused.notPassport" : "stamp.refused.already");
            if (!Sounds.Play(SoundCues.UiError))
                Play(_thunk, 1f);
            MotionKnobs knobs = UiMotion.Knobs;
            MotionAmount amount = UiMotion.Amount;
            if (!amount.Still)
                handle.Sway.Kick(knobs.Get(knobs.refuseFeel).KickFor(Bounce * amount.Share));
            Return(handle);
        }
        Raise();
    }

    /// <summary>
    /// A stroke begins where the dater is, onto <paramref name="surface"/>
    /// (a world point on what it meets; <paramref name="miss"/>: the bare
    /// desk): the press clack, a short rise (stampLift over
    /// stampRiseSeconds), then the drop exactly onto that surface and the
    /// sink on the Dater spring (a cut under Reduced Motion). The dater takes
    /// no input until it is done.
    /// </summary>
    private void BeginStroke(Handle handle, Vector3 surface, bool miss)
    {
        MotionKnobs knobs = UiMotion.Knobs;
        handle.Returning = false;
        handle.Miss = miss;
        handle.PressedAt = handle.Click.transform.localPosition;
        handle.Drop = StampTarget.StrokeDrop(handle.Die != null ? Height(handle.Die.position) : 0f, Height(surface));
        handle.Contact = false;
        handle.Depth.Snap(0f);
        handle.RiseFrom = UiMotion.Now;
        handle.Stroke = Stroke.Rise;
        if (UiMotion.Amount.Still || knobs.stampRiseSeconds <= 0f)
            Slam(handle);
        _presses++;
        Play(handle.Kind == DeskStamp.Approved ? approvedPress : deniedPress, Pitch());
        Show();
    }

    /// <summary>The rise is over: the dater slams from the top of it down onto the surface and its body sinks (the Dater spring; a cut under Reduced Motion).</summary>
    private void Slam(Handle handle)
    {
        MotionKnobs knobs = UiMotion.Knobs;
        handle.Stroke = Stroke.Down;
        bool still = UiMotion.Amount.Still;
        handle.Depth.Snap(still ? 0f : -knobs.stampLift);
        handle.Depth.Target = handle.Drop + knobs.daterCompress;
        if (still)
            handle.Depth.Snap(handle.Depth.Target);
    }

    /// <summary>
    /// The stroke bottomed out: on the passport the owed impression lands
    /// (its ink the pad's, DaterInk), the body squashes and the hit comes; on
    /// the bare desk the wood's thump (stamp_miss) and no print.
    /// </summary>
    private void Bottom(Handle handle)
    {
        handle.Contact = true;
        handle.ContactAt = UiMotion.Now;
        if (handle.Miss)
        {
            if (!Sounds.Play(SoundCues.StampMiss))
                Play(_thunk, MissPitch);
            return;
        }
        PrintOwed(handle);
        MotionKnobs knobs = UiMotion.Knobs;
        MotionAmount amount = UiMotion.Amount;
        if (!amount.Still)
            handle.Squash.Kick(knobs.Get(knobs.stampFeel).KickFor(amount.Share));
        FeelDirector.Punch(FeelHit.Stamp);
    }

    /// <summary>The print <paramref name="handle"/>'s stroke owes lands now, once (StampInk), on the paper it was pressed on.</summary>
    private void PrintOwed(Handle handle)
    {
        DeskDocument paper = handle.PrintOn;
        handle.PrintOn = null;
        if (!handle.Ink.Land() || paper == null)
            return;
        bool approved = handle.Kind == DeskStamp.Approved;
        float density = config != null ? DaterInk.Print(handle.Prints, _day * 31 + (int)handle.Kind, config.daterInkFade, config.daterInkFloor, config.daterInkSpread) : 1f;
        int seed = _day * 977 + _presses * 13 + (int)handle.Kind;
        float twist = (DaterInk.Hash01(seed, 5, 5) * 2f - 1f) * PrintTwist;
        paper.Stamp(DaterImpressionArt.Paint(Word(approved), Ink(approved), _dateText, _byLine, daterFont, density, seed), handle.PrintAt, twist);
        handle.Prints++;
    }

    /// <summary>Every print still owed lands now (the papers are handed back, or the case changes): the ink always matches the recorded verdict.</summary>
    private void SettleInk()
    {
        foreach (Handle handle in _handles)
            PrintOwed(handle);
    }

    /// <summary>The stroke comes back up: the release clack, the body springing past its rest and the handle's follow-through wobble (a cut under Reduced Motion).</summary>
    private void Release(Handle handle)
    {
        if (handle.Stroke != Stroke.Down)
            return;
        handle.Stroke = Stroke.Up;
        handle.Depth.Target = 0f;
        MotionAmount amount = UiMotion.Amount;
        if (amount.Still)
            handle.Depth.Snap(0f);
        else if (!handle.Miss)
            handle.Wobble.Kick(UiMotion.Knobs.Get(UiMotion.Knobs.daterReleaseFeel).KickFor(WobbleDegrees * amount.Share));
        Play(handle.Kind == DeskStamp.Approved ? approvedRelease : deniedRelease, Pitch());
    }

    /// <summary>A dater's drag begins: it is carried and is the desk's hold (it stands straight, its lift and tip let go); the first touch of the day rolls its wheels.</summary>
    private void PickUp(Handle handle)
    {
        Deselect();
        Roll(handle);
        EndStroke(handle);
        handle.Returning = false;
        handle.Lift.Snap(0f);
        handle.Tip.Snap(0f);
        handle.Seat.Snap(0f);
        handle.Wobble.Snap(0f);
        handle.Click.transform.localRotation = handle.HomeTurn;
        _carried = handle;
        _aimed = false;
        HoldRegistry.Desk.Begin(handle);
        Sounds.Play(SoundCues.StampLift);
        Show();
    }

    /// <summary>A carried dater let go: it strokes there (down and straight up), then goes back to the rack.</summary>
    private void LetGo(Handle handle)
    {
        if (_carried == handle)
            _carried = null;
        HoldRegistry.Desk.End(handle);
        Press(handle);
        Show();
    }

    /// <summary>A carried dater's drag cut short (a right-click or Esc, the hold cancelled, the daters' input taken away): it goes back to the rack without a press.</summary>
    private void PutBack(Handle handle)
    {
        if (_carried == handle)
            _carried = null;
        HoldRegistry.Desk.End(handle);
        Return(handle);
        Show();
    }

    /// <summary>The side button: the pad re-inked (full again) with a squish.</summary>
    private void Reink(Handle handle)
    {
        if (!_live)
            return;
        handle.Prints = 0;
        CueSounds.Play(SoundCues.DaterReink, sound);
    }

    /// <summary>The day's first touch of a dater: its wheels roll from yesterday to today, one queued click per notch (day, then month, then year).</summary>
    private void Roll(Handle handle)
    {
        if (handle.Rolled || !_dated)
            return;
        handle.Rolled = true;
        WheelSetting steps = DaterWheels.Steps(DaterWheels.Shown(_today), _today);
        for (int i = 0; i < steps.Day; i++)
            handle.Clicks.Enqueue(0);
        for (int i = 0; i < steps.Month; i++)
            handle.Clicks.Enqueue(1);
        for (int i = 0; i < steps.Year; i++)
            handle.Clicks.Enqueue(2);
        handle.NextClick = UiMotion.Now;
    }

    /// <summary>Turns wheel <paramref name="wheel"/> of <paramref name="handle"/> at once to notch <paramref name="notch"/> of <paramref name="notches"/>.</summary>
    private void SetWheel(Handle handle, int wheel, int notch, int notches)
    {
        if (wheel >= handle.WheelAngle.Length)
            return;
        handle.WheelAngle[wheel].Snap(wheelFacing + notch * 360f / notches);
        ApplyWheel(handle, wheel);
    }

    private static void ApplyWheel(Handle handle, int wheel)
    {
        Transform t = handle.Wheels[wheel];
        if (t != null)
            t.localRotation = handle.WheelBase[wheel] * Quaternion.Euler(0f, handle.WheelAngle[wheel].Value, 0f);
    }

    /// <summary>The wheels' rubber bands: each notch's label (DaterWheels.Labels) in the date's face, light on dark rubber, for the decade of <paramref name="year"/>.</summary>
    private void PaintBands(Handle handle, int year)
    {
        for (int w = 0; w < handle.Wheels.Length; w++)
        {
            Transform band = handle.Wheels[w] != null ? handle.Wheels[w].Find("Band") : null;
            Renderer r = band != null ? band.GetComponent<Renderer>() : null;
            if (r == null)
                continue;
            if (handle.Bands[w] != null)
                Destroy(handle.Bands[w]);
            handle.Bands[w] = DaterImpressionArt.Band(DaterWheels.Labels(w, year), daterFont, new Color32(36, 34, 36, 255), new Color32(214, 208, 196, 255));
            var block = new MaterialPropertyBlock();
            r.GetPropertyBlock(block);
            block.SetTexture("_BaseMap", handle.Bands[w]);
            r.SetPropertyBlock(block);
        }
    }

    /// <summary>The top window's print: today's, clean (it shows the die).</summary>
    private void ShowWindow(Handle handle)
    {
        if (handle.Window == null)
            return;
        if (handle.WindowPrint != null)
            Destroy(handle.WindowPrint);
        handle.WindowPrint = DaterImpressionArt.Paint(Word(handle.Kind == DeskStamp.Approved), Ink(handle.Kind == DeskStamp.Approved), _dateText, _byLine, daterFont, 1f, 0, false);
        var block = new MaterialPropertyBlock();
        handle.Window.GetPropertyBlock(block);
        block.SetTexture("_BaseMap", handle.WindowPrint);
        handle.Window.SetPropertyBlock(block);
    }

    /// <summary>The verdict word the daters print (the papers' style's).</summary>
    private string Word(bool approved) => UiText.DocumentWord(style == null ? (approved ? "APPROVED" : "DENIED") : approved ? style.approvedStamp : style.deniedStamp);

    /// <summary>The verdict word's ink: APPROVED green, DENIED red (Saleh 2026-10-07: easy to tell apart).</summary>
    private static Color32 Ink(bool approved) => approved ? DaterImpressionArt.ApprovedInk : DaterImpressionArt.DeniedInk;

    /// <summary>This press's pitch: 1 ± PitchSpread, a value of the press's number (no draw).</summary>
    private float Pitch() => 1f + (DaterInk.Hash01(_day, _presses, 9) * 2f - 1f) * PitchSpread;

    /// <summary>The desk's up (its plane's normal; the world's without a desk).</summary>
    private Vector3 Up => surface != null ? surface.transform.up : Vector3.up;

    /// <summary>How high <paramref name="world"/> is over the desk's plane (metres).</summary>
    private float Height(Vector3 world) => surface != null ? Vector3.Dot(world - surface.transform.position, surface.transform.up) : world.y;

    /// <summary>
    /// What the stamp lands on and where (BUG-40a): along the pointer's ray
    /// the carried dater last aimed by (the player's view of the spot its die
    /// hangs over; straight down from the die when there is none, or when the
    /// carry was clamped away from that spot), every document and the
    /// rulebook met, and StampTarget.Pick's choice among them: the top-most,
    /// a folder's edge passed over for the paper under it. Null over the bare
    /// desk.
    /// </summary>
    private Component ThingUnder(Transform die, out Vector3 point)
    {
        point = default;
        if (die == null)
            return null;
        Physics.SyncTransforms(); // a document or a dater moved this frame is where the ray looks for it
        var down = new Ray(die.position + Up * 0.001f, -Up);
        Component thing = _aimed ? Pick(_aimRay, AimLength, out point) : null;
        if (thing != null && Vector3.ProjectOnPlane(point - die.position, Up).magnitude <= AimSlack)
            return thing;
        return Pick(down, RayLength, out point);
    }

    /// <summary>The documents and the rulebook along <paramref name="ray"/> within <paramref name="length"/> (each by its nearest hit), and StampTarget.Pick's choice among them with where the ray met it; null when it meets neither.</summary>
    private Component Pick(Ray ray, float length, out Vector3 point)
    {
        point = default;
        _met.Clear();
        _candidates.Clear();
        int n = Physics.RaycastNonAlloc(ray, _hits, length, paperLayers, QueryTriggerInteraction.Collide);
        for (int i = 0; i < n; i++)
        {
            Component thing = _hits[i].collider.GetComponentInParent<DeskDocument>();
            if (thing == null)
                thing = _hits[i].collider.GetComponentInParent<DeskRulebook>();
            if (thing == null)
                continue;
            int at = -1;
            for (int m = 0; m < _met.Count && at < 0; m++)
                if (_met[m].thing == thing)
                    at = m;
            float distance = _hits[i].distance;
            if (at >= 0 && _candidates[at].Distance <= distance)
                continue;
            bool folder = thing is DeskRulebook;
            var candidate = new StampCandidate(distance, folder ? StampTargetKind.Folder : StampTargetKind.Document, folder ? EdgeDistance(thing, _hits[i].point) : float.MaxValue);
            if (at >= 0)
            {
                _met[at] = (thing, _hits[i].point);
                _candidates[at] = candidate;
            }
            else
            {
                _met.Add((thing, _hits[i].point));
                _candidates.Add(candidate);
            }
        }
        int pick = StampTarget.Pick(_candidates, folderEdge);
        if (pick < 0)
            return null;
        point = _met[pick].point;
        return _met[pick].thing;
    }

    /// <summary>How far inside <paramref name="thing"/>'s footprint on the desk (the box colliders under it, along its own right and the desk's matching forward) <paramref name="world"/> lies: metres to its nearest edge.</summary>
    private float EdgeDistance(Component thing, Vector3 world)
    {
        Vector3 up = Up;
        Vector3 across = Vector3.ProjectOnPlane(thing.transform.right, up);
        if (across.sqrMagnitude < 1e-8f)
            across = Vector3.ProjectOnPlane(thing.transform.forward, up);
        across.Normalize();
        Vector3 along = Vector3.Cross(across, up);
        float minA = float.MaxValue, maxA = float.MinValue, minB = float.MaxValue, maxB = float.MinValue;
        thing.GetComponentsInChildren(false, _boxes);
        foreach (BoxCollider box in _boxes)
        {
            if (!box.enabled)
                continue;
            for (int i = 0; i < 8; i++)
            {
                Vector3 h = box.size * 0.5f;
                Vector3 corner = box.transform.TransformPoint(box.center + new Vector3((i & 1) == 0 ? -h.x : h.x, (i & 2) == 0 ? -h.y : h.y, (i & 4) == 0 ? -h.z : h.z));
                float a = Vector3.Dot(corner, across), b = Vector3.Dot(corner, along);
                minA = Mathf.Min(minA, a);
                maxA = Mathf.Max(maxA, a);
                minB = Mathf.Min(minB, b);
                maxB = Mathf.Max(maxB, b);
            }
        }
        if (minA > maxA)
            return float.MaxValue;
        float pa = Vector3.Dot(world, across), pb = Vector3.Dot(world, along);
        return Mathf.Max(0f, Mathf.Min(Mathf.Min(pa - minA, maxA - pa), Mathf.Min(pb - minB, maxB - pb)));
    }

    /// <summary>A carried dater's aim (its drag's): where the pointer's <paramref name="ray"/> meets the top-most document or the rulebook (StampTarget.Pick), so the die goes straight above the spot the pointer shows on the paper; null over the bare desk (the drag uses the desk plane). The ray is kept: the press looks along it.</summary>
    private Vector3? Aim(Ray ray)
    {
        _aimRay = ray;
        _aimed = true;
        return Pick(ray, AimLength, out Vector3 point) != null ? point : (Vector3?)null;
    }

    /// <summary>Moves the cradles, each dater (the stroke, the bounce, the way back, its answer to the hand, the wheels) and lets a note go once its time is up.</summary>
    private void Update()
    {
        float dt = UiMotion.Delta(Time.unscaledDeltaTime);
        MoveCradles(dt);
        Vector3? hand = Hand(out Ray pointer);
        foreach (Handle handle in _handles)
            Move(handle, dt, hand, pointer);
        if (_noteKey != null && UiMotion.Now >= _noteUntil)
        {
            _noteKey = null;
            Show();
        }
    }

    /// <summary>Where the hand is on the desk (the pointer on the desk's plane, through the main camera) and the pointer's ray; null without a mouse, a camera or a desk.</summary>
    private Vector3? Hand(out Ray pointer)
    {
        pointer = default;
        Mouse mouse = Mouse.current;
        Camera cam = Camera.main;
        if (mouse == null || cam == null || surface == null || !_live)
            return null;
        Vector2 at = mouse.position.ReadValue();
        pointer = cam.ScreenPointToRay(at);
        return surface.TryProject(cam, at, out Vector3 point) ? point : (Vector3?)null;
    }

    /// <summary>One dater's motion this frame, every part on its spring (MotionKnobs; cuts under Reduced Motion).</summary>
    private void Move(Handle handle, float dt, Vector3? hand, Ray pointer)
    {
        if (handle.Click == null)
            return;
        MotionKnobs knobs = UiMotion.Knobs;
        bool still = UiMotion.Amount.Still;
        Transform root = handle.Click.transform;
        float perMetre = PerMetre(root);
        bool carried = handle == _carried;

        if (handle.Stroke == Stroke.Rise)
        {
            float u = Mathf.Clamp01((UiMotion.Now - handle.RiseFrom) / Mathf.Max(1e-3f, knobs.stampRiseSeconds));
            float lifted = 1f - (1f - u) * (1f - u) * (1f - u); // out-cubic: quick off the hand, easing at the top
            root.localPosition = handle.PressedAt + Vector3.up * (knobs.stampLift * lifted * perMetre);
            if (u >= 1f)
                Slam(handle);
        }
        if (handle.Stroke == Stroke.Down || handle.Stroke == Stroke.Up)
        {
            SpringTuning tuning = knobs.Get(handle.Stroke == Stroke.Down ? knobs.daterFeel : knobs.daterReleaseFeel);
            // The way up settles loosely (a millimetre): the dater heads back to the rack while its last wobble dies out.
            bool moving = handle.Stroke == Stroke.Down ? handle.Depth.Step(dt, tuning, 1e-5f, 1e-3f) : handle.Depth.Step(dt, tuning, 1e-3f, 0.05f);
            float depth = handle.Depth.Value, drop = handle.Drop;
            // The stroke is in metres; the dater's parent (the drawer, DeskConfigSO.stampDrawerScale) may be scaled.
            root.localPosition = handle.PressedAt + Vector3.down * (Mathf.Min(depth, drop) * perMetre);
            if (handle.Body != null)
                handle.Body.localPosition = handle.BodyHome + Vector3.down * (Mathf.Max(0f, depth - drop) * perMetre);
            if (handle.Stroke == Stroke.Down)
            {
                if (!handle.Contact && depth >= drop + knobs.daterCompress * 0.8f)
                    Bottom(handle);
                if (handle.Contact && UiMotion.Now - handle.ContactAt >= knobs.daterQuickHold)
                    Release(handle);
            }
            else if (!moving)
            {
                EndStroke(handle);
                Return(handle);
            }
        }

        if (handle.Returning)
        {
            bool moving = handle.Back.Step(dt, knobs.Get(knobs.stampFeel), 1e-4f, 1e-3f);
            if (still)
            {
                handle.Back.Snap(1f);
                moving = false;
            }
            root.localPosition = Vector3.LerpUnclamped(handle.ReturnFrom, handle.Home, handle.Back.Value);
            if (!moving)
            {
                root.localPosition = handle.Home;
                handle.Returning = false;
                Seated(handle);
            }
        }

        bool home = !carried && handle.Stroke == Stroke.None && !handle.Returning;
        if (home)
            Answer(handle, dt, hand, pointer, perMetre);

        // The body's squash on impact, about its foot.
        if (handle.Body != null && (!handle.Squash.AtRest || handle.Body.localScale != Vector3.one))
        {
            handle.Squash.Step(dt, knobs.Get(knobs.stampFeel), 1e-4f, 1e-2f);
            float s = Mathf.Clamp(handle.Squash.Value, -1f, 1f) * knobs.stampSquash;
            handle.Body.localScale = new Vector3(1f + s / 2f, 1f - s, 1f + s / 2f);
            if (handle.Stroke == Stroke.None)
                handle.Body.localPosition = handle.BodyHome;
            handle.Body.localPosition += Vector3.up * (handle.BodyFoot * s);
        }

        // The handle's follow-through as the stroke springs up.
        if (!handle.Wobble.AtRest)
        {
            handle.Wobble.Step(dt, knobs.Get(knobs.daterReleaseFeel), 0.01f, 0.5f);
            if (!home)
                root.localRotation = handle.HomeTurn * Quaternion.AngleAxis(handle.Wobble.Value, Vector3.right);
        }

        // A refusal's bounce off the paper rides on top of wherever the dater is (up and back down).
        if (!handle.Sway.AtRest || handle.SwayShown != 0f)
        {
            handle.Sway.Step(dt, knobs.Get(knobs.refuseFeel), 1e-5f, 1e-3f);
            float sway = handle.Sway.Value;
            bool placed = !carried; // every other state sets the dater's place outright this frame
            root.localPosition += Vector3.up * ((placed ? sway : sway - handle.SwayShown) * perMetre);
            handle.SwayShown = sway;
        }

        if (handle.Clicks.Count > 0 && UiMotion.Now >= handle.NextClick)
        {
            int wheel = handle.Clicks.Dequeue();
            int notches = wheel == 0 ? DaterWheels.DayNotches : wheel == 1 ? DaterWheels.MonthNotches : DaterWheels.YearNotches;
            if (wheel < handle.WheelAngle.Length)
                handle.WheelAngle[wheel].Target += 360f / notches;
            CueSounds.Play(SoundCues.DaterWheelClick, sound, Pitch());
            handle.NextClick = UiMotion.Now + knobs.daterWheelClick;
        }
        for (int w = 0; w < handle.WheelAngle.Length; w++)
            if (!handle.WheelAngle[w].AtRest)
            {
                if (still)
                    handle.WheelAngle[w].Snap(handle.WheelAngle[w].Target);
                else
                    handle.WheelAngle[w].Step(dt, knobs.Get(knobs.wheelFeel), 0.01f, 0.1f);
                ApplyWheel(handle, w);
            }
    }

    /// <summary>
    /// A dater at home answers the hand (the feel bible's B3): the pointer
    /// over it lifts it HoverLift in its cradle (Balanced), the hand within
    /// TipReach of it tips it toward the hand, up to TipDegrees as it nears,
    /// and a seating dip settles (Cradle). Only while it takes input; still
    /// under Reduced Motion.
    /// </summary>
    private void Answer(Handle handle, float dt, Vector3? hand, Ray pointer, float perMetre)
    {
        MotionKnobs knobs = UiMotion.Knobs;
        Transform root = handle.Click.transform;
        bool usable = Usable(handle) && !UiMotion.Amount.Still;
        bool over = usable && hand.HasValue && handle.Box != null && handle.Box.enabled && handle.Box.Raycast(pointer, out _, AimLength);
        handle.Lift.Target = over ? 1f : 0f;
        float near = float.MaxValue;
        if (usable && hand.HasValue)
        {
            Vector3 flat = Vector3.ProjectOnPlane(hand.Value - root.position, Up);
            near = flat.magnitude;
            if (root.parent != null && near > 1e-4f)
            {
                Vector3 local = root.parent.InverseTransformDirection(flat);
                local.y = 0f;
                if (local.sqrMagnitude > 1e-10f)
                    handle.Toward = local.normalized;
            }
        }
        handle.Tip.Target = near < TipReach ? TipDegrees * (1f - near / TipReach) : 0f;
        if (!handle.Lift.AtRest)
            handle.Lift.Step(dt, knobs.Get(MotionFeel.Balanced), 1e-4f, 1e-3f);
        if (!handle.Tip.AtRest)
            handle.Tip.Step(dt, knobs.Get(MotionFeel.Balanced), 0.01f, 0.1f);
        if (!handle.Seat.AtRest)
            handle.Seat.Step(dt, knobs.Get(knobs.cradleFeel), 1e-5f, 1e-3f);
        root.localPosition = handle.Home + Vector3.up * ((handle.Lift.Value * HoverLift + handle.Seat.Value) * perMetre);
        Quaternion tip = Quaternion.FromToRotation(Vector3.up, (Vector3.up + handle.Toward * Mathf.Tan(handle.Tip.Value * Mathf.Deg2Rad)).normalized);
        root.localRotation = tip * handle.HomeTurn * Quaternion.AngleAxis(handle.Wobble.Value, Vector3.right);
    }

    /// <summary>A dater back in its cradle: it seats with a dip (the cradle's lock bounce) and the seat's click (the date wheels' ratchet click, pitched down for the heavier catch, until the rack's own sound comes).</summary>
    private void Seated(Handle handle)
    {
        MotionAmount amount = UiMotion.Amount;
        if (!amount.Still)
            handle.Seat.Kick(-UiMotion.Knobs.Get(UiMotion.Knobs.cradleFeel).KickFor(SeatDip * amount.Share));
        CueSounds.Play(SoundCues.DaterWheelClick, sound, LockPitch);
    }

    /// <summary>How many of <paramref name="root"/>'s local units make a metre along the desk's up (its parent, the drawer, is scaled by DeskConfigSO.stampDrawerScale).</summary>
    private static float PerMetre(Transform root) => root.parent != null && root.parent.lossyScale.y > 1e-4f ? 1f / root.parent.lossyScale.y : 1f;

    /// <summary>Ends a stroke where it started: a print still owed lands now (StampInk), the body home, the depth at rest.</summary>
    private void EndStroke(Handle handle)
    {
        if (handle.Stroke == Stroke.None)
            return;
        PrintOwed(handle);
        handle.Stroke = Stroke.None;
        handle.Depth.Snap(0f);
        if (handle.Body != null)
            handle.Body.localPosition = handle.BodyHome;
        handle.Click.transform.localPosition = handle.PressedAt;
        Show();
    }

    /// <summary>Sends a dater away from the rack back to its place on a firm spring (nothing when it is there, carried or stroking).</summary>
    private void Return(Handle handle)
    {
        if (handle.Click == null || handle == _carried || handle.Stroke != Stroke.None)
            return;
        Vector3 at = handle.Click.transform.localPosition;
        if ((at - handle.Home).sqrMagnitude < 1e-10f)
            return;
        handle.ReturnFrom = at;
        handle.Back = Spring.At(0f);
        handle.Back.Target = 1f;
        handle.Returning = true;
    }

    /// <summary>
    /// The cradles this frame (DrawerSequence on MotionKnobs' cradle tunings;
    /// a snap under Reduced Motion): up while a case is open (the passport
    /// known), DENIED then APPROVED, a click as each locks (a lock lets its
    /// dater take input) or lands; down when it ends, a cradle waiting while
    /// its dater is away from its place.
    /// </summary>
    private void MoveCradles(float dt)
    {
        bool open = _passport >= 0;
        if (drawer == null || (_drawer.Closed && !open))
            return;
        bool away = false;
        foreach (Handle h in _handles)
            away |= h == _carried || h.Stroke != Stroke.None || h.Returning;
        DrawerEvents events = _drawer.Step(dt, open, UiMotion.Knobs, UiMotion.Amount.Still, away);
        drawer.Pose(_drawer.Raise(DrawerSequence.Denied), _drawer.Raise(DrawerSequence.Approved));
        if (events != DrawerEvents.None)
        {
            // Each cradle's lock (and landing) clicks: the date wheels' ratchet click, pitched down for the heavier catch.
            CueSounds.Play(SoundCues.DaterWheelClick, sound, LockPitch);
            Show();
        }
    }

    /// <summary>The pitch of a cradle's lock click (the wheels' ratchet click, lower: a heavier catch).</summary>
    private const float LockPitch = 0.62f;

    /// <summary>The pitch of the refusal's thunk standing in for the bare desk's thump until stamp_miss is delivered (lower: wood).</summary>
    private const float MissPitch = 0.8f;

    private void Play(AudioClip clip, float pitch)
    {
        if (sound == null || clip == null)
            return;
        sound.pitch = pitch;
        sound.PlayOneShot(clip);
    }

    /// <summary>A button clicked leaves no selection behind (a selected button would take SPACE, the inspect key, as a press).</summary>
    private static void Deselect()
    {
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    /// <summary>The change is shown and announced.</summary>
    private void Raise()
    {
        Show();
        Changed?.Invoke();
    }

    /// <summary>True while <paramref name="handle"/> takes input: the daters are live, its cradle is locked upright (the built rack's daters stand all the time) and it is not mid-stroke (a re-grab can never cut a stroke: BUG-40a).</summary>
    private bool Usable(Handle handle) => _live && (drawer == null || _drawer.Locked(handle.Lane)) && handle.Stroke == Stroke.None;

    /// <summary>The daters' input (Usable) and the hint: a note while it lasts; else, a dater carried, to let it go on the passport, or (stamped) to hand the papers back on the counter.</summary>
    private void Show()
    {
        foreach (Handle handle in _handles)
        {
            bool usable = Usable(handle);
            if (handle.Click != null)
                handle.Click.Interactable = usable;
            if (handle.Drag != null && handle.Drag.enabled != usable)
                handle.Drag.enabled = usable;
            if (handle.Reink != null)
                handle.Reink.Interactable = usable;
        }
        if (hint == null)
            return;
        string key = _passport < 0 ? null
            : _noteKey ?? (!_live ? null : _flow.CanHandBack ? "stamp.hint.handBack" : _carried != null ? "stamp.hint.place" : null);
        GameObject plate = hint.transform.parent != null ? hint.transform.parent.gameObject : hint.gameObject;
        if (plate.activeSelf != (key != null))
            plate.SetActive(key != null);
        if (key != null)
            hint.text = UiText.Get(key);
    }
}
