using UnityEngine;

/// <summary>The validator's agency check (redesign phase 2): the library's agency block holds what Generate World requires of it (AgencyContent.Problems).</summary>
public static partial class ContentLibraryValidator
{
    /// <summary>Reports each problem of the library's agency block; returns how many.</summary>
    private static int CheckAgency(ContentLibrarySO lib)
    {
        int issues = 0;
        foreach (string problem in lib.Agency.Problems())
        {
            Debug.LogError($"[ContentLibraryValidator] Agency: {problem.TrimEnd('.')} in '{lib.name}' (Tools > TimeDesk > Generate World writes world_source.json \"agency\").", lib);
            issues++;
        }
        return issues;
    }
}
