using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The office builder's Investigation app panes (redesign phase 18; the PC
/// spec's AP2, AP9, LK2, CM3; the PC workbench spec IA5, §3, §5): a pane
/// (AppPane) framed by a hairline (the target's, a stronger one), its header
/// (a button: a click makes the side the target; the side's tag, filled with
/// the primary colour on the target, quiet on the other; the document's name,
/// bold, on one line (wave 5 A3: a slim header, the room to the document);
/// "Opens here" on the target; a hairline under it), its content with a view per source
/// and the no-case state (its words wrapping and shrinking to fit); the ↗ (a
/// drawn glyph in the link ink, 28 u, with its hover hint) and the found
/// outline, which the forms' FormView clones; and the hover hints of the
/// chrome, sized to their words. Rebuilt fresh with the app (its one
/// convergence policy, audit R6-008); every reference is checked (Wire,
/// audit R6-004). Part of <see cref="OfficeSceneUIBuilder"/>;
/// BuildInvestigationApp calls it.
/// </summary>
public static partial class OfficeSceneUIBuilder
{
    /// <summary>The no-case state's words: their size, and the least they shrink to (wrapping onto a second line first).</summary>
    private const float NoCaseText = 40f, NoCaseTextMin = 28f;

    /// <summary>The found mark's outline width.</summary>
    private const float FoundFrameWidth = 2f;

    /// <summary>The ↗'s hit box (a square).</summary>
    private const float LinkSize = 28f;

    /// <summary>The side tag's size in a pane's header.</summary>
    private static readonly Vector2 PaneTagSize = new Vector2(72f, 28f);

    /// <summary>The target hint's width at a pane header's right.</summary>
    private const float PaneHintWidth = 130f;

    /// <summary>The accent (the found mark, the Orders' selection): the focus ring's built colour.</summary>
    private static readonly Color AccentInk = new Color(0.95f, 0.55f, 0.1f, 1f);

    /// <summary>The link ink of the ↗ on a paper or a row (diegetic: never themed).</summary>
    private static readonly Color LinkInk = new Color(0.12f, 0.3f, 0.72f, 1f);

    /// <summary>One pane's views, for the façade (each source's view).</summary>
    private struct PaneViews
    {
        public DocumentsView Documents;
        public RecordsView Records;
        public ReferenceView Reference;
        public TranscriptView Transcript;
        public ReportView Report;
        public RulesView Rules;
        public CalendarView Calendar;
    }

    /// <summary>
    /// A pane named <paramref name="name"/> filling <paramref name="area"/>
    /// (the app lays the two out at runtime): its hairline frames, its header
    /// (the side's tags, "Left" when <paramref name="left"/> else "Right", the
    /// title, the target hint), the content with the seven views and the
    /// no-case state; it shows <paramref name="start"/> first and its rows
    /// pick into <paramref name="compare"/>. Its views come back in
    /// <paramref name="views"/>.
    /// </summary>
    private static AppPane BuildAppPane(Transform area, string name, AppTab start, CompareController compare, DesktopConfigSO config, bool left, out PaneViews views)
    {
        Transform paneRoot = Panel(area, name, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, WbScreen, ThemeRoleId.WindowBody);
        AppPane pane = paneRoot.gameObject.AddComponent<AppPane>();

        float head = WbSize.PaneHead;
        Button header = MakeButton(paneRoot, "PaneHeader", null, new Vector2(0f, 1f), Vector2.one, WbSurface, ThemeRoleId.Surface);
        DestroyChildIfPresent(header.transform, "Label");
        PlaceRect(header.transform, new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -head), Vector2.zero);
        HairlineEdge(header.transform, "Rule", 1);
        string sideKey = left ? "app.side.left" : "app.side.right";
        Transform tagTarget = PaneTag(header.transform, "TagTarget", sideKey, WbAction, ThemeRoleId.PrimaryAction);
        Transform tag = PaneTag(header.transform, "Tag", sideKey, WbInfoBg, ThemeRoleId.Info);
        // The title is the pane's to write (the document shown), never a theme's keyed label (a theme applied again would reset it).
        TMP_Text title = WbText(header.transform, "TitleText", null, UiText.Get(AppTabTitleKey(start)), PcType.Body, ThemeRoleId.Surface, TextAlignmentOptions.MidlineLeft,
                                FontStyles.Bold);
        PlaceRect(title.transform, Vector2.zero, Vector2.one, new Vector2(PcSize.L + PaneTagSize.x + PcSize.M, 0f), new Vector2(-(PaneHintWidth + PcSize.L), 0f));
        title.enableAutoSizing = true;
        title.fontSizeMax = PcType.Body;
        title.fontSizeMin = PcType.Caption;
        title.overflowMode = TextOverflowModes.Ellipsis;
        TMP_Text hint = WbText(header.transform, "HintTarget", "app.pane.shelfHere", null, PcType.Caption, ThemeRoleId.SurfaceMuted, TextAlignmentOptions.MidlineRight);
        PlaceRect(hint.transform, new Vector2(1f, 0f), Vector2.one, new Vector2(-(PaneHintWidth + PcSize.L), 0f), new Vector2(-PcSize.L, 0f));

