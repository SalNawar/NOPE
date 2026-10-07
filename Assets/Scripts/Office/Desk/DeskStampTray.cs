using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// The stamp bar, Papers, Please's way with the art's 3D stamps (Saleh
/// 2026-10-06: "always have some shortcuts to pull the stamp like Papers,
/// Please, they have this grey button ... it should be clear which document
/// to approve", then "I want the 3D stamp; there should be a label on the
/// screen to bring out the stamp stuff, and the Tab shortcut", then "the
/// stamp should be two stamps that I physically move, not move the document
/// under like Papers, Please ... I should be able to stamp anywhere on the
/// document"). A grey tab on the right edge of the office overlay ("STAMPS"
/// over "TAB"), the TAB key (OfficeControls) and the desk's stamp prop slide
/// the bar out over the desk and back (ToggleBar); out, it brings the reading
/// view. The bar is a rack in the desk's own materials holding the DENIED
/// stamp (left) and the APPROVED stamp (right), the art's desk stamp model (a
/// wooden handle, a green or red cap, the word on the block), each hanging
/// DeskConfigSO.stampHover above the desk with its word printed on the rail
/// over it; it slides in from the desk's right (stampBarTravel) to the point
/// the reading view shows at stampBarView, and is hidden while in. A stamp is
/// moved, not the paper: left-press and drag a stamp (its DeskDraggable, the
/// papers' one input model) and it follows the pointer over the desk, still
/// stampHover up, over the papers, its die straight above the pointer's point
/// wherever the stamp was grabbed (the drag's anchor); letting go presses it there and it goes
/// back to its place in the rack (stampReturnSeconds); a left-click on a
/// stamp presses it where it hangs. A press stamps whatever lies under its
/// die (a ray straight down from the die's centre: the first document or the
/// rulebook it meets): only the passport takes it, anywhere on it, and the
/// mark prints exactly there, over the boxes too (Saleh 2026-10-06: "when I
/// stamp it doesn't stamp where I'm pressing"; the ENTRY VISA box is a guide:
/// DeskDocument.Stamp, StampSpots.AtPoint), and only once (StampFlow: the
/// second stamp, another paper or the rulebook are refused with a thunk, a
/// shake and a short note; no mark); pressed on the bare desk the stamp just
/// goes back. The accepted press dips the stamp onto the paper and is the
/// passport's verdict; the counter then reads "▲ HAND BACK ▲", and a paper
/// dropped on the counter hands the papers back (DeskController calls
/// HandBack: Decided), the only way a case is decided (the PC only
/// investigates). The press is the game feel's slam (Saleh 2026-10-07): the
/// stamp rises a little (the anticipation, MotionKnobs.stampLift), slams
/// down, squashes on impact (stampSquash, keeping its volume), the desk
/// shakes (FeelDirector.Punch: a hit-stop and a Cinemachine impulse) and the mark's ink blooms in (DeskDocument.BloomLastStamp:
/// the mark is printed, and counts, at the press; only its look waits for the
/// impact), then the stamp rebounds on its spring (stampFeel) and goes back
/// (the way back and the bar's slide on the desk's spring curve, their seconds
/// the desk's knobs); a refused press shakes sideways on a spring; Reduced
/// Motion cuts it all (the mark shows at once). There is no ink: Papers, Please has none. A right-click or
/// Esc drops a carried stamp back into the rack, else slides the bar back
/// (ControlRules.BackOut). The bar takes input while BoothRules.StampsLive
/// (false slides it back) and shows its tab while the desk takes input
/// (BoothRules.PropsLive). The hint at the top right says the next step, or
/// a note (a refusal's, the counter's "Stamp the passport first": Note). The
/// thump and the thunk are made in code until the sound bank has stamp_approve /
/// stamp_deny and ui_error clips (Sounds), and the bar plays stamp_bar_out /
/// stamp_bar_in. Build Office UI
/// builds the tab, the hint and the audio source on the overlay and the rack
/// and its stamps in the office; the office binder lays the rack (Lay).
/// </summary>
public sealed class DeskStampTray : MonoBehaviour
{
    /// <summary>The desk tuning (the stamps' hover, the bar's place, travel and slide, a press's and a return's time, a note's time).</summary>
    [SerializeField] private DeskConfigSO config;

    /// <summary>The reading view (optional): the bar slid out brings it, and the bar hangs where it shows the desk at DeskConfigSO.stampBarView.</summary>
    [SerializeField] private DeskView deskView;

