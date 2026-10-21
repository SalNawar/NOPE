using NUnit.Framework;

/// <summary>
/// How the UV torch is held (FIX_PLAN TOOLS, BUG-2c; GAME_FEEL_BIBLE C3): its
/// body never covers the patch it lights, seen from the camera; the pool is
/// drawn only on the desk; the grip trails the pointer by at most 8 mm; the
/// revealed marks fluoresce in over 120 ms.
/// </summary>
public class UvTorchHoldTests
{
    /// <summary>The torch as built (uv_torch_model.py, metres): the hold's height and tilt (DeskConfigSO), the lens and the tail cap along the body from the grip, the head's radius, the cone's radius.</summary>
    private const float Height = 0.14f, Tilt = 40f, LensLength = 0.1285f, TailLength = 0.066f, BodyRadius = 0.0245f, ConeRadius = 0.075f;

    /// <summary>The farthest the hold may put the patch from the grip (metres): it stays by the hand.</summary>
    private const float MaxReach = 0.35f;

    /// <summary>The reading view's eye over a grip near the mat's middle (DeskViewTuning: 0.8 m from the aim at an 80-degree pitch).</summary>
    private static readonly TorchPoint ReadingEye = new TorchPoint(0f, 0.79f, -0.14f);

    /// <summary>Eyes from both views, the grip at the origin and the beam pointing away along +z: the reading view over the grip, behind it, ahead of it and to its side; the low office view (about 9 to 17 degrees down) straight on and from the side.</summary>
    private static readonly TorchPoint[] Eyes =
    {
        ReadingEye, new TorchPoint(0f, 0.79f, -0.4f), new TorchPoint(0f, 0.79f, 0.2f), new TorchPoint(0.3f, 0.79f, 0f),
        new TorchPoint(0f, 0.42f, -1.2f), new TorchPoint(0.3f, 0.42f, -1f), new TorchPoint(0f, 0.35f, -0.8f), new TorchPoint(-0.4f, 0.4f, -0.9f),
    };

    /// <summary>The share of the patch <paramref name="eye"/> sees with the torch held at <paramref name="tilt"/> and <paramref name="yaw"/>, and the patch's distance from the grip.</summary>
    private static float ShareAt(TorchPoint eye, float tilt, float yaw, out float reach)
    {
        double radians = yaw * System.Math.PI / 180.0;
        float dirX = (float)System.Math.Sin(radians), dirZ = (float)System.Math.Cos(radians);
        (float x, float z) = UvCone.Ahead(0f, 0f, dirX, dirZ, Height, tilt);
        reach = (float)System.Math.Sqrt(x * x + z * z);
        var aim = new TorchPoint(x, 0f, z);
        UvTorchHold.Body(new TorchPoint(0f, Height, 0f), aim, TailLength, LensLength, out TorchPoint tail, out TorchPoint lens);
        return UvTorchHold.Unoccluded(eye, tail, lens, BodyRadius, aim, ConeRadius);
    }

    private static TorchAim HoldFor(TorchPoint eye) =>
        UvTorchHold.Hold(eye, 0f, 0f, 0f, Height, 0f, 1f, Tilt, TailLength, LensLength, BodyRadius, ConeRadius);

    [Test]
    public void Unoccluded_ABodyLyingOverThePatch_CoversIt_OneHeldAside_DoesNot()
    {
        var aim = new TorchPoint(0f, 0f, 0f);
        var eye = new TorchPoint(0f, 1f, 0f);
        Assert.Less(UvTorchHold.Unoccluded(eye, new TorchPoint(-0.05f, 0.1f, 0f), new TorchPoint(0.05f, 0.1f, 0f), 0.02f, aim, 0.075f), 0.9f,
                    "a body right across the patch, between it and the eye, hides part of it");
        Assert.AreEqual(1f, UvTorchHold.Unoccluded(eye, new TorchPoint(0.3f, 0.1f, 0f), new TorchPoint(0.2f, 0.1f, 0f), 0.02f, aim, 0.075f), 1e-6f,
                        "a body well to the side hides nothing");
    }

