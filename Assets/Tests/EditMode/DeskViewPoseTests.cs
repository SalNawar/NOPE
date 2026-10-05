using NUnit.Framework;

/// <summary>
/// The desk view's pose (piece 10 section 11, T2; the desk-first redesign,
/// item 5: an 80 degree tilt, closer): the camera sits the knob's distance
/// back from the aim point along a view pitched by the knob, the pitch kept
/// between level and straight down; the blend's seconds, a cut under Reduced
/// Motion.
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
        Assert.Less(t.distance, 1.2f, "closer than the old view's 1.2 m to the mat");
        Assert.AreEqual(0.5f, t.seconds, Eps);
    }
}
