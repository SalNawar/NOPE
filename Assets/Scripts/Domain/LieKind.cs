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

    /// <summary>L8, the fake displaced: a 2150 citizen posing as a displaced person, whose papers or answers leak the present's values (a place lie whose one candidate home is the present).</summary>
    FakeDisplaced,

    /// <summary>
    /// L6, smuggling: 2150 currency or technology on the way out. The
    /// traveller's claim is honest; the manifest (or the declaration) or an
    /// answer gives the present's value for Currency or Technology, proven
    /// against the destination's book row or the present's (Lies.Plan with
    /// the present as the only candidate and Lies.SmuggledCategories).
    /// </summary>
    Smuggling,

    /// <summary>L4, a debtor posing as a tourist: an Eligible citizen drawn from a tourist entry, whose papers print the tourist's status and class (as rich: the visa's and the manifest's classes; as poor: the visa's and the proof's class, a sliver of the debt on the waiver, and a proof of means the account does not hold).</summary>
    DebtorPosingAsTourist,

    /// <summary>L5, a forged contract: a labourer's contract with a higher wage, a shorter term, another employer or another worksite than the one registered.</summary>
    ForgedContract,

    /// <summary>L3, a fake waiver: a Stranding Waiver with a number the account never registered, or one made out for another transponder (which also contradicts the manifest).</summary>
    FakeWaiver,

    /// <summary>L10, a forged proof of means: a credit line, savings or a policy number the account does not hold on file.</summary>
    ForgedProof,

    /// <summary>L11, a forged seal (the document design spec, D4): one of the traveller's papers carries a seal that differs from its office's in the Seal Register (another outline, ink or legend); every value printed is true.</summary>
    ForgedSeal,

    /// <summary>L12, someone else's photo (the document design spec, D8): the primary form's photo shows another person (another skin tone and hair colour) than the traveller at the desk; every value printed is true.</summary>
    SwappedPhoto
}

/// <summary>The lies' rules: which kinds of traveller each lie fits, and which lies are record lies. Pure.</summary>
public static class LieKinds
{
    /// <summary>
    /// True when a traveller of <paramref name="kind"/> may carry
    /// <paramref name="lie"/> (traveller types §6.1): a false origin is the
    /// displaced's (a 2150 citizen is who they say and comes from where they
    /// say), and so is the fake displaced (a 2150 citizen posing as one);
    /// poor posing as rich is drawn from the rich entry; a doctored identity
    /// is a tourist's, and so is a debtor posing as a tourist; a forged
    /// contract is a labourer's; a fake waiver is a poor tourist's or a
    /// labourer's (the kinds that carry one); a forged proof of means is a
    /// poor tourist's; smuggling is every kind's (the days decide when: 2150
    /// citizens from day 4, the displaced from day 5); so are the visual lies,
    /// a forged seal and someone else's photo (every kind's papers carry
    /// seals and a photo).
    /// </summary>
    public static bool AppliesTo(LieKind lie, TravellerKind kind)
    {
        switch (lie)
        {
            case LieKind.ForgedSeal:
            case LieKind.SwappedPhoto:
                return true;
            case LieKind.FalseOrigin:
            case LieKind.FakeDisplaced:
                return kind == TravellerKind.Displaced;
            case LieKind.PoorPosingAsRich:
                return kind == TravellerKind.RichTourist;
            case LieKind.DoctoredIdentity:
            case LieKind.DebtorPosingAsTourist:
                return kind == TravellerKind.RichTourist || kind == TravellerKind.PoorTourist;
            case LieKind.Smuggling:
                return true;
            case LieKind.ForgedContract:
                return kind == TravellerKind.Labourer;
            case LieKind.FakeWaiver:
                return kind == TravellerKind.PoorTourist || kind == TravellerKind.Labourer;
            case LieKind.ForgedProof:
                return kind == TravellerKind.PoorTourist;
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

    /// <summary>True for a place lie, planned by Lies.Plan: the false origin (another of today's places), the fake displaced (the present as the one candidate home) or smuggling (the present's goods).</summary>
    public static bool IsPlaceLie(LieKind lie) => lie == LieKind.FalseOrigin || lie == LieKind.FakeDisplaced || lie == LieKind.Smuggling;

    /// <summary>True for a visual lie (the document design spec, D4, D8): a forged seal, proven against the Seal Register, or someone else's photo, proven against the traveller at the desk; planned by their own makers (Seals.Forge, Looks.Stranger), never by RecordLies or Lies.Plan.</summary>
    public static bool IsVisualLie(LieKind lie) => lie == LieKind.ForgedSeal || lie == LieKind.SwappedPhoto;

    /// <summary>
    /// The first of <paramref name="days"/> (each day's number and the lies it
    /// enables) that enables <paramref name="lie"/>: the day its check arrives
    /// in the ramp (the forged seal's is the day the Seal Register joins the
    /// workbench's shelf; the document design spec, D4); 0 when no day does.
    /// </summary>
    public static int FirstDay(LieKind lie, IEnumerable<(int day, IEnumerable<LieKind> lies)> days)
    {
        int first = 0;
        if (days != null)
            foreach ((int day, IEnumerable<LieKind> lies) in days)
                if (lies != null && (first == 0 || day < first))
                    foreach (LieKind enabled in lies)
                        if (enabled == lie)
                        {
                            first = day;
                            break;
                        }
        return first;
    }

    /// <summary>True for a record lie, planned by RecordLies against the traveller's own record; false for a place lie or a visual lie.</summary>
    public static bool IsRecordLie(LieKind lie) => !IsPlaceLie(lie) && !IsVisualLie(lie);

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
