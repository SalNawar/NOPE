using NUnit.Framework;

public class VerdictRulesTests
{
    [TestCase(false, true, true)]
    [TestCase(false, false, false)]
    [TestCase(true, true, false)]
    [TestCase(true, false, false)]
    public void ShouldAccept_OnlyAnHonestTravellerWithAnAllowedClaim(bool isLiar, bool claimAllowed, bool expected)
    {
        Assert.AreEqual(expected, VerdictRules.ShouldAccept(isLiar, claimAllowed));
    }

    // Row 1 is the one unproven denial; every other row flips one input of it.
    [TestCase(true, 0, false, true, true, true)]
    [TestCase(false, 0, false, true, true, false)]
    [TestCase(true, -1, false, true, true, false)]
    [TestCase(true, 1, false, true, true, false)]
    [TestCase(true, 0, true, true, true, false)]
    [TestCase(true, 0, false, false, true, false)]
    [TestCase(true, 0, false, true, false, false)]
    public void IsUnprovenDenial_OnlyAnUnevidencedDenialOfAnAllowedLiar(bool requireEvidence, int evidenceCount, bool accepted, bool isLiar, bool claimAllowed, bool expected)
    {
        Assert.AreEqual(expected, VerdictRules.IsUnprovenDenial(requireEvidence, evidenceCount, accepted, isLiar, claimAllowed));
    }
}
