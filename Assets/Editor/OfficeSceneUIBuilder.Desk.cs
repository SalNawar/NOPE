using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The office builder's booth and desk parts (the physical desk, piece 7):
/// the live monitor on the CRT, the traveller wheel and the overlay callouts,
/// the desk tuning asset and the shared hit-zone helpers. Part of <see cref="OfficeSceneUIBuilder"/>; Build() calls these in
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
