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

    /// <summary>A citation slip waits for Acknowledge (the traveller has left; the slip holds the clock and the screen).</summary>
    public readonly bool CitationPending;

    /// <summary>A desk stamp is in the hand (the desk-first redesign, item 12: the APPROVED or DENIED stamp picked up from the stamp tray).</summary>
    public readonly bool StampHeld;

    /// <summary>At least one paper is held in the hand (piece 10).</summary>
    public readonly bool PapersHeld;

    /// <summary>The camera is tilted forward over the desk (the desk view, piece 10 section 11).</summary>
    public readonly bool DeskView;

    /// <summary>The desk view exists (the art office has a Cinemachine camera to tilt): without it the papers on the desk move in the normal view (false in the default context).</summary>
    public readonly bool DeskViewBound;

    /// <summary>Creates a context.</summary>
    public BoothContext(bool focused, bool screenOn, BoothPhase phase, bool wheelOpen, bool citationPending, bool stampHeld, bool papersHeld, bool deskView, bool deskViewBound)
    {
        Focused = focused;
        ScreenOn = screenOn;
        Phase = phase;
        WheelOpen = wheelOpen;
        CitationPending = citationPending;
        StampHeld = stampHeld;
        PapersHeld = papersHeld;
        DeskView = deskView;
        DeskViewBound = deskViewBound;
    }
}

/// <summary>Which of the booth's inputs are live (BoothRules.Evaluate).</summary>
public readonly struct BoothInput
{
    /// <summary>The desktop takes clicks: the frame open, the screen on, no newsletter.</summary>
    public readonly bool DesktopInteractive;

    /// <summary>Clicking the PC opens its frame: in the office view, no newsletter, the wheel closed, no stamp in the hand.</summary>
    public readonly bool CrtFocusable;

    /// <summary>The power buttons (the frame's and the PC's knob) toggle the screen: no newsletter, the wheel closed, no pending citation slip.</summary>
    public readonly bool PowerButtonLive;

    /// <summary>The desk props react to clicks: the office view, no newsletter, the wheel closed, no stamp in the hand.</summary>
    public readonly bool PropsLive;

    /// <summary>The papers on the desk can be dragged and clicked: the office view, no newsletter, the wheel closed (a stamp in the hand presses on them), while a traveller is at the desk and the camera is tilted over the desk (Saleh 2026-09-30: "moving papers should only be possible when camera is tilted"; a click on a paper in the normal view reaches the mat under it and tilts in); in the normal view only when there is no desk view.</summary>
    public readonly bool PapersLive;

    /// <summary>The wheel may be open: the office view while a traveller is at the desk (false closes an open wheel).</summary>
    public readonly bool WheelAllowed;

    /// <summary>The traveller hit zone opens the wheel: the wheel is allowed and closed, no stamp in the hand.</summary>
    public readonly bool TravellerLive;

    /// <summary>Papers held in the hand take clicks (their rows, a put-back): a traveller at the desk, in either view (beside the open frame too), no newsletter, the wheel closed.</summary>
    public readonly bool HeldPapersLive;

    /// <summary>A click on the desk puts every held paper back: as the props, while a traveller is at the desk and a paper is held (in either view).</summary>
    public readonly bool DeskCatcherLive;

    /// <summary>Escape puts every held paper back: as the desk catcher (Escape closes the frame or the wheel first, and puts a held stamp down).</summary>
    public readonly bool ExamineEscapeLive;

    /// <summary>The stamp tray's stamps and ink pad take clicks: the office view while a traveller is at the desk and the wheel is closed (false puts a held stamp down; the tray itself may stay out).</summary>
    public readonly bool StampsLive;

    /// <summary>The office case HUD (the claim tag and the office compare strip) shows: the office view while a traveller is at the desk.</summary>
    public readonly bool CaseHudVisible;

    /// <summary>A click on the mat tilts the camera into the desk view and back: the props are live and no paper is held (with papers held the desk catcher takes the click).</summary>
    public readonly bool DeskViewToggleLive;

    /// <summary>Escape and a right-click on empty space return from the desk view: the desk view is on and the mat's toggle is live (Escape closes the frame or the wheel, puts a held stamp down and puts held papers back first).</summary>
    public readonly bool DeskViewReturnLive;

    /// <summary>The desk view may stay: no newsletter, the wheel closed and the PC frame closed (false returns to the normal view: a click on the intercom, the traveller or the PC from the tilted view blends straight up to the wheel or the frame; Saleh 2026-09-30).</summary>
    public readonly bool DeskViewAllowed;

    /// <summary>The "▲ Back" control shows at the top of the office overlay and the mouse wheel rolled up returns from the desk view: the desk view is on and the props are live (no frame, newsletter, wheel or stamp in the hand), papers held or not.</summary>
    public readonly bool DeskViewBackLive;

    /// <summary>The mouse wheel rolled down over the empty mat tilts into the desk view: the view is normal and the mat's toggle is live (the pointer must be on the mat, not on UI: DeskView checks it).</summary>
    public readonly bool DeskViewScrollInLive;

    /// <summary>
    /// A paper held in the hand can be dragged out onto the desk: held papers
    /// are live and so are the papers on the desk (a drag-out drops the paper
    /// there). Never beside the open frame, where held papers take clicks but a
    /// press-and-move on one starts no drag, so it stays held (audit R5-001).
    /// </summary>
    public readonly bool HeldDragOutLive;

    /// <summary>Creates an output set.</summary>
    public BoothInput(bool desktopInteractive, bool crtFocusable, bool powerButtonLive,
                      bool propsLive, bool papersLive, bool wheelAllowed, bool travellerLive,
                      bool heldPapersLive, bool deskCatcherLive, bool examineEscapeLive,
                      bool stampsLive, bool caseHudVisible,
                      bool deskViewToggleLive, bool deskViewReturnLive, bool deskViewAllowed,
                      bool deskViewBackLive, bool deskViewScrollInLive, bool heldDragOutLive)
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
        StampsLive = stampsLive;
        CaseHudVisible = caseHudVisible;
        DeskViewToggleLive = deskViewToggleLive;
        DeskViewReturnLive = deskViewReturnLive;
        DeskViewAllowed = deskViewAllowed;
        DeskViewBackLive = deskViewBackLive;
        DeskViewScrollInLive = deskViewScrollInLive;
        HeldDragOutLive = heldDragOutLive;
    }
}

