using System;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// The reading view (piece 10 section 11, T1-T4; Papers, Please's desk;
/// Saleh 2026-10-20, D1: "we are doing the camera tilt no need to enlarge the
/// documents when you click them"): a document dropped on the desk tilts the
/// camera forward over it (TiltIn: DeskController), into one fixed reading
/// pose per aspect, never re-aimed per paper; a click on the mat tilts it in
/// and back; the "▲ Back" control at the top of the office overlay (shown
/// while tilted, fading in over the last third of the move: BUG-32b) and the
/// mouse wheel rolled up return, and the wheel rolled down over the empty mat
/// tilts in; a right-click and Esc return as the last thing they back out of
/// (ControlRules.BackOut, OfficeControls). BoothCoordinator returns it when
/// the next traveller is called, a newsletter shows, or the PC frame opens,
/// and says when the mat, the Back control and the wheel are live
/// (BoothRules). Nothing changes the view while the player holds something
/// (HoldRegistry.Desk; BUG-55c), and the Back control hides while they do
/// (BUG-59).
/// <para>
/// The view is a gameplay-owned Cinemachine camera, posed at bind
/// (OfficeSceneBinder) and again whenever the screen's aspect changes: on
/// the desk home map, LensFit.Reading backs it out at DeskConfigSO.deskView's
/// pitch and lens until the big mat, the inner strips of the homes beside it
/// and the deal tray fit inside the screen's margins, and under the desk is
/// never shown (BUG-41, 1b, 7b; Saleh's "keep a big mat size": the papers
/// show about two thirds as big as before); without the map, the knobs'
/// fallback pose over the mat's centre. It is raised above the art camera's
/// priority while on, so the art camera's brain blends. The blend's time and
/// style come from the one CinemachineCore.GetBlendOverride hook (installed
/// once, statically, answering for the live reading view only, so no day's
/// dead view stays chained in it: BUG-73e), a cut under Reduced Motion read
/// at each blend (BUG-64b). Every move is tagged with its cause for the QA
/// camera logger (QaSignals.TagCamera: the D1 drop tilt or a view key). The
/// art camera is never moved. Unbound (no art Cinemachine camera), the mat
/// never shows.
/// </para>
/// </summary>
public sealed class DeskView : MonoBehaviour
{
    /// <summary>The desk tuning (the reading view's knobs, the desk's top).</summary>
    [SerializeField] private DeskConfigSO config;

    /// <summary>The reading view's camera (inactive until bound; priority 0 while the view is off).</summary>
    [SerializeField] private CinemachineCamera deskCamera;

    /// <summary>The mat's click box (the desk's clamp area just under its plane, laid by the office binder; inactive while the toggle is not live).</summary>
    [SerializeField] private ClickCatcher mat;

    /// <summary>The "▲ Back" control on the office overlay (optional; inactive until the Back control is live): a click returns.</summary>
    [SerializeField] private Button backButton;

    /// <summary>The desk camera sits this far above the art office's camera while on (the binder raises that one first; audit R5-015).</summary>
    private const int PriorityAboveOffice = 1;

    /// <summary>The desk camera's priority while off: under the art office's.</summary>
    private const int IdlePriority = 0;

    /// <summary>The live reading view the static blend hook answers for (null: none; a destroyed day's view is never asked).</summary>
    private static DeskView _hookOwner;

    /// <summary>The blend hook that was there before ours (another system's; none today), asked for every other blend.</summary>
    private static CinemachineCore.GetBlendOverrideDelegate _before;

    private CinemachineCamera _office;
    private int _onPriority;
    private Vector3 _matCentre;
    private float _posedAspect = -1f;
    private float _readingFov;
    private bool _toggleLive;
    private bool _backLive;
    private int _backLiveSince;
    private bool _scrollInLive;
    private int _scrollInLiveSince;
    private float _movedAt;
    private float _moveSeconds;
    private CanvasGroup _backGroup;
    private readonly List<RaycastResult> _hits = new List<RaycastResult>();

    /// <summary>True while the camera is (or is blending) over the desk.</summary>
    public bool IsOn { get; private set; }

    /// <summary>Why the view last turned on or off (the D1 drop tilt or a view key): the push-in that follows is tagged with it (CameraFeel).</summary>
    public CameraMoveCause LastCause { get; private set; } = CameraMoveCause.ViewKey;

    /// <summary>The office view's level forward on the desk (Bind; +z before): the papers lie upright along it, toward the chair.</summary>
    public Vector3 LevelForward { get; private set; } = Vector3.forward;

