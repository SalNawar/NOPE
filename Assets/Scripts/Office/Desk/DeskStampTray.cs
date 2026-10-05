using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// The physical stamps (the desk-first redesign, Saleh 2026-10-05, item 12:
/// "approve or reject are actual physical seals: the player picks stamps,
/// inks them, then stamps on the document"). A click on the desk stamp slides
/// the stamp tray out of the desk's near edge (again: back in): on it lie the
/// APPROVED stamp (green), the DENIED stamp (red) and the ink pad. A click on
/// a stamp picks it up (the view tilts down over the desk) and it follows the
/// pointer over the desk; a click on the ink pad inks it
/// (DeskConfigSO.stampPressesPerInking presses); a click on a paper lying on
/// the desk presses it there (DeskController routes the click: Press): an
/// inked press prints the stamp's mark where it was pressed
/// (DeskDocument.Stamp), and on the passport (the traveller's first paper)
/// that is the verdict; a dry press leaves a faint mark and counts for
/// nothing. A click on the held stamp's place on the tray, Escape or a
/// right-click puts it down. Once the passport carries a verdict, the papers
/// are handed back the physical way (the desk-first polish, 2026-10-05): the
/// stamped passport slid onto the traveller's side of the desk, where a strip
/// marked "HAND BACK" shows while it can (OnTravellersSide; DeskController
/// routes the drop), with "Hand the papers back" over the office as the
/// secondary way; either decides the case (Decided), as the PC's Accept and
/// Deny do. The tray, out, takes the room it lies on: DeskController moves
/// the papers off its footprint (TryFootprint). The rules are StampFlow's;
/// the stamps take clicks while BoothRules.StampsLive (false puts a held
/// stamp down). The office binder places the tray; Build Office UI builds its
/// parts (the art's desk stamp and ink pad models, ART_ASSET_LIST "The
/// desk"), the hand-back strip and the overlay's button and hint.
/// </summary>
public sealed class DeskStampTray : MonoBehaviour
{
    /// <summary>The desk tuning (the stamps' knobs).</summary>
    [SerializeField] private DeskConfigSO config;

    /// <summary>The desk plane (the held stamp follows the pointer on it).</summary>
    [SerializeField] private DeskSurface surface;

    /// <summary>The desk view (optional): a stamp picked up tilts the view down over the desk.</summary>
    [SerializeField] private DeskView deskView;

    /// <summary>The sliding tray (the stamps and the pad ride on it).</summary>
    [SerializeField] private Transform tray;

    /// <summary>The APPROVED stamp's click (its object is the stamp itself; it leaves the tray while held).</summary>
    [SerializeField] private Clickable approvedStamp;

    /// <summary>The DENIED stamp's click.</summary>
    [SerializeField] private Clickable deniedStamp;

    /// <summary>The ink pad's click.</summary>
    [SerializeField] private Clickable inkPad;

    /// <summary>The ink on a stamp's face, shown while it is inked (optional; one per stamp: APPROVED, DENIED).</summary>
    [SerializeField] private GameObject[] inkedFaces = new GameObject[2];

    /// <summary>"Hand the papers back" with APPROVED on the passport (its tick; shown once the passport carries that verdict).</summary>
    [SerializeField] private Button handBackApproved;

    /// <summary>"Hand the papers back" with DENIED on the passport (its cross).</summary>
    [SerializeField] private Button handBackDenied;

    /// <summary>The step's hint over the office (pick a stamp, ink it, stamp the passport, slide it back); optional.</summary>
    [SerializeField] private TMP_Text hint;

    /// <summary>The strip on the traveller's side of the desk, shown while the stamped passport can be slid there to hand the papers back (laid at Bind; optional).</summary>
    [SerializeField] private GameObject handBackZone;

    /// <summary>The strip's quad (sized at Bind to the traveller's side: the desk's width by DeskConfigSO.handBackDepth).</summary>
    [SerializeField] private Transform handBackStrip;

