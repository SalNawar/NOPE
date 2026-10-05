using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The office builder's workbench parts of the Investigation app (the PC
/// workbench spec §2, §3, §5, polished in wave 5 A3; the
/// redesign-existing-projects and minimalist-ui skills' principles within
/// uGUI): white surfaces parted by hairlines, colour only for state,
/// sentence-case labels at the type scale, quiet buttons with a hairline, one
/// primary button per view, slim chrome so the documents get the room. The
/// menu bar (the desk-first redesign, item 8: the menus' titles and their
/// drop-down, who is at the desk, Search; in place of the header, the shelf
/// and the foot), the status line (one plate per state), the line layer over
/// the panes, the decision step, the findings column (its rail) and the
/// Calendar view. Neutral colours are
/// baked (the theme recolours every tagged graphic at load). Part of
/// <see cref="OfficeSceneUIBuilder"/>; BuildInvestigationApp calls it.
/// </summary>
public static partial class OfficeSceneUIBuilder
{
    /// <summary>The workbench's sizes (desktop units; the spec's §3 as polished in wave 5 A3, then the desk-first redesign's menu bar: slim chrome, the room to the documents).</summary>
    private static class WbSize
    {
        /// <summary>The side padding of the menu bar and the main column.</summary>
        public const float Pad = 24f;

        /// <summary>The gap between the main column and the findings, and above and below the work.</summary>
        public const float Gap = 16f;

        /// <summary>The menu bar's height (the titles, who is at the desk, Search).</summary>
        public const float MenuBar = 56f;

        /// <summary>A menu title's height (and Search's).</summary>
        public const float MenuTitle = 40f;

        /// <summary>The drop-down's least width.</summary>
        public const float Menu = 420f;

        /// <summary>The room for who is at the desk (the name and the counters) left of Search.</summary>
        public const float Who = 560f;

        /// <summary>The Search button's width (at the menu bar's right).</summary>
        public const float Search = 200f;

        /// <summary>The findings column's width while something is logged.</summary>
        public const float Findings = 280f;

        /// <summary>The findings column's width while nothing is logged (the rail).</summary>
        public const float Rail = 56f;

        /// <summary>The status line's height (two lines at Caption size, in the tallest culture font).</summary>
        public const float Status = 64f;

        /// <summary>The panes' top under the main column's top: the status line and a gap.</summary>
        public const float PanesTop = Status + 12f;

        /// <summary>A pane's header (one line: the side, the document's name, the target hint).</summary>
        public const float PaneHead = 44f;

        /// <summary>The decision's words' size.</summary>
        public const int Name = 30;

        /// <summary>The gutter between the two panes, where a line's label sits.</summary>
        public const float Gutter = 120f;
    }

    /// <summary>The neutral look, baked (the theme's roles recolour it at load): white surfaces on a warm screen, hairlines, ink, muted ink, the states' plates.</summary>
    private static readonly Color WbSurface = Color.white, WbScreen = Hex(0xFBFBFA), WbLine = Hex(0xE6E4DF), WbLineStrong = Hex(0xC9C6BF),
                                  WbInk = Hex(0x1D1F21), WbMuted = Hex(0x6F6E6A), WbOkBg = Hex(0xEDF3EC), WbOk = Hex(0x2F5E33),
                                  WbWarnBg = Hex(0xFDEBEC), WbWarn = Hex(0x9A2E2C), WbInfoBg = Hex(0xF3F2EF), WbHoldBg = Hex(0xE1F3FE),
                                  WbHold = Hex(0x1F6C9F), WbAction = Hex(0x1C2636);

