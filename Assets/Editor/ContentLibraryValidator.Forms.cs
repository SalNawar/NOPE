using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;

/// <summary>
/// The validator's forms check (redesign phase 4, PC spec FO10): every
/// traveller's document template has a form that places each of its fields
/// once and fits the paper at its fields' longest values (DocumentForm.Problems,
/// measured with TextMeshPro as the paper prints), at the form style's sizes
/// (FormStyle_Agency, or the defaults before Build Office UI made it).
/// </summary>
public static partial class ContentLibraryValidator
{
    /// <summary>Reports each problem of each traveller document template's form; returns how many.</summary>
    private static int CheckForms(ContentLibrarySO lib)
    {
        string[] styles = AssetDatabase.FindAssets("t:" + nameof(FormStyleSO));
        FormStyleSO style = styles.Length > 0 ? AssetDatabase.LoadAssetAtPath<FormStyleSO>(AssetDatabase.GUIDToAssetPath(styles[0])) : null;
        FormMetrics metrics = style != null ? style.metrics : new FormMetrics();
        var probe = new GameObject("FormsCheckText", typeof(TextMeshPro)) { hideFlags = HideFlags.HideAndDontSave };
        int issues = 0;
        try
        {
            var measure = new TmpFormText(probe.GetComponent<TextMeshPro>());
            var seen = new HashSet<DocumentTemplateSO>();
            foreach (CaseBlueprintSO blueprint in TravellerBlueprints(lib))
                foreach (DocumentTemplateSO template in blueprint != null && blueprint.DocumentTemplates != null ? blueprint.DocumentTemplates : new DocumentTemplateSO[0])
                {
                    if (template == null || !seen.Add(template))
                        continue;
                    foreach (string problem in DocumentForm.Problems(template, lib.Agency, metrics, measure))
                    {
                        Debug.LogError($"[ContentLibraryValidator] Document template '{template.name}' {problem} (its form, FormLayout.Check).", template);
                        issues++;
                    }
                }
        }
        finally
        {
            Object.DestroyImmediate(probe);
        }
        return issues;
    }
}
