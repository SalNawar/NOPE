using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

/// <summary>
/// The office builder's gameplay-layer parts (the physical desk, piece 7, moved
/// onto the art office): the desktop's own place (the World Space desktop on
/// its layer, the frame and clone cameras, the screen), the PC frame on the
/// overlay canvas, the traveller wheel and the overlay callouts, and the
/// Office root: the click boxes the office binder puts on the art's props at
/// load, the desk (its plane, the paper template with its face, the desk
/// catcher and the paper examiner, the scanner and its stand-in machine, the
/// day-1 notes), the traveller, the READY sign, the readouts, the decoration
/// slots, the booth coordinator and the binder; on the overlay the office
/// case HUD and the stamp tray (piece 10). Nothing here
/// knows where the art puts things: the binder reads the scene contract at
/// load. Part of <see cref="OfficeSceneUIBuilder"/>; Build() calls these in
/// its order.
/// </summary>
public static partial class OfficeSceneUIBuilder
{
    /// <summary>The desk tuning asset, created by the builder when missing (a designer's edits are kept).</summary>
    private const string DeskConfigPath = "Assets/Data/Config/Desk_Default.asset";

    /// <summary>The office scene contract, created by the builder when missing (the art side's anchors and a designer's edits are kept).</summary>
    private const string OfficeContractPath = "Assets/Data/Config/OfficeSceneContract.asset";

    /// <summary>Where the desk reactions live (created by the builder when missing; a designer's edits are kept).</summary>
    private const string DeskReactionFolder = "Assets/Data/Config/DeskReactions";

    /// <summary>The gameplay layer's shaders and materials.</summary>
    private const string GameplayArtFolder = "Assets/Art/Office/Gameplay";

    /// <summary>URP's unlit sprite material: the traveller's layers and the passport photo (unlit art, tinted into the room by the binder).</summary>
    private const string UnlitSpriteMaterialPath = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";

    /// <summary>The desktop canvas in canvas units: 4:3 at the old full-screen desktop's 1080-unit height.</summary>
    private static readonly Vector2 DesktopSize = new Vector2(1440f, 1080f);

    /// <summary>Metres per desktop canvas unit (the desktop is 14.4 x 10.8 m, far from the office).</summary>
    private const float DesktopScale = 0.01f;

    /// <summary>Where the desktop and its cameras live: far below the office floor, on their own layer.</summary>
    private static readonly Vector3 DesktopHome = new Vector3(0f, -100f, 0f);

    /// <summary>The PC frame on the overlay canvas (reference px): the whole monitor, left of centre.</summary>
    private static readonly Vector2 FrameSize = new Vector2(1240f, 1060f);

    /// <summary>The frame's distance from the screen's left edge (reference px).</summary>
    private const float FrameLeft = 20f;

    /// <summary>The frame's glass (reference px from the frame's bottom left): 4:3 like the desktop.</summary>
    private static readonly Rect FrameGlass = new Rect(60f, 160f, 1120f, 840f);

    /// <summary>The placeholder bezel art's scale (it is drawn at half the frame's reference size).</summary>
    private const int FrameArtDivisor = 2;

    /// <summary>The placeholder paper's colour.</summary>
    private static readonly Color PaperCream = new Color(0.95f, 0.92f, 0.82f, 1f);

    /// <summary>The paper's photo frame (shown only on a photo document).</summary>
    private static readonly Color PhotoGrey = new Color(0.55f, 0.56f, 0.58f, 1f);

    /// <summary>The photo's height as a share of its frame's.</summary>
    private const float PhotoFill = 0.92f;

    /// <summary>The desk notes' ink.</summary>
    private static readonly Color NoteInk = new Color(0.96f, 0.95f, 0.88f, 1f);

    /// <summary>A paper row's label ink (quieter than its value, dark enough to read at 720p).</summary>
    private static readonly Color PaperLabelInk = new Color(0.25f, 0.23f, 0.2f, 1f);

    /// <summary>The smallest auto-size of a paper row's texts (TMP world units: a few millimetres).</summary>
    private const float PaperTextMinSize = 0.03f;

    /// <summary>The desk's named spots (decoration hooks, item 7), in the desk plane's local XZ (metres): empty for now.</summary>
    private static readonly (string id, DeskSlotKind kind, Vector3 position)[] DeskSlots =
    {
        ("photo", DeskSlotKind.Decoration, new Vector3(-0.55f, 0f, 0.3f)),
        ("free_1", DeskSlotKind.Free, new Vector3(-0.75f, 0f, -0.35f)),
        ("free_2", DeskSlotKind.Free, new Vector3(0.75f, 0f, 0.3f)),
    };

    // -----------------------------
    // Assets
    // -----------------------------

    /// <summary>Returns a desk reaction asset, creating it with a kind, a tooltip key and an amplitude when missing (a designer's edits are kept).</summary>
    private static DeskReactionSO EnsureDeskReaction(string name, ReactionKind kind, string tooltipKey, float amplitude = 0.12f)
    {
        string path = $"{DeskReactionFolder}/{name}.asset";
        DeskReactionSO reaction = AssetDatabase.LoadAssetAtPath<DeskReactionSO>(path);
        if (reaction != null)
            return reaction;

        PlaceholderPng.EnsureFolderTree(DeskReactionFolder);
        reaction = ScriptableObject.CreateInstance<DeskReactionSO>();
        reaction.kind = kind;
        reaction.tooltipKey = tooltipKey;
        reaction.amplitude = amplitude;
        AssetDatabase.CreateAsset(reaction, path);
        return reaction;
    }

    /// <summary>Returns Desk_Default, creating it with the defaults when missing (a designer's edits are kept).</summary>
    private static DeskConfigSO EnsureDeskConfig()
    {
        DeskConfigSO config = AssetDatabase.LoadAssetAtPath<DeskConfigSO>(DeskConfigPath);
        if (config != null)
            return config;

        PlaceholderPng.EnsureFolderTree("Assets/Data/Config");
        config = ScriptableObject.CreateInstance<DeskConfigSO>();
        AssetDatabase.CreateAsset(config, DeskConfigPath);
        AssetDatabase.SaveAssets();
        return config;
    }

    /// <summary>Returns the office scene contract, creating it with the defaults when missing, and puts it on RunConfig (the load hook reads it there).</summary>
    private static OfficeSceneContractSO EnsureOfficeContract()
    {
        OfficeSceneContractSO contract = AssetDatabase.LoadAssetAtPath<OfficeSceneContractSO>(OfficeContractPath);
        if (contract == null)
        {
            PlaceholderPng.EnsureFolderTree("Assets/Data/Config");
            contract = ScriptableObject.CreateInstance<OfficeSceneContractSO>();
            AssetDatabase.CreateAsset(contract, OfficeContractPath);
        }

        RunConfigSO runConfig = FindAssetByName<RunConfigSO>("RunConfig");
        if (runConfig == null)
        {
            Debug.LogWarning("[TimeDesk] No RunConfig asset, so the art office's load hook has no contract. Create Assets/Resources/RunConfig.asset and rebuild.");
        }
        else if (runConfig.officeContract != contract || runConfig.officeGameplaySceneName != GameplaySceneName)
        {
            runConfig.officeContract = contract;
            runConfig.officeGameplaySceneName = GameplaySceneName;
            EditorUtility.SetDirty(runConfig);
        }

        AssetDatabase.SaveAssets();
        return contract;
    }

