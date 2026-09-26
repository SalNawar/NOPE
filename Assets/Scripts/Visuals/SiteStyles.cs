using System.Collections.Generic;

/// <summary>A site's fixed page colours (P spec IN6): its pages are content, so the culture theme never touches them.</summary>
public readonly struct SiteStyle
{
    /// <summary>A style from its colours.</summary>
    public SiteStyle(Rgba paper, Rgba ink, Rgba muted, Rgba accent, Rgba link, Rgba box, Rgba rule)
    {
        Paper = paper;
        Ink = ink;
        Muted = muted;
        Accent = accent;
        Link = link;
        Box = box;
        Rule = rule;
    }

    /// <summary>The page.</summary>
    public Rgba Paper { get; }

    /// <summary>Body text and values.</summary>
    public Rgba Ink { get; }

    /// <summary>Notes, labels and column heads.</summary>
    public Rgba Muted { get; }

    /// <summary>The masthead, headings, box titles and revised notes.</summary>
    public Rgba Accent { get; }

    /// <summary>Links.</summary>
    public Rgba Link { get; }

    /// <summary>Boxes, infobox cells, tiles and chips (drawn on the page).</summary>
    public Rgba Box { get; }

    /// <summary>The masthead's rule (decoration: no text is drawn on it).</summary>
    public Rgba Rule { get; }
}

/// <summary>
/// The sites' page styles: newsprint for News, an encyclopedia for History,
/// an archive card for Ancestry, and the start page's for Static sites, the
/// start page and the missing page. Every text colour reads on the page and
/// on a box at the body-text minimum (tested), so one check covers every site.
/// </summary>
public static class SiteStyles
{
    /// <summary>Newsprint (The Temporal Times).</summary>
    private static readonly SiteStyle News = new SiteStyle(Hex(0xF4, 0xEF, 0xE3), Hex(0x1C, 0x1A, 0x17), Hex(0x55, 0x50, 0x47), Hex(0x7A, 0x1E, 0x12),
                                                           Hex(0x1F, 0x3F, 0x7A), Hex(0xE6, 0xDE, 0xCB), Hex(0x3A, 0x35, 0x2C));

    /// <summary>An encyclopedia (Chronopedia).</summary>
    private static readonly SiteStyle History = new SiteStyle(Hex(0xFB, 0xFB, 0xF8), Hex(0x20, 0x21, 0x22), Hex(0x54, 0x59, 0x5D), Hex(0x0B, 0x3D, 0x5C),
                                                              Hex(0x2A, 0x55, 0xB0), Hex(0xEA, 0xEC, 0xF0), Hex(0xA2, 0xA9, 0xB1));

    /// <summary>An archive card (the Lineage Archive).</summary>
    private static readonly SiteStyle Ancestry = new SiteStyle(Hex(0xEF, 0xE6, 0xD2), Hex(0x2B, 0x21, 0x18), Hex(0x5A, 0x4D, 0x3C), Hex(0x5B, 0x3A, 0x1A),
                                                               Hex(0x6B, 0x2C, 0x0E), Hex(0xE2, 0xD5, 0xB8), Hex(0x8C, 0x7A, 0x5B));

    /// <summary>The start page's (and every Static site's and the missing page's).</summary>
    private static readonly SiteStyle Start = new SiteStyle(Hex(0xF2, 0xF4, 0xF7), Hex(0x1B, 0x1F, 0x24), Hex(0x4F, 0x56, 0x61), Hex(0x1C, 0x4E, 0x80),
                                                            Hex(0x1C, 0x4E, 0x80), Hex(0xE1, 0xE7, 0xEE), Hex(0x9A, 0xA5, 0xB1));

    /// <summary>Every style by its site kind's name ("News", "History", "Ancestry"; anything else is the start page's).</summary>
    public static SiteStyle For(string kind)
    {
        switch (kind)
        {
            case "News": return News;
            case "History": return History;
            case "Ancestry": return Ancestry;
            default: return Start;
        }
    }

    /// <summary>The named styles, for the contrast check.</summary>
    public static IReadOnlyList<(string Name, SiteStyle Style)> All => new[] { ("News", News), ("History", History), ("Ancestry", Ancestry), ("Start", Start) };

    /// <summary>Every text colour of a style on the page and on a box, as body text (the strictest class).</summary>
    public static List<ContrastPair> Pairs(string name, SiteStyle s)
    {
        var pairs = new List<ContrastPair>();
        foreach ((string what, Rgba ink) in new[] { ("ink", s.Ink), ("muted", s.Muted), ("accent", s.Accent), ("link", s.Link) })
        {
            pairs.Add(new ContrastPair($"{name} {what} on the page", ink, s.Paper, ContrastClass.Text));
            pairs.Add(new ContrastPair($"{name} {what} on a box", ink, s.Box, ContrastClass.Text));
        }
        pairs.Add(new ContrastPair($"{name} page on the accent (an active chip)", s.Paper, s.Accent, ContrastClass.Text));
        return pairs;
    }

    /// <summary>An opaque colour from 0-255 channels.</summary>
    private static Rgba Hex(int r, int g, int b) => new Rgba(r / 255f, g / 255f, b / 255f);
}
