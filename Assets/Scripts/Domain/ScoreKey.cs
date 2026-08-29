/// <summary>Which score-key shape a parsed timeline key came from.</summary>
public enum ScoreKeyKind
{
    /// <summary>"attr:{profileId}:{attrId}" — an attribute inside an authored profile.</summary>
    ProfileAttr,

    /// <summary>"attr:{nationId}@{eraId}:{attrId}" — an unauthored nation+era destination.</summary>
    AdHocAttr,

    /// <summary>"attrTotal:{attrId}" — an attribute's total across the whole timeline.</summary>
    GlobalAttr,

    /// <summary>"nation:{nationId}" — a nation's global score.</summary>
    Nation
}

/// <summary>A score key broken into its parts. Unused fields are null for a given kind.</summary>
public struct ParsedScoreKey
{
    public ScoreKeyKind kind;
    public string profileId;
    public string nationId;
    public string eraId;
    public string attributeId;
}

/// <summary>
/// Parses the timeline score keys built by TimelineKeys back into their parts.
/// Pure string logic with no Unity dependency, so the key format is covered by
/// EditMode tests; TimelineKeys (Assembly-CSharp) owns building them.
/// </summary>
public static class ScoreKey
{
    private const string GlobalAttrPrefix = "attrTotal:";
    private const string NationPrefix = "nation:";
    private const string AttrPrefix = "attr:";

    /// <summary>
    /// Parses a score key. Returns false for null/empty input or an unrecognized
    /// shape, so callers can fall back to showing the raw key rather than hiding it.
    /// </summary>
    public static bool TryParse(string key, out ParsedScoreKey parsed)
    {
        parsed = default;

        if (string.IsNullOrEmpty(key))
            return false;

        if (key.StartsWith(GlobalAttrPrefix))
        {
            string attributeId = key.Substring(GlobalAttrPrefix.Length);

            if (string.IsNullOrEmpty(attributeId))
                return false;

            parsed = new ParsedScoreKey { kind = ScoreKeyKind.GlobalAttr, attributeId = attributeId };
            return true;
        }

        if (key.StartsWith(NationPrefix))
        {
            string nationId = key.Substring(NationPrefix.Length);

            if (string.IsNullOrEmpty(nationId))
                return false;

            parsed = new ParsedScoreKey { kind = ScoreKeyKind.Nation, nationId = nationId };
            return true;
        }

        if (!key.StartsWith(AttrPrefix))
            return false;

        // "attr:{scope}:{attrId}" where scope is "{profileId}" or "{nationId}@{eraId}".
        string rest = key.Substring(AttrPrefix.Length);
        int split = rest.LastIndexOf(':');

        if (split <= 0 || split >= rest.Length - 1)
            return false;

        string scope = rest.Substring(0, split);
        string attrId = rest.Substring(split + 1);
        int at = scope.IndexOf('@');

        if (at > 0 && at < scope.Length - 1)
        {
            parsed = new ParsedScoreKey
            {
                kind = ScoreKeyKind.AdHocAttr,
                nationId = scope.Substring(0, at),
                eraId = scope.Substring(at + 1),
                attributeId = attrId
            };
            return true;
        }

        parsed = new ParsedScoreKey
        {
            kind = ScoreKeyKind.ProfileAttr,
            profileId = scope,
            attributeId = attrId
        };
        return true;
    }

    /// <summary>Rebuilds the dominance bookkeeping key ("profileId:attrId") for a parsed profile attribute.</summary>
    public static string DominanceKey(ParsedScoreKey parsed) => $"{parsed.profileId}:{parsed.attributeId}";
}
