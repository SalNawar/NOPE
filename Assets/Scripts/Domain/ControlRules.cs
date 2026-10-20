using System.Collections.Generic;

/// <summary>What the office is in, as the one input model reads it (OfficeControls builds it each press).</summary>
public readonly struct ControlState
{
    /// <summary>The PC's frame is open.</summary>
    public readonly bool PcOpen;

    /// <summary>The morning briefing or the shift report is up (the office takes no keys).</summary>
    public readonly bool Newsletter;

    /// <summary>A traveller is at the desk.</summary>
    public readonly bool TravellerAtDesk;

    /// <summary>The traveller wheel is open.</summary>
    public readonly bool WheelOpen;

    /// <summary>A value is held on the workbench (the first of a comparison).</summary>
    public readonly bool ValueHeld;

    /// <summary>Inspect mode is on.</summary>
    public readonly bool Inspecting;

    /// <summary>The view looks at the city.</summary>
    public readonly bool CityView;

    /// <summary>The view is tilted over the desk (the reading view).</summary>
    public readonly bool DeskView;

    /// <summary>Something is held: a document, the rulebook or a stamp being dragged, or any other hold (HoldRegistry.IsHolding).</summary>
    public readonly bool Dragging;

    /// <summary>A text field on the PC has the keyboard (the PC key is typed there).</summary>
    public readonly bool TextFieldFocused;

    /// <summary>A state; every flag defaults to false.</summary>
    public ControlState(bool pcOpen = false, bool newsletter = false, bool travellerAtDesk = false, bool wheelOpen = false, bool valueHeld = false,
                        bool inspecting = false, bool cityView = false, bool deskView = false, bool dragging = false,
                        bool textFieldFocused = false)
    {
        PcOpen = pcOpen;
        Newsletter = newsletter;
        TravellerAtDesk = travellerAtDesk;
        WheelOpen = wheelOpen;
        ValueHeld = valueHeld;
        Inspecting = inspecting;
        CityView = cityView;
        DeskView = deskView;
        Dragging = dragging;
        TextFieldFocused = textFieldFocused;
    }
}

/// <summary>The office's keys (OfficeControls maps the Input System's keys onto these). Not serialized.</summary>
public enum ControlKey
{
    /// <summary>SPACE: inspect mode on or off.</summary>
    Inspect,

    /// <summary>TAB: to the stamp rack (the reading view over it) and back up.</summary>
    Stamps,

    /// <summary>Q: to the PC and back.</summary>
    Pc,

    /// <summary>A or the left arrow: look at the city.</summary>
    CityLeft,

    /// <summary>D or the right arrow: back from the city.</summary>
    CityRight,

    /// <summary>F1: the keys (the PC's shortcut card).</summary>
    Help
}

/// <summary>What an office key does (ControlRules.Resolve). Not serialized.</summary>
public enum ControlAction
{
    /// <summary>Nothing.</summary>
    None,

    /// <summary>Inspect mode on or off.</summary>
    ToggleInspect,

    /// <summary>The reading view over the stamp rack, or back up to the office (the desk layout spec §2: the rack is fixed beside the counter).</summary>
    ToggleStamps,

    /// <summary>Switch to the PC.</summary>
    OpenPc,

    /// <summary>Switch back from the PC to the desk.</summary>
    ClosePc,

    /// <summary>Turn to the city.</summary>
    LookAtCity,

    /// <summary>Turn back from the city.</summary>
    LeaveCity,

    /// <summary>Show the keys: the PC with its shortcut card.</summary>
    ShowKeys,

    /// <summary>Let go of what is held (HoldRegistry.Cancel: back where it was picked up, nothing committed) and do nothing else: a view key pressed while holding (BUG-55).</summary>
    CancelHold
}

/// <summary>What one right-click or Esc backs out of (ControlRules.BackOut). Not serialized.</summary>
public enum BackOutStep
{
    /// <summary>Nothing to back out of.</summary>
    None,

    /// <summary>A document being dragged goes back to where it was picked up; a stamp being dragged goes back to the rack unpressed.</summary>
    CancelDrag,

    /// <summary>The traveller wheel closes.</summary>
    CloseWheel,

    /// <summary>The PC closes back to the desk (once the PC's own chain had nothing to close: DesktopEscapeRule).</summary>
    ClosePc,

    /// <summary>The value held on the workbench is let go.</summary>
    DropValue,

    /// <summary>Inspect mode ends.</summary>
    LeaveInspect,

    /// <summary>The view turns back from the city.</summary>
    LeaveCity,

    /// <summary>The view tilts back up from the desk.</summary>
    LeaveDeskView
}

