using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>A scan's layers (the scanner app spec §2.4): the print as it reads, the UV light's hidden layer, the chip's stored record. Runtime only.</summary>
public enum ScanLayer
{
    /// <summary>The paper as printed (the scanned copy).</summary>
    Print,

    /// <summary>The hidden layer under UV light: watermark, the ghost of an erased value, microprint.</summary>
    Uv,

    /// <summary>The chip's stored data, in the print's layout so differences line up.</summary>
    Chip,

    /// <summary>The microprint line along a paper's foot (the desk machines spec §10): the holder's Citizen ID, repeated too fine to read by eye at the desk; the scan view's hidden layer prints it large enough to read (ScanMicroprint; Saleh D5), and a read is credited only once it rendered there (MicroLens.Readable).</summary>
    Micro
}

/// <summary>What a UV mark is (the desk-machine spec §5's hidden layer).</summary>
public enum UvMarkKind
{
    /// <summary>The agency's watermark (present on a genuine paper).</summary>
    Watermark,

    /// <summary>The ghost of an erased value under a doctored one.</summary>
    Ghost,

    /// <summary>The issuing office's microprint (missing on a forged seal).</summary>
    Microprint
}

/// <summary>One mark of a paper's hidden layer: its kind, the field it lies under (-1: the whole paper), what it reads, and whether it shows a fault (a ghost value, a missing mark).</summary>
public readonly struct UvMark
{
    /// <summary>A mark.</summary>
    public UvMark(UvMarkKind kind, int field, string text, bool fault)
    {
        Kind = kind;
        Field = field;
        Text = text ?? string.Empty;
        Fault = fault;
    }

    /// <summary>What it is.</summary>
    public UvMarkKind Kind { get; }

    /// <summary>The field it lies under (-1: the whole paper).</summary>
    public int Field { get; }

    /// <summary>What it reads ("AGENCY", the erased value).</summary>
    public string Text { get; }

    /// <summary>True when it shows a fault (a ghost under a doctored value, a mark that should be there and is not).</summary>
    public bool Fault { get; }
}

/// <summary>The scan layers' rules, pure.</summary>
public static class ScanLayers
{
    /// <summary>The printed fields whose chip value differs (both state a value that is not the same value, Values.Match): the CHIP layer's glow; none without a chip.</summary>
    public static List<int> ChipDifferences(IReadOnlyList<DocumentField> print, IReadOnlyList<DocumentField> chip)
    {
        var differ = new List<int>();
        if (print == null || chip == null)
            return differ;
        for (int i = 0; i < print.Count && i < chip.Count; i++)
            if (print[i] != null && chip[i] != null && !string.IsNullOrWhiteSpace(print[i].value) && !string.IsNullOrWhiteSpace(chip[i].value)
                && !Values.Match(print[i].value, chip[i].value))
                differ.Add(i);
        return differ;
    }
}

/// <summary>
/// The microprint in the scanner's hidden-layer view (FIX_PLAN_v2 TOOLS,
/// BUG-2b; Saleh D5: "the scanner's hidden-layer view reveals the microprint
/// of the ReprintedPaper lie. The physical magnifiers are cut."): what the
/// layer prints for a paper's line (one repeat of it: the Bureau's mark, the
/// form and the Citizen ID the player holds against the ID box) and the size
/// it prints at, so its capitals reach <see cref="MinCapPx"/> on the screen
/// with <see cref="Headroom"/> however the view is drawn (the PC's window at
/// 1280x720, the reading view's tilt). The scan view measures how many screen
/// pixels one unit of its text covers as it draws and asks for the size; the
/// read is credited on the measured capitals (MicroLens.Readable). Pure
/// (MicroLensTests).
/// </summary>
public static class ScanMicroprint
{
    /// <summary>The smallest capital height on the screen at which the microprint counts as legible (FIX_PLAN: "≥12 px cap at A2", 1280x720).</summary>
    public const float MinCapPx = 12f;

    /// <summary>The view prints the line this much over the cap (15 px), so rounding, a font's own cap or a window dragged a little smaller never drop it under.</summary>
    public const float Headroom = 1.25f;

    /// <summary>The largest size the view prints the line at (its text's units): a guard for a view not measured yet.</summary>
    public const float MaxFontSize = 96f;

    /// <summary>The text the hidden layer prints for <paramref name="line"/> on form <paramref name="formNumber"/>: the line as the paper prints it, once, without its trailing separator.</summary>
    public static string Text(MicroLine line, string formNumber)
    {
        string once = line.Printed(formNumber, 1);
        return once.TrimEnd(' ', '·');
    }

