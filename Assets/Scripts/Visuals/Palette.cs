using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>One entry of the shared role map (world_source.json ui.paletteMap): a role's fill and ink seed names, the fill's alpha and the text's contrast class.</summary>
[Serializable]
public sealed class PaletteRule
{
    /// <summary>The ThemeRoleId name.</summary>
    public string role;

    /// <summary>The fill's seed name (empty = no fill).</summary>
    public string fill;

    /// <summary>Multiplies the fill seed's alpha.</summary>
    public float alpha = 1f;

    /// <summary>The ink's seed name (empty = no ink).</summary>
    public string ink;

    /// <summary>How much contrast the ink needs on the fill.</summary>
    public ContrastClass textClass;
}

/// <summary>An exact colour for one role of one theme (hex; either part may be empty), applied after the map.</summary>
[Serializable]
public sealed class PaletteOverride
{
    /// <summary>The ThemeRoleId name.</summary>
    public string role;

    /// <summary>The fill as "#RRGGBB" or "#RRGGBBAA" (empty = keep the map's).</summary>
    public string fill;

    /// <summary>The ink as hex (empty = keep the map's).</summary>
    public string ink;
}

/// <summary>A role's resolved colours in one theme.</summary>
public sealed class ResolvedRole
{
    /// <summary>The role.</summary>
    public ThemeRoleId Role;

    /// <summary>Its fill, or null.</summary>
    public Rgba? Fill;

    /// <summary>Its ink, or null.</summary>
    public Rgba? Ink;

    /// <summary>How much contrast the ink needs on the fill.</summary>
    public ContrastClass TextClass;
}

/// <summary>
/// Resolves a theme's colours: each culture authors a few named seed colours
/// and one shared role map turns them into every role's fill and ink (piece 6
/// R3). Diegetic roles have no seeds: only the neutral theme names their
/// colours, and cultures may not override them (Z4).
/// </summary>
public static class Palette
{
    /// <summary>
    /// The theme's roles, in role order. A seed missing from
    /// <paramref name="seeds"/> falls back to <paramref name="baseSeeds"/> (the
    /// neutral theme's); the rule's alpha multiplies the fill; an override
    /// replaces the fill and/or ink (its hex alpha as written). Each problem is
    /// added to <paramref name="problems"/>: an unknown role name, a map rule for
    /// a diegetic role, a repeated rule, an unknown seed, bad hex, and an
    /// override of a diegetic role unless <paramref name="allowDiegetic"/> (the
    /// neutral theme only).
    /// </summary>
    public static List<ResolvedRole> Resolve(IReadOnlyList<PaletteRule> map, IReadOnlyDictionary<string, Rgba> baseSeeds, IReadOnlyDictionary<string, Rgba> seeds,
                                             IReadOnlyList<PaletteOverride> overrides, bool allowDiegetic, List<string> problems)
    {
        if (problems == null)
            throw new ArgumentException("Palette.Resolve needs a problem list.", nameof(problems));

        var byRole = new SortedDictionary<ThemeRoleId, ResolvedRole>();
        foreach (PaletteRule rule in map ?? Array.Empty<PaletteRule>())
        {
            if (rule == null || !TryRole(rule.role, "Palette map", problems, out ThemeRoleId role))
                continue;
            if (ThemeRoles.IsDiegetic(role))
            {
                problems.Add($"Palette map: role '{role}' is diegetic; it takes no seeds (only the neutral theme names its colours).");
                continue;
            }
            if (byRole.ContainsKey(role))
            {
                problems.Add($"Palette map: role '{role}' has more than one rule.");
                continue;
            }

            Rgba? fill = Seed(rule.fill, role, baseSeeds, seeds, problems);
            byRole[role] = new ResolvedRole
            {
                Role = role,
                Fill = fill.HasValue ? fill.Value.WithAlpha(fill.Value.A * rule.alpha) : (Rgba?)null,
                Ink = Seed(rule.ink, role, baseSeeds, seeds, problems),
                TextClass = rule.textClass
            };
        }

        foreach (PaletteOverride o in overrides ?? Array.Empty<PaletteOverride>())
        {
            if (o == null || !TryRole(o.role, "Override", problems, out ThemeRoleId role))
                continue;
            if (ThemeRoles.IsDiegetic(role) && !allowDiegetic)
            {
                problems.Add($"Override: role '{role}' is diegetic; only the neutral theme names its colours.");
                continue;
            }

            if (!byRole.TryGetValue(role, out ResolvedRole entry))
                byRole[role] = entry = new ResolvedRole { Role = role };
            if (!string.IsNullOrEmpty(o.fill))
                entry.Fill = Hex(o.fill, role, problems) ?? entry.Fill;
            if (!string.IsNullOrEmpty(o.ink))
                entry.Ink = Hex(o.ink, role, problems) ?? entry.Ink;
        }

        return byRole.Values.ToList();
    }

    /// <summary>
    /// The roles a theme lacks: every chrome role needs a colour, and with
    /// <paramref name="includeDiegetic"/> (the neutral theme) every diegetic one too.
    /// </summary>
    public static List<ThemeRoleId> Missing(IReadOnlyList<ResolvedRole> roles, bool includeDiegetic)
    {
        var present = new HashSet<ThemeRoleId>((roles ?? Array.Empty<ResolvedRole>()).Where(r => r != null).Select(r => r.Role));
        return ((ThemeRoleId[])Enum.GetValues(typeof(ThemeRoleId)))
            .Where(r => !present.Contains(r) && (includeDiegetic || !ThemeRoles.IsDiegetic(r)))
            .ToList();
    }

