using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// Where a paper handed over while papers are held lands (piece 10 Q7: the
/// third paper lands where the player sees it): the visible share of a spot
/// under the covers, the choice among the spots (whole first, the paper held
/// longest sent back when nothing shows whole, else the spot that shows most)
/// and the fallback grid's order.
/// </summary>
public class PaperLandingTests
{
    private static readonly ScreenRect Screen = new ScreenRect(0f, 0f, 1920f, 1080f);

    private static ScreenRect R(float x0, float y0, float x1, float y1) => new ScreenRect(x0, y0, x1, y1);

    private static readonly ScreenRect None = default;

    [Test]
    public void ScreenRect_AreaIntersectionAndEnclosing()
    {
        ScreenRect a = R(0f, 0f, 100f, 50f), b = R(50f, 25f, 150f, 75f);
        Assert.AreEqual(5000f, a.Area);
        Assert.AreEqual(1250f, a.Intersect(b).Area);
        Assert.IsTrue(a.Intersect(R(200f, 0f, 300f, 50f)).IsEmpty, "apart");
        ScreenRect e = ScreenRect.Enclosing(a, b);
        Assert.AreEqual((0f, 0f, 150f, 75f), (e.XMin, e.YMin, e.XMax, e.YMax));
        Assert.AreEqual((0f, 0f, 100f, 50f), (ScreenRect.Enclosing(a, None).XMin, ScreenRect.Enclosing(a, None).YMin, ScreenRect.Enclosing(None, a).XMax, ScreenRect.Enclosing(None, a).YMax),
                        "an empty rectangle adds nothing");
        Assert.IsTrue(R(10f, 10f, 5f, 20f).IsEmpty && R(10f, 10f, 5f, 20f).Area == 0f, "inverted is empty");
    }

    [Test]
    public void VisibleShare_CountsTheScreenAndEveryCoverOnce()
    {
        ScreenRect paper = R(100f, 100f, 200f, 200f);
        Assert.AreEqual(1f, PaperLanding.VisibleShare(paper, new ScreenRect[0], Screen), 1e-5f, "nothing covers it");
        Assert.AreEqual(0.5f, PaperLanding.VisibleShare(paper, new[] { R(150f, 0f, 400f, 400f) }, Screen), 1e-5f, "its right half is covered");
        Assert.AreEqual(0.5f, PaperLanding.VisibleShare(paper, new[] { R(150f, 0f, 400f, 400f), R(160f, 120f, 300f, 180f) }, Screen), 1e-5f,
                        "overlapping covers are counted once");
        Assert.AreEqual(0.25f, PaperLanding.VisibleShare(paper, new[] { R(150f, 0f, 400f, 400f), R(0f, 150f, 400f, 400f) }, Screen), 1e-5f, "two covers leave a quarter");
        Assert.AreEqual(0f, PaperLanding.VisibleShare(paper, new[] { R(0f, 0f, 1000f, 1000f) }, Screen), 1e-5f, "under a bigger cover");
        Assert.AreEqual(0.5f, PaperLanding.VisibleShare(R(-50f, 0f, 50f, 100f), new ScreenRect[0], Screen), 1e-5f, "half off the screen");
        Assert.AreEqual(0f, PaperLanding.VisibleShare(None, new ScreenRect[0], Screen), "an empty paper shows nothing");
        Assert.AreEqual(1f, PaperLanding.VisibleShare(paper, new[] { None, R(200f, 100f, 300f, 200f) }, Screen), 1e-5f, "an empty cover and a touching one hide nothing");
    }

    /// <summary>With papers held, a spot a held paper covers is skipped for the next that shows whole (the order given).</summary>
    [Test]
    public void Choose_TheFirstSpotThatShowsWhole()
    {
        var spots = new[] { R(900f, 200f, 1020f, 270f), R(1300f, 150f, 1420f, 220f), R(600f, 150f, 720f, 220f) };
        ScreenRect rightHeld = R(995f, 22f, 1358f, 497f);
        Assert.AreEqual(new LandingChoice(0, false), PaperLanding.Choose(spots, new ScreenRect[0], None, None, Screen), "nothing held: the next in turn");
        Assert.AreEqual(new LandingChoice(2, false), PaperLanding.Choose(spots, new ScreenRect[0], rightHeld, R(1000f, 150f, 1120f, 220f), Screen),
                        "the held paper covers the first two: the third");
        Assert.AreEqual(new LandingChoice(-1, false), PaperLanding.Choose(new ScreenRect[0], new ScreenRect[0], rightHeld, None, Screen), "no spots");
    }

