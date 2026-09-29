using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The office builder's search (redesign phase 19; the PC spec's SE1, SE4,
/// §2.3): on the Investigation app's window, the toolbar's search field gets
/// its SearchBox; the results panel drops down under it over the panes to the
/// window's bottom (last in the window, so it draws over the body): a header
/// with the source chips' row and Close, the scrolling list with its
/// scrollbar (shown when the groups outgrow the list) and inactive templates (a group's
/// heading, a hit with its glyph, title and snippet, "Show all"), the line
/// shown when nothing matches and the footer's hint (a hit's row carries phase
/// 18's found mark, as a link's target does). The field's chip for a pasted
/// untranslated line is the keys' (BuildSearchChip). Rebuilt
/// fresh with the app's window on each run; every reference it wires is checked
/// (Wire). Part of <see cref="OfficeSceneUIBuilder"/>; BuildInvestigationApp
/// calls it.
/// </summary>
public static partial class OfficeSceneUIBuilder
{
    /// <summary>The gap between the results panel's bottom and the window's: the panel takes the panes' height, so a search's groups show whole (it was 520 u, which cut the third group at 1080p).</summary>
    private const float SearchPanelMargin = 8f;

    /// <summary>The results list's scrollbar width (shown only when the groups outgrow the list).</summary>
    private const float SearchScrollbar = 16f;

    /// <summary>The panel's header (the chips and Close) height.</summary>
    private const float SearchHeaderHeight = 48f;

    /// <summary>The panel's footer (the hint) height.</summary>
    private const float SearchFooterHeight = 34f;

    /// <summary>A hit's row height (its title over its snippet).</summary>
    private const float SearchHitHeight = 74f;

    /// <summary>A group heading's height.</summary>
    private const float SearchHeadingHeight = 34f;

    /// <summary>"Show all"'s height.</summary>
    private const float SearchMoreHeight = 40f;

    /// <summary>The width of a hit's source glyph column.</summary>
    private const float SearchGlyphWidth = 72f;

    /// <summary>
    /// Builds the search on the app's window <paramref name="win"/>: the
    /// SearchBox on the toolbar's search field, the results panel from
    /// <paramref name="top"/> (the toolbar's bottom) down; wires them into
    /// <paramref name="app"/>.
    /// </summary>
    private static void BuildAppSearch(InvestigationApp app, Transform win, float top, DesktopConfigSO config)
    {
        TMP_InputField field = Need(win, "Toolbar/SearchField")?.GetComponent<TMP_InputField>();
        if (field == null)
            return;
        SearchResultsView results = BuildSearchResults(win, top);

        SearchBox box = GetOrAdd<SearchBox>(field.gameObject);
        var soBox = new SerializedObject(box);
        Wire(soBox, "field", field);
        Wire(soBox, "results", results);
        Wire(soBox, "config", config);
        soBox.ApplyModifiedProperties();

        var so = new SerializedObject(app);
        Wire(so, "searchBox", box);
        so.ApplyModifiedProperties();
    }

