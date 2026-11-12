using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The office builder's Lens case screen (the PC Lens spec,
/// docs/superpowers/specs/2026-11-12-pc-lens-case-screen.md, sections 1 and 5):
/// the Investigation window covers the whole desktop (the Lens layer, above the
/// taskbar) and has no title bar. Top to bottom: the header (the Home key, the
/// traveller line, the findings chip, the Inspect key, the time), the two sides
/// (a slim tag strip each: OfficeSceneUIBuilder.Panes), the answer plate over the
/// sides' bottom edge, and the dock (one row of document items, from one item
/// template) with the drag ghost. The findings list drops down under the chip.
/// Plain rects, hairlines and text in the present theme's roles: no neon, no art.
/// Units are desktop units on the 1440 x 1080 glass. Part of
/// <see cref="OfficeSceneUIBuilder"/>; BuildInvestigationApp calls it.
/// </summary>
public static partial class OfficeSceneUIBuilder
{
    /// <summary>The side padding of the header, the sides and the dock.</summary>
    private const float LensPad = 30f;

    /// <summary>The header's height.</summary>
    private const float LensHeaderHeight = 96f;

    /// <summary>A side's tag strip height.</summary>
    private const float LensTagHeight = 34f;

    /// <summary>The gap between the two sides.</summary>
    private const float LensSideGap = 22f;

    /// <summary>The dock's height, its distance from the bottom edge and its gap to the sides.</summary>
    private const float LensDockHeight = 76f, LensDockBottom = 22f, LensDockGap = 16f;

    /// <summary>The answer plate's size.</summary>
    private static readonly Vector2 LensPlateSize = new Vector2(420f, 92f);

    /// <summary>The findings list under the chip: its size.</summary>
    private static readonly Vector2 LensFindingsSize = new Vector2(420f, 480f);

    /// <summary>The notice's size, centred under the header.</summary>
    private static readonly Vector2 LensToastSize = new Vector2(520f, 56f);

    /// <summary>The header's keys: the Home key, the Inspect key, the findings chip and the time (widths) and the gap between them.</summary>
    private const float LensHomeWidth = 150f, LensInspectWidth = 230f, LensChipWidth = 210f, LensClockWidth = 120f, LensKeyGap = 14f, LensKeyHeight = 56f;

    /// <summary>Text sizes: the traveller line (bold), the answer headline (bold), the answer line, the hold line, the dock label (it shrinks to the least), the tag strip, the empty state, the key words.</summary>
    private const int LensTravellerText = 30, LensHeadlineText = 28, LensLineText = 17, LensHoldText = 24, LensDockText = 19, LensDockTextMin = 15, LensTagText = 19,
                      LensEmptyText = 30, LensKeyText = 24;

    /// <summary>A dock item's size: the least width, the wish, the height; its icon (a page with a band on top).</summary>
    private static readonly Vector2 LensItemSize = new Vector2(190f, 56f);
    private const float LensItemMinWidth = 70f, LensBand = 8f;
    private static readonly Vector2 LensIconSize = new Vector2(26f, 34f);

    /// <summary>The header's parts the app takes.</summary>
    private struct LensHeaderParts
    {
        public Button Home;
        public TMP_Text Traveller;
        public Button Chip;
        public TMP_Text ChipText;
        public Button Inspect;
        public GameObject InspectOn;
        public TMP_Text Clock;
    }

    /// <summary>The answer plate's four plates' line texts (MatchBoard shows the plate of the text's parent).</summary>
    private struct LensPlateParts
    {
        public TMP_Text Hold;
        public TMP_Text Match;
        public TMP_Text Differ;
        public TMP_Text Info;
    }

