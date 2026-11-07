using System;
using System.Collections.Generic;

/// <summary>What one glyph slot of a dial's inner ring carries (GateDial.Ring). Not serialized.</summary>
public enum GateGlyphKind
{
    /// <summary>The point of origin (the Bureau's hourglass): it locks nothing.</summary>
    Origin,

    /// <summary>A nation's emblem: under the top chevron it locks the PLACE connection.</summary>
    Nation,

    /// <summary>An era's glyph: under the second chevron it locks the PERIOD connection.</summary>
    Era,

    /// <summary>An empty slot (a ring padded to the chevrons' spacing).</summary>
    Blank
}

/// <summary>One glyph slot of a dial's inner ring: what it carries and which nation or era (the board's destination or period index). Not serialized.</summary>
public readonly struct GateGlyph
{
    /// <summary>What the slot carries.</summary>
    public readonly GateGlyphKind Kind;

    /// <summary>The nation's destination index or the era's period index (0 for the origin and a blank).</summary>
    public readonly int Index;

    /// <summary>A slot carrying <paramref name="kind"/> number <paramref name="index"/>.</summary>
    public GateGlyph(GateGlyphKind kind, int index)
    {
        Kind = kind;
        Index = index;
    }

    /// <summary>"nation 2", "era 0", "origin" (the log names a glyph by it).</summary>
    public override string ToString() => Kind == GateGlyphKind.Nation ? "nation " + Index : Kind == GateGlyphKind.Era ? "era " + Index : Kind.ToString().ToLowerInvariant();
}

/// <summary>What a ring let go of does to its dial's two locks (GateDial.Resolve). Not serialized.</summary>
public enum DialAction
{
    /// <summary>Nothing changes.</summary>
    None,

    /// <summary>The nation under the top chevron locks the PLACE connection (chevron 1 lights).</summary>
    LockPlace,

    /// <summary>The era under the second chevron locks the PERIOD connection (chevron 2 lights; a different era replaces the old one).</summary>
    LockPeriod,

    /// <summary>The ring moved on with no era under the second chevron: both locks open (the dial starts over).</summary>
    Reset
}

/// <summary>
/// The Stargate dialer's dial (Saleh 2026-11-07: "a switchboard that is
/// intresting ( my friends suggested something based on stargate)"; the
/// director's D1): a fixed outer ring of seven chevrons and an inner ring of
/// glyph slots the hand turns: the point of origin, the nations' emblems and
/// the eras' glyphs, interleaved, padded so a chevron stands every
/// <see cref="Spacing"/> slots (8 nations and 5 eras: 14 slots, a chevron every
/// 2), so every detent puts a glyph exactly under every chevron. Chevron 1
/// (the top) reads the PLACE connection, chevron 2 (the next clockwise) the
/// PERIOD; during the 20 s calibration the chevrons engage one by one. Angles
/// are degrees clockwise from the top of the plate (y down). Pure.
/// </summary>
public static class GateDial
{
    /// <summary>The chevrons round a dial (the strobe engages one every calibration / 7).</summary>
    public const int Chevrons = 7;

    /// <summary>The chevron that reads the PLACE connection (the top).</summary>
    public const int PlaceChevron = 0;

    /// <summary>The chevron that reads the PERIOD connection (the next clockwise).</summary>
    public const int PeriodChevron = 1;

    /// <summary>The ring's glyph slots for <paramref name="nations"/> nations and <paramref name="eras"/> eras, clockwise from slot 0: the origin, then a nation and an era in turn while both last, then the rest, then blanks up to a multiple of <see cref="Chevrons"/> (at least two slots a chevron).</summary>
    public static GateGlyph[] Ring(int nations, int eras)
    {
        nations = Math.Max(0, nations);
        eras = Math.Max(0, eras);
        var slots = new List<GateGlyph> { new GateGlyph(GateGlyphKind.Origin, 0) };
        int n = 0, e = 0;
        while (n < nations || e < eras)
        {
            if (n < nations)
                slots.Add(new GateGlyph(GateGlyphKind.Nation, n++));
            if (e < eras && (n > e || n >= nations))
                slots.Add(new GateGlyph(GateGlyphKind.Era, e++));
        }
        int size = Math.Max(2 * Chevrons, (slots.Count + Chevrons - 1) / Chevrons * Chevrons);
        while (slots.Count < size)
            slots.Add(new GateGlyph(GateGlyphKind.Blank, 0));
        return slots.ToArray();
    }

