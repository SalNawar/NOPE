using System;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// The desk view (piece 10 section 11, T1-T4): a click on the mat tilts the
/// camera forward over the desk, and the same click tilts it back; Escape and
/// a right-click on empty space (nothing under the pointer takes clicks, or the
/// mat does) also return; BoothCoordinator returns it when the next traveller
/// is called or a newsletter shows, and says when the mat and the returns are
/// live (BoothRules). The view is a gameplay-owned Cinemachine camera, posed at
/// bind (OfficeSceneBinder) from the art office's camera and the mat's centre
/// (DeskViewPose, DeskConfigSO.deskView), and raised above the art camera's
/// priority while on, so the art camera's brain blends. The blend's time and
/// style come from a CinemachineCore.GetBlendOverride hook that answers only
/// for blends to or from the desk camera (a cut under Reduced Motion). The art
/// camera is never moved. Unbound (no art Cinemachine camera), the mat never
/// shows.
/// </summary>
public sealed class DeskView : MonoBehaviour
{
    /// <summary>The desk tuning (the desk view's knobs).</summary>
    [SerializeField] private DeskConfigSO config;

    /// <summary>The desk view's camera (inactive until bound; priority 0 while the view is off).</summary>
    [SerializeField] private CinemachineCamera deskCamera;

    /// <summary>The mat's click box (the desk's clamp area just under its plane, laid by the office binder; inactive while the toggle is not live).</summary>
    [SerializeField] private ClickCatcher mat;

    private CinemachineCamera _office;
    private int _onPriority;
    private bool _toggleLive;
    private bool _returnLive;
    private int _returnLiveSince;
    private CinemachineCore.GetBlendOverrideDelegate _blend;
    private CinemachineCore.GetBlendOverrideDelegate _previous;
    private readonly List<RaycastResult> _hits = new List<RaycastResult>();

    /// <summary>True while the camera is (or is blending) over the desk.</summary>
    public bool IsOn { get; private set; }

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
    /// moved along the view's level forward and up by the knobs, its yaw kept,
    /// pitched by DeskViewPose; the lens copied. The camera then waits at
    /// priority 0.
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
        float pitch = DeskViewPose.Pitch(art.position.y - matCentre.y, Vector3.Dot(matCentre - art.position, level), tuning);
        deskCamera.transform.SetPositionAndRotation(art.position + level * tuning.forward + Vector3.up * tuning.rise,
                                                    Quaternion.LookRotation(level, Vector3.up) * Quaternion.Euler(pitch, 0f, 0f));
        deskCamera.Lens = office.Lens;
        _onPriority = office.Priority.Value + 1;
        deskCamera.Priority = IsOn ? _onPriority : 0;
        deskCamera.gameObject.SetActive(true);
    }

    /// <summary>The mat's click: tilts into the desk view, or back (only while the toggle is live).</summary>
    public void Toggle()
    {
        if (_toggleLive)
            Set(!IsOn);
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

    /// <summary>While on and its return is live: Escape, or a right-click on empty space, returns.</summary>
    private void Update()
    {
        if (!IsOn || !_returnLive || _returnLiveSince >= Time.frameCount)
            return;

        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;
        if ((keyboard != null && keyboard.escapeKey.wasPressedThisFrame) ||
            (mouse != null && mouse.rightButton.wasPressedThisFrame && OnEmptySpace(mouse.position.ReadValue())))
            Return();
    }

    /// <summary>True when nothing under the screen point takes clicks, or the mat does.</summary>
    private bool OnEmptySpace(Vector2 screen)
    {
        EventSystem events = EventSystem.current;
        if (events == null)
            return true;

        _hits.Clear();
        events.RaycastAll(new PointerEventData(events) { position = screen }, _hits);
        if (_hits.Count == 0)
            return true;
        GameObject handler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(_hits[0].gameObject);
        return handler == null || (mat != null && handler == mat.gameObject);
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
        deskCamera.Priority = on ? _onPriority : 0;
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
