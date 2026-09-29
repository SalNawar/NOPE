using NUnit.Framework;

/// <summary>
/// The Home phase's family rules (audit R2-021): the medical drain's points,
/// treating a member and the nightly drift. The drift roll is pinned to the
/// numbers it draws today, so a change of its stream (audit R2-008) shows here
/// as a deliberate change.
/// </summary>
public class HomeRulesTests
{
    [TestCase(3, 3)]
    [TestCase(0, 0)]
    [TestCase(-2, 0, Description = "a negative condition bills nothing")]
    public void DrainPoints(int condition, int expected)
    {
        Assert.AreEqual(expected, HomeRules.DrainPoints(condition));
    }

    [TestCase(2, 50, 20, true)]
    [TestCase(2, 20, 20, true, Description = "exactly the cost")]
    [TestCase(2, 19, 20, false, Description = "short of the cost")]
    [TestCase(0, 50, 20, false, Description = "nothing to treat")]
    [TestCase(-1, 50, 20, false)]
    [TestCase(1, -5, 0, false, Description = "free care still needs money >= 0")]
    [TestCase(1, 0, 0, true)]
    public void CanTreat(int condition, int money, int careCost, bool expected)
    {
        Assert.AreEqual(expected, HomeRules.CanTreat(condition, money, careCost));
    }

    [TestCase(3, 2)]
    [TestCase(1, 0)]
    [TestCase(0, 0, Description = "never below 0")]
    public void Treated(int condition, int expected)
    {
        Assert.AreEqual(expected, HomeRules.Treated(condition));
    }

    [TestCase(3, 10, 4)]
    [TestCase(9, 10, 10)]
    [TestCase(10, 10, 10, Description = "capped")]
    [TestCase(12, 10, 10, Description = "above the cap falls to it")]
    public void Worsened(int condition, int cap, int expected)
    {
        Assert.AreEqual(expected, HomeRules.Worsened(condition, cap));
    }

    // The rolls of the family's stream (audit R2-008): SeededRandom(Seeds.Mix(Seeds.ForFamily(daySeed), index + 1)).Value();
    // seed 12345: member 0 -> 0.87719, member 1 -> 0.96399, member 2 -> 0.44680;
    // seed 1: member 0 -> 0.01161, member 1 -> 0.17987; seed 0: member 0 -> 0.38002.
    [TestCase(12345, 0, 0.87f, false)]
    [TestCase(12345, 0, 0.88f, true)]
    [TestCase(12345, 1, 0.96f, false)]
    [TestCase(12345, 1, 0.97f, true)]
    [TestCase(12345, 2, 0.44f, false)]
    [TestCase(12345, 2, 0.45f, true)]
    [TestCase(1, 0, 0.01f, false)]
    [TestCase(1, 0, 0.02f, true)]
    [TestCase(1, 1, 0.17f, false)]
    [TestCase(1, 1, 0.18f, true)]
    [TestCase(0, 0, 0.38f, false)]
    [TestCase(0, 0, 0.39f, true)]
    public void Worsens_DrawsFromTheFamilysStream(int seed, int memberIndex, float chance, bool expected)
    {
        Assert.AreEqual(expected, HomeRules.Worsens(seed, memberIndex, chance));
    }

    [Test]
    public void Worsens_NeverAtChanceZero_AlwaysAtChanceOne()
    {
        foreach (int seed in new[] { 0, 1, -7, 12345, int.MaxValue, int.MinValue })
            for (int i = 0; i < 5; i++)
            {
                Assert.IsFalse(HomeRules.Worsens(seed, i, 0f), $"seed {seed} member {i}");
                Assert.IsTrue(HomeRules.Worsens(seed, i, 1f), $"seed {seed} member {i}");
            }
    }

    [Test]
    public void Worsens_IsDeterministic()
    {
        for (int i = 0; i < 5; i++)
            Assert.AreEqual(HomeRules.Worsens(424242, i, 0.5f), HomeRules.Worsens(424242, i, 0.5f));
    }
}
