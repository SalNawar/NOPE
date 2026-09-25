using NUnit.Framework;

/// <summary>Premade scheduling: which slots hold a premade, who may roll, and the roll's draws.</summary>
public class PremadesTests
{
    [TestCase(true, false, false, PremadeSlot.Forced)]
    [TestCase(true, false, true, PremadeSlot.Forced)]
    [TestCase(true, true, false, PremadeSlot.None)]
    [TestCase(true, true, true, PremadeSlot.None)]
    [TestCase(false, false, false, PremadeSlot.Roll)]
    [TestCase(false, true, false, PremadeSlot.Roll)]
    [TestCase(false, false, true, PremadeSlot.None)]
    [TestCase(false, true, true, PremadeSlot.None)]
    public void SlotSource_DecisionTable(bool forcedHere, bool forcedMet, bool violatorSlot, PremadeSlot expected)
    {
        Assert.AreEqual(expected, Premades.SlotSource(forcedHere, forcedMet, violatorSlot));
    }

    [TestCase(false, false, true)]
    [TestCase(true, false, false)]
    [TestCase(false, true, false)]
    [TestCase(true, true, false)]
    public void IsRollable_NotMetAndNameFree(bool met, bool nameTaken, bool expected)
    {
        Assert.AreEqual(expected, Premades.IsRollable(met, nameTaken));
    }

    [Test]
    public void Roll_NoCandidates_IsMinusOne_WithNoDraw()
    {
        var rng = new ScriptedRandom();
        Assert.AreEqual(-1, Premades.Roll(1f, 0, false, rng));
        Assert.AreEqual(-1, Premades.Roll(1f, 0, true, rng));
        Assert.AreEqual(0, rng.Draws);
    }

    [Test]
    public void Roll_AValueAtOrAboveTheChance_IsMinusOne_AfterOneDraw()
    {
        var rng = new ScriptedRandom(ScriptStep.Value(0.05f));
        Assert.AreEqual(-1, Premades.Roll(0.05f, 3, false, rng));
        Assert.IsTrue(rng.Done);
    }

    [Test]
    public void Roll_AValueBelowTheChance_PicksWithOneRange()
    {
        var rng = new ScriptedRandom(ScriptStep.Value(0.01f), ScriptStep.Range(2));
        Assert.AreEqual(2, Premades.Roll(0.05f, 3, false, rng));
        Assert.IsTrue(rng.Done);
    }

    [Test]
    public void Roll_TheCheat_SkipsTheChanceDraw()
    {
        var rng = new ScriptedRandom(ScriptStep.Range(1));
        Assert.AreEqual(1, Premades.Roll(0f, 3, true, rng));
        Assert.IsTrue(rng.Done);
    }
}
