using NUnit.Framework;

/// <summary>The House panel's effect line (the Home upgrades spec §6): each household op in words, in the ops' order, joined by middle dots.</summary>
public class HouseEffectsTests
{
    [TestCase(EffectOpType.HouseholdExpense, -4f, "Rent and utilities -4 cr a night")]
    [TestCase(EffectOpType.HouseholdExpense, 12f, "Rent and utilities +12 cr a night")]
    [TestCase(EffectOpType.SicknessChance, -0.05f, "Sick nights -5 %")]
    [TestCase(EffectOpType.CareCost, -3f, "Treatment -3 cr")]
    [TestCase(EffectOpType.Upkeep, 4f, "Upkeep 4 cr a night")]
    [TestCase(EffectOpType.MedicalDrain, -1f, "Medical drain -1 cr a point")]
    [TestCase(EffectOpType.Mood, 2f, "Mood +2")]
    [TestCase(EffectOpType.BreakInChance, -0.03f, "Break-ins -3 %")]
    [TestCase(EffectOpType.BreakInShare, -0.15f, "A break-in's take -15 %")]
    public void Describe_EachOpInWords(EffectOpType op, float value, string expected)
    {
        Assert.AreEqual(expected, HouseEffects.Describe(op, value));
    }

    [Test]
    public void Describe_NotAHouseholdOp_OrZero_IsNothing()
    {
        Assert.AreEqual(string.Empty, HouseEffects.Describe(EffectOpType.PayRateBonus, 0.25f));
        Assert.AreEqual(string.Empty, HouseEffects.Describe(EffectOpType.Mood, 0f));
    }

    [Test]
    public void Line_JoinsTheOpsInOrder_SkippingTheRest()
    {
        Assert.AreEqual("Sick nights -4 % · Mood +1", HouseEffects.Line(new[] { (EffectOpType.SicknessChance, -0.04f), (EffectOpType.PayRateBonus, 1f), (EffectOpType.Mood, 1f) }));
        Assert.AreEqual(string.Empty, HouseEffects.Line(null));
    }
}