    /// <summary>One contrast pair per role with a fill, an ink and a class other than None.</summary>
    public static List<ContrastPair> Pairs(IReadOnlyList<ResolvedRole> roles) =>
        (roles ?? Array.Empty<ResolvedRole>())
            .Where(r => r != null && r.Fill.HasValue && r.Ink.HasValue && r.TextClass != ContrastClass.None)
            .Select(r => new ContrastPair(r.Role.ToString(), r.Ink.Value, r.Fill.Value, r.TextClass))
            .ToList();

    /// <summary>
    /// The diegetic texts a theme's chrome sits behind, as contrast pairs (all
    /// at the Text minimum): the rows of the scanned page, the records, the
    /// transcript and the books, with and without the theme's selection
    /// highlight, the record's labels, note and origin on the theme's window
    /// body, the scanner footer and the speech bubble. Diegetic colours come from
    /// <paramref name="neutral"/> (only it names them); a pair whose colours are
    /// missing is skipped (Missing reports those roles). Translucent fills are
    /// composited over what they sit on.
    /// </summary>
    public static List<ContrastPair> DiegeticPairs(IReadOnlyList<ResolvedRole> theme, IReadOnlyList<ResolvedRole> neutral)
    {
        Rgba? body = Find(theme, ThemeRoleId.WindowBody, true), highlight = Find(theme, ThemeRoleId.SelectionHighlight, true);
        Rgba? paper = Find(neutral, ThemeRoleId.DiegeticPaper, true);
        Rgba? row = Find(neutral, ThemeRoleId.DiegeticRow, true), rowInk = Find(neutral, ThemeRoleId.DiegeticRow, false);
        Rgba? book = Find(neutral, ThemeRoleId.DiegeticBookRow, true), bookInk = Find(neutral, ThemeRoleId.DiegeticBookRow, false);
        Rgba? label = Find(neutral, ThemeRoleId.DiegeticLabel, false), note = Find(neutral, ThemeRoleId.DiegeticNote, false);
        Rgba? backing = Find(neutral, ThemeRoleId.DiegeticBacking, true), backingInk = Find(neutral, ThemeRoleId.DiegeticBacking, false);
        Rgba? bubble = Find(neutral, ThemeRoleId.DiegeticBubble, true), bubbleInk = Find(neutral, ThemeRoleId.DiegeticBubble, false);

        var pairs = new List<ContrastPair>();
        void Add(string name, Rgba? ink, Rgba? fill, Rgba? under = null)
        {
            if (ink.HasValue && fill.HasValue && (under.HasValue || fill.Value.A >= 1f))
                pairs.Add(new ContrastPair(name, ink.Value, under.HasValue ? Contrast.Over(fill.Value, under.Value) : fill.Value, ContrastClass.Text));
        }

        Add("SelectionHighlight on a document row", rowInk, highlight, paper);
        Add("SelectionHighlight on a record or transcript row", rowInk, highlight, body);
        Add("SelectionHighlight on a book row", bookInk, highlight, body);
        Add("DiegeticRow on the scanned page", rowInk, row, paper);
        Add("DiegeticRow on the window body", rowInk, row, body);
        Add("DiegeticLabel on a record row", label, row.HasValue && body.HasValue ? Contrast.Over(row.Value, body.Value) : (Rgba?)null);
        Add("DiegeticNote on the window body", note, body);
        Add("DiegeticRow text on the window body", rowInk, body);
        Add("DiegeticBookRow on the window body", bookInk, book, body);
        Add("DiegeticBacking", backingInk, backing);
        Add("DiegeticBubble", bubbleInk, bubble.HasValue ? bubble.Value.WithAlpha(1f) : (Rgba?)null);
        return pairs;
    }

    /// <summary>A role's fill or ink in a resolved theme, or null.</summary>
    private static Rgba? Find(IReadOnlyList<ResolvedRole> roles, ThemeRoleId role, bool fill)
    {
        ResolvedRole r = (roles ?? Array.Empty<ResolvedRole>()).FirstOrDefault(x => x != null && x.Role == role);
        return r == null ? null : fill ? r.Fill : r.Ink;
    }

    /// <summary>Parses a role name (exact case); an unknown one adds a problem.</summary>
    private static bool TryRole(string name, string where, List<string> problems, out ThemeRoleId role)
    {
        if (!string.IsNullOrEmpty(name) && Enum.TryParse(name, false, out role) && Enum.IsDefined(typeof(ThemeRoleId), role) && !char.IsDigit(name[0]))
            return true;
        role = default;
        problems.Add($"{where}: unknown role '{name}'.");
        return false;
    }

    /// <summary>A seed by name (the theme's, else the base's); empty gives null; unknown adds a problem.</summary>
    private static Rgba? Seed(string name, ThemeRoleId role, IReadOnlyDictionary<string, Rgba> baseSeeds, IReadOnlyDictionary<string, Rgba> seeds, List<string> problems)
    {
        if (string.IsNullOrEmpty(name))
            return null;
        if (seeds != null && seeds.TryGetValue(name, out Rgba c))
            return c;
        if (baseSeeds != null && baseSeeds.TryGetValue(name, out c))
            return c;
        problems.Add($"Role '{role}': unknown seed '{name}'.");
        return null;
    }

    /// <summary>Parses an override's hex; bad hex adds a problem.</summary>
    private static Rgba? Hex(string hex, ThemeRoleId role, List<string> problems)
    {
        if (Rgba.TryParseHex(hex, out Rgba c))
            return c;
        problems.Add($"Override of role '{role}': '{hex}' is not #RRGGBB or #RRGGBBAA.");
        return null;
    }
}
