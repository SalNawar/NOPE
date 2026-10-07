using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// The cream 80s scanner's art (Track BR, Saleh 2026-10-08: "the cream: it
/// auto opens when you drag a document near and makes a xerox sound as it
/// quickly scans, then opens again"; the model and sounds of
/// tools/props/scanner/scanner_model.py and tools/audio/make_scanner_sfx.py):
/// Office/Scanner's Machine, the model in the desk's materials (NOPE/Desk
/// Anime for the solid parts, URP Unlit for what glows, Unlit Transparent for
/// the two glasses), its hinged Lid, the SweepBar raised over the scanning
/// paper (it shows through the shut lid's smoked glass), a cyan Spill along
/// the lid's edges for the scan, and live digits on the green Readout. Used
/// only when the model carries every part of its contract (PropArt.Scanner);
/// else BuildDesk keeps the stand-in flatbed (a model missing parts says
/// which).
/// </summary>
public static partial class OfficeSceneUIBuilder
{
    /// <summary>The scanner's model and its badge's texture.</summary>
    private const string ScannerModel = "Assets/Art/Office/Props/Scanner/Scanner.fbx", ScannerBadge = "Assets/Art/Office/Props/Scanner/badge_tex.png";

    /// <summary>Where the paper lies on the scanner's glass (the glass's top, 0.0585, plus a hair; its centre 1 cm behind the middle) and how high the sweep bar runs (over a paper on the glass, under the shut lid's smoked glass).</summary>
    private static readonly Vector3 ScannerArtBed = new Vector3(0f, 0.059f, 0.01f);
    private const float ScannerSweepHeight = 0.0625f;

    /// <summary>The cyan glow leaking from the shut lid's edges: its strips' width (metres) and colour.</summary>
    private const float ScannerSpillWidth = 0.008f;
    private static readonly Color ScannerSpillColour = new Color(0.2f, 0.85f, 0.8f, 0.45f);

    /// <summary>The readout's digits' green (the phosphor's).</summary>
    private static readonly Color ScannerReadoutInk = new Color(0.43f, 1f, 0.59f);

    /// <summary>
    /// The scanner's art under <paramref name="scanner"/> (Machine, its parts in
    /// the desk's materials, no shadows cast by its glows), wired on the
    /// DeskScanner through <paramref name="so"/> (its bed, height, sweep bar,
    /// lid, spill and readout; no feeder tray or lamp: the art has none yet);
    /// false (nothing built) when the model is missing or lacks a part.
    /// </summary>
    private static bool BuildScannerArt(Transform scanner, SerializedObject so, out GameObject machine)
    {
        machine = null;
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(ScannerModel);
        bool Has(string part) => model != null && model.transform.Find(part) != null;
        if (!PropArt.UseArt(model != null, PropArt.Scanner, Has))
        {
            if (model != null)
                Debug.LogWarning($"[TimeDesk] {ScannerModel} lacks {string.Join(", ", PropArt.Missing(PropArt.Scanner, Has))}: the scanner keeps its stand-in.");
            return false;
        }

        machine = Object.Instantiate(model, scanner, false);
        machine.name = "Machine";
        Transform m = machine.transform;
        m.localPosition = Vector3.zero;
        m.localRotation = Quaternion.identity;
        m.localScale = Vector3.one;
        foreach (MeshRenderer r in machine.GetComponentsInChildren<MeshRenderer>(true))
        {
            r.sharedMaterials = r.sharedMaterials.Select(x => ScannerMaterial(x != null ? x.name : null)).ToArray();
            if (r.sharedMaterials.Any(x => x != null && x.shader != null && x.shader.name.Contains("Unlit")))
                r.shadowCastingMode = ShadowCastingMode.Off;
        }

        // The machine's height with the lid shut: the top of every part (all lie unrotated in the machine's space).
        float top = 0f;
        foreach (MeshFilter f in machine.GetComponentsInChildren<MeshFilter>(true))
            if (f.sharedMesh != null)
                top = Mathf.Max(top, m.InverseTransformPoint(f.transform.TransformPoint(f.sharedMesh.bounds.max)).y);

        Transform sweep = m.Find("SweepBar");
        sweep.localPosition = new Vector3(sweep.localPosition.x, ScannerSweepHeight, sweep.localPosition.z);
        sweep.gameObject.SetActive(false);
        Transform lid = m.Find("Lid");

        so.FindProperty("bedCentre").vector3Value = ScannerArtBed;
        so.FindProperty("machineHeight").floatValue = top;
        SetRef(so, "sweepBar", sweep);
        SetRef(so, "lid", lid);
        SetRef(so, "spill", ScannerSpill(m, lid));
        SetRef(so, "readout", ScannerReadout(m.Find("Readout")));
        SetRef(so, "feederTray", null);
        SetRef(so, "analysisLamp", null);
        return true;
    }

