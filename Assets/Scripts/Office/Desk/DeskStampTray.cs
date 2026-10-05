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
/// screen to bring out the stamp stuff, and the Tab shortcut"). A grey tab on
/// the right edge of the office overlay ("STAMPS" over "TAB"), the TAB key
/// (OfficeControls) and the desk's stamp prop slide the bar out over the desk
/// and back (ToggleBar); out, it brings the reading view. The bar is a rack
/// in the desk's own materials holding the DENIED stamp (left) and the
/// APPROVED stamp (right), the art's desk stamp model (a wooden handle, a
/// green or red cap, the word on the block), each hanging DeskConfigSO.stampHover
/// above the desk with its word printed on the rail over it; it slides in
/// from the desk's right (stampBarTravel) to the point the reading view shows
/// at stampBarView, and is hidden while in. There is no ink: Papers, Please's
/// stamps have none, so a stamp is only clicked. A left-click on a stamp
/// presses it on whatever lies under its die (a ray straight down from the
/// die's centre: the first document or the rulebook it meets): only the
/// passport's ENTRY VISA box takes it (DeskDocument.InVisaBox), and only once
/// (StampFlow: the second stamp, another paper or the rulebook, the passport
/// outside its visa box are refused with a thunk, a shake and a short note;
/// no mark). The accepted press dips the stamp onto the paper, prints the
/// mark where it landed (DeskDocument.Stamp) and is the passport's verdict;
/// the strip on the counter then reads "▲ HAND BACK ▲", and the stamped
/// passport dragged onto the counter hands the papers back (DeskController
/// calls HandBack: Decided), the only way a case is decided (the PC only
/// investigates). A right-click or Esc slides the bar back
/// (ControlRules.BackOut). The bar takes input while BoothRules.StampsLive
/// (false slides it back) and shows its tab while the desk takes input
/// (BoothRules.PropsLive). The hint at the top right says the next step. The
/// thump and the thunk are made in code (no sound asset yet). Build Office UI
/// builds the tab, the hint and the audio source on the overlay and the rack
/// and its stamps in the office; the office binder lays the rack (Lay).
/// </summary>
public sealed class DeskStampTray : MonoBehaviour
{
    /// <summary>The desk tuning (the stamps' hover, the bar's place, travel and slide, a press's time, a note's time).</summary>
    [SerializeField] private DeskConfigSO config;

    /// <summary>The reading view (optional): the bar slid out brings it, and the bar hangs where it shows the desk at DeskConfigSO.stampBarView.</summary>
    [SerializeField] private DeskView deskView;

    /// <summary>The desk plane (the bar's height and, without the reading view, its place).</summary>
    [SerializeField] private DeskSurface surface;

    /// <summary>The rack in the office (the rail, the arms, the two stamps): hidden while in.</summary>
    [SerializeField] private Transform rack;

    /// <summary>The grey tab (with "TAB" printed on it): a click slides the bar out or back.</summary>
    [SerializeField] private Button tab;

    /// <summary>The APPROVED stamp's click box (its object is the stamp: it dips on a press).</summary>
    [SerializeField] private Clickable approvedStamp;

    /// <summary>The DENIED stamp's click box.</summary>
    [SerializeField] private Clickable deniedStamp;

    /// <summary>The APPROVED stamp's die: its centre, at the stamp's foot, is where it presses.</summary>
    [SerializeField] private Transform approvedDie;

    /// <summary>The DENIED stamp's die.</summary>
    [SerializeField] private Transform deniedDie;

    /// <summary>The hint at the top right (the next step, or why a press was refused); its parent is its plate.</summary>
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

    private readonly StampFlow _flow = new StampFlow();
    /// <summary>The ray's hits (room for every click box under a stamp: the rulebook's rows and tabs, the papers, the mat).</summary>
    private readonly RaycastHit[] _hits = new RaycastHit[64];
    private int _passport = -1;
    private bool _live;
    private float _slide;
    private Vector3 _out, _in;
    private bool _laid;
    private Transform _pressed;
    private Vector3 _pressedHome;
    private float _pressElapsed = -1f;
    private bool _pressRefused;
    private string _noteKey;
    private float _noteUntil;
    private AudioClip _thump, _thunk;

