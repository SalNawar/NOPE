using System;
using System.Collections.Generic;

/// <summary>
/// One nation's passport as world_source.json countries[].passport authors it
/// (the travel documents spec, TD3): the cover's colour, the emblem
/// (EmblemShapes) and the three-letter code printed under the emblem.
/// </summary>
[Serializable]
public sealed class PassportLook
{
    /// <summary>The cover's colour ("#RRGGBB"): the open booklet's edge round its pages and the emblem's ink.</summary>
    public string cover = string.Empty;

    /// <summary>The emblem's name (EmblemShapes.Names).</summary>
    public string emblem = string.Empty;

    /// <summary>The nation's three-letter code ("EGY"), printed under the passport's emblem (its issuing nation, as Papers, Please's passports print theirs).</summary>
    public string code = string.Empty;
}

/// <summary>
/// The passports' content check (the travel documents spec, TD3), run by
/// Generate World and the validator: every country's cover is "#RRGGBB" and
/// dark enough to ink its emblem on a passport page, its emblem is drawn, its
/// code is three capitals, and no two countries share a cover, an emblem or a
/// code (a passport is told by its cover). Pure, so it is tested headless.
/// </summary>
public static class PassportCovers
{
    /// <summary>The least contrast of a cover against a passport page (the emblem is inked in it): the outline minimum.</summary>
    public const double EmblemContrast = 3.0;

    /// <summary>Every problem of the countries' passports (<paramref name="countries"/>: each country's id and look; a null look is a problem), each against <paramref name="page"/>, the passport page's colour; empty when sound.</summary>
    public static List<string> Problems(IEnumerable<(string id, PassportLook look)> countries, Rgba page)
    {
        var problems = new List<string>();
        var covers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var emblems = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var codes = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach ((string id, PassportLook look) in countries ?? Array.Empty<(string, PassportLook)>())
        {
            if (look == null)
            {
                problems.Add($"Country '{id}' has no passport (cover, emblem, code).");
                continue;
            }
            if (!Rgba.TryParseHex(look.cover, out Rgba cover))
                problems.Add($"Country '{id}': its passport cover '{look.cover}' is not #RRGGBB.");
            else if (Contrast.Ratio(cover.WithAlpha(1f), page.WithAlpha(1f)) + 1e-6 < EmblemContrast)
                problems.Add($"Country '{id}': its passport cover {look.cover} is too light for its emblem on the page (under {EmblemContrast:0.0}:1).");
            if (!EmblemShapes.Has(look.emblem))
                problems.Add($"Country '{id}': its passport emblem '{look.emblem}' is not one of {string.Join(", ", EmblemShapes.Names)}.");
            if (look.code == null || look.code.Length != 3 || !IsCapitals(look.code))
                problems.Add($"Country '{id}': its passport code '{look.code}' is not three capitals.");
            Unique(covers, look.cover, id, "cover", problems);
            Unique(emblems, look.emblem, id, "emblem", problems);
            Unique(codes, look.code, id, "code", problems);
        }
        return problems;
    }

    private static bool IsCapitals(string text)
    {
        foreach (char c in text)
            if (c < 'A' || c > 'Z')
                return false;
        return true;
    }

    private static void Unique(Dictionary<string, string> seen, string value, string id, string what, List<string> problems)
    {
        if (string.IsNullOrEmpty(value))
            return;
        if (seen.TryGetValue(value, out string first))
            problems.Add($"Countries '{first}' and '{id}' share the passport {what} '{value}'.");
        else
            seen[value] = id;
    }
}

/// <summary>Where a booklet's cover is (BookletFlip).</summary>
public enum CoverState
{
    /// <summary>Shut: the cover lies over the data page, the visa page folded under it.</summary>
    Closed,

    /// <summary>Swinging open about the spine.</summary>
    Opening,

    /// <summary>Open flat: both pages show.</summary>
    Open,

    /// <summary>Swinging shut.</summary>
    Closing
}