    /// <summary>The tray's footprint on the desk while out (metres: across, deep), kept clear of papers.</summary>
    [SerializeField] private Vector2 footprint = new Vector2(0.36f, 0.17f);

    /// <summary>How far above the desk the hand-back strip lies (metres), over the desk's own top and under the papers.</summary>
    private const float ZoneLift = 0.0004f;

    /// <summary>The highest the traveller's side reaches on the screen in the desk view (a share of its height from the bottom): a passport can be slid there, under the overlay's top controls.</summary>
    private const float ViewTop = 0.9f;

    private StampFlow _flow;
    private Camera _camera;
    private Vector3 _trayOut;
    private Vector3 _trayIn;
    private float _slide;
    private Vector3 _approvedHome;
    private Vector3 _deniedHome;
    private int _passport = -1;
    private bool _live;
    private bool _faintPressed;
    private int _changedFrame;
    private float _pressElapsed = -1f;
    private Vector3 _pressAt;
    private Vector3 _forward = Vector3.forward;
    private Vector3 _right = Vector3.right;

    /// <summary>True while a stamp is in the hand (BoothRules.StampHeld).</summary>
    public bool IsHolding => Flow.Held != DeskStamp.None;

    /// <summary>True while the passport carries a verdict (the decision skips the leaving papers' own verdict ink: the player's is on them).</summary>
    public bool HasVerdict => Flow.CanHandBack;

    /// <summary>The frame a held stamp was last put down or picked up (one Escape or right-click does one thing).</summary>
    public int ChangedFrame => _changedFrame;

    /// <summary>True while the tray is out on the desk (or sliding out).</summary>
    public bool TrayOut => Flow.TrayOut;

    /// <summary>Raised when the tray, the hand or the verdict changes (the booth re-applies its rules).</summary>
    public event Action Changed;

    /// <summary>Raised when the papers are handed back with the passport's verdict: true for APPROVED.</summary>
    public event Action<bool> Decided;

    private StampFlow Flow => _flow ??= new StampFlow(config != null ? config.stampPressesPerInking : 1);

    private void Awake()
    {
        if (approvedStamp != null)
        {
            _approvedHome = approvedStamp.transform.localPosition;
            approvedStamp.onClick.AddListener(() => Pick(DeskStamp.Approved));
        }
        if (deniedStamp != null)
        {
            _deniedHome = deniedStamp.transform.localPosition;
            deniedStamp.onClick.AddListener(() => Pick(DeskStamp.Denied));
        }
        if (inkPad != null)
            inkPad.onClick.AddListener(Ink);
        if (handBackApproved != null)
            handBackApproved.onClick.AddListener(HandBack);
        if (handBackDenied != null)
            handBackDenied.onClick.AddListener(HandBack);
        if (tray != null)
            tray.gameObject.SetActive(false);
        Show();
    }

    /// <summary>
    /// Places the tray (the office binder): out at <paramref name="outPoint"/>
    /// on the desk, facing along <paramref name="levelForward"/> (the office
    /// view's), and in under the desk's near edge (DeskConfigSO.stampTraySlide
    /// toward the chair and below the top); the office camera projects the
    /// pointer; the hand-back strip lies on the traveller's side of the desk.
    /// </summary>
    public void Bind(Camera office, Vector3 outPoint, Vector3 levelForward)
    {
        _camera = office;
        Vector3 forward = Vector3.ProjectOnPlane(levelForward, Vector3.up);
        if (forward.sqrMagnitude < 1e-6f)
            forward = Vector3.forward;
        forward.Normalize();
        _forward = forward;
        _right = Vector3.Cross(Vector3.up, forward);
        PlaceHandBackZone();
        if (tray == null || config == null)
            return;
        _trayOut = outPoint;
        _trayIn = outPoint - forward * config.stampTraySlide - Vector3.up * config.stampTrayDrop;
        tray.SetPositionAndRotation(Vector3.Lerp(_trayIn, _trayOut, _slide), Quaternion.LookRotation(forward, Vector3.up));
    }

