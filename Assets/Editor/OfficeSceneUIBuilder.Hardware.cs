using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

/// <summary>
/// The desk machine's hardware (the desk machine spec, build-order step 1;
/// Saleh 2026-10-07: "retro two-click stamps", "three actions"): the two
/// daters' bodies after his S-401 reference, the gate lever beside the desk
/// and RETURN and DETAIN as plain kit buttons by the counter (their own
/// hardware comes in step 2). Every prop is built in-engine from primitives
/// in the desk's materials under a prop contract the art may later fill
/// with its own meshes: a dater is a root holding Body (the glossy black
/// body: its rounded top, the window showing the die's print, the model
/// name and the side Button), Frame (the white frame), Die (the press point
/// at the foot, over the rubber) and Wheels (Day, Month, Year, turning
/// about their own axes); the lever is a root holding Base, Arm (its pivot
/// at the hinge) and the Arm's Knob (the click box). Rebuilt each run.
/// </summary>
public static partial class OfficeSceneUIBuilder
{
    /// <summary>Saleh's dater sounds (the GPT-made "cha-ka", split at the gap between its two clicks; the deep one for DENIED).</summary>
    private const string DaterSoundsFolder = "Assets/Art/Office/Sounds";

    /// <summary>A dater's measures (metres, its foot at the origin): the frame's height and half width and depth, the body's height over the frame and its half width and depth, the wheels' radius.</summary>
    private const float DaterFrameTop = 0.033f, DaterHalfWidth = 0.034f, DaterHalfDepth = 0.023f, DaterBodyHeight = 0.048f, DaterWheelRadius = 0.0095f;

    /// <summary>The model name on a dater's front (a generic stand-in for the reference's).</summary>
    private const string DaterModel = "TC-401  DATER";

    /// <summary>The gate lever's measures (metres; the hinge at the origin on the desk's plane): the floor below it, the column's side, the arm's length and its rest tilt away from the chair (degrees).</summary>
    private const float LeverFloor = 0.78f, LeverColumn = 0.08f, LeverArm = 0.5f, LeverRestTilt = 28f;

    /// <summary>RETURN's and DETAIN's size on the overlay and their place (reference px): right of the counter's middle, up from the bottom.</summary>
    private static readonly Vector2 VerdictButtonSize = new Vector2(230f, 76f);
    private const float VerdictButtonsRight = 560f, VerdictButtonsBottom = 24f, VerdictButtonsGap = 12f;

    /// <summary>
    /// A dater's body under <paramref name="root"/> (its foot at the origin,
    /// its front toward the chair, -z): the Frame (white plastic: two side
    /// plates, a top band and thin rims), the Die (the press point; the dark
    /// rubber under it), the Wheels (Day, Month, Year: dark rubber cylinders
    /// across the frame, axes along x, the tray paints their bands and turns
    /// them) and the Body (glossy black over the frame with a rounded top, its
    /// Window, the model name on its front and the side Button: green on
    /// APPROVED, red on DENIED, a click box that re-inks). Returns the
    /// measures the click box and the rack need.
    /// </summary>
    private static StampShape DaterBody(Transform root, Color button)
    {
        foreach (string part in new[] { "Frame", "Die", "Wheels", "Body" })
            DestroyChildIfPresent(root, part);
        Material frame = DaterMaterial("Dater_Frame", new Color(0.9f, 0.9f, 0.88f), 0.05f, 0.08f);
        Material black = DaterMaterial("Dater_Body", new Color(0.07f, 0.07f, 0.08f), 0.3f, 0.12f);
        Material rubber = DaterMaterial("Dater_Rubber", new Color(0.16f, 0.15f, 0.15f), 0f, 0.05f);
        Material glass = DaterMaterial("Dater_Window", new Color(0.2f, 0.22f, 0.26f), 0.4f, 0.2f);

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
        GameObject cap = PrimitivePart(reink.transform, "Cap", PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.015f, 0.003f, 0.015f), DaterMaterial(button.g > button.r ? "Dater_ButtonGreen" : "Dater_ButtonRed", button, 0.35f, 0.1f));
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
    /// The gate lever (the desk machine spec §2), rebuilt each run: Office/
    /// GateLever (the GateLever; the office binder lays it beside the desk,
    /// its hinge on the desk's plane) with its Base (a dark steel column from
    /// the floor, a floor plate and the hinge housing), its Arm (turned about
    /// the hinge, resting tilted away from the chair: a steel rod) and the
    /// Arm's Knob (a red ball; a click box on the Interactable layer: the
    /// pointer drags it down), and its AudioSource. Returns it.
    /// </summary>
    private static GateLever BuildGateLever(Transform office, DeskStampTray stamps)
    {
        DestroyChildIfPresent(office, "GateLever");
        Transform root = EnsureChild(office, "GateLever");
        Material steel = DaterMaterial("Lever_Steel", new Color(0.24f, 0.25f, 0.27f), 0.25f, 0.12f);
        Material rod = DaterMaterial("Lever_Rod", new Color(0.62f, 0.6f, 0.55f), 0.35f, 0.1f);
        Material knob = DaterMaterial("Lever_Knob", new Color(0.76f, 0.2f, 0.16f), 0.4f, 0.1f);

        Transform baseRoot = EnsureChild(root, "Base");
        PrimitivePart(baseRoot, "Column", PrimitiveType.Cube, new Vector3(0f, -LeverFloor / 2f, 0f), new Vector3(LeverColumn, LeverFloor, LeverColumn), steel);
        PrimitivePart(baseRoot, "Plate", PrimitiveType.Cube, new Vector3(0f, -LeverFloor + 0.01f, 0f), new Vector3(0.2f, 0.02f, 0.2f), steel);
        PrimitivePart(baseRoot, "Housing", PrimitiveType.Cube, new Vector3(0f, 0f, 0f), new Vector3(LeverColumn + 0.04f, 0.07f, LeverColumn + 0.02f), steel);
        GameObject quadrant = PrimitivePart(baseRoot, "Quadrant", PrimitiveType.Cylinder, new Vector3(0f, 0.01f, 0f), new Vector3(0.16f, 0.006f, 0.16f), steel);
        quadrant.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);

