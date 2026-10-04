/// <summary>The Investigation app's keyboard regions (the PC redesign KB4; the PC workbench spec section 7), in Tab's order.</summary>
public enum AppRegion
{
    /// <summary>The search drawer's field (only while the drawer is open).</summary>
    Search,

    /// <summary>The search drawer's hits (only while it lists some; down from the search field comes here too).</summary>
    Results,

    /// <summary>The guided steps in the header.</summary>
    Steps,

    /// <summary>The shelf's documents.</summary>
    Shelf,

    /// <summary>The target pane's values.</summary>
    PaneContent,

    /// <summary>The other pane's values (only while two panes show).</summary>
    OtherPane,

    /// <summary>The findings column's entries (only while it shows).</summary>
    Findings,

    /// <summary>The status line's Cancel (only while a value is held).</summary>
    Holding,

    /// <summary>Accept and Deny (only at the decision, with a traveller at the desk).</summary>
    Decision
}

/// <summary>What the app shows, as the regions read it.</summary>
public readonly struct AppFocusState
{
    /// <summary>A state.</summary>
    public AppFocusState(bool drawer = false, bool results = false, bool split = false, bool findings = true, bool caseOn = false, bool holding = false,
                         bool deciding = false)
    {
        Drawer = drawer;
        Results = results;
        Split = split;
        Findings = findings;
        CaseOn = caseOn;
        Holding = holding;
        Deciding = deciding;
    }

    /// <summary>The search drawer is open: only its field and hits take the ring (it covers the rest).</summary>
    public bool Drawer { get; }

    /// <summary>The drawer lists hits.</summary>
    public bool Results { get; }

    /// <summary>Two panes show.</summary>
    public bool Split { get; }

    /// <summary>The findings column shows.</summary>
    public bool Findings { get; }

    /// <summary>A traveller is at the desk.</summary>
    public bool CaseOn { get; }

    /// <summary>A value is held.</summary>
    public bool Holding { get; }

    /// <summary>The decision step shows (in place of the panes).</summary>
    public bool Deciding { get; }
}

/// <summary>
/// Tab and Shift+Tab through the Investigation app's regions (the PC
/// redesign KB4; the PC workbench spec section 7): with the search drawer
/// open, its field and its hits; otherwise the steps, the shelf, the target
/// pane's values, the other pane's, the findings, the held value's Cancel,
/// then Accept and Deny, round again. A region that is not there is skipped:
/// the hits while none is listed, the panes at the decision, the other pane
/// with one, the findings while hidden, Cancel with nothing held, the
/// decision before its step or with no traveller. Pure; the app moves its
/// focus ring with it.
/// </summary>
public static class AppFocus
{
    private const int Count = (int)AppRegion.Decision + 1;

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
            case AppRegion.PaneContent:
                return !state.Deciding;
            case AppRegion.OtherPane:
                return state.Split && !state.Deciding;
            case AppRegion.Findings:
                return state.Findings;
            case AppRegion.Holding:
                return state.Holding;
            case AppRegion.Decision:
                return state.CaseOn && state.Deciding;
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

    /// <summary>Where the ring starts (Tab's first press, or its region gone): the search field while the drawer is open, else the target pane's values (the steps at the decision).</summary>
    public static AppRegion Home(AppFocusState state) =>
        state.Drawer ? AppRegion.Search : state.Deciding ? AppRegion.Steps : AppRegion.PaneContent;
}
