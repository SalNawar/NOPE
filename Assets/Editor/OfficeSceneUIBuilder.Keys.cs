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
/// field made live with its chip for a pasted untranslated line, the Keys
/// button toggling the card, the sidebar's Pinned and Recent lists (their
/// placeholders replaced), the pane header's Pin button, the focus ring
/// (four FocusRing edges above everything in the window), each pane's zoom
/// (its content becomes a scrolling viewport over a zoom root holding the
/// views), and the references the keys read (Accept, Deny, the dock's
/// clear). Notes and Settings get the app. It runs on the freshly built app
/// each time (the app's partial rebuilds the window), so it is idempotent;
/// every reference it wires is checked (Wire, audit R6-004) and every part
/// it looks up by path logs an error when missing. Part of
/// <see cref="OfficeSceneUIBuilder"/>; Build() calls it after the window
/// stack is built.
/// </summary>
public static partial class OfficeSceneUIBuilder
{
    /// <summary>A sidebar row's height.</summary>
    private const float SidebarRowHeight = 34f;

    /// <summary>The card's keys column width (what they do takes the rest; a row is as tall as its text).</summary>
    private const float CardKeysWidth = 250f;

    /// <summary>The pane header's Pin button width (at its right end).</summary>
    private const float PinButtonWidth = 72f;

