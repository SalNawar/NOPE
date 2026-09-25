using System.Collections.Generic;

/// <summary>
/// The UI-channel cue that carries the present culture: the timeline leader's
/// generated effect broadcasts "culture:{nationId}" (piece 5 emits it, piece 6
/// reads it through TimelineCueReceiver). One home for the grammar.
/// </summary>
public static class CultureCue
{
    /// <summary>The cue's prefix.</summary>
    public const string Prefix = "culture:";

    /// <summary>The cue for a nation ("culture:china").</summary>
    public static string Format(string nationId) => Prefix + nationId;

    /// <summary>The nation a culture cue names; false (and a null id) for any other cue, a blank id, or a prefix in another case.</summary>
    public static bool TryParse(string cue, out string nationId)
    {
        nationId = null;
        if (cue == null || !cue.StartsWith(Prefix, System.StringComparison.Ordinal))
            return false;

        string id = cue.Substring(Prefix.Length);
        if (string.IsNullOrWhiteSpace(id))
            return false;

        nationId = id;
        return true;
    }

    /// <summary>
    /// The culture a list of active UI cues selects: the first parsable culture
    /// cue in list order, or null. <paramref name="matches"/> counts the cues
    /// that parsed (the caller warns when more than one did).
    /// </summary>
    public static string Pick(IReadOnlyList<string> cues, out int matches)
    {
        matches = 0;
        string first = null;
        if (cues == null)
            return null;

        foreach (string cue in cues)
        {
            if (!TryParse(cue, out string id))
                continue;
            matches++;
            first = first ?? id;
        }
        return first;
    }
}
