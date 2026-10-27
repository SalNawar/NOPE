using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// The two fixed camera poses at the screen's aspect (CAM: BUG-41, 1b, 7b).
/// The office pose: at 5:4 the office view widened to the 16:9 width (its top
/// edge kept, down to the desk's edge); at 21:9 it narrows to the hall art's
/// width (its bottom edge kept). The reading pose: at every aspect the big
/// mat, the stamp rack and the intercom whole, the other side homes' inner
/// strips and the deal tray's strip nearest the mat fit inside the margins
/// under the overlay's top band of chrome, with the least distance, centred,
/// and the screen's bottom row never past the desk's front edge; papers show
/// at the measured share of the old reading view: on the plan's map, and on
/// the painted desk's measured map (PaintedDeskFixture: the prefab's homes,
/// the map in use while the painted desk shows).
/// </summary>
public class LensFitTests
{
    private static float Tan(float degrees) => (float)System.Math.Tan(degrees * System.Math.PI / 180.0);

    private static readonly float[] Aspects = { 16f / 9f, 16f / 10f, 2560f / 1080f, 5f / 4f };

    private static float Front => DeskHomeMap.DeskTop.CentreY - DeskHomeMap.DeskTop.Height / 2f;

    private static List<(float x, float y, float z)> ApprovedTargets(DeskViewTuning t) => DeskViewPose.Targets(t, DeskHomeMap.Mat, DeskHomeMap.Approved());

    /// <summary>The targets on the painted desk's map (the prefab's measured homes over the plan's: the rack's outer edge at x 1.11, the intercom's at -1.09, the deal tray's mouth).</summary>
    private static List<(float x, float y, float z)> PaintedTargets(DeskViewTuning t) => DeskViewPose.Targets(t, DeskHomeMap.Mat, PaintedDeskFixture.Map());

    private static ReadingPose Solve(DeskViewTuning t, float aspect, float front, List<(float x, float y, float z)> targets = null)
    {
        Assert.IsTrue(LensFit.Reading(targets ?? ApprovedTargets(t), DeskViewPose.Pitch(t), t.fieldOfView, aspect, DeskViewPose.TopMargin(t, aspect), t.bottomMargin, t.sideMargin, front, out ReadingPose pose),
                      "solves at " + aspect);
        return pose;
    }

    /// <summary>Every target inside the margins under the top band at every aspect, the bottom row on the desk, and 3 cm nearer something leaves the frame (the least distance).</summary>
    private static void AssertFramed(DeskViewTuning t, List<(float x, float y, float z)> targets, string map)
    {
        foreach (float aspect in Aspects)
        {
            ReadingPose pose = Solve(t, aspect, Front, targets);
            float top = DeskViewPose.TopMargin(t, aspect);
            var nearer = new ReadingPose(pose.AimX, pose.AimY, pose.Distance - 0.03f, pose.BottomRowY);
            bool allIn = true;
            foreach ((float x, float y, float z) p in targets)
            {
                Assert.IsTrue(LensFit.Project(pose, t.pitch, t.fieldOfView, aspect, p, out (float x, float y) ndc), $"{map}: in front at {aspect}");
                Assert.LessOrEqual(ndc.y, 1f - top + 1e-3f, $"{map}: {p} under the overlay's top band of chrome at {aspect}");
                Assert.GreaterOrEqual(ndc.y, -(1f - t.bottomMargin) - 1e-3f, $"{map}: {p} over the bottom margin at {aspect}");
                Assert.LessOrEqual(System.Math.Abs(ndc.x), 1f - t.sideMargin + 1e-3f, $"{map}: {p} inside the side margins at {aspect}");
                if (!LensFit.Project(nearer, t.pitch, t.fieldOfView, aspect, p, out (float x, float y) near) ||
                    near.y > 1f - top || near.y < -(1f - t.bottomMargin) || System.Math.Abs(near.x) > 1f - t.sideMargin)
                    allIn = false;
            }
            Assert.GreaterOrEqual(pose.BottomRowY, Front - 1e-3f, $"{map}: under the desk never shows at {aspect}");
            Assert.IsFalse(allIn, $"{map}: 3 cm nearer, something leaves the frame at {aspect}");
        }
    }

