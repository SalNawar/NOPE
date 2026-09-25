using NUnit.Framework;

/// <summary>
/// The score-key grammar: the builders every system writes with, and the
/// parser history reads them back with (parse cases ported from PR #3's
/// ScoreKeyTests, commit 160afde).
/// </summary>
public class ScoreKeyTests
{
    [Test]
    public void Builders_WriteTheSavedGrammar()
    {
        Assert.AreEqual("attr:egypt_ancient:science", ScoreKey.ProfileAttr("egypt_ancient", "science"));
        Assert.AreEqual("attr:egypt@future:art", ScoreKey.AdHocAttr("egypt", "future", "art"));
        Assert.AreEqual("attrTotal:democracy", ScoreKey.GlobalAttr("democracy"));
        Assert.AreEqual("nation:china", ScoreKey.Nation("china"));
        Assert.AreEqual("p:a", ScoreKey.Dominance("p", "a"));
    }

    [Test]
    public void TryParse_ProfileAttr()
    {
        Assert.IsTrue(ScoreKey.TryParse("attr:greece_ancient:art", out ParsedScoreKey k));
        Assert.AreEqual(ScoreKeyKind.ProfileAttr, k.kind);
        Assert.AreEqual("greece_ancient", k.profileId);
        Assert.AreEqual("art", k.attributeId);
    }

    [Test]
    public void TryParse_AdHocAttr_SplitsNationAndEra()
    {
        Assert.IsTrue(ScoreKey.TryParse("attr:japan@modern:science", out ParsedScoreKey k));
        Assert.AreEqual(ScoreKeyKind.AdHocAttr, k.kind);
        Assert.AreEqual("japan", k.nationId);
        Assert.AreEqual("modern", k.eraId);
        Assert.AreEqual("science", k.attributeId);
    }

    [Test]
    public void TryParse_GlobalAttr_WinsOverTheAttrLookAlike()
    {
        Assert.IsTrue(ScoreKey.TryParse("attrTotal:art", out ParsedScoreKey k));
        Assert.AreEqual(ScoreKeyKind.GlobalAttr, k.kind);
        Assert.AreEqual("art", k.attributeId);
    }

    [Test]
    public void TryParse_Nation()
    {
        Assert.IsTrue(ScoreKey.TryParse("nation:britain", out ParsedScoreKey k));
        Assert.AreEqual(ScoreKeyKind.Nation, k.kind);
        Assert.AreEqual("britain", k.nationId);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("garbage")]
    [TestCase("attr:")]
    [TestCase("attr:noattribute")]
    [TestCase("attr:trailing:")]
    [TestCase("attrTotal:")]
    [TestCase("nation:")]
    public void TryParse_RejectsUnrecognisedShapes(string key)
    {
        Assert.IsFalse(ScoreKey.TryParse(key, out _));
    }

    [Test]
    public void EveryBuilder_RoundTrips()
    {
        Assert.IsTrue(ScoreKey.TryParse(ScoreKey.ProfileAttr("italy_medieval", "art"), out ParsedScoreKey a));
        Assert.AreEqual((ScoreKeyKind.ProfileAttr, "italy_medieval", "art"), (a.kind, a.profileId, a.attributeId));

        Assert.IsTrue(ScoreKey.TryParse(ScoreKey.AdHocAttr("iraq", "industrial", "democracy"), out ParsedScoreKey b));
        Assert.AreEqual((ScoreKeyKind.AdHocAttr, "iraq", "industrial", "democracy"), (b.kind, b.nationId, b.eraId, b.attributeId));

        Assert.IsTrue(ScoreKey.TryParse(ScoreKey.GlobalAttr("science"), out ParsedScoreKey c));
        Assert.AreEqual((ScoreKeyKind.GlobalAttr, "science"), (c.kind, c.attributeId));

        Assert.IsTrue(ScoreKey.TryParse(ScoreKey.Nation("germany"), out ParsedScoreKey d));
        Assert.AreEqual((ScoreKeyKind.Nation, "germany"), (d.kind, d.nationId));
    }
}
