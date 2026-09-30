using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The office builder's search palette (redesign phase 19; the PC spec's
/// SE1, SE4; the PC UX redesign IA9, IA10, C5): on the Investigation app's
/// window, the toolbar's search field gets its SearchBox; under the field,
/// as wide as it, drop the results panel (to the window's bottom, last in
/// the window so it draws over the body: a header with the source chips'
/// row, each "Papers (3)", and Close; the scrolling list with its scrollbar
/// and inactive templates: a group's heading in the source's full name, a
/// hit with its title over its snippet, "Show all"; the line shown when
/// nothing matches and the footer's hint) and the quick-open panel (the
/// pinned and the recent items before anything is typed: the keys' lists,
/// BuildQuickOpenList). A hit's row carries phase 18's found mark, as a
/// link's target does. The field's chip for a pasted untranslated line is
/// the keys' (BuildSearchChip). Rebuilt fresh with the app's window on each
/// run; every reference it wires is checked (Wire). Part of
/// <see cref="OfficeSceneUIBuilder"/>; BuildInvestigationApp calls it.
/// </summary>
public static partial class OfficeSceneUIBuilder
{
    /// <summary>The gap between the results panel's bottom and the window's.</summary>
    private const float SearchPanelMargin = 8f;

    /// <summary>The results list's scrollbar width (shown only when the groups outgrow the list).</summary>
    private const float SearchScrollbar = 16f;

    /// <summary>The panel's header (the chips and Close) height.</summary>
    private const float SearchHeaderHeight = 56f;

    /// <summary>The panel's footer (the hint) height.</summary>
    private const float SearchFooterHeight = 40f;

    /// <summary>A hit's row height (its title over its snippet).</summary>
    private const float SearchHitHeight = 80f;

    /// <summary>A group heading's height.</summary>
    private const float SearchHeadingHeight = 44f;

    /// <summary>"Show all"'s height.</summary>
    private const float SearchMoreHeight = 44f;

    /// <summary>The quick-open panel's height (Pinned over Recent).</summary>
    private const float QuickOpenHeight = 480f;

    /// <summary>
    /// Builds the search on the app's window <paramref name="win"/>: the
    /// SearchBox on the toolbar's search field, the results panel and the
    /// quick-open panel from <paramref name="top"/> (the toolbar's bottom)
    /// down, as wide as the field; wires them into <paramref name="app"/>.
    /// </summary>
    private static void BuildAppSearch(InvestigationApp app, Transform win, float top, DesktopConfigSO config)
    {
        TMP_InputField field = Need(win, "Toolbar/SearchField")?.GetComponent<TMP_InputField>();
        if (field == null)
            return;
        var fieldRect = (RectTransform)field.transform;
        SearchResultsView results = BuildSearchResults(win, top, fieldRect);
        Transform quick = BuildQuickOpen(win, top, fieldRect);

        SearchBox box = GetOrAdd<SearchBox>(field.gameObject);
        var soBox = new SerializedObject(box);
        Wire(soBox, "field", field);
        Wire(soBox, "results", results);
        Wire(soBox, "config", config);
        Wire(soBox, "quickOpen", quick.gameObject);
        soBox.ApplyModifiedProperties();

        var so = new SerializedObject(app);
        Wire(so, "searchBox", box);
        so.ApplyModifiedProperties();
    }

    /// <summary>A panel under the search field, as wide as it (the field's horizontal offsets in the window), from <paramref name="top"/> down: to <paramref name="height"/>, or to the window's bottom when 0.</summary>
    private static Transform PanelUnderField(Transform win, string name, float top, RectTransform field, float height)
    {
        DestroyChildIfPresent(win, name);
        Transform panel = Panel(win, name, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, XpFace, ThemeRoleId.WindowBody);
        ((RectTransform)panel).pivot = new Vector2(0.5f, 1f);
        float bottom = height > 0f ? 0f : SearchPanelMargin;
        PlaceRect(panel, height > 0f ? new Vector2(0f, 1f) : Vector2.zero, Vector2.one, new Vector2(field.offsetMin.x, height > 0f ? -(top + height) : bottom),
                  new Vector2(field.offsetMax.x, -top));
        panel.SetAsLastSibling();
        Transform frame = BuildFrame(panel, "Frame", 2f, new Color(0.2f, 0.25f, 0.35f, 1f), ThemeRoleId.TitleBar);
        frame.SetAsLastSibling();
        return panel;
    }

