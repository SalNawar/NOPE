using NUnit.Framework;

/// <summary>
/// What a finished scan does in the Investigation app (the PC redesign WN5,
/// the only thing that opens by itself): the app opens only when it is closed
/// (a minimised one stays down), the paper shows only in a Documents view that
/// shows no paper yet, the Documents tab is badged unless the player sees it,
/// and a toast always names the paper. Nothing switches the tab.
/// </summary>
public class ScanArrivalTests
{
    [Test]
    public void AppClosed_OpensIt_AndShowsThePaperWhenNoneIsShown()
    {
        ScanArrival a = ScanArrival.Decide(false, false, AppTab.Documents, false);
        Assert.IsTrue(a.OpenApp);
        Assert.IsTrue(a.ShowPaper);
        Assert.IsFalse(a.BadgeDocuments, "the app opens on Documents: the player sees it");
        Assert.IsTrue(a.Toast);
    }

    [Test]
    public void AppClosed_OnAnotherTab_OpensItOnThatTab_AndBadgesDocuments()
    {
        ScanArrival a = ScanArrival.Decide(false, false, AppTab.Records, false);
        Assert.IsTrue(a.OpenApp);
        Assert.IsTrue(a.ShowPaper, "the Documents view gets the paper without being shown");
        Assert.IsTrue(a.BadgeDocuments);
        Assert.IsTrue(a.Toast);
    }

    [Test]
    public void AppMinimised_StaysDown_AndBadges()
    {
        ScanArrival a = ScanArrival.Decide(true, true, AppTab.Documents, false);
        Assert.IsFalse(a.OpenApp);
        Assert.IsTrue(a.ShowPaper);
        Assert.IsTrue(a.BadgeDocuments);
        Assert.IsTrue(a.Toast);
    }

    [Test]
    public void APaperAlreadyShown_IsNeverReplaced()
    {
        ScanArrival a = ScanArrival.Decide(true, false, AppTab.Documents, true);
        Assert.IsFalse(a.OpenApp);
        Assert.IsFalse(a.ShowPaper);
        Assert.IsFalse(a.BadgeDocuments, "the player is looking at Documents");
        Assert.IsTrue(a.Toast);
    }

    [Test]
    public void AppShowingAnotherTab_OpensNothing_AndBadgesDocuments()
    {
        ScanArrival a = ScanArrival.Decide(true, false, AppTab.Transcript, true);
        Assert.IsFalse(a.OpenApp);
        Assert.IsFalse(a.ShowPaper);
        Assert.IsTrue(a.BadgeDocuments);
        Assert.IsTrue(a.Toast);
    }
}
