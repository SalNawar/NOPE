using NUnit.Framework;

/// <summary>Which day plan a day uses: its own, else the latest earlier one, else none.</summary>
public class DayPlansTests
{
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
}
