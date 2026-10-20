using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

/// <summary>
/// The office builder's stamp rack: the stamp tray on the overlay and its
/// rack of daters on the desk.
/// Part of <see cref="OfficeSceneUIBuilder"/>; carved out of OfficeSceneUIBuilder.Desk.cs by P0-A (pure move). Owner: STAMP.
/// </summary>
public static partial class OfficeSceneUIBuilder
{
    /// <summary>Where the STAMPS tab's left edge sits along the screen's top edge (viewport x): where the drawer's band was before the fixed rack.</summary>
    private const float StampTabX = 0.66f;

    /// <summary>The 3D stamp bar (metres, in the rack's space: x along the office view's right, z away from the chair, y up from the stamps' feet): the two daters' distance apart (DENIED left, APPROVED right), the label lip's reach past each, its gap in front of the daters' frames and its section (deep, tall). The lip lies low at the daters' feet, in front of them (toward the chair), its words on its top, so the whole green and red bodies with their wheels show over it (run 7's integration: the rail at the bodies' height hid them).</summary>
    private const float StampSpacing = 0.14f;

    private const float StampRailOverhang = 0.07f;

    private const float StampLipGap = 0.004f;

    private static readonly Vector2 StampRailSection = new Vector2(0.03f, 0.008f);

    /// <summary>The stamps' hint plate (reference px), at the top right under the case HUD's strip.</summary>
    private static readonly Vector2 StampHintSize = new Vector2(560f, 50f);

    /// <summary>
    /// The stamp bar, Papers, Please's with 3D stamps (Saleh 2026-10-06: "I
    /// want the 3D stamp; there should be a label on the screen to bring out
    /// the stamp stuff, and the Tab shortcut"), its stamps the two daters (the
    /// desk machine spec §1), rebuilt each run. On the office overlay:
    /// StampBar (the DeskStampTray, an always-active full-screen host) with
    /// the Tab on the top edge (StampTabX; "STAMPS" beside the key, TAB: it
    /// takes the player to the stamps, the reading view framing the fixed
    /// rack, and the daters lift to answer; inactive until the booth shows
    /// it, in the office view only), the hint's plate at the top right
    /// under the case HUD's strip (inactive) and an AudioSource for the clacks.
    /// In the office: StampRack (the office binder lays it on its bay of the
    /// desk's plan, its origin at the daters' feet): the heavy brass drawer when its
    /// art is in the project and complete (BuildBrassDrawer; PropArt.UseArt over
    /// PropArt.BrassDrawer), its daters standing in its cradles and the
    /// verdict words on its enamel plates; else (the fallback) a low lip in the
    /// art's DeskClean green-dark at the daters' feet in front of them, with
    /// wooden end caps and brass brackets to the two daters, the words on its
    /// top; and the DENIED dater (left) and the APPROVED dater (right), each a click box
    /// on the Interactable layer (its pivot at its foot) with a DeskDraggable
    /// (the click box its proxy: the dater is dragged onto the paper; stamping
    /// is drag-only), holding its body (DaterBody: the prop
    /// contract's Body, Frame, Die and Wheels; green on APPROVED, red on DENIED).
    /// Under the rack lies its ContactShadow (the papers' shadow material,
    /// inactive: DeskStampTray.Lay draws it under the rack's footprint; BUG-16a).
    /// The tray gets the papers' style, the date's face,
    /// Saleh's dater sounds (WireDaters). The old overlay bar, the 3D tray of
    /// the desk-first redesign and the overlay's hand-back buttons are destroyed.
    /// </summary>
    private static DeskStampTray BuildStampTray(Transform overlay, Transform office, DeskConfigSO config)
    {
        DestroyChildIfPresent(overlay, "StampTray");
        DestroyChildIfPresent(office, "StampTray");
        DestroyChildIfPresent(overlay, "StampHandBack");
        DestroyChildIfPresent(overlay, "StampBar");
        DestroyChildIfPresent(office, "StampRack");
        Transform host = Panel(overlay, "StampBar", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);

        Button tab = MakeButton(host, "Tab", null, Vector2.zero, Vector2.one, StampGrey, ThemeRoleId.DiegeticDevice);
        var tabRect = (RectTransform)tab.transform;
        tabRect.anchorMin = tabRect.anchorMax = new Vector2(StampTabX, 1f);
        tabRect.pivot = new Vector2(0f, 1f);
        tabRect.anchoredPosition = Vector2.zero;
        tabRect.sizeDelta = StampTabSize;
        TMP_Text tabLabel = tab.transform.Find("Label").GetComponent<TMP_Text>();
        tabLabel.text = $"{UiText.Get("controls.stampsTab")}  <b>{ControlRules.StampsKey}</b>";
        tabLabel.color = Color.white;
        tabLabel.enableAutoSizing = false;
        tabLabel.fontSize = 26f;
        tab.gameObject.SetActive(false);

        Transform plateHint = Panel(host, "Hint", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-16f, -StampPlateTop), StampHintSize, Tooltip, ThemeRoleId.Tooltip);
        ((RectTransform)plateHint).pivot = new Vector2(1f, 1f);
        plateHint.GetComponent<Image>().raycastTarget = false;
        TMP_Text hint = Text(plateHint, "Label", "", 22, TextAlignmentOptions.Center, new Vector2(0.03f, 0.06f), new Vector2(0.97f, 0.94f), Ink, ThemeRoleId.Tooltip, fit: true);
        hint.raycastTarget = false;
        plateHint.gameObject.SetActive(false);

