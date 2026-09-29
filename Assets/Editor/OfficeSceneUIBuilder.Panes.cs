using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The office builder's Investigation app panes (redesign phase 18; the PC
/// spec's AP2-AP4, AP9, LK2, CM3): a pane (AppPane) with its tab strip (the
/// six tabs of OfficeSceneUIBuilder.App's BuildTab, each with its glyph for a
/// narrow strip, a tooltip naming it and the handle that drags it or opens
/// its menu), its chip row (BuildChipTemplate), its content with a view per
/// tab and the no-case state, and its 3 u accent frame; the ↗ (a drawn glyph
/// in the link ink, 28 u, with its hover hint) and the found outline, which
/// the forms' FormView clones; and the small hover hints of the toolbar and
/// the tabs. Rebuilt fresh with the app (its one convergence
/// policy, audit R6-008); every reference is checked (Wire, audit R6-004).
/// Part of <see cref="OfficeSceneUIBuilder"/>; BuildInvestigationApp calls it.
/// </summary>
public static partial class OfficeSceneUIBuilder
{
    /// <summary>The active pane's accent frame's width.</summary>
    private const float AppFrameWidth = 3f;

    /// <summary>The found mark's outline width.</summary>
    private const float FoundFrameWidth = 2f;

    /// <summary>The ↗'s hit box (a square).</summary>
    private const float LinkSize = 28f;

    /// <summary>A tab's tooltip, the Split button's hint and a ↗'s hint (their sizes).</summary>
    private static readonly Vector2 AppTabHintSize = new Vector2(200f, 34f), AppSplitHintSize = new Vector2(380f, 34f), AppLinkHintSize = new Vector2(420f, 34f);

    /// <summary>The accent (the active pane's frame, the found mark): the focus ring's built colour.</summary>
    private static readonly Color AccentInk = new Color(0.95f, 0.55f, 0.1f, 1f);

    /// <summary>The link ink of the ↗ on a paper or a row (diegetic: never themed).</summary>
    private static readonly Color LinkInk = new Color(0.12f, 0.3f, 0.72f, 1f);

    /// <summary>The tabs' glyph keys (a narrow strip's inactive tabs), by tab.</summary>
    private static readonly Dictionary<AppTab, string> AppTabGlyphKeys = new Dictionary<AppTab, string>
    {
        { AppTab.Documents, "app.tabGlyph.documents" },
        { AppTab.Records, "app.tabGlyph.records" },
        { AppTab.Reference, "app.tabGlyph.reference" },
        { AppTab.Transcript, "app.tabGlyph.transcript" },
        { AppTab.Report, "app.tabGlyph.report" },
        { AppTab.Rules, "app.tabGlyph.rules" },
    };

    /// <summary>One pane's views, for the façade (each tab's view).</summary>
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
    /// (the app lays the two out at runtime): the tab strip, the chip row, the
    /// content with the six views and the no-case state, the accent frame; it
    /// shows <paramref name="start"/> first and its rows pick into
    /// <paramref name="compare"/>. Its views come back in <paramref name="views"/>.
    /// </summary>
    private static AppPane BuildAppPane(Transform area, string name, AppTab start, CompareController compare, DesktopConfigSO config, out PaneViews views)
    {
        Transform paneRoot = Panel(area, name, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        AppPane pane = paneRoot.gameObject.AddComponent<AppPane>();

        float stripHeight = config.tabStripHeight;
        float rowHeight = config.chipRowHeight;
        Transform strip = Panel(paneRoot, "TabStrip", new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -stripHeight / 2f),
                                new Vector2(0f, stripHeight), XpBlue, ThemeRoleId.TabStrip);
        HorizontalLayoutGroup tabs = GetOrAdd<HorizontalLayoutGroup>(strip.gameObject);
        tabs.padding = new RectOffset(8, 8, 6, 0);
        tabs.spacing = 3f;
        tabs.childAlignment = TextAnchor.LowerLeft;
        tabs.childControlWidth = true;
        tabs.childControlHeight = true;
        tabs.childForceExpandWidth = false;
        tabs.childForceExpandHeight = true;
        var tabButtons = new List<Object>();
        var tabActive = new List<Object>();
        var tabBadges = new List<Object>();
        var tabLabels = new List<Object>();
        var tabGlyphs = new List<Object>();
        foreach (AppTab tab in TabOrder.Default)
        {
            Button button = BuildPaneTab(strip, tab, pane, config, out GameObject active, out GameObject badge, out GameObject label, out GameObject glyph);
            tabButtons.Add(button);
            tabActive.Add(active);
            tabBadges.Add(badge);
            tabLabels.Add(label);
            tabGlyphs.Add(glyph);
        }

        Transform header = Panel(paneRoot, "PaneHeader", new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -(stripHeight + rowHeight / 2f)),
                                 new Vector2(0f, rowHeight), Paper, ThemeRoleId.WindowBody);
        HorizontalLayoutGroup row = GetOrAdd<HorizontalLayoutGroup>(header.gameObject);
        row.padding = new RectOffset(8, 8, 6, 8);
        row.spacing = 6f;
        row.childAlignment = TextAnchor.MiddleLeft;
        row.childControlWidth = true;
        row.childControlHeight = true;
        row.childForceExpandWidth = false;
        row.childForceExpandHeight = true;
        Button chip = BuildChipTemplate(header, config);
        Transform rule = Panel(header, "Rule", Vector2.zero, new Vector2(1f, 0f), new Vector2(0f, AppPaneRuleHeight / 2f), new Vector2(0f, AppPaneRuleHeight),
                               XpBlue, ThemeRoleId.TabStrip);
        rule.GetComponent<Image>().raycastTarget = false;
        GetOrAdd<LayoutElement>(rule.gameObject).ignoreLayout = true;

