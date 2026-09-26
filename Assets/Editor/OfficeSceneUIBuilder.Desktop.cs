using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The office builder's desktop (the PC redesign DK1-DK8, DK10, TH2-TH4;
/// plan phase 17): the six icons on their own layer (the icon area above the
/// compare dock and the taskbar, under the case chrome and every window;
/// built in the default arrangement, the player's layout is restored at
/// runtime), the desktop's context menu, the Start menu (the six apps,
/// Arrange icons, Turn off screen, Quit game), and phase 25's DesktopApps,
/// the one OpenApp(id) entry point, with each app's window registered under
/// its DesktopAppIds id. Until the Investigation app exists (phase 16), the
/// Investigation icon opens an interim window holding today's case tiles
/// (Directives, the Deviation Report, Records, the Clue Log, the reference
/// books and the case's scanned documents). Also the canvas's layer order
/// and the check that every keyed label is in the reading table (audit
/// R6-023). Part of <see cref="OfficeSceneUIBuilder"/>; Build() calls these
/// in its order.
/// </summary>
public static partial class OfficeSceneUIBuilder
{
    /// <summary>Each app's desktop label (a Flavour string, TH3), by id.</summary>
    private static readonly Dictionary<string, string> AppLabelKeys = new Dictionary<string, string>
    {
        { DesktopAppIds.Investigation, "icon.investigation" },
        { DesktopAppIds.Internet, "icon.internet" },
        { DesktopAppIds.Mail, "icon.mail" },
        { DesktopAppIds.CitizenAccount, "icon.account" },
        { DesktopAppIds.Notes, "icon.notes" },
        { DesktopAppIds.Settings, "icon.settings" },
    };

    /// <summary>The Start menu's entry height (desktop units).</summary>
    private const float StartMenuEntryHeight = 40f;

    /// <summary>The gap between Start menu entries.</summary>
    private const float StartMenuSpacing = 4f;

    /// <summary>The context menu's width and entry height.</summary>
    private static readonly Vector2 ContextMenuEntry = new Vector2(240f, 40f);

    /// <summary>The interim Investigation window's size and its tiles' cell (three columns).</summary>
    private static readonly Vector2 InvestigationWindowSize = new Vector2(480f, 520f);

    /// <summary>A case tile's cell in the interim Investigation window.</summary>
    private static readonly Vector2 CaseTileCell = new Vector2(146f, 58f);

