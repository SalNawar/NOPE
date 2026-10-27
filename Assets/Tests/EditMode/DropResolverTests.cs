using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// Where a dropped paper rests (DOC-HANDLE, BUG-6 and BUG-8; Saleh's note 25):
/// DropResolver keeps it exactly where it was released unless part of it
/// would lie out of view or under a prop; then it moves the least it can,
/// clear of every keep-out at once; a refusal rests just beside what refused
/// it; the folder obeys the same rule; the footprint, not the pointer,
/// decides the dock; and the final size is what is judged.
/// </summary>
public class DropResolverTests
{
    private const float Eps = 1e-5f;

    /// <summary>The desk the reading view shows: 1.2 m wide, 0.8 m deep, centred on the desk.</summary>
    private static readonly DeskRect Area = new DeskRect(0f, 0f, 1.2f, 0.8f);

    [Test]
    public void ADrop_RestsExactlyWhereItWasReleased()
    {
        var paper = new DeskRect(0.13f, -0.07f, 0.26f, 0.34f);
        var keepOut = new List<DeskRect> { new DeskRect(0.45f, 0.2f, 0.2f, 0.2f) };
        Assert.AreEqual((0.13f, -0.07f), DropResolver.Rest(paper, Area, keepOut), "nothing in the way: not a millimetre of move");
        Assert.AreEqual((0.13f, -0.07f), DropResolver.Rest(paper, Area, null), "with no keep-outs at all");
    }

    [Test]
    public void ADropPartlyOutOfView_RestsWhollyInside_TheLeastMove()
    {
        var paper = new DeskRect(0.55f, -0.3f, 0.26f, 0.34f); // its right edge at 0.68 and its bottom at -0.47: past the area's 0.6 and -0.4
        (float x, float y) = DropResolver.Rest(paper, Area, null);
        Assert.AreEqual(0.6f - 0.13f, x, Eps, "its right edge on the area's");
        Assert.AreEqual(-0.4f + 0.17f, y, Eps, "its bottom edge on the area's");
        (float wx, _) = DropResolver.Inside(new DeskRect(0.3f, 0f, 2f, 0.3f), Area);
        Assert.AreEqual(0f, wx, Eps, "a paper wider than the area is centred on it");
    }

    [Test]
    public void ADropOverAProp_MovesTheLeastWayClearOfIt()
    {
        var prop = new DeskRect(0.3f, 0f, 0.2f, 0.2f); // x 0.2..0.4
        var paper = new DeskRect(0.12f, 0f, 0.2f, 0.2f); // x 0.02..0.22: 2 cm over the prop's left edge
        (float x, float y) = DropResolver.Rest(paper, Area, new List<DeskRect> { prop });
        Assert.AreEqual(0.2f - DropResolver.Gap - 0.1f, x, Eps, "just left of it: the shortest way out");
        Assert.AreEqual(0f, y, Eps, "and no other move");
        Assert.AreEqual(0f, DropResolver.Overlap(new DeskRect(x, y, 0.2f, 0.2f), new List<DeskRect> { prop }), "never under a prop");
    }

    [Test]
    public void TheKeepOuts_AreSolvedTogether_NeverOnePushIntoAnother()
    {
        // Pushed out of the right prop the short way (left), a one-at-a-time push landed in the left prop (BUG-6).
        var left = new DeskRect(-0.05f, 0f, 0.2f, 0.4f);
        var right = new DeskRect(0.3f, 0f, 0.2f, 0.4f);
        var paper = new DeskRect(0.16f, 0f, 0.2f, 0.2f);
        var keepOut = new List<DeskRect> { left, right };
        (float x, float y) = DropResolver.Rest(paper, Area, keepOut);
        var rest = new DeskRect(x, y, 0.2f, 0.2f);
        Assert.AreEqual(0f, DropResolver.Overlap(rest, keepOut), "clear of both at once");
        Assert.LessOrEqual(System.Math.Abs(x - 0.16f) + System.Math.Abs(y), 0.35f, "and still the nearest clear place");
    }

    [Test]
    public void ARefusalOnTheCounter_RestsJustOffIt_NeverFlyingBack()
    {
        // A desk paper dropped on the counter before the verdict: the counter (y from 0.3 on) is a keep-out; it rests just nearer.
        var counter = new DeskRect(0f, 0.3f + 50f, 100f, 100f);
        var paper = new DeskRect(-0.2f, 0.25f, 0.26f, 0.34f); // released with its top 12 cm over the counter
        (float x, float y) = DropResolver.Rest(paper, Area, new List<DeskRect> { counter });
        Assert.AreEqual(-0.2f, x, Eps, "in place across");
        Assert.AreEqual(0.3f - DropResolver.Gap - 0.17f, y, Eps, "just off the counter's near edge: about 12 cm, not back to where it was picked up");
    }