        Transform content = Panel(paneRoot, "Content", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Paper, ThemeRoleId.WindowBody);
        PlaceRect(content, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0f, -(stripHeight + rowHeight)));
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
        TMP_Text noCaseText = Text(noCase, "Text", null, 80, TextAlignmentOptions.Center, Vector2.zero, Vector2.one, Color.white,
                                   ThemeRoleId.ScreenStrip, "idle.waiting", FontStyles.Bold, ThemeTextKind.Heading, true);
        SetAnchors(noCaseText.transform, new Vector2(0.02f, 0f), new Vector2(0.98f, 1f));
        noCaseText.raycastTarget = false;
        noCase.gameObject.SetActive(false);

        Transform frame = BuildFrame(paneRoot, "ActiveFrame", AppFrameWidth, AccentInk, ThemeRoleId.FocusRing);
        frame.gameObject.SetActive(false);

        var so = new SerializedObject(pane);
        SerializedArrays.Set(so, "tabButtons", tabButtons);
        SerializedArrays.Set(so, "tabActive", tabActive);
        SerializedArrays.Set(so, "tabBadges", tabBadges);
        SerializedArrays.Set(so, "tabLabels", tabLabels);
        SerializedArrays.Set(so, "tabGlyphs", tabGlyphs);
        Wire(so, "tabStrip", strip);
        SerializedArrays.Set(so, "views", new Object[] { documents, views.Records, views.Reference, views.Transcript, views.Report, views.Rules });
        Wire(so, "chipStrip", header);
        Wire(so, "chipTemplate", chip);
        Wire(so, "noCase", noCase.gameObject);
        Wire(so, "activeFrame", frame.gameObject);
        so.FindProperty("startTab").enumValueIndex = (int)start;
        Wire(so, "config", config);
        so.ApplyModifiedProperties();
        return pane;
    }

    /// <summary>
    /// One tab of a pane's strip: the app's tab (BuildTab: the plate with its
    /// name, its active look, its badge in its slot), plus its glyph (a layout
    /// child after the name at DesktopConfigSO's glyph size, shrinking to fit
    /// its room, hidden: a narrow strip's inactive tab shows it in the name's
    /// place, the badge beside it), a tooltip above it naming it,
    /// and its handle (drag along the strip, right-click for its menu).
    /// </summary>
    private static Button BuildPaneTab(Transform strip, AppTab tab, AppPane pane, DesktopConfigSO config, out GameObject active, out GameObject badge,
                                       out GameObject label, out GameObject glyph)
    {
        Button button = BuildTab(strip, tab, config, out active, out badge);
        label = button.transform.Find("Label").gameObject;

        TMP_Text glyphText = Text(button.transform, "Glyph", null, Mathf.RoundToInt(config.tabGlyphSize), TextAlignmentOptions.Center, Vector2.zero, Vector2.one,
                                  Color.white, ThemeRoleId.Tab, AppTabGlyphKeys[tab], FontStyles.Bold, ThemeTextKind.Button, true);
        glyphText.raycastTarget = false;
        glyph = glyphText.gameObject;
        glyph.transform.SetSiblingIndex(label.transform.GetSiblingIndex() + 1);
        glyph.SetActive(false);

        BuildHoverHint(button, AppTabKeys[tab], null, AppTabHintSize, new Vector2(0.5f, 1f), new Vector2(0.5f, 0f));

        AppTabHandle handle = button.gameObject.AddComponent<AppTabHandle>();
        var so = new SerializedObject(handle);
        Wire(so, "pane", pane);
        so.FindProperty("tab").enumValueIndex = (int)tab;
        so.ApplyModifiedProperties();
        return button;
    }

    /// <summary>
    /// A hover hint on <paramref name="control"/> (HoverHint): a tooltip of
    /// <paramref name="size"/> at the control's <paramref name="anchor"/>, by
    /// its <paramref name="pivot"/> (a 4 u gap), reading the keyed
    /// <paramref name="labelKey"/>, else <paramref name="text"/> (a hint the
    /// runtime writes); hidden, never a raycast target. Returns its text.
    /// </summary>
    private static TMP_Text BuildHoverHint(Component control, string labelKey, string text, Vector2 size, Vector2 anchor, Vector2 pivot)
    {
        Transform hint = Panel(control.transform, "Hint", anchor, anchor, new Vector2(0f, pivot.y < 0.5f ? 4f : -4f), size, Tooltip, ThemeRoleId.Tooltip);
        ((RectTransform)hint).pivot = pivot;
        hint.GetComponent<Image>().raycastTarget = false;
        GetOrAdd<LayoutElement>(hint.gameObject).ignoreLayout = true;
        TMP_Text line = Text(hint, "Label", text, 16, TextAlignmentOptions.Center, new Vector2(0.03f, 0.05f), new Vector2(0.97f, 0.95f), Ink,
                             ThemeRoleId.Tooltip, labelKey, FontStyles.Normal, ThemeTextKind.Body, true);
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

        hint = BuildHoverHint(button, null, string.Empty, AppLinkHintSize, Vector2.zero, new Vector2(1f, 1f));
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