    /// <summary>Returns a material of the gameplay layer, creating it on <paramref name="shader"/> when missing; <paramref name="setup"/> runs on a new one only (a designer's edits are kept).</summary>
    private static Material EnsureMaterial(string name, string shader, System.Action<Material> setup)
    {
        string path = $"{GameplayArtFolder}/Materials/{name}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null)
            return material;

        PlaceholderPng.EnsureFolderTree($"{GameplayArtFolder}/Materials");
        Shader s = Shader.Find(shader);
        if (s == null)
        {
            Debug.LogError($"[TimeDesk] Shader '{shader}' not found, so {path} was not made.");
            return null;
        }

        material = new Material(s);
        setup?.Invoke(material);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    /// <summary>A lit (URP Lit) material of one colour.</summary>
    private static Material LitMaterial(string name, Color colour, float smoothness) =>
        EnsureMaterial(name, "Universal Render Pipeline/Lit", m =>
        {
            m.SetColor("_BaseColor", colour);
            m.SetFloat("_Smoothness", smoothness);
            m.SetFloat("_Metallic", 0f);
        });

    // -----------------------------
    // Layers and the build list
    // -----------------------------

    /// <summary>Makes sure the project has a user layer named <paramref name="name"/> (the first free one from 8).</summary>
    private static void EnsureLayer(string name)
    {
        if (LayerMask.NameToLayer(name) >= 0)
            return;

        var tags = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layers = tags.FindProperty("layers");
        for (int i = 8; i < layers.arraySize; i++)
        {
            SerializedProperty layer = layers.GetArrayElementAtIndex(i);
            if (!string.IsNullOrEmpty(layer.stringValue))
                continue;
            layer.stringValue = name;
            tags.ApplyModifiedProperties();
            return;
        }

        Debug.LogError($"[TimeDesk] No free layer for '{name}'.");
    }

    /// <summary>Puts the gameplay layer in the build list right after the art office (whose load brings it).</summary>
    private static void EnsureBuildSettings()
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        scenes.RemoveAll(s => s.path == GameplayScenePath);
        int art = scenes.FindIndex(s => s.path == ArtScenePath);
        var gameplay = new EditorBuildSettingsScene(GameplayScenePath, true);
        if (art >= 0)
            scenes.Insert(art + 1, gameplay);
        else
            scenes.Add(gameplay);

        EditorBuildSettingsScene[] list = scenes.ToArray();
        bool same = list.Length == EditorBuildSettings.scenes.Length;
        for (int i = 0; same && i < list.Length; i++)
            same = list[i].path == EditorBuildSettings.scenes[i].path && list[i].enabled == EditorBuildSettings.scenes[i].enabled;
        if (!same)
            EditorBuildSettings.scenes = list;
    }

    /// <summary>The object's component of type T, added when missing (the editor's GetComponent returns a fake null, so ?? cannot be used).</summary>
    private static T GetOrAdd<T>(GameObject host) where T : Component
    {
        T component = host.GetComponent<T>();
        return component != null ? component : host.AddComponent<T>();
    }

    /// <summary>Sets a subtree's layer.</summary>
    private static void SetLayer(Transform root, int layer)
    {
        root.gameObject.layer = layer;
        foreach (Transform child in root)
            SetLayer(child, layer);
    }

    // -----------------------------
    // The desktop, its cameras and the screen
    // -----------------------------

    /// <summary>
    /// The desktop's own place, far below the office on the PCDesktop layer:
    /// the World Space desktop canvas (1440 x 1080 units at 1 cm each), the
    /// frame camera (draws the desktop into the PC frame's glass; the canvas's
    /// event camera), the clone camera (draws it into the office PC's texture),
    /// and the MonitorScreen and PcScreenClone that own them. Idempotent.
    /// </summary>
    private static MonitorScreen BuildPcDesktop(Canvas desktopCanvas, DeskConfigSO config, out Camera frameCamera)
    {
        GameObject root = GameObject.Find("PcDesktop") is GameObject found ? found : new GameObject("PcDesktop");
        root.transform.SetPositionAndRotation(DesktopHome, Quaternion.identity);
        root.transform.localScale = Vector3.one;

        Transform canvas = desktopCanvas.transform;
        canvas.SetParent(root.transform, false);
        canvas.localPosition = Vector3.zero;
        canvas.localRotation = Quaternion.identity;
        canvas.localScale = Vector3.one * DesktopScale;

        float halfHeight = DesktopSize.y * DesktopScale / 2f;
        frameCamera = EnsureDesktopCamera(root.transform, "FrameCamera", halfHeight, 10f);
        Camera cloneCamera = EnsureDesktopCamera(root.transform, "CloneCamera", halfHeight, -10f);
        frameCamera.enabled = false;
        cloneCamera.enabled = false;
        desktopCanvas.worldCamera = frameCamera;

        Material clone = EnsureMaterial("PcScreenClone", "TimeDesk/PlanarScreen", null);
        PcScreenClone screenClone = GetOrAdd<PcScreenClone>(root.gameObject);
        var soClone = new SerializedObject(screenClone);
        SetRef(soClone, "cloneCamera", cloneCamera);
        SetRef(soClone, "cloneMaterial", clone);
        SetRef(soClone, "config", config);
        soClone.ApplyModifiedProperties();

        MonitorScreen screen = GetOrAdd<MonitorScreen>(root.gameObject);
        var so = new SerializedObject(screen);
        SetRef(so, "desktopCanvas", desktopCanvas);
        SetRef(so, "desktopRaycaster", desktopCanvas.GetComponent<GraphicRaycaster>());
        SetRef(so, "clone", screenClone);
        SetRef(so, "config", config);
        so.ApplyModifiedProperties();

        SetLayer(root.transform, OfficeLayers.PcDesktopLayer);
        return screen;
    }

    /// <summary>An orthographic camera looking at the desktop that draws only its layer (no post-processing, no shadows). Idempotent.</summary>
    private static Camera EnsureDesktopCamera(Transform root, string name, float orthoSize, float depth)
    {
        Transform t = EnsureChild(root, name);
        t.localPosition = new Vector3(0f, 0f, -10f);
        t.localRotation = Quaternion.identity;

        Camera cam = GetOrAdd<Camera>(t.gameObject);
        cam.orthographic = true;
        cam.orthographicSize = orthoSize;
        cam.nearClipPlane = 1f;
        cam.farClipPlane = 20f;
        cam.cullingMask = 1 << OfficeLayers.PcDesktopLayer;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.depth = depth;
        cam.allowHDR = false;
        cam.allowMSAA = false;

        UniversalAdditionalCameraData data = GetOrAdd<UniversalAdditionalCameraData>(t.gameObject);
        data.renderType = CameraRenderType.Base;
        data.renderPostProcessing = false;
        data.renderShadows = false;
        data.antialiasing = AntialiasingMode.None;
        data.requiresColorOption = CameraOverrideOption.Off;
        data.requiresDepthOption = CameraOverrideOption.Off;
        return cam;
    }

    // -----------------------------
    // The PC frame (overlay)
    // -----------------------------

    /// <summary>
    /// The PC frame on the office overlay canvas, rebuilt each run: an
    /// always-active host with the PcFrame; its Root (inactive until opened)
    /// holds the full-screen exit catcher (a click outside the frame closes
    /// it), the bezel (placeholder art; clicks on it do nothing), the Glass the
    /// frame camera draws into (4:3; the catcher and the bezel let clicks
    /// through there), the red close X, the power button and LED, and the
    /// brand plate. Returns the power LED and button through out parameters.
    /// </summary>
    private static PcFrame BuildPcFrame(Transform overlay, Camera frameCamera, OfficeViewController view, out Image powerLed, out Button powerButton)
    {
        DestroyChildIfPresent(overlay, "PcFrame");
        Transform host = Panel(overlay, "PcFrame", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        Transform root = Panel(host, "Root", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);

        Transform catcher = Panel(root, "ExitCatcher", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0f), ThemeRoleId.ClickCatcher);
        ClickCatcher exit = catcher.gameObject.AddComponent<ClickCatcher>();
        WirePersistentVoid(exit, "onClick", view, nameof(OfficeViewController.FocusOffice));

        Transform frame = Panel(root, "Frame", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, FrameSize, Color.white, ThemeRoleId.DiegeticDevice);
        var frameRect = (RectTransform)frame;
        frameRect.pivot = new Vector2(0f, 0.5f);
        frameRect.anchoredPosition = new Vector2(FrameLeft, 0f);
        Image bezel = frame.GetComponent<Image>();
        bezel.sprite = EnsureOfficeShape("pc_frame", (int)FrameSize.x / FrameArtDivisor, (int)FrameSize.y / FrameArtDivisor, Center, FramePixel);
        bezel.type = Image.Type.Simple;

        Transform glass = Panel(frame, "Glass", Vector2.zero, Vector2.zero, Vector2.zero, FrameGlass.size, null);
        var glassRect = (RectTransform)glass;
        glassRect.pivot = Vector2.zero;
        glassRect.anchoredPosition = FrameGlass.position;

        catcher.gameObject.AddComponent<RectHoleRaycastFilter>();
        frame.gameObject.AddComponent<RectHoleRaycastFilter>();
        foreach (RectHoleRaycastFilter filter in new[] { catcher.GetComponent<RectHoleRaycastFilter>(), frame.GetComponent<RectHoleRaycastFilter>() })
        {
            var soFilter = new SerializedObject(filter);
            SetRef(soFilter, "hole", glassRect);
            soFilter.ApplyModifiedProperties();
        }

        // Piece 10: held papers beside the open frame take clicks through a second hole in the catcher (PaperExaminer sizes it).
        Transform examineHole = Panel(root, "ExamineHole", Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, null);
        ((RectTransform)examineHole).pivot = Vector2.zero;
        var soHole = new SerializedObject(catcher.gameObject.AddComponent<RectHoleRaycastFilter>());
        SetRef(soHole, "hole", examineHole);
        soHole.ApplyModifiedProperties();

        Button close = MakeButton(frame, "CloseButton", "", new Vector2(1f, 1f), new Vector2(1f, 1f), Color.white, ThemeRoleId.DiegeticDevice);
        var closeRect = (RectTransform)close.transform;
        closeRect.pivot = new Vector2(1f, 1f);
        closeRect.sizeDelta = new Vector2(64f, 64f);
        closeRect.anchoredPosition = new Vector2(-14f, -14f);
        close.GetComponent<Image>().sprite = EnsureOfficeShape("pc_close", 48, 48, Center, CloseButtonPixel);
        WirePersistentVoid(close, "m_OnClick", view, nameof(OfficeViewController.FocusOffice));

        powerButton = MakeButton(frame, "PowerButton", "", new Vector2(1f, 0f), new Vector2(1f, 0f), Color.white, ThemeRoleId.DiegeticDevice);
        var powerRect = (RectTransform)powerButton.transform;
        powerRect.pivot = new Vector2(1f, 0f);
        powerRect.sizeDelta = new Vector2(64f, 64f);
        powerRect.anchoredPosition = new Vector2(-70f, 46f);
        powerButton.GetComponent<Image>().sprite = EnsureOfficeShape("crt_power", 28, 28, Center, PowerButtonPixel);

        powerLed = Panel(frame, "PowerLed", new Vector2(1f, 0f), new Vector2(1f, 0f), Vector2.zero, new Vector2(18f, 18f), Color.white, ThemeRoleId.DiegeticDevice).GetComponent<Image>();
        ((RectTransform)powerLed.transform).anchoredPosition = new Vector2(-170f, 78f);
        powerLed.sprite = EnsureOfficeShape("crt_led", 8, 8, Center, LedPixel);
        powerLed.raycastTarget = false;

        TMP_Text brand = Text(frame, "Brand", "CHRONODESK 2150", 34, TextAlignmentOptions.Center, new Vector2(0.3f, 0f), new Vector2(0.7f, 0f), new Color(0.42f, 0.38f, 0.3f, 1f),
                              ThemeRoleId.DiegeticDevice, style: FontStyles.Bold);
        var brandRect = (RectTransform)brand.transform;
        brandRect.sizeDelta = new Vector2(0f, 60f);
        brandRect.anchoredPosition = new Vector2(0f, 78f);
        brand.raycastTarget = false;

        PcFrame pcFrame = host.gameObject.AddComponent<PcFrame>();
        var so = new SerializedObject(pcFrame);
        SetRef(so, "root", root.gameObject);
        SetRef(so, "glass", glassRect);
        SetRef(so, "frameCamera", frameCamera);
        SetRef(so, "examineHole", examineHole);
        so.ApplyModifiedProperties();

        root.gameObject.SetActive(false);
        return pcFrame;
    }