    /// <summary>True while the passport carries a verdict (the decision skips the leaving papers' own verdict ink: the player's is on them).</summary>
    public bool HasVerdict => _flow.CanHandBack;

    /// <summary>True while the bar is out.</summary>
    public bool BarOut => _flow.BarOut;

    /// <summary>What the last press did (the probes read it).</summary>
    public StampPress LastPress { get; private set; }

    /// <summary>Raised when the bar or the verdict changes (the counter's label, the booth's rules).</summary>
    public event Action Changed;

    /// <summary>Raised when the papers are handed back with the passport's verdict: true for APPROVED.</summary>
    public event Action<bool> Decided;

    private void Awake()
    {
        if (tab != null)
            tab.onClick.AddListener(ToggleBar);
        if (approvedStamp != null)
            approvedStamp.onClick.AddListener(() => Press(DeskStamp.Approved));
        if (deniedStamp != null)
            deniedStamp.onClick.AddListener(() => Press(DeskStamp.Denied));
        if (rack != null)
            rack.gameObject.SetActive(false);
        _thump = Tone("StampThump", 140f, 0.09f, 0.9f);
        _thunk = Tone("StampThunk", 70f, 0.16f, 0.7f);
        Show();
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
        rack.SetPositionAndRotation(Vector3.Lerp(_in, _out, DeskZones.Ease(_slide)), Quaternion.LookRotation(forward, Vector3.up));
    }

    /// <summary>The grey tab, TAB, the desk's stamp: slides the bar out (bringing the reading view) or back; nothing while the stamps take no input.</summary>
    public void ToggleBar()
    {
        Deselect();
        if (!_live)
            return;
        if (_flow.ToggleBar() && deskView != null)
            deskView.TiltIn();
        Raise();
    }

