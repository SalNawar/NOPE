using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// DOC-LOOK's gate checks (FIX_PLAN DOC-LOOK; BUG-9, 29, 63). Static: every
/// document drawn on its art prints its labels over its clean face and its
/// data grew (doclook.forms.regrown); the office's art imports with mipmaps,
/// trilinear and the high-quality compression, the art side's hall layers
/// untouched (doclook.import.office); the modelled cap height of every
/// document's smallest data and label at the big mat's reading view against
/// BUG-9's floors (doclook.forms.capModel: 10 px data at A1, 7 px at A2,
/// labels 6 px at A2), at merge level so the merge gate shows the shortfall
/// (known red until FORMS-REDESIGN redesigns the 8 art forms with larger
/// places: the orchestrator's decision 2 of 2026-10-20, which approved the
/// 0.5 fit floor and labels x1.3 as the interim; docs/pending/DOC-LOOK.md,
/// "Decided by the orchestrator", and docs/pending/FORMS-REDESIGN.md).
/// Played, at merge level: the pointer on a shut passport's and the shut
/// folder's hidden halves (doclook.play.coverHits, the doclook_print
/// scenario's coverHits op: they take no hover, click or drag, and each step
/// checks the open or shut state it names; it needs no later track's
/// framing, so every merge from DOC-LOOK's on needs its results taken at
/// HEAD). Demo only: the
/// measured cap height on screen (doclook.play.capHeight, the doclook_cap
/// scenario's capHeight op), which counts only at the final paper size and
/// framing.
/// </summary>
public static class DocLookChecks
{
    private const string Owner = "DOC-LOOK";

    /// <summary>
    /// The modelled reading view's scale at the paper (pixels per metre at
    /// 1080 lines): today's reading view (about 1041, measured from the
    /// audit's DESTINATION at 5.1 px cap on the passport at its desk size)
    /// times the big mat's 0.67 (Saleh's "keep a big mat size": papers show at
    /// about 67%, DESK-PLAN-HOMEMAP Q5 B). The on-screen measure
    /// (doclook.play.capHeight) is the truth; this one runs without Unity
    /// playing.
    /// </summary>
    public const float ModelPixelsPerMetre = 1041f * 0.67f;

    /// <summary>The least growth of a document's smallest data over the art's own size that counts as re-laid (the art's boxes cap it: 1.18x to 1.43x on the eight documents).</summary>
    public const float LeastGrowth = 1.15f;

    /// <summary>Every document drawn on its art with a clean face prints its labels on it (FormArt.printLabels) and its smallest data grew by LeastGrowth or more, none smaller than the art set it.</summary>
    [GateCheck("doclook.forms.regrown", Owner)]
    public static GateResult Regrown()
    {
        var problems = new List<string>();
        var lines = new List<string>();
        foreach ((DocumentTemplateSO template, FormSpec spec) in ArtTemplates())
        {
            FormArt art = spec.look.art;
            if (!art.clean)
                continue;
            if (!art.printLabels)
                problems.Add($"{template.name} has a clean face but does not print its labels on it");
            float grown = CapHeight.Smallest(Place(spec), false);
            float plain = CapHeight.Smallest(Place(Ungrown(spec)), false);
            float growth = plain > 0f ? grown / plain : 0f;
            lines.Add(string.Format(CultureInfo.InvariantCulture, "{0} x{1:0.00}", template.formNumber, growth));
            if (growth + 1e-3f < LeastGrowth)
                problems.Add(string.Format(CultureInfo.InvariantCulture, "{0}'s smallest data grew only x{1:0.00}", template.name, growth));
        }
        return GateResult.Of("doclook.forms.regrown", Owner, problems.Count == 0, string.Join(", ", lines), string.Join("; ", problems));
    }

    /// <summary>Every document's smallest data, modelled at the big mat's reading view (ModelPixelsPerMetre) at its desk size, reads at 10 px cap at A1 and 7 px at A2, and its smallest label at 6 px at A2 (BUG-9's floors); each line says how much larger the smallest data must print to read. Merge level: the gate shows the shortfall at every merge.</summary>
    [GateCheck("doclook.forms.capModel", Owner)]
    public static GateResult CapModel()
    {
        DeskConfigSO config = AssetDatabase.FindAssets("t:" + nameof(DeskConfigSO)).Select(g => AssetDatabase.LoadAssetAtPath<DeskConfigSO>(AssetDatabase.GUIDToAssetPath(g))).FirstOrDefault(c => c != null);
        if (config == null)
            return GateResult.Fail("doclook.forms.capModel", Owner, "no DeskConfigSO", "The desk's config (Desk_Default) is missing.");
        var problems = new List<string>();
        var lines = new List<string>();
        foreach ((DocumentTemplateSO template, FormSpec spec) in ArtTemplates())
        {
            PlacedForm form = Place(spec);
            float smallest = CapHeight.Smallest(form, false), label = CapHeight.Smallest(form, true);
            float metres = DeskHeight(spec, config);
            float a1 = CapHeight.Pixels(smallest, form.PageHeight, metres, CapHeight.ValueCapPerEm, ModelPixelsPerMetre, CapHeight.A1Lines);
            float a2 = CapHeight.Pixels(smallest, form.PageHeight, metres, CapHeight.ValueCapPerEm, ModelPixelsPerMetre, CapHeight.A2Lines);
            float labelA2 = CapHeight.Pixels(label, form.PageHeight, metres, CapHeight.LabelCapPerEm, ModelPixelsPerMetre, CapHeight.A2Lines);
            lines.Add(string.Format(CultureInfo.InvariantCulture, "{0} data {1:0.0}/{2:0.0} px, labels {3:0.0} px at A2 (x{4:0.0} to read)", template.formNumber, a1, a2, labelA2, CapHeight.Needed(a1, a2)));
            if (!CapHeight.DataReads(a1, a2))
                problems.Add(string.Format(CultureInfo.InvariantCulture, "{0}: data {1:0.0} px at A1, {2:0.0} px at A2 (floor {3:0}/{4:0})", template.name, a1, a2, CapHeight.DataFloorA1, CapHeight.DataFloorA2));
            if (label > 0f && labelA2 + 1e-3f < CapHeight.LabelFloorA2)
                problems.Add(string.Format(CultureInfo.InvariantCulture, "{0}: labels {1:0.0} px at A2 (floor {2:0})", template.name, labelA2, CapHeight.LabelFloorA2));
        }
        return GateResult.Of("doclook.forms.capModel", Owner, problems.Count == 0, string.Join(", ", lines), string.Join("; ", problems));
    }

