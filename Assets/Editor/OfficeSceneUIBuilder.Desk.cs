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
/// The office builder's gameplay-layer parts (the physical desk, piece 7, moved
/// onto the art office): the desktop's own place (the World Space desktop on
/// its layer, the frame and clone cameras, the screen), the PC frame on the
/// overlay canvas, the traveller wheel and the overlay callouts, and the
/// Office root: the click boxes the office binder puts on the art's props at
/// load, the desk (its plane, the paper template with its face, the desk
/// counter, the mat's click, the scanner and its
/// stand-in machine, the day-1 notes), the desk view's camera, the traveller,
/// the AVAILABLE sign, the readouts, the decoration slots, the booth coordinator
/// and the binder; on the overlay the office case HUD, the stamp bar, the
/// inspect button and the PC's tab (Papers, Please's controls). Nothing here
/// knows where the art puts things: the binder reads the scene contract at
/// load. Part of <see cref="OfficeSceneUIBuilder"/>; Build() calls these in
/// its order.
/// </summary>
public static partial class OfficeSceneUIBuilder
{
    /// <summary>The desk tuning asset, created by the builder when missing (a designer's edits are kept).</summary>
    internal const string DeskConfigPath = "Assets/Data/Config/Desk_Default.asset";

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

    /// <summary>The paper's photo frame (shown only on a photo document).</summary>
    private static readonly Color PhotoGrey = new Color(0.55f, 0.56f, 0.58f, 1f);

    /// <summary>The photo's height as a share of its frame's.</summary>
    private const float PhotoFill = 0.92f;

    /// <summary>The desk notes' ink.</summary>
    private static readonly Color NoteInk = new Color(0.96f, 0.95f, 0.88f, 1f);

    /// <summary>The day-1 scan note's box (metres): two lines of the note at the size one line of the old, shorter note had.</summary>
    private static readonly Vector2 ScanHintBox = new Vector2(0.78f, 0.13f);

    /// <summary>The day-1 wheel note's box (metres): two lines, twice the size of the old one-line note (it floats above the traveller, far from the camera).</summary>
    private static readonly Vector2 WheelHintBox = new Vector2(1.2f, 0.23f);

    /// <summary>The wheel note's largest size (TMP world units): room for its two lines at twice the old note's size.</summary>
    private const float WheelHintMaxSize = 1f;

    /// <summary>The day-1 notes' backing (the readability fix): a dark, mostly opaque plate behind the light text, so it reads over the pale morning crowds and the evening palette.</summary>
    private static readonly Color NoteBackingColour = new Color(0.07f, 0.08f, 0.1f, 0.85f);

    /// <summary>The margin of a note's backing around its text (metres).</summary>
    private static readonly Vector2 NoteBackingMargin = new Vector2(0.04f, 0.02f);

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
    private static DeskConfigSO EnsureDeskConfig() => EnsureConfigAsset<DeskConfigSO>(DeskConfigPath);

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

    /// <summary>Makes sure the project has a user layer named <paramref name="name"/> (the first free one from 8; also Add Anime Hall Hooks' HallBackdrop).</summary>
    internal static void EnsureLayer(string name)
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

    /// <summary>
    /// Makes sure the project has a sorting layer named <paramref name="name"/>,
    /// listed after every existing one (so it draws over Default), or before
    /// every one with <paramref name="first"/> (Add Anime Hall Hooks' HallSky,
    /// drawn under the hall's painted layers). Its id is a stable hash of the
    /// name, as the tag manager wants a unique non-zero one.
    /// </summary>
    internal static void EnsureSortingLayer(string name, bool first = false)
    {
        if (SortingLayer.layers.Any(l => l.name == name))
            return;

        int id = 17;
        foreach (char c in name)
            id = unchecked(id * 31 + c);
        if (id == 0)
            id = 1;

        var tags = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layers = tags.FindProperty("m_SortingLayers");
        int index = first ? 0 : layers.arraySize;
        layers.InsertArrayElementAtIndex(index);
        SerializedProperty layer = layers.GetArrayElementAtIndex(index);
        layer.FindPropertyRelative("name").stringValue = name;
        layer.FindPropertyRelative("uniqueID").intValue = id;
        layer.FindPropertyRelative("locked").boolValue = false;
        tags.ApplyModifiedProperties();
        if (!SortingLayer.layers.Any(l => l.name == name))
            Debug.LogError($"[TimeDesk] The sorting layer '{name}' could not be added to ProjectSettings/TagManager.asset.");
    }

    /// <summary>The gameplay sorting layer's id (Default's, with an error, when the project lacks it).</summary>
    private static int GameplaySortingLayerId()
    {
        int id = SortingLayer.NameToID(OfficeLayers.SortingLayer);
        if (id == 0)
            Debug.LogError($"[TimeDesk] No sorting layer '{OfficeLayers.SortingLayer}': the traveller and the desk notes stay on Default, behind the art's sprites.");
        return id;
    }

