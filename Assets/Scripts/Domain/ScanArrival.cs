/// <summary>
/// What a finished scan does in the Investigation app (the PC redesign WN5:
/// nothing steals the view; a scan is the only thing that opens by itself).
/// The app window opens only when it is closed (a minimised one stays down);
/// the scanned paper shows in the Documents view only when that view shows
/// no paper yet, so it never replaces the paper the player reads, and the tab
/// never switches; the Documents tab is badged unless the player sees it; a
/// toast always names the paper. Pure; InvestigationApp applies it.
/// </summary>
public readonly struct ScanArrival
{
    private ScanArrival(bool openApp, bool showPaper, bool badgeDocuments)
    {
        OpenApp = openApp;
        ShowPaper = showPaper;
        BadgeDocuments = badgeDocuments;
    }

    /// <summary>Open the app window (it was closed).</summary>
    public bool OpenApp { get; }

    /// <summary>Show the scanned paper in the Documents view (it showed none).</summary>
    public bool ShowPaper { get; }

    /// <summary>Badge the Documents tab (the player does not see it).</summary>
    public bool BadgeDocuments { get; }

    /// <summary>Show the toast naming the paper (always).</summary>
    public bool Toast => true;

    /// <summary>
    /// Decides a scan's effects from the app window's state (<paramref name="appOpen"/>:
    /// on the taskbar, <paramref name="appMinimised"/>: open but minimised), the
    /// pane's active tab and whether the Documents view shows a paper.
    /// </summary>
    public static ScanArrival Decide(bool appOpen, bool appMinimised, AppTab activeTab, bool paperShown)
    {
        bool openApp = !appOpen;
        bool showing = openApp || !appMinimised;
        return new ScanArrival(openApp, !paperShown, !(showing && activeTab == AppTab.Documents));
    }
}
