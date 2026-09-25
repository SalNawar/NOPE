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
    /// <summary>The PC's frame is open (MonitorFocus).</summary>
    public readonly bool Focused;

    /// <summary>The PC's screen is on.</summary>
    public readonly bool ScreenOn;

    /// <summary>Where the shift is.</summary>
    public readonly BoothPhase Phase;

    /// <summary>The traveller wheel is open.</summary>
    public readonly bool WheelOpen;

    /// <summary>A citation slip waits for Acknowledge (the traveller has left; the slip holds the clock and the screen).</summary>
    public readonly bool CitationPending;

    /// <summary>Creates a context.</summary>
    public BoothContext(bool focused, bool screenOn, BoothPhase phase, bool wheelOpen, bool citationPending)
    {
        Focused = focused;
        ScreenOn = screenOn;
        Phase = phase;
        WheelOpen = wheelOpen;
        CitationPending = citationPending;
    }
}

/// <summary>Which of the booth's inputs are live (BoothRules.Evaluate).</summary>
public readonly struct BoothInput
{
    /// <summary>The desktop takes clicks: the frame open, the screen on, no newsletter.</summary>
    public readonly bool DesktopInteractive;

    /// <summary>Clicking the PC opens its frame: in the office view, no newsletter, the wheel closed.</summary>
    public readonly bool CrtFocusable;

    /// <summary>The power buttons (the frame's and the PC's knob) toggle the screen: no newsletter, the wheel closed, no pending citation slip.</summary>
    public readonly bool PowerButtonLive;

    /// <summary>The desk props react to clicks: the office view, no newsletter, the wheel closed.</summary>
    public readonly bool PropsLive;

    /// <summary>The papers can be dragged and clicked: as the props, while a traveller is at the desk.</summary>
    public readonly bool PapersLive;

    /// <summary>The wheel may be open: the office view while a traveller is at the desk (false closes an open wheel).</summary>
    public readonly bool WheelAllowed;

    /// <summary>The traveller hit zone opens the wheel: the wheel is allowed and closed.</summary>
    public readonly bool TravellerLive;

    /// <summary>Creates an output set.</summary>
    public BoothInput(bool desktopInteractive, bool crtFocusable, bool powerButtonLive,
                      bool propsLive, bool papersLive, bool wheelAllowed, bool travellerLive)
    {
        DesktopInteractive = desktopInteractive;
        CrtFocusable = crtFocusable;
        PowerButtonLive = powerButtonLive;
        PropsLive = propsLive;
        PapersLive = papersLive;
        WheelAllowed = wheelAllowed;
        TravellerLive = travellerLive;
    }
}

/// <summary>
/// The booth's input table (the physical-desk spec section 1.9 as the office
/// move changed it: the PC opens a frame over the office at once, with no
/// camera blend): from the frame, the screen's power, the shift's phase, the
/// wheel and a pending citation slip, which of the desktop, the PC, the power
/// buttons, the props, the papers, the wheel and the traveller take input.
/// Pure, so every row is tested headless; BoothCoordinator applies it.
/// </summary>
public static class BoothRules
{
    /// <summary>Evaluates the table for one context.</summary>
    public static BoothInput Evaluate(BoothContext c)
    {
        bool newsletter = c.Phase == BoothPhase.Newsletter;
        bool props = !c.Focused && !newsletter && !c.WheelOpen;
        bool wheel = !c.Focused && c.Phase == BoothPhase.TravellerAtDesk;
        return new BoothInput(
            desktopInteractive: c.Focused && c.ScreenOn && !newsletter,
            crtFocusable: !c.Focused && !newsletter && !c.WheelOpen,
            powerButtonLive: !newsletter && !c.WheelOpen && !c.CitationPending,
            propsLive: props,
            papersLive: props && c.Phase == BoothPhase.TravellerAtDesk,
            wheelAllowed: wheel,
            travellerLive: wheel && !c.WheelOpen);
    }
}
