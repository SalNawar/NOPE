using System.Collections.Generic;
using System.Linq;
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
/// Library check the same. A form drawn on its art is measured in the art's
/// value face (FormStyleSO.artValueFont), the face its values print in
/// (BUG-9: the check measured them in LiberationSans, so a value that fit the
/// check overflowed its place on the paper), and none of the content's names
/// or origins has a word wider than its place there even at the least size
/// (ArtLayout.CutWords: the paper would cut it with a full stop, and a cut
/// value reads as a false discrepancy against another paper).
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
        var artProbe = new GameObject("FormsCheckArtText", typeof(TextMeshPro)) { hideFlags = HideFlags.HideAndDontSave };
        try
        {
            var measure = new TmpFormText(probe.GetComponent<TextMeshPro>());
            TextMeshPro artText = artProbe.GetComponent<TextMeshPro>();
            if (style != null && style.artValueFont != null)
            {
                artText.font = style.artValueFont;
                artText.fontSharedMaterial = style.artValueFont.material;
            }
            var artMeasure = new TmpFormText(artText);
            var seen = new HashSet<DocumentTemplateSO>();
            foreach (CaseBlueprintSO blueprint in TravellerBlueprints(lib))
                foreach (DocumentTemplateSO template in blueprint != null && blueprint.DocumentTemplates != null ? blueprint.DocumentTemplates : new DocumentTemplateSO[0])
                {
                    if (template == null || !seen.Add(template))
                        continue;
                    foreach (string problem in DocumentForm.Problems(template, lib.Agency, longestOrigin, metrics, ArtLayout.IsArt(template.form) ? artMeasure : measure))
                        problems.Add($"{template.displayName} ({template.name}): {problem} (its form, FormLayout.Check; edit the template's form or FormStyle_Agency).");
                    foreach (string problem in ArtLayout.CutWords(template.form, ContentWords(lib, template), artMeasure))
                        problems.Add($"{template.displayName} ({template.name}): {problem}.");
                }
        }
        finally
        {
            Object.DestroyImmediate(probe);
            Object.DestroyImmediate(artProbe);
        }
        return problems;
    }

    /// <summary>How many of a category's longest words (by letters) CutWords measures in each field.</summary>
    private const int CutWordsPerField = 12;

    /// <summary>The content's longest words each of <paramref name="template"/>'s fields prints (ArtLayout.CutWords), in capitals as a printed form sets them: a Name field's and a Signature field's (a holder signs with their given name, CaseFactory) every given name a traveller can carry (every place's lists, the 2150 citizens' among them, and every premade's own name, CaseFactory's two sources; a returning traveller keeps the name drawn from them), a Destination field's every place's origin label; none for the other fields. Those keep only FormLayout.Check's probe (ArtLayout.Problems' word check), whose words stand in for theirs only where they are as long: a status (8 characters), an incident number (9), a credits figure (9) and a fact's word (up to 14) are longer than their probes' longest words (6, 6, 6 and 11), so they are not measured whole (docs/pending/DOC-LOOK.md, "The words").</summary>
    private static IReadOnlyList<IReadOnlyList<string>> ContentWords(ContentLibrarySO lib, DocumentTemplateSO template)
    {
        List<string> Longest(IEnumerable<string> texts) => texts.Where(t => !string.IsNullOrWhiteSpace(t)).SelectMany(TextFit.Words).Select(ArtLayout.Capitals)
            .Distinct().OrderByDescending(w => w.Length).Take(CutWordsPerField).ToList();
        List<string> names = Longest(lib.Profiles.Where(p => p != null).SelectMany(p => (p.maleNames ?? new string[0]).Concat(p.femaleNames ?? new string[0]))
                                        .Concat(lib.Legendaries.Where(l => l != null).Select(l => l.displayName)));
        List<string> origins = Longest(lib.Profiles.Where(p => p != null).Select(p => p.OriginLabel));
        var words = new List<IReadOnlyList<string>>();
        foreach (DocumentFieldSpec spec in template.fieldSpecs ?? new DocumentFieldSpec[0])
            words.Add(spec == null ? null : spec.category == ClueCategory.Name || spec.category == ClueCategory.Signature ? names : spec.category == ClueCategory.Destination ? origins : null);
        return words;
    }
}
