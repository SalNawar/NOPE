using TMPro;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// One-click builder for the full Office investigation scene.
/// Creates and wires everything needed for a playable shift:
/// - Canvas + EventSystem (new Input System)
/// - HUD (day/money/stability), citation slip, verdict line  [OfficeUIController]
/// - Morning briefing + shift report panels  [DayFlowUIController]
/// - Investigation desk: claim banner, directives, draggable/multi-page document
///   windows, a reference-book shelf with openable book windows, a visual
///   compare bar, and Accept/Deny buttons  [InvestigationUIController + CompareController]
/// - GameManager + DaySystem (DayOrchestrator + DayEventDirector), auto-wired to
///   ContentLibrary_Main and a Day Plan
/// Safe to re-run: finds existing pieces by name and only fills gaps.
/// </summary>
public static class OfficeSceneUIBuilder
{
    // Windows XP "Luna" palette
    private static readonly Color XpBlue = new Color(0.13f, 0.34f, 0.86f, 1f);    // taskbar / title-bar base
    private static readonly Color XpGreen = new Color(0.24f, 0.6f, 0.23f, 1f);    // Start button
    private static readonly Color XpFace = new Color(0.925f, 0.913f, 0.847f, 1f); // #ECE9D8 control face
    private static readonly Color XpRed = new Color(0.86f, 0.25f, 0.18f, 1f);     // close button
    private static readonly Color Tooltip = new Color(1f, 1f, 0.88f, 1f);         // #FFFFE1 info yellow

    private static readonly Color PanelNavy = new Color(0.1f, 0.12f, 0.2f, 0.97f);
    private static readonly Color Paper = new Color(0.925f, 0.913f, 0.847f, 1f);  // XP window body
    private static readonly Color HeaderBar = new Color(0.13f, 0.34f, 0.86f, 1f); // XP title bar
    private static readonly Color RowBg = new Color(1f, 1f, 1f, 0.7f);            // near-white field row
    private static readonly Color Ink = new Color(0.1f, 0.09f, 0.08f, 1f);

