/// <summary>What a scan does (the PC redesign SC4). Not serialized.</summary>
public enum ScanPass
{
    /// <summary>A plain scan: the copy reaches the PC.</summary>
    Plain,

    /// <summary>The Analysis Scanner's pass: a plain scan, then the scanned papers are read against each other (PaperAnalysis).</summary>
    Analysis,

    /// <summary>A scan by hand of a document the Analysis Scanner has analysed already: a plain scan, and its strip says so (the pass works once per document).</summary>
    AlreadyAnalysed
}

/// <summary>
/// The day's scanner upgrades (the PC redesign SC1, SC2), read from the
/// day-start snapshot like every upgrade: a scanner bought tonight is in force
/// from the next office day. The Auto-Feed changes how a paper reaches the
/// scanner (a handed-over paper scans itself: DeskPapers' queue); the Analysis
/// changes what a scan by hand does (an analysis pass: PaperAnalysis), once per
/// document (PassFor; Saleh 2026-09-29: "it only works once per document").
/// They are neither tiers nor exclusive: each is its own flag, and they combine.
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

    /// <summary>
    /// The pass a scan takes: a scan by hand (<paramref name="byHand"/>) with the
    /// Analysis Scanner is the analysis pass, once per document (a document
    /// <paramref name="analysedBefore"/> scans plainly and says it was analysed
    /// already); the scanner's own feed and any scan without the Analysis
    /// Scanner are plain.
    /// </summary>
    public ScanPass PassFor(bool byHand, bool analysedBefore) =>
        !byHand || !Analysis ? ScanPass.Plain : analysedBefore ? ScanPass.AlreadyAnalysed : ScanPass.Analysis;

    /// <summary>The upgrades <paramref name="dayStart"/> owns (WorldState.HasUpgrade, never the retired 'upgrade:x' flag); none without a snapshot.</summary>
    public static ScannerDay From(GateSnapshot dayStart) =>
        dayStart == null ? default : new ScannerDay(dayStart.HasUpgrade(AutoFeedUpgradeId), dayStart.HasUpgrade(AnalysisUpgradeId));
}
