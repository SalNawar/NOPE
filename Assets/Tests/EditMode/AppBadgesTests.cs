using NUnit.Framework;

/// <summary>
/// The Investigation app's tab badges (the PC redesign WN5): something new for
/// a tab badges it unless the player is looking at that tab in either pane;
/// showing the tab clears it; a new case clears every badge. Nothing switches
/// the tab.
/// </summary>
public class AppBadgesTests
{
    private static readonly AppTab[] Hidden = new AppTab[0];

    private static AppTab[] Shown(params AppTab[] tabs) => tabs;

    [Test]
    public void Arrival_OnAnotherTab_BadgesIt()
    {
        var badges = new AppBadges();
        Assert.IsTrue(badges.Arrived(AppTab.Transcript, Shown(AppTab.Documents)));
        Assert.IsTrue(badges.IsBadged(AppTab.Transcript));
        Assert.IsFalse(badges.IsBadged(AppTab.Documents));
    }

    [Test]
    public void Arrival_OnTheTabThePlayerSees_BadgesNothing()
    {
        var badges = new AppBadges();
        Assert.IsFalse(badges.Arrived(AppTab.Report, Shown(AppTab.Report)));
        Assert.IsFalse(badges.IsBadged(AppTab.Report));
    }

    [Test]
    public void Arrival_WhileTheAppIsClosedOrMinimised_BadgesEvenTheActiveTab()
    {
        var badges = new AppBadges();
        Assert.IsTrue(badges.Arrived(AppTab.Documents, Hidden));
        Assert.IsTrue(badges.IsBadged(AppTab.Documents));
        Assert.IsTrue(badges.Arrived(AppTab.Report, null));
    }

    [Test]
    public void Arrival_OnTheTabTheOtherPaneShows_BadgesNothing()
    {
        var badges = new AppBadges();
        Assert.IsFalse(badges.Arrived(AppTab.Transcript, Shown(AppTab.Documents, AppTab.Transcript)));
        Assert.IsTrue(badges.Arrived(AppTab.Report, Shown(AppTab.Documents, AppTab.Transcript)));
    }

    [Test]
    public void Seen_ClearsThatTabOnly()
    {
        var badges = new AppBadges();
        badges.Arrived(AppTab.Transcript, Hidden);
        badges.Arrived(AppTab.Report, Hidden);
        badges.Seen(AppTab.Transcript);
        Assert.IsFalse(badges.IsBadged(AppTab.Transcript));
        Assert.IsTrue(badges.IsBadged(AppTab.Report));
    }

    [Test]
    public void Clear_ANewCase_ClearsEveryBadge()
    {
        var badges = new AppBadges();
        foreach (AppTab tab in TabOrder.Default)
            badges.Arrived(tab, Hidden);
        badges.Clear();
        foreach (AppTab tab in TabOrder.Default)
            Assert.IsFalse(badges.IsBadged(tab), tab.ToString());
    }
}
