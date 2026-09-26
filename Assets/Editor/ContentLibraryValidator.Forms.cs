using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;

/// <summary>
/// The forms part of the desk fit (redesign phase 4, PC spec FO10): every
/// traveller's document template has a form that places each of its fields
/// once and fits the paper at its fields' longest values (an origin at the
/// library's longest origin label; DocumentForm.Problems,
/// measured with TextMeshPro as the paper prints), at the form style's sizes
/// (FormStyle_Agency, or the defaults before Build Office UI made it).
/// DeskFitProblems reports them, so Build Office UI and Validate Content
/// Library check the same.
/// </summary>
public static partial class ContentLibraryValidator
{
    /// <summary>Each problem of each traveller document template's form, one message each.</summary>
    private static List<string> FormFitProblems(ContentLibrarySO lib)
    {
        var problems = new List<string>();
        string[] styles = AssetDatabase.FindAssets("t:" + nameof(FormStyleSO));
        FormStyleSO style = styles.Length > 0 ? AssetDatabase.LoadAssetAtPath<FormStyleSO>(AssetDatabase.GUIDToAssetPath(styles[0])) : null;
        FormMetrics metrics = style != null ? style.metrics : new FormMetrics();
        int longestOrigin = 0;
        foreach (NationEraProfileSO place in lib.Profiles)
            if (place != null)
                longestOrigin = Mathf.Max(longestOrigin, place.OriginLabel.Length);
        var probe = new GameObject("FormsCheckText", typeof(TextMeshPro)) { hideFlags = HideFlags.HideAndDontSave };
        try
        {
            var measure = new TmpFormText(probe.GetComponent<TextMeshPro>());
            var seen = new HashSet<DocumentTemplateSO>();
            foreach (CaseBlueprintSO blueprint in TravellerBlueprints(lib))
                foreach (DocumentTemplateSO template in blueprint != null && blueprint.DocumentTemplates != null ? blueprint.DocumentTemplates : new DocumentTemplateSO[0])
                {
                    if (template == null || !seen.Add(template))
                        continue;
                    foreach (string problem in DocumentForm.Problems(template, lib.Agency, longestOrigin, metrics, measure))
                        problems.Add($"{template.displayName} ({template.name}): {problem} (its form, FormLayout.Check; edit the template's form or FormStyle_Agency).");
                }
        }
        finally
        {
            Object.DestroyImmediate(probe);
        }
        return problems;
    }
}
