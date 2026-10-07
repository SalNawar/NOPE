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
    Chip
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

/// <summary>
/// The scan layers' data (the scanner app spec §2.4): a paper's hidden layer
/// and its chip record, by its index in the case and its printed fields.
/// The document track's `hidden` and `chip` data provide them once it lands;
/// until then <see cref="StubScanLayers"/> does.
/// </summary>
public interface IScanLayers
{
    /// <summary>The marks paper <paramref name="document"/> shows under UV (its printed <paramref name="print"/> fields).</summary>
    IReadOnlyList<UvMark> Uv(int document, IReadOnlyList<DocumentField> print);

    /// <summary>The chip's stored fields of paper <paramref name="document"/>, one per printed field in the print's order (null: the paper has no chip).</summary>
    IReadOnlyList<DocumentField> Chip(int document, IReadOnlyList<DocumentField> print);
}

/// <summary>
/// The scan layers until the document track's data lands (the orchestrator's
/// brief): every paper shows the agency's watermark, intact, and no ghost;
/// a paper that prints a Citizen ID carries a chip that stores exactly what
/// it prints, so nothing differs and nothing is ever a false alarm.
/// </summary>
public sealed class StubScanLayers : IScanLayers
{
    /// <summary>The watermark's words.</summary>
    public const string WatermarkText = "TEMPORAL CUSTOMS";

    /// <inheritdoc />
    public IReadOnlyList<UvMark> Uv(int document, IReadOnlyList<DocumentField> print) =>
        new[] { new UvMark(UvMarkKind.Watermark, -1, WatermarkText, false) };

    /// <inheritdoc />
    public IReadOnlyList<DocumentField> Chip(int document, IReadOnlyList<DocumentField> print)
    {
        if (print == null || !print.Any(f => f != null && f.category == ClueCategory.CitizenId))
            return null;
        return print.Select(f => f == null ? null : new DocumentField { category = f.category, label = f.label, value = f.value, page = f.page, issuer = f.issuer }).ToList();
    }
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
