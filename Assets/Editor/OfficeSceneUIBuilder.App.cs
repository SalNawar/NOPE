using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The office builder's Investigation app (redesign phase 16; the PC spec's
/// AP1-AP8, WN4, WN5, §2.2-§2.10, §5.2): one desktop window on the window
/// layer ("Investigation"; the restored size from DesktopConfigSO, maximised
/// on its first open) with its case header (the claim, the counters, the PC's
/// Accept and Deny with their fixed glyphs), its toolbar (Back, Forward, the
/// search field (live since phase 19: OfficeSceneUIBuilder.Search), Steps,
/// Split, Keys: built, not live until phases 18, 20, 21), its
/// sidebar (Steps, Pinned, Recent: placeholders until phases 20-21) and one
/// pane (AppPane): the six tabs in TabOrder.Default with their active looks
/// and badges, the chip row, the content with a view per tab and the no-case
/// state over it. The views host today's page components (the scanned page,
/// Citizen Records, a book's register, the transcript, the report's and the
/// rules' texts), each behind IAppView, so phase 5's FormView replaces one
/// view at a time; the scan toast goes on the investigation host above the
/// window layer. Rebuilt fresh on each run (the one convergence policy of
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

    /// <summary>The app's parts the rest of Build wires (the façade, the desktop's registry, Mail).</summary>
    private struct AppParts
    {
        public InvestigationApp App;
        public DesktopWindow Window;
        public Button Accept;
        public Button Deny;
        public DocumentsView Documents;
        public CitizenRecordsWindowController Records;
        public ReferenceView Reference;
        public TranscriptWindowController Transcript;
        public TMP_Text ReportText;
        public TMP_Text RulesText;
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
        Selectable[] notYetLive = BuildAppToolbar(win, top + AppHeaderHeight, out TMP_InputField searchField);

        Transform body = Panel(win, "AppBody", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        PlaceRect(body, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0f, -(top + AppHeaderHeight + AppToolbarHeight)));
        BuildAppSidebar(body);
        AppPane pane = BuildAppPane(body, compare, ref parts);

        AppToast toast = BuildAppToast(investHost, config);

        parts.App = win.gameObject.AddComponent<InvestigationApp>();
        var so = new SerializedObject(parts.App);
        Wire(so, "window", window);
        Wire(so, "pane", pane);
        Wire(so, "claimText", claim);
        Wire(so, "countersText", counters);
        SerializedArrays.Set(so, "notYetLive", notYetLive);
        Wire(so, "toast", toast);
        Wire(so, "config", config);
        so.ApplyModifiedProperties();
        BuildAppSearch(parts.App, win, searchField, top + AppHeaderHeight + AppToolbarHeight, config);
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

    /// <summary>The toolbar (AP2): Back, Forward, the search field (<paramref name="search"/>, live), Steps, Split and Keys; returns those not live until phases 18, 20 and 21.</summary>
    private static Selectable[] BuildAppToolbar(Transform win, float top, out TMP_InputField search)
    {
        Transform bar = Panel(win, "Toolbar", new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -(top + AppToolbarHeight / 2f)),
                              new Vector2(0f, AppToolbarHeight), XpFace, ThemeRoleId.WindowBody);
        Button back = ToolbarButton(bar, "BackButton", "browser.back", 0.005f, 0.045f);
        Button forward = ToolbarButton(bar, "ForwardButton", "browser.forward", 0.05f, 0.09f);
        search = BuildInputField(bar, "SearchField", "app.search", new Vector2(0.1f, 0.14f), new Vector2(0.7f, 0.86f));
        Button steps = ToolbarButton(bar, "StepsButton", "app.toolbar.steps", 0.71f, 0.79f);
        Button split = ToolbarButton(bar, "SplitButton", "app.toolbar.split", 0.8f, 0.88f);
        Button keys = ToolbarButton(bar, "KeysButton", "app.toolbar.keys", 0.89f, 0.995f);
        return new Selectable[] { back, forward, steps, split, keys };
    }

    /// <summary>A toolbar button between two horizontal anchors (its label keyed).</summary>
    private static Button ToolbarButton(Transform bar, string name, string labelKey, float from, float to)
    {
        Button b = MakeButton(bar, name, null, new Vector2(from, 0.14f), new Vector2(to, 0.86f), null, ThemeRoleId.Button, labelKey);
        SetAnchors(b.transform, new Vector2(from, 0.14f), new Vector2(to, 0.86f));
        return b;
    }

    /// <summary>The sidebar (AP2): Steps, Pinned and Recent, each a heading over a placeholder line until phases 20-21 fill them.</summary>
    private static void BuildAppSidebar(Transform body)
    {
        Transform side = Panel(body, "Sidebar", Vector2.zero, new Vector2(0f, 1f), Vector2.zero, Vector2.zero, XpFace, ThemeRoleId.Sidebar);
        PlaceRect(side, Vector2.zero, new Vector2(0f, 1f), Vector2.zero, new Vector2(AppSidebarWidth, 0f));
        string[] sections = { "Steps", "Pinned", "Recent" };
        string[] keys = { "app.sidebar.steps", "app.sidebar.pinned", "app.sidebar.recent" };
        for (int i = 0; i < sections.Length; i++)
        {
            float high = 1f - i / 3f;
            Text(side, sections[i] + "Heading", null, 18, TextAlignmentOptions.TopLeft, new Vector2(0.06f, high - 0.07f), new Vector2(0.94f, high - 0.015f), Ink,
                 ThemeRoleId.Sidebar, keys[i], FontStyles.Bold, ThemeTextKind.Heading, true);
            Text(side, sections[i] + "Empty", null, 16, TextAlignmentOptions.TopLeft, new Vector2(0.06f, high - 0.14f), new Vector2(0.94f, high - 0.075f), Ink,
                 ThemeRoleId.Sidebar, "app.sidebar.empty", FontStyles.Italic, ThemeTextKind.Body, true);
        }
    }

    /// <summary>The pane (AP2, AP3, AP5): the tab strip, the chip row, the content with the six views and the no-case state. Fills the views into <paramref name="parts"/>.</summary>
    private static AppPane BuildAppPane(Transform body, CompareController compare, ref AppParts parts)
    {
        Transform paneRoot = Panel(body, "Pane", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        PlaceRect(paneRoot, Vector2.zero, Vector2.one, new Vector2(AppSidebarWidth + AppDividerWidth, 0f), Vector2.zero);

        Transform strip = Panel(paneRoot, "TabStrip", new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -AppTabStripHeight / 2f),
                                new Vector2(0f, AppTabStripHeight), XpBlue, ThemeRoleId.TabStrip);
        AddHLayout(strip, 2f);
        GetOrAdd<HorizontalLayoutGroup>(strip.gameObject).padding = new RectOffset(4, 4, 4, 0);
        var tabButtons = new List<Object>();
        var tabActive = new List<Object>();
        var tabBadges = new List<Object>();
        foreach (AppTab tab in TabOrder.Default)
        {
            Button button = MakeButton(strip, "Tab_" + tab, null, Vector2.zero, Vector2.one, XpFace, ThemeRoleId.Tab, AppTabKeys[tab]);
            Transform active = Panel(button.transform, "Active", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Paper, ThemeRoleId.TabActive);
            active.GetComponent<Image>().raycastTarget = false;
            TMP_Text activeLabel = Text(active, "Label", null, 22, TextAlignmentOptions.Center, Vector2.zero, Vector2.one, Ink,
                                        ThemeRoleId.TabActive, AppTabKeys[tab], FontStyles.Bold, ThemeTextKind.Button, true);
            activeLabel.raycastTarget = false;
            active.gameObject.SetActive(false);
            Transform badge = Panel(button.transform, "Badge", Vector2.one, Vector2.one, new Vector2(-10f, -10f), new Vector2(14f, 14f), XpGreen, ThemeRoleId.Badge);
            badge.GetComponent<Image>().raycastTarget = false;
            badge.gameObject.SetActive(false);
            tabButtons.Add(button);
            tabActive.Add(active.gameObject);
            tabBadges.Add(badge.gameObject);
        }

        Transform header = Panel(paneRoot, "PaneHeader", new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -(AppTabStripHeight + AppPaneHeaderHeight / 2f)),
                                 new Vector2(0f, AppPaneHeaderHeight), Paper, ThemeRoleId.WindowBody);
        HorizontalLayoutGroup row = GetOrAdd<HorizontalLayoutGroup>(header.gameObject);
        row.padding = new RectOffset(6, 6, 4, 4);
        row.spacing = 4f;
        row.childAlignment = TextAnchor.MiddleLeft;
        row.childControlWidth = true;
        row.childControlHeight = true;
        row.childForceExpandWidth = false;
        row.childForceExpandHeight = true;
        Button chip = MakeButton(header, "ChipTemplate", "Chip", Vector2.zero, Vector2.one, null, ThemeRoleId.Button);
        LayoutElement chipSize = GetOrAdd<LayoutElement>(chip.gameObject);
        chipSize.minWidth = AppChipWidth.x;
        chipSize.preferredWidth = AppChipWidth.y;
        chipSize.flexibleWidth = 0f;
        TMP_Text chipLabel = chip.transform.Find("Label").GetComponent<TMP_Text>();
        chipLabel.enableAutoSizing = true;
        chipLabel.fontSizeMin = 11f;
        chipLabel.fontSizeMax = 17f;
        chipLabel.textWrappingMode = TextWrappingModes.NoWrap;
        chipLabel.overflowMode = TextOverflowModes.Ellipsis;
        chipLabel.margin = new Vector4(6f, 0f, 6f, 0f);
        chip.gameObject.SetActive(false);

        Transform content = Panel(paneRoot, "Content", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Paper, ThemeRoleId.WindowBody);
        PlaceRect(content, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0f, -(AppTabStripHeight + AppPaneHeaderHeight)));
        content.gameObject.AddComponent<RectMask2D>();

        parts.Documents = BuildDocumentsView(content, out AppView documents);
        parts.Records = BuildRecordsView(content, compare, out AppView records);
        parts.Reference = BuildReferenceView(content);
        parts.Transcript = BuildTranscriptView(content, out AppView transcript);
        parts.ReportText = BuildTextView<ReportView>(content, "ReportView", "scanner.idle", out AppView report);
        parts.RulesText = BuildTextView<RulesView>(content, "RulesView", "directives.none", out AppView rules);

        Transform noCase = Panel(content, "NoCase", new Vector2(0.03f, 0.38f), new Vector2(0.97f, 0.62f), Vector2.zero, Vector2.zero, ScreenStripColor, ThemeRoleId.ScreenStrip);
        noCase.GetComponent<Image>().raycastTarget = false;
        TMP_Text noCaseText = Text(noCase, "Text", null, 80, TextAlignmentOptions.Center, Vector2.zero, Vector2.one, Color.white,
                                   ThemeRoleId.ScreenStrip, "idle.waiting", FontStyles.Bold, ThemeTextKind.Heading, true);
        SetAnchors(noCaseText.transform, new Vector2(0.02f, 0f), new Vector2(0.98f, 1f));
        noCaseText.raycastTarget = false;
        noCase.gameObject.SetActive(false);

        AppPane pane = paneRoot.gameObject.AddComponent<AppPane>();
        var so = new SerializedObject(pane);
        SerializedArrays.Set(so, "tabButtons", tabButtons);
        SerializedArrays.Set(so, "tabActive", tabActive);
        SerializedArrays.Set(so, "tabBadges", tabBadges);
        SerializedArrays.Set(so, "views", new Object[] { documents, records, parts.Reference, transcript, report, rules });
        Wire(so, "chipStrip", header);
        Wire(so, "chipTemplate", chip);
        Wire(so, "noCase", noCase.gameObject);
        so.ApplyModifiedProperties();
        return pane;
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

    /// <summary>The Transcript tab (§2.7): the interview's rows (the speaker column, the sentence wrapping) and Prev/Next.</summary>
    private static TranscriptWindowController BuildTranscriptView(Transform content, out AppView view)
    {
        Transform root = ViewRoot(content, "TranscriptView", Paper, ThemeRoleId.WindowBody);
        PagedBody paged = BuildPagedBody(root, new Vector2(0.03f, 0.12f), new Vector2(0.97f, 0.97f), ThemeRoleId.WindowBody, ThemeRoleId.DiegeticRow, false);
        ApplyTranscriptRowLayout(paged.rowTemplate);
        TranscriptWindowController transcript = root.gameObject.AddComponent<TranscriptWindowController>();
        var so = new SerializedObject(transcript);
        WirePaging(so, paged, AppBookRowsPerPage);
        so.ApplyModifiedProperties();
        view = root.gameObject.AddComponent<TranscriptView>();
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
