/// <summary>
/// The day's scanner upgrades (the PC redesign SC1, SC2), read from the
/// day-start snapshot like every upgrade: a scanner bought tonight is in force
/// from the next office day. The Auto-Feed changes how a paper reaches the
/// scanner (a handed-over paper scans itself: DeskPapers' queue); the Analysis
/// changes what a scan by hand does (an analysis pass: PaperAnalysis). They are
/// neither tiers nor exclusive: each is its own flag, and they combine.
/// </summary>
public readonly struct ScannerDay
{
    /// <summary>The Auto-Feed Scanner's upgrade id (Upgrade_AutoFeedScanner.asset).</summary>
    public const string AutoFeedUpgradeId = "scanner_autofeed";

    /// <summary>The Analysis Scanner's upgrade id (Upgrade_AdvancedScanner.asset: the old Advanced Scanner's id is kept, so a save that owns it keeps it).</summary>
    public const string AnalysisUpgradeId = "adv_scanner";

    /// <summary>A day with the given upgrades.</summary>
    public ScannerDay(bool autoFeed, bool analysis)
    {
        AutoFeed = autoFeed;
        Analysis = analysis;
    }

    /// <summary>True when handed-over papers scan themselves, in hand-over order (SC3).</summary>
    public bool AutoFeed { get; }

    /// <summary>True when a scan by hand takes the analysis pass and marks the first undocumented contradicting pair (SC4).</summary>
    public bool Analysis { get; }

    /// <summary>The upgrades <paramref name="dayStart"/> owns (WorldState.HasUpgrade, never the retired 'upgrade:x' flag); none without a snapshot.</summary>
    public static ScannerDay From(GateSnapshot dayStart) =>
        dayStart == null ? default : new ScannerDay(dayStart.HasUpgrade(AutoFeedUpgradeId), dayStart.HasUpgrade(AnalysisUpgradeId));
}