    /// <summary>The on-screen cap height of every printed value and label at A1 and A2 passed (the doclook_cap scenario's capHeight op).</summary>
    [GateCheck("doclook.play.capHeight", Owner, GateKind.Play, DemoOnly = true)]
    public static GateResult PlayCapHeight() => GateResults.Require("doclook.play.capHeight", Owner, "doclook_cap.capHeight.*");

    /// <summary>The hidden half of a shut passport and of the shut folder takes no pointer, the shown half does, and both halves do once open (the doclook_print scenario's coverHits op; DOC-LOOK review, S3): each step finds a booklet and the folder in the state it names, or fails. Merge level: the fix depends on no later track, so a merge gate needs these results taken at HEAD (recipe R7b).</summary>
    [GateCheck("doclook.play.coverHits", Owner, GateKind.Play)]
    public static GateResult PlayCoverHits() => GateResults.Require("doclook.play.coverHits", Owner, "doclook_print.coverHits.*");

    /// <summary>Every texture of the office's art (ArtSlots.OfficeImports: never the art side's hall layers) imports with mipmaps, trilinear and the high-quality compression (OfficeArtImporter; BUG-63).</summary>
    [GateCheck("doclook.import.office", Owner)]
    public static GateResult OfficeImport()
    {
        var off = new List<string>();
        int checkedCount = 0, skipped = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { ArtSlots.OfficeArtRoot.TrimEnd('/') }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!ArtSlots.OfficeImports(path))
            {
                skipped++;
                continue;
            }
            if (!(AssetImporter.GetAtPath(path) is TextureImporter importer) || importer.textureType == TextureImporterType.NormalMap
                || importer.textureType == TextureImporterType.Lightmap || importer.textureType == TextureImporterType.DirectionalLightmap
                || importer.textureType == TextureImporterType.Shadowmask || importer.textureType == TextureImporterType.Cursor)
                continue;
            checkedCount++;
            if (!importer.mipmapEnabled || importer.filterMode != FilterMode.Trilinear || importer.textureCompression != TextureImporterCompression.CompressedHQ)
                off.Add(path);
        }
        return GateResult.Of("doclook.import.office", Owner, off.Count == 0, $"{checkedCount} checked, {off.Count} off, {skipped} under the hall layers left alone",
                             off.Count == 0 ? string.Empty : "Not yet imported by OfficeArtImporter (reimport them): " + string.Join(", ", off.Take(20)));
    }

    /// <summary>Every document template drawn on its art, with its form.</summary>
    private static IEnumerable<(DocumentTemplateSO template, FormSpec spec)> ArtTemplates()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(DocumentTemplateSO)))
        {
            var template = AssetDatabase.LoadAssetAtPath<DocumentTemplateSO>(AssetDatabase.GUIDToAssetPath(guid));
            if (template != null && ArtLayout.IsArt(template.form))
                yield return (template, template.form);
        }
    }

    /// <summary><paramref name="spec"/>'s art placed one unit wide, every field showing a value of its category's longest (an origin at 30 characters).</summary>
    private static PlacedForm Place(FormSpec spec)
    {
        int count = spec.look.art.fields.Where(f => f != null).Select(f => f.field).DefaultIfEmpty(-1).Max() + 1;
        var labels = Enumerable.Range(0, count).Select(i => "FIELD").ToList();
        var data = new FormData { FieldLabels = labels, FieldValues = FormLayout.Reserve(labels.ConvertAll(_ => string.Empty), labels.ConvertAll(_ => 12)).ToList() };
        return ArtLayout.Place(spec, data, 1f, null);
    }

    /// <summary>A copy of <paramref name="spec"/> with no growth (the art's own sizes).</summary>
    private static FormSpec Ungrown(FormSpec spec)
    {
        FormSpec copy = JsonUtility.FromJson<FormSpec>(JsonUtility.ToJson(spec));
        copy.look.art.labelGrowth = 1f;
        copy.look.art.valueGrowth = 1f;
        return copy;
    }

    /// <summary>A document's height on the desk (metres): the desk's paper height times its look's scale, brought to its reading size (DeskZones.ReadingScale, the art's reading share), as DeskDocument sizes it on the desk.</summary>
    private static float DeskHeight(FormSpec spec, DeskConfigSO config)
    {
        float height = config.paperSize.y * spec.look.Scale;
        float width = height * spec.look.AspectOr(1f);
        float reading = spec.look.art.reading > 0f ? spec.look.art.reading : 1f;
        return height * reading * DeskZones.ReadingScale(width, height, config.readingHeight, config.paperSize.x / config.paperSize.y);
    }
}
