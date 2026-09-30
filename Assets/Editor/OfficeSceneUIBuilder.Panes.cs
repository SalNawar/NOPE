using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The office builder's Investigation app panes (redesign phase 18; the PC
/// spec's AP2, AP9, LK2, CM3; the PC UX redesign IA6, IA7, C4, C11): a pane
/// (AppPane) with its header (the title naming the source and the item it
/// shows, wrapping to two lines rather than being cut; Pin; Open beside in
/// the left pane, Close in the right one; a hairline under it and the
/// active pane's accent underline), its content with a view per source and
/// the no-case state (its words wrapping and shrinking to fit a split
/// pane); the ↗ (a drawn glyph in the link ink, 28 u, with its hover hint)
/// and the found outline, which the forms' FormView clones; and the hover
/// hints of the chrome, sized to their words. Rebuilt fresh with the app
/// (its one convergence policy, audit R6-008); every reference is checked
/// (Wire, audit R6-004). Part of <see cref="OfficeSceneUIBuilder"/>;
/// BuildInvestigationApp calls it.
/// </summary>
public static partial class OfficeSceneUIBuilder
{
    /// <summary>The active pane's accent underline's height.</summary>
    private const float AppFrameWidth = 3f;

    /// <summary>The no-case state's words: their size, and the least they shrink to (wrapping onto a second line first) in a split pane.</summary>
    private const float NoCaseText = 80f, NoCaseTextMin = 40f;

    /// <summary>The found mark's outline width.</summary>
    private const float FoundFrameWidth = 2f;

    /// <summary>The ↗'s hit box (a square).</summary>
    private const float LinkSize = 28f;

    /// <summary>A pane header's icon button (Pin, Open beside, Close: a drawn glyph, its name in a hover hint).</summary>
    private const float HeaderIconSize = 44f;