    /// <summary>The case HUD's strips, the bubble and the wheel hide the desk as well as held papers do.</summary>
    [Test]
    public void Choose_SkipsSpotsUnderTheOverlay()
    {
        var spots = new[] { R(900f, 820f, 1020f, 890f), R(900f, 300f, 1020f, 370f) };
        ScreenRect bubble = R(750f, 783f, 1170f, 893f);
        Assert.AreEqual(new LandingChoice(1, false), PaperLanding.Choose(spots, new[] { bubble }, None, None, Screen));
    }

    /// <summary>Both slots full on a 16:9 screen hide the whole mat: the paper held longest goes back, and the paper lands where the one still held and the returned paper leave it whole.</summary>
    [Test]
    public void Choose_PutsThePaperHeldLongestBack_WhenNothingShowsWhole()
    {
        ScreenRect left = R(562f, 22f, 925f, 497f), right = R(995f, 22f, 1358f, 497f);
        var spots = new[] { R(960f, 200f, 1080f, 270f), R(1200f, 180f, 1320f, 250f), R(700f, 170f, 820f, 240f), R(840f, 120f, 960f, 190f) };
        ScreenRect leftRest = R(700f, 170f, 820f, 240f);
        Assert.AreEqual(new LandingChoice(3, true), PaperLanding.Choose(spots, new[] { right }, left, leftRest, Screen),
                        "spot 2 is where the returned paper lies: spot 3 shows whole");
        Assert.AreEqual(new LandingChoice(2, true), PaperLanding.Choose(spots, new[] { right }, left, None, Screen), "with nothing where it lies, the first clear of the right slot");
    }

    /// <summary>When nothing shows whole either way, the spot that shows most, and the paper is put back only when that shows more.</summary>
    [Test]
    public void Choose_TheSpotThatShowsMost_WhenNoneShowsWhole()
    {
        var spots = new[] { R(0f, 0f, 100f, 100f), R(200f, 0f, 300f, 100f) };
        ScreenRect[] cover = { R(0f, 0f, 60f, 100f) };
        ScreenRect held = R(200f, 0f, 290f, 100f);
        // Kept: spot 0 shows 40%, spot 1 10%. Put back to half of spot 1: spot 1 shows 50%, more than 40%.
        Assert.AreEqual(new LandingChoice(1, true), PaperLanding.Choose(spots, cover, held, R(200f, 0f, 250f, 100f), Screen));
        // Put back over all of spot 1: nothing shows more than keeping it, so it stays held.
        Assert.AreEqual(new LandingChoice(0, false), PaperLanding.Choose(spots, cover, held, R(200f, 0f, 300f, 100f), Screen));
        // Two spots that show the same: the earlier.
        Assert.AreEqual(new LandingChoice(0, false), PaperLanding.Choose(spots, new[] { R(0f, 0f, 50f, 100f), R(200f, 0f, 250f, 100f) }, None, None, Screen));
    }

    [Test]
    public void Choose_WithoutAHeldPaper_NeverPutsBack()
    {
        var spots = new[] { R(0f, 0f, 100f, 100f) };
        LandingChoice c = PaperLanding.Choose(spots, new[] { R(0f, 0f, 1000f, 1000f) }, None, None, Screen);
        Assert.AreEqual(new LandingChoice(0, false), c, "all covered and nothing held: the first spot, as before");
    }

    [Test]
    public void GridSpots_NearestTheCentreFirst_ThenNearEdgeUp_LeftToRight()
    {
        IReadOnlyList<(float u, float v)> grid = PaperLanding.GridSpots(3, 3);
        Assert.AreEqual(9, grid.Count);
        Assert.AreEqual((0.5f, 0.5f), grid[0], "the centre first");
        CollectionAssert.AreEqual(new[] { (0.5f, 1f / 6f), (1f / 6f, 0.5f), (5f / 6f, 0.5f), (0.5f, 5f / 6f) }, grid.Skip(1).Take(4).ToList(), "then the edges' middles, near edge up");
        Assert.AreEqual((1f / 6f, 1f / 6f), grid[5], "then the corners");
        Assert.AreEqual(0, PaperLanding.GridSpots(0, 3).Count);
        Assert.AreEqual(20, PaperLanding.GridSpots(5, 4).Count);
        Assert.IsTrue(PaperLanding.GridSpots(5, 4).All(p => p.u > 0f && p.u < 1f && p.v > 0f && p.v < 1f), "cell centres, inside the area");
    }
}
