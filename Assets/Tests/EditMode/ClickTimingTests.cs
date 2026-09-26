using NUnit.Framework;

/// <summary>The double-click rule (the PC redesign DK6, WN3): inside and outside the time and the distance, the first click, and a third click starting over.</summary>
public class ClickTimingTests
{
    private const float Seconds = 0.4f;
    private const float Distance = 6f;

    [Test]
    public void IsDouble_InsideTheTimeAndTheDistance()
    {
        Assert.IsTrue(ClickTiming.IsDouble(1f, 10f, 10f, 1.3f, 13f, 14f, Seconds, Distance));
        Assert.IsTrue(ClickTiming.IsDouble(1f, 10f, 10f, 1.4f, 16f, 10f, Seconds, Distance), "the limits count");
    }

    [Test]
    public void IsDouble_OutsideTheTime()
    {
        Assert.IsFalse(ClickTiming.IsDouble(1f, 10f, 10f, 1.41f, 10f, 10f, Seconds, Distance));
        Assert.IsFalse(ClickTiming.IsDouble(1f, 10f, 10f, 0.9f, 10f, 10f, Seconds, Distance), "a click before the last one");
    }

    [Test]
    public void IsDouble_OutsideTheDistance()
    {
        Assert.IsFalse(ClickTiming.IsDouble(1f, 10f, 10f, 1.1f, 15f, 15f, Seconds, Distance));
        Assert.IsFalse(ClickTiming.IsDouble(1f, 10f, 10f, 1.1f, 10f, 16.5f, Seconds, Distance));
    }

    [Test]
    public void IsDouble_TheFirstClickIsNeverDouble()
    {
        Assert.IsFalse(ClickTiming.IsDouble(ClickTiming.Never, 0f, 0f, 0f, 0f, 0f, Seconds, Distance));
        Assert.IsFalse(ClickTiming.IsDouble(ClickTiming.Never, 0f, 0f, float.MaxValue, 0f, 0f, float.MaxValue, Distance));
    }

    [Test]
    public void DoubleClick_TheSecondQuickClickCompletesIt_AndAThirdStartsOver()
    {
        var clicks = new DoubleClick();
        Assert.IsFalse(clicks.Click(1f, 0f, 0f, Seconds, Distance), "the first click");
        Assert.IsTrue(clicks.Click(1.2f, 1f, 1f, Seconds, Distance), "the second");
        Assert.IsFalse(clicks.Click(1.3f, 1f, 1f, Seconds, Distance), "a third starts a new pair");
        Assert.IsTrue(clicks.Click(1.5f, 1f, 1f, Seconds, Distance), "and completes it with a fourth");
    }

    [Test]
    public void DoubleClick_ASlowOrFarSecondClickStartsANewPair()
    {
        var clicks = new DoubleClick();
        clicks.Click(1f, 0f, 0f, Seconds, Distance);
        Assert.IsFalse(clicks.Click(2f, 0f, 0f, Seconds, Distance), "too slow");
        Assert.IsFalse(clicks.Click(2.1f, 50f, 0f, Seconds, Distance), "too far");
        Assert.IsTrue(clicks.Click(2.2f, 51f, 0f, Seconds, Distance), "a pair with the far click");
    }
}
