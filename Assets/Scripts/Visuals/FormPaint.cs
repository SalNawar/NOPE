using System;
using System.Collections.Generic;

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
/// section band, rule and barcode bar, each checkbox's outline and tick and
/// the stamp area's dash, as coloured rectangles in form space. Both renderers
/// draw these quads: the desk paper (DeskDocument, one mesh per layer) and the
/// PC (FormView, one graphic per layer), so the paper and its scanned copy
/// print the same strokes. Texts, the seal and the photo are the renderers'.
/// Pure and engine-free.
/// </summary>
public static class FormPaint
{
    /// <summary>The share of a dashed edge that is ink (the stamp area).</summary>
    public const float DashShare = 0.55f;

    /// <summary>A dash's length, in rule widths.</summary>
    public const float DashRules = 4f;

    /// <summary>How far a tick sits inside its checkbox, as a share of the box's width.</summary>
    public const float TickInset = 0.22f;

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

        float rule = (m ?? new FormMetrics()).ruleWidth * form.PageHeight;
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
                    Dashed(quads, item.Rect, rule, rule * DashRules, palette.StampDash);
                    break;
            }
        }
        return quads;
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
