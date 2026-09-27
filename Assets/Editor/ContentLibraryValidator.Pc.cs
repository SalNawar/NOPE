using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>The validator's PC block checks (P spec IN2, ST3): the steps checklist's sets, the Internet's sites, pages and people, and the page words.</summary>
public static partial class ContentLibraryValidator
{
    /// <summary>
    /// Reports the pc block's content problems (StepSets.Problems against the
    /// reading UI string table and the forms of the blueprints travellers come
    /// from, Sites.Problems, AncestryPages.Problems against the library's
    /// premades, places and traveller names) and every Sites.WordKeys key
    /// missing from the reading UI string table; warns (no issue) about a
    /// data-only step set whose kind a blueprint now makes, so its mark goes
    /// and its forms and categories are checked. Returns the issue count.
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
        UiStringTableSO reading = lib.GetStringTable(lib.CultureUi.readingLanguage);
        var keys = new HashSet<string>(reading != null ? reading.entries.Where(e => e != null).Select(e => e.key) : Array.Empty<string>());
        List<CaseBlueprintSO> blueprints = TravellerBlueprints(lib).Where(b => b != null).Distinct().ToList();
        List<string> forms = blueprints.SelectMany(b => b.DocumentTemplates ?? new DocumentTemplateSO[0]).Where(t => t != null).Select(t => t.formNumber).ToList();
        foreach (string problem in StepSets.Problems(pc.steps, keys, forms))
            Error(problem);
        foreach (string type in StepSets.DataOnlyInPlay(pc.steps, blueprints.Select(b => b.Kind)))
            Debug.LogWarning($"[ContentLibraryValidator] PC: the {type} step set is marked dataOnly, but a blueprint makes {type} travellers now: drop the mark in world_source.json pc.steps, so its forms and categories are checked ('{lib.name}').", lib);

        foreach (string problem in Sites.Problems(pc))
            Error(problem);

        IEnumerable<string> names = lib.Profiles.Where(p => p != null).SelectMany(p => p.AllNames)
                                       .Concat(lib.Legendaries.Where(l => l != null).Select(l => l.displayName));
        foreach (string problem in AncestryPages.Problems(pc.ancestry, lib.Legendaries.Where(l => l != null).Select(l => l.id).ToList(),
                                                          new HashSet<string>(lib.Profiles.Where(p => p != null).Select(p => p.id)), names))
            Error(problem);

        foreach (string key in Sites.WordKeys.Where(k => !keys.Contains(k)))
            Error($"the reading UI string table has no '{key}', which the Internet's pages write with");
        return issues;
    }
}
