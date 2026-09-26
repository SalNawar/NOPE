using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// The strokes a placed form prints (redesign phase 5, PC spec FO1, FO8):
/// FormPaint turns boxes, bands, rules, bars, checkboxes and the stamp area
/// into coloured quads in form space, the ones the desk paper and the PC's
/// scanned copy both draw. Fills go under the slots' tints, lines over them.
/// </summary>
public class FormPaintTests
{
    private const float Eps = 1e-4f;

    private static readonly Rgba InkC = new Rgba(0.1f, 0.1f, 0.1f);
    private static readonly Rgba RuleC = new Rgba(0.3f, 0.3f, 0.3f);
    private static readonly Rgba FillC = new Rgba(0.98f, 0.97f, 0.9f);
    private static readonly Rgba BandC = new Rgba(0.87f, 0.84f, 0.76f);
    private static readonly Rgba DashC = new Rgba(0.4f, 0.4f, 0.35f);

    private static FormPalette Palette() => new FormPalette { Ink = InkC, Rule = RuleC, BoxFill = FillC, Band = BandC, StampDash = DashC };

    /// <summary>A page 100 wide with an H of 100 and a rule of 0.01 H (1 unit).</summary>
    private static readonly FormMetrics M = new FormMetrics { ruleWidth = 0.01f };

    private static PlacedForm Form(params FormItem[] items) =>
        new PlacedForm(100f, 100f, 100f, items, new List<FormSlot>(), new List<float> { 0f });

    private static FormItem Item(FormItemKind kind, FaceRect rect, string text = "") =>
        new FormItem(kind, FormTextRole.Paragraph, rect, -1, text, 0f, FormTextAlign.Left);

    private static bool Inside(FaceRect inner, FaceRect outer) =>
        inner.XMin >= outer.XMin - Eps && inner.XMax <= outer.XMax + Eps && inner.YMin >= outer.YMin - Eps && inner.YMax <= outer.YMax + Eps;

    private static float Area(IEnumerable<FormQuad> quads) => quads.Sum(q => q.Rect.Width * q.Rect.Height);

    [Test]
    public void ABox_IsItsFillUnderTheTints_AndAnOutlineOverThem_InsideItsEdges_NeverOverlapping()
    {
        var box = FaceRect.FromTop(10f, 20f, 40f, 30f);
        List<FormQuad> quads = FormPaint.Quads(Form(Item(FormItemKind.Box, box)), Palette(), M);

        FormQuad fill = quads.Single(q => q.Layer == FormPaintLayer.Fill);
        Assert.AreEqual(box, fill.Rect);
        Assert.AreEqual(FillC, fill.Colour);

        List<FormQuad> outline = quads.Where(q => q.Layer == FormPaintLayer.Line).ToList();
        Assert.AreEqual(4, outline.Count);
        Assert.IsTrue(outline.All(q => q.Colour.Equals(RuleC) && Inside(q.Rect, box)));
        float perimeterBand = 2f * 40f * 1f + 2f * (30f - 2f) * 1f;
        Assert.AreEqual(perimeterBand, Area(outline), Eps, "the four edges tile the border once (no corner drawn twice)");
    }

    [Test]
    public void BandsFill_RulesAndBarsLine_InTheirColours()
    {
        List<FormQuad> quads = FormPaint.Quads(Form(
            Item(FormItemKind.RowBand, FaceRect.FromTop(0f, 0f, 100f, 5f)),
            Item(FormItemKind.Rule, FaceRect.FromTop(0f, 10f, 100f, 1f)),
            Item(FormItemKind.Bar, FaceRect.FromTop(5f, 20f, 2f, 8f))), Palette(), M);

        Assert.AreEqual(3, quads.Count);
        Assert.AreEqual((FormPaintLayer.Fill, BandC), (quads[0].Layer, quads[0].Colour));
        Assert.AreEqual((FormPaintLayer.Line, RuleC), (quads[1].Layer, quads[1].Colour));
        Assert.AreEqual((FormPaintLayer.Line, InkC), (quads[2].Layer, quads[2].Colour));
    }

    [Test]
    public void ACheckbox_IsAnOutline_AndATickOnlyWhenTicked()
    {
        var mark = FaceRect.FromTop(0f, 0f, 10f, 10f);
        List<FormQuad> empty = FormPaint.Quads(Form(Item(FormItemKind.Checkbox, mark)), Palette(), M);
        List<FormQuad> ticked = FormPaint.Quads(Form(Item(FormItemKind.Checkbox, mark, FormLayout.Tick)), Palette(), M);

        Assert.AreEqual(4, empty.Count);
        Assert.AreEqual(5, ticked.Count);
        FormQuad tick = ticked.Last();
        Assert.AreEqual(InkC, tick.Colour);
        Assert.AreEqual(FaceRect.FromTop(2.2f, 2.2f, 5.6f, 5.6f).XMin, tick.Rect.XMin, Eps);
        Assert.AreEqual(5.6f, tick.Rect.Width, Eps);
    }

    [Test]
    public void TheStampArea_IsDashed_InWholeDashesAlongEachEdge_InsideIt()
    {
        var stamp = FaceRect.FromTop(50f, 60f, 40f, 12f);
        List<FormQuad> dashes = FormPaint.Quads(Form(Item(FormItemKind.StampArea, stamp)), Palette(), M);

        Assert.IsTrue(dashes.All(q => q.Layer == FormPaintLayer.Line && q.Colour.Equals(DashC) && Inside(q.Rect, stamp)));
        int along = (int)System.Math.Round(40f / (1f * FormPaint.DashRules)), across = (int)System.Math.Round(12f / (1f * FormPaint.DashRules));
        Assert.AreEqual(2 * along + 2 * across, dashes.Count);
        float inked = 2f * 40f * FormPaint.DashShare + 2f * 12f * FormPaint.DashShare;
        Assert.AreEqual(inked, Area(dashes), 1e-3f, "each edge is inked at the dash share");
    }

    [Test]
    public void TextsTheSealAndThePhoto_AreTheRenderers_NotQuads()
    {
        List<FormQuad> quads = FormPaint.Quads(Form(
            Item(FormItemKind.Text, FaceRect.FromTop(0f, 0f, 10f, 5f), "Hello"),
            Item(FormItemKind.Seal, FaceRect.FromTop(0f, 0f, 10f, 10f)),
            Item(FormItemKind.Photo, FaceRect.FromTop(0f, 0f, 8f, 10f))), Palette(), M);
        CollectionAssert.IsEmpty(quads);
    }

    [Test]
    public void TheRuleWidth_IsTheMetricsInPageHeights()
    {
        var box = FaceRect.FromTop(0f, 0f, 50f, 50f);
        var form = new PlacedForm(50f, 200f, 200f, new[] { Item(FormItemKind.Box, box) }, new List<FormSlot>(), new List<float> { 0f });
        FormQuad top = FormPaint.Quads(form, Palette(), M).First(q => q.Layer == FormPaintLayer.Line);
        Assert.AreEqual(2f, top.Rect.Height, Eps, "0.01 H of a 200-unit page");
    }

    [Test]
    public void NoForm_NoQuads()
    {
        CollectionAssert.IsEmpty(FormPaint.Quads(null, Palette(), M));
        CollectionAssert.IsEmpty(FormPaint.Quads(Form(Item(FormItemKind.Box, FaceRect.FromTop(0f, 0f, 5f, 5f))), null, M));
    }
}