    /// <summary>The results panel (C5), hidden: the header's chip row and Close, the list and its templates, the empty line and the hint.</summary>
    private static SearchResultsView BuildSearchResults(Transform win, float top, RectTransform field)
    {
        Transform panel = PanelUnderField(win, "SearchResults", top, field, 0f);

        Transform header = Panel(panel, "Header", new Vector2(0f, 1f), Vector2.one, Vector2.zero, Vector2.zero, null);
        PlaceRect(header, new Vector2(0f, 1f), Vector2.one, new Vector2(PcSize.S, -SearchHeaderHeight), new Vector2(-PcSize.S, 0f));
        Transform chips = Panel(header, "Chips", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        PlaceRect(chips, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-128f, 0f));
        HorizontalLayoutGroup row = GetOrAdd<HorizontalLayoutGroup>(chips.gameObject);
        row.padding = new RectOffset(0, 0, 8, 8);
        row.spacing = 6f;
        row.childAlignment = TextAnchor.MiddleLeft;
        row.childControlWidth = true;
        row.childControlHeight = true;
        row.childForceExpandWidth = false;
        row.childForceExpandHeight = true;
        Button chip = MakeButton(chips, "ChipTemplate", "All", Vector2.zero, Vector2.one, null, ThemeRoleId.Button);
        HorizontalLayoutGroup chipPad = GetOrAdd<HorizontalLayoutGroup>(chip.gameObject);
        chipPad.padding = new RectOffset(14, 14, 0, 0);
        chipPad.childControlWidth = true;
        chipPad.childControlHeight = true;
        chipPad.childForceExpandWidth = false;
        chipPad.childForceExpandHeight = true;
        TMP_Text chipLabel = chip.transform.Find("Label").GetComponent<TMP_Text>();
        Chrome(chipLabel, PcType.Caption);
        chipLabel.raycastTarget = false;
        chip.gameObject.SetActive(false);
        Button close = MakeButton(header, "CloseButton", null, new Vector2(1f, 0.14f), new Vector2(1f, 0.86f), null, ThemeRoleId.Button, "search.close");
        PlaceRect(close.transform, new Vector2(1f, 0.14f), new Vector2(1f, 0.86f), new Vector2(-120f, 0f), Vector2.zero);
        ButtonLabel(close, PcType.Body);

        RectTransform list = BuildScrollList(panel, "List", Vector2.zero, Vector2.one, 2f, XpFace, ThemeRoleId.Sidebar);
        Transform box = list.parent.parent;
        PlaceRect(box, Vector2.zero, Vector2.one, new Vector2(PcSize.S, SearchFooterHeight), new Vector2(-PcSize.S, -SearchHeaderHeight));
        PlaceRect(list.parent, Vector2.zero, Vector2.one, new Vector2(4f, 4f), new Vector2(-(SearchScrollbar + 6f), -4f));
        Scrollbar bar = BuildScrollbar(box, SearchScrollbar, XpFace, ThemeRoleId.WindowBody, XpBlue, ThemeRoleId.TitleBar);
        PlaceRect(bar.transform, new Vector2(1f, 0f), Vector2.one, new Vector2(-(SearchScrollbar + 2f), 4f), new Vector2(-2f, -4f));
        ScrollRect listScroll = box.GetComponent<ScrollRect>();
        listScroll.verticalScrollbar = bar;
        listScroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

        TMP_Text heading = Text(list, "HeadingTemplate", "Reference books", PcType.Caption, TextAlignmentOptions.BottomLeft, Vector2.zero, Vector2.one, Ink,
                                ThemeRoleId.WindowBody, style: FontStyles.Bold, kind: ThemeTextKind.Heading);
        Chrome(heading, PcType.Caption);
        heading.margin = new Vector4(PcSize.M, 0f, PcSize.M, 4f);
        heading.raycastTarget = false;
        SetLayoutHeight(heading, SearchHeadingHeight);
        heading.gameObject.SetActive(false);

        Button hit = MakeButton(list, "HitTemplate", string.Empty, Vector2.zero, Vector2.one, null, ThemeRoleId.InputField);
        SetLayoutHeight(hit, SearchHitHeight);
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

        Button more = MakeButton(list, "MoreTemplate", "Show all", Vector2.zero, Vector2.one, null, ThemeRoleId.InputField);
        SetLayoutHeight(more, SearchMoreHeight);
        TMP_Text moreLabel = ButtonLabel(more, PcType.Caption, TextAlignmentOptions.MidlineLeft, PcSize.L);
        moreLabel.fontStyle = FontStyles.Italic;
        more.gameObject.SetActive(false);

        TMP_Text empty = Text(panel, "EmptyText", string.Empty, PcType.Body, TextAlignmentOptions.Center, new Vector2(0.05f, 0.3f), new Vector2(0.95f, 0.7f), Ink,
                              ThemeRoleId.WindowBody);
        Chrome(empty, PcType.Body, true);
        empty.raycastTarget = false;
        empty.gameObject.SetActive(false);

        TMP_Text hint = Text(panel, "HintText", null, PcType.Caption, TextAlignmentOptions.MidlineLeft, Vector2.zero, new Vector2(1f, 0f), Ink,
                             ThemeRoleId.WindowBody, "search.hint", FontStyles.Italic, ThemeTextKind.Body, true);
        PlaceRect(hint.transform, Vector2.zero, new Vector2(1f, 0f), new Vector2(PcSize.L, 2f), new Vector2(-PcSize.L, SearchFooterHeight - 2f));
        hint.raycastTarget = false;

        SearchResultsView view = panel.gameObject.AddComponent<SearchResultsView>();
        var so = new SerializedObject(view);
        Wire(so, "chipRow", chips);
        Wire(so, "chipTemplate", chip);
        Wire(so, "scroll", box.GetComponent<ScrollRect>());
        Wire(so, "headingTemplate", heading);
        Wire(so, "hitTemplate", hit);
        Wire(so, "moreTemplate", more);
        Wire(so, "emptyText", empty);
        Wire(so, "closeButton", close);
        so.ApplyModifiedProperties();
        panel.gameObject.SetActive(false);
        return view;
    }

    /// <summary>The quick-open panel (IA9), hidden: its heading, then the Pinned and Recent lists (the keys' partial builds them in: BuildQuickOpenList).</summary>
    private static Transform BuildQuickOpen(Transform win, float top, RectTransform field)
    {
        Transform panel = PanelUnderField(win, "QuickOpen", top, field, QuickOpenHeight);
        TMP_Text title = Text(panel, "Title", null, PcType.Caption, TextAlignmentOptions.MidlineLeft, new Vector2(0f, 1f), Vector2.one, Ink,
                              ThemeRoleId.WindowBody, "app.pins.title", FontStyles.Bold, ThemeTextKind.Heading, true);
        PlaceRect(title.transform, new Vector2(0f, 1f), Vector2.one, new Vector2(PcSize.L, -48f), new Vector2(-PcSize.L, -4f));
        title.raycastTarget = false;
        panel.gameObject.SetActive(false);
        return panel;
    }
}