    /// <summary>
    /// The font size (the text's own units) at which capitals reach
    /// MinCapPx × Headroom on the screen: <paramref name="capShare"/> is the
    /// font's capital height per unit of size (its face's cap line over its
    /// point size), <paramref name="pixelsPerUnit"/> the screen pixels one unit
    /// of the text covers where it is drawn. Never above MaxFontSize (nothing
    /// measured yet: no metrics, no pixels).
    /// </summary>
    public static float FontSize(float capShare, float pixelsPerUnit)
    {
        float perSize = capShare * pixelsPerUnit;
        if (!(perSize > 1e-6f))
            return MaxFontSize;
        float size = MinCapPx * Headroom / perSize;
        return size < MaxFontSize ? size : MaxFontSize;
    }

    /// <summary>The capital height in screen pixels of text at <paramref name="fontSize"/> (FontSize's inverse).</summary>
    public static float CapPx(float fontSize, float capShare, float pixelsPerUnit) => fontSize * capShare * pixelsPerUnit;
}

/// <summary>One value of an overlay source: its detail, its issuing office (a seal's), its label and value.</summary>
public readonly struct OverlayValue
{
    /// <summary>A value.</summary>
    public OverlayValue(ClueCategory category, string label, string value, string issuer = null)
    {
        Category = category;
        Label = label ?? string.Empty;
        Value = value ?? string.Empty;
        Issuer = issuer ?? string.Empty;
    }

    /// <summary>The detail.</summary>
    public ClueCategory Category { get; }

    /// <summary>Its word on its source.</summary>
    public string Label { get; }

    /// <summary>What it reads (a photo: who it shows; a seal: its description).</summary>
    public string Value { get; }

    /// <summary>A seal's office (blank for anything else): seals line up by office.</summary>
    public string Issuer { get; }
}

/// <summary>A source the overlay can lay over another: a scanned paper, the record, the traveller's face, the Seal Register.</summary>
public sealed class OverlaySource
{
    /// <summary>A source.</summary>
    public OverlaySource(string title, IEnumerable<OverlayValue> values)
    {
        Title = title ?? string.Empty;
        Values = values != null ? values.ToList() : new List<OverlayValue>();
    }

    /// <summary>Its name.</summary>
    public string Title { get; }

    /// <summary>Its values.</summary>
    public IReadOnlyList<OverlayValue> Values { get; }
}

/// <summary>One line of the overlay: a detail both sources state, their two values and whether it shimmers (they really differ).</summary>
public readonly struct OverlayRow
{
    /// <summary>A line.</summary>
    public OverlayRow(ClueCategory category, string label, string a, string b, bool differs)
    {
        Category = category;
        Label = label ?? string.Empty;
        A = a ?? string.Empty;
        B = b ?? string.Empty;
        Differs = differs;
    }

    /// <summary>The detail.</summary>
    public ClueCategory Category { get; }

    /// <summary>The first source's word for it.</summary>
    public string Label { get; }

    /// <summary>The first source's value.</summary>
    public string A { get; }

    /// <summary>The second source's value.</summary>
    public string B { get; }

    /// <summary>True when the two really differ (Values.Match): only these shimmer.</summary>
    public bool Differs { get; }
}

/// <summary>
/// The overlay compare (the scanner app spec §2.5; the orchestrator's
/// Decision: the shimmer marks only areas that really differ in the data, so
/// it never gives a false alarm), pure: laid one over the other, two sources
/// line up on every detail both state (a seal against a seal of the same
/// office), and a line shimmers only when the two values are not the same
/// value (Values.Match): the passport photo against the face (who each
/// shows), a seal against the Seal Register's, a field against the record's.
/// </summary>
public static class OverlayCompare
{
    /// <summary>The lines of <paramref name="a"/> over <paramref name="b"/>, in <paramref name="a"/>'s order (each detail once; a seal once per office).</summary>
    public static List<OverlayRow> Rows(OverlaySource a, OverlaySource b)
    {
        var rows = new List<OverlayRow>();
        if (a == null || b == null)
            return rows;
        var seen = new HashSet<string>();
        foreach (OverlayValue va in a.Values)
        {
            if (string.IsNullOrWhiteSpace(va.Value) || !seen.Add(va.Category + "|" + va.Issuer))
                continue;
            foreach (OverlayValue vb in b.Values)
            {
                if (vb.Category != va.Category || string.IsNullOrWhiteSpace(vb.Value))
                    continue;
                if (va.Category == ClueCategory.Seal && !string.Equals(va.Issuer, vb.Issuer, StringComparison.Ordinal))
                    continue;
                rows.Add(new OverlayRow(va.Category, va.Label, va.Value, vb.Value, !Values.Match(va.Value, vb.Value)));
                break;
            }
        }
        return rows;
    }
}