    /// <summary>Placeholder bezel (half the frame's reference size): warm ivory plastic with rounded corners, a dark gasket round a transparent 4:3 glass, a darker chin.</summary>
    private static Color32 FramePixel(int x, int y)
    {
        int w = (int)FrameSize.x / FrameArtDivisor;
        int h = (int)FrameSize.y / FrameArtDivisor;
        float gx0 = FrameGlass.xMin / FrameArtDivisor, gx1 = FrameGlass.xMax / FrameArtDivisor;
        float gy0 = FrameGlass.yMin / FrameArtDivisor, gy1 = FrameGlass.yMax / FrameArtDivisor;
        const float radius = 34f;

        float cx = Mathf.Clamp(x + 0.5f, radius, w - radius);
        float cy = Mathf.Clamp(y + 0.5f, radius, h - radius);
        float corner = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
        if (corner > radius)
            return new Color32(0, 0, 0, 0);
        if (x + 0.5f >= gx0 && x + 0.5f <= gx1 && y + 0.5f >= gy0 && y + 0.5f <= gy1)
            return new Color32(0, 0, 0, 0);

        float dx = Mathf.Max(gx0 - (x + 0.5f), 0f, (x + 0.5f) - gx1);
        float dy = Mathf.Max(gy0 - (y + 0.5f), 0f, (y + 0.5f) - gy1);
        float fromGlass = Mathf.Sqrt(dx * dx + dy * dy);
        if (fromGlass < 9f)
            return Color32.Lerp(new Color32(46, 43, 38, 255), new Color32(92, 86, 74, 255), fromGlass / 9f);

        float fromEdge = radius - corner + Mathf.Min(Mathf.Min(x, w - 1 - x), Mathf.Min(y, h - 1 - y));
        if (fromEdge < 3f)
            return new Color32(150, 138, 112, 255);

        float t = y / (float)h;
        var top = new Color32(228, 214, 184, 255);
        var bottom = new Color32(204, 189, 156, 255);
        Color32 plastic = Color32.Lerp(bottom, top, t);
        if (y < gy0 - 2f)
            plastic = Color32.Lerp(plastic, new Color32(186, 171, 140, 255), 0.35f);
        if (Mathf.Abs(y - (gy0 - 2f)) < 1f)
            plastic = new Color32(170, 156, 126, 255);
        if (fromGlass < 16f)
            plastic = Color32.Lerp(new Color32(176, 163, 134, 255), plastic, (fromGlass - 9f) / 7f);
        return plastic;
    }

    /// <summary>Placeholder close button: a red rounded square with a white X.</summary>
    private static Color32 CloseButtonPixel(int x, int y)
    {
        float cx = Mathf.Clamp(x + 0.5f, 8f, 40f), cy = Mathf.Clamp(y + 0.5f, 8f, 40f);
        float corner = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
        if (corner > 8f)
            return new Color32(0, 0, 0, 0);
        if (corner > 6f || x < 2 || y < 2 || x > 45 || y > 45)
            return new Color32(120, 28, 22, 255);
        float u = x - 23.5f, v = y - 23.5f;
        bool cross = (Mathf.Abs(u - v) < 3.6f || Mathf.Abs(u + v) < 3.6f) && Mathf.Abs(u) < 12f && Mathf.Abs(v) < 12f;
        return cross ? new Color32(255, 250, 244, 255) : new Color32(214, 64, 50, 255);
    }

    /// <summary>Placeholder power button: a cream round face on a dark rim, with a power glyph.</summary>
    private static Color32 PowerButtonPixel(int x, int y)
    {
        float dx = x - 13.5f;
        float dy = y - 13.5f;
        float d = Mathf.Sqrt(dx * dx + dy * dy);
        if (d > 13.5f)
            return new Color32(0, 0, 0, 0);
        if (d > 11.5f)
            return new Color32(74, 70, 62, 255);

        var glyph = new Color32(90, 86, 78, 255);
        bool bar = Mathf.Abs(dx) < 1.3f && dy > 0f && dy < 7.5f;
        bool ring = d > 5f && d < 7f && !(dy > 0f && Mathf.Abs(dx) < 3f);
        return bar || ring ? glyph : new Color32(214, 208, 190, 255);
    }

    /// <summary>Placeholder LED: a white disc, tinted on and off by MonitorScreen.</summary>
    private static Color32 LedPixel(int x, int y)
    {
        float dx = x - 3.5f;
        float dy = y - 3.5f;
        return dx * dx + dy * dy <= 14.5f ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
    }

    // -----------------------------
    // The Office root: click boxes, desk, traveller, readouts
    // -----------------------------

    /// <summary>The Office root and its view controller (the frame opens and closes through it). Idempotent.</summary>
    private static OfficeViewController EnsureOfficeRoot()
    {
        GameObject root = GameObject.Find("Office") is GameObject found ? found : new GameObject("Office");
        root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        return GetOrAdd<OfficeViewController>(root.gameObject);
    }

