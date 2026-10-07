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
    /// <summary>
    /// Each fixed word a paper or a PC page prints with no doc.* string in the
    /// reading table (DocumentWords.Key; Saleh 2026-10-07: "I want the language
    /// to change on all documents and the apps"): every traveller document
    /// template's (DocumentForm.PrintedWords), every PC page kind's
    /// (FormSpecSO) and the citation form's. A word without its string would
    /// print in English while a culture's language is read.
    /// </summary>
    private static List<string> DocumentWordProblems(ContentLibrarySO lib)
    {
        var problems = new List<string>();
        UiStringTableSO reading = lib.GetStringTable(lib.CultureUi.readingLanguage);
        if (reading == null)
            return problems;
        var keys = new HashSet<string>();
        foreach (UiStringEntry e in reading.entries)
            if (e != null && e.key != null)
                keys.Add(e.key);
        var reported = new HashSet<string>();
        void Check(string owner, IEnumerable<string> words)
        {
            foreach (string word in words)
            {
                string key = DocumentWords.Key(word);
                if (key != null && !keys.Contains(key) && reported.Add(key))
                    problems.Add($"{owner} prints '{word}' but ui.strings has no '{key}': it stays English in a culture's language (add it to world_source.json ui.strings and its translations).");
            }
        }
        foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(DocumentTemplateSO)))
        {
            var template = AssetDatabase.LoadAssetAtPath<DocumentTemplateSO>(AssetDatabase.GUIDToAssetPath(guid));
            if (template != null)
                Check(template.name, DocumentForm.PrintedWords(template, lib.Agency));
        }
        foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(FormSpecSO)))
        {
            var form = AssetDatabase.LoadAssetAtPath<FormSpecSO>(AssetDatabase.GUIDToAssetPath(guid));
            if (form != null)
                Check(form.name, FormLayout.PrintedWords(form.form, form.Page(lib.Agency)));
        }
        return problems;
    }

    /// <summary>Each problem of each traveller document template's form, one message each.</summary>
    private static List<string> FormFitProblems(ContentLibrarySO lib)
    {
        var problems = new List<string>();
        string[] styles = AssetDatabase.FindAssets("t:" + nameof(FormStyleSO));
        FormStyleSO style = styles.Length > 0 ? AssetDatabase.LoadAssetAtPath<FormStyleSO>(AssetDatabase.GUIDToAssetPath(styles[0])) : null;
        FormMetrics metrics = style != null ? style.metrics : new FormMetrics();
        int longestOrigin = lib.LongestOriginLabel;
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
