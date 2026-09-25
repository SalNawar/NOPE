using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// Which day plan a day uses: its own, else the latest earlier one, else none;
/// and the day plans' identity and size problems Generate World and the
/// validator both report (audit R6-001).
/// </summary>
public class DayPlansTests
{
    private static DayPlanEntry E(string asset, int day, int queue) => new DayPlanEntry(asset, day, queue);

    [TestCase(new[] { 1, 2, 3 }, 2, 1)]
    [TestCase(new[] { 1, 2, 3 }, 5, 2, Description = "the latest earlier plan")]
    [TestCase(new[] { 1, 2, 3 }, 0, -1)]
    [TestCase(new int[0], 1, -1)]
    [TestCase(new[] { 1, 3, 2 }, 7, 1, Description = "the largest day below, not the last entry")]
    [TestCase(new[] { 1, 2, 2 }, 2, 1, Description = "the first on a repeat")]
    [TestCase(new[] { 1, 3, 3 }, 5, 1, Description = "the first of a repeated latest day")]
    [TestCase(new[] { 4, 6 }, 2, -1, Description = "no plan at or before the day")]
    public void Pick(int[] days, int day, int expected)
    {
        Assert.AreEqual(expected, DayPlans.Pick(days, day));
    }

    [Test]
    public void Pick_NullList()
    {
        Assert.AreEqual(-1, DayPlans.Pick(null, 3));
    }

    [Test]
    public void Problems_TheShippedDays_HaveNone()
    {
        var days = new List<DayPlanEntry>();
        for (int d = 1; d <= 6; d++)
            days.Add(E($"DayPlan_Inv_Day{d}", d, 6 + 2 * d));
        CollectionAssert.IsEmpty(DayPlans.Problems(days));
        CollectionAssert.IsEmpty(DayPlans.Problems(null));
    }

    /// <summary>The audit's first failing input: a seventh day copied from day 6 that keeps its asset name overwrote day 6's plan and was wired twice.</summary>
    [Test]
    public void Problems_AnAssetNameUsedTwice_IsReported_IgnoringCase()
    {
        CollectionAssert.AreEqual(new[] { "Day plan asset 'dayplan_inv_day6' is used twice (days 6 and 7)." },
            DayPlans.Problems(new[] { E("DayPlan_Inv_Day6", 6, 14), E("dayplan_inv_day6", 7, 14) }));
    }

    /// <summary>A day planned twice is reported once, in the validator's words.</summary>
    [Test]
    public void Problems_ADayPlannedTwice_IsReportedOnce()
    {
        CollectionAssert.AreEqual(new[] { "Duplicate DayPlan for day 2 (asset 'B')." },
            DayPlans.Problems(new[] { E("A", 2, 8), E("B", 2, 8), E("C", 2, 8) }));
    }

    /// <summary>The audit's second failing input: a day without "queue" read 0 and was written as a day of no travellers.</summary>
    [Test]
    public void Problems_AQueueBelowOne_ADayBelowOne_AndABlankAssetName()
    {
        CollectionAssert.AreEqual(new[] { "Day plan 'B' queues 0 travellers; a day needs at least 1." },
            DayPlans.Problems(new[] { E("A", 1, 8), E("B", 2, 0) }));
        CollectionAssert.AreEqual(new[] { "Day plan 'A' is for day 0; days start at 1." },
            DayPlans.Problems(new[] { E("A", 0, 8) }));
        CollectionAssert.AreEqual(new[] { "Day plan for day 2 has a blank asset name." },
            DayPlans.Problems(new[] { E("A", 1, 8), E(" ", 2, 8) }));
    }
}
