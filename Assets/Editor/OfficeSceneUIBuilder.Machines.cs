using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// The desk machines (the desk machines spec §1-2; Saleh 2026-10-17: the chip
/// reader "an actual machine that reads the chip", the UV light "an actual uv
/// flashlight"): Office/DeskHardware with its ChipReader and UvTorch, each the
/// model of tools/props/machines (Assets/Art/Office/Gameplay/Machines) in the
/// desk's materials (NOPE/Desk Anime for the solid parts, URP Unlit for what
/// glows, Unlit Transparent for the glasses and the beam), the reader's live
/// readout text and its click box, the torch's click box, its violet spot
/// light and its hum's looping source, each machine's first-day note, and
/// the reveal's materials and textures for the papers' UV layers. A machine
/// whose model lacks a part of its contract (PropArt) is left out with a
/// warning (no stand-in: the spec builds them from art). Idempotent.
/// P0-A carve (docs/pending/CARVE.md): the UV torch is in .Tools.cs. Owner:
/// DESK-PLAN.
/// </summary>
public static partial class OfficeSceneUIBuilder
{
    /// <summary>The machines' models and the reveal's textures and shaders.</summary>
    private const string MachinesFolder = "Assets/Art/Office/Gameplay/Machines", ChipReaderModel = MachinesFolder + "/ChipReader.fbx", UvTorchModel = MachinesFolder + "/UvTorch.fbx",
                         UvTextures = "Assets/Art/Office/Gameplay/Textures";

    /// <summary>The reader's readout ink (its VFD's green) and the torch's light (a blacklight's violet).</summary>
    private static readonly Color ChipReadoutInk = new Color(0.42f, 1f, 0.6f), UvLightColour = new Color(0.55f, 0.32f, 1f);

    /// <summary>The note boxes over the machines (metres: two short lines).</summary>
    private static readonly Vector2 MachineHintBox = new Vector2(0.62f, 0.12f);

    /// <summary>
    /// Office/DeskHardware: the chip reader and the UV torch from their models,
    /// wired on the DeskHardware with the config, the notes and the reveal's
    /// materials and textures. Returns it (the desk and the binder take it).
    /// </summary>
    private static DeskHardware BuildDeskHardware(Transform office, DeskConfigSO config, OverlayCallout tooltip)
    {
        DestroyChildIfPresent(office, "DeskHardware");
        Transform root = EnsureChild(office, "DeskHardware");
        DeskHardware hardware = GetOrAdd<DeskHardware>(root.gameObject);
        ChipReader reader = BuildChipReader(root);
        UvTorch torch = BuildUvTorch(root);
        DeskUpgrades upgrades = BuildDeskUpgrades(root, config, tooltip);

        TextMeshPro uvHint = FloatingNote(root, "UvHint", true);
        TextMeshPro chipHint = FloatingNote(root, "ChipHint", true);
        foreach (TextMeshPro hint in new[] { uvHint, chipHint })
        {
            ((RectTransform)hint.transform).sizeDelta = MachineHintBox;
            hint.textWrappingMode = TextWrappingModes.Normal;
            hint.gameObject.SetActive(false);
        }

        Material mark = EnsureMaterial("UvReveal_Mark", "TimeDesk/UvReveal", m => m.SetColor("_Color", new Color(0.72f, 0.55f, 1f, 0.9f)));
        Material text = EnsureMaterial("UvReveal_Text", "TimeDesk/UvRevealText", m => m.SetColor("_FaceColor", new Color(0.8f, 0.62f, 1f, 1f)));

        var so = new SerializedObject(hardware);
        SetRef(so, "chipReader", reader);
        SetRef(so, "uvTorch", torch);
        SetRef(so, "upgrades", upgrades);
        SetRef(so, "config", config);
        SetRef(so, "uvHint", uvHint);
        SetRef(so, "chipHint", chipHint);
        SetRef(so, "markMaterial", mark);
        SetRef(so, "textMaterial", text);
        SetRef(so, "watermarkTexture", AssetDatabase.LoadAssetAtPath<Texture2D>(UvTextures + "/UvWatermark.png"));
        SetRef(so, "microprintTexture", AssetDatabase.LoadAssetAtPath<Texture2D>(UvTextures + "/UvMicroprint.png"));
        SetRef(so, "tickTexture", AssetDatabase.LoadAssetAtPath<Texture2D>(UvTextures + "/UvTick.png"));
        so.ApplyModifiedProperties();
        return hardware;
    }

