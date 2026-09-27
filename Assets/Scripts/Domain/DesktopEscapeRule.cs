/// <summary>What the desktop does with an Escape press (the PC redesign section 3.5), in the chain's order; None lets the frame close. Not serialized.</summary>
public enum DesktopEscape
{
    /// <summary>The desktop takes nothing: the frame closes (OfficeViewController), then the office's own order.</summary>
    None,

    /// <summary>A context menu is open: it closes.</summary>
    CloseMenu,

    /// <summary>The shortcut card (F1) is open: it closes.</summary>
    CloseCard,

    /// <summary>The search results panel is open: it closes (the panel comes with search, redesign phase 19).</summary>
    CloseResults,

    /// <summary>The search field has the keyboard and holds text (or a pasted chip): it is cleared, and keeps the keyboard.</summary>
    ClearSearch,

    /// <summary>A text field is focused: it is left (focus returns to its pane; the press types nothing and closes nothing).</summary>
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

    /// <summary>The shortcut card is open.</summary>
    public readonly bool CardOpen;

    /// <summary>The search results panel is open.</summary>
    public readonly bool ResultsOpen;

    /// <summary>The Investigation app's search field has the keyboard.</summary>
    public readonly bool SearchFocused;

    /// <summary>The search field holds text or a pasted chip.</summary>
    public readonly bool SearchHasText;

    /// <summary>A text field on the desktop has the keyboard (the search field included).</summary>
    public readonly bool FieldFocused;

    /// <summary>The Start menu is open.</summary>
    public readonly bool StartMenuOpen;

    /// <summary>A window is being dragged by its title bar, or an icon across the desktop.</summary>
    public readonly bool Dragging;

    /// <summary>Creates a state.</summary>
    public DesktopEscapeState(bool menuOpen, bool cardOpen, bool resultsOpen, bool searchFocused, bool searchHasText, bool fieldFocused, bool startMenuOpen,
                              bool dragging)
    {
        MenuOpen = menuOpen;
        CardOpen = cardOpen;
        ResultsOpen = resultsOpen;
        SearchFocused = searchFocused;
        SearchHasText = searchHasText;
        FieldFocused = fieldFocused;
        StartMenuOpen = startMenuOpen;
        Dragging = dragging;
    }
}

/// <summary>
/// The desktop's part of the one Escape chain (the PC redesign KB3, section
/// 3.5; the desktop's share of audit R5-005): one press does one thing, the
/// first of: close a context menu, close the shortcut card, close the search
/// results, clear the focused search field's text, leave a focused field,
/// close the Start menu, cancel a drag, else None. The desktop's keyboard
/// poller (DesktopKeyboard) takes the press when the rule returns something
/// and stamps the frame, so the PC frame's own Escape (OfficeViewController)
/// skips that press; on None the frame closes and the office's order goes on.
/// Pure.
/// </summary>
public static class DesktopEscapeRule
{
    /// <summary>The first thing Escape does on the desktop in this state.</summary>
    public static DesktopEscape Resolve(DesktopEscapeState state)
    {
        if (state.MenuOpen)
            return DesktopEscape.CloseMenu;
        if (state.CardOpen)
            return DesktopEscape.CloseCard;
        if (state.ResultsOpen)
            return DesktopEscape.CloseResults;
        if (state.SearchFocused && state.SearchHasText)
            return DesktopEscape.ClearSearch;
        if (state.FieldFocused)
            return DesktopEscape.LeaveField;
        if (state.StartMenuOpen)
            return DesktopEscape.CloseStartMenu;
        if (state.Dragging)
            return DesktopEscape.CancelDrag;
        return DesktopEscape.None;
    }
}