    /// <summary>True once posed from the art office's Cinemachine camera (Bind): the view can tilt, so the papers on the desk move only while it is on (BoothRules).</summary>
    public bool IsBound => _office != null;

    /// <summary>The reading view's field of view at rest (degrees): the solved pose's lens, once the push-in has settled (CameraFeel widens it by the push-in while it moves).</summary>
    public float ReadingFov => _readingFov;

    /// <summary>The office view's controls' opacity (BUG-32b): 0 while the reading view is on, fading in over the last third of the move back (CityView's pull tabs).</summary>
    public float OfficeControlsAlpha => IsOn ? 0f : DeskViewPose.ControlAlpha(Time.unscaledTime - _movedAt, _moveSeconds);

    /// <summary>The "▲ Back" control (the guide points at it for a thing only the office view shows), or null.</summary>
    public RectTransform BackControl => backButton != null ? (RectTransform)backButton.transform : null;

    /// <summary>Raised after the view turns on or off.</summary>
    public event Action Changed;

    /// <summary>Forgets the static hook's state when the game starts without a domain reload (the hook itself stays installed and answers for no view).</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetHook()
    {
        _hookOwner = null;
        _before = null;
    }

    private void Awake()
    {
        if (mat != null)
        {
            mat.onClick.AddListener(Toggle);
            mat.gameObject.SetActive(false);
        }
        if (backButton != null)
        {
            backButton.onClick.AddListener(Back);
            if (!backButton.TryGetComponent(out _backGroup))
                _backGroup = backButton.gameObject.AddComponent<CanvasGroup>();
            backButton.gameObject.SetActive(false);
        }
    }

    /// <summary>A destroyed view stops being the one the blend hook answers for.</summary>
    private void OnDestroy()
    {
        if (_hookOwner == this)
            _hookOwner = null;
    }

    /// <summary>
    /// Poses the reading camera from the art office's camera
    /// <paramref name="office"/> and the mat's centre (the office binder, once
    /// the art office is bound and the desk home map laid): the solved pose at
    /// the screen's aspect (or the knobs' fallback), the lens copied with the
    /// solved field of view. The camera then waits at priority 0; the push-in,
    /// breathing and impulses attach (CameraFeel).
    /// </summary>
    public void Bind(CinemachineCamera office, Vector3 matCentre)
    {
        if (office == null || deskCamera == null || config == null)
            return;

        _office = office;
        _matCentre = matCentre;
        Vector3 level = Vector3.ProjectOnPlane(office.transform.forward, Vector3.up);
        if (level.sqrMagnitude < 1e-6f)
            level = Vector3.ProjectOnPlane(office.transform.up, Vector3.up);
        LevelForward = level.normalized;
        Pose();
        _onPriority = office.Priority.Value + PriorityAboveOffice;
        deskCamera.Priority = IsOn ? _onPriority : IdlePriority;
        deskCamera.gameObject.SetActive(true);
        Hook(this);
        CameraFeel.Attach(this, office, deskCamera); // the cameras' breathing, impulses and the reading push-in
    }

    /// <summary>
    /// The reading pose at the screen's aspect: on the desk home map
    /// (DeskLayout bound), LensFit.Reading framing DeskViewPose.Targets in the
    /// desk's frame, its bottom row kept on the desk's top
    /// (DeskLayoutSettings.deskTop); otherwise the knobs' fallback over the
    /// mat's centre. The lens is the office's with the knobs' field of view.
    /// </summary>
    private void Pose()
    {
        DeskViewTuning tuning = config.deskView;
        float aspect = Screen.height > 0 ? (float)Screen.width / Screen.height : LensFit.DesignAspect;
        _posedAspect = aspect;
        LensSettings lens = _office.Lens;
        if (tuning.fieldOfView > 0f)
            lens.FieldOfView = tuning.fieldOfView;
        _readingFov = lens.FieldOfView;
        float pitch = DeskViewPose.Pitch(tuning);
        Quaternion look = Quaternion.LookRotation(LevelForward, Vector3.up) * Quaternion.Euler(pitch, 0f, 0f);

        if (DeskLayout.Bound && config.layout != null &&
            LensFit.Reading(DeskViewPose.Targets(tuning, DeskLayout.Mat.Rect, config.layout.homes), pitch, _readingFov, aspect,
                            tuning.topMargin, tuning.bottomMargin, tuning.sideMargin, FrontEdge(config.layout.deskTop.Rect), out ReadingPose pose))
        {
            DeskFrame frame = DeskLayout.Frame;
            float p = pitch * Mathf.Deg2Rad;
            Vector3 aim = frame.Point(new Vector2(pose.AimX, pose.AimY));
            deskCamera.transform.SetPositionAndRotation(aim - frame.Forward * (pose.Distance * Mathf.Cos(p)) + Vector3.up * (pose.Distance * Mathf.Sin(p)),
                                                        Quaternion.LookRotation(frame.Forward, Vector3.up) * Quaternion.Euler(pitch, 0f, 0f));
        }
        else
        {
            Vector3 right = Vector3.Cross(Vector3.up, LevelForward);
            Vector3 aim = _matCentre + right * tuning.aimRight + LevelForward * tuning.aimForward;
            (float back, float up) = DeskViewPose.Offset(tuning);
            deskCamera.transform.SetPositionAndRotation(aim - LevelForward * back + Vector3.up * up, look);
        }
        deskCamera.Lens = lens;
    }

