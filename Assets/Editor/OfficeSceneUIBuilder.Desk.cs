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
/// and the binder; on the overlay the office case HUD, the stamp bar and
/// the PC's tab (Papers, Please's controls). Nothing here
/// knows where the art puts things: the binder reads the scene contract at
/// load. Part of <see cref="OfficeSceneUIBuilder"/>; Build() calls these in
/// its order.
/// P0-A carve (docs/pending/CARVE.md): the paper and the rulebook are in
/// .DeskPaper.cs, the stamp rack in .StampRack.cs, the view chrome in
/// .Chrome.cs, the CRT clock in .Clock.cs, the booth in .Booth.cs, the PC
/// frame in .PcFrame.cs. Owner: DESK-PLAN.
/// </summary>
public static partial class OfficeSceneUIBuilder
{
    /// <summary>The desk tuning asset, created by the builder when missing (a designer's edits are kept).</summary>
    internal const string DeskConfigPath = "Assets/Data/Config/Desk_Default.asset";

    /// <summary>The office scene contract, created by the builder when missing (the art side's anchors and a designer's edits are kept).</summary>
    internal const string OfficeContractPath = "Assets/Data/Config/OfficeSceneContract.asset";

    /// <summary>Where the desk reactions live (created by the builder when missing; a designer's edits are kept).</summary>
    private const string DeskReactionFolder = "Assets/Data/Config/DeskReactions";

    /// <summary>The gameplay layer's shaders and materials.</summary>
    private const string GameplayArtFolder = "Assets/Art/Office/Gameplay";

    /// <summary>URP's unlit sprite material: the traveller's layers and the passport photo (unlit art, tinted into the room by the binder).</summary>
    private const string UnlitSpriteMaterialPath = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";

    /// <summary>The desktop canvas in canvas units: 4:3 at the old full-screen desktop's 1080-unit height.</summary>
    private static readonly Vector2 DesktopSize = new Vector2(1440f, 1080f);

    /// <summary>The frame's glass (reference px from the frame's bottom left): 4:3 like the desktop.</summary>
    private static readonly Rect FrameGlass = new Rect(60f, 160f, 1120f, 840f);

    /// <summary>The day-1 scan note's box (metres): two lines of the note at the size one line of the old, shorter note had.</summary>
    private static readonly Vector2 ScanHintBox = new Vector2(0.78f, 0.13f);

    /// <summary>The day-1 wheel note's box (metres): two lines, twice the size of the old one-line note (it floats above the traveller, far from the camera).</summary>
    private static readonly Vector2 WheelHintBox = new Vector2(1.2f, 0.23f);

    /// <summary>The wheel note's largest size (TMP world units): room for its two lines at twice the old note's size.</summary>
    private const float WheelHintMaxSize = 1f;

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

        // The desk machines (the desk machines spec §1): the chip reader and the UV torch.
        DeskHardware hardware = BuildDeskHardware(office, config, tooltip);

        // The documents land on the counter and come to the reading view by themselves (Papers, Please's zones).
        var soDesk = new SerializedObject(desk);
        SetRef(soDesk, "hardware", hardware);
        SetRef(soDesk, "counter", counter);
        SetRef(soDesk, "deskView", deskView);
        SetRef(soDesk, "stamps", stampTray);
        soDesk.ApplyModifiedProperties();

        // The traveller (the wheel's one opener is the desk intercom the office binder builds: P0-C, docs/pending/CUTS.md).
        TravellerView traveller = BuildTraveller(office, out Clickable travellerZone);
        // Inspection at the desk (Papers, Please's inspect mode): a click on the traveller picks their face in inspect mode, else nothing.
        DestroyChildIfPresent(office, "DeskInspect");
        DeskInspect inspect = EnsureChild(office, "DeskInspect").gameObject.AddComponent<DeskInspect>();
        // The red SPACE inspect button is cut (P0-C): a scene built before keeps none.
        DestroyChildIfPresent(deskViewBack.transform.parent, "InspectButton");
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

