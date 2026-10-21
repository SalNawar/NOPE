using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

/// <summary>
/// The desk's plan (the desk layout spec, docs/superpowers/specs/2026-10-18-desk-layout-design.md;
/// the desk home map Saleh approved on 2026-10-20, DeskHomeMap): every home
/// is a yaw-aware rectangle in the desk's frame (metres right of and away
/// from the chair from the mat's centre, DeskRect; Footprint). The homes
/// must lie on the desk, off the paper mat, clear of each other and of the
/// fixed art, slide in without reaching any of them, and stand where the
/// office camera sees them (HomeProblems, the builder's gate, which fails the
/// build; SightBlocked, VisibleShare: a line-of-sight check over the boxes'
/// heights), with every two shown objects' click boxes apart
/// (ClickBoxProblems; ClickBoxClashes judges the real boxes in the QA rig:
/// BUG-4a). A decoration starts in the decor zone, clear of the others
/// (TrySettle), and the player may let it go on any free spot off the mat:
/// it rests where it was let go, nudged a few centimetres at most, or goes
/// back (TrySettleFree). Where the player put each decoration is saved as
/// text (FormatPlaces, ParsePlaces: "id:x,y;..." in the desk's frame). Pure;
/// tested headless; DeskDecor and the builder apply it.
/// </summary>
public static class DeskArrangement
{
    /// <summary>True when <paramref name="a"/> and <paramref name="b"/> share more than an edge.</summary>
    public static bool Overlap(DeskRect a, DeskRect b) =>
        Math.Abs(a.CentreX - b.CentreX) * 2f < a.Width + b.Width - Epsilon &&
        Math.Abs(a.CentreY - b.CentreY) * 2f < a.Height + b.Height - Epsilon;

    /// <summary>True when <paramref name="item"/> lies wholly inside <paramref name="zone"/> (its edges may touch).</summary>
    public static bool Holds(DeskRect zone, DeskRect item) =>
        item.CentreX - item.Width / 2f >= zone.CentreX - zone.Width / 2f - Epsilon &&
        item.CentreX + item.Width / 2f <= zone.CentreX + zone.Width / 2f + Epsilon &&
        item.CentreY - item.Height / 2f >= zone.CentreY - zone.Height / 2f - Epsilon &&
        item.CentreY + item.Height / 2f <= zone.CentreY + zone.Height / 2f + Epsilon;

    /// <summary>
    /// Where a decoration <paramref name="width"/> by <paramref name="depth"/>
    /// metres comes to rest when let go at (<paramref name="wantX"/>,
    /// <paramref name="wantY"/>): its centre at the nearest point to that from
    /// which it lies wholly inside one of <paramref name="zones"/> and
    /// overlaps none of <paramref name="taken"/> (the other decorations),
    /// tried on a grid of <paramref name="step"/> metres from the clamped
    /// point; false (nowhere) when no zone has room for it.
    /// </summary>
    public static bool TrySettle(float width, float depth, float wantX, float wantY, IReadOnlyList<DeskRect> zones, IReadOnlyList<DeskRect> taken,
                                 float step, out float x, out float y)
    {
        x = wantX;
        y = wantY;
        if (zones == null || step <= 0f)
            return false;
        float best = float.MaxValue;
        bool found = false;
        foreach (DeskRect zone in zones)
        {
            float roomX = zone.Width - width, roomY = zone.Height - depth;
            if (roomX < -Epsilon || roomY < -Epsilon)
                continue;
            // The centres the decoration may take in this zone.
            var centres = new DeskRect(zone.CentreX, zone.CentreY, Math.Max(0f, roomX), Math.Max(0f, roomY));
            (float cx, float cy) = centres.Clamp(wantX, wantY);
            int across = (int)Math.Ceiling(centres.Width / step), down = (int)Math.Ceiling(centres.Height / step);
            for (int i = -across; i <= across; i++)
                for (int j = -down; j <= down; j++)
                {
                    (float px, float py) = centres.Clamp(cx + i * step, cy + j * step);
                    float d = (px - wantX) * (px - wantX) + (py - wantY) * (py - wantY);
                    if (d >= best || !Clear(new DeskRect(px, py, width, depth), taken))
                        continue;
                    best = d;
                    x = px;
                    y = py;
                    found = true;
                }
        }
        return found;
    }

