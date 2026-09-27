using NUnit.Framework;

/// <summary>
/// The day's scanner upgrades (the PC redesign SC1, SC2): read from the
/// day-start snapshot, so a scanner bought tonight works from the next office
/// day; each is its own flag, and they combine.
/// </summary>
public class ScannerDayTests
{
    private static GateSnapshot Owning(params string[] upgrades) => new GateSnapshot(2, 100f, null, upgrades, null, null, null, null);

    [Test]
    public void TheIds_AreTheShopsUpgradeIds()
    {
        Assert.AreEqual("scanner_autofeed", ScannerDay.AutoFeedUpgradeId, "Upgrade_AutoFeedScanner.asset");
        Assert.AreEqual("adv_scanner", ScannerDay.AnalysisUpgradeId, "Upgrade_AdvancedScanner.asset, renamed Analysis Scanner: the id is kept so a save that owns it keeps it");
    }

    [Test]
    public void From_ReadsEachUpgrade_AndTheyCombine()
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

        ScannerDay both = ScannerDay.From(Owning(ScannerDay.AnalysisUpgradeId, "diplo_contacts", ScannerDay.AutoFeedUpgradeId));
        Assert.IsTrue(both.AutoFeed);
        Assert.IsTrue(both.Analysis);
    }

    [Test]
    public void TheRetiredFlag_IsNotOwnership()
    {
        var flagOnly = new GateSnapshot(2, 100f, new[] { "upgrade:adv_scanner" }, null, null, null, null, null);
        Assert.IsFalse(ScannerDay.From(flagOnly).Analysis, "the retired Effect_Upgrade_ScannerBoost set this flag; only the owned upgrade counts");
    }

    [Test]
    public void NoSnapshot_NoUpgrades()
    {
        ScannerDay day = ScannerDay.From(null);
        Assert.IsFalse(day.AutoFeed);
        Assert.IsFalse(day.Analysis);
        Assert.IsFalse(default(ScannerDay).AutoFeed);
        Assert.IsFalse(default(ScannerDay).Analysis);
    }
}