    /// <summary>The desk stamp's click: slides the tray out, or back in (a held stamp goes back first).</summary>
    public void ToggleTray()
    {
        if (!_live && !Flow.TrayOut)
            return;
        if (Flow.TrayOut)
        {
            ReturnStamps();
            Flow.CloseTray();
        }
        else
        {
            Flow.OpenTray();
            if (tray != null)
                tray.gameObject.SetActive(true);
        }
        Raise();
    }

    /// <summary>Lets the stamps and the pad take clicks (BoothRules.StampsLive) or not: not puts a held stamp down.</summary>
    public void SetLive(bool live)
    {
        _live = live;
        foreach (Clickable c in new[] { approvedStamp, deniedStamp, inkPad })
            if (c != null)
                c.Interactable = live;
        if (!live && IsHolding)
            PutDown();
        Show();
    }

    /// <summary>A new traveller (DeskController): <paramref name="passport"/> is the paper whose inked stamp is the verdict (the first paper handed over; -1: none). Nothing is held, no verdict yet.</summary>
    public void BeginCase(int passport)
    {
        _passport = passport;
        _faintPressed = false;
        ReturnStamps();
        Flow.BeginCase();
        Raise();
    }

    /// <summary>The decision (DeskController): no passport, nothing held, no verdict.</summary>
    public void EndCase() => BeginCase(-1);

    /// <summary>True when paper <paramref name="index"/> is the passport and it carries a verdict (dropped on the traveller's side, the papers go back).</summary>
    public bool CanHandBack(int index) => index == _passport && Flow.CanHandBack;

    /// <summary>True when <paramref name="point"/> lies in the strip of the desk's clamp area at its far edge along the office view (DeskConfigSO.handBackDepth): the traveller's side, where the stamped passport hands the papers back.</summary>
    public bool OnTravellersSide(Vector3 point) =>
        TravellersSide(out float far, out _, out _, out _) && Vector3.Dot(point - surface.transform.position, _forward) >= far - config.handBackDepth;

    /// <summary>The tray's footprint on the desk where it lies out (its four corners), while it is out; false while in.</summary>
    public bool TryFootprint(out Vector3[] corners)
    {
        corners = null;
        if (!Flow.TrayOut || tray == null)
            return false;
        Vector3 x = _right * footprint.x / 2f, z = _forward * footprint.y / 2f;
        corners = new[] { _trayOut - x - z, _trayOut + x - z, _trayOut - x + z, _trayOut + x + z };
        return true;
    }

    /// <summary>
    /// Presses the held stamp on <paramref name="paper"/> at <paramref name="world"/>
    /// (DeskController, a click on a paper lying on the desk): the mark the
    /// press leaves (StampFlow.Press: inked or faint) prints there, and the
    /// stamp dips onto the paper.
    /// </summary>
    public void Press(DeskDocument paper, Vector3 world)
    {
        if (paper == null || !IsHolding)
            return;
        DeskStamp held = Flow.Held;
        StampMark mark = Flow.Press(paper.Index == _passport);
        if (mark == StampMark.None)
            return;
        paper.Stamp(held == DeskStamp.Approved, paper.PagePoint(world), mark == StampMark.Faint);
        _faintPressed = mark == StampMark.Faint;
        _pressAt = world;
        _pressElapsed = 0f;
        Raise();
    }

    /// <summary>The papers handed back with the passport's verdict (the button, or the passport slid onto the traveller's side): Decided (nothing without a verdict).</summary>
    public void HandBack()
    {
        if (!Flow.CanHandBack)
            return;
        bool approved = Flow.Verdict == DeskStamp.Approved;
        ReturnStamps();
        Decided?.Invoke(approved);
    }

