using NUnit.Framework;

/// <summary>
/// The agency's calendar (redesign phase 2; the traveller-types spec's F6):
/// day 1 is the agency block's firstDate and today is firstDate + day - 1,
/// written as BirthDates writes dates; the block's own checks.
/// </summary>
public class AgencyCalendarTests
{
    private const string First = "14 Mar 2150";

    [Test]
    public void Today_DayOneIsTheFirstDate()
    {
        Assert.AreEqual(First, AgencyCalendar.Today(First, 1));
    }

    [Test]
    public void Today_CountsDaysFromTheFirstDate()
    {
        Assert.AreEqual("15 Mar 2150", AgencyCalendar.Today(First, 2));
        Assert.AreEqual("28 Mar 2150", AgencyCalendar.Today(First, 15));
    }

    [Test]
    public void Today_RollsOverMonthsAndYears()
    {
        Assert.AreEqual("31 Mar 2150", AgencyCalendar.Today(First, 18));
        Assert.AreEqual("1 Apr 2150", AgencyCalendar.Today(First, 19), "March has 31 days");
        Assert.AreEqual("1 Jan 2151", AgencyCalendar.Today("30 Dec 2150", 3));
        Assert.AreEqual("1 Mar 2150", AgencyCalendar.Today("28 Feb 2150", 2), "2150 is not a leap year");
        Assert.AreEqual("29 Feb 2152", AgencyCalendar.Today("28 Feb 2152", 2), "2152 is");
    }

    [Test]
    public void Today_IsWrittenAsBirthDatesWritesDates()
    {
        string today = AgencyCalendar.Today(First, 19);
        Assert.IsTrue(BirthDates.TryParse(today, out int day, out int month, out int year));
        Assert.AreEqual(BirthDates.Format(day, month, year), today, "no leading zero, the month's abbreviation");
        Assert.AreEqual((1, 3, 2150), (day, month, year));
    }

    [Test]
    public void Today_NullForAnUnreadableFirstDateOrADayBeforeOne()
    {
        Assert.IsNull(AgencyCalendar.Today(null, 1));
        Assert.IsNull(AgencyCalendar.Today("Monday", 1));
        Assert.IsNull(AgencyCalendar.Today("31 Feb 2150", 1), "no such day");
        Assert.IsNull(AgencyCalendar.Today("3 Jun 1450 BCE", 1), "the agency's calendar is after the common era's start");
        Assert.IsNull(AgencyCalendar.Today(First, 0));
    }

    [Test]
    public void FirstDateProblem_NullForAReadableDate()
    {
        Assert.IsNull(AgencyCalendar.FirstDateProblem(First));
    }

    [Test]
    public void FirstDateProblem_NamesTheBadDate()
    {
        foreach (string bad in new[] { "", "Monday", "31 Feb 2150", "14 Mar 1450 BCE", "14 March 2150" })
        {
            string problem = AgencyCalendar.FirstDateProblem(bad);
            Assert.IsNotNull(problem, bad);
            StringAssert.Contains("agency.firstDate", problem);
        }
    }

    [Test]
    public void AgencyContent_Problems_NoneWhenComplete()
    {
        var agency = new AgencyContent { name = "TEMPORAL CUSTOMS", programme = "Debt Relief Departures", firstDate = First };
        CollectionAssert.IsEmpty(agency.Problems());
    }

    [Test]
    public void AgencyContent_Problems_OnePerBlankOrBadField()
    {
        var agency = new AgencyContent { name = " ", programme = null, firstDate = "soon" };
        var problems = agency.Problems();
        Assert.AreEqual(3, problems.Count, string.Join(" | ", problems));
        StringAssert.Contains("agency.name", problems[0]);
        StringAssert.Contains("agency.programme", problems[1]);
        StringAssert.Contains("agency.firstDate", problems[2]);
    }
}
