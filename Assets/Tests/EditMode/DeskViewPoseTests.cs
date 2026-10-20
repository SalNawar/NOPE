using NUnit.Framework;

/// <summary>
/// The reading view's pose (piece 10 section 11, T2; the desk-first redesign,
/// item 5: an 80 degree tilt; CAM): the fallback pose sits the knob's
/// distance back from the aim point along a view pitched by the knob, the
/// pitch kept between level and straight down; what the solved pose frames
/// (the mat, the stamp rack and the intercom whole, the other side homes by
/// their inner strips, the deal tray whole); the top margin the overlay's
/// chrome band takes at each aspect; papers against the old reading view;
/// the blend's seconds, a cut when motion is still; the controls' fade over
/// the last third of a move (BUG-32b).
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
    public void Seconds_AreTheKnobs_ACutWhenMotionIsStill()
    {
        Assert.AreEqual(0.5f, DeskViewPose.Seconds(Tuning(80f, 1f, 0.5f), false), Eps);
        Assert.AreEqual(0f, DeskViewPose.Seconds(Tuning(80f, 1f, 0.5f), true), Eps, "Reduced Motion or a Motion intensity of 0 (MotionAmount.Still): a cut (BUG-64b)");
        Assert.AreEqual(0f, DeskViewPose.Seconds(Tuning(80f, 1f, -1f), false), Eps);
    }

    [Test]
    public void TheDefaults_AreSalehsTilt()
    {
        var t = new DeskViewTuning();
        Assert.AreEqual(80f, t.pitch, Eps, "Saleh 2026-10-05: the tilt is 80 degrees");
        Assert.Less(t.distance, 1.2f, "the fallback: closer than the old view's 1.2 m to the mat");
        Assert.AreEqual(0.5f, t.seconds, Eps);
        Assert.AreEqual(47.5f, t.fieldOfView, Eps, "the lens main's reading view rested at (50 pushed in by 2.5)");
        Assert.GreaterOrEqual(t.sideReach, 0.08f, "the desk plan's §6: at least 8 x 8 cm of each other side home");
        CollectionAssert.AreEquivalent(new[] { DeskHomeMap.StampRack, DeskHomeMap.Intercom }, t.whole, "the desk plan's §6: rack and intercom 100% in frame");
        CollectionAssert.IsSubsetOf(new[] { DeskHomeMap.ChipReader, DeskHomeMap.Scanner, DeskHomeMap.StampRack, DeskHomeMap.Intercom,
                                            DeskHomeMap.BabelTuner, DeskHomeMap.RulebookPocket, DeskHomeMap.UvTorch, DeskHomeMap.DealTray }, t.homes);
    }

    [Test]
    public void Targets_TheMatWhole_TheRackAndIntercomWhole_TheOtherSideHomesByTheirInnerStrip_TheTrayWhole()
    {
        var t = new DeskViewTuning();
        DeskRect mat = DeskHomeMap.Mat;
        DeskHome[] homes = DeskHomeMap.Approved();
        var points = DeskViewPose.Targets(t, mat, homes);

        Assert.Contains((-0.73f, -0.515f, 0f), points, "the mat's corner on the desk");
        Assert.Contains((0.73f, 0.515f, t.paperLift), points, "and at the papers' lift");

        DeskRect scanner = DeskHomeMap.Find(homes, DeskHomeMap.Scanner).Footprint;
        float inner = scanner.CentreX - scanner.Width / 2f;
        float right = float.MinValue, left = float.MaxValue, scannerFar = float.MinValue;
        foreach ((float x, float y, float z) p in points)
        {
            right = System.Math.Max(right, p.x);
            left = System.Math.Min(left, p.x);
            if (p.y >= scanner.CentreY - scanner.Height / 2f && p.y <= scanner.CentreY + scanner.Height / 2f && p.x > inner)
                scannerFar = System.Math.Max(scannerFar, p.x);
        }
        Assert.AreEqual(inner + t.sideReach, scannerFar, Eps, "the scanner shows by its inner 8 cm");
        DeskRect rack = DeskHomeMap.Find(homes, DeskHomeMap.StampRack).Footprint;
        Assert.AreEqual(rack.CentreX + rack.Width / 2f, right, Eps, "the stamp rack whole: its outer edge is the frame's right");
        DeskRect intercom = DeskHomeMap.Find(homes, DeskHomeMap.Intercom).Footprint;
        Assert.AreEqual(intercom.CentreX - intercom.Width / 2f, left, Eps, "the intercom whole: its outer edge is the frame's left");
        DeskRect babel = DeskHomeMap.Find(homes, DeskHomeMap.BabelTuner).Footprint;
        Assert.Contains((babel.CentreX + babel.Width / 2f - t.sideReach, babel.CentreY - babel.Height / 2f, 0f), points, "the tuner by its inner 8 cm");
        Assert.IsFalse(points.Exists(p => System.Math.Abs(p.x - (babel.CentreX - babel.Width / 2f)) < 1e-4f), "not the tuner's outer edge");

        DeskRect tray = DeskHomeMap.Find(homes, DeskHomeMap.DealTray).Footprint;
        Assert.Contains((tray.CentreX - tray.Width / 2f, tray.CentreY + tray.Height / 2f, 0f), points, "the deal tray whole (over the mat, not beside it)");
    }

    [Test]
    public void TopMargin_IsTheChromeBand_AtTheCanvasRowsOfTheAspect()
    {
        var t = new DeskViewTuning();
        Assert.AreEqual(96f, t.topBand, Eps, "the case HUD's strips and the Back control (16..88 px) and a gap");
        Assert.AreEqual(1080f, DeskViewPose.CanvasRows(16f / 9f), Eps);
        Assert.AreEqual(1080f, DeskViewPose.CanvasRows(2560f / 1080f), Eps, "wider than 16:9: more columns, the same rows");
        Assert.AreEqual(1200f, DeskViewPose.CanvasRows(1.6f), Eps, "16:10: the canvas expands to 1200 rows");
        Assert.AreEqual(1536f, DeskViewPose.CanvasRows(1.25f), Eps, "5:4: 1536 rows");
        Assert.AreEqual(96f / 540f, DeskViewPose.TopMargin(t, 16f / 9f), Eps);
        Assert.AreEqual(96f / 768f, DeskViewPose.TopMargin(t, 1.25f), Eps, "the same band is a smaller share of a taller canvas");
        t.topBand = -5f;
        Assert.AreEqual(0f, DeskViewPose.TopMargin(t, 16f / 9f), Eps);
    }

    [Test]
    public void PaperShare_IsAgainstTheOldReadingView()
    {
        Assert.AreEqual(1f, DeskViewPose.PaperShare(DeskViewPose.OldDistance, DeskViewPose.OldFieldOfView), Eps, "the old view itself");
        Assert.AreEqual(0.5f, DeskViewPose.PaperShare(1.6f, 47.5f), Eps, "twice as far, half as big");
        float wider = DeskViewPose.PaperShare(0.8f, 50f);
        Assert.Less(wider, 1f, "the same distance through a wider lens shows papers smaller");
        Assert.AreEqual(0f, DeskViewPose.PaperShare(0f, 47.5f), Eps);
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
