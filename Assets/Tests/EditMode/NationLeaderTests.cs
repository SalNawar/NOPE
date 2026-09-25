using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// Who leads the timeline, with hysteresis (floor 4, keep floor 2, margin 2
/// unless stated). A ranking is written "a 5, b 3" (highest first).
/// </summary>
public class NationLeaderTests
{
    private static List<RankedScore> R(string ranking)
    {
        var list = new List<RankedScore>();
        if (string.IsNullOrEmpty(ranking))
            return list;
        foreach (string entry in ranking.Split(','))
        {
            string[] parts = entry.Trim().Split(' ');
            list.Add(new RankedScore(parts[0], float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture)));
        }
        return ScoreRanking.Rank(list);
    }

    private static string Decide(string incumbent, string ranking, float floor = 4f, float keepFloor = 2f, float margin = 2f) =>
        NationLeader.Decide(incumbent, R(ranking), floor, keepFloor, margin);

    [TestCase("a 5, b 3", "a")]
    [TestCase("a 4, b 1", "", Description = "not above the floor")]
    [TestCase("a 5, b 5", "", Description = "a tie at the top")]
    [TestCase("a 5", "a")]
    [TestCase("", "")]
    [TestCase("a -1, b -2", "")]
    public void NoIncumbent(string ranking, string expected)
    {
        Assert.AreEqual(expected, Decide("", ranking));
        Assert.AreEqual(expected, Decide(null, ranking));
    }

    [TestCase("b 6, a 5", "a")]
    [TestCase("b 7, a 5", "a", Description = "exactly the margin")]
    [TestCase("b 7.5, a 5", "b")]
    [TestCase("a 5, b 5", "a")]
    public void Incumbent_KeepsUnlessBeatenByMoreThanTheMargin(string ranking, string expected)
    {
        Assert.AreEqual(expected, Decide("a", ranking));
    }

    [TestCase("b 8, c 8, a 5", "a", Description = "tied challengers never replace the incumbent")]
    [TestCase("b 8, c 7, a 5", "b")]
    public void TiedChallengers(string ranking, string expected)
    {
        Assert.AreEqual(expected, Decide("a", ranking));
    }

    [Test]
    public void KeepFloor()
    {
        Assert.AreEqual("a", Decide("a", "a 3, b 1"), "between the keep floor and the floor the incumbent stays");
        Assert.AreEqual("a", Decide("a", "b 4.5, a 4"), "b does not beat a by more than the margin");
        Assert.AreEqual("b", Decide("a", "b 4.5, a 2"), "at the keep floor the lead is open again");
        Assert.AreEqual("", Decide("a", "a 2, b 1"), "at the keep floor with nobody above the floor");
        Assert.AreEqual("a", Decide("a", "a 5, b 5", keepFloor: 6f), "a keep floor above the floor counts as the floor");
    }

    [Test]
    public void AnIncumbentOutsideTheRanking_BehavesLikeNone()
    {
        Assert.AreEqual("b", Decide("z", "b 5, a 3"));
    }

    [Test]
    public void Margin_ZeroAndNegative()
    {
        Assert.AreEqual("a", Decide("a", "b 5, a 5", margin: 0f));
        Assert.AreEqual("b", Decide("a", "b 5.1, a 5", margin: 0f));
        Assert.AreEqual("b", Decide("a", "b 5.1, a 5", margin: -3f), "a negative margin counts as 0");
    }
}
