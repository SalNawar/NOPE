using System.Collections.Generic;
using NUnit.Framework;

public class ScoreRankingTests
{
    /// <summary>Floor that accepts anything — keeps ranking-behaviour tests focused on ranking.</summary>
    private const float NoFloor = float.NegativeInfinity;

    /// <summary>The shipped default: only positive influence counts.</summary>
    private const float DefaultFloor = 0f;

    private static List<KeyValuePair<string, float>> Scores(params (string key, float value)[] entries)
    {
        var list = new List<KeyValuePair<string, float>>();

        foreach ((string key, float value) in entries)
            list.Add(new KeyValuePair<string, float>(key, value));

        return list;
    }

    // --- Ranking behaviour (floor open, so these test ordering only) ---

    [Test]
    public void TopNation_PicksHighestNationScore()
    {
        var scores = Scores(
            ("nation:latia", 5f),
            ("nation:helios", 9f),
            ("nation:norvik", 2f));

        Assert.IsTrue(ScoreRanking.TryGetTop(scores, RankCategory.TopNation, NoFloor, out RankedScore top));
        Assert.AreEqual("helios", top.id);
        Assert.AreEqual(9f, top.value, 0.0001f);
    }

    [Test]
    public void TopAttribute_PicksHighestGlobalAttribute()
    {
        var scores = Scores(
            ("attrTotal:aristocracy", 4f),
            ("attrTotal:robotics", 11f));

        Assert.IsTrue(ScoreRanking.TryGetTop(scores, RankCategory.TopAttribute, NoFloor, out RankedScore top));
        Assert.AreEqual("robotics", top.id);
    }

    [Test]
    public void TopProfileAttribute_IdCombinesProfileAndAttribute()
    {
        var scores = Scores(
            ("attr:latia_rome:aristocracy", 3f),
            ("attr:helios_fut:robotics", 7f));

        Assert.IsTrue(ScoreRanking.TryGetTop(scores, RankCategory.TopProfileAttribute, NoFloor, out RankedScore top));
        Assert.AreEqual("helios_fut:robotics", top.id);
        Assert.AreEqual("attr:helios_fut:robotics", top.key);
    }

    [Test]
    public void OtherCategories_AreIgnored()
    {
        // A huge attribute total must not win the nation category.
        var scores = Scores(
            ("attrTotal:robotics", 999f),
            ("attr:latia_rome:aristocracy", 500f),
            ("nation:latia", 1f));

        Assert.IsTrue(ScoreRanking.TryGetTop(scores, RankCategory.TopNation, NoFloor, out RankedScore top));
        Assert.AreEqual("latia", top.id);
        Assert.AreEqual(1f, top.value, 0.0001f);
    }

    [Test]
    public void Ties_KeepFirstEncountered()
    {
        var scores = Scores(
            ("nation:latia", 5f),
            ("nation:helios", 5f));

        Assert.IsTrue(ScoreRanking.TryGetTop(scores, RankCategory.TopNation, NoFloor, out RankedScore top));
        Assert.AreEqual("latia", top.id);
    }

    [Test]
    public void NegativeScores_RankWhenTheFloorPermits()
    {
        var scores = Scores(
            ("nation:latia", -5f),
            ("nation:helios", -2f));

        Assert.IsTrue(ScoreRanking.TryGetTop(scores, RankCategory.TopNation, NoFloor, out RankedScore top));
        Assert.AreEqual("helios", top.id);
    }

    [Test]
    public void NoMatchingEntries_ReturnsFalse()
    {
        var scores = Scores(("attrTotal:robotics", 5f));

        Assert.IsFalse(ScoreRanking.TryGetTop(scores, RankCategory.TopNation, NoFloor, out _));
    }

    [Test]
    public void EmptyAndNull_ReturnFalse()
    {
        Assert.IsFalse(ScoreRanking.TryGetTop(Scores(), RankCategory.TopNation, NoFloor, out _));
        Assert.IsFalse(ScoreRanking.TryGetTop(null, RankCategory.TopNation, NoFloor, out _));
    }

    [Test]
    public void UnparseableKeys_AreSkipped()
    {
        var scores = Scores(
            ("garbage", 999f),
            ("nation:latia", 3f));

        Assert.IsTrue(ScoreRanking.TryGetTop(scores, RankCategory.TopNation, NoFloor, out RankedScore top));
        Assert.AreEqual("latia", top.id);
    }

    // --- The minimum-score floor ---

    [Test]
    public void AllNegative_RejectedAtDefaultFloor()
    {
        // The shipped defect: philosophy at -1 was rendered as "dominant" when
        // nothing had actually taken hold.
        var scores = Scores(
            ("attrTotal:philosophy", -1f),
            ("attrTotal:industry", -4f));

        Assert.IsFalse(ScoreRanking.TryGetTop(scores, RankCategory.TopAttribute, DefaultFloor, out _));
    }

    [Test]
    public void LoneNegativeEntry_RejectedAtDefaultFloor()
    {
        var scores = Scores(("nation:latia", -0.5f));

        Assert.IsFalse(ScoreRanking.TryGetTop(scores, RankCategory.TopNation, DefaultFloor, out _));
    }

    [Test]
    public void ExactlyAtFloor_IsRejected()
    {
        // Strict comparison: a score of 0 is no influence, not a win.
        var scores = Scores(("nation:latia", 0f));

        Assert.IsFalse(ScoreRanking.TryGetTop(scores, RankCategory.TopNation, DefaultFloor, out _));
    }

    [Test]
    public void JustAboveFloor_IsAccepted()
    {
        var scores = Scores(("nation:latia", 0.01f));

        Assert.IsTrue(ScoreRanking.TryGetTop(scores, RankCategory.TopNation, DefaultFloor, out RankedScore top));
        Assert.AreEqual("latia", top.id);
    }

    [Test]
    public void MixedScores_PickHighestAboveFloor()
    {
        var scores = Scores(
            ("nation:latia", -3f),
            ("nation:helios", 2f),
            ("nation:norvik", 6f));

        Assert.IsTrue(ScoreRanking.TryGetTop(scores, RankCategory.TopNation, DefaultFloor, out RankedScore top));
        Assert.AreEqual("norvik", top.id);
        Assert.AreEqual(6f, top.value, 0.0001f);
    }

    [Test]
    public void RaisedFloor_ExcludesSmallWinners()
    {
        // A designer raising the floor should be able to demand a real lead.
        var scores = Scores(
            ("nation:latia", 2f),
            ("nation:helios", 3f));

        Assert.IsTrue(ScoreRanking.TryGetTop(scores, RankCategory.TopNation, 2.5f, out RankedScore top));
        Assert.AreEqual("helios", top.id);

        Assert.IsFalse(ScoreRanking.TryGetTop(scores, RankCategory.TopNation, 10f, out _));
    }

    [Test]
    public void FloorDoesNotLeakAcrossCategories()
    {
        // A nation above the floor must not rescue an attribute category that is below it.
        var scores = Scores(
            ("nation:latia", 50f),
            ("attrTotal:philosophy", -1f));

        Assert.IsFalse(ScoreRanking.TryGetTop(scores, RankCategory.TopAttribute, DefaultFloor, out _));
        Assert.IsTrue(ScoreRanking.TryGetTop(scores, RankCategory.TopNation, DefaultFloor, out _));
    }
}