    /// <summary>
    /// The Lens layer: a transparent full-glass panel on the desktop canvas, above the
    /// taskbar (OrderDesktopLayers), that holds the Investigation window alone, so the
    /// window can cover the taskbar. Other windows stay on the window layer.
    /// </summary>
    private static Transform EnsureLensLayer(Transform canvasRoot)
    {
        Transform layer = Panel(canvasRoot, "LensLayer", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        PlaceRect(layer, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        return layer;
    }

    /// <summary>
    /// The header: a surface with a hairline under it. At the left the Home key
    /// ("&lt; Home"); then the traveller line (bold, one line); at the right the findings
    /// chip, the Inspect key (its word and the Space hint; a filled face over it while
    /// inspect is on) and the time.
    /// </summary>
    private static LensHeaderParts BuildLensHeader(Transform body)
    {
        Transform header = Panel(body, "LensHeader", new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -LensHeaderHeight / 2f), new Vector2(0f, LensHeaderHeight), WbSurface,
                                 ThemeRoleId.Surface);
        HairlineEdge(header, "Rule", 1);
        var parts = new LensHeaderParts();

        parts.Home = QuietButton(header, "HomeButton", "lens.home");
        PlaceKey(parts.Home.transform, true, LensPad, LensHomeWidth);
        TMP_Text homeLabel = ButtonLabel(parts.Home, LensKeyText, TextAlignmentOptions.Center, 12f);
        homeLabel.margin = new Vector4(36f, 0f, 12f, 0f);
        TMP_Text mark = WbText(parts.Home.transform, "Mark", null, "<", LensKeyText, ThemeRoleId.Button, TextAlignmentOptions.Center, FontStyles.Bold);
        PlaceRect(mark.transform, Vector2.zero, new Vector2(0f, 1f), new Vector2(8f, 0f), new Vector2(40f, 0f));

        float clockRight = LensPad + LensClockWidth, inspectRight = clockRight + LensKeyGap + LensInspectWidth, chipRight = inspectRight + LensKeyGap + LensChipWidth;
        parts.Clock = WbText(header, "Clock", null, "09:00", LensTravellerText, ThemeRoleId.Surface, TextAlignmentOptions.MidlineRight);
        PlaceRect(parts.Clock.transform, new Vector2(1f, 0f), Vector2.one, new Vector2(-clockRight, 0f), new Vector2(-LensPad, 0f));

        parts.Inspect = BuildInspectKey(header, out parts.InspectOn);
        PlaceKey(parts.Inspect.transform, false, clockRight + LensKeyGap, LensInspectWidth);

        parts.Chip = QuietButton(header, "FindingsChip", null);
        PlaceKey(parts.Chip.transform, false, inspectRight + LensKeyGap, LensChipWidth);
        parts.ChipText = ButtonLabel(parts.Chip, LensKeyText);
        parts.ChipText.text = UiText.Get("lens.findings.none");

        parts.Traveller = WbText(header, "Traveller", null, string.Empty, LensTravellerText, ThemeRoleId.Surface, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        parts.Traveller.richText = true;
        PlaceRect(parts.Traveller.transform, Vector2.zero, Vector2.one, new Vector2(LensPad + LensHomeWidth + LensKeyGap, 0f), new Vector2(-(chipRight + LensKeyGap), 0f));
        return parts;
    }

    /// <summary>Places a header key of <paramref name="width"/> at <paramref name="edge"/> from the left (<paramref name="left"/>) or the right edge, centred in the header.</summary>
    private static void PlaceKey(Transform key, bool left, float edge, float width)
    {
        Vector2 anchor = new Vector2(left ? 0f : 1f, 0.5f);
        float from = left ? edge : -(edge + width), to = left ? edge + width : -edge;
        PlaceRect(key, anchor, anchor, new Vector2(from, -LensKeyHeight / 2f), new Vector2(to, LensKeyHeight / 2f));
    }

    /// <summary>The Inspect key: a quiet plate with its word and the Space hint; over it a face filled with the primary colour (the same words in its ink), shown while inspect is on.</summary>
    private static Button BuildInspectKey(Transform header, out GameObject on)
    {
        Button key = QuietButton(header, "InspectButton", "lens.inspect");
        TMP_Text label = ButtonLabel(key, LensKeyText, TextAlignmentOptions.MidlineLeft, 16f);
        SetAnchors(label.transform, Vector2.zero, new Vector2(0.62f, 1f));
        label.margin = new Vector4(16f, 0f, 0f, 0f);
        TMP_Text hint = WbText(key.transform, "Hint", "lens.inspect.key", null, LensLineText, ThemeRoleId.SurfaceMuted, TextAlignmentOptions.MidlineRight);
        PlaceRect(hint.transform, new Vector2(0.62f, 0f), Vector2.one, Vector2.zero, new Vector2(-16f, 0f));

        Transform face = Panel(key.transform, "On", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, WbAction, ThemeRoleId.PrimaryAction);
        face.GetComponent<Image>().raycastTarget = false;
        TMP_Text onLabel = WbText(face, "Label", "lens.inspect", null, LensKeyText, ThemeRoleId.PrimaryAction, TextAlignmentOptions.MidlineLeft);
        PlaceRect(onLabel.transform, Vector2.zero, new Vector2(0.62f, 1f), new Vector2(16f, 0f), Vector2.zero);
        TMP_Text onHint = WbText(face, "Hint", "lens.inspect.key", null, LensLineText, ThemeRoleId.PrimaryAction, TextAlignmentOptions.MidlineRight);
        PlaceRect(onHint.transform, new Vector2(0.62f, 0f), Vector2.one, Vector2.zero, new Vector2(-16f, 0f));
        onLabel.color = onHint.color = Color.white;
        face.gameObject.SetActive(false);
        on = face.gameObject;
        return key;
    }

    /// <summary>The two sides' area (under the header, down to the dock) and the two panes in it, equal width with <see cref="LensSideGap"/> between them.</summary>
    private static Transform BuildLensSides(Transform body)
    {
        Transform sides = Panel(body, "Sides", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        PlaceRect(sides, Vector2.zero, Vector2.one, new Vector2(LensPad, LensDockBottom + LensDockHeight + LensDockGap), new Vector2(-LensPad, -(LensHeaderHeight + 14f)));
        return sides;
    }

    /// <summary>Puts the left and the right pane side by side in the sides' area.</summary>
    private static void PlaceLensPanes(AppPane left, AppPane right)
    {
        PlaceRect(left.transform, Vector2.zero, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(-LensSideGap / 2f, 0f));
        PlaceRect(right.transform, new Vector2(0.5f, 0f), Vector2.one, new Vector2(LensSideGap / 2f, 0f), Vector2.zero);
    }

    /// <summary>
    /// The answer plate: centred above the dock (its bottom edge 8 units over the dock's top), one plate on at a time
    /// (MatchBoard): holding ("Pick a second thing."), a match, a difference or a note,
    /// each with a headline (static, bold) over its line (what MatchBoard writes).
    /// </summary>
    private static LensPlateParts BuildAnswerPlate(Transform body)
    {
        Transform root = Panel(body, "AnswerPlate", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, LensDockBottom + LensDockHeight + 8f + LensPlateSize.y / 2f), LensPlateSize, null); // its bottom edge 8 above the dock; built after the sides, so it draws over them
        var parts = new LensPlateParts
        {
            Hold = AnswerPlate(root, "Hold", WbHoldBg, WbHold, ThemeRoleId.Holding, null),
            Match = AnswerPlate(root, "Match", WbOkBg, WbOk, ThemeRoleId.FindingMatch, "lens.answer.match"),
            Differ = AnswerPlate(root, "Differ", WbWarnBg, WbWarn, ThemeRoleId.FindingDiffer, "lens.answer.differ"),
            Info = AnswerPlate(root, "Note", WbInfoBg, WbMuted, ThemeRoleId.Info, "lens.answer.none"),
        };
        foreach (Transform plate in root)
            plate.gameObject.SetActive(false);
        return parts;
    }

    /// <summary>One answer plate filling the root: its headline (<paramref name="headlineKey"/>; none for the holding plate) and its line text, which it returns.</summary>
    private static TMP_Text AnswerPlate(Transform root, string name, Color fill, Color ink, ThemeRoleId role, string headlineKey)
    {
        Transform plate = Panel(root, name, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, fill, role);
        plate.GetComponent<Image>().raycastTarget = false;
        HairlineFrame(plate, ink, role);
        if (headlineKey != null)
        {
            TMP_Text headline = WbText(plate, "Headline", headlineKey, null, LensHeadlineText, role, TextAlignmentOptions.Center, FontStyles.Bold);
            headline.color = ink;
            PlaceRect(headline.transform, new Vector2(0f, 0.46f), Vector2.one, new Vector2(PcSize.L, 0f), new Vector2(-PcSize.L, -6f));
        }
        TMP_Text line = WbText(plate, "Text", null, string.Empty, headlineKey != null ? LensLineText : LensHoldText, role, TextAlignmentOptions.Center);
        line.color = ink;
        line.richText = true;
        PlaceRect(line.transform, Vector2.zero, headlineKey != null ? new Vector2(1f, 0.5f) : Vector2.one, new Vector2(PcSize.L, headlineKey != null ? 6f : 0f), new Vector2(-PcSize.L, 0f));
        return line;
    }

    /// <summary>The icon of a dock item: a light page with a band on top; the three bands (Traveller, Agency, Books) are made, the item shows one.</summary>
    private static Transform BuildDockIcon(Transform parent, bool inLayout)
    {
        Transform icon = Panel(parent, "Icon", Center, Center, Vector2.zero, LensIconSize, WbSurface, ThemeRoleId.Surface);
        icon.GetComponent<Image>().raycastTarget = false;
        if (inLayout)
        {
            LayoutElement size = GetOrAdd<LayoutElement>(icon.gameObject);
            size.minWidth = size.preferredWidth = LensIconSize.x;
            size.minHeight = size.preferredHeight = LensIconSize.y;
        }
        HairlineFrame(icon, WbLineStrong, ThemeRoleId.HairlineStrong);
        Band(icon, "BandTraveller", WbAction, ThemeRoleId.PrimaryAction);
        Band(icon, "BandAgency", WbHold, ThemeRoleId.Holding);
        Band(icon, "BandBooks", WbMuted, ThemeRoleId.SurfaceMuted);
        return icon;
    }

    private static void Band(Transform icon, string name, Color colour, ThemeRoleId role)
    {
        Transform band = Panel(icon, name, new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -LensBand / 2f), new Vector2(0f, LensBand), colour, role);
        band.GetComponent<Image>().raycastTarget = false;
        band.gameObject.SetActive(false);
    }

    /// <summary>
    /// The dock: one row, centred, <see cref="LensDockHeight"/> high, above the bottom edge. Its items
    /// are cloned from one template (an icon, an unread dot, the name that shrinks then ends with
    /// an ellipsis, the side tag, the outline of a paper not handed over), and a thin rule between
    /// the groups. The drag ghost is last in <paramref name="body"/>, over everything, taking no clicks.
    /// </summary>
    private static LensDock BuildLensDock(Transform body)
    {
        Transform root = Panel(body, "Dock", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, LensDockBottom + LensDockHeight / 2f), new Vector2(-2f * LensPad, LensDockHeight), null);
        var row = (RectTransform)Panel(root, "Row", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        PlaceRect(row, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        HorizontalLayoutGroup line = GetOrAdd<HorizontalLayoutGroup>(row.gameObject);
        line.spacing = 10f;
        line.childAlignment = TextAnchor.MiddleCenter;
        line.childControlWidth = true;
        line.childControlHeight = true;
        line.childForceExpandWidth = false;
        line.childForceExpandHeight = false;

        Button item = MakeButton(row, "ItemTemplate", null, Vector2.zero, Vector2.one, WbSurface, ThemeRoleId.Button);
        DestroyChildIfPresent(item.transform, "Label");
        HorizontalLayoutGroup content = GetOrAdd<HorizontalLayoutGroup>(item.gameObject);
        content.padding = new RectOffset(10, 10, 0, 0);
        content.spacing = 8f;
        content.childAlignment = TextAnchor.MiddleLeft;
        content.childControlWidth = true;
        content.childControlHeight = true;
        content.childForceExpandWidth = false;
        content.childForceExpandHeight = false;
        LayoutElement size = GetOrAdd<LayoutElement>(item.gameObject);
        size.minWidth = LensItemMinWidth;
        size.preferredWidth = LensItemSize.x;
        size.minHeight = size.preferredHeight = LensItemSize.y;
        size.flexibleWidth = 0f;
        GetOrAdd<CanvasGroup>(item.gameObject);
        BuildDockIcon(item.transform, true);
        UnreadDot(item.transform);
        TMP_Text label = WbText(item.transform, "Label", null, "Passport", LensDockText, ThemeRoleId.Button, TextAlignmentOptions.MidlineLeft);
        label.enableAutoSizing = true;
        label.fontSizeMax = LensDockText;
        label.fontSizeMin = LensDockTextMin;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Ellipsis;
        LayoutElement labelRoom = GetOrAdd<LayoutElement>(label.gameObject);
        labelRoom.minWidth = 0f;
        labelRoom.flexibleWidth = 1f;
        Transform side = Panel(item.transform, "Side", Center, Center, Vector2.zero, new Vector2(28f, 26f), WbAction, ThemeRoleId.PrimaryAction);
        side.GetComponent<Image>().raycastTarget = false;
        LayoutElement sideSize = GetOrAdd<LayoutElement>(side.gameObject);
        sideSize.minWidth = sideSize.preferredWidth = 28f;
        sideSize.minHeight = sideSize.preferredHeight = 26f;
        TMP_Text sideText = WbText(side, "Text", null, "L", LensLineText, ThemeRoleId.PrimaryAction, TextAlignmentOptions.Center, FontStyles.Bold);
        sideText.overflowMode = TextOverflowModes.Overflow;
        side.gameObject.SetActive(false);
        HairlineFrame(item.transform);
        HairlineFrame(item.transform, WbLineStrong, ThemeRoleId.HairlineStrong, 2f, "Missing").gameObject.SetActive(false);
        item.gameObject.SetActive(false);

        Transform rule = Panel(row, "RuleTemplate", Center, Center, Vector2.zero, new Vector2(2f, 44f), WbLineStrong, ThemeRoleId.HairlineStrong);
        rule.GetComponent<Image>().raycastTarget = false;
        LayoutElement ruleSize = GetOrAdd<LayoutElement>(rule.gameObject);
        ruleSize.minWidth = ruleSize.preferredWidth = 2f;
        ruleSize.minHeight = ruleSize.preferredHeight = 44f;
        rule.gameObject.SetActive(false);

        Transform ghost = Panel(body, "DockGhost", Center, Center, Vector2.zero, new Vector2(260f, LensItemSize.y), WbSurface, ThemeRoleId.Surface);
        ghost.GetComponent<Image>().raycastTarget = false;
        HairlineFrame(ghost, WbLineStrong, ThemeRoleId.HairlineStrong, 2f);
        Transform ghostIcon = BuildDockIcon(ghost, false);
        PlaceRect(ghostIcon, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(10f, -LensIconSize.y / 2f), new Vector2(10f + LensIconSize.x, LensIconSize.y / 2f));
        TMP_Text ghostLabel = WbText(ghost, "Label", null, "Passport", LensDockText, ThemeRoleId.Surface, TextAlignmentOptions.MidlineLeft);
        PlaceRect(ghostLabel.transform, Vector2.zero, Vector2.one, new Vector2(10f + LensIconSize.x + 8f, 0f), new Vector2(-10f, 0f));
        CanvasGroup noClicks = GetOrAdd<CanvasGroup>(ghost.gameObject);
        noClicks.blocksRaycasts = false;
        noClicks.interactable = false;
        ghost.gameObject.SetActive(false);

        LensDock dock = GetOrAdd<LensDock>(root.gameObject);
        var so = new SerializedObject(dock);
        Wire(so, "row", row);
        Wire(so, "itemTemplate", item);
        Wire(so, "ruleTemplate", rule);
        Wire(so, "ghost", ghost);
        so.ApplyModifiedProperties();
        return dock;
    }

    /// <summary>The notice's place: centred under the header, over the sides (BuildAppToast fills it).</summary>
    private static Transform BuildToastHost(Transform body) =>
        Panel(body, "ToastHost", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -(LensHeaderHeight + 8f + LensToastSize.y / 2f)), LensToastSize, null);

    /// <summary>The findings list: the findings view (OfficeSceneUIBuilder.Workbench) as a panel dropping down under the chip, at the right, closed until the chip is clicked.</summary>
    private static FindingsView BuildFindingsPanel(Transform body, out RectTransform panel)
    {
        FindingsView view = BuildFindingsColumn(body, out panel);
        PlaceRect(panel, Vector2.one, Vector2.one, new Vector2(-(LensPad + LensFindingsSize.x), -(LensHeaderHeight + LensFindingsSize.y)), new Vector2(-LensPad, -LensHeaderHeight));
        HairlineFrame(panel, WbLineStrong, ThemeRoleId.HairlineStrong);
        panel.gameObject.SetActive(false);
        return view;
    }
}
