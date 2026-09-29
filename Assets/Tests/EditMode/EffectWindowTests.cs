using NUnit.Framework;

/// <summary>An effect's window (audit R3-006): in force from its start day for its duration; one that starts tomorrow is neither in force nor ended today.</summary>
public class EffectWindowTests
{
    [TestCase(3, 2, 2, false, Description = "the day before it starts: a consequence earned tonight waits for tomorrow")]
    [TestCase(3, 2, 3, true, Description = "its first day")]
    [TestCase(3, 2, 4, true, Description = "its last day")]
    [TestCase(3, 2, 5, false, Description = "the day after")]
    [TestCase(3, 0, 3, false, Description = "no days at all")]
    [TestCase(3, -1, 2, false, Description = "a permanent effect before it starts")]
    [TestCase(3, -1, 3, true, Description = "a permanent effect from its start")]
    [TestCase(3, -1, 400, true, Description = "a permanent effect for good")]
    public void IsActive(int startDay, int durationDays, int day, bool expected)
    {
        Assert.AreEqual(expected, EffectWindow.IsActive(startDay, durationDays, day));
    }

    [TestCase(3, 2, 2, false, Description = "not started is not ended: the night's expiry keeps tomorrow's effects")]
    [TestCase(3, 2, 4, false)]
    [TestCase(3, 2, 5, true)]
    [TestCase(3, 0, 3, true)]
    [TestCase(3, -1, 400, false, Description = "a permanent effect never ends by itself")]
    public void HasEnded(int startDay, int durationDays, int day, bool expected)
    {
        Assert.AreEqual(expected, EffectWindow.HasEnded(startDay, durationDays, day));
    }
}