    [MenuItem("Tools/TimeDesk/Build Office UI (HUD + Panels)")]
    public static void Build()
    {
        Canvas canvas = EnsureCanvas();
        Transform root = canvas.transform;
        EnsureEventSystem();

        // Hide leftovers from older builds (replaced by the investigation desk +
        // XP taskbar). Left in the scene, just inactive.
        foreach (string legacy in new[] { "CaseView", "HUD" })
        {
            Transform leg = root.Find(legacy);
            if (leg != null)
                leg.gameObject.SetActive(false);
        }

        // --- OfficeUIController (HUD + citation + verdict only; case display is the investigation desk) ---
        OfficeUIController officeUI = Object.FindFirstObjectByType<OfficeUIController>();
        if (officeUI != null && officeUI.GetComponent<Canvas>() != null)
        {
            Object.DestroyImmediate(officeUI);
            officeUI = null;
        }
        if (officeUI == null)
        {
            Transform existing = root.Find("OfficeUI");
            if (existing != null)
                officeUI = existing.GetComponent<OfficeUIController>() ?? existing.gameObject.AddComponent<OfficeUIController>();
            else
            {
                var go = new GameObject("OfficeUI", typeof(RectTransform));
                go.transform.SetParent(root, false);
                FullStretch((RectTransform)go.transform);
                officeUI = go.AddComponent<OfficeUIController>();
            }
        }

        // XP desktop wallpaper (behind everything) + taskbar with system-tray HUD.
        BuildDesktop(root);
        BuildTaskbar(root, out TMP_Text dayText, out TMP_Text moneyText, out TMP_Text stabilityText);

        // Verdict line (result text)
        TMP_Text verdictText = Text(root, "VerdictText", "", 26, TextAlignmentOptions.Center, new Vector2(0.25f, 0.86f), new Vector2(0.75f, 0.92f), Color.white);

        // Citation slip
        Transform citation = Panel(root, "CitationPanel", Center, Center, Vector2.zero, new Vector2(560f, 320f), new Color(0.85f, 0.2f, 0.15f, 0.96f));
        TMP_Text citationText = Text(citation, "CitationText", "TIMELINE DEVIATION NOTICE", 24, TextAlignmentOptions.Center, new Vector2(0.05f, 0.28f), new Vector2(0.95f, 0.95f), Color.white);
        Button citationContinue = MakeButton(citation, "ContinueButton", "Acknowledge", new Vector2(0.3f, 0.06f), new Vector2(0.7f, 0.24f));
        citation.gameObject.SetActive(false);

        // Briefing + Results — newsletter panels on the office overlay canvas so
        // they read in the booth view, not inside the PC desktop (which is hidden
        // outside MonitorFocus).
        Canvas officeCanvas = EnsureOfficeOverlayCanvas();
        DestroyChildIfPresent(root, "BriefingPanel");
        DestroyChildIfPresent(root, "ResultsPanel");
        DestroyChildIfPresent(officeCanvas.transform, "BriefingPanel");
        DestroyChildIfPresent(officeCanvas.transform, "ResultsPanel");

        Transform briefing = BuildNewsletter(officeCanvas.transform, "BriefingPanel", "THE TEMPORAL TIMES",
            "START SHIFT", out TMP_Text briefingTitle, out TMP_Text briefingBody, out Button startShift);
        Transform results = BuildNewsletter(officeCanvas.transform, "ResultsPanel", "SHIFT LEDGER — EVENING EDITION",
            "GO HOME", out TMP_Text resultsTitle, out TMP_Text resultsBody, out Button goHome);

        DayFlowUIController dayFlow = Object.FindFirstObjectByType<DayFlowUIController>();
        if (dayFlow == null)
        {
            var go = new GameObject("DayFlowUI");
            go.transform.SetParent(root, false);
            dayFlow = go.AddComponent<DayFlowUIController>();
        }

        // --- Investigation desk ---
        // Persistent host (never toggled) holds the controllers; InvestigationRoot is the toggled overlay.
        Transform investHost = Panel(root, "InvestigationUI", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        InvestigationUIController invest = investHost.GetComponent<InvestigationUIController>() ?? investHost.gameObject.AddComponent<InvestigationUIController>();
        CompareController compare = investHost.GetComponent<CompareController>() ?? investHost.gameObject.AddComponent<CompareController>();

        Transform investRoot = Panel(investHost, "InvestigationRoot", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        Transform windowLayer = Panel(investRoot, "WindowLayer", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);

        // Claim on a translucent XP-blue strip.
        Panel(investRoot, "ClaimStrip", new Vector2(0.06f, 0.87f), new Vector2(0.94f, 1f), Vector2.zero, Vector2.zero, new Color(0.06f, 0.18f, 0.42f, 0.8f));
        TMP_Text claimText = Text(investRoot, "ClaimBanner", "Visitor", 26, TextAlignmentOptions.Center, new Vector2(0.1f, 0.88f), new Vector2(0.9f, 0.99f), Color.white);

        // Desktop icon grid pinned to the top-left of the screen (like a real
        // OS). Investigation documents + reference books + the desktop apps all
        // register their launch tiles here, filling top-left downward.
        Transform bookShelf = Panel(investRoot, "BookShelf", new Vector2(0.005f, 0.06f), new Vector2(0.17f, 0.855f), Vector2.zero, Vector2.zero, null);
        AddGridLayout(bookShelf, new Vector2(82f, 60f), new Vector2(6f, 6f));
        Button shelfButtonTemplate = MakeButton(bookShelf, "BookShelfButtonTemplate", "Book", Vector2.zero, Vector2.one);
        shelfButtonTemplate.gameObject.SetActive(false);

        // Directives live in a sticky-note window (closed by default) opened
        // from a desktop icon, instead of a note that is always open.
        DestroyChildIfPresent(investRoot, "DirectivesNote");
        DestroyChildIfPresent(investRoot, "Directives"); // stray text from older builds
        OSWindowChrome directivesWindow = BuildOSWindow(windowLayer, "DirectivesWindow", "Directives",
            "Directives: all destinations cleared today.", new Vector2(430f, 360f));
        Image directivesImg = directivesWindow.GetComponent<Image>();
        if (directivesImg != null)
            directivesImg.color = new Color(1f, 0.96f, 0.6f, 0.97f);
        TMP_Text directivesText = directivesWindow.transform.Find("Body").GetComponent<TMP_Text>();
        BuildDesktopIcon(bookShelf, "IconDirectives", "Directives", directivesWindow, "");

        // Scanner is the deviation report: its body lists the discrepancies the
        // player has documented for the current case (drives deny gating).
        DestroyChildIfPresent(windowLayer, "IconScannerWindow");
        OSWindowChrome scannerWindow = BuildOSWindow(windowLayer, "IconScannerWindow", "Scanner — Deviation Report",
            "No deviations documented.", new Vector2(560f, 420f));
        TMP_Text scannerText = scannerWindow.transform.Find("Body").GetComponent<TMP_Text>();
        BuildDesktopIcon(bookShelf, "IconScanner", "Scanner", scannerWindow, "");

        // Compare bar (XP tooltip-yellow, above the shelf)
        Transform compareBar = Panel(investRoot, "CompareBar", new Vector2(0.1f, 0.27f), new Vector2(0.9f, 0.34f), Vector2.zero, Vector2.zero, Tooltip);
        TMP_Text compareText = Text(compareBar, "CompareText", "", 22, TextAlignmentOptions.Center, new Vector2(0.02f, 0f), new Vector2(0.98f, 1f), Ink);
        compareBar.gameObject.SetActive(false);

        // Accept / Deny (above the taskbar)
        Button acceptButton = MakeButton(investRoot, "AcceptButton", "ACCEPT", new Vector2(0.3f, 0.06f), new Vector2(0.49f, 0.15f), new Color(0.2f, 0.5f, 0.24f, 1f));
        Button denyButton = MakeButton(investRoot, "DenyButton", "DENY", new Vector2(0.51f, 0.06f), new Vector2(0.7f, 0.15f), new Color(0.72f, 0.2f, 0.18f, 1f));

        // Window templates (disabled, cloned at runtime)
        DocumentWindowController docTemplate = BuildDocumentWindow(windowLayer);
        ReferenceBookWindowController bookTemplate = BuildBookWindow(windowLayer);

        investRoot.gameObject.SetActive(false);

        // --- Content + logic objects ---
        ContentLibrarySO library = FindAssetByName<ContentLibrarySO>("ContentLibrary_Main") ?? FindFirstAsset<ContentLibrarySO>();
        DayPlanSO dayPlan = null;
        if (library != null)
        {
            dayPlan = library.GetDayPlan(1);
            if (dayPlan == null && library.DayPlans.Count > 0) dayPlan = library.DayPlans[0];
        }
        if (dayPlan == null) dayPlan = FindFirstAsset<DayPlanSO>();
        if (library == null) Debug.LogWarning("[TimeDesk] No ContentLibrarySO found — assign GameManager.contentLibrary manually.");
        if (dayPlan == null) Debug.LogWarning("[TimeDesk] No DayPlanSO found — generate content first (Tools > TimeDesk).");

        DayOrchestrator orchestrator = Object.FindFirstObjectByType<DayOrchestrator>();
        if (orchestrator == null) orchestrator = new GameObject("DaySystem").AddComponent<DayOrchestrator>();
        DayEventDirector eventDirector = orchestrator.GetComponent<DayEventDirector>() ?? orchestrator.gameObject.AddComponent<DayEventDirector>();

        GameManager gameManager = Object.FindFirstObjectByType<GameManager>();
        if (gameManager == null) gameManager = new GameObject("GameManager").AddComponent<GameManager>();

        // Booth + cameras + view controller (new). The existing Canvas becomes
        // the Monitor-Focus desktop, hidden until the CRT is focused.
        OfficeViewController officeView = BuildBooth(canvas);

        // Fake-OS desktop shell: NEW apps only (existing document/reference/compare
        // windows are launched by the investigation icon grid), plus a Start menu.
        BuildDesktopShell(canvas, bookShelf, windowLayer);

        // --- Wire everything ---
        var soOffice = new SerializedObject(officeUI);
        SetRef(soOffice, "moneyText", moneyText);
        SetRef(soOffice, "stabilityText", stabilityText);
        SetRef(soOffice, "dayText", dayText);
        SetRef(soOffice, "resultText", verdictText);
        SetRef(soOffice, "citationPanel", citation.gameObject);
        SetRef(soOffice, "citationText", citationText);
        SetRef(soOffice, "citationContinueButton", citationContinue);
        soOffice.ApplyModifiedProperties();

        var soFlow = new SerializedObject(dayFlow);
        SetRef(soFlow, "briefingPanel", briefing.gameObject);
        SetRef(soFlow, "briefingTitleText", briefingTitle);
        SetRef(soFlow, "briefingBodyText", briefingBody);
        SetRef(soFlow, "startShiftButton", startShift);
        SetRef(soFlow, "resultsPanel", results.gameObject);
        SetRef(soFlow, "resultsTitleText", resultsTitle);
        SetRef(soFlow, "resultsBodyText", resultsBody);
        SetRef(soFlow, "goHomeButton", goHome);
        soFlow.ApplyModifiedProperties();

        var soCompare = new SerializedObject(compare);
        SetRef(soCompare, "compareBar", compareBar.gameObject);
        SetRef(soCompare, "compareText", compareText);
        SetColor(soCompare, "matchColor", new Color(0.05f, 0.45f, 0.12f, 1f));
        SetColor(soCompare, "mismatchColor", new Color(0.72f, 0.1f, 0.08f, 1f));
        SetColor(soCompare, "neutralColor", new Color(0.18f, 0.15f, 0.05f, 1f));
        soCompare.ApplyModifiedProperties();

        var soInvest = new SerializedObject(invest);
        SetRef(soInvest, "root", investRoot.gameObject);
        SetRef(soInvest, "claimText", claimText);
        SetRef(soInvest, "directivesText", directivesText);
        SetRef(soInvest, "acceptButton", acceptButton);
        SetRef(soInvest, "denyButton", denyButton);
        SetRef(soInvest, "compareController", compare);
        SetRef(soInvest, "windowLayer", windowLayer);
        SetRef(soInvest, "documentWindowTemplate", docTemplate);
        SetRef(soInvest, "bookWindowTemplate", bookTemplate);
        SetRef(soInvest, "bookShelfRoot", bookShelf);
        SetRef(soInvest, "bookShelfButtonTemplate", shelfButtonTemplate);
        SetRef(soInvest, "scannerText", scannerText);
        SetRef(soInvest, "scannerWindow", scannerWindow);
        soInvest.ApplyModifiedProperties();

        var soOrch = new SerializedObject(orchestrator);
        SetRef(soOrch, "eventDirector", eventDirector);
        if (dayPlan != null) SetRef(soOrch, "dayPlan", dayPlan);
        soOrch.ApplyModifiedProperties();

        var soGm = new SerializedObject(gameManager);
        SetRef(soGm, "orchestrator", orchestrator);
        if (library != null) SetRef(soGm, "contentLibrary", library);
        if (dayPlan != null) SetRef(soGm, "dayPlan", dayPlan);
        SetRef(soGm, "officeUI", officeUI);
        SetRef(soGm, "investigationUI", invest);
        SetRef(soGm, "dayFlowUI", dayFlow);
        SetRef(soGm, "officeView", officeView);
        SetRef(soGm, "readySign", GameObject.Find("OfficeRoot")?.transform.Find("ReadySign")?.GetComponent<Clickable>());
        soGm.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        Debug.Log("[TimeDesk] Office investigation desk built and wired (HUD, citation, briefing/results, claim, document + book windows, compare, Accept/Deny, GameManager, DaySystem). Save the scene.");
    }

    // -----------------------------
    // Window builders
    // -----------------------------

    private static DocumentWindowController BuildDocumentWindow(Transform layer)
    {
        Transform win = Panel(layer, "DocumentWindowTemplate", Center, Center, Vector2.zero, new Vector2(540f, 440f), Paper);
        WindowShell s = BuildWindowShell(win, "Document");
        DocumentWindowController c = win.GetComponent<DocumentWindowController>() ?? win.gameObject.AddComponent<DocumentWindowController>();
        var so = new SerializedObject(c);
        SetRef(so, "titleText", s.title);
        SetRef(so, "pageText", s.page);
        SetRef(so, "prevButton", s.prev);
        SetRef(so, "nextButton", s.next);
        SetRef(so, "fieldRowsRoot", s.rowsRoot);
        SetRef(so, "fieldRowTemplate", s.rowTemplate);
        so.ApplyModifiedProperties();
        win.gameObject.SetActive(false);
        return c;
    }

    private static ReferenceBookWindowController BuildBookWindow(Transform layer)
    {
        Transform win = Panel(layer, "BookWindowTemplate", Center, Center, Vector2.zero, new Vector2(540f, 440f), Paper);
        WindowShell s = BuildWindowShell(win, "Reference");
        ReferenceBookWindowController c = win.GetComponent<ReferenceBookWindowController>() ?? win.gameObject.AddComponent<ReferenceBookWindowController>();
        var so = new SerializedObject(c);
        SetRef(so, "titleText", s.title);
        SetRef(so, "pageText", s.page);
        SetRef(so, "prevButton", s.prev);
        SetRef(so, "nextButton", s.next);
        SetRef(so, "entryRowsRoot", s.rowsRoot);
        SetRef(so, "entryRowTemplate", s.rowTemplate);
        so.ApplyModifiedProperties();
        win.gameObject.SetActive(false);
        return c;
    }

    private struct WindowShell
    {
        public TMP_Text title;
        public TMP_Text page;
        public Button prev;
        public Button next;
        public Transform rowsRoot;
        public GameObject rowTemplate;
    }

    private static WindowShell BuildWindowShell(Transform win, string titleLabel)
    {
        // XP title bar (drag handle) with gloss highlight + window controls.
        Transform header = Panel(win, "Header", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -15f), new Vector2(0f, 30f), HeaderBar);
        Panel(header, "Gloss", new Vector2(0f, 0.5f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, new Color(1f, 1f, 1f, 0.14f));
        DraggableWindow drag = header.GetComponent<DraggableWindow>() ?? header.gameObject.AddComponent<DraggableWindow>();
        var soDrag = new SerializedObject(drag);
        SetRef(soDrag, "windowRoot", (RectTransform)win);
        soDrag.ApplyModifiedProperties();
        TMP_Text title = Text(header, "TitleText", titleLabel, 15, TextAlignmentOptions.Left, new Vector2(0.04f, 0f), new Vector2(0.76f, 1f), Color.white);
        title.fontStyle = FontStyles.Bold;
        BuildWinControls(win, header);

        // Rows container (scroll-free vertical list)
        Transform rowsRoot = Panel(win, "Rows", new Vector2(0.04f, 0.12f), new Vector2(0.96f, 0.84f), Vector2.zero, Vector2.zero, null);
        AddVLayout(rowsRoot, 4f);
        if (rowsRoot.GetComponent<RectMask2D>() == null)
            rowsRoot.gameObject.AddComponent<RectMask2D>(); // clip any overflow inside the window

        // Row template
        GameObject rowTemplate = BuildRowTemplate(rowsRoot);

        // Footer page controls
        Button prev = MakeButton(win, "PrevButton", "<", new Vector2(0.04f, 0.02f), new Vector2(0.18f, 0.1f));
        TMP_Text page = Text(win, "PageText", "Page 1/1", 18, TextAlignmentOptions.Center, new Vector2(0.2f, 0.02f), new Vector2(0.8f, 0.1f), Ink);
        Button next = MakeButton(win, "NextButton", ">", new Vector2(0.82f, 0.02f), new Vector2(0.96f, 0.1f));

        return new WindowShell { title = title, page = page, prev = prev, next = next, rowsRoot = rowsRoot, rowTemplate = rowTemplate };
    }

    private static GameObject BuildRowTemplate(Transform parent)
    {
        Transform existing = parent.Find("RowTemplate");
        if (existing != null)
            return existing.gameObject;

        var go = new GameObject("RowTemplate", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        img.color = RowBg;
        go.AddComponent<Button>().targetGraphic = img;
        var le = go.AddComponent<LayoutElement>();
        le.minHeight = 34f;
        le.preferredHeight = 34f;
        AddHLayout(go.transform, 8f);

        Text(go.transform, "Label", "Label", 18, TextAlignmentOptions.Left, Vector2.zero, Vector2.one, Ink);
        Text(go.transform, "Value", "Value", 18, TextAlignmentOptions.Left, Vector2.zero, Vector2.one, Ink);

        go.SetActive(false);
        return go;
    }

    // -----------------------------
    // Primitive helpers
    // -----------------------------

    private static readonly Vector2 Center = new Vector2(0.5f, 0.5f);

    private static Canvas EnsureCanvas()
    {
        // The desktop canvas — never the office overlay canvas, which hosts the
        // briefing/results newsletters and stays visible outside MonitorFocus.
        Canvas canvas = null;
        foreach (Canvas c in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (c.gameObject.name == "OfficeOverlayCanvas")
                continue;
            canvas = c;
            break;
        }
        if (canvas == null)
        {
            var go = new GameObject("Canvas", typeof(RectTransform));
            canvas = go.AddComponent<Canvas>();
            go.AddComponent<CanvasScaler>();
            go.AddComponent<GraphicRaycaster>();
        }
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvas.GetComponent<CanvasScaler>() ?? canvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        if (canvas.GetComponent<GraphicRaycaster>() == null) canvas.gameObject.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
    }

    private static void SetRef(SerializedObject so, string prop, Object value)
    {
        SerializedProperty p = so.FindProperty(prop);
        if (p != null) p.objectReferenceValue = value;
    }

    private static void SetColor(SerializedObject so, string prop, Color value)
    {
        SerializedProperty p = so.FindProperty(prop);
        if (p != null) p.colorValue = value;
    }

    private static void FullStretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void AddVLayout(Transform t, float spacing)
    {
        var l = t.GetComponent<VerticalLayoutGroup>() ?? t.gameObject.AddComponent<VerticalLayoutGroup>();
        l.spacing = spacing;
        l.padding = new RectOffset(6, 6, 6, 6);
        l.childAlignment = TextAnchor.UpperCenter;
        l.childForceExpandWidth = true;
        l.childForceExpandHeight = false;
        l.childControlWidth = true;
        l.childControlHeight = true; // honor each row's LayoutElement height so rows don't overflow
    }

    private static void AddHLayout(Transform t, float spacing)
    {
        var l = t.GetComponent<HorizontalLayoutGroup>() ?? t.gameObject.AddComponent<HorizontalLayoutGroup>();
        l.spacing = spacing;
        l.childAlignment = TextAnchor.MiddleCenter;
        l.childForceExpandWidth = true;
        l.childForceExpandHeight = true;
        l.childControlWidth = true;
        l.childControlHeight = true;
    }

    private static void AddGridLayout(Transform t, Vector2 cell, Vector2 spacing)
    {
        // A GameObject can only host one layout group — drop any conflicting one.
        HorizontalLayoutGroup h = t.GetComponent<HorizontalLayoutGroup>();
        if (h != null)
            Object.DestroyImmediate(h);
        VerticalLayoutGroup v = t.GetComponent<VerticalLayoutGroup>();
        if (v != null)
            Object.DestroyImmediate(v);

        GridLayoutGroup l = t.GetComponent<GridLayoutGroup>();
        if (l == null)
            l = t.gameObject.AddComponent<GridLayoutGroup>();
        l.cellSize = cell;
        l.spacing = spacing;
        l.padding = new RectOffset(4, 4, 4, 4);
        l.startCorner = GridLayoutGroup.Corner.UpperLeft;
        l.startAxis = GridLayoutGroup.Axis.Horizontal;
        l.childAlignment = TextAnchor.UpperLeft;
        l.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        l.constraintCount = 2;
    }

    private static Transform Panel(Transform parent, string name, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size, Color? bg)
    {
        Transform existing = parent.Find(name);
        GameObject go = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform));
        if (existing == null)
            go.transform.SetParent(parent, false);

        // Geometry is re-applied on every build so layout fixes reach scenes
        // that were built with an older version of this tool.
        var rt = (RectTransform)go.transform;
        rt.anchorMin = aMin;
        rt.anchorMax = aMax;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        if (bg.HasValue)
        {
            Image img = go.GetComponent<Image>();
            if (img == null)
                img = go.AddComponent<Image>();
            img.color = bg.Value;
        }
        return go.transform;
    }

    private static TMP_Text Text(Transform parent, string name, string content, int size, TextAlignmentOptions align, Vector2 aMin, Vector2 aMax, Color color)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
        {
            var et = existing.GetComponent<TMP_Text>();
            if (et != null) return et;
            // Same-named non-text leftover from an older build: replace it.
            Object.DestroyImmediate(existing.gameObject);
        }

        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = aMin;
        rt.anchorMax = aMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = content;
        t.fontSize = size;
        t.alignment = align;
        t.color = color;
        return t;
    }

