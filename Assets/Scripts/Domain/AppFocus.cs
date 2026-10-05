/// <summary>The Investigation app's keyboard regions (the PC redesign KB4; the PC workbench spec section 7), in Tab's order.</summary>
public enum AppRegion
{
    /// <summary>The search drawer's field (only while the drawer is open).</summary>
    Search,

    /// <summary>The search drawer's hits (only while it lists some; down from the search field comes here too).</summary>
    Results,

    /// <summary>The menu bar's titles and the open menu's rows.</summary>
    Shelf,

    /// <summary>The target pane's values.</summary>
    PaneContent,

    /// <summary>The other pane's values (only while two panes show).</summary>
    OtherPane,

    /// <summary>The findings column's entries (only while it shows).</summary>
    Findings,

    /// <summary>The status line's Cancel (only while a value is held).</summary>
    Holding
}

/// <summary>What the app shows, as the regions read it.</summary>
public readonly struct AppFocusState
{
    /// <summary>A state.</summary>
    public AppFocusState(bool drawer = false, bool results = false, bool split = false, bool findings = true, bool holding = false)
    {
        Drawer = drawer;
        Results = results;
        Split = split;
        Findings = findings;
        Holding = holding;
    }

    /// <summary>The search drawer is open: only its field and hits take the ring (it covers the rest).</summary>
    public bool Drawer { get; }

    /// <summary>The drawer lists hits.</summary>
    public bool Results { get; }

    /// <summary>Two panes show.</summary>
    public bool Split { get; }

    /// <summary>The findings column shows.</summary>
    public bool Findings { get; }

    /// <summary>A value is held.</summary>
    public bool Holding { get; }
}

/// <summary>
/// Tab and Shift+Tab through the Investigation app's regions (the PC
/// redesign KB4; the PC workbench spec section 7): with the search drawer
/// open, its field and its hits; otherwise the menu bar, the target pane's
/// values, the other pane's, the findings and the held value's Cancel, round
/// again (the PC only investigates: no Accept or Deny since the clean-up of
/// 2026-10-05). A region that is not there is skipped: the hits while none is
/// listed, the other pane with one, the findings while hidden, Cancel with
/// nothing held. Pure; the app moves its focus ring with it.
/// </summary>
public static class AppFocus
{
    private const int Count = (int)AppRegion.Holding + 1;

    /// <summary>True when <paramref name="region"/> is there in <paramref name="state"/> (see the class summary).</summary>
    public static bool Available(AppRegion region, AppFocusState state)
    {
        if (state.Drawer)
            return region == AppRegion.Search || (region == AppRegion.Results && state.Results);
        switch (region)
        {
            case AppRegion.Search:
            case AppRegion.Results:
                return false;
            case AppRegion.OtherPane:
                return state.Split;
            case AppRegion.Findings:
                return state.Findings;
            case AppRegion.Holding:
                return state.Holding;
            default:
                return true;
        }
    }

    /// <summary>The region after <paramref name="from"/> (<paramref name="direction"/> 1) or before it (-1) that is there, wrapping round; <paramref name="from"/> itself when nothing else is.</summary>
    public static AppRegion Next(AppRegion from, AppFocusState state, int direction)
    {
        int step = direction < 0 ? -1 : 1;
        int at = (int)from;
        for (int i = 0; i < Count; i++)
        {
            at = ((at + step) % Count + Count) % Count;
            if (Available((AppRegion)at, state))
                return (AppRegion)at;
        }
        return from;
    }

    /// <summary>Where the ring starts (Tab's first press, or its region gone): the search field while the drawer is open, else the target pane's values.</summary>
    public static AppRegion Home(AppFocusState state) =>
        state.Drawer ? AppRegion.Search : AppRegion.PaneContent;
}
