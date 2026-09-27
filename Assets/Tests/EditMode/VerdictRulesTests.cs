using NUnit.Framework;

/// <summary>The verdict table (traveller types §5.2): the two fault kinds, and the evidence gate on deviation faults only.</summary>
public class VerdictRulesTests
{
    [TestCase(false, false, true, Description = "no fault: accept")]
    [TestCase(true, false, false, Description = "a deviation fault (a lie, a costume error): deny")]
    [TestCase(false, true, false, Description = "a directive fault (a closed destination): deny")]
    [TestCase(true, true, false, Description = "both (never generated, K5): deny")]
    public void ShouldAccept_OnlyATravellerWithNoFault(bool deviation, bool directive, bool expected)
    {
        Assert.AreEqual(expected, VerdictRules.ShouldAccept(deviation, directive));
    }

    // Row 1 is the one unproven denial; every other row flips one input of it.
    [TestCase(true, 0, false, true, false, true)]
    [TestCase(false, 0, false, true, false, false, Description = "the gate is off")]
    [TestCase(true, -1, false, true, false, false, Description = "no evidence system")]
    [TestCase(true, 1, false, true, false, false, Description = "a deviation logged")]
    [TestCase(true, 0, true, true, false, false, Description = "accepted")]
    [TestCase(true, 0, false, false, false, false, Description = "no fault: a wrong denial, not an unproven one")]
    [TestCase(true, 0, false, true, true, false, Description = "a directive fault needs no evidence")]
    [TestCase(true, 0, false, false, true, false, Description = "a directive denial needs no evidence")]
    public void IsUnprovenDenial_OnlyAnUnevidencedDenialOfADeviationFault(bool requireEvidence, int evidenceCount, bool accepted, bool deviation, bool directive, bool expected)
    {
        Assert.AreEqual(expected, VerdictRules.IsUnprovenDenial(requireEvidence, evidenceCount, accepted, deviation, directive));
    }
}
