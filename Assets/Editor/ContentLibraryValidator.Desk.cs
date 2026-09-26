using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// The content-against-desk checks (audit R6-021): Build Office UI and
/// Validate Content Library run the same rules, so a content change that
/// breaks the office is caught by the content tool too, not only on the next
/// rebuild.
/// </summary>
public static partial class ContentLibraryValidator
{
    /// <summary>
    /// What the desk cannot show of this content: fewer paper spawn slots than
    /// the most papers one traveller carries (MaxDocuments), a form with more
    /// fields than its paper's face holds (PaperFace.Capacity; the forms
    /// engine's layout check replaces it), and a traveller wheel that fits
    /// fewer choices than the interview's menu capacity. Empty when it fits
    /// (and for a null library or desk).
    /// </summary>
    public static List<string> DeskFitProblems(ContentLibrarySO library, DeskConfigSO desk)
    {
        var problems = new List<string>();
        if (library == null || desk == null)
            return problems;

        int maxPapers = MaxDocuments(TravellerBlueprints(library));
        int slots = desk.paperSpawnSlots != null ? desk.paperSpawnSlots.Length : 0;
        if (slots < maxPapers)
            problems.Add($"The desk has {slots} paper spawn slots but a traveller can carry {maxPapers} papers; add slots in Desk_Default.");

        var checkedTemplates = new HashSet<DocumentTemplateSO>();
        foreach (CaseBlueprintSO blueprint in TravellerBlueprints(library))
            foreach (DocumentTemplateSO template in blueprint != null && blueprint.DocumentTemplates != null ? blueprint.DocumentTemplates : new DocumentTemplateSO[0])
            {
                if (template == null || !checkedTemplates.Add(template))
                    continue;
                int fields = template.fieldSpecs != null ? template.fieldSpecs.Length : 0;
                int capacity = PaperFace.Capacity(template.showsPhoto, desk.face);
                if (fields > capacity)
                    problems.Add($"{template.displayName} has {fields} fields but a paper face holds {capacity}; raise Desk_Default.face or shorten the template.");
            }

        if (library.Interview != null)
        {
            int wheelFit = RadialLayout.MaxFit(desk.wheelRadii.x, desk.wheelRadii.y, desk.wheelItemSize.x, desk.wheelItemSize.y,
                                               desk.wheelCentreSize.x, desk.wheelCentreSize.y, desk.wheelItemGap, library.Interview.menuCapacity);
            if (wheelFit < library.Interview.menuCapacity)
                problems.Add($"The traveller wheel fits {wheelFit} choices, but the content library's interview menu capacity is {library.Interview.menuCapacity}; lower interview.menuCapacity in world_source.json or enlarge the wheel (Desk_Default: wheelRadii, wheelItemSize).");
        }

        return problems;
    }

    /// <summary>Reports each of <see cref="DeskFitProblems"/> against Desk_Default (a warning, and nothing checked, while Build Office UI has not made it); returns how many.</summary>
    private static int CheckDeskFit(ContentLibrarySO lib)
    {
        var desk = AssetDatabase.LoadAssetAtPath<DeskConfigSO>(OfficeSceneUIBuilder.DeskConfigPath);
        if (desk == null)
        {
            Debug.LogWarning($"[ContentLibraryValidator] No desk config at '{OfficeSceneUIBuilder.DeskConfigPath}' (Build Office UI makes it), so the desk fit is not checked.", lib);
            return 0;
        }

        List<string> problems = DeskFitProblems(lib, desk);
        foreach (string problem in problems)
            Debug.LogError($"[ContentLibraryValidator] {problem} ('{lib.name}')", desk);
        return problems.Count;
    }
}
