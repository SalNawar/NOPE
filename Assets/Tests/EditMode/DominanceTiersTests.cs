using NUnit.Framework;

/// <summary>A place's attribute tiers: the ranked top N are Dominant, the next M Supporting.</summary>
public class DominanceTiersTests
{
    private static readonly RankedScore[] Athens =
    {
        new RankedScore("democracy", 5f), new RankedScore("science", 3f), new RankedScore("art", 3.1f)
    };

    [Test]
    public void Classify_AlignsWithTheInputOrder()
    {
        CollectionAssert.AreEqual(new[] { DominanceTier.Dominant, DominanceTier.Supporting, DominanceTier.Supporting },
                                  DominanceTiers.Classify(Athens, 1, 2));
        CollectionAssert.AreEqual(new[] { DominanceTier.Dominant, DominanceTier.None, DominanceTier.Supporting },
                                  DominanceTiers.Classify(Athens, 1, 1), "the third-ranked (science) is None");
    }

    [Test]
    public void Classify_ATieKeepsBaselineOrder()
    {
        var tied = new[] { new RankedScore("a", 4f), new RankedScore("b", 4f) };
        CollectionAssert.AreEqual(new[] { DominanceTier.Dominant, DominanceTier.Supporting }, DominanceTiers.Classify(tied, 1, 1));
    }

    [Test]
    public void Classify_Counts_ZeroLargeAndNegative()
    {
        CollectionAssert.AreEqual(new[] { DominanceTier.None, DominanceTier.None, DominanceTier.None }, DominanceTiers.Classify(Athens, 0, 0));
        CollectionAssert.AreEqual(new[] { DominanceTier.Dominant, DominanceTier.Dominant, DominanceTier.Dominant }, DominanceTiers.Classify(Athens, 5, 5));
        CollectionAssert.AreEqual(new[] { DominanceTier.Supporting, DominanceTier.None, DominanceTier.Supporting }, DominanceTiers.Classify(Athens, -1, 2),
                                  "a negative count counts as 0");
    }

    [Test]
    public void Classify_Null_GivesAnEmptyArray()
    {
        Assert.AreEqual(0, DominanceTiers.Classify(null, 1, 2).Length);
    }
}
