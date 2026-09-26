using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

/// <summary>
/// Generate World's news block (redesign phase 13; the traveller-types spec's
/// §10): world_source.json "news" (the morning paper's debt lines, "debt") is
/// checked (NewsContent.Problems) and written into the content library, where
/// the nightly resolve picks tomorrow's line (DebtNews.Line). The block is
/// optional: without it the paper prints no debt line.
/// </summary>
public static partial class WorldContentGenerator
{
    /// <summary>The news block as authored ("news").</summary>
    [Serializable] private sealed class NewsData { public string[] debt; }

    /// <summary>The news block's content (its lines verbatim; none when the section is missing).</summary>
    private static NewsContent BuildNews(NewsData n) =>
        new NewsContent { debt = (n?.debt ?? Array.Empty<string>()).ToList() };

    /// <summary>The news block's problems (NewsContent.Problems, the validator's rule).</summary>
    private static void CheckNews(WorldSource src, List<string> errors) => errors.AddRange(BuildNews(src.news).Problems());

    /// <summary>Writes the news block into the content library.</summary>
    private static void WireNews(ContentLibrarySO lib, NewsData news)
    {
        var so = new SerializedObject(lib);
        so.FindProperty("news").boxedValue = BuildNews(news);
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(lib);
    }
}