    /// <summary>The angle between two glyph slots of a ring of <paramref name="slots"/>.</summary>
    public static float SlotDegrees(int slots) => 360f / Math.Max(1, slots);

    /// <summary>Slots from one chevron to the next on a ring of <paramref name="slots"/>.</summary>
    public static int Spacing(int slots) => Math.Max(1, slots / Chevrons);

    /// <summary>Chevron <paramref name="chevron"/>'s angle (0 = the top).</summary>
    public static float ChevronAngle(int chevron) => chevron * 360f / Chevrons;

    /// <summary>Where slot <paramref name="slot"/> is drawn with the ring turned <paramref name="rotation"/> (0 to 360).</summary>
    public static float SlotAngle(int slot, float rotation, int slots) => Wrap360(slot * SlotDegrees(slots) + rotation);

    /// <summary>The slot nearest the angle <paramref name="angle"/> with the ring turned <paramref name="rotation"/>.</summary>
    public static int SlotUnder(float rotation, float angle, int slots)
    {
        slots = Math.Max(1, slots);
        int s = (int)MathF.Round((angle - rotation) / SlotDegrees(slots));
        return ((s % slots) + slots) % slots;
    }

    /// <summary>The slot under chevron <paramref name="chevron"/> with the ring at <paramref name="rotation"/>.</summary>
    public static int SlotUnderChevron(float rotation, int chevron, int slots) => SlotUnder(rotation, ChevronAngle(chevron), slots);

    /// <summary>The detent nearest <paramref name="rotation"/>: a glyph exactly under every chevron (not wrapped, so the ring eases the short way).</summary>
    public static float Detent(float rotation, int slots)
    {
        float step = SlotDegrees(slots);
        return MathF.Round(rotation / step) * step;
    }

    /// <summary>The rotation (0 to 360) that puts slot <paramref name="slot"/> under chevron <paramref name="chevron"/>.</summary>
    public static float RotationFor(int slot, int chevron, int slots) => Wrap360(ChevronAngle(chevron) - slot * SlotDegrees(slots));

    /// <summary>The slot carrying <paramref name="kind"/> number <paramref name="index"/> on <paramref name="ring"/>, or -1.</summary>
    public static int SlotOf(IReadOnlyList<GateGlyph> ring, GateGlyphKind kind, int index)
    {
        for (int s = 0; ring != null && s < ring.Count; s++)
            if (ring[s].Kind == kind && ring[s].Index == index)
                return s;
        return -1;
    }

    /// <summary>The angle of <paramref name="p"/> about <paramref name="centre"/> (0 to 360, clockwise from the top; the plate's y runs down).</summary>
    public static float AngleOf(BoardPoint centre, BoardPoint p) => Wrap360(MathF.Atan2(p.X - centre.X, centre.Y - p.Y) * 180f / MathF.PI);

    /// <summary>The short way from <paramref name="from"/> to <paramref name="to"/> (-180 to 180).</summary>
    public static float Delta(float from, float to)
    {
        float d = Wrap360(to - from);
        return d >= 180f ? d - 360f : d;
    }

    /// <summary><paramref name="degrees"/> in 0 to 360.</summary>
    public static float Wrap360(float degrees)
    {
        float w = degrees % 360f;
        return w < 0f ? w + 360f : w;
    }

    /// <summary>
    /// What a let-go ring does (the dialer's rule): with nothing locked, a
    /// nation under the top chevron (<paramref name="top"/>) locks PLACE; with
    /// PLACE locked, an era under the second chevron (<paramref name="second"/>)
    /// locks PERIOD (a different era replaces the old one, so a wrong era costs
    /// one drag), and a ring <paramref name="moved"/> to another detent with
    /// anything else there opens both locks (the dial starts over); a ring let
    /// go where it was picked up changes nothing it has not already locked.
    /// </summary>
    public static DialAction Resolve(bool placeLocked, bool moved, GateGlyph top, GateGlyph second)
    {
        if (!placeLocked)
            return top.Kind == GateGlyphKind.Nation ? DialAction.LockPlace : DialAction.None;
        if (second.Kind == GateGlyphKind.Era)
            return DialAction.LockPeriod;
        return moved ? DialAction.Reset : DialAction.None;
    }

