/// <summary>What the PC does with a back-out press (Esc or a right-click) once its own chain (DesktopEscapeRule) has run. Not serialized.</summary>
public enum PcBackOutStep
{
    /// <summary>The desktop's chain took the press (it closed a menu, the card, left a field, ...); nothing else happens.</summary>
    Chain,

    /// <summary>Esc with nothing left to close: the Settings app comes to the front (Saleh 2026-10-20, R3/R4: "if you press escape you should auto open the pc setting menu").</summary>
    OpenSettings,

    /// <summary>A right-click with nothing left to close: the press is taken and does nothing (it never leaves the PC: only Q and the bezel do).</summary>
    Swallow,

    /// <summary>Esc on the PC with its screen off (no desktop to back out of): the screen comes on and the Settings app comes to the front (R3/R4: "Esc opens the same Settings window everywhere").</summary>
    WakeAndOpenSettings,

    /// <summary>Esc with nothing else to close and a window other than Settings in front: that window closes (D13, UI-23: Esc backs out of a PC window first; GameConfigSO.escBacksOutFirst; <see cref="PcBackOut.ClosesFront"/>). With no such window, Esc brings Settings forward and Settings stays (R3/R4).</summary>
    CloseWindow
}

/// <summary>Which of the PC keyboard's pollers runs in a frame (DesktopKeyboard, <see cref="PcBackOut.Poller"/>). Not serialized.</summary>
public enum PcPoller
{
    /// <summary>Neither: the PC takes no key and no back-out (the PC closed, or the desktop taking no input with the screen on).</summary>
    None,

    /// <summary>The desktop's keys and back-out: it takes input (the frame open, the screen on), from the press frame.</summary>
    Desktop,

    /// <summary>The back-out alone, on the PC open with its screen off (<see cref="PcBackOut.ScreenOff"/>).</summary>
    ScreenOff,

    /// <summary>
    /// An overlay holds the InputGate (the cheat panel over the PC): the PC
    /// takes no key and no back-out, and a desktop text field that has the
    /// keyboard is left, its text kept. The EventSystem still updates the
    /// selected field under the gate, so TMP would read the keys there itself:
    /// the typing would land under the panel, and its own Esc would cancel the
    /// edit and put back the text the field had when it was focused (Notes
    /// keeps every change on the day's page, so the notes typed since would be
    /// lost). Taking the selection off ends the edit without a cancel, as the
    /// screen going dark does (MonitorScreen.SetInteractive).
    /// </summary>
    Gated
}

/// <summary>
/// The PC's back-out (BUG-50a, UI-PC; FIX_PLAN_v2 D20 and the R3/R4
/// confirmation: "Esc opens the same Settings window everywhere ... To leave
/// the PC, press Q or click the bezel"): the desktop's own Escape chain runs
/// first, one step per press; when it has nothing left to close, Esc closes
/// the front window unless it is Settings (D13, UI-23, while
/// GameConfigSO.escBacksOutFirst is on; <see cref="ClosesFront"/>), then
/// brings the Settings app to the front, where it stays (Esc never closes the
/// pause it opens), and a right-click does nothing. Either way
/// the desktop takes the press, so the office's back-out never closes the PC
/// (OfficeControls skips a press the desktop stamped). With the PC's screen
/// off there is no desktop to take the press, so the PC still takes it: Esc
/// turns the screen on and brings Settings forward, a right-click does
/// nothing (<see cref="ScreenOff"/>). While an overlay holds the InputGate
/// (the cheat panel; RUN-SAFE's BUG-68a contract: "the PC's input handlers
/// stand still"), the PC takes neither a key nor a back-out, and a desktop
/// text field that has the keyboard is left with its text kept
/// (<see cref="Poller"/>, <see cref="PcPoller.Gated"/>). Pure; DesktopKeyboard
/// applies it.
/// </summary>
public static class PcBackOut
{
    /// <summary>
    /// What the PC's keyboard polls this frame: nothing while
    /// <paramref name="gateHeld"/> (InputGate.Blocked: the cheat panel over
    /// the PC, so an Esc never brings Settings forward behind it), when it
    /// leaves a focused desktop field instead (<see cref="PcPoller.Gated"/>);
    /// the desktop's keys and back-out while it takes input
    /// (<paramref name="desktopInteractive"/>: BoothRules.DesktopInteractive),
    /// the screen-off back-out while the PC is open
    /// (<paramref name="pcOpen"/>) with its screen off
    /// (<paramref name="screenOn"/> false), else nothing.
    /// </summary>
    public static PcPoller Poller(bool gateHeld, bool desktopInteractive, bool pcOpen, bool screenOn)
    {
        if (gateHeld)
            return PcPoller.Gated;
        if (desktopInteractive)
            return PcPoller.Desktop;
        return pcOpen && !screenOn ? PcPoller.ScreenOff : PcPoller.None;
    }

    /// <summary>
    /// What a back-out does after the chain resolved <paramref name="chain"/>;
    /// <paramref name="fromKey"/> is true for Esc, false for a right-click.
    /// With nothing left in the chain, Esc closes the front window when
    /// <paramref name="windowToClose"/> (D13, UI-23: a window open on the PC
    /// and GameConfigSO.escBacksOutFirst on), else brings Settings forward; a
    /// right-click does nothing (it never leaves the PC: R3).
    /// </summary>
    public static PcBackOutStep Resolve(DesktopEscape chain, bool fromKey, bool windowToClose = false)
    {
        if (chain != DesktopEscape.None)
            return PcBackOutStep.Chain;
        if (!fromKey)
            return PcBackOutStep.Swallow;
        return windowToClose ? PcBackOutStep.CloseWindow : PcBackOutStep.OpenSettings;
    }

    /// <summary>
    /// True when Esc, with the desktop's chain empty, closes the PC's front
    /// window (Resolve's windowToClose): a window is in front
    /// (<paramref name="windowInFront"/>), it is not the Settings app
    /// (<paramref name="frontAppId"/>, DesktopApps.IdOf: null for a window of
    /// no app), and <paramref name="escBacksOutFirst"/>
    /// (GameConfigSO.escBacksOutFirst, D13). Settings is the pause Esc opens
    /// (R3/R4: "Esc opens the same Settings window everywhere"; the gate check
    /// flow.esc.pc): Esc never closes it, so Esc twice from the desk keeps the
    /// PC open with Settings in front and the shift clock held. The Investigation window (the Lens screen) is not closed by Esc either:
    /// the pick, then inspect back out first (the chain), then Esc opens Settings (the Lens spec section 2).
    /// </summary>
    public static bool ClosesFront(string frontAppId, bool windowInFront, bool escBacksOutFirst) =>
        windowInFront && escBacksOutFirst && frontAppId != DesktopAppIds.Settings && frontAppId != DesktopAppIds.Investigation;

    /// <summary>What a back-out does on the PC while its screen is off; <paramref name="fromKey"/> is true for Esc, false for a right-click.</summary>
    public static PcBackOutStep ScreenOff(bool fromKey) => fromKey ? PcBackOutStep.WakeAndOpenSettings : PcBackOutStep.Swallow;
}
