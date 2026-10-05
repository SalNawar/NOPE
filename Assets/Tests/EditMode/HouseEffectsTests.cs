using NUnit.Framework;

/// <summary>The House panel's effect line (the Home upgrades spec §6): each household op in words, in the ops' order, joined by middle dots; a chance, a share or the mood never as a number (Saleh's Q3, 2026-09-29).</summary>
public class HouseEffectsTests
{
    private static readonly EffectOpType[] Worded = { EffectOpType.SicknessChance, EffectOpType.Mood, EffectOpType.BreakInChance, EffectOpType.BreakInShare };

    [TestCase(EffectOpType.HouseholdExpense, -4f, "Rent and utilities -4 cr a night")]
    [TestCase(EffectOpType.HouseholdExpense, 12f, "Rent and utilities +12 cr a night")]
    [TestCase(EffectOpType.SicknessChance, -0.02f, "Slightly fewer sick nights")]
    [TestCase(EffectOpType.SicknessChance, -0.04f, "Fewer sick nights")]
    [TestCase(EffectOpType.SicknessChance, -0.05f, "Far fewer sick nights")]
    [TestCase(EffectOpType.SicknessChance, 0.03f, "More sick nights")]
    [TestCase(EffectOpType.CareCost, -3f, "Medicine -3 cr")]
    [TestCase(EffectOpType.Upkeep, 4f, "Upkeep 4 cr a night")]
    [TestCase(EffectOpType.MedicalDrain, -1f, "Sick pet's extra care -1 cr a step")]
    [TestCase(EffectOpType.Mood, 1f, "A little cheerier at home")]
    [TestCase(EffectOpType.Mood, 2f, "Cheerier at home")]
    [TestCase(EffectOpType.Mood, 3f, "Much cheerier at home")]
    [TestCase(EffectOpType.Mood, -1f, "Gloomier at home")]
    [TestCase(EffectOpType.BreakInChance, -0.03f, "Fewer break-ins")]
    [TestCase(EffectOpType.BreakInShare, -0.15f, "A break-in takes less")]
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
        Assert.AreEqual("Fewer sick nights · A little cheerier at home", HouseEffects.Line(new[] { (EffectOpType.SicknessChance, -0.04f), (EffectOpType.PayRateBonus, 1f), (EffectOpType.Mood, 1f) }));
        Assert.AreEqual(string.Empty, HouseEffects.Line(null));
    }

    [Test]
    public void Describe_AChanceAShareOrTheMood_NeverShowsANumber()
    {
        foreach (EffectOpType op in Worded)
            for (float v = -20f; v <= 20f; v += 0.01f)
            {
                string words = HouseEffects.Describe(op, v);
                StringAssert.DoesNotMatch("[0-9%]", words, $"{op} {v}");
            }
    }
}
