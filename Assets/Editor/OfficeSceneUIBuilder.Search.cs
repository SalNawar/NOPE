using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The office builder's search drawer (redesign phase 19; the PC spec's SE1,
/// SE4; the PC workbench spec IA9): over the Investigation app's body, a
/// drawer (hidden) whose clear catcher closes it on a press and whose panel
/// comes in from the right: a surface with a hairline at its left holding its
/// title, Close, the hint, the search field (SearchBox) and, under the field,
/// the quick-open panel (the pinned and the recent items before anything is
/// typed: the keys' lists, BuildQuickOpenList) and the results panel (the
/// source chips' row, each "Papers (3)"; the scrolling list with its
/// scrollbar and inactive templates: a group's heading in the source's full
/// name, a hit with its title over its snippet, "Show all"; the line shown
/// when nothing matches and the footer's hint). The shelf's Search button
/// opens it. A hit's row carries phase 18's found mark, as a link's target
/// does. The field's chip for a pasted untranslated line is the keys'
/// (BuildSearchChip). Rebuilt fresh with the app's window on each run; every
/// reference it wires is checked (Wire). Part of
/// <see cref="OfficeSceneUIBuilder"/>; BuildInvestigationApp calls it.
/// </summary>
public static partial class OfficeSceneUIBuilder
{
    /// <summary>The drawer's panel width.</summary>
    private const float DrawerWidth = 580f;

    /// <summary>The field's top under the panel's top, and its height; the lists start under it.</summary>
    private const float DrawerFieldTop = 136f, DrawerField = 52f, DrawerAreaTop = DrawerFieldTop + DrawerField + 16f;

    /// <summary>The results list's scrollbar width (shown only when the groups outgrow the list).</summary>
    private const float SearchScrollbar = 16f;

    /// <summary>The results' header (the chips, two rows) height.</summary>
    private const float SearchHeaderHeight = 104f;

    /// <summary>The results' footer (the hint, two lines) height.</summary>
    private const float SearchFooterHeight = 60f;

    /// <summary>A hit's row height (its title over its snippet).</summary>
    private const float SearchHitHeight = 80f;

    /// <summary>A group heading's height.</summary>
    private const float SearchHeadingHeight = 44f;

    /// <summary>"Show all"'s height.</summary>
    private const float SearchMoreHeight = 44f;