        // The old stamp is cut (P0-C): a scene built before keeps no click box for it.
        DestroyChildIfPresent(propsRoot, "Stamp");
        // The art's phone (the Intercom anchor), moved onto the intercom's home by the binder, is the intercom (the desk home map): its squash here, its wheel call added at bind (OfficeSceneBinder.BindIntercom), none stored.
        Prop("Intercom", OfficeAnchorId.Intercom, EnsureDeskReaction("Reaction_Intercom", ReactionKind.Squash, ""), null, null);
        Prop("Till", OfficeAnchorId.Till, EnsureDeskReaction("Reaction_Till", ReactionKind.Pulse, "tooltip.credits", 0.08f), OfficeAnchorId.ReadoutCredits, null);
        Prop("StabilityMonitor", OfficeAnchorId.StabilityMonitor, EnsureDeskReaction("Reaction_Stability", ReactionKind.None, "tooltip.stability"), null, null);
        Prop("Calendar", OfficeAnchorId.Calendar, EnsureDeskReaction("Reaction_Calendar", ReactionKind.None, "tooltip.day"), OfficeAnchorId.ReadoutDay, null);
        Prop("Clock", OfficeAnchorId.Clock, EnsureDeskReaction("Reaction_Clock", ReactionKind.None, "tooltip.value"), OfficeAnchorId.ReadoutClock, null);
        // The calculator, the pen pot and the stapler are cut (Saleh 2026-10-20, the desk home map): a scene built before keeps no click box for them.
        DestroyChildIfPresent(propsRoot, "Calculator");
        DestroyChildIfPresent(propsRoot, "PenPot");
        DestroyChildIfPresent(propsRoot, "Stapler");
        ClearPersistentCalls(propsRoot.Find("Intercom").GetComponent<Clickable>(), "onClick");
        WirePersistentVoid(propsRoot.Find("Calendar").GetComponent<Clickable>(), "onClick", inspect, nameof(DeskInspect.CalendarClicked));
        clicks.AddRange(rulebook.Clicks);
        var soInspect = new SerializedObject(inspect);
        SetRef(soInspect, "desk", desk);
        SetRef(soInspect, "traveller", traveller);
        SetRef(soInspect, "calendar", propsRoot.Find("Calendar"));
        SetRef(soInspect, "rulebook", rulebook);
        SetRef(soInspect, "sealRegister", library != null ? library.ReferenceBooks.FirstOrDefault(b => b != null && b.category == ClueCategory.Seal) : null);
        SetRef(soInspect, "view", view);
        SetRef(soInspect, "city", cityView);
        soInspect.ApplyModifiedProperties();
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
        CrtClock crtClock = BuildCrtClock(office, config, trayClockText);
        SetRef(soClock, "crtClockText", crtClock.transform.Find("Time").GetComponent<TMP_Text>());
        soClock.ApplyModifiedProperties();
        var soScreenClone = new SerializedObject(screen.GetComponent<PcScreenClone>());
        SetRef(soScreenClone, "clock", crtClock);
        soScreenClone.ApplyModifiedProperties();

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
        SetRef(so, "game", game);
        so.ApplyModifiedProperties();

        // The one input model: the office's keys, the right-click back-out and the PC's grey tab (Papers, Please's controls).
        OfficeControls controls = BuildControls(deskViewBack.transform.parent, view, coordinator, wheel, inspect, stampTray, deskView, cityView, desk, config);
        var soControls = new SerializedObject(coordinator);
        SetRef(soControls, "controls", controls);
        soControls.ApplyModifiedProperties();

        // The edge pull tabs (the PC tab, the CITY tab, the STAMPS tab on the top edge) keep clear of the calendar on the corkboard (Saleh 2026-10-18).
        EdgeTabClearance tabClearance = GetOrAdd<EdgeTabClearance>(view.gameObject);
        var soClear = new SerializedObject(tabClearance);
        SetRef(soClear, "config", config);
        SetRef(soClear, "calendar", propsRoot.Find("Calendar"));
        Transform edgeOverlay = deskViewBack.transform.parent;
        SerializedArrays.Set(soClear, "tabs", new[] { (RectTransform)edgeOverlay.Find("PcTab"), (RectTransform)edgeOverlay.Find("CityLook"), (RectTransform)edgeOverlay.Find("StampBar/Tab") });
        soClear.ApplyModifiedProperties();