    [Test]
    public void Fit_KeepsTheArtsLensFromTheDesignAspectToTheArtsWidth()
    {
        Assert.AreEqual((55f, 0f), LensFit.Fit(55f, LensFit.DesignAspect, 16f / 9f, 42f));
        Assert.AreEqual((55f, 0f), LensFit.Fit(55f, LensFit.DesignAspect, 2f, 42f));
        Assert.AreEqual((55f, 0f), LensFit.Fit(55f, LensFit.DesignAspect, LensFit.ArtAspect, 42f));
        Assert.AreEqual((55f, 0f), LensFit.Fit(55f, LensFit.DesignAspect, 0f, 42f));
    }

    [Test]
    public void Fit_AScreenWiderThanTheArt_SeesNoWiderThanIt_ItsBottomEdgeKept()
    {
        // 21:9 showed black top corners (BUG-41): the view narrows to the art's width and keeps the desk at its bottom.
        const float wide = 2560f / 1080f;
        (float fov, float tilt) = LensFit.Fit(58f, LensFit.DesignAspect, wide, 42f);
        Assert.Less(fov, 58f);
        Assert.AreEqual(Tan(29f) * LensFit.ArtAspect, Tan(fov / 2f) * wide, 1e-4f, "as wide as the art");
        Assert.AreEqual(29f, fov / 2f + tilt, 1e-4f, "the bottom edge where the art's is");
        Assert.Greater(tilt, 0f, "it gives up the bare top of the hall");
    }

    [Test]
    public void Fit_ANarrowerScreenSeesAsWideAsTheDesignAspect_ItsTopEdgeKept()
    {
        // 16:10 has room below: the full 16:9 width, tilted down so the top edge stays the art's.
        (float fov, float tilt) = LensFit.Fit(55f, LensFit.DesignAspect, 16f / 10f, 42f);
        Assert.AreEqual(Tan(27.5f) * LensFit.DesignAspect, Tan(fov / 2f) * 1.6f, 1e-4f);
        Assert.AreEqual(27.5f, fov / 2f - tilt, 1e-4f);
    }

    [Test]
    public void Fit_WidensNoFurtherThanTheDeskReachesBelow()
    {
        // 5:4 would need 73 degrees; the desk ends 42 degrees below: half of 27.5 + 42, the top edge kept, the bottom on the desk's edge.
        (float fov, float tilt) = LensFit.Fit(55f, LensFit.DesignAspect, 5f / 4f, 42f);
        Assert.AreEqual(69.5f, fov, 1e-4f);
        Assert.AreEqual(7.25f, tilt, 1e-4f);
        Assert.AreEqual(42f, fov / 2f + tilt, 1e-4f);
        Assert.Greater(Tan(fov / 2f) * 1.25f, Tan(27.5f) * LensFit.DesignAspect * 0.9f);
    }

    [Test]
    public void Reading_FramesTheMatTheRackTheIntercomTheSideStripsAndTheTray_InsideTheMargins_AtEveryAspect()
    {
        var t = new DeskViewTuning();
        AssertFramed(t, ApprovedTargets(t), "the plan's map");
    }

