/// <summary>
/// The accept/deny decision table: who should be accepted, and when a right
/// denial still earns a citation because nothing was documented. Pure, so
/// every row is tested headless.
/// </summary>
public static class VerdictRules
{
    /// <summary>Accept only an honest traveller whose claimed destination today's rules allow.</summary>
    public static bool ShouldAccept(bool isLiar, bool claimAllowed) => !isLiar && claimAllowed;

    /// <summary>
    /// True when a denial is right but unproven: the evidence gate is on, the
    /// evidence system is active and logged nothing (<paramref name="evidenceCount"/>
    /// is -1 when the system is inactive), and the denied traveller is a liar
    /// whose claim is allowed (directive denials never need evidence).
    /// </summary>
    public static bool IsUnprovenDenial(bool requireEvidence, int evidenceCount, bool accepted, bool isLiar, bool claimAllowed) =>
        requireEvidence && evidenceCount == 0 && !accepted && isLiar && claimAllowed;
}
