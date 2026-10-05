using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Which printed layer a quad goes in: under the slots' hover and pick tints, or over them.</summary>
public enum FormPaintLayer
{
    /// <summary>Box fills and section bands, under the tints.</summary>
    Fill,

    /// <summary>Box outlines, rules, barcode bars, checkbox outlines and ticks and the stamp area's dash, over the tints.</summary>
    Line
}

/// <summary>One coloured rectangle a form prints, in form space (top-left origin, y down).</summary>
public readonly struct FormQuad
{
    /// <summary>A quad from its parts.</summary>
    public FormQuad(FaceRect rect, Rgba colour, FormPaintLayer layer)
    {
        Rect = rect;
        Colour = colour;
        Layer = layer;
    }

    /// <summary>Where it is.</summary>
    public FaceRect Rect { get; }

    /// <summary>Its colour.</summary>
    public Rgba Colour { get; }

    /// <summary>The layer it prints in.</summary>
    public FormPaintLayer Layer { get; }
}

/// <summary>
/// The lines and fills a placed form prints (redesign phase 5, PC spec FO1,
/// FO8: the code draws the lines): each box's fill and its outline, each
/// section band, rule and barcode bar, each checkbox's outline and tick, the
/// stamp area's dash, and a look's frame bands (in its accent) and a ticket's
/// perforation (in the rule colour; the document design spec, D1), a
/// booklet's cover edge (in the holder's nation's colour) and spine, a
/// folded card's crease (a faint shade and highlight over the boxes, as a
/// fold shows through print) and a card's chip (the travel documents spec, TD1), as
/// coloured rectangles in form space. Both renderers
/// draw these quads: the desk paper (DeskDocument, one mesh per layer) and the
/// PC (FormView, one graphic per layer), so the paper and its scanned copy
/// print the same strokes. The PC alone adds the Analysis Scanner's marks
/// (MarkQuads, the PC redesign SC4, SC5: the desk paper is a physical
/// object). Texts, the seal and the photo are the renderers'.
/// Pure and engine-free.
/// </summary>
public static class FormPaint
{
    /// <summary>The share of a dashed edge that is ink (the stamp area).</summary>
    public const float DashShare = 0.55f;

    /// <summary>A dash's length, in rule widths.</summary>
    public const float DashRules = 4f;

    /// <summary>How many rule widths a passport's visa box is drawn in (FormLayout.VisaBox: the one box a verdict stamp takes, clearly drawn; Saleh 2026-10-06).</summary>
    public const float VisaRules = 2.5f;

    /// <summary>How far a tick sits inside its checkbox, as a share of the box's width.</summary>
    public const float TickInset = 0.22f;

    /// <summary>An analysis mark's width, in rule widths: the dashed outline reads over the box's own outline.</summary>
    public const float MarkRules = 2f;

    /// <summary>A spine's shadow and a crease's, as the rule colour's alpha over the paper.</summary>
    public const float FoldShade = 0.22f;

    /// <summary>A spine's stitches: a dash's length in rule widths (the stitch line is drawn in the rule colour).</summary>
    public const float StitchRules = 6f;

    /// <summary>A card's chip: its gold and its contacts' lines.</summary>
    public static readonly Rgba ChipGold = new Rgba(0.76f, 0.58f, 0.2f), ChipLine = new Rgba(0.4f, 0.28f, 0.06f);

    /// <summary>A crease's highlight beside its shadow: white at this alpha.</summary>
    public const float CreaseLight = 0.5f;

    /// <summary>
    /// The quads <paramref name="form"/> prints in <paramref name="palette"/>'s
    /// colours, in drawing order; the rule width is <paramref name="m"/>'s, in
    /// the form's page heights. An outline lies inside its box's edges.
    /// </summary>
    public static List<FormQuad> Quads(PlacedForm form, FormPalette palette, FormMetrics m)
    {
        var quads = new List<FormQuad>();
        if (form == null || palette == null)
            return quads;

        float rule = (m ?? new FormMetrics()).ruleWidth * form.Unit;
        foreach (FormItem item in form.Items)
        {
            switch (item.Kind)
            {
                case FormItemKind.Box:
                    Add(quads, item.Rect, palette.BoxFill, FormPaintLayer.Fill);
                    Outline(quads, item.Rect, rule, palette.Rule);
                    break;
                case FormItemKind.RowBand:
                    Add(quads, item.Rect, palette.Band, FormPaintLayer.Fill);
                    break;
                case FormItemKind.Rule:
                    Add(quads, item.Rect, palette.Rule, FormPaintLayer.Line);
                    break;
                case FormItemKind.Bar:
                    Add(quads, item.Rect, palette.Ink, FormPaintLayer.Line);
                    break;
                case FormItemKind.Checkbox:
                    Outline(quads, item.Rect, rule, palette.Rule);
                    if (item.Text == FormLayout.Tick)
                        Add(quads, Inset(item.Rect, item.Rect.Width * TickInset), palette.Ink, FormPaintLayer.Line);
                    break;
                case FormItemKind.StampArea:
                    Dashed(quads, item.Rect, item.Text == FormLayout.VisaBox ? rule * VisaRules : rule, rule * DashRules, palette.StampDash);
                    break;
                case FormItemKind.Stripe:
                    Add(quads, item.Rect, palette.Accent, FormPaintLayer.Fill);
                    break;
                case FormItemKind.Perforation:
                    Add(quads, item.Rect, palette.Rule, FormPaintLayer.Line);
                    break;
                case FormItemKind.Cover:
                    Add(quads, item.Rect, Rgba.TryParseHex(item.Text, out Rgba cover) ? cover.WithAlpha(1f) : palette.Accent, FormPaintLayer.Fill);
                    break;
                case FormItemKind.Spine:
                    Add(quads, item.Rect, palette.Rule.WithAlpha(FoldShade), FormPaintLayer.Fill);
                    Edge(quads, item.Rect.XMin, item.Rect.CentreY - rule / 2f, item.Rect.Width, true, rule, rule * StitchRules, palette.Rule);
                    break;
                case FormItemKind.Crease:
                    Add(quads, FaceRect.FromTop(item.Rect.XMin, item.Rect.YMin, item.Rect.Width / 2f, item.Rect.Height), palette.Rule.WithAlpha(FoldShade), FormPaintLayer.Line);
                    Add(quads, FaceRect.FromTop(item.Rect.CentreX, item.Rect.YMin, item.Rect.Width / 2f, item.Rect.Height), new Rgba(1f, 1f, 1f, CreaseLight), FormPaintLayer.Line);
                    break;
                case FormItemKind.Chip:
                    Chip(quads, item.Rect, rule);
                    break;
            }
        }
        return quads;
    }