    /// <summary>How many chevrons have engaged <paramref name="seconds"/> into a calibration of <paramref name="calibration"/> seconds: one every calibration / 7 (Saleh's 20 s: one every 2.86 s), all seven as it opens.</summary>
    public static int Engaged(float seconds, float calibration)
    {
        if (calibration <= 0f)
            return Chevrons;
        int n = (int)MathF.Floor(seconds / (calibration / Chevrons) + 1e-4f);
        return Math.Max(0, Math.Min(Chevrons, n));
    }

    /// <summary>Seconds since chevron <paramref name="chevron"/> engaged, <paramref name="seconds"/> into a calibration of <paramref name="calibration"/> (negative before).</summary>
    public static float SinceEngaged(int chevron, float seconds, float calibration) => seconds - (chevron + 1) * (calibration / Chevrons);
}

/// <summary>
/// Where the dialer's parts sit on the switchboard's plate (pixels, y down;
/// the plate is 1920 x 1080; the lo-fi mock of 2026-11-07): the slate cover
/// over the Classic board's jacks (MASTER's panel stays the plate's), the call
/// row of the nations (emblem, call lamp, era pips), the three big dials (01
/// to 03) with their cradles and displays, the cell rack, and the sealed
/// dials 04 and 05 under their placards. Pure.
/// </summary>
public sealed class GateBoardLayout
{
    /// <summary>The slate cover over the Classic board's jacks and modules.</summary>
    public BoardRect Cover { get; } = new BoardRect(300f, 84f, 1132f, 864f);

    /// <summary>The big dials (01 to 03); the modules after them are the small sealed ones.</summary>
    public const int BigDials = 3;

    /// <summary>The inner ring's glyph radius, a glyph's size, the hub's, the inner ring's and the outer ring's radii, and the chevrons' (a big dial; a sealed one scales by its radius).</summary>
    public const float GlyphRadius = 116f, GlyphSize = 34f, HubRadius = 86f, RingInner = 92f, RingOuter = 140f, OuterRing = 154f, ChevronRadius = 158f;

    /// <summary>A sealed dial's outer radius.</summary>
    public const float SealedRadius = 64f;

    /// <summary>A cradle slot's size and the gap between two.</summary>
    public const float SlotW = 42f, SlotH = 70f, SlotGap = 10f;

    /// <summary>A rack bay's size.</summary>
    public const float BayW = 48f, BayH = 88f;

    /// <summary>A cell's size (its canister).</summary>
    public const float CellW = 32f, CellH = 64f;

    private readonly int _nations;
    private readonly int _cells;
    private readonly int _slots;

    /// <summary>A layout for <paramref name="nations"/> call slots, <paramref name="cells"/> rack bays and <paramref name="cradleSlots"/> slots a cradle.</summary>
    public GateBoardLayout(int nations, int cells, int cradleSlots)
    {
        _nations = Math.Max(1, nations);
        _cells = Math.Max(0, cells);
        _slots = Math.Max(1, cradleSlots);
    }

    /// <summary>How many call slots (nations) the row has.</summary>
    public int Nations => _nations;

    /// <summary>How many slots a cradle has.</summary>
    public int CradleSlots => _slots;

    /// <summary>How many bays the rack has.</summary>
    public int Bays => _cells;

    // ------------------------------------------------------------------ the call row

    private float CallStep => Math.Min(141f, (Cover.W - 60f) / _nations);

    /// <summary>Call slot <paramref name="d"/>'s centre line (its x).</summary>
    public float CallX(int d) => Cover.X + Cover.W / 2f + (d - (_nations - 1) / 2f) * CallStep - 4f;

    /// <summary>Call slot <paramref name="d"/>'s plate.</summary>
    public BoardRect CallPlate(int d) => new BoardRect(CallX(d) - 62f, 96f, 124f, 132f);

    /// <summary>Call slot <paramref name="d"/>'s lamp.</summary>
    public BoardPoint CallLamp(int d) => new BoardPoint(CallX(d), 114f);

    /// <summary>The call lamp's glass radius.</summary>
    public const float CallLampGlass = 10f;

    /// <summary>Call slot <paramref name="d"/>'s emblem centre (its well's radius is 27).</summary>
    public BoardPoint CallEmblem(int d) => new BoardPoint(CallX(d), 160f);

