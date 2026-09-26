/// <summary>What the desktop does with an Escape press (the PC redesign section 3.5); None lets the frame close.</summary>
public enum DesktopEscape
{
    /// <summary>The desktop takes nothing: the frame closes (OfficeViewController), then the office's own order.</summary>
    None,

    /// <summary>A context menu is open: it closes.</summary>
    CloseMenu,

    /// <summary>A text field is focused: it is left (the press types nothing and closes nothing).</summary>
    LeaveField,

    /// <summary>The Start menu is open: it closes.</summary>
    CloseStartMenu,

    /// <summary>A window or icon drag is under way: it is cancelled, the window or icon back where it started.</summary>
    CancelDrag
}

/// <summary>The desktop state the Escape rule reads.</summary>
public readonly struct DesktopEscapeState
{
    /// <summary>A context menu is open (the desktop's right-click menu).</summary>
    public readonly bool MenuOpen;

    /// <summary>A text field on the desktop has the keyboard (or had it at the end of the last frame).</summary>
    public readonly bool FieldFocused;

    /// <summary>The Start menu is open.</summary>
    public readonly bool StartMenuOpen;

    /// <summary>A window is being dragged by its title bar, or an icon across the desktop.</summary>
    public readonly bool Dragging;

    /// <summary>Creates a state.</summary>
    public DesktopEscapeState(bool menuOpen, bool fieldFocused, bool startMenuOpen, bool dragging)
    {
        MenuOpen = menuOpen;
        FieldFocused = fieldFocused;
        StartMenuOpen = startMenuOpen;
        Dragging = dragging;
    }
}

/// <summary>
/// The desktop's part of the one Escape chain (the PC redesign KB3, section
/// 3.5; the desktop's share of audit R5-005): one press does one thing, the
/// first of: close a context menu, leave a focused field, close the Start
/// menu, cancel a drag, else None. The desktop's poller takes the press when
/// the rule returns something and stamps the frame, so the PC frame's own
/// Escape (OfficeViewController) skips that press. Later phases add the
/// shortcut card and the search panel at their places in the chain. Pure.
/// </summary>
public static class DesktopEscapeRule
{
    /// <summary>The first thing Escape does on the desktop in this state.</summary>
    public static DesktopEscape Resolve(DesktopEscapeState state)
    {
        if (state.MenuOpen)
            return DesktopEscape.CloseMenu;
        if (state.FieldFocused)
            return DesktopEscape.LeaveField;
        if (state.StartMenuOpen)
            return DesktopEscape.CloseStartMenu;
        if (state.Dragging)
            return DesktopEscape.CancelDrag;
        return DesktopEscape.None;
    }
}
