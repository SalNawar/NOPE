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
