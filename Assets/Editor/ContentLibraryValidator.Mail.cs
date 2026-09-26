using UnityEngine;

/// <summary>The validator's mail check (redesign phase 25): the library's authored mail holds what Generate World requires of it (Mailbox.AuthoredProblems).</summary>
public static partial class ContentLibraryValidator
{
    /// <summary>Reports each problem of the library's authored mail; returns how many.</summary>
    private static int CheckMail(ContentLibrarySO lib)
    {
        int issues = 0;
        foreach (string problem in Mailbox.AuthoredProblems(lib.Mail))
        {
            Debug.LogError($"[ContentLibraryValidator] Mail: {problem.TrimEnd('.')} in '{lib.name}' (Tools > TimeDesk > Generate World writes world_source.json \"pc.mail\").", lib);
            issues++;
        }
        return issues;
    }
}
