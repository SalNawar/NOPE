using NUnit.Framework;

/// <summary>
/// The readout line on the painted chip reader (PaintedReadout; desk v3
/// round 12's S1, the night of 2026-10-28, seam D): it lies on the top face,
/// inside it, on its near third, and its click box stands over the line
/// above the body's padded box, so the line is clicked before the body.
/// </summary>
public class PaintedReadoutTests
{
    private static readonly PaintedReadout.Box Reader = new PaintedReadout.Box(0.79f, 1.01f, 1.08f, 1.16f, 0.27f, 0.43f);

    [Test]
    public void TheLine_LiesOnTheTopFace_OnItsNearThird_InsideIt()
    {
        (float x, float y, float z) = PaintedReadout.LinePosition(Reader);
        (float width, float height) = PaintedReadout.LineSize(Reader);
        Assert.AreEqual(Reader.MaxY + PaintedReadout.Lift, y, 1e-6f, "over the top face by the lift");
        Assert.AreEqual(Reader.CentreX, x, 1e-6f, "centred across");
        Assert.Less(z, Reader.CentreZ, "on the near half (toward the chair)");
        Assert.GreaterOrEqual(z - height / 2f, Reader.MinZ, "its near edge inside the face");
        Assert.LessOrEqual(z + height / 2f, Reader.MaxZ, "its far edge inside the face");
        Assert.LessOrEqual(width, Reader.Width, "no wider than the face");
        Assert.Greater(width, 0.8f * Reader.Width, "most of the face's width: a readable line");
    }

    [Test]
    public void TheClickBox_StandsOverTheLine_AboveTheBodysPaddedBox()
    {
        (float cx, float cy, float cz) = PaintedReadout.BoxCentre(Reader);
        (float sx, float sy, float sz) = PaintedReadout.BoxSize(Reader);
        (float lx, float ly, float lz) = PaintedReadout.LinePosition(Reader);
        (float width, float height) = PaintedReadout.LineSize(Reader);
        Assert.AreEqual(lx, cx, 1e-6f);
        Assert.AreEqual(lz, cz, 1e-6f);
        Assert.AreEqual(width, sx, 1e-6f, "the line's footprint");
        Assert.AreEqual(height, sz, 1e-6f);
        float top = cy + sy / 2f - Reader.MaxY;
        Assert.AreEqual(PaintedReadout.BoxTop, top, 1e-6f);
        Assert.Greater(top, DeskHomeMap.ClickPad, "its top over the body's padded box (DeskPainted.EnsureClickBox pads by ClickPad), so the ray meets the readout's box first");
        Assert.Less(cy - sy / 2f, ly, "and it reaches down to the line");
    }
}
