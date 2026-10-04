using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The office builder's Investigation app as a workbench (the PC workbench
/// spec, docs/superpowers/specs/2026-09-30-pc-workbench-design.md, §2, §3,
/// §5): one desktop window on the window layer ("Investigation"; the
/// restored size from DesktopConfigSO, maximised on its first open). Under
/// its title bar: the header (the traveller's face, name and counters; the
/// five guided steps' pills: GuideBar), the shelf (the documents' groups
/// and chips wrapping in a FlowLayoutGroup, the Search button: ShelfView),
/// the work area (the main column with the step's lead, the status line,
/// the two panes with the line layer over them (AppPane, MatchLines) and the
/// decision step in their place when it shows (DecisionView); the findings
/// column at its right: FindingsView), the foot (Back, the progress line,
/// Next) and the search drawer (OfficeSceneUIBuilder.Search). The panes'
/// views are forms (OfficeSceneUIBuilder.AppViews, PcForms) behind IAppView;
/// the scan toast goes on the investigation host above the window layer.
/// The workbench's parts are OfficeSceneUIBuilder.Workbench's. Rebuilt fresh
/// on each run (the one convergence policy of this partial, audit R6-008);
/// every reference it wires is checked (Wire, audit R6-004). Part of
/// <see cref="OfficeSceneUIBuilder"/>; Build() calls it in its order.
/// </summary>
public static partial class OfficeSceneUIBuilder
{
    /// <summary>The scan toast's size, and its gap above the taskbar.</summary>
    private static readonly Vector2 AppToastSize = new Vector2(620f, 60f);

