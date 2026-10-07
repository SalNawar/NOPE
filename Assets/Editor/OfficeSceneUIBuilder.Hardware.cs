using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

/// <summary>
/// The desk machine's hardware (the desk machine spec, build-order step 1;
/// Saleh 2026-10-07: "retro two-click stamps", "three actions", then
/// "everything diegetic" and "no lever"): the two daters' bodies after his
/// S-401 reference and the DETAIN button on the desk (approval and denial
/// are the hand-back itself). Every prop is built in-engine from primitives
/// in the desk's materials under a prop contract the art may later fill
/// with its own meshes: a dater is a root holding Body (the glossy green
/// or red body: its rounded top, the window showing the die's print, the model
/// name and the side Button), Frame (the white frame), Die (the press point
/// at the foot, over the rubber) and Wheels (Day, Month, Year, turning
/// about their own axes); the DETAIN button is a root holding Base, Cover
/// (its pivot at the hinge) and Button (its Cap sinks). Rebuilt each run.
/// </summary>
public static partial class OfficeSceneUIBuilder
{
    /// <summary>Saleh's dater sounds (the GPT-made "cha-ka", split at the gap between its two clicks; the deep one for DENIED).</summary>
    private const string DaterSoundsFolder = "Assets/Art/Office/Sounds";

    /// <summary>A dater's measures (metres, its foot at the origin): the frame's height and half width and depth, the body's height over the frame and its half width and depth, the wheels' radius.</summary>
    private const float DaterFrameTop = 0.033f, DaterHalfWidth = 0.034f, DaterHalfDepth = 0.023f, DaterBodyHeight = 0.048f, DaterWheelRadius = 0.0095f;

    /// <summary>The colour of a dater's top window (the light grey of a clear plastic top over a white die plate).</summary>
    private static readonly Color DaterWindowColour = new Color(0.86f, 0.87f, 0.88f);

    /// <summary>The model name on a dater's front (a generic stand-in for the reference's).</summary>
    private const string DaterModel = "TC-401  DATER";

    /// <summary>The DETAIN button's measures (metres, its foot at the origin on the desk): the base's side and height, the mushroom's diameter, the cover's height over the base.</summary>
    private const float DetainBase = 0.1f, DetainBaseHeight = 0.022f, DetainMushroom = 0.05f, DetainCoverHeight = 0.045f;

    /// <summary>The DETAIN button's scale on the desk (its parts are drawn at desk-prop size; the art's props are large: the calculator is about 0.2 m across).</summary>
    private const float DetainScale = 1.7f;

