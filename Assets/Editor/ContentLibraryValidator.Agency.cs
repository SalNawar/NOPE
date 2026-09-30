using System.Linq;
using UnityEngine;

/// <summary>The validator's agency check (redesign phase 2): the library's agency block holds what Generate World requires of it (AgencyContent.Problems), the clerk's own account too (ClerkContent.Problems, redesign phase 25), and the strandings' fate table and failure report (StrandingFates.Problems, ReportProblems; the endings and strandings spec §6).</summary>
public static partial class ContentLibraryValidator
{
    /// <summary>Reports each problem of the library's agency block; returns how many.</summary>
    private static int CheckAgency(ContentLibrarySO lib)
    {
        int issues = 0;
        foreach (string problem in lib.Agency.Problems().Concat(lib.Agency.clerk.Problems())
                                     .Concat(StrandingFates.Problems(lib.Agency.strandingFates, lib.Eras.Where(e => e != null).Select(e => e.id).ToList()))
                                     .Concat(StrandingFates.ReportProblems(lib.Agency.strandingReport)))
        {
            Debug.LogError($"[ContentLibraryValidator] Agency: {problem.TrimEnd('.')} in '{lib.name}' (Tools > TimeDesk > Generate World writes world_source.json \"agency\").", lib);
            issues++;
        }

        // The issuing offices and the fault canon against the kinds' forms (the document design spec, D4, D9).
        var carried = TravellerBlueprints(lib).Where(b => b != null)
            .SelectMany(b => (b.DocumentTemplates ?? new DocumentTemplateSO[0]).Select(t => (b.Kind, t)));
        foreach (string problem in DocumentContentChecks.Problems(lib.Agency, carried))
        {
            Debug.LogError($"[ContentLibraryValidator] Documents: {problem.TrimEnd('.')} in '{lib.name}' (world_source.json agency.offices and agency.faults; docs/DOCUMENT_FAULTS.md).", lib);
            issues++;
        }
        return issues;
    }
}
