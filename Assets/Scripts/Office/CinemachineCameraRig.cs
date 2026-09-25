using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// Real <see cref="ICameraRig"/> backed by two Cinemachine cameras. Raises the
/// active camera's priority so the CinemachineBrain blends the push-in. With a
/// live monitor and the desk tuning wired, an orthographic monitor camera is
/// framed on the CRT's glass (MonitorFraming, the fill knob) and the blend
/// takes the knob's time. Every new reference is optional: without a brain
/// (the hybrid scene does not wire one) it is looked up in Start, and a rig
/// with no brain counts as settled at once so the booth never waits for it.
/// </summary>
public sealed class CinemachineCameraRig : MonoBehaviour, ICameraRig
{
    /// <summary>Wide booth camera.</summary>
    [SerializeField] private CinemachineCamera officeCam;

    /// <summary>Close-up monitor camera.</summary>
    [SerializeField] private CinemachineCamera monitorCam;

    /// <summary>The brain on the camera these cameras drive; set by the builder. Unset (the hybrid scene), it is looked up in Start.</summary>
    [SerializeField] private CinemachineBrain brain;

    /// <summary>Optional: the live monitor, whose glass the monitor camera frames.</summary>
    [SerializeField] private MonitorScreen monitorScreen;

    /// <summary>Optional: the desk tuning (the glass's fill of the view, the blend time).</summary>
    [SerializeField] private DeskConfigSO config;

    private const int Active = 20;
    private const int Idle = 10;

    /// <summary>After every OnEnable (brains register there): finds the brain when unset, and sets its blend from the knob.</summary>
    private void Start()
    {
        if (brain == null)
        {
            brain = CinemachineCore.FindPotentialTargetBrain(officeCam);
            if (brain == null)
                Debug.LogWarning("[CinemachineCameraRig] No CinemachineBrain drives the office cameras: views count as settled at once. Run Tools > TimeDesk > Build Office UI.", this);
        }

        if (brain != null && config != null)
            brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, config.focusBlendSeconds);
    }

    /// <summary>Raises the office camera above the monitor camera.</summary>
    public void ShowOffice()
    {
        if (officeCam != null) officeCam.Priority = Active;
        if (monitorCam != null) monitorCam.Priority = Idle;
    }

    /// <summary>Frames the glass (orthographic cameras with a live monitor), then raises the monitor camera above the office camera.</summary>
    public void ShowMonitor()
    {
        FrameMonitor();
        if (officeCam != null) officeCam.Priority = Idle;
        if (monitorCam != null) monitorCam.Priority = Active;
    }

    /// <summary>True when the brain shows that view's camera with no blend running; true with no brain (the camera never moves, so nothing waits for it).</summary>
    public bool IsSettled(OfficeView view)
    {
        if (brain == null)
            return true;

        CinemachineCamera cam = view == OfficeView.MonitorFocus ? monitorCam : officeCam;
        return !brain.IsBlending && brain.ActiveVirtualCamera == (ICinemachineCamera)cam;
    }

    /// <summary>
    /// Centres the monitor camera on the glass (keeping its z) and sizes it so
    /// the glass fills the knob's fraction of the view at the output camera's
    /// aspect. Only for an orthographic output camera with the brain, the
    /// monitor screen and the config wired: a perspective camera (the hybrid
    /// scene) is left alone. (The camera's own flag is read, not
    /// Lens.Orthographic, which Cinemachine fills only when it pulls the camera state.)
    /// </summary>
    private void FrameMonitor()
    {
        if (monitorCam == null || brain == null || brain.OutputCamera == null || monitorScreen == null || config == null ||
            !brain.OutputCamera.orthographic)
            return;

        Vector3 glass = monitorScreen.GlassCentre;
        Transform t = monitorCam.transform;
        t.position = new Vector3(glass.x, glass.y, t.position.z);

        Vector2 size = monitorScreen.GlassWorldSize;
        monitorCam.Lens.OrthographicSize = MonitorFraming.OrthoSize(size.x, size.y, brain.OutputCamera.aspect, config.monitorFill);
    }
}
