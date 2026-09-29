using System;

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

    /// <summary>
    /// True while the day's free warnings last: the day's
    /// <paramref name="citationNumberToday"/>-th citation (1-based) is at or
    /// under <paramref name="freeWarningsPerDay"/> (GameConfigSO).
    /// </summary>
    public static bool IsFreeWarning(int citationNumberToday, int freeWarningsPerDay) => citationNumberToday <= freeWarningsPerDay;

    /// <summary>
    /// The one penalty for a wrong decision (redesign phase 23; Saleh: "clerk
    /// is fined for any mistake on application the same either approval or
    /// rejection. we dont penalize based on the type of mistake"): a wrong
    /// accept, a wrong deny and an unproven denial all cost
    /// <paramref name="penalty"/> (GameConfigSO.wrongDecisionPenalty), whatever
    /// the fault and however many came before; 0 while the day's free warnings
    /// last (<see cref="IsFreeWarning"/>); never below 0. Nothing else fines
    /// the clerk (a stranding is a world consequence, not a fine).
    /// </summary>
    public static int WrongDecisionPenalty(int citationNumberToday, int freeWarningsPerDay, int penalty) =>
        IsFreeWarning(citationNumberToday, freeWarningsPerDay) ? 0 : Math.Max(0, penalty);
}