    /// <summary>The search field's chip width (at the field's right end).</summary>
    private const float SearchChipWidth = 340f;

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
        TMP_Text keys = Text(row, "Keys", "", 18, TextAlignmentOptions.TopLeft, Vector2.zero, Vector2.one, Ink, ThemeRoleId.InputField, style: FontStyles.Bold);
        keys.textWrappingMode = TextWrappingModes.Normal;
        keys.raycastTarget = false;
        LayoutElement keysSize = GetOrAdd<LayoutElement>(keys.gameObject);
        keysSize.minWidth = CardKeysWidth;
        keysSize.preferredWidth = CardKeysWidth;
        keysSize.flexibleWidth = 0f;
        TMP_Text what = Text(row, "Text", "", 18, TextAlignmentOptions.TopLeft, Vector2.zero, Vector2.one, Ink, ThemeRoleId.InputField);
        what.textWrappingMode = TextWrappingModes.Normal;
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
    /// zoom (the class summary); <paramref name="view"/>'s Escape defers to the
    /// poller's stamp.
    /// </summary>
    private static void BuildDesktopKeys(Canvas canvas, OfficeViewController view, AppParts app, DesktopIcons icons)
    {
        Transform root = canvas.transform;
        DesktopConfigSO config = EnsureDesktopConfig();
        Transform windowLayer = app.Window.transform.parent;
        DesktopWindow card = Need(windowLayer, "ShortcutsWindow")?.GetComponent<DesktopWindow>();
        DesktopContextMenu menu = Need(root, "ContextMenu")?.GetComponent<DesktopContextMenu>();
        NotesWindow notes = root.GetComponentInChildren<NotesWindow>(true);

        DesktopKeyboard keyboard = GetOrAdd<DesktopKeyboard>(root.gameObject);
        var so = new SerializedObject(keyboard);
        Wire(so, "raycaster", canvas.GetComponent<GraphicRaycaster>());
        Wire(so, "manager", root.GetComponent<DesktopWindowManager>());
        Wire(so, "icons", icons);
        Wire(so, "shell", root.GetComponent<DesktopShell>());
        Wire(so, "contextMenu", menu);
        Wire(so, "app", app.App);
        Wire(so, "notes", notes);
        Wire(so, "card", card);
        so.ApplyModifiedProperties();

        var soView = new SerializedObject(view);
        Wire(soView, "keyboard", keyboard);
        soView.ApplyModifiedProperties();

        if (menu != null)
            BuildRowMenuEntries(menu);
        BuildAppKeys(app, keyboard, menu, config);

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
            Button entry = MakeButton(panel, names[i], null, Vector2.zero, Vector2.one, new Color(0.2f, 0.25f, 0.35f, 1f), ThemeRoleId.MenuEntry, keys[i]);
            SetLayoutHeight(entry, ContextMenuEntry.y);
            Wire(so, props[i], entry);
        }
        so.ApplyModifiedProperties();
    }

    /// <summary>The app's parts for the keys (the class summary), wired into the app.</summary>
    private static void BuildAppKeys(AppParts app, DesktopKeyboard keyboard, DesktopContextMenu menu, DesktopConfigSO config)
    {
        Transform win = app.Window.transform;

        TMP_InputField search = Need(win, "Toolbar/SearchField")?.GetComponent<TMP_InputField>();
        SearchFieldChip chip = search != null ? BuildSearchChip(search, app.App) : null;
        Button keys = Need(win, "Toolbar/KeysButton")?.GetComponent<Button>();
        if (keys != null)
            WirePersistentVoid(keys, "m_OnClick", keyboard, nameof(DesktopKeyboard.ToggleCard));

        Transform sidebar = Need(win, "AppBody/Sidebar");
        SidebarEntryList pins = sidebar != null ? BuildSidebarList(sidebar, app.App, "Pinned", true, "app.pins.empty", 0.345f, 0.595f) : null;
        SidebarEntryList recent = sidebar != null ? BuildSidebarList(sidebar, app.App, "Recent", false, "app.recent.empty", 0.01f, 0.26f) : null;

        var zooms = new List<Object>();
        Button pinButton = null;
        foreach (AppPane pane in win.GetComponentsInChildren<AppPane>(true))
        {
            zooms.Add(BuildPaneZoom(pane));
            pinButton = BuildPinButton(pane);
        }

        AppFocusRing ring = BuildFocusRing(win, config);
        Transform dockClear = Need(win.parent.parent, "CompareDock/Pair/ClearButton");

        var so = new SerializedObject(app.App);
        Wire(so, "searchField", search);
        Wire(so, "searchChip", chip);
        Wire(so, "sidebar", sidebar != null ? sidebar.gameObject : null);
        Wire(so, "panes", Need(win, "AppBody/Pane"));
        Wire(so, "pinsList", pins);
        Wire(so, "recentList", recent);
        Wire(so, "pinButton", pinButton);
        Wire(so, "focusRing", ring);
        SerializedArrays.Set(so, "zooms", zooms);
        Wire(so, "acceptButton", app.Accept);
        Wire(so, "denyButton", app.Deny);
        Wire(so, "dockClearButton", dockClear != null ? dockClear.GetComponent<Button>() : null);
        Wire(so, "compareDock", Need(win.parent.parent, "CompareDock"));
        Wire(so, "rowMenu", menu);
        so.ApplyModifiedProperties();
    }

    /// <summary>The search field's chip (CP3): a plate at the field's right end with its text and an X, hidden.</summary>
    private static SearchFieldChip BuildSearchChip(TMP_InputField search, InvestigationApp app)
    {
        Transform box = search.transform;
        Transform plate = Panel(box, "Chip", new Vector2(1f, 0.1f), new Vector2(1f, 0.9f), new Vector2(-SearchChipWidth / 2f - 4f, 0f), new Vector2(SearchChipWidth, 0f),
                                XpFace, ThemeRoleId.Button);
        TMP_Text label = Text(plate, "Label", "", 16, TextAlignmentOptions.MidlineLeft, Vector2.zero, new Vector2(0.86f, 1f), Ink, ThemeRoleId.Button, fit: true);
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Ellipsis;
        label.margin = new Vector4(8f, 0f, 4f, 0f);
        label.raycastTarget = false;
        Button remove = MakeButton(plate, "RemoveButton", null, new Vector2(0.87f, 0.1f), new Vector2(0.98f, 0.9f), null, ThemeRoleId.CloseButton, "window.close");
        SetAnchors(remove.transform, new Vector2(0.87f, 0.1f), new Vector2(0.98f, 0.9f));
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

    /// <summary>A sidebar list (PR1, PR2) under its heading, between <paramref name="bottom"/> and <paramref name="top"/>: a scrolling list of jump rows and its empty hint; the section's placeholder goes.</summary>
    private static SidebarEntryList BuildSidebarList(Transform sidebar, InvestigationApp app, string section, bool pins, string hintKey, float bottom, float top)
    {
        DestroyChildIfPresent(sidebar, section + "Empty");
        DestroyChildIfPresent(sidebar, section + "List");
        RectTransform rows = BuildScrollList(sidebar, section + "List", new Vector2(0.04f, bottom), new Vector2(0.96f, top), 2f, XpFace, ThemeRoleId.Sidebar);
        Transform box = rows.parent.parent;
        TMP_Text hint = Text(box, "EmptyHint", null, 16, TextAlignmentOptions.TopLeft, new Vector2(0.04f, 0.05f), new Vector2(0.96f, 0.95f), Ink,
                             ThemeRoleId.Sidebar, hintKey, FontStyles.Italic, ThemeTextKind.Body);
        hint.textWrappingMode = TextWrappingModes.Normal;
        hint.raycastTarget = false;

        Button row = BuildListRow(rows, "EntryTemplate");
        SetLayoutHeight(row, SidebarRowHeight);
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
        so.FindProperty("pins").boolValue = pins;
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

    /// <summary>The pane header's Pin button (PR1: it pins the pane's item), at the header's right end, outside the chip row's layout.</summary>
    private static Button BuildPinButton(AppPane pane)
    {
        Transform header = Need(pane.transform, "PaneHeader");
        if (header == null)
            return null;
        Button pin = MakeButton(header, "PinButton", null, new Vector2(1f, 0.1f), new Vector2(1f, 0.9f), null, ThemeRoleId.Button, "app.pin");
        var rect = (RectTransform)pin.transform;
        rect.anchorMin = new Vector2(1f, 0.1f);
        rect.anchorMax = new Vector2(1f, 0.9f);
        rect.sizeDelta = new Vector2(PinButtonWidth, 0f);
        rect.anchoredPosition = new Vector2(-PinButtonWidth / 2f - 6f, 0f);
        GetOrAdd<LayoutElement>(pin.gameObject).ignoreLayout = true;
        HorizontalLayoutGroup row = header.GetComponent<HorizontalLayoutGroup>();
        if (row != null)
            row.padding = new RectOffset(row.padding.left, (int)PinButtonWidth + 12, row.padding.top, row.padding.bottom);
        return pin;
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