    /// <summary>Slides the tray, carries the held stamp under the pointer (dipping it for a press), and puts it down on Escape or a right-click.</summary>
    private void Update()
    {
        SlideTray();
        if (!IsHolding)
            return;

        Keyboard kb = Keyboard.current;
        Mouse mouse = Mouse.current;
        if (_changedFrame < Time.frameCount &&
            ((kb != null && kb.escapeKey.wasPressedThisFrame) || (mouse != null && mouse.rightButton.wasPressedThisFrame)))
        {
            PutDown();
            return;
        }

        Transform stamp = HeldStamp();
        if (stamp == null || surface == null || _camera == null || mouse == null)
            return;
        float lift = config != null ? config.stampLift : 0.06f;
        float seconds = config != null ? config.stampPressSeconds : 0.18f;
        Vector3 at;
        if (_pressElapsed >= 0f)
        {
            _pressElapsed += Time.deltaTime;
            float t = seconds > 0f ? Mathf.Clamp01(_pressElapsed / seconds) : 1f;
            at = _pressAt + Vector3.up * (lift * Mathf.Abs(1f - 2f * t));
            if (t >= 1f)
                _pressElapsed = -1f;
        }
        else if (surface.TryProject(_camera, mouse.position.ReadValue(), out Vector3 point))
        {
            at = surface.Clamp(point) + Vector3.up * lift;
        }
        else
        {
            return;
        }
        stamp.position = at;
    }

    /// <summary>Eases the tray toward out or in (a cut under Reduced Motion); hidden once fully in.</summary>
    private void SlideTray()
    {
        if (tray == null)
            return;
        float target = Flow.TrayOut ? 1f : 0f;
        if (Mathf.Approximately(_slide, target))
            return;
        float seconds = config != null && !MotionPreference.Reduced ? config.stampTraySeconds : 0f;
        _slide = seconds > 0f ? Mathf.MoveTowards(_slide, target, Time.deltaTime / seconds) : target;
        tray.position = Vector3.Lerp(_trayIn, _trayOut, ExamineLayout.Ease(_slide));
        if (_slide <= 0f && !Flow.TrayOut)
            tray.gameObject.SetActive(false);
    }

    /// <summary>A stamp clicked on the tray: picked up (the view tilts down over the desk), or put down when it is the one held.</summary>
    private void Pick(DeskStamp stamp)
    {
        if (!_live || !Flow.TrayOut)
            return;
        if (Flow.Held == stamp)
        {
            PutDown();
            return;
        }
        ReturnStamps();
        Flow.PickUp(stamp);
        Clickable held = stamp == DeskStamp.Approved ? approvedStamp : deniedStamp;
        if (held != null && held.TryGetComponent(out Collider box))
            box.enabled = false;
        if (deskView != null)
            deskView.TiltInNow();
        _faintPressed = false;
        Raise();
    }

    /// <summary>The ink pad clicked: the held stamp is inked (it dips onto the pad).</summary>
    private void Ink()
    {
        if (!_live || !Flow.Ink())
            return;
        _faintPressed = false;
        if (inkPad != null)
        {
            _pressAt = inkPad.transform.position;
            _pressElapsed = 0f;
        }
        Raise();
    }

    /// <summary>Puts the held stamp back on the tray.</summary>
    private void PutDown()
    {
        if (!Flow.PutDown())
            return;
        ReturnStamps();
        Raise();
    }

    /// <summary>Both stamps lie in their places on the tray, their click boxes on.</summary>
    private void ReturnStamps()
    {
        _pressElapsed = -1f;
        Home(approvedStamp, _approvedHome);
        Home(deniedStamp, _deniedHome);
    }

    private static void Home(Clickable stamp, Vector3 home)
    {
        if (stamp == null)
            return;
        stamp.transform.localPosition = home;
        if (stamp.TryGetComponent(out Collider box))
            box.enabled = true;
    }

