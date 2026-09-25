using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

/// <summary>
/// The office builder's booth and desk parts (the physical desk, piece 7):
/// the live monitor on the CRT, the traveller wheel and the overlay callouts,
/// the desk (its surface, the paper template, the scanner and the day-1
/// notes), the reacting props, the decoration slots, the traveller's view and
/// hit zone, the booth coordinator, the desk tuning assets and the shared
/// hit-zone helpers. Part of <see cref="OfficeSceneUIBuilder"/>; Build() calls these in
/// its order (geometry before the objects that wire to it).
/// </summary>
public static partial class OfficeSceneUIBuilder
{
    /// <summary>The desk tuning asset, created by the builder when missing (a designer's edits are kept).</summary>
    private const string DeskConfigPath = "Assets/Data/Config/Desk_Default.asset";

    /// <summary>The desktop canvas in canvas units: the old full-screen desktop's 1080-unit height, at the glass's 4:3.</summary>
    private static readonly Vector2 DesktopSize = new Vector2(1440f, 1080f);

    /// <summary>Size of the largest 4:3 rectangle inside the CRT's glass, in the crt sprite's own units (measured from crt.png with a colour mask): the desktop canvas fills it.</summary>
    private static readonly Vector2 CrtGlassSize = new Vector2(0.676f, 0.507f);

    /// <summary>The power button on the CRT's right bezel, beside the glass's lower right corner (crt sprite units).</summary>
    private static readonly Vector2 CrtPowerButtonCentre = new Vector2(0.285f, -0.19f);

    /// <summary>The power LED, right of the button (crt sprite units).</summary>
    private static readonly Vector2 CrtPowerLedCentre = new Vector2(0.33f, -0.19f);

    /// <summary>The focus exit zone around the screen (crt sprite units): larger than any focused view, so a click anywhere outside the screen leaves focus.</summary>
    private static readonly Vector2 FocusExitSize = new Vector2(8f, 6f);

    /// <summary>Where the desk reactions live (created by the builder when missing; a designer's edits are kept).</summary>
    private const string DeskReactionFolder = "Assets/Data/Config/DeskReactions";

    /// <summary>The highest sorting order of a desk prop (the till's number): the focus exit zone must sit above it.</summary>
    private const int HighestPropOrder = 6;

    /// <summary>The desk rectangle paper centres stay in (world centre): left of the CRT, reaching the scanner.</summary>
    private static readonly Vector3 DeskSurfaceCentre = new Vector3(-2.25f, -4.15f, 0f);

    /// <summary>The desk rectangle's size (world units): x -7.4..2.9 keeps paper centres left of the CRT, y -5.9..-2.4.</summary>
    private static readonly Vector2 DeskSurfaceSize = new Vector2(10.3f, 3.5f);

    /// <summary>Where papers slide in from and back to (world units): just past the desk rectangle's far edge, below the traveller.</summary>
    private static readonly Vector3 PaperHandOverPoint = new Vector3(0f, -2.3f, 0f);

    /// <summary>The placeholder paper's colour.</summary>
    private static readonly Color PaperCream = new Color(0.95f, 0.92f, 0.82f, 1f);

    /// <summary>The paper's photo frame (shown only on a photo document).</summary>
    private static readonly Color PhotoGrey = new Color(0.55f, 0.56f, 0.58f, 1f);

    /// <summary>The photo's height as a share of its frame's.</summary>
    private const float PhotoFill = 0.92f;

    /// <summary>The photo's first layer order in the paper's sorting group: above the frame (1), the title (2) and the holder (3).</summary>
    private const int PhotoFirstOrder = 4;

    /// <summary>The scanner's glass bed centre, in the tray sprite's own units.</summary>
    private static readonly Vector2 ScannerBedCentre = new Vector2(0.1f, 0.3f);

    /// <summary>The day-1 scan note just below the tray (tray sprite units: centre and box).</summary>
    private static readonly Vector2 ScanHintCentre = new Vector2(0f, -1.35f);

    /// <summary>The scan note's box (tray sprite units).</summary>
    private static readonly Vector2 ScanHintSize = new Vector2(6.6f, 0.6f);

    /// <summary>The scan note's order: above the tray, under the papers.</summary>
    private const int ScanHintOrder = 4;

    /// <summary>The day-1 wheel note above the traveller's head (world centre).</summary>
    private static readonly Vector2 WheelHintCentre = new Vector2(0f, 2.75f);

    /// <summary>The wheel note's box (world units).</summary>
    private static readonly Vector2 WheelHintSize = new Vector2(4.4f, 0.5f);