/// <summary>
/// A booklet's or a folder's cover opening and closing (Saleh's D3: "yes and
/// other documents other than solid id cards should feel a bit like paper";
/// FIX_PLAN DOC-LOOK: the passport arrives closed and opens with a page flip,
/// the folder opens to its tabbed sheet; GAME_FEEL_BIBLE A1: the cover lifts
/// 15 degrees over 40 ms, then swings open in 160 ms with an OutBack
/// overshoot, and shuts the same way back). The cover's angle about the
/// spine: 0 shut over the page it covers, 180 lying open beside it. The cover
/// is hinged on a surface at both ends, so the overshoot never passes through
/// it: where OutBack would carry the cover past open (or past shut) it
/// rebounds off the surface instead, toward the viewer (Swing), and the angle
/// stays in [0, 180]. Turning back mid-swing starts from the angle the cover
/// is at, with no lift (it is already up), over a time in proportion to the
/// way left (as fast as a swing from rest), so the cover never jumps. Its
/// free edge bows behind the swing (Flex), eased so a turn back never flips
/// the bow in one frame. Pure: PaperCover and FolderCover drive it on
/// unscaled time, and under Reduced Motion it jumps (instant), unbowed.
/// </summary>
public sealed class BookletFlip
{
    /// <summary>The anticipation: the cover lifts this far (degrees) before it swings.</summary>
    public const float LiftDegrees = 15f;

    /// <summary>The anticipation's and the swing's durations (seconds).</summary>
    public const float LiftSeconds = 0.04f, SwingSeconds = 0.16f;

    /// <summary>The swing's OutBack overshoot (the classic 1.70158), folded back off the surface the cover lands on.</summary>
    public const float Overshoot = 1.70158f;

    /// <summary>The cover's angles shut and open (degrees about the spine).</summary>
    public const float ShutDegrees = 0f, OpenDegrees = 180f;

    /// <summary>The least share of a whole swing's time a turn back takes (a cover turned back a hair from where it started still swings, never snaps).</summary>
    public const float LeastShare = 0.25f;

    /// <summary>The swing's angular speed at which its free edge bows fully (degrees a second): a whole swing's mean, the lift aside.</summary>
    public const float FullBowSpeed = (OpenDegrees - ShutDegrees - LiftDegrees) / SwingSeconds;

    /// <summary>The most the bow (Flex) changes in a second, as a share of its most: it turns from one way's most to the other's in no less than 2 / FlexRate seconds (67 ms; at 60 frames a second the passport's free edge moves at most 1.5 mm a frame), so a turn back never pops the edge across (DOC-LOOK review: the bow's sign used to flip in one frame, a 6 mm pop on the passport).</summary>
    public const float FlexRate = 30f;

    /// <summary>Where the cover is.</summary>
    public CoverState State { get; private set; } = CoverState.Closed;

    /// <summary>How far through its open or close (0 to 1).</summary>
    public float Progress { get; private set; }

    /// <summary>True while it shows open or opening (its pages are read).</summary>
    public bool IsOpenOrOpening => State == CoverState.Open || State == CoverState.Opening;

    /// <summary>True while the cover swings.</summary>
    public bool Moving => State == CoverState.Opening || State == CoverState.Closing;

    /// <summary>
    /// How much the swinging cover's free edge bows behind its swing
    /// (GAME_FEEL_BIBLE A1: a page bow of 2 to 4 mm on the flip), as a share
    /// of the most (-1 to 1). Each Tick it follows the sine of the cover's
    /// angle times its angular speed over FullBowSpeed (at most all of it),
    /// signed by the way the cover turns (positive toward open), so the edge
    /// trails the swing and its rebound off the desk alike; it never changes
    /// faster than FlexRate (a turn back eases it through flat, never a flip
    /// in one frame) and it settles flat once the cover rests.
    /// </summary>
    public float Flex { get; private set; }

    /// <summary>The angle the swing under way started from (degrees).</summary>
    private float _from;

    /// <summary>True when the swing under way started from rest (it lifts first).</summary>
    private bool _lift;

    /// <summary>How long the swing under way lasts (seconds).</summary>
    private float _seconds = LiftSeconds + SwingSeconds;

    /// <summary>Starts opening (nothing when open or opening); <paramref name="instant"/> (Reduced Motion) opens at once. True when it started to open.</summary>
    public bool Open(bool instant = false) => Go(CoverState.Opening, CoverState.Open, OpenDegrees, instant);

