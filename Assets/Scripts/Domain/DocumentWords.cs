using System.Text;

/// <summary>
/// The UI string key of a word a document prints (Saleh 2026-10-07: "I want
/// the language to change on all documents and the apps"): every fixed word on
/// a paper or a PC page (a label, a title, a caption, a section head, a column
/// head, fine print) is looked up in world_source.json ui.strings under
/// "doc." and its English folded to lower-case letters and digits joined by
/// '_' ("CITIZEN ID" and "Citizen ID" give doc.citizen_id), so a culture
/// table translates it like any UI string while the document's data keeps its
/// English. Values are never looked up. Pure; tested (DocumentWordsTests).
/// </summary>
public static class DocumentWords
{
    /// <summary>The keys' prefix.</summary>
    public const string Prefix = "doc.";

    /// <summary>The longest key body (a long fine print's key is cut, so keys stay readable).</summary>
    public const int MaxBody = 60;

    /// <summary>The key of <paramref name="english"/>, or null when it has no ASCII letter or digit (nothing to translate).</summary>
    public static string Key(string english)
    {
        if (string.IsNullOrEmpty(english))
            return null;
        var sb = new StringBuilder(english.Length);
        bool gap = false;
        foreach (char raw in english)
        {
            char c = char.ToLowerInvariant(raw);
            bool keep = (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9');
            if (!keep)
            {
                gap = sb.Length > 0;
                continue;
            }
            if (gap)
                sb.Append('_');
            gap = false;
            sb.Append(c);
            if (sb.Length >= MaxBody)
                break;
        }
        return sb.Length == 0 ? null : Prefix + sb;
    }
}