        AudioSource sound = GetOrAdd<AudioSource>(host.gameObject);
        sound.playOnAwake = false;

        // The rack in the office: x along the office view's right, z away from the chair, y up from the stamps' feet.
        Transform rack = EnsureChild(office, "StampRack");
        BrassDrawer drawer = BuildBrassDrawer(rack);
        Material rail = DeskMaterial("GreenDark", new Color(0.204f, 0.294f, 0.275f));
        Material wood = DeskMaterial("Wood", new Color(0.537f, 0.392f, 0.282f));
        Material brass = DeskMaterial("Brass", new Color(0.72f, 0.58f, 0.3f));
        float railLength = 2f * (StampSpacing / 2f + StampRailOverhang);
        // The fallback's lip at the daters' feet, in front of their frames (DaterHalfDepth), its top StampRailSection.y over the feet.
        float lipZ = -(DaterHalfDepth + StampLipGap + StampRailSection.x / 2f);
        if (drawer == null)
        {
            PrimitivePart(rack, "Rail", PrimitiveType.Cube, new Vector3(0f, StampRailSection.y / 2f, lipZ), new Vector3(railLength, StampRailSection.y, StampRailSection.x), rail);
            PrimitivePart(rack, "CapLeft", PrimitiveType.Cube, new Vector3(-railLength / 2f, (StampRailSection.y + 0.004f) / 2f, lipZ), new Vector3(0.012f, StampRailSection.y + 0.004f, StampRailSection.x + 0.006f), wood);
            PrimitivePart(rack, "CapRight", PrimitiveType.Cube, new Vector3(railLength / 2f, (StampRailSection.y + 0.004f) / 2f, lipZ), new Vector3(0.012f, StampRailSection.y + 0.004f, StampRailSection.x + 0.006f), wood);
        }
        Color labelInk = new Color(0.95f, 0.93f, 0.86f);