    /// <summary>
    /// Keeps the build list in boot order (BuildScenes.Order, audit R3-001): the
    /// title first (a player build boots it), then the art office the knob names
    /// (RunConfig.officeSceneName), the gameplay layer (whose load the art office
    /// brings) and Home, each enabled; every other listed scene stays after them,
    /// disabled (the other art office, the legacy Test_DayLoop). Written only when
    /// it changes.
    /// </summary>
    private static void EnsureBuildSettings()
    {
        EditorBuildSettingsScene[] list = BuildScenes
            .Order(EditorBuildSettings.scenes.Select(s => new BuildScene(s.path, s.enabled)), TitleScenePath, ArtScenePath, GameplayScenePath, HomeScenePath)
            .Select(s => new EditorBuildSettingsScene(s.Path, s.Enabled))
            .ToArray();
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

    /// <summary>An orthographic camera looking at the desktop that draws only its layer (no post-processing, no shadows); its built <paramref name="depth"/> is a placeholder the binder reorders around the art's camera at load (PcFrame.DrawAfter, PcScreenClone.Bind). Idempotent.</summary>
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
    /// The PC's device ink (the brand plate): the neutral theme's DiegeticDevice
    /// ink, #5E5446 in world_source.json, the one source of that colour (audit
    /// R6-005: the builder drew its own #6B614D beside it, and the theme never
    /// recolours a diegetic role, so the drawn colour and the checked one
    /// differed). A missing entry is an error; the text then draws black.
    /// </summary>
    private static Color DeviceInk(ContentLibrarySO library)
    {
        PaletteEntry device = library != null && library.NeutralTheme != null ? library.NeutralTheme.Get(ThemeRoleId.DiegeticDevice) : null;
        if (device != null && device.hasInk)
            return device.ink;

        Debug.LogError("[TimeDesk] The neutral theme has no DiegeticDevice ink for the PC's brand plate. Run Tools > TimeDesk > Generate World, then build again.");
        return Color.black;
    }

    /// <summary>
    /// The PC frame on the office overlay canvas, rebuilt each run: an
    /// always-active host with the PcFrame; its Root (inactive until opened)
    /// holds the full-screen exit catcher (a click outside the frame closes
    /// it and goes on to what it lands on: the traveller, the intercom, the
    /// desk), the bezel (placeholder art; clicks on it do nothing), the Glass the
    /// frame camera draws into (4:3; the catcher and the bezel let clicks
    /// through there), the red close X, the power button and LED, and the
    /// brand plate, in the neutral theme's DiegeticDevice ink. Returns the power LED and button through out parameters.
    /// </summary>
    private static PcFrame BuildPcFrame(Transform overlay, Camera frameCamera, OfficeViewController view, ContentLibrarySO library, out Image powerLed, out Button powerButton)
    {
        DestroyChildIfPresent(overlay, "PcFrame");
        Transform host = Panel(overlay, "PcFrame", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        Transform root = Panel(host, "Root", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);

        Transform catcher = Panel(root, "ExitCatcher", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0f), ThemeRoleId.ClickCatcher);
        ClickCatcher exit = catcher.gameObject.AddComponent<ClickCatcher>();
        WirePersistentVoid(exit, "onClick", view, nameof(OfficeViewController.FocusOffice));
        var soExit = new SerializedObject(exit);
        soExit.FindProperty("passThrough").boolValue = true;
        soExit.ApplyModifiedProperties();

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

        Button close = MakeButton(frame, "CloseButton", "", new Vector2(1f, 1f), new Vector2(1f, 1f), Color.white, ThemeRoleId.DiegeticDevice);
        var closeRect = (RectTransform)close.transform;
        closeRect.pivot = new Vector2(1f, 1f);
        closeRect.sizeDelta = new Vector2(64f, 64f);
        closeRect.anchoredPosition = new Vector2(-14f, -14f);
        close.GetComponent<Image>().sprite = EnsureOfficeShape("pc_close", 48, 48, Center, CloseButtonPixel);
        Object.DestroyImmediate(close.transform.Find("Label").gameObject); // its cross is drawn in the sprite: no text
        WirePersistentVoid(close, "m_OnClick", view, nameof(OfficeViewController.FocusOffice));

        powerButton = MakeButton(frame, "PowerButton", "", new Vector2(1f, 0f), new Vector2(1f, 0f), Color.white, ThemeRoleId.DiegeticDevice);
        var powerRect = (RectTransform)powerButton.transform;
        powerRect.pivot = new Vector2(1f, 0f);
        powerRect.sizeDelta = new Vector2(64f, 64f);
        powerRect.anchoredPosition = new Vector2(-70f, 46f);
        powerButton.GetComponent<Image>().sprite = EnsureOfficeShape("crt_power", 28, 28, Center, PowerButtonPixel);
        Object.DestroyImmediate(powerButton.transform.Find("Label").gameObject); // its symbol is drawn in the sprite: no text

        powerLed = Panel(frame, "PowerLed", new Vector2(1f, 0f), new Vector2(1f, 0f), Vector2.zero, new Vector2(18f, 18f), Color.white, ThemeRoleId.DiegeticDevice).GetComponent<Image>();
        ((RectTransform)powerLed.transform).anchoredPosition = new Vector2(-170f, 78f);
        powerLed.sprite = EnsureOfficeShape("crt_led", 8, 8, Center, LedPixel);
        powerLed.raycastTarget = false;

        TMP_Text brand = Text(frame, "Brand", "CHRONODESK 2150", 34, TextAlignmentOptions.Center, new Vector2(0.3f, 0f), new Vector2(0.7f, 0f), DeviceInk(library),
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
    /// load: the PC's and its power knob's click boxes, the AVAILABLE sign (with a
    /// stand-in sign), the desk (its plane, the papers' root, the hand-over
    /// point, the paper template, the decoration slots), the scanner (with a
    /// stand-in machine) and the two day-1 notes, the traveller, the props'
    /// click boxes and reactions, the readouts, the booth coordinator and the
    /// binder, wired to each other; then the desk's checks. Idempotent.
    /// </summary>
    private static BoothCoordinator BuildOffice(OfficeViewController view, MonitorScreen screen, Button framePower, DeskConfigSO config,
                                                OfficeSceneContractSO contract, TravellerWheel wheel, OverlayCallout[] callouts,
                                                OverlayCallout tooltip, OverlayCallout boardTooltip, GameManager game, TMP_Text trayClockText, ShiftClockDriver clock,
                                                ContentLibrarySO library, FallbackHud hud, PcFrame pcFrame, DeskStampTray stampTray,
                                                OfficeCaseHud caseHud, Button deskViewBack, out Clickable readySign)
    {
        Transform office = view.transform;

        // The PC opens the frame; its knob turns the screen on and off.
        Clickable pc = EnsureClickBox(office, "PC");
        WirePersistentVoid(pc, "onClick", view, nameof(OfficeViewController.FocusMonitor));
        Clickable pcPower = EnsureClickBox(office, "PCPower");
        WirePersistentVoid(pcPower, "onClick", screen, nameof(MonitorScreen.TogglePower));
        WirePersistentVoid(framePower, "m_OnClick", screen, nameof(MonitorScreen.TogglePower));

        // The AVAILABLE sign only toggles GameManager's desk (its Clickable is GameManager.readySign).
        readySign = EnsureClickBox(office, "ReadySign");
        ClearPersistentCalls(readySign, "onClick");
        GameObject readyPlaceholder = BuildReadyPlaceholder(readySign.transform);

        // The desk, the scanner and the notes.
        DeskController desk = BuildDesk(office, config, out DeskScanner scanner, out GameObject scannerPlaceholder, out TextMeshPro scanHint);
        DeskView deskView = BuildDeskView(office, config, desk.transform.Find("ViewCatcher").GetComponent<ClickCatcher>(), deskViewBack);
        DeskCounter counter = BuildCounter(desk.transform, config, desk.GetComponent<DeskSurface>(), deskView);
        CityView cityView = BuildCityView(office, deskViewBack.transform.parent, config);
        var soView = new SerializedObject(view);
        SetRef(soView, "deskView", deskView);
        soView.ApplyModifiedProperties();
        var soScanner = new SerializedObject(scanner);
        SetRef(soScanner, "reaction", WireReaction(scanner.GetComponent<Clickable>(), EnsureDeskReaction("Reaction_Scanner", ReactionKind.Pulse, ""), tooltip, null));
        soScanner.ApplyModifiedProperties();

        // The documents land on the counter and come to the reading view by themselves (Papers, Please's zones).
        var soDesk = new SerializedObject(desk);
        SetRef(soDesk, "counter", counter);
        SetRef(soDesk, "deskView", deskView);
        SetRef(soDesk, "stamps", stampTray);
        soDesk.ApplyModifiedProperties();

        // The traveller and the wheel's openers (the traveller and the desk intercom).
        TravellerView traveller = BuildTraveller(office, out Clickable travellerZone);
        // Inspection at the desk (Papers, Please's inspect mode): a click on the traveller picks their face in inspect mode, else opens the wheel.
        DestroyChildIfPresent(office, "DeskInspect");
        DeskInspect inspect = EnsureChild(office, "DeskInspect").gameObject.AddComponent<DeskInspect>();
        BuildInspectButton(deskViewBack.transform.parent, inspect);
        DeskRulebook rulebook = BuildRulebook(office, desk.GetComponent<DeskSurface>(), counter);
        var soDeskBook = new SerializedObject(desk);
        SetRef(soDeskBook, "rulebook", rulebook);
        soDeskBook.ApplyModifiedProperties();
        WirePersistentVoid(travellerZone, "onClick", inspect, nameof(DeskInspect.TravellerClicked));
        var soWheel = new SerializedObject(wheel);
        SetRef(soWheel, "traveller", traveller);
        soWheel.ApplyModifiedProperties();
        TextMeshPro wheelHint = FloatingNote(office, "WheelHint", true);
        ((RectTransform)wheelHint.transform).sizeDelta = WheelHintBox;
        wheelHint.textWrappingMode = TextWrappingModes.Normal;
        wheelHint.fontSizeMax = WheelHintMaxSize;

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
        Prop("Till", OfficeAnchorId.Till, EnsureDeskReaction("Reaction_Till", ReactionKind.Pulse, "tooltip.credits", 0.08f), OfficeAnchorId.ReadoutCredits, null);
        Prop("StabilityMonitor", OfficeAnchorId.StabilityMonitor, EnsureDeskReaction("Reaction_Stability", ReactionKind.None, "tooltip.stability"), null, null);
        Prop("Calendar", OfficeAnchorId.Calendar, EnsureDeskReaction("Reaction_Calendar", ReactionKind.None, "tooltip.day"), OfficeAnchorId.ReadoutDay, null);
        Prop("Clock", OfficeAnchorId.Clock, EnsureDeskReaction("Reaction_Clock", ReactionKind.None, "tooltip.value"), OfficeAnchorId.ReadoutClock, null);
        Prop("Calculator", OfficeAnchorId.Calculator, EnsureDeskReaction("Reaction_Calculator", ReactionKind.Squash, ""), null, "calculator");
        Prop("PenPot", OfficeAnchorId.PenPot, EnsureDeskReaction("Reaction_PenPot", ReactionKind.Wobble, ""), null, "pen_pot");
        Prop("Stapler", OfficeAnchorId.Stapler, EnsureDeskReaction("Reaction_Stapler", ReactionKind.Squash, ""), null, "stapler");
        WirePersistentVoid(propsRoot.Find("Intercom").GetComponent<Clickable>(), "onClick", wheel, nameof(TravellerWheel.Open));
        WirePersistentVoid(propsRoot.Find("Calendar").GetComponent<Clickable>(), "onClick", inspect, nameof(DeskInspect.CalendarClicked));
        clicks.AddRange(rulebook.Clicks);
        var soInspect = new SerializedObject(inspect);
        SetRef(soInspect, "desk", desk);
        SetRef(soInspect, "wheel", wheel);
        SetRef(soInspect, "traveller", traveller);
        SetRef(soInspect, "calendar", propsRoot.Find("Calendar"));
        SetRef(soInspect, "rulebook", rulebook);
        SetRef(soInspect, "sealRegister", library != null ? library.ReferenceBooks.FirstOrDefault(b => b != null && b.category == ClueCategory.Seal) : null);
        SetRef(soInspect, "view", view);
        SetRef(soInspect, "city", cityView);
        soInspect.ApplyModifiedProperties();
        WirePersistentVoid(propsRoot.Find("Stamp").GetComponent<Clickable>(), "onClick", stampTray, nameof(DeskStampTray.ToggleBar));
        var soStamps = new SerializedObject(stampTray);
        SetRef(soStamps, "deskView", deskView);
        SetRef(soStamps, "surface", desk.GetComponent<DeskSurface>());
        soStamps.ApplyModifiedProperties();

        // The hall's Departure Board and portal rings (the portals spec v3; OfficeSceneUIBuilder.Portals.cs).
        DepartureBoardView board = BuildDepartureBoard(office, config, boardTooltip, game);
        clicks.Add(board.transform.Find("ClickBox").GetComponent<Clickable>());
        PortalEffect[] portalEffects = BuildPortalEffects(office, config);
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
        SetRef(so, "stampTray", stampTray);
        SetRef(so, "inspect", inspect);
        SetRef(so, "rulebook", rulebook);
        SetRef(so, "hud", caseHud);
        SetRef(so, "deskView", deskView);
        SetRef(so, "cityView", cityView);
        so.ApplyModifiedProperties();

        // The one input model: the office's keys, the right-click back-out and the PC's grey tab (Papers, Please's controls).
        OfficeControls controls = BuildControls(deskViewBack.transform.parent, view, coordinator, wheel, inspect, stampTray, deskView, cityView, desk);
        var soControls = new SerializedObject(coordinator);
        SetRef(soControls, "controls", controls);
        soControls.ApplyModifiedProperties();

        // The binder puts all of it on the art office at load.
        OfficeSceneBinder binder = GetOrAdd<OfficeSceneBinder>(office.gameObject);
        var soBinder = new SerializedObject(binder);
        SetRef(soBinder, "contract", contract);
        SetRef(soBinder, "config", config);
        SetRef(soBinder, "wheel", wheel);
        SerializedArrays.Set(soBinder, "callouts", callouts);
        SetRef(soBinder, "stampTray", stampTray);
        SetRef(soBinder, "gateLever", BuildGateLever(office, stampTray));
        SetRef(soBinder, "counter", counter);
        SetRef(soBinder, "deskInspect", inspect);
        SetRef(soBinder, "rulebook", rulebook);
        SetRef(soBinder, "matCatcher", desk.transform.Find("ViewCatcher").GetComponent<BoxCollider>());
        SetRef(soBinder, "deskView", deskView);
        SetRef(soBinder, "cityView", cityView);
        SetRef(soBinder, "screenClone", screen.GetComponent<PcScreenClone>());
        SetRef(soBinder, "frame", pcFrame);
        SetRef(soBinder, "pc", pc);
        SetRef(soBinder, "pcPower", pcPower);
        SetRef(soBinder, "desk", desk);
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
        SetRef(soBinder, "hudRiver", hud.river);
        SetRef(soBinder, "deskRiver", BuildDeskRiver(office));
        SetRef(soBinder, "hudCredits", hud.credits);
        SetRef(soBinder, "hudClock", hud.clock);
        SetRef(soBinder, "game", game);
        SetRef(soBinder, "board", board);
        SerializedArrays.Set(soBinder, "portalEffects", portalEffects);
        soBinder.ApplyModifiedProperties();

        // The desk's guide: the FTUE's prompt and arrow, the director and the tutorial's replay buttons (OfficeSceneUIBuilder.Guide.cs).
        BuildGuide(deskViewBack.transform.parent, view, deskView, desk, inspect, stampTray, rulebook, controls, readySign, propsRoot.Find("Calendar"), board, scanner, counter,
                   travellerZone, binder, game);

        // Checks (the validator runs the same, audit R6-021): every paper a traveller carries has a counter spot, every document's rows fit its paper's face, the wheel shows the menu capacity.
        foreach (string problem in ContentLibraryValidator.DeskFitProblems(library, config))
            Debug.LogError($"[TimeDesk] {problem}");
        CheckFormStyle(library, config, EnsureFormStyle());

        return coordinator;
    }

    /// <summary>The AVAILABLE sign's stand-in (shown by the binder only when the art office has no NEXT sign): a small lit box with "AVAILABLE" on it (the binder's AvailableSignLink writes the caption and dims it while paused). Idempotent.</summary>
    private static GameObject BuildReadyPlaceholder(Transform sign)
    {
        DestroyChildIfPresent(sign, "Placeholder");
        Transform placeholder = EnsureChild(sign, "Placeholder");
        PrimitivePart(placeholder, "Body", PrimitiveType.Cube, new Vector3(0f, 0.1f, 0f), new Vector3(0.5f, 0.2f, 0.12f), LitMaterial("Placeholder_Sign", new Color(0.2f, 0.34f, 0.32f), 0.4f));
        TextMeshPro label = FloatingNote(placeholder, "Label");
        label.transform.localPosition = new Vector3(0f, 0.1f, -0.065f);
        label.transform.localRotation = Quaternion.identity;
        label.text = "AVAILABLE";
        label.color = new Color(0.9f, 0.95f, 0.9f, 1f);
        placeholder.gameObject.SetActive(false);
        return placeholder.gameObject;
    }

    /// <summary>A primitive part of a stand-in prop (no collider: the prop's click box takes the clicks).</summary>
    private static GameObject PrimitivePart(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 size, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        part.name = name;
        Object.DestroyImmediate(part.GetComponent<Collider>());
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        part.transform.localScale = size;
        if (material != null)
            part.GetComponent<MeshRenderer>().sharedMaterial = material;
        return part;
    }

    /// <summary>A placeholder part of a scanner upgrade (the PC redesign SC6): a cube like PrimitivePart's, turned by <paramref name="rotation"/>, inactive until the upgrade is owned (DeskScanner.ShowUpgrades).</summary>
    private static GameObject UpgradePart(Transform parent, string name, Vector3 position, Vector3 size, Quaternion rotation, Material material)
    {
        GameObject part = PrimitivePart(parent, name, PrimitiveType.Cube, position, size, material);
        part.transform.localRotation = rotation;
        part.SetActive(false);
        return part;
    }

    /// <summary>
    /// The desk: Office/Desk (the DeskSurface plane and the DeskController)
    /// with its Papers root, the HandOver point, the inactive paper template,
    /// the decoration slots and the ViewCatcher (the mat's click, the desk
    /// view's toggle: a box on the Interactable layer the binder sizes under
    /// the desk plane, inactive until DeskView makes it live); the hand's
    /// Catcher and Examiner retired with Papers, Please's controls (destroyed
    /// when an older scene still holds them); Office/Scanner (the
    /// DeskScanner, its click box, Clickable and reaction, and a stand-in
    /// flatbed machine the binder shows where the art has no scanner, with
    /// the upgrades' feeder tray and analysis lamp, inactive until owned: SC6); the
    /// day-1 scan note. Idempotent.
    /// </summary>
    private static DeskController BuildDesk(Transform office, DeskConfigSO config, out DeskScanner scanner, out GameObject scannerPlaceholder, out TextMeshPro scanHint)
    {
        Clickable scannerClick = EnsureClickBox(office, "Scanner");
        scanner = GetOrAdd<DeskScanner>(scannerClick.gameObject);
        DestroyChildIfPresent(scannerClick.transform, "Placeholder");
        Transform machine = EnsureChild(scannerClick.transform, "Placeholder");
        PrimitivePart(machine, "Base", PrimitiveType.Cube, new Vector3(0f, 0.025f, 0f), new Vector3(0.4f, 0.05f, 0.32f), LitMaterial("Placeholder_ScannerBody", new Color(0.24f, 0.33f, 0.31f), 0.35f));
        PrimitivePart(machine, "Bed", PrimitiveType.Cube, new Vector3(0f, 0.051f, 0.01f), new Vector3(0.34f, 0.004f, 0.25f), LitMaterial("Placeholder_ScannerGlass", new Color(0.08f, 0.16f, 0.17f), 0.85f));
        PrimitivePart(machine, "Hinge", PrimitiveType.Cube, new Vector3(0f, 0.06f, 0.15f), new Vector3(0.4f, 0.03f, 0.03f), LitMaterial("Placeholder_ScannerTrim", new Color(0.84f, 0.78f, 0.65f), 0.3f));
        PrimitivePart(machine, "Light", PrimitiveType.Cube, new Vector3(0.16f, 0.052f, -0.135f), new Vector3(0.02f, 0.006f, 0.02f), LitMaterial("Placeholder_ScannerLight", new Color(0.35f, 0.95f, 0.45f), 0.6f));
        // The upgrades' parts (SC6), shown by DeskScanner.ShowUpgrades while owned: the Auto-Feed's sheet tray leaning on the hinge, the Analysis's lamp bar across the bed.
        GameObject tray = UpgradePart(machine, "FeederTray", new Vector3(0f, 0.09f, 0.19f), new Vector3(0.3f, 0.006f, 0.12f), Quaternion.Euler(-35f, 0f, 0f), LitMaterial("Placeholder_ScannerTrim", new Color(0.84f, 0.78f, 0.65f), 0.3f));
        GameObject lamp = UpgradePart(machine, "AnalysisLamp", new Vector3(0f, 0.11f, -0.1f), new Vector3(0.3f, 0.014f, 0.024f), Quaternion.identity, LitMaterial("Placeholder_ScannerLamp", new Color(0.78f, 0.72f, 0.98f), 0.7f));
        scannerPlaceholder = machine.gameObject;
        var soScanner = new SerializedObject(scanner);
        soScanner.FindProperty("dropSize").vector2Value = new Vector2(0.4f, 0.32f);
        soScanner.FindProperty("bedCentre").vector3Value = new Vector3(0f, 0.056f, 0.01f);
        SetRef(soScanner, "feederTray", tray);
        SetRef(soScanner, "analysisLamp", lamp);
        soScanner.ApplyModifiedProperties();

        scanHint = FloatingNote(office, "ScanHint", true);
        // Piece 10's longer note ("Click a paper to read it; drag it onto the scanner to open it on the PC.") wraps onto two lines, so it keeps its size.
        ((RectTransform)scanHint.transform).sizeDelta = ScanHintBox;
        scanHint.textWrappingMode = TextWrappingModes.Normal;

        Transform deskTransform = EnsureChild(office, "Desk");
        DeskSurface surface = GetOrAdd<DeskSurface>(deskTransform.gameObject);
        Transform papers = EnsureChild(deskTransform, "Papers");
        papers.localPosition = Vector3.zero;
        Transform handOver = EnsureChild(deskTransform, "HandOver");
        handOver.localPosition = Vector3.zero;
        DeskDocument template = BuildPaperTemplate(deskTransform, config);
        BuildDeskSlots(deskTransform);

        DestroyChildIfPresent(deskTransform, "Catcher");

        DestroyChildIfPresent(deskTransform, "ViewCatcher");
        Transform viewCatcher = EnsureChild(deskTransform, "ViewCatcher");
        viewCatcher.gameObject.layer = OfficeLayers.InteractableLayer;
        viewCatcher.gameObject.AddComponent<BoxCollider>();
        viewCatcher.gameObject.AddComponent<ClickCatcher>();
        viewCatcher.gameObject.SetActive(false);

        DestroyChildIfPresent(deskTransform, "Examiner");

        DeskController desk = GetOrAdd<DeskController>(deskTransform.gameObject);
        var so = new SerializedObject(desk);
        SetRef(so, "surface", surface);
        SetRef(so, "scanner", scanner);
        SetRef(so, "paperTemplate", template);
        SetRef(so, "paperRoot", papers);
        SetRef(so, "handOverPoint", handOver);
        SetRef(so, "scanHint", scanHint);
        SetRef(so, "config", config);
        so.ApplyModifiedProperties();
        return desk;
    }

    /// <summary>
    /// The inactive paper every handed-over document clones: a root (the
    /// DeskDocument, its DeskDraggable and Clickable) and its lying Sheet (the
    /// click box on the Interactable layer, the lit paper quad in the form
    /// style's paper tone that the hover outlines, the hidden photo frame with
    /// the traveller's photo, and the printing parts: BuildPaperPrint); the
    /// unlit reading material the paper wears on the desk, full size. The paper prints its
    /// document's form when it binds (redesign phase 4). Rebuilt each run.
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
        box.size = new Vector3(size.x, size.y, PaperBoxThickness);

        Sprite paperSprite = EnsureOfficeSprite("paper", EnsureFormStyle().paper, 150, 200);
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

        // The photo frame: PhotoAspect by 1 (the paper scales it to its form's photo cell) with the crop sprites one unit tall inside.
        Transform frame = EnsureChild(sheet, "PhotoSlot");
        frame.localPosition = new Vector3(0f, 0f, -0.0005f);
        PrimitivePart(frame, "Frame", PrimitiveType.Quad, Vector3.zero, new Vector3(LookCanvas.PhotoAspect, 1f, 1f), LitMaterial("Paper_PhotoFrame", PhotoGrey, 0.1f));
        frame.Find("Frame").GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
        Transform portrait = EnsureChild(frame, "Photo");
        portrait.localPosition = new Vector3(0f, 0f, -0.0005f);
        portrait.localRotation = Quaternion.identity;
        portrait.localScale = Vector3.one * PhotoFill;
        LookSpriteStack stack = portrait.gameObject.AddComponent<LookSpriteStack>();
        WireLayers(stack, portrait, 1, true);
        frame.gameObject.SetActive(false);

        PaperPrint print = BuildPaperPrint(sheet);

        Clickable click = root.gameObject.AddComponent<Clickable>();
        click.SetOutline(new Renderer[] { paper });
        DeskDraggable drag = root.gameObject.AddComponent<DeskDraggable>();
        var soDrag = new SerializedObject(drag);
        SetRef(soDrag, "proxy", box);
        soDrag.ApplyModifiedProperties();

        DeskDocument doc = root.gameObject.AddComponent<DeskDocument>();
        var so = new SerializedObject(doc);
        SetRef(so, "sheet", sheet);
        SetRef(so, "photoSlot", frame.gameObject);
        SetRef(so, "photo", stack);
        SetRef(so, "textTemplate", print.Text);
        SetRef(so, "fills", print.Fills);
        SetRef(so, "lines", print.Lines);
        SetRef(so, "seal", print.Seal);
        SetRef(so, "highlightTemplate", print.Slot);
        SetRef(so, "style", EnsureFormStyle());
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

    /// <summary>
    /// A note floating in the office (the binder places it and turns it to the
    /// camera): light text with a dark outline, auto-sized, inactive until
    /// shown; with <paramref name="backing"/> (the day-1 hints) a dark plate
    /// behind it (a Backing quad in FloatingNote_Backing, fitted to the text by
    /// NoteBacking). Idempotent.
    /// </summary>
    private static TextMeshPro FloatingNote(Transform parent, string name, bool backing = false)
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
        tmp.sortingLayerID = GameplaySortingLayerId();
        if (backing)
        {
            PrimitivePart(go.transform, "Backing", PrimitiveType.Quad, new Vector3(0f, 0f, 0.002f), Vector3.one, NoteBackingMaterial());
            MeshRenderer plate = go.transform.Find("Backing").GetComponent<MeshRenderer>();
            plate.shadowCastingMode = ShadowCastingMode.Off;
            plate.receiveShadows = false;
            plate.sortingLayerID = tmp.sortingLayerID;
            NoteBacking fit = go.AddComponent<NoteBacking>();
            var so = new SerializedObject(fit);
            SetRef(so, "text", tmp);
            SetRef(so, "backing", plate.transform);
            so.FindProperty("margin").vector2Value = NoteBackingMargin;
            so.ApplyModifiedProperties();
        }
        go.SetActive(false);
        return tmp;
    }

    /// <summary>The day-1 notes' backing material: unlit, transparent, dark, drawn before the notes' text. Created once; a designer's edits are kept.</summary>
    private static Material NoteBackingMaterial() =>
        EnsureMaterial("FloatingNote_Backing", "Universal Render Pipeline/Unlit", m =>
        {
            m.SetColor("_BaseColor", NoteBackingColour);
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_QueueOffset", -10f);
            BaseShaderGUI.SetMaterialKeywords(m);
        });

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
        group.sortingLayerID = GameplaySortingLayerId();
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
        public HelixRiverMonitor river;
        public TMP_Text credits;
        public TMP_Text clock;
    }

    /// <summary>The fallback HUD on the office overlay canvas, top right, rebuilt each run and inactive: day, credits, the Helix River and clock.</summary>
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
            river = BuildHudRiver(panel),
            clock = Cell("Clock", "09:00"),
        };
        panel.gameObject.SetActive(false);
        return hud;
    }

    /// <summary>
    /// The office overlay's top strips (reference px from the top centre): the
    /// office case HUD's compare strip, in the place of the claim tag the
    /// personalities spec's B1 removed, and the verdict strip in the same place
    /// (they never show together: the HUD shows only while a traveller is at
    /// the desk, the verdict line once they have gone).
    /// </summary>
    private const float TopStripTop = 16f;
    private static readonly Vector2 CompareStripSize = new Vector2(760f, 56f);
    private static readonly Vector2 VerdictStripSize = new Vector2(1100f, 64f);

    /// <summary>Where the desk view's "▲ Back" control starts (reference px from the top): under the office case HUD's compare strip and a gap.</summary>
    private static readonly float CaseHudClearance = TopStripTop + CompareStripSize.y + 8f;

    /// <summary>The desk view's "▲ Back" control (reference px), top left under the case HUD and the tutorial's prompt plate.</summary>
    private static readonly Vector2 DeskViewBackSize = new Vector2(200f, 44f);

    /// <summary>The gap (reference px) between the tutorial's prompt plate and the desk view's "▲ Back" control under it.</summary>
    private const float DeskViewBackGap = 12f;

    /// <summary>The band at the overlay's top the speech bubble and the wheel's ring keep clear (reference px): the office case HUD's strips, the desk view's Back control and gaps (the desk view clamps both to the top).</summary>
    private static readonly float OverlayTopClearance = CaseHudClearance + DeskViewBackSize.y + 8f;

    /// <summary>The city view's edge buttons (reference px) and their height on the screen (a share from the bottom: above the corkboards' calendar and clock).</summary>
    private static readonly Vector2 CityButtonSize = new Vector2(170f, 44f);
    private const float CityButtonHeight = 0.74f;

    /// <summary>The desk's rulebook booklet (metres, width by depth), its RULES rows and their pitch, its PAPERS rows and their pitch (the papers not handed over, to flag missing), and a tab on its top edge (metres).</summary>
    private static readonly Vector2 RulebookSize = new Vector2(0.26f, 0.3f);
    private const int RulebookRows = 5;
    private const float RulebookRowPitch = 0.044f;
    private const int RulebookPaperRows = 4;
    private const float RulebookPaperPitch = 0.04f;
    private static readonly Vector2 RulebookTabSize = new Vector2(0.058f, 0.03f);

    /// <summary>The rulebook's SEALS page: its rows (one per office) and their pitch (metres).</summary>
    private const int RulebookSealRows = 8;
    private const float RulebookSealPitch = 0.029f;

    /// <summary>The rulebook's GUIDE sheet text box (metres) and its PREV / NEXT buttons (metres).</summary>
    private static readonly Vector2 RulebookGuideBody = new Vector2(0.236f, 0.205f);
    private static readonly Vector2 RulebookGuideButton = new Vector2(0.07f, 0.024f);

    /// <summary>The red of the rulebook's NEW marks.</summary>
    private static readonly Color RulebookNewInk = new Color(0.72f, 0.1f, 0.08f);

    /// <summary>A paper's and a rulebook part's click box thickness (metres, before a paper's zone scale): thin, so the papers' stack (DeskConfigSO.paperStackStep a place) alone decides which of two overlapping papers, or a paper and the rulebook, a click or a stamp meets (Saleh 2026-10-06: documents clipping).</summary>
    private const float PaperBoxThickness = 0.0004f;

    /// <summary>The stamp bar's grey tab on the overlay's right edge (reference px).</summary>
    private static readonly Vector2 StampTabSize = new Vector2(92f, 170f);

    /// <summary>The 3D stamp bar (metres, in the rack's space: x along the office view's right, z away from the chair, y up from the stamps' feet): the two stamps' distance apart (DENIED left, APPROVED right), the rail's reach past each, its height, how far in front of the stamps it runs (toward the chair: under them on the screen, so it never covers the passport whose visa box is under a stamp) and its section (deep, tall).</summary>
    private const float StampSpacing = 0.14f;
    private const float StampRailOverhang = 0.07f;
    private const float StampRailHeight = 0.07f;
    private const float StampRailFront = 0.05f;
    private static readonly Vector2 StampRailSection = new Vector2(0.032f, 0.016f);

    /// <summary>The art's desk folder (DeskClean): the stamp rack is in its NOPE/Desk Anime materials (the desk polish: the rack matches the desk).</summary>
    private const string DeskCleanFolder = "Assets/Art/Office/DeskClean";

    /// <summary>The grey of the stamp bar's tab and the PC's tab (Papers, Please's grey tabs).</summary>
    private static readonly Color StampGrey = new Color(0.36f, 0.37f, 0.39f, 1f);

    /// <summary>The red inspect button (reference px), its margin from the bottom right corner, and its hint beside it.</summary>
    private static readonly Vector2 InspectButtonSize = new Vector2(112f, 112f);
    private const float InspectMargin = 18f;
    private static readonly Vector2 InspectHintSize = new Vector2(470f, 56f);

    /// <summary>The PC's grey tab on the overlay's left edge (reference px) and its height on the screen (a share from the bottom).</summary>
    private static readonly Vector2 PcTabSize = new Vector2(96f, 96f);
    private const float PcTabHeight = 0.4f;

    /// <summary>The arrow at an end of the desk's line waiting at the screen's edge (reference px).</summary>
    private static readonly Vector2 DeskLineArrowSize = new Vector2(30f, 30f);

    /// <summary>The stamps' hint plate (reference px), at the top right under the case HUD's strip.</summary>
    private static readonly Vector2 StampHintSize = new Vector2(560f, 50f);

    /// <summary>Where the stamps' hint plate starts below the overlay's top (reference px): under the case HUD's strip.</summary>
    private static readonly float StampPlateTop = CaseHudClearance;

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
    /// and its Root, top centre: the office compare strip (CompareBar,
    /// inactive; the CompareController draws it), where the claim tag was (no
    /// claim is printed: the personalities spec's B1). No part takes raycasts.
    /// Returns the HUD and the strip's object and text through out parameters.
    /// </summary>
    private static OfficeCaseHud BuildOfficeCaseHud(Transform overlay, out GameObject compareStrip, out TMP_Text compareText)
    {
        DestroyChildIfPresent(overlay, "OfficeCaseHud");
        Transform host = Panel(overlay, "OfficeCaseHud", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        Transform root = Panel(host, "Root", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        compareText = TopStrip(root, "CompareStrip", CompareStripSize, TopStripTop, Tooltip, ThemeRoleId.CompareBar, 18, Ink, out Transform compare);
        compareStrip = compare.gameObject;
        compareStrip.SetActive(false);

        OfficeCaseHud hud = host.gameObject.AddComponent<OfficeCaseHud>();
        var so = new SerializedObject(hud);
        SetRef(so, "root", root.gameObject);
        so.ApplyModifiedProperties();
        root.gameObject.SetActive(false);
        return hud;
    }

    /// <summary>
    /// The desk view's "▲ Back" control (the readability fix: a visible way
    /// out of the desk view) under the office overlay canvas, rebuilt each run:
    /// a small button at the top left (the art pass, 2026-10-07: off the
    /// traveller's face in the closer counter framing), under the case HUD's
    /// strips and under the tutorial's prompt plate (GuidePlateAt,
    /// GuidePlateSize: both show in the reading view on day 1), in the
    /// "&lt; Desk" button's role, its label keyed (deskView.back); a hover hint
    /// beside it, on its right (deskView.backHint: the other ways back), in the
    /// tooltip's role, shown by HoverHint (beside, not under: the speech bubble
    /// waits right under the Back control in the desk view). Inactive:
    /// DeskView shows it while tilted (BoothRules.DeskViewBackLive). Returns
    /// its button.
    /// </summary>
    private static Button BuildDeskViewBack(Transform overlay)
    {
        DestroyChildIfPresent(overlay, "DeskViewBack");
        Button back = MakeButton(overlay, "DeskViewBack", null, Vector2.zero, Vector2.one, new Color(0.2f, 0.3f, 0.5f, 0.95f), ThemeRoleId.DeskButton, "deskView.back");
        var rt = (RectTransform)back.transform;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(24f, Mathf.Min(-CaseHudClearance, GuidePlateAt.y - GuidePlateSize.y - DeskViewBackGap));
        rt.sizeDelta = DeskViewBackSize;

        Transform hint = Panel(back.transform, "Hint", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(8f, 0f), new Vector2(DeskViewBackSize.x, 34f), Tooltip, ThemeRoleId.Tooltip);
        ((RectTransform)hint).pivot = new Vector2(0f, 0.5f);
        hint.GetComponent<Image>().raycastTarget = false;
        TMP_Text hintText = Text(hint, "Label", null, 18, TextAlignmentOptions.Center, new Vector2(0.04f, 0.05f), new Vector2(0.96f, 0.95f), Ink,
                                 ThemeRoleId.Tooltip, "deskView.backHint", FontStyles.Normal, ThemeTextKind.Body, true);
        hintText.raycastTarget = false;
        hint.gameObject.SetActive(false);

        HoverHint hover = back.gameObject.AddComponent<HoverHint>();
        var so = new SerializedObject(hover);
        SetRef(so, "hint", hint.gameObject);
        so.ApplyModifiedProperties();

        back.gameObject.SetActive(false);
        return back;
    }

    /// <summary>
    /// The stamp bar, Papers, Please's with 3D stamps (Saleh 2026-10-06: "I
    /// want the 3D stamp; there should be a label on the screen to bring out
    /// the stamp stuff, and the Tab shortcut"), its stamps the two daters (the
    /// desk machine spec §1), rebuilt each run. On the office overlay:
    /// StampBar (the DeskStampTray, an always-active full-screen host) with
    /// the grey Tab at the right edge's middle ("STAMPS" over the key, TAB;
    /// inactive until the booth shows it), the hint's plate at the top right
    /// under the case HUD's strip (inactive) and an AudioSource for the clacks.
    /// In the office: StampRack (inactive until slid out; the office binder
    /// lays it, its origin at the daters' feet): a rail in the art's DeskClean
    /// green-dark with wooden end caps, brass arms down to the two daters, and
    /// the DENIED dater (left) and the APPROVED dater (right), each a click box
    /// on the Interactable layer (its pivot at its foot) with a DeskDraggable
    /// (the click box its proxy: the dater is dragged onto the paper) and a
    /// PointerHold (a held press), holding its body (DaterBody: the prop
    /// contract's Body, Frame, Die and Wheels; a green or red side button);
    /// the word printed on the rail's top over each dater, readable from the
    /// reading view. The tray gets the papers' style, the date's face and
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
        tabRect.anchorMin = tabRect.anchorMax = new Vector2(1f, 0.5f);
        tabRect.pivot = new Vector2(1f, 0.5f);
        tabRect.anchoredPosition = Vector2.zero;
        tabRect.sizeDelta = StampTabSize;
        TMP_Text tabLabel = tab.transform.Find("Label").GetComponent<TMP_Text>();
        tabLabel.text = $"<size=72%>{UiText.Get("controls.stampsTab")}</size>\n<b>{ControlRules.StampsKey}</b>";
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
        Material rail = DeskMaterial("GreenDark", new Color(0.204f, 0.294f, 0.275f));
        Material wood = DeskMaterial("Wood", new Color(0.537f, 0.392f, 0.282f));
        Material brass = DeskMaterial("Brass", new Color(0.72f, 0.58f, 0.3f));
        float railLength = 2f * (StampSpacing / 2f + StampRailOverhang);
        PrimitivePart(rack, "Rail", PrimitiveType.Cube, new Vector3(0f, StampRailHeight, -StampRailFront), new Vector3(railLength, StampRailSection.y, StampRailSection.x), rail);
        PrimitivePart(rack, "CapLeft", PrimitiveType.Cube, new Vector3(-railLength / 2f, StampRailHeight, -StampRailFront), new Vector3(0.012f, StampRailSection.y + 0.006f, StampRailSection.x + 0.006f), wood);
        PrimitivePart(rack, "CapRight", PrimitiveType.Cube, new Vector3(railLength / 2f, StampRailHeight, -StampRailFront), new Vector3(0.012f, StampRailSection.y + 0.006f, StampRailSection.x + 0.006f), wood);
        Color labelInk = new Color(0.95f, 0.93f, 0.86f);

        (Clickable stamp, Transform die) Stamp(string name, float x, Color button, string labelKey)
        {
            Clickable click = EnsureClickBox(rack, name);
            click.transform.localPosition = new Vector3(x, 0f, 0f);
            StampShape shape = DaterBody(click.transform, button);
            var box = click.GetComponent<BoxCollider>();
            box.center = new Vector3(0f, shape.Top / 2f, 0f);
            box.size = new Vector3(shape.HalfWidth * 2f + 0.006f, shape.Top, shape.HalfDepth * 2f + 0.006f);
            string word = UiText.Get(labelKey);
            Transform die = click.transform.Find("Die");

            // The arm from the rail down to the handle, and the word on the rail's top over the stamp.
            PrimitivePart(rack, name + "Arm", PrimitiveType.Cube, new Vector3(x, StampRailHeight - 0.004f, -StampRailFront / 2f), new Vector3(0.012f, 0.008f, StampRailFront), brass);
            TextMeshPro label = FlatText(rack, name + "Label", new Vector3(x, StampRailHeight + StampRailSection.y / 2f + 0.0006f, -StampRailFront),
                                         new Vector2(StampSpacing - 0.012f, StampRailSection.x - 0.004f), 0.2f, labelInk, FontStyles.Bold);
            label.text = word;

            click.SetOutline(click.GetComponentsInChildren<Renderer>(true).Where(r => r.GetComponent<TextMeshPro>() == null && r.name != "Window").ToArray());
            // The dater is moved, not the paper (Saleh 2026-10-06): left-drag carries it over the desk; its click box is the drag's proxy.
            // A left-press held on it strokes it where it hangs (the desk machine spec §1: "holding the button holds the stamp down").
            GetOrAdd<PointerHold>(click.gameObject);
            DeskDraggable drag = GetOrAdd<DeskDraggable>(click.gameObject);
            var soDrag = new SerializedObject(drag);
            SetRef(soDrag, "proxy", box);
            soDrag.ApplyModifiedProperties();
            return (click, die);
        }

        (Clickable denied, Transform deniedDie) = Stamp("Denied", -StampSpacing / 2f, new Color(0.76f, 0.16f, 0.13f), "stamp.label.denied");
        (Clickable approved, Transform approvedDie) = Stamp("Approved", StampSpacing / 2f, new Color(0.18f, 0.58f, 0.26f), "stamp.label.approved");
        // No shadows: hanging over the desk under the hall's low light they cast long dark shapes across the papers (Papers, Please's bar casts none).
        foreach (MeshRenderer part in rack.GetComponentsInChildren<MeshRenderer>(true))
            part.shadowCastingMode = ShadowCastingMode.Off;
        rack.gameObject.SetActive(false);

        DeskStampTray stamps = GetOrAdd<DeskStampTray>(host.gameObject);
        var so = new SerializedObject(stamps);
        SetRef(so, "config", config);
        SetRef(so, "rack", rack);
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

    /// <summary>A stamp body's measures (metres, in the stamp's space, its foot at the origin): the block's half width and half depth, its bottom (the die's top) and top, and the stamp's top (the knob's).</summary>
    private readonly struct StampShape
    {
        public StampShape(float halfWidth, float halfDepth, float blockBottom, float blockTop, float top)
        {
            HalfWidth = halfWidth;
            HalfDepth = halfDepth;
            BlockBottom = blockBottom;
            BlockTop = blockTop;
            Top = top;
        }

        /// <summary>The block's half width (along the rail).</summary>
        public float HalfWidth { get; }

        /// <summary>The block's half depth.</summary>
        public float HalfDepth { get; }

        /// <summary>The block's bottom: the rubber die's top.</summary>
        public float BlockBottom { get; }

        /// <summary>The block's top: the handle rises from it.</summary>
        public float BlockTop { get; }

        /// <summary>The stamp's top: its knob's cap.</summary>
        public float Top { get; }
    }

    /// <summary>The art's DeskClean material <paramref name="name"/> (NOPE/Desk Anime: the desk's own look); without it, a stand-in of that colour.</summary>
    private static Material DeskMaterial(string name, Color colour) =>
        AssetDatabase.LoadAssetAtPath<Material>($"{DeskCleanFolder}/Materials/DeskClean_{name}.mat") ?? AnimeMaterial("StampAnime_" + name, colour);

    /// <summary>A gameplay material of <paramref name="colour"/> in the desk's NOPE/Desk Anime shader (its tone bands match the art's props); URP Lit where that shader is missing.</summary>
    private static Material AnimeMaterial(string name, Color colour) =>
        Shader.Find("NOPE/Desk Anime") != null
            ? EnsureMaterial(name, "NOPE/Desk Anime", m => m.SetColor("_BaseColor", colour))
            : LitMaterial(name, colour, 0.3f);

    /// <summary>
    /// The rulebook on the desk, Papers, Please's booklet (Saleh 2026-10-06),
    /// rebuilt each run: Office/Rulebook (the DeskRulebook and its DeskDraggable;
    /// the office binder lays it beside the mat on the desk plane) and its
    /// Booklet (lifted by the papers' stack: DeskRulebook.SetLift): a cream card lying face up,
    /// its click box (CardClick: its outline and the drag's proxy), three tabs on
    /// its top edge (RULES, PAPERS, GUIDE: a plate, a word and a click box each;
    /// GUIDE's NEW badge above it), the
    /// RULES page (its title, five rows, each a click box on the Interactable
    /// layer over its text, two lines at most, and the line for a day with no
    /// directive) and the PAPERS page (its heading, four rows, click boxes over
    /// their texts: the papers not handed over, to flag missing, and the line
    /// when none is left) and the GUIDE page (the help guide: a sheet's title,
    /// its NEW mark, its text, PREV and NEXT click boxes and its number).
    /// Returns it.
    /// </summary>
    private static DeskRulebook BuildRulebook(Transform office, DeskSurface surface, DeskCounter counter)
    {
        DestroyChildIfPresent(office, "Rulebook");
        Transform book = EnsureChild(office, "Rulebook");
        Transform booklet = EnsureChild(book, "Booklet"); // lifted by the papers' stack (DeskController), the root on the desk plane
        GameObject card = PrimitivePart(booklet, "Card", PrimitiveType.Quad, Vector3.zero, new Vector3(RulebookSize.x, RulebookSize.y, 1f), LitMaterial("Rulebook_Card", new Color(0.93f, 0.9f, 0.8f), 0.15f));
        card.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        Color ink = new Color(0.13f, 0.12f, 0.15f);

        // The tabs on the top edge.
        var tabs = new List<Clickable>();
        var plates = new List<Renderer>();
        string[] tabKeys = { "desk.rulebook.tabRules", "desk.rulebook.tabPapers", "desk.rulebook.tabGuide", "desk.rulebook.tabSeals" };
        for (int i = 0; i < tabKeys.Length; i++)
        {
            float x = -RulebookSize.x / 2f + RulebookTabSize.x / 2f + 0.01f + i * (RulebookTabSize.x + 0.004f);
            Vector3 at = new Vector3(x, 0.0002f, RulebookSize.y / 2f + RulebookTabSize.y / 2f);
            GameObject plate = PrimitivePart(booklet, "TabPlate" + (i + 1), PrimitiveType.Quad, at, new Vector3(RulebookTabSize.x, RulebookTabSize.y, 1f),
                                             LitMaterial("Rulebook_Tab", new Color(0.93f, 0.9f, 0.8f), 0.15f));
            plate.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            plates.Add(plate.GetComponent<Renderer>());
            Clickable tab = EnsureClickBox(booklet, "Tab" + (i + 1));
            tab.transform.localPosition = at + new Vector3(0f, 0.0004f, 0f);
            var box = tab.GetComponent<BoxCollider>();
            box.center = Vector3.zero;
            box.size = new Vector3(RulebookTabSize.x, PaperBoxThickness, RulebookTabSize.y);
            tab.SetOutline(new[] { plate.GetComponent<Renderer>() });
            TextMeshPro word = FlatText(tab.transform, "Text", Vector3.zero, RulebookTabSize - new Vector2(0.008f, 0.006f), 0.11f, ink, FontStyles.Bold);
            word.text = UiText.Get(tabKeys[i]);
            tabs.Add(tab);
        }

        // The RULES page.
        Transform rulesPage = EnsureChild(booklet, "RulesPage");
        TextMeshPro title = FlatText(rulesPage, "Title", new Vector3(0f, 0.0006f, RulebookSize.y / 2f - 0.022f), new Vector2(RulebookSize.x - 0.02f, 0.03f), 0.2f, ink, FontStyles.Bold);
        var rows = new List<Clickable>();
        for (int i = 0; i < RulebookRows; i++)
        {
            float z = RulebookSize.y / 2f - 0.058f - i * RulebookRowPitch;
            Clickable row = EnsureClickBox(rulesPage, "Row" + (i + 1));
            row.transform.localPosition = new Vector3(0f, 0.0006f, z);
            var box = row.GetComponent<BoxCollider>();
            box.center = Vector3.zero;
            box.size = new Vector3(RulebookSize.x - 0.016f, PaperBoxThickness, RulebookRowPitch - 0.004f);
            TextMeshPro text = FlatText(row.transform, "Text", Vector3.zero, new Vector2(RulebookSize.x - 0.024f, RulebookRowPitch - 0.006f), 0.13f, ink, FontStyles.Normal);
            text.textWrappingMode = TextWrappingModes.Normal;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            rows.Add(row);
        }
        TextMeshPro none = FlatText(rulesPage, "None", new Vector3(0f, 0.0006f, RulebookSize.y / 2f - 0.058f), new Vector2(RulebookSize.x - 0.024f, 0.03f), 0.13f, ink, FontStyles.Italic);

        // The PAPERS page: its heading and a row per paper not handed over (a click flags it missing).
        Transform papersPage = EnsureChild(booklet, "PapersPage");
        TextMeshPro papersTitle = FlatText(papersPage, "PapersTitle", new Vector3(0f, 0.0006f, RulebookSize.y / 2f - 0.022f), new Vector2(RulebookSize.x - 0.02f, 0.03f), 0.16f, ink, FontStyles.Bold);
        var paperRows = new List<Clickable>();
        for (int i = 0; i < RulebookPaperRows; i++)
        {
            Clickable row = EnsureClickBox(papersPage, "Paper" + (i + 1));
            row.transform.localPosition = new Vector3(0f, 0.0006f, RulebookSize.y / 2f - 0.058f - i * RulebookPaperPitch);
            var box = row.GetComponent<BoxCollider>();
            box.center = Vector3.zero;
            box.size = new Vector3(RulebookSize.x - 0.016f, PaperBoxThickness, RulebookPaperPitch - 0.004f);
            TextMeshPro text = FlatText(row.transform, "Text", Vector3.zero, new Vector2(RulebookSize.x - 0.024f, RulebookPaperPitch - 0.006f), 0.12f, ink, FontStyles.Normal);
            text.textWrappingMode = TextWrappingModes.Normal;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            paperRows.Add(row);
        }
        TextMeshPro papersNone = FlatText(papersPage, "None", new Vector3(0f, 0.0006f, RulebookSize.y / 2f - 0.058f), new Vector2(RulebookSize.x - 0.024f, 0.03f), 0.13f, ink, FontStyles.Italic);
        papersPage.gameObject.SetActive(false);

        // The GUIDE page: the help guide's sheet (DeskRulebook.SetGuide), and the tab's NEW badge.
        Transform guidePage = EnsureChild(booklet, "GuidePage");
        TextMeshPro guideTitle = FlatText(guidePage, "Title", new Vector3(0f, 0.0006f, RulebookSize.y / 2f - 0.022f), new Vector2(RulebookSize.x - 0.02f, 0.03f), 0.15f, ink, FontStyles.Bold);
        TextMeshPro guideNew = FlatText(guidePage, "New", new Vector3(RulebookSize.x / 2f - 0.035f, 0.0006f, RulebookSize.y / 2f - 0.046f), new Vector2(0.05f, 0.018f), 0.09f, RulebookNewInk, FontStyles.Bold);
        guideNew.text = UiText.Get("desk.guide.new");
        TextMeshPro guideBody = FlatText(guidePage, "Body", new Vector3(0f, 0.0006f, RulebookSize.y / 2f - 0.058f - RulebookGuideBody.y / 2f), RulebookGuideBody, 0.14f, ink, FontStyles.Normal);
        guideBody.textWrappingMode = TextWrappingModes.Normal;
        guideBody.alignment = TextAlignmentOptions.TopLeft;
        guideBody.richText = true;
        float footer = -RulebookSize.y / 2f + 0.018f;
        Clickable GuideButton(string name, float x, string key)
        {
            Clickable button = EnsureClickBox(guidePage, name);
            button.transform.localPosition = new Vector3(x, 0.0006f, footer);
            var box = button.GetComponent<BoxCollider>();
            box.center = Vector3.zero;
            box.size = new Vector3(RulebookGuideButton.x, PaperBoxThickness, RulebookGuideButton.y);
            TextMeshPro word = FlatText(button.transform, "Text", Vector3.zero, RulebookGuideButton, 0.1f, ink, FontStyles.Bold);
            word.text = UiText.Get(key);
            return button;
        }
        Clickable guidePrev = GuideButton("Prev", -RulebookSize.x / 2f + 0.012f + RulebookGuideButton.x / 2f, "desk.guide.prev");
        Clickable guideNext = GuideButton("Next", RulebookSize.x / 2f - 0.012f - RulebookGuideButton.x / 2f, "desk.guide.next");
        TextMeshPro guideNumber = FlatText(guidePage, "Number", new Vector3(0f, 0.0006f, footer), new Vector2(0.07f, 0.02f), 0.09f, ink, FontStyles.Normal);
        guidePage.gameObject.SetActive(false);
        Transform guideTab = tabs[DeskRulebook.GuidePageIndex].transform;
        TextMeshPro guideBadge = FlatText(booklet, "GuideBadge", guideTab.localPosition + new Vector3(0f, 0f, RulebookTabSize.y / 2f + 0.011f), new Vector2(0.05f, 0.018f), 0.09f, RulebookNewInk, FontStyles.Bold);
        guideBadge.text = UiText.Get("desk.guide.new");
        guideBadge.gameObject.SetActive(false);

        // The SEALS page: the Seal Register at the desk (DeskRulebook.ShowSeals), a row per office: its seal's mark and legend, its name.
        Transform sealsPage = EnsureChild(booklet, "SealsPage");
        TextMeshPro sealsTitle = FlatText(sealsPage, "Title", new Vector3(0f, 0.0006f, RulebookSize.y / 2f - 0.022f), new Vector2(RulebookSize.x - 0.02f, 0.03f), 0.15f, ink, FontStyles.Bold);
        sealsTitle.text = UiText.Get("desk.rulebook.seals");
        Material sealMaterial = FormSealMaterial();
        var sealRows = new List<Clickable>();
        var sealMarks = new List<Renderer>();
        var sealLegends = new List<TextMeshPro>();
        var sealNames = new List<TextMeshPro>();
        float markSize = RulebookSealPitch - 0.005f;
        for (int i = 0; i < RulebookSealRows; i++)
        {
            Clickable row = EnsureClickBox(sealsPage, "Seal" + (i + 1));
            row.transform.localPosition = new Vector3(0f, 0.0006f, RulebookSize.y / 2f - 0.052f - i * RulebookSealPitch);
            var box = row.GetComponent<BoxCollider>();
            box.center = Vector3.zero;
            box.size = new Vector3(RulebookSize.x - 0.016f, PaperBoxThickness, RulebookSealPitch - 0.003f);
            float markX = -RulebookSize.x / 2f + 0.016f + markSize / 2f;
            GameObject mark = PrimitivePart(row.transform, "Mark", PrimitiveType.Quad, new Vector3(markX, 0.0002f, 0f), new Vector3(markSize, markSize, 1f), sealMaterial);
            mark.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            mark.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            Object.DestroyImmediate(mark.GetComponent<Collider>());
            TextMeshPro legend = FlatText(row.transform, "Legend", new Vector3(markX, 0.0004f, 0f), new Vector2(markSize * 0.62f, markSize * 0.4f), 0.06f, ink, FontStyles.Bold);
            float nameX = markX + markSize / 2f + 0.008f;
            float nameWidth = RulebookSize.x / 2f - 0.012f - nameX;
            TextMeshPro name = FlatText(row.transform, "Text", new Vector3(nameX + nameWidth / 2f, 0f, 0f), new Vector2(nameWidth, RulebookSealPitch - 0.006f), 0.12f, ink, FontStyles.Normal);
            name.alignment = TextAlignmentOptions.MidlineLeft;
            sealRows.Add(row);
            sealMarks.Add(mark.GetComponent<Renderer>());
            sealLegends.Add(legend);
            sealNames.Add(name);
        }
        sealsPage.gameObject.SetActive(false);

        Clickable cardClick = EnsureClickBox(booklet, "CardClick");
        var cardBox = cardClick.GetComponent<BoxCollider>();
        cardBox.center = new Vector3(0f, -0.0002f, 0f);
        cardBox.size = new Vector3(RulebookSize.x, 0.0004f, RulebookSize.y);
        cardClick.SetOutline(new[] { card.GetComponent<Renderer>() });

        DeskDraggable drag = GetOrAdd<DeskDraggable>(book.gameObject);
        var soDrag = new SerializedObject(drag);
        SetRef(soDrag, "proxy", cardBox);
        soDrag.ApplyModifiedProperties();

        DeskRulebook rulebook = GetOrAdd<DeskRulebook>(book.gameObject);
        var so = new SerializedObject(rulebook);
        SetRef(so, "booklet", booklet);
        SetRef(so, "card", cardClick);
        SetRef(so, "title", title);
        SerializedArrays.Set(so, "rows", rows);
        SetRef(so, "none", none);
        SetRef(so, "papersTitle", papersTitle);
        SerializedArrays.Set(so, "paperRows", paperRows);
        SetRef(so, "papersNone", papersNone);
        SetRef(so, "rulesPage", rulesPage.gameObject);
        SetRef(so, "papersPage", papersPage.gameObject);
        SetRef(so, "guidePage", guidePage.gameObject);
        SetRef(so, "guideTitle", guideTitle);
        SetRef(so, "guideBody", guideBody);
        SetRef(so, "guideNumber", guideNumber);
        SetRef(so, "guideNew", guideNew.gameObject);
        SetRef(so, "guidePrev", guidePrev);
        SetRef(so, "guideNext", guideNext);
        SetRef(so, "guideBadge", guideBadge.gameObject);
        SetRef(so, "sealsPage", sealsPage.gameObject);
        SerializedArrays.Set(so, "sealRows", sealRows);
        SerializedArrays.Set(so, "sealMarks", sealMarks);
        SerializedArrays.Set(so, "sealLegends", sealLegends);
        SerializedArrays.Set(so, "sealNames", sealNames);
        SerializedArrays.Set(so, "tabs", tabs);
        SerializedArrays.Set(so, "tabPlates", plates);
        SetRef(so, "drag", drag);
        SetRef(so, "surface", surface);
        SetRef(so, "counter", counter);
        so.ApplyModifiedProperties();
        return rulebook;
    }

    /// <summary>
    /// The counter (Papers, Please's, Saleh 2026-10-06), rebuilt each run:
    /// Office/Desk/Counter (the DeskCounter; the office binder lays it along the
    /// desk's far edge) holding its Zone (inactive until a traveller's papers
    /// are on the desk): an ivory see-through Strip (an unlit quad the counter
    /// tints) and its Label printed flat ("COUNTER", "▲ HAND BACK ▲", "STAMP THE
    /// PASSPORT FIRST"), as deep as the slim strip allows.
    /// </summary>
    private static DeskCounter BuildCounter(Transform desk, DeskConfigSO config, DeskSurface surface, DeskView deskView)
    {
        DestroyChildIfPresent(desk, "Counter");
        Transform host = EnsureChild(desk, "Counter");
        Transform zone = EnsureChild(host, "Zone");
        Material stripMaterial = EnsureMaterial("Counter_Strip", "Universal Render Pipeline/Unlit", m =>
        {
            m.SetColor("_BaseColor", new Color(1f, 0.96f, 0.84f, 0.14f));
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            BaseShaderGUI.SetMaterialKeywords(m);
        });
        GameObject strip = PrimitivePart(zone, "Strip", PrimitiveType.Quad, Vector3.zero, Vector3.one, stripMaterial);
        strip.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        strip.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
        // The label fills the slim strip's depth (DeskConfigSO.counterDepth: Saleh 2026-10-06, "reduce the size of the top counter line").
        TextMeshPro label = FlatText(zone, "Label", new Vector3(0f, 0.0004f, 0f), new Vector2(0.6f, config.counterDepth * 0.75f), 0.32f, new Color(0.13f, 0.12f, 0.15f), FontStyles.Bold);
        label.text = UiText.Get("desk.counter");
        zone.gameObject.SetActive(false);

        DeskCounter counter = GetOrAdd<DeskCounter>(host.gameObject);
        var so = new SerializedObject(counter);
        SetRef(so, "config", config);
        SetRef(so, "surface", surface);
        SetRef(so, "deskView", deskView);
        SetRef(so, "zone", zone.gameObject);
        SetRef(so, "strip", strip.GetComponent<Renderer>());
        SetRef(so, "label", label);
        so.ApplyModifiedProperties();
        return counter;
    }

    /// <summary>
    /// Papers, Please's red inspect button (Saleh 2026-10-06), rebuilt each run
    /// on the office overlay at the bottom right: a red Button with a white
    /// magnifying glass drawn in code (MagnifierGlyph; Saleh: "the inspect mode
    /// should be a magnify glass icon") over the key (SPACE,
    /// ControlRules.InspectKey), and its On state
    /// (inactive: DeskInspect shows it while inspect mode is on): a yellow ring
    /// round the button and the hint beside it on the tooltip's plate. Wired
    /// into <paramref name="inspect"/>; the button is inactive until the booth
    /// shows it.
    /// </summary>
    private static void BuildInspectButton(Transform overlay, DeskInspect inspect)
    {
        DestroyChildIfPresent(overlay, "InspectButton");
        Button button = MakeButton(overlay, "InspectButton", null, Vector2.zero, Vector2.one, new Color(0.72f, 0.12f, 0.1f, 1f), ThemeRoleId.DiegeticDevice);
        var rt = (RectTransform)button.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(1f, 0f);
        rt.anchoredPosition = new Vector2(-InspectMargin, InspectMargin);
        rt.sizeDelta = InspectButtonSize;
        TMP_Text label = button.transform.Find("Label").GetComponent<TMP_Text>();
        label.text = ControlRules.InspectKey;
        label.color = Color.white;
        label.enableAutoSizing = false;
        label.fontSize = 19f;
        label.fontStyle = FontStyles.Bold;
        var labelRect = (RectTransform)label.transform;
        labelRect.anchorMin = new Vector2(0f, 0.04f);
        labelRect.anchorMax = new Vector2(1f, 0.3f);
        labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
        var icon = new GameObject("Magnifier", typeof(RectTransform), typeof(CanvasRenderer), typeof(MagnifierGlyph));
        icon.transform.SetParent(button.transform, false);
        var iconRect = (RectTransform)icon.transform;
        iconRect.anchorMin = new Vector2(0.2f, 0.3f);
        iconRect.anchorMax = new Vector2(0.8f, 0.94f);
        iconRect.offsetMin = iconRect.offsetMax = Vector2.zero;
        MagnifierGlyph glyph = icon.GetComponent<MagnifierGlyph>();
        glyph.color = Color.white;
        glyph.raycastTarget = false;
        SceneUiKit.Tag(glyph, ThemeRoleId.DiegeticDevice, ThemePart.Ink);

        Transform on = Panel(button.transform, "On", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        void Ring(string side, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size)
        {
            Transform bar = Panel(on, side, aMin, aMax, pos, size, new Color(1f, 0.82f, 0.18f, 1f), ThemeRoleId.DiegeticDevice);
            bar.GetComponent<Image>().raycastTarget = false;
        }
        Ring("Top", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 3f), new Vector2(12f, 6f));
        Ring("Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, -3f), new Vector2(12f, 6f));
        Ring("Left", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(-3f, 0f), new Vector2(6f, 0f));
        Ring("Right", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(3f, 0f), new Vector2(6f, 0f));
        Transform hint = Panel(on, "Hint", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(-16f, 0f), InspectHintSize, Tooltip, ThemeRoleId.Tooltip);
        ((RectTransform)hint).pivot = new Vector2(1f, 0.5f);
        hint.GetComponent<Image>().raycastTarget = false;
        TMP_Text hintText = Text(hint, "Label", null, 20, TextAlignmentOptions.Center, new Vector2(0.03f, 0.06f), new Vector2(0.97f, 0.94f), Ink, ThemeRoleId.Tooltip,
                                 "controls.inspectHint", FontStyles.Normal, ThemeTextKind.Body, true);
        hintText.raycastTarget = false;
        on.gameObject.SetActive(false);
        button.gameObject.SetActive(false);

        var so = new SerializedObject(inspect);
        SetRef(so, "inspectButton", button);
        SetRef(so, "onState", on.gameObject);
        so.ApplyModifiedProperties();
    }

    /// <summary>
    /// The one input model (Saleh 2026-10-06), rebuilt each run: the
    /// OfficeControls on the Office root, wired to the view, the desktop's
    /// keyboard poller, the booth, the wheel, inspect mode, the stamp bar, the
    /// reading and city views and the desk; and the PC's grey tab on the
    /// overlay's left edge ("PC" over the key, Q; inactive until the booth makes
    /// the switch live), last on the overlay so it stays on top of the open PC
    /// frame. Returns the controls.
    /// </summary>
    private static OfficeControls BuildControls(Transform overlay, OfficeViewController view, BoothCoordinator booth, TravellerWheel wheel, DeskInspect inspect,
                                                DeskStampTray stamps, DeskView deskView, CityView cityView, DeskController desk)
    {
        DestroyChildIfPresent(overlay, "PcTab");
        Button tab = MakeButton(overlay, "PcTab", null, Vector2.zero, Vector2.one, StampGrey, ThemeRoleId.DiegeticDevice);
        var rt = (RectTransform)tab.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0f, PcTabHeight);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = PcTabSize;
        TMP_Text label = tab.transform.Find("Label").GetComponent<TMP_Text>();
        label.text = UiText.Format("controls.pcTab", ControlRules.PcKey);
        label.color = Color.white;
        label.enableAutoSizing = false;
        label.fontSize = 22f;
        tab.gameObject.SetActive(false);
        tab.transform.SetAsLastSibling();

        OfficeControls controls = GetOrAdd<OfficeControls>(view.gameObject);
        var so = new SerializedObject(controls);
        SetRef(so, "view", view);
        SetRef(so, "keyboard", Object.FindFirstObjectByType<DesktopKeyboard>(FindObjectsInactive.Include));
        SetRef(so, "booth", booth);
        SetRef(so, "wheel", wheel);
        SetRef(so, "inspect", inspect);
        SetRef(so, "stamps", stamps);
        SetRef(so, "deskView", deskView);
        SetRef(so, "cityView", cityView);
        SetRef(so, "desk", desk);
        SetRef(so, "pcTab", tab);
        SetRef(so, "pcTabLabel", label);
        so.ApplyModifiedProperties();
        return controls;
    }

    /// <summary>A world-space text lying face up (top edge away from the chair) under <paramref name="parent"/>, auto-sized up to <paramref name="maxSize"/>.</summary>
    private static TextMeshPro FlatText(Transform parent, string name, Vector3 position, Vector2 box, float maxSize, Color ink, FontStyles style)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshPro));
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        ((RectTransform)go.transform).sizeDelta = box;
        TextMeshPro tmp = go.GetComponent<TextMeshPro>();
        tmp.text = string.Empty;
        tmp.fontSize = maxSize;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMax = maxSize;
        tmp.fontSizeMin = maxSize * 0.4f;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = style;
        tmp.color = ink;
        tmp.sortingLayerID = GameplaySortingLayerId();
        return tmp;
    }

    /// <summary>
    /// The workbench's line over the office (the desk-first redesign, item
    /// 11), rebuilt each run: a full-screen host on the office overlay with
    /// the line layer (MatchLines, as the PC's, no gutter) and the line's two
    /// ends (proxies DeskInspect places over the values on the screen,
    /// inactive), each with its arrow (a small plate with a "▲", inactive:
    /// DeskInspect shows and turns it toward a value off the screen). Wires them
    /// into <paramref name="inspect"/> with the workbench, the compare and the
    /// office view.
    /// </summary>
    private static void BuildDeskLines(Transform overlay, DeskInspect inspect, MatchBoard board, CompareController compare)
    {
        DestroyChildIfPresent(overlay, "DeskMatchLines");
        Transform host = Panel(overlay, "DeskMatchLines", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        RectTransform End(string name, out RectTransform arrow)
        {
            Transform end = Panel(host, name, Center, Center, Vector2.zero, new Vector2(40f, 20f), null);
            ((RectTransform)end).pivot = Center;
            Transform plate = Panel(end, "Arrow", Center, Center, Vector2.zero, DeskLineArrowSize, Tooltip, ThemeRoleId.Tooltip);
            ((RectTransform)plate).pivot = Center;
            plate.GetComponent<Image>().raycastTarget = false;
            TMP_Text glyph = Text(plate, "Glyph", "\u25B2", 22, TextAlignmentOptions.Center, Vector2.zero, Vector2.one, Ink, ThemeRoleId.Tooltip);
            glyph.raycastTarget = false;
            plate.gameObject.SetActive(false);
            arrow = (RectTransform)plate;
            end.gameObject.SetActive(false);
            return (RectTransform)end;
        }
        RectTransform a = End("EndA", out RectTransform arrowA), b = End("EndB", out RectTransform arrowB);
        MatchLines lines = BuildMatchLines(host, null);
        var soLines = new SerializedObject(lines);
        soLines.FindProperty("dashNotes").boolValue = false; // over the office a dashed line only ever means a value held (Saleh 2026-10-06)
        soLines.ApplyModifiedProperties();
        host.SetSiblingIndex(0);
        var so = new SerializedObject(inspect);
        SetRef(so, "board", board);
        SetRef(so, "compare", compare);
        SetRef(so, "lines", lines);
        SetRef(so, "endA", a);
        SetRef(so, "endB", b);
        SetRef(so, "arrowA", arrowA);
        SetRef(so, "arrowB", arrowB);
        so.ApplyModifiedProperties();
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
    /// The desk view (piece 10 section 11), rebuilt each run: Office/DeskView
    /// (DeskView, wired to the desk tuning, the mat's click and the overlay's
    /// "▲ Back" control) and its inactive Camera child, a CinemachineCamera at
    /// priority 0 that the office binder poses from the art's camera and the
    /// mat at load.
    /// </summary>
    private static DeskView BuildDeskView(Transform office, DeskConfigSO config, ClickCatcher mat, Button back)
    {
        DestroyChildIfPresent(office, "DeskView");
        Transform host = EnsureChild(office, "DeskView");
        Transform cameraHost = EnsureChild(host, "Camera");
        CinemachineCamera deskCamera = cameraHost.gameObject.AddComponent<CinemachineCamera>();
        deskCamera.Priority = 0;
        cameraHost.gameObject.SetActive(false);

        DeskView deskView = host.gameObject.AddComponent<DeskView>();
        var so = new SerializedObject(deskView);
        SetRef(so, "config", config);
        SetRef(so, "deskCamera", deskCamera);
        SetRef(so, "mat", mat);
        SetRef(so, "backButton", back);
        so.ApplyModifiedProperties();
        return deskView;
    }

    /// <summary>
    /// The city view (the desk-first redesign, item 6; Saleh 2026-10-07),
    /// rebuilt each run: Office/CityView (the CityView; the binder binds it to
    /// the hall's living city), and on the office overlay, first so the rest
    /// of the overlay draws over it, CityScreen (full screen, inactive until
    /// the fade: its CanvasGroup the fade, its Image the matte in
    /// DeskConfigSO.cityMatte; both in the diegetic DiegeticDevice role, so no
    /// theme recolours the art's view) holding Panorama (a RawImage fitted whole
    /// inside the screen by an AspectRatioFitter; the view gives it the living
    /// city's material at bind); then "◀ City (A)" at the left edge and
    /// "Desk (D) ▶" at the right edge, in the "&lt; Desk" button's role
    /// (inactive: the view shows them). Returns it.
    /// </summary>
    private static CityView BuildCityView(Transform office, Transform overlay, DeskConfigSO config)
    {
        DestroyChildIfPresent(office, "CityView");
        Transform host = EnsureChild(office, "CityView");

        DestroyChildIfPresent(overlay, "CityScreen");
        // The art's view of the city, never themed (a diegetic role keeps its look).
        Transform screen = Panel(overlay, "CityScreen", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, config.cityMatte, ThemeRoleId.DiegeticDevice);
        screen.SetAsFirstSibling();
        CanvasGroup group = screen.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;
        Transform picture = Panel(screen, "Panorama", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        RawImage panorama = picture.gameObject.AddComponent<RawImage>();
        panorama.raycastTarget = false;
        SceneUiKit.Tag(panorama, ThemeRoleId.DiegeticDevice, ThemePart.Fill);
        AspectRatioFitter fitter = picture.gameObject.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fitter.aspectRatio = 3f;
        screen.gameObject.SetActive(false);

        Button Edge(string name, string key, bool left)
        {
            DestroyChildIfPresent(overlay, name);
            Button b = MakeButton(overlay, name, null, Vector2.zero, Vector2.one, new Color(0.2f, 0.3f, 0.5f, 0.95f), ThemeRoleId.DeskButton, key);
            var rt = (RectTransform)b.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(left ? 0f : 1f, CityButtonHeight);
            rt.pivot = new Vector2(left ? 0f : 1f, 0.5f);
            rt.anchoredPosition = new Vector2(left ? 12f : -12f, 0f);
            rt.sizeDelta = CityButtonSize;
            b.gameObject.SetActive(false);
            return b;
        }

        CityView view = host.gameObject.AddComponent<CityView>();
        var so = new SerializedObject(view);
        SetRef(so, "config", config);
        SetRef(so, "screen", group);
        SetRef(so, "matte", screen.GetComponent<Image>());
        SetRef(so, "panorama", panorama);
        SetRef(so, "fitter", fitter);
        SetRef(so, "lookButton", Edge("CityLook", "city.look", true));
        SetRef(so, "backButton", Edge("CityBack", "city.back", false));
        so.ApplyModifiedProperties();
        return view;
    }

    /// <summary>
    /// An overlay callout (a timed label that takes no clicks) under the office
    /// overlay canvas, rebuilt each run: an always-active full-screen host with
    /// no graphic, and its Panel child (anchors and pivot (0.5, 0.5), raycast
    /// targets off, inactive) holding an auto-sized label; with
    /// <paramref name="keepOnScreen"/> it waits at the screen's edge while its
    /// object is out of view, below the case HUD's strips (the speech bubble),
    /// else it hides (the tooltip).
    /// </summary>
    private static OverlayCallout BuildOverlayCallout(Transform overlay, string name, Vector2 size, Color background, ThemeRoleId role, bool keepOnScreen, bool grows = false)
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
        if (grows)
            Grow(panel, label, size);

        OverlayCallout callout = host.gameObject.AddComponent<OverlayCallout>();
        var so = new SerializedObject(callout);
        SetRef(so, "panel", panel);
        SetRef(so, "label", label);
        so.FindProperty("keepOnScreen").boolValue = keepOnScreen;
        so.FindProperty("topInset").floatValue = keepOnScreen ? OverlayTopClearance : 0f;
        so.FindProperty("grows").boolValue = grows;
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
        so.FindProperty("ringTopInset").floatValue = OverlayTopClearance;
        so.ApplyModifiedProperties();

        catcher.gameObject.SetActive(false);
        return wheel;
    }
}
