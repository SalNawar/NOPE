using NUnit.Framework;

/// <summary>
/// The day's scanner upgrade (the PC redesign SC1): read from the upgrades in
/// force at the day's start (only the installed scanner: InstallsTests); each
/// is its own flag.
/// </summary>
public class ScannerDayTests
{
    private static string[] Owning(params string[] upgrades) => upgrades;

    [Test]
    public void TheIds_AreTheShopsUpgradeIds()
    {
        Assert.AreEqual("scanner_autofeed", ScannerDay.AutoFeedUpgradeId, "Upgrade_AutoFeedScanner.asset");
        Assert.AreEqual("adv_scanner", ScannerDay.AnalysisUpgradeId, "Upgrade_AdvancedScanner.asset, renamed Analysis Scanner: the id is kept so a save that owns it keeps it");
    }

    [Test]
    public void From_ReadsEachUpgrade()
    {
        ScannerDay none = ScannerDay.From(Owning("diplo_contacts"));
        Assert.IsFalse(none.AutoFeed);
        Assert.IsFalse(none.Analysis);

        ScannerDay autoFeed = ScannerDay.From(Owning(ScannerDay.AutoFeedUpgradeId));
        Assert.IsTrue(autoFeed.AutoFeed);
        Assert.IsFalse(autoFeed.Analysis);

        ScannerDay analysis = ScannerDay.From(Owning(ScannerDay.AnalysisUpgradeId));
        Assert.IsFalse(analysis.AutoFeed);
        Assert.IsTrue(analysis.Analysis);

        Assert.IsFalse(ScannerDay.From(Owning("upgrade:adv_scanner")).Analysis, "the retired Effect_Upgrade_ScannerBoost's flag is no upgrade id");
    }

    [Test]
    public void TheAnalysis_TakesAScanByHand_OncePerDocument()
    {
        var analysis = new ScannerDay(false, true);
        Assert.AreEqual(ScanPass.Analysis, analysis.PassFor(true, false), "a scan by hand of a paper not analysed yet: the analysis pass");
        Assert.AreEqual(ScanPass.AlreadyAnalysed, analysis.PassFor(true, true), "Saleh 2026-09-29: it only works once per document");
        Assert.AreEqual(ScanPass.Plain, analysis.PassFor(false, false), "the scanner's own feed is never an analysis");
        Assert.AreEqual(ScanPass.Plain, analysis.PassFor(false, true));

        foreach (ScannerDay plain in new[] { default(ScannerDay), new ScannerDay(true, false) })
        {
            Assert.AreEqual(ScanPass.Plain, plain.PassFor(true, false), "without the Analysis Scanner a scan by hand is plain");
            Assert.AreEqual(ScanPass.Plain, plain.PassFor(true, true));
            Assert.AreEqual(ScanPass.Plain, plain.PassFor(false, false));
        }
    }

    [Test]
    public void NoUpgrades_NoScanner()
    {
        ScannerDay day = ScannerDay.From(null);
        Assert.IsFalse(day.AutoFeed);
        Assert.IsFalse(day.Analysis);
        Assert.IsFalse(default(ScannerDay).AutoFeed);
        Assert.IsFalse(default(ScannerDay).Analysis);
    }
}