    /// <summary>The desk plane (the bar's height and, without the reading view, its place; the stamps are dragged over it).</summary>
    [SerializeField] private DeskSurface surface;

    /// <summary>The rack in the office (the rail, the arms, the two stamps): hidden while in.</summary>
    [SerializeField] private Transform rack;

    /// <summary>The grey tab (with "TAB" printed on it): a click slides the bar out or back.</summary>
    [SerializeField] private Button tab;

    /// <summary>The APPROVED stamp's click box (its object is the stamp, with its DeskDraggable: it is dragged, and dips on a press).</summary>
    [SerializeField] private Clickable approvedStamp;

    /// <summary>The DENIED stamp's click box.</summary>
    [SerializeField] private Clickable deniedStamp;

    /// <summary>The APPROVED stamp's die: its centre, at the stamp's foot, is where it presses.</summary>
    [SerializeField] private Transform approvedDie;

    /// <summary>The DENIED stamp's die.</summary>
    [SerializeField] private Transform deniedDie;

    /// <summary>The hint at the top right (the next step, or a note); its parent is its plate.</summary>
    [SerializeField] private TMP_Text hint;

    /// <summary>Plays the press's thump and a refusal's thunk (optional).</summary>
    [SerializeField] private AudioSource sound;

    /// <summary>The physics layers a stamp's ray meets documents on (the Interactable layer).</summary>
    [SerializeField] private LayerMask paperLayers = ~0;

    /// <summary>How far a refused stamp shakes sideways (metres).</summary>
    private const float Shake = 0.006f;

    /// <summary>How far above the desk a press stops (metres): on the paper lying there.</summary>
    private const float PressFloor = 0.003f;

    /// <summary>The longest ray a stamp casts down (metres).</summary>
    private const float RayLength = 0.5f;

    /// <summary>The longest pointer ray a carried stamp aims along (metres: the camera to the desk).</summary>
    private const float AimLength = 10f;

    /// <summary>One stamp of the rack: what it stamps, its parts, its place in the rack and its motion (a press's dip or shake, then the way back to its place).</summary>
    private sealed class Handle
    {
        /// <summary>The stamp it prints (APPROVED or DENIED).</summary>
        public DeskStamp Kind;

        /// <summary>Its click box (the stamp's object).</summary>
        public Clickable Click;

        /// <summary>Its die's centre (where it presses).</summary>
        public Transform Die;

        /// <summary>Its drag over the desk (null: it is only clicked).</summary>
        public DeskDraggable Drag;

        /// <summary>Its place in the rack (local).</summary>
        public Vector3 Home;

        /// <summary>Its scale at rest (a slam squashes it and gives it back).</summary>
        public Vector3 RestScale = Vector3.one;

        /// <summary>Seconds into its press (-1: no press runs).</summary>
        public float Pressing = -1f;

        /// <summary>True once the running press struck the paper (its rebound and squash then spring back).</summary>
        public bool Struck;

        /// <summary>After the impact: its height over the press point (metres) and its squash (a share shorter); a refusal's sideways shake (metres).</summary>
        public Spring Rebound, Squash, Shake;

        /// <summary>True when the running press was refused (a shake, not a dip).</summary>
        public bool Refused;

        /// <summary>Where the running press started (local): it dips from there.</summary>
        public Vector3 PressedAt;

        /// <summary>Seconds into its way back to the rack (-1: not going back).</summary>
        public float Returning = -1f;

        /// <summary>Where its way back started (local).</summary>
        public Vector3 ReturnFrom;
    }

    private readonly StampFlow _flow = new StampFlow();
    /// <summary>The ray's hits (room for every click box under a stamp: the rulebook's rows and tabs, the papers, the mat).</summary>
    private readonly RaycastHit[] _hits = new RaycastHit[64];
    private Handle[] _handles = Array.Empty<Handle>();
    private Handle _carried;
    private int _passport = -1;
    private bool _live;
    private float _slide;
    private Vector3 _out, _in;
    private bool _laid;
    private string _noteKey;
    private float _noteUntil;
    private AudioClip _thump, _thunk;

    /// <summary>True while the passport carries a verdict (the decision skips the leaving papers' own verdict ink: the player's is on them; a paper dropped on the counter hands the papers back).</summary>
    public bool HasVerdict => _flow.CanHandBack;

    /// <summary>True while the bar is out.</summary>
    public bool BarOut => _flow.BarOut;

    /// <summary>True while a stamp is dragged over the desk (a right-click or Esc drops it back into the rack: CancelCarry).</summary>
    public bool IsCarrying => _carried != null;

