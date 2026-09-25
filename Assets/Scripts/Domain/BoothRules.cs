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

    /// <summary>The stamp tray (Accept and Deny at the desk, piece 10) is open.</summary>
    public readonly bool StampOpen;

    /// <summary>At least one paper is held in the hand (piece 10).</summary>
    public readonly bool PapersHeld;

    /// <summary>Creates a context.</summary>
    public BoothContext(bool focused, bool screenOn, BoothPhase phase, bool wheelOpen, bool citationPending, bool stampOpen, bool papersHeld)
    {
        Focused = focused;
        ScreenOn = screenOn;
        Phase = phase;
        WheelOpen = wheelOpen;
        CitationPending = citationPending;
        StampOpen = stampOpen;
        PapersHeld = papersHeld;
    }
}

/// <summary>Which of the booth's inputs are live (BoothRules.Evaluate).</summary>
public readonly struct BoothInput
{
    /// <summary>The desktop takes clicks: the frame open, the screen on, no newsletter.</summary>
    public readonly bool DesktopInteractive;

    /// <summary>Clicking the PC opens its frame: in the office view, no newsletter, the wheel and the stamp tray closed.</summary>
    public readonly bool CrtFocusable;

    /// <summary>The power buttons (the frame's and the PC's knob) toggle the screen: no newsletter, the wheel and the stamp tray closed, no pending citation slip.</summary>
    public readonly bool PowerButtonLive;

    /// <summary>The desk props react to clicks: the office view, no newsletter, the wheel and the stamp tray closed.</summary>
    public readonly bool PropsLive;

    /// <summary>The papers on the desk can be dragged and clicked: as the props, while a traveller is at the desk.</summary>
    public readonly bool PapersLive;

    /// <summary>The wheel may be open: the office view while a traveller is at the desk (false closes an open wheel).</summary>
    public readonly bool WheelAllowed;

    /// <summary>The traveller hit zone opens the wheel: the wheel is allowed and closed, the stamp tray closed.</summary>
    public readonly bool TravellerLive;

    /// <summary>Papers held in the hand take clicks (their rows, a put-back): a traveller at the desk, in either view (beside the open frame too), no newsletter, the wheel and the stamp tray closed.</summary>
    public readonly bool HeldPapersLive;

    /// <summary>A click on the desk puts every held paper back: the papers are live and one is held.</summary>
    public readonly bool DeskCatcherLive;

    /// <summary>Escape puts every held paper back: as the desk catcher (Escape closes the frame, the wheel or the stamp tray first).</summary>
    public readonly bool ExamineEscapeLive;

    /// <summary>The stamp tray may be open: the office view while a traveller is at the desk (false closes an open tray).</summary>
    public readonly bool StampTrayAllowed;

    /// <summary>The office case HUD (the claim tag and the office compare strip) shows: the office view while a traveller is at the desk.</summary>
    public readonly bool CaseHudVisible;

    /// <summary>Creates an output set.</summary>
    public BoothInput(bool desktopInteractive, bool crtFocusable, bool powerButtonLive,
                      bool propsLive, bool papersLive, bool wheelAllowed, bool travellerLive,
                      bool heldPapersLive, bool deskCatcherLive, bool examineEscapeLive,
                      bool stampTrayAllowed, bool caseHudVisible)
    {
        DesktopInteractive = desktopInteractive;
        CrtFocusable = crtFocusable;
        PowerButtonLive = powerButtonLive;
        PropsLive = propsLive;
        PapersLive = papersLive;
        WheelAllowed = wheelAllowed;
        TravellerLive = travellerLive;
        HeldPapersLive = heldPapersLive;
        DeskCatcherLive = deskCatcherLive;
        ExamineEscapeLive = examineEscapeLive;
        StampTrayAllowed = stampTrayAllowed;
        CaseHudVisible = caseHudVisible;
    }
}

/// <summary>
/// The booth's input table (the physical-desk spec section 1.9 as the office
/// move changed it: the PC opens a frame over the office at once, with no
/// camera blend; piece 10 adds the stamp tray and papers held in the hand):
/// from the frame, the screen's power, the shift's phase, the wheel, a pending
/// citation slip, the stamp tray and held papers, which of the desktop, the PC,
/// the power buttons, the props, the papers (on the desk and in the hand), the
/// desk catcher, Escape, the wheel, the stamp tray, the traveller and the case
/// HUD take input or show. Pure, so every row is tested headless;
/// BoothCoordinator applies it.
/// </summary>
public static class BoothRules
{
    /// <summary>Evaluates the table for one context.</summary>
    public static BoothInput Evaluate(BoothContext c)
    {
        bool newsletter = c.Phase == BoothPhase.Newsletter;
        bool atDesk = c.Phase == BoothPhase.TravellerAtDesk;
        bool modal = c.WheelOpen || c.StampOpen;
        bool props = !c.Focused && !newsletter && !modal;
        bool office = !c.Focused && atDesk;
        bool papers = props && atDesk;
        bool catcher = papers && c.PapersHeld;
        return new BoothInput(
            desktopInteractive: c.Focused && c.ScreenOn && !newsletter,
            crtFocusable: !c.Focused && !newsletter && !modal,
            powerButtonLive: !newsletter && !modal && !c.CitationPending,
            propsLive: props,
            papersLive: papers,
            wheelAllowed: office,
            travellerLive: office && !modal,
            heldPapersLive: atDesk && !modal,
            deskCatcherLive: catcher,
            examineEscapeLive: catcher,
            stampTrayAllowed: office,
            caseHudVisible: office);
    }
}
