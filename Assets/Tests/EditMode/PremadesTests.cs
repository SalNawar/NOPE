using NUnit.Framework;

/// <summary>Premade scheduling: which slots hold a premade, who may roll, and the roll's draws.</summary>
public class PremadesTests
{
    [TestCase(true, true, false, PremadeSlot.Forced)]
    [TestCase(true, true, true, PremadeSlot.Forced)]
    [TestCase(true, false, false, PremadeSlot.None)]
    [TestCase(true, false, true, PremadeSlot.None)]
    [TestCase(false, false, false, PremadeSlot.Roll)]
    [TestCase(false, true, false, PremadeSlot.Roll)]
    [TestCase(false, false, true, PremadeSlot.None)]
    [TestCase(false, true, true, PremadeSlot.None)]
    public void SlotSource_DecisionTable(bool forcedHere, bool premadeStands, bool violatorSlot, PremadeSlot expected)
    {
        Assert.AreEqual(expected, Premades.SlotSource(forcedHere, premadeStands, violatorSlot));
    }

    [Test]
    public void SlotSource_AFailedConditionHoldsAnOrdinaryTraveller()
    {
        bool stands = Premades.Stands(true, false, false);
        Assert.IsFalse(stands);
        Assert.AreEqual(PremadeSlot.None, Premades.SlotSource(true, stands, false), "an ordinary traveller, never a roll");
    }

    [Test]
    public void SlotSource_ARepeatablePremadeStandsEveryListedDay()
    {
        Assert.IsTrue(Premades.Stands(false, false, true));
        Assert.IsTrue(Premades.Stands(false, true, true), "a repeatable premade is never kept out by a met flag");
        Assert.AreEqual(PremadeSlot.Forced, Premades.SlotSource(true, Premades.Stands(false, true, true), false));
    }

    [Test]
    public void SlotSource_AMetOncePerRunPremadeHoldsAnOrdinaryTraveller()
    {
        bool stands = Premades.Stands(true, true, true);
        Assert.IsFalse(stands);
        Assert.AreEqual(PremadeSlot.None, Premades.SlotSource(true, stands, false));
    }

    [Test]
    public void SlotSource_ConditionsAreReadOnlyForAForcedSlot()
    {
        Assert.AreEqual(PremadeSlot.Roll, Premades.SlotSource(false, false, false));
        Assert.AreEqual(PremadeSlot.Roll, Premades.SlotSource(false, true, false));
    }

    [Test]
    public void Appearance_TheFirstStandingEntryWins()
    {
        Assert.AreEqual(0, Premades.Appearance(new[] { true, true }));
        Assert.AreEqual(1, Premades.Appearance(new[] { false, true, true }), "an earlier entry whose conditions fail gives way to the next");
        Assert.AreEqual(2, Premades.Appearance(new[] { false, false, true }));
    }

    [TestCase("slot line", "premade line", "slot line")]
    [TestCase("", "premade line", "premade line")]
    [TestCase("  ", "premade line", "premade line")]
    [TestCase(null, "premade line", "premade line")]
    [TestCase(null, null, null)]
    public void Voice_TheSlotsLineReplacesThePremadesOwn(string slot, string premade, string expected)
    {
        Assert.AreEqual(expected, Premades.Voice(slot, premade));
    }

    [Test]
    public void Appearance_NoneStandingIsAnOrdinaryTraveller()
    {
        Assert.AreEqual(-1, Premades.Appearance(new[] { false, false }));
        Assert.AreEqual(-1, Premades.Appearance(new bool[0]));
        Assert.AreEqual(-1, Premades.Appearance(null));
    }

    [TestCase(false, false, true)]
    [TestCase(true, false, false)]
    [TestCase(false, true, false)]
    [TestCase(true, true, false)]
    public void IsRollable_NotMetAndNameFree(bool met, bool nameTaken, bool expected)
    {
        Assert.AreEqual(expected, Premades.IsRollable(met, nameTaken));
    }

    /// <summary>The famous arrive on day 6 (traveller types K3, section 2.3): its forced slots, 8 and 11 of a queue of 14, are in the second half, outside the guaranteed faulty travellers' window (ViolatorSlots.Window).</summary>
    [TestCase(8)]
    [TestCase(11)]
    public void Day6ForcedSlots_AreOutsideTheViolatorWindow(int slot)
    {
        Assert.Greater(slot, ViolatorSlots.Window(14));
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