    /// <summary>The cyan glow strips along the shut lid's front and side edges, on the deck just outside it (inactive: the scanner shows them while a scan runs).</summary>
    private static GameObject ScannerSpill(Transform machine, Transform lid)
    {
        Transform spill = EnsureChild(machine, "Spill");
        Bounds b = default;
        bool any = false;
        foreach (MeshFilter f in lid.GetComponentsInChildren<MeshFilter>(true))
            if (f.sharedMesh != null)
                foreach (Vector3 corner in Corners(f.sharedMesh.bounds))
                {
                    Vector3 p = machine.InverseTransformPoint(f.transform.TransformPoint(corner));
                    if (any)
                        b.Encapsulate(p);
                    else
                        b = new Bounds(p, Vector3.zero);
                    any = true;
                }
        float y = lid.localPosition.y + 0.0004f, w = ScannerSpillWidth;
        Material glow = EnsureMaterial("ScannerArt_Spill", "Universal Render Pipeline/Unlit", x =>
        {
            x.SetColor("_BaseColor", ScannerSpillColour);
            x.SetFloat("_Surface", 1f);
            x.SetFloat("_Blend", 2f); // additive: light, not paint
            UnityEditor.BaseShaderGUI.SetMaterialKeywords(x);
        });
        PrimitivePart(spill, "Front", PrimitiveType.Cube, new Vector3(b.center.x, y, b.min.z - w / 2f), new Vector3(b.size.x * 0.94f, 0.0008f, w), glow);
        PrimitivePart(spill, "Left", PrimitiveType.Cube, new Vector3(b.min.x - w / 2f, y, b.center.z - 0.01f), new Vector3(w, 0.0008f, b.size.z * 0.8f), glow);
        PrimitivePart(spill, "Right", PrimitiveType.Cube, new Vector3(b.max.x + w / 2f, y, b.center.z - 0.01f), new Vector3(w, 0.0008f, b.size.z * 0.8f), glow);
        foreach (MeshRenderer r in spill.GetComponentsInChildren<MeshRenderer>(true))
            r.shadowCastingMode = ShadowCastingMode.Off;
        spill.gameObject.SetActive(false);
        return spill.gameObject;
    }

    private static Vector3[] Corners(Bounds b) =>
        new[] { -1f, 1f }.SelectMany(x => new[] { -1f, 1f }.SelectMany(y => new[] { -1f, 1f }.Select(z => b.center + Vector3.Scale(b.extents, new Vector3(x, y, z))))).ToArray();

