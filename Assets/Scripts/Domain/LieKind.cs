using System.Collections.Generic;

/// <summary>
/// The lies of the catalogue a day may enable (traveller types §6.1,
/// world_source.json days[].lies): a place lie leaks another place's values
/// (Lies.Plan); a record lie forges fields the traveller's own record
/// disproves (RecordLies). Serialized in DayPlanSO.lieKinds: append only
/// (SerializedEnumsTests pins every value).
/// </summary>
public enum LieKind
{
    /// <summary>L7, today's liar: a displaced person from another of today's places, whose papers, answers or dress leak that home's values.</summary>
    FalseOrigin,

    /// <summary>L1, poor posing as rich: a Standard citizen drawn from the rich entry, with a forged visa and manifest class, or a rich citizen's borrowed manifest.</summary>
    PoorPosingAsRich,

    /// <summary>L2, a doctored identity: a tourist's visa with another Citizen ID or another birth year.</summary>
    DoctoredIdentity,

    /// <summary>L4, a debtor posing as a tourist: an Eligible citizen drawn from a tourist entry, whose visa and manifest print the tourist's classes.</summary>
    DebtorPosingAsTourist,

    /// <summary>L5, a forged contract: a labourer's contract with a higher wage, a shorter term, another employer or another worksite than the one registered.</summary>
    ForgedContract
}

/// <summary>The lies' rules: which kinds of traveller each lie fits, and which lies are record lies. Pure.</summary>
public static class LieKinds
{
    /// <summary>
    /// True when a traveller of <paramref name="kind"/> may carry
    /// <paramref name="lie"/> (traveller types §6.1): a false origin is the
    /// displaced's (a 2150 citizen is who they say and comes from where they
    /// say); poor posing as rich is drawn from the rich entry; a doctored
    /// identity and a debtor posing as a tourist are a tourist's; a forged
    /// contract is a labourer's.
    /// </summary>
    public static bool AppliesTo(LieKind lie, TravellerKind kind)
    {
        switch (lie)
        {
            case LieKind.FalseOrigin:
                return kind == TravellerKind.Displaced;
            case LieKind.PoorPosingAsRich:
                return kind == TravellerKind.RichTourist;
            case LieKind.DoctoredIdentity:
            case LieKind.DebtorPosingAsTourist:
                return kind == TravellerKind.RichTourist || kind == TravellerKind.PoorTourist;
            case LieKind.ForgedContract:
                return kind == TravellerKind.Labourer;
            default:
                return false;
        }
    }

    /// <summary>The lies of <paramref name="enabled"/> (today's, in the plan's order) a traveller of <paramref name="kind"/> may carry; empty for a null list.</summary>
    public static List<LieKind> For(IReadOnlyList<LieKind> enabled, TravellerKind kind)
    {
        var lies = new List<LieKind>();
        if (enabled == null)
            return lies;

        foreach (LieKind lie in enabled)
            if (AppliesTo(lie, kind) && !lies.Contains(lie))
                lies.Add(lie);

        return lies;
    }

    /// <summary>True for a record lie, planned by RecordLies against the traveller's own record; false for the place lie, planned by Lies.Plan.</summary>
    public static bool IsRecordLie(LieKind lie) => lie != LieKind.FalseOrigin;

    /// <summary>
    /// The status of the account a citizen really holds (the truth their
    /// papers must match): poor posing as rich is a Standard citizen drawn
    /// from the rich entry (L1), so their account is Standard whatever the
    /// kind's status (AccountMaker.StatusOf); a debtor posing as a tourist is
    /// an Eligible citizen drawn from a tourist entry (L4); every other
    /// traveller holds their kind's.
    /// </summary>
    public static CitizenStatus TrueStatus(LieKind? lie, CitizenStatus kindStatus) =>
        lie == LieKind.PoorPosingAsRich ? CitizenStatus.Standard
        : lie == LieKind.DebtorPosingAsTourist ? CitizenStatus.Eligible
        : kindStatus;
}