    /// <summary>
    /// Builds the search drawer on the app's <paramref name="body"/> (last in
    /// it, over everything), opened by <paramref name="open"/>, and wires it
    /// into <paramref name="app"/>.
    /// </summary>
    private static void BuildAppSearch(InvestigationApp app, Transform body, Button open, DesktopConfigSO config)
    {
        DestroyChildIfPresent(body, "SearchDrawer");
        Transform drawer = Panel(body, "SearchDrawer", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(1f, 1f, 1f, 0f), ThemeRoleId.ClickCatcher);
        Button dim = GetOrAdd<Button>(drawer.gameObject);
        dim.transition = Selectable.Transition.None;
        dim.targetGraphic = drawer.GetComponent<Image>();

        var panel = (RectTransform)Panel(drawer, "Panel", new Vector2(1f, 0f), Vector2.one, Vector2.zero, Vector2.zero, WbSurface, ThemeRoleId.Surface);
        PlaceRect(panel, new Vector2(1f, 0f), Vector2.one, new Vector2(-DrawerWidth, 0f), Vector2.zero);
        HairlineEdge(panel, "Rule", 2);
        HairlineFrame(panel, WbLineStrong, ThemeRoleId.HairlineStrong, 2f, "Edge");

        TMP_Text title = WbText(panel, "Title", "search.drawer.title", null, PcType.Body, ThemeRoleId.Surface, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        PlaceRect(title.transform, new Vector2(0f, 1f), Vector2.one, new Vector2(20f, -64f), new Vector2(-(20f + 196f + 8f), -16f));
        Button close = QuietButton(panel, "CloseButton", "search.close");
        PlaceRect(close.transform, Vector2.one, Vector2.one, new Vector2(-(20f + 196f), -62f), new Vector2(-20f, -18f));
        TMP_Text hint = WbText(panel, "Hint", "search.drawer.hint", null, PcType.Caption, ThemeRoleId.SurfaceMuted, TextAlignmentOptions.TopLeft, FontStyles.Normal, true);
        PlaceRect(hint.transform, new Vector2(0f, 1f), Vector2.one, new Vector2(20f, -(DrawerFieldTop - 4f)), new Vector2(-20f, -72f));

        TMP_InputField field = BuildInputField(panel, "SearchField", "app.search", Vector2.zero, Vector2.one);
        PlaceRect(field.transform, new Vector2(0f, 1f), Vector2.one, new Vector2(20f, -(DrawerFieldTop + DrawerField)), new Vector2(-20f, -DrawerFieldTop));
        HairlineFrame(field.transform, WbLineStrong, ThemeRoleId.HairlineStrong);

        Transform quick = Panel(panel, "QuickOpen", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        PlaceRect(quick, Vector2.zero, Vector2.one, new Vector2(8f, 8f), new Vector2(-8f, -DrawerAreaTop));
        SearchResultsView results = BuildSearchResults(panel);

        SearchBox box = GetOrAdd<SearchBox>(field.gameObject);
        var soBox = new SerializedObject(box);
        Wire(soBox, "drawer", drawer.gameObject);
        Wire(soBox, "panel", panel);
        Wire(soBox, "dimButton", dim);
        Wire(soBox, "closeButton", close);
        Wire(soBox, "field", field);
        Wire(soBox, "results", results);
        Wire(soBox, "config", config);
        Wire(soBox, "quickOpen", quick.gameObject);
        soBox.ApplyModifiedProperties();

        var so = new SerializedObject(app);
        Wire(so, "searchBox", box);
        so.ApplyModifiedProperties();
        WirePersistentVoid(open, "m_OnClick", app, nameof(InvestigationApp.OpenSearch));

        drawer.SetAsLastSibling();
        drawer.gameObject.SetActive(false);
    }

    /// <summary>The results panel (C5) under the drawer's field, hidden: the chips' row, the list and its templates, the empty line and the hint.</summary>
    private static SearchResultsView BuildSearchResults(Transform drawerPanel)
    {
        Transform panel = Panel(drawerPanel, "SearchResults", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, WbSurface, ThemeRoleId.Surface);
        PlaceRect(panel, Vector2.zero, Vector2.one, new Vector2(8f, 0f), new Vector2(-8f, -DrawerAreaTop));

        Transform header = Panel(panel, "Header", new Vector2(0f, 1f), Vector2.one, Vector2.zero, Vector2.zero, null);
        PlaceRect(header, new Vector2(0f, 1f), Vector2.one, new Vector2(PcSize.S, -SearchHeaderHeight), new Vector2(-PcSize.S, 0f));
        Transform chips = Panel(header, "Chips", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        FlowLayoutGroup row = GetOrAdd<FlowLayoutGroup>(chips.gameObject);
        row.padding = new RectOffset(0, 0, 8, 8);
        var soRow = new SerializedObject(row);
        soRow.FindProperty("spacingX").floatValue = 6f;
        soRow.FindProperty("spacingY").floatValue = 6f;
        soRow.ApplyModifiedProperties();
        Button chip = MakeButton(chips, "ChipTemplate", "All", Vector2.zero, Vector2.one, WbSurface, ThemeRoleId.Button);
        HairlineFrame(chip.transform);
        SetLayoutHeight(chip, 38f);
        HorizontalLayoutGroup chipPad = GetOrAdd<HorizontalLayoutGroup>(chip.gameObject);
        chipPad.padding = new RectOffset(12, 12, 0, 0);
        chipPad.childControlWidth = true;
        chipPad.childControlHeight = true;
        chipPad.childForceExpandWidth = false;
        chipPad.childForceExpandHeight = true;
        TMP_Text chipLabel = chip.transform.Find("Label").GetComponent<TMP_Text>();
        Chrome(chipLabel, PcType.Caption);
        chipLabel.raycastTarget = false;
        if (_kit != null)
        {
            // Sheet 02 C3: a search chip is the kit's pill chip (SearchResultsView shows the chosen one slate).
            DestroyChildIfPresent(chip.transform, "Frame");
            KitSkin(chip, "chip_rest", _kit.desktopScale);
            SceneUiKit.SkinText(chipLabel, _kit.InkOn("chip_rest"), _kit.labelFont, true);
        }
        chip.gameObject.SetActive(false);

        RectTransform list = BuildScrollList(panel, "List", Vector2.zero, Vector2.one, 2f, WbSurface, ThemeRoleId.Surface);
        Transform box = list.parent.parent;
        PlaceRect(box, Vector2.zero, Vector2.one, new Vector2(0f, SearchFooterHeight), new Vector2(0f, -SearchHeaderHeight));
        PlaceRect(list.parent, Vector2.zero, Vector2.one, new Vector2(4f, 4f), new Vector2(-(SearchScrollbar + 6f), -4f));
        Scrollbar bar = BuildScrollbar(box, SearchScrollbar, WbInfoBg, ThemeRoleId.Info, WbLineStrong, ThemeRoleId.HairlineStrong);
        PlaceRect(bar.transform, new Vector2(1f, 0f), Vector2.one, new Vector2(-(SearchScrollbar + 2f), 4f), new Vector2(-2f, -4f));
        ScrollRect listScroll = box.GetComponent<ScrollRect>();
        listScroll.verticalScrollbar = bar;
        listScroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

        TMP_Text heading = Text(list, "HeadingTemplate", "Reference books", PcType.Caption, TextAlignmentOptions.BottomLeft, Vector2.zero, Vector2.one, WbMuted,
                                ThemeRoleId.SurfaceMuted, style: FontStyles.Bold | FontStyles.UpperCase, kind: ThemeTextKind.Heading);
        Chrome(heading, PcType.Caption);
        heading.margin = new Vector4(PcSize.M, 0f, PcSize.M, 4f);
        heading.characterSpacing = 4f;
        heading.raycastTarget = false;
        SetLayoutHeight(heading, SearchHeadingHeight);
        heading.gameObject.SetActive(false);

        Button hit = MakeButton(list, "HitTemplate", string.Empty, Vector2.zero, Vector2.one, WbSurface, ThemeRoleId.InputField);
        SetLayoutHeight(hit, SearchHitHeight);
        HairlineEdge(hit.transform, "Rule", 1);
        TMP_Text blank = hit.transform.Find("Label").GetComponent<TMP_Text>();
        Object.DestroyImmediate(blank.gameObject);
        TMP_Text title = Text(hit.transform, "Title", "Currency Ledger · Periclean Athens (Ancient)", PcType.Body, TextAlignmentOptions.BottomLeft, new Vector2(0f, 0.5f), Vector2.one, Ink,
                              ThemeRoleId.InputField, style: FontStyles.Bold, kind: ThemeTextKind.Button);
        PlaceRect(title.transform, new Vector2(0f, 0.5f), Vector2.one, new Vector2(PcSize.L, 0f), new Vector2(-PcSize.L, -4f));
        TMP_Text snippet = Text(hit.transform, "Snippet", "Drachma", PcType.Caption, TextAlignmentOptions.TopLeft, Vector2.zero, new Vector2(1f, 0.5f), Ink,
                                ThemeRoleId.InputField, kind: ThemeTextKind.Button);
        PlaceRect(snippet.transform, Vector2.zero, new Vector2(1f, 0.5f), new Vector2(PcSize.L, 4f), new Vector2(-PcSize.L, 0f));
        foreach (TMP_Text t in new[] { title, snippet })
        {
            Chrome(t, (int)t.fontSize);
            t.overflowMode = TextOverflowModes.Ellipsis;
            t.richText = true;
            t.raycastTarget = false;
        }
        hit.gameObject.SetActive(false);

        Button more = MakeButton(list, "MoreTemplate", "Show all", Vector2.zero, Vector2.one, WbSurface, ThemeRoleId.InputField);
        SetLayoutHeight(more, SearchMoreHeight);
        TMP_Text moreLabel = ButtonLabel(more, PcType.Caption, TextAlignmentOptions.MidlineLeft, PcSize.L);
        moreLabel.fontStyle = FontStyles.Underline;
        more.gameObject.SetActive(false);

        TMP_Text empty = WbText(panel, "EmptyText", null, string.Empty, PcType.Body, ThemeRoleId.Surface, TextAlignmentOptions.Center, FontStyles.Normal, true);
        PlaceRect(empty.transform, new Vector2(0.05f, 0.3f), new Vector2(0.95f, 0.7f), Vector2.zero, Vector2.zero);
        empty.gameObject.SetActive(false);

        TMP_Text footer = WbText(panel, "HintText", "search.hint", null, PcType.Caption, ThemeRoleId.SurfaceMuted, TextAlignmentOptions.MidlineLeft, FontStyles.Normal, true);
        PlaceRect(footer.transform, Vector2.zero, new Vector2(1f, 0f), new Vector2(PcSize.L, 2f), new Vector2(-PcSize.L, SearchFooterHeight - 2f));

        SearchResultsView view = panel.gameObject.AddComponent<SearchResultsView>();
        var so = new SerializedObject(view);
        Wire(so, "chipRow", chips);
        Wire(so, "chipTemplate", chip);
        Wire(so, "kit", _kit);
        Wire(so, "scroll", box.GetComponent<ScrollRect>());
        Wire(so, "headingTemplate", heading);
        Wire(so, "hitTemplate", hit);
        Wire(so, "moreTemplate", more);
        Wire(so, "emptyText", empty);
        so.ApplyModifiedProperties();
        panel.gameObject.SetActive(false);
        return view;
    }
}