    [Test]
    public void TheBuiltHold_CoversThePatch_FromTheReadingView()
    {
        Assert.Less(ShareAt(ReadingEye, Tilt, 0f, out _), UvTorchHold.UnoccludedShare, "the 1019a hold (40 degrees, straight away): from above, the lens end lies over the patch (BUG-2c)");
    }

    [Test]
    public void Hold_ShowsThePatchPastTheBody_FromEveryView_AndKeepsItByTheHand()
    {
        foreach (TorchPoint eye in Eyes)
        {
            TorchAim aim = HoldFor(eye);
            float share = ShareAt(eye, aim.Tilt, aim.Yaw, out float reach);
            string at = $"eye ({eye.X}, {eye.Y}, {eye.Z}): tilt {aim.Tilt}, turn {aim.Yaw}";
            Assert.GreaterOrEqual(share, UvTorchHold.UnoccludedShare, at);
            Assert.AreEqual(share, aim.Unoccluded, 1e-5f, at + ": the share it reports is the one measured");
            Assert.LessOrEqual(reach, MaxReach, at);
            Assert.GreaterOrEqual(aim.Tilt, UvCone.MinTilt, at);
            Assert.LessOrEqual(aim.Tilt, UvCone.MaxTilt, at);
            Assert.LessOrEqual(System.Math.Abs(aim.Yaw), UvTorchHold.MaxYaw, at);
        }
    }

    [Test]
    public void Hold_KeepsTheBuiltAim_WhenTheEyeAlreadySeesThePatch()
    {
        var eyeBeside = new TorchPoint(0.9f, 0.3f, 0.15f);
        Assert.GreaterOrEqual(ShareAt(eyeBeside, Tilt, 0f, out _), UvTorchHold.TargetShare, "the eye looks at the patch from the side");
        TorchAim aim = HoldFor(eyeBeside);
        Assert.AreEqual(Tilt, aim.Tilt, 1e-4f);
        Assert.AreEqual(0f, aim.Yaw, 1e-4f);
    }

    [Test]
    public void PoolShows_OnlyOnTheDesk()
    {
        Assert.IsTrue(UvTorchHold.PoolShows(0.2f, 0.3f, 0.2f, 0.3f), "a surface hit: the cone's centre is on the desk area");
        Assert.IsFalse(UvTorchHold.PoolShows(0.2f, 0.9f, 0.2f, 0.51f), "a miss: past the desk's far edge there is nothing to light, so no pool floats there");
    }

    [Test]
    public void Follow_TrailsThePointer_ByAtMost8Millimetres()
    {
        (float x, float z) = UvTorchHold.Follow(0f, 0f, 0.3f, 0f, 1f / 60f);
        Assert.AreEqual(0.3f - UvTorchHold.MaxLag, x, 1e-5f, "a fast sweep: the grip is held 8 mm behind");
        Assert.AreEqual(0f, z, 1e-6f);
        (x, _) = UvTorchHold.Follow(0f, 0f, 0.004f, 0f, 1f / 60f);
        Assert.Greater(x, 0f, "a slow move: it eases after the pointer");
        Assert.Less(x, 0.004f);
        (x, _) = UvTorchHold.Follow(0.1f, 0f, 0.1f, 0f, 1f / 60f);
        Assert.AreEqual(0.1f, x, 1e-6f, "at rest under the pointer");
    }

    [Test]
    public void Fluoresce_RisesOver120Milliseconds_AndGoesAtOnce()
    {
        float glow = UvTorchHold.Fluoresce(0f, true, 0.06f, false);
        Assert.AreEqual(0.5f, glow, 1e-4f, "half way after 60 ms");
        glow = UvTorchHold.Fluoresce(glow, true, 0.07f, false);
        Assert.AreEqual(1f, glow, "fully lit by 120 ms");
        Assert.AreEqual(0f, UvTorchHold.Fluoresce(glow, false, 0.01f, false), "switched off: gone at once");
        Assert.AreEqual(1f, UvTorchHold.Fluoresce(0f, true, 0.001f, true), "reduced motion: at once");
    }
}
