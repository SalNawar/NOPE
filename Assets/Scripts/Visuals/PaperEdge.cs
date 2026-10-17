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
    Cover,

    /// <summary>The edge's foot: the cel ink line where the thing meets what it lies on (its silhouette's outline).</summary>
    Ink,

    /// <summary>A folder board's cut edge catching the light (a lighter band over its board).</summary>
    Lit
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
/// too flat when you see them from the side"; 2026-10-17: "all documents need
/// to feel 3d especially the folder"): each paper, card, booklet and the
/// folder has an edge under its face, its side walls only (the face covers
/// its top, nothing looks at its bottom), drawn in bands, each ending in an
/// Ink line at its foot (the cel outline): a sheet's or a card's one edge, a
/// booklet's cover top and bottom with its pages between, a folder's board
/// (its lit cut edge over it). The walls lean out under the face toward the
/// bottom right of the reading view (Walls' lean: PaperDepth.Lean), so the
/// near camera, looking almost straight down, sees the thickness there; on
/// the far sides they tuck under the face. The walls start <see cref="TopGap"/> under the face so they
/// never fight it for depth. Engine-free: the desk paper and the folder build
/// their meshes from it; their stack lifts each by its thickness (PaperLayers.Lifts).
/// </summary>
public static class PaperEdge
{
    /// <summary>How far under the face the edge starts (metres): clear of the face for the depth buffer.</summary>
    public const float TopGap = 0.0001f;

    /// <summary>A booklet's cover and a folder's board: their share of the thickness, top and bottom (a folder's at the bottom only).</summary>
    private const float CoverShare = 0.15f;

    /// <summary>How many page edges a booklet shows (the folder's pages are its PaperDepth.PageBlock on its face).</summary>
    public const int PageBands = 6;

    /// <summary>The Ink line's share of the thickness, at the edge's foot.</summary>
    public const float InkShare = 0.25f;

    /// <summary>A folder board's lit cut edge: its share of the thickness, at the top.</summary>
    private const float LitShare = 0.2f;

    /// <summary>
    /// The bands of an edge of <paramref name="kind"/>, <paramref name="thickness"/>
    /// metres thick (at least TopGap more than nothing): contiguous from TopGap
    /// down to the thickness, the last its Ink line (InkShare). A sheet or a
    /// card is one Edge band; a booklet its Cover, then PageBands alternating
    /// pages, then its Cover; a folder its board's Lit cut edge over its Cover
    /// board.
    /// </summary>
    public static List<EdgeBand> Bands(PaperKind kind, float thickness)
    {
        float bottom = Math.Max(thickness, TopGap * 2f);
        float span = bottom - TopGap;
        float inkTop = bottom - span * InkShare;
        var bands = new List<EdgeBand>();
        float top = TopGap;
        if (kind == PaperKind.Sheet || kind == PaperKind.Card)
            bands.Add(new EdgeBand(top, inkTop, EdgeTone.Edge));
        else if (kind == PaperKind.Folder)
        {
            float lit = top + span * LitShare;
            bands.Add(new EdgeBand(top, lit, EdgeTone.Lit));
            bands.Add(new EdgeBand(lit, inkTop, EdgeTone.Cover));
        }
        else
        {
            float cover = span * CoverShare;
            bands.Add(new EdgeBand(top, top + cover, EdgeTone.Cover));
            top += cover;
            float pagesEnd = inkTop - cover;
            float page = (pagesEnd - top) / PageBands;
            for (int i = 0; i < PageBands; i++)
                bands.Add(new EdgeBand(top + page * i, i == PageBands - 1 ? pagesEnd : top + page * (i + 1), i % 2 == 0 ? EdgeTone.PageLight : EdgeTone.PageDark));
            bands.Add(new EdgeBand(pagesEnd, inkTop, EdgeTone.Cover));
        }
        bands.Add(new EdgeBand(inkTop, bottom, EdgeTone.Ink));
        return bands;
    }

    /// <summary>
    /// The side walls of <paramref name="outline"/> (a closed loop on the face's
    /// plane, x and y) through each of <paramref name="bands"/>: per band and per
    /// outline segment a quad (its own four corners, so a band's colour stays
    /// hard), its corners as (x, y, depth under the face) with the band's tone,
    /// two triangles each wound both ways (the walls show from outside whatever
    /// way the outline turns). Each corner at a depth moves out by
    /// <paramref name="lean"/> (x, y per metre of depth: PaperDepth.Lean) times
    /// its outline corner's <paramref name="lift"/> (PaperDepth.CornerLift;
    /// none: 1), so the walls lean out where the camera sees them. Clears and
    /// fills the three lists.
    /// </summary>
    public static void Walls(IReadOnlyList<(float x, float y)> outline, IReadOnlyList<EdgeBand> bands,
                             List<(float x, float y, float z)> vertices, List<EdgeTone> tones, List<int> triangles,
                             (float x, float y) lean = default, IReadOnlyList<float> lift = null)
    {
        vertices.Clear();
        tones.Clear();
        triangles.Clear();
        if (outline == null || outline.Count < 3 || bands == null)
            return;
        foreach (EdgeBand band in bands)
            for (int i = 0; i < outline.Count; i++)
            {
                int j = (i + 1) % outline.Count;
                (float x, float y) a = outline[i], b = outline[j];
                float la = lift != null && i < lift.Count ? lift[i] : 1f, lb = lift != null && j < lift.Count ? lift[j] : 1f;
                int v = vertices.Count;
                vertices.Add((a.x + lean.x * la * band.Top, a.y + lean.y * la * band.Top, band.Top));
                vertices.Add((b.x + lean.x * lb * band.Top, b.y + lean.y * lb * band.Top, band.Top));
                vertices.Add((b.x + lean.x * lb * band.Bottom, b.y + lean.y * lb * band.Bottom, band.Bottom));
                vertices.Add((a.x + lean.x * la * band.Bottom, a.y + lean.y * la * band.Bottom, band.Bottom));
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
