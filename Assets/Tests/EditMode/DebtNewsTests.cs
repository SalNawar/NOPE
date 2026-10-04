using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// The morning paper's debt line (redesign phase 13; the traveller-types
/// spec's §10, T1): one line of the news.debt pool a day, in the run's own
/// shuffled order (Seeds.ForDebtNews), so no line repeats before the pool has
/// run through; and the pool's content checks.
/// </summary>
public class DebtNewsTests
{
    /// <summary>The returned travellers' desk lines (wave 5, lesson 9).</summary>
    private const string Through = "Second time lucky: {name}, turned away on day {day}.";
    private const string Again = "{name} was turned away on day {day} and again on day {back}.";

    private static readonly string[] Pool =
    {
        "Debt Relief Departures reach a record high.",
        "CLEAR YOUR DEBT: 180 days in the Victorian mills.",
        "12,000 accounts frozen in default.",
        "Premium families book Periclean Athens for the summer.",
        "A stranded tourist's family inherits 212,000 cr."
    };

    [Test]
    public void Line_IsTheSameForTheSameRunAndDay()
    {
        Assert.AreEqual(DebtNews.Line(Pool, 12345, 4), DebtNews.Line(Pool, 12345, 4));
        CollectionAssert.Contains(Pool, DebtNews.Line(Pool, 12345, 4));
    }

    [Test]
    public void Line_RunsThroughThePoolBeforeRepeating()
    {
        List<string> days = Enumerable.Range(2, Pool.Length).Select(day => DebtNews.Line(Pool, 777, day)).ToList();

        CollectionAssert.AreEquivalent(Pool, days, "each line once in the pool's first run-through");
        Assert.AreEqual(DebtNews.Line(Pool, 777, 2), DebtNews.Line(Pool, 777, 2 + Pool.Length), "then the same order again");
    }

    [Test]
    public void Line_OrderDependsOnTheRun()
    {
        string[] Order(int run) => Enumerable.Range(1, Pool.Length).Select(day => DebtNews.Line(Pool, run, day)).ToArray();

        Assert.IsTrue(new[] { 1, 2, 3, 4, 5 }.Select(Order).Any(o => !o.SequenceEqual(Order(12345))), "another run reads the lines in another order");
    }

    [Test]
    public void Line_OfAnEmptyPoolIsNull()
    {
        Assert.IsNull(DebtNews.Line(new string[0], 12345, 2));
        Assert.IsNull(DebtNews.Line(null, 12345, 2));
        Assert.AreEqual("only", DebtNews.Line(new[] { "only" }, 12345, 9));
    }

    private const string Stranded = "Stranded: {name}, lost in {place} when an Economy transponder failed.";
    private const string Count = "Citizens who left on Debt Relief yesterday: {count}.";

    /// <summary>Phase 9 (§10): the paper counts yesterday's Debt Relief departures, and says nothing for none.</summary>
    [Test]
    public void YesterdayLine_FillsTheCount_AndIsNullForNoneOrABlankTemplate()
    {
        Assert.AreEqual("Citizens who left on Debt Relief yesterday: 43.", DebtNews.YesterdayLine(Count, 43));
        Assert.AreEqual("Citizens who left on Debt Relief yesterday: 1.", DebtNews.YesterdayLine(Count, 1), "the line reads for any count");
        Assert.IsNull(DebtNews.YesterdayLine(Count, 0));
        Assert.IsNull(DebtNews.YesterdayLine(Count, -3));
        Assert.IsNull(DebtNews.YesterdayLine(" ", 5));
        Assert.IsNull(DebtNews.YesterdayLine(null, 5));
    }

    [Test]
    public void NewsContentProblems_TheCountLineHoldsItsToken()
    {
        StringAssert.Contains("news.debtReliefCount", new NewsContent { returnedAccepted = Through, returnedDenied = Again, debt = Pool.ToList(), stranded = Stranded }.Problems().Single(), "a blank line");
        StringAssert.Contains("{count}", new NewsContent { returnedAccepted = Through, returnedDenied = Again, debt = Pool.ToList(), stranded = Stranded, debtReliefCount = "Citizens left on Debt Relief yesterday." }.Problems().Single(), "no count");
        CollectionAssert.IsEmpty(new NewsContent { returnedAccepted = Through, returnedDenied = Again, debt = Pool.ToList(), stranded = Stranded, debtReliefCount = Count }.Problems());
    }

    [Test]
    public void NewsContentProblems_RefuseABlankLine()
    {
        CollectionAssert.IsEmpty(new NewsContent { returnedAccepted = Through, returnedDenied = Again, debt = Pool.ToList(), stranded = Stranded, debtReliefCount = Count }.Problems());
        CollectionAssert.IsEmpty(new NewsContent { returnedAccepted = Through, returnedDenied = Again, stranded = Stranded, debtReliefCount = Count }.Problems(), "an empty pool prints no debt line");

        List<string> problems = new NewsContent { returnedAccepted = Through, returnedDenied = Again, debt = new List<string> { "A line.", " " }, stranded = Stranded, debtReliefCount = Count }.Problems();
        Assert.AreEqual(1, problems.Count);
        StringAssert.Contains("news.debt", problems[0]);
    }

    /// <summary>Redesign phase 13b: the stranding line (Strandings.Lines) names the traveller and the place, so it must hold both tokens.</summary>
    [Test]
    public void NewsContentProblems_TheStrandingLineHoldsItsTokens()
    {
        StringAssert.Contains("news.stranded", new NewsContent { returnedAccepted = Through, returnedDenied = Again, debt = Pool.ToList(), debtReliefCount = Count }.Problems().Single(), "a blank line");
        StringAssert.Contains("{place}", new NewsContent { returnedAccepted = Through, returnedDenied = Again, debt = Pool.ToList(), stranded = "Stranded: {name}.", debtReliefCount = Count }.Problems().Single(), "no place");
        StringAssert.Contains("{name}", new NewsContent { returnedAccepted = Through, returnedDenied = Again, debt = Pool.ToList(), stranded = "Lost in {place}.", debtReliefCount = Count }.Problems().Single(), "no name");
    }

    /// <summary>Wave 5, lesson 9: the desk section's lines for a traveller who came back after a denial name them and the day they were turned away.</summary>
    [Test]
    public void NewsContentProblems_TheReturnedLinesHoldTheirTokens()
    {
        var sound = new NewsContent { debt = Pool.ToList(), stranded = Stranded, debtReliefCount = Count, returnedAccepted = Through, returnedDenied = Again };
        CollectionAssert.IsEmpty(sound.Problems());
        sound.returnedAccepted = " ";
        StringAssert.Contains("news.returnedAccepted is blank", sound.Problems().Single());
        sound.returnedAccepted = Through;
        sound.returnedDenied = "{name}, again.";
        StringAssert.Contains("news.returnedDenied must hold {name} and {day}", sound.Problems().Single());
    }
}