    /// <summary>The wheel note's order: over the back wall and the desk art, under every prop.</summary>
    private const int WheelHintOrder = -9;

    /// <summary>The desk notes' ink.</summary>
    private static readonly Color NoteInk = new Color(0.12f, 0.14f, 0.18f, 1f);

    /// <summary>The traveller's spot behind the desk (world): the Traveller object, a scale-1 parent of the anchor, the hit zone and the figure.</summary>
    private static readonly Vector3 TravellerSpot = new Vector3(0f, 0f, 2f);

    /// <summary>Where a new figure's feet stand (world): below the desk art, so its far edge (-0.84) crosses the hips.</summary>
    private static readonly Vector3 TravellerFeetWorld = new Vector3(0f, -3.9f, 2f);

    /// <summary>A new figure's canvas height in world units (head top near 2.5, the purple placeholder's place and size).</summary>
    private const float TravellerCanvasHeight = 8f;

    /// <summary>The figure's sorting group order: over the back wall (-100) and the partitions (-50), under the desk (-10).</summary>
    private const int TravellerOrder = -20;

    /// <summary>How far above the head top (canvas px) the hit zone reaches: tall hats and buns.</summary>
    private const int TravellerHatReach = 120;

    /// <summary>The desk art's far edge in world units (desk_deep.png's first opaque row): the traveller is hidden below it.</summary>
    private const float DeskArtFarEdgeY = -0.84f;

    /// <summary>The traveller hit zone's order: just above the traveller (-20).</summary>
    private const int TravellerZoneOrder = -19;

    /// <summary>The calendar hit zone over the sheet painted on the left partition (partition sprite units: centre and size).</summary>
    private static readonly Vector2 CalendarZoneCentre = new Vector2(0.35f, 3.1f);

    /// <summary>The calendar hit zone's size (partition sprite units).</summary>
    private static readonly Vector2 CalendarZoneSize = new Vector2(2.4f, 3.6f);

    /// <summary>The calendar hit zone's order: just above the partition (-50).</summary>
    private const int CalendarZoneOrder = -49;

    /// <summary>The desk's named spots (decoration hooks, item 7): the plant and the mug stand at theirs; the rest are empty for now.</summary>
    private static readonly (string id, DeskSlotKind kind, Vector3 position)[] DeskSlots =
    {
        ("plant", DeskSlotKind.Decoration, new Vector3(-7.5f, -1.15f, 0f)),
        ("mug", DeskSlotKind.Decoration, new Vector3(-6.5f, -4.8f, 0f)),
        ("photo", DeskSlotKind.Decoration, new Vector3(2.4f, -1.6f, 0f)),
        ("free_1", DeskSlotKind.Free, new Vector3(-6.8f, -5.6f, 0f)),
        ("free_2", DeskSlotKind.Free, new Vector3(2.5f, -5.6f, 0f)),
    };

    /// <summary>Returns a desk reaction asset, creating it with a kind and a tooltip when missing (a designer's edits are kept).</summary>
    private static DeskReactionSO EnsureDeskReaction(string name, ReactionKind kind, string tooltip)
    {
        string path = $"{DeskReactionFolder}/{name}.asset";
        DeskReactionSO reaction = AssetDatabase.LoadAssetAtPath<DeskReactionSO>(path);
        if (reaction != null)
            return reaction;

        EnsureFolderTree(DeskReactionFolder);
        reaction = ScriptableObject.CreateInstance<DeskReactionSO>();
        reaction.kind = kind;
        reaction.tooltip = tooltip;
        AssetDatabase.CreateAsset(reaction, path);
        return reaction;
    }

    /// <summary>Returns Desk_Default, creating it with the spec's defaults when missing (a designer's edits are kept).</summary>
    private static DeskConfigSO EnsureDeskConfig()
    {
        DeskConfigSO config = AssetDatabase.LoadAssetAtPath<DeskConfigSO>(DeskConfigPath);
        if (config != null)
            return config;

        EnsureFolderTree("Assets/Data/Config");
        config = ScriptableObject.CreateInstance<DeskConfigSO>();
        AssetDatabase.CreateAsset(config, DeskConfigPath);
        AssetDatabase.SaveAssets();
        return config;
    }

