using NUnit.Framework;

/// <summary>
/// Which effect ops change play for as long as their effect is active (a
/// dialog's effect may hold none of them), and the ints assets store.
/// </summary>
public class EffectOpsTests
{
    [TestCase(EffectOpType.SetFlag, false)]
    [TestCase(EffectOpType.ClearFlag, false)]
    [TestCase(EffectOpType.AddCounter, false)]
    [TestCase(EffectOpType.AddMoney, false)]
    [TestCase(EffectOpType.AddStability, false)]
    [TestCase(EffectOpType.UnlockUpgrade, false)]
    [TestCase(EffectOpType.AddAttributeScore, false)]
    [TestCase(EffectOpType.AddNationScore, false)]
    [TestCase(EffectOpType.LegendaryChanceBonus, true)]
    [TestCase(EffectOpType.ForgeryChanceBonus, true)]
    [TestCase(EffectOpType.PayRateBonus, true)]
    [TestCase(EffectOpType.VisitorTagWeight, true)]
    [TestCase(EffectOpType.ShopDiscountPercent, true)]
    [TestCase(EffectOpType.CaseBlueprintWeight, true)]
    [TestCase(EffectOpType.Cue, true)]
    [TestCase(EffectOpType.BriefingLine, false)]
    [TestCase(EffectOpType.NewsLine, false)]
    [TestCase(EffectOpType.SetFact, false)]
    [TestCase(EffectOpType.HouseholdExpense, true)]
    [TestCase(EffectOpType.SicknessChance, true)]
    [TestCase(EffectOpType.CareCost, true)]
    [TestCase(EffectOpType.Upkeep, true)]
    [TestCase(EffectOpType.MedicalDrain, true)]
    [TestCase(EffectOpType.Mood, true)]
    [TestCase(EffectOpType.BreakInChance, true)]
    [TestCase(EffectOpType.BreakInShare, true)]
    public void ActsWhileActive_OnlyTheContinuousModifiersAndCues(EffectOpType type, bool expected)
    {
        Assert.AreEqual(expected, EffectOps.ActsWhileActive(type));
    }

    [Test]
    public void EffectOpType_KeepsItsSerializedInts()
    {
        Assert.AreEqual(0, (int)EffectOpType.SetFlag);
        Assert.AreEqual(1, (int)EffectOpType.ClearFlag);
        Assert.AreEqual(2, (int)EffectOpType.AddCounter);
        Assert.AreEqual(3, (int)EffectOpType.AddMoney);
        Assert.AreEqual(4, (int)EffectOpType.AddStability);
        Assert.AreEqual(5, (int)EffectOpType.UnlockUpgrade);
        Assert.AreEqual(6, (int)EffectOpType.AddAttributeScore);
        Assert.AreEqual(7, (int)EffectOpType.AddNationScore);
        Assert.AreEqual(8, (int)EffectOpType.LegendaryChanceBonus);
        Assert.AreEqual(9, (int)EffectOpType.ForgeryChanceBonus);
        Assert.AreEqual(10, (int)EffectOpType.PayRateBonus);
        Assert.AreEqual(11, (int)EffectOpType.VisitorTagWeight);
        Assert.AreEqual(12, (int)EffectOpType.ShopDiscountPercent);
        Assert.AreEqual(13, (int)EffectOpType.CaseBlueprintWeight);
        Assert.AreEqual(14, (int)EffectOpType.Cue);
        Assert.AreEqual(15, (int)EffectOpType.BriefingLine);
        Assert.AreEqual(16, (int)EffectOpType.NewsLine);
        Assert.AreEqual(17, (int)EffectOpType.SetFact);
        Assert.AreEqual(18, (int)EffectOpType.HouseholdExpense);
        Assert.AreEqual(19, (int)EffectOpType.SicknessChance);
        Assert.AreEqual(20, (int)EffectOpType.CareCost);
        Assert.AreEqual(21, (int)EffectOpType.Upkeep);
        Assert.AreEqual(22, (int)EffectOpType.MedicalDrain);
        Assert.AreEqual(23, (int)EffectOpType.Mood);
        Assert.AreEqual(24, (int)EffectOpType.BreakInChance);
        Assert.AreEqual(25, (int)EffectOpType.BreakInShare);
        Assert.AreEqual(26, System.Enum.GetValues(typeof(EffectOpType)).Length, "a new member is appended here too");
    }

    [Test]
    public void HistoryOnly_JustSetFact()
    {
        foreach (EffectOpType type in System.Enum.GetValues(typeof(EffectOpType)))
            Assert.AreEqual(type == EffectOpType.SetFact, EffectOps.HistoryOnly(type), type.ToString());
    }
}
