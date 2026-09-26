using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;

public class AgencyNumbersTests
{
    private static readonly DateTime Today = new DateTime(2150, 3, 14);

    [Test]
    public void DisplacementNumber_IsDP_FourDigits_TwoDigits_FromTwoDraws()
    {
        var rng = new ScriptedRandom(ScriptStep.Range(4471), ScriptStep.Range(2));
        Assert.AreEqual("DP-4471-02", AgencyNumbers.DisplacementNumber(rng));
        Assert.IsTrue(rng.Done);
        Assert.AreEqual("DP-0000-00", AgencyNumbers.DisplacementNumber(new ScriptedRandom(ScriptStep.Range(0), ScriptStep.Range(0))), "zero-padded");
        Assert.AreEqual("DP-9999-99", AgencyNumbers.DisplacementNumber(new ScriptedRandom(ScriptStep.Range(9999), ScriptStep.Range(99))));
    }

    [Test]
    public void IncidentNumber_IsR_TheDatesMonthAndDay_ThenASerialFrom01()
    {
        var rng = new ScriptedRandom(ScriptStep.Range(6));
        Assert.AreEqual("R-0311-07", AgencyNumbers.IncidentNumber(new DateTime(2150, 3, 11), rng), "serial 1-99: offset 6 is 07");
        Assert.IsTrue(rng.Done);
        Assert.AreEqual("R-1231-01", AgencyNumbers.IncidentNumber(new DateTime(2149, 12, 31), new ScriptedRandom(ScriptStep.Range(0))));
    }

    [Test]
    public void DaysAgo_OneToMaxDaysBeforeToday_AcrossMonthAndYear()
    {
        Assert.AreEqual(new DateTime(2150, 3, 13), AgencyNumbers.DaysAgo(Today, 30, new ScriptedRandom(ScriptStep.Range(0))), "at least a day ago");
        Assert.AreEqual(new DateTime(2150, 2, 12), AgencyNumbers.DaysAgo(Today, 30, new ScriptedRandom(ScriptStep.Range(29))), "at most 30 days ago (2150 is no leap year)");
        Assert.AreEqual(new DateTime(2149, 12, 27), AgencyNumbers.DaysAgo(new DateTime(2150, 1, 1), 30, new ScriptedRandom(ScriptStep.Range(4))), "into the last year");
    }

    [Test]
    public void DaysAhead_MinToMaxDaysAfterToday()
    {
        Assert.AreEqual(new DateTime(2150, 3, 17), AgencyNumbers.DaysAhead(Today, 3, 365, new ScriptedRandom(ScriptStep.Range(0))));
        Assert.AreEqual(new DateTime(2151, 3, 14), AgencyNumbers.DaysAhead(Today, 3, 365, new ScriptedRandom(ScriptStep.Range(362))));
        Assert.AreEqual(new DateTime(2150, 3, 17), AgencyNumbers.DaysAhead(Today, 5, 3, new ScriptedRandom(ScriptStep.Range(0))), "reversed bounds are swapped");
    }

    [Test]
    public void Displaced_DrawsInTheFixedOrder_NumberFoundIncidentValidUntil()
    {
        var rng = new ScriptedRandom(
            ScriptStep.Range(4471), ScriptStep.Range(2),   // the number
            ScriptStep.Range(2),                           // found 3 days ago: 11 Mar
            ScriptStep.Range(6),                           // the incident's serial
            ScriptStep.Range(10));                         // valid 13 days ahead
        var taken = new HashSet<string>();

        DisplacementFile file = AgencyNumbers.Displaced(Today, Ranges(), taken, rng);

        Assert.IsTrue(rng.Done, "five draws");
        Assert.AreEqual("DP-4471-02", file.Number);
        Assert.AreEqual("11 Mar 2150", file.Found);
        Assert.AreEqual("R-0311-07", file.Incident, "the incident's date is the day they were found");
        Assert.AreEqual("27 Mar 2150", file.ValidUntil);
        CollectionAssert.Contains(taken, "DP-4471-02", "the number is taken for the rest of the day");
    }

    [Test]
    public void Displaced_ANumberTakenToday_IsDrawnAgain()
    {
        var taken = new HashSet<string> { "DP-4471-02" };
        var rng = new ScriptedRandom(
            ScriptStep.Range(4471), ScriptStep.Range(2),
            ScriptStep.Range(4471), ScriptStep.Range(3),
            ScriptStep.Range(0), ScriptStep.Range(0), ScriptStep.Range(0));

        DisplacementFile file = AgencyNumbers.Displaced(Today, Ranges(), taken, rng);

        Assert.AreEqual("DP-4471-03", file.Number);
        Assert.IsTrue(rng.Done);
        Assert.AreEqual(2, taken.Count);
    }

    [Test]
    public void Displaced_SeededDay_EveryNumberUnique_EveryDateInRange()
    {
        var taken = new HashSet<string>();
        for (int slot = 1; slot <= 14; slot++)
        {
            var rng = new SeededRandom(Seeds.ForAccount(Seeds.ForCase(Seeds.Day(12345, 1), slot)));
            DisplacementFile file = AgencyNumbers.Displaced(Today, Ranges(), taken, rng);
            StringAssert.IsMatch(@"^DP-\d{4}-\d{2}$", file.Number);
            StringAssert.IsMatch(@"^R-\d{4}-\d{2}$", file.Incident);
            Assert.IsTrue(BirthDates.TryParse(file.Found, out int fd, out int fm, out int fy));
            Assert.IsTrue(BirthDates.TryParse(file.ValidUntil, out int vd, out int vm, out int vy));
            double ago = (Today - new DateTime(fy, fm + 1, fd)).TotalDays;
            double ahead = (new DateTime(vy, vm + 1, vd) - Today).TotalDays;
            Assert.That(ago, Is.InRange(1, 30), file.Found);
            Assert.That(ahead, Is.InRange(3, 365), file.ValidUntil);
            Assert.AreEqual(Regex.Match(file.Incident, @"R-(\d{2})(\d{2})").Groups[1].Value, (fm + 1).ToString("D2"), "the incident's month");
        }
        Assert.AreEqual(14, taken.Count, "14 travellers, 14 numbers");
    }

    [Test]
    public void TakeUnique_KeepsDrawing_UntilAFreeValue()
    {
        var taken = new HashSet<string> { "a", "b" };
        var draws = new Queue<string>(new[] { "a", "b", "c" });
        Assert.AreEqual("c", AgencyNumbers.TakeUnique(taken, () => draws.Dequeue()));
        CollectionAssert.Contains(taken, "c");
        Assert.AreEqual(0, draws.Count);
    }

    [Test]
    public void TakeUnique_GivesUp_AfterTheAttemptLimit_WithTheLastDraw()
    {
        var taken = new HashSet<string> { "a" };
        int calls = 0;
        Assert.AreEqual("a", AgencyNumbers.TakeUnique(taken, () => { calls++; return "a"; }), "a full space cannot hang generation");
        Assert.AreEqual(AgencyNumbers.MaxAttempts, calls);
    }

    /// <summary>The spec's first cut: found in the last 30 days, a certificate valid 3 to 365 days.</summary>
    private static DisplacementRanges Ranges() => new DisplacementRanges { foundWithinDays = 30, validDaysMin = 3, validDaysMax = 365 };
}
