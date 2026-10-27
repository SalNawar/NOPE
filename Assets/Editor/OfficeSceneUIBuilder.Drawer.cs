using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;

/// <summary>
/// The brass stamp rack's art (Track BR, Saleh 2026-10-08: the brass
/// concept; flipped and deep since Saleh's 1016a playtest: its labelled bar
/// on the far side, the daters in a cast-brass box in front of it; a fixed
/// rack since his D4, the drawer never slides): the model
/// tools/props/brass_drawer.py writes (BrassDrawer.fbx, every part in the
/// rack's space, upright) installed under the stamp rack in the desk's
/// NOPE/Desk Anime materials (cel brass since BUG-16a: two tones, a painted
/// highlight, an ink rim, no photo texture), its mechanism wired
/// (BrassDrawer) and its enamel plates carrying the verdict words as live
/// text. Used only when the model is there with every part of its
/// contract (PropArt.BrassDrawer); otherwise the stamp tray keeps its built
/// lip (the fallback), and a model missing parts says which.
/// </summary>
public static partial class OfficeSceneUIBuilder
{
    /// <summary>The drawer's model.</summary>
    private const string BrassDrawerModel = "Assets/Art/Office/Props/BrassDrawer/BrassDrawer.fbx";

    /// <summary>The desk's cel shader, the rack's brass is painted in.</summary>
    private const string DeskAnimeShader = "NOPE/Desk Anime";

    /// <summary>The model's measures the game needs (tools/props/brass_drawer.py: keep in step): a cradle's pivot in front of the dater's centre (its front-foot edge, toward the chair: the dater folds that way), the pinions' radius, the levers' length, the rack's pin past its middle, and the plates' tilt toward the chair (degrees from flat) and their face's height over their origin.</summary>
    private const float BrassPivotDepth = 0.023f, BrassPinionRadius = 0.011f, BrassLeverLength = 0.021f, BrassPinReach = 0.027f, BrassPlateTilt = 35f, BrassPlateFace = 0.0019f;

    /// <summary>The words' box on a plate (metres) and their ink (the kit's bone).</summary>
    private static readonly Vector2 BrassPlateText = new Vector2(0.08f, 0.0115f);
    private static readonly Color BrassPlateInk = new Color(0.93f, 0.9f, 0.82f);

    /// <summary>
    /// The brass drawer under <paramref name="rack"/> (its Drawer child: the
    /// model's parts, each renderer's slots in the desk's materials, casting
    /// no light's shadow: its drawn contact shadow is laid with it,
    /// DeskStampTray.Lay) with its mechanism (BrassDrawer: the
    /// cradles, pinions, racks and levers, DENIED then APPROVED); null when the
    /// model is missing or lacks a part (a warning names them: the fallback
    /// lip is built instead).
    /// </summary>
    private static BrassDrawer BuildBrassDrawer(Transform rack)
    {
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(BrassDrawerModel);
        bool Has(string part) => model != null && model.transform.Find(part) != null;
        if (!PropArt.UseArt(model != null, PropArt.BrassDrawer, Has))
        {
            if (model != null)
                Debug.LogWarning($"[TimeDesk] {BrassDrawerModel} lacks {string.Join(", ", PropArt.Missing(PropArt.BrassDrawer, Has))}: the stamp rack keeps its built lip.");
            return null;
        }

        GameObject art = Object.Instantiate(model, rack, false);
        art.name = "Drawer";
        art.transform.localPosition = Vector3.zero;
        art.transform.localRotation = Quaternion.identity;
        art.transform.localScale = Vector3.one;
        foreach (MeshRenderer r in art.GetComponentsInChildren<MeshRenderer>(true))
            r.sharedMaterials = r.sharedMaterials.Select(m => DrawerMaterial(m != null ? m.name : null)).ToArray();

        BrassDrawer drawer = art.AddComponent<BrassDrawer>();
        var so = new SerializedObject(drawer);
        foreach ((string field, string part) in new[] { ("cradles", "Cradle"), ("pinions", "Pinion"), ("racks", "Rack"), ("levers", "Lever") })
        {
            SerializedProperty list = so.FindProperty(field);
            list.arraySize = 2;
            list.GetArrayElementAtIndex(DrawerSequence.Denied).objectReferenceValue = art.transform.Find(part + "Denied");
            list.GetArrayElementAtIndex(DrawerSequence.Approved).objectReferenceValue = art.transform.Find(part + "Approved");
        }
        so.FindProperty("pinionRadius").floatValue = BrassPinionRadius;
        so.FindProperty("leverLength").floatValue = BrassLeverLength;
        so.FindProperty("pinReach").floatValue = BrassPinReach;
        so.ApplyModifiedPropertiesWithoutUndo();
        return drawer;
    }

