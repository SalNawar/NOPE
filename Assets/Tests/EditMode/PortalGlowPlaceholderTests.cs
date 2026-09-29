using NUnit.Framework;

/// <summary>The portal rings' placeholder effects (the portals spec v3 VX1, VX5) and the departure flare's curve (VX4).</summary>
public class PortalGlowPlaceholderTests
{
    private static byte Alpha(byte[] rgba, int x, int y) => rgba[(y * PortalGlowPlaceholder.Size + x) * 4 + 3];

    [Test]
    public void EachEffect_IsWhite_BrightInside_ClearAtTheRim()
    {
        int size = PortalGlowPlaceholder.Size;
        foreach (byte[] rgba in new[] { PortalGlowPlaceholder.Glow(), PortalGlowPlaceholder.ReturnSpiral() })
        {
            Assert.AreEqual(size * size * 4, rgba.Length);
            for (int i = 0; i < rgba.Length; i += 4)
                Assert.IsTrue(rgba[i] == 255 && rgba[i + 1] == 255 && rgba[i + 2] == 255, "white, tinted by the game");
            Assert.Greater(Alpha(rgba, size / 2, size / 2), 150, "bright at the centre");
            Assert.AreEqual(0, Alpha(rgba, 0, 0), "a corner is clear");
            Assert.AreEqual(0, Alpha(rgba, size / 2, 0), "the rim is clear, so the edge hides under the ring's frame");
            Assert.AreEqual(0, Alpha(rgba, size - 1, size / 2));
        }
    }

    [Test]
    public void TheReturnGatesSpiral_IsItsOwn()
    {
        CollectionAssert.AreNotEqual(PortalGlowPlaceholder.Glow(), PortalGlowPlaceholder.ReturnSpiral());
    }

    [Test]
    public void Pulse_RisesAndFalls_OrOneStepWithReducedMotion()
    {
        Assert.AreEqual(0f, PortalGlowPlaceholder.Pulse(0f, false));
        Assert.AreEqual(1f, PortalGlowPlaceholder.Pulse(0.5f, false), 1e-5f);
        Assert.AreEqual(0f, PortalGlowPlaceholder.Pulse(1f, false));
        Assert.Greater(PortalGlowPlaceholder.Pulse(0.25f, false), 0.5f);
        Assert.AreEqual(1f, PortalGlowPlaceholder.Pulse(0.1f, true), "reduced motion: one step up");
        Assert.AreEqual(1f, PortalGlowPlaceholder.Pulse(0.9f, true));
        Assert.AreEqual(0f, PortalGlowPlaceholder.Pulse(1f, true), "and down at its end");
    }
}
