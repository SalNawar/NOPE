using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>The validator's PC block checks (P spec IN2): the Internet's sites, pages and people, and the page words.</summary>
public static partial class ContentLibraryValidator
{
    /// <summary>
    /// Reports the pc block's content problems (Sites.Problems,
    /// AncestryPages.Problems against the library's premades, places and
    /// traveller names) and every Sites.WordKeys key missing from the reading
    /// UI string table. Returns the issue count.
    /// </summary>
    private static int CheckPc(ContentLibrarySO lib)
    {
        int issues = 0;
        void Error(string message)
        {
            Debug.LogError($"[ContentLibraryValidator] PC: {message.TrimEnd('.')} in '{lib.name}' (Tools > TimeDesk > Generate World writes the pc block).", lib);
            issues++;
        }

        PcContent pc = lib.Pc;
        foreach (string problem in Sites.Problems(pc))
            Error(problem);

        IEnumerable<string> names = lib.Profiles.Where(p => p != null).SelectMany(p => p.AllNames)
                                       .Concat(lib.Legendaries.Where(l => l != null).Select(l => l.displayName));
        foreach (string problem in AncestryPages.Problems(pc.ancestry, lib.Legendaries.Where(l => l != null).Select(l => l.id).ToList(),
                                                          new HashSet<string>(lib.Profiles.Where(p => p != null).Select(p => p.id)), names))
            Error(problem);

        UiStringTableSO reading = lib.GetStringTable(lib.CultureUi.readingLanguage);
        var keys = new HashSet<string>(reading != null ? reading.entries.Where(e => e != null).Select(e => e.key) : Array.Empty<string>());
        foreach (string key in Sites.WordKeys.Where(k => !keys.Contains(k)))
            Error($"the reading UI string table has no '{key}', which the Internet's pages write with");
        return issues;
    }
}
