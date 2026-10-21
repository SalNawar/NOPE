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
    WakeAndOpenSettings
}

/// <summary>Which of the PC keyboard's pollers runs in a frame (DesktopKeyboard, <see cref="PcBackOut.Poller"/>). Not serialized.</summary>
public enum PcPoller
{
    /// <summary>Neither: the PC takes no key and no back-out (the PC closed, the desktop taking no input with the screen on, or the InputGate held).</summary>
    None,

    /// <summary>The desktop's keys and back-out: it takes input (the frame open, the screen on), from the press frame.</summary>
    Desktop,

    /// <summary>The back-out alone, on the PC open with its screen off (<see cref="PcBackOut.ScreenOff"/>).</summary>
    ScreenOff
}

/// <summary>
/// The PC's back-out (BUG-50a, UI-PC; FIX_PLAN_v2 D20 and the R3/R4
/// confirmation: "Esc opens the same Settings window everywhere ... To leave
/// the PC, press Q or click the bezel"): the desktop's own Escape chain runs
/// first, one step per press; when it has nothing left to close, Esc brings
/// the Settings app to the front and a right-click does nothing. Either way
/// the desktop takes the press, so the office's back-out never closes the PC
/// (OfficeControls skips a press the desktop stamped). With the PC's screen
/// off there is no desktop to take the press, so the PC still takes it: Esc
/// turns the screen on and brings Settings forward, a right-click does
/// nothing (<see cref="ScreenOff"/>). While an overlay holds the InputGate
/// (the cheat panel; RUN-SAFE's BUG-68a contract: "the PC's input handlers
/// stand still"), the PC takes neither a key nor a back-out
/// (<see cref="Poller"/>). Pure; DesktopKeyboard applies it.
/// </summary>
public static class PcBackOut
{
    /// <summary>
    /// What the PC's keyboard polls this frame: nothing while
    /// <paramref name="gateHeld"/> (InputGate.Blocked: the cheat panel over
    /// the PC, so an Esc never brings Settings forward behind it), the
    /// desktop's keys and back-out while it takes input
    /// (<paramref name="desktopInteractive"/>: BoothRules.DesktopInteractive),
    /// the screen-off back-out while the PC is open
    /// (<paramref name="pcOpen"/>) with its screen off
    /// (<paramref name="screenOn"/> false), else nothing.
    /// </summary>
    public static PcPoller Poller(bool gateHeld, bool desktopInteractive, bool pcOpen, bool screenOn)
    {
        if (gateHeld)
            return PcPoller.None;
        if (desktopInteractive)
            return PcPoller.Desktop;
        return pcOpen && !screenOn ? PcPoller.ScreenOff : PcPoller.None;
    }

    /// <summary>What a back-out does after the chain resolved <paramref name="chain"/>; <paramref name="fromKey"/> is true for Esc, false for a right-click.</summary>
    public static PcBackOutStep Resolve(DesktopEscape chain, bool fromKey)
    {
        if (chain != DesktopEscape.None)
            return PcBackOutStep.Chain;
        return fromKey ? PcBackOutStep.OpenSettings : PcBackOutStep.Swallow;
    }

    /// <summary>What a back-out does on the PC while its screen is off; <paramref name="fromKey"/> is true for Esc, false for a right-click.</summary>
    public static PcBackOutStep ScreenOff(bool fromKey) => fromKey ? PcBackOutStep.WakeAndOpenSettings : PcBackOutStep.Swallow;
}
