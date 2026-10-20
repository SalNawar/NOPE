using NUnit.Framework;

/// <summary>
/// The reading view's pose (piece 10 section 11, T2; the desk-first redesign,
/// item 5: an 80 degree tilt; CAM): the fallback pose sits the knob's
/// distance back from the aim point along a view pitched by the knob, the
/// pitch kept between level and straight down; what the solved pose frames
/// (the mat, the side homes by their inner strips, the deal tray whole); the
/// blend's seconds, a cut under Reduced Motion; the controls' fade over the
/// last third of a move (BUG-32b).
/// </summary>
public class DeskViewPoseTests
{
    private const float Eps = 1e-3f;

    private static DeskViewTuning Tuning(float pitch, float distance, float seconds = 0.5f) =>
        new DeskViewTuning { pitch = pitch, distance = distance, seconds = seconds };

    [Test]
    public void Offset_PutsTheCameraOnTheViewThroughTheAimPoint()
    {
        (float back, float up) = DeskViewPose.Offset(Tuning(45f, 1f));
        Assert.AreEqual(0.7071f, back, Eps);
        Assert.AreEqual(0.7071f, up, Eps);

        (back, up) = DeskViewPose.Offset(Tuning(80f, 0.62f));
        Assert.AreEqual(0.62f * 0.17365f, back, Eps, "80 degrees: nearly straight above");
        Assert.AreEqual(0.62f * 0.98481f, up, Eps);
        Assert.AreEqual(80f, DeskViewPose.Pitch(Tuning(80f, 1f)), Eps);
    }

    [Test]
    public void Pitch_StaysBetweenLevelAndStraightDown()
    {
        Assert.AreEqual(DeskViewPose.MaxPitch, DeskViewPose.Pitch(Tuning(120f, 1f)), Eps);
        Assert.AreEqual(0f, DeskViewPose.Pitch(Tuning(-10f, 1f)), Eps);
        (float back, float up) = DeskViewPose.Offset(Tuning(0f, 2f));
        Assert.AreEqual(2f, back, Eps, "level: straight back");
        Assert.AreEqual(0f, up, Eps);
        Assert.AreEqual(0f, DeskViewPose.Offset(Tuning(80f, -1f)).back, Eps, "a negative distance sits on the aim point");
    }

    [Test]
    public void Seconds_AreTheKnobs_ACutUnderReducedMotion()
    {
        Assert.AreEqual(0.5f, DeskViewPose.Seconds(Tuning(80f, 1f, 0.5f), false), Eps);
        Assert.AreEqual(0f, DeskViewPose.Seconds(Tuning(80f, 1f, 0.5f), true), Eps);
        Assert.AreEqual(0f, DeskViewPose.Seconds(Tuning(80f, 1f, -1f), false), Eps);
    }

    [Test]
    public void TheDefaults_AreSalehsTilt()
    {
        var t = new DeskViewTuning();
        Assert.AreEqual(80f, t.pitch, Eps, "Saleh 2026-10-05: the tilt is 80 degrees");
        Assert.Less(t.distance, 1.2f, "the fallback: closer than the old view's 1.2 m to the mat");
        Assert.AreEqual(0.5f, t.seconds, Eps);
        Assert.GreaterOrEqual(t.sideReach, 0.08f, "DESK-PLAN's hand-off: at least 8 cm of each side home");
        CollectionAssert.IsSubsetOf(new[] { DeskHomeMap.ChipReader, DeskHomeMap.Scanner, DeskHomeMap.StampRack, DeskHomeMap.Intercom,
                                            DeskHomeMap.BabelTuner, DeskHomeMap.RulebookPocket, DeskHomeMap.UvTorch, DeskHomeMap.DealTray }, t.homes);
    }

    [Test]
    public void Targets_TheMatWhole_TheSideHomesByTheirInnerStrip_TheTrayWhole()
    {
        var t = new DeskViewTuning();
        DeskRect mat = DeskHomeMap.Mat;
        DeskHome[] homes = DeskHomeMap.Approved();
        var points = DeskViewPose.Targets(t, mat, homes);

        Assert.Contains((-0.73f, -0.515f, 0f), points, "the mat's corner on the desk");
        Assert.Contains((0.73f, 0.515f, t.paperLift), points, "and at the papers' lift");

        DeskRect scanner = DeskHomeMap.Find(homes, DeskHomeMap.Scanner).Footprint;
        float inner = scanner.CentreX - scanner.Width / 2f;
        float right = float.MinValue, left = float.MaxValue;
        foreach ((float x, float y, float z) p in points)
        {
            right = System.Math.Max(right, p.x);
            left = System.Math.Min(left, p.x);
        }
        Assert.AreEqual(inner + t.sideReach, right, Eps, "the rail shows by its inner 8 cm (the scanner's is the widest reach)");
        DeskRect babel = DeskHomeMap.Find(homes, DeskHomeMap.BabelTuner).Footprint;
        Assert.AreEqual(babel.CentreX + babel.Width / 2f - t.sideReach, left, Eps, "the left column by its inner 8 cm (the tuner's)");

        DeskRect tray = DeskHomeMap.Find(homes, DeskHomeMap.DealTray).Footprint;
        Assert.Contains((tray.CentreX - tray.Width / 2f, tray.CentreY + tray.Height / 2f, 0f), points, "the deal tray whole (over the mat, not beside it)");
    }

    [Test]
    public void Targets_WithoutHomes_AreTheMatAlone()
    {
        var t = new DeskViewTuning();
        Assert.AreEqual(8, DeskViewPose.Targets(t, DeskHomeMap.Mat, null).Count);
        t.homes = new[] { "noSuchHome" };
        Assert.AreEqual(8, DeskViewPose.Targets(t, DeskHomeMap.Mat, DeskHomeMap.Approved()).Count, "an unknown id frames nothing");
    }

    [Test]
    public void ControlAlpha_StaysOutForTwoThirdsOfTheMove_ThenFadesInByItsEnd()
    {
        Assert.AreEqual(0f, DeskViewPose.ControlAlpha(0f, 0.6f), Eps, "nothing on frame 0");
        Assert.AreEqual(0f, DeskViewPose.ControlAlpha(0.399f, 0.6f), Eps, "nothing until 2/3");
        Assert.AreEqual(0.5f, DeskViewPose.ControlAlpha(0.5f, 0.6f), Eps, "half way through the last third");
        Assert.AreEqual(1f, DeskViewPose.ControlAlpha(0.6f, 0.6f), Eps, "in by the move's end");
        Assert.AreEqual(1f, DeskViewPose.ControlAlpha(9f, 0.6f), Eps);
        Assert.AreEqual(1f, DeskViewPose.ControlAlpha(0f, 0f), Eps, "a cut (Reduced Motion): at once");
    }
}
