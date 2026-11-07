using NUnit.Framework;

/// <summary>
/// The speech bubble's reveal count by time (D26): the speed times the time
/// in whole characters, clamped to the line; Reduced Motion and a speed of 0
/// show the whole line at once.
/// </summary>
public class TextRevealTests
{
    [Test]
    public void Count_IsTheSpeedTimesTheTime_RoundedDown()
    {
        Assert.AreEqual(0, TextReveal.Count(40f, 0f, 40));
        Assert.AreEqual(1, TextReveal.Count(40f, 0.025f, 40));
        Assert.AreEqual(4, TextReveal.Count(40f, 0.11f, 40), "4.4 characters show as 4");
        Assert.AreEqual(20, TextReveal.Count(40f, 0.5f, 40));
        Assert.AreEqual(5, TextReveal.Count(10f, 0.5f, 11));
    }

    [Test]
    public void Count_IsClampedToTheLine()
    {
        Assert.AreEqual(40, TextReveal.Count(40f, 1f, 40), "done at 1 s");
        Assert.AreEqual(40, TextReveal.Count(40f, 30f, 40), "never past the line");
        Assert.AreEqual(0, TextReveal.Count(40f, -1f, 40), "nothing before the line starts");
        Assert.AreEqual(0, TextReveal.Count(40f, float.NaN, 40));
        Assert.AreEqual(0, TextReveal.Count(40f, 1f, 0), "an empty line");
        Assert.AreEqual(0, TextReveal.Count(40f, 1f, -3));
    }

    [Test]
    public void ReducedMotion_OrNoSpeed_ShowsTheWholeLineAtOnce()
    {
        Assert.AreEqual(40, TextReveal.Count(40f, 0f, 40, true), "Reduced Motion: the whole line on its first frame");
        Assert.AreEqual(40, TextReveal.Count(0f, 0f, 40));
        Assert.AreEqual(40, TextReveal.Count(-5f, 0f, 40));
        Assert.AreEqual(40, TextReveal.Count(float.NaN, 0f, 40));
    }

    [Test]
    public void Seconds_IsTheLineOverTheSpeed_AndZeroAtOnce()
    {
        Assert.AreEqual(1f, TextReveal.Seconds(40f, 40), 1e-6f);
        Assert.AreEqual(1.1f, TextReveal.Seconds(10f, 11), 1e-6f);
        Assert.AreEqual(0f, TextReveal.Seconds(40f, 40, true));
        Assert.AreEqual(0f, TextReveal.Seconds(0f, 40));
        Assert.AreEqual(0f, TextReveal.Seconds(40f, 0));
    }
}
