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
    /// True when the decision is right: it matches <paramref name="shouldAccept"/>,
    /// or it is a denial of a traveller whose directive fault the desk cured by
    /// getting a waiver signed from the pad (<paramref name="curedAtDesk"/>; the
    /// endings and strandings spec §7.3, Saleh's Q14 = A: approving is then
    /// right, and denying stays right, the rule refusing unsigned papers).
    /// </summary>
    public static bool IsCorrect(bool accepted, bool shouldAccept, bool curedAtDesk) => accepted == shouldAccept || (!accepted && curedAtDesk);

    /// <summary>
    /// The three verdicts (the desk machine spec §2; Saleh 2026-10-07:
    /// "detain only if the traveller breaks the law"): a detention is right
    /// only for a traveller who <paramref name="breaksLaw"/> (Law.Breaks);
    /// denying a law-breaker stays right (detain is the stronger, never
    /// required option); APPROVED and DENIED follow the two-verdict table
    /// (approving a law-breaker is wrong, as they have a fault).
    /// </summary>
    public static bool IsCorrect(DeskStamp verdict, bool shouldAccept, bool curedAtDesk, bool breaksLaw) =>
        verdict == DeskStamp.Detained ? breaksLaw : IsCorrect(verdict == DeskStamp.Approved, shouldAccept, curedAtDesk);

    /// <summary>True when <paramref name="verdict"/> detains a traveller who broke no law (<paramref name="breaksLaw"/> false): wrong, the one citation ("Detained a traveller who broke no law").</summary>
    public static bool IsWrongDetention(DeskStamp verdict, bool breaksLaw) => verdict == DeskStamp.Detained && !breaksLaw;

    /// <summary>
    /// True when a right denial or a right detention is unproven: the
    /// evidence rule of a denial (<see cref="IsUnprovenDenial"/>) holds for
    /// a detention too (the spec: "Detain with zero logged evidence follows
    /// today's evidence rule for denials"); a detention of a traveller who
    /// broke no law is a wrong detention instead, never unproven
    /// (<see cref="IsWrongDetention"/>).
    /// </summary>
    public static bool IsUnproven(bool requireEvidence, int evidenceCount, DeskStamp verdict, bool hasDeviationFault, bool hasDirectiveFault, bool breaksLaw) =>
        !IsWrongDetention(verdict, breaksLaw)
        && IsUnprovenDenial(requireEvidence, evidenceCount, verdict == DeskStamp.Approved, hasDeviationFault, hasDirectiveFault);

    /// <summary>
    /// True when a denial is right but unproven: the evidence gate is on, the
    /// evidence system is active and logged nothing (<paramref name="evidenceCount"/>
    /// is -1 when the system is inactive), and the denied traveller has a
    /// fault of either kind. Saleh, 2026-10-05 (the desk-first redesign): "the
    /// player may deny freely, but a denial with no logged evidence earns a
    /// citation", a directive fault's too (a rule held against the value it
    /// breaks, or the calendar against a date, logs it); a denial of a
    /// traveller with no fault is a wrong denial instead. Either way it is the
    /// one penalty (<see cref="WrongDecisionPenalty"/>).
    /// </summary>
    public static bool IsUnprovenDenial(bool requireEvidence, int evidenceCount, bool accepted, bool hasDeviationFault, bool hasDirectiveFault) =>
        requireEvidence && evidenceCount == 0 && !accepted && (hasDeviationFault || hasDirectiveFault);

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
    /// last (<see cref="IsFreeWarning"/>); never below 0. The one other fine is
    /// Saleh's knowing exception (2026-09-30, Q10 = D): the stranding fine the
    /// agency's failure report charges when a stranded traveller had no valid
    /// signed waiver (GameConfigSO.strandingFine; ShiftStrandings), a
    /// consequence of a stranding, not of a decision.
    /// </summary>
    public static int WrongDecisionPenalty(int citationNumberToday, int freeWarningsPerDay, int penalty) =>
        IsFreeWarning(citationNumberToday, freeWarningsPerDay) ? 0 : Math.Max(0, penalty);
}
