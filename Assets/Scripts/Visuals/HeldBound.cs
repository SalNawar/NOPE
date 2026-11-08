using System;

/// <summary>
/// The bound a held thing's root obeys while it is dragged (DeskDraggable.Bound,
/// DeskController.PaperBound and FolderBound; FOLD-01 rounds 2 to 4). Every
/// rectangle is in DropResolver's desk frame (x to the right, y away from the
/// chair, metres).
/// <list type="bullet">
/// <item>The folder is kept wholly inside the area it may rest in from its
/// first frame (round 2, F3: Papers, Please keeps a held thing on the desk,
/// so a release never slides after).</item>
/// <item>A paper is kept there only once it has lain wholly there in this
/// drag and zone (round 3, <see cref="DropResolver.Held"/>): one pulled from
/// the counter follows the pointer one to one onto the desk. Where it lay at
/// the pick-up counts, so a desk paper is held from its first frame.</item>
/// <item>A paper is judged where it lies once its size has ended its ease
/// (round 4, G2): its footprint at its zone's size with its root moved on by
/// the growth still to come (the grab offset times the grab scale still to
/// grow). A paper crossing the counter's edge grows 1.5 to 3.4 times under
/// the cursor over about 5 frames, and its root moves away from the held
/// point as it grows. Round 3 judged it at the counter's edge where a smaller
/// offset had put it, so the bound turned on and then held the paper at the
/// back step's lip for 1 to 3 frames, up to 59.9 mm, while the pointer moved
/// on.</item>
/// <item>A zone change is not a pick-up: the paper starts free in its new
/// zone and is held from the frame it lies wholly in the new area.</item>
/// <item>Over a machine that takes a drop there the bound gives way, so a
/// paper's centre reaches the machine's bed (the bounce of 2026-11-07: the
/// chip card at its 0.6 m reading size was held wholly in the reading view,
/// its centre short of the scanner's bed at the view's edge, and nothing
/// scanned). Its state goes on, so off the machine it is held again.</item>
/// </list>
/// The area is taken at each pick-up, and again only when the zone or the
/// footprint's size changes, so a drag frame allocates nothing. Pure, so every
/// case is tested headless (HeldBoundTests).
/// </summary>
public sealed class HeldBound
{
    private DeskRect _area;
    private bool _set, _onDesk;
    private float _width, _height;

    /// <summary>True once the held paper has lain wholly in its area in this drag and zone: the bound keeps it there from then on.</summary>
    public bool Holding { get; private set; }

    /// <summary>A drag picked the thing up: the next step takes its area afresh and judges where it lay.</summary>
    public void PickUp() => _set = false;

    /// <summary>
    /// One follow: the move (desk frame, metres) the bound gives the root.
    /// <paramref name="footprint"/> is the thing at the size it rests at in
    /// its zone, centred where this follow puts its root;
    /// (<paramref name="growX"/>, <paramref name="growY"/>) is how far that
    /// root still moves while its size ends its ease (0 for a thing whose size
    /// does not ease); <paramref name="onDesk"/> says the zone;
    /// <paramref name="lies"/> is where a paper lay before this follow, at its
    /// zone's size (read only at a pick-up), or null for the folder (kept
    /// inside from its first frame). <paramref name="area"/> gives the area a
    /// footprint rests wholly inside in a zone (DeskController.RestArea).
    /// <paramref name="overMachine"/> is true when the footprint's centre
    /// lies over a machine that takes a drop there (DeskController.DockAt):
    /// the step then gives no move but still keeps the bound's state.
    /// </summary>
    public (float x, float y) Step(DeskRect footprint, float growX, float growY, bool onDesk, DeskRect? lies, Func<DeskRect, bool, DeskRect> area, bool overMachine = false)
    {
        var settled = new DeskRect(footprint.CentreX + growX, footprint.CentreY + growY, footprint.Width, footprint.Height);
        bool pickUp = !_set, crossed = _set && onDesk != _onDesk;
        if (pickUp || crossed || Math.Abs(footprint.Width - _width) > 1e-4f || Math.Abs(footprint.Height - _height) > 1e-4f)
        {
            _area = area(settled, onDesk);
            if (pickUp)
                Holding = lies != null && DropResolver.Held(lies.Value, _area, false).holding;
            else if (crossed)
                Holding = false;
            _set = true;
            _onDesk = onDesk;
            _width = footprint.Width;
            _height = footprint.Height;
        }
        float x, y;
        if (lies != null)
        {
            bool holding;
            (x, y, holding) = DropResolver.Held(settled, _area, Holding);
            Holding = holding;
        }
        else
            (x, y) = DropResolver.Inside(settled, _area);
        return overMachine ? (0f, 0f) : (x - settled.CentreX, y - settled.CentreY);
    }
}
