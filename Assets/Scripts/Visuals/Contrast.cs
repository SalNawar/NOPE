using System;
using System.Collections.Generic;
using System.Globalization;

/// <summary>How much contrast a themed text or mark needs against its fill (piece 6 U10).</summary>
public enum ContrastClass
{
    /// <summary>No text or mark: never checked.</summary>
    None,

    /// <summary>Body text: 4.5:1.</summary>
    Text,

    /// <summary>Large text (TMP size 24+, or 18+ bold): 3:1.</summary>
    LargeText,

    /// <summary>A UI glyph such as a window control's "X": 3:1.</summary>
    Glyph,

    /// <summary>An input field's placeholder: 3:1.</summary>
    Hint
}

/// <summary>The contrast minimums a theme must pass (authored in world_source.json ui.contrast).</summary>
[Serializable]
public sealed class ContrastRules
{
    /// <summary>Minimum for body text.</summary>
    public float text = 4.5f;

    /// <summary>Minimum for large text.</summary>
    public float largeText = 3f;

    /// <summary>Minimum for UI glyphs.</summary>
    public float glyph = 3f;

    /// <summary>Minimum for input placeholders.</summary>
    public float hint = 3f;

    /// <summary>What one of the two hover rings must reach against any background; the rings must be outline² apart.</summary>
    public float outline = 3f;

    /// <summary>The lowest fill alpha a text may sit on (fills are checked as opaque).</summary>
    public float minTextFillAlpha = 0.7f;

    /// <summary>The minimum for a class (0 for None).</summary>
    public float Min(ContrastClass c)
    {
        switch (c)
        {
            case ContrastClass.Text: return text;
            case ContrastClass.LargeText: return largeText;
            case ContrastClass.Glyph: return glyph;
            case ContrastClass.Hint: return hint;
            default: return 0f;
        }
    }
}

/// <summary>One themed ink on its fill, with the class that says how much contrast it needs.</summary>
public readonly struct ContrastPair
{
    /// <summary>Name used in problem messages (the role).</summary>
    public readonly string Name;

    /// <summary>The text or mark colour.</summary>
    public readonly Rgba Ink;

    /// <summary>The colour behind it.</summary>
    public readonly Rgba Fill;

    /// <summary>How much contrast it needs.</summary>
    public readonly ContrastClass Class;

    /// <summary>A pair.</summary>
    public ContrastPair(string name, Rgba ink, Rgba fill, ContrastClass cls)
    {
        Name = name;
        Ink = ink;
        Fill = fill;
        Class = cls;
    }
}

/// <summary>
/// WCAG 2.x contrast maths and the theme contrast check that Generate World
/// and the content validator run on every culture theme (piece 6 U10, R7).
/// </summary>
public static class Contrast
{
    /// <summary>WCAG relative luminance (sRGB linearised, 0.2126/0.7152/0.0722); alpha is ignored.</summary>
    public static double RelativeLuminance(Rgba c) =>
        0.2126 * Linear(c.R) + 0.7152 * Linear(c.G) + 0.0722 * Linear(c.B);

    /// <summary>The contrast ratio (L1 + 0.05) / (L2 + 0.05), lighter over darker: 1..21, symmetric.</summary>
    public static double Ratio(Rgba a, Rgba b)
    {
        double la = RelativeLuminance(a), lb = RelativeLuminance(b);
        return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
    }

    /// <summary>WCAG's large text: 18 pt, as pixels drawn on a 1920x1080 screen (1 pt = 4/3 px).</summary>
    public const float LargePixels = 24f;

    /// <summary>WCAG's large bold text: 14 pt bold, as pixels drawn on a 1920x1080 screen.</summary>
    public const float LargeBoldPixels = 18.66f;

    /// <summary>
    /// The class a text needs by its size (the readability fix): large text
    /// (3:1) from <see cref="LargePixels"/>, or <see cref="LargeBoldPixels"/>
    /// when bold, where <paramref name="pixels"/> is its font size as drawn on
    /// a 1920x1080 screen (the office overlay's reference size; the PC's
    /// desktop is drawn smaller than its own units); body text (4.5:1) below.
    /// </summary>
    public static ContrastClass ClassFor(float pixels, bool bold) =>
        pixels >= LargePixels || (bold && pixels >= LargeBoldPixels) ? ContrastClass.LargeText : ContrastClass.Text;

