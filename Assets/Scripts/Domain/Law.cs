/// <summary>
/// What breaking the law means (the desk machine spec §2; Saleh 2026-10-07:
/// "detain only if the traveller breaks the law"). One place in the domain:
/// a forgery (a forged seal, a doctored value: every record lie and the
/// forged seal), a false identity (someone else's photo) and contraband
/// (smuggling the present's goods out). A lie about one's home (a false
/// origin, the fake displaced) and a costume error are deviation faults, not
/// crimes, and a directive fault (missing papers, a closed destination, a
/// wrong date, an expired paper, a frozen account, a recalled transponder)
/// is a rule broken, not a law: all of them are a DENY, never a detention.
/// Pure.
/// </summary>
public static class Law
{
    /// <summary>
    /// True when the traveller breaks the law: their <paramref name="lie"/>
    /// (the lie they carry, or null) is a forgery, a false identity or
    /// contraband. A traveller with a directive fault or a costume error
    /// carries no lie (one fault source per traveller, K5), so breaks none.
    /// </summary>
    public static bool Breaks(LieKind? lie)
    {
        if (lie == null)
            return false;
        LieKind kind = lie.Value;
        return LieKinds.IsRecordLie(kind) || LieKinds.IsVisualLie(kind) || kind == LieKind.Smuggling;
    }
}
