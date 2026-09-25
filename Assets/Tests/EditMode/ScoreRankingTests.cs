using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>The one stable ranking of the timeline (per-place tiers and the nation leader).</summary>
public class ScoreRankingTests
{
    private static RankedScore S(string id, float score) => new RankedScore(id, score);

    private static string Ids(IEnumerable<RankedScore> ranked) => string.Join(",", ranked.Select(r => r.id));

    [Test]
    public void Rank_HighestFirst_NegativesBelowZero()
    {
        Assert.AreEqual("b,c,a,d", Ids(ScoreRanking.Rank(new[] { S("a", 0f), S("b", 3f), S("c", 1f), S("d", -2f) })));
    }

    [Test]
    public void Rank_EqualScoresKeepInputOrder()
    {
        Assert.AreEqual("x,y,z", Ids(ScoreRanking.Rank(new[] { S("x", 2f), S("y", 2f), S("z", 2f) })), "a three-way tie");
        Assert.AreEqual("a,d,b,c", Ids(ScoreRanking.Rank(new[] { S("a", 5f), S("b", 1f), S("c", 0f), S("d", 5f) })), "the first and last entries tie");
    }

    [Test]
    public void Rank_NullAndEmpty_GiveAnEmptyList()
    {
        Assert.AreEqual(0, ScoreRanking.Rank(null).Count);
        Assert.AreEqual(0, ScoreRanking.Rank(new RankedScore[0]).Count);
    }

    [Test]
    public void Rank_LeavesTheInputAlone()
    {
        var input = new List<RankedScore> { S("a", 1f), S("b", 2f) };
        List<RankedScore> ranked = ScoreRanking.Rank(input);
        Assert.AreEqual("a,b", Ids(input));
        Assert.AreEqual("b,a", Ids(ranked));
        Assert.AreNotSame(input, ranked);
    }
}
