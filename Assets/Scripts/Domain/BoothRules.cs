/// <summary>Where the shift is, as the booth's input rules need it. Set by GameManager.</summary>
public enum BoothPhase
{
    /// <summary>No traveller at the desk: before the shift, waiting for the AVAILABLE sign or between travellers (the default before Start).</summary>
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

    /// <summary>The camera is tilted forward over the desk (the reading view, piece 10 section 11).</summary>
    public readonly bool DeskView;

    /// <summary>Creates a context.</summary>
    public BoothContext(bool focused, bool screenOn, BoothPhase phase, bool wheelOpen, bool deskView)
    {
        Focused = focused;
        ScreenOn = screenOn;
        Phase = phase;
        WheelOpen = wheelOpen;
        DeskView = deskView;
    }
}

/// <summary>Which of the booth's inputs are live (BoothRules.Evaluate).</summary>
public readonly struct BoothInput
{
    /// <summary>The desktop takes clicks: the frame open, the screen on, no newsletter.</summary>
    public readonly bool DesktopInteractive;

    /// <summary>Clicking the PC opens its frame: the frame closed, no newsletter, the wheel closed.</summary>
    public readonly bool CrtFocusable;

    /// <summary>The power buttons (the frame's and the PC's knob) toggle the screen: no newsletter, the wheel closed.</summary>
    public readonly bool PowerButtonLive;

    /// <summary>The desk props react to clicks: the frame closed, no newsletter, the wheel closed.</summary>
    public readonly bool PropsLive;

    /// <summary>The documents on the desk can be dragged and clicked (Papers, Please's one mouse button: left-drag moves them in either view, the reading view entered by itself when one lands on the desk): as the props, while a traveller is at the desk.</summary>
    public readonly bool PapersLive;

    /// <summary>The wheel may be open: the frame closed while a traveller is at the desk (false closes an open wheel).</summary>
    public readonly bool WheelAllowed;

    /// <summary>The traveller hit zone takes clicks (the wheel, or their face in inspect mode): the wheel is allowed and closed.</summary>
    public readonly bool TravellerLive;

    /// <summary>The stamp bar's tab, TAB and the stamps take input: as the papers (false slides the bar back).</summary>
    public readonly bool StampsLive;

    /// <summary>The red inspect button and SPACE take input: as the papers (false ends inspect mode).</summary>
    public readonly bool InspectLive;

    /// <summary>The office case HUD (the office compare strip) shows: the frame closed while a traveller is at the desk.</summary>
    public readonly bool CaseHudVisible;

    /// <summary>The reading view may stay, and a click on the mat tilts into it and back: as the props (false returns to the normal view: a click on the intercom, the traveller or the PC from the tilted view blends straight up to the wheel or the frame; Saleh 2026-09-30).</summary>
    public readonly bool DeskViewAllowed;

    /// <summary>The "▲ Back" control shows at the top of the office overlay and the mouse wheel rolled up returns from the reading view: the reading view is on and the props are live.</summary>
    public readonly bool DeskViewBackLive;

    /// <summary>The normal view's own ways out take input: the mouse wheel rolled down over the empty mat tilts into the reading view, and the city view may turn (A, the "◀ City" button): the props are live and the view is not tilted.</summary>
    public readonly bool NormalViewLive;

    /// <summary>The PC's grey tab and Q switch to the PC and back: no newsletter, and either the frame is open (back to the desk) or the wheel is closed (to the PC).</summary>
    public readonly bool PcSwitchLive;

    /// <summary>Creates an output set.</summary>
    public BoothInput(bool desktopInteractive, bool crtFocusable, bool powerButtonLive, bool propsLive, bool papersLive, bool wheelAllowed,
                      bool travellerLive, bool stampsLive, bool inspectLive, bool caseHudVisible, bool deskViewAllowed, bool deskViewBackLive,
                      bool normalViewLive, bool pcSwitchLive)
    {
        DesktopInteractive = desktopInteractive;
        CrtFocusable = crtFocusable;
        PowerButtonLive = powerButtonLive;
        PropsLive = propsLive;
        PapersLive = papersLive;
        WheelAllowed = wheelAllowed;
        TravellerLive = travellerLive;
        StampsLive = stampsLive;
        InspectLive = inspectLive;
        CaseHudVisible = caseHudVisible;
        DeskViewAllowed = deskViewAllowed;
        DeskViewBackLive = deskViewBackLive;
        NormalViewLive = normalViewLive;
        PcSwitchLive = pcSwitchLive;
    }
}

/// <summary>
/// The booth's input table (the physical-desk spec section 1.9 as the office
/// move changed it: the PC opens a frame over the office at once, with no
/// camera blend; piece 10 added the reading view; Papers, Please's controls,
/// Saleh 2026-10-06, made the documents one left-drag in either view, the
/// stamps a bar and inspect a mode, and took the hand and its catcher away):
/// from the frame, the screen's power, the shift's phase, the wheel and the
/// reading view, which of the desktop, the PC, the
/// power buttons, the props, the documents, the wheel, the traveller, the
/// stamp bar, inspect mode, the case HUD, the reading view, its "▲ Back"
/// control, the normal view's own ways and the PC switch take input or show.
/// What a right-click or Esc backs out of is ControlRules'. Pure, so every
/// row is tested headless; BoothCoordinator applies it.
/// </summary>
public static class BoothRules
{
    /// <summary>Evaluates the table for one context.</summary>
    public static BoothInput Evaluate(BoothContext c)
    {
        bool newsletter = c.Phase == BoothPhase.Newsletter;
        bool atDesk = c.Phase == BoothPhase.TravellerAtDesk;
        bool desk = !c.Focused && !newsletter && !c.WheelOpen;
        bool office = !c.Focused && atDesk;
        bool papers = desk && atDesk;
        return new BoothInput(
            desktopInteractive: c.Focused && c.ScreenOn && !newsletter,
            crtFocusable: desk,
            powerButtonLive: !newsletter && !c.WheelOpen,
            propsLive: desk,
            papersLive: papers,
            wheelAllowed: office,
            travellerLive: office && !c.WheelOpen,
            stampsLive: papers,
            inspectLive: papers,
            caseHudVisible: office,
            deskViewAllowed: desk,
            deskViewBackLive: c.DeskView && desk,
            normalViewLive: desk && !c.DeskView,
            pcSwitchLive: !newsletter && (c.Focused || !c.WheelOpen));
    }
}
