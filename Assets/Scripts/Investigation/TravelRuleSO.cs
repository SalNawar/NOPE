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
    DressForDestination
}

/// <summary>
/// A daily travel restriction announced in the morning briefing. The player
/// must DENY an otherwise-valid traveler whose claimed destination violates an
/// active closure. A standing procedure (dress for the destination) closes no
/// destination: its line tells the player what to check. Rules are listed on
/// a DayPlan and evaluated per case.
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
