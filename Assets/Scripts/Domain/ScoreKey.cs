/// <summary>What a timeline score key measures. Parsed from the key text, never saved.</summary>
public enum ScoreKeyKind
{
    /// <summary>An attribute delta inside an authored place ("attr:{profileId}:{attrId}").</summary>
    ProfileAttr,

    /// <summary>An attribute delta at an unauthored nation and era ("attr:{nationId}@{eraId}:{attrId}").</summary>
    AdHocAttr,

    /// <summary>The global total of an attribute ("attrTotal:{attrId}").</summary>
    GlobalAttr,

    /// <summary>A nation's own score ("nation:{nationId}").</summary>
    Nation
}

/// <summary>A score key split into its parts (unused parts are null).</summary>
public struct ParsedScoreKey
{
    /// <summary>Which grammar the key follows.</summary>
    public ScoreKeyKind kind;

    /// <summary>The place id (ProfileAttr).</summary>
    public string profileId;

    /// <summary>The nation id (AdHocAttr, Nation).</summary>
    public string nationId;

    /// <summary>The era id (AdHocAttr).</summary>
    public string eraId;

    /// <summary>The attribute id (ProfileAttr, AdHocAttr, GlobalAttr).</summary>
    public string attributeId;
}

/// <summary>
/// The one home of the timeline's score-key grammar: the builders every
/// system writes scores with (TimelineKeys wraps them for the content types)
/// and the parser history reads them back with. The parser is ported from
/// PR #3 (Marwan, commit 160afde), credited; saves already hold these keys.
/// </summary>
public static class ScoreKey
{
    /// <summary>Prefix of attribute keys (profile and ad-hoc).</summary>
    private const string AttrPrefix = "attr:";

    /// <summary>Prefix of global attribute totals (checked before <see cref="AttrPrefix"/>, which it starts with).</summary>
    private const string GlobalPrefix = "attrTotal:";

    /// <summary>Prefix of nation scores.</summary>
    private const string NationPrefix = "nation:";

    /// <summary>Separates the nation from the era in an ad-hoc place scope.</summary>
    private const char AdHocSeparator = '@';

    /// <summary>Score of an attribute inside an authored place.</summary>
    public static string ProfileAttr(string profileId, string attrId) => $"{AttrPrefix}{profileId}:{attrId}";

    /// <summary>Score of an attribute at an unauthored nation and era.</summary>
    public static string AdHocAttr(string nationId, string eraId, string attrId) => $"{AttrPrefix}{nationId}{AdHocSeparator}{eraId}:{attrId}";

    /// <summary>Global total of an attribute across the whole timeline.</summary>
    public static string GlobalAttr(string attrId) => $"{GlobalPrefix}{attrId}";

    /// <summary>Global score of a nation.</summary>
    public static string Nation(string nationId) => $"{NationPrefix}{nationId}";

    /// <summary>Dominance-tier key of a place's attribute ("{profileId}:{attrId}"; a tier key, not a score key).</summary>
    public static string Dominance(string profileId, string attrId) => $"{profileId}:{attrId}";

    /// <summary>
    /// Splits a score key into its parts; false for null, blank or any other
    /// shape (a missing part counts as another shape). "attrTotal:" is checked
    /// before "attr:"; an attribute key splits on its last colon, and a scope
    /// holding '@' is an ad-hoc nation@era.
    /// </summary>
    public static bool TryParse(string key, out ParsedScoreKey parsed)
    {
        parsed = default;
        if (string.IsNullOrEmpty(key))
            return false;

        if (key.StartsWith(GlobalPrefix, System.StringComparison.Ordinal))
        {
            string attr = key.Substring(GlobalPrefix.Length);
            if (attr.Length == 0)
                return false;
            parsed = new ParsedScoreKey { kind = ScoreKeyKind.GlobalAttr, attributeId = attr };
            return true;
        }

        if (key.StartsWith(NationPrefix, System.StringComparison.Ordinal))
        {
            string nation = key.Substring(NationPrefix.Length);
            if (nation.Length == 0)
                return false;
            parsed = new ParsedScoreKey { kind = ScoreKeyKind.Nation, nationId = nation };
            return true;
        }

        if (!key.StartsWith(AttrPrefix, System.StringComparison.Ordinal))
            return false;

        string rest = key.Substring(AttrPrefix.Length);
        int colon = rest.LastIndexOf(':');
        if (colon <= 0 || colon == rest.Length - 1)
            return false;

        string scope = rest.Substring(0, colon);
        string attrId = rest.Substring(colon + 1);
        int at = scope.IndexOf(AdHocSeparator);
        if (at < 0)
        {
            parsed = new ParsedScoreKey { kind = ScoreKeyKind.ProfileAttr, profileId = scope, attributeId = attrId };
            return true;
        }

        if (at == 0 || at == scope.Length - 1)
            return false;

        parsed = new ParsedScoreKey
        {
            kind = ScoreKeyKind.AdHocAttr, nationId = scope.Substring(0, at), eraId = scope.Substring(at + 1), attributeId = attrId
        };
        return true;
    }
}