    /// <summary>The rectangle an object <paramref name="width"/> by <paramref name="depth"/> metres, centred at (<paramref name="x"/>, <paramref name="y"/>) and turned <paramref name="yaw"/> degrees, covers along the desk's axes (yaw-aware: a rack turned 90 degrees covers its depth across).</summary>
    public static DeskRect Footprint(float x, float y, float width, float depth, float yaw)
    {
        double a = yaw * Math.PI / 180.0;
        float c = (float)Math.Abs(Math.Cos(a)), s = (float)Math.Abs(Math.Sin(a));
        return new DeskRect(x, y, width * c + depth * s, width * s + depth * c);
    }

    /// <summary>
    /// What is wrong with the desk home map (one line each; empty when it
    /// holds): a home without an id or an id with two homes; a home off
    /// <paramref name="desk"/>, on the <paramref name="mat"/> (papers only),
    /// over another home or over the <paramref name="fixedArt"/>; a machine
    /// whose slide-in (its <paramref name="travel"/> sideways, the desk's
    /// slide data, else <paramref name="slideReach"/> metres) would reach the
    /// mat, another home or the fixed art, or that slides further than
    /// <paramref name="slideReach"/>; a shown home (DeskHome.Shown) the office
    /// camera at <paramref name="eye"/> sees less than
    /// <paramref name="minVisible"/> of past the other shown homes and the
    /// fixed art.
    /// </summary>
    public static List<string> HomeProblems(IReadOnlyList<DeskHome> homes, IReadOnlyList<DeskHome> fixedArt, DeskRect mat, DeskRect desk, float slideReach,
                                            (float x, float y, float h) eye, float minVisible, IReadOnlyDictionary<string, float> travel = null)
    {
        var problems = new List<string>();
        if (homes == null)
        {
            problems.Add("The plan has no homes.");
            return problems;
        }
        IReadOnlyList<DeskHome> art = fixedArt ?? Array.Empty<DeskHome>();
        var seen = new HashSet<string>();
        foreach (DeskHome home in homes)
        {
            if (home == null || string.IsNullOrEmpty(home.id))
            {
                problems.Add("A home has no id.");
                continue;
            }
            if (!seen.Add(home.id))
                problems.Add($"The id '{home.id}' has two homes.");
            DeskRect f = home.Footprint;
            if (!Holds(desk, f))
                problems.Add($"The home '{home.id}' runs off the desk.");
            if (Overlap(f, mat))
                problems.Add($"The home '{home.id}' lies on the paper mat.");
            foreach (DeskHome fixedPiece in art)
                if (fixedPiece != null && Overlap(f, fixedPiece.Footprint))
                    problems.Add($"The home '{home.id}' lies on the fixed art '{fixedPiece.id}'.");
            if (home.slide == HomeSlide.None)
                continue;
            float reach = slideReach;
            if (travel != null && travel.TryGetValue(home.id, out float slides))
            {
                if (slides > slideReach + Epsilon)
                    problems.Add($"The home '{home.id}' slides {slides * 100f:0.0} cm, further than {slideReach * 100f:0.0} cm.");
                reach = slides;
            }
            DeskRect swept = home.Swept(reach);
            if (Overlap(swept, mat))
                problems.Add($"The home '{home.id}' slides onto the paper mat.");
            foreach (DeskHome other in homes)
                if (other != null && other != home && Overlap(swept, other.Footprint))
                    problems.Add($"The home '{home.id}' slides into '{other.id}'.");
            foreach (DeskHome fixedPiece in art)
                if (fixedPiece != null && Overlap(swept, fixedPiece.Footprint))
                    problems.Add($"The home '{home.id}' slides into the fixed art '{fixedPiece.id}'.");
        }
        for (int i = 0; i < homes.Count; i++)
            for (int j = i + 1; j < homes.Count; j++)
                if (homes[i] != null && homes[j] != null && Overlap(homes[i].Footprint, homes[j].Footprint))
                    problems.Add($"The homes '{homes[i].id}' and '{homes[j].id}' overlap.");
        var blockers = new List<DeskHome>(art);
        foreach (DeskHome home in homes)
            if (home != null && home.Shown)
                blockers.Add(home);
        foreach (DeskHome home in homes)
        {
            if (home == null || !home.Shown)
                continue;
            float share = VisibleShare(home, eye, blockers);
            if (share < minVisible)
                problems.Add($"The office camera sees {share * 100f:0}% of '{home.id}' (under {minVisible * 100f:0}%).");
        }
        return problems;
    }

