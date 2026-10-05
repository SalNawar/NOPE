using System;
using System.Collections.Generic;

/// <summary>
/// The frame a form is printed in (the document design spec, D1): coloured
/// bands in the page's margins, so each kind of paper is known by its outline
/// before a word is read. Serialized in every form asset: append only.
/// </summary>
public enum FormFrame
{
    /// <summary>No band (the agency's plain paper).</summary>
    Plain,

    /// <summary>A band across the top edge.</summary>
    TopBand,

    /// <summary>A band down the left edge.</summary>
    SideBand,

    /// <summary>A band around the whole page, inside its edges (a certificate).</summary>
    Framed,

    /// <summary>A narrow band down the left edge and a perforation beside it (a ticket's stub).</summary>
    Ticket,

    /// <summary>A thick band across the bottom edge and a thin one across the top (a letterhead).</summary>
    BottomBand,

    /// <summary>An open passport booklet (the travel documents spec, TD1): the cover's edge around the pages in the holder's nation's colour (FormData.Cover, else the accent) and the nation's emblem at the header's left; a Fold block draws the spine between the data page and the visa page.</summary>
    Booklet,

    /// <summary>A plastic card (TD1): rounded corners, a sheen band down the left edge in the accent, and a chip where a cell names the chip slot.</summary>
    Card,

    /// <summary>A folded card (TD1): a band across the top in the accent and the crease down the middle of the page, under the boxes.</summary>
    Folded
}

/// <summary>
/// A form's silhouette (the document design spec, D1): its frame and band
/// colour, its paper's tint, its page's aspect and its size on the desk. Held
/// by the form (FormSpec.look), so a paper and its scanned copy share it; the
/// sizes of print stay the style's (FormMetrics, in page heights), so a
/// smaller paper is no smaller to read when held or scanned.
/// </summary>
[Serializable]
public sealed class FormLook
{
    /// <summary>The frame's bands.</summary>
    public FormFrame frame;

    /// <summary>The bands' colour ("#RRGGBB"); the section bands take a pale shade of it. Blank: the style's band.</summary>
    public string accent = string.Empty;

    /// <summary>The paper's tint ("#RRGGBB"). Blank: the style's paper.</summary>
    public string paper = string.Empty;

    /// <summary>The page's width over its height; 0: the style's (FormMetrics.aspect). Every paper's print is sized by its width (FormLayout.PrintUnit; the travel documents spec, TD2): on the PC each is drawn as wide as the style's page, a narrower one (a long legal sheet) taller, a wider one (a ticket, a card) less tall.</summary>
    public float aspect;

    /// <summary>The paper's height on the desk relative to the desk's paper (Desk_Default.paperSize); 1: the same.</summary>
    public float scale = 1f;

    /// <summary>The least and the greatest aspect a look may set (a long legal sheet to a card).</summary>
    public const float MinAspect = 0.5f, MaxAspect = 2f;

    /// <summary>The least and the greatest scale a look may set (a card or a ticket stub to a large sheet).</summary>
    public const float MinScale = 0.3f, MaxScale = 1.3f;

    /// <summary>The page's aspect: its own, else <paramref name="styleAspect"/>.</summary>
    public float AspectOr(float styleAspect) => aspect > 0f ? aspect : styleAspect;

    /// <summary>The paper's height on the desk as a share of the desk's paper (1 for a scale that is not positive).</summary>
    public float Scale => scale > 0f ? scale : 1f;

    /// <summary>A section band's share of the accent over the paper: pale enough for the ink (FormContrast checks it).</summary>
    public const float BandShare = 0.16f;

    /// <summary>A box's share of white over the paper (the style's box fill is its paper halfway to white).</summary>
    public const float BoxShare = 0.5f;

    /// <summary>
    /// <paramref name="style"/>'s colours as this look prints them: its paper
    /// tint (a box a lighter shade of it), its accent on the bands (a section
    /// band a pale shade of it over the paper); every ink the style's.
    /// </summary>
    public FormPalette Palette(FormPalette style)
    {
        FormPalette p = style.Copy();
        if (Rgba.TryParseHex(paper, out Rgba tint))
        {
            p.Paper = tint.WithAlpha(1f);
            p.BoxFill = Contrast.Over(new Rgba(1f, 1f, 1f, BoxShare), p.Paper);
        }
        if (Rgba.TryParseHex(accent, out Rgba band))
        {
            p.Accent = band.WithAlpha(1f);
            p.Band = Contrast.Over(band.WithAlpha(BandShare), p.Paper);
        }
        return p;
    }

    /// <summary>What Build Office UI and the validator refuse in a look: a colour that is not "#RRGGBB", a frame with bands but no accent, an aspect outside MinAspect to MaxAspect, a scale outside MinScale to MaxScale. Empty when sound. (<paramref name="styleAspect"/> is kept for the callers' messages: a look may be narrower or wider than the style's page.)</summary>
    public List<string> Problems(float styleAspect)
    {
        var problems = new List<string>();
        if (!string.IsNullOrEmpty(paper) && !Rgba.TryParseHex(paper, out _))
            problems.Add($"its look's paper '{paper}' is not #RRGGBB");
        if (!string.IsNullOrEmpty(accent) && !Rgba.TryParseHex(accent, out _))
            problems.Add($"its look's accent '{accent}' is not #RRGGBB");
        if (frame != FormFrame.Plain && string.IsNullOrEmpty(accent))
            problems.Add($"its look's {frame} frame has no accent colour");
        if (aspect > 0f && (aspect > MaxAspect + 1e-4f || aspect < MinAspect - 1e-4f))
            problems.Add($"its look's aspect {aspect:0.000} is outside {MinAspect:0.0} to {MaxAspect:0.0} (the style's page is {styleAspect:0.000})");
        if (scale < MinScale - 1e-4f || scale > MaxScale + 1e-4f)
            problems.Add($"its look's scale {scale:0.00} is outside {MinScale:0.0} to {MaxScale:0.0}");
        return problems;
    }
}
