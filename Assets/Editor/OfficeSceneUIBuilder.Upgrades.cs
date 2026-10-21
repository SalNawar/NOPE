using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;

/// <summary>
/// The upgrades' desk objects (the desk machines spec §4; Saleh 2026-10-17:
/// "every upgrade should have a visual representation on the desk" "and a
/// satysfing interaction"): DeskHardware/DeskUpgrades with the supply crate,
/// the protocol ring, the BABEL rack, the Rolodex and the
/// RING SERVICE panel, and the scanner's Auto-Feed tray and Analysis beacon
/// on the scanner's art (ScannerAddOns), each the model of
/// tools/props/machines/upgrade_props_model.py in the desk's materials (its
/// slots' colours and decals, PropSlots), with its click box and its cues'
/// source. An object whose model lacks a part of its contract (PropArt) is
/// left out with a warning. Idempotent (DeskHardware is rebuilt whole).
/// P0-A carve (docs/pending/CARVE.md): the BABEL rack is in .Babel.cs, the
/// RING SERVICE panel in .Ring.cs; the lens stand and its magnifiers were cut
/// by P0-C (docs/pending/CUTS.md). Owner: DESK-PLAN.
/// </summary>
public static partial class OfficeSceneUIBuilder
{
    private const string SupplyCrateModel = MachinesFolder + "/SupplyCrate.fbx", ScannerFeederModel = MachinesFolder + "/ScannerFeeder.fbx",
                         AnalysisBeaconModel = MachinesFolder + "/AnalysisBeacon.fbx", ProtocolRingModel = MachinesFolder + "/ProtocolRing.fbx",
                         BabelRackModel = MachinesFolder + "/BabelRack.fbx",
                         RolodexModel = MachinesFolder + "/Rolodex.fbx", RingPanelModel = MachinesFolder + "/RingPanel.fbx";

    private enum PropSlotKind { Solid, Decal, Glass, Lamp }