    /// <summary>
    /// The window layer: every desktop window's parent, on the investigation
    /// host after the case root (never toggled: an app opens between
    /// travellers too), the icon area exactly (the desktop above the compare
    /// dock and the taskbar, so no window covers them and a maximised one
    /// fills it), masked to it. A layer an older build put under the case root
    /// is moved out with its windows.
    /// </summary>
    private static Transform EnsureWindowLayer(Transform investHost, Transform caseRoot)
    {
        MoveChildIfPresent(caseRoot, "WindowLayer", investHost);
        Transform layer = Panel(investHost, "WindowLayer", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        PlaceIconArea(layer);
        GetOrAdd<RectMask2D>(layer.gameObject);
        return layer;
    }

    /// <summary>Stretches a rect over the icon area: the whole desktop but the taskbar and the dock at its bottom.</summary>
    private static void PlaceIconArea(Transform t)
    {
        var rt = (RectTransform)t;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = Center;
        rt.offsetMin = new Vector2(0f, EnsureDesktopConfig().MaximisedBottom);
        rt.offsetMax = Vector2.zero;
    }

    /// <summary>
    /// The case root (shown while a traveller is at the desk): the claim, Accept
    /// and Deny and the compare dock. Its old dim (the retired DeskDim role, TH2)
    /// goes, so the icons and the wallpaper show around the case chrome.
    /// </summary>
    private static Transform EnsureCaseRoot(Transform investHost)
    {
        Transform caseRoot = Panel(investHost, "InvestigationRoot", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        ThemeTag dimTag = caseRoot.GetComponent<ThemeTag>();
        if (dimTag != null)
            Object.DestroyImmediate(dimTag);
        Image dim = caseRoot.GetComponent<Image>();
        if (dim != null)
            Object.DestroyImmediate(dim);
        return caseRoot;
    }

    /// <summary>
    /// The interim Investigation window (until phase 16's app): a desktop
    /// window titled "Investigation" whose body is a grid of case tiles (three
    /// columns). The builder puts the day's windows there (Directives, the
    /// Deviation Report, Records, the Clue Log: BuildCaseTile); the
    /// investigation controller adds the reference books and each scanned
    /// document at runtime by cloning <paramref name="tileTemplate"/>. Returns the grid.
    /// </summary>
    private static Transform BuildInvestigationWindow(Transform windowLayer, out DesktopWindow window, out Button tileTemplate)
    {
        DestroyChildIfPresent(windowLayer, "InvestigationWindow");
        window = BuildOSWindow(windowLayer, "InvestigationWindow", "window.investigation", null, string.Empty, InvestigationWindowSize);
        Transform win = window.transform;
        float top = EnsureDesktopConfig().titleBarHeight;

        Transform grid = Panel(win, "CaseTiles", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        PlaceRect(grid, Vector2.zero, Vector2.one, new Vector2(8f, 8f), new Vector2(-8f, -(top + 8f)));
        AddGridLayout(grid, CaseTileCell, new Vector2(6f, 6f));
        grid.GetComponent<GridLayoutGroup>().constraintCount = 3;

        tileTemplate = MakeButton(grid, "CaseTileTemplate", "Book", Vector2.zero, Vector2.one, null, ThemeRoleId.Button);
        FitIconLabel(tileTemplate);
        tileTemplate.gameObject.SetActive(false);
        return grid;
    }

    /// <summary>A case tile in the interim Investigation window: its keyed label (Full tier) opens its window (a persistent call to DesktopWindow.Open).</summary>
    private static void BuildCaseTile(Transform grid, string name, string labelKey, DesktopWindow window)
    {
        Button tile = MakeButton(grid, name, null, Vector2.zero, Vector2.one, null, ThemeRoleId.Button, labelKey);
        Transform label = tile.transform.Find("Label");
        if (label != null)
        {
            TMP_Text text = label.GetComponent<TMP_Text>();
            text.text = UiText.Get(labelKey);
            // Tile labels wrap between words and shrink (FitIconLabel), not the one-line fit.
            Tag(text, ThemeRoleId.Button, ThemePart.Ink, labelKey, FontStyles.Normal, ThemeTextKind.Button, false);
        }
        FitIconLabel(tile);
        WirePersistentVoid(tile, "m_OnClick", window, nameof(DesktopWindow.Open));
    }

    /// <summary>Gives the Settings window the desktop's icons (Reset icon positions arranges them).</summary>
    private static void WireIconSettings(DesktopWindow settings, DesktopIcons icons)
    {
        var so = new SerializedObject(settings.GetComponent<SettingsWindowController>());
        SetRef(so, "icons", icons);
        so.ApplyModifiedProperties();
    }

    /// <summary>
    /// The desktop's apps and icons: DesktopApps on the canvas (phase 25's
    /// registry: each app's window by id, and the Start menu's shell it
    /// closes), the icon layer (a transparent catcher over the icon area,
    /// DesktopIcons, whose Mail badge reads <paramref name="mail"/>'s unread
    /// count) with one icon per id in the default arrangement, and the
    /// context menu. Every icon is rebuilt each run. Returns the icons.
    /// </summary>
    private static DesktopIcons BuildDesktopIcons(Canvas canvas, IReadOnlyDictionary<string, DesktopWindow> windows, DeskController desk, MailFeed mail,
                                                  out DesktopContextMenu contextMenu)
    {
        Transform root = canvas.transform;
        DesktopConfigSO config = EnsureDesktopConfig();

        DesktopApps apps = GetOrAdd<DesktopApps>(root.gameObject);
        var soApps = new SerializedObject(apps);
        SerializedProperty list = soApps.FindProperty("apps");
        list.arraySize = config.iconOrder.Length;
        for (int i = 0; i < config.iconOrder.Length; i++)
        {
            string id = config.iconOrder[i];
            if (!windows.TryGetValue(id, out DesktopWindow window) || window == null)
                Debug.LogError($"[TimeDesk] The desktop app '{id}' has no window: its icon would open nothing. Register it in BuildDesktopShell.");
            SerializedProperty entry = list.GetArrayElementAtIndex(i);
            entry.FindPropertyRelative("id").stringValue = id;
            entry.FindPropertyRelative("window").objectReferenceValue = window;
        }
        SetRef(soApps, "shell", GetOrAdd<DesktopShell>(root.gameObject));
        soApps.ApplyModifiedProperties();

        // The icon layer: the icon area, a transparent catcher (a press deselects, a right-click opens the menu).
        DestroyChildIfPresent(root, "DesktopIcons");
        Transform layer = Panel(root, "DesktopIcons", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(1f, 1f, 1f, 0f), ThemeRoleId.ClickCatcher);
        PlaceIconArea(layer);
        DesktopIcons board = layer.gameObject.AddComponent<DesktopIcons>();

        IReadOnlyList<IconPlace> places = DesktopLayout.Arrange(config.iconOrder, new IconGrid(DesktopSize.x, DesktopSize.y - config.MaximisedBottom,
            config.iconCellSize.x, config.iconCellSize.y, config.iconOrigin.x, config.iconOrigin.y, config.iconColumnStep, config.iconRowStep));
        var views = new List<Object>();
        foreach (IconPlace place in places)
            views.Add(BuildIcon(layer, place, board, config));

        contextMenu = BuildContextMenu(root, board);

        var so = new SerializedObject(board);
        SetRef(so, "config", config);
        SetRef(so, "apps", apps);
        SetRef(so, "manager", GetOrAdd<DesktopWindowManager>(root.gameObject));
        SetRef(so, "contextMenu", contextMenu);
        SetRef(so, "raycaster", canvas.GetComponent<GraphicRaycaster>());
        SetRef(so, "desk", desk);
        SetRef(so, "mail", mail);
        SetRef(so, "investigationWindow", windows.TryGetValue(DesktopAppIds.Investigation, out DesktopWindow investigation) ? investigation : null);
        SerializedArrays.Set(so, "icons", views.ToArray());
        so.ApplyModifiedProperties();
        return board;
    }

    /// <summary>
    /// One icon cell (DK2): a transparent hit area, the selection plate
    /// (IconSelection), the glyph on its plate (the DesktopIcon role; no art
    /// yet, so the runtime draws the placeholder glyph), the label (its
    /// Flavour key, at most two lines, shrinking) on its plate, and the badge
    /// (a Badge circle with its count) at the glyph's top right.
    /// </summary>
    private static DesktopIconView BuildIcon(Transform layer, IconPlace place, DesktopIcons board, DesktopConfigSO config)
    {
        string labelKey = AppLabelKeys.TryGetValue(place.Id, out string key) ? key : "icon." + place.Id;
        Transform cell = Panel(layer, "Icon_" + place.Id, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(place.X, -place.Y), config.iconCellSize, null);
        ((RectTransform)cell).pivot = new Vector2(0f, 1f);

        Panel(cell, "Hit", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(1f, 1f, 1f, 0f), ThemeRoleId.ClickCatcher);

        Transform selection = Panel(cell, "Selection", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.2f, 0.5f, 0.2f, 0.45f), ThemeRoleId.IconSelection);
        selection.GetComponent<Image>().raycastTarget = false;
        selection.gameObject.SetActive(false);

        float glyphSize = config.iconGlyphSize;
        Transform glyphPlate = Panel(cell, "GlyphPlate", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -glyphSize / 2f - 2f), new Vector2(glyphSize, glyphSize),
                                     new Color(0.2f, 0.3f, 0.45f, 0.85f), ThemeRoleId.DesktopIcon);
        glyphPlate.GetComponent<Image>().raycastTarget = false;
        Transform glyph = Panel(glyphPlate, "Glyph", Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-14f, -14f), Color.white);
        Image glyphImage = glyph.GetComponent<Image>();
        glyphImage.raycastTarget = false;
        glyphImage.preserveAspect = true;
        Tag(glyphImage, ThemeRoleId.DesktopIcon, ThemePart.Ink);

        float labelHeight = config.iconCellSize.y - glyphSize - 6f;
        Transform labelPlate = Panel(cell, "LabelPlate", Vector2.zero, new Vector2(1f, 0f), new Vector2(0f, labelHeight / 2f), new Vector2(0f, labelHeight),
                                     new Color(0.2f, 0.3f, 0.45f, 0.85f), ThemeRoleId.DesktopIcon);
        labelPlate.GetComponent<Image>().raycastTarget = false;
        TMP_Text label = Text(labelPlate, "Label", null, Mathf.RoundToInt(config.iconLabelSize), TextAlignmentOptions.Center, Vector2.zero, Vector2.one, Color.white,
                              ThemeRoleId.DesktopIcon, labelKey, FontStyles.Normal, ThemeTextKind.Button, false);
        label.raycastTarget = false;
        label.enableAutoSizing = true;
        label.fontSizeMax = config.iconLabelSize;
        label.fontSizeMin = IconLabelMinSize;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.overflowMode = TextOverflowModes.Ellipsis;
        label.maxVisibleLines = 2;
        label.margin = new Vector4(3f, 1f, 3f, 1f);

        float badgeSize = config.iconBadgeSize;
        Transform badge = Panel(cell, "Badge", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(glyphSize / 2f, -4f), new Vector2(badgeSize, badgeSize),
                                new Color(0.2f, 0.5f, 0.2f, 1f), ThemeRoleId.Badge);
        Image badgeImage = badge.GetComponent<Image>();
        badgeImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        badgeImage.raycastTarget = false;
        TMP_Text badgeText = Text(badge, "Count", string.Empty, 16, TextAlignmentOptions.Center, Vector2.zero, Vector2.one, Color.white, ThemeRoleId.Badge,
                                  null, FontStyles.Bold);
        badgeText.raycastTarget = false;
        badge.gameObject.SetActive(false);

        DesktopIconView view = cell.gameObject.AddComponent<DesktopIconView>();
        var so = new SerializedObject(view);
        so.FindProperty("appId").stringValue = place.Id;
        SetRef(so, "board", board);
        SetRef(so, "glyph", glyphImage);
        SetRef(so, "selection", selection.gameObject);
        SetRef(so, "badge", badge.gameObject);
        SetRef(so, "badgeText", badgeText);
        so.ApplyModifiedProperties();
        return view;
    }