    /// <summary>The verdict <paramref name="word"/> on an enamel <paramref name="plate"/> (live TextMeshPro in the kit's condensed label face, lying on the plate's tilted face; the language track can translate it like any UI string).</summary>
    private static TextMeshPro PlateWord(Transform plate, string word)
    {
        if (plate == null)
            return null;
        Vector3 face = Quaternion.Euler(-BrassPlateTilt, 0f, 0f) * Vector3.up;
        TextMeshPro text = FlatText(plate, "Word", face * BrassPlateFace, BrassPlateText, 0.2f, BrassPlateInk, FontStyles.Bold);
        text.transform.localRotation = Quaternion.Euler(90f - BrassPlateTilt, 0f, 0f);
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiKitFonts.Label);
        if (font != null)
            text.font = font;
        text.characterSpacing = 6f;
        text.text = word;
        return text;
    }

    /// <summary>The desk material for a slot of the drawer's model by its name (Brass, BrassDark, Steel, EnamelRed, EnamelGreen; anything else is Brass).</summary>
    private static Material DrawerMaterial(string slot)
    {
        switch (slot)
        {
            case "BrassDark":
                return BrassMaterial("Drawer_BrassDark", new Color(0.6f, 0.43f, 0.21f), 0.2f);
            case "Steel":
                return DaterMaterial("Drawer_Steel", new Color(0.4f, 0.4f, 0.43f), 0.3f, 0.12f);
            case "EnamelRed":
                return DaterMaterial("Drawer_EnamelRed", new Color(0.66f, 0.13f, 0.11f), 0.35f, 0.1f);
            case "EnamelGreen":
                return DaterMaterial("Drawer_EnamelGreen", new Color(0.12f, 0.42f, 0.22f), 0.35f, 0.1f);
            default:
                return BrassMaterial("Drawer_Brass", new Color(0.88f, 0.68f, 0.31f), 0.35f);
        }
    }

    /// <summary>
    /// A brass material of the rack, cel brass (BUG-16a: the drawer was the
    /// only photoreal metal on a cel desk, URP Lit with a brushed texture that
    /// had no mipmaps and turned olive-grey at night) in the desk's
    /// NOPE/Desk Anime: <paramref name="colour"/> in two tones (a hard band
    /// edge, the midtone lifted to the light), a painted highlight of
    /// <paramref name="highlight"/> taking the brass's own hue, a contour, and
    /// LIGHT's painted-highlight and ink-rim seams full on; no texture. Set on
    /// every build, so the old URP Lit asset is converted in place (its GUID
    /// kept); without the shader the asset stays as it is.
    /// </summary>
    private static Material BrassMaterial(string name, Color colour, float highlight)
    {
        Material m = EnsureMaterial(name, DeskAnimeShader, null);
        Shader anime = Shader.Find(DeskAnimeShader);
        if (m == null || anime == null)
            return m;
        if (m.shader != anime)
        {
            foreach (string texture in new[] { "_BaseMap", "_MainTex" })
                if (m.HasProperty(texture))
                    m.SetTexture(texture, null);
            m.shader = anime;
        }
        m.SetTexture("_BaseMap", null);
        m.SetColor("_BaseColor", colour);
        m.SetFloat("_ShadowValue", 0.5f);
        m.SetFloat("_MidValue", 1f);
        m.SetFloat("_BandSoftness", 0.02f);
        m.SetFloat("_ShadowThreshold", 0f);
        m.SetFloat("_LightThreshold", 0.62f);
        m.SetFloat("_HighlightStrength", highlight);
        m.SetFloat("_HighlightSize", 0.1f);
        m.SetFloat("_Metallic", 1f);
        m.SetFloat("_EdgeStrength", 0.22f);
        m.SetFloat("_PaintedHighlight", 1f);
        m.SetFloat("_InkRim", 1f);
        EditorUtility.SetDirty(m);
        AssetDatabase.SaveAssetIfDirty(m);
        return m;
    }
}