        Transform content = Panel(paneRoot, "Content", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, WbScreen, ThemeRoleId.WindowBody);
        PlaceRect(content, Vector2.zero, Vector2.one, new Vector2(1f, 1f), new Vector2(-1f, -head));
        content.gameObject.AddComponent<RectMask2D>();

        views = new PaneViews
        {
            Documents = BuildDocumentsView(content, out AppView documents),
            Records = BuildRecordsView(content, compare),
            Reference = BuildReferenceView(content),
            Transcript = BuildTranscriptView(content),
            Report = BuildReportView(content),
            Rules = BuildRulesView(content),
            Calendar = BuildCalendarView(content),
        };

        Transform noCase = Panel(content, "NoCase", new Vector2(0.06f, 0.4f), new Vector2(0.94f, 0.6f), Vector2.zero, Vector2.zero, WbInfoBg, ThemeRoleId.Info);
        noCase.GetComponent<Image>().raycastTarget = false;
        TMP_Text noCaseText = Text(noCase, "Text", null, Mathf.RoundToInt(NoCaseText), TextAlignmentOptions.Center, Vector2.zero, Vector2.one, WbMuted,
                                   ThemeRoleId.Info, "idle.waiting", FontStyles.Bold, ThemeTextKind.Heading);
        SetAnchors(noCaseText.transform, new Vector2(0.04f, 0f), new Vector2(0.96f, 1f));
        noCaseText.raycastTarget = false;
        noCaseText.textWrappingMode = TextWrappingModes.Normal;
        noCaseText.enableAutoSizing = true;
        noCaseText.fontSizeMax = NoCaseText;
        noCaseText.fontSizeMin = NoCaseTextMin;
        noCase.gameObject.SetActive(false);

        Transform frame = HairlineFrame(paneRoot);
        Transform strong = HairlineFrame(paneRoot, WbLineStrong, ThemeRoleId.HairlineStrong, 2f, "FrameStrong");
        strong.gameObject.SetActive(false);