    /// <summary>Starts closing (nothing when shut or closing); <paramref name="instant"/> shuts at once. True when it started to close.</summary>
    public bool Close(bool instant = false) => Go(CoverState.Closing, CoverState.Closed, ShutDegrees, instant);

    /// <summary>Shut at once, with no swing and no bow (a booklet arriving, a folder at the day's start).</summary>
    public void Shut()
    {
        State = CoverState.Closed;
        Progress = 0f;
        Flex = 0f;
    }

    /// <summary>Advances a swing and its bow (Flex) by <paramref name="seconds"/>; true while the cover moved or its bow settled this step (false at rest, flat).</summary>
    public bool Tick(float seconds)
    {
        if (!Moving && Flex == 0f)
            return false;
        seconds = Math.Max(0f, seconds);
        float before = Angle;
        if (Moving)
        {
            Progress = Math.Min(1f, Progress + seconds / Math.Max(_seconds, 1e-4f));
            if (Progress >= 1f)
            {
                State = State == CoverState.Opening ? CoverState.Open : CoverState.Closed;
                Progress = 0f;
            }
        }
        float after = Angle;
        float speed = seconds > 0f ? (after - before) / seconds : 0f;
        float trail = (float)Math.Sin(after * Math.PI / 180.0) * Math.Max(-1f, Math.Min(1f, speed / FullBowSpeed));
        float most = FlexRate * seconds;
        Flex += Math.Max(-most, Math.Min(most, trail - Flex));
        return true;
    }

    /// <summary>The cover's angle about the spine now (degrees: 0 shut, 180 open; always between them).</summary>
    public float Angle
    {
        get
        {
            switch (State)
            {
                case CoverState.Open:
                    return OpenDegrees;
                case CoverState.Opening:
                    return Swing(_from, OpenDegrees, Progress, _lift);
                case CoverState.Closing:
                    return Swing(_from, ShutDegrees, Progress, _lift);
                default:
                    return ShutDegrees;
            }
        }
    }

    /// <summary>
    /// The cover's angle <paramref name="t"/> (0 to 1) of the way through a
    /// swing from <paramref name="from"/> to <paramref name="to"/> degrees:
    /// from rest (<paramref name="lift"/>) it first lifts LiftDegrees toward
    /// <paramref name="to"/> over LiftSeconds' share of the time, then swings
    /// the rest of the way in OutBack, its overshoot folded back off the
    /// surface at <paramref name="to"/> (a rebound toward the viewer, never
    /// through the paper or the desk); never outside the two angles.
    /// </summary>
    public static float Swing(float from, float to, float t, bool lift)
    {
        t = t < 0f ? 0f : t > 1f ? 1f : t;
        float start = from;
        if (lift)
        {
            float share = LiftSeconds / (LiftSeconds + SwingSeconds);
            float raised = from + Math.Sign(to - from) * Math.Min(LiftDegrees, Math.Abs(to - from));
            if (t <= share)
                return from + (raised - from) * (share > 0f ? t / share : 1f);
            start = raised;
            t = (t - share) / (1f - share);
        }
        float u = t - 1f;
        float back = 1f + (Overshoot + 1f) * u * u * u + Overshoot * u * u;
        if (back > 1f)
            back = 2f - back; // the overshoot rebounds off the surface the cover lands on
        return start + (to - start) * back;
    }

    private bool Go(CoverState moving, CoverState done, float target, bool instant)
    {
        if (State == moving || State == done)
            return false;
        // a turn back mid-swing starts from where the cover is, already up (no lift), timed by the way left
        bool fromRest = !Moving;
        float from = Angle;
        State = instant ? done : moving;
        Progress = 0f;
        if (instant)
            Flex = 0f; // Reduced Motion: no swing, no bow
        _from = from;
        _lift = fromRest;
        _seconds = fromRest ? LiftSeconds + SwingSeconds
            : SwingSeconds * Math.Max(LeastShare, Math.Abs(target - from) / (OpenDegrees - ShutDegrees - LiftDegrees));
        return true;
    }
}