    /// <summary>
    /// The live monitor on the CRT: a ScreenAnchor at the glass's centre holding
    /// the World Space desktop canvas (1440 x 1080 units scaled into the glass's
    /// 4:3 rectangle, drawn by the main camera just above the CRT), the bezel's
    /// power button (persistent MonitorScreen.TogglePower) and LED, the focus
    /// exit zone around the screen (BuildBooth wires it to FocusOffice once the
    /// view exists) and the inert glass zone, both inactive until focused; and
    /// the MonitorScreen that owns them. Idempotent.
    /// </summary>
    private static MonitorScreen BuildMonitorScreen(SpriteRenderer crt, Canvas desktopCanvas, Camera main, DeskConfigSO config)
    {
        Transform anchor = EnsureChild(crt.transform, "ScreenAnchor");
        anchor.localPosition = new Vector3(CrtGlassCentre.x, CrtGlassCentre.y, 0f);
        anchor.localRotation = Quaternion.identity;
        anchor.localScale = Vector3.one;

        Transform canvasTransform = desktopCanvas.transform;
        canvasTransform.SetParent(anchor, false);
        canvasTransform.localPosition = Vector3.zero;
        canvasTransform.localRotation = Quaternion.identity;
        canvasTransform.localScale = Vector3.one * (CrtGlassSize.y / DesktopSize.y);
        desktopCanvas.worldCamera = main;
        desktopCanvas.sortingLayerID = SortingLayer.NameToID("Default");
        desktopCanvas.sortingOrder = config.screenCanvasOrder;

        SpriteRenderer power = PlaceSprite(crt.transform, "PowerButton", EnsureOfficeShape("crt_power", 28, 28, Center, PowerButtonPixel),
            new Vector3(CrtPowerButtonCentre.x, CrtPowerButtonCentre.y, 0f), 0.06f, config.bezelOrder);
        Clickable powerClick = EnsureClickable(power);
        SpriteRenderer led = PlaceSprite(crt.transform, "PowerLed", EnsureOfficeShape("crt_led", 8, 8, Center, LedPixel),
            new Vector3(CrtPowerLedCentre.x, CrtPowerLedCentre.y, 0f), 0.02f, config.bezelOrder);

        Clickable exit = EnsureHitZone(crt.transform, "FocusExitZone", Vector3.zero, FocusExitSize, config.focusExitOrder);
        exit.gameObject.SetActive(false);

        // A click on the screen never leaves focus, even while it is dark (its raycaster is then off).
        Clickable glass = EnsureHitZone(anchor, "GlassZone", Vector3.zero, CrtGlassSize, config.glassOrder);
        glass.Interactable = false;
        ClearPersistentCalls(glass, "onClick");
        glass.gameObject.SetActive(false);

        MonitorScreen screen = crt.GetComponent<MonitorScreen>();
        if (screen == null)
            screen = crt.gameObject.AddComponent<MonitorScreen>();
        var so = new SerializedObject(screen);
        SetRef(so, "desktopCanvas", desktopCanvas);
        SetRef(so, "desktopRaycaster", desktopCanvas.GetComponent<GraphicRaycaster>());
        SetRef(so, "glass", anchor);
        so.FindProperty("glassSize").vector2Value = CrtGlassSize;
        SetRef(so, "powerLed", led);
        SetRef(so, "config", config);
        so.ApplyModifiedProperties();

        WirePersistentVoid(powerClick, "onClick", screen, nameof(MonitorScreen.TogglePower));
        return screen;
    }

