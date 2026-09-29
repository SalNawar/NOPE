using System.Linq;
using System.Collections.Generic;
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

    [TestCase(0, 0)]
    [TestCase(1, 1)]
    [TestCase(8, 4)]
    [TestCase(9, 5)]
    [TestCase(10, 5)]
    [TestCase(12, 6)]
    public void Window_IsTheFirstHalfRoundedUp(int queue, int window)
    {
        Assert.AreEqual(window, ViolatorSlots.Window(queue));
    }

    [Test]
    public void Exclusions_AreNeverPicked_AndCapTheCount()
    {
        for (int seed = 0; seed < 200; seed++)
        {
            int[] slots = ViolatorSlots.Pick(8, 3, new SeededRandom(seed), new[] { 3 });
            CollectionAssert.DoesNotContain(slots, 3, $"seed {seed}");
            Assert.AreEqual(3, slots.Length, $"seed {seed}");
            CollectionAssert.AllItemsAreUnique(slots);
            Assert.IsTrue(slots.All(s => s >= 1 && s <= 4), $"seed {seed}");
        }

        CollectionAssert.AreEquivalent(new[] { 1, 4 }, ViolatorSlots.Pick(8, 5, new SeededRandom(1), new[] { 2, 3 }), "capped by the two open slots");
        CollectionAssert.IsEmpty(ViolatorSlots.Pick(2, 1, new SeededRandom(1), new[] { 1 }), "no open slot");
    }

    [Test]
    public void NoExclusions_GiveTheOriginalSlots()
    {
        for (int seed = 0; seed < 200; seed++)
        {
            int[] plain = ViolatorSlots.Pick(12, 2, new SeededRandom(seed));
            CollectionAssert.AreEqual(plain, ViolatorSlots.Pick(12, 2, new SeededRandom(seed), null), $"seed {seed}: null");
            CollectionAssert.AreEqual(plain, ViolatorSlots.Pick(12, 2, new SeededRandom(seed), new int[0]), $"seed {seed}: empty");
            CollectionAssert.AreEqual(plain, ViolatorSlots.Pick(12, 2, new SeededRandom(seed), new[] { 7, 9 }), $"seed {seed}: second-half exclusions change nothing");
        }
    }

    /// <summary>The day plan's guarantee counts (traveller types §2.3, phase 9): day 2's three of five, day 3's four of six, day 4's three of six, day 5's three of seven, distinct and in the first half.</summary>
    [TestCase(10, 3, 5)]
    [TestCase(12, 4, 6)]
    [TestCase(12, 3, 6)]
    [TestCase(13, 3, 7)]
    public void TheGuaranteedFaultyTravellers_OfEachDay_FitTheFirstHalf(int queue, int planned, int window)
    {
        for (int seed = 0; seed < 100; seed++)
        {
            int[] slots = ViolatorSlots.Pick(queue, planned, new SeededRandom(seed));
            Assert.AreEqual(planned, slots.Length, $"seed {seed}");
            CollectionAssert.AllItemsAreUnique(slots, $"seed {seed}");
            Assert.That(slots, Is.All.InRange(1, window), $"seed {seed}");
        }
    }

    [Test]
    public void RoomProblems_NoneWhileTheFreeWindowHoldsTheGuarantees()
    {
        CollectionAssert.IsEmpty(ViolatorSlots.RoomProblems("DayPlan_Inv_Day13", 17, new[] { 6 }, 3), "9 slots in the window, 8 free, 3 guarantees");
        CollectionAssert.IsEmpty(ViolatorSlots.RoomProblems("tight", 4, new[] { 1 }, 1), "2 slots, 1 free, 1 guarantee");
        CollectionAssert.IsEmpty(ViolatorSlots.RoomProblems("none", 14, new int[0], 0));
    }

    [Test]
    public void RoomProblems_WarnsWhenForcedSlotsLeaveTooFewFreeSlots()
    {
        List<string> problems = ViolatorSlots.RoomProblems("crowded", 6, new[] { 1, 2 }, 2);
        Assert.AreEqual(1, problems.Count, "3 slots in the window, 1 free, 2 guarantees: one is dropped");
        StringAssert.Contains("'crowded'", problems[0]);
        StringAssert.Contains("1 free", problems[0]);
        StringAssert.Contains("2 guaranteed", problems[0]);
    }

    [Test]
    public void RoomProblems_ForcedSlotsPastTheWindowTakeNoRoom()
    {
        CollectionAssert.IsEmpty(ViolatorSlots.RoomProblems("DayPlan_Inv_Day6", 14, new[] { 8, 11 }, 7), "day 6's famous stand past slot 7");
        CollectionAssert.IsEmpty(ViolatorSlots.RoomProblems("repeat", 6, new[] { 3, 3, 3 }, 2), "a slot's alternatives take its one slot");
    }

    [Test]
    public void SameSeed_SameSlots()
    {
        CollectionAssert.AreEqual(ViolatorSlots.Pick(12, 2, new SeededRandom(42)), ViolatorSlots.Pick(12, 2, new SeededRandom(42)));
    }
}
