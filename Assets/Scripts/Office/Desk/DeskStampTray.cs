using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// The stamp bar with the two daters (the desk machine spec §1-2; Saleh
/// 2026-10-07: "the stamps need to be retro two-click stamps … one saying
/// approved, one denied"; before it, 2026-10-06: Papers, Please's grey tab,
/// TAB, and "two stamps that I physically move ... I should be able to stamp
/// anywhere on the document"). A grey tab on the right edge of the office
/// overlay ("STAMPS" over "TAB"), the TAB key (OfficeControls) and the desk's
/// stamp prop open the bar over the desk and close it (ToggleBar); out, it
/// brings the reading view. The bar is the heavy brass drawer (Track BR,
/// Saleh 2026-10-08: "a heavy brass drawer that makes the sound a typewriter
/// makes when the carriage returns"): it slides out toward the chair from
/// under the counter's side with a slow start, an accelerating carry and a
/// hard stop (DrawerSequence: the carry, the stiff Drawer spring's overshoot
/// and settle, FeelDirector's small hit, SoundCues.DrawerOpen's ratchet
/// carry), and its brass mechanism (BrassDrawer: a lever, a rack and
/// pinion, a cradle per dater) stands the daters up from flat on their
/// backs, DENIED then APPROVED, each locking upright with a click; closing,
/// they fold down first (a dater still away comes back first), then a shove
/// seats the drawer (SoundCues.DrawerClose: the reverse rasp and the thud).
/// A dater takes input once its cradle is locked. Without the drawer's art
/// the built rack (a lip with end caps) moves the same way without the
/// mechanism. The rack holds the DENIED dater
/// (left) and the APPROVED dater (right), self-inking daters after Saleh's
/// S-401 reference, built under a prop contract (a root with Body, Frame,
/// Die, Wheels and Button: the art may replace the meshes): a glossy body,
/// deep green on APPROVED and red on DENIED (Saleh 2026-10-07: "easy to
/// tell apart"), with a window on top showing the die's print, a white
/// frame, the date wheels seen through it and a lighter side button. A dater is moved, not the paper: left-press and drag it (its
/// DeskDraggable) and its die is carried over the pointer's spot on the
/// papers; letting go strokes it there (down and straight back up: both
/// clacks), then it goes back to its place in the rack; a left-press on a
/// hanging dater held past MotionKnobs.daterHoldDelay strokes it where it
/// hangs and holds it down until the button comes up, a quick click strokes
/// it down and up. A stroke (all motion on springs, MotionKnobs' Dater
/// tunings): the press clack as it starts (the die flipping off the pad:
/// Saleh's "cha-ka", the deep one on DENIED, the pitch ±3 % a press), the
/// dater drops onto the paper and its body sinks over its frame against
/// the stiff Dater spring; as it bottoms out the impression lands
/// (DaterImpressionArt: the outline word, the date in red, BY: the clerk's
/// id; its ink fading with the pad over the shift, DaterInk) and the hit
/// comes (FeelDirector: the hit-stop and the camera's impulse); on release
/// the second clack and the body springs up past its rest. Each morning the
/// date wheels still show yesterday; the first time a dater is picked up
/// that day they roll to today, one ratchet click per notch (DaterWheels).
/// The side button re-inks the pad (a squish). A stroke prints only on the
/// passport, anywhere on it, once (StampFlow: the other dater, another paper
/// or the rulebook are refused with a thunk, a shake and a note); over the
/// bare desk the dater just goes back. Then the stamped passport dropped on
/// the counter hands the papers back (HandBack), and that commits the
/// verdict (Saleh 2026-10-07: no extra step): APPROVED spins the portal up,
/// DENIED sends the traveller back; the desk's DETAIN button commits the
/// third verdict any time (Commit). A right-click or Esc
/// drops a carried dater back into the rack, else slides the bar back
/// (ControlRules.BackOut). The bar takes input while BoothRules.StampsLive
/// and shows its tab while BoothRules.PropsLive. Reduced Motion cuts every
/// stroke and drops the shake. Build Office UI builds the tab, the hint, the
/// audio source, the rack and the daters; the office binder lays the rack.
/// </summary>
public sealed class DeskStampTray : MonoBehaviour
{
    /// <summary>The desk tuning (the daters' hover, the bar's place, travel and slide, the ink, a note's time).</summary>
    [SerializeField] private DeskConfigSO config;

