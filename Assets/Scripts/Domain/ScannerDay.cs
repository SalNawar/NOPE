using System.Collections.Generic;

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
/// The day's scanner upgrade (the PC redesign SC1; Saleh 2026-09-29: "you can
/// only have one type of upgraded scanner installed at a time"), read at the
/// day's start from the upgrades in force (Installs.InForce: the scanners
/// share one install slot, so only the installed one counts, and the desk
/// shows only its part). The Auto-Feed changes how a paper reaches the
/// scanner (a handed-over paper scans itself: DeskPapers' queue); the Analysis
/// changes what a scan by hand does (an analysis pass: PaperAnalysis), once per
/// document (PassFor; Saleh 2026-09-29: "it only works once per document").
/// Each is its own flag; with one installed at a time, at most one is set.
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

    /// <summary>The scanners among the upgrades in force at the day's start (<paramref name="inForce"/>: owned upgrade ids, never the retired 'upgrade:x' flag); none without them.</summary>
    public static ScannerDay From(IEnumerable<string> inForce)
    {
        bool autoFeed = false, analysis = false;
        foreach (string id in inForce ?? System.Array.Empty<string>())
        {
            autoFeed |= id == AutoFeedUpgradeId;
            analysis |= id == AnalysisUpgradeId;
        }
        return new ScannerDay(autoFeed, analysis);
    }
}