    /// <summary>
    /// The Analysis Scanner's marks (the PC redesign SC4): a dashed outline in
    /// <paramref name="colour"/> (the form style's analysis colour) over the box
    /// of each slot of <paramref name="form"/> whose field is in
    /// <paramref name="fields"/> (a table row is never marked), MarkRules rule
    /// widths thick in whole dashes like the stamp area's, in the Line layer
    /// over the form's own strokes. None without a form or fields.
    /// </summary>
    public static List<FormQuad> MarkQuads(PlacedForm form, IReadOnlyCollection<int> fields, Rgba colour, FormMetrics m)
    {
        var quads = new List<FormQuad>();
        if (form == null || fields == null || fields.Count == 0)
            return quads;

        float width = (m ?? new FormMetrics()).ruleWidth * form.Unit * MarkRules;
        foreach (FormSlot slot in form.Slots)
            if (slot.Field >= 0 && fields.Contains(slot.Field))
                Dashed(quads, slot.Hit, width, width * DashRules, colour);
        return quads;
    }

    /// <summary>A card's chip: its gold plate, outlined, and its contacts (a line across its middle and two down it, at its thirds), in the Line layer over the card's fills.</summary>
    private static void Chip(List<FormQuad> quads, FaceRect r, float rule)
    {
        Add(quads, r, ChipGold, FormPaintLayer.Line);
        Outline(quads, r, rule, ChipLine);
        Add(quads, FaceRect.FromTop(r.XMin, r.CentreY - rule / 2f, r.Width, rule), ChipLine, FormPaintLayer.Line);
        Add(quads, FaceRect.FromTop(r.XMin + r.Width / 3f - rule / 2f, r.YMin, rule, r.Height), ChipLine, FormPaintLayer.Line);
        Add(quads, FaceRect.FromTop(r.XMin + 2f * r.Width / 3f - rule / 2f, r.YMin, rule, r.Height), ChipLine, FormPaintLayer.Line);
    }

    /// <summary>A quad, unless it has no area.</summary>
    private static void Add(List<FormQuad> quads, FaceRect r, Rgba colour, FormPaintLayer layer)
    {
        if (r.Width > 0f && r.Height > 0f)
            quads.Add(new FormQuad(r, colour, layer));
    }

    /// <summary>A rectangle's outline, <paramref name="width"/> thick, inside its edges: the top and bottom edges whole, the sides between them.</summary>
    private static void Outline(List<FormQuad> quads, FaceRect r, float width, Rgba colour)
    {
        Add(quads, FaceRect.FromTop(r.XMin, r.YMin, r.Width, width), colour, FormPaintLayer.Line);
        Add(quads, FaceRect.FromTop(r.XMin, r.YMax - width, r.Width, width), colour, FormPaintLayer.Line);
        Add(quads, FaceRect.FromTop(r.XMin, r.YMin + width, width, r.Height - 2f * width), colour, FormPaintLayer.Line);
        Add(quads, FaceRect.FromTop(r.XMax - width, r.YMin + width, width, r.Height - 2f * width), colour, FormPaintLayer.Line);
    }

    /// <summary>A dashed outline: each edge in whole dashes about <paramref name="dash"/> long, <see cref="DashShare"/> of each ink.</summary>
    private static void Dashed(List<FormQuad> quads, FaceRect r, float width, float dash, Rgba colour)
    {
        Edge(quads, r.XMin, r.YMin, r.Width, true, width, dash, colour);
        Edge(quads, r.XMin, r.YMax - width, r.Width, true, width, dash, colour);
        Edge(quads, r.XMin, r.YMin, r.Height, false, width, dash, colour);
        Edge(quads, r.XMax - width, r.YMin, r.Height, false, width, dash, colour);
    }

    private static void Edge(List<FormQuad> quads, float x, float y, float length, bool horizontal, float width, float dash, Rgba colour)
    {
        int count = Math.Max(1, (int)Math.Round(length / Math.Max(dash, 1e-6f)));
        float step = length / count, ink = step * DashShare;
        for (int i = 0; i < count; i++)
        {
            float at = i * step;
            Add(quads, horizontal ? FaceRect.FromTop(x + at, y, ink, width) : FaceRect.FromTop(x, y + at, width, ink), colour, FormPaintLayer.Line);
        }
    }

    /// <summary>A rectangle shrunk by <paramref name="by"/> on every side.</summary>
    private static FaceRect Inset(FaceRect r, float by) =>
        new FaceRect(r.XMin + by, r.YMin + by, Math.Max(r.XMin + by, r.XMax - by), Math.Max(r.YMin + by, r.YMax - by));
}