    /// <summary>The reading view (optional): the bar slid out brings it, and the bar hangs where it shows the desk at DeskConfigSO.stampBarView.</summary>
    [SerializeField] private DeskView deskView;

    /// <summary>The desk plane (the bar's height and, without the reading view, its place; the daters are dragged over it).</summary>
    [SerializeField] private DeskSurface surface;

    /// <summary>The rack in the office (the brass drawer or the built lip, the two daters): hidden while all the way in.</summary>
    [SerializeField] private Transform rack;

    /// <summary>The brass drawer's mechanism on the rack (optional: the built rack has none; its daters stand all the time).</summary>
    [SerializeField] private BrassDrawer drawer;

    /// <summary>The drawer's placeholder sounds (tools/audio/make_drawer_sfx.py) until the sound bank has SoundCues.DrawerOpen / DrawerClose: the opening's typewriter carriage-return carry and clunk, the closing's reverse rasp and thud.</summary>
    [SerializeField] private AudioClip drawerOpen, drawerClose;

    /// <summary>The grey tab (with "TAB" printed on it): a click slides the bar out or back.</summary>
    [SerializeField] private Button tab;

    /// <summary>The APPROVED dater's click box: its root (the prop contract's Body, Frame, Die, Wheels and Button are its children), with its DeskDraggable and PointerHold.</summary>
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

    /// <summary>The physics layers a dater's ray meets documents on (the Interactable layer).</summary>
    [SerializeField] private LayerMask paperLayers = ~0;

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

    /// <summary>How far a refused dater shakes sideways (metres).</summary>
    private const float Shake = 0.006f;

    /// <summary>How far above the desk a stroke's foot stops (metres): on the paper lying there.</summary>
    private const float PressFloor = 0.003f;

    /// <summary>The longest ray a dater casts down, and the longest pointer ray a carried dater aims along (metres).</summary>
    private const float RayLength = 0.5f, AimLength = 10f;

    /// <summary>How far a print may turn either way (degrees): a hand's slight twist.</summary>
    private const float PrintTwist = 1.6f;

    /// <summary>How much the press's pitch varies either way (Saleh's sounds: ±3 % a press).</summary>
    private const float PitchSpread = 0.03f;

    /// <summary>Where a stroke is.</summary>
    private enum Stroke
    {
        None,
        Down,
        Up
    }

    /// <summary>One dater of the rack: what it prints, its parts, its place in the rack and its motion.</summary>
    private sealed class Handle
    {
        public DeskStamp Kind;

        /// <summary>Its lane in the drawer (DrawerSequence.Denied or Approved).</summary>
        public int Lane;
        public Clickable Click;
        public Transform Die;
        public DeskDraggable Drag;
        public PointerHold Hold;
        public Clickable Reink;
        public Transform Body;
        public Vector3 BodyHome;
        public Transform[] Wheels = Array.Empty<Transform>();
        public Quaternion[] WheelBase = Array.Empty<Quaternion>();
        public Spring[] WheelAngle = Array.Empty<Spring>();
        public Renderer Window;
        public Texture2D WindowPrint;

        /// <summary>The wheels' rubber bands (their labels painted for the day's decade).</summary>
        public Texture2D[] Bands = new Texture2D[3];

        /// <summary>Its place in the rack (local).</summary>
        public Vector3 Home;

        /// <summary>Where its stroke started (local): it drops from there.</summary>
        public Vector3 PressedAt;

        /// <summary>The stroke's depth below PressedAt (metres: the drop, then the body's sink), and the refusal's sideways shake.</summary>
        public Spring Depth, Sway;

        /// <summary>The shake's offset as last applied.</summary>
        public float SwayShown;

        public Stroke Stroke;
        public bool Contact;
        public bool Holding;
        public float ContactAt;

