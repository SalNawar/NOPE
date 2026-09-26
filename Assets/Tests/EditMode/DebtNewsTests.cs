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

    [Test]
    public void NewsContentProblems_RefuseABlankLine()
    {
        CollectionAssert.IsEmpty(new NewsContent { debt = Pool.ToList() }.Problems());
        CollectionAssert.IsEmpty(new NewsContent().Problems(), "an empty pool prints no debt line");

        List<string> problems = new NewsContent { debt = new List<string> { "A line.", " " } }.Problems();
        Assert.AreEqual(1, problems.Count);
        StringAssert.Contains("news.debt", problems[0]);
    }
}
