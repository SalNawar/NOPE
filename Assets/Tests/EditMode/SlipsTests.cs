using NUnit.Framework;

/// <summary>Who slips (the personalities spec's T9): only a generated liar rolls, once, against the day's chance; a premade liar slips when authored.</summary>
public class SlipsTests
{
    private sealed class Counting : IRandomSource
    {
        private readonly float _value;
        public int Draws;
        public Counting(float value) => _value = value;
        public int Range(int minInclusive, int maxExclusive) { Draws++; return minInclusive; }
        public float Value() { Draws++; return _value; }
    }

    [Test]
    public void Rolls_OnlyLyingIntent()
    {
        Assert.IsTrue(Slips.Rolls(ReactionIntent.Lying, false));
        Assert.IsFalse(Slips.Rolls(ReactionIntent.Honest, false), "an honest traveller never rolls");
    }

    [Test]
    public void Rolls_NeverAPremade()
    {
        Assert.IsFalse(Slips.Rolls(ReactionIntent.Lying, true));
        Assert.IsFalse(Slips.Rolls(ReactionIntent.Honest, true));
    }

    [Test]
    public void Roll_IsOneDraw()
    {
        var rng = new Counting(0.5f);
        Slips.Roll(0.15f, rng);
        Assert.AreEqual(1, rng.Draws);
    }

    [Test]
    public void Roll_FollowsTheChance()
    {
        Assert.IsTrue(Slips.Roll(0.15f, new Counting(0.1f)));
        Assert.IsFalse(Slips.Roll(0.15f, new Counting(0.2f)));
        Assert.IsTrue(Slips.Roll(1f, new Counting(0.9999f)), "1 always slips");
    }

    [Test]
    public void Roll_AtZeroNeverSlips()
    {
        Assert.IsFalse(Slips.Roll(0f, new Counting(0f)));
        Assert.IsFalse(Slips.Roll(0.5f, null), "no stream: no slip, no draw");
    }

    [Test]
    public void Premade_SlipsWhenItsLineIsAuthored()
    {
        Assert.IsTrue(Slips.PremadeSlips(ReactionIntent.Lying, true));
        Assert.IsFalse(Slips.PremadeSlips(ReactionIntent.Lying, false), "no line: no slip");
        Assert.IsFalse(Slips.PremadeSlips(ReactionIntent.Honest, true), "an honest premade never slips");
    }
}
