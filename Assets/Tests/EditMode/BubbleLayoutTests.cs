using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// The speech bubble belongs to the traveller and never covers the wheel
/// (Saleh 2026-10-07: "STEP CLOSER" was hidden under it): with the wheel
/// closed it sits beside and above the head, the tail pointing down at the
/// mouth; with the wheel open (any number of choices, the worst case nine)
/// the box and its tail clear every pill and the centre, on the screen (going
/// beside or under the mouth only where the screen has no room above), for
/// the bubble's whole box (the longest line shrinks inside it), at the office
/// view and the desk view's raised head, at 1080p and 720p (the overlay
/// scales from 1920 x 1080, so both are the same canvas; 720p also checked
/// as a 1280 x 720 canvas, the tightest case).
/// </summary>
public class BubbleLayoutTests
{
    // The office's knobs (DeskConfigSO, the builder's bubble and the kit's tail).
    private const float RadiusX = 365f, RadiusY = 225f, ItemW = 240f, ItemH = 44f, CentreW = 150f, CentreH = 44f;
    private const float BubbleW = 420f, BubbleH = 110f, Tail = 41f, Gap = 8f, TopInset = 96f;

    /// <summary>The wheel's pills and its centre around <paramref name="shoulders"/> (RadialLayout, as the ring lays them out).</summary>
    private static List<FaceRect> Wheel(int n, float sx, float sy)
    {
        var rects = new List<FaceRect>();
        if (n == 0)
            return rects;
        for (int i = 0; i < n; i++)
        {
            (float x, float y) = RadialLayout.Point(i, n, RadiusX, RadiusY);
            rects.Add(new FaceRect(sx + x - ItemW / 2f, sy + y - ItemH / 2f, sx + x + ItemW / 2f, sy + y + ItemH / 2f));
        }
        rects.Add(new FaceRect(sx - CentreW / 2f, sy - CentreH / 2f, sx + CentreW / 2f, sy + CentreH / 2f));
        return rects;
    }

    private static FaceRect Screen(float w, float h) => new FaceRect(-w / 2f, -h / 2f, w / 2f, h / 2f - TopInset);

    /// <summary>The traveller as the office shows them (canvas units from the centre, y up): shoulders, the head's top 230 above, the mouth 80 under it.</summary>
    private static IEnumerable<(string name, float sx, float sy, float w, float h)> Views()
    {
        yield return ("1080p office view", 0f, -220f, 1920f, 1080f);
        yield return ("720p office view (the same canvas)", 0f, -220f, 1920f, 1080f);
        yield return ("720p as a 1280x720 canvas", 0f, -147f, 1280f, 720f);
        yield return ("desk view, the head near the top", 0f, 160f, 1920f, 1080f);
        yield return ("traveller off centre", 260f, -220f, 1920f, 1080f);
    }

    [Test]
    public void OpenWheel_TheBubbleAndItsTailNeverOverlapAPill()
    {
        foreach (var v in Views())
            foreach (int n in new[] { 1, 3, 4, 6, 8, 9 })
            {
                List<FaceRect> wheel = Wheel(n, v.sx, v.sy);
                FaceRect screen = Screen(v.w, v.h);
                BubblePlacement p = BubbleLayout.Place(v.sx, v.sy + 230f, v.sx, v.sy + 150f, BubbleW, BubbleH, Tail, screen, wheel, Gap);
                Assert.IsTrue(p.Clear, $"{v.name}, {n} choices: a clear place exists");
                foreach (FaceRect pill in wheel)
                {
                    Assert.IsFalse(BubbleLayout.Overlap(p.Box, pill, 0f), $"{v.name}, {n} choices: the box covers a pill");
                    Assert.IsFalse(BubbleLayout.Overlap(BubbleLayout.TailBox(p, Tail), pill, 0f), $"{v.name}, {n} choices: the tail crosses a pill");
                }
                Assert.IsTrue(p.Box.XMin >= screen.XMin && p.Box.XMax <= screen.XMax && p.Box.YMin >= screen.YMin && p.Box.YMax <= screen.YMax,
                              $"{v.name}, {n} choices: on the screen, under the HUD's band");
            }
    }

