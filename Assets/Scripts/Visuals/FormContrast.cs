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

    /// <summary>The Analysis Scanner's mark: the dashed outline on a contradicting pair's boxes on the PC (the PC redesign SC4).</summary>
    public Rgba Analysis;

    /// <summary>A form's frame bands (FormLook.accent; the document design spec, D1): no text is printed on them.</summary>
    public Rgba Accent;

    /// <summary>A copy (a look's palette starts from the style's, FormLook.Palette).</summary>
    public FormPalette Copy() => (FormPalette)MemberwiseClone();
}

/// <summary>
/// The form style's contrast pairs (PC spec FO7, §6.4), checked once by Build
/// Office UI: forms are diegetic and never themed, so one check covers every
/// culture. Texts need the body-text minimum (fine print and labels too: they
/// are small); rules, the stamp dash and the analysis mark the outline minimum; each fill laid
/// over a box (the hover tint, every theme's pick highlight) is composited
/// over the box fill and must keep the ink readable. The scanner backing's
/// strip text is checked on the backing (the PC's scanned copy, phase 5).
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
        Pair("analysis mark on a box", p.Analysis, p.BoxFill, ContrastClass.Glyph);
        Pair("analysis mark on the paper", p.Analysis, p.Paper, ContrastClass.Glyph);
        Pair("scan strip text on the scanner backing", p.BackingInk, p.Backing, ContrastClass.Text);
        foreach ((string name, Rgba fill) in overlays ?? new List<(string, Rgba)>())
            Pair($"ink on {name} over a box", p.Ink, Contrast.Over(fill, p.BoxFill.WithAlpha(1f)), ContrastClass.Text);
        return problems;
    }

    /// <summary>
    /// Each seal ink (the document design spec, D4: a legend is text) on a
    /// form's paper, below the body-text minimum; one message each (empty when
    /// every ink reads).
    /// </summary>
    public static List<string> SealProblems(FormPalette p, IEnumerable<(string name, Rgba ink)> inks, ContrastRules rules)
    {
        var problems = new List<string>();
        float min = rules.Min(ContrastClass.Text);
        foreach ((string name, Rgba ink) in inks ?? new List<(string, Rgba)>())
        {
            double ratio = Contrast.Ratio(ink.WithAlpha(1f), p.Paper.WithAlpha(1f));
            if (ratio + 1e-6 < min)
                problems.Add($"Form seal ink {name} on the paper is {F(ratio)}:1; it needs {F(min)}:1.");
        }
        return problems;
    }

    /// <summary>A ratio for messages: one decimal, rounded down.</summary>
    private static string F(double v) => (System.Math.Floor(v * 10.0) / 10.0).ToString("0.0", CultureInfo.InvariantCulture);
}