    private static Button MakeButton(Transform parent, string name, string label, Vector2 aMin, Vector2 aMax, Color? color = null)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
        {
            var eb = existing.GetComponent<Button>();
            if (eb != null) return eb;
            // Same-named non-button leftover from an older build: replace it
            // instead of silently creating a duplicate sibling.
            Object.DestroyImmediate(existing.gameObject);
        }

        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = aMin;
        rt.anchorMax = aMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        Image img = go.AddComponent<Image>();
        img.color = color ?? XpFace;
        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        Color labelColor = color.HasValue ? Color.white : Color.black;
        Text(go.transform, "Label", label, 22, TextAlignmentOptions.Center, Vector2.zero, Vector2.one, labelColor);
        return btn;
    }

    // ----------------------------- Windows XP theme -----------------------------

    private static void BuildDesktop(Transform root)
    {
        Transform desk = Panel(root, "Desktop", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.23f, 0.45f, 0.74f, 1f));
        Image img = desk.GetComponent<Image>();
        Sprite wall = EnsureWallpaper();
        if (img != null && wall != null)
        {
            img.sprite = wall;
            img.type = Image.Type.Simple;
            img.preserveAspect = false;
            img.color = Color.white; // don't tint the wallpaper with the fallback color
        }
        desk.SetAsFirstSibling();
    }

    /// <summary>Pins a layout-group child to a fixed height.</summary>
    private static void SetLayoutHeight(Component c, float height)
    {
        LayoutElement le = c.GetComponent<LayoutElement>();
        if (le == null)
            le = c.gameObject.AddComponent<LayoutElement>();
        le.minHeight = height;
        le.preferredHeight = height;
    }

    // ----------------------------- Office newsletter panels -----------------------------

    /// <summary>
    /// Overlay canvas for office-view UI (the briefing/results newsletters).
    /// Separate from the desktop canvas, which OfficeViewController hides
    /// outside MonitorFocus.
    /// </summary>
    private static Canvas EnsureOfficeOverlayCanvas()
    {
        GameObject go = GameObject.Find("OfficeOverlayCanvas");
        if (go == null)
            go = new GameObject("OfficeOverlayCanvas", typeof(RectTransform));

        Canvas canvas = go.GetComponent<Canvas>();
        if (canvas == null)
            canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10; // above the desktop canvas

        CanvasScaler scaler = go.GetComponent<CanvasScaler>();
        if (scaler == null)
            scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        if (go.GetComponent<GraphicRaycaster>() == null)
            go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    /// <summary>
    /// Builds a newsletter-styled panel (masthead + rule + dateline title + body
    /// + one action button) for the office view. Hidden by default.
    /// </summary>
    private static Transform BuildNewsletter(Transform parent, string name, string masthead, string buttonLabel,
        out TMP_Text title, out TMP_Text body, out Button action)
    {
        Transform panel = Panel(parent, name, Center, Center, Vector2.zero, new Vector2(700f, 780f), Ink);
        Transform paper = Panel(panel, "Paper", Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-10f, -10f), Paper);

        TMP_Text head = Text(paper, "Masthead", masthead, 38, TextAlignmentOptions.Center, new Vector2(0.04f, 0.9f), new Vector2(0.96f, 0.99f), Ink);
        head.fontStyle = FontStyles.Bold;
        Panel(paper, "Rule", new Vector2(0.06f, 0.885f), new Vector2(0.94f, 0.889f), Vector2.zero, Vector2.zero, Ink);

        title = Text(paper, "TitleText", "", 24, TextAlignmentOptions.Center, new Vector2(0.05f, 0.82f), new Vector2(0.95f, 0.875f), Ink);
        body = Text(paper, "BodyText", "...", 21, TextAlignmentOptions.TopLeft, new Vector2(0.08f, 0.14f), new Vector2(0.92f, 0.8f), Ink);
        action = MakeButton(paper, "ActionButton", buttonLabel, new Vector2(0.3f, 0.03f), new Vector2(0.7f, 0.11f), new Color(0.16f, 0.15f, 0.13f, 1f));

        panel.gameObject.SetActive(false);
        return panel;
    }

    private static void BuildTaskbar(Transform root, out TMP_Text dayText, out TMP_Text moneyText, out TMP_Text stabilityText)
    {
        Transform bar = Panel(root, "Taskbar", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 18f), new Vector2(0f, 36f), XpBlue);
        Panel(bar, "TaskbarGloss", new Vector2(0f, 0.72f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, new Color(1f, 1f, 1f, 0.18f));

        Transform start = Panel(bar, "StartButton", new Vector2(0f, 0f), new Vector2(0.12f, 1f), Vector2.zero, Vector2.zero, XpGreen);
        Panel(start, "StartGloss", new Vector2(0f, 0.55f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, new Color(1f, 1f, 1f, 0.18f));
        TMP_Text st = Text(start, "Label", "start", 20, TextAlignmentOptions.Center, Vector2.zero, Vector2.one, Color.white);
        st.fontStyle = FontStyles.Bold | FontStyles.Italic;

        Transform tray = Panel(bar, "Tray", new Vector2(0.74f, 0.12f), new Vector2(0.995f, 0.88f), Vector2.zero, Vector2.zero, new Color(0.1f, 0.32f, 0.78f, 1f));
        dayText = Text(tray, "DayText", "Day 1", 18, TextAlignmentOptions.Center, new Vector2(0f, 0f), new Vector2(0.34f, 1f), Color.white);
        moneyText = Text(tray, "MoneyText", "Credits: 0", 18, TextAlignmentOptions.Center, new Vector2(0.34f, 0f), new Vector2(0.67f, 1f), Color.white);
        stabilityText = Text(tray, "StabilityText", "Stability: 100%", 18, TextAlignmentOptions.Center, new Vector2(0.67f, 0f), new Vector2(1f, 1f), Color.white);

        bar.SetAsLastSibling();
    }

    /// <summary>
    /// Builds WORKING min/max/close buttons on a window header and wires an
    /// OSWindowChrome on the window root. Shared by every window type so the
    /// chrome behaves identically everywhere. Destroys older decorative
    /// controls (pre-chrome builds used plain panels) before rebuilding.
    /// </summary>
    private static OSWindowChrome BuildWinControls(Transform win, Transform header)
    {
        DestroyChildIfPresent(header, "MinBtn");
        DestroyChildIfPresent(header, "MaxBtn");
        DestroyChildIfPresent(header, "CloseBtn");

        Button minB = MakeButton(header, "MinBtn", "_", new Vector2(0.79f, 0.16f), new Vector2(0.85f, 0.86f));
        Button maxB = MakeButton(header, "MaxBtn", "[]", new Vector2(0.855f, 0.16f), new Vector2(0.915f, 0.86f));
        Button closeB = MakeButton(header, "CloseBtn", "X", new Vector2(0.925f, 0.16f), new Vector2(0.985f, 0.86f), XpRed);

        OSWindowChrome chrome = win.GetComponent<OSWindowChrome>();
        if (chrome == null)
            chrome = win.gameObject.AddComponent<OSWindowChrome>();
        var so = new SerializedObject(chrome);
        SetRef(so, "window", (RectTransform)win);
        SetRef(so, "minimizeButton", minB);
        SetRef(so, "maximizeButton", maxB);
        SetRef(so, "closeButton", closeB);
        so.ApplyModifiedProperties();
        return chrome;
    }

    private static Sprite EnsureWallpaper()
    {
        const string assetPath = "Assets/Art/Generated/xp_bliss.png";
        Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        if (existing != null)
            return existing;

        EnsureFolderTree("Assets/Art/Generated");

        int W = 960, H = 540;
        var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
        for (int y = 0; y < H; y++)
        {
            float fy = (float)y / H; // 0 bottom .. 1 top
            for (int x = 0; x < W; x++)
            {
                float fx = (float)x / W;
                float hill = 0.30f + 0.05f * Mathf.Sin(fx * 6.2831f * 1.4f) + 0.03f * Mathf.Sin(fx * 6.2831f * 3.1f + 1.2f);
                Color c;
                if (fy < hill)
                {
                    float t = Mathf.InverseLerp(0f, hill, fy);
                    c = Color.Lerp(new Color(0.27f, 0.45f, 0.15f), new Color(0.49f, 0.69f, 0.26f), t);
                }
                else
                {
                    float t = Mathf.InverseLerp(hill, 1f, fy);
                    c = Color.Lerp(new Color(0.78f, 0.9f, 1f), new Color(0.2f, 0.43f, 0.76f), t);
                    c = Color.Lerp(c, Color.white, Clouds(fx, fy));
                }
                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();

        System.IO.File.WriteAllBytes(System.IO.Path.Combine(Application.dataPath, "Art/Generated/xp_bliss.png"), tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        if (AssetImporter.GetAtPath(assetPath) is TextureImporter imp)
        {
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.mipmapEnabled = false;
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
    }

    private static float Clouds(float fx, float fy)
    {
        float c = Blob(fx, fy, 0.22f, 0.82f, 0.13f, 0.05f)
                + Blob(fx, fy, 0.6f, 0.9f, 0.16f, 0.05f)
                + Blob(fx, fy, 0.82f, 0.73f, 0.1f, 0.04f);
        return Mathf.Clamp01(c);
    }

    private static float Blob(float fx, float fy, float cx, float cy, float rx, float ry)
    {
        float dx = (fx - cx) / rx, dy = (fy - cy) / ry;
        return Mathf.Clamp01(1f - (dx * dx + dy * dy)) * 0.85f;
    }

    // ----------------------------- Office booth (world-space) -----------------------------

    /// <summary>
    /// Returns a flat-color placeholder Sprite at Assets/Art/Office/Placeholder/{name}.png,
    /// creating it if missing. Swap the PNG later for final art (same path/name).
    /// </summary>
    private static Sprite EnsureOfficeSprite(string name, Color color, int w, int h)
    {
        string folder = "Assets/Art/Office/Placeholder";
        string assetPath = $"{folder}/{name}.png";
        Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        if (existing != null)
            return existing;

        EnsureFolderTree(folder);

        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        var pixels = new Color32[w * h];
        Color32 c32 = color;
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = c32;
        tex.SetPixels32(pixels);
        tex.Apply();

        string abs = System.IO.Path.Combine(Application.dataPath, $"Art/Office/Placeholder/{name}.png");
        System.IO.File.WriteAllBytes(abs, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        if (AssetImporter.GetAtPath(assetPath) is TextureImporter imp)
        {
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = 100f;
            imp.mipmapEnabled = false;
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
    }

    /// <summary>Creates (or finds) a world-space sprite GameObject under a parent.</summary>
    private static SpriteRenderer EnsureSprite(Transform parent, string name, Sprite sprite, Vector3 localPos, int sortingOrder)
    {
        Transform existing = parent.Find(name);
        GameObject go = existing != null ? existing.gameObject : new GameObject(name);
        if (existing == null)
            go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        var sr = go.GetComponent<SpriteRenderer>();
        if (sr == null)
            sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = sortingOrder;
        return sr;
    }

    /// <summary>
    /// Builds the world-space booth, two Cinemachine cameras, a Physics2DRaycaster,
    /// and wires OfficeViewController + the CRT/READY clickables. Idempotent.
    /// </summary>
    private static OfficeViewController BuildBooth(Canvas desktopCanvas)
    {
        // Root for all booth world objects.
        GameObject root = GameObject.Find("OfficeRoot") ?? new GameObject("OfficeRoot");

        // Set dressing (flat placeholder sprites; swap later).
        EnsureSprite(root.transform, "BackWall",       EnsureOfficeSprite("backwall",  new Color(0.17f, 0.17f, 0.22f), 400, 240), new Vector3(0f, 0f, 10f), -100);
        EnsureSprite(root.transform, "LeftPartition",  EnsureOfficeSprite("partition", new Color(0.24f, 0.24f, 0.30f), 120, 240), new Vector3(-6.5f, 0f, 5f), -50);
        EnsureSprite(root.transform, "RightPartition", EnsureOfficeSprite("partition", new Color(0.24f, 0.24f, 0.30f), 120, 240), new Vector3( 6.5f, 0f, 5f), -50);
        EnsureSprite(root.transform, "Desk",           EnsureOfficeSprite("desk",      new Color(0.26f, 0.20f, 0.15f), 400, 90),  new Vector3(0f, -3.6f, 0f), -10);
        EnsureSprite(root.transform, "Traveller",      EnsureOfficeSprite("traveller", new Color(0.49f, 0.42f, 0.86f), 60, 110),  new Vector3(0f, 0.4f, 2f), -20);

        // Interactables: CRT (right of desk) and READY sign (center).
        SpriteRenderer crt = EnsureSprite(root.transform, "CRTMonitor", EnsureOfficeSprite("crt", new Color(0.85f, 0.81f, 0.65f), 150, 130), new Vector3(3.4f, -1.8f, 0f), 0);
        Clickable crtClick = EnsureClickable(crt.gameObject);

        SpriteRenderer sign = EnsureSprite(root.transform, "ReadySign", EnsureOfficeSprite("sign", new Color(0.79f, 0.76f, 0.58f), 96, 50), new Vector3(0f, -2.4f, 0f), 0);
        Clickable signClick = EnsureClickable(sign.gameObject);

        // Cameras.
        GameObject camsRoot = GameObject.Find("Cameras") ?? new GameObject("Cameras");
        CinemachineCamera officeCam = EnsureVcam(camsRoot.transform, "OfficeVCam", new Vector3(0f, -1f, -10f), 6f);
        CinemachineCamera monitorCam = EnsureVcam(camsRoot.transform, "MonitorVCam", new Vector3(3.4f, -1.8f, -10f), 1.4f);

        // Brain + 2D raycaster on the Main Camera.
        Camera main = Camera.main;
        if (main == null)
        {
            var mc = new GameObject("Main Camera", typeof(Camera));
            mc.tag = "MainCamera";
            main = mc.GetComponent<Camera>();
        }
        main.orthographic = true;
        if (main.GetComponent<CinemachineBrain>() == null) main.gameObject.AddComponent<CinemachineBrain>();
        if (main.GetComponent<Physics2DRaycaster>() == null)
            main.gameObject.AddComponent<Physics2DRaycaster>();

        // Camera rig + view controller on OfficeRoot.
        CinemachineCameraRig rig = root.GetComponent<CinemachineCameraRig>();
        if (rig == null)
            rig = root.AddComponent<CinemachineCameraRig>();
        var soRig = new SerializedObject(rig);
        SetRef(soRig, "officeCam", officeCam);
        SetRef(soRig, "monitorCam", monitorCam);
        soRig.ApplyModifiedProperties();

        OfficeViewController view = root.GetComponent<OfficeViewController>();
        if (view == null)
            view = root.AddComponent<OfficeViewController>();
        var soView = new SerializedObject(view);
        SetRef(soView, "cameraRigBehaviour", rig);
        SetRef(soView, "desktopRoot", desktopCanvas.gameObject);
        soView.ApplyModifiedProperties();

        // CRT click -> focus monitor; READY click is wired to GameManager's gate,
        // but also focuses the monitor so the player lands on the desktop.
        WireClickToFocusMonitor(crtClick, view);
        WireClickToFocusMonitor(signClick, view);

        // Diegetic readouts + a timeline-reactive poster + the desktop Back button.
        BuildReadouts(root.transform);
        BuildReactiveProp(root.transform);
        BuildBackToOfficeButton(desktopCanvas, view);

        return view;
    }

    /// <summary>
    /// Builds the in-world Day calendar, Stability monitor, and Credits till, and
    /// wires an OfficeReadouts component on OfficeRoot. Idempotent.
    /// </summary>
    private static void BuildReadouts(Transform root)
    {
        // Day — wall calendar on the left partition.
        SpriteRenderer cal = EnsureSprite(root, "DayCalendar", EnsureOfficeSprite("calendar", new Color(0.93f, 0.91f, 0.85f), 80, 100), new Vector3(-6.2f, 1.6f, 4.5f), -40);
        TextMeshPro dayText = WorldText(cal.transform, "DayNumber", "01", 6f, new Color(0.15f, 0.13f, 0.1f), new Vector3(0f, -0.1f, -0.1f), new Vector2(2f, 2f), -39);

        // Stability — TVA-style monitor on the right partition (the sprite is the lamp).
        SpriteRenderer lamp = EnsureSprite(root, "StabilityMonitor", EnsureOfficeSprite("stabilitymonitor", Color.white, 110, 80), new Vector3(6.2f, 1.6f, 4.5f), -40);
        TextMeshPro stabText = WorldText(lamp.transform, "StabilityPercent", "100%", 4.5f, new Color(0.05f, 0.1f, 0.07f), new Vector3(0f, 0f, -0.1f), new Vector2(2.4f, 1f), -39);

        // Credits — cash till on the desk (with an AudioSource for the ding).
        SpriteRenderer till = EnsureSprite(root, "CreditsTill", EnsureOfficeSprite("till", new Color(0.55f, 0.5f, 0.42f), 110, 80), new Vector3(-2.6f, -2.7f, -1f), 5);
        TextMeshPro creditsText = WorldText(till.transform, "CreditsNumber", "0", 4.5f, new Color(0.1f, 0.9f, 0.5f), new Vector3(0f, 0.1f, -0.1f), new Vector2(2.4f, 1f), 6);
        AudioSource ding = till.GetComponent<AudioSource>();
        if (ding == null)
            ding = till.gameObject.AddComponent<AudioSource>();
        ding.playOnAwake = false;

        // OfficeReadouts on the root, wired to all three.
        OfficeReadouts readouts = root.GetComponent<OfficeReadouts>();
        if (readouts == null)
            readouts = root.gameObject.AddComponent<OfficeReadouts>();
        var so = new SerializedObject(readouts);
        SetRef(so, "dayText", dayText);
        SetRef(so, "stabilityText", stabText);
        SetRef(so, "stabilityLamp", lamp);
        SetRef(so, "creditsText", creditsText);
        SetRef(so, "creditsDing", ding);
        so.ApplyModifiedProperties();
    }

    /// <summary>
    /// Adds a booth poster that reacts to the timeline (Visuals cue channel).
    /// Ships with a default sprite and an empty mapping list for the designer.
    /// </summary>
    private static void BuildReactiveProp(Transform root)
    {
        SpriteRenderer poster = EnsureSprite(root, "ReactivePoster", EnsureOfficeSprite("poster", new Color(0.5f, 0.45f, 0.6f), 80, 110), new Vector3(-6.2f, -0.6f, 4.5f), -40);
        TimelineReactiveSprite reactive = poster.GetComponent<TimelineReactiveSprite>();
        if (reactive == null)
            reactive = poster.gameObject.AddComponent<TimelineReactiveSprite>();
        var so = new SerializedObject(reactive);
        SetRef(so, "target", poster);
        so.ApplyModifiedProperties();
    }

    /// <summary>Adds a visible "Back to Office" button to the desktop canvas.</summary>
    private static void BuildBackToOfficeButton(Canvas desktopCanvas, OfficeViewController view)
    {
        Button back = MakeButton(desktopCanvas.transform, "BackToOfficeButton", "< Office",
            new Vector2(0.005f, 0.93f), new Vector2(0.105f, 0.99f), new Color(0.2f, 0.3f, 0.5f, 0.95f));
        WirePersistentVoid(back, "m_OnClick", view, nameof(OfficeViewController.FocusOffice));
    }

    /// <summary>
    /// Builds the fake-OS desktop shell: a left column of icons (some unlock-gated)
    /// that open placeholder windows with min/max/close chrome, plus a Start menu
    /// (Settings + Power) wired to a DesktopShell on the canvas. Idempotent.
    /// </summary>
    private static void BuildDesktopShell(Canvas canvas, Transform iconGrid, Transform windowLayer)
    {
        Transform root = canvas.transform;

        // Remove the earlier redundant placeholder shell (parallel desktop) from
        // prior builds so it no longer overlaps the real investigation desk.
        DestroyChildIfPresent(root, "DesktopIcons");
        DestroyChildIfPresent(root, "DesktopWindowLayer");

        // Only genuinely-new apps get placeholder windows; existing
        // Passport/Permit documents, reference books, and Compare are launched by
        // the investigation icon grid (real windows with real data). Scanner and
        // Directives are built separately in Build() and wired to controllers.
        var apps = new (string name, string label, string title, string body, string upgrade)[]
        {
            ("IconInternet", "Internet", "Internet - News",  "Today's news feed. (placeholder)", ""),
            ("IconLexicon",  "Lexicon",  "Lexicon",          "Wikipedia-style era glossary. (placeholder)", "ArchiveAccess"),
            ("IconDialect",  "Dialect",  "Dialect Filter",   "Highlights anachronistic phrases. (upgrade)", "DialectFilter"),
            ("IconMaterial", "Material", "Material Scanner", "Flags tech/materials beyond the claimed era. (upgrade)", "AdvancedScanner"),
            ("IconClueLog",  "Clue Log", "Case Notes",       "Clues & contradictions for the current case. (placeholder)", ""),
            ("IconNotes",    "Notes",    "Sticky Notes",     "Your notes. (placeholder)", ""),
        };

        foreach (var a in apps)
        {
            OSWindowChrome w = BuildOSWindow(windowLayer, a.name + "Window", a.title, a.body);
            BuildDesktopIcon(iconGrid, a.name, a.label, w, a.upgrade);
        }

        OSWindowChrome settings = BuildOSWindow(windowLayer, "SettingsWindow", "Settings", "Settings (empty for now).");

        // Rebuilt from scratch each run: the entries need fixed LayoutElement
        // heights or the vertical layout collapses them on top of each other.
        DestroyChildIfPresent(root, "StartMenu");
        Transform startMenu = Panel(root, "StartMenu", new Vector2(0f, 0f), new Vector2(0.14f, 0f), new Vector2(0f, 96f), new Vector2(0f, 112f), new Color(0.1f, 0.12f, 0.18f, 0.97f));
        AddVLayout(startMenu, 4f);
        Button settingsEntry = MakeButton(startMenu, "SettingsEntry", "Settings", Vector2.zero, Vector2.one, new Color(0.2f, 0.25f, 0.35f, 1f));
        SetLayoutHeight(settingsEntry, 46f);
        Button powerEntry = MakeButton(startMenu, "PowerEntry", "Power", Vector2.zero, Vector2.one, new Color(0.5f, 0.2f, 0.2f, 1f));
        SetLayoutHeight(powerEntry, 46f);
        startMenu.gameObject.SetActive(false);

        Button startBtn = null;
        Transform taskbarStart = root.Find("Taskbar/StartButton");
        if (taskbarStart != null)
        {
            startBtn = taskbarStart.GetComponent<Button>();
            if (startBtn == null)
                startBtn = taskbarStart.gameObject.AddComponent<Button>();
            Image img = taskbarStart.GetComponent<Image>();
            if (img != null)
                startBtn.targetGraphic = img;
        }

        DesktopShell shell = root.GetComponent<DesktopShell>();
        if (shell == null)
            shell = root.gameObject.AddComponent<DesktopShell>();
        var soShell = new SerializedObject(shell);
        SetRef(soShell, "startButton", startBtn);
        SetRef(soShell, "startMenu", startMenu.gameObject);
        SetRef(soShell, "settingsButton", settingsEntry);
        SetRef(soShell, "powerButton", powerEntry);
        SetRef(soShell, "settingsWindow", settings);
        soShell.ApplyModifiedProperties();
    }

    /// <summary>Builds a placeholder desktop window with a draggable title bar and min/max/close chrome.</summary>
    private static OSWindowChrome BuildOSWindow(Transform layer, string name, string title, string body, Vector2? size = null)
    {
        Transform win = Panel(layer, name, Center, Center, Vector2.zero, size ?? new Vector2(580f, 400f), Paper);

        Transform header = Panel(win, "Header", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -15f), new Vector2(0f, 30f), HeaderBar);
        DraggableWindow drag = header.GetComponent<DraggableWindow>();
        if (drag == null)
            drag = header.gameObject.AddComponent<DraggableWindow>();
        var soDrag = new SerializedObject(drag);
        SetRef(soDrag, "windowRoot", (RectTransform)win);
        soDrag.ApplyModifiedProperties();

        TMP_Text titleText = Text(header, "TitleText", title, 15, TextAlignmentOptions.Left, new Vector2(0.04f, 0f), new Vector2(0.7f, 1f), Color.white);
        titleText.fontStyle = FontStyles.Bold;

        Text(win, "Body", body, 20, TextAlignmentOptions.TopLeft, new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.82f), Ink);

        OSWindowChrome chrome = BuildWinControls(win, header);

        win.gameObject.SetActive(false); // opened by its icon
        return chrome;
    }

    /// <summary>Builds a desktop icon button bound to a window, with optional unlock-gating.</summary>
    private static void BuildDesktopIcon(Transform grid, string name, string label, OSWindowChrome window, string upgradeId)
    {
        Button btn = MakeButton(grid, name, label, Vector2.zero, Vector2.one, new Color(0.2f, 0.3f, 0.45f, 0.85f));
        LayoutElement le = btn.GetComponent<LayoutElement>();
        if (le == null)
            le = btn.gameObject.AddComponent<LayoutElement>();
        le.minHeight = 40f;
        le.preferredHeight = 40f;

        CanvasGroup cg = btn.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = btn.gameObject.AddComponent<CanvasGroup>();

        DesktopIcon icon = btn.GetComponent<DesktopIcon>();
        if (icon == null)
            icon = btn.gameObject.AddComponent<DesktopIcon>();
        var so = new SerializedObject(icon);
        SetRef(so, "targetWindow", window);
        SetRef(so, "button", btn);
        SetRef(so, "canvasGroup", cg);
        SerializedProperty up = so.FindProperty("requiredUpgradeId");
        if (up != null)
            up.stringValue = upgradeId ?? string.Empty;
        so.ApplyModifiedProperties();
    }

    private static Clickable EnsureClickable(GameObject go)
    {
        if (go.GetComponent<Collider2D>() == null)
            go.AddComponent<BoxCollider2D>();
        Clickable c = go.GetComponent<Clickable>();
        if (c == null)
            c = go.AddComponent<Clickable>();
        return c;
    }

    private static CinemachineCamera EnsureVcam(Transform parent, string name, Vector3 pos, float orthoSize)
    {
        Transform existing = parent.Find(name);
        GameObject go = existing != null ? existing.gameObject : new GameObject(name);
        if (existing == null) go.transform.SetParent(parent, false);
        go.transform.position = pos;
        var cam = go.GetComponent<CinemachineCamera>();
        if (cam == null)
            cam = go.AddComponent<CinemachineCamera>();
        cam.Lens.OrthographicSize = orthoSize;
        return cam;
    }

    private static void WireClickToFocusMonitor(Clickable clickable, OfficeViewController view)
        => WirePersistentVoid(clickable, "onClick", view, nameof(OfficeViewController.FocusMonitor));

    /// <summary>
    /// Wires a single persistent, parameterless (Void) call on a UnityEvent
    /// serialized property (e.g. Clickable "onClick" or Button "m_OnClick").
    /// </summary>
    private static void WirePersistentVoid(Object host, string eventProp, Object target, string method)
    {
        var so = new SerializedObject(host);
        SerializedProperty calls = so.FindProperty(eventProp + ".m_PersistentCalls.m_Calls");
        if (calls == null)
            return;

        calls.ClearArray();
        calls.InsertArrayElementAtIndex(0);
        SerializedProperty call = calls.GetArrayElementAtIndex(0);
        call.FindPropertyRelative("m_Target").objectReferenceValue = target;
        call.FindPropertyRelative("m_TargetAssemblyTypeName").stringValue = target.GetType().AssemblyQualifiedName;
        call.FindPropertyRelative("m_MethodName").stringValue = method;
        call.FindPropertyRelative("m_Mode").enumValueIndex = 1; // Void
        call.FindPropertyRelative("m_CallState").enumValueIndex = 2; // RuntimeOnly
        so.ApplyModifiedProperties();
    }

    /// <summary>Creates (or finds) a world-space TextMeshPro label under a parent.</summary>
    private static TextMeshPro WorldText(Transform parent, string name, string content, float fontSize, Color color, Vector3 localPos, Vector2 size, int sortingOrder)
    {
        Transform existing = parent.Find(name);
        GameObject go = existing != null ? existing.gameObject : new GameObject(name, typeof(TextMeshPro));
        if (existing == null)
            go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;

        TextMeshPro tmp = go.GetComponent<TextMeshPro>();
        if (tmp == null)
            tmp = go.AddComponent<TextMeshPro>();
        tmp.text = content;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;

        if (go.transform is RectTransform rt)
            rt.sizeDelta = size;

        MeshRenderer mr = go.GetComponent<MeshRenderer>();
        if (mr != null)
            mr.sortingOrder = sortingOrder;

        return tmp;
    }

    private static void DestroyChildIfPresent(Transform parent, string name)
    {
        Transform t = parent.Find(name);
        if (t != null)
            Object.DestroyImmediate(t.gameObject);
    }

    private static void EnsureFolderTree(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;
        string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        string leaf = System.IO.Path.GetFileName(path);
        if (!AssetDatabase.IsValidFolder(parent))
            EnsureFolderTree(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }

    private static T FindAssetByName<T>(string assetName) where T : Object
    {
        foreach (string guid in AssetDatabase.FindAssets($"{assetName} t:{typeof(T).Name}"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (System.IO.Path.GetFileNameWithoutExtension(path) == assetName)
                return AssetDatabase.LoadAssetAtPath<T>(path);
        }
        return null;
    }

    private static T FindFirstAsset<T>() where T : Object
    {
        foreach (string guid in AssetDatabase.FindAssets($"t:{typeof(T).Name}"))
            return AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
        return null;
    }
}