    /// <summary>The desktop's context menu (DK5, TH4): a Start-menu-styled panel whose entries (Arrange icons; Open) show per target; hidden.</summary>
    private static DesktopContextMenu BuildContextMenu(Transform root, DesktopIcons icons)
    {
        DestroyChildIfPresent(root, "ContextMenu");
        Transform menu = Panel(root, "ContextMenu", Center, Center, Vector2.zero, new Vector2(ContextMenuEntry.x, ContextMenuEntry.y + 2f * VLayoutPadding),
                               new Color(0.1f, 0.12f, 0.18f, 0.97f), ThemeRoleId.StartMenu);
        ((RectTransform)menu).pivot = new Vector2(0f, 1f);
        AddVLayout(menu, StartMenuSpacing);
        ContentSizeFitter fit = GetOrAdd<ContentSizeFitter>(menu.gameObject);
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        Button arrange = MakeButton(menu, "ArrangeEntry", null, Vector2.zero, Vector2.one, new Color(0.2f, 0.25f, 0.35f, 1f), ThemeRoleId.MenuEntry, "desktop.arrange");
        SetLayoutHeight(arrange, ContextMenuEntry.y);
        Button open = MakeButton(menu, "OpenEntry", null, Vector2.zero, Vector2.one, new Color(0.2f, 0.25f, 0.35f, 1f), ThemeRoleId.MenuEntry, "menu.open");
        SetLayoutHeight(open, ContextMenuEntry.y);

        DesktopContextMenu contextMenu = menu.gameObject.AddComponent<DesktopContextMenu>();
        var so = new SerializedObject(contextMenu);
        SetRef(so, "arrangeEntry", arrange);
        SetRef(so, "openEntry", open);
        SetRef(so, "icons", icons);
        so.ApplyModifiedProperties();
        menu.gameObject.SetActive(false);
        return contextMenu;
    }

