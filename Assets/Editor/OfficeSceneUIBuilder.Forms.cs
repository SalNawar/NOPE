using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// The office builder's forms parts (redesign phase 4, PC spec FO1, FO7, FO8,
/// FO10): the form style (FormStyleSO, created when missing), the desk paper's
/// printing parts (the text template every printed word clones, the fills and
/// lines meshes, the seal's quad, the quad each pickable box clones) and the
/// form style's checks: the paper's aspect and the contrast pairs with the
/// hover tint and each theme's pick highlight over a box (FormContrast). Each
/// template's form is checked by the desk fit
/// (ContentLibraryValidator.DeskFitProblems). Part of <see cref="OfficeSceneUIBuilder"/>.
/// </summary>
public static partial class OfficeSceneUIBuilder
{
    /// <summary>The forms' style, created by the builder when missing (a designer's edits are kept).</summary>
    private const string FormStylePath = "Assets/Data/Forms/FormStyle_Agency.asset";

    /// <summary>The seal placeholder's side in pixels (a code-drawn ring until the art's agency seal).</summary>
    private const int SealPixels = 128;

    /// <summary>The render queues of the printed layers: the seal, the fills, the lines (the hover and pick quads sit at 2990, the texts at 3000).</summary>
    private const int SealQueue = 2975, FillQueue = 2980, LineQueue = 2995;

    /// <summary>The desk paper's printing parts, wired onto the DeskDocument.</summary>
    private struct PaperPrint
    {
        public TextMeshPro Text;
        public MeshFilter Fills;
        public MeshFilter Lines;
        public Renderer Seal;
        public Renderer Slot;
    }

    /// <summary>Returns the forms' style, creating it with the defaults when missing.</summary>
    private static FormStyleSO EnsureFormStyle()
    {
        FormStyleSO style = AssetDatabase.LoadAssetAtPath<FormStyleSO>(FormStylePath);
        if (style != null)
            return style;

        PlaceholderPng.EnsureFolderTree("Assets/Data/Forms");
        style = ScriptableObject.CreateInstance<FormStyleSO>();
        AssetDatabase.CreateAsset(style, FormStylePath);
        AssetDatabase.SaveAssets();
        return style;
    }

    /// <summary>
    /// The paper's printing parts under the sheet: the text template (its
    /// renderer off; the paper clones it per word and measures with it), the
    /// Fills and Lines meshes in the vertex-coloured form materials, the
    /// inactive Seal quad (the placeholder ring) and the inactive Slot quad
    /// (the hover and pick highlight, cloned per box).
    /// </summary>
    private static PaperPrint BuildPaperPrint(Transform sheet)
    {
        var print = new PaperPrint();
        print.Text = PaperText(sheet, "Text", string.Empty, Vector2.zero, new Vector2(0.1f, 0.02f), false);
        print.Text.GetComponent<MeshRenderer>().enabled = false;

        print.Fills = PrintMesh(sheet, "Fills", FormMaterial("Form_Fill", FillQueue));
        print.Lines = PrintMesh(sheet, "Lines", FormMaterial("Form_Lines", LineQueue));

        Sprite ring = EnsureOfficeShape("form_seal", SealPixels, SealPixels, new Vector2(0.5f, 0.5f), SealRing);
        Material sealMaterial = EnsureMaterial("Form_Seal", "Universal Render Pipeline/Unlit", m =>
        {
            m.SetTexture("_BaseMap", ring.texture);
            m.SetColor("_BaseColor", Color.white);
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_QueueOffset", SealQueue - (int)RenderQueue.Transparent);
            UnityEditor.BaseShaderGUI.SetMaterialKeywords(m);
        });
        PrimitivePart(sheet, "Seal", PrimitiveType.Quad, Vector3.zero, Vector3.one, sealMaterial);
        print.Seal = sheet.Find("Seal").GetComponent<MeshRenderer>();
        ((MeshRenderer)print.Seal).shadowCastingMode = ShadowCastingMode.Off;
        print.Seal.gameObject.SetActive(false);

        Material highlight = EnsureMaterial("PaperRow_Highlight", "Universal Render Pipeline/Unlit", m =>
        {
            m.SetColor("_BaseColor", new Color(0f, 0f, 0f, 0f));
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_QueueOffset", -10f);
            UnityEditor.BaseShaderGUI.SetMaterialKeywords(m);
        });
        PrimitivePart(sheet, "Slot", PrimitiveType.Quad, Vector3.zero, Vector3.one, highlight);
        print.Slot = sheet.Find("Slot").GetComponent<MeshRenderer>();
        ((MeshRenderer)print.Slot).shadowCastingMode = ShadowCastingMode.Off;
        print.Slot.gameObject.SetActive(false);
        return print;
    }

