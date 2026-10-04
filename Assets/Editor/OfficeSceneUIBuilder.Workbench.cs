using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The office builder's workbench parts of the Investigation app (the PC
/// workbench spec §2, §3, §5; the redesign-existing-projects and
/// minimalist-ui skills' principles within uGUI): white surfaces parted by
/// hairlines, colour only for state, sentence-case labels at the type scale,
/// quiet buttons with a hairline, one primary button per view. The header
/// (the face, the name, the counters, the step pills), the shelf (its flow,
/// a group's and a chip's templates, Search), the lead, the status line (one
/// plate per state), the line layer over the panes, the decision step, the
/// findings column, the foot and the Calendar view. Neutral colours are
/// baked (the theme recolours every tagged graphic at load). Part of
/// <see cref="OfficeSceneUIBuilder"/>; BuildInvestigationApp calls it.
/// </summary>
public static partial class OfficeSceneUIBuilder
{
    /// <summary>The workbench's sizes (desktop units; the spec's §3).</summary>
    private static class WbSize
    {
        /// <summary>The side padding of the header, the shelf, the foot and the main column.</summary>
        public const float Pad = 24f;

        /// <summary>The gap between the main column and the findings, and above and below the work.</summary>
        public const float Gap = 16f;

        /// <summary>The header's height.</summary>
        public const float Header = 96f;

        /// <summary>The shelf's height before its rows are laid out (two rows).</summary>
        public const float ShelfStart = 110f;

        /// <summary>The shelf's padding above and below its rows.</summary>
        public const float ShelfPad = 12f;

        /// <summary>A chip's height.</summary>
        public const float Chip = 40f;

        /// <summary>The Search button's width (at the lead's right).</summary>
        public const float Search = 230f;

        /// <summary>The foot's height.</summary>
        public const float Foot = 76f;

        /// <summary>The findings column's width.</summary>
        public const float Findings = 280f;

        /// <summary>The lead's height (the step's title, the Search button at its right).</summary>
        public const float Lead = 44f;

        /// <summary>The status line's height (two lines at Caption size, in the tallest culture font).</summary>
        public const float Status = 68f;

        /// <summary>The panes' top under the main column's top: the lead, a gap, the status line, a gap.</summary>
        public const float PanesTop = Lead + 8f + Status + 12f;

        /// <summary>A step pill's height.</summary>
        public const float Pill = 52f;

        /// <summary>The steps' row width at the header's right.</summary>
        public const float Steps = 800f;

        /// <summary>The traveller's face.</summary>
        public const float Face = 64f;

        /// <summary>A pane's header (room for a document's name on two lines).</summary>
        public const float PaneHead = 64f;

        /// <summary>The name's size.</summary>
        public const int Name = 40;

        /// <summary>The lead's title size.</summary>
        public const int LeadTitle = 30;
    }

    /// <summary>The neutral look, baked (the theme's roles recolour it at load): white surfaces on a warm screen, hairlines, ink, muted ink, the states' plates.</summary>
    private static readonly Color WbSurface = Color.white, WbScreen = Hex(0xFBFBFA), WbLine = Hex(0xE6E4DF), WbLineStrong = Hex(0xC9C6BF),
                                  WbInk = Hex(0x1D1F21), WbMuted = Hex(0x6F6E6A), WbOkBg = Hex(0xEDF3EC), WbOk = Hex(0x2F5E33),
                                  WbWarnBg = Hex(0xFDEBEC), WbWarn = Hex(0x9A2E2C), WbInfoBg = Hex(0xF3F2EF), WbHoldBg = Hex(0xE1F3FE),
                                  WbHold = Hex(0x1F6C9F), WbAction = Hex(0x1C2636);