        /// <summary>The print the stroke owes the passport when it bottoms out (null: none).</summary>
        public DeskDocument PrintOn;
        public Vector2 PrintAt;

        /// <summary>The way back to the rack: 0 to 1 on a spring.</summary>
        public Spring Back;
        public bool Returning;
        public Vector3 ReturnFrom;

        /// <summary>A press of the pointer waiting to become a held stroke (no drag yet), and when it went down.</summary>
        public bool Gripped;
        public float GripAt;

        /// <summary>Prints since its pad was last inked, and true once its wheels rolled today.</summary>
        public int Prints;
        public bool Rolled;

        /// <summary>The wheels' clicks still to come in the morning roll (the wheel of each), and when the next one comes.</summary>
        public readonly Queue<int> Clicks = new Queue<int>();
        public float NextClick;
    }

    private readonly StampFlow _flow = new StampFlow();
    private readonly RaycastHit[] _hits = new RaycastHit[64];
    private Handle[] _handles = Array.Empty<Handle>();
    private Handle _carried;
    private int _passport = -1;
    private bool _live;
    private readonly DrawerSequence _drawer = new DrawerSequence();
    private Vector3 _out, _in;
    private bool _laid;
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

    /// <summary>True while the bar is out.</summary>
    public bool BarOut => _flow.BarOut;

    /// <summary>True while a dater is dragged over the desk (a right-click or Esc drops it back into the rack: CancelCarry).</summary>
    public bool IsCarrying => _carried != null;

    /// <summary>True while a dater strokes, shakes, goes back to the rack or rolls its wheels (the probes wait for it).</summary>
    public bool IsMoving
    {
        get
        {
            foreach (Handle h in _handles)
                if (h.Stroke != Stroke.None || h.Returning || !h.Sway.AtRest || h.Clicks.Count > 0)
                    return true;
            return false;
        }
    }

    /// <summary>What the last press did (the probes read it).</summary>
    public StampPress LastPress { get; private set; }

    /// <summary>Where the last press met a paper or the rulebook (straight under the dater's die; the probes read it).</summary>
    public Vector3 LastPressPoint { get; private set; }

    /// <summary>Raised when the bar, the verdict or the case changes (the counter's label, the booth's rules, the DETAIN button).</summary>
    public event Action Changed;

    /// <summary>Raised when the case's verdict is committed (the hand-back APPROVED or DENIED, the DETAIN button DETAINED).</summary>
    public event Action<DeskStamp> Decided;

    private void Awake()
    {
        if (tab != null)
            tab.onClick.AddListener(ToggleBar);
        _handles = new[] { MakeHandle(DeskStamp.Approved, approvedStamp, approvedDie), MakeHandle(DeskStamp.Denied, deniedStamp, deniedDie) };
        if (rack != null)
            rack.gameObject.SetActive(false);
        _thunk = CueSounds.Tone("StampThunk", 70f, 0.16f, 0.7f);
        Show();
    }

    /// <summary>A dater of the rack: its parts by the prop contract's names, its drag (carried stampHover over the desk, its die over the pointer's spot), its hold (a held press) and its side button (re-ink).</summary>
    private Handle MakeHandle(DeskStamp kind, Clickable click, Transform die)
    {
        var handle = new Handle { Kind = kind, Lane = kind == DeskStamp.Denied ? DrawerSequence.Denied : DrawerSequence.Approved, Click = click, Die = die, Back = Spring.At(1f) };
        if (click == null)
            return handle;
        Transform root = click.transform;
        handle.Home = root.localPosition;
        handle.Body = root.Find("Body");
        if (handle.Body != null)
            handle.BodyHome = handle.Body.localPosition;
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
        handle.Hold = click.GetComponent<PointerHold>();
        if (handle.Hold != null)
        {
            handle.Hold.Down += _ => Grip(handle);
            handle.Hold.Up += _ => Ungrip(handle);
        }
        return handle;
    }

