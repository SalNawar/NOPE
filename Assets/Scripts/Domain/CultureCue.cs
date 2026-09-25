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
}
