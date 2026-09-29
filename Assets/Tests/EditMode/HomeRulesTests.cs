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

    // ---- The house upgrades (docs/superpowers/specs/2026-09-30-home-upgrades-design.md HU4, HU6, HU7) ----

    [TestCase(0.25f, -0.05f, 0.2f)]
    [TestCase(0.25f, 0f, 0.25f)]
    [TestCase(0.25f, 0.1f, 0.35f, Description = "a positive bonus raises it")]
    [TestCase(0.25f, -0.4f, 0f, Description = "never below 0")]
    public void Adjusted_AddsTheBonus_NeverBelowZero(float value, float bonus, float expected)
    {
        Assert.AreEqual(expected, HomeRules.Adjusted(value, bonus), 1e-6f);
    }

    [TestCase(30, -4f, 26)]
    [TestCase(30, 12f, 42)]
    [TestCase(8, -3f, 5)]
    [TestCase(8, -10f, 0, Description = "never below 0")]
    [TestCase(1, 1.5f, 2, Description = "2.5 rounds half to even")]
    [TestCase(1, 2.5f, 4, Description = "3.5 rounds half to even")]
    [TestCase(0, 0f, 0)]
    public void Cost_AddsTheBonus_RoundedHalfToEven_NeverBelowZero(int cost, float bonus, int expected)
    {
        Assert.AreEqual(expected, HomeRules.Cost(cost, bonus));
    }

    [TestCase(0f, 0.04f, 0.5f, 0f)]
    [TestCase(3f, 0.04f, 0.5f, 0.12f)]
    [TestCase(14f, 0.04f, 0.5f, 0.5f, Description = "capped")]
    [TestCase(-2f, 0.04f, 0.5f, 0f, Description = "a negative mood heals nobody")]
    [TestCase(5f, 0.04f, -1f, 0f, Description = "a negative cap is none")]
    public void RecoveryChance_MoodTimesThePerPointRate_UpToTheCap(float mood, float perPoint, float cap, float expected)
    {
        Assert.AreEqual(expected, HomeRules.RecoveryChance(mood, perPoint, cap), 1e-6f);
    }

    // The recovery stream: SeededRandom(Seeds.Mix(Seeds.ForRecovery(daySeed), index + 1)).Value();
    // seed 12345: member 0 -> 0.07909, member 1 -> 0.13269; seed 1: member 0 -> 0.59552.
    [TestCase(12345, 0, 0.07f, false)]
    [TestCase(12345, 0, 0.08f, true)]
    [TestCase(12345, 1, 0.13f, false)]
    [TestCase(12345, 1, 0.14f, true)]
    [TestCase(1, 0, 0.59f, false)]
    [TestCase(1, 0, 0.60f, true)]
    public void Recovers_DrawsFromTheRecoveryStream(int seed, int memberIndex, float chance, bool expected)
    {
        Assert.AreEqual(expected, HomeRules.Recovers(seed, memberIndex, chance));
    }

    [Test]
    public void Recovers_NeverAtChanceZero_AlwaysAtChanceOne_AndApartFromTheDrift()
    {
        int differ = 0;
        foreach (int seed in new[] { 0, 1, -7, 12345, int.MaxValue, int.MinValue })
            for (int i = 0; i < 5; i++)
            {
                Assert.IsFalse(HomeRules.Recovers(seed, i, 0f), $"seed {seed} member {i}");
                Assert.IsTrue(HomeRules.Recovers(seed, i, 1f), $"seed {seed} member {i}");
                if (HomeRules.Recovers(seed, i, 0.5f) != HomeRules.Worsens(seed, i, 0.5f))
                    differ++;
            }
        Assert.Greater(differ, 0, "the recovery roll is not the drift's roll");
    }

    [TestCase(3, true, false, 10, 4, Description = "worse")]
    [TestCase(3, false, true, 10, 2, Description = "better")]
    [TestCase(3, true, true, 10, 4, Description = "a member who worsens does not also recover")]
    [TestCase(0, false, true, 10, 0, Description = "a well member stays well")]
    [TestCase(10, true, false, 10, 10, Description = "capped")]
    [TestCase(3, false, false, 10, 3)]
    public void Night_WorsensFirst_ElseRecovers(int condition, bool worsens, bool recovers, int cap, int expected)
    {
        Assert.AreEqual(expected, HomeRules.Night(condition, worsens, recovers, cap));
    }

    // The break-in stream: SeededRandom(Seeds.ForBreakIns(daySeed)).Value(); seed 12345 -> 0.66336, seed 1 -> 0.75408.
    [TestCase(2, 2, 12345, 0.66f, false)]
    [TestCase(2, 2, 12345, 0.67f, true)]
    [TestCase(5, 2, 1, 0.75f, false)]
    [TestCase(5, 2, 1, 0.76f, true)]
    [TestCase(1, 2, 12345, 1f, false, Description = "not before the first night it may happen")]
    [TestCase(3, 2, 12345, 0f, false, Description = "never at chance 0")]
    public void BreakIn_FromItsDay_OnTheBreakInStream(int day, int fromDay, int seed, float chance, bool expected)
    {
        Assert.AreEqual(expected, HomeRules.BreakIn(day, fromDay, seed, chance));
    }

    [TestCase(100, 0.25f, 60, 25)]
    [TestCase(400, 0.25f, 60, 60, Description = "capped")]
    [TestCase(10, 0.25f, 60, 2, Description = "2.5 rounds half to even")]
    [TestCase(0, 0.25f, 60, 0, Description = "an empty wallet loses nothing")]
    [TestCase(-40, 0.25f, 60, 0, Description = "nor does a wallet in debt")]
    [TestCase(100, 0f, 60, 0)]
    [TestCase(100, -0.1f, 60, 0, Description = "a negative share takes nothing")]
    [TestCase(100, 0.25f, -5, 0, Description = "a negative cap takes nothing")]
    public void BreakInLoss_AShareOfAPositiveWallet_UpToTheCap(int money, float share, int maxLoss, int expected)
    {
        Assert.AreEqual(expected, HomeRules.BreakInLoss(money, share, maxLoss));
    }
}
