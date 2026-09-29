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
    [TestCase(ClueCategory.WaiverNo)]
    [TestCase(ClueCategory.PolicyNo)]
    [TestCase(ClueCategory.Signature)]
    public void AFactOrAName_RunsToABookRow(ClueCategory category)
    {
        Assert.AreEqual(FactTable.MaxValueLength, FieldLengths.Longest(category, 40));
    }

    /// <summary>The contract's boxes (phase 9): a wage as wide as the widest debt, a term as wide as the longest term ("99999 days"), an employer a book row.</summary>
    [Test]
    public void TheContractsRows_AWageLikeADebt_ATermOfFiveDigits_AnEmployerLikeARow()
    {
        Assert.AreEqual(FieldLengths.Longest(ClueCategory.Debt, 40), FieldLengths.Longest(ClueCategory.Wage, 40));
        Assert.AreEqual("99999 days".Length, FieldLengths.Longest(ClueCategory.Term, 40));
        Assert.AreEqual(FactTable.MaxValueLength, FieldLengths.Longest(ClueCategory.Employer, 40));
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

    /// <summary>Phase 8: a credit line and savings are amounts of credits like the debt; a waiver or policy number and a signature (a name) run to a book row (AgencyContent.Problems holds every number prefix to it).</summary>
    [Test]
    public void TheProofsOfMeans_AreAmounts_TheNumbersAndTheSignature_RunToABookRow()
    {
        Assert.AreEqual(AccountMaker.Credits(AccountRanges.MaxDebt).Length, FieldLengths.Longest(ClueCategory.Credit, 40));
        Assert.AreEqual(AccountMaker.Credits(AccountRanges.MaxDebt).Length, FieldLengths.Longest(ClueCategory.Funds, 40));
        Assert.AreEqual(FactTable.MaxValueLength, FieldLengths.Longest(ClueCategory.WaiverNo, 40));
        Assert.AreEqual(FactTable.MaxValueLength, FieldLengths.Longest(ClueCategory.PolicyNo, 40));
        Assert.AreEqual(FactTable.MaxValueLength, FieldLengths.Longest(ClueCategory.Signature, 40));
    }
}
