using NUnit.Framework;

/// <summary>
/// A moving paper's bend (Saleh's 1018a playtest: "all documents are stiff"):
/// PaperBend bows the paper along its travel, the trailing end most, twists
/// its corners and ripples it from the leading edge back; its slopes are its
/// depth's, the paper rises by its lowest point, and flat is flat everywhere.
/// </summary>
public class PaperBendTests
{
    private const float W = 0.29f, H = 0.38f;

    [Test]
    public void Flat_IsFlatEverywhere()
    {
        PaperBend flat = PaperBend.Flat;
        Assert.IsTrue(flat.IsFlat);
        for (float x = -1f; x <= 1f; x += 0.5f)
            for (float y = -1f; y <= 1f; y += 0.5f)
            {
                Assert.AreEqual(0f, flat.Depth(x, y, W, H));
                Assert.AreEqual((0f, 0f), flat.Slope(x, y, W, H));
            }
        Assert.AreEqual(0f, flat.Lowest(W, H));
    }

    [Test]
    public void ABow_DroopsBothEndsAlongTheTravel_TheTrailingEndMost()
    {
        var bend = new PaperBend(0f, 1f, 0.02f, 0.5f, 0f, 0f, 0f, 0f); // travelling up the page
        Assert.AreEqual(0f, bend.Depth(0f, 0f, W, H), 1e-6f, "the middle, where the hand holds it, stays");
        float trailing = bend.Depth(0f, -1f, W, H), leading = bend.Depth(0f, 1f, W, H);
        Assert.Greater(leading, 0f, "both ends droop away from the viewer");
        Assert.Greater(trailing, leading, "the trailing end lags most");
        Assert.AreEqual(0.03f, trailing, 1e-5f, "the bow times one and a half at the trailing end (lag 0.5)");
        Assert.AreEqual(bend.Depth(-1f, 0.5f, W, H), bend.Depth(1f, 0.5f, W, H), 1e-6f, "across the travel it does not bend");
        var curl = new PaperBend(0f, 1f, -0.01f, 0f, 0f, 0f, 0f, 0f);
        Assert.Less(curl.Depth(0f, 1f, W, H), 0f, "a negative bow curls the ends up toward the viewer");
    }

    [Test]
    public void ATwist_FlapsOppositeCornersOppositeWays()
    {
        var bend = new PaperBend(1f, 0f, 0f, 0f, 0.01f, 0f, 0f, 0f);
        Assert.AreEqual(bend.Depth(1f, 1f, W, H), -bend.Depth(1f, -1f, W, H), 1e-6f);
        Assert.AreEqual(bend.Depth(1f, 1f, W, H), bend.Depth(-1f, -1f, W, H), 1e-6f);
        Assert.AreEqual(0.01f, System.Math.Abs(bend.Depth(1f, 1f, W, H)), 1e-6f, "the twist's height at the corners");
    }

    [Test]
    public void TheRipple_IsStillAtTheLeadingEdge_AndRunsWithItsPhase()
    {
        var a = new PaperBend(1f, 0f, 0f, 0f, 0f, 0.008f, 0.3f, 1.5f);
        var b = new PaperBend(1f, 0f, 0f, 0f, 0f, 0.008f, 1.3f, 1.5f);
        Assert.AreEqual(0f, a.Depth(1f, 0f, W, H), 1e-6f, "nothing at the leading edge");
        Assert.AreNotEqual(a.Depth(-0.5f, 0f, W, H), b.Depth(-0.5f, 0f, W, H), "it moves as its phase grows");
        Assert.LessOrEqual(System.Math.Abs(a.Depth(-1f, 0.3f, W, H)), 0.008f + 1e-6f, "never more than its height");
    }

    [Test]
    public void TheSlopes_AreTheDepthsSlopes()
    {
        var bend = new PaperBend(0.6f, 0.8f, 0.018f, 0.4f, 0.006f, 0.005f, 0.7f, 1.5f);
        const float e = 1e-3f;
        foreach ((float x, float y) in new[] { (0.3f, -0.4f), (-0.8f, 0.6f), (0.1f, 0.9f), (-0.5f, -0.9f) })
        {
            (float dx, float dy) = bend.Slope(x, y, W, H);
            // a metre across is 2 / W of X
            float nx = (bend.Depth(x + e, y, W, H) - bend.Depth(x - e, y, W, H)) / (2f * e) * 2f / W;
            float ny = (bend.Depth(x, y + e, W, H) - bend.Depth(x, y - e, W, H)) / (2f * e) * 2f / H;
            Assert.AreEqual(nx, dx, 2e-3f, $"across at {x},{y}");
            Assert.AreEqual(ny, dy, 2e-3f, $"up at {x},{y}");
        }
    }

    [Test]
    public void Lowest_IsAtLeastEveryDepth_AndNeverBelowZero()
    {
        var bend = new PaperBend(0.6f, 0.8f, 0.018f, 0.4f, 0.006f, 0.005f, 0.7f, 1.5f);
        float lowest = bend.Lowest(W, H);
        for (float x = -1f; x <= 1f; x += 0.25f)
            for (float y = -1f; y <= 1f; y += 0.25f)
                Assert.GreaterOrEqual(lowest + 1e-6f, bend.Depth(x, y, W, H));
        Assert.AreEqual(0f, new PaperBend(1f, 0f, -0.02f, 0f, 0f, 0f, 0f, 0f).Lowest(W, H), "curled up, nothing goes under");
    }
}