    /// <summary>What the last press did (the probes read it).</summary>
    public StampPress LastPress { get; private set; }

    /// <summary>Where the last press met a paper or the rulebook (straight under the stamp's die; the probes read it).</summary>
    public Vector3 LastPressPoint { get; private set; }

    /// <summary>Raised when the bar or the verdict changes (the counter's label, the booth's rules).</summary>
    public event Action Changed;

    /// <summary>Raised when the papers are handed back with the passport's verdict: true for APPROVED.</summary>
    public event Action<bool> Decided;

    private void Awake()
    {
        if (tab != null)
            tab.onClick.AddListener(ToggleBar);
        _handles = new[] { MakeHandle(DeskStamp.Approved, approvedStamp, approvedDie), MakeHandle(DeskStamp.Denied, deniedStamp, deniedDie) };
        if (rack != null)
            rack.gameObject.SetActive(false);
        _thump = CodeTones.Tone("StampThump", 140f, 0.09f, 0.9f);
        _thunk = CodeTones.Tone("StampThunk", 70f, 0.16f, 0.7f);
        Show();
    }

    /// <summary>A stamp of the rack: a click presses it where it hangs, a drag carries it stampHover over the desk, its die over the pointer (the drag's anchor), and the release presses it there.</summary>
    private Handle MakeHandle(DeskStamp kind, Clickable click, Transform die)
    {
        var handle = new Handle { Kind = kind, Click = click, Die = die };
        if (click == null)
            return handle;
        handle.Home = click.transform.localPosition;
        handle.RestScale = click.transform.localScale;
        click.onClick.AddListener(() => Press(handle));
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

    private void OnDestroy()
    {
        if (_thump != null)
            Destroy(_thump);
        if (_thunk != null)
            Destroy(_thunk);
    }

    /// <summary>
    /// Lays the rack (the office binder, once the desk and the reading view
    /// are placed): out where the reading view shows the desk at
    /// DeskConfigSO.stampBarView, the stamps' feet stampHover above the desk,
    /// facing along the office view's level <paramref name="levelForward"/>;
    /// in stampBarTravel to the right of it.
    /// </summary>
    public void Lay(Vector3 levelForward)
    {
        if (rack == null || surface == null || config == null)
            return;
        Vector3 forward = Vector3.ProjectOnPlane(levelForward, Vector3.up);
        if (forward.sqrMagnitude < 1e-6f)
            forward = Vector3.forward;
        forward.Normalize();
        Vector3 right = Vector3.Cross(Vector3.up, forward);
        float desk = surface.transform.position.y;
        if (deskView == null || !deskView.TryViewPoint(config.stampBarView, desk, out Vector3 at))
            at = surface.transform.position;
        _out = new Vector3(at.x, desk + config.stampHover, at.z);
        _in = _out + right * config.stampBarTravel;
        _laid = true;
        rack.SetPositionAndRotation(Vector3.LerpUnclamped(_in, _out, BarCurve(config.stampBarSeconds)), Quaternion.LookRotation(forward, Vector3.up));
    }

    /// <summary>The grey tab, TAB, the desk's stamp: slides the bar out (bringing the reading view) or back; nothing while the stamps take no input.</summary>
    public void ToggleBar()
    {
        Deselect();
        if (!_live)
            return;
        CancelCarry();
        bool slidOut = _flow.ToggleBar();
        Sounds.Play(slidOut ? SoundCues.StampBarOut : SoundCues.StampBarIn);
        if (slidOut && deskView != null)
            deskView.TiltIn();
        Raise();
    }

    /// <summary>Slides the bar back (a right-click or Esc: ControlRules.BackOut), a carried stamp dropped back into the rack first; false when it is in already.</summary>
    public bool Stow()
    {
        CancelCarry();
        if (!_flow.StowBar())
            return false;
        Sounds.Play(SoundCues.StampBarIn);
        Raise();
        return true;
    }

    /// <summary>Drops a carried stamp back into the rack without a press (a right-click or Esc mid-drag: ControlRules.BackOut's CancelDrag); false when no stamp is carried.</summary>
    public bool CancelCarry()
    {
        if (_carried == null)
            return false;
        _carried.Drag.Cancel();
        return true;
    }

    /// <summary>Lets the bar and its stamps take input (<paramref name="live"/>: BoothRules.StampsLive; false slides the bar back) and shows the grey tab (<paramref name="shown"/>: BoothRules.PropsLive).</summary>
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

    /// <summary>A new traveller (DeskController): <paramref name="passport"/> is the paper that takes the verdict (the first paper handed over; -1: none). No verdict yet.</summary>
    public void BeginCase(int passport)
    {
        _passport = passport;
        _noteKey = null;
        _flow.BeginCase();
        Raise();
    }

    /// <summary>The decision (DeskController): no passport, no verdict.</summary>
    public void EndCase() => BeginCase(-1);

    /// <summary>The papers handed back with the passport's verdict (a paper dropped on the counter once the passport is stamped: DeskController): Decided (nothing without a verdict).</summary>
    public void HandBack()
    {
        if (!_flow.CanHandBack)
            return;
        CancelCarry();
        if (_flow.StowBar())
            Sounds.Play(SoundCues.StampBarIn);
        Sounds.Play(SoundCues.PaperSlide);
        Decided?.Invoke(_flow.Verdict == DeskStamp.Approved);
    }

    /// <summary>Shows the note <paramref name="key"/> (a UI string) on the hint's plate for DeskConfigSO.stampNoteSeconds (DeskController: a paper bounced off the counter, "Stamp the passport first").</summary>
    public void Note(string key)
    {
        _noteKey = key;
        _noteUntil = Time.unscaledTime + (config != null ? config.stampNoteSeconds : 2.5f);
        Show();
    }

    /// <summary>
    /// Presses <paramref name="handle"/> on what lies under its die (a ray
    /// straight down from the die's centre meets a document or the rulebook
    /// first, or nothing): StampFlow decides; an accepted press prints the
    /// mark centred where the ray met the passport (anywhere on it), dips and
    /// thumps; a refused one thunks, shakes the stamp and
    /// says why; pressed on the bare desk the stamp just goes back. A stamp
    /// carried away from the rack goes back to it after the press (a click on
    /// a stamp presses it where it hangs).
    /// </summary>
    private void Press(Handle handle)
    {
        Deselect();
        if (!_live || !_flow.BarOut || handle.Click == null)
        {
            Return(handle);
            return;
        }
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
            paper.Stamp(handle.Kind == DeskStamp.Approved, paper.PagePoint(point));
            MotionKnobs knobs = UiMotion.Knobs;
            paper.BloomLastStamp(knobs.stampRiseSeconds + knobs.stampSlamSeconds);
        }
        string note = LastPress switch
        {
            StampPress.NotPassport => "stamp.refused.notPassport",
            StampPress.AlreadyStamped => "stamp.refused.already",
            _ => null
        };
        if (note != null)
            Note(note);
        bool refused = LastPress != StampPress.Stamped;
        if (refused && !Sounds.Play(SoundCues.UiError))
            Play(_thunk);
        Dip(handle, refused);
        Raise();
    }

