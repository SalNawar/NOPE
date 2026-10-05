using System;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// The desk view (piece 10 section 11, T1-T4): a click on the mat tilts the
/// camera forward over the desk, and the same click tilts it back; Escape and
/// a right-click on empty space (nothing under the pointer takes clicks, or the
/// mat does) also return; so do the "▲ Back" control at the top of the office
/// overlay (shown while tilted) and the mouse wheel rolled up, and the wheel
/// rolled down over the empty mat tilts in (the readability fix: a visible way
/// out). BoothCoordinator returns it when the next traveller is called, a
/// newsletter shows, or the wheel or the PC frame opens (a click on the
/// intercom, the traveller or the PC from the tilted view blends straight up
/// there; Saleh 2026-09-30), and says when the mat, the returns, the Back
/// control and the wheel are live (BoothRules). The view is a gameplay-owned Cinemachine
/// camera, posed at bind (OfficeSceneBinder) from the art office's camera and
/// the mat's centre (DeskViewPose, DeskConfigSO.deskView), and raised above the
/// art camera's priority while on, so the art camera's brain blends. The
/// blend's time and style come from a CinemachineCore.GetBlendOverride hook
/// that answers only for blends to or from the desk camera (a cut under Reduced
/// Motion). The art camera is never moved. Unbound (no art Cinemachine camera),
/// the mat never shows.
/// </summary>
public sealed class DeskView : MonoBehaviour
{
    /// <summary>The desk tuning (the desk view's knobs).</summary>
    [SerializeField] private DeskConfigSO config;

    /// <summary>The desk view's camera (inactive until bound; priority 0 while the view is off).</summary>
    [SerializeField] private CinemachineCamera deskCamera;

    /// <summary>The mat's click box (the desk's clamp area just under its plane, laid by the office binder; inactive while the toggle is not live).</summary>
    [SerializeField] private ClickCatcher mat;

    /// <summary>The "▲ Back" control on the office overlay (optional; inactive until the Back control is live): a click returns.</summary>
    [SerializeField] private Button backButton;

    private CinemachineCamera _office;
    /// <summary>The desk camera sits this far above the art office's camera while on (the binder raises that one first; audit R5-015).</summary>
    private const int PriorityAboveOffice = 1;

    /// <summary>The desk camera's priority while off: under the art office's.</summary>
    private const int IdlePriority = 0;

    private int _onPriority;
    private bool _toggleLive;
    private bool _returnLive;
    private int _returnLiveSince;
    private bool _backLive;
    private int _backLiveSince;
    private bool _scrollInLive;
    private int _scrollInLiveSince;
    private CinemachineCore.GetBlendOverrideDelegate _blend;
    private CinemachineCore.GetBlendOverrideDelegate _previous;
    private readonly List<RaycastResult> _hits = new List<RaycastResult>();

    /// <summary>True while the camera is (or is blending) over the desk.</summary>
    public bool IsOn { get; private set; }

    /// <summary>True once posed from the art office's Cinemachine camera (Bind): the view can tilt, so the papers on the desk move only while it is on (BoothRules).</summary>
    public bool IsBound => _office != null;

    /// <summary>Raised after the view turns on or off.</summary>
    public event Action Changed;

    private void Awake()
    {
        _blend = Blend;
        if (mat != null)
        {
            mat.onClick.AddListener(Toggle);
            mat.gameObject.SetActive(false);
        }
        if (backButton != null)
        {
            backButton.onClick.AddListener(Back);
            backButton.gameObject.SetActive(false);
        }
    }

    /// <summary>Takes the blend hook out again if it is still ours.</summary>
    private void OnDestroy()
    {
        if (CinemachineCore.GetBlendOverride == _blend)
            CinemachineCore.GetBlendOverride = _previous;
    }

    /// <summary>
    /// Poses the desk camera from the art office's camera <paramref name="office"/>
    /// and the mat's centre (the office binder, once the art office is bound):
    /// looking down at the knob's pitch (80 degrees; Saleh 2026-10-05) onto
    /// the aim point (the mat's centre moved by the knobs along the view's
    /// level right and forward) from the knob's distance, its yaw kept
    /// (DeskViewPose); the lens copied, with the knob's field of view. The
    /// camera then waits at priority 0.
    /// </summary>
    public void Bind(CinemachineCamera office, Vector3 matCentre)
    {
        if (office == null || deskCamera == null || config == null)
            return;

        _office = office;
        Transform art = office.transform;
        Vector3 level = Vector3.ProjectOnPlane(art.forward, Vector3.up);
        if (level.sqrMagnitude < 1e-6f)
            level = Vector3.ProjectOnPlane(art.up, Vector3.up);
        level.Normalize();

        DeskViewTuning tuning = config.deskView;
        Vector3 right = Vector3.Cross(Vector3.up, level);
        Vector3 aim = matCentre + right * tuning.aimRight + level * tuning.aimForward;
        (float back, float up) = DeskViewPose.Offset(tuning);
        deskCamera.transform.SetPositionAndRotation(aim - level * back + Vector3.up * up,
                                                    Quaternion.LookRotation(level, Vector3.up) * Quaternion.Euler(DeskViewPose.Pitch(tuning), 0f, 0f));
        LensSettings lens = office.Lens;
        if (tuning.fieldOfView > 0f)
            lens.FieldOfView = tuning.fieldOfView;
        deskCamera.Lens = lens;
        _onPriority = office.Priority.Value + PriorityAboveOffice;
        deskCamera.Priority = IsOn ? _onPriority : IdlePriority;
        deskCamera.gameObject.SetActive(true);
    }