    /// <summary>A machine's model under <paramref name="parent"/> as its child Machine, in the desk's materials (by the imported slot's name, <paramref name="material"/>), no shadows cast by what glows; null (with a warning) when the model is missing or lacks a part of <paramref name="contract"/>.</summary>
    private static GameObject MachineArt(Transform parent, string path, string[] contract, System.Func<string, Material> material, string name = "Machine")
    {
        DestroyChildIfPresent(parent, name);
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        bool Has(string part) => model != null && model.transform.Find(part) != null;
        if (!PropArt.UseArt(model != null, contract, Has))
        {
            Debug.LogWarning(model == null ? $"[TimeDesk] {path} is missing: the desk machine is left out." : $"[TimeDesk] {path} lacks {string.Join(", ", PropArt.Missing(contract, Has))}: the desk machine is left out.");
            return null;
        }
        GameObject machine = Object.Instantiate(model, parent, false);
        machine.name = name;
        machine.transform.localPosition = Vector3.zero;
        machine.transform.localRotation = Quaternion.identity;
        machine.transform.localScale = Vector3.one;
        foreach (MeshRenderer r in machine.GetComponentsInChildren<MeshRenderer>(true))
        {
            r.sharedMaterials = r.sharedMaterials.Select(x => material(x != null ? x.name : null)).ToArray();
            if (r.sharedMaterials.Any(x => x != null && x.shader != null && x.shader.name.Contains("Unlit")))
                r.shadowCastingMode = ShadowCastingMode.Off;
        }
        return machine;
    }

    /// <summary>The bounds of everything drawn under <paramref name="root"/>, in <paramref name="space"/>'s local space.</summary>
    private static Bounds LocalBounds(Transform root, Transform space)
    {
        Bounds b = default;
        bool any = false;
        foreach (MeshFilter f in root.GetComponentsInChildren<MeshFilter>(true))
            if (f.sharedMesh != null)
                foreach (Vector3 corner in Corners(f.sharedMesh.bounds))
                {
                    Vector3 p = space.InverseTransformPoint(f.transform.TransformPoint(corner));
                    if (any)
                        b.Encapsulate(p);
                    else
                        b = new Bounds(p, Vector3.zero);
                    any = true;
                }
        return b;
    }

    /// <summary>The chip reader (DeskHardware/ChipReader): its model, the readout's live text and click box, its sounds' source; null without its art.</summary>
    private static ChipReader BuildChipReader(Transform root)
    {
        Transform host = EnsureChild(root, "ChipReader");
        ChipReader reader = GetOrAdd<ChipReader>(host.gameObject);
        GameObject machine = MachineArt(host, ChipReaderModel, PropArt.ChipReader, ChipReaderMaterial);
        if (machine == null)
        {
            Object.DestroyImmediate(host.gameObject);
            return null;
        }
        Transform m = machine.transform;
        Transform readoutQuad = m.Find("Body/Readout");
        TMP_Text readout = MachineReadout(readoutQuad, "Readout", ChipReadoutInk, TextAlignmentOptions.MidlineLeft, "READY");
        DestroyChildIfPresent(host, "ReadoutClick");
        Clickable click = EnsureClickBox(host, "ReadoutClick");
        Bounds rb = LocalBounds(readoutQuad, host);
        click.transform.localPosition = rb.center;
        click.transform.localRotation = Quaternion.identity;
        click.GetComponent<BoxCollider>().size = new Vector3(Mathf.Max(rb.size.x, 0.02f), Mathf.Max(rb.size.y, 0.02f), Mathf.Max(rb.size.z, 0.02f));
        click.SetOutline(readoutQuad.GetComponentsInChildren<Renderer>(true).Where(r => r.GetComponent<TMP_Text>() == null).ToArray());
        AudioSource sound = GetOrAdd<AudioSource>(host.gameObject);
        sound.playOnAwake = false;
        sound.spatialBlend = 0f;

        var so = new SerializedObject(reader);
        SetRef(so, "slotFlap", m.Find("Body/SlotFlap"));
        SetRef(so, "padCentre", m.Find("Body/PadCentre"));
        SetRef(so, "cardSlot", m.Find("Body/CardSlot")); // a card slides into the slot at its height and angle (IPaperDock)
        SetRef(so, "ledPower", m.Find("Body/LED_Power").GetComponent<Renderer>());
        SetRef(so, "ledRead", m.Find("Body/LED_Read").GetComponent<Renderer>());
        SetRef(so, "ledFault", m.Find("Body/LED_Fault").GetComponent<Renderer>());
        SetRef(so, "readout", readout);
        SetRef(so, "readoutClick", click);
        SetRef(so, "sound", sound);
        Bounds body = LocalBounds(m, host);
        so.FindProperty("dropSize").vector2Value = new Vector2(body.size.x + 0.04f, body.size.z + 0.04f);
        so.ApplyModifiedProperties();
        return reader;
    }

