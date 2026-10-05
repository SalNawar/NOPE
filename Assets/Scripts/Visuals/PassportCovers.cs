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