    /// <summary>
    /// Where two shown objects' click boxes would overlap (one line each;
    /// empty when none do; BUG-4a: "fails the build on ... overlapping click
    /// boxes"): each shown home's box is its footprint grown by
    /// <paramref name="pad"/> on every side (the builder's box hugs the mesh
    /// fitted to the home, plus its pad), except an art object the plan moves
    /// (DeskHome.artParts: its box is the art's bounds, fitted to the home)
    /// and the <paramref name="fixedArt"/> (the art's bounds); a home in
    /// <paramref name="noClickBox"/> has none. Pairs whose footprints already
    /// overlap are HomeProblems' and left out here.
    /// </summary>
    public static List<string> ClickBoxProblems(IReadOnlyList<DeskHome> homes, IReadOnlyList<DeskHome> fixedArt, float pad, IReadOnlyCollection<string> noClickBox)
    {
        var problems = new List<string>();
        var boxes = new List<(DeskHome home, DeskRect box)>();
        var unclicked = new HashSet<string>(noClickBox ?? Array.Empty<string>());
        foreach (DeskHome home in homes ?? Array.Empty<DeskHome>())
            if (home != null && home.Shown && !unclicked.Contains(home.id))
            {
                DeskRect f = home.Footprint;
                float grow = string.IsNullOrEmpty(home.artParts) ? 2f * pad : 0f;
                boxes.Add((home, new DeskRect(f.CentreX, f.CentreY, f.Width + grow, f.Height + grow)));
            }
        int shown = boxes.Count;
        foreach (DeskHome art in fixedArt ?? Array.Empty<DeskHome>())
            if (art != null)
                boxes.Add((art, art.Footprint));
        for (int i = 0; i < shown; i++)
            for (int j = i + 1; j < boxes.Count; j++)
                if (Overlap(boxes[i].box, boxes[j].box) && !Overlap(boxes[i].home.Footprint, boxes[j].home.Footprint))
                    problems.Add($"The click boxes of '{boxes[i].home.id}' and '{boxes[j].home.id}' overlap ({pad * 100f:0.#} cm pad).");
        return problems;
    }

    /// <summary>
    /// The real click boxes that clash (the QA rig's deskclickboxes op, BUG-4a;
    /// one line each, empty when none do): every pair of
    /// <paramref name="names"/> whose <paramref name="owners"/> differ (one
    /// desk object's own boxes, such as the RING SERVICE breakers, never
    /// count against each other; a null owner is the box's own) and whose
    /// boxes go into each other deeper than <paramref name="tolerance"/>
    /// metres (<paramref name="depth"/>: the penetration depth, 0 when apart).
    /// </summary>
    public static List<string> ClickBoxClashes(IReadOnlyList<string> names, IReadOnlyList<string> owners, Func<int, int, float> depth, float tolerance)
    {
        var clashes = new List<string>();
        int n = names != null ? names.Count : 0;
        if (depth == null)
            return clashes;
        for (int i = 0; i < n; i++)
            for (int j = i + 1; j < n; j++)
            {
                string a = owners != null && i < owners.Count ? owners[i] : null, b = owners != null && j < owners.Count ? owners[j] : null;
                if (a != null && a == b)
                    continue;
                float d = depth(i, j);
                if (d > tolerance)
                    clashes.Add($"'{names[i]}' and '{names[j]}' overlap by {d * 1000f:0} mm.");
            }
        return clashes;
    }