    /// <summary>The mat's click: tilts into the desk view, or back (only while the toggle is live).</summary>
    public void Toggle()
    {
        if (_toggleLive)
            Set(!IsOn);
    }

    /// <summary>Tilts into the desk view (no-op if it is on, or while the mat's toggle is not live): the PC's "&lt; Desk" button, once its frame has closed.</summary>
    public void TiltIn()
    {
        if (_toggleLive)
            Set(true);
    }

    /// <summary>Returns to the normal view (no-op if it is on): Escape, the right-click, the next traveller, a newsletter.</summary>
    public void Return() => Set(false);

    /// <summary>Shows the mat's click box (a click toggles) or hides it (BoothCoordinator: BoothRules.DeskViewToggleLive); never while unbound.</summary>
    public void SetToggleLive(bool live)
    {
        _toggleLive = live && _office != null;
        if (mat != null && mat.gameObject.activeSelf != _toggleLive)
            mat.gameObject.SetActive(_toggleLive);
    }

    /// <summary>Lets Escape and a right-click on empty space return, from the next frame on (BoothCoordinator: BoothRules.DeskViewReturnLive; X8's one press, one thing).</summary>
    public void SetReturnLive(bool live)
    {
        if (live && !_returnLive)
            _returnLiveSince = Time.frameCount;
        _returnLive = live;
    }

    /// <summary>Shows the "▲ Back" control and lets the wheel rolled up return, from the next frame on, or hides it (BoothCoordinator: BoothRules.DeskViewBackLive).</summary>
    public void SetBackLive(bool live)
    {
        if (live && !_backLive)
            _backLiveSince = Time.frameCount;
        _backLive = live;
        if (backButton != null && backButton.gameObject.activeSelf != live)
            backButton.gameObject.SetActive(live);
    }

    /// <summary>Lets the wheel rolled down over the empty mat tilt in, from the next frame on (BoothCoordinator: BoothRules.DeskViewScrollInLive); never while unbound.</summary>
    public void SetScrollInLive(bool live)
    {
        live &= _office != null;
        if (live && !_scrollInLive)
            _scrollInLiveSince = Time.frameCount;
        _scrollInLive = live;
    }

    /// <summary>
    /// While on: Escape, or a right-click on empty space, returns while the
    /// return is live, and the wheel rolled up while the Back control is live.
    /// While off: the wheel rolled down with the pointer on the empty mat
    /// (nothing above it takes the pointer, no UI) tilts in.
    /// </summary>
    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;
        float scroll = mouse != null ? mouse.scroll.ReadValue().y : 0f;
        if (IsOn)
        {
            if (_returnLive && _returnLiveSince < Time.frameCount &&
                ((keyboard != null && keyboard.escapeKey.wasPressedThisFrame) ||
                 (mouse != null && mouse.rightButton.wasPressedThisFrame && OnEmptySpace(mouse.position.ReadValue()))))
                Return();
            else if (_backLive && _backLiveSince < Time.frameCount && scroll > 0f)
                Return();
        }
        else if (_scrollInLive && _scrollInLiveSince < Time.frameCount && scroll < 0f && mouse != null && OnMat(mouse.position.ReadValue()))
        {
            Set(true);
        }
    }

    /// <summary>The Back control's click (only while it is live).</summary>
    private void Back()
    {
        if (_backLive)
            Return();
    }

    /// <summary>True when nothing under the screen point takes clicks, or the mat does.</summary>
    private bool OnEmptySpace(Vector2 screen)
    {
        GameObject handler = TopHandler(screen, out bool any);
        return !any || handler == null || (mat != null && handler == mat.gameObject);
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

    private void Set(bool on)
    {
        if (on == IsOn || (on && _office == null))
            return;

        IsOn = on;
        if (CinemachineCore.GetBlendOverride != _blend)
        {
            _previous = CinemachineCore.GetBlendOverride;
            CinemachineCore.GetBlendOverride = _blend;
        }
        deskCamera.Priority = on ? _onPriority : IdlePriority;
        Changed?.Invoke();
    }

    /// <summary>The blend to or from the desk camera: the knob's seconds, eased (a cut under Reduced Motion); any other blend is left to the hook before ours, or the brain's own.</summary>
    private CinemachineBlendDefinition Blend(ICinemachineCamera from, ICinemachineCamera to, CinemachineBlendDefinition fallback, UnityEngine.Object owner)
    {
        if (deskCamera == null || (!ReferenceEquals(from, deskCamera) && !ReferenceEquals(to, deskCamera)))
            return _previous != null ? _previous(from, to, fallback, owner) : fallback;

        float seconds = DeskViewPose.Seconds(config.deskView, MotionPreference.Reduced);
        return seconds > 0f
            ? new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, seconds)
            : new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0f);
    }
}
