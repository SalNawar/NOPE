using System;
using System.Collections.Generic;
using System.Globalization;

/// <summary>A point on the board plate in its pixels (origin top-left, y down; the plate is 1920 x 1080). Not serialized.</summary>
public readonly struct BoardPoint
{
    /// <summary>Pixels from the plate's left edge.</summary>
    public readonly float X;

    /// <summary>Pixels down from the plate's top edge.</summary>
    public readonly float Y;

    /// <summary>The point (<paramref name="x"/>, <paramref name="y"/>).</summary>
    public BoardPoint(float x, float y)
    {
        X = x;
        Y = y;
    }

    /// <summary>The distance to <paramref name="other"/> (pixels).</summary>
    public float DistanceTo(BoardPoint other)
    {
        float dx = other.X - X, dy = other.Y - Y;
        return MathF.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>The point moved by (<paramref name="dx"/>, <paramref name="dy"/>).</summary>
    public BoardPoint Plus(float dx, float dy) => new BoardPoint(X + dx, Y + dy);

    /// <summary>The point <paramref name="t"/> of the way to <paramref name="other"/>.</summary>
    public BoardPoint Lerp(BoardPoint other, float t) => new BoardPoint(X + (other.X - X) * t, Y + (other.Y - Y) * t);

    /// <summary>"(x, y)" for the log.</summary>
    public override string ToString() => "(" + X.ToString("0.#", CultureInfo.InvariantCulture) + ", " + Y.ToString("0.#", CultureInfo.InvariantCulture) + ")";
}

/// <summary>A rectangle on the board plate in its pixels (x, y the top-left corner, y down). Not serialized.</summary>
public readonly struct BoardRect
{
    /// <summary>The left edge.</summary>
    public readonly float X;

    /// <summary>The top edge.</summary>
    public readonly float Y;

    /// <summary>The width.</summary>
    public readonly float W;

    /// <summary>The height.</summary>
    public readonly float H;

    /// <summary>The rectangle at (<paramref name="x"/>, <paramref name="y"/>) of <paramref name="w"/> by <paramref name="h"/>.</summary>
    public BoardRect(float x, float y, float w, float h)
    {
        X = x;
        Y = y;
        W = w;
        H = h;
    }

    /// <summary>Its centre.</summary>
    public BoardPoint Centre => new BoardPoint(X + W / 2f, Y + H / 2f);

    /// <summary>True when <paramref name="p"/> lies inside it (edges included).</summary>
    public bool Contains(BoardPoint p) => p.X >= X && p.X <= X + W && p.Y >= Y && p.Y <= Y + H;

    /// <summary>The rectangle grown by <paramref name="margin"/> on every side.</summary>
    public BoardRect Grown(float margin) => new BoardRect(X - margin, Y - margin, W + 2f * margin, H + 2f * margin);
}

/// <summary>A socket or jack of the plate: its centre, its brass ring, how near a plug must land to seat in it, and whether a placard covers it. Not serialized.</summary>
public sealed class BoardSocket
{
    /// <summary>A socket.</summary>
    public BoardSocket(BoardPoint centre, float ringRadius, float hitRadius, bool covered)
    {
        Centre = centre;
        RingRadius = ringRadius;
        HitRadius = hitRadius;
        Covered = covered;
    }

    /// <summary>The hole's centre.</summary>
    public BoardPoint Centre { get; }

    /// <summary>The brass (or red) ring's radius.</summary>
    public float RingRadius { get; }

    /// <summary>How near a dropped plug must land to seat here (the art's suggested hit radius).</summary>
    public float HitRadius { get; }

    /// <summary>True when a placard (LOCKED, OUT OF SERVICE) is painted over it.</summary>
    public bool Covered { get; }
}

/// <summary>A lamp's dome on the plate: its centre and its glass's radius. Not serialized.</summary>
public sealed class BoardLamp
{
    /// <summary>A lamp.</summary>
    public BoardLamp(BoardPoint centre, float glassRadius)
    {
        Centre = centre;
        GlassRadius = glassRadius;
    }

    /// <summary>The dome's centre.</summary>
    public BoardPoint Centre { get; }

    /// <summary>The glass's radius (the lit sprite is drawn at 12; it scales by GlassRadius / 12).</summary>
    public float GlassRadius { get; }
}

/// <summary>One period jack of a destination column and its blank tag. Not serialized.</summary>
public sealed class LayoutPeriod
{
    /// <summary>A period row.</summary>
    public LayoutPeriod(BoardSocket jack, BoardRect tagText)
    {
        Jack = jack;
        TagText = tagText;
    }

    /// <summary>The period jack.</summary>
    public BoardSocket Jack { get; }

    /// <summary>The box the tag's live text fits in.</summary>
    public BoardRect TagText { get; }
}

/// <summary>One destination column of the plate: its blank name plate's text box, its place jack, its call lamp and its period rows. Not serialized.</summary>
public sealed class LayoutDestination
{
    /// <summary>A column.</summary>
    public LayoutDestination(BoardRect nameText, BoardSocket place, BoardLamp lamp, IReadOnlyList<LayoutPeriod> periods)
    {
        NameText = nameText;
        Place = place;
        Lamp = lamp;
        Periods = periods;
    }

    /// <summary>The box the name plate's live text fits in.</summary>
    public BoardRect NameText { get; }

    /// <summary>The place jack.</summary>
    public BoardSocket Place { get; }

    /// <summary>The call lamp.</summary>
    public BoardLamp Lamp { get; }

    /// <summary>The period rows, top first.</summary>
    public IReadOnlyList<LayoutPeriod> Periods { get; }
}

/// <summary>One portal module of the plate: its face, its number plate, its lamp, its POWER, PLACE and PERIOD sockets and how the plate paints it. Not serialized.</summary>
public sealed class LayoutModule
{
    /// <summary>A module.</summary>
    public LayoutModule(string id, BoardRect rect, BoardRect numberPlate, BoardLamp lamp, BoardSocket power, BoardSocket place, BoardSocket period, string plateState)
    {
        Id = id;
        Rect = rect;
        NumberPlate = numberPlate;
        Lamp = lamp;
        Power = power;
        Place = place;
        Period = period;
        PlateState = plateState ?? string.Empty;
    }

    /// <summary>Its number ("01").</summary>
    public string Id { get; }

    /// <summary>Its face.</summary>
    public BoardRect Rect { get; }

    /// <summary>Its number plate.</summary>
    public BoardRect NumberPlate { get; }

    /// <summary>Its lamp.</summary>
    public BoardLamp Lamp { get; }

    /// <summary>Its POWER socket.</summary>
    public BoardSocket Power { get; }

    /// <summary>Its PLACE socket (its place cord's home).</summary>
    public BoardSocket Place { get; }

    /// <summary>Its PERIOD socket (its period cord's home).</summary>
    public BoardSocket Period { get; }

    /// <summary>How the plate paints it: "free", "locked" (the LOCKED placard) or "out_of_service".</summary>
    public string PlateState { get; }

    /// <summary>True when the plate paints a placard over its sockets.</summary>
    public bool Placarded => PlateState == "locked" || PlateState == "out_of_service";
}

/// <summary>The MASTER lever of the plate: its lamp, its handle's pull from rest to pulled, and its two rods from their pivots. Not serialized.</summary>
public sealed class LayoutMaster
{
    /// <summary>The lever.</summary>
    public LayoutMaster(BoardLamp lamp, BoardRect handleRest, BoardPoint from, BoardPoint to, BoardPoint handlePivot,
                        IReadOnlyList<BoardPoint> rodPivots, IReadOnlyList<BoardPoint> rodTops, float rodAxisX, float rodTopY, float rodPivotY)
    {
        Lamp = lamp;
        HandleRest = handleRest;
        From = from;
        To = to;
        HandlePivot = handlePivot;
        RodPivots = rodPivots;
        RodTops = rodTops;
        RodAxisX = rodAxisX;
        RodTopY = rodTopY;
        RodPivotY = rodPivotY;
    }

    /// <summary>Its green lamp.</summary>
    public BoardLamp Lamp { get; }

    /// <summary>The handle's box at rest (its grab area, grown).</summary>
    public BoardRect HandleRest { get; }

    /// <summary>The handle's centre at rest (top).</summary>
    public BoardPoint From { get; }

    /// <summary>The handle's centre fully pulled (low).</summary>
    public BoardPoint To { get; }

    /// <summary>The handle sprite's pixel that sits on the handle's centre.</summary>
    public BoardPoint HandlePivot { get; }

    /// <summary>The rods' pivots (where they stay).</summary>
    public IReadOnlyList<BoardPoint> RodPivots { get; }

    /// <summary>Each rod's top as an offset from the handle's centre.</summary>
    public IReadOnlyList<BoardPoint> RodTops { get; }

    /// <summary>The rod sprite's axis column.</summary>
    public float RodAxisX { get; }

    /// <summary>The rod sprite's row that lands on the handle end.</summary>
    public float RodTopY { get; }

    /// <summary>The rod sprite's row that lands on the pivot.</summary>
    public float RodPivotY { get; }

    /// <summary>The handle's centre <paramref name="t"/> of the way down its pull (0 rest, 1 pulled).</summary>
    public BoardPoint HandleAt(float t) => From.Lerp(To, t < 0f ? 0f : t > 1f ? 1f : t);

    /// <summary>How far down its pull the point <paramref name="p"/> lies: its projection on the pull, 0 at rest to 1 pulled (clamped).</summary>
    public float PullOf(BoardPoint p)
    {
        float dx = To.X - From.X, dy = To.Y - From.Y;
        float len2 = dx * dx + dy * dy;
        if (len2 <= 0f)
            return 0f;
        float t = ((p.X - From.X) * dx + (p.Y - From.Y) * dy) / len2;
        return t < 0f ? 0f : t > 1f ? 1f : t;
    }
}

/// <summary>
/// The switchboard plate's measured layout (design\switchboard_v4\board\
/// board_layout.json, measured on the finished 1920 x 1080 plate: every
/// socket, lamp, text box and module, the MASTER lever's pull and rods, and
/// the plug sprites' cord anchors), read from the art's own JSON so the plate
/// and its live parts never disagree. Pure; Parse throws a FormatException
/// naming what is missing.
/// </summary>
public sealed class SwitchboardLayout
{
    private SwitchboardLayout()
    {
    }

    /// <summary>The plate's width (pixels).</summary>
    public float Width { get; private set; }

    /// <summary>The plate's height (pixels).</summary>
    public float Height { get; private set; }

    /// <summary>The board itself on the plate (the wood frame around it is wall).</summary>
    public BoardRect Board { get; private set; }

    /// <summary>The destination columns, left first.</summary>
    public IReadOnlyList<LayoutDestination> Destinations { get; private set; }

    /// <summary>The POWER row's sockets, left first.</summary>
    public IReadOnlyList<BoardSocket> PowerRow { get; private set; }

    /// <summary>The portal modules, 01 first.</summary>
    public IReadOnlyList<LayoutModule> Modules { get; private set; }

    /// <summary>The MASTER lever.</summary>
    public LayoutMaster Master { get; private set; }

    /// <summary>Where a plug's cord leaves it: its end cap, from the socket's centre (pixels at full size).</summary>
    public BoardPoint PlugTail { get; private set; }

    /// <summary>The most period rows any destination has.</summary>
    public int PeriodRows
    {
        get
        {
            int rows = 0;
            foreach (LayoutDestination d in Destinations)
                rows = Math.Max(rows, d.Periods.Count);
            return rows;
        }
    }

    /// <summary>Reads board_layout.json's text.</summary>
    public static SwitchboardLayout Parse(string json)
    {
        ContentNode root = ContentJson.Parse(json);
        var layout = new SwitchboardLayout();
        ContentNode size = Need(root, "image_size");
        layout.Width = Num(size.Items[0]);
        layout.Height = Num(size.Items[1]);
        layout.Board = Rect(Need(root, "board_rect"));

        var destinations = new List<LayoutDestination>();
        foreach (ContentNode d in Need(root, "destinations").Items)
        {
            var periods = new List<LayoutPeriod>();
            foreach (ContentNode p in Need(d, "periods").Items)
                periods.Add(new LayoutPeriod(Socket(Need(p, "jack")), Rect(Need(p, "tag_text_box"))));
            destinations.Add(new LayoutDestination(Rect(Need(d, "name_text_box")), Socket(Need(d, "place_jack")), Lamp(Need(d, "call_lamp")), periods));
        }
        layout.Destinations = destinations;

        var power = new List<BoardSocket>();
        foreach (ContentNode s in Need(root, "power_row").Items)
            power.Add(Socket(s));
        layout.PowerRow = power;

        var modules = new List<LayoutModule>();
        foreach (ContentNode m in Need(root, "modules").Items)
        {
            ContentNode sockets = Need(m, "sockets");
            modules.Add(new LayoutModule(Need(m, "id").Text, Rect(Need(m, "rect")), Rect(Need(m, "number_plate_rect")), Lamp(Need(m, "lamp")),
                                         Socket(Need(sockets, "power")), Socket(Need(sockets, "place")), Socket(Need(sockets, "period")),
                                         m.Get("plate_state")?.Text));
        }
        layout.Modules = modules;

        ContentNode master = Need(root, "master");
        ContentNode path = Need(master, "handle_path");
        ContentNode axis = Need(master, "rod_sprite_axis");
        var pivots = new List<BoardPoint>();
        foreach (ContentNode p in Need(master, "rod_pivots").Items)
            pivots.Add(Point(p));
        var tops = new List<BoardPoint>();
        foreach (ContentNode p in Need(master, "rod_top_offsets_from_handle_c").Items)
            tops.Add(Point(p));
        layout.Master = new LayoutMaster(Lamp(Need(master, "lamp")), Rect(Need(master, "handle_rest_rect")), Point(Need(path, "from")), Point(Need(path, "to")),
                                         Point(Need(master, "handle_sprite_pivot_px")), pivots, tops,
                                         Num(Need(axis, "x")), Num(Need(axis, "top_y")), Num(Need(axis, "pivot_y")));

        ContentNode plug = root.Get("sprites")?.Get("plug_meta")?.Get("plug_power");
        layout.PlugTail = plug?.Get("tip_px") != null ? Point(plug.Get("tip_px")) : new BoardPoint(0f, 0f);
        return layout;
    }

    private static ContentNode Need(ContentNode node, string key) =>
        node?.Get(key) ?? throw new FormatException($"board_layout.json: '{key}' is missing.");

    private static float Num(ContentNode node) => float.Parse(node.Text, NumberStyles.Float, CultureInfo.InvariantCulture);

    private static BoardPoint Point(ContentNode xy) => new BoardPoint(Num(xy.Items[0]), Num(xy.Items[1]));

    private static BoardRect Rect(ContentNode xywh) => new BoardRect(Num(xywh.Items[0]), Num(xywh.Items[1]), Num(xywh.Items[2]), Num(xywh.Items[3]));

    private static BoardSocket Socket(ContentNode s) =>
        new BoardSocket(Point(Need(s, "c")), Num(Need(s, "ring_r")), Num(Need(s, "hit_r")), s.Get("covered")?.Text == "true");

    private static BoardLamp Lamp(ContentNode l) => new BoardLamp(Point(Need(l, "c")), Num(Need(l, "glass_r")));
}

/// <summary>
/// The switchboard on the booth's right wall, seen from the desk (Saleh,
/// 2026-10-28: "Switchboard isnt visible instead it looks weird from the
/// desk"; the director's night review: the right wall 3/10, "a flat dark
/// slab stands there"): the board's own plate stands where concept B
/// (office_bars_v1.png) paints the board, so the clerk sees the board in the
/// room, its lamps live, before D turns to it. In the painted desk's frame
/// (the bake's: x metres right of the mat's centre, y metres away from the
/// chair, z metres above the floor; DeskPainted's local space is (x, z, y)).
/// The painted board, back-projected through the office camera (the eye at
/// 0, -2.10, 2.16, 9 degrees down, 58 degrees high at 16:9) from the desk
/// pipeline's render/board_mask_office.png onto the board's face, 8 cm proud
/// of the right wall at x 2.30 (layout.py's BOARD_DEPTH; the desk plan's
/// switchboard home at x 2.22): its far edge at y 0.88, its top at z 2.12,
/// its foot at z 1.15, just over the back step (z 1.139). It runs toward the
/// chair at the plate's aspect, on past the frame's right edge, as painted.
/// Pure.
/// </summary>
public readonly struct SwitchboardWall
{
    /// <summary>A board face at x <paramref name="face"/>, its far edge at y <paramref name="far"/>, from z <paramref name="foot"/> up to z <paramref name="top"/>.</summary>
    public SwitchboardWall(float face, float far, float top, float foot)
    {
        Face = face;
        Far = far;
        Top = top;
        Foot = foot;
    }

    /// <summary>The board on the painted booth's right wall (the measure above).</summary>
    public static SwitchboardWall Painted => new SwitchboardWall(2.22f, 0.88f, 2.12f, 1.15f);

    /// <summary>The board face's x (metres right of the mat's centre).</summary>
    public float Face { get; }

    /// <summary>The board's far edge (its left, as the clerk faces the wall): y metres away from the chair.</summary>
    public float Far { get; }

    /// <summary>The board's top, z metres above the floor.</summary>
    public float Top { get; }

    /// <summary>The board's foot, z metres above the floor.</summary>
    public float Foot { get; }

    /// <summary>The board's height on the wall (metres).</summary>
    public float Height => Top - Foot;

    /// <summary>How far <paramref name="board"/> (the plate's board rectangle) runs along the wall toward the chair at its own aspect (metres).</summary>
    public float Length(BoardRect board) => board.H > 0f ? Height * board.W / board.H : 0f;

    /// <summary>Metres on the wall per plate pixel of <paramref name="board"/>.</summary>
    public float MetresPerPixel(BoardRect board) => board.H > 0f ? Height / board.H : 0f;

    /// <summary>Where the plate pixel <paramref name="p"/> lies on the wall (the painted desk's frame): the board's left edge at the far end, its top at Top.</summary>
    public (float x, float y, float z) At(BoardRect board, BoardPoint p)
    {
        float s = board.W > 0f ? (p.X - board.X) / board.W : 0f;
        float t = board.H > 0f ? (p.Y - board.Y) / board.H : 0f;
        return (Face, Far - s * Length(board), Top - t * Height);
    }
}