    /// <summary>The accent (the active pane's underline, the found mark): the focus ring's built colour.</summary>
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
    }

    /// <summary>
    /// A pane named <paramref name="name"/> filling <paramref name="area"/>
    /// (the app lays the two out at runtime): the header (its title, Pin, and
    /// Open beside when <paramref name="left"/>, else Close, returned in
    /// <paramref name="split"/>), the content with the six views and the
    /// no-case state, the active underline; it shows <paramref name="start"/>
    /// first and its rows pick into <paramref name="compare"/>. Its views come
    /// back in <paramref name="views"/>.
    /// </summary>
    private static AppPane BuildAppPane(Transform area, string name, AppTab start, CompareController compare, DesktopConfigSO config, bool left,
                                        out PaneViews views, out Button split)
    {
        Transform paneRoot = Panel(area, name, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        AppPane pane = paneRoot.gameObject.AddComponent<AppPane>();

        float headerHeight = PcSize.PaneHeader;
        Transform header = Panel(paneRoot, "PaneHeader", new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -headerHeight / 2f),
                                 new Vector2(0f, headerHeight), Paper, ThemeRoleId.WindowBody);
        float buttons = PcSize.S + 2f * HeaderIconSize + 4f + PcSize.S;
        TMP_Text title = Text(header, "TitleText", UiText.Get(AppTabKeys[start]), PcType.Body, TextAlignmentOptions.MidlineLeft, Vector2.zero, Vector2.one, Ink,
                              ThemeRoleId.WindowBody, kind: ThemeTextKind.Heading);
        PlaceRect(title.transform, Vector2.zero, Vector2.one, new Vector2(PcSize.L, 2f), new Vector2(-buttons, -2f));
        Chrome(title, PcType.Body, true);
        title.lineSpacing = -12f;
        title.richText = true;
        title.raycastTarget = false;

        split = HeaderIcon(header, left ? "BesideButton" : "CloseButton", left ? "app.pane.beside" : "app.pane.close", PcSize.S);
        if (left)
        {
            // Open beside: two panes side by side.
            IconOutline(split.transform, "Left", new Vector2(-6f, 0f), new Vector2(11f, 18f));
            IconOutline(split.transform, "Right", new Vector2(6f, 0f), new Vector2(11f, 18f));
        }
        else
        {
            ButtonStroke(split.transform, "Stroke1", Vector2.zero, new Vector2(2.5f, 20f), 45f);
            ButtonStroke(split.transform, "Stroke2", Vector2.zero, new Vector2(2.5f, 20f), -45f);
        }
        Button pin = HeaderIcon(header, "PinButton", "app.pin", PcSize.S + HeaderIconSize + 4f);
        Transform head = Panel(pin.transform, "Head", Center, Center, new Vector2(0f, 5f), new Vector2(12f, 12f), Ink);
        Image headImage = head.GetComponent<Image>();
        headImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        headImage.raycastTarget = false;
        SceneUiKit.Tag(headImage, ThemeRoleId.Button, ThemePart.Ink);
        ButtonStroke(pin.transform, "Needle", new Vector2(0f, -5f), new Vector2(2.5f, 12f), 0f);

        Transform rule = Panel(header, "Rule", Vector2.zero, new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(0f, 2f), XpFace, ThemeRoleId.Sidebar);
        rule.GetComponent<Image>().raycastTarget = false;
        Transform underline = Panel(header, "ActiveFrame", Vector2.zero, new Vector2(1f, 0f), new Vector2(0f, AppFrameWidth / 2f), new Vector2(0f, AppFrameWidth),
                                    AccentInk, ThemeRoleId.FocusRing);
        underline.GetComponent<Image>().raycastTarget = false;
        underline.gameObject.SetActive(false);

        Transform content = Panel(paneRoot, "Content", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Paper, ThemeRoleId.WindowBody);
        PlaceRect(content, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0f, -headerHeight));
        content.gameObject.AddComponent<RectMask2D>();

        views = new PaneViews
        {
            Documents = BuildDocumentsView(content, out AppView documents),
            Records = BuildRecordsView(content, compare),
            Reference = BuildReferenceView(content),
            Transcript = BuildTranscriptView(content),
            Report = BuildReportView(content),
            Rules = BuildRulesView(content),
        };

        Transform noCase = Panel(content, "NoCase", new Vector2(0.03f, 0.38f), new Vector2(0.97f, 0.62f), Vector2.zero, Vector2.zero, ScreenStripColor, ThemeRoleId.ScreenStrip);
        noCase.GetComponent<Image>().raycastTarget = false;
        TMP_Text noCaseText = Text(noCase, "Text", null, Mathf.RoundToInt(NoCaseText), TextAlignmentOptions.Center, Vector2.zero, Vector2.one, Color.white,
                                   ThemeRoleId.ScreenStrip, "idle.waiting", FontStyles.Bold, ThemeTextKind.Heading);
        SetAnchors(noCaseText.transform, new Vector2(0.02f, 0f), new Vector2(0.98f, 1f));
        noCaseText.raycastTarget = false;
        noCaseText.textWrappingMode = TextWrappingModes.Normal;
        noCaseText.enableAutoSizing = true;
        noCaseText.fontSizeMax = NoCaseText;
        noCaseText.fontSizeMin = NoCaseTextMin;
        noCase.gameObject.SetActive(false);

        var so = new SerializedObject(pane);
        SerializedArrays.Set(so, "views", new Object[] { documents, views.Records, views.Reference, views.Transcript, views.Report, views.Rules });
        Wire(so, "titleText", title);
        Wire(so, "noCase", noCase.gameObject);
        Wire(so, "activeFrame", underline.gameObject);
        so.FindProperty("startTab").enumValueIndex = (int)start;
        Wire(so, "config", config);
        so.ApplyModifiedProperties();
        return pane;
    }

    /// <summary>A pane header's icon button (Pin, Open beside, Close): HeaderIconSize square, <paramref name="right"/> from the header's right end, its label gone (the caller draws its glyph) and its name (<paramref name="hintKey"/>) in a hover hint under it.</summary>
    private static Button HeaderIcon(Transform header, string name, string hintKey, float right)
    {
        Button button = MakeButton(header, name, null, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), null, ThemeRoleId.Button);
        PlaceRect(button.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-(right + HeaderIconSize), -HeaderIconSize / 2f), new Vector2(-right, HeaderIconSize / 2f));
        DestroyChildIfPresent(button.transform, "Label");
        BuildHoverHint(button, hintKey, null, new Vector2(1f, 0f), new Vector2(1f, 1f));
        return button;
    }

    /// <summary>A drawn outlined rectangle (four strokes in the Button role's ink) centred at <paramref name="centre"/>: a glyph's part.</summary>
    private static void IconOutline(Transform parent, string name, Vector2 centre, Vector2 size)
    {
        const float w = 2.5f;
        Transform box = Panel(parent, name, Center, Center, centre, size, null);
        ButtonStroke(box, "Top", new Vector2(0f, size.y / 2f - w / 2f), new Vector2(size.x, w), 0f);
        ButtonStroke(box, "Bottom", new Vector2(0f, -size.y / 2f + w / 2f), new Vector2(size.x, w), 0f);
        ButtonStroke(box, "Left", new Vector2(-size.x / 2f + w / 2f, 0f), new Vector2(w, size.y), 0f);
        ButtonStroke(box, "Right", new Vector2(size.x / 2f - w / 2f, 0f), new Vector2(w, size.y), 0f);
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