    /// <summary>The props' material slots (upgrade_props_model.py's table, its verify report): kind, colour, alpha and decal.</summary>
    private static readonly Dictionary<string, (PropSlotKind kind, string hex, float alpha, string tex)> PropSlots =
        new Dictionary<string, (PropSlotKind, string, float, string)>
        {
            ["Crate_Kraft"] = (PropSlotKind.Solid, "#C4A070", 1f, null),
            ["Crate_Tape"] = (PropSlotKind.Decal, "#C23A2E", 1f, "Crate_Tape.png"),
            ["Crate_Label"] = (PropSlotKind.Decal, "#FAF2E1", 1f, "Crate_Label.png"),
            ["Feed_Cream"] = (PropSlotKind.Solid, "#DCD0B4", 1f, null),
            ["Feed_Slate"] = (PropSlotKind.Solid, "#4A5872", 1f, null),
            ["Feed_Rubber"] = (PropSlotKind.Solid, "#2B1C24", 1f, null),
            ["Feed_Brass"] = (PropSlotKind.Solid, "#D4A055", 1f, null),
            ["Feed_LED"] = (PropSlotKind.Lamp, "#3CFF86", 1f, null),
            ["Feed_Plate"] = (PropSlotKind.Decal, "#4A5872", 1f, "Feeder_Plate.png"),
            ["Beacon_Slate"] = (PropSlotKind.Solid, "#4A5872", 1f, null),
            ["Beacon_Brass"] = (PropSlotKind.Solid, "#D4A055", 1f, null),
            ["Beacon_Ink"] = (PropSlotKind.Solid, "#2B1C24", 1f, null),
            ["Beacon_Chrome"] = (PropSlotKind.Solid, "#C9CCD4", 1f, null),
            ["Beacon_Dome"] = (PropSlotKind.Glass, "#E8A23C", 0.5f, null),
            ["Beacon_Lamp"] = (PropSlotKind.Lamp, "#FFB23C", 1f, null),
            ["Beacon_Plate"] = (PropSlotKind.Decal, "#8A2F3B", 1f, "Beacon_Plate.png"),
            ["Proto_Slate"] = (PropSlotKind.Solid, "#4A5872", 1f, null),
            ["Proto_Brass"] = (PropSlotKind.Solid, "#D4A055", 1f, null),
            ["Proto_Card"] = (PropSlotKind.Solid, "#FAF2E1", 1f, null),
            ["Proto_Face"] = (PropSlotKind.Decal, "#FAF2E1", 1f, "Protocol_Card.png"),
            ["Babel_Slate"] = (PropSlotKind.Solid, "#4A5872", 1f, null),
            ["Babel_Deck"] = (PropSlotKind.Solid, "#2B1C24", 1f, null),
            ["Babel_Bay"] = (PropSlotKind.Solid, "#1A1016", 1f, null),
            ["Babel_Grille"] = (PropSlotKind.Solid, "#2B1C24", 1f, null),
            ["Babel_Brass"] = (PropSlotKind.Solid, "#D4A055", 1f, null),
            ["Babel_VU"] = (PropSlotKind.Decal, "#EEDEAA", 1f, "Babel_VU.png"),
            ["Babel_Glass"] = (PropSlotKind.Glass, "#EEE5D0", 0.12f, null),
            ["Babel_Needle"] = (PropSlotKind.Solid, "#C23A2E", 1f, null),
            ["Babel_Plate"] = (PropSlotKind.Decal, "#4A5872", 1f, "Babel_Plate.png"),
            ["Babel_Cart0"] = (PropSlotKind.Solid, "#D4A055", 1f, null),
            ["Babel_CartLabel0"] = (PropSlotKind.Decal, "#D4A055", 1f, "Cartridge_NearEast.png"),
            ["Babel_LED0"] = (PropSlotKind.Lamp, "#3CFF86", 1f, null),
            ["Babel_Cart1"] = (PropSlotKind.Solid, "#4A5872", 1f, null),
            ["Babel_CartLabel1"] = (PropSlotKind.Decal, "#4A5872", 1f, "Cartridge_Mediterranean.png"),
            ["Babel_LED1"] = (PropSlotKind.Lamp, "#3CFF86", 1f, null),
            ["Babel_Cart2"] = (PropSlotKind.Solid, "#C23A2E", 1f, null),
            ["Babel_CartLabel2"] = (PropSlotKind.Decal, "#C23A2E", 1f, "Cartridge_EastAsia.png"),
            ["Babel_LED2"] = (PropSlotKind.Lamp, "#3CFF86", 1f, null),
            ["Babel_Cart3"] = (PropSlotKind.Solid, "#9886A8", 1f, null),
            ["Babel_CartLabel3"] = (PropSlotKind.Decal, "#9886A8", 1f, "Cartridge_NorthEurope.png"),
            ["Babel_LED3"] = (PropSlotKind.Lamp, "#3CFF86", 1f, null),
            ["Rdx_Oxblood"] = (PropSlotKind.Solid, "#8A2F3B", 1f, null),
            ["Rdx_Brass"] = (PropSlotKind.Solid, "#D4A055", 1f, null),
            ["Rdx_Slate"] = (PropSlotKind.Solid, "#4A5872", 1f, null),
            ["Rdx_Card"] = (PropSlotKind.Solid, "#FAF2E1", 1f, null),
            ["Rdx_Face"] = (PropSlotKind.Decal, "#FAF2E1", 1f, "Rolodex_Card.png"),
            ["Rdx_Gold"] = (PropSlotKind.Decal, "#FAECC4", 1f, "Rolodex_GoldCard.png"),
            ["Panel_Slate"] = (PropSlotKind.Solid, "#4A5872", 1f, null),
            ["Panel_Ink"] = (PropSlotKind.Solid, "#2B1C24", 1f, null),
            ["Panel_Brass"] = (PropSlotKind.Solid, "#D4A055", 1f, null),
            ["Panel_Lever"] = (PropSlotKind.Solid, "#EEE5D0", 1f, null),
            ["Panel_LeverTip"] = (PropSlotKind.Solid, "#C23A2E", 1f, null),
            ["Panel_Face"] = (PropSlotKind.Decal, "#4A5872", 1f, "Relay_Face.png"),
            ["Panel_Sticker"] = (PropSlotKind.Decal, "#78C478", 1f, "Relay_Serviced.png"),
            ["Panel_Lamp1"] = (PropSlotKind.Lamp, "#1F5A36", 1f, null),
            ["Panel_Lamp2"] = (PropSlotKind.Lamp, "#1F5A36", 1f, null),
            ["Panel_Lamp3"] = (PropSlotKind.Lamp, "#1F5A36", 1f, null),
            ["Panel_Lamp4"] = (PropSlotKind.Lamp, "#1F5A36", 1f, null),
            ["Panel_Lamp5"] = (PropSlotKind.Lamp, "#1F5A36", 1f, null),
        };