    /// <summary>The desk's front edge (the side by the chair) in the desk's frame.</summary>
    private static float FrontEdge(DeskRect deskTop) => deskTop.CentreY - deskTop.Height / 2f;

    /// <summary>Where the reading view's ray through <paramref name="viewport"/> (0..1 each, from the bottom left) meets the level plane at <paramref name="height"/>: what of the desk the view shows there at rest (false while unbound, or for a ray that never comes down to it).</summary>
    public bool TryViewPoint(Vector2 viewport, float height, out Vector3 point)
    {
        point = default;
        if (_office == null || deskCamera == null)
            return false;
        Transform t = deskCamera.transform;
        float tan = Mathf.Tan(_readingFov * 0.5f * Mathf.Deg2Rad);
        float aspect = Screen.height > 0 ? (float)Screen.width / Screen.height : 16f / 9f;
        Vector3 ray = t.forward + t.up * ((viewport.y * 2f - 1f) * tan) + t.right * ((viewport.x * 2f - 1f) * tan * aspect);
        if (ray.y > -1e-5f)
            return false;
        float along = (height - t.position.y) / ray.y;
        point = t.position + ray * along;
        return along > 0f;
    }

    /// <summary>The mat's click: tilts into the reading view, or back (only while the toggle is live).</summary>
    public void Toggle()
    {
        if (_toggleLive)
            Set(!IsOn, CameraMoveCause.ViewKey);
    }

    /// <summary>Tilts into the reading view for a document dropped on the desk (D1: the drop tilt; no-op if it is on, or while the desk takes no input).</summary>
    public void TiltIn() => TiltIn(CameraMoveCause.DeskDropTilt);

    /// <summary>Tilts into the reading view for <paramref name="cause"/> (the PC's "&lt; Desk" button: a view key; no-op if it is on, or while the desk takes no input).</summary>
    public void TiltIn(CameraMoveCause cause)
    {
        if (_toggleLive)
            Set(true, cause);
    }

    /// <summary>Returns to the office view (no-op if it is off): a right-click or Esc with nothing else to back out of, the next traveller called, the PC opened, a newsletter: each a view key's consequence.</summary>
    public void Return() => Set(false, CameraMoveCause.ViewKey);

    /// <summary>Shows the mat's click box (a click toggles) and lets the view tilt in, or not (BoothCoordinator: BoothRules.DeskViewAllowed); never while unbound.</summary>
    public void SetToggleLive(bool live)
    {
        _toggleLive = live && _office != null;
        if (mat != null && mat.gameObject.activeSelf != _toggleLive)
            mat.gameObject.SetActive(_toggleLive);
    }

    /// <summary>Shows the "▲ Back" control and lets the wheel rolled up return, from the next frame on, or hides it (BoothCoordinator: BoothRules.DeskViewBackLive).</summary>
    public void SetBackLive(bool live)
    {
        if (live && !_backLive)
            _backLiveSince = Time.frameCount;
        _backLive = live;
        if (backButton != null && backButton.gameObject.activeSelf != live)
        {
            backButton.gameObject.SetActive(live);
            ShowBack();
        }
    }

    /// <summary>Lets the wheel rolled down over the empty mat tilt in, from the next frame on (BoothCoordinator: BoothRules.NormalViewLive); never while unbound.</summary>
    public void SetScrollInLive(bool live)
    {
        live &= _office != null;
        if (live && !_scrollInLive)
            _scrollInLiveSince = Time.frameCount;
        _scrollInLive = live;
    }