    /// <summary>
    /// A hit zone over other art: a Clickable on a box of <paramref name="size"/>
    /// (the zone's local units) with a hidden SpriteRenderer that carries only
    /// the sorting order the raycast ranks it by (a collider with no renderer
    /// reports order 0). The hover shows the hand cursor and no outline. Idempotent.
    /// </summary>
    private static Clickable EnsureHitZone(Transform parent, string name, Vector3 localPosition, Vector2 size, int sortingOrder)
    {
        Transform t = EnsureChild(parent, name);
        t.localPosition = localPosition;
        t.localRotation = Quaternion.identity;
        t.localScale = Vector3.one;

        SpriteRenderer sr = t.GetComponent<SpriteRenderer>();
        if (sr == null)
            sr = t.gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = null;
        sr.enabled = false;
        sr.sortingOrder = sortingOrder;

        BoxCollider2D box = t.GetComponent<BoxCollider2D>();
        if (box == null)
            box = t.gameObject.AddComponent<BoxCollider2D>();
        box.offset = Vector2.zero;
        box.size = size;

        Clickable click = t.GetComponent<Clickable>();
        if (click == null)
            click = t.gameObject.AddComponent<Clickable>();
        return click;
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

    /// <summary>The named desk spots under OfficeRoot/DeskSlots, each with its DeskSlot id and kind. Idempotent.</summary>
    private static Transform BuildDeskSlots(Transform booth)
    {
        Transform root = EnsureChild(booth, "DeskSlots");
        root.localPosition = Vector3.zero;
        root.localRotation = Quaternion.identity;
        root.localScale = Vector3.one;

        foreach ((string id, DeskSlotKind kind, Vector3 position) in DeskSlots)
        {
            Transform slot = EnsureChild(root, id);
            slot.localPosition = position;
            DeskSlot component = slot.GetComponent<DeskSlot>();
            if (component == null)
                component = slot.gameObject.AddComponent<DeskSlot>();
            var so = new SerializedObject(component);
            so.FindProperty("slotId").stringValue = id;
            so.FindProperty("kind").enumValueIndex = (int)kind;
            so.ApplyModifiedProperties();
        }

        return root;
    }

    /// <summary>
    /// The traveller: OfficeRoot/Traveller (TravellerView, scale 1) with its
    /// Figure (a SortingGroup with one SpriteRenderer per LookLayer, the
    /// LookSpriteStack) and the Anchor at the figure's shoulders. Existing-wins:
    /// the figure is placed only when it is created (feet at
    /// <see cref="TravellerFeetWorld"/>, canvas <see cref="TravellerCanvasHeight"/>
    /// tall); a placed figure keeps its transform, and the anchor (and, in
    /// BuildDeskInteraction, the hit zone) follow it. An earlier build's
    /// placeholder sprite on Traveller is removed (traveller.png stays). Idempotent.
    /// </summary>
    private static void BuildTraveller(Transform booth)
    {
        bool created = booth.Find("Traveller") == null;
        Transform traveller = EnsureChild(booth, "Traveller");
        SpriteRenderer placeholder = traveller.GetComponent<SpriteRenderer>();
        if (created || placeholder != null)
        {
            if (placeholder != null)
                Object.DestroyImmediate(placeholder);
            traveller.localPosition = TravellerSpot;
            traveller.localRotation = Quaternion.identity;
            traveller.localScale = Vector3.one;
        }

        Transform figure = traveller.Find("Figure");
        if (figure == null)
        {
            figure = EnsureChild(traveller, "Figure");
            figure.position = TravellerFeetWorld;
            figure.localRotation = Quaternion.identity;
            figure.localScale = Vector3.one * TravellerCanvasHeight;
        }

        SortingGroup group = figure.GetComponent<SortingGroup>();
        if (group == null)
            group = figure.gameObject.AddComponent<SortingGroup>();
        group.sortingOrder = TravellerOrder;

        LookSpriteStack stack = figure.GetComponent<LookSpriteStack>();
        if (stack == null)
            stack = figure.gameObject.AddComponent<LookSpriteStack>();
        WireLayers(stack, figure, 0, false);

        Transform anchor = EnsureChild(traveller, "Anchor");
        anchor.position = figure.TransformPoint(0f, LookCanvas.LocalY(LookCanvas.Shoulders), 0f);
        anchor.localRotation = Quaternion.identity;
        anchor.localScale = Vector3.one;

        TravellerView view = traveller.GetComponent<TravellerView>();
        if (view == null)
            view = traveller.gameObject.AddComponent<TravellerView>();
        var so = new SerializedObject(view);
        SetRef(so, "figure", stack);
        SetRef(so, "anchor", anchor);
        so.ApplyModifiedProperties();
    }

    /// <summary>
    /// One empty SpriteRenderer child per LookLayer under <paramref name="parent"/>
    /// (named after the layer, at its origin, ordered <paramref name="firstOrder"/>
    /// + the layer), wired to the stack in layer order; <paramref name="photo"/>
    /// makes it show photo crops.
    /// </summary>
    private static void WireLayers(LookSpriteStack stack, Transform parent, int firstOrder, bool photo)
    {
        var layers = new List<Object>();
        foreach (LookLayer layer in System.Enum.GetValues(typeof(LookLayer)))
        {
            SpriteRenderer sr = EnsureSprite(parent, layer.ToString(), null, Vector3.zero, firstOrder + (int)layer);
            sr.enabled = false;
            layers.Add(sr);
        }

        var so = new SerializedObject(stack);
        SerializedArrays.Set(so, "layers", layers);
        so.FindProperty("photo").boolValue = photo;
        so.ApplyModifiedProperties();
    }

    /// <summary>
    /// The desk: the scanner on the tray (its sprite's size is the drop area)
    /// with the day-1 scan note below it; OfficeRoot/DeskSurface (the rectangle
    /// paper centres stay in) with its Papers root, the HandOver point on the
    /// traveller's side and the inactive PaperTemplate; and the DeskController
    /// wired to them. Idempotent.
    /// </summary>
    private static void BuildDesk(Transform booth, SpriteRenderer tray, DeskConfigSO config)
    {
        DeskScanner scanner = tray.GetComponent<DeskScanner>();
        if (scanner == null)
            scanner = tray.gameObject.AddComponent<DeskScanner>();
        var soScanner = new SerializedObject(scanner);
        soScanner.FindProperty("dropSize").vector2Value = tray.sprite != null ? (Vector2)tray.sprite.bounds.size : Vector2.zero;
        soScanner.FindProperty("bedCentre").vector2Value = ScannerBedCentre;
        soScanner.ApplyModifiedProperties();

        TextMeshPro scanHint = WorldText(tray.transform, "ScanHint", "", NoteInk, ScanHintCentre, ScanHintSize, 0f, 5f, ScanHintOrder);
        scanHint.fontStyle = FontStyles.Bold;
        scanHint.gameObject.SetActive(false);

        Transform surfaceTransform = EnsureChild(booth, "DeskSurface");
        surfaceTransform.position = DeskSurfaceCentre;
        surfaceTransform.localRotation = Quaternion.identity;
        surfaceTransform.localScale = Vector3.one;
        DeskSurface surface = surfaceTransform.GetComponent<DeskSurface>();
        if (surface == null)
            surface = surfaceTransform.gameObject.AddComponent<DeskSurface>();
        var soSurface = new SerializedObject(surface);
        soSurface.FindProperty("size").vector2Value = DeskSurfaceSize;
        soSurface.ApplyModifiedProperties();

        Transform papers = EnsureChild(surfaceTransform, "Papers");
        papers.localPosition = Vector3.zero;
        Transform handOver = EnsureChild(surfaceTransform, "HandOver");
        handOver.position = PaperHandOverPoint;
        DeskDocument template = BuildPaperTemplate(surfaceTransform, config);

        DeskController desk = surfaceTransform.GetComponent<DeskController>();
        if (desk == null)
            desk = surfaceTransform.gameObject.AddComponent<DeskController>();
        var so = new SerializedObject(desk);
        SetRef(so, "surface", surface);
        SetRef(so, "scanner", scanner);
        SetRef(so, "paperTemplate", template);
        SetRef(so, "paperRoot", papers);
        SetRef(so, "handOverPoint", handOver);
        SetRef(so, "scanHint", scanHint);
        SetRef(so, "config", config);
        so.ApplyModifiedProperties();
    }

    /// <summary>
    /// The inactive paper every handed-over document clones: a SortingGroup root
    /// carrying the paper sprite (1.5 units wide), a click box fitted to it, a
    /// Clickable, a DeskDraggable (the box is its proxy) and the DeskDocument;
    /// the title and holder texts inside the group; the hidden photo slot. Idempotent.
    /// </summary>
    private static DeskDocument BuildPaperTemplate(Transform surface, DeskConfigSO config)
    {
        Sprite paperSprite = EnsureOfficeSprite("paper", PaperCream, 150, 200);
        SpriteRenderer paper = PlaceSprite(surface, "PaperTemplate", paperSprite, Vector3.zero, 1.5f, 0);

        SortingGroup group = paper.GetComponent<SortingGroup>();
        if (group == null)
            group = paper.gameObject.AddComponent<SortingGroup>();
        group.sortingLayerID = SortingLayer.NameToID("Default");
        group.sortingOrder = config.paperBaseOrder;

        Clickable click = EnsureClickable(paper);
        DeskDraggable drag = paper.GetComponent<DeskDraggable>();
        if (drag == null)
            drag = paper.gameObject.AddComponent<DeskDraggable>();
        var soDrag = new SerializedObject(drag);
        SetRef(soDrag, "proxy", paper.GetComponent<BoxCollider2D>());
        soDrag.ApplyModifiedProperties();

        TextMeshPro title = WorldText(paper.transform, "Title", "Document", Ink, new Vector2(0f, 0.72f), new Vector2(1.3f, 0.28f), 0f, 3f, 2);
        TextMeshPro holder = WorldText(paper.transform, "Holder", "Name", Ink, new Vector2(0f, 0.42f), new Vector2(1.3f, 0.24f), 0f, 2.4f, 3);
        SpriteRenderer photo = PlaceSprite(paper.transform, "PhotoSlot", paperSprite, new Vector3(0f, -0.25f, 0f), 0.6f, 1);
        photo.color = PhotoGrey;
        photo.gameObject.SetActive(false);

        // The photo: crop sprites one unit tall, scaled to fill the frame's height.
        Transform portrait = EnsureChild(photo.transform, "Photo");
        portrait.localPosition = Vector3.zero;
        portrait.localRotation = Quaternion.identity;
        portrait.localScale = Vector3.one * (paperSprite.bounds.size.y * PhotoFill);
        LookSpriteStack stack = portrait.GetComponent<LookSpriteStack>();
        if (stack == null)
            stack = portrait.gameObject.AddComponent<LookSpriteStack>();
        WireLayers(stack, portrait, PhotoFirstOrder, true);

        DeskDocument doc = paper.GetComponent<DeskDocument>();
        if (doc == null)
            doc = paper.gameObject.AddComponent<DeskDocument>();
        var so = new SerializedObject(doc);
        SetRef(so, "title", title);
        SetRef(so, "holder", holder);
        SetRef(so, "photoSlot", photo.gameObject);
        SetRef(so, "photo", stack);
        SetRef(so, "click", click);
        SetRef(so, "drag", drag);
        SetRef(so, "group", group);
        so.ApplyModifiedProperties();

        paper.gameObject.SetActive(false);
        return doc;
    }

    /// <summary>
    /// The booth's clicks, after every prop exists (the wall clock is built by
    /// BuildShiftClockReadouts): each prop's click box and reaction (the reaction
    /// assets are created once), the calendar and traveller hit zones, the
    /// wheel's openers (the traveller and the desk intercom) and its traveller,
    /// the decor props' ids, the day-1 wheel note, and the BoothCoordinator wired
    /// to all of it; then the desk's checks (sorting bands, paper spawn slots).
    /// Idempotent.
    /// </summary>
    private static BoothCoordinator BuildDeskInteraction(OfficeViewController view, MonitorScreen screen, DeskConfigSO config, TravellerWheel wheel,
                                                         OverlayCallout tooltip, TMP_Text trayClockText, ContentLibrarySO library)
    {
        Transform booth = view.transform;
        Transform trayTransform = booth.Find("ScannerTray");
        Transform tillTransform = booth.Find("CreditsTill");
        Transform stabilityTransform = booth.Find("StabilityMonitor");
        Transform partition = booth.Find("LeftPartition");

        Clickable stamp = BuildProp(booth.Find("DeskStamp"), EnsureDeskReaction("Reaction_Stamp", ReactionKind.Squash, ""), tooltip, null, null);
        Clickable mug = BuildProp(booth.Find("DeskMug"), EnsureDeskReaction("Reaction_Mug", ReactionKind.Wobble, ""), tooltip, null, null);
        Clickable plant = BuildProp(booth.Find("DeskPlant"), EnsureDeskReaction("Reaction_Plant", ReactionKind.Wobble, ""), tooltip, null, null);
        Clickable poster = BuildProp(booth.Find("ReactivePoster"), EnsureDeskReaction("Reaction_Poster", ReactionKind.Wobble, ""), tooltip, null, null);
        Clickable intercom = BuildProp(booth.Find("DeskIntercom"), EnsureDeskReaction("Reaction_Intercom", ReactionKind.Squash, ""), tooltip, null, null);
        Clickable tray = BuildProp(trayTransform, EnsureDeskReaction("Reaction_Scanner", ReactionKind.Pulse, ""), tooltip, null, null);
        Clickable till = BuildProp(tillTransform, EnsureDeskReaction("Reaction_Till", ReactionKind.Nudge, "Credits: {value}"), tooltip,
                                   ReadoutText(tillTransform, "CreditsNumber"), tillTransform.GetComponent<AudioSource>());
        Clickable stability = BuildProp(stabilityTransform, EnsureDeskReaction("Reaction_Stability", ReactionKind.None, "Timeline stability: {value}"), tooltip,
                                        ReadoutText(stabilityTransform, "StabilityPercent"), null);
        Clickable clock = BuildProp(booth.Find("WallClock"), EnsureDeskReaction("Reaction_Clock", ReactionKind.None, "{value}"), tooltip, trayClockText, null);

        // The calendar is painted on the left partition: a hit zone over its sheet.
        Clickable calendar = EnsureHitZone(partition, "CalendarZone", CalendarZoneCentre, CalendarZoneSize, CalendarZoneOrder);
        WireReaction(calendar, EnsureDeskReaction("Reaction_Calendar", ReactionKind.None, "Day {value}"), tooltip, ReadoutText(partition, "DayNumber"), null);
        AssetDatabase.SaveAssets();

        // A finished scan pulses the scanner.
        var soScanner = new SerializedObject(trayTransform.GetComponent<DeskScanner>());
        SetRef(soScanner, "reaction", trayTransform.GetComponent<DeskReaction>());
        soScanner.ApplyModifiedProperties();

        // Decoration hooks: the decor props' ids.
        SetDeskItem(booth.Find("DeskStamp"), "stamp");
        SetDeskItem(booth.Find("DeskMug"), "mug");
        SetDeskItem(booth.Find("DeskPlant"), "plant");
        SetDeskItem(booth.Find("ReactivePoster"), "poster");

        // The traveller: a hit zone over the figure above the desk art's far
        // edge (arms' reach wide, up into the headroom for hats), a child of
        // Traveller outside the figure's sorting group with its own hidden
        // renderer. It follows the figure's placement. It and the desk
        // intercom open the wheel, which centres on the traveller's anchor.
        Transform traveller = booth.Find("Traveller");
        Transform figure = traveller.Find("Figure");
        Vector3 left = traveller.InverseTransformPoint(figure.TransformPoint(LookCanvas.LocalX(LookCanvas.CenterX - LookCanvas.ArmReach), 0f, 0f));
        Vector3 right = traveller.InverseTransformPoint(figure.TransformPoint(LookCanvas.LocalX(LookCanvas.CenterX + LookCanvas.ArmReach), 0f, 0f));
        float top = traveller.InverseTransformPoint(figure.TransformPoint(0f, LookCanvas.LocalY(LookCanvas.HeadTop - TravellerHatReach), 0f)).y;
        float bottom = Mathf.Max(traveller.InverseTransformPoint(new Vector3(0f, DeskArtFarEdgeY, 0f)).y, traveller.InverseTransformPoint(figure.position).y);
        Clickable travellerZone = EnsureHitZone(traveller, "TravellerHitZone", new Vector3((left.x + right.x) / 2f, (bottom + top) / 2f, 0f),
                                                new Vector2(right.x - left.x, top - bottom), TravellerZoneOrder);
        WirePersistentVoid(travellerZone, "onClick", wheel, nameof(TravellerWheel.Open));
        WirePersistentVoid(intercom, "onClick", wheel, nameof(TravellerWheel.Open));
        var soWheel = new SerializedObject(wheel);
        SetRef(soWheel, "traveller", traveller.GetComponent<TravellerView>());
        soWheel.ApplyModifiedProperties();

        // The day-1 wheel note above the traveller's head (the coordinator sets its text and shows it).
        TextMeshPro wheelHint = WorldText(booth, "WheelHint", "", NoteInk, WheelHintCentre, WheelHintSize, 0f, 5f, WheelHintOrder);
        wheelHint.fontStyle = FontStyles.Bold;
        wheelHint.gameObject.SetActive(false);

        // The booth coordinator applies the input rules to all of it.
        Transform crt = booth.Find("CRTMonitor");
        BoothCoordinator coordinator = booth.GetComponent<BoothCoordinator>();
        if (coordinator == null)
            coordinator = booth.gameObject.AddComponent<BoothCoordinator>();
        var so = new SerializedObject(coordinator);
        SetRef(so, "view", view);
        SetRef(so, "screen", screen);
        SetRef(so, "desk", booth.Find("DeskSurface").GetComponent<DeskController>());
        SetRef(so, "wheel", wheel);
        SetRef(so, "crt", crt.GetComponent<Clickable>());
        SetRef(so, "powerButton", crt.Find("PowerButton").GetComponent<Clickable>());
        SetRef(so, "focusExit", crt.Find("FocusExitZone").GetComponent<Clickable>());
        SetRef(so, "glassZone", crt.Find("ScreenAnchor/GlassZone").GetComponent<Clickable>());
        SetRef(so, "travellerHitZone", travellerZone);
        Clickable[] props = { stamp, mug, plant, poster, intercom, tray, till, stability, clock, calendar };
        SerializedProperty propList = so.FindProperty("props");
        propList.arraySize = props.Length;
        for (int i = 0; i < props.Length; i++)
            propList.GetArrayElementAtIndex(i).objectReferenceValue = props[i];
        SetRef(so, "wheelHint", wheelHint);
        SetRef(so, "config", config);
        so.ApplyModifiedProperties();

        // Checks: input and drawing order agree; every paper a traveller carries has a spawn slot.
        int maxPapers = library != null ? ContentLibraryValidator.MaxDocuments(ContentLibraryValidator.TravellerBlueprints(library)) : 0;
        foreach (string problem in SortingBands.Problems(HighestPropOrder, config.focusExitOrder, config.glassOrder, config.bezelOrder,
                                                         config.screenCanvasOrder, config.paperBaseOrder, maxPapers, config.heldPaperOrder))
            Debug.LogError($"[TimeDesk] The desk's sorting bands overlap: {problem}; fix the orders in Desk_Default.");
        int slots = config.paperSpawnSlots != null ? config.paperSpawnSlots.Length : 0;
        if (slots < maxPapers)
            Debug.LogError($"[TimeDesk] The desk has {slots} paper spawn slots but a traveller can carry {maxPapers} papers; add slots in Desk_Default.");

        return coordinator;
    }

    /// <summary>Makes a booth sprite a reacting prop: a click box fitted to its art and a DeskReaction.</summary>
    private static Clickable BuildProp(Transform prop, DeskReactionSO reaction, OverlayCallout tooltip, TMP_Text readout, AudioSource audioSource)
    {
        Clickable click = EnsureClickable(prop.GetComponent<SpriteRenderer>());
        WireReaction(click, reaction, tooltip, readout, audioSource);
        return click;
    }

    /// <summary>Adds or rewires a clickable's DeskReaction (its readout and audio source are optional).</summary>
    private static void WireReaction(Clickable click, DeskReactionSO reaction, OverlayCallout tooltip, TMP_Text readout, AudioSource audioSource)
    {
        DeskReaction component = click.GetComponent<DeskReaction>();
        if (component == null)
            component = click.gameObject.AddComponent<DeskReaction>();
        var so = new SerializedObject(component);
        SetRef(so, "reaction", reaction);
        SetRef(so, "readout", readout);
        SetRef(so, "tooltip", tooltip);
        SetRef(so, "audioSource", audioSource);
        so.ApplyModifiedProperties();
    }

    /// <summary>A readout's text under a prop, or null.</summary>
    private static TMP_Text ReadoutText(Transform prop, string name)
    {
        Transform t = prop.Find(name);
        return t != null ? t.GetComponent<TMP_Text>() : null;
    }

    /// <summary>Gives a decor prop its DeskItem id.</summary>
    private static void SetDeskItem(Transform prop, string id)
    {
        DeskItem item = prop.GetComponent<DeskItem>();
        if (item == null)
            item = prop.gameObject.AddComponent<DeskItem>();
        var so = new SerializedObject(item);
        so.FindProperty("itemId").stringValue = id;
        so.ApplyModifiedProperties();
    }

    /// <summary>
    /// An overlay callout (a timed label that takes no clicks) under the office
    /// overlay canvas, rebuilt each run: an always-active full-screen host with
    /// no graphic, and its Panel child (anchors and pivot (0.5, 0.5), raycast
    /// targets off, inactive) holding an auto-sized label.
    /// </summary>
    private static OverlayCallout BuildOverlayCallout(Transform overlay, string name, Vector2 size, Color background)
    {
        DestroyChildIfPresent(overlay, name);
        Transform host = Panel(overlay, name, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        Transform panel = Panel(host, "Panel", Center, Center, Vector2.zero, size, background);
        ((RectTransform)panel).pivot = Center;
        panel.GetComponent<Image>().raycastTarget = false;

        TMP_Text label = Text(panel, "Label", "", 24, TextAlignmentOptions.Center, new Vector2(0.04f, 0.08f), new Vector2(0.96f, 0.92f), Ink);
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
    /// BuildDeskInteraction, once the traveller view exists.
    /// </summary>
    private static TravellerWheel BuildTravellerWheel(Transform overlay, DeskConfigSO config, OverlayCallout bubble)
    {
        DestroyChildIfPresent(overlay, "TravellerWheel");
        Transform host = Panel(overlay, "TravellerWheel", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        Transform catcher = Panel(host, "Catcher", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0f));

        Transform ring = Panel(catcher, "Ring", Center, Center, Vector2.zero, Vector2.zero, null);
        ((RectTransform)ring).pivot = Center;
        RadialLayoutGroup layout = ring.gameObject.AddComponent<RadialLayoutGroup>();
        layout.Radii = config.wheelRadii;
        layout.ItemSize = config.wheelItemSize;

        Transform centre = Panel(ring, "Centre", Center, Center, Vector2.zero, config.wheelCentreSize, null);
        ((RectTransform)centre).pivot = Center;
        centre.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

        Button template = MakeButton(ring, "ActionButtonTemplate", "Choice", Vector2.zero, Vector2.one, new Color(0.16f, 0.28f, 0.42f, 0.95f));
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
