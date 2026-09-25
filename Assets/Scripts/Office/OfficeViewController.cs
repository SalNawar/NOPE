using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>The two camera states of the office scene.</summary>
public enum OfficeView
{
    /// <summary>Wide booth view from the agent's POV (default).</summary>
    OfficeFocus,

    /// <summary>Close-up on the CRT; the desktop takes input once the push-in settles.</summary>
    MonitorFocus
}

/// <summary>
/// Drives the office's two camera states through <see cref="ICameraRig"/> and
/// reports when the rig has settled on a view (the desktop's input waits for
/// it). With a live monitor wired (MonitorScreen) the desktop canvas stays
/// active on the CRT and BoothCoordinator gates its input; without one (the
/// hybrid scene) the desktop is shown only in MonitorFocus. Clicking the CRT
/// calls FocusMonitor; Escape, a click outside the screen and the desktop's
/// "&lt; Desk" button call FocusOffice.
/// </summary>
public sealed class OfficeViewController : MonoBehaviour
{
    /// <summary>Cinemachine rig (a CinemachineCameraRig MonoBehaviour).</summary>
    [SerializeField] private MonoBehaviour cameraRigBehaviour;

    /// <summary>The desktop canvas's object: shown only in MonitorFocus when no live monitor is wired (the hybrid scene's full-screen desktop); left active otherwise.</summary>
    [SerializeField] private GameObject desktopRoot;

    /// <summary>
    /// Optional: the live desktop on the CRT. When set, the desktop canvas
    /// stays active (the booth coordinator gates its input); when not (the
    /// hybrid scene), it is shown only in MonitorFocus.
    /// </summary>
    [SerializeField] private MonitorScreen monitorScreen;

    private ICameraRig _rig;

    /// <summary>The current view state.</summary>
    public OfficeView Current { get; private set; } = OfficeView.OfficeFocus;

    /// <summary>True once the rig shows the current view with no blend running (at once with no rig).</summary>
    public bool IsSettled { get; private set; }

    /// <summary>Raised after the view changes to the given state.</summary>
    public event Action<OfficeView> ViewChanged;

    /// <summary>Raised once per view change, when the rig first shows that view with no blend running.</summary>
    public event Action<OfficeView> Settled;

    private void Awake()
    {
        _rig = cameraRigBehaviour as ICameraRig;
        ApplyState();
    }

    /// <summary>
    /// The settle poll first, in either view and only while unsettled (no
    /// allocation); then Escape leaves the monitor, like the desktop's
    /// "&lt; Desk" button and a click outside the screen.
    /// </summary>
    private void Update()
    {
        if (!IsSettled && _rig != null && _rig.IsSettled(Current))
        {
            IsSettled = true;
            Settled?.Invoke(Current);
        }

        if (Current != OfficeView.MonitorFocus)
            return;

        Keyboard kb = Keyboard.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame)
            FocusOffice();
    }

    /// <summary>Pushes in to the monitor (no-op if already there).</summary>
    public void FocusMonitor() => SetView(OfficeView.MonitorFocus);

    /// <summary>Pulls back to the booth (no-op if already there).</summary>
    public void FocusOffice() => SetView(OfficeView.OfficeFocus);

    private void SetView(OfficeView view)
    {
        if (view == Current)
            return;

        Current = view;
        ApplyState();
        ViewChanged?.Invoke(Current);
    }

    /// <summary>Shows the view on the rig (unsettled until the poll sees it); with no rig the view is settled at once.</summary>
    private void ApplyState()
    {
        bool monitor = Current == OfficeView.MonitorFocus;

        if (monitorScreen == null && desktopRoot != null)
            desktopRoot.SetActive(monitor);

        if (_rig == null)
        {
            IsSettled = true;
            Settled?.Invoke(Current);
            return;
        }

        IsSettled = false;
        if (monitor)
            _rig.ShowMonitor();
        else
            _rig.ShowOffice();
    }
}