    /// <summary>The emblem's size in the call row.</summary>
    public const float CallEmblemSize = 40f;

    /// <summary>Call slot <paramref name="d"/>'s pip for period <paramref name="period"/> of <paramref name="periods"/>.</summary>
    public BoardPoint CallPip(int d, int period, int periods) => new BoardPoint(CallX(d) + (period - (periods - 1) / 2f) * 22f, 208f);

    /// <summary>An era pip's size.</summary>
    public const float PipSize = 16f;

    // ------------------------------------------------------------------ the dials

    /// <summary>True when module <paramref name="m"/> is one of the big dials.</summary>
    public static bool Big(int m) => m >= 0 && m < BigDials;

    /// <summary>Dial <paramref name="m"/>'s centre (01 to 03 in a row; 04 and 05 small, bottom right).</summary>
    public BoardPoint DialCentre(int m) =>
        Big(m) ? new BoardPoint(510f + 356f * m, 468f) : new BoardPoint(1070f + 186f * (m - BigDials), 868f);

    /// <summary>Dial <paramref name="m"/>'s outer radius.</summary>
    public float DialRadius(int m) => Big(m) ? OuterRing : SealedRadius;

    /// <summary>How big dial <paramref name="m"/> draws against a big one.</summary>
    public float DialScale(int m) => DialRadius(m) / OuterRing;

    /// <summary>Chevron <paramref name="chevron"/>'s centre on dial <paramref name="m"/>.</summary>
    public BoardPoint ChevronAt(int m, int chevron)
    {
        BoardPoint c = DialCentre(m);
        float a = GateDial.ChevronAngle(chevron) * MathF.PI / 180f, r = ChevronRadius * DialScale(m);
        return new BoardPoint(c.X + r * MathF.Sin(a), c.Y - r * MathF.Cos(a));
    }

    /// <summary>True when <paramref name="p"/> is on dial <paramref name="m"/>'s inner ring (where the hand takes it; a little wider than the band).</summary>
    public bool OnRing(int m, BoardPoint p)
    {
        float d = DialCentre(m).DistanceTo(p), k = DialScale(m);
        return d >= (RingInner - 12f) * k && d <= (OuterRing + 6f) * k;
    }

    /// <summary>Dial <paramref name="m"/>'s display (its words: PULL, the countdown, OPEN, DONE, FUSE, CLOSED).</summary>
    public BoardPoint Display(int m) => Big(m) ? new BoardPoint(DialCentre(m).X, 778f) : DialCentre(m).Plus(0f, 0f);

    // ------------------------------------------------------------------ the cradles

    /// <summary>Dial <paramref name="m"/>'s cradle slot <paramref name="slot"/> (left first).</summary>
    public BoardRect CradleSlot(int m, int slot)
    {
        float x = DialCentre(m).X + (slot - (_slots - 1) / 2f) * (SlotW + SlotGap);
        return new BoardRect(x - SlotW / 2f, 706f - SlotH / 2f, SlotW, SlotH);
    }

    /// <summary>Dial <paramref name="m"/>'s whole cradle (where a dropped cell seats), grown by the hand's margin.</summary>
    public BoardRect Cradle(int m)
    {
        BoardRect a = CradleSlot(m, 0), b = CradleSlot(m, _slots - 1);
        return new BoardRect(a.X, a.Y, b.X + b.W - a.X, a.H).Grown(18f);
    }

    /// <summary>The era mark over dial <paramref name="m"/>'s cradle at <paramref name="bars"/> bars (the point of the brass arrow).</summary>
    public BoardPoint Mark(int m, int bars)
    {
        BoardRect slot = CradleSlot(m, Math.Max(0, Math.Min(_slots, bars) - 1));
        return new BoardPoint(slot.Centre.X, slot.Y - 8f);
    }

    // ------------------------------------------------------------------ the rack

    /// <summary>The cell rack.</summary>
    public BoardRect Rack => new BoardRect(330f, 808f, 470f, 124f);

    /// <summary>Rack bay <paramref name="cell"/>'s centre (a cell's home).</summary>
    public BoardPoint Bay(int cell)
    {
        float step = _cells <= 4 ? 100f : (Rack.W - 40f) / _cells;
        return new BoardPoint(Rack.X + Rack.W / 2f + (cell - (_cells - 1) / 2f) * step, Rack.Y + Rack.H / 2f);
    }
}
