using System.Collections.Generic;
using System.Globalization;

/// <summary>A form style's colours (FormStyleSO's, as Rgba), for the contrast check.</summary>
public sealed class FormPalette
{
    /// <summary>The paper.</summary>
    public Rgba Paper;

    /// <summary>Values, titles, section heads, cells, the barcode.</summary>
    public Rgba Ink;

    /// <summary>Box labels, the programme line, captions.</summary>
    public Rgba LabelInk;

    /// <summary>Fine print and the serial.</summary>
    public Rgba FinePrintInk;

    /// <summary>Box outlines and rules.</summary>
    public Rgba Rule;

    /// <summary>A box's fill.</summary>
    public Rgba BoxFill;

    /// <summary>A section head's band and a table's head row.</summary>
    public Rgba Band;

    /// <summary>The stamp area's dash.</summary>
    public Rgba StampDash;

    /// <summary>The scanner's dark backing behind a scanned copy on the PC.</summary>
    public Rgba Backing;

    /// <summary>The scan strip's text on the backing ("SCANNED 10:42 · DESK SCANNER 1").</summary>
    public Rgba BackingInk;

    /// <summary>Search's found outline around what a result opened (redesign phase 19).</summary>
    public Rgba Found;
}

/// <summary>
/// The form style's contrast pairs (PC spec FO7, §6.4), checked once by Build
/// Office UI: forms are diegetic and never themed, so one check covers every
/// culture. Texts need the body-text minimum (fine print and labels too: they
/// are small); rules and the stamp dash the outline minimum; each fill laid
/// over a box (the hover tint, every theme's pick highlight) is composited
/// over the box fill and must keep the ink readable. The scanner backing's
/// strip text is checked on the backing (the PC's scanned copy, phase 5);
/// search's found outline on the paper and a box as an outline (phase 19).
/// </summary>
public static class FormContrast
{
    /// <summary>Every pair below its minimum, one message each (empty when the style passes).</summary>
    public static List<string> Problems(FormPalette p, IReadOnlyList<(string name, Rgba fill)> overlays, ContrastRules rules)
    {
        var problems = new List<string>();
        void Pair(string what, Rgba ink, Rgba fill, ContrastClass cls)
        {
            float min = rules.Min(cls);
            Rgba backdrop = fill.WithAlpha(1f);
            double ratio = Contrast.Ratio(Contrast.Over(ink, backdrop), backdrop);
            if (ratio + 1e-6 < min)
                problems.Add($"Form {what} is {F(ratio)}:1; it needs {F(min)}:1.");
        }

        Pair("ink on the paper", p.Ink, p.Paper, ContrastClass.Text);
        Pair("ink on a box", p.Ink, p.BoxFill, ContrastClass.Text);
        Pair("label ink on the paper", p.LabelInk, p.Paper, ContrastClass.Text);
        Pair("label ink on a box", p.LabelInk, p.BoxFill, ContrastClass.Text);
        Pair("ink on a section band", p.Ink, p.Band, ContrastClass.Text);
        Pair("fine print on the paper", p.FinePrintInk, p.Paper, ContrastClass.Text);
        Pair("rule on the paper", p.Rule, p.Paper, ContrastClass.Glyph);
        Pair("rule on a box", p.Rule, p.BoxFill, ContrastClass.Glyph);
        Pair("stamp dash on the paper", p.StampDash, p.Paper, ContrastClass.Glyph);
        Pair("scan strip text on the scanner backing", p.BackingInk, p.Backing, ContrastClass.Text);
        Pair("found outline on the paper", p.Found, p.Paper, ContrastClass.Glyph);
        Pair("found outline on a box", p.Found, p.BoxFill, ContrastClass.Glyph);
        foreach ((string name, Rgba fill) in overlays ?? new List<(string, Rgba)>())
            Pair($"ink on {name} over a box", p.Ink, Contrast.Over(fill, p.BoxFill.WithAlpha(1f)), ContrastClass.Text);
        return problems;
    }

    /// <summary>A ratio for messages: one decimal, rounded down.</summary>
    private static string F(double v) => (System.Math.Floor(v * 10.0) / 10.0).ToString("0.0", CultureInfo.InvariantCulture);
}