        Transform arm = EnsureChild(root, "Arm");
        arm.localPosition = Vector3.zero;
        arm.localRotation = Quaternion.Euler(LeverRestTilt, 0f, 0f);
        PrimitivePart(arm, "Rod", PrimitiveType.Cylinder, new Vector3(0f, LeverArm / 2f, 0f), new Vector3(0.034f, LeverArm / 2f, 0.034f), rod);
        Clickable grip = EnsureClickBox(arm, "Knob");
        grip.transform.localPosition = new Vector3(0f, LeverArm, 0f);
        GameObject ball = PrimitivePart(grip.transform, "Ball", PrimitiveType.Sphere, Vector3.zero, new Vector3(0.095f, 0.095f, 0.095f), knob);
        var box = grip.GetComponent<BoxCollider>();
        box.center = new Vector3(0f, -0.06f, 0f);
        box.size = new Vector3(0.14f, 0.26f, 0.14f);
        grip.SetOutline(new[] { ball.GetComponent<Renderer>() });
        foreach (MeshRenderer part in root.GetComponentsInChildren<MeshRenderer>(true))
            part.shadowCastingMode = ShadowCastingMode.Off;

        AudioSource sound = GetOrAdd<AudioSource>(root.gameObject);
        sound.playOnAwake = false;
        GateLever lever = GetOrAdd<GateLever>(root.gameObject);
        var so = new SerializedObject(lever);
        SetRef(so, "stamps", stamps);
        SetRef(so, "arm", arm);
        SetRef(so, "sound", sound);
        so.FindProperty("downSign").floatValue = -1f; // the rest tilt leans away from the chair; a pull brings the knob toward it and down
        so.ApplyModifiedProperties();
        return lever;
    }

    /// <summary>
    /// RETURN and DETAIN (the prototype's plain kit buttons, rebuilt each
    /// run): on the office overlay a host VerdictButtons with RETURN (the
    /// kit's brass plate) over DETAIN (its red plate) right of the counter's
    /// middle at the bottom, a full-screen red Flash over them (clear, no raycasts) and
    /// an AudioSource. Returns it.
    /// </summary>
    private static VerdictButtons BuildVerdictButtons(Transform overlay, DeskStampTray stamps)
    {
        DestroyChildIfPresent(overlay, "VerdictButtons");
        Transform host = Panel(overlay, "VerdictButtons", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);

        UiKitSO kit = UiKitAssets.Ensure();
        Button Plate(string name, string key, string piece, float bottom, Color? ink)
        {
            Button b = MakeButton(host, name, UiText.Get(key), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), null, ThemeRoleId.Button, key);
            var rt = (RectTransform)b.transform;
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = VerdictButtonSize;
            rt.anchoredPosition = new Vector2(VerdictButtonsRight, bottom);
            if (kit != null)
                KitScreens.Plate(b, kit, piece, ink: ink);
            return b;
        }
        Button detain = Plate("Detain", "hardware.detain", "plate_red", VerdictButtonsBottom, null);
        // The amber RETURN (the spec's colour) takes the dark ink: the light one is too faint on the brass plate's mid tone.
        Button back = Plate("Return", "hardware.return", "plate_brass", VerdictButtonsBottom + VerdictButtonSize.y + VerdictButtonsGap, kit != null ? kit.inkOnLight : (Color?)null);

        // The flash last, over the buttons (never behind a label), clear at rest; diegetic: no look recolours it.
        Transform flashHost = Panel(host, "Flash", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.85f, 0.08f, 0.06f, 0f), ThemeRoleId.DiegeticDevice);
        Image flash = flashHost.GetComponent<Image>();
        flash.raycastTarget = false;

        AudioSource sound = GetOrAdd<AudioSource>(host.gameObject);
        sound.playOnAwake = false;
        VerdictButtons buttons = GetOrAdd<VerdictButtons>(host.gameObject);
        var so = new SerializedObject(buttons);
        SetRef(so, "stamps", stamps);
        SetRef(so, "returnButton", back);
        SetRef(so, "detainButton", detain);
        SetRef(so, "flash", flash);
        SetRef(so, "sound", sound);
        so.ApplyModifiedProperties();
        return buttons;
    }
}
