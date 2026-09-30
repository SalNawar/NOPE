using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// The strandings' fates (the endings and strandings spec §6; Saleh's answers
/// Q10-Q13): the column by waiver, a personality's tilt, the paper's line by
/// era with news.stranded as the fallback and none for the forgotten, the
/// failure report's lines, and the fate table's and report's content rules.
/// </summary>
public class StrandingFatesTests
{
    /// <summary>Saleh's Q11 odds: signed 40/25/20/10/5, unsigned 20/20/20/15/25.</summary>
    private static List<StrandingFateRow> Table() => new List<StrandingFateRow>
    {
        new StrandingFateRow { id = "forgotten", fate = StrandingFate.Forgotten, weightWaivered = 40, weightUnwaivered = 20, status = "Status: not recovered." },
        new StrandingFateRow { id = "news", fate = StrandingFate.News, weightWaivered = 25, weightUnwaivered = 20, status = "Status: not recovered.",
            lines = { new StrandingFateLine { text = "Chroniclers in {place} record a stranger called {name}." }, new StrandingFateLine { era = "ancient", text = "An oracle in {place} is replaced by {name}." } } },
        new StrandingFateRow { id = "carry", fate = StrandingFate.Carry, weightWaivered = 20, weightUnwaivered = 20, status = "Status: not recovered." },
        new StrandingFateRow { id = "tremor", fate = StrandingFate.Tremor, weightWaivered = 10, weightUnwaivered = 15, stability = 1, status = "Status: not recovered.",
            lines = { new StrandingFateLine { text = "TIMELINE TREMOR near {place}." } } },
        new StrandingFateRow { id = "police", fate = StrandingFate.Police, weightWaivered = 5, weightUnwaivered = 25, status = "Status: removed. File closed.",
            lines = { new StrandingFateLine { text = "TIME POLICE: a traveller was removed from {place}." } } }
    };

    private static readonly string[] Eras = { "ancient", "medieval" };

    [Test]
    public void Pick_ReadsTheWaiversColumn()
    {
        List<StrandingFateRow> t = Table();
        // Waivered cumulative: 40 | 65 | 85 | 95 | 100 of 100.
        Assert.AreEqual(StrandingFate.Forgotten, StrandingFates.Pick(t, true, null, 2f, 0.39f).fate);
        Assert.AreEqual(StrandingFate.News, StrandingFates.Pick(t, true, null, 2f, 0.41f).fate);
        Assert.AreEqual(StrandingFate.Police, StrandingFates.Pick(t, true, null, 2f, 0.96f).fate);
        // Unwaivered cumulative: 20 | 40 | 60 | 75 | 100 of 100.
        Assert.AreEqual(StrandingFate.News, StrandingFates.Pick(t, false, null, 2f, 0.39f).fate);
        Assert.AreEqual(StrandingFate.Police, StrandingFates.Pick(t, false, null, 2f, 0.76f).fate, "without a waiver the Time Police take a quarter");
    }

    [Test]
    public void Pick_ATiltMultipliesOneFatesWeight()
    {
        List<StrandingFateRow> t = Table();
        // Waivered with News doubled: 40 | 90 | 110 | 120 | 125 of 125; 0.5 lands at 62.5, inside News.
        Assert.AreEqual(StrandingFate.News, StrandingFates.Pick(t, true, StrandingFate.News, 2f, 0.5f).fate);
        Assert.AreEqual(StrandingFate.Carry, StrandingFates.Pick(t, true, null, 2f, 0.7f).fate, "untilted, 70 is a carry");
        Assert.AreEqual(StrandingFate.News, StrandingFates.Pick(t, true, StrandingFate.News, 2f, 0.7f).fate, "tilted, 87.5 of 125 is still news");
        Assert.AreEqual(StrandingFate.Carry, StrandingFates.Pick(t, true, StrandingFate.News, 1f, 0.7f).fate, "a factor of 1 tilts nothing");
    }

    [Test]
    public void Pick_NoRowsOrNoWeight_IsNull_AndOf_FindsAFatesRow()
    {
        Assert.IsNull(StrandingFates.Pick(null, true, null, 2f, 0.5f));
        Assert.IsNull(StrandingFates.Pick(new List<StrandingFateRow> { new StrandingFateRow { fate = StrandingFate.News } }, true, null, 2f, 0.5f));
        Assert.AreEqual("tremor", StrandingFates.Of(Table(), StrandingFate.Tremor).id);
        Assert.IsNull(StrandingFates.Of(null, StrandingFate.Tremor));
    }

    [Test]
    public void Line_ByEraElseAnyEra_TheFallbackWithoutLines_NoneForTheForgotten()
    {
        List<StrandingFateRow> t = Table();
        StrandingFateRow news = StrandingFates.Of(t, StrandingFate.News);
        Assert.AreEqual("An oracle in Babylonia is replaced by Pell.", StrandingFates.Line(news, "ancient", "fallback", "Pell", "Babylonia", 0.9f), "the era's own line first");
        Assert.AreEqual("Chroniclers in Kyoto record a stranger called Pell.", StrandingFates.Line(news, "medieval", "fallback", "Pell", "Kyoto", 0.9f), "no line of the era: any era's");
        Assert.AreEqual("Stranded: Pell, lost in Kyoto.",
                        StrandingFates.Line(StrandingFates.Of(t, StrandingFate.Carry), "medieval", "Stranded: {name}, lost in {place}.", "Pell", "Kyoto", 0.3f), "no lines: news.stranded");
        Assert.AreEqual(string.Empty, StrandingFates.Line(StrandingFates.Of(t, StrandingFate.Forgotten), "ancient", "Stranded: {name}.", "Pell", "Babylonia", 0.3f), "Q12: the forgotten make no paper");
        Assert.AreEqual(string.Empty, StrandingFates.Line(null, "ancient", "x {place}", "Pell", "Babylonia", 0.3f));
        Assert.AreEqual(string.Empty, StrandingFates.Line(StrandingFates.Of(t, StrandingFate.Carry), "ancient", " ", "Pell", "Babylonia", 0.3f), "no fallback either");
    }