    private void OnDestroy()
    {
        if (_thunk != null)
            Destroy(_thunk);
        foreach (Handle handle in _handles)
        {
            if (handle.WindowPrint != null)
                Destroy(handle.WindowPrint);
            foreach (Texture2D band in handle.Bands)
                if (band != null)
                    Destroy(band);
        }
    }

    /// <summary>
    /// Lays the rack (the office binder, once the desk and the reading view
    /// are placed): out where the reading view shows the desk at
    /// DeskConfigSO.stampBarView (its bottom edge), the daters' feet
    /// stampHover above the desk, facing along the office view's level
    /// <paramref name="levelForward"/>, at DeskConfigSO.stampDrawerScale; in
    /// stampBarTravel nearer the chair, below the view's bottom edge (the
    /// drawer rises from the bottom, Saleh's 1008a playtest).
    /// </summary>
    public void Lay(Vector3 levelForward)
    {
        if (rack == null || surface == null || config == null)
            return;
        Vector3 forward = Vector3.ProjectOnPlane(levelForward, Vector3.up);
        if (forward.sqrMagnitude < 1e-6f)
            forward = Vector3.forward;
        forward.Normalize();
        float desk = surface.transform.position.y;
        if (deskView == null || !deskView.TryViewPoint(config.stampBarView, desk, out Vector3 at))
            at = surface.transform.position;
        _out = new Vector3(at.x, desk + config.stampHover, at.z);
        _in = _out - forward * config.stampBarTravel;
        _laid = true;
        rack.localScale = Vector3.one * config.stampDrawerScale;
        rack.SetPositionAndRotation(Vector3.LerpUnclamped(_in, _out, _drawer.Travel), Quaternion.LookRotation(forward, Vector3.up));
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

    /// <summary>The grey tab, TAB, the desk's stamp: opens the drawer (bringing the reading view) or closes it; nothing while the daters take no input.</summary>
    public void ToggleBar()
    {
        Deselect();
        if (!_live)
            return;
        CancelCarry();
        bool slidOut = _flow.ToggleBar();
        if (slidOut && deskView != null)
            deskView.TiltIn();
        Raise();
    }

    /// <summary>Slides the bar back (a right-click or Esc: ControlRules.BackOut), a carried dater dropped back into the rack first; false when it is in already.</summary>
    public bool Stow()
    {
        CancelCarry();
        if (!_flow.StowBar())
            return false;
        Raise();
        return true;
    }

    /// <summary>Drops a carried dater back into the rack without a press (a right-click or Esc mid-drag: ControlRules.BackOut's CancelDrag); false when none is carried.</summary>
    public bool CancelCarry()
    {
        if (_carried == null)
            return false;
        _carried.Drag.Cancel();
        return true;
    }

    /// <summary>Lets the bar and its daters take input (<paramref name="live"/>: BoothRules.StampsLive; false slides the bar back) and shows the grey tab (<paramref name="shown"/>: BoothRules.PropsLive).</summary>
    public void SetLive(bool live, bool shown)
    {
        _live = live;
        if (tab != null)
        {
            if (tab.gameObject.activeSelf != shown)
                tab.gameObject.SetActive(shown);
            tab.interactable = live;
        }
        if (!live)
            Stow();
        Show();
    }

    /// <summary>A new traveller (DeskController): <paramref name="passport"/> is the paper that takes the verdict (the first paper handed over; -1: none, nobody here). No verdict yet.</summary>
    /// <summary>The paper that takes the verdict (-1: none, nobody here).</summary>
    public int Passport => _passport;

    /// <summary>The point on the desk where the passport's stamp area goes as the drawer opens (DeskConfigSO.stampSpotView in the reading view, above the drawer; DeskController); false without the reading view.</summary>
    public bool TryStampSpot(out Vector3 point)
    {
        point = default;
        return surface != null && config != null && deskView != null && deskView.TryViewPoint(config.stampSpotView, surface.transform.position.y, out point);
    }

    public void BeginCase(int passport)
    {
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
    /// Please's way). The bar slides in, the view lifts to the hall and the
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
        _flow.StowBar();
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
        _flow.StowBar();
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
    /// Presses <paramref name="handle"/> on what lies under its die (a ray
    /// straight down from the die's centre meets a document or the rulebook
    /// first, or nothing): StampFlow decides; an accepted press strokes the
    /// dater down (the print lands as it bottoms out; held while
    /// <paramref name="hold"/>, else straight back up); a refused one
    /// thunks, shakes the dater and says why; over the bare desk the dater
    /// just goes back.
    /// </summary>
    private void Press(Handle handle, bool hold)
    {
        Deselect();
        if (!_live || !_flow.BarOut || handle.Click == null || handle.Stroke != Stroke.None)
        {
            Return(handle);
            return;
        }
        Roll(handle);
        Component under = ThingUnder(handle.Die, out Vector3 point);
        var paper = under as DeskDocument;
        LastPressPoint = point;
        LastPress = _flow.Press(handle.Kind, under != null, paper != null && paper.Index == _passport);
        if (LastPress == StampPress.Nothing)
        {
            Return(handle);
            Raise();
            return;
        }
        if (LastPress == StampPress.Stamped)
        {
            handle.PrintOn = paper;
            handle.PrintAt = paper.PagePoint(point);
            BeginStroke(handle, hold);
        }
        else
        {
            Note(LastPress == StampPress.NotPassport ? "stamp.refused.notPassport" : "stamp.refused.already");
            if (!Sounds.Play(SoundCues.UiError))
                Play(_thunk, 1f);
            MotionKnobs knobs = UiMotion.Knobs;
            MotionAmount amount = UiMotion.Amount;
            if (!amount.Still)
                handle.Sway.Kick(knobs.Get(knobs.refuseFeel).KickFor(Shake * amount.Share));
            Return(handle);
        }
        Raise();
    }

    /// <summary>A stroke begins where the dater is: the press clack, the drop and the sink on the Dater spring (a cut under Reduced Motion).</summary>
    private void BeginStroke(Handle handle, bool hold)
    {
        MotionKnobs knobs = UiMotion.Knobs;
        handle.Returning = false;
        handle.PressedAt = handle.Click.transform.localPosition;
        handle.Stroke = Stroke.Down;
        handle.Contact = false;
        handle.Holding = hold;
        handle.Depth.Target = Drop + knobs.daterCompress;
        if (UiMotion.Amount.Still)
            handle.Depth.Snap(handle.Depth.Target);
        _presses++;
        Play(handle.Kind == DeskStamp.Approved ? approvedPress : deniedPress, Pitch());
    }

    /// <summary>How far a stroke drops before the frame stands on the paper (metres).</summary>
    private float Drop => config != null ? Mathf.Max(0f, config.stampHover - PressFloor) : 0f;

    /// <summary>The stroke bottomed out: the impression lands on the passport (its ink the pad's, DaterInk) and the hit comes.</summary>
    private void Bottom(Handle handle)
    {
        handle.Contact = true;
        handle.ContactAt = UiMotion.Now;
        if (handle.PrintOn != null)
        {
            float density = config != null ? DaterInk.Print(handle.Prints, _day * 31 + (int)handle.Kind, config.daterInkFade, config.daterInkFloor, config.daterInkSpread) : 1f;
            int seed = _day * 977 + _presses * 13 + (int)handle.Kind;
            float twist = (DaterInk.Hash01(seed, 5, 5) * 2f - 1f) * PrintTwist;
            handle.PrintOn.Stamp(DaterImpressionArt.Paint(Word(handle.Kind == DeskStamp.Approved), Ink(handle.Kind == DeskStamp.Approved), _dateText, _byLine, daterFont, density, seed), handle.PrintAt, twist);
            handle.Prints++;
            handle.PrintOn = null;
        }
        FeelDirector.Punch(FeelHit.Stamp);
    }

    /// <summary>The stroke comes back up: the release clack and the body springing past its rest (a cut under Reduced Motion).</summary>
    private void Release(Handle handle)
    {
        if (handle.Stroke != Stroke.Down)
            return;
        handle.Stroke = Stroke.Up;
        handle.Holding = false;
        handle.Depth.Target = 0f;
        if (UiMotion.Amount.Still)
            handle.Depth.Snap(0f);
        Play(handle.Kind == DeskStamp.Approved ? approvedRelease : deniedRelease, Pitch());
    }

    /// <summary>The left button went down on a hanging dater: it waits to see a drag (a carry) or a hold (a held stroke); the first touch of the day rolls its wheels.</summary>
    private void Grip(Handle handle)
    {
        if (!_live || !_flow.BarOut)
            return;
        Roll(handle);
        handle.Gripped = true;
        handle.GripAt = UiMotion.Now;
    }

    /// <summary>The button came back up: a held stroke releases (at once, or as soon as it bottoms out); a click that neither dragged nor held yet strokes down and straight up.</summary>
    private void Ungrip(Handle handle)
    {
        if (handle.Stroke == Stroke.Down && handle.Holding)
        {
            handle.Holding = false;
            if (handle.Contact)
                Release(handle);
            return;
        }
        if (!handle.Gripped)
            return;
        handle.Gripped = false;
        if (handle.Drag == null || !handle.Drag.IsDragging)
            Press(handle, false);
    }

    /// <summary>A dater's drag begins: it is carried (a running stroke is cut, its motion stops where it is); the first touch of the day rolls its wheels.</summary>
    private void PickUp(Handle handle)
    {
        Deselect();
        handle.Gripped = false;
        Roll(handle);
        EndStroke(handle);
        handle.Returning = false;
        _carried = handle;
    }

    /// <summary>A carried dater let go: it strokes there (down and straight up), then goes back to the rack.</summary>
    private void LetGo(Handle handle)
    {
        if (_carried == handle)
            _carried = null;
        Press(handle, false);
    }

    /// <summary>A carried dater's drag cut short (a right-click or Esc, the bar stowed, the daters' input taken away): it goes back to the rack without a press.</summary>
    private void PutBack(Handle handle)
    {
        if (_carried == handle)
            _carried = null;
        Return(handle);
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
    private string Word(bool approved) => style == null ? (approved ? "APPROVED" : "DENIED") : approved ? style.approvedStamp : style.deniedStamp;

    /// <summary>The verdict word's ink: APPROVED green, DENIED red (Saleh 2026-10-07: easy to tell apart).</summary>
    private static Color32 Ink(bool approved) => approved ? DaterImpressionArt.ApprovedInk : DaterImpressionArt.DeniedInk;

    /// <summary>This press's pitch: 1 ± PitchSpread, a value of the press's number (no draw).</summary>
    private float Pitch() => 1f + (DaterInk.Hash01(_day, _presses, 9) * 2f - 1f) * PitchSpread;

    /// <summary>The first document or the rulebook straight under <paramref name="die"/> (the top of a pile), and where the ray met it; null when it meets neither.</summary>
    private Component ThingUnder(Transform die, out Vector3 point)
    {
        point = default;
        if (die == null)
            return null;
        Physics.SyncTransforms(); // a document or a dater moved this frame is where the ray looks for it
        return FirstAlong(new Ray(die.position + Vector3.up * 0.001f, Vector3.down), RayLength, out point);
    }

    /// <summary>A carried dater's aim (its drag's): where the pointer's <paramref name="ray"/> meets the first document or the rulebook, so the die goes straight above the spot the pointer shows on the paper; null over the bare desk (the drag uses the desk plane).</summary>
    private Vector3? Aim(Ray ray) => FirstAlong(ray, AimLength, out Vector3 point) != null ? point : (Vector3?)null;

    /// <summary>The first document or the rulebook along <paramref name="ray"/> within <paramref name="length"/> (daters and the rest of the office are passed through), and where it was met; null when it meets neither.</summary>
    private Component FirstAlong(Ray ray, float length, out Vector3 point)
    {
        point = default;
        int n = Physics.RaycastNonAlloc(ray, _hits, length, paperLayers, QueryTriggerInteraction.Collide);
        Component best = null;
        float nearest = float.MaxValue;
        for (int i = 0; i < n; i++)
        {
            Component thing = _hits[i].collider.GetComponentInParent<DeskDocument>();
            if (thing == null)
                thing = _hits[i].collider.GetComponentInParent<DeskRulebook>();
            if (thing == null || _hits[i].distance >= nearest)
                continue;
            nearest = _hits[i].distance;
            best = thing;
            point = _hits[i].point;
        }
        return best;
    }

    /// <summary>Moves the drawer, moves each dater (a held press waking into a stroke, the stroke, the shake, the way back, the wheels) and lets a note go once its time is up.</summary>
    private void Update()
    {
        float dt = UiMotion.Delta(Time.unscaledDeltaTime);
        MoveDrawer(dt);
        foreach (Handle handle in _handles)
            Move(handle, dt);
        if (_noteKey != null && UiMotion.Now >= _noteUntil)
        {
            _noteKey = null;
            Show();
        }
    }

    /// <summary>One dater's motion this frame, every part on its spring (MotionKnobs; cuts under Reduced Motion).</summary>
    private void Move(Handle handle, float dt)
    {
        if (handle.Click == null)
            return;
        MotionKnobs knobs = UiMotion.Knobs;
        if (handle.Gripped && handle.Stroke == Stroke.None && (handle.Drag == null || !handle.Drag.IsDragging)
            && UiMotion.Now - handle.GripAt >= knobs.daterHoldDelay)
        {
            handle.Gripped = false;
            Press(handle, true);
        }

        Transform root = handle.Click.transform;
        bool placed = handle.Stroke != Stroke.None || handle.Returning; // this frame sets the dater's place outright
        if (handle.Stroke != Stroke.None)
        {
            SpringTuning tuning = knobs.Get(handle.Stroke == Stroke.Down ? knobs.daterFeel : knobs.daterReleaseFeel);
            // The way up settles loosely (a millimetre): the dater heads back to the rack while its last wobble dies out.
            bool moving = handle.Stroke == Stroke.Down ? handle.Depth.Step(dt, tuning, 1e-5f, 1e-3f) : handle.Depth.Step(dt, tuning, 1e-3f, 0.05f);
            float depth = handle.Depth.Value, drop = Drop;
            // The stroke is in metres; the dater's parent (the drawer, DeskConfigSO.stampDrawerScale) may be scaled.
            float perMetre = root.parent != null && root.parent.lossyScale.y > 1e-4f ? 1f / root.parent.lossyScale.y : 1f;
            root.localPosition = handle.PressedAt + Vector3.down * (Mathf.Min(depth, drop) * perMetre);
            if (handle.Body != null)
                handle.Body.localPosition = handle.BodyHome + Vector3.down * (Mathf.Max(0f, depth - drop) * perMetre);
            if (handle.Stroke == Stroke.Down)
            {
                if (!handle.Contact && depth >= drop + knobs.daterCompress * 0.8f)
                    Bottom(handle);
                if (handle.Contact && !handle.Holding && UiMotion.Now - handle.ContactAt >= knobs.daterQuickHold)
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
            bool moving = handle.Back.Step(dt, knobs.Get(knobs.appearFeel), 1e-4f, 1e-3f);
            if (UiMotion.Amount.Still)
            {
                handle.Back.Snap(1f);
                moving = false;
            }
            root.localPosition = Vector3.LerpUnclamped(handle.ReturnFrom, handle.Home, handle.Back.Value);
            if (!moving)
            {
                root.localPosition = handle.Home;
                handle.Returning = false;
            }
        }

        // A refusal's shake rides on top of wherever the dater is.
        if (!handle.Sway.AtRest || handle.SwayShown != 0f)
        {
            handle.Sway.Step(dt, knobs.Get(knobs.refuseFeel), 1e-5f, 1e-3f);
            float sway = handle.Sway.Value;
            root.localPosition += Vector3.right * (placed ? sway : sway - handle.SwayShown);
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
                if (UiMotion.Amount.Still)
                    handle.WheelAngle[w].Snap(handle.WheelAngle[w].Target);
                else
                    handle.WheelAngle[w].Step(dt, knobs.Get(knobs.wheelFeel), 0.01f, 0.1f);
                ApplyWheel(handle, w);
            }
    }

    /// <summary>Ends a stroke where it started: the body home, the depth at rest, a print still owed dropped.</summary>
    private void EndStroke(Handle handle)
    {
        if (handle.Stroke == Stroke.None)
            return;
        handle.Stroke = Stroke.None;
        handle.Holding = false;
        handle.PrintOn = null;
        handle.Depth.Snap(0f);
        if (handle.Body != null)
            handle.Body.localPosition = handle.BodyHome;
        handle.Click.transform.localPosition = handle.PressedAt;
    }

    /// <summary>Sends a dater away from the rack back to its place on a spring (nothing when it is there, carried or stroking).</summary>
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
    /// The drawer this frame (DrawerSequence on MotionKnobs' drawer tunings; a
    /// snap under Reduced Motion): its place between in and out, the
    /// mechanism's pose, and its moments: the opening's carry (DrawerOpen),
    /// the stop's small hit, a click as each cradle locks or lands (a lock
    /// lets its dater take input), the closing's shove (DrawerClose). The
    /// cradles wait to fold while a dater is away from its place. The rack
    /// shows unless the drawer is all the way in.
    /// </summary>
    private void MoveDrawer(float dt)
    {
        if (rack == null || (_drawer.Closed && !_flow.BarOut))
            return;
        bool away = false;
        foreach (Handle h in _handles)
            away |= h == _carried || h.Stroke != Stroke.None || h.Returning;
        MotionKnobs knobs = UiMotion.Knobs;
        DrawerEvents events = _drawer.Step(dt, _flow.BarOut, knobs, UiMotion.Amount.Still, away);
        if (_laid)
            rack.position = Vector3.LerpUnclamped(_in, _out, _drawer.Travel);
        if (drawer != null)
            drawer.Pose(_drawer.Raise(DrawerSequence.Denied), _drawer.Raise(DrawerSequence.Approved));
        if ((events & DrawerEvents.Carried) != 0)
            PlayCue(SoundCues.DrawerOpen, drawerOpen);
        if ((events & DrawerEvents.Stopped) != 0)
            FeelDirector.Hit(knobs.drawerStopHit);
        if ((events & (DrawerEvents.LockedDenied | DrawerEvents.LockedApproved | DrawerEvents.FoldedDenied | DrawerEvents.FoldedApproved)) != 0)
        {
            // Each cradle's lock (and landing) clicks: the date wheels' ratchet click, pitched down for the heavier catch.
            CueSounds.Play(SoundCues.DaterWheelClick, sound, LockPitch);
            Show();
        }
        if ((events & DrawerEvents.Shoved) != 0)
            PlayCue(SoundCues.DrawerClose, drawerClose);
        bool shown = !_drawer.Closed;
        if (rack.gameObject.activeSelf != shown)
            rack.gameObject.SetActive(shown);
    }

    /// <summary>The pitch of a cradle's lock click (the wheels' ratchet click, lower: a heavier catch).</summary>
    private const float LockPitch = 0.62f;

    /// <summary>Plays <paramref name="cue"/> from the sound bank, else its placeholder <paramref name="placeholder"/> through the tray's source.</summary>
    private void PlayCue(string cue, AudioClip placeholder)
    {
        if (!Sounds.Play(cue))
            Play(placeholder, 1f);
    }

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

    /// <summary>The daters' input (pressed and dragged while the bar is out and its cradle is locked upright) and the hint: a note while it lasts; else, out, to drag a dater onto the passport, or (stamped) to hand the papers back on the counter.</summary>
    private void Show()
    {
        foreach (Handle handle in _handles)
        {
            bool usable = _live && _flow.BarOut && _drawer.Locked(handle.Lane);
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
            : _noteKey ?? (!_live ? null : _flow.CanHandBack ? "stamp.hint.handBack" : _flow.BarOut ? "stamp.hint.place" : null);
        GameObject plate = hint.transform.parent != null ? hint.transform.parent.gameObject : hint.gameObject;
        if (plate.activeSelf != (key != null))
            plate.SetActive(key != null);
        if (key != null)
            hint.text = UiText.Get(key);
    }
}