    /// <summary>An opaque colour from 0xRRGGBB.</summary>
    private static Color Hex(int rgb) => new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, 1f);

    /// <summary>The header's parts the app and the steps take.</summary>
    private struct AppHeader
    {
        public Transform Root;
        public TravellerPortraitView Face;
        public TMP_Text Name;
        public TMP_Text Counters;
        public List<Object> Pills;
    }

    /// <summary>The lead's title and Search.</summary>
    private struct AppLead
    {
        public TMP_Text Title;
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

    /// <summary>The foot's controls.</summary>
    private struct AppFoot
    {
        public Button Back;
        public Button Next;
        public TMP_Text NextLabel;
        public TMP_Text Progress;
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

    /// <summary>The primary button: filled with the culture's deep colour, its label bold at Body size.</summary>
    private static Button PrimaryButton(Transform parent, string name, string key)
    {
        Button button = MakeButton(parent, name, null, Vector2.zero, Vector2.one, WbAction, ThemeRoleId.PrimaryAction, key);
        TMP_Text label = ButtonLabel(button, PcType.Body, TextAlignmentOptions.Center, PcSize.L);
        label.fontStyle = FontStyles.Bold;
        SceneUiKit.Tag(label, ThemeRoleId.PrimaryAction, ThemePart.Ink, key, FontStyles.Bold, ThemeTextKind.Button, key != null);
        return button;
    }

    /// <summary>A text link: no plate, its keyed label muted and underlined at Body size.</summary>
    private static Button LinkButton(Transform parent, string name, string key)
    {
        Button button = MakeButton(parent, name, null, Vector2.zero, Vector2.one, new Color(1f, 1f, 1f, 0f), ThemeRoleId.ClickCatcher);
        TMP_Text label = ButtonLabel(button, PcType.Body, TextAlignmentOptions.MidlineLeft, 4f);
        label.fontStyle = FontStyles.Underline;
        label.text = UiText.Get(key);
        label.color = WbMuted;
        SceneUiKit.Tag(label, ThemeRoleId.SurfaceMuted, ThemePart.Ink, key, FontStyles.Underline, ThemeTextKind.Button, false);
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

    // ----------------------------- the header -----------------------------

    /// <summary>
    /// The header (IA1, IA2): on a surface with a hairline under it, the
    /// traveller's face (a 64-unit box holding the portrait's layers over a
    /// warm grey, framed), their name (40 u, bold, shrinking no further than
    /// 30 u) over the counters (Caption, muted), and at its right the steps'
    /// pills (BuildStepPill), one per CaseGuide stage.
    /// </summary>
    private static AppHeader BuildAppHeader(Transform body)
    {
        Transform root = Panel(body, "Header", new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -WbSize.Header / 2f), new Vector2(0f, WbSize.Header), WbSurface,
                               ThemeRoleId.Surface);
        HairlineEdge(root, "Rule", 1);

        Transform faceBox = Panel(root, "Face", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(WbSize.Pad + WbSize.Face / 2f, 0f),
                                  new Vector2(WbSize.Face, WbSize.Face), WbInfoBg, ThemeRoleId.Info);
        GetOrAdd<RectMask2D>(faceBox.gameObject);
        TravellerPortraitView face = BuildPortrait(faceBox);
        HairlineFrame(faceBox);

        float textLeft = WbSize.Pad + WbSize.Face + 20f, textRight = WbSize.Steps + WbSize.Pad + PcSize.L;
        TMP_Text name = WbText(root, "Name", null, UiText.Get("app.title"), WbSize.Name, ThemeRoleId.Surface, TextAlignmentOptions.BottomLeft, FontStyles.Bold);
        PlaceRect(name.transform, new Vector2(0f, 0.5f), Vector2.one, new Vector2(textLeft, -8f), new Vector2(-textRight, -2f));
        name.enableAutoSizing = true;
        name.fontSizeMax = WbSize.Name;
        name.fontSizeMin = PcType.Caption;
        TMP_Text counters = WbText(root, "Counters", null, UiText.Get("idle.waiting"), PcType.Caption, ThemeRoleId.SurfaceMuted, TextAlignmentOptions.TopLeft);
        PlaceRect(counters.transform, Vector2.zero, new Vector2(1f, 0.5f), new Vector2(textLeft, 2f), new Vector2(-textRight, -8f));

        Transform steps = Panel(root, "Steps", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-WbSize.Pad - WbSize.Steps / 2f, 0f),
                                new Vector2(WbSize.Steps, WbSize.Pill), null);
        HorizontalLayoutGroup row = GetOrAdd<HorizontalLayoutGroup>(steps.gameObject);
        row.spacing = 6f;
        row.childAlignment = TextAnchor.MiddleRight;
        row.childControlWidth = true;
        row.childControlHeight = true;
        row.childForceExpandWidth = false;
        row.childForceExpandHeight = true;
        var pills = new List<Object>();
        for (int i = 0; i < CaseGuide.Stages.Count; i++)
            pills.Add(BuildStepPill(steps, CaseGuide.Stages[i], i + 1));

        return new AppHeader { Root = root, Face = face, Name = name, Counters = counters, Pills = pills };
    }

    /// <summary>
    /// One step's pill (C: the prototype's step button): a clear button whose
    /// row holds its circle and its name; the current step's plate (a surface
    /// with a hairline) behind it; three circles (the number on warm grey,
    /// the number on the primary colour while current, a tick on the match
    /// plate once done) and two labels (muted, and bold while current); the
    /// guide shows the right ones.
    /// </summary>
    private static Button BuildStepPill(Transform steps, GuideStage stage, int number)
    {
        string key = "guide." + stage.ToString().ToLowerInvariant() + ".name";
        Button pill = MakeButton(steps, "Step_" + stage, null, Vector2.zero, Vector2.one, new Color(1f, 1f, 1f, 0f), ThemeRoleId.ClickCatcher);
        DestroyChildIfPresent(pill.transform, "Label");
        HorizontalLayoutGroup row = GetOrAdd<HorizontalLayoutGroup>(pill.gameObject);
        row.padding = new RectOffset(12, 16, 0, 0);
        row.spacing = 10f;
        row.childAlignment = TextAnchor.MiddleLeft;
        row.childControlWidth = true;
        row.childControlHeight = true;
        row.childForceExpandWidth = false;
        row.childForceExpandHeight = false;

        Transform current = Panel(pill.transform, "Current", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, WbSurface, ThemeRoleId.Surface);
        current.GetComponent<Image>().raycastTarget = false;
        GetOrAdd<LayoutElement>(current.gameObject).ignoreLayout = true;
        HairlineFrame(current);
        current.gameObject.SetActive(false);

        StepCircle(pill.transform, "Circle", number.ToString(), WbInfoBg, ThemeRoleId.Info, false);
        StepCircle(pill.transform, "CircleCurrent", number.ToString(), WbAction, ThemeRoleId.PrimaryAction, false).gameObject.SetActive(false);
        StepCircle(pill.transform, "CircleDone", null, WbOkBg, ThemeRoleId.FindingMatch, true).gameObject.SetActive(false);

        TMP_Text label = WbText(pill.transform, "Label", key, null, PcType.Body, ThemeRoleId.SurfaceMuted, TextAlignmentOptions.MidlineLeft);
        label.overflowMode = TextOverflowModes.Overflow;
        TMP_Text chosen = WbText(pill.transform, "LabelCurrent", key, null, PcType.Body, ThemeRoleId.Surface, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        chosen.overflowMode = TextOverflowModes.Overflow;
        chosen.gameObject.SetActive(false);
        return pill;
    }

    /// <summary>A step's 30-unit circle in <paramref name="role"/>'s colours: its number, or a drawn tick (<paramref name="tick"/>).</summary>
    private static Transform StepCircle(Transform pill, string name, string number, Color fill, ThemeRoleId role, bool tick)
    {
        Transform circle = Panel(pill, name, Center, Center, Vector2.zero, new Vector2(30f, 30f), fill, role);
        Image image = circle.GetComponent<Image>();
        image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        image.raycastTarget = false;
        LayoutElement size = GetOrAdd<LayoutElement>(circle.gameObject);
        size.minWidth = size.preferredWidth = 30f;
        size.minHeight = size.preferredHeight = 30f;
        if (tick)
        {
            GlyphBar(circle, "Stroke1", new Vector2(-3.5f, -1.5f), new Vector2(3f, 8f), 45f, role);
            GlyphBar(circle, "Stroke2", new Vector2(3f, 1.5f), new Vector2(3f, 15f), -45f, role);
            foreach (Image bar in circle.GetComponentsInChildren<Image>(true))
                if (bar != image)
                    bar.color = WbOk;
        }
        else
        {
            TMP_Text text = WbText(circle, "Number", null, number, PcType.Caption, role, TextAlignmentOptions.Center, FontStyles.Bold);
            text.overflowMode = TextOverflowModes.Overflow;
        }
        return circle;
    }

    // ----------------------------- the shelf -----------------------------

    /// <summary>
    /// The shelf (IA4): a surface under the header with a hairline under it,
    /// its flow (FlowLayoutGroup) of a group label's template (its caps word,
    /// kept with the next chip) and a chip's template (its unread dot, its name
    /// and its side's plate, on the screen's paper with a hairline, dimmed by
    /// its CanvasGroup when not readable). Its height is ShelfView's.
    /// </summary>
    private static ShelfView BuildShelf(Transform body)
    {
        var root = (RectTransform)Panel(body, "Shelf", new Vector2(0f, 1f), Vector2.one, Vector2.zero, Vector2.zero, WbSurface, ThemeRoleId.Surface);
        root.pivot = new Vector2(0.5f, 1f);
        root.anchoredPosition = new Vector2(0f, -WbSize.Header);
        root.sizeDelta = new Vector2(0f, WbSize.ShelfStart);
        HairlineEdge(root, "Rule", 1);

        var flow = (RectTransform)Panel(root, "Flow", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        PlaceRect(flow, Vector2.zero, Vector2.one, new Vector2(WbSize.Pad, WbSize.ShelfPad), new Vector2(-WbSize.Pad, -WbSize.ShelfPad));
        FlowLayoutGroup layout = GetOrAdd<FlowLayoutGroup>(flow.gameObject);
        var soFlow = new SerializedObject(layout);
        soFlow.FindProperty("spacingX").floatValue = 8f;
        soFlow.FindProperty("spacingY").floatValue = 6f;
        soFlow.ApplyModifiedProperties();

        TMP_Text groupLabel = WbText(flow, "LabelTemplate", null, "Papers", PcType.Caption, ThemeRoleId.SurfaceMuted, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        groupLabel.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
        groupLabel.characterSpacing = 4f;
        groupLabel.overflowMode = TextOverflowModes.Overflow;
        groupLabel.margin = new Vector4(PcSize.L, 0f, 2f, 0f);
        SceneUiKit.Tag(groupLabel, ThemeRoleId.SurfaceMuted, ThemePart.Ink, null, FontStyles.Bold | FontStyles.UpperCase, ThemeTextKind.Heading, false);
        GetOrAdd<LayoutElement>(groupLabel.gameObject).minHeight = WbSize.Chip;
        GetOrAdd<FlowKeepWithNext>(groupLabel.gameObject);
        groupLabel.gameObject.SetActive(false);

        Button chip = MakeButton(flow, "ChipTemplate", null, Vector2.zero, Vector2.one, WbScreen, ThemeRoleId.WindowBody);
        HorizontalLayoutGroup chipRow = GetOrAdd<HorizontalLayoutGroup>(chip.gameObject);
        chipRow.padding = new RectOffset(12, 12, 0, 0);
        chipRow.spacing = 8f;
        chipRow.childAlignment = TextAnchor.MiddleLeft;
        chipRow.childControlWidth = true;
        chipRow.childControlHeight = true;
        chipRow.childForceExpandWidth = false;
        chipRow.childForceExpandHeight = false;
        LayoutElement chipSize = GetOrAdd<LayoutElement>(chip.gameObject);
        chipSize.minHeight = chipSize.preferredHeight = WbSize.Chip;
        GetOrAdd<CanvasGroup>(chip.gameObject);
        HairlineFrame(chip.transform);

        Transform dot = Panel(chip.transform, "Unread", Center, Center, Vector2.zero, new Vector2(10f, 10f), WbHold);
        Image dotImage = dot.GetComponent<Image>();
        dotImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        dotImage.raycastTarget = false;
        SceneUiKit.Tag(dotImage, ThemeRoleId.Holding, ThemePart.Ink);
        LayoutElement dotSize = GetOrAdd<LayoutElement>(dot.gameObject);
        dotSize.minWidth = dotSize.preferredWidth = 10f;
        dotSize.minHeight = dotSize.preferredHeight = 10f;
        dot.SetSiblingIndex(0);
        dot.gameObject.SetActive(false);

        TMP_Text chipLabel = chip.transform.Find("Label").GetComponent<TMP_Text>();
        Chrome(chipLabel, PcType.Caption);
        chipLabel.alignment = TextAlignmentOptions.MidlineLeft;
        chipLabel.raycastTarget = false;
        chipLabel.color = WbInk;

        Transform side = Panel(chip.transform, "Side", Center, Center, Vector2.zero, new Vector2(60f, 30f), WbAction, ThemeRoleId.PrimaryAction);
        side.GetComponent<Image>().raycastTarget = false;
        HorizontalLayoutGroup sidePad = GetOrAdd<HorizontalLayoutGroup>(side.gameObject);
        sidePad.padding = new RectOffset(8, 8, 2, 2);
        sidePad.childControlWidth = true;
        sidePad.childControlHeight = true;
        sidePad.childForceExpandWidth = false;
        sidePad.childForceExpandHeight = false;
        TMP_Text sideText = WbText(side, "Text", null, "Left", PcType.Caption, ThemeRoleId.PrimaryAction, TextAlignmentOptions.Center, FontStyles.Bold);
        sideText.overflowMode = TextOverflowModes.Overflow;
        side.gameObject.SetActive(false);
        chip.gameObject.SetActive(false);

        ShelfView shelf = GetOrAdd<ShelfView>(root.gameObject);
        var so = new SerializedObject(shelf);
        Wire(so, "flow", flow);
        Wire(so, "labelTemplate", groupLabel);
        Wire(so, "chipTemplate", chip);
        so.FindProperty("padding").floatValue = WbSize.ShelfPad;
        so.ApplyModifiedProperties();
        return shelf;
    }

    // ----------------------------- the work -----------------------------

    /// <summary>The lead (W1): the step's title (30 u, bold) and, at its right, Search (Ctrl+K; the drawer); the step's sentence is the status line's hint.</summary>
    private static AppLead BuildLead(Transform main)
    {
        Transform root = Panel(main, "Lead", new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -WbSize.Lead / 2f), new Vector2(0f, WbSize.Lead), null);
        TMP_Text title = WbText(root, "Title", null, UiText.Get("idle.waiting"), WbSize.LeadTitle, ThemeRoleId.WindowBody, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        PlaceRect(title.transform, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-(WbSize.Search + PcSize.L), 0f));
        Button search = QuietButton(root, "SearchButton", "app.search.open");
        PlaceRect(search.transform, new Vector2(1f, 0f), Vector2.one, new Vector2(-WbSize.Search, 0f), Vector2.zero);
        return new AppLead { Title = title, Search = search };
    }

    /// <summary>
    /// The status line (§4.1): one plate per state, each in its role's colours
    /// with its line (Caption, wrapping onto two): the hint (Info), what is
    /// held with Cancel (Holding), a match (FindingMatch), a difference
    /// (FindingDiffer) and a note (Info); the board shows one.
    /// </summary>
    private static AppStatus BuildStatusLine(Transform main)
    {
        Transform root = Panel(main, "Status", new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -(WbSize.Lead + 8f + WbSize.Status / 2f)), new Vector2(0f, WbSize.Status), null);
        var status = new AppStatus
        {
            Root = root,
            Idle = StatusPlate(root, "Idle", WbInfoBg, ThemeRoleId.Info, 0f),
            Hold = StatusPlate(root, "Hold", WbHoldBg, ThemeRoleId.Holding, 226f),
            Match = StatusPlate(root, "Match", WbOkBg, ThemeRoleId.FindingMatch, 0f),
            Differ = StatusPlate(root, "Differ", WbWarnBg, ThemeRoleId.FindingDiffer, 0f),
            Info = StatusPlate(root, "Note", WbInfoBg, ThemeRoleId.Info, 0f),
        };
        Transform hold = root.Find("Hold");
        status.Cancel = QuietButton(hold, "CancelButton", "status.cancel");
        PlaceRect(status.Cancel.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-(210f + 8f), -22f), new Vector2(-8f, 22f));
        foreach (string plate in new[] { "Hold", "Match", "Differ", "Note" })
            root.Find(plate).gameObject.SetActive(false);
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

    /// <summary>The line layer over the two panes (MatchLines, last in the panes' area so it draws over both): its label plate (the primary colour, the label in caps) hidden until a line shows.</summary>
    private static MatchLines BuildMatchLines(Transform panes)
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

        Transform plate = Panel(go.transform, "Label", Center, Center, Vector2.zero, new Vector2(200f, 36f), WbAction, ThemeRoleId.PrimaryAction);
        plate.GetComponent<Image>().raycastTarget = false;
        HorizontalLayoutGroup pad = GetOrAdd<HorizontalLayoutGroup>(plate.gameObject);
        pad.padding = new RectOffset(12, 12, 4, 4);
        pad.childControlWidth = true;
        pad.childControlHeight = true;
        pad.childForceExpandWidth = false;
        pad.childForceExpandHeight = false;
        ContentSizeFitter fit = GetOrAdd<ContentSizeFitter>(plate.gameObject);
        fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        TMP_Text label = WbText(plate, "Text", null, "Matching data", PcType.Caption, ThemeRoleId.PrimaryAction, TextAlignmentOptions.Center, FontStyles.Bold);
        label.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
        label.characterSpacing = 4f;
        label.overflowMode = TextOverflowModes.Overflow;
        SceneUiKit.Tag(label, ThemeRoleId.PrimaryAction, ThemePart.Ink, null, FontStyles.Bold | FontStyles.UpperCase, ThemeTextKind.Heading, false);
        plate.gameObject.SetActive(false);

        var so = new SerializedObject(lines);
        Wire(so, "labelPlate", plate.GetComponent<Image>());
        Wire(so, "labelText", label);
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
        PlaceRect(root, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0f, -(WbSize.Lead + 8f)));
        TMP_Text summary = WbText(root, "Summary", null, string.Empty, PcType.Body, ThemeRoleId.WindowBody, TextAlignmentOptions.TopLeft, FontStyles.Normal, true);
        PlaceRect(summary.transform, new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -64f), Vector2.zero);

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
        PlaceRect(card.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(x, -(80f + 160f)), new Vector2(x + 460f, -80f));
        TMP_Text word = card.transform.Find("Label").GetComponent<TMP_Text>();
        Chrome(word, WbSize.LeadTitle);
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
    /// The findings column (IA8, §4.4): a surface at the work area's right with
    /// a hairline at its left, its heading (caps, muted) and "Open the report"
    /// (a text link), the scrolling list of findings (a plate's template: a
    /// match plate and a difference plate, each with its title and detail
    /// wrapping) and the line shown while nothing is logged.
    /// </summary>
    private static FindingsView BuildFindingsColumn(Transform work, out RectTransform column)
    {
        column = (RectTransform)Panel(work, "Findings", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, WbSurface, ThemeRoleId.Surface);
        PlaceRect(column, new Vector2(1f, 0f), Vector2.one, new Vector2(-WbSize.Findings, 0f), Vector2.zero);
        HairlineEdge(column, "Rule", 2);

        TMP_Text heading = WbText(column, "Heading", "findings.title", null, PcType.Caption, ThemeRoleId.SurfaceMuted, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        heading.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
        heading.characterSpacing = 4f;
        SceneUiKit.Tag(heading, ThemeRoleId.SurfaceMuted, ThemePart.Ink, "findings.title", FontStyles.Bold | FontStyles.UpperCase, ThemeTextKind.Heading, false);
        PlaceRect(heading.transform, new Vector2(0f, 1f), Vector2.one, new Vector2(20f, -56f), new Vector2(-20f, -12f));
        Button report = LinkButton(column, "ReportButton", "findings.report");
        PlaceRect(report.transform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(16f, 12f), new Vector2(-16f, 56f));

        RectTransform list = BuildScrollList(column, "List", Vector2.zero, Vector2.one, 8f, WbSurface, ThemeRoleId.Surface);
        Transform box = list.parent.parent;
        PlaceRect(box, Vector2.zero, Vector2.one, new Vector2(12f, 64f), new Vector2(-12f, -60f));
        TMP_Text empty = WbText(box, "Empty", "findings.empty", null, PcType.Caption, ThemeRoleId.SurfaceMuted, TextAlignmentOptions.TopLeft, FontStyles.Normal, true);
        PlaceRect(empty.transform, Vector2.zero, Vector2.one, new Vector2(8f, 8f), new Vector2(-8f, -8f));

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
        Wire(so, "emptyText", empty.gameObject);
        Wire(so, "reportButton", report);
        so.ApplyModifiedProperties();
        return view;
    }

    /// <summary>A finding's plate in <paramref name="role"/>'s colours: its title over its detail, both wrapping.</summary>
    private static Transform FindingPlate(Transform row, string name, Color fill, ThemeRoleId role)
    {
        Transform plate = Panel(row, name, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, fill, role);
        plate.GetComponent<Image>().raycastTarget = false;
        VerticalLayoutGroup pad = GetOrAdd<VerticalLayoutGroup>(plate.gameObject);
        pad.padding = new RectOffset(12, 12, 10, 10);
        pad.spacing = 2f;
        pad.childControlWidth = true;
        pad.childControlHeight = true;
        pad.childForceExpandWidth = true;
        pad.childForceExpandHeight = false;
        foreach (string text in new[] { "Title", "Detail" })
        {
            TMP_Text line = LayoutText(plate, text, PcType.Caption, FontStyles.Normal, role);
            line.color = role == ThemeRoleId.FindingMatch ? WbOk : WbWarn;
        }
        return plate;
    }

    /// <summary>The foot (§3): a surface with a hairline over it: Back (a text link), the progress line (Caption, muted) and Next (the primary button).</summary>
    private static AppFoot BuildAppFoot(Transform body)
    {
        Transform root = Panel(body, "Foot", Vector2.zero, new Vector2(1f, 0f), new Vector2(0f, WbSize.Foot / 2f), new Vector2(0f, WbSize.Foot), WbSurface, ThemeRoleId.Surface);
        HairlineEdge(root, "Rule", 0);
        Button back = LinkButton(root, "BackButton", "guide.back");
        PlaceRect(back.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(WbSize.Pad, -22f), new Vector2(WbSize.Pad + 120f, 22f));
        TMP_Text progress = WbText(root, "Progress", null, string.Empty, PcType.Caption, ThemeRoleId.SurfaceMuted, TextAlignmentOptions.MidlineLeft, FontStyles.Normal, true);
        progress.lineSpacing = -8f;
        progress.overflowMode = TextOverflowModes.Ellipsis;
        PlaceRect(progress.transform, Vector2.zero, Vector2.one, new Vector2(WbSize.Pad + 120f + PcSize.L, 0f), new Vector2(-(WbSize.Pad + 360f + PcSize.L), 0f));
        Button next = PrimaryButton(root, "NextButton", null);
        PlaceRect(next.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-(WbSize.Pad + 360f), -26f), new Vector2(-WbSize.Pad, 26f));
        return new AppFoot { Back = back, Next = next, NextLabel = next.transform.Find("Label").GetComponent<TMP_Text>(), Progress = progress };
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
