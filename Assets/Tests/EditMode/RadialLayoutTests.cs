using System;
using NUnit.Framework;

/// <summary>
/// The traveller wheel's ring: where each item sits, how many choices fit,
/// and the ring's extent. The wheel's defaults: radii 300 x 200, items
/// 240 x 44, the centre ("&lt; Back") 150 x 44, gap 8 (reference px).
/// </summary>
public class RadialLayoutTests
{
    private const float Tolerance = 0.001f;

    private static int FitOfTheDefaults(int limit) => RadialLayout.MaxFit(300f, 200f, 240f, 44f, 150f, 44f, 8f, limit);

    [Test]
    public void Point_ItemZeroIsAtTheTop()
    {
        (float x, float y) = RadialLayout.Point(0, 8, 300f, 200f);
        Assert.AreEqual(0f, x, Tolerance);
        Assert.AreEqual(200f, y, Tolerance);
    }

    [Test]
    public void Point_GoesClockwise()
    {
        (float x, float y) = RadialLayout.Point(1, 8, 300f, 200f);
        Assert.Greater(x, 0f, "item 1 is right of the top");
        Assert.AreEqual(300f * Math.Cos(Math.PI / 4), x, Tolerance);
        Assert.AreEqual(200f * Math.Sin(Math.PI / 4), y, Tolerance);
    }

    [Test]
    public void Point_FourItemsSitOnTheAxes()
    {
        var expected = new[] { (0f, 200f), (300f, 0f), (0f, -200f), (-300f, 0f) };
        for (int i = 0; i < 4; i++)
        {
            (float x, float y) = RadialLayout.Point(i, 4, 300f, 200f);
            Assert.AreEqual(expected[i].Item1, x, Tolerance, $"item {i} x");
            Assert.AreEqual(expected[i].Item2, y, Tolerance, $"item {i} y");
        }
    }

    [Test]
    public void Point_NoItems_IsTheCentre()
    {
        Assert.AreEqual((0f, 0f), RadialLayout.Point(0, 0, 300f, 200f));
        Assert.AreEqual((0f, 0f), RadialLayout.Point(2, -1, 300f, 200f));
    }

    [Test]
    public void MaxFit_TheDefaultsFitEight_TheInterviewsMenuCapacity()
    {
        // n = 7: the lowest pair sits at (+-130.1, -180.2), 260.2 >= 248 apart;
        // n = 8: top neighbours (0, 200) and (212.1, 141.4) are 58.6 >= 52 apart vertically;
        // n = 9: neighbours (0, 200) and (192.8, 153.2) are 46.8 < 52 apart vertically.
        Assert.AreEqual(8, FitOfTheDefaults(16));
    }

    [Test]
    public void MaxFit_ACircleOfRadius210_FitsFour()
    {
        // n = 5 already fails: items 2 and 3 sit level at (+-123.4, -169.9), 246.9 < 248 apart.
        Assert.AreEqual(4, RadialLayout.MaxFit(210f, 210f, 240f, 44f, 150f, 44f, 8f, 16));
    }

    [Test]
    public void MaxFit_ACentreAsLargeAsTheRing_FitsNothing()
    {
        Assert.AreEqual(0, RadialLayout.MaxFit(300f, 200f, 240f, 44f, 900f, 500f, 8f, 16));
    }

    [Test]
    public void MaxFit_RespectsTheLimit()
    {
        Assert.AreEqual(5, FitOfTheDefaults(5));
        Assert.AreEqual(0, FitOfTheDefaults(0));
    }

    [Test]
    public void Extent_IsTheBoxAroundEveryPossibleItem()
    {
        Assert.AreEqual((840f, 444f), RadialLayout.Extent(300f, 200f, 240f, 44f));
    }
}
