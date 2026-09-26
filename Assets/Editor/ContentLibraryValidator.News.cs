using UnityEngine;

/// <summary>The validator's news check (redesign phase 13): the morning paper's debt lines hold what Generate World requires of them (NewsContent.Problems).</summary>
public static partial class ContentLibraryValidator
{
    /// <summary>Reports each problem of the library's news block; returns how many.</summary>
    private static int CheckNews(ContentLibrarySO lib)
    {
        int issues = 0;
        foreach (string problem in lib.News.Problems())
        {
            Debug.LogError($"[ContentLibraryValidator] News: {problem.TrimEnd('.')} in '{lib.name}' (Tools > TimeDesk > Generate World writes world_source.json \"news\").", lib);
            issues++;
        }
        return issues;
    }
}
