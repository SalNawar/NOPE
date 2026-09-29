using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The office builder's Investigation app (redesign phases 16, 18 and 21; the
/// PC spec's AP1-AP9, ST1, WN4, WN5, §2.2-§2.10, §5.2): one desktop window on
/// the window layer ("Investigation"; the restored size from DesktopConfigSO,
/// maximised on its first open) with its case header (the claim, the
/// counters, the PC's Accept and Deny with their fixed glyphs), its toolbar
/// (Back, Forward and Split live; the search field search's, its SearchBox and
/// results panel (OfficeSceneUIBuilder.Search), and the keys', its chip; Keys
/// the keys' (OfficeSceneUIBuilder.Keys); Steps the steps' (OfficeSceneUIBuilder.Steps)),
/// its sidebar (the steps checklist at its top: OfficeSceneUIBuilder.Steps;
/// Pinned and Recent: the keys' partial fills them) and two panes side by
/// side (AppPane, built by OfficeSceneUIBuilder.Panes from the parts here:
/// BuildTab, each tab a plate on the chrome, its active look a paper plate
/// with an ink bar, its badge an accent dot in a slot reserved after the
/// label; BuildChipTemplate, a chip as wide as its label, the chosen one on
/// an accent plate; the left pane starts on Documents, the right one on
/// Reference, and is hidden until the app splits). Each pane's views are
/// forms (the scanned copy, and OfficeSceneUIBuilder.AppViews' Record Extract,
/// registers, Interview Record, Deviation Report and Directive Memo, each on
/// a FormPage), each behind IAppView; their rows light by key and carry the
/// found mark on the form (the answers and the report's sides their ↗); the
/// scan toast goes on the investigation host above the window layer.
/// Rebuilt fresh on each run (the
/// one convergence policy of
/// this partial, audit R6-008); every reference it wires is checked (Wire,
/// audit R6-004). Part of <see cref="OfficeSceneUIBuilder"/>; Build() calls
/// it in its order.
/// </summary>
public static partial class OfficeSceneUIBuilder
{
    /// <summary>The case header's height under the title bar.</summary>
    private const float AppHeaderHeight = 72f;

    /// <summary>The toolbar's height under the case header.</summary>
    private const float AppToolbarHeight = 52f;

    /// <summary>The sidebar's width at the body's left.</summary>
    private const float AppSidebarWidth = 272f;

    /// <summary>The gap between the sidebar and the pane.</summary>
    private const float AppDividerWidth = 6f;

    /// <summary>A tab's narrowest width: on a strip too narrow for every label the tabs shrink towards it and their labels shrink to fit (phase 18 shows glyphs instead).</summary>
    private const float AppTabMinWidth = 96f;

    /// <summary>The room at each end of a tab's label.</summary>
    private const int AppTabPadding = 18;

    /// <summary>The gap between a tab's label and its badge slot.</summary>
    private const float AppTabGap = 6f;

    /// <summary>The bar along the active tab's top edge (in the body's ink: an accent would read under 3:1 on the paper plate in four cultures' themes).</summary>
    private const float AppTabBarHeight = 4f;

    /// <summary>A tab's badge dot, after its label (its slot is always reserved, so a badge never moves the label).</summary>
    private const float AppTabBadgeSize = 12f;

    /// <summary>The hairline under the chip row (2 units, so it still draws at 720p).</summary>
    private const float AppPaneRuleHeight = 2f;

    /// <summary>A chip's narrowest width: a chip is as wide as its label, and the row shrinks chips towards this to fit.</summary>
    private const float AppChipMinWidth = 120f;

    /// <summary>The room at each end of a chip's label.</summary>
    private const int AppChipPadding = 12;

    /// <summary>The scan toast's size, and its gap above the compare dock.</summary>
    private static readonly Vector2 AppToastSize = new Vector2(640f, 56f);

    /// <summary>The tabs' label keys, in TabOrder.Default.</summary>
    private static readonly Dictionary<AppTab, string> AppTabKeys = new Dictionary<AppTab, string>
    {
        { AppTab.Documents, "app.tab.documents" },
        { AppTab.Records, "app.tab.records" },
        { AppTab.Reference, "app.tab.reference" },
        { AppTab.Transcript, "app.tab.transcript" },
        { AppTab.Report, "app.tab.report" },
        { AppTab.Rules, "app.tab.rules" },
    };

