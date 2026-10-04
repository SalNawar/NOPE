using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// Wave 5, Papers, Please lesson 10: the morning paper's headlines about
/// changes travellers caused name the traveller the clerk stamped and the
/// day: a carry, a panic and a world outcome (the last traveller whose
/// verdict pulled it).
/// </summary>
public class TracesTests
{
    private static OutcomePull Pull(string factor, string outcome, float amount) => new OutcomePull { factor = factor, outcome = outcome, amount = amount };

    [Test]
    public void Record_TheLastVerdictToPullEachOutcome()
    {
        var traces = new List<PullTrace>();
        Assert.AreEqual(2, Traces.Record(traces, new[] { Pull("future", "space", 4f), Pull("money", "commons", 5f), Pull("government", "democracy", 0f) }, "Iset (Scientist)", 2));
        Assert.AreEqual(1, Traces.Record(traces, new[] { Pull("future", "space", 1f) }, "Omar (Merchant)", 3));
        Assert.AreEqual(2, traces.Count, "one entry per outcome; no pull, no trace");
        PullTrace space = traces.Single(t => t.outcome == "space");
        Assert.AreEqual("Omar (Merchant)", space.traveller);
        Assert.AreEqual(3, space.day);
        Assert.AreEqual("Iset (Scientist)", traces.Single(t => t.outcome == "commons").traveller);
        Assert.AreEqual(0, Traces.Record(traces, new[] { Pull("future", "space", 1f) }, " ", 4), "no name, no trace");
        Assert.AreEqual(0, Traces.Record(null, new[] { Pull("future", "space", 1f) }, "X", 4));
    }

    [Test]
    public void Of_TheOutcomesTrace_ASplitsLaterOne()
    {
        var traces = new List<PullTrace>
        {
            new PullTrace { factor = "future", outcome = "space", traveller = "Iset", day = 2 },
            new PullTrace { factor = "future", outcome = "nuclear", traveller = "Omar", day = 4 }
        };
        Assert.AreEqual("Iset", Traces.Of(traces, new FactorLead { factor = "future", outcome = "space" }).traveller);
        Assert.AreEqual("Omar", Traces.Of(traces, new FactorLead { factor = "future", outcome = "space", split = "nuclear" }).traveller);
        Assert.AreEqual("Omar", Traces.Of(traces, new FactorLead { factor = "future", outcome = "credit", split = "nuclear" }).traveller, "one side untraced: the other");
        Assert.IsNull(Traces.Of(traces, new FactorLead { factor = "money", outcome = "debt" }));
        Assert.IsNull(Traces.Of(null, new FactorLead { factor = "future", outcome = "space" }));
    }

    [Test]
    public void Traced_TheHeadlineNamesItsFace_OrStandsAlone()
    {
        var trace = new PullTrace { factor = "future", outcome = "space", traveller = "Iset (Scientist)", day = 6 };
        Assert.AreEqual("THE SPACE AGE begins. It began at Desk 3 with Iset (Scientist), stamped on day 6.",
                        Traces.Traced("THE SPACE AGE begins.", "It began at Desk 3 with {name}, stamped on day {day}.", trace));
        Assert.AreEqual("THE SPACE AGE begins.", Traces.Traced("THE SPACE AGE begins.", "It began with {name}.", null), "no trace (an older save's pulls)");
        Assert.AreEqual("THE SPACE AGE begins.", Traces.Traced("THE SPACE AGE begins.", " ", trace), "no template");
        Assert.IsNull(Traces.Traced(null, "{name}", trace));
    }

    [Test]
    public void CarryLine_NamesTheTravellerAndTheDay_AnOlderSavesCarryKeepsTheOldLine()
    {
        var edit = new FactEdit("italy", "medieval", ClueCategory.Technology, "Wrist comm", 4, EditCause.Carry, "The present") { traveller = "Omar (Merchant)", travellerDay = 3 };
        const string old = "HISTORY: travellers brought {value} to {place}.";
        const string by = "HISTORY: {name}, stamped on day {day}, brought {value} to {place}.";
        Assert.AreEqual("HISTORY: Omar (Merchant), stamped on day 3, brought Wrist comm to Florence (Medieval).", Carries.Line(old, by, edit, "Florence (Medieval)"));
        edit.traveller = string.Empty;
        Assert.AreEqual("HISTORY: travellers brought Wrist comm to Florence (Medieval).", Carries.Line(old, by, edit, "Florence (Medieval)"));
        Assert.AreEqual(string.Empty, Carries.Line(old, by, null, "x"));
    }

    [Test]
    public void CarriesKeepTheirTraveller_ThePromotedEditTheLatestRecords()
    {
        var table = new FactTable();
        table.Add("greece", "ancient", "Athens (Ancient)", ClueCategory.Technology, "Water clock");
        table.Add("italy", "medieval", "Florence (Medieval)", ClueCategory.Technology, "Printing press");
        var history = new HistoryState();
        history.pendingCarries.Add(Carries.Make("greece", "ancient", "italy", "medieval", ClueCategory.Technology, table, 2, "Iset (Scholar)"));
        history.pendingCarries.Add(Carries.Make("greece", "ancient", "italy", "medieval", ClueCategory.Technology, table, 3, "Nakht (Scribe)"));
        Assert.AreEqual("Iset (Scholar)", history.pendingCarries[0].traveller);
        FactEdit edit = Carries.Promote(history, 1, 4, table).Single();
        Assert.AreEqual("Nakht (Scribe)", edit.traveller);
        Assert.AreEqual(3, edit.travellerDay);
        Assert.AreEqual(string.Empty, Carries.Make("greece", "ancient", "italy", "medieval", ClueCategory.Technology, table, 2).traveller, "no traveller named: blank");
    }

    [Test]
    public void PanicLines_NameTheFace_AnOlderSavesPanicKeepsTheOldLine()
    {
        var panics = new List<PanicRecord>
        {
            new PanicRecord { placeLabel = "Athens (Ancient)", item = "wrist comm", day = 2, traveller = "Iset (Scholar)" },
            new PanicRecord { placeLabel = "Athens (Ancient)", item = "sneakers", day = 2 }
        };
        CollectionAssert.AreEqual(new[] { "PANIC in Athens (Ancient): Iset (Scholar), day 2, in wrist comm.", "PANIC in Athens (Ancient): someone in sneakers." },
                                  History.PanicLines("PANIC in {place}: someone in {value}.", panics, "PANIC in {place}: {name}, day {day}, in {value}."));
        CollectionAssert.AreEqual(new[] { "PANIC in Athens (Ancient): someone in wrist comm.", "PANIC in Athens (Ancient): someone in sneakers." },
                                  History.PanicLines("PANIC in {place}: someone in {value}.", panics), "no traced template: the old line");
    }
}
