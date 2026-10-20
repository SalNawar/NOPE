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
/// A passport booklet's cover opening and closing (Saleh's D3: "yes and other
/// documents other than solid id cards should feel a bit like paper"; FIX_PLAN
/// DOC-LOOK: the passport arrives closed and opens with a page flip;
/// GAME_FEEL_BIBLE A1: the cover lifts 15 degrees over 40 ms, then swings
/// open in 160 ms with an OutBack overshoot, and shuts the same way back).
/// The cover's angle about the spine: 0 shut over the data page, 180 lying
/// open where the visa page is. Pure: the desk's PaperCover drives it on
/// unscaled time, and under Reduced Motion it jumps (Instant).
/// </summary>
public sealed class BookletFlip
{
    /// <summary>The anticipation: the cover lifts this far (degrees) before it swings.</summary>
    public const float LiftDegrees = 15f;

    /// <summary>The anticipation's and the swing's durations (seconds).</summary>
    public const float LiftSeconds = 0.04f, SwingSeconds = 0.16f;

    /// <summary>The swing's OutBack overshoot (the classic 1.70158).</summary>
    public const float Overshoot = 1.70158f;

    /// <summary>Where the cover is.</summary>
    public CoverState State { get; private set; } = CoverState.Closed;

    /// <summary>How far through its open or close (0 to 1).</summary>
    public float Progress { get; private set; }

    /// <summary>True while it shows open or opening (its pages are read).</summary>
    public bool IsOpenOrOpening => State == CoverState.Open || State == CoverState.Opening;

    /// <summary>True while the cover swings.</summary>
    public bool Moving => State == CoverState.Opening || State == CoverState.Closing;

    /// <summary>Starts opening (nothing when open or opening); <paramref name="instant"/> (Reduced Motion) opens at once.</summary>
    public void Open(bool instant = false) => Go(CoverState.Opening, CoverState.Open, instant);

    /// <summary>Starts closing (nothing when shut or closing); <paramref name="instant"/> shuts at once.</summary>
    public void Close(bool instant = false) => Go(CoverState.Closing, CoverState.Closed, instant);

    /// <summary>Shut at once, with no swing (a booklet arriving).</summary>
    public void Shut()
    {
        State = CoverState.Closed;
        Progress = 0f;
    }

    /// <summary>Advances a swing by <paramref name="seconds"/>; true while it moved this step.</summary>
    public bool Tick(float seconds)
    {
        if (!Moving)
            return false;
        Progress = Math.Min(1f, Progress + Math.Max(0f, seconds) / (LiftSeconds + SwingSeconds));
        if (Progress >= 1f)
        {
            State = State == CoverState.Opening ? CoverState.Open : CoverState.Closed;
            Progress = 0f;
        }
        return true;
    }

    /// <summary>The cover's angle about the spine now (degrees: 0 shut, 180 open; past 180 in the overshoot).</summary>
    public float Angle
    {
        get
        {
            switch (State)
            {
                case CoverState.Open:
                    return 180f;
                case CoverState.Opening:
                    return Swing(Progress);
                case CoverState.Closing:
                    return 180f - Swing(Progress);
                default:
                    return 0f;
            }
        }
    }

    /// <summary>The angle swung at <paramref name="t"/> (0 to 1) through a swing: the lift, then the OutBack swing the rest of the way.</summary>
    public static float Swing(float t)
    {
        float lift = LiftSeconds / (LiftSeconds + SwingSeconds);
        if (t <= lift)
            return LiftDegrees * (lift > 0f ? t / lift : 1f);
        float u = (t - lift) / (1f - lift) - 1f;
        float back = 1f + (Overshoot + 1f) * u * u * u + Overshoot * u * u;
        return LiftDegrees + (180f - LiftDegrees) * back;
    }

    private void Go(CoverState moving, CoverState done, bool instant)
    {
        if (State == moving || State == done)
            return;
        // turning back mid-swing starts from where the cover is
        float from = Moving ? 1f - Progress : 0f;
        State = instant ? done : moving;
        Progress = instant ? 0f : from;
    }
}
