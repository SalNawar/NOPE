using NUnit.Framework;

/// <summary>
/// The desk props' kicks respect the comfort settings (BUG-64a: Reduced
/// Motion or a Motion intensity of 0 adds no velocity; the intensity scales
/// the rest).
/// </summary>
public class PropKickTests
{
    [Test]
    public void AKick_UnderReducedMotion_AddsNoVelocity()
    {
        Spring s = Spring.At(1f);
        s.Kick(3f, new MotionAmount(1f, true));
        Assert.AreEqual(0f, s.Velocity);
        Assert.IsTrue(s.AtRest, "nothing moves");
    }

    [Test]
    public void AKick_AtNoIntensity_AddsNoVelocity()
    {
        Spring s = Spring.At(0f);
        s.Kick(3f, new MotionAmount(0f, false));
        Assert.AreEqual(0f, s.Velocity);
    }

    [Test]
    public void AKick_ScalesWithTheIntensity()
    {
        Spring s = Spring.At(0f);
        s.Kick(4f, new MotionAmount(0.5f, false));
        Assert.AreEqual(2f, s.Velocity, 1e-6f);
        s.Kick(4f, MotionAmount.Full);
        Assert.AreEqual(6f, s.Velocity, 1e-6f);
    }
}
