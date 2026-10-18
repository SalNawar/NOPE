using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

/// <summary>
/// The desk's plan (the desk layout spec, docs/superpowers/specs/2026-10-18-desk-layout-design.md;
/// Saleh's 1018a playtest: "every machine should have a space pre planed for
/// it ... the big machience and upgrades the player cant move but small
/// decorations he can"): every bay is a rectangle in the desk's frame (metres
/// right of and away from the chair from the mat's centre, DeskRect). The
/// bays must lie on the desk and never overlap (Problems, the builder's
/// check). A decoration the player drags settles inside one of the decor
/// zones, clear of every other decoration (Settle): the nearest such spot to
/// where it was let go, on a grid of <c>step</c> metres; none when no zone
/// has room. Where the player put each decoration is saved as text
/// (FormatPlaces, ParsePlaces: "id:x,y;..." in the desk's frame). Pure;
/// tested headless; DeskDecor applies it.
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

    /// <summary>
    /// What is wrong with the desk's bays (named <c>Key</c>): each that does
    /// not lie wholly on <paramref name="desk"/>, and each pair that overlaps,
    /// one line each; empty when the plan holds.
    /// </summary>
    public static List<string> Problems(IReadOnlyList<KeyValuePair<string, DeskRect>> bays, DeskRect desk)
    {
        var problems = new List<string>();
        if (bays == null)
            return problems;
        for (int i = 0; i < bays.Count; i++)
        {
            if (!Holds(desk, bays[i].Value))
                problems.Add($"The bay '{bays[i].Key}' runs off the desk.");
            for (int j = i + 1; j < bays.Count; j++)
                if (Overlap(bays[i].Value, bays[j].Value))
                    problems.Add($"The bays '{bays[i].Key}' and '{bays[j].Key}' overlap.");
        }
        return problems;
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
