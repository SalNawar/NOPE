/// <summary>
/// A traveller's directive fault (traveller types P1, §5.2): what the
/// Directives forbid in the presented papers and the claim, read against
/// them with no evidence needed. Runtime only (not serialized); later phases
/// append the missing or wrong paper set, the frozen account, the wrong
/// departure date and the expired paper.
/// </summary>
public enum DirectiveFault
{
    /// <summary>The Directives allow the traveller's papers and claim.</summary>
    None,

    /// <summary>The claimed destination is closed today (a closure rule; today's guaranteed violators).</summary>
    ClosedDestination
}

/// <summary>
/// The fault reasons (traveller types §5.2): why a wrong accept was wrong, as
/// the citation key suffix of "citation.acceptedWrong." ("Approved forged
/// papers." / "Approved a disguised traveller." / "Let 2150 goods leave
/// 2150." / "Approved a closed destination." / a costume error's panic). One
/// fault source per traveller (K5), so one reason. Pure.
/// </summary>
public static class Faults
{
    /// <summary>A record lie: the papers forge fields the record disproves ("Approved forged papers.").</summary>
    public const string Forged = "forged";

    /// <summary>A false origin: the traveller comes from another place ("Approved a disguised traveller.").</summary>
    public const string Disguised = "disguised";

    /// <summary>Smuggling: the traveller carries the present's currency or technology out (L6; "Let 2150 goods leave 2150.").</summary>
    public const string Smuggled = "smuggled";

    /// <summary>A closed destination ("Approved a closed destination.").</summary>
    public const string Closed = "closed";

    /// <summary>The reason of a directive fault; empty for none.</summary>
    public static string Reason(DirectiveFault fault) =>
        fault == DirectiveFault.ClosedDestination ? Closed : string.Empty;

    /// <summary>
    /// A traveller's one fault reason: the directive fault's when there is
    /// one; else the costume error's panic (CostumeErrors.FaultReason), or
    /// the lie's (<paramref name="lie"/>, the lie the traveller carries, or
    /// null): a record lie's forgery, smuggling, or a false origin's
    /// disguise; empty for no fault.
    /// </summary>
    public static string Reason(DirectiveFault directive, CostumeError costume, LieKind? lie)
    {
        if (directive != DirectiveFault.None)
            return Reason(directive);
        if (costume != CostumeError.None)
            return CostumeErrors.FaultReason;
        if (lie == null)
            return string.Empty;
        if (LieKinds.IsRecordLie(lie.Value))
            return Forged;
        return lie == LieKind.Smuggling ? Smuggled : Disguised;
    }
}
