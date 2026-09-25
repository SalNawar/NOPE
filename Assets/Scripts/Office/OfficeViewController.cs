using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>The two views of the office.</summary>
public enum OfficeView
{
    /// <summary>The office from the agent's chair (default).</summary>
    OfficeFocus,

    /// <summary>The PC frame is open over the office; the desktop takes input.</summary>
    MonitorFocus
}

/// <summary>
/// The office's two views: clicking the PC opens its frame over the office
/// (MonitorFocus); Escape, a click outside the frame, its close button and the
/// desktop's "&lt; Desk" button close it (OfficeFocus). The camera never moves.
/// BoothCoordinator reads the view to gate the office's input.
/// </summary>
public sealed class OfficeViewController : MonoBehaviour
{
    /// <summary>The PC frame the monitor view opens.</summary>
    [SerializeField] private PcFrame frame;

    /// <summary>The current view.</summary>
    public OfficeView Current { get; private set; } = OfficeView.OfficeFocus;

    /// <summary>Raised after the view changes.</summary>
    public event Action<OfficeView> ViewChanged;

    /// <summary>Escape closes the frame, like its close button and a click outside it.</summary>
    private void Update()
    {
        if (Current != OfficeView.MonitorFocus)
            return;

        Keyboard kb = Keyboard.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame)
            FocusOffice();
    }

    /// <summary>Opens the PC frame (no-op if open).</summary>
    public void FocusMonitor() => SetView(OfficeView.MonitorFocus);

    /// <summary>Closes the PC frame (no-op if closed).</summary>
    public void FocusOffice() => SetView(OfficeView.OfficeFocus);

    private void SetView(OfficeView view)
    {
        if (view == Current)
            return;

        Current = view;
        if (frame != null)
            frame.SetOpen(view == OfficeView.MonitorFocus);
        ViewChanged?.Invoke(Current);
    }
}
