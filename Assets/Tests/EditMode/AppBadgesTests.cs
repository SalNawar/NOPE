using NUnit.Framework;

/// <summary>
/// The Investigation app's tab badges (the PC redesign WN5): something new for
/// a tab badges it unless the player is looking at that tab; showing the tab
/// clears it; a new case clears every badge. Nothing switches the tab.
/// </summary>
public class AppBadgesTests
{
    [Test]
    public void Arrival_OnAnotherTab_BadgesIt()
    {
        var badges = new AppBadges();
        Assert.IsTrue(badges.Arrived(AppTab.Transcript, AppTab.Documents));
        Assert.IsTrue(badges.IsBadged(AppTab.Transcript));
        Assert.IsFalse(badges.IsBadged(AppTab.Documents));
    }

    [Test]
    public void Arrival_OnTheTabThePlayerSees_BadgesNothing()
    {
        var badges = new AppBadges();
        Assert.IsFalse(badges.Arrived(AppTab.Report, AppTab.Report));
        Assert.IsFalse(badges.IsBadged(AppTab.Report));
    }

    [Test]
    public void Arrival_WhileTheAppIsClosedOrMinimised_BadgesEvenTheActiveTab()
    {
        var badges = new AppBadges();
        Assert.IsTrue(badges.Arrived(AppTab.Documents, null));
        Assert.IsTrue(badges.IsBadged(AppTab.Documents));
    }

    [Test]
    public void Seen_ClearsThatTabOnly()
    {
        var badges = new AppBadges();
        badges.Arrived(AppTab.Transcript, null);
        badges.Arrived(AppTab.Report, null);
        badges.Seen(AppTab.Transcript);
        Assert.IsFalse(badges.IsBadged(AppTab.Transcript));
        Assert.IsTrue(badges.IsBadged(AppTab.Report));
    }

    [Test]
    public void Clear_ANewCase_ClearsEveryBadge()
    {
        var badges = new AppBadges();
        foreach (AppTab tab in TabOrder.Default)
            badges.Arrived(tab, null);
        badges.Clear();
        foreach (AppTab tab in TabOrder.Default)
            Assert.IsFalse(badges.IsBadged(tab), tab.ToString());
    }
}
