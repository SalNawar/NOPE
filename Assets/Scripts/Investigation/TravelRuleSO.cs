using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A daily travel restriction announced in the morning briefing (its type,
/// TravelRuleType, is Domain's). The player must DENY an otherwise-valid
/// traveler whose claimed destination violates an active closure (a
/// directive fault, DirectiveFault.ClosedDestination), or whose papers or
/// account break a standing procedure with a predicate (the paper set, the
/// debt standing: Directives.Breaks). A standing procedure without one
/// (dress for the destination, a kind's procedure line, the displaced's
/// return home) closes no destination: its line tells the player what to
/// check. Rules are listed on a DayPlan and evaluated per case.
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

    /// <summary>The kinds the rule is read for (traveller types §5.3; written by Generate World from rules[].kinds); empty for every kind; on a closure, the kinds it closes for (the Economy range limit, days 7-15 §6.1).</summary>
    public TravellerKind[] kinds;

    /// <summary>The model a transponder recall grounds (rules[].transponder, an agency.transponders id; days 7-15 §6); blank for every other rule.</summary>
    public string transponder;

    /// <summary>True for a closure: it forbids destinations, and each active one gets a planned violator (CaseFactory.PlanViolators).</summary>
    public bool IsClosure => Directives.IsClosure(type);

    /// <summary>The rule as the Domain predicates see it: its type, kinds and a closure's place by ids.</summary>
    public Directive Directive => new Directive(type, kinds ?? System.Array.Empty<TravellerKind>(), nation != null ? nation.id : null, era != null ? era.id : null,
                                                string.IsNullOrWhiteSpace(transponder) ? null : transponder);

    /// <summary>True when the rule is read for a traveller of <paramref name="kind"/> (Directive.AppliesTo).</summary>
    public bool AppliesTo(TravellerKind kind) => Directive.AppliesTo(kind);

    /// <summary>
    /// Returns true if this rule permits the given claimed destination (the
    /// closure predicate, Directives.Closes).
    /// </summary>
    public bool Allows(NationSO claimNation, EraSO claimEra) =>
        !Directive.Closes(claimNation != null ? claimNation.id : null, claimEra != null ? claimEra.id : null);

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
