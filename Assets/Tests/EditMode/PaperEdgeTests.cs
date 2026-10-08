using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// The desk's papers have depth and never meet the desk or each other
/// (Saleh's 1007d playtest: "too flat when you see them from the side",
/// "sometimes they clip or render with the table"): PaperEdge's bands and
/// walls, PaperLayers' stack lifts by thickness and the tilt's dip.
/// </summary>
public class PaperEdgeTests
{
    private const float Eps = 1e-6f;

    [Test]
    public void Bands_RunFromUnderTheFace_DownTheWholeThickness_Contiguous()
    {
        foreach (PaperKind kind in new[] { PaperKind.Sheet, PaperKind.Card, PaperKind.Booklet, PaperKind.Folder })
        {
            List<EdgeBand> bands = PaperEdge.Bands(kind, 0.003f);
            Assert.AreEqual(PaperEdge.TopGap, bands[0].Top, Eps, $"{kind}: the edge starts under the face, clear of it");
            Assert.AreEqual(0.003f, bands[bands.Count - 1].Bottom, Eps, $"{kind}: the edge reaches the paper's whole thickness");
            for (int i = 1; i < bands.Count; i++)
                Assert.AreEqual(bands[i - 1].Bottom, bands[i].Top, Eps, $"{kind}: no gap between bands");
            foreach (EdgeBand b in bands)
                Assert.Greater(b.Bottom, b.Top, $"{kind}: every band has depth");
        }
    }

    [Test]
    public void ABooklet_ShowsItsCoverTopAndBottom_AndItsPages_AFolderItsPagesOverItsBoard()
    {
        List<EdgeBand> booklet = PaperEdge.Bands(PaperKind.Booklet, 0.003f);
        Assert.AreEqual(EdgeTone.Cover, booklet[0].Tone);
        Assert.AreEqual(EdgeTone.Cover, booklet[booklet.Count - 1].Tone);
        Assert.AreEqual(PaperEdge.PageBands, booklet.FindAll(b => b.Tone == EdgeTone.PageLight || b.Tone == EdgeTone.PageDark).Count);
        List<EdgeBand> folder = PaperEdge.Bands(PaperKind.Folder, 0.0025f);
        Assert.AreNotEqual(EdgeTone.Cover, folder[0].Tone, "a folder's pages lie on top");
        Assert.AreEqual(EdgeTone.Cover, folder[folder.Count - 1].Tone, "its board under them");
        Assert.AreEqual(1, PaperEdge.Bands(PaperKind.Sheet, 0.0007f).Count);
        Assert.AreEqual(PaperKind.Booklet, PaperEdge.KindOf(FormFrame.Booklet));
        Assert.AreEqual(PaperKind.Card, PaperEdge.KindOf(FormFrame.Card));
        Assert.AreEqual(PaperKind.Sheet, PaperEdge.KindOf(FormFrame.TopBand));
    }

    [Test]
    public void Walls_AreAQuadPerSegmentPerBand_WithinTheBand()
    {
        var vertices = new List<(float x, float y, float z)>();
        var tones = new List<EdgeTone>();
        var triangles = new List<int>();
        List<EdgeBand> bands = PaperEdge.Bands(PaperKind.Booklet, 0.003f);
        PaperEdge.Walls(PaperEdge.Rectangle(1f, 1f), bands, vertices, tones, triangles);
        Assert.AreEqual(4 * 4 * bands.Count, vertices.Count, "four corners per side per band");
        Assert.AreEqual(vertices.Count, tones.Count);
        Assert.AreEqual(12 * 4 * bands.Count, triangles.Count, "two triangles each way per side per band");
        foreach ((float x, float y, float z) v in vertices)
            Assert.IsTrue(v.z >= PaperEdge.TopGap - Eps && v.z <= 0.003f + Eps, "every corner within the edge's depth");
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
