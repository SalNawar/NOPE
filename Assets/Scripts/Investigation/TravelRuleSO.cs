using UnityEngine;

/// <summary>
/// What a travel rule forbids.
/// </summary>
public enum TravelRuleType
{
    /// <summary>No travel to a specific era today.</summary>
    EraForbidden,

    /// <summary>No travel to a specific nation today.</summary>
    NationForbidden,

    /// <summary>No travel to a specific nation+era combination today.</summary>
    NationEraForbidden
}

/// <summary>
/// A daily travel restriction announced in the morning briefing. The player
/// must DENY an otherwise-valid traveler whose claimed destination violates an
/// active rule. Rules are listed on a DayPlan and evaluated per case.
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

    /// <summary>Optional custom briefing line; auto-generated if blank.</summary>
    [TextArea] public string description;

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

        string e = era != null ? era.displayName : "?";
        string n = nation != null ? nation.displayName : "?";

        switch (type)
        {
            case TravelRuleType.EraForbidden:
                return $"No travel permitted to {e}.";
            case TravelRuleType.NationForbidden:
                return $"No travel permitted to {n}.";
            case TravelRuleType.NationEraForbidden:
                return $"No travel permitted to {n} in {e}.";
            default:
                return "Travel restriction in effect.";
        }
    }
}
