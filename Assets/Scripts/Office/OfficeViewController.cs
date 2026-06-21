using System;
using UnityEngine;

/// <summary>The two camera states of the office scene.</summary>
public enum OfficeView
{
    /// <summary>Wide booth view from the agent's POV (default).</summary>
    OfficeFocus,

    /// <summary>Close-up on the CRT; the desktop UI is interactive.</summary>
    MonitorFocus
}

/// <summary>
/// Drives the office's two camera states. Logic is decoupled from Cinemachine
/// via <see cref="ICameraRig"/> so it is unit-testable; the desktop Canvas is
/// shown only in MonitorFocus. Tapping the CRT/READY sign calls FocusMonitor;
/// a desktop "back" affordance calls FocusOffice.
/// </summary>
public sealed class OfficeViewController : MonoBehaviour
{
    /// <summary>Cinemachine rig (a CinemachineCameraRig MonoBehaviour).</summary>
    [SerializeField] private MonoBehaviour cameraRigBehaviour;

    /// <summary>The screen-space desktop canvas, shown only in MonitorFocus.</summary>
    [SerializeField] private GameObject desktopRoot;

    private ICameraRig _rig;

    /// <summary>The current view state.</summary>
    public OfficeView Current { get; private set; } = OfficeView.OfficeFocus;

    /// <summary>Raised after the view changes to the given state.</summary>
    public event Action<OfficeView> ViewChanged;

    private void Awake()
    {
        if (_rig == null)
            _rig = cameraRigBehaviour as ICameraRig;

        ApplyState(force: true);
    }

    /// <summary>Test seam: inject a fake rig + desktop and apply the default state.</summary>
    public void InitForTest(ICameraRig rig, GameObject desktop)
    {
        _rig = rig;
        desktopRoot = desktop;
        Current = OfficeView.OfficeFocus;
        ApplyState(force: true);
    }

    /// <summary>Pushes in to the monitor (no-op if already there).</summary>
    public void FocusMonitor() => SetView(OfficeView.MonitorFocus);

    /// <summary>Pulls back to the booth (no-op if already there).</summary>
    public void FocusOffice() => SetView(OfficeView.OfficeFocus);

    /// <summary>Toggles between the two views.</summary>
    public void Toggle() =>
        SetView(Current == OfficeView.OfficeFocus ? OfficeView.MonitorFocus : OfficeView.OfficeFocus);

    private void SetView(OfficeView view)
    {
        if (view == Current)
            return;

        Current = view;
        ApplyState(force: false);
        ViewChanged?.Invoke(Current);
    }

    private void ApplyState(bool force)
    {
        bool monitor = Current == OfficeView.MonitorFocus;

        if (desktopRoot != null)
            desktopRoot.SetActive(monitor);

        if (_rig != null)
        {
            if (monitor)
                _rig.ShowMonitor();
            else
                _rig.ShowOffice();
        }
    }
}