/// <summary>
/// The booth's input table (the physical-desk spec section 1.9 as the office
/// move changed it: the PC opens a frame over the office at once, with no
/// camera blend; piece 10 adds papers held in the hand and the desk view; the
/// desk-first redesign makes the stamps physical: a stamp in the hand is a
/// tool, not a modal tray): from the frame, the screen's power, the shift's
/// phase, the wheel, a pending citation slip, a stamp in the hand, held
/// papers, the desk view and whether there is one, which of the desktop, the
/// PC, the power buttons, the props, the papers (on the desk and in the
/// hand), the desk catcher, Escape, the wheel, the stamps, the traveller, the
/// case HUD, the mat, the desk view's
/// return, its "▲ Back" control, the mouse wheel and a held paper's drag out
/// of the hand take input or show. Pure,
/// so every row is tested headless;
/// BoothCoordinator applies it.
/// </summary>
public static class BoothRules
{
    /// <summary>Evaluates the table for one context.</summary>
    public static BoothInput Evaluate(BoothContext c)
    {
        bool newsletter = c.Phase == BoothPhase.Newsletter;
        bool atDesk = c.Phase == BoothPhase.TravellerAtDesk;
        bool modal = c.WheelOpen;
        bool desk = !c.Focused && !newsletter && !modal;
        bool props = desk && !c.StampHeld;
        bool office = !c.Focused && atDesk;
        bool papers = desk && atDesk && (c.DeskView || !c.DeskViewBound);
        bool catcher = props && atDesk && c.PapersHeld;
        bool mat = props && !c.PapersHeld;
        bool held = atDesk && !modal;
        return new BoothInput(
            desktopInteractive: c.Focused && c.ScreenOn && !newsletter,
            crtFocusable: props,
            powerButtonLive: !newsletter && !modal && !c.CitationPending,
            propsLive: props,
            papersLive: papers,
            wheelAllowed: office,
            travellerLive: office && !modal && !c.StampHeld,
            heldPapersLive: held,
            deskCatcherLive: catcher,
            examineEscapeLive: catcher,
            stampsLive: office && !modal,
            caseHudVisible: office,
            deskViewToggleLive: mat,
            deskViewReturnLive: c.DeskView && mat,
            deskViewAllowed: !newsletter && !c.WheelOpen && !c.Focused,
            deskViewBackLive: c.DeskView && props,
            deskViewScrollInLive: mat && !c.DeskView,
            heldDragOutLive: held && papers);
    }
}
