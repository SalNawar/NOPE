using System;
using UnityEngine;

/// <summary>The two views of the office.</summary>
public enum OfficeView
{
    /// <summary>The office from the agent's chair (default).</summary>
    OfficeFocus,

    /// <summary>The PC frame is open over the office; the desktop takes input.</summary>
    MonitorFocus
}

/// <summary>
/// The office's two views: clicking the PC, its grey tab or Q (OfficeControls)
/// opens its frame over the office (MonitorFocus); the tab or Q again, a
/// right-click or Esc once the desktop's own chain has nothing left to close
/// (ControlRules.BackOut, OfficeControls), a click outside the frame, its
/// close button and the desktop's "&lt; Desk" button close it (OfficeFocus); a click outside the
/// frame then goes on to what it lands on (the traveller or the intercom opens
/// the wheel, the desk tilts the view: the exit catcher's pass-through), and
/// the "&lt; Desk" button closes it straight into the desk view (FocusDesk;
/// Saleh 2026-09-30). The frame itself moves no camera (from the desk view,
/// BoothCoordinator blends it back up as the frame opens). BoothCoordinator
/// reads the view to gate the office's input.
/// </summary>
public sealed class OfficeViewController : MonoBehaviour
{
    /// <summary>The PC frame the monitor view opens.</summary>
    [SerializeField] private PcFrame frame;

    /// <summary>The desk view (optional): the taskbar's "&lt; Desk" button closes the frame straight into it (FocusDesk).</summary>
    [SerializeField] private DeskView deskView;

    /// <summary>The current view.</summary>
    public OfficeView Current { get; private set; } = OfficeView.OfficeFocus;

    /// <summary>Raised after the view changes.</summary>
    public event Action<OfficeView> ViewChanged;

    /// <summary>Opens the PC frame (no-op if open).</summary>
    public void FocusMonitor() => SetView(OfficeView.MonitorFocus);

    /// <summary>Closes the PC frame (no-op if closed).</summary>
    public void FocusOffice() => SetView(OfficeView.OfficeFocus);

    /// <summary>The taskbar's "&lt; Desk" button: closes the PC frame and tilts the camera over the desk in the same click (the desk view's blend; it stays raised while papers are held or without a desk view; Saleh 2026-09-30: from the PC to the desk without backing out first).</summary>
    public void FocusDesk()
    {
        FocusOffice();
        if (deskView != null)
            deskView.TiltIn();
    }

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