    /// <summary>Finds or creates a plain child object under a parent.</summary>
    private static Transform EnsureChild(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
            return existing;

        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    /// <summary>A click box: a child with a BoxCollider on the Interactable layer and a Clickable (the office binder places and sizes it at load). Idempotent.</summary>
    private static Clickable EnsureClickBox(Transform parent, string name)
    {
        Transform t = EnsureChild(parent, name);
        t.gameObject.layer = OfficeLayers.InteractableLayer;
        if (t.GetComponent<BoxCollider>() == null)
            t.gameObject.AddComponent<BoxCollider>();
        return GetOrAdd<Clickable>(t.gameObject);
    }

    /// <summary>
    /// The Office root's gameplay objects, all placed by the office binder at
    /// load: the PC's and its power knob's click boxes, the READY sign (with a
    /// stand-in sign), the desk (its plane, the papers' root, the hand-over
    /// point, the paper template, the decoration slots), the scanner (with a
    /// stand-in machine) and the two day-1 notes, the traveller, the props'
    /// click boxes and reactions, the readouts, the booth coordinator and the
    /// binder, wired to each other; then the desk's checks. Idempotent.
    /// </summary>
    private static BoothCoordinator BuildOffice(OfficeViewController view, MonitorScreen screen, Button framePower, DeskConfigSO config,
                                                OfficeSceneContractSO contract, TravellerWheel wheel, OverlayCallout[] callouts,
                                                OverlayCallout tooltip, TMP_Text trayClockText, ShiftClockDriver clock,
                                                ContentLibrarySO library, FallbackHud hud, PcFrame pcFrame, StampTray stampTray,
                                                OfficeCaseHud caseHud, out Clickable readySign)
    {
        Transform office = view.transform;

        // The PC opens the frame; its knob turns the screen on and off.
        Clickable pc = EnsureClickBox(office, "PC");
        WirePersistentVoid(pc, "onClick", view, nameof(OfficeViewController.FocusMonitor));
        Clickable pcPower = EnsureClickBox(office, "PCPower");
        WirePersistentVoid(pcPower, "onClick", screen, nameof(MonitorScreen.TogglePower));
        WirePersistentVoid(framePower, "m_OnClick", screen, nameof(MonitorScreen.TogglePower));

        // READY only releases GameManager's gate (its Clickable is GameManager.readySign).
        readySign = EnsureClickBox(office, "ReadySign");
        ClearPersistentCalls(readySign, "onClick");
        GameObject readyPlaceholder = BuildReadyPlaceholder(readySign.transform);

        // The desk, the scanner and the notes.
        DeskController desk = BuildDesk(office, config, pcFrame, out DeskScanner scanner, out GameObject scannerPlaceholder, out TextMeshPro scanHint);
        var soScanner = new SerializedObject(scanner);
        SetRef(soScanner, "reaction", WireReaction(scanner.GetComponent<Clickable>(), EnsureDeskReaction("Reaction_Scanner", ReactionKind.Pulse, ""), tooltip, null));
        soScanner.ApplyModifiedProperties();

        // The traveller and the wheel's openers (the traveller and the desk intercom).
        TravellerView traveller = BuildTraveller(office, out Clickable travellerZone);
        WirePersistentVoid(travellerZone, "onClick", wheel, nameof(TravellerWheel.Open));
        var soWheel = new SerializedObject(wheel);
        SetRef(soWheel, "traveller", traveller);
        soWheel.ApplyModifiedProperties();
        TextMeshPro wheelHint = FloatingNote(office, "WheelHint");

        // The props: a click box and a reaction each (the binder hands them the art prop and its readout).
        Transform propsRoot = EnsureChild(office, "Props");
        var bindings = new List<OfficeSceneBinder.PropBinding>();
        var clicks = new List<Clickable> { scanner.GetComponent<Clickable>() };
        void Prop(string name, OfficeAnchorId anchor, DeskReactionSO reaction, OfficeAnchorId? readout, string itemId)
        {
            Clickable click = EnsureClickBox(propsRoot, name);
            WireReaction(click, reaction, tooltip, null);
            if (itemId != null)
                SetDeskItem(click.transform, itemId);
            bindings.Add(new OfficeSceneBinder.PropBinding { anchor = anchor, click = click, hasReadout = readout.HasValue, readout = readout ?? default });
            clicks.Add(click);
        }

        Prop("Stamp", OfficeAnchorId.Stamp, EnsureDeskReaction("Reaction_Stamp", ReactionKind.Squash, ""), null, "stamp");
        Prop("Intercom", OfficeAnchorId.Intercom, EnsureDeskReaction("Reaction_Intercom", ReactionKind.Squash, ""), null, null);
        Prop("Till", OfficeAnchorId.Till, EnsureDeskReaction("Reaction_Till", ReactionKind.Nudge, "tooltip.credits", 0.02f), OfficeAnchorId.ReadoutCredits, null);
        Prop("StabilityMonitor", OfficeAnchorId.StabilityMonitor, EnsureDeskReaction("Reaction_Stability", ReactionKind.None, "tooltip.stability"), OfficeAnchorId.ReadoutStability, null);
        Prop("Calendar", OfficeAnchorId.Calendar, EnsureDeskReaction("Reaction_Calendar", ReactionKind.None, "tooltip.day"), OfficeAnchorId.ReadoutDay, null);
        Prop("Clock", OfficeAnchorId.Clock, EnsureDeskReaction("Reaction_Clock", ReactionKind.None, "tooltip.value"), OfficeAnchorId.ReadoutClock, null);
        Prop("Calculator", OfficeAnchorId.Calculator, EnsureDeskReaction("Reaction_Calculator", ReactionKind.Squash, ""), null, "calculator");
        Prop("PenPot", OfficeAnchorId.PenPot, EnsureDeskReaction("Reaction_PenPot", ReactionKind.Wobble, ""), null, "pen_pot");
        Prop("Stapler", OfficeAnchorId.Stapler, EnsureDeskReaction("Reaction_Stapler", ReactionKind.Squash, ""), null, "stapler");
        WirePersistentVoid(propsRoot.Find("Intercom").GetComponent<Clickable>(), "onClick", wheel, nameof(TravellerWheel.Open));
        WirePersistentVoid(propsRoot.Find("Stamp").GetComponent<Clickable>(), "onClick", stampTray, nameof(StampTray.Open));
        AssetDatabase.SaveAssets();

        // The readouts (the binder hands them the art's texts or the fallback HUD's).
        Transform readoutsHost = EnsureChild(office, "Readouts");
        OfficeReadouts readouts = GetOrAdd<OfficeReadouts>(readoutsHost.gameObject);
        AudioSource ding = GetOrAdd<AudioSource>(readoutsHost.gameObject);
        ding.playOnAwake = false;
        var soReadouts = new SerializedObject(readouts);
        SetRef(soReadouts, "creditsDing", ding);
        soReadouts.ApplyModifiedProperties();
        ShiftClockReadouts clockReadouts = GetOrAdd<ShiftClockReadouts>(readoutsHost.gameObject);
        var soClock = new SerializedObject(clockReadouts);
        SetRef(soClock, "driver", clock);
        SetRef(soClock, "trayClockText", trayClockText);
        soClock.ApplyModifiedProperties();

        // The booth coordinator applies the input rules to all of it.
        BoothCoordinator coordinator = GetOrAdd<BoothCoordinator>(office.gameObject);
        var so = new SerializedObject(coordinator);
        SetRef(so, "view", view);
        SetRef(so, "screen", screen);
        SetRef(so, "desk", desk);
        SetRef(so, "wheel", wheel);
        SetRef(so, "crt", pc);
        SetRef(so, "powerButton", pcPower);
        SetRef(so, "framePowerButton", framePower);
        SetRef(so, "travellerHitZone", travellerZone);
        SerializedArrays.Set(so, "props", clicks);
        SetRef(so, "wheelHint", wheelHint);
        SetRef(so, "config", config);
        SetRef(so, "examiner", desk.transform.Find("Examiner").GetComponent<PaperExaminer>());
        SetRef(so, "stampTray", stampTray);
        SetRef(so, "hud", caseHud);
        so.ApplyModifiedProperties();

        // The binder puts all of it on the art office at load.
        OfficeSceneBinder binder = GetOrAdd<OfficeSceneBinder>(office.gameObject);
        var soBinder = new SerializedObject(binder);
        SetRef(soBinder, "contract", contract);
        SetRef(soBinder, "config", config);
        SetRef(soBinder, "wheel", wheel);
        SerializedArrays.Set(soBinder, "callouts", callouts);
        SetRef(soBinder, "examiner", desk.transform.Find("Examiner").GetComponent<PaperExaminer>());
        SetRef(soBinder, "stampTray", stampTray);
        SetRef(soBinder, "deskCatcher", desk.transform.Find("Catcher").GetComponent<BoxCollider>());
        SetRef(soBinder, "screenClone", screen.GetComponent<PcScreenClone>());
        SetRef(soBinder, "pc", pc);
        SetRef(soBinder, "pcPower", pcPower);
        SetRef(soBinder, "surface", desk.GetComponent<DeskSurface>());
        SetRef(soBinder, "scanner", scanner);
        SetRef(soBinder, "scannerPlaceholder", scannerPlaceholder);
        SetRef(soBinder, "handOver", desk.transform.Find("HandOver"));
        SetRef(soBinder, "scanHint", scanHint.transform);
        SetRef(soBinder, "traveller", traveller);
        SetRef(soBinder, "wheelHint", wheelHint.transform);
        SetRef(soBinder, "readySign", readySign);
        SetRef(soBinder, "readyPlaceholder", readyPlaceholder);
        SerializedProperty propList = soBinder.FindProperty("props");
        propList.arraySize = bindings.Count;
        for (int i = 0; i < bindings.Count; i++)
        {
            SerializedProperty p = propList.GetArrayElementAtIndex(i);
            p.FindPropertyRelative("anchor").enumValueIndex = (int)bindings[i].anchor;
            p.FindPropertyRelative("click").objectReferenceValue = bindings[i].click;
            p.FindPropertyRelative("hasReadout").boolValue = bindings[i].hasReadout;
            p.FindPropertyRelative("readout").enumValueIndex = (int)bindings[i].readout;
        }
        SetRef(soBinder, "readouts", readouts);
        SetRef(soBinder, "clock", clockReadouts);
        SetRef(soBinder, "fallbackHud", hud.root);
        SetRef(soBinder, "hudDay", hud.day);
        SetRef(soBinder, "hudStability", hud.stability);
        SetRef(soBinder, "hudCredits", hud.credits);
        SetRef(soBinder, "hudClock", hud.clock);
        soBinder.ApplyModifiedProperties();

        // Checks: every paper a traveller carries has a spawn slot, and every document's rows fit its paper's face.
        int maxPapers = library != null ? ContentLibraryValidator.MaxDocuments(ContentLibraryValidator.TravellerBlueprints(library)) : 0;
        int slots = config.paperSpawnSlots != null ? config.paperSpawnSlots.Length : 0;
        if (slots < maxPapers)
            Debug.LogError($"[TimeDesk] The desk has {slots} paper spawn slots but a traveller can carry {maxPapers} papers; add slots in Desk_Default.");
        if (library != null)
        {
            var checkedTemplates = new HashSet<DocumentTemplateSO>();
            foreach (CaseBlueprintSO blueprint in ContentLibraryValidator.TravellerBlueprints(library))
                foreach (DocumentTemplateSO template in blueprint != null && blueprint.DocumentTemplates != null ? blueprint.DocumentTemplates : new DocumentTemplateSO[0])
                {
                    if (template == null || !checkedTemplates.Add(template))
                        continue;
                    int fields = template.fieldSpecs != null ? template.fieldSpecs.Length : 0;
                    int capacity = PaperFace.Capacity(template.showsPhoto, config.face);
                    if (fields > capacity)
                        Debug.LogError($"[TimeDesk] {template.displayName} has {fields} fields but a paper face holds {capacity}; raise Desk_Default.face or shorten the template.");
                }
        }

        return coordinator;
    }

    /// <summary>The READY sign's stand-in (shown by the binder only when the art office has no NEXT sign): a small lit box with "NEXT" on it. Idempotent.</summary>
    private static GameObject BuildReadyPlaceholder(Transform sign)
    {
        DestroyChildIfPresent(sign, "Placeholder");
        Transform placeholder = EnsureChild(sign, "Placeholder");
        PrimitivePart(placeholder, "Body", PrimitiveType.Cube, new Vector3(0f, 0.1f, 0f), new Vector3(0.5f, 0.2f, 0.12f), LitMaterial("Placeholder_Sign", new Color(0.2f, 0.34f, 0.32f), 0.4f));
        TextMeshPro label = FloatingNote(placeholder, "Label");
        label.transform.localPosition = new Vector3(0f, 0.1f, -0.065f);
        label.transform.localRotation = Quaternion.identity;
        label.text = "NEXT";
        label.color = new Color(0.9f, 0.95f, 0.9f, 1f);
        placeholder.gameObject.SetActive(false);
        return placeholder.gameObject;
    }

    /// <summary>A primitive part of a stand-in prop (no collider: the prop's click box takes the clicks).</summary>
    private static void PrimitivePart(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 size, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        part.name = name;
        Object.DestroyImmediate(part.GetComponent<Collider>());
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        part.transform.localScale = size;
        if (material != null)
            part.GetComponent<MeshRenderer>().sharedMaterial = material;
    }

    /// <summary>
    /// The desk: Office/Desk (the DeskSurface plane and the DeskController)
    /// with its Papers root, the HandOver point, the inactive paper template,
    /// the decoration slots, the Catcher (a click on the desk puts held papers
    /// back; a box on the Interactable layer the binder sizes under the desk
    /// plane, inactive until papers are held) and the Examiner (poses held
    /// papers; the PC frame bounds their region); Office/Scanner (the
    /// DeskScanner, its click box, Clickable and reaction, and a stand-in
    /// flatbed machine the binder shows where the art has no scanner); the
    /// day-1 scan note. Idempotent.
    /// </summary>
    private static DeskController BuildDesk(Transform office, DeskConfigSO config, PcFrame pcFrame, out DeskScanner scanner, out GameObject scannerPlaceholder, out TextMeshPro scanHint)
    {
        Clickable scannerClick = EnsureClickBox(office, "Scanner");
        scanner = GetOrAdd<DeskScanner>(scannerClick.gameObject);
        DestroyChildIfPresent(scannerClick.transform, "Placeholder");
        Transform machine = EnsureChild(scannerClick.transform, "Placeholder");
        PrimitivePart(machine, "Base", PrimitiveType.Cube, new Vector3(0f, 0.025f, 0f), new Vector3(0.4f, 0.05f, 0.32f), LitMaterial("Placeholder_ScannerBody", new Color(0.24f, 0.33f, 0.31f), 0.35f));
        PrimitivePart(machine, "Bed", PrimitiveType.Cube, new Vector3(0f, 0.051f, 0.01f), new Vector3(0.34f, 0.004f, 0.25f), LitMaterial("Placeholder_ScannerGlass", new Color(0.08f, 0.16f, 0.17f), 0.85f));
        PrimitivePart(machine, "Hinge", PrimitiveType.Cube, new Vector3(0f, 0.06f, 0.15f), new Vector3(0.4f, 0.03f, 0.03f), LitMaterial("Placeholder_ScannerTrim", new Color(0.84f, 0.78f, 0.65f), 0.3f));
        PrimitivePart(machine, "Light", PrimitiveType.Cube, new Vector3(0.16f, 0.052f, -0.135f), new Vector3(0.02f, 0.006f, 0.02f), LitMaterial("Placeholder_ScannerLight", new Color(0.35f, 0.95f, 0.45f), 0.6f));
        scannerPlaceholder = machine.gameObject;
        var soScanner = new SerializedObject(scanner);
        soScanner.FindProperty("dropSize").vector2Value = new Vector2(0.4f, 0.32f);
        soScanner.FindProperty("bedCentre").vector3Value = new Vector3(0f, 0.056f, 0.01f);
        soScanner.ApplyModifiedProperties();

        scanHint = FloatingNote(office, "ScanHint");

        Transform deskTransform = EnsureChild(office, "Desk");
        DeskSurface surface = GetOrAdd<DeskSurface>(deskTransform.gameObject);
        Transform papers = EnsureChild(deskTransform, "Papers");
        papers.localPosition = Vector3.zero;
        Transform handOver = EnsureChild(deskTransform, "HandOver");
        handOver.localPosition = Vector3.zero;
        DeskDocument template = BuildPaperTemplate(deskTransform, config);
        BuildDeskSlots(deskTransform);

        DestroyChildIfPresent(deskTransform, "Catcher");
        Transform catcher = EnsureChild(deskTransform, "Catcher");
        catcher.gameObject.layer = OfficeLayers.InteractableLayer;
        catcher.gameObject.AddComponent<BoxCollider>();
        ClickCatcher deskCatcher = catcher.gameObject.AddComponent<ClickCatcher>();
        catcher.gameObject.SetActive(false);

        DestroyChildIfPresent(deskTransform, "Examiner");
        PaperExaminer examiner = EnsureChild(deskTransform, "Examiner").gameObject.AddComponent<PaperExaminer>();
        var soExaminer = new SerializedObject(examiner);
        SetRef(soExaminer, "config", config);
        SetRef(soExaminer, "surface", surface);
        SetRef(soExaminer, "frame", pcFrame);
        soExaminer.ApplyModifiedProperties();

        DeskController desk = GetOrAdd<DeskController>(deskTransform.gameObject);
        var so = new SerializedObject(desk);
        SetRef(so, "surface", surface);
        SetRef(so, "scanner", scanner);
        SetRef(so, "paperTemplate", template);
        SetRef(so, "paperRoot", papers);
        SetRef(so, "handOverPoint", handOver);
        SetRef(so, "scanHint", scanHint);
        SetRef(so, "config", config);
        SetRef(so, "examiner", examiner);
        SetRef(so, "deskCatcher", deskCatcher);
        so.ApplyModifiedProperties();
        return desk;
    }

    /// <summary>
    /// The inactive paper every handed-over document clones: a root (the
    /// DeskDocument, its DeskDraggable and Clickable) and its lying Sheet (the
    /// click box on the Interactable layer, the lit paper quad the hover
    /// outlines, the title, the hidden photo frame with the traveller's photo,
    /// and the Rows root with its inactive RowTemplate: a Label and a Value
    /// text and a Highlight quad), laid out by PaperFace from Desk_Default's
    /// face (the runtime re-lays each paper from the same knobs); the unlit
    /// examine material the paper wears while held. Rebuilt each run.
    /// </summary>
    private static DeskDocument BuildPaperTemplate(Transform desk, DeskConfigSO config)
    {
        DestroyChildIfPresent(desk, "PaperTemplate");
        Transform root = EnsureChild(desk, "PaperTemplate");
        Transform sheet = EnsureChild(root, "Sheet");
        sheet.localRotation = Quaternion.Euler(90f, 0f, 0f);
        sheet.gameObject.layer = OfficeLayers.InteractableLayer;
        Vector2 size = config.paperSize;
        BoxCollider box = sheet.gameObject.AddComponent<BoxCollider>();
        box.size = new Vector3(size.x, size.y, 0.004f);

        Sprite paperSprite = EnsureOfficeSprite("paper", PaperCream, 150, 200);
        Material paperMaterial = EnsureMaterial("Paper", "Universal Render Pipeline/Lit", m =>
        {
            m.SetTexture("_BaseMap", paperSprite.texture);
            m.SetColor("_BaseColor", Color.white);
            m.SetFloat("_Smoothness", 0.12f);
            m.SetFloat("_Metallic", 0f);
        });
        PrimitivePart(sheet, "Paper", PrimitiveType.Quad, Vector3.zero, new Vector3(size.x, size.y, 1f), paperMaterial);
        MeshRenderer paper = sheet.Find("Paper").GetComponent<MeshRenderer>();
        paper.shadowCastingMode = ShadowCastingMode.Off;
        Material examineMaterial = EnsureMaterial("Paper_Examine", "Universal Render Pipeline/Unlit", m =>
        {
            m.SetTexture("_BaseMap", paperSprite.texture);
            m.SetColor("_BaseColor", Color.white);
        });

        // The face (piece 10): where the title, the photo and the rows go (the runtime re-lays each paper from the same knobs).
        float h = size.y;
        FaceLayout face = PaperFace.Layout(1, true, size.x / h, config.face);
        TextMeshPro title = PaperText(sheet, "Title", "Document", new Vector2(face.Title.CentreX * h, face.Title.CentreY * h), new Vector2(face.Title.Width * h, face.Title.Height * h), true);

        float photoHeight = face.Photo.Height * h;
        var photoSize = new Vector2(photoHeight * LookCanvas.PhotoAspect, photoHeight);
        Transform frame = EnsureChild(sheet, "PhotoSlot");
        frame.localPosition = new Vector3(face.Photo.CentreX * h, face.Photo.CentreY * h, -0.0005f);
        PrimitivePart(frame, "Frame", PrimitiveType.Quad, Vector3.zero, new Vector3(photoSize.x, photoSize.y, 1f), LitMaterial("Paper_PhotoFrame", PhotoGrey, 0.1f));
        frame.Find("Frame").GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;

        // The photo: crop sprites one unit tall, scaled to fill the frame's height.
        Transform portrait = EnsureChild(frame, "Photo");
        portrait.localPosition = new Vector3(0f, 0f, -0.0005f);
        portrait.localRotation = Quaternion.identity;
        portrait.localScale = Vector3.one * (photoHeight * PhotoFill);
        LookSpriteStack stack = portrait.gameObject.AddComponent<LookSpriteStack>();
        WireLayers(stack, portrait, 1, true);
        frame.gameObject.SetActive(false);

        // The rows: one inactive template (a label over a value, and a highlight quad behind both) the paper clones per field.
        Transform rows = EnsureChild(sheet, "Rows");
        rows.localPosition = Vector3.zero;
        rows.localRotation = Quaternion.identity;
        Transform rowTemplate = EnsureChild(rows, "RowTemplate");
        FaceRow first = face.Rows[0];
        TextMeshPro label = PaperText(rowTemplate, "Label", "Label", new Vector2(first.Label.CentreX * h, first.Label.CentreY * h), new Vector2(first.Label.Width * h, first.Label.Height * h), false);
        label.alignment = TextAlignmentOptions.BottomLeft;
        label.color = PaperLabelInk;
        label.fontSizeMin = PaperTextMinSize;
        TextMeshPro value = PaperText(rowTemplate, "Value", "Value", new Vector2(first.Value.CentreX * h, first.Value.CentreY * h), new Vector2(first.Value.Width * h, first.Value.Height * h), false);
        value.alignment = TextAlignmentOptions.TopLeft;
        value.textWrappingMode = TextWrappingModes.Normal;
        value.fontSizeMin = PaperTextMinSize;
        Material highlightMaterial = EnsureMaterial("PaperRow_Highlight", "Universal Render Pipeline/Unlit", m =>
        {
            m.SetColor("_BaseColor", new Color(0f, 0f, 0f, 0f));
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_QueueOffset", -10f);
            BaseShaderGUI.SetMaterialKeywords(m);
        });
        PrimitivePart(rowTemplate, "Highlight", PrimitiveType.Quad, new Vector3(first.Hit.CentreX * h, first.Hit.CentreY * h, -0.0003f),
                      new Vector3(first.Hit.Width * h, first.Hit.Height * h, 1f), highlightMaterial);
        rowTemplate.Find("Highlight").GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
        rowTemplate.gameObject.SetActive(false);

        Clickable click = root.gameObject.AddComponent<Clickable>();
        click.SetOutline(new Renderer[] { paper });
        DeskDraggable drag = root.gameObject.AddComponent<DeskDraggable>();
        var soDrag = new SerializedObject(drag);
        SetRef(soDrag, "proxy", box);
        soDrag.ApplyModifiedProperties();

        DeskDocument doc = root.gameObject.AddComponent<DeskDocument>();
        var so = new SerializedObject(doc);
        SetRef(so, "sheet", sheet);
        SetRef(so, "title", title);
        SetRef(so, "photoSlot", frame.gameObject);
        SetRef(so, "photo", stack);
        SetRef(so, "rowTemplate", rowTemplate.gameObject);
        SetRef(so, "paperQuad", paper);
        SetRef(so, "examineMaterial", examineMaterial);
        SetRef(so, "click", click);
        SetRef(so, "drag", drag);
        so.ApplyModifiedProperties();

        var soClick = new SerializedObject(click);
        SerializedArrays.Set(soClick, "outline", new Object[] { paper });
        soClick.ApplyModifiedProperties();

        root.gameObject.SetActive(false);
        return doc;
    }

    /// <summary>A text lying on the paper sheet (in the sheet's plane, just above it), auto-sized into a box of metres.</summary>
    private static TextMeshPro PaperText(Transform sheet, string name, string content, Vector2 centre, Vector2 box, bool bold)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshPro));
        go.transform.SetParent(sheet, false);
        go.transform.localPosition = new Vector3(centre.x, centre.y, -0.0006f);
        TextMeshPro tmp = go.GetComponent<TextMeshPro>();
        ((RectTransform)go.transform).sizeDelta = box;
        tmp.text = content;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMax = 0.5f;
        tmp.fontSizeMin = 0.05f;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Ink;
        if (bold)
            tmp.fontStyle = FontStyles.Bold;
        return tmp;
    }

    /// <summary>A note floating in the office (the binder places it and turns it to the camera): light text on a dark backing, auto-sized, inactive until shown. Idempotent.</summary>
    private static TextMeshPro FloatingNote(Transform parent, string name)
    {
        DestroyChildIfPresent(parent, name);
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshPro));
        go.transform.SetParent(parent, false);
        ((RectTransform)go.transform).sizeDelta = new Vector2(1.1f, 0.09f);
        TextMeshPro tmp = go.GetComponent<TextMeshPro>();
        tmp.text = string.Empty;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMax = 0.6f;
        tmp.fontSizeMin = 0.1f;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = NoteInk;
        tmp.fontStyle = FontStyles.Bold;
        tmp.fontSharedMaterial = NoteMaterial(tmp.font);
        go.SetActive(false);
        return tmp;
    }

    /// <summary>
    /// The floating notes' shared font material: the font's own with a dark
    /// outline (setting a text's outline in edit mode would instance its
    /// material into the scene). Created once; a designer's edits are kept.
    /// </summary>
    private static Material NoteMaterial(TMP_FontAsset font)
    {
        string path = $"{GameplayArtFolder}/Materials/FloatingNote_Outline.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null || font == null)
            return material;

        PlaceholderPng.EnsureFolderTree($"{GameplayArtFolder}/Materials");
        material = new Material(font.material) { name = "FloatingNote_Outline" };
        material.EnableKeyword(ShaderUtilities.Keyword_Outline);
        material.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.25f);
        material.SetColor(ShaderUtilities.ID_OutlineColor, new Color32(20, 22, 26, 255));
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    /// <summary>The named desk spots under the desk plane, each with its DeskSlot id and kind. Idempotent.</summary>
    private static void BuildDeskSlots(Transform desk)
    {
        DestroyChildIfPresent(desk, "DeskSlots");
        Transform root = EnsureChild(desk, "Slots");
        foreach ((string id, DeskSlotKind kind, Vector3 position) in DeskSlots)
        {
            Transform slot = EnsureChild(root, id);
            slot.localPosition = position;
            DeskSlot component = GetOrAdd<DeskSlot>(slot.gameObject);
            var so = new SerializedObject(component);
            so.FindProperty("slotId").stringValue = id;
            so.FindProperty("kind").enumValueIndex = (int)kind;
            so.ApplyModifiedProperties();
        }
    }

    /// <summary>
    /// The traveller: Office/Traveller (TravellerView) with its Figure (a
    /// SortingGroup with one unlit SpriteRenderer per LookLayer, the
    /// LookSpriteStack), the Anchor and the HitZone (a click box). The binder
    /// stands it at the traveller anchor at load. Idempotent.
    /// </summary>
    private static TravellerView BuildTraveller(Transform office, out Clickable hitZone)
    {
        Transform traveller = EnsureChild(office, "Traveller");
        DestroyChildIfPresent(traveller, "Figure");
        Transform figure = EnsureChild(traveller, "Figure");
        SortingGroup group = figure.gameObject.AddComponent<SortingGroup>();
        group.sortingOrder = 0;
        LookSpriteStack stack = figure.gameObject.AddComponent<LookSpriteStack>();
        WireLayers(stack, figure, 0, false);

        Transform anchor = EnsureChild(traveller, "Anchor");
        hitZone = EnsureClickBox(traveller, "HitZone");

        TravellerView view = GetOrAdd<TravellerView>(traveller.gameObject);
        var so = new SerializedObject(view);
        SetRef(so, "figure", stack);
        SetRef(so, "anchor", anchor);
        SetRef(so, "hitZone", hitZone.GetComponent<BoxCollider>());
        so.ApplyModifiedProperties();
        return view;
    }

    /// <summary>
    /// One empty unlit SpriteRenderer child per LookLayer under <paramref name="parent"/>
    /// (named after the layer, at its origin, ordered <paramref name="firstOrder"/>
    /// + the layer), wired to the stack in layer order; <paramref name="photo"/>
    /// makes it show photo crops.
    /// </summary>
    private static void WireLayers(LookSpriteStack stack, Transform parent, int firstOrder, bool photo)
    {
        Material unlit = AssetDatabase.LoadAssetAtPath<Material>(UnlitSpriteMaterialPath);
        var layers = new List<Object>();
        foreach (LookLayer layer in System.Enum.GetValues(typeof(LookLayer)))
        {
            SpriteRenderer sr = EnsureSprite(parent, layer.ToString(), null, Vector3.zero, firstOrder + (int)layer);
            sr.enabled = false;
            if (unlit != null)
                sr.sharedMaterial = unlit;
            layers.Add(sr);
        }

        var so = new SerializedObject(stack);
        SerializedArrays.Set(so, "layers", layers);
        so.FindProperty("photo").boolValue = photo;
        so.ApplyModifiedProperties();
    }

    /// <summary>Adds or rewires a clickable's DeskReaction (its readout is set by the binder at load; the audio source is optional). Returns it.</summary>
    private static DeskReaction WireReaction(Clickable click, DeskReactionSO reaction, OverlayCallout tooltip, AudioSource audioSource)
    {
        DeskReaction component = GetOrAdd<DeskReaction>(click.gameObject);
        var so = new SerializedObject(component);
        SetRef(so, "reaction", reaction);
        SetRef(so, "tooltip", tooltip);
        SetRef(so, "audioSource", audioSource);
        so.ApplyModifiedProperties();
        return component;
    }

    /// <summary>Gives a decor prop's click box its DeskItem id.</summary>
    private static void SetDeskItem(Transform prop, string id)
    {
        DeskItem item = GetOrAdd<DeskItem>(prop.gameObject);
        var so = new SerializedObject(item);
        so.FindProperty("itemId").stringValue = id;
        so.ApplyModifiedProperties();
    }

    /// <summary>Empties a UnityEvent's persistent calls on <paramref name="host"/> (a call saved in the scene would otherwise survive the build).</summary>
    private static void ClearPersistentCalls(Object host, string eventProp)
    {
        var so = new SerializedObject(host);
        if (ClearPersistentCalls(so, eventProp) != null)
            so.ApplyModifiedProperties();
    }

    /// <summary>
    /// Empties a UnityEvent's persistent calls in <paramref name="so"/> and
    /// returns them (null when the object has no such event); the caller
    /// applies <paramref name="so"/>. WirePersistentVoid adds its call to them.
    /// </summary>
    private static SerializedProperty ClearPersistentCalls(SerializedObject so, string eventProp)
    {
        SerializedProperty calls = so.FindProperty(eventProp + ".m_PersistentCalls.m_Calls");
        if (calls != null)
            calls.ClearArray();
        return calls;
    }

    // -----------------------------
    // Overlay: callouts, wheel, fallback HUD
    // -----------------------------

    /// <summary>The fallback HUD's parts (shown by the binder only for readouts the art office lacks).</summary>
    private struct FallbackHud
    {
        public GameObject root;
        public TMP_Text day;
        public TMP_Text stability;
        public TMP_Text credits;
        public TMP_Text clock;
    }

    /// <summary>The fallback HUD on the office overlay canvas, top right, rebuilt each run and inactive: day, credits, stability and clock.</summary>
    private static FallbackHud BuildFallbackHud(Transform overlay)
    {
        DestroyChildIfPresent(overlay, "FallbackHud");
        Transform panel = Panel(overlay, "FallbackHud", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-230f, -40f), new Vector2(420f, 56f), new Color(0.08f, 0.1f, 0.12f, 0.85f),
                                ThemeRoleId.ScreenStrip);
        AddHLayout(panel, 6f);
        TMP_Text Cell(string name, string text)
        {
            TMP_Text t = Text(panel, name, text, 22, TextAlignmentOptions.Center, Vector2.zero, Vector2.one, new Color(0.9f, 0.92f, 0.88f, 1f), ThemeRoleId.ScreenStrip);
            t.raycastTarget = false;
            return t;
        }

        var hud = new FallbackHud
        {
            root = panel.gameObject,
            day = Cell("Day", "01"),
            credits = Cell("Credits", "0"),
            stability = Cell("Stability", "100%"),
            clock = Cell("Clock", "09:00"),
        };
        panel.gameObject.SetActive(false);
        return hud;
    }

    /// <summary>The office case HUD's strips (reference px from the top centre): the claim tag, the office compare strip under it.</summary>
    private static readonly Vector2 ClaimStripSize = new Vector2(1100f, 64f);
    private const float ClaimStripTop = 16f;
    private static readonly Vector2 CompareStripSize = new Vector2(1200f, 56f);
    private const float CompareStripTop = 88f;

    /// <summary>The stamp tray's panel (reference px).</summary>
    private static readonly Vector2 StampTraySize = new Vector2(420f, 96f);

    /// <summary>A strip at the top centre of the overlay, <paramref name="top"/> px down, with its text (auto-sized, no raycasts).</summary>
    private static TMP_Text TopStrip(Transform parent, string name, Vector2 size, float top, Color background, ThemeRoleId role, int fontSize, Color ink, out Transform strip)
    {
        strip = Panel(parent, name, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -top - size.y / 2f), size, background, role);
        ((RectTransform)strip).pivot = Center;
        strip.GetComponent<Image>().raycastTarget = false;
        TMP_Text text = Text(strip, name + "Text", "", fontSize, TextAlignmentOptions.Center, new Vector2(0.02f, 0.04f), new Vector2(0.98f, 0.96f), ink, role);
        text.enableAutoSizing = true;
        text.fontSizeMin = 11f;
        text.fontSizeMax = fontSize;
        text.raycastTarget = false;
        return text;
    }

    /// <summary>
    /// The office case HUD (piece 10) under the office overlay canvas, rebuilt
    /// each run: an always-active full-screen host (OfficeCaseHud, no graphic)
    /// and its Root, top centre: the claim tag (ClaimStrip, the claim banner's
    /// text) and under it the office compare strip (CompareBar, inactive; the
    /// CompareController draws it). No part takes raycasts. Returns the HUD and
    /// the strip's object and text through out parameters.
    /// </summary>
    private static OfficeCaseHud BuildOfficeCaseHud(Transform overlay, out GameObject compareStrip, out TMP_Text compareText)
    {
        DestroyChildIfPresent(overlay, "OfficeCaseHud");
        Transform host = Panel(overlay, "OfficeCaseHud", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        Transform root = Panel(host, "Root", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        TMP_Text claimText = TopStrip(root, "ClaimStrip", ClaimStripSize, ClaimStripTop, ScreenStripColor, ThemeRoleId.ClaimStrip, 26, Color.white, out Transform claim);
        compareText = TopStrip(root, "CompareStrip", CompareStripSize, CompareStripTop, Tooltip, ThemeRoleId.CompareBar, 22, Ink, out Transform compare);
        compareStrip = compare.gameObject;
        compareStrip.SetActive(false);

        OfficeCaseHud hud = host.gameObject.AddComponent<OfficeCaseHud>();
        var so = new SerializedObject(hud);
        SetRef(so, "root", root.gameObject);
        SetRef(so, "claimRoot", claim.gameObject);
        SetRef(so, "claimText", claimText);
        so.ApplyModifiedProperties();
        root.gameObject.SetActive(false);
        return hud;
    }

    /// <summary>
    /// The stamp tray (piece 10) under the office overlay canvas, rebuilt each
    /// run, the wheel's host pattern: an always-active full-screen host
    /// (StampTray, no graphic); its Catcher, a full-screen transparent
    /// click-to-close area, inactive; the Panel under it (anchors and pivot
    /// (0.5, 0.5), placed over the stamp by projection) with Accept (left,
    /// tick) and Deny (right, cross) in piece 6's decision roles and labels.
    /// </summary>
    private static StampTray BuildStampTray(Transform overlay, DeskConfigSO config)
    {
        DestroyChildIfPresent(overlay, "StampTray");
        Transform host = Panel(overlay, "StampTray", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        Transform catcher = Panel(host, "Catcher", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0f), ThemeRoleId.ClickCatcher);
        Transform panel = Panel(catcher, "Panel", Center, Center, Vector2.zero, StampTraySize, PanelNavy, ThemeRoleId.Panel);
        ((RectTransform)panel).pivot = Center;

        Button accept = MakeButton(panel, "AcceptButton", null, new Vector2(0.03f, 0.12f), new Vector2(0.485f, 0.88f), new Color(0.2f, 0.5f, 0.24f, 1f),
                                   ThemeRoleId.AcceptButton, "accept");
        Button deny = MakeButton(panel, "DenyButton", null, new Vector2(0.515f, 0.12f), new Vector2(0.97f, 0.88f), new Color(0.72f, 0.2f, 0.18f, 1f),
                                 ThemeRoleId.DenyButton, "deny");
        BuildDecisionGlyph(accept, ThemeRoleId.AcceptButton, true);
        BuildDecisionGlyph(deny, ThemeRoleId.DenyButton, false);

        StampTray tray = host.gameObject.AddComponent<StampTray>();
        var so = new SerializedObject(tray);
        SetRef(so, "catcher", catcher.gameObject);
        SetRef(so, "panel", panel);
        SetRef(so, "acceptButton", accept);
        SetRef(so, "denyButton", deny);
        SetRef(so, "config", config);
        so.ApplyModifiedProperties();

        catcher.gameObject.SetActive(false);
        return tray;
    }

    /// <summary>
    /// The speech bubble takes input (piece 10): its panel image catches
    /// raycasts (hovering holds the line), carries a transition-free Button
    /// (interactable only while an answer shows, set by the wheel) and a
    /// SpeechBubbleInput, and the bubble moves above the wheel (after it on
    /// the overlay) so an answer can be picked while the wheel is open.
    /// </summary>
    private static void BuildBubbleInput(OverlayCallout bubble, TravellerWheel wheel)
    {
        Transform panel = bubble.transform.Find("Panel");
        Image image = panel.GetComponent<Image>();
        image.raycastTarget = true;
        Button button = panel.gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.targetGraphic = image;
        button.interactable = false;
        SpeechBubbleInput input = panel.gameObject.AddComponent<SpeechBubbleInput>();
        var so = new SerializedObject(input);
        SetRef(so, "wheel", wheel);
        SetRef(so, "button", button);
        so.ApplyModifiedProperties();

        var soWheel = new SerializedObject(wheel);
        SetRef(soWheel, "bubbleButton", button);
        soWheel.ApplyModifiedProperties();
        bubble.transform.SetSiblingIndex(wheel.transform.GetSiblingIndex() + 1);
    }

    /// <summary>
    /// An overlay callout (a timed label that takes no clicks) under the office
    /// overlay canvas, rebuilt each run: an always-active full-screen host with
    /// no graphic, and its Panel child (anchors and pivot (0.5, 0.5), raycast
    /// targets off, inactive) holding an auto-sized label.
    /// </summary>
    private static OverlayCallout BuildOverlayCallout(Transform overlay, string name, Vector2 size, Color background, ThemeRoleId role)
    {
        DestroyChildIfPresent(overlay, name);
        Transform host = Panel(overlay, name, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        Transform panel = Panel(host, "Panel", Center, Center, Vector2.zero, size, background, role);
        ((RectTransform)panel).pivot = Center;
        panel.GetComponent<Image>().raycastTarget = false;

        TMP_Text label = Text(panel, "Label", "", 24, TextAlignmentOptions.Center, new Vector2(0.04f, 0.08f), new Vector2(0.96f, 0.92f), Ink, role);
        label.enableAutoSizing = true;
        label.fontSizeMin = 14f;
        label.fontSizeMax = 24f;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.raycastTarget = false;

        OverlayCallout callout = host.gameObject.AddComponent<OverlayCallout>();
        var so = new SerializedObject(callout);
        SetRef(so, "panel", panel);
        SetRef(so, "label", label);
        so.ApplyModifiedProperties();

        panel.gameObject.SetActive(false);
        return callout;
    }

    /// <summary>
    /// The traveller wheel under the office overlay canvas, rebuilt each run: an
    /// always-active full-screen host (TravellerWheel, no graphic); its Catcher,
    /// a full-screen transparent click-to-close area, inactive; the Ring under it
    /// (anchors and pivot (0.5, 0.5), placed by projection) with the choice
    /// renderer (InteractionPanelController, its template stretched so a centre
    /// clone fills the centre slot), the RadialLayoutGroup and the Centre slot
    /// ("&lt; Back", ignored by the layout). The wheel's traveller is set by
    /// BuildOffice, once the traveller view exists.
    /// </summary>
    private static TravellerWheel BuildTravellerWheel(Transform overlay, DeskConfigSO config, OverlayCallout bubble)
    {
        DestroyChildIfPresent(overlay, "TravellerWheel");
        Transform host = Panel(overlay, "TravellerWheel", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        Transform catcher = Panel(host, "Catcher", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0f), ThemeRoleId.ClickCatcher);

        Transform ring = Panel(catcher, "Ring", Center, Center, Vector2.zero, Vector2.zero, null);
        ((RectTransform)ring).pivot = Center;
        RadialLayoutGroup layout = ring.gameObject.AddComponent<RadialLayoutGroup>();
        layout.Radii = config.wheelRadii;
        layout.ItemSize = config.wheelItemSize;

        Transform centre = Panel(ring, "Centre", Center, Center, Vector2.zero, config.wheelCentreSize, null);
        ((RectTransform)centre).pivot = Center;
        centre.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

        Button template = MakeButton(ring, "ActionButtonTemplate", "Choice", Vector2.zero, Vector2.one, new Color(0.16f, 0.28f, 0.42f, 0.95f), ThemeRoleId.WheelButton);
        TMP_Text choice = template.transform.Find("Label").GetComponent<TMP_Text>();
        choice.enableAutoSizing = true;
        choice.fontSizeMin = 12f;
        choice.fontSizeMax = 20f;
        choice.textWrappingMode = TextWrappingModes.Normal;
        template.gameObject.SetActive(false);

        InteractionPanelController panel = ring.gameObject.AddComponent<InteractionPanelController>();
        var soPanel = new SerializedObject(panel);
        SetRef(soPanel, "actionsRoot", ring);
        SetRef(soPanel, "actionButtonTemplate", template);
        SetRef(soPanel, "centreSlot", centre);
        soPanel.ApplyModifiedProperties();

        TravellerWheel wheel = host.gameObject.AddComponent<TravellerWheel>();
        var so = new SerializedObject(wheel);
        SetRef(so, "catcher", catcher.gameObject);
        SetRef(so, "ring", ring);
        SetRef(so, "layout", layout);
        SetRef(so, "centreSlot", centre);
        SetRef(so, "bubble", bubble);
        SetRef(so, "config", config);
        so.ApplyModifiedProperties();

        catcher.gameObject.SetActive(false);
        return wheel;
    }
}