    /// <summary>A stamp's drag begins: it is carried (its motion stops where it is).</summary>
    private void PickUp(Handle handle)
    {
        Deselect();
        handle.Pressing = handle.Returning = -1f;
        handle.Click.transform.localScale = handle.RestScale;
        _carried = handle;
    }

    /// <summary>A carried stamp let go: it presses there (Press), then goes back to the rack.</summary>
    private void LetGo(Handle handle)
    {
        if (_carried == handle)
            _carried = null;
        Press(handle);
    }

    /// <summary>A carried stamp's drag cut short (a right-click or Esc, the bar stowed, the stamps' input taken away): it goes back to the rack without a press.</summary>
    private void PutBack(Handle handle)
    {
        if (_carried == handle)
            _carried = null;
        Return(handle);
    }

    /// <summary>The first document or the rulebook straight under <paramref name="die"/> (the top of a pile), and where the ray met it; null when it meets neither.</summary>
    private Component ThingUnder(Transform die, out Vector3 point)
    {
        point = default;
        if (die == null)
            return null;
        Physics.SyncTransforms(); // a document or a stamp moved this frame is where the ray looks for it
        return FirstAlong(new Ray(die.position + Vector3.up * 0.001f, Vector3.down), RayLength, out point);
    }

    /// <summary>A carried stamp's aim (its drag's): where the pointer's <paramref name="ray"/> meets the first document or the rulebook, so the die goes straight above the spot the pointer shows on the paper; null over the bare desk (the drag uses the desk plane).</summary>
    private Vector3? Aim(Ray ray) => FirstAlong(ray, AimLength, out Vector3 point) != null ? point : (Vector3?)null;