    /// <summary>Slides the bar back (a right-click or Esc: ControlRules.BackOut); false when it is in already.</summary>
    public bool Stow()
    {
        if (!_flow.StowBar())
            return false;
        Raise();
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

    /// <summary>A new traveller (DeskController): <paramref name="passport"/> is the paper whose ENTRY VISA box takes the verdict (the first paper handed over; -1: none). No verdict yet.</summary>
    public void BeginCase(int passport)
    {
        _passport = passport;
        _noteKey = null;
        _flow.BeginCase();
        Raise();
    }

    /// <summary>The decision (DeskController): no passport, no verdict.</summary>
    public void EndCase() => BeginCase(-1);

    /// <summary>True when paper <paramref name="index"/> is the passport and it carries a verdict (dropped on the counter, the papers go back).</summary>
    public bool CanHandBack(int index) => index >= 0 && index == _passport && _flow.CanHandBack;

    /// <summary>The papers handed back with the passport's verdict (the stamped passport dropped on the counter: DeskController): Decided (nothing without a verdict).</summary>
    public void HandBack()
    {
        if (!_flow.CanHandBack)
            return;
        _flow.StowBar();
        Decided?.Invoke(_flow.Verdict == DeskStamp.Approved);
    }

    /// <summary>
    /// A stamp clicked: it presses on what lies under its die (a ray straight
    /// down from the die's centre meets a document or the rulebook first, or
    /// nothing): StampFlow decides; an accepted press prints the mark where it
    /// landed in the visa box and thumps; a refused one thunks, shakes the
    /// stamp and says why; a press on the bare desk thumps and says where to
    /// put the passport.
    /// </summary>
    public void Press(DeskStamp stamp)
    {
        Deselect();
        if (!_live || !_flow.BarOut)
            return;
        Transform die = stamp == DeskStamp.Approved ? approvedDie : deniedDie;
        Component under = ThingUnder(die, out Vector3 point);
        var paper = under as DeskDocument;
        bool onPassport = paper != null && paper.Index == _passport;
        LastPress = _flow.Press(stamp, under != null, onPassport, onPassport && paper.InVisaBox(point));
        if (LastPress == StampPress.Stamped)
            paper.Stamp(stamp == DeskStamp.Approved, paper.PagePoint(point));
        _noteKey = LastPress switch
        {
            StampPress.NotPassport => "stamp.refused.notPassport",
            StampPress.OutsideVisa => "stamp.refused.outsideVisa",
            StampPress.AlreadyStamped => "stamp.refused.already",
            StampPress.Nothing => "stamp.refused.nothing",
            _ => null
        };
        _noteUntil = Time.unscaledTime + (config != null ? config.stampNoteSeconds : 2.5f);
        bool refused = LastPress != StampPress.Stamped && LastPress != StampPress.Nothing;
        Play(refused ? _thunk : _thump);
        Dip(stamp == DeskStamp.Approved ? approvedStamp : deniedStamp, refused);
        Raise();
    }

    /// <summary>The first document or the rulebook straight under <paramref name="die"/> (the top of a pile), and where the ray met it; null when it meets neither.</summary>
    private Component ThingUnder(Transform die, out Vector3 point)
    {
        point = default;
        if (die == null)
            return null;
        Physics.SyncTransforms(); // a document dragged this frame is where the ray looks for it
        int n = Physics.RaycastNonAlloc(new Ray(die.position + Vector3.up * 0.001f, Vector3.down), _hits, RayLength, paperLayers, QueryTriggerInteraction.Collide);
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

    /// <summary>Slides the bar, dips (or shakes) the pressed stamp, and lets a refusal's note go once its time is up.</summary>
    private void Update()
    {
        SlideBar();
        if (_pressElapsed >= 0f && _pressed != null)
        {
            float seconds = config != null && !MotionPreference.Reduced ? config.stampPressSeconds : 0f;
            _pressElapsed += Time.unscaledDeltaTime;
            float t = seconds > 0f ? Mathf.Clamp01(_pressElapsed / seconds) : 1f;
            float wave = Mathf.Sin(t * Mathf.PI);
            float depth = config != null ? Mathf.Max(0f, config.stampHover - PressFloor) : 0f;
            _pressed.localPosition = _pressedHome + (_pressRefused
                ? new Vector3(Mathf.Sin(t * Mathf.PI * 4f) * Shake * (1f - t), 0f, 0f)
                : Vector3.down * depth * wave);
            if (t >= 1f)
            {
                _pressed.localPosition = _pressedHome;
                _pressElapsed = -1f;
            }
        }
        if (_noteKey != null && Time.unscaledTime >= _noteUntil)
        {
            _noteKey = null;
            Show();
        }
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
            rack.position = Vector3.Lerp(_in, _out, DeskZones.Ease(_slide));
        bool shown = _slide > 0f;
        if (rack.gameObject.activeSelf != shown)
            rack.gameObject.SetActive(shown);
    }

    /// <summary>Starts a stamp's dip onto the paper (or its shake, <paramref name="refused"/>).</summary>
    private void Dip(Clickable stamp, bool refused)
    {
        if (stamp == null)
            return;
        if (_pressed != null && _pressElapsed >= 0f)
            _pressed.localPosition = _pressedHome;
        _pressed = stamp.transform;
        _pressedHome = _pressed.localPosition;
        _pressRefused = refused;
        _pressElapsed = 0f;
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

    /// <summary>The stamps' interactivity and the hint: a refusal's note while it lasts; else, out, where to put the passport, or (stamped) to hand it back on the counter; the hand-back also with the bar in.</summary>
    private void Show()
    {
        foreach (Clickable stamp in new[] { approvedStamp, deniedStamp })
            if (stamp != null)
                stamp.Interactable = _live && _flow.BarOut;
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

    /// <summary>A short decaying tone made in code (<paramref name="hertz"/>, <paramref name="seconds"/> long, at <paramref name="volume"/>): the stamp's thump and a refusal's thunk until a sound asset exists.</summary>
    private static AudioClip Tone(string name, float hertz, float seconds, float volume)
    {
        const int rate = 22050;
        int n = Mathf.Max(1, (int)(rate * seconds));
        var samples = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / rate;
            samples[i] = volume * Mathf.Sin(2f * Mathf.PI * hertz * t) * Mathf.Exp(-t * 6f / seconds);
        }
        AudioClip clip = AudioClip.Create(name, n, 1, rate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
