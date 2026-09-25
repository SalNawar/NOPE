/// <summary>Where the shift is, as the booth's input rules need it. Set by GameManager.</summary>
public enum BoothPhase
{
    /// <summary>No traveller at the desk: before the shift, behind READY or between travellers (the default before Start).</summary>
    NoTraveller,

    /// <summary>A traveller is at the desk, from presentation until the decision.</summary>
    TravellerAtDesk,

    /// <summary>The morning briefing or the shift report is up.</summary>
    Newsletter
}

/// <summary>What the booth's input rules read.</summary>
public readonly struct BoothContext
{
    /// <summary>The view is the monitor (MonitorFocus).</summary>
    public readonly bool Focused;

    /// <summary>The camera rig shows that view and no blend is running.</summary>
    public readonly bool Settled;

    /// <summary>The PC's screen is on.</summary>
    public readonly bool ScreenOn;

    /// <summary>Where the shift is.</summary>
    public readonly BoothPhase Phase;

    /// <summary>The traveller wheel is open.</summary>
    public readonly bool WheelOpen;

    /// <summary>A citation slip waits for Acknowledge (the traveller has left; the slip holds the clock and the screen).</summary>
    public readonly bool CitationPending;

    /// <summary>Creates a context.</summary>
    public BoothContext(bool focused, bool settled, bool screenOn, BoothPhase phase, bool wheelOpen, bool citationPending)
    {
        Focused = focused;
        Settled = settled;
        ScreenOn = screenOn;
        Phase = phase;
        WheelOpen = wheelOpen;
        CitationPending = citationPending;
    }
}

/// <summary>Which of the booth's inputs are live (BoothRules.Evaluate).</summary>
public readonly struct BoothInput
{
    /// <summary>The desktop canvas takes clicks: focused, settled, the screen on, no newsletter.</summary>
    public readonly bool DesktopInteractive;

    /// <summary>Clicking the CRT focuses it: in the booth (settled or blending back), no newsletter, the wheel closed.</summary>
    public readonly bool CrtFocusable;

    /// <summary>The bezel power button toggles the screen: settled in either view, no newsletter, the wheel closed, no pending citation slip.</summary>
    public readonly bool PowerButtonLive;

    /// <summary>The focus exit zone and the glass zone are up: focused and settled.</summary>
    public readonly bool FocusExitLive;

    /// <summary>The desk props react to clicks: the settled booth, no newsletter, the wheel closed.</summary>
    public readonly bool PropsLive;

    /// <summary>The papers can be dragged and clicked: as the props, while a traveller is at the desk.</summary>
    public readonly bool PapersLive;

    /// <summary>The wheel may be open: the settled booth while a traveller is at the desk (false closes an open wheel).</summary>
    public readonly bool WheelAllowed;

    /// <summary>The traveller hit zone opens the wheel: the wheel is allowed and closed.</summary>
    public readonly bool TravellerLive;

    /// <summary>Creates an output set.</summary>
    public BoothInput(bool desktopInteractive, bool crtFocusable, bool powerButtonLive, bool focusExitLive,
                      bool propsLive, bool papersLive, bool wheelAllowed, bool travellerLive)
    {
        DesktopInteractive = desktopInteractive;
        CrtFocusable = crtFocusable;
        PowerButtonLive = powerButtonLive;
        FocusExitLive = focusExitLive;
        PropsLive = propsLive;
        PapersLive = papersLive;
        WheelAllowed = wheelAllowed;
        TravellerLive = travellerLive;
    }
}

/// <summary>
/// The booth's input table (the physical-desk spec, section 1.9): from the
/// view, its settling, the screen's power, the shift's phase, the wheel and a
/// pending citation slip, which of the desktop, the CRT, the bezel power
/// button, the focus exit, the props, the papers, the wheel and the traveller
/// take input. Pure, so every row is tested headless; BoothCoordinator applies it.
/// </summary>
public static class BoothRules
{
    /// <summary>Evaluates the table for one context.</summary>
    public static BoothInput Evaluate(BoothContext c)
    {
        bool newsletter = c.Phase == BoothPhase.Newsletter;
        bool props = !c.Focused && c.Settled && !newsletter && !c.WheelOpen;
        bool wheel = !c.Focused && c.Settled && c.Phase == BoothPhase.TravellerAtDesk;

        return new BoothInput(
            desktopInteractive: c.Focused && c.Settled && c.ScreenOn && !newsletter,
            crtFocusable: !c.Focused && !newsletter && !c.WheelOpen,
            powerButtonLive: c.Settled && !newsletter && !c.WheelOpen && !c.CitationPending,
            focusExitLive: c.Focused && c.Settled,
            propsLive: props,
            papersLive: props && c.Phase == BoothPhase.TravellerAtDesk,
            wheelAllowed: wheel,
            travellerLive: wheel && !c.WheelOpen);
    }
}
