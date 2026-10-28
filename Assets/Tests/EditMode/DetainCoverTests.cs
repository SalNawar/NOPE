using NUnit.Framework;

/// <summary>
/// DETAIN's safety cover (Saleh's playtest of 2026-10-28, point 2: "detain
/// button case open and cant be closed its collider is small too"): the
/// painted guard swings with the cover's spring, never into the button, and a
/// click on the button's housing under the shut cover lifts the cover.
/// </summary>
public class DetainCoverTests
{
    [Test]
    public void TheGuard_FollowsTheCoversSwing_FromShutToThePaintedOpenPose()
    {
        Assert.AreEqual(0f, DetainCover.Share(0f, 105f), "shut: over the button");
        Assert.AreEqual(1f, DetainCover.Share(105f, 105f), 1e-6f, "open: the painting's raised guard");
        Assert.AreEqual(0.5f, DetainCover.Share(52.5f, 105f), 1e-6f, "half way");
        Assert.Greater(DetainCover.Share(112f, 105f), 1f, "the spring's overshoot past open shows");
    }

    [Test]
    public void TheGuard_NeverSwingsPastShut_IntoTheButton()
    {
        Assert.AreEqual(0f, DetainCover.Share(-6f, 105f), "a spring's overshoot past shut stays shut");
        Assert.AreEqual(0f, DetainCover.Share(30f, 0f), "no open angle: shut");
    }

    [Test]
    public void AClickOnTheButton_LiftsTheShutCover_ThenPresses()
    {
        Assert.AreEqual(DetainClick.LiftCover, DetainCover.OnButton(false), "shut: the first click lifts the cover (it never did nothing)");
        Assert.AreEqual(DetainClick.Press, DetainCover.OnButton(true), "up: the press");
    }
}
