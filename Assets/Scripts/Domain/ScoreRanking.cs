using System.Collections.Generic;

/// <summary>Which slice of the timeline scores a ranked office layer follows.</summary>
public enum RankCategory
{
    /// <summary>Highest "nation:{id}" score — which nation is winning overall.</summary>
    TopNation,

    /// <summary>Highest "attrTotal:{id}" score — which trait is winning across all nations.</summary>
    TopAttribute,

    /// <summary>Highest "attr:{profileId}:{attrId}" score — which nation-at-an-era + trait pairing is winning.</summary>
    TopProfileAttribute
}

/// <summary>One winning score: the key it came from, its id, and its value.</summary>
public struct RankedScore
{
    /// <summary>The full score key (e.g. "nation:latia").</summary>
    public string key;

    /// <summary>The identifying part used to look up sprite mappings (e.g. "latia", or "latia_rome:aristocracy").</summary>
    public string id;

    /// <summary>The winning score value.</summary>
    public float value;
}

/// <summary>
/// Picks the highest-scoring entry within one category of timeline scores.
/// Pure C# over (key, value) pairs so it stays testable without Unity types.
/// </summary>
public static class ScoreRanking
{
    /// <summary>
    /// Returns the highest-scoring entry in the category, or false if no entry clears
    /// <paramref name="minScore"/>. Ties keep the first entry encountered, so the
    /// displayed winner stays stable rather than flickering between equal scores.
    ///
    /// The floor is what stops a merely least-bad score from being presented as a
    /// winner: with a single attribute sitting at -1, the strict maximum is -1, but
    /// nothing is actually dominant. Comparison is strict, so a floor of 0 means
    /// "only positive influence counts".
    /// </summary>
    public static bool TryGetTop(IEnumerable<KeyValuePair<string, float>> scores, RankCategory category, float minScore, out RankedScore top)
    {
        top = default;

        if (scores == null)
            return false;

        bool found = false;

        foreach (KeyValuePair<string, float> entry in scores)
        {
            if (!ScoreKey.TryParse(entry.Key, out ParsedScoreKey parsed))
                continue;

            if (!MatchesCategory(parsed, category))
                continue;

            if (entry.Value <= minScore)
                continue;

            if (found && entry.Value <= top.value)
                continue;

            top = new RankedScore
            {
                key = entry.Key,
                id = IdFor(parsed, category),
                value = entry.Value
            };
            found = true;
        }

        return found;
    }

    /// <summary>True when a parsed key belongs to the requested category.</summary>
    private static bool MatchesCategory(ParsedScoreKey parsed, RankCategory category) => category switch
    {
        RankCategory.TopNation => parsed.kind == ScoreKeyKind.Nation,
        RankCategory.TopAttribute => parsed.kind == ScoreKeyKind.GlobalAttr,
        RankCategory.TopProfileAttribute => parsed.kind == ScoreKeyKind.ProfileAttr,
        _ => false
    };

    /// <summary>The id a sprite mapping is authored against for this category.</summary>
    private static string IdFor(ParsedScoreKey parsed, RankCategory category) => category switch
    {
        RankCategory.TopNation => parsed.nationId,
        RankCategory.TopAttribute => parsed.attributeId,
        RankCategory.TopProfileAttribute => ScoreKey.DominanceKey(parsed),
        _ => null
    };
}
