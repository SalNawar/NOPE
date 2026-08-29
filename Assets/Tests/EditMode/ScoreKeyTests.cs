using NUnit.Framework;

public class ScoreKeyTests
{
    [Test]
    public void ProfileAttr_ParsesProfileAndAttribute()
    {
        Assert.IsTrue(ScoreKey.TryParse("attr:latia_rome:aristocracy", out ParsedScoreKey parsed));

        Assert.AreEqual(ScoreKeyKind.ProfileAttr, parsed.kind);
        Assert.AreEqual("latia_rome", parsed.profileId);
        Assert.AreEqual("aristocracy", parsed.attributeId);
    }

    [Test]
    public void AdHocAttr_SplitsNationEraAndAttribute()
    {
        Assert.IsTrue(ScoreKey.TryParse("attr:latia@rome:industry", out ParsedScoreKey parsed));

        Assert.AreEqual(ScoreKeyKind.AdHocAttr, parsed.kind);
        Assert.AreEqual("latia", parsed.nationId);
        Assert.AreEqual("rome", parsed.eraId);
        Assert.AreEqual("industry", parsed.attributeId);
    }

    [Test]
    public void GlobalAttr_ParsesAttributeOnly()
    {
        Assert.IsTrue(ScoreKey.TryParse("attrTotal:robotics", out ParsedScoreKey parsed));

        Assert.AreEqual(ScoreKeyKind.GlobalAttr, parsed.kind);
        Assert.AreEqual("robotics", parsed.attributeId);
    }

    [Test]
    public void Nation_ParsesNationOnly()
    {
        Assert.IsTrue(ScoreKey.TryParse("nation:helios", out ParsedScoreKey parsed));

        Assert.AreEqual(ScoreKeyKind.Nation, parsed.kind);
        Assert.AreEqual("helios", parsed.nationId);
    }

    [Test]
    public void AttrPrefixWins_OverAttrTotalLookalike()
    {
        // "attrTotal:" is checked first, so a profile literally named "Total" still parses as a profile attr.
        Assert.IsTrue(ScoreKey.TryParse("attr:Total:industry", out ParsedScoreKey parsed));

        Assert.AreEqual(ScoreKeyKind.ProfileAttr, parsed.kind);
        Assert.AreEqual("Total", parsed.profileId);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("garbage")]
    [TestCase("attr:")]
    [TestCase("attr:noattribute")]
    [TestCase("attr:trailing:")]
    [TestCase("attrTotal:")]
    [TestCase("nation:")]
    public void UnrecognizedShapes_ReturnFalse(string key)
    {
        Assert.IsFalse(ScoreKey.TryParse(key, out _));
    }

    [Test]
    public void DominanceKey_MatchesTimelineKeysFormat()
    {
        var parsed = new ParsedScoreKey { profileId = "latia_rome", attributeId = "aristocracy" };

        Assert.AreEqual("latia_rome:aristocracy", ScoreKey.DominanceKey(parsed));
    }
}
