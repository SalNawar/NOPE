using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The office builder's Investigation app (redesign phases 16 and 18; the PC
/// spec's AP1-AP9, WN4, WN5, §2.2-§2.10, §5.2): one desktop window on the
/// window layer ("Investigation"; the restored size from DesktopConfigSO,
/// maximised on its first open) with its case header (the claim, the
/// counters, the PC's Accept and Deny with their fixed glyphs), its toolbar
/// (Back, Forward and Split live; the search field live since phase 19,
/// OfficeSceneUIBuilder.Search, with phase 20's keys; Keys live since phase
/// 20; Steps live since phase 21: OfficeSceneUIBuilder.Steps), its sidebar
/// (the steps checklist at its top: OfficeSceneUIBuilder.Steps; Pinned and
/// Recent: the keys' partial, OfficeSceneUIBuilder.Keys, fills them) and two panes side by side (AppPane, built by
/// OfficeSceneUIBuilder.Panes: the left one starts on Documents, the right one
/// on Reference, and is hidden until the app splits). Each pane's views host
/// today's page components (the scanned copy on FormView, Citizen Records, a
/// book's register, the transcript, the report's and the rules' texts), each
/// behind IAppView, so FormView replaces one view at a time; the rows of the
/// registers, Records and the transcript light by key and carry the found
/// mark (the transcript's answers their ↗); the scan toast goes on the
/// investigation host above the window layer. Rebuilt fresh on each run (the one convergence policy of
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

    /// <summary>A pane's tab strip height.</summary>
    private const float AppTabStripHeight = 44f;

    /// <summary>A pane's header (the chip row) height.</summary>
    private const float AppPaneHeaderHeight = 40f;

    /// <summary>A chip's widest and narrowest width (the row shrinks them to fit).</summary>
    private static readonly Vector2 AppChipWidth = new Vector2(64f, 220f);

    /// <summary>Rows per page of the lists the tabs page through until they scroll (phase 5's forms): the restored window's smallest pane fits these.</summary>
    private const int AppBookRowsPerPage = 10;

    /// <summary>Rows per page of Citizen Records (under the lookup).</summary>
    private const int AppRecordRowsPerPage = 8;

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
        public CitizenRecordsWindowController[] Records;
        public ReferenceView[] Reference;
        public TranscriptWindowController[] Transcript;
        public TMP_Text[] ReportText;
        public TMP_Text[] RulesText;
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
        parts.ReportText = new[] { leftViews.ReportText, rightViews.ReportText };
        parts.RulesText = new[] { leftViews.RulesText, rightViews.RulesText };

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
        BuildAppSearch(parts.App, win, toolbar.Search, top + AppHeaderHeight + AppToolbarHeight, config);
        WireStepsPanel(parts.Steps, parts, toolbar.Steps, toast, config);
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

    /// <summary>The toolbar's controls the app wires.</summary>
    private struct AppToolbar
    {
        public Button Back;
        public Button Forward;
        public Button Split;
        public TMP_Text SplitHint;
        public TMP_InputField Search;
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
        toolbar.Search = BuildInputField(bar, "SearchField", "app.search", new Vector2(0.1f, 0.14f), new Vector2(0.7f, 0.86f));
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

    /// <summary>The sidebar (AP2): the steps checklist at its top (<paramref name="steps"/>: OfficeSceneUIBuilder.Steps), then Pinned and Recent, each a heading over a placeholder line the keys' partial replaces with its list (BuildAppKeys). Returns it.</summary>
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

    /// <summary>The Reference tab (§2.6): "Claimed place only" at its top right, and a book's register page (its title, rows, Prev/Next), cloned per book by ReferenceView.</summary>
    private static ReferenceView BuildReferenceView(Transform content)
    {
        Transform root = ViewRoot(content, "ReferenceView", Paper, ThemeRoleId.WindowBody);
        Toggle claimedOnly = BuildToggle(root, "ClaimedOnly", "app.ref.claimedOnly", new Vector2(0.62f, 0.915f), new Vector2(0.98f, 0.985f));

        Transform page = Panel(root, "PageTemplate", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        TMP_Text title = Text(page, "TitleText", UiText.Get("book.untitled"), 22, TextAlignmentOptions.MidlineLeft, new Vector2(0.03f, 0.915f),
                              new Vector2(0.6f, 0.985f), Ink, ThemeRoleId.WindowBody, style: FontStyles.Bold);
        PagedBody paged = BuildPagedBody(page, new Vector2(0.03f, 0.12f), new Vector2(0.97f, 0.9f), ThemeRoleId.WindowBody, ThemeRoleId.DiegeticBookRow, true);
        DecorateAppRow(paged.rowTemplate, false);
        ReferenceBookWindowController register = page.gameObject.AddComponent<ReferenceBookWindowController>();
        var so = new SerializedObject(register);
        Wire(so, "titleText", title);
        WirePaging(so, paged, AppBookRowsPerPage);
        so.ApplyModifiedProperties();
        page.gameObject.SetActive(false);

        ReferenceView reference = root.gameObject.AddComponent<ReferenceView>();
        var soView = new SerializedObject(reference);
        Wire(soView, "pageTemplate", register);
        Wire(soView, "claimedOnly", claimedOnly);
        soView.ApplyModifiedProperties();
        return reference;
    }

    /// <summary>The Transcript tab (§2.7): the interview's rows (the speaker column, the sentence wrapping; an answer's ↗ at the row's end) and Prev/Next.</summary>
    private static TranscriptWindowController BuildTranscriptView(Transform content, out AppView view)
    {
        Transform root = ViewRoot(content, "TranscriptView", Paper, ThemeRoleId.WindowBody);
        PagedBody paged = BuildPagedBody(root, new Vector2(0.03f, 0.12f), new Vector2(0.97f, 0.97f), ThemeRoleId.WindowBody, ThemeRoleId.DiegeticRow, false);
        ApplyTranscriptRowLayout(paged.rowTemplate);
        DecorateAppRow(paged.rowTemplate, true);
        TranscriptWindowController transcript = root.gameObject.AddComponent<TranscriptWindowController>();
        var so = new SerializedObject(transcript);
        WirePaging(so, paged, AppBookRowsPerPage);
        so.ApplyModifiedProperties();
        TranscriptView transcriptView = root.gameObject.AddComponent<TranscriptView>();
        var soView = new SerializedObject(transcriptView);
        Wire(soView, "transcript", transcript);
        soView.ApplyModifiedProperties();
        view = transcriptView;
        return transcript;
    }

    /// <summary>A text tab (the Report, the Rules): one wrapping text on the page, keyed <paramref name="sampleKey"/> until its presenter writes it.</summary>
    private static TMP_Text BuildTextView<T>(Transform content, string name, string sampleKey, out AppView view) where T : AppView
    {
        Transform root = ViewRoot(content, name, Paper, ThemeRoleId.WindowBody);
        TMP_Text body = Text(root, "BodyText", UiText.Get(sampleKey), 20, TextAlignmentOptions.TopLeft, new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.96f),
                             Ink, ThemeRoleId.WindowBody);
        body.textWrappingMode = TextWrappingModes.Normal;
        body.overflowMode = TextOverflowModes.Truncate;
        view = root.gameObject.AddComponent<T>();
        return body;
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

    /// <summary>Wires a paged list's shared fields (PagedRowsWindow) and its rows per page.</summary>
    private static void WirePaging(SerializedObject so, PagedBody paged, int rowsPerPage)
    {
        Wire(so, "pageText", paged.page);
        Wire(so, "prevButton", paged.prev);
        Wire(so, "nextButton", paged.next);
        Wire(so, "entryRowsRoot", paged.rowsRoot);
        Wire(so, "entryRowTemplate", paged.rowTemplate);
        so.FindProperty("entriesPerPage").intValue = rowsPerPage;
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
