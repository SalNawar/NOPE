using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// The desktop shows the wallpaper of the most recent world change
/// (DesktopWallpaper): the latest factor to leave its "as you found it"
/// outcome, ties in content order, a split's newer half, else the leading
/// culture's, else neutral.
/// </summary>
public class DesktopWallpaperTests
{
    private static readonly List<PullFactor> Factors = new List<PullFactor>
    {
        new PullFactor("government", "directorate", new[] { "directorate", "democracy", "monarchy" }),
        new PullFactor("future", "credit_age", new[] { "credit_age", "nuclear", "space_age" }),
        new PullFactor("money", "debt", new[] { "debt", "jubilee", "commons" }),
    };

    private static FactorLead L(string factor, string outcome, int since, string split = "") =>
        new FactorLead { factor = factor, outcome = outcome, split = split, sinceDay = since };

    [Test]
    public void AsFound_ShowsTheLeadingCulture_ElseNeutral()
    {
        var leads = new List<FactorLead> { L("government", "directorate", 0), L("future", "credit_age", 0), L("money", "debt", 0) };

        WallpaperPick culture = DesktopWallpaper.Pick(Factors, leads, "japan");
        Assert.IsTrue(culture.IsCulture);
        Assert.AreEqual("japan", culture.Outcome);

        Assert.AreEqual(DesktopWallpaper.Neutral, DesktopWallpaper.Pick(Factors, leads, null).Outcome);
        Assert.AreEqual(DesktopWallpaper.Neutral, DesktopWallpaper.Pick(Factors, null, " ").Outcome);
    }

    [Test]
    public void TheMostRecentChange_Wins_OverTheCulture()
    {
        var leads = new List<FactorLead> { L("government", "democracy", 4), L("future", "nuclear", 9), L("money", "debt", 0) };

        WallpaperPick pick = DesktopWallpaper.Pick(Factors, leads, "egypt");

        Assert.AreEqual("future", pick.Factor);
        Assert.AreEqual("nuclear", pick.Outcome);
        Assert.IsFalse(pick.IsCulture);
    }

    [Test]
    public void ATie_GoesToContentOrder_GovernmentThenFutureThenMoney()
    {
        var leads = new List<FactorLead> { L("money", "jubilee", 6), L("future", "space_age", 6), L("government", "directorate", 0) };

        Assert.AreEqual("future", DesktopWallpaper.Pick(Factors, leads, null).Factor);

        leads[2] = L("government", "monarchy", 6);
        Assert.AreEqual("government", DesktopWallpaper.Pick(Factors, leads, null).Factor);
    }

    [Test]
    public void AFactorBackAsFound_NoLongerCounts()
    {
        var leads = new List<FactorLead> { L("government", "directorate", 11), L("money", "commons", 3) };

        WallpaperPick pick = DesktopWallpaper.Pick(Factors, leads, "china");

        Assert.AreEqual("money", pick.Factor);
        Assert.AreEqual("commons", pick.Outcome);
    }

    [Test]
    public void ASplit_ShowsItsNewerHalf()
    {
        Assert.AreEqual("democracy", DesktopWallpaper.Pick(Factors, new[] { L("government", "directorate", 5, "democracy") }, null).Outcome);
        Assert.AreEqual("democracy", DesktopWallpaper.Pick(Factors, new[] { L("government", "democracy", 5, "directorate") }, null).Outcome);
        Assert.AreEqual("monarchy", DesktopWallpaper.Pick(Factors, new[] { L("government", "monarchy", 5, "democracy") }, null).Outcome);
    }

    [Test]
    public void UnknownFactorsAndBlankLeads_AreIgnored()
    {
        var leads = new List<FactorLead> { null, L("weather", "rain", 9), L("future", "", 9) };

        Assert.IsTrue(DesktopWallpaper.Pick(Factors, leads, "italy").IsCulture);
    }
}
