using NUnit.Framework;

/// <summary>The monitor camera's framing: the glass fills a fraction of the view's height, or of its width on narrow screens.</summary>
public class MonitorFramingTests
{
    private const float Tolerance = 0.001f;

    [Test]
    public void TheCrtGlass_AtSixteenByNine_IsHeightLimited()
    {
        // The glass is 2.343 x 1.758 world units; at fill 0.85 it is 85% of the view's height.
        Assert.AreEqual(1.034f, MonitorFraming.OrthoSize(2.343f, 1.758f, 16f / 9f, 0.85f), Tolerance);
    }

    [Test]
    public void AWideGlass_IsWidthLimited_AtFourByThreeAndAtOneByOne()
    {
        Assert.AreEqual(3f, MonitorFraming.OrthoSize(4f, 1f, 4f / 3f, 0.5f), Tolerance, "width 4 at 4:3 needs a view 3 high");
        Assert.AreEqual(2f, MonitorFraming.OrthoSize(2f, 1f, 1f, 0.5f), Tolerance, "width 2 at 1:1 needs a view 2 high");
    }

    [Test]
    public void TheFill_IsClampedToFivePercentAndOne()
    {
        Assert.AreEqual(10f, MonitorFraming.OrthoSize(1f, 1f, 1f, 0f), Tolerance, "0 counts as 0.05");
        Assert.AreEqual(0.5f, MonitorFraming.OrthoSize(1f, 1f, 1f, 2f), Tolerance, "2 counts as 1");
    }

    [Test]
    public void AnAspectOfZeroOrLess_CountsAsOne()
    {
        Assert.AreEqual(2f, MonitorFraming.OrthoSize(2f, 1f, 0f, 0.5f), Tolerance);
        Assert.AreEqual(2f, MonitorFraming.OrthoSize(2f, 1f, -3f, 0.5f), Tolerance);
    }
}
