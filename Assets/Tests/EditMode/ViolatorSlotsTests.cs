using System.Linq;
using NUnit.Framework;

public class ViolatorSlotsTests
{
    [Test]
    public void Slots_AreDistinct_AndInTheFirstHalfOfTheQueue()
    {
        for (int seed = 0; seed < 200; seed++)
        {
            int[] slots = ViolatorSlots.Pick(10, 2, new SeededRandom(seed));
            Assert.AreEqual(2, slots.Length);
            Assert.AreEqual(2, slots.Distinct().Count());
            Assert.That(slots, Is.All.InRange(1, 5));
        }
    }

    [Test]
    public void EveryFirstHalfSlot_CanBeChosen()
    {
        var seen = Enumerable.Range(0, 300).Select(s => ViolatorSlots.Pick(12, 1, new SeededRandom(s))[0]).Distinct().OrderBy(x => x);
        CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5, 6 }, seen.ToArray());
    }

    [Test]
    public void OddQueue_RoundsTheHalfUp()
    {
        var seen = Enumerable.Range(0, 300).Select(s => ViolatorSlots.Pick(9, 1, new SeededRandom(s))[0]).Distinct();
        Assert.That(seen, Is.All.InRange(1, 5));
        Assert.Contains(5, seen.ToList());
    }

    [Test]
    public void MoreViolatorsThanHalfTheQueue_AreCappedAtTheHalf()
    {
        int[] slots = ViolatorSlots.Pick(4, 5, new SeededRandom(1));
        CollectionAssert.AreEquivalent(new[] { 1, 2 }, slots);
    }

    [TestCase(0, 2)]
    [TestCase(8, 0)]
    [TestCase(-3, -1)]
    public void NoQueueOrNoViolators_GivesNoSlots(int queue, int violators)
    {
        Assert.IsEmpty(ViolatorSlots.Pick(queue, violators, new SeededRandom(1)));
    }

    [Test]
    public void SameSeed_SameSlots()
    {
        CollectionAssert.AreEqual(ViolatorSlots.Pick(12, 2, new SeededRandom(42)), ViolatorSlots.Pick(12, 2, new SeededRandom(42)));
    }
}