    [Test]
    public void Reading_OnThePaintedDesk_FramesTheRackWhole_InsideTheMargins_AtEveryAspect()
    {
        // The night of 2026-10-28 (integrate/night 80f6335: the painted desk on): the pose read the plan's map and left 4 of the
        // painted rack's 24 corners off screen (its outer edge 109 px past the screen's right at 1080p). On the map in use, the rack
        // (x 0.815 to 1.11, 24 cm tall) and the intercom (-1.09 to -0.80) are whole at every aspect.
        var t = new DeskViewTuning();
        List<(float x, float y, float z)> targets = PaintedTargets(t);
        AssertFramed(t, targets, "the painted map");
        DeskHome rack = DeskHomeMap.Find(PaintedDeskFixture.Map(), DeskHomeMap.StampRack);
        DeskRect f = rack.Footprint;
        foreach (float aspect in Aspects)
        {
            ReadingPose pose = Solve(t, aspect, Front, targets);
            foreach (float x in new[] { f.CentreX - f.Width / 2f, f.CentreX + f.Width / 2f })
                foreach (float y in new[] { f.CentreY - f.Height / 2f, f.CentreY + f.Height / 2f })
                    foreach (float z in new[] { 0f, rack.height })
                    {
                        Assert.IsTrue(LensFit.Project(pose, t.pitch, t.fieldOfView, aspect, (x, y, z), out (float x, float y) ndc));
                        Assert.LessOrEqual(System.Math.Abs(ndc.x), 1f, $"the rack's corner ({x}, {y}, {z}) on screen at {aspect}");
                        Assert.LessOrEqual(System.Math.Abs(ndc.y), 1f, $"the rack's corner ({x}, {y}, {z}) on screen at {aspect}");
                    }
        }
    }

    [Test]
    public void Reading_BacksOutNoFurtherThanItMust()
    {
        var t = new DeskViewTuning();
        List<(float x, float y, float z)> targets = ApprovedTargets(t);
        foreach (float aspect in Aspects)
        {
            ReadingPose pose = Solve(t, aspect, Front);
            var nearer = new ReadingPose(pose.AimX, pose.AimY, pose.Distance - 0.03f, pose.BottomRowY);
            float top = DeskViewPose.TopMargin(t, aspect);
            bool allIn = true;
            foreach ((float x, float y, float z) p in targets)
                if (!LensFit.Project(nearer, t.pitch, t.fieldOfView, aspect, p, out (float x, float y) ndc) ||
                    ndc.y > 1f - top || ndc.y < -(1f - t.bottomMargin) || System.Math.Abs(ndc.x) > 1f - t.sideMargin)
                    allIn = false;
            Assert.IsFalse(allIn, "3 cm nearer, something leaves the frame at " + aspect);
        }
    }

    [Test]
    public void Reading_TheBigMat_ShowsPapersAtTheMeasuredShareOfTheOldView()
    {
        // Saleh's "keep a big mat size" was put to him as papers at about 67% of the old view (SPEC Q5 B, a different map);
        // on the approved map with the desk plan's §6 framing and the top band, the solve gives about 55% at 16:9, 52% at
        // 16:10, 41% at 5:4 (CAM.md: Needs Saleh). These pin the solve, so a change to it shows here. On the landed map (DESK-PLAN
        // ffa5ce5: the scanner 8 cm further right, its inner strip now the frame's right) the solve gives 54% at 16:9, 50% at 16:10
        // and 39% at 5:4 (CAM's landing, 2026-10-27).
        var t = new DeskViewTuning();
        ReadingPose wide = Solve(t, 16f / 9f, Front);
        Assert.That(DeskViewPose.PaperShare(wide.Distance, t.fieldOfView), Is.InRange(0.53f, 0.57f), "papers against the old reading view at 16:9");
        Assert.That(DeskViewPose.PaperShare(Solve(t, 16f / 10f, Front).Distance, t.fieldOfView), Is.InRange(0.48f, 0.52f), "at 16:10");
        Assert.That(DeskViewPose.PaperShare(Solve(t, 5f / 4f, Front).Distance, t.fieldOfView), Is.InRange(0.37f, 0.41f), "at 5:4");
        Assert.AreEqual(wide.Distance, Solve(t, 2560f / 1080f, Front).Distance, 0.02f, "21:9 is framed by its height, as 16:9");
        Assert.Greater(Solve(t, 5f / 4f, Front).Distance, wide.Distance, "5:4 backs out for the width");
    }