        // The binder puts all of it on the art office at load.
        OfficeSceneBinder binder = GetOrAdd<OfficeSceneBinder>(office.gameObject);
        var soBinder = new SerializedObject(binder);
        SetRef(soBinder, "contract", contract);
        SetRef(soBinder, "config", config);
        SetRef(soBinder, "wheel", wheel);
        SetRef(soBinder, "tabClearance", tabClearance);
        SerializedArrays.Set(soBinder, "callouts", callouts);
        SetRef(soBinder, "stampTray", stampTray);
        SetRef(soBinder, "detainButton", BuildDetainButton(office, stampTray));
        SetRef(soBinder, "hardware", hardware);
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
    /// DeskScanner, its click box, Clickable and reaction, its AudioSource and
    /// sounds, and its machine the binder shows where the art office has no
    /// scanner: the cream scanner when its art is complete (BuildScannerArt),
    /// else the stand-in flatbed (BuildScannerStandIn)); the day-1 scan note.
    /// Idempotent.
    /// </summary>
    private static DeskController BuildDesk(Transform office, DeskConfigSO config, out DeskScanner scanner, out GameObject scannerPlaceholder, out TextMeshPro scanHint)
    {
        Clickable scannerClick = EnsureClickBox(office, "Scanner");
        scanner = GetOrAdd<DeskScanner>(scannerClick.gameObject);
        DestroyChildIfPresent(scannerClick.transform, "Placeholder");
        DestroyChildIfPresent(scannerClick.transform, "Machine");
        DestroyChildIfPresent(scannerClick.transform, "SweepBar");
        var soScanner = new SerializedObject(scanner);
        soScanner.FindProperty("dropSize").vector2Value = new Vector2(0.4f, 0.32f);
        AudioSource scannerSound = GetOrAdd<AudioSource>(scannerClick.gameObject);
        scannerSound.playOnAwake = false;
        scannerSound.spatialBlend = 0f;
        SetRef(soScanner, "sound", scannerSound);
        SetRef(soScanner, "lidOpenSound", DaterSound("scanner_lid_open"));
        SetRef(soScanner, "lidCloseSound", DaterSound("scanner_lid_close"));
        SetRef(soScanner, "scanSound", DaterSound("scanner_scan"));
        scannerPlaceholder = BuildScannerArt(scannerClick.transform, soScanner, out GameObject art) ? art : BuildScannerStandIn(scannerClick.transform, soScanner);
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
    private static readonly Vector2 VerdictStripSize = new Vector2(820f, 72f);

    /// <summary>Where the desk view's "▲ Back" control starts (reference px from the top): under the office case HUD's compare strip and a gap.</summary>
    private static readonly float CaseHudClearance = TopStripTop + CompareStripSize.y + 8f;

    /// <summary>The desk view's "▲ Back" control (reference px), top left under the case HUD and the tutorial's prompt plate.</summary>
    private static readonly Vector2 DeskViewBackSize = new Vector2(200f, 44f);

    /// <summary>The band at the overlay's top the speech bubble and the wheel's ring keep clear (reference px): the office case HUD's strips, the desk view's Back control and gaps (the desk view clamps both to the top).</summary>
    private static readonly float OverlayTopClearance = CaseHudClearance + DeskViewBackSize.y + 8f;
    /// <summary>The desk's rulebook folder (Saleh's Canva art, run 7: "folder too"; ArtSlots.RulebookFolder and its tabs, ArtSlots.RulebookTab), metres (width by depth, at the art's 16:9).</summary>
    private static readonly Vector2 RulebookFolderSize = new Vector2(0.46f, 0.25875f);

    /// <summary>Places on the folder art as shares of it from its top-left (x0, y0, x1, y1), measured on the art: the right sheet the pages print on, the RULES page's heading between its two printed rules, and its six ruled rows (the art's lines; each row's text above its line).</summary>
    private static readonly Vector4 RulebookSheet = new Vector4(0.54167f, 0.17778f, 0.93333f, 0.95926f), RulebookHeading = new Vector4(0.57083f, 0.22074f, 0.90417f, 0.26963f);

    /// <summary>The rulebook's page: the folder's right sheet (metres, width by depth); every page prints on it.</summary>
    private static readonly Vector2 RulebookSize = new Vector2((RulebookSheet.z - RulebookSheet.x) * RulebookFolderSize.x, (RulebookSheet.w - RulebookSheet.y) * RulebookFolderSize.y);
    /// <summary>The rulebook's GUIDE sheet text box (metres) and its PREV / NEXT buttons (metres).</summary>
    private static readonly Vector2 RulebookGuideBody = new Vector2(RulebookSize.x - 0.016f, RulebookSize.y - 0.075f);
    /// <summary>The STAMPS tab on the overlay's top edge beside the drawer, flush on the edge (reference px; Saleh's 1016a playtest: "the ui for it should be the same area it spawns so not on the side"; DeskStampTray.PlaceTab keeps it beside the drawer).</summary>
    private static readonly Vector2 StampTabSize = new Vector2(236f, 64f);
    /// <summary>The art's desk folder (DeskClean): the stamp rack is in its NOPE/Desk Anime materials (the desk polish: the rack matches the desk).</summary>
    private const string DeskCleanFolder = "Assets/Art/Office/DeskClean";

    /// <summary>The grey of the stamp bar's tab and the PC's tab (Papers, Please's grey tabs).</summary>
    private static readonly Color StampGrey = new Color(0.36f, 0.37f, 0.39f, 1f);
    /// <summary>The arrow at an end of the desk's line waiting at the screen's edge (reference px).</summary>
    private static readonly Vector2 DeskLineArrowSize = new Vector2(30f, 30f);

    /// <summary>Where the stamps' hint plate starts below the overlay's top (reference px): under the STAMPS tab on the top edge and the guide's arrow's room under it (clear of the case HUD's strip too).</summary>
    private static readonly float StampPlateTop = StampTabSize.y + StampArrowRoom;

    /// <summary>The room under the STAMPS tab on the top edge for the guide's arrow pointing up at it (reference px): the stamps' hint plate starts below it.</summary>
    private const float StampArrowRoom = 80f;

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

    /// <summary>A gameplay material of <paramref name="colour"/> in the desk's NOPE/Desk Anime shader (its tone bands match the art's props); URP Lit where that shader is missing.</summary>
    private static Material AnimeMaterial(string name, Color colour) =>
        Shader.Find("NOPE/Desk Anime") != null
            ? EnsureMaterial(name, "NOPE/Desk Anime", m => m.SetColor("_BaseColor", colour))
            : LitMaterial(name, colour, 0.3f);

    /// <summary>The desk documents' models (Saleh 2026-10-17: "use canva and blender to make actual 3d documents"; made headless by tools/props/documents/docs_models.py) and their edge atlas.</summary>
    private const string DocumentsFolder = GameplayArtFolder + "/Documents";

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
}