    /// <summary>
    /// A dater's body under <paramref name="root"/> (its foot at the origin,
    /// its front toward the chair, -z): the Frame (white plastic: two side
    /// plates, a top band and thin rims), the Die (the press point; the dark
    /// rubber under it), the Wheels (Day, Month, Year: dark rubber cylinders
    /// across the frame, axes along x, the tray paints their bands and turns
    /// them) and the Body (Saleh 2026-10-07: a deep glossy green on
    /// APPROVED, red on DENIED, so they tell apart at a glance; over the frame
    /// with a rounded top, its Window, the model name on its front and the
    /// side Button in a lighter tint, a click box that re-inks). Returns the
    /// measures the click box and the rack need.
    /// </summary>
    private static StampShape DaterBody(Transform root, bool approved)
    {
        foreach (string part in new[] { "Frame", "Die", "Wheels", "Body" })
            DestroyChildIfPresent(root, part);
        Material frame = DaterMaterial("Dater_Frame", new Color(0.9f, 0.9f, 0.88f), 0.05f, 0.08f);
        // Saleh 2026-10-07: "approve and deny stamp need to be green and red so they are easy to tell apart": a deep, slightly glossy kit
        // green or red body (darkened so the window and the wheels read on it), its side button a lighter tint.
        Color bodyColour = approved ? new Color(0.13f, 0.38f, 0.21f) : new Color(0.52f, 0.11f, 0.12f);
        Color button = approved ? new Color(0.46f, 0.76f, 0.52f) : new Color(0.9f, 0.46f, 0.42f);
        Material black = DaterMaterial(approved ? "Dater_BodyGreen" : "Dater_BodyRed", bodyColour, 0.3f, 0.12f);
        Material rubber = DaterMaterial("Dater_Rubber", new Color(0.16f, 0.15f, 0.15f), 0f, 0.05f);
        // Saleh 2026-10-07: "the top part of the stamps should be white or grey, not black. It is hard to read": the window over the
        // die is a light grey clear top (the S-401's), so the green or red preview and the date read on it; set on every build.
        Material glass = DaterMaterial("Dater_Window", DaterWindowColour, 0.55f, 0.12f);
        if (glass != null && glass.HasProperty("_BaseColor") && glass.GetColor("_BaseColor") != DaterWindowColour)
        {
            glass.SetColor("_BaseColor", DaterWindowColour);
            if (glass.HasProperty("_HighlightStrength"))
                glass.SetFloat("_HighlightStrength", 0.55f);
            if (glass.HasProperty("_EdgeStrength"))
                glass.SetFloat("_EdgeStrength", 0.12f);
            EditorUtility.SetDirty(glass);
            AssetDatabase.SaveAssetIfDirty(glass);
        }

        Transform frameRoot = EnsureChild(root, "Frame");
        float plateH = DaterFrameTop - 0.002f;
        foreach (float side in new[] { -1f, 1f })
            PrimitivePart(frameRoot, side < 0f ? "SideLeft" : "SideRight", PrimitiveType.Cube, new Vector3(side * (DaterHalfWidth - 0.0025f), 0.002f + plateH / 2f, 0f),
                          new Vector3(0.005f, plateH, DaterHalfDepth * 2f), frame);
        PrimitivePart(frameRoot, "Band", PrimitiveType.Cube, new Vector3(0f, DaterFrameTop - 0.003f, 0f), new Vector3(DaterHalfWidth * 2f, 0.006f, DaterHalfDepth * 2f + 0.002f), frame);
        foreach (float side in new[] { -1f, 1f })
            PrimitivePart(frameRoot, side < 0f ? "RimFront" : "RimBack", PrimitiveType.Cube, new Vector3(0f, 0.004f, side * (DaterHalfDepth - 0.0015f)),
                          new Vector3(DaterHalfWidth * 2f - 0.008f, 0.004f, 0.003f), frame);

        Transform die = EnsureChild(root, "Die");
        die.localPosition = Vector3.zero;
        PrimitivePart(die, "Rubber", PrimitiveType.Cube, new Vector3(0f, 0.0015f, 0f), new Vector3(DaterHalfWidth * 2f - 0.012f, 0.003f, DaterHalfDepth * 2f - 0.014f), rubber);

        // The wheels side by side across the frame: day (narrow), month, year (wide).
        Transform wheels = EnsureChild(root, "Wheels");
        float[] widths = { 0.012f, 0.016f, 0.02f };
        string[] names = { "Day", "Month", "Year" };
        float x = -(widths[0] + widths[1] + widths[2] + 0.004f) / 2f;
        for (int i = 0; i < 3; i++)
        {
            Transform wheel = EnsureChild(wheels, names[i]);
            wheel.localPosition = new Vector3(x + widths[i] / 2f, 0.0175f, 0f);
            wheel.localRotation = Quaternion.Euler(0f, 0f, 90f); // the cylinder's axis (its local y) along the dater's x
            GameObject drum = PrimitivePart(wheel, "Band", PrimitiveType.Cylinder, Vector3.zero, new Vector3(DaterWheelRadius * 2f, widths[i] / 2f, DaterWheelRadius * 2f), rubber);
            drum.GetComponent<MeshRenderer>().sharedMaterial = DaterMaterial("Dater_Band" + names[i], Color.white, 0f, 0.05f);
            x += widths[i] + 0.002f;
        }

        // The body over the frame: a box, a rounded top, the window on top, the name on the front and the side button.
        Transform body = EnsureChild(root, "Body");
        body.localPosition = Vector3.zero;
        float bodyBottom = DaterFrameTop, boxTop = bodyBottom + DaterBodyHeight - DaterHalfDepth;
        PrimitivePart(body, "Shell", PrimitiveType.Cube, new Vector3(0f, (bodyBottom + boxTop) / 2f, 0f), new Vector3(DaterHalfWidth * 2f - 0.002f, boxTop - bodyBottom, DaterHalfDepth * 2f), black);
        GameObject top = PrimitivePart(body, "Top", PrimitiveType.Cylinder, new Vector3(0f, boxTop, 0f), new Vector3(DaterHalfDepth * 2f, DaterHalfWidth - 0.001f, DaterHalfDepth * 2f), black);
        top.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        float crown = boxTop + DaterHalfDepth;
        PrimitivePart(body, "WindowFrame", PrimitiveType.Cube, new Vector3(0f, crown - 0.0015f, 0f), new Vector3(DaterHalfWidth * 1.5f, 0.003f, DaterHalfDepth * 0.95f), glass);
        GameObject window = PrimitivePart(body, "Window", PrimitiveType.Quad, new Vector3(0f, crown + 0.0003f, 0f), new Vector3(DaterHalfWidth * 1.4f, DaterHalfWidth * 1.4f * DaterImpressionArt.Height / DaterImpressionArt.Width, 1f),
                                          AssetDatabase.LoadAssetAtPath<Material>($"{GameplayArtFolder}/Materials/Paper_Overlay.mat"));
        window.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // face up, its top edge away from the chair

        var name = new GameObject("Model", typeof(RectTransform), typeof(TextMeshPro));
        name.transform.SetParent(body, false);
        name.transform.localPosition = new Vector3(0f, bodyBottom + 0.012f, -DaterHalfDepth - 0.0006f);
        ((RectTransform)name.transform).sizeDelta = new Vector2(DaterHalfWidth * 1.7f, 0.008f);
        TextMeshPro label = name.GetComponent<TextMeshPro>();
        label.text = DaterModel;
        label.enableAutoSizing = true;
        label.fontSizeMin = 0.005f;
        label.fontSizeMax = 0.06f;
        label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.color = new Color(0.86f, 0.84f, 0.8f);
        label.fontStyle = FontStyles.Bold;
        label.sortingLayerID = GameplaySortingLayerId();

        Clickable reink = EnsureClickBox(body, "Button");
        reink.transform.localPosition = new Vector3(DaterHalfWidth + 0.003f, bodyBottom + 0.024f, 0f);
        GameObject cap = PrimitivePart(reink.transform, "Cap", PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.015f, 0.003f, 0.015f), DaterMaterial(approved ? "Dater_ButtonGreen" : "Dater_ButtonRed", button, 0.35f, 0.1f));
        cap.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        var capBox = reink.GetComponent<BoxCollider>();
        capBox.center = Vector3.zero;
        capBox.size = new Vector3(0.008f, 0.018f, 0.018f);
        reink.SetOutline(new[] { cap.GetComponent<Renderer>() });

