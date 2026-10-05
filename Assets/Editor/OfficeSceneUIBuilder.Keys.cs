using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The office builder's keys, clipboard, pins and zoom (redesign phase 20;
/// the PC spec's KB1-KB5, CP1-CP3, PR1-PR2, §3.4, §3.5, §5.2): the desktop's
/// one keyboard poller (DesktopKeyboard, on the desktop canvas, wired to the
/// window stack, the icons, the Start menu, the context menu, the
/// Investigation app, Notes and the F1 card; the office view's Escape defers
/// to its stamp); the F1 card (the ShortcutsWindow: a scrolling list the
/// card fills from ShortcutMap.Card); the context menu's row entries (Copy
/// value, Copy row, Pin, Pick for compare); and the app's parts: the search
/// drawer's field made live with its chip for a pasted untranslated line,
/// the drawer's Pinned and Recent lists (its quick-open panel's: the PC
/// workbench spec IA9), the focus ring (four FocusRing edges above
/// everything in the window), each pane's zoom (its content becomes a
/// scrolling viewport over a zoom root holding the views), and the
/// references the keys read (Accept, Deny). Notes and Settings get the app. It runs on the freshly built app
/// each time (the app's partial rebuilds the window), so it is idempotent;
/// every reference it wires is checked (Wire, audit R6-004) and every part
/// it looks up by path logs an error when missing. Part of
/// <see cref="OfficeSceneUIBuilder"/>; Build() calls it after the window
/// stack is built.
/// </summary>
public static partial class OfficeSceneUIBuilder
{

    /// <summary>The card's keys column width (what they do takes the rest; a row is as tall as its text).</summary>
    private const float CardKeysWidth = 300f;

    /// <summary>The search field's chip width (at the field's right end).</summary>
    private const float SearchChipWidth = 420f;

