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

    /// <summary>The places an open-destinations rule leaves open ("country:era" keys, Directives.PlaceKey; rules[].places; the desk-first ramp); empty for every other rule.</summary>
    public string[] openPlaces;

    /// <summary>True for a closure: it forbids destinations, and each active one gets a planned violator (CaseFactory.PlanViolators).</summary>
    public bool IsClosure => Directives.IsClosure(type);

    /// <summary>The rule as authored: its type, kinds, a closure's place by ids and an open-destinations rule's places (the content checks and the portals' own schedule read this).</summary>
    public Directive AuthoredDirective => new Directive(type, kinds ?? System.Array.Empty<TravellerKind>(), nation != null ? nation.id : null, era != null ? era.id : null,
                                                        string.IsNullOrWhiteSpace(transponder) ? null : transponder, openPlaces);

    /// <summary>The rule as the Domain predicates see it today: as authored, an open-destinations rule also opening the routes of today's repaired portals (DayOpenings, Directives.Opening; Saleh 2026-10-18).</summary>
    public Directive Directive => Directives.Opening(AuthoredDirective, DayOpenings.Keys);

    /// <summary>True when the rule is read for a traveller of <paramref name="kind"/> (Directive.AppliesTo).</summary>
    public bool AppliesTo(TravellerKind kind) => Directive.AppliesTo(kind);

    /// <summary>
    /// Returns true if this rule permits the given claimed destination (the
    /// closure predicate, Directives.Closes).
    /// </summary>
    public bool Allows(NationSO claimNation, EraSO claimEra) =>
        !Directive.Closes(claimNation != null ? claimNation.id : null, claimEra != null ? claimEra.id : null);

    /// <summary>
    /// The rule's line today: as authored, an open-destinations rule naming
    /// after it the destinations today's repaired portals also open
    /// (DayOpenings, Directives.OpenedLine with the UI string
    /// "rule.openRepaired"), so the rulebook, the Rules app, search and the
    /// case board print what the verdicts read; its fixed words as
    /// <paramref name="surface"/> prints them (the PC's in English: UiText.Pc).
    /// </summary>
    public string Summary(TextSurface surface = TextSurface.Desk)
    {
        string line = AuthoredSummary(surface);
        if (type != TravelRuleType.OpenDestinations || DayOpenings.Names.Count == 0)
            return line;
        var opened = new List<string>();
        for (int i = 0; i < DayOpenings.Keys.Count; i++)
            if (openPlaces == null || System.Array.IndexOf(openPlaces, DayOpenings.Keys[i]) < 0)
                opened.Add(DayOpenings.Names[i]);
        return Directives.OpenedLine(line, opened, opened.Count > 0 ? UiText.Get("rule.openRepaired", surface) : null);
    }

    /// <summary>The rule's line as authored (a past day's briefing: the Mail's archive), its fixed words as <paramref name="surface"/> prints them.</summary>
    public string AuthoredSummary(TextSurface surface = TextSurface.Desk)
    {
        if (!string.IsNullOrWhiteSpace(description))
            return description;

        string Format(string key, params object[] args) => surface == TextSurface.Pc ? UiText.Pc.Format(key, args) : UiText.Format(key, args);
        string e = era != null ? era.displayName : Format("rule.unknownPlace");
        string n = nation != null ? nation.displayName : Format("rule.unknownPlace");

        switch (type)
        {
            case TravelRuleType.EraForbidden:
                return Format("rule.eraForbidden", e);
            case TravelRuleType.NationForbidden:
                return Format("rule.nationForbidden", n);
            case TravelRuleType.NationEraForbidden:
                return Format("rule.nationEraForbidden", n, e);
            default:
                return Format("rule.generic");
        }
    }
}