        return new StampShape(DaterHalfWidth, DaterHalfDepth, DaterFrameTop, boxTop, crown);
    }

    /// <summary>A dater part's material in the desk's NOPE/Desk Anime shader (<paramref name="highlight"/> its painted gloss, <paramref name="edge"/> its contour), else URP Lit.</summary>
    private static Material DaterMaterial(string name, Color colour, float highlight, float edge) =>
        Shader.Find("NOPE/Desk Anime") != null
            ? EnsureMaterial(name, "NOPE/Desk Anime", m =>
            {
                m.SetColor("_BaseColor", colour);
                m.SetFloat("_HighlightStrength", highlight);
                m.SetFloat("_HighlightSize", 0.12f);
                m.SetFloat("_EdgeStrength", edge);
            })
            : LitMaterial(name, colour, highlight);

    /// <summary>The daters' print, words and sounds on the stamp tray: the papers' style, the date's face (GeistMono Bold: its atlas is readable) and Saleh's cha-ka split into press and release, the deep one for DENIED.</summary>
    private static void WireDaters(SerializedObject so)
    {
        SetRef(so, "style", EnsureFormStyle());
        SetRef(so, "daterFont", AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiKitFonts.Readout));
        SetRef(so, "approvedPress", DaterSound("stamp_press"));
        SetRef(so, "approvedRelease", DaterSound("stamp_release"));
        SetRef(so, "deniedPress", DaterSound("stamp_press_deep"));
        SetRef(so, "deniedRelease", DaterSound("stamp_release_deep"));
    }

    /// <summary>A dater sound by its file name (a warning when it is missing: the dater is then silent there).</summary>
    private static AudioClip DaterSound(string file)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{DaterSoundsFolder}/{file}.wav");
        if (clip == null)
            Debug.LogWarning($"[TimeDesk] The dater sound {DaterSoundsFolder}/{file}.wav is missing: that click is silent.");
        return clip;
    }

    /// <summary>
    /// The DETAIN button (the desk machine spec §2; Saleh 2026-10-07:
    /// "everything diegetic"), rebuilt each run: Office/DetainButton (the
    /// DetainButton; the office binder lays it on the desk where the office
    /// view shows DeskConfigSO.detainView) with its Base (a dark steel plinth
    /// with a hazard-yellow rim and "DETAIN" on its front), its Button (a
    /// short stem and its Cap, a red mushroom; a click box on the Interactable
    /// layer) and its Cover (hinged at the back edge: a clear box over the
    /// button, its Lid a click box of its own), an AudioSource, and on the
    /// office overlay its red flash (DetainFlash: clear at rest, no raycasts,
    /// over every panel). Returns it.
    /// </summary>
    private static DetainButton BuildDetainButton(Transform office, DeskStampTray stamps)
    {
        DestroyChildIfPresent(office, "DetainButton");
        Transform root = EnsureChild(office, "DetainButton");
        root.localScale = Vector3.one * DetainScale;
        Material steel = DaterMaterial("Detain_Steel", new Color(0.2f, 0.21f, 0.23f), 0.25f, 0.12f);
        Material hazard = DaterMaterial("Detain_Hazard", new Color(0.86f, 0.68f, 0.12f), 0.2f, 0.1f);
        Material red = DaterMaterial("Detain_Red", new Color(0.78f, 0.12f, 0.1f), 0.45f, 0.1f);
        Material glass = EnsureMaterial("Detain_Glass", "Universal Render Pipeline/Unlit", m =>
        {
            m.SetColor("_BaseColor", new Color(0.78f, 0.88f, 0.95f, 0.28f));
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            UnityEditor.BaseShaderGUI.SetMaterialKeywords(m);
        });

        Transform baseRoot = EnsureChild(root, "Base");
        PrimitivePart(baseRoot, "Plinth", PrimitiveType.Cube, new Vector3(0f, DetainBaseHeight / 2f, 0f), new Vector3(DetainBase, DetainBaseHeight, DetainBase), steel);
        PrimitivePart(baseRoot, "Rim", PrimitiveType.Cube, new Vector3(0f, DetainBaseHeight + 0.0015f, 0f), new Vector3(DetainBase + 0.004f, 0.003f, DetainBase + 0.004f), hazard);
        var label = new GameObject("Label", typeof(RectTransform), typeof(TextMeshPro));
        label.transform.SetParent(baseRoot, false);
        label.transform.localPosition = new Vector3(0f, DetainBaseHeight / 2f, -DetainBase / 2f - 0.0006f);
        ((RectTransform)label.transform).sizeDelta = new Vector2(DetainBase * 0.9f, DetainBaseHeight * 0.8f);
        TextMeshPro text = label.GetComponent<TextMeshPro>();
        text.text = UiText.Get("hardware.detain");
        text.enableAutoSizing = true;
        text.fontSizeMin = 0.005f;
        text.fontSizeMax = 0.1f;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.color = new Color(0.95f, 0.8f, 0.2f);
        text.fontStyle = FontStyles.Bold;
        text.sortingLayerID = GameplaySortingLayerId();

        Clickable button = EnsureClickBox(root, "Button");
        button.transform.localPosition = new Vector3(0f, DetainBaseHeight + 0.003f, 0f);
        PrimitivePart(button.transform, "Stem", PrimitiveType.Cylinder, new Vector3(0f, 0.006f, 0f), new Vector3(0.02f, 0.006f, 0.02f), steel);
        Transform cap = EnsureChild(button.transform, "Cap");
        cap.localPosition = new Vector3(0f, 0.014f, 0f);
        GameObject dome = PrimitivePart(cap, "Dome", PrimitiveType.Sphere, Vector3.zero, new Vector3(DetainMushroom, 0.022f, DetainMushroom), red);
        var buttonBox = button.GetComponent<BoxCollider>();
        buttonBox.center = new Vector3(0f, 0.014f, 0f);
        buttonBox.size = new Vector3(DetainMushroom, 0.03f, DetainMushroom);
        button.SetOutline(new[] { dome.GetComponent<Renderer>() });

        // The cover's hinge on the back edge (away from the chair); the clear box hangs forward from it over the button.
        Transform cover = EnsureChild(root, "Cover");
        cover.localPosition = new Vector3(0f, DetainBaseHeight + 0.003f, DetainBase / 2f);
        Clickable lid = EnsureClickBox(cover, "Lid");
        lid.transform.localPosition = new Vector3(0f, DetainCoverHeight / 2f, -DetainBase / 2f);
        GameObject box = PrimitivePart(lid.transform, "Glass", PrimitiveType.Cube, Vector3.zero, new Vector3(DetainBase * 0.9f, DetainCoverHeight, DetainBase * 0.9f), glass);
        var lidBox = lid.GetComponent<BoxCollider>();
        lidBox.center = Vector3.zero;
        lidBox.size = new Vector3(DetainBase * 0.9f, DetainCoverHeight, DetainBase * 0.9f);
        lid.SetOutline(new[] { box.GetComponent<Renderer>() });
        PrimitivePart(cover, "Hinge", PrimitiveType.Cube, Vector3.zero, new Vector3(DetainBase * 0.9f, 0.005f, 0.006f), steel);
        foreach (MeshRenderer part in root.GetComponentsInChildren<MeshRenderer>(true))
            part.shadowCastingMode = ShadowCastingMode.Off;

        // The flash over the hall: on the office overlay, last, so it lies over every panel.
        Transform overlay = stamps != null ? stamps.transform.parent : null;
        Image flash = null;
        if (overlay != null)
        {
            DestroyChildIfPresent(overlay, "DetainFlash");
            Transform flashHost = Panel(overlay, "DetainFlash", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.85f, 0.08f, 0.06f, 0f), ThemeRoleId.DiegeticDevice);
            flash = flashHost.GetComponent<Image>();
            flash.raycastTarget = false;
        }

        AudioSource sound = GetOrAdd<AudioSource>(root.gameObject);
        sound.playOnAwake = false;
        DetainButton detain = GetOrAdd<DetainButton>(root.gameObject);
        var so = new SerializedObject(detain);
        SetRef(so, "stamps", stamps);
        SetRef(so, "cover", cover);
        SetRef(so, "coverClick", lid);
        SetRef(so, "cap", cap);
        SetRef(so, "buttonClick", button);
        SetRef(so, "flash", flash);
        SetRef(so, "sound", sound);
        so.ApplyModifiedProperties();
        return detain;
    }
}