    /// <summary>The first document or the rulebook along <paramref name="ray"/> within <paramref name="length"/> (stamps and the rest of the office are passed through), and where it was met; null when it meets neither.</summary>
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

    /// <summary>Slides the bar, moves each stamp (a press's dip or shake, then its way back to the rack) and lets a note go once its time is up.</summary>
    private void Update()
    {
        SlideBar();
        foreach (Handle handle in _handles)
            Move(handle);
        if (_noteKey != null && Time.unscaledTime >= _noteUntil)
        {
            _noteKey = null;
            Show();
        }
    }

    /// <summary>One stamp's motion this frame: the press (the slam: a rise, the fall, the impact's squash, shake and thump, the rebound; or a refusal's shake), then, away from the rack, the way back to its place (cuts under Reduced Motion).</summary>
    private void Move(Handle handle)
    {
        if (handle.Click == null)
            return;
        Transform stamp = handle.Click.transform;
        if (handle.Pressing >= 0f && Pressed(handle, stamp, Time.unscaledDeltaTime))
            return;
        if (handle.Returning >= 0f)
        {
            float seconds = config != null && !MotionPreference.Reduced ? config.stampReturnSeconds : 0f;
            handle.Returning += Time.unscaledDeltaTime;
            float t = seconds > 0f ? Mathf.Clamp01(handle.Returning / seconds) : 1f;
            stamp.localPosition = Vector3.LerpUnclamped(handle.ReturnFrom, handle.Home, UiMotion.Ease(t, UiMotion.Knobs.deskMoveFeel, seconds));
            if (t >= 1f)
                handle.Returning = -1f;
        }
    }

    /// <summary>
    /// Steps a running press by <paramref name="dt"/>: a refusal's shake on
    /// its spring; an accepted press's rise (stampRiseSeconds, easing out to
    /// stampLift), its slam (stampSlamSeconds, accelerating down onto the
    /// paper), the impact (Strike), then the rebound and the squash springing
    /// back. True while the press still runs; once it is over the stamp is at
    /// rest where it was pressed and starts back to the rack.
    /// </summary>
    private bool Pressed(Handle handle, Transform stamp, float dt)
    {
        MotionKnobs knobs = UiMotion.Knobs;
        MotionAmount amount = UiMotion.Amount;
        handle.Pressing += dt;
        if (handle.Refused)
        {
            if (handle.Shake.Step(dt, knobs.Get(knobs.refuseFeel), 1e-5f, 1e-4f))
            {
                stamp.localPosition = handle.PressedAt + Vector3.right * handle.Shake.Value;
                return true;
            }
        }
        else if (!handle.Struck)
        {
            float depth = config != null ? Mathf.Max(0f, config.stampHover - PressFloor) : 0f;
            float rise = amount.Still ? 0f : knobs.stampRiseSeconds, slam = amount.Still ? 0f : knobs.stampSlamSeconds;
            float lift = knobs.stampLift * amount.Share, t = handle.Pressing, y;
            if (t < rise)
            {
                float p = t / rise;
                y = lift * (1f - (1f - p) * (1f - p));
            }
            else if (t < rise + slam)
            {
                float p = (t - rise) / slam;
                y = lift - (lift + depth) * p * p;
            }
            else
            {
                Strike(handle, depth, knobs, amount);
                y = -depth;
            }
            stamp.localPosition = handle.PressedAt + Vector3.up * y;
            return true;
        }
        else
        {
            SpringTuning tuning = knobs.Get(knobs.stampFeel);
            bool moving = handle.Rebound.Step(dt, tuning, 1e-4f, 1e-3f) | handle.Squash.Step(dt, tuning, knobs.settleValue, knobs.settleSpeed);
            stamp.localPosition = handle.PressedAt + Vector3.up * handle.Rebound.Value;
            Stretch squash = SquashStretch.Preserve(1f - handle.Squash.Value);
            float across = Mathf.Sqrt(squash.Across); // the two other axes share the area's rule, so the volume keeps
            stamp.localScale = new Vector3(handle.RestScale.x * across, handle.RestScale.y * squash.Along, handle.RestScale.z * across);
            if (moving)
                return true;
        }
        stamp.localPosition = handle.PressedAt;
        stamp.localScale = handle.RestScale;
        handle.Pressing = -1f;
        handle.Struck = false;
        Return(handle);
        return false;
    }

