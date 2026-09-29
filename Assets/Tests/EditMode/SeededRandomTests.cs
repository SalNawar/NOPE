using System.Linq;
using NUnit.Framework;

public class SeededRandomTests
{
    private static int[] Draw(IRandomSource rng, int count) =>
        Enumerable.Range(0, count).Select(_ => rng.Range(0, 1000000)).ToArray();

    [Test]
    public void SameSeed_GivesTheSameSequence()
    {
        CollectionAssert.AreEqual(Draw(new SeededRandom(42), 20), Draw(new SeededRandom(42), 20));
    }

    [Test]
    public void DifferentSeeds_GiveDifferentSequences()
    {
        CollectionAssert.AreNotEqual(Draw(new SeededRandom(42), 20), Draw(new SeededRandom(43), 20));
    }

    [Test]
    public void Range_StaysWithinBounds_AndReachesBothEnds()
    {
        var rng = new SeededRandom(7);
        bool sawMin = false, sawMax = false;
        for (int i = 0; i < 5000; i++)
        {
            int v = rng.Range(-3, 4);
            Assert.That(v, Is.InRange(-3, 3));
            sawMin |= v == -3;
            sawMax |= v == 3;
        }
        Assert.IsTrue(sawMin && sawMax);
    }

    [TestCase(5, 5)]
    [TestCase(7, 3)]
    public void Range_WithEmptyOrReversedBounds_ReturnsMin_WithoutThrowing(int min, int max)
    {
        Assert.AreEqual(min, new SeededRandom(1).Range(min, max));
    }

    // The sequences today's SplitMix64 produces, written out (audit R1-002): a changed constant, reduction or
    // seed conversion reshuffles every run's travellers, and these fail first. Value() is the top 24 bits over
    // 2^24, so each expected value is exact.
    [TestCase(0, new[] { 607535, 355700, 545679, 542444, 94747, 162090, 306913, 346940 }, new[] { 14819496, 7239838, 443485, 16288696 })]
    [TestCase(42, new[] { 275413, 892291, 763858, 255764, 963250, 989062, 624925, 775908 }, new[] { 12441394, 2682851, 4674151, 5774561 })]
    [TestCase(-1, new[] { 79680, 663860, 43187, 713102, 58122, 657396, 238135, 942972 }, new[] { 7582011, 6365251, 15616713, 1242350 })]
    [TestCase(int.MinValue, new[] { 717868, 741979, 818326, 658031, 826542, 49135, 569326, 536262 }, new[] { 2443580, 16286811, 14861148, 5199585 })]
    public void Sequence_IsPinned(int seed, int[] ranges, int[] valueTop24Bits)
    {
        CollectionAssert.AreEqual(ranges, Draw(new SeededRandom(seed), ranges.Length), "Range(0, 1000000)");
        var rng = new SeededRandom(seed);
        foreach (int bits in valueTop24Bits)
            Assert.AreEqual(bits / 16777216f, rng.Value(), "Value()");
    }

    [Test]
    public void Value_IsInTheHalfOpenUnitInterval()
    {
        var rng = new SeededRandom(99);
        for (int i = 0; i < 5000; i++)
        {
            float v = rng.Value();
            Assert.That(v, Is.GreaterThanOrEqualTo(0f).And.LessThan(1f));
        }
    }
}
