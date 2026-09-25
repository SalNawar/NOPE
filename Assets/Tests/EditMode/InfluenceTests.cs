using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// A nation's influence on the past: the attribute deltas stored for its
/// places outside the Future. Places: egypt_ancient and egypt_medieval
/// (Egypt), greece_ancient (Greece), china_future (China, Future era).
/// </summary>
public class InfluenceTests
{
    private static readonly string[] Nations = { "egypt", "greece", "china" };

    private static PlaceRef? PlaceOf(string profileId)
    {
        switch (profileId)
        {
            case "egypt_ancient": return new PlaceRef("egypt", "ancient");
            case "egypt_medieval": return new PlaceRef("egypt", "medieval");
            case "greece_ancient": return new PlaceRef("greece", "ancient");
            case "china_future": return new PlaceRef("china", "future");
            default: return null;
        }
    }

    private static List<RankedScore> Of(params (string key, float value)[] scores) =>
        Influence.ByNation(Nations, scores.Select(s => new KeyValuePair<string, float>(s.key, s.value)), PlaceOf, "future");

    private static float ScoreOf(List<RankedScore> influence, string nation) => influence.Single(r => r.id == nation).score;

    [Test]
    public void SumsEveryAttributeOfEveryPlaceOfANation()
    {
        List<RankedScore> influence = Of(("attr:egypt_ancient:art", 2f), ("attr:egypt_ancient:science", 1.5f), ("attr:egypt_medieval:democracy", -1f));
        Assert.AreEqual(2.5f, ScoreOf(influence, "egypt"));
    }

    [Test]
    public void AnAdHocKeyCountsForItsNation()
    {
        Assert.AreEqual(2f, ScoreOf(Of(("attr:greece@industrial:art", 2f)), "greece"));
    }

    [Test]
    public void TheFutureIsExcluded_ProfileAndAdHoc()
    {
        List<RankedScore> influence = Of(("attr:china_future:science", 5f), ("attr:china@future:art", 3f), ("attr:egypt@future:art", 1f));
        Assert.AreEqual(0f, ScoreOf(influence, "china"));
        Assert.AreEqual(0f, ScoreOf(influence, "egypt"));
    }

    [Test]
    public void NationScores_GlobalTotals_AndOtherKeysAreNotInfluence()
    {
        List<RankedScore> influence = Of(("nation:egypt", 9f), ("attrTotal:art", 9f), ("garbage", 9f), ("sent:tag:x", 9f));
        Assert.IsTrue(influence.All(r => r.score == 0f), "AddNationScore is not a lever");
    }

    [Test]
    public void UnknownProfilesAndUnlistedNationsAreIgnored()
    {
        List<RankedScore> influence = Of(("attr:atlantis_ancient:art", 4f), ("attr:japan@modern:art", 4f));
        Assert.IsTrue(influence.All(r => r.score == 0f));
        Assert.AreEqual(3, influence.Count);
    }

    [Test]
    public void OneEntryPerNation_InTheGivenOrder_ZeroWithoutKeys()
    {
        List<RankedScore> influence = Of(("attr:greece_ancient:art", 1f));
        CollectionAssert.AreEqual(Nations, influence.Select(r => r.id));
        CollectionAssert.AreEqual(new[] { 0f, 1f, 0f }, influence.Select(r => r.score));
    }

    [Test]
    public void NullInputs_GiveZeroes()
    {
        List<RankedScore> influence = Influence.ByNation(Nations, null, PlaceOf, "future");
        Assert.AreEqual(3, influence.Count);
        Assert.AreEqual(0, Influence.ByNation(null, null, PlaceOf, "future").Count);
    }
}
