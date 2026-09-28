/// <summary>
/// A traveller's directive fault (traveller types P1, §5.2): what the
/// Directives forbid in the presented papers and the claim, read against
/// them with no evidence needed. Runtime only (not serialized); later phases
/// append the wrong departure date and the expired paper.
/// </summary>
public enum DirectiveFault
{
    /// <summary>The Directives allow the traveller's papers and claim.</summary>
    None,

    /// <summary>The claimed destination is closed today (a closure rule; today's guaranteed violators).</summary>
    ClosedDestination,

    /// <summary>The kind's paper set is incomplete or wrong (TravelRuleType.PaperSet): a missing or unsigned form, or the wrong transponder class.</summary>
    IncompletePapers,

    /// <summary>The Citizen Account's standing is Frozen (TravelRuleType.DebtStanding).</summary>
    FrozenAccount
}

/// <summary>
/// The fault reasons (traveller types §5.2): why a wrong accept was wrong, as
/// the citation key suffix of "citation.acceptedWrong." ("Approved forged
/// papers." / "Approved a disguised traveller." / "Approved a closed
/// destination." / "Approved incomplete paperwork." / "Approved a frozen
/// account." / a costume error's panic). One fault source per traveller
/// (K5), so one reason. Pure.
/// </summary>
public static class Faults
{
    /// <summary>A record lie: the papers forge fields the record disproves ("Approved forged papers.").</summary>
    public const string Forged = "forged";

    /// <summary>A place lie: the traveller comes from another place ("Approved a disguised traveller.").</summary>
    public const string Disguised = "disguised";

    /// <summary>A closed destination ("Approved a closed destination.").</summary>
    public const string Closed = "closed";

    /// <summary>An incomplete or wrong paper set ("Approved incomplete paperwork.").</summary>
    public const string Incomplete = "incomplete";

    /// <summary>A frozen account ("Approved a frozen account.").</summary>
    public const string Frozen = "frozen";

    /// <summary>The reason of a directive fault; empty for none.</summary>
    public static string Reason(DirectiveFault fault)
    {
        switch (fault)
        {
            case DirectiveFault.ClosedDestination: return Closed;
            case DirectiveFault.IncompletePapers: return Incomplete;
            case DirectiveFault.FrozenAccount: return Frozen;
            default: return string.Empty;
        }
    }

    /// <summary>
    /// A traveller's one fault reason: the directive fault's when there is
    /// one; else the costume error's panic (CostumeErrors.FaultReason), a
    /// record lie's forgery or a place lie's disguise; empty for no fault.
    /// </summary>
    public static string Reason(DirectiveFault directive, CostumeError costume, bool recordLie, bool placeLie)
    {
        if (directive != DirectiveFault.None)
            return Reason(directive);
        if (costume != CostumeError.None)
            return CostumeErrors.FaultReason;
        if (recordLie)
            return Forged;
        return placeLie ? Disguised : string.Empty;
    }
}