    /// <summary>The app's parts the rest of Build wires (the façade, the desktop's registry, Mail, the dock): each tab's views, one per pane, the left pane's first.</summary>
    private struct AppParts
    {
        public InvestigationApp App;
        public DesktopWindow Window;
        public Button Accept;
        public Button Deny;
        public DocumentsView[] Documents;
        public RecordsView[] Records;
        public ReferenceView[] Reference;
        public TranscriptView[] Transcript;
        public ReportView[] Report;
        public RulesView[] Rules;
        public StepsPanel Steps;
    }

    /// <summary>Sets an object reference, logging an error when the property does not exist (audit R6-004: a renamed field is never skipped silently).</summary>
    private static void Wire(SerializedObject so, string prop, Object value)
    {
        SerializedProperty p = so.FindProperty(prop);
        if (p == null)
        {
            Debug.LogError($"[TimeDesk] {so.targetObject.GetType().Name} has no serialized field '{prop}' to wire; fix OfficeSceneUIBuilder.");
            return;
        }
        p.objectReferenceValue = value;
    }

    /// <summary>
    /// Builds the Investigation app on <paramref name="windowLayer"/> and its
    /// scan toast on <paramref name="investHost"/> (above the layer); its rows
    /// pick into <paramref name="compare"/>. The retired per-source windows
    /// (and phase 17's case-tile window) go.
    /// </summary>
    private static AppParts BuildInvestigationApp(Transform windowLayer, Transform investHost, CompareController compare)
    {
        foreach (string retired in new[] { "InvestigationWindow", "DirectivesWindow", "IconScannerWindow", "RecordsWindow", "TranscriptWindow",
                                           "DocumentWindowTemplate", "BookWindowTemplate", "InvestigationApp" })
            DestroyChildIfPresent(windowLayer, retired);

        DesktopConfigSO config = EnsureDesktopConfig();
        DesktopWindow window = BuildOSWindow(windowLayer, "InvestigationApp", null, null, string.Empty, config.investigationWindowSize);
        Transform win = window.transform;
        DestroyChildIfPresent(win, "Body");
        TMP_Text title = win.Find("Header/TitleText").GetComponent<TMP_Text>();
        title.text = UiText.Get("app.title");
        float top = config.titleBarHeight;

        var parts = new AppParts { Window = window };
        BuildAppHeader(win, top, out TMP_Text claim, out TMP_Text counters, out parts.Accept, out parts.Deny);
        AppToolbar toolbar = BuildAppToolbar(win, top + AppHeaderHeight);

        Transform body = Panel(win, "AppBody", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        PlaceRect(body, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0f, -(top + AppHeaderHeight + AppToolbarHeight)));
        Transform sidebar = BuildAppSidebar(body, out parts.Steps);
        Transform panes = Panel(body, "Panes", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        PlaceRect(panes, Vector2.zero, Vector2.one, new Vector2(AppSidebarWidth + AppDividerWidth, 0f), Vector2.zero);
        AppPane left = BuildAppPane(panes, "PaneLeft", AppTab.Documents, compare, config, out PaneViews leftViews);
        AppPane right = BuildAppPane(panes, "PaneRight", AppTab.Reference, compare, config, out PaneViews rightViews);
        PlaceRect(right.transform, new Vector2(0.5f, 0f), Vector2.one, new Vector2(config.paneGap / 2f, 0f), Vector2.zero);
        right.gameObject.SetActive(false);
        parts.Documents = new[] { leftViews.Documents, rightViews.Documents };
        parts.Records = new[] { leftViews.Records, rightViews.Records };
        parts.Reference = new[] { leftViews.Reference, rightViews.Reference };
        parts.Transcript = new[] { leftViews.Transcript, rightViews.Transcript };
        parts.Report = new[] { leftViews.Report, rightViews.Report };
        parts.Rules = new[] { leftViews.Rules, rightViews.Rules };

        AppToast toast = BuildAppToast(investHost, config);

        parts.App = win.gameObject.AddComponent<InvestigationApp>();
        var so = new SerializedObject(parts.App);
        Wire(so, "window", window);
        Wire(so, "leftPane", left);
        Wire(so, "rightPane", right);
        Wire(so, "body", body);
        Wire(so, "sidebar", sidebar);
        Wire(so, "claimText", claim);
        Wire(so, "countersText", counters);
        Wire(so, "backButton", toolbar.Back);
        Wire(so, "forwardButton", toolbar.Forward);
        Wire(so, "splitButton", toolbar.Split);
        Wire(so, "splitHint", toolbar.SplitHint);
        Wire(so, "toast", toast);
        Wire(so, "config", config);
        so.ApplyModifiedProperties();
        WireStepsPanel(parts.Steps, parts, toolbar.Steps, toast, config);
        BuildAppSearch(parts.App, win, top + AppHeaderHeight + AppToolbarHeight, config);
        return parts;
    }

    /// <summary>The case header (AP1): the claim and the counters on a strip, the PC's Accept and Deny at its right with their fixed glyphs (piece 6 R8).</summary>
    private static void BuildAppHeader(Transform win, float top, out TMP_Text claim, out TMP_Text counters, out Button accept, out Button deny)
    {
        Transform header = Panel(win, "CaseHeader", new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -(top + AppHeaderHeight / 2f)),
                                 new Vector2(0f, AppHeaderHeight), ScreenStripColor, ThemeRoleId.ClaimStrip);
        claim = Text(header, "ClaimText", UiText.Get("idle.waiting"), 20, TextAlignmentOptions.MidlineLeft, new Vector2(0.01f, 0.38f), new Vector2(0.66f, 0.98f),
                     Color.white, ThemeRoleId.ClaimStrip, fit: true);
        counters = Text(header, "CountersText", string.Empty, 18, TextAlignmentOptions.MidlineLeft, new Vector2(0.01f, 0.02f), new Vector2(0.66f, 0.38f),
                        Color.white, ThemeRoleId.ClaimStrip, fit: true);
        accept = MakeButton(header, "AcceptButton", null, new Vector2(0.68f, 0.12f), new Vector2(0.835f, 0.88f), new Color(0.2f, 0.5f, 0.24f, 1f),
                            ThemeRoleId.AcceptButton, "accept");
        deny = MakeButton(header, "DenyButton", null, new Vector2(0.845f, 0.12f), new Vector2(0.995f, 0.88f), new Color(0.72f, 0.2f, 0.18f, 1f),
                          ThemeRoleId.DenyButton, "deny");
        BuildDecisionGlyph(accept, ThemeRoleId.AcceptButton, true);
        BuildDecisionGlyph(deny, ThemeRoleId.DenyButton, false);
    }

