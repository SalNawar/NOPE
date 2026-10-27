using NUnit.Framework;

/// <summary>
/// The portal rings' vortex (Saleh's demo 1018a, PortalVortex): the effect's
/// quad sits on its opening in the drawing's local space, maps onto its ring's
/// crop of the mask atlas, and the iris, flare and swirl behave (one step, or
/// still, with reduced motion).
/// </summary>
public class PortalVortexTests
{
    // The anime hall's drawing: 2172 x 724 pixels, pivot at its centre, 100 pixels a unit.
    private const float W = 2172f, H = 724f;

    [Test]
    public void LocalQuad_CentresOnTheOpeningInTheDrawingsUnits()
    {
        // The front ring's opening, centred at canvas (1096, 488), radii 48 x 55.
        PortalVortex.Box q = PortalVortex.LocalQuad(1096f, 488f, 48f, 55f, W, H, W / 2f, H / 2f, 100f);
        Assert.AreEqual((1096f - 1086f) / 100f, q.CentreX, 1e-4f);
        Assert.AreEqual((362f - 488f) / 100f, q.CentreY, 1e-4f, "canvas rows run down, local y up");
        Assert.AreEqual(2f * PortalVortex.Margin * 48f / 100f, q.W, 1e-4f);
        Assert.AreEqual(2f * PortalVortex.Margin * 55f / 100f, q.H, 1e-4f);
    }

    [Test]
    public void LocalQuad_ReadsThePivot()
    {
        PortalVortex.Box centred = PortalVortex.LocalQuad(100f, 100f, 10f, 10f, W, H, W / 2f, H / 2f, 100f);
        PortalVortex.Box corner = PortalVortex.LocalQuad(100f, 100f, 10f, 10f, W, H, 0f, 0f, 100f);
        Assert.AreEqual(centred.CentreX + W / 200f, corner.CentreX, 1e-4f);
        Assert.AreEqual(centred.CentreY + H / 200f, corner.CentreY, 1e-4f);
    }

    [Test]
    public void MaskMap_PutsTheQuadOnItsRingsCropOfTheAtlas()
    {
        // An opening at canvas (100, 50), radii 20 x 10; its crop is canvas (70, 30) 60 x 40, at atlas (0.25, 0.5) 0.5 x 0.25.
        var crop = new PortalVortex.Box(70f, 30f, 60f, 40f);
        var atlas = new PortalVortex.Box(0.25f, 0.5f, 0.5f, 0.25f);
        PortalVortex.Box map = PortalVortex.MaskMap(100f, 50f, 20f, 10f, crop, atlas);

        // A canvas point's place in the atlas: its share across the crop, y flipped (canvas rows run down, the atlas's v up).
        System.Func<float, float, (float u, float v)> atlasOf = (x, y) =>
            (atlas.X + atlas.W * (x - crop.X) / crop.W, atlas.Y + atlas.H * (1f - (y - crop.Y) / crop.H));
        System.Func<float, float, (float u, float v)> mapped = (u, v) => (u * map.W + map.X, v * map.H + map.Y);

        var centre = atlasOf(100f, 50f);
        Assert.AreEqual(centre.u, mapped(0.5f, 0.5f).u, 1e-5f, "the quad's middle is the opening's centre");
        Assert.AreEqual(centre.v, mapped(0.5f, 0.5f).v, 1e-5f);
        var topLeft = atlasOf(100f - PortalVortex.Margin * 20f, 50f - PortalVortex.Margin * 10f);
        Assert.AreEqual(topLeft.u, mapped(0f, 1f).u, 1e-5f, "the quad's top-left corner is Margin radii up and left");
        Assert.AreEqual(topLeft.v, mapped(0f, 1f).v, 1e-5f);
        var bottomRight = atlasOf(100f + PortalVortex.Margin * 20f, 50f + PortalVortex.Margin * 10f);
        Assert.AreEqual(bottomRight.u, mapped(1f, 0f).u, 1e-5f);
        Assert.AreEqual(bottomRight.v, mapped(1f, 0f).v, 1e-5f);
    }

    [Test]
    public void Reveal_OpensAndShutsOverItsTime()
    {
        Assert.AreEqual(0.5f, PortalVortex.Reveal(0f, true, 0.3f, 0.6f, false), 1e-5f);
        Assert.AreEqual(1f, PortalVortex.Reveal(0.9f, true, 0.3f, 0.6f, false), "never past open");
        Assert.AreEqual(0.5f, PortalVortex.Reveal(1f, false, 0.3f, 0.6f, false), 1e-5f);
        Assert.AreEqual(0f, PortalVortex.Reveal(0.1f, false, 0.3f, 0.6f, false), "never past shut");
        Assert.AreEqual(1f, PortalVortex.Reveal(0f, true, 0.01f, 0.6f, true), "reduced motion: at once");
        Assert.AreEqual(0f, PortalVortex.Reveal(1f, false, 0f, 0f, false), "no time: at once");
    }

    [Test]
    public void Iris_SpringsOpenPastItsRimAndSettles()
    {
        Assert.AreEqual(0f, PortalVortex.Iris(0f));
        Assert.AreEqual(1f, PortalVortex.Iris(1f));
        Assert.Greater(PortalVortex.Iris(0.8f), 1f, "it overshoots the rim");
        Assert.Greater(PortalVortex.Iris(0.3f), 0.3f, "it opens fast");
        Assert.AreEqual(0f, PortalVortex.Iris(-1f));
    }

    [Test]
    public void Pulse_FlaresUpAndDown_OneStepWithReducedMotion()
    {
        Assert.AreEqual(0f, PortalVortex.Pulse(0f, false));
        Assert.AreEqual(1f, PortalVortex.Pulse(0.5f, false), 1e-5f);
        Assert.AreEqual(0f, PortalVortex.Pulse(1f, false));
        Assert.Greater(PortalVortex.Pulse(0.25f, false), 0.5f);
        Assert.AreEqual(1f, PortalVortex.Pulse(0.1f, true), "reduced motion: one step up");
        Assert.AreEqual(1f, PortalVortex.Pulse(0.9f, true));
        Assert.AreEqual(0f, PortalVortex.Pulse(1f, true), "and down at its end");
    }

    [Test]
    public void Turn_TurnsAndWraps_StillWithReducedMotion()
    {
        Assert.AreEqual((float)(System.Math.PI / 2.0), PortalVortex.Turn(0f, 90f, 1f, false), 1e-5f);
        Assert.AreEqual(0.5f, PortalVortex.Turn(0.5f, 90f, 1f, true), "reduced motion holds the swirl");
        float a = PortalVortex.Turn(6f, 90f, 1f, false);
        Assert.That(a, Is.InRange(0f, (float)(2.0 * System.Math.PI)));
        Assert.Greater(PortalVortex.Turn(1f, -90f, 0.5f, false), 0f, "a reversed swirl wraps too");
    }
}