    /// <summary>
    /// Re-poses the camera when the screen's aspect changed. While on: the
    /// wheel rolled up returns while the Back control is live. While off: the
    /// wheel rolled down with the pointer on the empty mat (nothing above it
    /// takes the pointer, no UI) tilts in. (A right-click and Esc are the one
    /// input model's: OfficeControls.) The Back control fades in over the
    /// move's last third and hides while something is held.
    /// </summary>
    private void Update()
    {
        if (_office != null && Screen.height > 0 && !Mathf.Approximately((float)Screen.width / Screen.height, _posedAspect))
        {
            Pose();
            CameraFeel.Attach(this, _office, deskCamera);
        }

        Mouse mouse = Mouse.current;
        float scroll = mouse != null ? mouse.scroll.ReadValue().y : 0f;
        if (IsOn)
        {
            if (_backLive && _backLiveSince < Time.frameCount && scroll > 0f)
                Return();
        }
        else if (_scrollInLive && _scrollInLiveSince < Time.frameCount && scroll < 0f && mouse != null && OnMat(mouse.position.ReadValue()))
        {
            Set(true, CameraMoveCause.ViewKey);
        }
        ShowBack();
    }

    /// <summary>The Back control's opacity: in only over the last third of the move into the view (DeskViewPose.ControlAlpha), none while something is held; it takes clicks once it shows.</summary>
    private void ShowBack()
    {
        if (_backGroup == null || !backButton.gameObject.activeSelf)
            return;
        float alpha = IsOn && !HoldRegistry.Desk.IsHolding ? DeskViewPose.ControlAlpha(Time.unscaledTime - _movedAt, _moveSeconds) : 0f;
        if (_backGroup.alpha != alpha)
        {
            _backGroup.alpha = alpha;
            _backGroup.blocksRaycasts = _backGroup.interactable = alpha > 0f;
        }
    }

    /// <summary>The Back control's click (only while it is live).</summary>
    private void Back()
    {
        if (_backLive)
            Return();
    }

    /// <summary>True when the mat is the first thing under the screen point (so no UI and no paper or prop covers it there).</summary>
    private bool OnMat(Vector2 screen)
    {
        GameObject handler = TopHandler(screen, out bool any);
        return any && mat != null && mat.gameObject.activeInHierarchy && handler == mat.gameObject;
    }

    /// <summary>The click handler of the first hit under a screen point (<paramref name="any"/>: something was hit).</summary>
    private GameObject TopHandler(Vector2 screen, out bool any)
    {
        any = false;
        EventSystem events = EventSystem.current;
        if (events == null)
            return null;

        _hits.Clear();
        events.RaycastAll(new PointerEventData(events) { position = screen }, _hits);
        if (_hits.Count == 0)
            return null;
        any = true;
        return ExecuteEvents.GetEventHandler<IPointerClickHandler>(_hits[0].gameObject);
    }

    /// <summary>Turns the view on or off for <paramref name="cause"/>: never while unbound, and never while the player holds something (BUG-55c); the move is tagged for the QA camera logger over the blend's seconds.</summary>
    private void Set(bool on, CameraMoveCause cause)
    {
        if (on == IsOn || (on && _office == null) || HoldRegistry.Desk.IsHolding)
            return;

        IsOn = on;
        LastCause = cause;
        _movedAt = Time.unscaledTime;
        _moveSeconds = DeskViewPose.Seconds(config.deskView, MotionPreference.Reduced);
        QaSignals.TagCamera(cause, _moveSeconds);
        Hook(this);
        deskCamera.Priority = on ? _onPriority : IdlePriority;
        ShowBack();
        Changed?.Invoke();
    }

    /// <summary>Makes <paramref name="owner"/> the view the blend hook answers for, installing the static hook once (a hook that was there before it is kept and asked for every other blend).</summary>
    private static void Hook(DeskView owner)
    {
        _hookOwner = owner;
        CinemachineCore.GetBlendOverrideDelegate ours = OwnersBlend;
        CinemachineCore.GetBlendOverrideDelegate current = CinemachineCore.GetBlendOverride;
        if (current == ours)
            return;
        _before = current;
        CinemachineCore.GetBlendOverride = ours;
    }

    /// <summary>The blend to or from the live reading view's camera: the knob's seconds, eased (a cut under Reduced Motion, read now); any other blend is left to the hook before ours, or the brain's own.</summary>
    private static CinemachineBlendDefinition OwnersBlend(ICinemachineCamera from, ICinemachineCamera to, CinemachineBlendDefinition fallback, UnityEngine.Object owner)
    {
        DeskView view = _hookOwner;
        if (view == null || view.deskCamera == null || view.config == null || (!ReferenceEquals(from, view.deskCamera) && !ReferenceEquals(to, view.deskCamera)))
            return _before != null ? _before(from, to, fallback, owner) : fallback;

        float seconds = DeskViewPose.Seconds(view.config.deskView, MotionPreference.Reduced);
        return seconds > 0f
            ? new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, seconds)
            : new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0f);
    }
}