    /// <summary>The app's parts the rest of Build wires (the façade, the desktop's registry, Mail): each source's views, one per pane, the left pane's first; the steps and the workbench.</summary>
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
        public CalendarView[] Calendar;
        public GuideBar Guide;
        public MatchBoard Board;
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
    /// pick into <paramref name="compare"/>. The retired per-source windows,
    /// phase 17's case-tile window and the compare dock go.
    /// </summary>
    private static AppParts BuildInvestigationApp(Transform windowLayer, Transform investHost, CompareController compare)
    {
        foreach (string retired in new[] { "InvestigationWindow", "DirectivesWindow", "IconScannerWindow", "RecordsWindow", "TranscriptWindow",
                                           "DocumentWindowTemplate", "BookWindowTemplate", "InvestigationApp" })
            DestroyChildIfPresent(windowLayer, retired);
        DestroyChildIfPresent(investHost, "CompareDock");

        DesktopConfigSO config = EnsureDesktopConfig();
        DesktopWindow window = BuildOSWindow(windowLayer, "InvestigationApp", null, null, string.Empty, config.investigationWindowSize);
        Transform win = window.transform;
        DestroyChildIfPresent(win, "Body");
        TMP_Text title = win.Find("Header/TitleText").GetComponent<TMP_Text>();
        title.text = UiText.Get("app.title");
        float top = config.titleBarHeight;

        var parts = new AppParts { Window = window };
        Transform body = Panel(win, "AppBody", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        PlaceRect(body, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0f, -top));

        // The header, the shelf, the foot, then the work area between them (the shelf's height moves its top at runtime).
        AppHeader header = BuildAppHeader(body);
        ShelfView shelf = BuildShelf(body);
        AppFoot foot = BuildAppFoot(body);
        Transform work = Panel(body, "Work", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        PlaceRect(work, Vector2.zero, Vector2.one, new Vector2(0f, WbSize.Foot), new Vector2(0f, -(WbSize.Header + WbSize.ShelfStart)));

        FindingsView findings = BuildFindingsColumn(work, out RectTransform findingsColumn);
        Transform main = Panel(work, "Main", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        PlaceRect(main, Vector2.zero, Vector2.one, new Vector2(WbSize.Pad, WbSize.Gap), new Vector2(-(WbSize.Findings + WbSize.Gap), -WbSize.Gap));
        AppLead lead = BuildLead(main);
        AppStatus status = BuildStatusLine(main);

        Transform panes = Panel(main, "Panes", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        PlaceRect(panes, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0f, -WbSize.PanesTop));
        AppPane left = BuildAppPane(panes, "PaneLeft", AppTab.Documents, compare, config, true, out PaneViews leftViews);
        AppPane right = BuildAppPane(panes, "PaneRight", AppTab.Reference, compare, config, false, out PaneViews rightViews);
        PlaceRect(right.transform, new Vector2(0.5f, 0f), Vector2.one, new Vector2(config.paneGap / 2f, 0f), Vector2.zero);
        right.gameObject.SetActive(false);
        MatchLines lines = BuildMatchLines(panes);

        DecisionView decision = BuildDecision(main, out parts.Accept, out parts.Deny);

        parts.Documents = new[] { leftViews.Documents, rightViews.Documents };
        parts.Records = new[] { leftViews.Records, rightViews.Records };
        parts.Reference = new[] { leftViews.Reference, rightViews.Reference };
        parts.Transcript = new[] { leftViews.Transcript, rightViews.Transcript };
        parts.Report = new[] { leftViews.Report, rightViews.Report };
        parts.Rules = new[] { leftViews.Rules, rightViews.Rules };
        parts.Calendar = new[] { leftViews.Calendar, rightViews.Calendar };

        AppToast toast = BuildAppToast(investHost, config);
        parts.App = win.gameObject.AddComponent<InvestigationApp>();
        parts.Guide = header.Root.gameObject.AddComponent<GuideBar>();
        parts.Board = work.gameObject.AddComponent<MatchBoard>();

        var soGuide = new SerializedObject(parts.Guide);
        SerializedArrays.Set(soGuide, "pills", header.Pills);
        Wire(soGuide, "leadTitle", lead.Title);
        Wire(soGuide, "backButton", foot.Back);
        Wire(soGuide, "nextButton", foot.Next);
        Wire(soGuide, "nextLabel", foot.NextLabel);
        Wire(soGuide, "progressText", foot.Progress);
        Wire(soGuide, "app", parts.App);
        Wire(soGuide, "board", parts.Board);
        soGuide.ApplyModifiedProperties();

        var soBoard = new SerializedObject(parts.Board);
        Wire(soBoard, "compare", compare);
        Wire(soBoard, "app", parts.App);
        Wire(soBoard, "lines", lines);
        Wire(soBoard, "findings", findings);
        Wire(soBoard, "idleText", status.Idle);
        Wire(soBoard, "holdText", status.Hold);
        Wire(soBoard, "cancelButton", status.Cancel);
        Wire(soBoard, "matchText", status.Match);
        Wire(soBoard, "differText", status.Differ);
        Wire(soBoard, "infoText", status.Info);
        soBoard.ApplyModifiedProperties();

        var soFindings = new SerializedObject(findings);
        Wire(soFindings, "app", parts.App);
        soFindings.ApplyModifiedProperties();

        foreach (RulesView rules in parts.Rules)
            WireBoard(rules, parts.Board);
        foreach (CalendarView calendar in parts.Calendar)
            WireBoard(calendar, parts.Board);

        var so = new SerializedObject(parts.App);
        Wire(so, "window", window);
        Wire(so, "leftPane", left);
        Wire(so, "rightPane", right);
        Wire(so, "work", work);
        Wire(so, "findingsColumn", findingsColumn);
        Wire(so, "mainColumn", main);
        Wire(so, "face", header.Face);
        Wire(so, "nameText", header.Name);
        Wire(so, "countersText", header.Counters);
        Wire(so, "guide", parts.Guide);
        Wire(so, "shelf", shelf);
        Wire(so, "board", parts.Board);
        Wire(so, "decision", decision);
        Wire(so, "panesArea", panes.gameObject);
        Wire(so, "statusLine", status.Root.gameObject);
        Wire(so, "toast", toast);
        Wire(so, "config", config);
        so.ApplyModifiedProperties();
        BuildAppSearch(parts.App, body, lead.Search, config);
        return parts;
    }

    /// <summary>A view's workbench (a rule or the date is held on it).</summary>
    private static void WireBoard(Component view, MatchBoard board)
    {
        var so = new SerializedObject(view);
        Wire(so, "board", board);
        so.ApplyModifiedProperties();
    }

    /// <summary>A view's root: the whole content, hidden until its source shows.</summary>
    private static Transform ViewRoot(Transform content, string name, Color? fill, ThemeRoleId? role)
    {
        Transform view = Panel(content, name, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, fill, role);
        view.gameObject.SetActive(false);
        return view;
    }

    /// <summary>
    /// The Papers view (§2.4): the scanner's dark backing (the form style's),
    /// the hint shown instead of a copy, and the scanned-copy page template (the
    /// paper's form in a scroll, OfficeSceneUIBuilder.PcForms), cloned per
    /// paper by DocumentsView.
    /// </summary>
    private static DocumentsView BuildDocumentsView(Transform content, out AppView view)
    {
        FormStyleSO style = EnsureFormStyle();
        Transform root = ViewRoot(content, "DocumentsView", style.backing, ThemeRoleId.DiegeticBacking);
        TMP_Text hint = Text(root, "Hint", UiText.Get("app.doc.none"), PcType.Body, TextAlignmentOptions.Center, new Vector2(0.08f, 0.4f), new Vector2(0.92f, 0.6f),
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

    /// <summary>A labelled checkbox (a Toggle on a Button-role plate: the box, its check, the keyed label at Body size), on by default.</summary>
    private static Toggle BuildToggle(Transform parent, string name, string labelKey, Vector2 aMin, Vector2 aMax)
    {
        Transform plate = Panel(parent, name, aMin, aMax, Vector2.zero, Vector2.zero, WbSurface, ThemeRoleId.Button);
        SetAnchors(plate, aMin, aMax);
        HairlineFrame(plate);
        Transform box = Panel(plate, "Box", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(PcSize.M + 14f, 0f), new Vector2(28f, 28f), Color.white, ThemeRoleId.InputField);
        HairlineFrame(box, WbLineStrong, ThemeRoleId.HairlineStrong);
        Transform check = Panel(box, "Check", Center, Center, Vector2.zero, new Vector2(16f, 16f), WbAction, ThemeRoleId.PrimaryAction);
        check.GetComponent<Image>().raycastTarget = false;
        TMP_Text label = Text(plate, "Label", null, PcType.Body, TextAlignmentOptions.MidlineLeft, Vector2.zero, Vector2.one, Ink, ThemeRoleId.Button, labelKey,
                              FontStyles.Normal, ThemeTextKind.Button, true);
        ((RectTransform)label.transform).offsetMin = new Vector2(PcSize.M + 28f + PcSize.M, 0f);
        label.raycastTarget = false;
        Toggle toggle = GetOrAdd<Toggle>(plate.gameObject);
        toggle.targetGraphic = box.GetComponent<Image>();
        toggle.graphic = check.GetComponent<Image>();
        toggle.isOn = true;
        return toggle;
    }

    /// <summary>The scan toast (WN5, C10) on the investigation host, above the window layer: centred over the app's foot (never over the documents), its line at Body size and Open, hidden.</summary>
    private static AppToast BuildAppToast(Transform investHost, DesktopConfigSO config)
    {
        DestroyChildIfPresent(investHost, "AppToast");
        Transform strip = Panel(investHost, "AppToast", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-40f, config.MaximisedBottom + WbSize.Foot / 2f),
                                AppToastSize, PanelNavy, ThemeRoleId.Toast);
        TMP_Text line = Text(strip, "Text", string.Empty, PcType.Body, TextAlignmentOptions.MidlineLeft, new Vector2(0f, 0f), new Vector2(0.76f, 1f), Color.white,
                             ThemeRoleId.Toast, fit: true);
        ((RectTransform)line.transform).offsetMin = new Vector2(PcSize.L + 4f, 0f);
        Button open = MakeButton(strip, "OpenButton", null, new Vector2(0.78f, 0.18f), new Vector2(0.97f, 0.82f), null, ThemeRoleId.Button, "app.toast.open");
        ButtonLabel(open, PcType.Body);
        AppToast toast = strip.gameObject.AddComponent<AppToast>();
        var so = new SerializedObject(toast);
        Wire(so, "text", line);
        Wire(so, "openButton", open);
        so.ApplyModifiedProperties();
        strip.gameObject.SetActive(false);
        return toast;
    }
}