    /// <summary>The impact: the thump (StampSlam's clip, else the made-up one), the desk's shake, and the rebound from <paramref name="depth"/> down and the squash (stampSquash) handed to their springs (at rest at once without motion).</summary>
    private void Strike(Handle handle, float depth, MotionKnobs knobs, MotionAmount amount)
    {
        handle.Struck = true;
        if (!Sounds.Play(handle.Kind == DeskStamp.Approved ? SoundCues.StampApprove : SoundCues.StampDeny))
            Play(_thump);
        FeelDirector.Punch(FeelHit.Stamp);
        handle.Rebound = amount.Still ? Spring.At(0f) : new Spring { Value = -depth, Target = 0f };
        handle.Squash = amount.Still ? Spring.At(0f) : new Spring { Value = knobs.stampSquash * amount.Share, Target = 0f };
    }

    /// <summary>Sends a stamp away from the rack back to its place (nothing when it is there, or carried).</summary>
    private void Return(Handle handle)
    {
        if (handle.Click == null || handle == _carried)
            return;
        Vector3 at = handle.Click.transform.localPosition;
        if ((at - handle.Home).sqrMagnitude < 1e-10f)
            return;
        handle.Pressing = -1f;
        handle.ReturnFrom = at;
        handle.Returning = 0f;
    }

    /// <summary>Eases the bar toward out or in (a cut under Reduced Motion); the rack shows while it is not all the way in.</summary>
    private void SlideBar()
    {
        if (rack == null)
            return;
        float target = _flow.BarOut ? 1f : 0f;
        if (Mathf.Approximately(_slide, target))
            return;
        float seconds = config != null && !MotionPreference.Reduced ? config.stampBarSeconds : 0f;
        _slide = seconds > 0f ? Mathf.MoveTowards(_slide, target, Time.unscaledDeltaTime / seconds) : target;
        if (_laid)
            rack.position = Vector3.LerpUnclamped(_in, _out, BarCurve(seconds));
        bool shown = _slide > 0f;
        if (rack.gameObject.activeSelf != shown)
            rack.gameObject.SetActive(shown);
    }

    /// <summary>Where the bar is along its travel (0 in, 1 out) on the desk's spring curve: past out as it arrives, past in as it goes back (UiMotion.Ease over <paramref name="seconds"/>).</summary>
    private float BarCurve(float seconds)
    {
        MotionFeel feel = UiMotion.Knobs.deskMoveFeel;
        return _flow.BarOut ? UiMotion.Ease(_slide, feel, seconds) : 1f - UiMotion.Ease(1f - _slide, feel, seconds);
    }

    /// <summary>Starts a stamp's slam onto the paper where it is (or its shake, <paramref name="refused"/>).</summary>
    private static void Dip(Handle handle, bool refused)
    {
        handle.Returning = -1f;
        if (handle.Pressing >= 0f)
        {
            handle.Click.transform.localPosition = handle.PressedAt; // a press while one runs starts from where that one did
            handle.Click.transform.localScale = handle.RestScale;
        }
        handle.PressedAt = handle.Click.transform.localPosition;
        handle.Refused = refused;
        handle.Struck = false;
        handle.Pressing = 0f;
        handle.Shake = Spring.At(0f);
        MotionAmount amount = UiMotion.Amount;
        if (refused && !amount.Still)
        {
            MotionKnobs knobs = UiMotion.Knobs;
            handle.Shake.Kick(knobs.Get(knobs.refuseFeel).KickFor(Shake * amount.Share));
        }
    }

    private void Play(AudioClip clip)
    {
        if (sound != null && clip != null)
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

    /// <summary>The stamps' input (clicked and dragged while the bar is out) and the hint: a note while it lasts; else, out, to drag a stamp onto the passport, or (stamped) to hand the papers back on the counter; the hand-back also with the bar in.</summary>
    private void Show()
    {
        bool usable = _live && _flow.BarOut;
        foreach (Handle handle in _handles)
        {
            if (handle.Click != null)
                handle.Click.Interactable = usable;
            if (handle.Drag != null && handle.Drag.enabled != usable)
                handle.Drag.enabled = usable;
        }
        if (hint == null)
            return;
        string key = !_live || _passport < 0 ? null
            : _noteKey ?? (_flow.CanHandBack ? "stamp.hint.handBack" : _flow.BarOut ? "stamp.hint.place" : null);
        GameObject plate = hint.transform.parent != null ? hint.transform.parent.gameObject : hint.gameObject;
        if (plate.activeSelf != (key != null))
            plate.SetActive(key != null);
        if (key != null)
            hint.text = UiText.Get(key);
    }
}
