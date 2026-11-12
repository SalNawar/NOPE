using System;

/// <summary>One of the two sides of the Lens case screen.</summary>
public enum LensSide
{
    /// <summary>The left side.</summary>
    Left,

    /// <summary>The right side.</summary>
    Right
}

/// <summary>
/// Which document is on which side of the Lens case screen (the PC Lens spec,
/// section 2). LinkTarget.None is an empty side. One side is selected: the
/// next paper opens there. A paper put on a side sends what was there back
/// to the dock (it just leaves), a drag from side to side changes the two
/// places, and a paper already on the other side moves. Pure.
/// </summary>
public sealed class LensSides
{
    private LinkTarget _left;
    private LinkTarget _right;

    /// <summary>The left document (None when empty).</summary>
    public LinkTarget Left => _left;

    /// <summary>The right document (None when empty).</summary>
    public LinkTarget Right => _right;

    /// <summary>The side the next paper opens on; starts Left.</summary>
    public LensSide Selected { get; private set; }

    /// <summary>The document on <paramref name="side"/>.</summary>
    public LinkTarget this[LensSide side] => side == LensSide.Left ? _left : _right;

    /// <summary>The other side.</summary>
    public static LensSide Other(LensSide side) => side == LensSide.Left ? LensSide.Right : LensSide.Left;

    /// <summary>The same document: the same tab, and the same item or one item below 0. None is never the same as anything.</summary>
    public static bool Same(LinkTarget a, LinkTarget b) =>
        !a.IsNone && !b.IsNone && a.Tab == b.Tab && (a.Item < 0 || b.Item < 0 || a.Item == b.Item);

    /// <summary>A side tag clicked (or F6): the next paper opens there.</summary>
    public void Select(LensSide side) => Selected = side;

    /// <summary>A click in the dock: the document opens on the selected side.</summary>
    public void Put(LinkTarget doc) => Place(doc, Selected);

    /// <summary>
    /// A document goes to <paramref name="side"/>. <paramref name="from"/> is the side a drag started on, null for a paper from the dock.
    /// Afterwards the other side is selected (unless nothing changed).
    /// </summary>
    public void Place(LinkTarget doc, LensSide side, LensSide? from = null)
    {
        if (doc.IsNone || from == side)
            return;
        LensSide other = Other(side);
        if (from == other)
            Set(other, this[side]);
        else if (Same(this[other], doc))
            Set(other, LinkTarget.None);
        Set(side, doc);
        Selected = other;
    }

    /// <summary>A drop outside the sides: the side is empty.</summary>
    public void PutAway(LensSide side) => Set(side, LinkTarget.None);

    /// <summary>A case ends: the documents that are <paramref name="gone"/> leave their sides.</summary>
    public void RemoveWhere(Predicate<LinkTarget> gone)
    {
        if (!_left.IsNone && gone(_left))
            _left = LinkTarget.None;
        if (!_right.IsNone && gone(_right))
            _right = LinkTarget.None;
    }

    private void Set(LensSide side, LinkTarget doc)
    {
        if (side == LensSide.Left)
            _left = doc;
        else
            _right = doc;
    }
}
