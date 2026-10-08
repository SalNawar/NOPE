using System;
using System.Collections.Generic;

/// <summary>What a thing lying on the desk is, for its edge (PaperEdge): a sheet of paper, a passport booklet, a plastic card or the rulebook's folder.</summary>
public enum PaperKind
{
    /// <summary>A sheet of paper: one thin edge a shade darker than its face.</summary>
    Sheet,

    /// <summary>A passport booklet: its cover top and bottom, its pages' edges between.</summary>
    Booklet,

    /// <summary>A plastic card (the transponder card): one plain edge.</summary>
    Card,

    /// <summary>The rulebook's folder: its pages' edges over a board cover.</summary>
    Folder
}

/// <summary>Which colour an edge band takes (the desk paper picks the colours: its face's, its cover's).</summary>
public enum EdgeTone
{
    /// <summary>A sheet's or a card's own edge: its face a shade darker.</summary>
    Edge,

    /// <summary>A page's edge, light.</summary>
    PageLight,

    /// <summary>A page's edge, a little darker (pages alternate).</summary>
    PageDark,

    /// <summary>A booklet's cover or a folder's board.</summary>
    Cover
}

/// <summary>One band of an edge: from <see cref="Top"/> down to <see cref="Bottom"/> under the face (metres, both positive), in <see cref="Tone"/>.</summary>
public readonly struct EdgeBand
{
    /// <summary>The band's top, metres under the face.</summary>
    public readonly float Top;

    /// <summary>The band's bottom, metres under the face.</summary>
    public readonly float Bottom;

    /// <summary>The band's colour.</summary>
    public readonly EdgeTone Tone;

    /// <summary>A band from <paramref name="top"/> to <paramref name="bottom"/> in <paramref name="tone"/>.</summary>
    public EdgeBand(float top, float bottom, EdgeTone tone)
    {
        Top = top;
        Bottom = bottom;
        Tone = tone;
    }
}

/// <summary>
/// The thickness of what lies on the desk (Saleh's 1007d playtest: "they are
/// too flat when you see them from the side"): each paper, card, booklet and
/// the folder has an edge under its face, its side walls only (the face covers
/// its top, nothing looks at its bottom), drawn in bands: a sheet's one edge,
/// a booklet's cover top and bottom with its pages between, a folder's pages
/// over its board. The walls start <see cref="TopGap"/> under the face so they
/// never fight it for depth. Engine-free: the desk paper and the folder build
/// their meshes from it; their stack lifts each by its thickness (PaperLayers.Lifts).
/// </summary>
public static class PaperEdge
{
    /// <summary>How far under the face the edge starts (metres): clear of the face for the depth buffer.</summary>
    public const float TopGap = 0.0001f;

    /// <summary>A booklet's cover and a folder's board: their share of the thickness, top and bottom (a folder's at the bottom only).</summary>
    private const float CoverShare = 0.15f;

    /// <summary>How many page edges a booklet or a folder shows.</summary>
    public const int PageBands = 6;

    /// <summary>
    /// The bands of an edge of <paramref name="kind"/>, <paramref name="thickness"/>
    /// metres thick (at least TopGap more than nothing): contiguous from TopGap
    /// down to the thickness. A sheet or a card is one Edge band; a booklet its
    /// Cover, then PageBands alternating pages, then its Cover; a folder
    /// PageBands pages over its Cover board.
    /// </summary>
    public static List<EdgeBand> Bands(PaperKind kind, float thickness)
    {
        float bottom = Math.Max(thickness, TopGap * 2f);
        var bands = new List<EdgeBand>();
        if (kind == PaperKind.Sheet || kind == PaperKind.Card)
        {
            bands.Add(new EdgeBand(TopGap, bottom, EdgeTone.Edge));
            return bands;
        }
        float span = bottom - TopGap;
        float cover = span * CoverShare;
        float top = TopGap;
        if (kind == PaperKind.Booklet)
        {
            bands.Add(new EdgeBand(top, top + cover, EdgeTone.Cover));
            top += cover;
        }
        float pagesEnd = bottom - cover;
        float page = (pagesEnd - top) / PageBands;
        for (int i = 0; i < PageBands; i++)
            bands.Add(new EdgeBand(top + page * i, i == PageBands - 1 ? pagesEnd : top + page * (i + 1), i % 2 == 0 ? EdgeTone.PageLight : EdgeTone.PageDark));
        bands.Add(new EdgeBand(pagesEnd, bottom, EdgeTone.Cover));
        return bands;
    }

    /// <summary>
    /// The side walls of <paramref name="outline"/> (a closed loop on the face's
    /// plane, x and y) through each of <paramref name="bands"/>: per band and per
    /// outline segment a quad (its own four corners, so a band's colour stays
    /// hard), its corners as (x, y, depth under the face) with the band's tone,
    /// two triangles each wound both ways (the walls show from outside whatever
    /// way the outline turns). Clears and fills the three lists.
    /// </summary>
    public static void Walls(IReadOnlyList<(float x, float y)> outline, IReadOnlyList<EdgeBand> bands,
                             List<(float x, float y, float z)> vertices, List<EdgeTone> tones, List<int> triangles)
    {
        vertices.Clear();
        tones.Clear();
        triangles.Clear();
        if (outline == null || outline.Count < 3 || bands == null)
            return;
        foreach (EdgeBand band in bands)
            for (int i = 0; i < outline.Count; i++)
            {
                (float x, float y) a = outline[i], b = outline[(i + 1) % outline.Count];
                int v = vertices.Count;
                vertices.Add((a.x, a.y, band.Top));
                vertices.Add((b.x, b.y, band.Top));
                vertices.Add((b.x, b.y, band.Bottom));
                vertices.Add((a.x, a.y, band.Bottom));
                for (int k = 0; k < 4; k++)
                    tones.Add(band.Tone);
                triangles.AddRange(new[] { v, v + 1, v + 2, v, v + 2, v + 3, v, v + 2, v + 1, v, v + 3, v + 2 });
            }
    }

    /// <summary>The kind of a paper drawn with <paramref name="frame"/>: a booklet's or a card's, else a sheet.</summary>
    public static PaperKind KindOf(FormFrame frame) => frame == FormFrame.Booklet ? PaperKind.Booklet : frame == FormFrame.Card ? PaperKind.Card : PaperKind.Sheet;

    /// <summary>A rectangle's outline, <paramref name="width"/> by <paramref name="height"/> centred on the origin, corners counter-clockwise from the bottom left.</summary>
    public static List<(float x, float y)> Rectangle(float width, float height) =>
        new List<(float x, float y)> { (-width / 2f, -height / 2f), (width / 2f, -height / 2f), (width / 2f, height / 2f), (-width / 2f, height / 2f) };
}
