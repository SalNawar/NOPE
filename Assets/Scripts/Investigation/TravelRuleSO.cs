using UnityEngine;

/// <summary>
/// What a travel rule forbids: a closure (the first three) or a standing
/// procedure. Serialized in the rule assets: append only.
/// </summary>
public enum TravelRuleType
{
    /// <summary>No travel to a specific era today.</summary>
    EraForbidden,

    /// <summary>No travel to a specific nation today.</summary>
    NationForbidden,

    /// <summary>No travel to a specific nation+era combination today.</summary>
    NationEraForbidden,

    /// <summary>
    /// A standing procedure (traveller types P3, P5): a traveller must be
    /// dressed for their destination, or they would cause a panic there. It
    /// closes no destination; a 2150 citizen's costume error breaks it, a
    /// deviation fault proven against the Costume Guide (CostumeErrors).
    /// </summary>
    DressForDestination,

    /// <summary>
    /// A procedure line with no predicate (traveller types §5.3): what the
    /// desk checks for a kind ("Leisure departures: a Leisure Visa and a
    /// Departure Manifest. Every paper must match the Citizen Account."). It
    /// closes no destination and plans no violator; the liars break it (L1,
    /// L2: record lies proven against the account, RecordLies).
    /// </summary>
    Procedure,

    /// <summary>
    /// A standing procedure (traveller types P3, L6; from day 4): no 2150
    /// currency or technology leaves 2150. It closes no destination and has
    /// no predicate: a smuggler breaks it, a deviation fault proven against
    /// the books (LieKind.Smuggling). Guaranteed on its first day
    /// (DayPlanSO.GuaranteedRules), its faulty traveller is a smuggler.
    /// </summary>
    NoPresentGoods,

    /// <summary>
    /// A standing procedure (traveller types P3, F7; from day 4): depart only
    /// on the date on the manifest or the return order, and never on an
    /// expired paper, read against the agency calendar (Directives.PaperDates:
    /// a directive fault, no evidence needed). Guaranteed on its first day,
    /// its faulty traveller has one date falsified (Directives' makers).
    /// </summary>
    PaperDates
}

/// <summary>
/// A daily travel restriction announced in the morning briefing. The player
/// must DENY an otherwise-valid traveler whose claimed destination violates an
/// active closure (a directive fault, DirectiveFault.ClosedDestination). A
/// standing procedure (dress for the destination, a kind's procedure line,
/// no 2150 goods, the papers' dates) closes no destination: its line tells
/// the player what to check; the dates are read per case
/// (Directives.PaperDates). Rules are listed on a DayPlan and evaluated per
/// case.
/// </summary>
[CreateAssetMenu(fileName = "Rule_", menuName = "TimeDesk/Travel Rule", order = 6)]
public sealed class TravelRuleSO : ScriptableObject
{
    /// <summary>What this rule forbids.</summary>
    public TravelRuleType type;

    /// <summary>Era referenced by the rule (for Era/NationEra types).</summary>
    public EraSO era;

    /// <summary>Nation referenced by the rule (for Nation/NationEra types).</summary>
    public NationSO nation;

    /// <summary>Optional custom briefing line; auto-generated if blank (a standing procedure's is authored).</summary>
    [TextArea] public string description;

    /// <summary>True for a closure: it forbids destinations, and each active one gets a planned violator (CaseFactory.PlanViolators).</summary>
    public bool IsClosure => IsClosureType(type);

    /// <summary>True for the closure types (a forbidden era, nation or place); false for a standing procedure.</summary>
    public static bool IsClosureType(TravelRuleType type) =>
        type == TravelRuleType.EraForbidden || type == TravelRuleType.NationForbidden || type == TravelRuleType.NationEraForbidden;

    /// <summary>
    /// True for a standing procedure with a maker (traveller types P4, §5.4),
    /// which a day may guarantee a faulty traveller for (world_source.json
    /// days[].guarantee; CaseFactory.PlanViolators): NoPresentGoods (a
    /// smuggler) and PaperDates (a falsified date). A closure is guaranteed
    /// by the day's guarantee flag instead; a procedure line and the dress
    /// rule have no maker here (the costume roll is the dress rule's).
    /// </summary>
    public static bool IsGuaranteeable(TravelRuleType type) =>
        type == TravelRuleType.NoPresentGoods || type == TravelRuleType.PaperDates;

    /// <summary>
    /// Returns true if this rule permits the given claimed destination.
    /// </summary>
    public bool Allows(NationSO claimNation, EraSO claimEra)
    {
        switch (type)
        {
            case TravelRuleType.EraForbidden:
                return claimEra != era;
            case TravelRuleType.NationForbidden:
                return claimNation != nation;
            case TravelRuleType.NationEraForbidden:
                return !(claimNation == nation && claimEra == era);
            default:
                return true;
        }
    }

    /// <summary>Briefing line describing the restriction.</summary>
    public string Summary()
    {
        if (!string.IsNullOrWhiteSpace(description))
            return description;

        string e = era != null ? era.displayName : UiText.Get("rule.unknownPlace");
        string n = nation != null ? nation.displayName : UiText.Get("rule.unknownPlace");

        switch (type)
        {
            case TravelRuleType.EraForbidden:
                return UiText.Format("rule.eraForbidden", e);
            case TravelRuleType.NationForbidden:
                return UiText.Format("rule.nationForbidden", n);
            case TravelRuleType.NationEraForbidden:
                return UiText.Format("rule.nationEraForbidden", n, e);
            default:
                return UiText.Get("rule.generic");
        }
    }
}