    /// <summary>A machine's live readout text lying on <paramref name="quad"/>'s face (its mesh's first normal), in the kit's readout face and <paramref name="ink"/>.</summary>
    private static TMP_Text MachineReadout(Transform quad, string name, Color ink, TextAlignmentOptions align, string placeholder)
    {
        var filter = quad != null ? quad.GetComponent<MeshFilter>() : null;
        if (filter == null || filter.sharedMesh == null || filter.sharedMesh.normals.Length == 0)
            return null;
        DestroyChildIfPresent(quad, name);
        Mesh mesh = filter.sharedMesh;
        Vector3 normal = mesh.normals[0].normalized;
        Vector3 up = Vector3.ProjectOnPlane(Vector3.up, normal).normalized;
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshPro));
        go.transform.SetParent(quad, false);
        go.transform.localPosition = mesh.bounds.center + normal * 0.0008f;
        go.transform.localRotation = Quaternion.LookRotation(-normal, up);
        float height = new Vector2(mesh.bounds.size.y, mesh.bounds.size.z).magnitude;
        ((RectTransform)go.transform).sizeDelta = new Vector2(mesh.bounds.size.x * 0.92f, height * 0.86f);
        TextMeshPro text = go.GetComponent<TextMeshPro>();
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiKitFonts.Readout);
        if (font != null)
            text.font = font;
        text.text = placeholder;
        text.enableAutoSizing = true;
        text.fontSizeMin = 0.004f;
        text.fontSizeMax = 0.12f;
        text.alignment = align;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.color = ink;
        text.sortingLayerID = GameplaySortingLayerId();
        return text;
    }

    /// <summary>A textured decal's material (NOPE/Desk Anime with the texture as its base map; URP Lit without the shader).</summary>
    private static Material DecalMaterial(string name, string texture, Color fallback) =>
        Shader.Find("NOPE/Desk Anime") != null
            ? EnsureMaterial(name, "NOPE/Desk Anime", x =>
            {
                x.SetColor("_BaseColor", Color.white);
                x.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(MachinesFolder + "/" + texture));
            })
            : LitMaterial(name, fallback, 0.4f);

    /// <summary>The desk material for a slot of the chip reader's model (chip_reader_model.py's table): solids in NOPE/Desk Anime, the readout, its glass and the lamps URP Unlit (the lamps' colours are their glow; ChipReader dims them).</summary>
    private static Material ChipReaderMaterial(string slot)
    {
        switch (slot)
        {
            case "Chip_Cream": return AnimeMaterial("ChipArt_Cream", Hex("#E6DCC4"));
            case "Chip_Charcoal": return AnimeMaterial("ChipArt_Charcoal", Hex("#2F3036"));
            case "Chip_Slot": return AnimeMaterial("ChipArt_Slot", Hex("#0E0E10"));
            case "Chip_Rubber": return AnimeMaterial("ChipArt_Rubber", Hex("#2A2628"));
            case "Chip_KeySlate": return AnimeMaterial("ChipArt_KeySlate", Hex("#4A5872"));
            case "Chip_KeyBrass": return AnimeMaterial("ChipArt_KeyBrass", Hex("#D4A055"));
            case "Chip_KeySignal": return AnimeMaterial("ChipArt_KeySignal", Hex("#C23A2E"));
            case "Chip_Pad": return DecalMaterial("ChipArt_Pad", "ChipReader_Pad.png", Hex("#2B2630"));
            case "Chip_Badge": return DecalMaterial("ChipArt_Badge", "ChipReader_Badge.png", Hex("#8A2F3B"));
            case "Chip_SlotLabel": return DecalMaterial("ChipArt_SlotLabel", "ChipReader_SlotLabel.png", Hex("#464C56"));
            case "Chip_VFD": return UnlitMaterial("ChipArt_VFD", Hex("#061A12"));
            case "Chip_VFDGlass": return GlassMaterial("ChipArt_VFDGlass", Hex("#203A30", 0.3f));
            case "Chip_LEDGreen": return UnlitMaterial("ChipArt_LEDGreen", Hex("#3CFF86"));
            case "Chip_LEDAmber": return UnlitMaterial("ChipArt_LEDAmber", Hex("#FFB23C"));
            case "Chip_LEDRed": return UnlitMaterial("ChipArt_LEDRed", Hex("#FF3A2A"));
            default: return AnimeMaterial("ChipArt_Gunmetal", Hex("#4E5560"));
        }
    }
}
