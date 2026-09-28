using System;
using System.Collections.Generic;

/// <summary>Which date a planned paper-dates fault falsifies (Directives.PlanDateFault).</summary>
public enum PaperDateFault
{
    /// <summary>No date fault (nothing printed to falsify).</summary>
    None,

    /// <summary>The departure date (the manifest's or the return order's) is another day than today.</summary>
    Departure,

    /// <summary>One Valid Until (the visa's, a proof of means' or the certificate's) has passed.</summary>
    Expiry
}

/// <summary>A traveller's planned paper-dates fault: the variant and, for an expiry, which of the expiring forms (in paper order).</summary>
public readonly struct PaperDatePlan
{
    /// <summary>The variant (None: nothing to falsify).</summary>
    public readonly PaperDateFault Fault;

    /// <summary>The index of the expiring form whose Valid Until has passed, among the expiring forms in paper order; -1 unless <see cref="PaperDateFault.Expiry"/>.</summary>
    public readonly int ExpiryIndex;

    /// <summary>Creates a plan.</summary>
    public PaperDatePlan(PaperDateFault fault, int expiryIndex)
    {
        Fault = fault;
        ExpiryIndex = expiryIndex;
    }
}

/// <summary>
/// The standing procedures' predicates and makers (traveller types P3, F7,
/// §5.3-5.4): what a directive forbids in the presented papers, read against
/// them and the agency calendar with no evidence needed. PaperDates (from day
/// 4): depart only on the date on the manifest or the return order, and never
/// on an expired paper; its faulty traveller is made by falsifying one date.
/// NoPresentGoods (from day 4) has no predicate: a smuggler breaks it, a
/// deviation fault (LieKind.Smuggling) proven against the books. The closures
/// stay TravelRuleSO.Allows, the dress rule CostumeErrors. Pure and seeded,
/// so every table and draw is tested headless.
/// </summary>
public static class Directives
{
    /// <summary>How many days off, at most, a falsified departure is (1 to 3 days before or after today, §5.4).</summary>
    public const int DepartureOffsetMaxDays = 3;

    /// <summary>How many days ago, at most, a falsified Valid Until passed (1 to 30 days before today, §5.4).</summary>
    public const int ExpiredMaxDays = 30;

    /// <summary>
    /// The PaperDates directive (F7): the first fault it finds, in this
    /// order: a readable departure date that is not <paramref name="today"/>
    /// (DirectiveFault.WrongDepartureDate), then a readable Valid Until before
    /// today (DirectiveFault.ExpiredPaper); None when every readable date
    /// passes. A date the calendar cannot read (a placeholder) is skipped:
    /// a broken calendar is the content's problem, never the traveller's.
    /// Null lists count as empty.
    /// </summary>
    public static DirectiveFault PaperDates(IEnumerable<string> departures, IEnumerable<string> validUntils, DateTime today)
    {
        if (departures != null)
            foreach (string text in departures)
                if (AgencyCalendar.TryRead(text, out DateTime departure) && departure.Date != today.Date)
                    return DirectiveFault.WrongDepartureDate;

        if (validUntils != null)
            foreach (string text in validUntils)
                if (AgencyCalendar.TryRead(text, out DateTime validUntil) && validUntil.Date < today.Date)
                    return DirectiveFault.ExpiredPaper;

        return DirectiveFault.None;
    }

    /// <summary>
    /// The maker's variant (§5.4), one Range draw over the dates the papers
    /// print: the departure when <paramref name="hasDeparture"/>, then each of
    /// the <paramref name="expiringForms"/> Valid Untils in paper order. None,
    /// with no draw, when nothing is printed or <paramref name="rng"/> is null.
    /// </summary>
    public static PaperDatePlan PlanDateFault(bool hasDeparture, int expiringForms, IRandomSource rng)
    {
        int options = (hasDeparture ? 1 : 0) + Math.Max(0, expiringForms);
        if (rng == null || options == 0)
            return new PaperDatePlan(PaperDateFault.None, -1);

        int pick = rng.Range(0, options);
        if (hasDeparture && pick == 0)
            return new PaperDatePlan(PaperDateFault.Departure, -1);

        return new PaperDatePlan(PaperDateFault.Expiry, hasDeparture ? pick - 1 : pick);
    }

    /// <summary>A departure 1 to <see cref="DepartureOffsetMaxDays"/> days before or after <paramref name="today"/>, never today: one Range draw over the six offsets.</summary>
    public static DateTime OffsetDeparture(DateTime today, IRandomSource rng)
    {
        int pick = rng.Range(0, DepartureOffsetMaxDays * 2);
        int offset = pick < DepartureOffsetMaxDays ? pick - DepartureOffsetMaxDays : pick - DepartureOffsetMaxDays + 1;
        return today.AddDays(offset);
    }

    /// <summary>A Valid Until 1 to <see cref="ExpiredMaxDays"/> days before <paramref name="today"/>: one draw (AgencyNumbers.DaysAgo).</summary>
    public static DateTime ExpiredValidUntil(DateTime today, IRandomSource rng) => AgencyNumbers.DaysAgo(today, ExpiredMaxDays, rng);
}
