using System.Collections.Generic;
using System.Text;

/// <summary>
/// Turns raw timeline score keys into text a designer can read at a glance,
/// grouped by nation and resolved to authored display names.
/// Pure formatting — no state, no Unity scene dependencies.
/// </summary>
public static class TimelineScoreDisplay
{
    /// <summary>
    /// Formats every score in the world grouped by nation-era profile, followed by
    /// nation totals and attribute totals. Keys that can't be resolved against the
    /// library fall back to their raw form so nothing is silently hidden.
    /// </summary>
    public static string FormatGrouped(WorldState world, ContentLibrarySO lib)
    {
        if (world == null || world.timeline == null)
            return "(no timeline state)";

        IReadOnlyList<ScoreEntry> scores = world.timeline.scores;

        if (scores == null || scores.Count == 0)
            return "(no scores yet)";

        // profile/destination display name -> lines under it
        var grouped = new List<KeyValuePair<string, List<string>>>();
        var nationTotals = new List<string>();
        var attributeTotals = new List<string>();
        var unresolved = new List<string>();

        foreach (ScoreEntry entry in scores)
        {
            if (entry == null)
                continue;

            if (!ScoreKey.TryParse(entry.key, out ParsedScoreKey parsed))
            {
                unresolved.Add($"  {entry.key} = {entry.value:0.##}");
                continue;
            }

            switch (parsed.kind)
            {
                case ScoreKeyKind.Nation:
                    nationTotals.Add($"  {ResolveNationName(lib, parsed.nationId)} = {entry.value:0.##}");
                    break;

                case ScoreKeyKind.GlobalAttr:
                    attributeTotals.Add($"  {ResolveAttributeName(lib, parsed.attributeId)} = {entry.value:0.##}");
                    break;

                case ScoreKeyKind.ProfileAttr:
                {
                    string header = ResolveProfileName(lib, parsed.profileId);
                    string tier = TierSuffix(world, parsed.profileId, parsed.attributeId);
                    AddGrouped(grouped, header, $"  {ResolveAttributeName(lib, parsed.attributeId)} = {entry.value:0.##}{tier}");
                    break;
                }

                case ScoreKeyKind.AdHocAttr:
                {
                    string header = $"{ResolveNationName(lib, parsed.nationId)} — {ResolveEraName(lib, parsed.eraId)} (unauthored)";
                    AddGrouped(grouped, header, $"  {ResolveAttributeName(lib, parsed.attributeId)} = {entry.value:0.##}");
                    break;
                }
            }
        }

        var sb = new StringBuilder();

        foreach (KeyValuePair<string, List<string>> group in grouped)
        {
            sb.AppendLine(group.Key);
            foreach (string line in group.Value)
                sb.AppendLine(line);
        }

        AppendSection(sb, "Nation totals", nationTotals);
        AppendSection(sb, "Attribute totals (all nations)", attributeTotals);
        AppendSection(sb, "Unrecognized keys", unresolved);

        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// Human-readable label for one ranked winner, e.g. "Latia" or "Latia (Rome) / Aristocracy".
    /// Used by in-scene ranked readouts so scene and console never disagree on naming.
    /// </summary>
    public static string DescribeScoreKey(ContentLibrarySO lib, string key)
    {
        if (!ScoreKey.TryParse(key, out ParsedScoreKey parsed))
            return key;

        return parsed.kind switch
        {
            ScoreKeyKind.Nation => ResolveNationName(lib, parsed.nationId),
            ScoreKeyKind.GlobalAttr => ResolveAttributeName(lib, parsed.attributeId),
            ScoreKeyKind.ProfileAttr =>
                $"{ResolveProfileName(lib, parsed.profileId)} / {ResolveAttributeName(lib, parsed.attributeId)}",
            ScoreKeyKind.AdHocAttr =>
                $"{ResolveNationName(lib, parsed.nationId)} — {ResolveEraName(lib, parsed.eraId)} / {ResolveAttributeName(lib, parsed.attributeId)}",
            _ => key
        };
    }

    /// <summary>Appends a line under an existing header, creating the group in first-seen order.</summary>
    private static void AddGrouped(List<KeyValuePair<string, List<string>>> grouped, string header, string line)
    {
        foreach (KeyValuePair<string, List<string>> group in grouped)
        {
            if (group.Key == header)
            {
                group.Value.Add(line);
                return;
            }
        }

        grouped.Add(new KeyValuePair<string, List<string>>(header, new List<string> { line }));
    }

    /// <summary>Writes a titled block, skipping it entirely when empty.</summary>
    private static void AppendSection(StringBuilder sb, string title, List<string> lines)
    {
        if (lines.Count == 0)
            return;

        sb.AppendLine(title);
        foreach (string line in lines)
            sb.AppendLine(line);
    }

    /// <summary>"  [dominant]" / "  [supporting]" for a profile attribute, or empty.</summary>
    private static string TierSuffix(WorldState world, string profileId, string attributeId)
    {
        string dominanceKey = ScoreKey.DominanceKey(
            new ParsedScoreKey { profileId = profileId, attributeId = attributeId });

        if (world.timeline.dominantKeys != null && world.timeline.dominantKeys.Contains(dominanceKey))
            return "  [dominant]";

        if (world.timeline.supportingKeys != null && world.timeline.supportingKeys.Contains(dominanceKey))
            return "  [supporting]";

        return string.Empty;
    }

    private static string ResolveNationName(ContentLibrarySO lib, string id)
    {
        if (lib != null)
            foreach (NationSO nation in lib.Nations)
                if (nation != null && nation.id == id)
                    return string.IsNullOrEmpty(nation.displayName) ? id : nation.displayName;

        return id;
    }

    private static string ResolveAttributeName(ContentLibrarySO lib, string id)
    {
        if (lib != null)
            foreach (AttributeSO attr in lib.Attributes)
                if (attr != null && attr.id == id)
                    return string.IsNullOrEmpty(attr.displayName) ? id : attr.displayName;

        return id;
    }

    private static string ResolveEraName(ContentLibrarySO lib, string id)
    {
        if (lib != null)
            foreach (EraSO era in lib.Eras)
                if (era != null && era.id == id)
                    return string.IsNullOrEmpty(era.displayName) ? id : era.displayName;

        return id;
    }

    private static string ResolveProfileName(ContentLibrarySO lib, string id)
    {
        if (lib != null)
            foreach (NationEraProfileSO profile in lib.Profiles)
                if (profile != null && profile.id == id)
                    return string.IsNullOrEmpty(profile.displayName) ? id : profile.displayName;

        return id;
    }
}