/// <summary>
/// A shut booklet's or folder's click box (DOC-LOOK review, S3: shut, a
/// passport and the folder kept their whole boxes, so the half hidden under
/// the cover, where only bare counter or desk shows, still took the hover,
/// the click, the press that opens it and the drag): the part of the box's
/// span along one axis on the shown side of the spine. DeskDocument.ShowShut
/// keeps a shut passport's sheet box under its spine, DeskRulebook.ShowShut
/// the shut folder's card box on the side of its board. Pure.
/// </summary>
public static class ShutBox
{
    /// <summary>The span centred on <paramref name="centre"/>, <paramref name="size"/> long, cut at <paramref name="spine"/> and kept under it (<paramref name="below"/>) or over it: its centre and size (a spine outside the span keeps all of it or none).</summary>
    public static (float centre, float size) Keep(float centre, float size, float spine, bool below)
    {
        float low = centre - Math.Abs(size) / 2f, high = centre + Math.Abs(size) / 2f;
        float cut = Math.Min(high, Math.Max(low, spine));
        if (below)
            high = cut;
        else
            low = cut;
        return ((low + high) / 2f, high - low);
    }
}

/// <summary>
/// A cover's board bowing on its swing (the passport's front, the folder's
/// board: PaperCover.Bow) and a flat mark printed on it (the passport's
/// emblem, the folder's seal) riding that bow, as a paper's prints ride its
/// bend (DOC-LOOK re-verify, wave 2 point 8: shutting, the board rose up to
/// 0.25 mm under the seal and 0.7 mm under the emblem, more than their 0.05
/// and 0.1 mm gaps, so it came up through them). Pure, so the ride's
/// arithmetic is tested headless (DocLookTests.CoverBow_*); PaperCover.Ride
/// only puts its numbers on the mark's transform.
/// </summary>
public static class CoverBow
{
    /// <summary>A cover sheet's bow as DocumentCel draws it (PaperCover.Bow's _Flex) and its marks ride it: its travel points at the hinge, away from the free edge (<paramref name="freeX"/>, <paramref name="freeY"/>, in the sheet's own plane), and its full lag keeps the hinge end flat and curls the free edge twice <paramref name="bow"/> metres (along the sheet's own +z: positive away from its front).</summary>
    public static PaperBend Of(float freeX, float freeY, float bow) => new PaperBend(-freeX, -freeY, bow, 1f, 0f, 0f, 0f, 0f);

    /// <summary>
    /// Where a flat mark printed on a cover's sheet lies on the sheet's
    /// <paramref name="bend"/> (Of). The mark is centred at
    /// (<paramref name="markX"/>, <paramref name="markY"/>) on the flat sheet,
    /// <paramref name="halfWidth"/> by <paramref name="halfHeight"/> metres
    /// each way; the sheet is centred at (<paramref name="sheetX"/>,
    /// <paramref name="sheetY"/>) and <paramref name="sheetWidth"/> by
    /// <paramref name="sheetHeight"/> metres (its scale, the sign ignored),
    /// all in the space the two share (the hinge's). Returns how far to move
    /// the mark from its rest along that space's z (<c>depth</c>, metres: the
    /// face's depth under the mark's centre less how far the curl rises past
    /// the mark, PaperBend.Bulge, so the bowing board never comes up through
    /// it) and the turn that lays it on the face's slope there, in degrees as
    /// Unity's Euler angles take them (<c>pitch</c> about its x, <c>yaw</c>
    /// about its y: the sheet's +z runs away from the viewer, so a face
    /// sloping deeper up the page tips the mark's top away, and a face
    /// sloping deeper to the right turns the mark's right edge away). Flat,
    /// all three are 0 and the mark lies at rest.
    /// </summary>
    public static (float depth, float pitch, float yaw) Ride(float markX, float markY, float halfWidth, float halfHeight,
                                                             float sheetX, float sheetY, float sheetWidth, float sheetHeight, PaperBend bend)
    {
        float w = Math.Max(Math.Abs(sheetWidth), 1e-4f), h = Math.Max(Math.Abs(sheetHeight), 1e-4f);
        float x = (markX - sheetX) * 2f / w, y = (markY - sheetY) * 2f / h;
        float drop = bend.Depth(x, y, w, h);
        (float dx, float dy) = bend.Slope(x, y, w, h);
        float bulge = bend.Bulge(x, y, halfWidth, halfHeight, w, h);
        const double degrees = 180.0 / Math.PI;
        return (drop - bulge, (float)(Math.Atan(dy) * degrees), (float)(-Math.Atan(dx) * degrees));
    }
}