    /// <summary>
    /// How much of <paramref name="target"/>'s top the eye at <paramref name="eye"/>
    /// (metres in the frame, metres over the desk) sees past <paramref name="blockers"/>
    /// (each a box: its footprint, its height): the share of a 5 x 5 grid of
    /// points on the top face whose line of sight crosses no other box. Pure
    /// geometry, the plan's quick check; the ID-buffer render in Unity is the measure.
    /// </summary>
    public static float VisibleShare(DeskHome target, (float x, float y, float h) eye, IReadOnlyList<DeskHome> blockers)
    {
        if (target == null)
            return 0f;
        DeskRect f = target.Footprint;
        int seen = 0, all = 0;
        for (int i = 0; i < Grid; i++)
            for (int j = 0; j < Grid; j++)
            {
                (float px, float py) = f.PointAt((i + 0.5f) / Grid, (j + 0.5f) / Grid);
                all++;
                if (!SightBlocked(eye, (px, py, target.height), target, blockers))
                    seen++;
            }
        return (float)seen / all;
    }

    /// <summary>True when the straight line from <paramref name="eye"/> to <paramref name="point"/> (frame metres, height over the desk) passes through any of <paramref name="blockers"/>' boxes other than <paramref name="self"/>'s.</summary>
    public static bool SightBlocked((float x, float y, float h) eye, (float x, float y, float h) point, DeskHome self, IReadOnlyList<DeskHome> blockers)
    {
        foreach (DeskHome box in blockers ?? Array.Empty<DeskHome>())
        {
            if (box == null || box == self || box.height <= 0f)
                continue;
            DeskRect f = box.Footprint;
            if (SegmentHitsBox(eye, point, f.CentreX - f.Width / 2f, f.CentreX + f.Width / 2f, f.CentreY - f.Height / 2f, f.CentreY + f.Height / 2f, box.height))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Where a decoration <paramref name="width"/> by <paramref name="depth"/>
    /// metres comes to rest when let go at (<paramref name="wantX"/>,
    /// <paramref name="wantY"/>) anywhere free (BUG-23: decor rests where it
    /// is released on any free non-mat area, nudged at most a few cm): the
    /// nearest centre at most <paramref name="maxNudge"/> metres from the
    /// release, on a <paramref name="step"/> grid around it, from which it
    /// lies wholly on <paramref name="desk"/> and clear of every
    /// <paramref name="blocked"/> rectangle (the mat, the homes, the fixed
    /// art, the other decorations); false (it goes back) when there is none.
    /// </summary>
    public static bool TrySettleFree(float width, float depth, float wantX, float wantY, DeskRect desk, IReadOnlyList<DeskRect> blocked,
                                     float maxNudge, float step, out float x, out float y)
    {
        x = wantX;
        y = wantY;
        if (step <= 0f || maxNudge < 0f)
            return false;
        int reach = (int)Math.Floor(maxNudge / step + 1e-4f);
        float limit = maxNudge * maxNudge + 1e-8f, best = float.MaxValue;
        bool found = false;
        for (int i = -reach; i <= reach; i++)
            for (int j = -reach; j <= reach; j++)
            {
                float px = wantX + i * step, py = wantY + j * step;
                float d = (px - wantX) * (px - wantX) + (py - wantY) * (py - wantY);
                if (d > limit || d >= best)
                    continue;
                var item = new DeskRect(px, py, width, depth);
                if (!Holds(desk, item) || !Clear(item, blocked))
                    continue;
                best = d;
                x = px;
                y = py;
                found = true;
            }
        return found;
    }

    /// <summary>The decorations' places as saved text: "id:x,y" for each, joined by ';' (invariant culture, millimetres kept).</summary>
    public static string FormatPlaces(IEnumerable<KeyValuePair<string, (float x, float y)>> places)
    {
        var text = new StringBuilder();
        if (places == null)
            return string.Empty;
        foreach (KeyValuePair<string, (float x, float y)> place in places)
        {
            if (string.IsNullOrEmpty(place.Key) || place.Key.IndexOfAny(new[] { ':', ';', ',' }) >= 0)
                continue;
            if (text.Length > 0)
                text.Append(';');
            text.Append(place.Key).Append(':')
                .Append(place.Value.x.ToString("0.###", CultureInfo.InvariantCulture)).Append(',')
                .Append(place.Value.y.ToString("0.###", CultureInfo.InvariantCulture));
        }
        return text.ToString();
    }

    /// <summary>The places FormatPlaces wrote, by id; an entry that does not read (a missing part, a word for a number) is skipped, so a damaged save loses only that decoration's place.</summary>
    public static Dictionary<string, (float x, float y)> ParsePlaces(string text)
    {
        var places = new Dictionary<string, (float x, float y)>();
        if (string.IsNullOrEmpty(text))
            return places;
        foreach (string entry in text.Split(';'))
        {
            int colon = entry.IndexOf(':');
            if (colon <= 0)
                continue;
            string[] xy = entry.Substring(colon + 1).Split(',');
            if (xy.Length == 2 &&
                float.TryParse(xy[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x) &&
                float.TryParse(xy[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float y) &&
                !float.IsNaN(x) && !float.IsNaN(y) && !float.IsInfinity(x) && !float.IsInfinity(y))
                places[entry.Substring(0, colon)] = (x, y);
        }
        return places;
    }

    /// <summary>A millimetre's slack: rectangles that only touch neither overlap nor stick out.</summary>
    private const float Epsilon = 1e-3f;

    /// <summary>The sight check's grid on a top face (Grid x Grid points).</summary>
    private const int Grid = 5;

    /// <summary>True when the segment from <paramref name="a"/> to <paramref name="b"/> runs through the box standing on the desk (x0..x1, y0..y1, 0..<paramref name="top"/>); grazing a face counts as a miss.</summary>
    private static bool SegmentHitsBox((float x, float y, float h) a, (float x, float y, float h) b, float x0, float x1, float y0, float y1, float top)
    {
        float t0 = 0f, t1 = 1f;
        return Slab(a.x, b.x - a.x, x0, x1, ref t0, ref t1) && Slab(a.y, b.y - a.y, y0, y1, ref t0, ref t1) && Slab(a.h, b.h - a.h, 0f, top, ref t0, ref t1) &&
               t1 - t0 > 1e-4f;
    }

    /// <summary>Narrows [t0, t1] to where the line start + t * delta lies between min and max on one axis; false when nothing is left.</summary>
    private static bool Slab(float start, float delta, float min, float max, ref float t0, ref float t1)
    {
        if (Math.Abs(delta) < 1e-9f)
            return start > min && start < max;
        float ta = (min - start) / delta, tb = (max - start) / delta;
        if (ta > tb)
            (ta, tb) = (tb, ta);
        t0 = Math.Max(t0, ta);
        t1 = Math.Min(t1, tb);
        return t0 < t1;
    }

    private static bool Clear(DeskRect item, IReadOnlyList<DeskRect> taken)
    {
        if (taken == null)
            return true;
        foreach (DeskRect other in taken)
            if (Overlap(item, other))
                return false;
        return true;
    }
}