    [Test]
    public void Line_PicksWithinThePoolByTheValue()
    {
        var row = new StrandingFateRow
        {
            fate = StrandingFate.Police,
            lines = { new StrandingFateLine { text = "A {place}" }, new StrandingFateLine { text = "B {place}" } }
        };
        Assert.AreEqual("A x", StrandingFates.Line(row, null, null, "n", "x", 0f));
        Assert.AreEqual("B x", StrandingFates.Line(row, null, null, "n", "x", 0.5f));
        Assert.AreEqual("B x", StrandingFates.Line(row, null, null, "n", "x", 0.9999f));
    }

    [Test]
    public void Report_UnitTravellerWaiverFineStatus_InOrder()
    {
        var content = new StrandingReportContent
        {
            unit = "Unit {unit} failed in {place}.",
            traveller = "Traveller: {name}, {id}.",
            waivered = "Waiver {waiver} on file: the traveller's debt of {debt} passes to kin.",
            unwaivered = "No valid signed waiver on file. Liability referred to Desk 3.",
            fine = "A stranding fine of {fine} has been charged to Desk 3."
        };
        var signed = new StrandingRecord
        {
            travellerName = "Pell Quimby", placeLabel = "Periclean Athens", citizenId = "773-5512-08", transponder = "HP-40718",
            waiverNo = "SW-204817", debt = 212000, waivered = true
        };
        CollectionAssert.AreEqual(new[]
        {
            "Unit HP-40718 failed in Periclean Athens.",
            "Traveller: Pell Quimby, 773-5512-08.",
            "Waiver SW-204817 on file: the traveller's debt of 212,000 cr passes to kin.",
            "Status: not recovered."
        }, StrandingFates.Report(signed, content, "Status: not recovered.", n => n.ToString("N0", System.Globalization.CultureInfo.InvariantCulture) + " cr"));

        var unsigned = new StrandingRecord { travellerName = "Hori", placeLabel = "Babylonia", citizenId = "1", transponder = "HP-1", waiverNo = "SW-1", fine = 100 };
        List<string> lines = StrandingFates.Report(unsigned, content, "Status: removed. File closed.", n => n + " cr");
        Assert.AreEqual("No valid signed waiver on file. Liability referred to Desk 3.", lines[2]);
        Assert.AreEqual("A stranding fine of 100 cr has been charged to Desk 3.", lines[3]);
        Assert.AreEqual("Status: removed. File closed.", lines[4]);
        CollectionAssert.IsEmpty(StrandingFates.Report(null, content, "s", null));
    }

    [Test]
    public void Problems_TheFirstCutIsSound()
    {
        CollectionAssert.IsEmpty(StrandingFates.Problems(Table(), Eras));
    }

    [Test]
    public void Problems_NameEachBreak()
    {
        List<StrandingFateRow> t = Table();
        t[0].lines.Add(new StrandingFateLine { text = "x {place}" });
        t[1].lines.Add(new StrandingFateLine { era = "future", text = "{name} {count}" });
        t[3].stability = 0f;
        t[4].stability = 2f;
        t[2].status = " ";
        t.RemoveAll(r => r.fate == StrandingFate.Carry);
        t[0].weightWaivered = -1f;
        string all = string.Join("\n", StrandingFates.Problems(t, Eras));

        StringAssert.Contains("a forgotten traveller makes no paper", all);
        StringAssert.Contains("names the era 'future'", all);
        StringAssert.Contains("must hold {place}", all);
        StringAssert.Contains("holds {count}", all);
        StringAssert.Contains("a tremor's stability is 0", all);
        StringAssert.Contains("only a tremor moves stability", all);
        StringAssert.Contains("has no Carry row", all);
        StringAssert.Contains("a weight is negative", all);
        StringAssert.Contains("agency.strandingFates is empty", string.Join("\n", StrandingFates.Problems(null, Eras)));
    }

    [Test]
    public void Problems_AColumnWithNoWeight()
    {
        List<StrandingFateRow> t = Table();
        foreach (StrandingFateRow r in t)
            r.weightUnwaivered = 0f;
        StringAssert.Contains("no fate has an unwaivered weight above 0", string.Join("\n", StrandingFates.Problems(t, Eras)));
    }

    [Test]
    public void ReportProblems_EachLineAndItsTokens()
    {
        var content = new StrandingReportContent { unit = "Unit {unit} failed.", traveller = "{name}", waivered = "{waiver} {debt}", unwaivered = "", fine = "{fine}" };
        string all = string.Join("\n", StrandingFates.ReportProblems(content));
        StringAssert.Contains("agency.strandingReport.unit must hold {place}", all);
        StringAssert.Contains("agency.strandingReport.traveller must hold {id}", all);
        StringAssert.Contains("agency.strandingReport.unwaivered is blank", all);
        Assert.AreEqual(3, StrandingFates.ReportProblems(content).Count);
        StringAssert.Contains("is missing", StrandingFates.ReportProblems(null).Single());
    }
}
