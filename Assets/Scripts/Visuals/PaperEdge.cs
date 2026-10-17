using System.Collections.Generic;

/// <summary>What a thing lying on the desk is, for its model (DeskPaperDepth): a sheet of paper, a passport booklet, a plastic card or the rulebook's folder.</summary>
public enum PaperKind
{
    /// <summary>A sheet of paper: a gentle curl and a lifted corner.</summary>
    Sheet,

    /// <summary>An open passport booklet: its hard covers past its pages, its page blocks curving into the stitched gutter.</summary>
    Booklet,

    /// <summary>A thick card (the transponder card): rounded corners, a bevelled rim.</summary>
    Card,

    /// <summary>The rulebook's folder: card boards with a creased spine, loose sheets, flaps and a clip.</summary>
    Folder
}

/// <summary>
/// The kind and the outline of what lies on the desk (Saleh's 1007d playtest:
/// "they are too flat when you see them from the side"; 2026-10-17: "use
/// canva and blender to make actual 3d documents"): each paper wears its
/// kind's model (DeskPaperDepth), and its outline lays its contact shadow
/// (PaperDepth.Shadow). Engine-free.
/// </summary>
public static class PaperEdge
{
    /// <summary>The kind of a paper drawn with <paramref name="frame"/>: a booklet's or a card's, else a sheet.</summary>
    public static PaperKind KindOf(FormFrame frame) => frame == FormFrame.Booklet ? PaperKind.Booklet : frame == FormFrame.Card ? PaperKind.Card : PaperKind.Sheet;

    /// <summary>A rectangle's outline, <paramref name="width"/> by <paramref name="height"/> centred on the origin, corners counter-clockwise from the bottom left.</summary>
    public static List<(float x, float y)> Rectangle(float width, float height) =>
        new List<(float x, float y)> { (-width / 2f, -height / 2f), (width / 2f, -height / 2f), (width / 2f, height / 2f), (-width / 2f, height / 2f) };
}