/// <summary>
/// The one input model of the office (Saleh 2026-10-06: "copy the controls of
/// Papers, Please 1:1 ... the controls are inconsistent: I should hold to
/// pick and sometimes right click to cancel but not all actions like
/// matching"). Left-click always acts (drag a document or a stamp, a tab,
/// a button, a value in inspect mode); right-click and Esc always back out of
/// the innermost mode, one per press, in one order everywhere (BackOut): a
/// drag, the wheel, the PC (after its own chain: a menu, the card, the
/// search, a held value...), the value held, inspect mode, the city view,
/// the reading view. The keys (Resolve): SPACE toggles inspect mode and TAB
/// the reading view over the stamp rack from every desk state (the PC keeps them for
/// its own rows and regions), Q switches to the PC and back (not while a PC
/// text field types it), A/D look at the city and back, F1 shows the keys.
/// While something is held (BUG-55: a paper, the folder, a dater, a tool), a
/// key that changes the view (TAB, Q, A, D, F1) first lets go of it and does
/// nothing else, as a right-click or Esc does: no view ever changes under a
/// held thing, and nothing held is committed by a key.
/// DeskCard is the F1 card's desk section, printed from the same constants
/// as the tabs' hints. Pure; tested headless; OfficeControls applies it.
/// </summary>
public static class ControlRules
{
    /// <summary>The inspect key as printed on the red button and the card.</summary>
    public const string InspectKey = "SPACE";

    /// <summary>The stamp rack's key as printed on its grey tab and the card.</summary>
    public const string StampsKey = "TAB";

    /// <summary>The PC's key as printed on its grey tab and the card.</summary>
    public const string PcKey = "Q";

    /// <summary>The city view's key as printed on its pull tab's keycap.</summary>
    public const string CityKey = "A";

    /// <summary>The way back from the city view as printed on its pull tab's keycap.</summary>
    public const string CityBackKey = "D";

    /// <summary>The back-out as printed on the card and the hints.</summary>
    public const string BackKeys = "Right-click, Esc";

    /// <summary>The F1 card's desk section: the keys as printed and what they do (ui strings); no PC command (ShortcutMap.Card lists those).</summary>
    public static readonly IReadOnlyList<ShortcutCardRow> DeskCard = new[]
    {
        new ShortcutCardRow("Left-drag", "keys.desk.drag"),
        new ShortcutCardRow(InspectKey, "keys.desk.inspect"),
        new ShortcutCardRow(StampsKey, "keys.desk.stamps"),
        new ShortcutCardRow(PcKey, "keys.desk.pc"),
        new ShortcutCardRow(BackKeys, "keys.desk.back"),
        new ShortcutCardRow(CityKey + ", " + CityBackKey, "keys.desk.city"),
    };

    /// <summary>What <paramref name="key"/> does in <paramref name="s"/>: nothing under a newsletter; while something is held, a view key (TAB, Q, A, D, F1) lets go of it (CancelHold) and SPACE does as ever; on the PC only Q (back to the desk, unless a text field types it); at the desk SPACE and TAB with a traveller there, Q to the PC, A/D the city, F1 the keys.</summary>
    public static ControlAction Resolve(ControlKey key, ControlState s)
    {
        if (s.Newsletter)
            return ControlAction.None;
        if (s.Dragging && ChangesView(key))
            return ControlAction.CancelHold;
        if (s.PcOpen)
            return key == ControlKey.Pc && !s.TextFieldFocused ? ControlAction.ClosePc : ControlAction.None;
        switch (key)
        {
            case ControlKey.Inspect: return s.TravellerAtDesk ? ControlAction.ToggleInspect : ControlAction.None;
            case ControlKey.Stamps: return s.TravellerAtDesk ? ControlAction.ToggleStamps : ControlAction.None;
            case ControlKey.Pc: return ControlAction.OpenPc;
            case ControlKey.CityLeft: return s.CityView ? ControlAction.None : ControlAction.LookAtCity;
            case ControlKey.CityRight: return s.CityView ? ControlAction.LeaveCity : ControlAction.None;
            case ControlKey.Help: return ControlAction.ShowKeys;
            default: return ControlAction.None;
        }
    }

    /// <summary>True for a key that changes the view (TAB, Q, A, D, F1): pressed while holding, it lets go first (BUG-55).</summary>
    public static bool ChangesView(ControlKey key) => key != ControlKey.Inspect;

    /// <summary>The one thing a right-click or Esc backs out of in <paramref name="s"/>: the innermost mode first (a drag, the wheel, the PC, the held value, inspect mode, the city, the reading view); nothing under a newsletter.</summary>
    public static BackOutStep BackOut(ControlState s)
    {
        if (s.Newsletter)
            return BackOutStep.None;
        if (s.Dragging)
            return BackOutStep.CancelDrag;
        if (s.WheelOpen)
            return BackOutStep.CloseWheel;
        if (s.PcOpen)
            return BackOutStep.ClosePc;
        if (s.ValueHeld)
            return BackOutStep.DropValue;
        if (s.Inspecting)
            return BackOutStep.LeaveInspect;
        if (s.CityView)
            return BackOutStep.LeaveCity;
        if (s.DeskView)
            return BackOutStep.LeaveDeskView;
        return BackOutStep.None;
    }
}