    [Test]
    public void Reading_OnThePaintedDesk_ShowsPapersAtTheMeasuredShareOfTheOldView()
    {
        // The painted desk (desk v3, the default): the stamp rack's outer edge at x 1.11 (the plan's frame right was the scanner's strip
        // at 0.95) and the intercom's at -1.09 (-1.00) widen the frame to 2.20 m, and its deal tray, the 60 cm slot under the counter,
        // is framed by its 8 cm mouth: the solve backs out to 1.66 m at 16:9 (the plan's 1.47): papers 48% at 16:9, 44% at 16:10,
        // 51% at 21:9, 35% at 5:4 of the old reading view (fix/night-cam-pose, 2026-10-28; the plan's map: 54%, 50%, 55%, 39%).
        // Framed whole, the slot would have put them at 36% at every aspect. These pin the solve on the prefab's measured homes.
        var t = new DeskViewTuning();
        List<(float x, float y, float z)> targets = PaintedTargets(t);
        ReadingPose wide = Solve(t, 16f / 9f, Front, targets);
        Assert.That(DeskViewPose.PaperShare(wide.Distance, t.fieldOfView), Is.InRange(0.46f, 0.50f), "papers against the old reading view at 16:9");
        Assert.That(DeskViewPose.PaperShare(Solve(t, 16f / 10f, Front, targets).Distance, t.fieldOfView), Is.InRange(0.42f, 0.46f), "at 16:10");
        Assert.That(DeskViewPose.PaperShare(Solve(t, 2560f / 1080f, Front, targets).Distance, t.fieldOfView), Is.InRange(0.485f, 0.525f), "at 21:9");
        Assert.That(DeskViewPose.PaperShare(Solve(t, 5f / 4f, Front, targets).Distance, t.fieldOfView), Is.InRange(0.33f, 0.37f), "at 5:4");
        Assert.Greater(wide.Distance, Solve(t, 16f / 9f, Front).Distance, "the painted desk's wider frame (the rack whole at x 1.11) backs out further than the plan's");
        Assert.Greater(Solve(t, 5f / 4f, Front, targets).Distance, wide.Distance, "5:4 backs out for the width");
    }

    [Test]
    public void Reading_CentresBetweenTheMargins_UnlessTheDeskEdgeHoldsItForward()
    {
        var t = new DeskViewTuning();
        List<(float x, float y, float z)> targets = ApprovedTargets(t);
        ReadingPose pose = Solve(t, 16f / 9f, Front);
        float top = float.MinValue, bottom = float.MaxValue;
        foreach ((float x, float y, float z) p in targets)
        {
            LensFit.Project(pose, t.pitch, t.fieldOfView, 16f / 9f, p, out (float x, float y) ndc);
            top = System.Math.Max(top, ndc.y);
            bottom = System.Math.Min(bottom, ndc.y);
        }
        Assert.AreEqual(t.bottomMargin - DeskViewPose.TopMargin(t, 16f / 9f), top + bottom, 2e-3f, "centred between the margins");

        // A desk edge right under the mat holds the camera forward: the bottom row lands on it.
        const float edge = -0.6f;
        ReadingPose held = Solve(t, 5f / 4f, edge);
        Assert.AreEqual(edge, held.BottomRowY, 2e-3f);
        Assert.Greater(held.Distance, Solve(t, 5f / 4f, Front).Distance - 1e-3f);
    }

    [Test]
    public void Reading_RefusesNothingToFrameOrABadLens()
    {
        var t = new DeskViewTuning();
        Assert.IsFalse(LensFit.Reading(new List<(float, float, float)>(), 80f, 50f, 1.7f, 0f, 0f, 0f, -1f, out _));
        Assert.IsFalse(LensFit.Reading(ApprovedTargets(t), 90f, 50f, 1.7f, 0f, 0f, 0f, -1f, out _), "straight down loses the yaw");
        Assert.IsFalse(LensFit.Reading(ApprovedTargets(t), 80f, 0f, 1.7f, 0f, 0f, 0f, -1f, out _));
        Assert.IsFalse(LensFit.Reading(ApprovedTargets(t), 80f, 50f, 0f, 0f, 0f, 0f, -1f, out _));
    }
}
