using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// The stamp bar, Papers, Please's way (Saleh 2026-10-06: "always have some
/// shortcuts to pull the stamp like Papers, Please, they have this grey
/// button ... there is a bug that you can both approve and decline a paper;
/// also it should be clear which document to approve: there should be a box
/// for the approval. other documents shouldn't be approved"). A grey tab on
/// the right edge of the office overlay ("TAB" printed on it), the TAB key
/// (OfficeControls) and the desk's stamp prop slide the bar out from the
/// right edge and back (ToggleBar); out, it brings the reading view. The bar
/// holds the APPROVED and the DENIED stamp, each hanging over the desk with
/// its die: a framed window the paper under it shows through. A left-click on
/// a stamp presses it on whatever lies under its die (the office camera's ray
/// through the die's centre: the first document it meets): only the
/// passport's ENTRY VISA box takes it (DeskDocument.InVisaBox), and only
/// once (StampFlow: the second stamp, another paper, the passport outside
/// its visa box are refused with a thunk and a short note; no mark). The
/// accepted press prints the mark there (DeskDocument.Stamp) and is the
/// passport's verdict; the strip on the counter then reads "▲ HAND BACK ▲",
/// and the stamped passport dragged onto the counter hands the papers back
/// (DeskController calls HandBack: Decided), the only way a case is decided
/// (the PC only investigates). A right-click or Esc slides the bar back
/// (ControlRules.BackOut). The bar takes input while BoothRules.StampsLive
/// (false slides it back) and shows its tab while the desk takes input
/// (BoothRules.PropsLive). The hint over the bar says the next step. The
/// thump and the thunk are made in code (no sound asset yet). Build Office UI
/// builds the tab, the bar, the stamps and their dies, the hint and the
/// audio source.
/// </summary>
public sealed class DeskStampTray : MonoBehaviour
{
    /// <summary>The desk tuning (the bar's slide, a press's dip, a note's time).</summary>
    [SerializeField] private DeskConfigSO config;

    /// <summary>The reading view (optional): the bar slid out brings it.</summary>
    [SerializeField] private DeskView deskView;

    /// <summary>The sliding bar (the stamps hang from it), anchored to the overlay's right edge.</summary>
    [SerializeField] private RectTransform bar;

    /// <summary>The grey tab (with "TAB" printed on it): a click slides the bar out or back.</summary>
    [SerializeField] private Button tab;

    /// <summary>The APPROVED stamp: a click presses it.</summary>
    [SerializeField] private Button approvedStamp;

    /// <summary>The DENIED stamp: a click presses it.</summary>
    [SerializeField] private Button deniedStamp;

    /// <summary>The APPROVED stamp's die: the window under it is where it presses (its centre).</summary>
    [SerializeField] private RectTransform approvedDie;

    /// <summary>The DENIED stamp's die.</summary>
    [SerializeField] private RectTransform deniedDie;

    /// <summary>The hint over the bar (the next step, or why a press was refused); its parent is its plate.</summary>
    [SerializeField] private TMP_Text hint;

    /// <summary>Plays the press's thump and a refusal's thunk (optional).</summary>
    [SerializeField] private AudioSource sound;

    /// <summary>How far the bar slides out from the right edge (reference px: its width plus the stamps' overhang).</summary>
    [SerializeField] private float barTravel = 420f;

    /// <summary>The physics layers a stamp's ray meets documents on (the Interactable layer).</summary>
    [SerializeField] private LayerMask paperLayers = ~0;

    /// <summary>A press's dip: the stamp shrinks to this share of its size at the bottom of the press.</summary>
    private const float DipScale = 0.9f;

    /// <summary>How far the stamp shakes on a refused press (reference px).</summary>
    private const float Shake = 10f;

    /// <summary>The longest ray a stamp casts (metres).</summary>
    private const float RayLength = 50f;

    private readonly StampFlow _flow = new StampFlow();
    /// <summary>The ray's hits (room for every click box on the desk: the rulebook's rows and tabs, the props, the mat, the papers).</summary>
    private readonly RaycastHit[] _hits = new RaycastHit[128];
    private Camera _camera;
    private int _passport = -1;
    private bool _live;
    private float _slide;
    private float _barIn;
    private RectTransform _pressed;
    private Vector2 _pressedHome;
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
        if (bar != null)
            _barIn = bar.anchoredPosition.x;
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

