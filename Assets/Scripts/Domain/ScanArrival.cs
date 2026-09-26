using System.Collections.Generic;

/// <summary>
/// What a finished scan does in the Investigation app (the PC redesign WN5:
/// nothing steals the view; a scan is the only thing that opens by itself).
/// The app window opens only when it is closed (a minimised one stays down);
/// the scanned paper shows in a Documents view only when that view shows no
/// paper yet (each pane's view decides for itself), so it never replaces the
/// paper the player reads, and no tab switches; the Documents tab is badged
/// unless a showing pane shows it; a toast always names the paper. Pure;
/// InvestigationApp applies it.
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
    /// panes' tabs (<paramref name="paneTabs"/>: the left pane's, and the right
    /// one's while the split is on) and whether this Documents view shows a
    /// paper.
    /// </summary>
    public static ScanArrival Decide(bool appOpen, bool appMinimised, IReadOnlyCollection<AppTab> paneTabs, bool paperShown)
    {
        bool openApp = !appOpen;
        bool showing = openApp || !appMinimised;
        bool seen = showing && paneTabs != null && System.Linq.Enumerable.Contains(paneTabs, AppTab.Documents);
        return new ScanArrival(openApp, !paperShown, !seen);
    }
}
