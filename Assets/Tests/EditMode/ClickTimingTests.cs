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

    /// <summary>Clicks one target at (time, x) points on a row; whether each completed a double.</summary>
    private static bool[] Clicks(params (float time, float x)[] clicks)
    {
        var target = new DoubleClick();
        var doubles = new bool[clicks.Length];
        for (int i = 0; i < clicks.Length; i++)
            doubles[i] = target.Click(clicks[i].time, clicks[i].x, 0f, Seconds, Distance);
        return doubles;
    }

    [Test]
    public void DoubleClick_TheSecondQuickClickCompletesIt_AndAThirdStartsOver() =>
        CollectionAssert.AreEqual(new[] { false, true, false, true }, Clicks((1f, 0f), (1.2f, 1f), (1.3f, 1f), (1.5f, 1f)),
                                  "first, second, a third starting a new pair, a fourth completing it");

    [Test]
    public void DoubleClick_ASlowOrFarSecondClickStartsANewPair() =>
        CollectionAssert.AreEqual(new[] { false, false, false, true }, Clicks((1f, 0f), (2f, 0f), (2.1f, 50f), (2.2f, 51f)),
                                  "too slow, too far, then a pair with the far click");
}