    /// <summary>An empty mesh child of the sheet in <paramref name="material"/> (the paper fills it when it binds).</summary>
    private static MeshFilter PrintMesh(Transform sheet, string name, Material material)
    {
        var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        go.transform.SetParent(sheet, false);
        var renderer = go.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return go.GetComponent<MeshFilter>();
    }

    /// <summary>A vertex-coloured, unlit, see-through material drawn at <paramref name="queue"/> (the form's printed fills or lines).</summary>
    private static Material FormMaterial(string name, int queue)
    {
        Material m = EnsureMaterial(name, "Sprites/Default", mat => mat.SetColor("_Color", Color.white));
        if (m != null && m.renderQueue != queue)
        {
            m.renderQueue = queue;
            EditorUtility.SetDirty(m);
        }
        return m;
    }

    /// <summary>The seal placeholder: a grey double ring with a bar across, text-free (FO8: Temporal Customs' own mark until the art's).</summary>
    private static Color32 SealRing(int x, int y)
    {
        float cx = x - SealPixels / 2f + 0.5f, cy = y - SealPixels / 2f + 0.5f;
        float r = Mathf.Sqrt(cx * cx + cy * cy) / (SealPixels / 2f);
        bool ink = (r > 0.86f && r < 0.97f) || (r > 0.66f && r < 0.72f) || (r < 0.66f && Mathf.Abs(cy) < SealPixels * 0.035f);
        return ink ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
    }

    /// <summary>
    /// The form style's checks (FO7), logged as errors: the desk paper's aspect
    /// against the style's page aspect, and the style's contrast pairs with the
    /// hover tint and the pick highlight of the neutral theme and every culture
    /// over a box. (Each template's form against the paper is the desk fit's,
    /// ContentLibraryValidator.DeskFitProblems.)
    /// </summary>
    private static void CheckFormStyle(ContentLibrarySO library, DeskConfigSO config, FormStyleSO style)
    {
        if (style == null)
            return;
        float paperAspect = config.paperSize.y > 0f ? config.paperSize.x / config.paperSize.y : 0f;
        if (Mathf.Abs(paperAspect - style.metrics.aspect) > 0.002f)
            Debug.LogError($"[TimeDesk] The desk paper is {config.paperSize.x} x {config.paperSize.y} m (aspect {paperAspect:0.000}) but the form style's page aspect is {style.metrics.aspect:0.000}; set FormStyle_Agency's metrics.aspect or Desk_Default.paperSize so the forms fill the paper.");

        var overlays = new List<(string, Rgba)> { ("the hover tint", FormStyleSO.Rgb(config.rowHoverTint)) };
        var themes = new List<ThemeSO>();
        if (library != null)
        {
            if (library.NeutralTheme != null)
                themes.Add(library.NeutralTheme);
            themes.AddRange(library.Themes);
        }
        foreach (ThemeSO theme in themes)
            if (theme != null && theme.Get(ThemeRoleId.SelectionHighlight) is PaletteEntry pick && pick.hasFill)
                overlays.Add(($"the '{theme.cultureId}' pick highlight", FormStyleSO.Rgb(pick.fill)));
        ContrastRules rules = library != null && library.CultureUi.contrast != null ? library.CultureUi.contrast : new ContrastRules();
        foreach (string problem in FormContrast.Problems(style.Palette(), overlays, rules))
            Debug.LogError($"[TimeDesk] {problem} (FormStyle_Agency; the forms' contrast check, FO7).", style);
    }
}
