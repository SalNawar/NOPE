using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// The desk's papers have depth and never meet the desk or each other
/// (Saleh's 1007d playtest: "too flat when you see them from the side",
/// "sometimes they clip or render with the table"): each paper's kind (its
/// model), PaperLayers' stack lifts by thickness and the tilt's dip.
/// </summary>
public class PaperEdgeTests
{
    private const float Eps = 1e-6f;

    [Test]
    public void EachPaperWearsItsKindsModel_ByItsFrame()
    {
        Assert.AreEqual(PaperKind.Booklet, PaperEdge.KindOf(FormFrame.Booklet));
        Assert.AreEqual(PaperKind.Card, PaperEdge.KindOf(FormFrame.Card));
        Assert.AreEqual(PaperKind.Sheet, PaperEdge.KindOf(FormFrame.TopBand));
        Assert.AreEqual(PaperKind.Sheet, PaperEdge.KindOf(FormFrame.Ticket));
        List<(float x, float y)> r = PaperEdge.Rectangle(2f, 1f);
        Assert.AreEqual((-1f, -0.5f), r[0], "counter-clockwise from the bottom left");
        Assert.AreEqual((1f, 0.5f), r[2]);
    }

    [Test]
    public void Lifts_KeepEveryBottomOverThePartsUnderIt_AndTheLowestOverTheDesk()
    {
        var thickness = new List<float> { 0.0025f, 0.0007f, 0.003f, 0.0009f, 0.0007f };
        float[] lifts = PaperLayers.Lifts(thickness, 0.0002f);
        Assert.AreEqual(thickness.Count, lifts.Length);
        Assert.GreaterOrEqual(lifts[0] - thickness[0], PaperLayers.DeskClearance - Eps, "the lowest's bottom clears the desk top");
        for (int i = 1; i < lifts.Length; i++)
        {
            Assert.GreaterOrEqual(lifts[i] - thickness[i], lifts[i - 1] + PaperLayers.PartsDepth + PaperLayers.StackGap - Eps,
                                  $"place {i}: its bottom lies over every part of the one under it");
            Assert.GreaterOrEqual(lifts[i] - lifts[i - 1], PaperLayers.StackStep(0.0002f) - Eps, "never closer than the step");
        }
        Assert.AreEqual(0, PaperLayers.Lifts(new List<float>(), 0.001f).Length);
        float[] thin = PaperLayers.Lifts(new List<float> { 0f, 0f }, 0.0015f);
        Assert.AreEqual(0.0015f, thin[0], Eps, "with no thickness, the configured step as before");
        Assert.AreEqual(0.003f, thin[1], Eps);
    }

    [Test]
    public void TiltDrop_IsTheLowestCornersDip()
    {
        Assert.AreEqual(0f, PaperLayers.TiltDrop(0.13f, 0.17f, 0f, 0f), Eps, "flat: no dip");
        Assert.AreEqual(0.17f * (float)System.Math.Sin(3.0 * System.Math.PI / 180.0), PaperLayers.TiltDrop(0.13f, 0.17f, 3f, 0f), 1e-5f);
        Assert.AreEqual(PaperLayers.TiltDrop(0.13f, 0.17f, 2f, 4f), PaperLayers.TiltDrop(0.13f, 0.17f, -2f, -4f), Eps, "either way down");
        Assert.Greater(PaperLayers.TiltDrop(0.13f, 0.17f, 1.75f, 0f), 0.004f, "a drop's flutter dips a paper's edge by millimetres: more than its stack lift, so it must be lifted");
    }
}