    /// <summary>The office camera a stamp's ray is cast through (the office binder's, from the art office).</summary>
    public void Bind(Camera office) => _camera = office;

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
    /// A stamp clicked: it presses on what lies under its die (the office
    /// camera's ray through the die's centre meets a document first, or
    /// nothing): StampFlow decides; an accepted press prints the mark where
    /// it landed in the visa box and thumps; a refused one thunks, shakes the
    /// stamp and says why; a press on the bare desk thumps and says where to
    /// put the passport.
    /// </summary>
    public void Press(DeskStamp stamp)
    {
        Deselect();
        if (!_live || !_flow.BarOut)
            return;
        RectTransform die = stamp == DeskStamp.Approved ? approvedDie : deniedDie;
        DeskDocument paper = PaperUnder(die, out Vector3 point);
        bool onPassport = paper != null && paper.Index == _passport;
        LastPress = _flow.Press(stamp, paper != null, onPassport, onPassport && paper.InVisaBox(point));
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

    /// <summary>The first document the office camera's ray through <paramref name="die"/>'s centre meets (the top of a pile), and where; null when it meets none.</summary>
    private DeskDocument PaperUnder(RectTransform die, out Vector3 point)
    {
        point = default;
        if (die == null || _camera == null)
            return null;
        Canvas canvas = die.GetComponentInParent<Canvas>();
        Camera ui = canvas != null && canvas.rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.rootCanvas.worldCamera : null;
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(ui, die.TransformPoint(die.rect.center));
        Physics.SyncTransforms(); // a document dragged this frame is where the ray looks for it
        int n = Physics.RaycastNonAlloc(_camera.ScreenPointToRay(screen), _hits, RayLength, paperLayers, QueryTriggerInteraction.Collide);
        DeskDocument best = null;
        float nearest = float.MaxValue;
        for (int i = 0; i < n; i++)
        {
            DeskDocument paper = _hits[i].collider.GetComponentInParent<DeskDocument>();
            if (paper == null || _hits[i].distance >= nearest)
                continue;
            nearest = _hits[i].distance;
            best = paper;
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
            _pressed.localScale = Vector3.one * (_pressRefused ? 1f : Mathf.Lerp(1f, DipScale, wave));
            _pressed.anchoredPosition = _pressedHome + (_pressRefused ? new Vector2(Mathf.Sin(t * Mathf.PI * 4f) * Shake * (1f - t), 0f) : Vector2.zero);
            if (t >= 1f)
            {
                _pressed.localScale = Vector3.one;
                _pressed.anchoredPosition = _pressedHome;
                _pressElapsed = -1f;
            }
        }
        if (_noteKey != null && Time.unscaledTime >= _noteUntil)
        {
            _noteKey = null;
            Show();
        }
    }

    /// <summary>Eases the bar toward out or in (a cut under Reduced Motion).</summary>
    private void SlideBar()
    {
        if (bar == null)
            return;
        float target = _flow.BarOut ? 1f : 0f;
        if (Mathf.Approximately(_slide, target))
            return;
        float seconds = config != null && !MotionPreference.Reduced ? config.stampBarSeconds : 0f;
        _slide = seconds > 0f ? Mathf.MoveTowards(_slide, target, Time.unscaledDeltaTime / seconds) : target;
        bar.anchoredPosition = new Vector2(_barIn - barTravel * DeskZones.Ease(_slide), bar.anchoredPosition.y);
    }

    /// <summary>Starts a stamp's dip (or its shake, <paramref name="refused"/>).</summary>
    private void Dip(Button stamp, bool refused)
    {
        if (stamp == null)
            return;
        if (_pressed != null && _pressElapsed >= 0f)
        {
            _pressed.localScale = Vector3.one;
            _pressed.anchoredPosition = _pressedHome;
        }
        _pressed = (RectTransform)stamp.transform;
        _pressedHome = _pressed.anchoredPosition;
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
        foreach (Button stamp in new[] { approvedStamp, deniedStamp })
            if (stamp != null)
                stamp.interactable = _live;
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