    /// <summary>The results panel (§2.3), hidden: the header's chip row and Close, the list and its templates, the empty line and the hint.</summary>
    private static SearchResultsView BuildSearchResults(Transform win, float top)
    {
        DestroyChildIfPresent(win, "SearchResults");
        Transform panel = Panel(win, "SearchResults", new Vector2(0.1f, 0f), new Vector2(0.7f, 1f), Vector2.zero, Vector2.zero, XpFace, ThemeRoleId.WindowBody);
        ((RectTransform)panel).pivot = new Vector2(0.5f, 1f);
        PlaceRect(panel, new Vector2(0.1f, 0f), new Vector2(0.7f, 1f), new Vector2(0f, SearchPanelMargin), new Vector2(0f, -top));
        panel.SetAsLastSibling();

        Transform header = Panel(panel, "Header", new Vector2(0f, 1f), Vector2.one, Vector2.zero, Vector2.zero, null);
        PlaceRect(header, new Vector2(0f, 1f), Vector2.one, new Vector2(6f, -SearchHeaderHeight), new Vector2(-6f, 0f));
        Transform chips = Panel(header, "Chips", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        PlaceRect(chips, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-120f, 0f));
        HorizontalLayoutGroup row = GetOrAdd<HorizontalLayoutGroup>(chips.gameObject);
        row.padding = new RectOffset(0, 0, 6, 6);
        row.spacing = 4f;
        row.childAlignment = TextAnchor.MiddleLeft;
        row.childControlWidth = true;
        row.childControlHeight = true;
        row.childForceExpandWidth = false;
        row.childForceExpandHeight = true;
        Button chip = MakeButton(chips, "ChipTemplate", "All", Vector2.zero, Vector2.one, null, ThemeRoleId.Button);
        LayoutElement chipSize = GetOrAdd<LayoutElement>(chip.gameObject);
        chipSize.minWidth = 64f;
        chipSize.preferredWidth = 170f;
        chipSize.flexibleWidth = 0f;
        TMP_Text chipLabel = chip.transform.Find("Label").GetComponent<TMP_Text>();
        chipLabel.enableAutoSizing = true;
        chipLabel.fontSizeMin = 16f;
        chipLabel.fontSizeMax = 20f;
        chipLabel.textWrappingMode = TextWrappingModes.NoWrap;
        chipLabel.overflowMode = TextOverflowModes.Ellipsis;
        chipLabel.margin = new Vector4(6f, 0f, 6f, 0f);
        chip.gameObject.SetActive(false);
        Button close = MakeButton(header, "CloseButton", null, new Vector2(1f, 0.14f), new Vector2(1f, 0.86f), null, ThemeRoleId.Button, "search.close");
        PlaceRect(close.transform, new Vector2(1f, 0.14f), new Vector2(1f, 0.86f), new Vector2(-114f, 0f), Vector2.zero);

        RectTransform list = BuildScrollList(panel, "List", Vector2.zero, Vector2.one, 2f, Color.white, ThemeRoleId.WindowBody);
        Transform box = list.parent.parent;
        PlaceRect(box, Vector2.zero, Vector2.one, new Vector2(6f, SearchFooterHeight), new Vector2(-6f, -SearchHeaderHeight));
        PlaceRect(list.parent, Vector2.zero, Vector2.one, new Vector2(4f, 4f), new Vector2(-(SearchScrollbar + 6f), -4f));
        Scrollbar bar = BuildScrollbar(box, SearchScrollbar, XpFace, ThemeRoleId.WindowBody, XpBlue, ThemeRoleId.TitleBar);
        PlaceRect(bar.transform, new Vector2(1f, 0f), Vector2.one, new Vector2(-(SearchScrollbar + 2f), 4f), new Vector2(-2f, -4f));
        ScrollRect listScroll = box.GetComponent<ScrollRect>();
        listScroll.verticalScrollbar = bar;
        listScroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

        TMP_Text heading = Text(list, "HeadingTemplate", "REFERENCE", 18, TextAlignmentOptions.BottomLeft, Vector2.zero, Vector2.one, Ink,
                                ThemeRoleId.WindowBody, style: FontStyles.Bold, kind: ThemeTextKind.Heading);
        heading.margin = new Vector4(6f, 0f, 6f, 2f);
        heading.raycastTarget = false;
        SetLayoutHeight(heading, SearchHeadingHeight);
        heading.gameObject.SetActive(false);

        Button hit = MakeButton(list, "HitTemplate", string.Empty, Vector2.zero, Vector2.one, null, ThemeRoleId.MenuEntry);
        SetLayoutHeight(hit, SearchHitHeight);
        TMP_Text blank = hit.transform.Find("Label").GetComponent<TMP_Text>();
        Object.DestroyImmediate(blank.gameObject);
        TMP_Text glyph = Text(hit.transform, "Glyph", "BOOK", 16, TextAlignmentOptions.Center, Vector2.zero, new Vector2(0f, 1f), Ink,
                              ThemeRoleId.MenuEntry, style: FontStyles.Bold, kind: ThemeTextKind.Button);
        PlaceRect(glyph.transform, Vector2.zero, new Vector2(0f, 1f), new Vector2(4f, 4f), new Vector2(SearchGlyphWidth, -4f));
        TMP_Text title = Text(hit.transform, "Title", "Currency Ledger · Periclean Athens (Ancient)", 22, TextAlignmentOptions.BottomLeft, new Vector2(0f, 0.5f), Vector2.one, Ink,
                              ThemeRoleId.MenuEntry, style: FontStyles.Bold, kind: ThemeTextKind.Button);
        PlaceRect(title.transform, new Vector2(0f, 0.5f), Vector2.one, new Vector2(SearchGlyphWidth + 8f, 0f), new Vector2(-8f, -3f));
        TMP_Text snippet = Text(hit.transform, "Snippet", "Drachma", 20, TextAlignmentOptions.TopLeft, Vector2.zero, new Vector2(1f, 0.5f), Ink,
                                ThemeRoleId.MenuEntry, kind: ThemeTextKind.Button);
        PlaceRect(snippet.transform, Vector2.zero, new Vector2(1f, 0.5f), new Vector2(SearchGlyphWidth + 8f, 3f), new Vector2(-8f, 0f));
        foreach (TMP_Text t in new[] { glyph, title, snippet })
        {
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Ellipsis;
            t.richText = true;
            t.raycastTarget = false;
        }
        hit.gameObject.SetActive(false);

        Button more = MakeButton(list, "MoreTemplate", "Show all", Vector2.zero, Vector2.one, null, ThemeRoleId.MenuEntry);
        SetLayoutHeight(more, SearchMoreHeight);
        TMP_Text moreLabel = more.transform.Find("Label").GetComponent<TMP_Text>();
        moreLabel.fontSize = 20f;
        moreLabel.fontStyle = FontStyles.Italic;
        moreLabel.alignment = TextAlignmentOptions.MidlineLeft;
        moreLabel.margin = new Vector4(SearchGlyphWidth + 8f, 0f, 8f, 0f);
        more.gameObject.SetActive(false);

        TMP_Text empty = Text(panel, "EmptyText", string.Empty, 22, TextAlignmentOptions.Center, new Vector2(0.05f, 0.3f), new Vector2(0.95f, 0.7f), Ink,
                              ThemeRoleId.WindowBody);
        empty.textWrappingMode = TextWrappingModes.Normal;
        empty.raycastTarget = false;
        empty.gameObject.SetActive(false);

        TMP_Text hint = Text(panel, "HintText", null, 18, TextAlignmentOptions.MidlineLeft, Vector2.zero, new Vector2(1f, 0f), Ink,
                             ThemeRoleId.WindowBody, "search.hint", FontStyles.Italic, ThemeTextKind.Body, true);
        PlaceRect(hint.transform, Vector2.zero, new Vector2(1f, 0f), new Vector2(10f, 2f), new Vector2(-10f, SearchFooterHeight - 2f));
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
}
