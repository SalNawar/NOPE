using NUnit.Framework;

/// <summary>One fault source per traveller (traveller types K5): the order of the rolls, who rolls at all, and that an honest kind entry skips every roll.</summary>
public class FaultOrderTests
{
    [Test]
    public void Order_IsTheLieRoll_ThenTheViolationRoll_ThenTheCostumeRoll()
    {
        CollectionAssert.AreEqual(new[] { FaultRoll.Lie, FaultRoll.Violation, FaultRoll.Costume }, FaultOrder.Order);
    }

    [Test]
    public void Rolls_NeverAPremade_APlannedSlot_OrAnHonestEntry()
    {
        Assert.IsTrue(FaultOrder.Rolls(false, false, false), "an ordinary traveller rolls");
        Assert.IsFalse(FaultOrder.Rolls(true, false, false), "a premade's authoring decides");
        Assert.IsFalse(FaultOrder.Rolls(false, true, false), "a planned slot holds the day's guaranteed violator");
        Assert.IsFalse(FaultOrder.Rolls(false, false, true), "an honest kind entry rolls nothing");
    }

    [Test]
    public void MayRoll_EachRoll_OnlyWhileNoEarlierRollWon()
    {
        foreach (FaultRoll roll in FaultOrder.Order)
        {
            Assert.IsTrue(FaultOrder.MayRoll(roll, false, false, false, false), roll.ToString());
            Assert.IsFalse(FaultOrder.MayRoll(roll, false, false, true, false), $"{roll}: an honest entry skips it");
            Assert.IsFalse(FaultOrder.MayRoll(roll, true, false, false, false), $"{roll}: a premade skips it");
            Assert.IsFalse(FaultOrder.MayRoll(roll, false, true, false, false), $"{roll}: a planned slot skips it");
        }
        Assert.IsTrue(FaultOrder.MayRoll(FaultRoll.Lie, false, false, false, true), "nothing comes before the lie roll");
        Assert.IsFalse(FaultOrder.MayRoll(FaultRoll.Violation, false, false, false, true), "a liar never also breaks a directive");
        Assert.IsFalse(FaultOrder.MayRoll(FaultRoll.Costume, false, false, false, true), "a liar or a violator is dressed right");
    }

    /// <summary>An honest entry's traveller draws nothing on the lie and fault streams: the streams are untouched (CaseFactory reads FaultOrder before Lies.Roll and CostumeErrors.Plan).</summary>
    [Test]
    public void AnHonestEntry_SkipsEveryRoll_WithNoDraw()
    {
        var lie = new ScriptedRandom();
        var fault = new ScriptedRandom();
        bool honest = true;
        if (FaultOrder.MayRoll(FaultRoll.Lie, false, false, honest, false))
            Lies.Roll(1f, new[] { LieKind.DoctoredIdentity }, lie);
        if (FaultOrder.MayRoll(FaultRoll.Costume, false, false, honest, false))
            CostumeErrors.Plan(1f, false, CostumeError.None, new CostumeErrorWeights { otherPlace = 1f }, 1, false, 0, fault);
        Assert.IsTrue(lie.Done && fault.Done, "no draw was made on either stream");
    }
}
