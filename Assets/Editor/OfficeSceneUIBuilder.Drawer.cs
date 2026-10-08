using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;

/// <summary>
/// The heavy brass stamp drawer's art (Track BR, Saleh 2026-10-08: the brass
/// concept, "a heavy brass drawer that makes the sound a typewriter makes when
/// the carriage returns"): the model tools/props/brass_drawer.py writes
/// (BrassDrawer.fbx, every part in the rack's space, upright) installed under
/// the stamp rack in the desk's NOPE/Desk Anime materials, its mechanism
/// wired (BrassDrawer) and its enamel plates carrying the verdict words as
/// live text. Used only when the model is there with every part of its
/// contract (PropArt.BrassDrawer); otherwise the stamp tray keeps its built
/// lip (the fallback), and a model missing parts says which.
/// </summary>
public static partial class OfficeSceneUIBuilder
{
    /// <summary>The drawer's model and its brushed brass texture.</summary>
    private const string BrassDrawerModel = "Assets/Art/Office/Props/BrassDrawer/BrassDrawer.fbx", BrassDrawerTexture = "Assets/Art/Office/Props/BrassDrawer/BrassDrawer_Brass.png";

    /// <summary>The model's measures the game needs (tools/props/brass_drawer.py: keep in step): a cradle's pivot behind the dater's centre (its back-foot edge), the pinions' radius, the levers' length, the rack's pin past its middle, and the plates' tilt toward the chair (degrees from flat) and their face's height over their origin.</summary>
    private const float BrassPivotDepth = 0.023f, BrassPinionRadius = 0.011f, BrassLeverLength = 0.021f, BrassPinReach = 0.027f, BrassPlateTilt = 35f, BrassPlateFace = 0.0019f;

    /// <summary>The words' box on a plate (metres) and their ink (the kit's bone).</summary>
    private static readonly Vector2 BrassPlateText = new Vector2(0.08f, 0.0115f);
    private static readonly Color BrassPlateInk = new Color(0.93f, 0.9f, 0.82f);

    /// <summary>
    /// The brass drawer under <paramref name="rack"/> (its Drawer child: the
    /// model's parts, each renderer's slots in the desk's materials, no
    /// shadows: the rack casts none) with its mechanism (BrassDrawer: the
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
                return BrassMaterial("Drawer_BrassDark", new Color(0.56f, 0.39f, 0.18f), 0.22f, false);
            case "Steel":
                return DaterMaterial("Drawer_Steel", new Color(0.4f, 0.4f, 0.43f), 0.3f, 0.12f);
            case "EnamelRed":
                return DaterMaterial("Drawer_EnamelRed", new Color(0.66f, 0.13f, 0.11f), 0.35f, 0.1f);
            case "EnamelGreen":
                return DaterMaterial("Drawer_EnamelGreen", new Color(0.12f, 0.42f, 0.22f), 0.35f, 0.1f);
            default:
                return BrassMaterial("Drawer_Brass", new Color(0.83f, 0.63f, 0.33f), 0.4f, true);
        }
    }

    /// <summary>A brass material in the desk's NOPE/Desk Anime shader: warm gold, its shadow band a darker brown-gold (a warm shadow tint), a bright painted highlight and the metal sheen, the brushed texture on the bright brass (URP Lit where that shader is missing).</summary>
    private static Material BrassMaterial(string name, Color colour, float highlight, bool brushed) =>
        Shader.Find("NOPE/Desk Anime") != null
            ? EnsureMaterial(name, "NOPE/Desk Anime", m =>
            {
                m.SetColor("_BaseColor", colour);
                m.SetVector("_ShadowTint", new Vector4(0.7f, 0.52f, 0.4f, 0f));
                m.SetVector("_LightTint", new Vector4(1.1f, 1.03f, 0.86f, 0f));
                m.SetFloat("_HighlightStrength", highlight);
                m.SetFloat("_HighlightSize", 0.16f);
                m.SetFloat("_EdgeStrength", 0.14f);
                m.SetFloat("_Metallic", 0.55f);
                if (brushed)
                    m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(BrassDrawerTexture));
            })
            : LitMaterial(name, colour, 0.6f);
}