    /// <summary>
    /// The Start menu (DK8), rebuilt each run above the dock: an entry per app
    /// in the default order, each a persistent call to DesktopApps.OpenApp
    /// with its id (which closes the menu), labelled as phase 25 labels Mail,
    /// Citizen Account and Notes (the Mail feed writes its unread count into
    /// the Mail entry, returned in <paramref name="mailLabel"/>), the other
    /// apps by their desktop label and Settings by its Start-menu one; then
    /// Arrange icons, Turn off screen and Quit game, each 40 units tall in a
    /// vertical layout. Returns the menu.
    /// </summary>
    private static Transform BuildStartMenu(Transform root, DesktopApps apps, out TMP_Text mailLabel, out Button arrange, out Button screenOff, out Button quit)
    {
        DesktopConfigSO config = EnsureDesktopConfig();
        int count = config.iconOrder.Length + 3;
        float height = count * StartMenuEntryHeight + (count - 1) * StartMenuSpacing + 2f * VLayoutPadding;

        DestroyChildIfPresent(root, "StartMenu");
        Transform startMenu = Panel(root, "StartMenu", new Vector2(0f, 0f), new Vector2(0.2f, 0f), new Vector2(0f, StartMenuCentre(height)), new Vector2(0f, height),
                                    new Color(0.1f, 0.12f, 0.18f, 0.97f), ThemeRoleId.StartMenu);
        AddVLayout(startMenu, StartMenuSpacing);
        mailLabel = null;
        foreach (string id in config.iconOrder)
        {
            Button entry = MakeButton(startMenu, "App_" + id, null, Vector2.zero, Vector2.one, new Color(0.2f, 0.25f, 0.35f, 1f), ThemeRoleId.MenuEntry, StartMenuLabelKey(id));
            SetLayoutHeight(entry, StartMenuEntryHeight);
            WirePersistentString(entry, "m_OnClick", apps, nameof(DesktopApps.OpenApp), id);
            if (id == DesktopAppIds.Mail)
                mailLabel = entry.transform.Find("Label").GetComponent<TMP_Text>();
        }
        arrange = MakeButton(startMenu, "ArrangeEntry", null, Vector2.zero, Vector2.one, new Color(0.2f, 0.25f, 0.35f, 1f), ThemeRoleId.MenuEntry, "desktop.arrange");
        SetLayoutHeight(arrange, StartMenuEntryHeight);
        screenOff = MakeButton(startMenu, "ScreenOffEntry", null, Vector2.zero, Vector2.one, new Color(0.2f, 0.25f, 0.35f, 1f), ThemeRoleId.MenuEntry, "startmenu.screenOff");
        SetLayoutHeight(screenOff, StartMenuEntryHeight);
        quit = MakeButton(startMenu, "QuitEntry", null, Vector2.zero, Vector2.one, new Color(0.5f, 0.2f, 0.2f, 1f), ThemeRoleId.QuitEntry, "startmenu.quit");
        SetLayoutHeight(quit, StartMenuEntryHeight);
        startMenu.gameObject.SetActive(false);
        return startMenu;
    }

