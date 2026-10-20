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
/// The office's two views: clicking the PC, its grey tab or Q (OfficeControls)
/// opens its frame over the office (MonitorFocus); the tab or Q again, a
/// right-click or Esc once the desktop's own chain has nothing left to close
/// (ControlRules.BackOut, OfficeControls), a click outside the frame, its
/// close button and the desktop's "&lt; Desk" button close it (OfficeFocus); a click outside the
/// frame then goes on to what it lands on (the intercom opens the wheel, the
/// desk tilts the view: the exit catcher's pass-through), and
/// the "&lt; Desk" button closes it straight into the reading view (FocusDesk;
/// Saleh 2026-09-30: a view key's move, tagged so for the QA camera logger).
/// The frame itself moves no camera (from the reading view, BoothCoordinator
/// blends it back up as the frame opens). While the city view is on, Q
/// leaves the city instead of opening the PC (FIX_PLAN_v2's CAM delta: the
/// city view is left "with Q or a click on the city, the same as the PC");
/// F1 still opens the PC with its keys card there, as it always did. The PC
/// never opens while the player holds something on the desk
/// (HoldRegistry.Desk; BUG-55c: no view change while holding).
/// BoothCoordinator reads the view to gate the office's input.
/// </summary>
public sealed class OfficeViewController : MonoBehaviour
{
    /// <summary>The PC frame the monitor view opens.</summary>
    [SerializeField] private PcFrame frame;

    /// <summary>The desk view (optional): the taskbar's "&lt; Desk" button closes the frame straight into it (FocusDesk).</summary>
    [SerializeField] private DeskView deskView;

    /// <summary>The city view (optional): while it is on, Q leaves it instead of opening the PC (FocusMonitor; F1 still opens the PC).</summary>
    [SerializeField] private CityView cityView;

    /// <summary>The current view.</summary>
    public OfficeView Current { get; private set; } = OfficeView.OfficeFocus;

    /// <summary>Raised after the view changes.</summary>
    public event Action<OfficeView> ViewChanged;

    /// <summary>The PC's key (ControlRules.PcKey, Q) on the Input System's keyboard.</summary>
    private static readonly Key PcKey = Enum.TryParse(ControlRules.PcKey, true, out Key key) ? key : Key.Q;

    /// <summary>
    /// Opens the PC frame (no-op if open, or while something is held:
    /// BUG-55c). While the city view is on, the PC's key (Q) pressed this
    /// frame leaves the city instead (Q leaves the city as it leaves the PC);
    /// every other way here (F1's keys card) opens the PC as before, the city
    /// turning back as the PC takes over (BoothCoordinator). The key is read
    /// here until ControlRules sends Q in the city view to LeaveCity itself
    /// (DOC-HANDLE's hand-off, CAM.md).
    /// </summary>
    public void FocusMonitor()
    {
        if (HoldRegistry.Desk.IsHolding)
            return;
        if (cityView != null && cityView.IsOn && Keyboard.current is Keyboard keyboard && keyboard[PcKey].wasPressedThisFrame)
        {
            cityView.Return();
            return;
        }
        SetView(OfficeView.MonitorFocus);
    }

    /// <summary>Closes the PC frame (no-op if closed).</summary>
    public void FocusOffice() => SetView(OfficeView.OfficeFocus);

    /// <summary>The taskbar's "&lt; Desk" button: closes the PC frame and tilts the camera over the desk in the same click (the desk view's blend; it stays raised while papers are held or without a desk view; Saleh 2026-09-30: from the PC to the desk without backing out first).</summary>
    public void FocusDesk()
    {
        FocusOffice();
        if (deskView != null)
            deskView.TiltIn(CameraMoveCause.ViewKey);
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