        var so = new SerializedObject(pane);
        SerializedArrays.Set(so, "views", new Object[] { documents, views.Records, views.Reference, views.Transcript, views.Report, views.Rules, views.Calendar });
        Wire(so, "titleText", title);
        Wire(so, "headerButton", header);
        Wire(so, "noCase", noCase.gameObject);
        SerializedArrays.Set(so, "targetParts", new Object[] { tagTarget.gameObject, hint.gameObject, strong.gameObject });
        SerializedArrays.Set(so, "otherParts", new Object[] { tag.gameObject, frame.gameObject });
        so.FindProperty("startTab").enumValueIndex = (int)start;
        so.FindProperty("titleRightInsets").vector2Value = new Vector2(-(PaneHintWidth + PcSize.L), -PcSize.L);
        Wire(so, "config", config);
        so.ApplyModifiedProperties();
        return pane;
    }

    /// <summary>A source's name key ("app.tab.documents").</summary>
    private static string AppTabTitleKey(AppTab tab) => "app.tab." + tab.ToString().ToLowerInvariant();

    /// <summary>A side tag in a pane's header: a plate in <paramref name="role"/>'s colours with its keyed word (Caption, bold).</summary>
    private static Transform PaneTag(Transform header, string name, string key, Color fill, ThemeRoleId role)
    {
        Transform tag = Panel(header, name, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(PcSize.L + PaneTagSize.x / 2f, 0f), PaneTagSize, fill, role);
        tag.GetComponent<Image>().raycastTarget = false;
        TMP_Text word = WbText(tag, "Text", key, null, PcType.Caption, role, TextAlignmentOptions.Center, FontStyles.Bold);
        word.overflowMode = TextOverflowModes.Overflow;
        return tag;
    }

    /// <summary>
    /// A hover hint on <paramref name="control"/> (HoverHint): a tooltip at
    /// the control's <paramref name="anchor"/>, by its <paramref name="pivot"/>
    /// (a 4 u gap), sized to its words (one line at Caption size), reading the
    /// keyed <paramref name="labelKey"/>, else <paramref name="text"/> (a hint
    /// the runtime writes); hidden, never a raycast target. Returns its text.
    /// </summary>
    private static TMP_Text BuildHoverHint(Component control, string labelKey, string text, Vector2 anchor, Vector2 pivot)
    {
        Transform hint = Panel(control.transform, "Hint", anchor, anchor, new Vector2(0f, pivot.y < 0.5f ? 4f : -4f), new Vector2(200f, 40f), Tooltip, ThemeRoleId.Tooltip);
        ((RectTransform)hint).pivot = pivot;
        hint.GetComponent<Image>().raycastTarget = false;
        GetOrAdd<LayoutElement>(hint.gameObject).ignoreLayout = true;
        HorizontalLayoutGroup pad = GetOrAdd<HorizontalLayoutGroup>(hint.gameObject);
        pad.padding = new RectOffset(12, 12, 6, 6);
        pad.childControlWidth = true;
        pad.childControlHeight = true;
        pad.childForceExpandWidth = false;
        pad.childForceExpandHeight = false;
        ContentSizeFitter fit = GetOrAdd<ContentSizeFitter>(hint.gameObject);
        fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        TMP_Text line = Text(hint, "Label", text, PcType.Caption, TextAlignmentOptions.Center, Vector2.zero, Vector2.one, Ink,
                             ThemeRoleId.Tooltip, labelKey, FontStyles.Normal, ThemeTextKind.Body);
        Chrome(line, PcType.Caption);
        line.raycastTarget = false;
        hint.gameObject.SetActive(false);

        HoverHint hover = GetOrAdd<HoverHint>(control.gameObject);
        var so = new SerializedObject(hover);
        Wire(so, "hint", hint.gameObject);
        so.ApplyModifiedProperties();
        return line;
    }

    /// <summary>An outline <paramref name="width"/> thick just inside <paramref name="parent"/>'s rect: four bars in <paramref name="colour"/> (role <paramref name="role"/>), outside any layout, never a raycast target.</summary>
    private static Transform BuildFrame(Transform parent, string name, float width, Color colour, ThemeRoleId role)
    {
        Transform frame = Panel(parent, name, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        GetOrAdd<LayoutElement>(frame.gameObject).ignoreLayout = true;
        FrameBar(frame, "Top", new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -width / 2f), new Vector2(0f, width), colour, role);
        FrameBar(frame, "Bottom", Vector2.zero, new Vector2(1f, 0f), new Vector2(0f, width / 2f), new Vector2(0f, width), colour, role);
        FrameBar(frame, "Left", Vector2.zero, new Vector2(0f, 1f), new Vector2(width / 2f, 0f), new Vector2(width, 0f), colour, role);
        FrameBar(frame, "Right", new Vector2(1f, 0f), Vector2.one, new Vector2(-width / 2f, 0f), new Vector2(width, 0f), colour, role);
        frame.SetAsLastSibling();
        return frame;
    }

    /// <summary>One bar of a frame (no raycast).</summary>
    private static void FrameBar(Transform frame, string name, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size, Color colour, ThemeRoleId role) =>
        Panel(frame, name, aMin, aMax, pos, size, colour, role).GetComponent<Image>().raycastTarget = false;

    /// <summary>
    /// The ↗ (LK2): a clear 28 u button (role <paramref name="role"/>) holding
    /// the drawn glyph in the link ink, with a hover hint at its lower left
    /// whose text (<paramref name="hint"/>) says where the link goes.
    /// </summary>
    private static Button BuildLinkButton(Transform parent, string name, ThemeRoleId role, out TMP_Text hint)
    {
        Transform box = Panel(parent, name, Center, Center, Vector2.zero, new Vector2(LinkSize, LinkSize), Color.clear, role);
        Button button = GetOrAdd<Button>(box.gameObject);
        button.transition = Selectable.Transition.None;
        button.targetGraphic = box.GetComponent<Image>();

        Transform glyph = Panel(box, "Glyph", Center, Center, Vector2.zero, new Vector2(LinkSize, LinkSize), null);
        LinkBar(glyph, "Shaft", new Vector2(-0.5f, -0.5f), new Vector2(2.5f, 13.5f), -45f, role);
        LinkBar(glyph, "HeadTop", new Vector2(2f, 5f), new Vector2(8f, 2.5f), 0f, role);
        LinkBar(glyph, "HeadSide", new Vector2(5f, 2f), new Vector2(2.5f, 8f), 0f, role);

        hint = BuildHoverHint(button, null, string.Empty, Vector2.zero, new Vector2(1f, 1f));
        return button;
    }

    /// <summary>One bar of the ↗ glyph, in the link ink (diegetic: never themed), rotated by <paramref name="angle"/>.</summary>
    private static void LinkBar(Transform glyph, string name, Vector2 centre, Vector2 size, float angle, ThemeRoleId role)
    {
        Transform bar = Panel(glyph, name, Center, Center, centre, size, LinkInk, role);
        bar.localRotation = Quaternion.Euler(0f, 0f, angle);
        bar.GetComponent<Image>().raycastTarget = false;
    }
}
