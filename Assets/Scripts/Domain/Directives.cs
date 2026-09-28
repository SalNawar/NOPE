using System.Collections.Generic;

/// <summary>
/// The Directives' rules over the rule types (traveller types P3, P4, §5.3):
/// which types close destinations, which day a rule first stands on, and
/// which rules guarantee a faulty traveller in the first half of the queue
/// (CaseFactory.PlanViolators asks each such rule's maker for its slot).
/// Pure, so the decision tables are tested headless.
/// </summary>
public static class Directives
{
    /// <summary>True for the closure types (a forbidden era, nation or place), which forbid destinations; false for a standing procedure.</summary>
    public static bool IsClosure(TravelRuleType type) =>
        type == TravelRuleType.EraForbidden || type == TravelRuleType.NationForbidden || type == TravelRuleType.NationEraForbidden;

    /// <summary>
    /// A rule's first day: the smallest of <paramref name="daysListed"/>
    /// (the day numbers of the plans that list it); 0 when no plan lists it
    /// (a null list counts as empty).
    /// </summary>
    public static int FirstDay(IEnumerable<int> daysListed)
    {
        int first = 0;
        if (daysListed == null)
            return first;

        foreach (int day in daysListed)
            if (first == 0 || day < first)
                first = day;

        return first;
    }

    /// <summary>
    /// Whether an active rule plans a guaranteed faulty traveller in the
    /// first half of today's queue (P4): a closure every day it is active
    /// (a traveller bound for a place it forbids); the displaced's return
    /// home on its first day (a false-origin liar, L7 or L8, on the day the
    /// rule is announced); never a procedure line or the dress rule (its
    /// costume errors come from the costume roll).
    /// </summary>
    public static bool Guarantees(TravelRuleType type, int today, int firstDay) =>
        IsClosure(type) || (type == TravelRuleType.ReturnHome && today == firstDay);
}
