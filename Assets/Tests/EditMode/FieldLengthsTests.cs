using System;
using NUnit.Framework;

/// <summary>
/// The longest value a document field can print, by category (redesign phase
/// 4, PC spec FO6): a form's box reserves the lines this needs at the floor,
/// and FormLayout.Check measures a probe of this length. Place facts and names
/// run to a book row, an origin to the longest of today's origin labels, a
/// birth date to the widest date BirthDates writes, the agency's numbers and
/// dates to their makers' fixed widths.
/// </summary>
public class FieldLengthsTests
{
    /// <summary>An rng that always draws the top of each range (the widest numbers).</summary>
    private sealed class Top : IRandomSource
    {
        public int Range(int minInclusive, int maxExclusive) => maxExclusive - 1;
        public float Value() => 0.999f;
    }

    [TestCase(ClueCategory.Language)]
    [TestCase(ClueCategory.Currency)]
    [TestCase(ClueCategory.Technology)]
    [TestCase(ClueCategory.Culture)]
    [TestCase(ClueCategory.Name)]
    public void AFactOrAName_RunsToABookRow(ClueCategory category)
    {
        Assert.AreEqual(FactTable.MaxValueLength, FieldLengths.Longest(category, 40));
    }

    [Test]
    public void ABirthDate_IsTheWidestDateBirthDatesWrites()
    {
        Assert.AreEqual("28 Sep 9999 BCE".Length, FieldLengths.Longest(ClueCategory.BirthDate, 40));
        foreach (int year in new[] { -9999, -1505, -1, 1, 830, 2150, 9999 })
            for (int month = 0; month < 12; month++)
                Assert.LessOrEqual(BirthDates.Format(28, month, year).Length, FieldLengths.Longest(ClueCategory.BirthDate, 40));
    }

    [Test]
    public void AnOrigin_IsTheLongestOriginLabel()
    {
        Assert.AreEqual(43, FieldLengths.Longest(ClueCategory.Destination, 43));
    }

    [Test]
    public void TheAgencysNumbersAndDates_AreTheirMakersWidths()
    {
        Assert.AreEqual(Math.Max(AgencyNumbers.DisplacementNumber(new Top()).Length, AccountMaker.CitizenId(new Top()).Length), FieldLengths.Longest(ClueCategory.CitizenId, 40), "a Displacement No. or a Citizen ID, the wider");
        Assert.AreEqual("418-0937-52".Length, FieldLengths.Longest(ClueCategory.CitizenId, 40));
        Assert.AreEqual(AgencyNumbers.IncidentNumber(new DateTime(2150, 12, 28), new Top()).Length, FieldLengths.Longest(ClueCategory.Incident, 40));
        Assert.AreEqual(AgencyCalendar.Write(new DateTime(2150, 9, 28)).Length, FieldLengths.Longest(ClueCategory.Expiry, 40));
        Assert.AreEqual(AgencyCalendar.Write(new DateTime(2150, 9, 28)).Length, FieldLengths.Longest(ClueCategory.DepartureDate, 40));
    }

    /// <summary>Phase 6: an account's status and transponder class are their enum's longest name; a transponder's printed name runs to a book row (AccountRanges.Problems holds every model to it); a debt to the widest amount of credits the accounts may hold.</summary>
    [Test]
    public void TheAccountsValues_AreTheirWidths()
    {
        Assert.AreEqual("Standard".Length, FieldLengths.Longest(ClueCategory.AccountStatus, 40));
        foreach (CitizenStatus s in (CitizenStatus[])Enum.GetValues(typeof(CitizenStatus)))
            Assert.LessOrEqual(s.ToString().Length, FieldLengths.Longest(ClueCategory.AccountStatus, 40));
        Assert.AreEqual("Premium".Length, FieldLengths.Longest(ClueCategory.TransponderClass, 40));
        Assert.AreEqual(FactTable.MaxValueLength, FieldLengths.Longest(ClueCategory.TransponderId, 40));
        Assert.AreEqual(AccountMaker.Credits(AccountRanges.MaxDebt).Length, FieldLengths.Longest(ClueCategory.Debt, 40));
        Assert.AreEqual("9,999,999 cr", AccountMaker.Credits(AccountRanges.MaxDebt));
    }
}
