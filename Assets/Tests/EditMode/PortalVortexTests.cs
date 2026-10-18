using NUnit.Framework;

/// <summary>
/// The portal rings' vortex (Saleh's demo 1018a, PortalVortex): the effect's
/// quad sits on its opening in the drawing's local space and texture, the
/// glass fence's row maps across the quad, and the iris, flare and swirl
/// behave (one step, or still, with reduced motion).
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
    public void TextureQuad_CoversTheSamePixels()
    {
        PortalVortex.Box t = PortalVortex.TextureQuad(1096f, 488f, 48f, 55f, W, H);
        Assert.AreEqual(1096f / W, t.CentreX, 1e-5f);
        Assert.AreEqual(1f - 488f / H, t.CentreY, 1e-5f);
        Assert.AreEqual(2f * PortalVortex.Margin * 48f / W, t.W, 1e-5f);
        Assert.AreEqual(2f * PortalVortex.Margin * 55f / H, t.H, 1e-5f);
    }

    [Test]
    public void FenceV_MapsTheRailsRowAcrossTheQuad()
    {
        Assert.AreEqual(0.5f, PortalVortex.FenceV(364f, 22f, 364f), 1e-5f, "a rail through the centre halves the quad");
        Assert.Greater(PortalVortex.FenceV(488f, 55f, 478f), 0.5f, "a rail above the centre is above the middle");
        Assert.AreEqual(0f, PortalVortex.FenceV(488f, 55f, 0f), "no fence");
        Assert.AreEqual(1f, PortalVortex.FenceV(488f, 55f, 10f), "a rail above the quad: all of it behind the glass");
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