    /// <summary>The far edge of the traveller's side along the office view (the desk's clamp area's far edge, or nearer: as far as the desk view shows, ViewTop, so the passport can be slid there in it), the desk's left and right ends and the strip's middle across (the desk view's centre line, else the desk's), in metres from the desk's centre; false without a desk.</summary>
    private bool TravellersSide(out float far, out float left, out float right, out float middle)
    {
        far = right = float.MinValue;
        left = float.MaxValue;
        middle = 0f;
        if (surface == null || config == null)
            return false;
        Vector3 origin = surface.transform.position;
        foreach (Vector3 corner in surface.Corners())
        {
            far = Mathf.Max(far, Vector3.Dot(corner - origin, _forward));
            left = Mathf.Min(left, Vector3.Dot(corner - origin, _right));
            right = Mathf.Max(right, Vector3.Dot(corner - origin, _right));
        }
        middle = (left + right) / 2f;
        if (deskView != null && deskView.TryViewPoint(new Vector2(0.5f, ViewTop), origin.y, out Vector3 shown))
        {
            far = Mathf.Min(far, Vector3.Dot(shown - origin, _forward));
            middle = Mathf.Clamp(Vector3.Dot(shown - origin, _right), left, right);
        }
        return true;
    }

    /// <summary>Lays the hand-back strip over the traveller's side (handBackDepth deep at its far edge, centred on the desk view's centre line so its label shows there, as wide as the desk allows either side of it), facing the chair.</summary>
    private void PlaceHandBackZone()
    {
        if (handBackZone == null || !TravellersSide(out float far, out float left, out float right, out float middle))
            return;
        float depth = config.handBackDepth;
        Vector3 centre = surface.transform.position + _right * middle + _forward * (far - depth / 2f) + Vector3.up * ZoneLift;
        handBackZone.transform.SetPositionAndRotation(centre, Quaternion.LookRotation(_forward, Vector3.up));
        if (handBackStrip != null)
            handBackStrip.localScale = new Vector3(2f * Mathf.Min(middle - left, right - middle), depth, 1f);
    }

    /// <summary>The held stamp's object, or null.</summary>
    private Transform HeldStamp() =>
        Flow.Held == DeskStamp.Approved && approvedStamp != null ? approvedStamp.transform
        : Flow.Held == DeskStamp.Denied && deniedStamp != null ? deniedStamp.transform
        : null;

    private static void Show(Button button, bool on)
    {
        if (button != null && button.gameObject.activeSelf != on)
            button.gameObject.SetActive(on);
    }

    /// <summary>The change is shown and announced.</summary>
    private void Raise()
    {
        _changedFrame = Time.frameCount;
        Show();
        Changed?.Invoke();
    }

    /// <summary>The inked faces, the hint for the next step and the hand-back button.</summary>
    private void Show()
    {
        if (inkedFaces != null)
            for (int i = 0; i < inkedFaces.Length; i++)
                if (inkedFaces[i] != null)
                    inkedFaces[i].SetActive(Flow.InkOf(i == 0 ? DeskStamp.Approved : DeskStamp.Denied) > 0);

        bool back = _live && Flow.CanHandBack && !IsHolding;
        Show(handBackApproved, back && Flow.Verdict == DeskStamp.Approved);
        Show(handBackDenied, back && Flow.Verdict == DeskStamp.Denied);
        if (handBackZone != null && handBackZone.activeSelf != back)
            handBackZone.SetActive(back);

        if (hint == null)
            return;
        string key = !_live || _passport < 0 ? null
            : back ? "stamp.hint.handBack"
            : !Flow.TrayOut ? null
            : !IsHolding ? (Flow.CanHandBack ? null : "stamp.hint.pick")
            : _faintPressed ? "stamp.hint.dry"
            : !Flow.HeldInked ? "stamp.hint.ink"
            : "stamp.hint.press";
        GameObject plate = hint.transform.parent != null ? hint.transform.parent.gameObject : hint.gameObject;
        if (plate.activeSelf != (key != null))
            plate.SetActive(key != null);
        if (key != null)
            hint.text = UiText.Get(key);
    }
}
