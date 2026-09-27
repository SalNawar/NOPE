using System;

/// <summary>
/// The project's one value comparison (audit R1-004: it lived on the
/// Deviation Report's log, so the fact table, the history and carry rules,
/// the tell rules, the looks, gender derivation, the compare bar, Generate
/// World and the validator all depended on the evidence layer to compare two
/// strings). Trimmed, case-insensitive equality: what the scanner shows as
/// MATCH, what a proof needs to differ (DiscrepancyLog), what the paper cross
/// check reads (PaperChecks), and every other place two values are held equal.
/// </summary>
public static class Values
{
    /// <summary>True when <paramref name="x"/> and <paramref name="y"/> are the same value: trimmed, ignoring case; null counts as empty.</summary>
    public static bool Match(string x, string y) =>
        string.Equals((x ?? string.Empty).Trim(), (y ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase);
}
