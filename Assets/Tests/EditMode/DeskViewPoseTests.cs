using NUnit.Framework;

/// <summary>
/// The desk view's pose (piece 10 section 11, T2): the pitch that aims the
/// moved camera at the mat's centre, plus the pitch knob, kept between level
/// and straight down; the blend's seconds, a cut under Reduced Motion.
/// </summary>
public class DeskViewPoseTests
{
    private const float Eps = 1e-3f;

    private static DeskViewTuning Tuning(float forward, float rise, float pitch, float seconds = 0.35f) =>
        new DeskViewTuning { forward = forward, rise = rise, pitch = pitch, seconds = seconds };

    [Test]
    public void Pitch_AimsAtTheMatsCentre()
    {
        Assert.AreEqual(45f, DeskViewPose.Pitch(1f, 1f, Tuning(0f, 0f, 0f)), Eps);
        Assert.AreEqual(27.539f, DeskViewPose.Pitch(1.095f, 2.1f, Tuning(0f, 0f, 0f)), Eps, "the art office's camera over its mat");
    }

    [Test]
    public void Pitch_IsTakenFromTheMovedPose()
    {
        Assert.AreEqual(45f, DeskViewPose.Pitch(1f, 1.5f, Tuning(0.5f, 0f, 0f)), Eps, "moving forward shortens the depth");
        Assert.AreEqual(45f, DeskViewPose.Pitch(0.5f, 1f, Tuning(0f, 0.5f, 0f)), Eps, "rising adds height");
        Assert.AreEqual(71.565f, DeskViewPose.Pitch(1f, 1f, Tuning(0.5f, 0.5f, 0f)), Eps, "both: 1.5 m above, 0.5 m ahead");
    }

    [Test]
    public void Pitch_AddsTheKnob()
    {
        Assert.AreEqual(50f, DeskViewPose.Pitch(1f, 1f, Tuning(0f, 0f, 5f)), Eps);
        Assert.AreEqual(40f, DeskViewPose.Pitch(1f, 1f, Tuning(0f, 0f, -5f)), Eps);
    }

    [Test]
    public void Pitch_StaysBetweenLevelAndStraightDown()
    {
        Assert.AreEqual(DeskViewPose.MaxPitch, DeskViewPose.Pitch(1f, 0.5f, Tuning(1f, 0f, 0f)), Eps, "moved past the mat's centre");
        Assert.AreEqual(DeskViewPose.MaxPitch, DeskViewPose.Pitch(1f, 1f, Tuning(0f, 0f, 60f)), Eps, "a large knob");
        Assert.AreEqual(0f, DeskViewPose.Pitch(-0.2f, 1f, Tuning(0f, 0f, 0f)), Eps, "below the mat");
    }

    [Test]
    public void Seconds_AreTheKnobs_ACutUnderReducedMotion()
    {
        Assert.AreEqual(0.35f, DeskViewPose.Seconds(Tuning(0f, 0f, 0f, 0.35f), false), Eps);
        Assert.AreEqual(0f, DeskViewPose.Seconds(Tuning(0f, 0f, 0f, 0.35f), true), Eps);
        Assert.AreEqual(0f, DeskViewPose.Seconds(Tuning(0f, 0f, 0f, -1f), false), Eps);
    }

    [Test]
    public void TheDefaults_AreTheSpecsKnobs()
    {
        var t = new DeskViewTuning();
        Assert.AreEqual(0.6f, t.forward, Eps);
        Assert.AreEqual(0.3f, t.rise, Eps);
        Assert.AreEqual(-4f, t.pitch, Eps);
        Assert.AreEqual(0.35f, t.seconds, Eps);
    }
}