    /// <summary>The desk material for a slot of a prop's model (PropSlots; "PropArt_" + the slot): solids in NOPE/Desk Anime, decals with their texture, glasses URP Unlit transparent, lamps URP Unlit (the game tints them per renderer).</summary>
    private static Material PropMaterial(string slot)
    {
        if (slot == null || !PropSlots.TryGetValue(slot, out var s))
            return AnimeMaterial("PropArt_Slate", Hex("#4A5872"));
        string name = "PropArt_" + slot;
        switch (s.kind)
        {
            case PropSlotKind.Decal: return DecalMaterial(name, s.tex, Hex(s.hex));
            case PropSlotKind.Lamp: return UnlitMaterial(name, Hex(s.hex));
            case PropSlotKind.Glass:
                return s.tex == null
                    ? GlassMaterial(name, Hex(s.hex, s.alpha))
                    : EnsureMaterial(name, "Universal Render Pipeline/Unlit", x =>
                    {
                        x.SetColor("_BaseColor", Hex(s.hex, s.alpha));
                        x.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(MachinesFolder + "/" + s.tex));
                        x.SetFloat("_Surface", 1f);
                        x.SetFloat("_Blend", 0f);
                        x.SetFloat("_Cull", 0f);
                        UnityEditor.BaseShaderGUI.SetMaterialKeywords(x);
                    });
            default: return AnimeMaterial(name, Hex(s.hex));
        }
    }

    /// <summary>Sets the object array <paramref name="prop"/> of <paramref name="so"/> to <paramref name="values"/>.</summary>
    private static void SetArray(SerializedObject so, string prop, Object[] values)
    {
        SerializedProperty list = so.FindProperty(prop);
        list.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            list.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }

    /// <summary>A prop's host under <paramref name="root"/> with its model (child Machine) and a cues' source; null (the host removed) without its art.</summary>
    private static Transform PropHost(Transform root, string name, string model, string[] contract, out Transform machine)
    {
        Transform host = EnsureChild(root, name);
        GameObject art = MachineArt(host, model, contract, PropMaterial);
        machine = art != null ? art.transform : null;
        if (art == null)
        {
            Object.DestroyImmediate(host.gameObject);
            return null;
        }
        AudioSource sound = GetOrAdd<AudioSource>(host.gameObject);
        sound.playOnAwake = false;
        sound.spatialBlend = 0f;
        return host;
    }

    /// <summary>A click box named <paramref name="name"/> over everything drawn under <paramref name="cover"/> (grown by <paramref name="pad"/> metres), outlining it.</summary>
    private static Clickable PropClick(Transform host, string name, Transform cover, float pad = DeskHomeMap.ClickPad)
    {
        DestroyChildIfPresent(host, name);
        Clickable click = EnsureClickBox(host, name);
        Bounds b = LocalBounds(cover, host);
        click.transform.localPosition = b.center;
        click.transform.localRotation = Quaternion.identity;
        click.GetComponent<BoxCollider>().size = b.size + Vector3.one * (2f * pad);
        click.SetOutline(cover.GetComponentsInChildren<Renderer>(true).Where(r => r.GetComponent<TMP_Text>() == null).ToArray());
        return click;
    }

    /// <summary>A live text named <paramref name="name"/> on a prop's face (<paramref name="parent"/>'s local frame: centred at <paramref name="at"/>, <paramref name="size"/> metres, read from local -Z), in the kit font <paramref name="font"/>, auto-sized up to <paramref name="maxSize"/>, on the Gameplay sorting layer; empty until the game prints it.</summary>
    private static TextMeshPro PropText(Transform parent, string name, Vector3 at, Vector2 size, string font, Color colour, float maxSize)
    {
        DestroyChildIfPresent(parent, name);
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshPro));
        go.transform.SetParent(parent, false);
        go.transform.localPosition = at;
        go.transform.localRotation = Quaternion.identity;
        var text = go.GetComponent<TextMeshPro>();
        ((RectTransform)go.transform).sizeDelta = size;
        var asset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(font);
        if (asset != null)
            text.font = asset;
        text.enableAutoSizing = true;
        text.fontSizeMin = maxSize * 0.15f;
        text.fontSizeMax = maxSize;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.color = colour;
        text.margin = Vector4.zero;
        text.sortingLayerID = GameplaySortingLayerId();
        text.text = string.Empty;
        return text;
    }

    /// <summary>Office/DeskHardware/DeskUpgrades: the upgrades' objects (each inactive until DeskUpgrades.BeginDay shows it), wired with the config.</summary>
    private static DeskUpgrades BuildDeskUpgrades(Transform hardwareRoot, DeskConfigSO config, OverlayCallout tooltip)
    {
        Transform root = EnsureChild(hardwareRoot, "DeskUpgrades");
        DeskUpgrades upgrades = GetOrAdd<DeskUpgrades>(root.gameObject);
        var so = new SerializedObject(upgrades);
        SetRef(so, "crate", BuildSupplyCrate(root));
        SetRef(so, "protocolRing", BuildProtocolRing(root));
        SetRef(so, "babelRack", BuildBabelRack(root));
        SetRef(so, "rolodex", BuildRolodex(root));
        SetRef(so, "ringPanel", BuildRingPanel(root));
        SetRef(so, "scannerAddOns", Object.FindFirstObjectByType<ScannerAddOns>(FindObjectsInactive.Include)); // built with the scanner (BuildDesk, before the hardware)
        SetRef(so, "config", config);
        SetRef(so, "tooltip", tooltip);
        so.ApplyModifiedProperties();
        foreach (Transform child in root)
            child.gameObject.SetActive(false);
        return upgrades;
    }

    private static SupplyCrate BuildSupplyCrate(Transform root)
    {
        Transform host = PropHost(root, "SupplyCrate", SupplyCrateModel, PropArt.SupplyCrate, out Transform m);
        if (host == null)
            return null;
        SupplyCrate crate = GetOrAdd<SupplyCrate>(host.gameObject);
        Transform c = m.Find("Crate");
        Transform item = c.Find("ItemPoint");
        DestroyChildIfPresent(item, "Tag");
        var tagGo = new GameObject("Tag", typeof(RectTransform), typeof(TextMeshPro));
        tagGo.transform.SetParent(item, false);
        var tag = tagGo.GetComponent<TextMeshPro>();
        ((RectTransform)tagGo.transform).sizeDelta = new Vector2(0.24f, 0.1f);
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiKitFonts.Readout);
        if (font != null)
            tag.font = font;
        tag.enableAutoSizing = true;
        tag.fontSizeMin = 0.05f;
        tag.fontSizeMax = 0.2f;
        tag.alignment = TextAlignmentOptions.Center;
        tag.textWrappingMode = TextWrappingModes.Normal;
        tag.color = Hex("#2B1C24");
        tag.margin = new Vector4(0.008f, 0.004f, 0.008f, 0.004f);
        tag.sortingLayerID = GameplaySortingLayerId();
        tag.text = "Auto-Feed Scanner";
        // The tag's card behind the names (the kit's card colour), so they read over anything behind them.
        GameObject card = GameObject.CreatePrimitive(PrimitiveType.Quad);
        card.name = "Card";
        Object.DestroyImmediate(card.GetComponent<Collider>());
        card.transform.SetParent(tagGo.transform, false);
        card.transform.localPosition = new Vector3(0f, 0f, 0.001f);
        card.transform.localScale = new Vector3(0.24f, 0.1f, 1f);
        card.GetComponent<MeshRenderer>().sharedMaterial = UnlitMaterial("PropArt_TagCard", Hex("#FAF2E1"));
        card.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        tagGo.SetActive(false);

        var so = new SerializedObject(crate);
        SetRef(so, "flapLeft", c.Find("Flap_Left"));
        SetRef(so, "flapRight", c.Find("Flap_Right"));
        SetRef(so, "flapFront", c.Find("Flap_Front"));
        SetRef(so, "flapBack", c.Find("Flap_Back"));
        SetRef(so, "tape", c.Find("Tape"));
        SetRef(so, "itemPoint", item);
        SetRef(so, "tag", tag);
        SetRef(so, "click", PropClick(host, "Click", c));
        SetRef(so, "sound", host.GetComponent<AudioSource>());
        so.ApplyModifiedProperties();
        return crate;
    }

    private static ProtocolRing BuildProtocolRing(Transform root)
    {
        Transform host = PropHost(root, "ProtocolRing", ProtocolRingModel, PropArt.ProtocolRing, out Transform m);
        if (host == null)
            return null;
        ProtocolRing ring = GetOrAdd<ProtocolRing>(host.gameObject);
        Transform r = m.Find("ProtocolStand/Ring");
        // Each card's question, printed on its face below the header band (upgrade_props_model.py: the face centre (0, -0.0217, -0.0008), outward -Z, 0.085 x 0.055, the header the top 26 %).
        var labels = new Object[4];
        for (int i = 0; i < 4; i++)
            labels[i] = PropText(r.Find("Card_" + i), "Label", new Vector3(0f, -0.0289f, -0.0009f), new Vector2(0.076f, 0.034f), UiKitFonts.Label, Hex("#2B1C24"), 0.12f);
        var so = new SerializedObject(ring);
        SetArray(so, "cards", Enumerable.Range(0, 4).Select(i => (Object)r.Find("Card_" + i)).ToArray());
        SetArray(so, "labels", labels);
        SetRef(so, "ring", r);
        SetRef(so, "click", PropClick(host, "Click", m.Find("ProtocolStand")));
        SetRef(so, "sound", host.GetComponent<AudioSource>());
        so.ApplyModifiedProperties();
        return ring;
    }

    private static Rolodex BuildRolodex(Transform root)
    {
        Transform host = PropHost(root, "Rolodex", RolodexModel, PropArt.Rolodex, out Transform m);
        if (host == null)
            return null;
        Rolodex rolodex = GetOrAdd<Rolodex>(host.gameObject);
        Transform r = m.Find("Rolodex");
        host.gameObject.layer = OfficeLayers.InteractableLayer;
        BoxCollider box = GetOrAdd<BoxCollider>(host.gameObject);
        Bounds bounds = LocalBounds(r, host);
        box.center = bounds.center;
        box.size = bounds.size + Vector3.one * 0.02f;
        var so = new SerializedObject(rolodex);
        SetRef(so, "drum", r.Find("Drum"));
        SetRef(so, "knob", r.Find("Knob"));
        SetRef(so, "sound", host.GetComponent<AudioSource>());
        so.ApplyModifiedProperties();
        return rolodex;
    }

    /// <summary>
    /// The scanner's upgrades on its art (<paramref name="machine"/>, in its
    /// frame): the Auto-Feed tray and the Analysis beacon (each inactive:
    /// DeskScanner.ShowUpgrades shows them), wired on the scanner's
    /// ScannerAddOns. Returns it, with the two objects (null without their art).
    /// </summary>
    private static ScannerAddOns BuildScannerAddOns(Transform scanner, Transform machine, out GameObject feeder, out GameObject beacon)
    {
        ScannerAddOns addOns = GetOrAdd<ScannerAddOns>(scanner.gameObject);
        feeder = MachineArt(machine, ScannerFeederModel, PropArt.ScannerFeeder, PropMaterial, "AutoFeed");
        beacon = MachineArt(machine, AnalysisBeaconModel, PropArt.AnalysisBeacon, PropMaterial, "AnalysisBeacon");
        AudioSource sound = scanner.GetComponent<AudioSource>();
        var so = new SerializedObject(addOns);
        SetRef(so, "rollers", feeder != null ? feeder.transform.Find("Feeder/Rollers") : null);
        SetRef(so, "feedLed", feeder != null ? feeder.transform.Find("Feeder/FeedLED").GetComponent<Renderer>() : null);
        SetRef(so, "beaconPod", beacon != null ? beacon.transform.Find("Beacon/BeaconPod") : null);
        SetRef(so, "reflector", beacon != null ? beacon.transform.Find("Beacon/BeaconPod/Reflector") : null);
        SetRef(so, "beaconLamp", beacon != null ? beacon.transform.Find("Beacon/BeaconPod/BeaconLamp").GetComponent<Renderer>() : null);
        SetRef(so, "trayClick", feeder != null ? PropClick(feeder.transform, "Click", feeder.transform.Find("Feeder"), 0.006f) : null);
        SetRef(so, "beaconClick", beacon != null ? PropClick(beacon.transform, "Click", beacon.transform.Find("Beacon"), 0.006f) : null);
        SetRef(so, "sound", sound);
        so.ApplyModifiedProperties();
        if (feeder != null)
            feeder.SetActive(false);
        if (beacon != null)
            beacon.SetActive(false);
        return addOns;
    }
}
