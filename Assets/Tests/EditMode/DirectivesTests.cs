using System;
using NUnit.Framework;

/// <summary>
/// The PaperDates directive (traveller types F7, P3, §5.3-5.4): a departure
/// dated another day, an expired Valid Until, in that order; unreadable dates
/// skipped; and its maker's draws (the variant, the offsets). Today is 17 Mar
/// 2150, day 4 of the agency calendar.
/// </summary>
public class DirectivesTests
{
    private static readonly DateTime Today = new DateTime(2150, 3, 17);

    private static string On(int daysFromToday) => AgencyCalendar.Write(Today.AddDays(daysFromToday));

    private static ScriptedRandom Script(params ScriptStep[] steps) => new ScriptedRandom(steps);
    private static ScriptStep R(int offset) => ScriptStep.Range(offset);

    [Test]
    public void PaperDates_HonestPapers_DepartTodayAndHaveNotExpired()
    {
        Assert.AreEqual(DirectiveFault.None, Directives.PaperDates(new[] { On(0) }, new[] { On(3), On(365) }, Today));
        Assert.AreEqual(DirectiveFault.None, Directives.PaperDates(new[] { On(0), On(0) }, new[] { On(0) }, Today), "a paper valid until today is still valid");
        Assert.AreEqual(DirectiveFault.None, Directives.PaperDates(null, null, Today), "nothing printed");
        Assert.AreEqual(DirectiveFault.None, Directives.PaperDates(new string[0], new string[0], Today));
    }

    [Test]
    public void PaperDates_ADepartureOnAnotherDay_IsTheWrongDate_EitherWay()
    {
        Assert.AreEqual(DirectiveFault.WrongDepartureDate, Directives.PaperDates(new[] { On(1) }, null, Today), "tomorrow");
        Assert.AreEqual(DirectiveFault.WrongDepartureDate, Directives.PaperDates(new[] { On(-3) }, null, Today), "three days ago");
        Assert.AreEqual(DirectiveFault.WrongDepartureDate, Directives.PaperDates(new[] { On(0), On(2) }, null, Today), "any departure printed");
    }

    [Test]
    public void PaperDates_AValidUntilBeforeToday_IsExpired()
    {
        Assert.AreEqual(DirectiveFault.ExpiredPaper, Directives.PaperDates(new[] { On(0) }, new[] { On(-1) }, Today), "yesterday");
        Assert.AreEqual(DirectiveFault.ExpiredPaper, Directives.PaperDates(null, new[] { On(30), On(-30) }, Today), "any Valid Until printed");
    }

    [Test]
    public void PaperDates_TheDepartureIsReadFirst_AndUnreadableDatesAreSkipped()
    {
        Assert.AreEqual(DirectiveFault.WrongDepartureDate, Directives.PaperDates(new[] { On(1) }, new[] { On(-1) }, Today), "both wrong: the departure names the fault");
        Assert.AreEqual(DirectiveFault.None, Directives.PaperDates(new[] { "DepartureDate:none" }, new[] { "Expiry:none", null, "" }, Today), "placeholders are no fault");
        Assert.AreEqual(DirectiveFault.ExpiredPaper, Directives.PaperDates(new[] { "DepartureDate:none" }, new[] { On(-5) }, Today));
    }

    [Test]
    public void PlanDateFault_OneDrawOverTheDatesPrinted_TheDepartureFirst()
    {
        PaperDatePlan plan = Directives.PlanDateFault(true, 2, Script(R(0)));
        Assert.AreEqual(PaperDateFault.Departure, plan.Fault);
        Assert.AreEqual(-1, plan.ExpiryIndex);

        plan = Directives.PlanDateFault(true, 2, Script(R(1)));
        Assert.AreEqual(PaperDateFault.Expiry, plan.Fault);
        Assert.AreEqual(0, plan.ExpiryIndex, "the first expiring form");

        plan = Directives.PlanDateFault(true, 2, Script(R(2)));
        Assert.AreEqual((PaperDateFault.Expiry, 1), (plan.Fault, plan.ExpiryIndex), "the second");

        plan = Directives.PlanDateFault(false, 1, Script(R(0)));
        Assert.AreEqual((PaperDateFault.Expiry, 0), (plan.Fault, plan.ExpiryIndex), "no departure printed: the draw is over the expiring forms");

        plan = Directives.PlanDateFault(true, 0, Script(R(0)));
        Assert.AreEqual(PaperDateFault.Departure, plan.Fault);
    }

    [Test]
    public void PlanDateFault_NothingPrinted_OrNoStream_IsNone_WithNoDraw()
    {
        Assert.AreEqual(PaperDateFault.None, Directives.PlanDateFault(false, 0, Script()).Fault);
        Assert.AreEqual(PaperDateFault.None, Directives.PlanDateFault(false, -2, Script()).Fault);
        Assert.AreEqual(PaperDateFault.None, Directives.PlanDateFault(true, 1, null).Fault);
    }

    [Test]
    public void OffsetDeparture_OneDraw_OneToThreeDaysEitherWay_NeverToday()
    {
        var seen = new System.Collections.Generic.List<int>();
        for (int pick = 0; pick < Directives.DepartureOffsetMaxDays * 2; pick++)
        {
            DateTime date = Directives.OffsetDeparture(Today, Script(R(pick)));
            int offset = (date - Today).Days;
            Assert.AreNotEqual(0, offset, $"pick {pick}");
            Assert.LessOrEqual(Math.Abs(offset), Directives.DepartureOffsetMaxDays, $"pick {pick}");
            seen.Add(offset);
        }
        CollectionAssert.AreEqual(new[] { -3, -2, -1, 1, 2, 3 }, seen, "every offset once, in draw order");
        Assert.AreEqual(DirectiveFault.WrongDepartureDate, Directives.PaperDates(new[] { AgencyCalendar.Write(Directives.OffsetDeparture(Today, Script(R(5)))) }, null, Today), "the maker's date breaks the directive");
    }

    [Test]
    public void ExpiredValidUntil_OneDraw_OneToThirtyDaysAgo()
    {
        Assert.AreEqual(Today.AddDays(-1), Directives.ExpiredValidUntil(Today, Script(R(0))));
        Assert.AreEqual(Today.AddDays(-30), Directives.ExpiredValidUntil(Today, Script(R(29))));
        Assert.AreEqual(Today.AddDays(-30), Directives.ExpiredValidUntil(Today, Script(R(99))), "the draw is clamped to the range");
        Assert.AreEqual(DirectiveFault.ExpiredPaper, Directives.PaperDates(null, new[] { AgencyCalendar.Write(Directives.ExpiredValidUntil(Today, Script(R(0)))) }, Today), "the maker's date breaks the directive");
    }
}