    [Test]
    public void TheFolder_ObeysTheSameRule()
    {
        // The rulebook folder dropped over the scanner's casing rests just beside it, as a paper would.
        var scanner = new DeskRect(0.4f, 0.1f, 0.3f, 0.25f);
        var folder = new DeskRect(0.3f, 0.1f, 0.36f, 0.27f);
        (float x, float y) = DropResolver.Rest(folder, Area, new List<DeskRect> { scanner });
        Assert.AreEqual(0f, DropResolver.Overlap(new DeskRect(x, y, 0.36f, 0.27f), new List<DeskRect> { scanner }));
        var inView = new DeskRect(x, y, 0.36f, 0.27f);
        Assert.AreEqual((x, y), DropResolver.Inside(inView, Area), "and wholly in view");
    }

    [Test]
    public void TheFootprint_NotThePointer_DecidesTheDock()
    {
        var reader = new DeskRect(0.3f, 0f, 0.26f, 0.26f);
        var scanner = new DeskRect(-0.3f, 0f, 0.3f, 0.3f);
        var docks = new List<DeskRect> { reader, scanner };
        // DeskController.DockAt calls DockOf with the paper's footprint (DeskDocument.RestCorners at its root) and each machine's own drop-area test.
        int Dock(DeskRect paper) => DropResolver.DockOf(paper, docks, (d, x, y) => d.Contains(x, y));
        // Grabbed by its left edge, the pointer at x 0.1 is off the reader, but the paper's centre at x 0.25 lies on it.
        Assert.AreEqual(0, Dock(new DeskRect(0.25f, 0f, 0.26f, 0.34f)), "the paper's centre lies on the reader");
        // Grabbed by its right edge over the reader, the paper itself hangs off it: no dock.
        Assert.AreEqual(-1, Dock(new DeskRect(0.05f, 0.3f, 0.26f, 0.34f)));
        Assert.AreEqual(1, Dock(new DeskRect(-0.32f, 0.02f, 0.1f, 0.1f)));
        Assert.AreEqual(-1, DropResolver.DockOf(new DeskRect(0f, 0f, 1f, 1f), (List<DeskRect>)null, (d, x, y) => true));
        Assert.AreEqual(-1, DropResolver.DockOf(new DeskRect(0f, 0f, 1f, 1f), docks, null));
    }

    [Test]
    public void WhereTwoDropAreasMeet_TheFirstListed_TakesIt()
    {
        // DeskController.DockAt lists the chip reader first: where its drop area meets the scanner's, the reader takes the paper.
        var reader = new DeskRect(0.1f, 0f, 0.3f, 0.3f);
        var scanner = new DeskRect(-0.1f, 0f, 0.3f, 0.3f);
        var tried = new List<int>();
        int dock = DropResolver.DockOf(new DeskRect(0f, 0f, 0.2f, 0.2f), new List<DeskRect> { reader, scanner }, (d, x, y) =>
        {
            tried.Add(tried.Count);
            return d.Contains(x, y);
        });
        Assert.AreEqual(0, dock, "both hold the centre: the first listed wins");
        Assert.AreEqual(1, tried.Count, "and the rest are not tried");
        Assert.AreEqual(1, DropResolver.DockOf(new DeskRect(0f, 0f, 0.2f, 0.2f), new List<DeskRect> { new DeskRect(5f, 5f, 1f, 1f), scanner }, (d, x, y) => d.Contains(x, y)));
    }

    [Test]
    public void TheFinalSize_IsWhatIsJudged()
    {
        // A counter paper (0.16 by 0.2) dropped on the desk grows to its desk size (0.26 by 0.34) under the cursor first:
        // at its counter size it would clear the prop; at the size it rests at it does not, so it is judged at that size.
        var prop = new DeskRect(0.3f, 0f, 0.2f, 0.2f);
        var keepOut = new List<DeskRect> { prop };
        Assert.AreEqual(0f, DropResolver.Overlap(new DeskRect(0.1f, 0f, 0.16f, 0.2f), keepOut), "the counter size clears it");
        (float x, _) = DropResolver.Rest(new DeskRect(0.1f, 0f, 0.26f, 0.34f), Area, keepOut);
        Assert.AreEqual(0.2f - DropResolver.Gap - 0.13f, x, Eps, "the desk size does not: it rests clear at that size");
    }

    [Test]
    public void WhereNothingIsClear_ItRestsWhereItOverlapsLeast()
    {
        var everything = new DeskRect(0f, 0f, 1.2f, 0.8f);
        var paper = new DeskRect(0.1f, 0.1f, 0.26f, 0.34f);
        (float x, float y) = DropResolver.Rest(paper, Area, new List<DeskRect> { everything });
        Assert.AreEqual((x, y), DropResolver.Inside(new DeskRect(x, y, 0.26f, 0.34f), Area), "still wholly in view");
    }
}
