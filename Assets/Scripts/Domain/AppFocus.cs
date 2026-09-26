/// <summary>The Investigation app's keyboard regions, in Tab order (the PC redesign KB4). Not serialized.</summary>
public enum AppRegion
{
    /// <summary>The toolbar's search field.</summary>
    Search,

    /// <summary>The active pane's tab strip.</summary>
    TabStrip,

    /// <summary>The active pane's header: its item chips and its pin button.</summary>
    PaneHeader,

    /// <summary>The active pane's content: the view's rows.</summary>
    PaneContent,

    /// <summary>The other pane (only while the app is split).</summary>
    OtherPane,

    /// <summary>The sidebar's pins and recent items (only while it shows).</summary>
    Sidebar,

    /// <summary>The compare dock (only while a traveller is at the desk).</summary>
    Dock,

    /// <summary>Accept and Deny (only while a traveller is at the desk).</summary>
    Decision
}

/// <summary>
/// Tab and Shift+Tab through the Investigation app's regions (the PC
/// redesign KB4): search, the active pane's tab strip, its header, its
/// content, the other pane, the sidebar, the dock, then Accept/Deny, round
/// again. A region that is not there is skipped: the other pane without the
/// split, the sidebar while it is hidden, the dock and the decision with no
/// traveller at the desk. Pure; the app moves its focus ring with it.
/// </summary>
public static class AppFocus
{
    private const int Count = (int)AppRegion.Decision + 1;

    /// <summary>True when the region is there: the other pane needs the split, the sidebar showing, the dock and the decision a case.</summary>
    public static bool Available(AppRegion region, bool split, bool sidebar, bool caseOn)
    {
        switch (region)
        {
            case AppRegion.OtherPane: return split;
            case AppRegion.Sidebar: return sidebar;
            case AppRegion.Dock:
            case AppRegion.Decision: return caseOn;
            default: return true;
        }
    }

    /// <summary>The region after <paramref name="from"/> (<paramref name="direction"/> 1) or before it (-1) that is there, wrapping round.</summary>
    public static AppRegion Next(AppRegion from, bool split, bool sidebar, bool caseOn, int direction)
    {
        int step = direction < 0 ? -1 : 1;
        int at = (int)from;
        for (int i = 0; i < Count; i++)
        {
            at = ((at + step) % Count + Count) % Count;
            if (Available((AppRegion)at, split, sidebar, caseOn))
                return (AppRegion)at;
        }
        return AppRegion.Search;
    }
}
