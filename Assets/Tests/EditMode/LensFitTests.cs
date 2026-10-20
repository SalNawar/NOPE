using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// The two fixed camera poses at the screen's aspect (CAM: BUG-41, 1b, 7b).
/// The office pose: at 5:4 the office view widened to the 16:9 width (its top
/// edge kept, down to the desk's edge); at 21:9 it narrows to the hall art's
/// width (its bottom edge kept). The reading pose: at every aspect the big
/// mat, the side homes' inner strips and the deal tray fit inside the
/// margins, with the least distance, centred, and the screen's bottom row
/// never past the desk's front edge.
/// </summary>
public class LensFitTests
{
    private static float Tan(float degrees) => (float)System.Math.Tan(degrees * System.Math.PI / 180.0);

    private static readonly float[] Aspects = { 16f / 9f, 16f / 10f, 2560f / 1080f, 5f / 4f };

    private static float Front => DeskHomeMap.DeskTop.CentreY - DeskHomeMap.DeskTop.Height / 2f;

    private static List<(float x, float y, float z)> ApprovedTargets(DeskViewTuning t) => DeskViewPose.Targets(t, DeskHomeMap.Mat, DeskHomeMap.Approved());

    private static ReadingPose Solve(DeskViewTuning t, float aspect, float front)
    {
        Assert.IsTrue(LensFit.Reading(ApprovedTargets(t), DeskViewPose.Pitch(t), t.fieldOfView, aspect, t.topMargin, t.bottomMargin, t.sideMargin, front, out ReadingPose pose),
                      "solves at " + aspect);
        return pose;
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
    public void Reading_FramesTheMatTheSideStripsAndTheTray_InsideTheMargins_AtEveryAspect()
    {
        var t = new DeskViewTuning();
        List<(float x, float y, float z)> targets = ApprovedTargets(t);
        foreach (float aspect in Aspects)
        {
            ReadingPose pose = Solve(t, aspect, Front);
            foreach ((float x, float y, float z) p in targets)
            {
                Assert.IsTrue(LensFit.Project(pose, t.pitch, t.fieldOfView, aspect, p, out (float x, float y) ndc), "in front at " + aspect);
                Assert.LessOrEqual(ndc.y, 1f - t.topMargin + 1e-3f, $"{p} under the top margin at {aspect}");
                Assert.GreaterOrEqual(ndc.y, -(1f - t.bottomMargin) - 1e-3f, $"{p} over the bottom margin at {aspect}");
                Assert.LessOrEqual(System.Math.Abs(ndc.x), 1f - t.sideMargin + 1e-3f, $"{p} inside the side margins at {aspect}");
            }
            Assert.GreaterOrEqual(pose.BottomRowY, Front - 1e-3f, "under the desk never shows at " + aspect);
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
            bool allIn = true;
            foreach ((float x, float y, float z) p in targets)
                if (!LensFit.Project(nearer, t.pitch, t.fieldOfView, aspect, p, out (float x, float y) ndc) ||
                    ndc.y > 1f - t.topMargin || ndc.y < -(1f - t.bottomMargin) || System.Math.Abs(ndc.x) > 1f - t.sideMargin)
                    allIn = false;
            Assert.IsFalse(allIn, "3 cm nearer, something leaves the frame at " + aspect);
        }
    }

    [Test]
    public void Reading_TheBigMat_ShowsPapersAboutTwoThirdsAsBigAsTheOldView()
    {
        // Saleh's "keep a big mat size" (SPEC Q5 B): the reading view backs out, papers about 67% of the old 0.8 m view.
        var t = new DeskViewTuning();
        ReadingPose wide = Solve(t, 16f / 9f, Front);
        float share = 0.8f / wide.Distance;
        Assert.That(share, Is.InRange(0.55f, 0.72f), "papers on screen against the old reading view at 16:9: " + share);
        Assert.AreEqual(wide.Distance, Solve(t, 2560f / 1080f, Front).Distance, 0.02f, "21:9 is framed by its height, as 16:9");
        Assert.Greater(Solve(t, 5f / 4f, Front).Distance, wide.Distance, "5:4 backs out for the width");
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
        Assert.AreEqual(t.bottomMargin - t.topMargin, top + bottom, 2e-3f, "centred between the margins");

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