    [Test]
    public void ClosedWheel_TheBubbleSitsBesideAndAboveTheHead_TailAtTheMouth()
    {
        foreach (var v in Views())
        {
            float headTop = v.sy + 230f, mouthY = v.sy + 150f;
            FaceRect screen = Screen(v.w, v.h);
            BubblePlacement p = BubbleLayout.Place(v.sx, headTop, v.sx, mouthY, BubbleW, BubbleH, Tail, screen, null, Gap);
            Assert.IsTrue(p.Clear, v.name);
            Assert.IsFalse(BubbleLayout.Overlap(p.Box, new FaceRect(v.sx - 60f, mouthY - 30f, v.sx + 60f, headTop), 0f), $"{v.name}: the box never covers the face");
            Assert.IsTrue(p.Box.XMin <= v.sx && p.Box.XMax >= v.sx || System.Math.Abs(p.Box.CentreX - v.sx) <= BubbleW, $"{v.name}: beside the head");
            if (headTop + Tail + BubbleH <= screen.YMax)
            {
                Assert.GreaterOrEqual(p.Box.YMin, headTop, $"{v.name}: above the head's top");
                Assert.LessOrEqual(p.Box.YMin, headTop + Tail, $"{v.name}: and close to it, not over the bridge");
            }
            if (p.Box.YMin > mouthY)
                Assert.Greater(p.TailBaseY, p.TailTipY, $"{v.name}: above the mouth, the tail points down");
            Assert.LessOrEqual(System.Math.Abs(p.TailDegrees), BubbleLayout.MaxTailDegrees + 1e-3f, $"{v.name}: the tail never lies flat along the edge");
            float before = Dist(p.TailBaseX, p.TailBaseY, v.sx, mouthY), after = Dist(p.TailTipX, p.TailTipY, v.sx, mouthY);
            Assert.Less(after, before, $"{v.name}: the tail points at the mouth");
        }
    }

    [Test]
    public void TheBubbleStaysAboveTheMouthWhenThereIsRoom_AndFallsBackOnScreen()
    {
        var wall = new List<FaceRect> { new FaceRect(-2000f, -2000f, 2000f, 2000f) }; // nothing clears
        FaceRect screen = Screen(1920f, 1080f);
        BubblePlacement p = BubbleLayout.Place(0f, 10f, 0f, -70f, BubbleW, BubbleH, Tail, screen, wall, Gap);
        Assert.IsFalse(p.Clear);
        Assert.IsTrue(p.Box.XMin >= screen.XMin && p.Box.YMax <= screen.YMax, "the fallback stays on the screen");
        BubblePlacement open = BubbleLayout.Place(0f, 10f, 0f, -70f, BubbleW, BubbleH, Tail, screen, Wheel(9, 0f, -220f), Gap);
        Assert.GreaterOrEqual(open.Box.YMin, -70f, "with room above, never below the mouth");
    }

    [Test]
    public void TailDegrees_TurnsTowardTheMouth()
    {
        Assert.AreEqual(0f, new BubblePlacement(new FaceRect(0, 0, 1, 1), 0f, 0f, 0f, -10f, true).TailDegrees, 1e-4f);
        Assert.Greater(new BubblePlacement(new FaceRect(0, 0, 1, 1), 0f, 0f, 5f, -10f, true).TailDegrees, 0f, "down and right: counter-clockwise");
        Assert.Less(new BubblePlacement(new FaceRect(0, 0, 1, 1), 0f, 0f, -5f, -10f, true).TailDegrees, 0f);
    }

    private static float Dist(float ax, float ay, float bx, float by) => (float)System.Math.Sqrt((ax - bx) * (ax - bx) + (ay - by) * (ay - by));
}