    /// <summary>The toolbar's controls the app and the steps wire.</summary>
    private struct AppToolbar
    {
        public Button Back;
        public Button Forward;
        public Button Split;
        public TMP_Text SplitHint;
        public Button Steps;
    }

    /// <summary>The toolbar (AP2): Back, Forward and Split (with its hover hint above it, over the case header) live; the search field is search's (BuildAppSearch) and the keys' (BuildAppKeys), Keys the keys'; Steps the steps' (WireStepsPanel).</summary>
    private static AppToolbar BuildAppToolbar(Transform win, float top)
    {
        Transform bar = Panel(win, "Toolbar", new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -(top + AppToolbarHeight / 2f)),
                              new Vector2(0f, AppToolbarHeight), XpFace, ThemeRoleId.WindowBody);
        var toolbar = new AppToolbar
        {
            Back = ToolbarButton(bar, "BackButton", "browser.back", 0.005f, 0.045f),
            Forward = ToolbarButton(bar, "ForwardButton", "browser.forward", 0.05f, 0.09f),
        };
        BuildInputField(bar, "SearchField", "app.search", new Vector2(0.1f, 0.14f), new Vector2(0.7f, 0.86f));
        toolbar.Steps = ToolbarButton(bar, "StepsButton", "app.toolbar.steps", 0.71f, 0.79f);
        toolbar.Split = ToolbarButton(bar, "SplitButton", "app.toolbar.split", 0.8f, 0.88f);
        toolbar.SplitHint = BuildHoverHint(toolbar.Split, null, UiText.Get("app.split.hint"), AppSplitHintSize, new Vector2(1f, 1f), new Vector2(1f, 0f));
        ToolbarButton(bar, "KeysButton", "app.toolbar.keys", 0.89f, 0.995f);
        return toolbar;
    }

    /// <summary>A toolbar button between two horizontal anchors (its label keyed).</summary>
    private static Button ToolbarButton(Transform bar, string name, string labelKey, float from, float to)
    {
        Button b = MakeButton(bar, name, null, new Vector2(from, 0.14f), new Vector2(to, 0.86f), null, ThemeRoleId.Button, labelKey);
        SetAnchors(b.transform, new Vector2(from, 0.14f), new Vector2(to, 0.86f));
        return b;
    }

    /// <summary>The sidebar (AP2): the steps checklist at its top (OfficeSceneUIBuilder.Steps, StepsSectionShare of the height), then Pinned and Recent sharing the rest, each a heading over a placeholder line the keys' partial replaces with its list (BuildSidebarList). Returns the sidebar, and the steps in <paramref name="steps"/>.</summary>
    private static Transform BuildAppSidebar(Transform body, out StepsPanel steps)
    {
        Transform side = Panel(body, "Sidebar", Vector2.zero, new Vector2(0f, 1f), Vector2.zero, Vector2.zero, XpFace, ThemeRoleId.Sidebar);
        PlaceRect(side, Vector2.zero, new Vector2(0f, 1f), Vector2.zero, new Vector2(AppSidebarWidth, 0f));
        steps = BuildStepsSection(side, 1f, 1f - StepsSectionShare);
        string[] sections = { "Pinned", "Recent" };
        string[] keys = { "app.sidebar.pinned", "app.sidebar.recent" };
        for (int i = 0; i < sections.Length; i++)
        {
            float high = (1f - StepsSectionShare) * (1f - i / 2f);
            Text(side, sections[i] + "Heading", null, 18, TextAlignmentOptions.TopLeft, new Vector2(0.06f, high - 0.07f), new Vector2(0.94f, high - 0.015f), Ink,
                 ThemeRoleId.Sidebar, keys[i], FontStyles.Bold, ThemeTextKind.Heading, true);
            Text(side, sections[i] + "Empty", null, 16, TextAlignmentOptions.TopLeft, new Vector2(0.06f, high - 0.14f), new Vector2(0.94f, high - 0.075f), Ink,
                 ThemeRoleId.Sidebar, "app.sidebar.empty", FontStyles.Italic, ThemeTextKind.Body, true);
        }
        return side;
    }

    /// <summary>
    /// One tab of a strip (AP3's look): a plate in the Tab role (the chrome,
    /// lightened by a gloss) carrying its label in the chrome's ink, as wide as
    /// the label (never narrower than AppTabMinWidth; squeezed, the label shrinks
    /// to fit); its badge, an accent dot in a slot reserved after the label, so
    /// a badge never moves it; and its active look over both, shown by AppPane
    /// while the tab is active: a paper plate in the TabActive role joined to
    /// the row below, a bar in the body's ink along its top and the label in
    /// that ink, placed exactly over the inactive label. Returns the button;
    /// <paramref name="active"/> and <paramref name="badge"/> are what the pane
    /// toggles.
    /// </summary>
    private static Button BuildTab(Transform strip, AppTab tab, DesktopConfigSO config, out GameObject active, out GameObject badge)
    {
        Button button = MakeButton(strip, "Tab_" + tab, null, Vector2.zero, Vector2.one, XpBlue, ThemeRoleId.Tab, AppTabKeys[tab]);
        LayoutElement size = GetOrAdd<LayoutElement>(button.gameObject);
        size.minWidth = AppTabMinWidth;
        size.flexibleWidth = 0f;
        HorizontalLayoutGroup line = GetOrAdd<HorizontalLayoutGroup>(button.gameObject);
        line.padding = new RectOffset(AppTabPadding, AppTabPadding, 0, 0);
        line.spacing = AppTabGap;
        line.childAlignment = TextAnchor.MiddleCenter;
        line.childControlWidth = true;
        line.childControlHeight = true;
        line.childForceExpandWidth = false;
        line.childForceExpandHeight = true;

        TMP_Text label = button.transform.Find("Label").GetComponent<TMP_Text>();
        label.fontSize = config.tabLabelSize;
        label.raycastTarget = false;

        Transform gloss = Panel(button.transform, "Gloss", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(1f, 1f, 1f, 0.05f), ThemeRoleId.TitleGloss);
        gloss.GetComponent<Image>().raycastTarget = false;
        GetOrAdd<LayoutElement>(gloss.gameObject).ignoreLayout = true;
        gloss.SetAsFirstSibling();

        Transform look = Panel(button.transform, "Active", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Paper, ThemeRoleId.TabActive);
        look.GetComponent<Image>().raycastTarget = false;
        GetOrAdd<LayoutElement>(look.gameObject).ignoreLayout = true;
        Transform bar = Panel(look, "TopBar", new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -AppTabBarHeight / 2f), new Vector2(0f, AppTabBarHeight), Ink);
        Image barImage = bar.GetComponent<Image>();
        Tag(barImage, ThemeRoleId.TabActive, ThemePart.Ink);
        barImage.raycastTarget = false;
        TMP_Text activeLabel = Text(look, "Label", null, Mathf.RoundToInt(config.tabLabelSize), TextAlignmentOptions.Center, Vector2.zero, Vector2.one, Ink,
                                    ThemeRoleId.TabActive, AppTabKeys[tab], FontStyles.Normal, ThemeTextKind.Button, true);
        var activeRect = (RectTransform)activeLabel.transform;
        activeRect.offsetMin = new Vector2(AppTabPadding, 0f);
        activeRect.offsetMax = new Vector2(-(AppTabPadding + AppTabGap + AppTabBadgeSize), 0f);
        activeLabel.raycastTarget = false;
        look.gameObject.SetActive(false);

        Transform slot = Panel(button.transform, "BadgeSlot", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        LayoutElement slotSize = GetOrAdd<LayoutElement>(slot.gameObject);
        slotSize.minWidth = AppTabBadgeSize;
        slotSize.preferredWidth = AppTabBadgeSize;
        slotSize.flexibleWidth = 0f;
        Transform dot = Panel(slot, "Badge", Center, Center, Vector2.zero, new Vector2(AppTabBadgeSize, AppTabBadgeSize), XpGreen, ThemeRoleId.Badge);
        Image dotImage = dot.GetComponent<Image>();
        dotImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        dotImage.raycastTarget = false;
        dot.gameObject.SetActive(false);

        active = look.gameObject;
        badge = dot.gameObject;
        return button;
    }

    /// <summary>
    /// The chip row's template (AP5; hidden, cloned by AppPane per item of the
    /// active view): a button in the Button role as wide as its label (never
    /// narrower than AppChipMinWidth; the label at DesktopConfigSO's chip size,
    /// shrinking to three quarters of it before its text is cut with "…"),
    /// and over it the chosen look, "Chosen": an accent plate (the Badge role)
    /// with the same text in bold, shown by the pane for the item the view shows.
    /// </summary>
    private static Button BuildChipTemplate(Transform header, DesktopConfigSO config)
    {
        Button chip = MakeButton(header, "ChipTemplate", "Chip", Vector2.zero, Vector2.one, null, ThemeRoleId.Button);
        LayoutElement chipSize = GetOrAdd<LayoutElement>(chip.gameObject);
        chipSize.minWidth = AppChipMinWidth;
        chipSize.flexibleWidth = 0f;
        HorizontalLayoutGroup line = GetOrAdd<HorizontalLayoutGroup>(chip.gameObject);
        line.padding = new RectOffset(AppChipPadding, AppChipPadding, 0, 0);
        line.childAlignment = TextAnchor.MiddleCenter;
        line.childControlWidth = true;
        line.childControlHeight = true;
        line.childForceExpandWidth = false;
        line.childForceExpandHeight = true;
        ChipLabel(chip.transform.Find("Label").GetComponent<TMP_Text>(), config);

        Transform chosen = Panel(chip.transform, "Chosen", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, XpGreen, ThemeRoleId.Badge);
        chosen.GetComponent<Image>().raycastTarget = false;
        GetOrAdd<LayoutElement>(chosen.gameObject).ignoreLayout = true;
        TMP_Text chosenLabel = Text(chosen, "Label", "Chip", Mathf.RoundToInt(config.chipLabelSize), TextAlignmentOptions.Center, Vector2.zero, Vector2.one,
                                    Color.white, ThemeRoleId.Badge, null, FontStyles.Bold, ThemeTextKind.Button);
        var chosenRect = (RectTransform)chosenLabel.transform;
        chosenRect.offsetMin = new Vector2(AppChipPadding, 0f);
        chosenRect.offsetMax = new Vector2(-AppChipPadding, 0f);
        ChipLabel(chosenLabel, config);
        chosen.gameObject.SetActive(false);

        chip.gameObject.SetActive(false);
        return chip;
    }

    /// <summary>A chip label's fit: one line at the chip size, shrinking to three quarters of it, then cut with "…"; it takes no raycasts.</summary>
    private static void ChipLabel(TMP_Text label, DesktopConfigSO config)
    {
        label.enableAutoSizing = true;
        label.fontSizeMax = config.chipLabelSize;
        label.fontSizeMin = config.chipLabelSize * 0.75f;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Ellipsis;
        label.margin = Vector4.zero;
        label.raycastTarget = false;
    }

    /// <summary>A view's root: the whole content, hidden until its tab shows.</summary>
    private static Transform ViewRoot(Transform content, string name, Color? fill, ThemeRoleId? role)
    {
        Transform view = Panel(content, name, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, fill, role);
        view.gameObject.SetActive(false);
        return view;
    }

    /// <summary>
    /// The Documents tab (§2.4): the scanner's dark backing (the form style's),
    /// the hint shown instead of a copy, and the scanned-copy page template (the
    /// paper's form in a scroll, OfficeSceneUIBuilder.PcForms), cloned per
    /// paper by DocumentsView.
    /// </summary>
    private static DocumentsView BuildDocumentsView(Transform content, out AppView view)
    {
        FormStyleSO style = EnsureFormStyle();
        Transform root = ViewRoot(content, "DocumentsView", style.backing, ThemeRoleId.DiegeticBacking);
        TMP_Text hint = Text(root, "Hint", UiText.Get("app.doc.none"), 24, TextAlignmentOptions.Center, new Vector2(0.08f, 0.4f), new Vector2(0.92f, 0.6f),
                             style.backingInk, ThemeRoleId.DiegeticBacking);
        hint.textWrappingMode = TextWrappingModes.Normal;

        DocumentWindowController copy = BuildDocumentPage(root);

        DocumentsView documents = root.gameObject.AddComponent<DocumentsView>();
        var soView = new SerializedObject(documents);
        Wire(soView, "pageTemplate", copy);
        Wire(soView, "hintText", hint);
        soView.ApplyModifiedProperties();
        view = documents;
        return documents;
    }

    /// <summary>A labelled checkbox (a Toggle on a Button-role plate: the box, its check, the keyed label), on by default.</summary>
    private static Toggle BuildToggle(Transform parent, string name, string labelKey, Vector2 aMin, Vector2 aMax)
    {
        Transform plate = Panel(parent, name, aMin, aMax, Vector2.zero, Vector2.zero, XpFace, ThemeRoleId.Button);
        SetAnchors(plate, aMin, aMax);
        Transform box = Panel(plate, "Box", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(20f, 0f), new Vector2(24f, 24f), Color.white, ThemeRoleId.InputField);
        Transform check = Panel(box, "Check", Center, Center, Vector2.zero, new Vector2(14f, 14f), XpGreen, ThemeRoleId.Badge);
        check.GetComponent<Image>().raycastTarget = false;
        TMP_Text label = Text(plate, "Label", null, 18, TextAlignmentOptions.MidlineLeft, Vector2.zero, Vector2.one, Ink, ThemeRoleId.Button, labelKey,
                              FontStyles.Normal, ThemeTextKind.Button, true);
        ((RectTransform)label.transform).offsetMin = new Vector2(42f, 0f);
        label.raycastTarget = false;
        Toggle toggle = GetOrAdd<Toggle>(plate.gameObject);
        toggle.targetGraphic = box.GetComponent<Image>();
        toggle.graphic = check.GetComponent<Image>();
        toggle.isOn = true;
        return toggle;
    }

    /// <summary>The scan toast (WN5) on the investigation host, above the window layer: centred right above the compare dock, its line and Open, hidden.</summary>
    private static AppToast BuildAppToast(Transform investHost, DesktopConfigSO config)
    {
        DestroyChildIfPresent(investHost, "AppToast");
        Transform strip = Panel(investHost, "AppToast", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, config.MaximisedBottom + 12f + AppToastSize.y / 2f),
                                AppToastSize, PanelNavy, ThemeRoleId.Toast);
        TMP_Text line = Text(strip, "Text", string.Empty, 20, TextAlignmentOptions.MidlineLeft, new Vector2(0.03f, 0f), new Vector2(0.76f, 1f), Color.white,
                             ThemeRoleId.Toast, fit: true);
        Button open = MakeButton(strip, "OpenButton", null, new Vector2(0.78f, 0.16f), new Vector2(0.97f, 0.84f), null, ThemeRoleId.Button, "app.toast.open");
        AppToast toast = strip.gameObject.AddComponent<AppToast>();
        var so = new SerializedObject(toast);
        Wire(so, "text", line);
        Wire(so, "openButton", open);
        so.ApplyModifiedProperties();
        strip.gameObject.SetActive(false);
        return toast;
    }
}
