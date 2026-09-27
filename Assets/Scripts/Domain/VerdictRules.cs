/// <summary>
/// The accept/deny decision table (traveller types P1, P2, §5.2): who should
/// be accepted, and when a right denial still earns a citation because
/// nothing was documented. Two fault kinds: a directive fault is what the
/// Directives forbid, read against them (a closed destination; later a
/// missing form, a frozen account, a wrong date, an expired paper); a
/// deviation fault is anything that needs a statement compared with a truth
/// (every lie, a costume error, smuggling). One fault source per traveller
/// (K5), so no traveller has both. Pure, so every row is tested headless.
/// </summary>
public static class VerdictRules
{
    /// <summary>Accept only a traveller with no fault of either kind.</summary>
    public static bool ShouldAccept(bool hasDeviationFault, bool hasDirectiveFault) => !hasDeviationFault && !hasDirectiveFault;

    /// <summary>
    /// True when a denial is right but unproven: the evidence gate is on, the
    /// evidence system is active and logged nothing (<paramref name="evidenceCount"/>
    /// is -1 when the system is inactive), and the denied traveller has a
    /// deviation fault and no directive fault (a directive denial never needs
    /// evidence: the Directives are public).
    /// </summary>
    public static bool IsUnprovenDenial(bool requireEvidence, int evidenceCount, bool accepted, bool hasDeviationFault, bool hasDirectiveFault) =>
        requireEvidence && evidenceCount == 0 && !accepted && hasDeviationFault && !hasDirectiveFault;
}