    /// <summary>An app's Start menu label: phase 25's for Mail, Citizen Account and Notes, the Start menu's own for Settings, else the desktop label.</summary>
    private static string StartMenuLabelKey(string id)
    {
        switch (id)
        {
            case DesktopAppIds.Mail: return "startmenu.mail";
            case DesktopAppIds.CitizenAccount: return "startmenu.account";
            case DesktopAppIds.Notes: return "startmenu.notes";
            case DesktopAppIds.Settings: return "startmenu.settings";
            default: return AppLabelKeys[id];
        }
    }

    /// <summary>
    /// The desktop canvas's layers, bottom to top: the wallpaper, the idle
    /// line, the icons, the gameplay hosts (the investigation host holds the
    /// case chrome, the window layer and the compare dock), the taskbar, the
    /// context menu, the Start menu.
    /// </summary>
    private static void OrderDesktopLayers(Transform root)
    {
        Transform idle = root.Find("IdleScreen");
        Transform icons = root.Find("DesktopIcons");
        if (idle != null && icons != null)
            icons.SetSiblingIndex(idle.GetSiblingIndex() + 1);
        foreach (string top in new[] { "Taskbar", "ContextMenu", "StartMenu" })
        {
            Transform t = root.Find(top);
            if (t != null)
                t.SetAsLastSibling();
        }
    }

    /// <summary>Logs an error for every keyed label on the canvases whose key the reading table lacks (audit R6-023), and for any graphic still in a retired role (TH2).</summary>
    private static void CheckLabelKeysAndRoles(ContentLibrarySO library, params Canvas[] canvases)
    {
        UiStringTableSO reading = library != null ? library.GetStringTable(library.CultureUi.readingLanguage) : null;
        var keys = new HashSet<string>();
        if (reading != null)
            foreach (UiStringEntry e in reading.entries)
                keys.Add(e.key);

        foreach (Canvas canvas in canvases)
            foreach (ThemeTag tag in canvas.GetComponentsInChildren<ThemeTag>(true))
            {
                if (ThemeRoles.IsRetired(tag.Role))
                    Debug.LogError($"[TimeDesk] '{PathOf(tag.transform)}' takes the retired role {tag.Role}; give it a live one in OfficeSceneUIBuilder.", tag);
                if (reading != null && !string.IsNullOrEmpty(tag.LabelKey) && !keys.Contains(tag.LabelKey))
                    Debug.LogError($"[TimeDesk] '{PathOf(tag.transform)}' is keyed '{tag.LabelKey}', which world_source.json ui.strings lacks; add it and run Tools > TimeDesk > Generate World.", tag);
            }
    }
}