    /// <summary>
    /// The F1 card (KB1, §3.4): a desktop window of DesktopConfigSO's card size
    /// whose body is a scrolling list of rows (the keys, bold, then what they
    /// do), filled at runtime from the one shortcut table. Rebuilt fresh.
    /// </summary>
    private static DesktopWindow BuildShortcutCard(Transform windowLayer)
    {
        DesktopConfigSO config = EnsureDesktopConfig();
        DestroyChildIfPresent(windowLayer, "ShortcutsWindow");
        DesktopWindow card = BuildOSWindow(windowLayer, "ShortcutsWindow", "window.shortcuts", null, null, config.shortcutCardSize);
        Transform win = card.transform;
        Object.DestroyImmediate(win.Find("Body").gameObject);

        float top = 1f - (config.titleBarHeight + 8f) / config.shortcutCardSize.y;
        RectTransform rows = BuildScrollList(win, "Rows", new Vector2(0.03f, 0.02f), new Vector2(0.97f, top), 2f);
        var row = (RectTransform)Panel(rows, "RowTemplate", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        HorizontalLayoutGroup line = GetOrAdd<HorizontalLayoutGroup>(row.gameObject);
        line.padding = new RectOffset(6, 6, 5, 5);
        line.spacing = 12f;
        line.childAlignment = TextAnchor.UpperLeft;
        line.childControlWidth = true;
        line.childControlHeight = true;
        line.childForceExpandWidth = false;
        line.childForceExpandHeight = false;
        TMP_Text keys = Text(row, "Keys", "", PcType.Body, TextAlignmentOptions.TopLeft, Vector2.zero, Vector2.one, Ink, ThemeRoleId.InputField, style: FontStyles.Bold);
        Chrome(keys, PcType.Body, true);
        keys.raycastTarget = false;
        LayoutElement keysSize = GetOrAdd<LayoutElement>(keys.gameObject);
        keysSize.minWidth = CardKeysWidth;
        keysSize.preferredWidth = CardKeysWidth;
        keysSize.flexibleWidth = 0f;
        TMP_Text what = Text(row, "Text", "", PcType.Body, TextAlignmentOptions.TopLeft, Vector2.zero, Vector2.one, Ink, ThemeRoleId.InputField);
        Chrome(what, PcType.Body, true);
        what.raycastTarget = false;
        LayoutElement whatSize = GetOrAdd<LayoutElement>(what.gameObject);
        whatSize.preferredWidth = 1f;
        whatSize.flexibleWidth = 1f;
        row.gameObject.SetActive(false);

        ShortcutCard component = GetOrAdd<ShortcutCard>(win.gameObject);
        var so = new SerializedObject(component);
        Wire(so, "rowsRoot", rows);
        Wire(so, "rowTemplate", row);
        so.ApplyModifiedProperties();
        return card;
    }

    /// <summary>
    /// The desktop's keyboard poller and the app's keys, clipboard, pins and
    /// zoom (the class summary); the office's back-out (OfficeControls) defers
    /// to the poller's stamp.
    /// </summary>
    private static void BuildDesktopKeys(Canvas canvas, AppParts app, DesktopIcons icons)
    {
        Transform root = canvas.transform;
        DesktopConfigSO config = EnsureDesktopConfig();
        Transform windowLayer = app.Window.transform.parent;
        DesktopWindow card = Need(windowLayer, "ShortcutsWindow")?.GetComponent<DesktopWindow>();
        DesktopContextMenu menu = Need(root, "ContextMenu")?.GetComponent<DesktopContextMenu>();
        NotesWindow notes = root.GetComponentInChildren<NotesWindow>(true);
        OrdersWindow orders = root.GetComponentInChildren<OrdersWindow>(true);

        DesktopKeyboard keyboard = GetOrAdd<DesktopKeyboard>(root.gameObject);
        var so = new SerializedObject(keyboard);
        Wire(so, "raycaster", canvas.GetComponent<GraphicRaycaster>());
        Wire(so, "manager", root.GetComponent<DesktopWindowManager>());
        Wire(so, "icons", icons);
        Wire(so, "shell", root.GetComponent<DesktopShell>());
        Wire(so, "contextMenu", menu);
        Wire(so, "app", app.App);
        Wire(so, "notes", notes);
        Wire(so, "orders", orders);
        Wire(so, "card", card);
        so.ApplyModifiedProperties();

        if (menu != null)
            BuildRowMenuEntries(menu);
        BuildAppKeys(app, menu, config);

        if (notes != null)
        {
            var soNotes = new SerializedObject(notes);
            Wire(soNotes, "app", app.App);
            soNotes.ApplyModifiedProperties();
        }
        SettingsWindowController settings = root.GetComponentInChildren<SettingsWindowController>(true);
        if (settings != null)
        {
            var soSettings = new SerializedObject(settings);
            Wire(soSettings, "app", app.App);
            soSettings.ApplyModifiedProperties();
        }
    }

    /// <summary>The context menu's row entries (CP1, PR1, §3.2), after the desktop's and the icon's.</summary>
    private static void BuildRowMenuEntries(DesktopContextMenu menu)
    {
        Transform panel = menu.transform;
        string[] names = { "CopyEntry", "CopyRowEntry", "PinEntry", "PickEntry" };
        string[] keys = { "menu.copy", "menu.copyRow", "menu.pin", "menu.pick" };
        string[] props = { "copyEntry", "copyRowEntry", "pinEntry", "pickEntry" };
        var so = new SerializedObject(menu);
        for (int i = 0; i < names.Length; i++)
        {
            Wire(so, props[i], MenuEntry(panel, names[i], keys[i], ContextMenuEntry.y));
        }
        so.ApplyModifiedProperties();
    }

    /// <summary>The app's parts for the keys (the class summary), wired into the app.</summary>
    private static void BuildAppKeys(AppParts app, DesktopContextMenu menu, DesktopConfigSO config)
    {
        Transform win = app.Window.transform;

        TMP_InputField search = Need(win, "AppBody/SearchDrawer/Panel/SearchField")?.GetComponent<TMP_InputField>();
        SearchFieldChip chip = search != null ? BuildSearchChip(search, app.App) : null;

        Transform quick = Need(win, "AppBody/SearchDrawer/Panel/QuickOpen");
        SidebarEntryList pins = quick != null ? BuildQuickOpenList(quick, app.App, "Pinned", "app.sidebar.pinned", "app.pins.empty", 0.52f, 1f) : null;
        SidebarEntryList recent = quick != null ? BuildQuickOpenList(quick, app.App, "Recent", "app.sidebar.recent", "app.recent.empty", 0.02f, 0.49f) : null;

        var zooms = new List<Object>();
        foreach (AppPane pane in win.GetComponentsInChildren<AppPane>(true))
            zooms.Add(BuildPaneZoom(pane));

        AppFocusRing ring = BuildFocusRing(win, config);

        var so = new SerializedObject(app.App);
        Wire(so, "searchField", search);
        Wire(so, "searchChip", chip);
        Wire(so, "pinsList", pins);
        Wire(so, "recentList", recent);
        Wire(so, "focusRing", ring);
        SerializedArrays.Set(so, "zooms", zooms);
        Wire(so, "rowMenu", menu);
        so.ApplyModifiedProperties();
    }

    /// <summary>The search field's chip (CP3): a plate at the field's right end with its text and an X, hidden.</summary>
    private static SearchFieldChip BuildSearchChip(TMP_InputField search, InvestigationApp app)
    {
        Transform box = search.transform;
        Transform plate = Panel(box, "Chip", new Vector2(1f, 0.1f), new Vector2(1f, 0.9f), new Vector2(-SearchChipWidth / 2f - 4f, 0f), new Vector2(SearchChipWidth, 0f),
                                XpFace, ThemeRoleId.Button);
        TMP_Text label = Text(plate, "Label", "", PcType.Caption, TextAlignmentOptions.MidlineLeft, Vector2.zero, new Vector2(0.7f, 1f), Ink, ThemeRoleId.Button, fit: true);
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Ellipsis;
        label.margin = new Vector4(8f, 0f, 4f, 0f);
        label.raycastTarget = false;
        Button remove = MakeButton(plate, "RemoveButton", null, new Vector2(0.72f, 0.1f), new Vector2(0.98f, 0.9f), null, ThemeRoleId.Button, "notes.remove");
        SetAnchors(remove.transform, new Vector2(0.72f, 0.1f), new Vector2(0.98f, 0.9f));
        ButtonLabel(remove, PcType.Caption, TextAlignmentOptions.Center, 2f);
        plate.gameObject.SetActive(false);

        SearchFieldChip chip = GetOrAdd<SearchFieldChip>(box.gameObject);
        var so = new SerializedObject(chip);
        Wire(so, "field", search);
        Wire(so, "chip", plate.gameObject);
        Wire(so, "chipLabel", label);
        Wire(so, "removeButton", remove);
        Wire(so, "app", app);
        so.ApplyModifiedProperties();
        return chip;
    }

    /// <summary>
    /// A list of the search drawer's quick-open panel (IA9; PR1, PR2), between
    /// <paramref name="bottom"/> and <paramref name="top"/> of the panel: its
    /// heading (<paramref name="headingKey"/>), a scrolling list of jump rows
    /// and its empty hint (<paramref name="hintKey"/>).
    /// </summary>
    private static SidebarEntryList BuildQuickOpenList(Transform panel, InvestigationApp app, string section, string headingKey, string hintKey,
                                                       float bottom, float top)
    {
        TMP_Text heading = Text(panel, section + "Heading", null, PcType.Caption, TextAlignmentOptions.BottomLeft, new Vector2(0f, top), new Vector2(1f, top),
                                WbMuted, ThemeRoleId.SurfaceMuted, headingKey, FontStyles.Bold, ThemeTextKind.Heading, true);
        PlaceRect(heading.transform, new Vector2(0f, top), new Vector2(1f, top), new Vector2(PcSize.L, -40f), new Vector2(-PcSize.L, 0f));
        heading.raycastTarget = false;
        RectTransform rows = BuildScrollList(panel, section + "List", new Vector2(0f, bottom), new Vector2(1f, top), 2f, WbSurface, ThemeRoleId.Surface);
        Transform box = rows.parent.parent;
        PlaceRect(box, new Vector2(0f, bottom), new Vector2(1f, top), new Vector2(PcSize.S, 0f), new Vector2(-PcSize.S, -44f));
        TMP_Text hint = Text(box, "EmptyHint", null, PcType.Caption, TextAlignmentOptions.TopLeft, Vector2.zero, Vector2.one, WbMuted,
                             ThemeRoleId.SurfaceMuted, hintKey, FontStyles.Normal, ThemeTextKind.Body);
        PlaceRect(hint.transform, Vector2.zero, Vector2.one, new Vector2(PcSize.M, PcSize.S), new Vector2(-PcSize.M, -PcSize.S));
        Chrome(hint, PcType.Caption, true);
        hint.raycastTarget = false;

        Button row = BuildListRow(rows, "EntryTemplate");
        TMP_Text label = row.transform.Find("Label").GetComponent<TMP_Text>();
        SidebarEntryRow entry = GetOrAdd<SidebarEntryRow>(row.gameObject);
        var soRow = new SerializedObject(entry);
        Wire(soRow, "button", row);
        Wire(soRow, "label", label);
        soRow.ApplyModifiedProperties();
        row.gameObject.SetActive(false);

        SidebarEntryList list = GetOrAdd<SidebarEntryList>(box.gameObject);
        var so = new SerializedObject(list);
        Wire(so, "app", app);
        Wire(so, "rowsRoot", rows);
        Wire(so, "rowTemplate", entry);
        Wire(so, "emptyHint", hint.gameObject);
        so.ApplyModifiedProperties();
        return list;
    }

    /// <summary>
    /// A pane's zoom (KB5): its content becomes the viewport of a ScrollRect
    /// whose content is a zoom root (stretched, pivoted at the top-left)
    /// holding every view and the no-case state, in their order.
    /// </summary>
    private static PaneZoom BuildPaneZoom(AppPane pane)
    {
        Transform content = Need(pane.transform, "Content");
        if (content == null)
            return null;
        var zoom = (RectTransform)Panel(content, "Zoom", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        zoom.pivot = new Vector2(0f, 1f);
        zoom.offsetMin = Vector2.zero;
        zoom.offsetMax = Vector2.zero;
        var children = new List<Transform>();
        foreach (Transform child in content)
            if (child != zoom)
                children.Add(child);
        foreach (Transform child in children)
            child.SetParent(zoom, false);

        ScrollRect scroll = GetOrAdd<ScrollRect>(content.gameObject);
        scroll.content = zoom;
        scroll.viewport = (RectTransform)content;
        scroll.horizontal = false;
        scroll.vertical = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.inertia = false;
        scroll.scrollSensitivity = 30f;

        PaneZoom paneZoom = GetOrAdd<PaneZoom>(content.gameObject);
        var so = new SerializedObject(paneZoom);
        Wire(so, "scroll", scroll);
        Wire(so, "zoomRoot", zoom);
        so.ApplyModifiedProperties();
        return paneZoom;
    }

    /// <summary>The focus ring (KB4): four FocusRing edges of DesktopConfigSO's width just outside its rect, last in the window (above everything), taking no raycasts, hidden.</summary>
    private static AppFocusRing BuildFocusRing(Transform win, DesktopConfigSO config)
    {
        DestroyChildIfPresent(win, "FocusRing");
        Transform ring = Panel(win, "FocusRing", Center, Center, Vector2.zero, new Vector2(100f, 40f), null);
        float w = config.focusRingWidth;
        var accent = new Color(0.95f, 0.6f, 0.1f, 1f);
        Edge(ring, "Top", new Vector2(0f, 1f), Vector2.one, new Vector2(0.5f, 0f), new Vector2(2f * w, w), accent);
        Edge(ring, "Bottom", Vector2.zero, new Vector2(1f, 0f), new Vector2(0.5f, 1f), new Vector2(2f * w, w), accent);
        Edge(ring, "Left", Vector2.zero, new Vector2(0f, 1f), new Vector2(1f, 0.5f), new Vector2(w, 0f), accent);
        Edge(ring, "Right", new Vector2(1f, 0f), Vector2.one, new Vector2(0f, 0.5f), new Vector2(w, 0f), accent);
        ring.SetAsLastSibling();
        CanvasGroup group = GetOrAdd<CanvasGroup>(ring.gameObject);
        group.interactable = false;
        group.blocksRaycasts = false;
        AppFocusRing component = GetOrAdd<AppFocusRing>(ring.gameObject);
        var so = new SerializedObject(component);
        Wire(so, "group", group);
        so.ApplyModifiedProperties();
        ring.gameObject.SetActive(false);
        return component;
    }

    /// <summary>One edge of the focus ring.</summary>
    private static void Edge(Transform ring, string name, Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 size, Color colour)
    {
        var edge = (RectTransform)Panel(ring, name, aMin, aMax, Vector2.zero, size, colour, ThemeRoleId.FocusRing);
        edge.pivot = pivot;
        edge.anchoredPosition = Vector2.zero;
        edge.GetComponent<Image>().raycastTarget = false;
    }

    /// <summary>The child at <paramref name="path"/>, or null with an error (a part renamed elsewhere is never skipped silently).</summary>
    private static Transform Need(Transform parent, string path)
    {
        Transform found = parent != null ? parent.Find(path) : null;
        if (found == null)
            Debug.LogError($"[TimeDesk] The keys' builder did not find '{path}' under '{(parent != null ? parent.name : "null")}'; fix OfficeSceneUIBuilder.Keys.");
        return found;
    }
}