    /// <summary>
    /// The lowest contrast <paramref name="ink"/> can have on the layers drawn
    /// behind it (<paramref name="layersNearestFirst"/>, each composited by its
    /// alpha under the ones before it): once they are opaque, on what they
    /// make; while light still comes through them (a translucent strip, or no
    /// layer at all), what shows behind is unknown, so the worse of the stack
    /// over black and over white. A translucent ink is composited first.
    /// </summary>
    public static double WorstRatio(Rgba ink, IReadOnlyList<Rgba> layersNearestFirst)
    {
        float r = 0f, g = 0f, b = 0f, a = 0f;
        foreach (Rgba layer in layersNearestFirst ?? Array.Empty<Rgba>())
        {
            float k = (1f - a) * layer.A;
            r += layer.R * k;
            g += layer.G * k;
            b += layer.B * k;
            a += k;
            if (a >= OpaqueEnough)
                break;
        }

        double worst = double.MaxValue;
        foreach (float under in a >= OpaqueEnough ? new[] { -1f } : new[] { 0f, 1f })
        {
            Rgba backdrop = under < 0f
                ? new Rgba(r / a, g / a, b / a)
                : new Rgba(r + (1f - a) * under, g + (1f - a) * under, b + (1f - a) * under);
            worst = Math.Min(worst, Ratio(Over(ink, backdrop), backdrop));
        }
        return worst;
    }

    /// <summary>How opaque the layers behind a text must add up to for what shows behind them not to matter.</summary>
    private const float OpaqueEnough = 0.995f;

    /// <summary><paramref name="fg"/> composited over <paramref name="bg"/> by fg's alpha; the result is opaque.</summary>
    public static Rgba Over(Rgba fg, Rgba bg)
    {
        float a = fg.A;
        return new Rgba(fg.R * a + bg.R * (1f - a), fg.G * a + bg.G * (1f - a), fg.B * a + bg.B * (1f - a), 1f);
    }

    /// <summary>
    /// Every contrast problem of a theme, one message each: a pair below its
    /// class's minimum ("Role 'DenyButton': ink on fill is 3.9:1, needs 4.5:1"),
    /// a text pair whose fill alpha is below the floor, and hover rings less than
    /// outline² apart. Fills are checked as opaque (the alpha floor keeps that
    /// honest); a translucent ink is composited over its fill first.
    /// The ring rule: with ring luminances a &lt; b (each + 0.05) and any
    /// background s, if a ≤ s ≤ b then (s/a)·(b/s) = b/a ≥ o², so one factor is
    /// ≥ o; if s &lt; a or s &gt; b the farther ring alone is ≥ b/a ≥ o² ≥ o. So
    /// one ring reaches the outline minimum on any colour behind a clickable.
    /// </summary>
    public static List<string> Problems(IReadOnlyList<ContrastPair> pairs, Rgba ringDark, Rgba ringLight, ContrastRules rules)
    {
        if (pairs == null || rules == null)
            throw new ArgumentException("Contrast.Problems needs pairs and rules.");

        var problems = new List<string>();
        foreach (ContrastPair p in pairs)
        {
            float min = rules.Min(p.Class);
            if (min <= 0f)
                continue;

            if (p.Fill.A < rules.minTextFillAlpha)
                problems.Add($"Role '{p.Name}': its text sits on a fill of alpha {F(p.Fill.A)}, below the floor {F(rules.minTextFillAlpha)}.");

            Rgba fill = p.Fill.WithAlpha(1f);
            double ratio = Ratio(Over(p.Ink, fill), fill);
            if (ratio < min)
                problems.Add($"Role '{p.Name}': ink on fill is {F(ratio)}:1, needs {F(min)}:1.");
        }

        double rings = Ratio(ringDark, ringLight);
        double needed = (double)rules.outline * rules.outline;
        if (rings < needed)
            problems.Add($"Hover rings are {F(rings)}:1 apart; they need {F(needed)}:1 so that one of them reaches {F(rules.outline)}:1 on any background.");

        return problems;
    }

    /// <summary>sRGB channel to linear light.</summary>
    private static double Linear(float c) => c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);

    /// <summary>A ratio for messages: one decimal, rounded down, so 4.48 reads 4.4 and never looks like a pass.</summary>
    private static string F(double v) => (Math.Floor(v * 10.0) / 10.0).ToString("0.0", CultureInfo.InvariantCulture);
}