        (Clickable stamp, Transform die) Stamp(string name, float x, bool approved, string labelKey)
        {
            // In the brass drawer the dater stands in its cradle (the cradle's origin is its front-foot edge, toward the chair); else on the rack.
            Transform cradle = drawer != null ? drawer.Cradle(approved ? DrawerSequence.Approved : DrawerSequence.Denied) : null;
            Clickable click = EnsureClickBox(cradle != null ? cradle : rack, name);
            click.transform.localPosition = cradle != null ? new Vector3(0f, 0f, BrassPivotDepth) : new Vector3(x, 0f, 0f);
            StampShape shape = DaterBody(click.transform, approved);
            var box = click.GetComponent<BoxCollider>();
            box.center = new Vector3(0f, shape.Top / 2f, 0f);
            box.size = new Vector3(shape.HalfWidth * 2f + 0.006f, shape.Top, shape.HalfDepth * 2f + 0.006f);
            string word = UiText.Get(labelKey);
            Transform die = click.transform.Find("Die");

            if (drawer != null)
                Keyed(PlateWord(drawer.transform.Find("Plate" + name), word), labelKey);
            else
            {
                // The brass bracket from the lip to the dater's frame, and the word on the lip's top in front of the dater.
                PrimitivePart(rack, name + "Arm", PrimitiveType.Cube, new Vector3(x, 0.002f, -(DaterHalfDepth + StampLipGap / 2f)), new Vector3(0.012f, 0.004f, StampLipGap + 0.002f), brass);
                TextMeshPro label = FlatText(rack, name + "Label", new Vector3(x, StampRailSection.y + 0.0006f, lipZ),
                                             new Vector2(StampSpacing - 0.012f, StampRailSection.x - 0.004f), 0.2f, labelInk, FontStyles.Bold);
                label.text = word;
                Keyed(label, labelKey);
            }

            click.SetOutline(click.GetComponentsInChildren<Renderer>(true).Where(r => r.GetComponent<TextMeshPro>() == null && r.name != "Window").ToArray());
            // The dater is moved, not the paper (Saleh 2026-10-06): left-drag carries it over the desk; its click box is the drag's proxy.
            // Stamping is drag-only (Saleh's 1016a playtest: "should be dragging"): a click on a hanging dater strokes nothing.
            DeskDraggable drag = GetOrAdd<DeskDraggable>(click.gameObject);
            var soDrag = new SerializedObject(drag);
            SetRef(soDrag, "proxy", box);
            soDrag.ApplyModifiedProperties();
            return (click, die);
        }

        (Clickable denied, Transform deniedDie) = Stamp("Denied", -StampSpacing / 2f, false, "stamp.label.denied");
        (Clickable approved, Transform approvedDie) = Stamp("Approved", StampSpacing / 2f, true, "stamp.label.approved");
        // No light's shadows: under the hall's low light they cast long dark shapes across the papers (Papers, Please's bar casts none).
        foreach (MeshRenderer part in rack.GetComponentsInChildren<MeshRenderer>(true))
            part.shadowCastingMode = ShadowCastingMode.Off;
        // The rack's contact shadow instead (BUG-16a: the rack floated): the papers' drawn shadow, laid under its footprint by DeskStampTray.Lay.
        var shadow = new GameObject("ContactShadow", typeof(MeshFilter), typeof(MeshRenderer));
        shadow.transform.SetParent(rack, false);
        var shadowRenderer = shadow.GetComponent<MeshRenderer>();
        shadowRenderer.sharedMaterial = ShadowMaterial();
        shadowRenderer.shadowCastingMode = ShadowCastingMode.Off;
        shadowRenderer.receiveShadows = false;
        shadow.SetActive(false);
        rack.gameObject.SetActive(false);

        DeskStampTray stamps = GetOrAdd<DeskStampTray>(host.gameObject);
        var so = new SerializedObject(stamps);
        SetRef(so, "config", config);
        SetRef(so, "rack", rack);
        SetRef(so, "drawer", drawer);
        SetRef(so, "contactShadow", shadow.GetComponent<MeshFilter>());
        SetRef(so, "tab", tab);
        SetRef(so, "approvedStamp", approved);
        SetRef(so, "deniedStamp", denied);
        SetRef(so, "approvedDie", approvedDie);
        SetRef(so, "deniedDie", deniedDie);
        SetRef(so, "hint", hint);
        SetRef(so, "sound", sound);
        so.FindProperty("paperLayers").intValue = 1 << OfficeLayers.InteractableLayer;
        WireDaters(so);
        so.ApplyModifiedProperties();
        return stamps;
    }

    /// <summary>The art's DeskClean material <paramref name="name"/> (NOPE/Desk Anime: the desk's own look); without it, a stand-in of that colour.</summary>
    private static Material DeskMaterial(string name, Color colour) =>
        AssetDatabase.LoadAssetAtPath<Material>($"{DeskCleanFolder}/Materials/DeskClean_{name}.mat") ?? AnimeMaterial("StampAnime_" + name, colour);
}