    /// <summary>The green readout's live digits (000, counting to 100 as a scan runs: DeskScanner), lying on the readout's slanted glass in the kit's mono face.</summary>
    private static TMP_Text ScannerReadout(Transform readout)
    {
        var filter = readout.GetComponent<MeshFilter>();
        if (filter == null || filter.sharedMesh == null || filter.sharedMesh.normals.Length == 0)
            return null;
        Mesh mesh = filter.sharedMesh;
        Vector3 normal = mesh.normals[0].normalized;
        Vector3 up = Vector3.ProjectOnPlane(Vector3.up, normal).normalized;
        var go = new GameObject("Digits", typeof(RectTransform), typeof(TextMeshPro));
        go.transform.SetParent(readout, false);
        go.transform.localPosition = mesh.bounds.center + normal * 0.0005f;
        go.transform.localRotation = Quaternion.LookRotation(-normal, up);
        float height = new Vector2(mesh.bounds.size.y, mesh.bounds.size.z).magnitude;
        ((RectTransform)go.transform).sizeDelta = new Vector2(mesh.bounds.size.x * 0.9f, height * 0.8f);
        TextMeshPro text = go.GetComponent<TextMeshPro>();
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiKitFonts.Readout);
        if (font != null)
            text.font = font;
        text.text = "000";
        text.enableAutoSizing = true;
        text.fontSizeMin = 0.005f;
        text.fontSizeMax = 0.2f;
        text.alignment = TextAlignmentOptions.MidlineRight;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.color = ScannerReadoutInk;
        text.sortingLayerID = GameplaySortingLayerId();
        return text;
    }

    /// <summary>The desk material for a slot of the scanner's model by its name (the art's table, SCANNER_ART_READY.txt): the solid parts in NOPE/Desk Anime, the glowing ones URP Unlit, the glasses Unlit Transparent; the readout's glass a plain dark phosphor (its digits are live text).</summary>
    private static Material ScannerMaterial(string slot)
    {
        switch (slot)
        {
            case "Scanner_CreamDark": return AnimeMaterial("ScannerArt_CreamDark", Hex("#BCAE90"));
            case "Scanner_Charcoal": return AnimeMaterial("ScannerArt_Charcoal", Hex("#38393E"));
            case "Scanner_LidFrame": return AnimeMaterial("ScannerArt_LidFrame", Hex("#5F5C5A"));
            case "Scanner_ButtonCream": return AnimeMaterial("ScannerArt_ButtonCream", Hex("#ECE4D0"));
            case "Scanner_ButtonBlue": return AnimeMaterial("ScannerArt_ButtonBlue", Hex("#3F6FA8"));
            case "Scanner_ButtonOrange": return AnimeMaterial("ScannerArt_ButtonOrange", Hex("#E2643A"));
            case "Scanner_Vent": return AnimeMaterial("ScannerArt_Vent", Hex("#24242A"));
            case "Scanner_Slot": return AnimeMaterial("ScannerArt_Slot", Hex("#121214"));
            case "Scanner_Rubber": return AnimeMaterial("ScannerArt_Rubber", Hex("#2A2826"));
            case "Scanner_Badge":
                return Shader.Find("NOPE/Desk Anime") != null
                    ? EnsureMaterial("ScannerArt_Badge", "NOPE/Desk Anime", x =>
                    {
                        x.SetColor("_BaseColor", Color.white);
                        x.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(ScannerBadge));
                    })
                    : LitMaterial("ScannerArt_Badge", Hex("#2C2C30"), 0.3f);
            case "Scanner_BedGlow": return UnlitMaterial("ScannerArt_BedGlow", Hex("#33D9CC"));
            case "Scanner_Sweep": return UnlitMaterial("ScannerArt_Sweep", Hex("#DFFFFA"));
            case "Scanner_Readout": return UnlitMaterial("ScannerArt_Readout", Hex("#0A2414"));
            case "Scanner_ReadoutGreen": return UnlitMaterial("ScannerArt_ReadoutGreen", Hex("#3CFF86"));
            case "Scanner_LED": return UnlitMaterial("ScannerArt_LED", Hex("#FF2A1A"));
            case "Scanner_LidSmoke": return GlassMaterial("ScannerArt_LidSmoke", Hex("#262C31", 0.6f));
            case "Scanner_Glass": return GlassMaterial("ScannerArt_Glass", Hex("#A8EEE8", 0.22f));
            default: return AnimeMaterial("ScannerArt_Cream", Hex("#DCD0B4"));
        }
    }

    private static Material UnlitMaterial(string name, Color colour) =>
        EnsureMaterial(name, "Universal Render Pipeline/Unlit", x => x.SetColor("_BaseColor", colour));

    /// <summary>A see-through glass (URP Unlit, transparent, alpha blended: Detain_Glass's way).</summary>
    private static Material GlassMaterial(string name, Color colour) =>
        EnsureMaterial(name, "Universal Render Pipeline/Unlit", x =>
        {
            x.SetColor("_BaseColor", colour);
            x.SetFloat("_Surface", 1f);
            x.SetFloat("_Blend", 0f);
            UnityEditor.BaseShaderGUI.SetMaterialKeywords(x);
        });

    private static Color Hex(string hex, float alpha = 1f)
    {
        ColorUtility.TryParseHtmlString(hex, out Color c);
        c.a = alpha;
        return c;
    }

    /// <summary>The stand-in flatbed's height over the desk (metres, its hinge's top).</summary>
    private const float StandInScannerHeight = 0.075f;

    /// <summary>
    /// The stand-in flatbed under <paramref name="scanner"/> where the cream
    /// scanner's art is missing (Placeholder: a base, a bed, a hinge, a light,
    /// the upgrades' feeder tray and analysis lamp, inactive until owned: SC6)
    /// and its glowing SweepBar (drop and go, the scanner app spec §1: over the
    /// scanning paper, hidden while idle), wired on the DeskScanner through
    /// <paramref name="so"/> (no lid, spill or readout). Returns the machine.
    /// </summary>
    private static GameObject BuildScannerStandIn(Transform scanner, SerializedObject so)
    {
        Transform machine = EnsureChild(scanner, "Placeholder");
        PrimitivePart(machine, "Base", PrimitiveType.Cube, new Vector3(0f, 0.025f, 0f), new Vector3(0.4f, 0.05f, 0.32f), LitMaterial("Placeholder_ScannerBody", new Color(0.24f, 0.33f, 0.31f), 0.35f));
        PrimitivePart(machine, "Bed", PrimitiveType.Cube, new Vector3(0f, 0.051f, 0.01f), new Vector3(0.34f, 0.004f, 0.25f), LitMaterial("Placeholder_ScannerGlass", new Color(0.08f, 0.16f, 0.17f), 0.85f));
        PrimitivePart(machine, "Hinge", PrimitiveType.Cube, new Vector3(0f, 0.06f, 0.15f), new Vector3(0.4f, 0.03f, 0.03f), LitMaterial("Placeholder_ScannerTrim", new Color(0.84f, 0.78f, 0.65f), 0.3f));
        PrimitivePart(machine, "Light", PrimitiveType.Cube, new Vector3(0.16f, 0.052f, -0.135f), new Vector3(0.02f, 0.006f, 0.02f), LitMaterial("Placeholder_ScannerLight", new Color(0.35f, 0.95f, 0.45f), 0.6f));
        GameObject tray = UpgradePart(machine, "FeederTray", new Vector3(0f, 0.09f, 0.19f), new Vector3(0.3f, 0.006f, 0.12f), Quaternion.Euler(-35f, 0f, 0f), LitMaterial("Placeholder_ScannerTrim", new Color(0.84f, 0.78f, 0.65f), 0.3f));
        GameObject lamp = UpgradePart(machine, "AnalysisLamp", new Vector3(0f, 0.11f, -0.1f), new Vector3(0.3f, 0.014f, 0.024f), Quaternion.identity, LitMaterial("Placeholder_ScannerLamp", new Color(0.78f, 0.72f, 0.98f), 0.7f));
        GameObject sweep = PrimitivePart(scanner, "SweepBar", PrimitiveType.Cube, new Vector3(0f, 0.062f, -0.115f), new Vector3(0.36f, 0.004f, 0.012f),
                                         EnsureMaterial("Placeholder_ScannerSweep", "Universal Render Pipeline/Unlit", m => m.SetColor("_BaseColor", new Color(0.45f, 1f, 0.55f))));
        sweep.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
        sweep.SetActive(false);
        so.FindProperty("bedCentre").vector3Value = new Vector3(0f, 0.056f, 0.01f);
        so.FindProperty("machineHeight").floatValue = StandInScannerHeight;
        SetRef(so, "feederTray", tray);
        SetRef(so, "analysisLamp", lamp);
        SetRef(so, "sweepBar", sweep.transform);
        SetRef(so, "lid", null);
        SetRef(so, "spill", null);
        SetRef(so, "readout", null);
        return machine.gameObject;
    }
}