    /// <summary>An opaque colour from 0xRRGGBB.</summary>
    private static Color Hex(int rgb) => new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, 1f);

    /// <summary>The menu bar's parts the app takes: the view, its drop-down (drawn last, over the work), who is at the desk and Search.</summary>
    private struct AppMenuBar
    {
        public MenuBarView View;
        public RectTransform Dropdown;
        public TMP_Text Name;
        public TMP_Text Counters;
        public Button Search;
    }

    /// <summary>The status line's plates' texts and Cancel.</summary>
    private struct AppStatus
    {
        public Transform Root;
        public TMP_Text Idle;
        public TMP_Text Hold;
        public Button Cancel;
        public TMP_Text Match;
        public TMP_Text Differ;
        public TMP_Text Info;
    }

    // ----------------------------- small parts -----------------------------

    /// <summary>A 1-unit hairline frame just inside <paramref name="parent"/> (or <paramref name="colour"/> in <paramref name="role"/>, <paramref name="width"/> wide, named <paramref name="name"/>).</summary>
    private static Transform HairlineFrame(Transform parent, Color? colour = null, ThemeRoleId role = ThemeRoleId.Hairline, float width = 1f, string name = "Frame") =>
        BuildFrame(parent, name, width, colour ?? WbLine, role);

    /// <summary>A hairline along one edge of <paramref name="parent"/> (0 top, 1 bottom, 2 left, 3 right), outside any layout, never a raycast target.</summary>
    private static void HairlineEdge(Transform parent, string name, int edge)
    {
        Vector2 aMin = edge == 0 ? new Vector2(0f, 1f) : edge == 3 ? new Vector2(1f, 0f) : Vector2.zero;
        Vector2 aMax = edge == 1 ? new Vector2(1f, 0f) : edge == 2 ? new Vector2(0f, 1f) : Vector2.one;
        Vector2 pos = edge == 0 ? new Vector2(0f, -0.5f) : edge == 1 ? new Vector2(0f, 0.5f) : edge == 2 ? new Vector2(0.5f, 0f) : new Vector2(-0.5f, 0f);
        Vector2 size = edge < 2 ? new Vector2(0f, 1f) : new Vector2(1f, 0f);
        Transform line = Panel(parent, name, aMin, aMax, pos, size, WbLine, ThemeRoleId.Hairline);
        line.GetComponent<Image>().raycastTarget = false;
        GetOrAdd<LayoutElement>(line.gameObject).ignoreLayout = true;
    }

    /// <summary>A text at <paramref name="size"/> in <paramref name="role"/>'s ink (keyed by <paramref name="key"/>, else reading <paramref name="sample"/>), not a raycast target.</summary>
    private static TMP_Text WbText(Transform parent, string name, string key, string sample, int size, ThemeRoleId role, TextAlignmentOptions align,
                                   FontStyles style = FontStyles.Normal, bool wrap = false)
    {
        TMP_Text text = Text(parent, name, sample, size, align, Vector2.zero, Vector2.one, WbInk, role, key, style,
                             style == FontStyles.Bold ? ThemeTextKind.Heading : ThemeTextKind.Body);
        Chrome(text, size, wrap);
        text.raycastTarget = false;
        if (!wrap)
            text.overflowMode = TextOverflowModes.Ellipsis;
        return text;
    }

    /// <summary>A quiet button: a white plate with a hairline and its keyed label at Body size.</summary>
    private static Button QuietButton(Transform parent, string name, string key)
    {
        Button button = MakeButton(parent, name, null, Vector2.zero, Vector2.one, WbSurface, ThemeRoleId.Button, key);
        ButtonLabel(button, PcType.Body);
        HairlineFrame(button.transform);
        return button;
    }

    /// <summary>A toolbar button of PcSize.Control square at <paramref name="x"/> from the bar's left, its label gone and a chevron drawn in the Button role's ink (pointing left for Back), with a hover hint (<paramref name="hintKey"/>): the Internet's Back and Forward.</summary>
    private static Button ChevronButton(Transform bar, string name, string hintKey, float x, bool back)
    {
        float pad = (PcSize.Toolbar - PcSize.Control) / 2f;
        Button button = MakeButton(bar, name, null, Vector2.zero, new Vector2(0f, 1f), WbSurface, ThemeRoleId.Button);
        PlaceRect(button.transform, Vector2.zero, new Vector2(0f, 1f), new Vector2(x, pad), new Vector2(x + PcSize.Control, -pad));
        DestroyChildIfPresent(button.transform, "Label");
        HairlineFrame(button.transform);
        float s = back ? 1f : -1f;
        ButtonStroke(button.transform, "Upper", new Vector2(-1.5f * s, 4.5f), new Vector2(2.5f, 13f), -45f * s);
        ButtonStroke(button.transform, "Lower", new Vector2(-1.5f * s, -4.5f), new Vector2(2.5f, 13f), 45f * s);
        BuildHoverHint(button, hintKey, null, new Vector2(0.5f, 0f), new Vector2(0f, 1f));
        return button;
    }

    /// <summary>One stroke of a drawn glyph on a Button-role control, in its ink (no raycast).</summary>
    private static void ButtonStroke(Transform parent, string name, Vector2 centre, Vector2 size, float angle)
    {
        Transform bar = Panel(parent, name, Center, Center, centre, size, Ink);
        bar.localRotation = Quaternion.Euler(0f, 0f, angle);
        Image image = bar.GetComponent<Image>();
        image.raycastTarget = false;
        SceneUiKit.Tag(image, ThemeRoleId.Button, ThemePart.Ink);
    }

    // ----------------------------- the menu bar -----------------------------

    /// <summary>
    /// The menu bar (the desk-first redesign, Saleh 2026-10-05, item 8: menus
    /// at the top with drop-downs, in place of the header and the shelf, so
    /// the documents get the height): a surface WbSize.MenuBar units tall with
    /// a hairline under it; at its left the menu titles' row (MenuTitle,
    /// cloned per menu by MenuBarView); at its right Search (Ctrl K) and, left
    /// of it, one line naming who is at the desk (the name, bold, then the
    /// counters, muted); on <paramref name="body"/>, above the work, the
    /// drop-down: a white list framed by a strong hairline holding a row's
    /// template (MenuRow) and a caption's template ("Not handed over").
    /// </summary>
    private static AppMenuBar BuildMenuBar(Transform body)
    {
        Transform root = Panel(body, "MenuBar", new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -WbSize.MenuBar / 2f), new Vector2(0f, WbSize.MenuBar), WbSurface,
                               ThemeRoleId.Surface);
        HairlineEdge(root, "Rule", 1);

        var row = (RectTransform)Panel(root, "Row", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        PlaceRect(row, Vector2.zero, Vector2.one, new Vector2(WbSize.Pad - 8f, 0f), new Vector2(-(WbSize.Pad + WbSize.Search + WbSize.Who + 2f * PcSize.L), 0f));
        HorizontalLayoutGroup line = GetOrAdd<HorizontalLayoutGroup>(row.gameObject);
        line.spacing = 4f;
        line.childAlignment = TextAnchor.MiddleLeft;
        line.childControlWidth = true;
        line.childControlHeight = true;
        line.childForceExpandWidth = false;
        line.childForceExpandHeight = false;
        Button title = MenuTitle(row, "TitleTemplate");
        title.gameObject.SetActive(false);

        Button search = QuietButton(root, "SearchButton", "app.search.open");
        PlaceRect(search.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-(WbSize.Pad + WbSize.Search), -WbSize.MenuTitle / 2f),
                  new Vector2(-WbSize.Pad, WbSize.MenuTitle / 2f));

        var who = (RectTransform)Panel(root, "Who", new Vector2(1f, 0f), Vector2.one, Vector2.zero, Vector2.zero, null);
        PlaceRect(who, new Vector2(1f, 0f), Vector2.one, new Vector2(-(WbSize.Pad + WbSize.Search + PcSize.L + WbSize.Who), 0f),
                  new Vector2(-(WbSize.Pad + WbSize.Search + PcSize.L), 0f));
        HorizontalLayoutGroup whoRow = GetOrAdd<HorizontalLayoutGroup>(who.gameObject);
        whoRow.spacing = PcSize.M;
        whoRow.childAlignment = TextAnchor.MiddleRight;
        whoRow.childControlWidth = true;
        whoRow.childControlHeight = true;
        whoRow.childForceExpandWidth = false;
        whoRow.childForceExpandHeight = false;
        TMP_Text name = WbText(who, "Name", null, string.Empty, PcType.Caption, ThemeRoleId.Surface, TextAlignmentOptions.MidlineRight, FontStyles.Bold);
        TMP_Text counters = WbText(who, "Counters", null, UiText.Get("idle.waiting"), PcType.Caption, ThemeRoleId.SurfaceMuted, TextAlignmentOptions.MidlineRight);
        GetOrAdd<LayoutElement>(name.gameObject).flexibleWidth = 0f;
        GetOrAdd<LayoutElement>(counters.gameObject).flexibleWidth = 0f;

        var dropdown = (RectTransform)Panel(body, "MenuDropdown", new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(WbSize.Menu, 200f), WbSurface,
                                            ThemeRoleId.Surface);
        dropdown.pivot = new Vector2(0f, 1f);
        VerticalLayoutGroup list = GetOrAdd<VerticalLayoutGroup>(dropdown.gameObject);
        list.padding = new RectOffset(6, 6, 6, 6);
        list.spacing = 2f;
        list.childControlWidth = true;
        list.childControlHeight = true;
        list.childForceExpandWidth = true;
        list.childForceExpandHeight = false;
        ContentSizeFitter fit = GetOrAdd<ContentSizeFitter>(dropdown.gameObject);
        fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        GetOrAdd<LayoutElement>(dropdown.gameObject).minWidth = WbSize.Menu;
        HairlineFrame(dropdown, WbLineStrong, ThemeRoleId.HairlineStrong);
        Button rowTemplate = MenuRow(dropdown, "RowTemplate");
        rowTemplate.gameObject.SetActive(false);
        TMP_Text caption = WbText(dropdown, "CaptionTemplate", null, UiText.Get("menubar.notHandedOver"), PcType.Caption, ThemeRoleId.SurfaceMuted,
                                  TextAlignmentOptions.BottomLeft);
        caption.margin = new Vector4(PcSize.M, PcSize.S, PcSize.M, 2f);
        LayoutElement captionSize = GetOrAdd<LayoutElement>(caption.gameObject);
        captionSize.minHeight = captionSize.preferredHeight = 40f;
        caption.gameObject.SetActive(false);
        dropdown.gameObject.SetActive(false);

        MenuBarView view = GetOrAdd<MenuBarView>(root.gameObject);
        var so = new SerializedObject(view);
        Wire(so, "row", row);
        Wire(so, "titleTemplate", title);
        Wire(so, "dropdown", dropdown);
        Wire(so, "rowTemplate", rowTemplate);
        Wire(so, "captionTemplate", caption);
        so.ApplyModifiedProperties();
        return new AppMenuBar { View = view, Dropdown = dropdown, Name = name, Counters = counters, Search = search };
    }

    /// <summary>
    /// A menu's title: a clear button WbSize.MenuTitle tall whose row holds
    /// its unread dot, its name (Body, ink), a drawn chevron (muted) and its
    /// side's plate ("L", "R"); behind it the open menu's plate (warm grey
    /// with a bar of the primary colour along its foot, the step pills'
    /// look), shown while its menu is open.
    /// </summary>
    private static Button MenuTitle(Transform parent, string name)
    {
        Button title = MakeButton(parent, name, null, Vector2.zero, Vector2.one, new Color(1f, 1f, 1f, 0f), ThemeRoleId.ClickCatcher);
        HorizontalLayoutGroup content = GetOrAdd<HorizontalLayoutGroup>(title.gameObject);
        content.padding = new RectOffset(14, 12, 0, 0);
        content.spacing = 8f;
        content.childAlignment = TextAnchor.MiddleLeft;
        content.childControlWidth = true;
        content.childControlHeight = true;
        content.childForceExpandWidth = false;
        content.childForceExpandHeight = false;
        LayoutElement size = GetOrAdd<LayoutElement>(title.gameObject);
        size.minHeight = size.preferredHeight = WbSize.MenuTitle;
        size.flexibleWidth = 0f;

        Transform current = Panel(title.transform, "Current", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, WbInfoBg, ThemeRoleId.Info);
        current.GetComponent<Image>().raycastTarget = false;
        GetOrAdd<LayoutElement>(current.gameObject).ignoreLayout = true;
        current.SetSiblingIndex(0);
        Panel(current, "Bar", Vector2.zero, new Vector2(1f, 0f), new Vector2(0f, 1.5f), new Vector2(0f, 3f), WbAction, ThemeRoleId.PrimaryAction)
            .GetComponent<Image>().raycastTarget = false;
        current.gameObject.SetActive(false);

        UnreadDot(title.transform);
        TMP_Text label = title.transform.Find("Label").GetComponent<TMP_Text>();
        Chrome(label, PcType.Body);
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Overflow;
        label.raycastTarget = false;
        label.color = WbInk;
        SceneUiKit.Tag(label, ThemeRoleId.Surface, ThemePart.Ink, null, FontStyles.Normal, ThemeTextKind.Button, false);
        label.transform.SetAsLastSibling();

        Transform glyph = Panel(title.transform, "Chevron", Center, Center, Vector2.zero, new Vector2(14f, 14f), null);
        LayoutElement glyphSize = GetOrAdd<LayoutElement>(glyph.gameObject);
        glyphSize.minWidth = glyphSize.preferredWidth = 14f;
        glyphSize.minHeight = glyphSize.preferredHeight = 14f;
        GlyphBar(glyph, "Left", new Vector2(-2.6f, 0f), new Vector2(2f, 9f), 45f, ThemeRoleId.SurfaceMuted);
        GlyphBar(glyph, "Right", new Vector2(2.6f, 0f), new Vector2(2f, 9f), -45f, ThemeRoleId.SurfaceMuted);
        foreach (Image bar in glyph.GetComponentsInChildren<Image>(true))
            bar.color = WbMuted;
        SidePlate(title.transform);
        return title;
    }

    /// <summary>A drop-down's row: a white plate (the Button role) 44 units tall, dimmed by its CanvasGroup when not readable; its unread dot, its name (Caption, cut with an ellipsis, taking the room), its note (Caption, muted: what a click does to a paper not handed over) and its side's plate.</summary>
    private static Button MenuRow(Transform parent, string name)
    {
        Button button = MakeButton(parent, name, null, Vector2.zero, Vector2.one, WbSurface, ThemeRoleId.Button);
        HorizontalLayoutGroup content = GetOrAdd<HorizontalLayoutGroup>(button.gameObject);
        content.padding = new RectOffset(12, 12, 0, 0);
        content.spacing = 8f;
        content.childAlignment = TextAnchor.MiddleLeft;
        content.childControlWidth = true;
        content.childControlHeight = true;
        content.childForceExpandWidth = false;
        content.childForceExpandHeight = false;
        LayoutElement size = GetOrAdd<LayoutElement>(button.gameObject);
        size.minHeight = size.preferredHeight = 44f;
        GetOrAdd<CanvasGroup>(button.gameObject);

        UnreadDot(button.transform);
        TMP_Text label = button.transform.Find("Label").GetComponent<TMP_Text>();
        Chrome(label, PcType.Caption);
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Ellipsis;
        label.raycastTarget = false;
        label.color = WbInk;
        GetOrAdd<LayoutElement>(label.gameObject).flexibleWidth = 1f;
        label.transform.SetAsLastSibling();

        TMP_Text note = WbText(button.transform, "Note", null, string.Empty, PcType.Caption, ThemeRoleId.SurfaceMuted, TextAlignmentOptions.MidlineRight);
        note.overflowMode = TextOverflowModes.Overflow;
        note.gameObject.SetActive(false);
        SidePlate(button.transform);
        return button;
    }

    /// <summary>The small unread dot (the Holding role's ink) first in <paramref name="parent"/>'s row, hidden.</summary>
    private static void UnreadDot(Transform parent)
    {
        Transform dot = Panel(parent, "Unread", Center, Center, Vector2.zero, new Vector2(8f, 8f), WbHold);
        Image dotImage = dot.GetComponent<Image>();
        dotImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        dotImage.raycastTarget = false;
        SceneUiKit.Tag(dotImage, ThemeRoleId.Holding, ThemePart.Ink);
        LayoutElement dotSize = GetOrAdd<LayoutElement>(dot.gameObject);
        dotSize.minWidth = dotSize.preferredWidth = 8f;
        dotSize.minHeight = dotSize.preferredHeight = 8f;
        dot.SetSiblingIndex(0);
        dot.gameObject.SetActive(false);
    }

    /// <summary>The side's plate ("L", "R": the primary colour, its text bold) last in <paramref name="parent"/>'s row, hidden.</summary>
    private static void SidePlate(Transform parent)
    {
        Transform side = Panel(parent, "Side", Center, Center, Vector2.zero, new Vector2(56f, 26f), WbAction, ThemeRoleId.PrimaryAction);
        side.GetComponent<Image>().raycastTarget = false;
        HorizontalLayoutGroup sidePad = GetOrAdd<HorizontalLayoutGroup>(side.gameObject);
        sidePad.padding = new RectOffset(8, 8, 1, 1);
        sidePad.childControlWidth = true;
        sidePad.childControlHeight = true;
        sidePad.childForceExpandWidth = false;
        sidePad.childForceExpandHeight = false;
        TMP_Text sideText = WbText(side, "Text", null, "L", PcType.Caption, ThemeRoleId.PrimaryAction, TextAlignmentOptions.Center, FontStyles.Bold);
        sideText.overflowMode = TextOverflowModes.Overflow;
        side.SetAsLastSibling();
        side.gameObject.SetActive(false);
    }

    // ----------------------------- the work -----------------------------

    /// <summary>
    /// The status line (§4.1; wave 5 A3: the lead folded into it): one plate
    /// per state, each in its role's colours with its line (Caption, two lines
    /// at most, then cut): the step's title and sentence (Info), what is held
    /// with Cancel (Holding), a match (FindingMatch), a difference
    /// (FindingDiffer) and a note (Info); the board shows one. Search (Ctrl+K;
    /// the drawer) at its right.
    /// </summary>
    private static AppStatus BuildStatusLine(Transform main)
    {
        Transform root = Panel(main, "Status", new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -WbSize.Status / 2f), new Vector2(0f, WbSize.Status), null);
        Transform plates = Panel(root, "Plates", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        var status = new AppStatus
        {
            Root = root,
            Idle = StatusPlate(plates, "Idle", WbInfoBg, ThemeRoleId.Info, 0f),
            Hold = StatusPlate(plates, "Hold", WbHoldBg, ThemeRoleId.Holding, 196f),
            Match = StatusPlate(plates, "Match", WbOkBg, ThemeRoleId.FindingMatch, 0f),
            Differ = StatusPlate(plates, "Differ", WbWarnBg, ThemeRoleId.FindingDiffer, 0f),
            Info = StatusPlate(plates, "Note", WbInfoBg, ThemeRoleId.Info, 0f),
        };
        Transform hold = plates.Find("Hold");
        status.Cancel = QuietButton(hold, "CancelButton", "status.cancel");
        PlaceRect(status.Cancel.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-(180f + 8f), -20f), new Vector2(-8f, 20f));
        foreach (string plate in new[] { "Hold", "Match", "Differ", "Note" })
            plates.Find(plate).gameObject.SetActive(false);
        return status;
    }

    /// <summary>A status plate filling the line (its text inset, <paramref name="right"/> more on its right for a control).</summary>
    private static TMP_Text StatusPlate(Transform root, string name, Color fill, ThemeRoleId role, float right)
    {
        Transform plate = Panel(root, name, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, fill, role);
        plate.GetComponent<Image>().raycastTarget = false;
        TMP_Text text = WbText(plate, "Text", null, string.Empty, PcType.Caption, role, TextAlignmentOptions.MidlineLeft, FontStyles.Normal, true);
        PlaceRect(text.transform, Vector2.zero, Vector2.one, new Vector2(PcSize.L, 2f), new Vector2(-(PcSize.L + right), -2f));
        text.lineSpacing = -8f;
        text.richText = true;
        text.overflowMode = TextOverflowModes.Ellipsis;
        return text;
    }

    /// <summary>The line layer over the two panes (MatchLines, last in the panes' area so it draws over both): its label plate (the result's colour, the label bold, wrapping in the gutter between the panes) hidden until a line shows; it sits in the gutter while <paramref name="right"/> shows.</summary>
    private static MatchLines BuildMatchLines(Transform panes, GameObject right)
    {
        var go = new GameObject("Lines", typeof(RectTransform), typeof(CanvasRenderer));
        go.transform.SetParent(panes, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        MatchLines lines = go.AddComponent<MatchLines>();
        lines.raycastTarget = false;
        lines.color = WbHold;
        SceneUiKit.Tag(lines, ThemeRoleId.Holding, ThemePart.Ink);

        Transform plate = Panel(go.transform, "Label", Center, Center, Vector2.zero, new Vector2(WbSize.Gutter - 8f, 36f), WbAction, ThemeRoleId.PrimaryAction);
        plate.GetComponent<Image>().raycastTarget = false;
        HorizontalLayoutGroup pad = GetOrAdd<HorizontalLayoutGroup>(plate.gameObject);
        pad.padding = new RectOffset(6, 6, 4, 4);
        pad.childControlWidth = true;
        pad.childControlHeight = true;
        pad.childForceExpandWidth = true;
        pad.childForceExpandHeight = false;
        ContentSizeFitter fit = GetOrAdd<ContentSizeFitter>(plate.gameObject);
        fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        TMP_Text label = WbText(plate, "Text", null, "Matching data", PcType.Caption, ThemeRoleId.PrimaryAction, TextAlignmentOptions.Center, FontStyles.Bold, true);
        label.lineSpacing = -10f;
        label.overflowMode = TextOverflowModes.Overflow;
        SceneUiKit.Tag(label, ThemeRoleId.PrimaryAction, ThemePart.Ink, null, FontStyles.Bold, ThemeTextKind.Heading, false);
        plate.gameObject.SetActive(false);

        var so = new SerializedObject(lines);
        Wire(so, "labelPlate", plate.GetComponent<Image>());
        Wire(so, "labelText", label);
        Wire(so, "gutterPane", right);
        so.FindProperty("gutterLabelWidth").floatValue = WbSize.Gutter - 8f;
        so.ApplyModifiedProperties();
        go.transform.SetAsLastSibling();
        return lines;
    }

    /// <summary>
    /// The decision step (IA10, W5), in place of the status line and the
    /// panes: what was logged (Body), then Accept and Deny as large plates in
    /// their roles' colours (their keyed words with the fixed glyphs, and a
    /// second line of explanation), hidden until the step shows.
    /// </summary>
    private static DecisionView BuildDecision(Transform main, out Button accept, out Button deny)
    {
        Transform root = Panel(main, "Decision", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        TMP_Text summary = WbText(root, "Summary", null, string.Empty, PcType.Body, ThemeRoleId.WindowBody, TextAlignmentOptions.TopLeft, FontStyles.Normal, true);
        PlaceRect(summary.transform, new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -64f), new Vector2(0f, -4f));

        accept = DecisionCard(root, "AcceptButton", "accept", ThemeRoleId.AcceptButton, new Color(0.16f, 0.42f, 0.22f, 1f), 0f, true, out TMP_Text acceptDetail);
        deny = DecisionCard(root, "DenyButton", "deny", ThemeRoleId.DenyButton, new Color(0.46f, 0.16f, 0.16f, 1f), 460f + PcSize.L, false, out TMP_Text denyDetail);

        DecisionView view = GetOrAdd<DecisionView>(root.gameObject);
        var so = new SerializedObject(view);
        Wire(so, "summaryText", summary);
        Wire(so, "acceptDetail", acceptDetail);
        Wire(so, "denyDetail", denyDetail);
        so.ApplyModifiedProperties();
        root.gameObject.SetActive(false);
        return view;
    }

    /// <summary>One decision plate, 460 × 160 units at <paramref name="x"/> under the summary: its keyed word (bold, 30 u) after its fixed glyph, its detail under it.</summary>
    private static Button DecisionCard(Transform root, string name, string key, ThemeRoleId role, Color fill, float x, bool tick, out TMP_Text detail)
    {
        Button card = MakeButton(root, name, null, Vector2.zero, Vector2.one, fill, role, key);
        PlaceRect(card.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(x, -(72f + 160f)), new Vector2(x + 460f, -72f));
        TMP_Text word = card.transform.Find("Label").GetComponent<TMP_Text>();
        Chrome(word, WbSize.Name);
        word.fontStyle = FontStyles.Bold;
        word.alignment = TextAlignmentOptions.MidlineLeft;
        word.lineSpacing = -6f;
        word.raycastTarget = false;
        PlaceRect(word.transform, new Vector2(0f, 0.5f), Vector2.one, new Vector2(56f, 0f), new Vector2(-PcSize.L, -8f));
        SceneUiKit.Tag(word, role, ThemePart.Ink, key, FontStyles.Bold, ThemeTextKind.Button, true);
        Transform glyph = Panel(card.transform, "Glyph", new Vector2(0f, 0.75f), new Vector2(0f, 0.75f), new Vector2(30f, 0f), new Vector2(32f, 32f), null);
        ((RectTransform)glyph).pivot = Center;
        if (tick)
        {
            GlyphBar(glyph, "Stroke1", new Vector2(-6.95f, -4.05f), new Vector2(6f, 14f), 45f, role);
            GlyphBar(glyph, "Stroke2", new Vector2(7.19f, 0.19f), new Vector2(6f, 26f), -45f, role);
        }
        else
        {
            GlyphBar(glyph, "Stroke1", Vector2.zero, new Vector2(6f, 30f), 45f, role);
            GlyphBar(glyph, "Stroke2", Vector2.zero, new Vector2(6f, 30f), -45f, role);
        }
        detail = WbText(card.transform, "Detail", null, string.Empty, PcType.Caption, role, TextAlignmentOptions.TopLeft, FontStyles.Normal, true);
        PlaceRect(detail.transform, Vector2.zero, new Vector2(1f, 0.5f), new Vector2(PcSize.L + 4f, 10f), new Vector2(-PcSize.L, -4f));
        return card;
    }

    /// <summary>
    /// The findings column (IA8, §4.4; wave 5 A3): a surface at the work
    /// area's right with a hairline at its left. Its rail ("No findings yet"
    /// up its side, muted) shows while nothing is logged; then its full parts:
    /// the heading (bold, muted) and the
    /// scrolling list of findings (a plate's template: a match plate and a
    /// difference plate, each a headline over one short line, both cut rather
    /// than wrapped). The app sets its width (the rail's or the column's).
    /// </summary>
    private static FindingsView BuildFindingsColumn(Transform work, out RectTransform column)
    {
        column = (RectTransform)Panel(work, "Findings", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, WbSurface, ThemeRoleId.Surface);
        PlaceRect(column, new Vector2(1f, 0f), Vector2.one, new Vector2(-WbSize.Findings, 0f), Vector2.zero);
        HairlineEdge(column, "Rule", 2);

        Transform rail = Panel(column, "Rail", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        TMP_Text railText = WbText(rail, "Text", "findings.empty", null, PcType.Caption, ThemeRoleId.SurfaceMuted, TextAlignmentOptions.MidlineRight);
        var railRect = (RectTransform)railText.transform;
        railRect.anchorMin = railRect.anchorMax = new Vector2(0.5f, 1f);
        railRect.pivot = new Vector2(1f, 0.5f);
        railRect.sizeDelta = new Vector2(520f, 40f);
        railRect.anchoredPosition = new Vector2(0f, -16f);
        railRect.localRotation = Quaternion.Euler(0f, 0f, 90f);
        rail.gameObject.SetActive(false);

        Transform full = Panel(column, "Full", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        TMP_Text heading = WbText(full, "Heading", "findings.title", null, PcType.Caption, ThemeRoleId.SurfaceMuted, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        PlaceRect(heading.transform, new Vector2(0f, 1f), Vector2.one, new Vector2(16f, -48f), new Vector2(-16f, -8f));

        RectTransform list = BuildScrollList(full, "List", Vector2.zero, Vector2.one, 8f, WbSurface, ThemeRoleId.Surface);
        Transform box = list.parent.parent;
        PlaceRect(box, Vector2.zero, Vector2.one, new Vector2(10f, 8f), new Vector2(-10f, -52f));

        Button row = MakeButton(list, "FindingTemplate", null, Vector2.zero, Vector2.one, new Color(1f, 1f, 1f, 0f), ThemeRoleId.ClickCatcher);
        DestroyChildIfPresent(row.transform, "Label");
        VerticalLayoutGroup stack = GetOrAdd<VerticalLayoutGroup>(row.gameObject);
        stack.childControlWidth = true;
        stack.childControlHeight = true;
        stack.childForceExpandWidth = true;
        stack.childForceExpandHeight = false;
        FindingPlate(row.transform, "Match", WbOkBg, ThemeRoleId.FindingMatch);
        FindingPlate(row.transform, "Differ", WbWarnBg, ThemeRoleId.FindingDiffer).gameObject.SetActive(false);
        row.gameObject.SetActive(false);

        FindingsView view = GetOrAdd<FindingsView>(column.gameObject);
        var so = new SerializedObject(view);
        Wire(so, "rowsRoot", list);
        Wire(so, "rowTemplate", row);
        Wire(so, "full", full.gameObject);
        Wire(so, "rail", rail.gameObject);
        so.ApplyModifiedProperties();
        return view;
    }

    /// <summary>A finding's plate in <paramref name="role"/>'s colours: its headline over its short line, each one line, cut with an ellipsis.</summary>
    private static Transform FindingPlate(Transform row, string name, Color fill, ThemeRoleId role)
    {
        Transform plate = Panel(row, name, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, fill, role);
        plate.GetComponent<Image>().raycastTarget = false;
        VerticalLayoutGroup pad = GetOrAdd<VerticalLayoutGroup>(plate.gameObject);
        pad.padding = new RectOffset(12, 12, 8, 8);
        pad.spacing = 0f;
        pad.childControlWidth = true;
        pad.childControlHeight = true;
        pad.childForceExpandWidth = true;
        pad.childForceExpandHeight = false;
        foreach (string text in new[] { "Title", "Detail" })
        {
            TMP_Text line = LayoutText(plate, text, PcType.Caption, FontStyles.Normal, role);
            line.textWrappingMode = TextWrappingModes.NoWrap;
            line.overflowMode = TextOverflowModes.Ellipsis;
            line.color = role == ThemeRoleId.FindingMatch ? WbOk : WbWarn;
        }
        return plate;
    }

    // ----------------------------- the Calendar view -----------------------------

    /// <summary>
    /// The Calendar tab (IA6; lesson D10): a card on the pane's paper (a
    /// surface with a hairline, as wide as the pane): "Agency calendar" (caps,
    /// muted); the day's number (96 u) beside the weekday, the month and year
    /// and the shift day; a hairline; "Today" beside today's date as a quiet
    /// button (the value to click and match); and the week's seven days, each
    /// its weekday over its number, with today's mark behind its day.
    /// </summary>
    private static CalendarView BuildCalendarView(Transform content)
    {
        Transform root = ViewRoot(content, "CalendarView", WbScreen, ThemeRoleId.WindowBody);
        Transform card = Panel(root, "Card", new Vector2(0f, 1f), Vector2.one, Vector2.zero, Vector2.zero, WbSurface, ThemeRoleId.Surface);
        PlaceRect(card, new Vector2(0f, 1f), Vector2.one, new Vector2(PcSize.L, -(PcSize.L + 360f)), new Vector2(-PcSize.L, -PcSize.L));
        HairlineFrame(card);

        TMP_Text caption = WbText(card, "Caption", "calendar.title", null, PcType.Caption, ThemeRoleId.SurfaceMuted, TextAlignmentOptions.TopLeft, FontStyles.Bold);
        caption.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
        caption.characterSpacing = 4f;
        caption.overflowMode = TextOverflowModes.Overflow;
        SceneUiKit.Tag(caption, ThemeRoleId.SurfaceMuted, ThemePart.Ink, "calendar.title", FontStyles.Bold | FontStyles.UpperCase, ThemeTextKind.Heading, false);
        CardRow(caption, 16f, 48f);
        TMP_Text day = WbText(card, "Day", null, "30", 96, ThemeRoleId.Surface, TextAlignmentOptions.TopLeft, FontStyles.Bold);
        day.overflowMode = TextOverflowModes.Overflow;
        PlaceRect(day.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -166f), new Vector2(160f, -52f));
        TMP_Text weekday = WbText(card, "Weekday", null, "Wednesday", PcType.Title, ThemeRoleId.SurfaceMuted, TextAlignmentOptions.TopLeft);
        PlaceRect(weekday.transform, new Vector2(0f, 1f), Vector2.one, new Vector2(176f, -96f), new Vector2(-24f, -60f));
        TMP_Text month = WbText(card, "Month", null, "September 2150", PcType.Title, ThemeRoleId.Surface, TextAlignmentOptions.TopLeft, FontStyles.Bold);
        PlaceRect(month.transform, new Vector2(0f, 1f), Vector2.one, new Vector2(176f, -132f), new Vector2(-24f, -96f));
        TMP_Text shift = WbText(card, "Shift", null, "Shift day 1", PcType.Caption, ThemeRoleId.SurfaceMuted, TextAlignmentOptions.TopLeft);
        PlaceRect(shift.transform, new Vector2(0f, 1f), Vector2.one, new Vector2(176f, -166f), new Vector2(-24f, -134f));
        Transform rule = Panel(card, "Rule", new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -180f), new Vector2(-48f, 1f), WbLine, ThemeRoleId.Hairline);
        rule.GetComponent<Image>().raycastTarget = false;

        TMP_Text todayLabel = WbText(card, "TodayLabel", "calendar.today", null, PcType.Body, ThemeRoleId.SurfaceMuted, TextAlignmentOptions.MidlineLeft);
        PlaceRect(todayLabel.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -246f), new Vector2(150f, -196f));
        Button today = QuietButton(card, "TodayButton", null);
        PlaceRect(today.transform, new Vector2(0f, 1f), Vector2.one, new Vector2(150f, -246f), new Vector2(-24f, -196f));
        TMP_Text todayText = today.transform.Find("Label").GetComponent<TMP_Text>();
        todayText.fontStyle = FontStyles.Bold;
        todayText.text = "30 Sep 2150";

        Transform week = Panel(card, "Week", new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -304f), new Vector2(-48f, 72f), null);
        Transform mark = Panel(week, "TodayMark", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, WbHoldBg, ThemeRoleId.Holding);
        mark.GetComponent<Image>().raycastTarget = false;
        var cells = new List<Object>();
        for (int i = 0; i < 7; i++)
        {
            TMP_Text cell = WbText(week, "Day_" + i, null, "Mon\n28", PcType.Caption, ThemeRoleId.Surface, TextAlignmentOptions.Center, FontStyles.Normal, true);
            cell.overflowMode = TextOverflowModes.Overflow;
            cell.lineSpacing = -6f;
            SetAnchors(cell.transform, new Vector2(i / 7f, 0f), new Vector2((i + 1) / 7f, 1f));
            cells.Add(cell);
        }

        CalendarView view = root.gameObject.AddComponent<CalendarView>();
        var so = new SerializedObject(view);
        Wire(so, "weekdayText", weekday);
        Wire(so, "dayText", day);
        Wire(so, "monthText", month);
        Wire(so, "shiftText", shift);
        Wire(so, "todayButton", today);
        Wire(so, "todayText", todayText);
        SerializedArrays.Set(so, "weekTexts", cells);
        Wire(so, "todayMark", mark);
        so.ApplyModifiedProperties();
        return view;
    }

    /// <summary>A card's text from <paramref name="top"/> to <paramref name="bottom"/> units under its top, 24 units in from its sides.</summary>
    private static void CardRow(TMP_Text text, float top, float bottom) =>
        PlaceRect(text.transform, new Vector2(0f, 1f), Vector2.one, new Vector2(24f, -bottom), new Vector2(-24f, -top));

    // ----------------------------- wiring to the office -----------------------------

    /// <summary>The PC's screen (whether the player looks at the desktop) for the steps.</summary>
    private static void WireGuideToOffice(GuideBar guide, MonitorScreen screen)
    {
        var so = new SerializedObject(guide);
        Wire(so, "screen", screen);
        so.ApplyModifiedProperties();
    }

    /// <summary>Gives the Settings window the app's guided steps (its pair shows or hides their hints).</summary>
    private static void WireStepsSettings(DesktopWindow settings, GuideBar steps)
    {
        var so = new SerializedObject(settings.GetComponent<SettingsWindowController>());
        SetRef(so, "steps", steps);
        so.ApplyModifiedProperties();
    }
}
