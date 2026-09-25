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
/// - The PC desktop: a World Space canvas drawn live on the CRT's glass
///   (1440 x 1080 units at 4:3), with screen power and a focus push-in
///   [MonitorScreen, OfficeViewController, CinemachineCameraRig]; EventSystem
///   (new Input System)
/// - HUD (day/money/stability), citation slip, verdict line  [OfficeUIController]
/// - Morning briefing + shift report panels  [DayFlowUIController]
/// - Investigation desk: claim banner, directives, draggable/multi-page document
///   windows, a reference-book shelf with openable book windows, the interview
///   transcript, a visual compare bar, and Accept/Deny buttons, laid out for
///   the 4:3 desktop  [InvestigationUIController + CompareController]
/// - The traveller wheel (the interview's choices around the traveller), the
///   speech bubble and the desk tooltip on the office overlay canvas
///   [TravellerWheel, OverlayCallout]
/// - The physical desk: papers and the desk scanner, reacting props,
///   decoration slots, the traveller (shown while at the desk) and its hit
///   zone, and the booth's input rules  [DeskController, DeskReaction,
///   TravellerView, BoothCoordinator]
/// - GameManager + DaySystem (DayOrchestrator + DayEventDirector), auto-wired to
///   ContentLibrary_Main and a Day Plan
/// Safe to re-run: finds existing pieces by name and only fills gaps. Every UI
/// graphic it makes (and each existing one of those names) gets a ThemeTag
/// (piece 6: role, label key, style, fit), which CultureThemeService applies
/// at runtime; an untagged graphic on the two canvases is logged as an error.
/// It builds only Assets/Scenes/OfficeScene.unity and refuses any other active scene.
/// The booth and desk parts are in OfficeSceneUIBuilder.Desk.cs.
/// </summary>
public static partial class OfficeSceneUIBuilder
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

    /// <summary>Padding on every side of a vertical list (AddVLayout).</summary>
    private const int VLayoutPadding = 6;

    /// <summary>
    /// The reference resolution of the office overlay canvas's scaler, which
    /// never scales the canvas below it (ConfigureScaler); every overlay layout
    /// is authored against it, so what fits at this size fits on every screen.
    /// (The desktop is a World Space canvas of DesktopSize units with no scaler.)
    /// </summary>
    private static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);

    /// <summary>Transcript rows per page: the book row height (34 px) and spacing fit 8 in the window's row area.</summary>
    private const int TranscriptRowsPerPage = 8;

    /// <summary>
    /// The one scene this builder owns. Other scenes share its object names
    /// (OfficeScene_HybridArt keeps its own camera and 3D props), so the builder
    /// refuses to run anywhere else.
    /// </summary>
    private const string OfficeScenePath = "Assets/Scenes/OfficeScene.unity";

    /// <summary>Builds and wires the office in the open scene; refuses (with an error) unless that scene is <see cref="OfficeScenePath"/>.</summary>
    [MenuItem("Tools/TimeDesk/Build Office UI (HUD + Panels)")]
    public static void Build()
    {
        string activeScene = EditorSceneManager.GetActiveScene().path;
        if (activeScene != OfficeScenePath)
        {
            Debug.LogError($"[TimeDesk] Build Office UI only builds {OfficeScenePath}; the active scene is '{activeScene}'. Open {OfficeScenePath} and run it again. Nothing was changed.");
            return;
        }

        Canvas canvas = EnsureCanvas();
        Transform root = canvas.transform;
        EnsureEventSystem();
        ContentLibrarySO library = FindAssetByName<ContentLibrarySO>("ContentLibrary_Main") ?? FindFirstAsset<ContentLibrarySO>();

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

        // Leftovers of the pre-investigation era UI under the OfficeUIController's
        // object: a live monitor would show them. Its HUD and citation wiring stay
        // (Test_DayLoop runs the legacy fields from its own scene).
        foreach (string leftover in new[] { "VisitorText", "Doc1Text", "Doc2Text", "ResultText", "EraButtonsRoot" })
            DestroyChildIfPresent(officeUI.transform, leftover);

        // XP desktop wallpaper (behind everything), the idle line between
        // travellers + taskbar with system-tray HUD.
        GameObject idleScreen = BuildDesktop(root, library);
        BuildTaskbar(root, out TMP_Text dayText, out TMP_Text moneyText, out TMP_Text stabilityText, out TMP_Text trayClockText);

        // Verdict line (result text) on a strip that shows only while the line has text (piece 6 R18).
        Transform verdictStrip = Panel(root, "VerdictStrip", new Vector2(0.25f, 0.855f), new Vector2(0.75f, 0.925f), Vector2.zero, Vector2.zero, ScreenStripColor, ThemeRoleId.ScreenStrip);
        verdictStrip.GetComponent<Image>().raycastTarget = false;
        TMP_Text verdictText = Text(root, "VerdictText", "", 26, TextAlignmentOptions.Center, new Vector2(0.25f, 0.86f), new Vector2(0.75f, 0.92f), Color.white,
                                    ThemeRoleId.ScreenStrip, fit: true);
        if (verdictStrip.GetSiblingIndex() > verdictText.transform.GetSiblingIndex())
            verdictStrip.SetSiblingIndex(verdictText.transform.GetSiblingIndex()); // drawn behind the line
        verdictStrip.gameObject.SetActive(false);

        // Citation slip
        Transform citation = Panel(root, "CitationPanel", Center, Center, Vector2.zero, new Vector2(560f, 320f), new Color(0.85f, 0.2f, 0.15f, 0.96f), ThemeRoleId.Alert);
        TMP_Text citationText = Text(citation, "CitationText", UiText.Get("citation.title"), 24, TextAlignmentOptions.Center, new Vector2(0.05f, 0.28f), new Vector2(0.95f, 0.95f), Color.white,
                                     ThemeRoleId.Alert);
        Button citationContinue = MakeButton(citation, "ContinueButton", null, new Vector2(0.3f, 0.06f), new Vector2(0.7f, 0.24f), null, ThemeRoleId.Button, "citation.acknowledge");
        citation.gameObject.SetActive(false);

        // Briefing + Results: newsletter panels on the office overlay canvas, so
        // they read over the booth view, not on the small live desktop.
        Canvas officeCanvas = EnsureOfficeOverlayCanvas();
        DestroyChildIfPresent(root, "BriefingPanel");
        DestroyChildIfPresent(root, "ResultsPanel");
        DestroyChildIfPresent(officeCanvas.transform, "BriefingPanel");
        DestroyChildIfPresent(officeCanvas.transform, "ResultsPanel");

        Transform briefing = BuildNewsletter(officeCanvas.transform, "BriefingPanel", "briefing.masthead",
            "briefing.start", out TMP_Text briefingTitle, out TMP_Text briefingBody, out Button startShift);
        Transform results = BuildNewsletter(officeCanvas.transform, "ResultsPanel", "results.masthead",
            "results.goHome", out TMP_Text resultsTitle, out TMP_Text resultsBody, out Button goHome);

        // The desk tuning (created once): the monitor push-in, screen power, sorting bands, the wheel.
        DeskConfigSO deskConfig = EnsureDeskConfig();

        // Booth-view overlays, rebuilt each run with always-active hosts: the
        // traveller's speech bubble, the desk props' tooltip, and the traveller
        // wheel (the interview's choices; its ring replaces the retired intercom).
        OverlayCallout speechBubble = BuildOverlayCallout(officeCanvas.transform, "SpeechBubble", new Vector2(420f, 110f), new Color(0.98f, 0.97f, 0.93f, 0.97f), ThemeRoleId.DiegeticBubble);
        OverlayCallout deskTooltip = BuildOverlayCallout(officeCanvas.transform, "DeskTooltip", new Vector2(360f, 60f), Tooltip, ThemeRoleId.Tooltip);
        TravellerWheel wheel = BuildTravellerWheel(officeCanvas.transform, deskConfig, speechBubble);
        InteractionPanelController interaction = wheel.transform.Find("Catcher/Ring").GetComponent<InteractionPanelController>();

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

        Transform investRoot = Panel(investHost, "InvestigationRoot", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, DeskDimColor, ThemeRoleId.DeskDim);
        Transform windowLayer = Panel(investRoot, "WindowLayer", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);

        // Claim on a translucent XP-blue strip.
        Panel(investRoot, "ClaimStrip", new Vector2(0.06f, 0.87f), new Vector2(0.94f, 1f), Vector2.zero, Vector2.zero, ScreenStripColor, ThemeRoleId.ClaimStrip);
        TMP_Text claimText = Text(investRoot, "ClaimBanner", "Visitor", 26, TextAlignmentOptions.Center, new Vector2(0.1f, 0.88f), new Vector2(0.9f, 0.99f), Color.white, ThemeRoleId.ClaimStrip);

        // Desktop icon grid pinned to the top-left of the screen (like a real
        // OS). Investigation documents + reference books + the desktop apps all
        // register their launch tiles here, filling top-left downward.
        Transform bookShelf = Panel(investRoot, "BookShelf", new Vector2(0.005f, 0.06f), new Vector2(0.17f, 0.855f), Vector2.zero, Vector2.zero, null);
        AddGridLayout(bookShelf, new Vector2(82f, 60f), new Vector2(6f, 6f));
        Button shelfButtonTemplate = MakeButton(bookShelf, "BookShelfButtonTemplate", "Book", Vector2.zero, Vector2.one, null, ThemeRoleId.Button);
        FitIconLabel(shelfButtonTemplate);
        shelfButtonTemplate.gameObject.SetActive(false);

        // Directives live in a sticky-note window (closed by default) opened
        // from a desktop icon, instead of a note that is always open.
        DestroyChildIfPresent(investRoot, "DirectivesNote");
        DestroyChildIfPresent(investRoot, "Directives"); // stray text from older builds
        OSWindowChrome directivesWindow = BuildOSWindow(windowLayer, "DirectivesWindow", "window.directives", null,
            UiText.Get("directives.none"), new Vector2(430f, 360f), ThemeRoleId.StickyNote);
        Image directivesImg = directivesWindow.GetComponent<Image>();
        if (directivesImg != null)
            directivesImg.color = new Color(1f, 0.96f, 0.6f, 0.97f);
        TMP_Text directivesText = directivesWindow.transform.Find("Body").GetComponent<TMP_Text>();
        BuildDesktopIcon(bookShelf, "IconDirectives", "icon.directives", directivesWindow, "");

        // The Deviation Report: its body lists the discrepancies the player has
        // documented for the current case (drives deny gating). Its object names
        // keep "Scanner"; the desk device is the scanner.
        DestroyChildIfPresent(windowLayer, "IconScannerWindow");
        OSWindowChrome scannerWindow = BuildOSWindow(windowLayer, "IconScannerWindow", "window.scanner", null,
            UiText.Get("scanner.idle"), new Vector2(560f, 420f));
        TMP_Text scannerText = scannerWindow.transform.Find("Body").GetComponent<TMP_Text>();
        BuildDesktopIcon(bookShelf, "IconScanner", "icon.scanner", scannerWindow, "");

        // The intercom panel is retired: the traveller wheel's ring shows the interview.
        DestroyChildIfPresent(investRoot, "IntercomPanel");

        // Citizen Records app: the agency's master record of every (fake)
        // human. Registry content is injected per day by GameManager.
        DestroyChildIfPresent(windowLayer, "RecordsWindow");
        OSWindowChrome recordsChrome = BuildOSWindow(windowLayer, "RecordsWindow", "records.title", null,
            UiText.Get("records.idle"), new Vector2(520f, 430f));
        Transform recWin = recordsChrome.transform;
        TMP_Text recStatus = recWin.Find("Body").GetComponent<TMP_Text>();
        var recStatusRt = (RectTransform)recStatus.transform;
        recStatusRt.anchorMin = new Vector2(0.05f, 0.6f);
        recStatusRt.anchorMax = new Vector2(0.95f, 0.7f);
        TMP_InputField recSearchInput = BuildInputField(recWin, "SearchInput", "records.placeholder", new Vector2(0.05f, 0.74f), new Vector2(0.68f, 0.86f));
        Button recSearchButton = MakeButton(recWin, "SearchButton", null, new Vector2(0.71f, 0.74f), new Vector2(0.95f, 0.86f), new Color(0.15f, 0.3f, 0.5f, 1f),
                                            ThemeRoleId.SearchButton, "records.search");
        (GameObject recNameRow, TMP_Text recNameValue) = BuildRecordRow(recWin, "NameRow", "Name", new Vector2(0.05f, 0.46f), new Vector2(0.95f, 0.56f));
        (GameObject recBornRow, TMP_Text recBornValue) = BuildRecordRow(recWin, "BornRow", "Born", new Vector2(0.05f, 0.34f), new Vector2(0.95f, 0.44f));
        TMP_Text recOrigin = Text(recWin, "OriginText", "", 17, TextAlignmentOptions.Left, new Vector2(0.06f, 0.24f), new Vector2(0.95f, 0.32f), Ink, ThemeRoleId.DiegeticRow);
        TMP_Text recNote = Text(recWin, "NoteText", "", 15, TextAlignmentOptions.TopLeft, new Vector2(0.06f, 0.05f), new Vector2(0.95f, 0.22f), new Color(0.35f, 0.3f, 0.2f, 1f),
                                ThemeRoleId.DiegeticNote, style: FontStyles.Italic);
        CitizenRecordsWindowController records = recWin.GetComponent<CitizenRecordsWindowController>();
        if (records == null)
            records = recWin.gameObject.AddComponent<CitizenRecordsWindowController>();
        var soRecords = new SerializedObject(records);
        SetRef(soRecords, "searchInput", recSearchInput);
        SetRef(soRecords, "searchButton", recSearchButton);
        SetRef(soRecords, "statusText", recStatus);
        SetRef(soRecords, "nameRow", recNameRow);
        SetRef(soRecords, "nameValueText", recNameValue);
        SetRef(soRecords, "bornRow", recBornRow);
        SetRef(soRecords, "bornValueText", recBornValue);
        SetRef(soRecords, "originText", recOrigin);
        SetRef(soRecords, "noteText", recNote);
        SetRef(soRecords, "compareController", compare);
        soRecords.ApplyModifiedProperties();
        BuildDesktopIcon(bookShelf, "IconRecords", "icon.records", recordsChrome, "");

        // Case Notes: Interview — the current traveller's transcript, in the
        // retired Clue Log placeholder's slot. Rebuilt fresh each run (like
        // Records), so its row template always has the transcript layout and
        // the window layer keeps a stable order. Opened by every interview
        // choice but a document request; answer rows are compare-clickable.
        DestroyChildIfPresent(windowLayer, "IconClueLogWindow");
        DestroyChildIfPresent(windowLayer, "TranscriptWindow");
        Transform transcriptWin = Panel(windowLayer, "TranscriptWindow", Center, Center, new Vector2(395f, 60f), new Vector2(620f, 460f), Paper, ThemeRoleId.WindowBody);
        WindowShell transcriptShell = BuildWindowShell(transcriptWin, "window.transcript", null, ThemeRoleId.WindowBody, ThemeRoleId.DiegeticRow, false);
        TranscriptWindowController transcript = transcriptWin.gameObject.AddComponent<TranscriptWindowController>();
        var soTranscript = new SerializedObject(transcript);
        SetRef(soTranscript, "titleText", transcriptShell.title);
        SetRef(soTranscript, "pageText", transcriptShell.page);
        SetRef(soTranscript, "prevButton", transcriptShell.prev);
        SetRef(soTranscript, "nextButton", transcriptShell.next);
        SetRef(soTranscript, "entryRowsRoot", transcriptShell.rowsRoot);
        SetRef(soTranscript, "entryRowTemplate", transcriptShell.rowTemplate);
        soTranscript.FindProperty("entriesPerPage").intValue = TranscriptRowsPerPage;
        soTranscript.ApplyModifiedProperties();
        ApplyTranscriptRowLayout(transcriptShell.rowTemplate);
        OSWindowChrome transcriptChrome = transcriptWin.GetComponent<OSWindowChrome>();
        transcriptWin.gameObject.SetActive(false);
        BuildDesktopIcon(bookShelf, "IconClueLog", "icon.clueLog", transcriptChrome, "");

        // Compare bar (XP tooltip-yellow, above the shelf). Auto-sizing keeps
        // long verdict lines inside the bar.
        Transform compareBar = Panel(investRoot, "CompareBar", new Vector2(0.1f, 0.27f), new Vector2(0.9f, 0.34f), Vector2.zero, Vector2.zero, Tooltip, ThemeRoleId.CompareBar);
        TMP_Text compareText = Text(compareBar, "CompareText", "", 22, TextAlignmentOptions.Center, new Vector2(0.02f, 0f), new Vector2(0.98f, 1f), Ink, ThemeRoleId.CompareBar);
        compareText.enableAutoSizing = true;
        compareText.fontSizeMin = 11f;
        compareText.fontSizeMax = 22f;
        compareBar.gameObject.SetActive(false);

        // Accept / Deny (above the taskbar), each with a fixed glyph so a culture's colours never carry the meaning alone (piece 6 R8)
        Button acceptButton = MakeButton(investRoot, "AcceptButton", null, new Vector2(0.3f, 0.06f), new Vector2(0.49f, 0.15f), new Color(0.2f, 0.5f, 0.24f, 1f),
                                         ThemeRoleId.AcceptButton, "accept");
        Button denyButton = MakeButton(investRoot, "DenyButton", null, new Vector2(0.51f, 0.06f), new Vector2(0.7f, 0.15f), new Color(0.72f, 0.2f, 0.18f, 1f),
                                       ThemeRoleId.DenyButton, "deny");
        BuildDecisionGlyph(acceptButton, ThemeRoleId.AcceptButton, true);
        BuildDecisionGlyph(denyButton, ThemeRoleId.DenyButton, false);

        // Window templates (disabled, cloned at runtime)
        DocumentWindowController docTemplate = BuildDocumentWindow(windowLayer);
        ReferenceBookWindowController bookTemplate = BuildBookWindow(windowLayer);

        investRoot.gameObject.SetActive(false);

        // --- Content + logic objects ---
        DayPlanSO dayPlan = null;
        if (library != null)
        {
            dayPlan = library.GetDayPlan(1);
            if (dayPlan == null && library.DayPlans.Count > 0) dayPlan = library.DayPlans[0];
        }
        if (dayPlan == null) dayPlan = FindFirstAsset<DayPlanSO>();
        if (library == null) Debug.LogWarning("[TimeDesk] No ContentLibrarySO found — assign GameManager.contentLibrary manually.");
        if (library != null && library.Interview != null)
        {
            int wheelFit = RadialLayout.MaxFit(deskConfig.wheelRadii.x, deskConfig.wheelRadii.y, deskConfig.wheelItemSize.x, deskConfig.wheelItemSize.y,
                                               deskConfig.wheelCentreSize.x, deskConfig.wheelCentreSize.y, deskConfig.wheelItemGap, library.Interview.menuCapacity);
            if (wheelFit < library.Interview.menuCapacity)
                Debug.LogError($"[TimeDesk] The traveller wheel fits {wheelFit} choices, but the content library's interview menu capacity is {library.Interview.menuCapacity}; lower interview.menuCapacity in world_source.json or enlarge the wheel (Desk_Default: wheelRadii, wheelItemSize).");
        }
        if (dayPlan == null) Debug.LogWarning("[TimeDesk] No DayPlanSO found — generate content first (Tools > TimeDesk).");

        DayOrchestrator orchestrator = Object.FindFirstObjectByType<DayOrchestrator>();
        if (orchestrator == null) orchestrator = new GameObject("DaySystem").AddComponent<DayOrchestrator>();
        DayEventDirector eventDirector = orchestrator.GetComponent<DayEventDirector>() ?? orchestrator.gameObject.AddComponent<DayEventDirector>();

        GameManager gameManager = Object.FindFirstObjectByType<GameManager>();
        if (gameManager == null) gameManager = new GameObject("GameManager").AddComponent<GameManager>();

        // Booth + cameras + view controller. The desktop canvas is drawn live on
        // the CRT's glass (BuildMonitorScreen), always active; its input is gated.
        OfficeViewController officeView = BuildBooth(canvas, deskConfig, out MonitorScreen monitorScreen);

        // Fake-OS desktop shell: NEW apps only (existing document/reference/compare
        // windows are launched by the investigation icon grid), plus a Start menu
        // and the taskbar's way back to the desk.
        BuildDesktopShell(canvas, bookShelf, windowLayer, library, officeView, monitorScreen);

        // Cursor + hover highlight settings (a persistent highlighter uses them in every scene).
        BuildInteractionFeedback();

        // Shift clock: driver beside the GameManager, tray + wall-clock readouts.
        ShiftClockDriver shiftClock = gameManager.GetComponent<ShiftClockDriver>();
        if (shiftClock == null)
            shiftClock = gameManager.gameObject.AddComponent<ShiftClockDriver>();
        BuildShiftClockReadouts(shiftClock, trayClockText);

        // The booth's clicks, once every prop exists (the wall clock above):
        // reactions, hit zones, the wheel's openers and notes, the coordinator.
        BoothCoordinator booth = BuildDeskInteraction(officeView, monitorScreen, deskConfig, wheel, deskTooltip, trayClockText, library);

        // --- Wire everything ---
        var soOffice = new SerializedObject(officeUI);
        SetRef(soOffice, "moneyText", moneyText);
        SetRef(soOffice, "stabilityText", stabilityText);
        SetRef(soOffice, "dayText", dayText);
        SetRef(soOffice, "resultText", verdictText);
        SetRef(soOffice, "resultBackdrop", verdictStrip.gameObject);
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
        SetRef(soInvest, "interactionPanel", interaction);
        SetRef(soInvest, "recordsWindow", records);
        SetRef(soInvest, "transcriptWindow", transcript);
        SetRef(soInvest, "transcriptChrome", transcriptChrome);
        SetRef(soInvest, "desk", officeView.transform.Find("DeskSurface").GetComponent<DeskController>());
        SetRef(soInvest, "wheel", wheel);
        SetRef(soInvest, "idleScreen", idleScreen);
        // The 4:3 desktop: documents cascade on the left, clear of the icon
        // column; books open in two staggered rows (the fields' defaults are
        // the 16:9 layout a scene that is not rebuilt keeps).
        soInvest.FindProperty("documentWindowOrigin").vector2Value = new Vector2(-195f, 150f);
        soInvest.FindProperty("documentWindowStep").vector2Value = new Vector2(40f, -40f);
        soInvest.FindProperty("bookWindowOrigin").vector2Value = new Vector2(-180f, -150f);
        soInvest.FindProperty("bookWindowColumnStep").floatValue = 300f;
        soInvest.FindProperty("bookWindowRowStep").vector2Value = new Vector2(40f, 40f);
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
        SetRef(soGm, "shiftClock", shiftClock);
        SetRef(soGm, "travellerView", officeView.transform.Find("Traveller").GetComponent<TravellerView>());
        SetRef(soGm, "booth", booth);
        soGm.ApplyModifiedProperties();

        CheckThemeTags(canvas, officeCanvas);
        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        Debug.Log("[TimeDesk] Office investigation desk built and wired (every UI graphic theme-tagged, live monitor on the CRT with screen power, the desk with papers, scanner and reacting props, traveller + wheel + speech bubble, booth input rules, HUD, citation, briefing/results, claim, document + book windows, interview transcript, compare, Accept/Deny, GameManager, DaySystem). Save the scene.");
    }

    // -----------------------------
    // Window builders
    // -----------------------------

    private static DocumentWindowController BuildDocumentWindow(Transform layer)
    {
        // Rebuilt fresh each run: visitor papers read as SCANNED documents —
        // a white page with a photo corner on a dark scanner backing — so they
        // never look like just another OS window.
        DestroyChildIfPresent(layer, "DocumentWindowTemplate");
        Transform win = Panel(layer, "DocumentWindowTemplate", Center, Center, Vector2.zero, new Vector2(540f, 440f), new Color(0.13f, 0.14f, 0.17f, 1f), ThemeRoleId.DiegeticBacking);
        WindowShell s = BuildWindowShell(win, null, UiText.Get("document.untitled"), ThemeRoleId.DiegeticBacking, ThemeRoleId.DiegeticRow, false);

        Transform page = Panel(win, "ScanPage", new Vector2(0.025f, 0.115f), new Vector2(0.975f, 0.85f), Vector2.zero, Vector2.zero, new Color(0.97f, 0.96f, 0.92f, 1f), ThemeRoleId.DiegeticPaper);
        page.SetSiblingIndex(1); // render after the header, behind the rows
        Transform photo = Panel(page, "PhotoBox", new Vector2(0.76f, 0.66f), new Vector2(0.96f, 0.96f), Vector2.zero, Vector2.zero, new Color(0.55f, 0.56f, 0.58f, 1f), ThemeRoleId.DiegeticPhoto);
        Text(photo, "Label", "PHOTO", 13, TextAlignmentOptions.Center, Vector2.zero, Vector2.one, new Color(0.25f, 0.26f, 0.28f, 1f), ThemeRoleId.DiegeticPhoto);

        // Footer page label needs light ink on the dark backing.
        s.page.color = new Color(0.85f, 0.86f, 0.88f, 1f);

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
        Transform win = Panel(layer, "BookWindowTemplate", Center, Center, Vector2.zero, new Vector2(540f, 440f), Paper, ThemeRoleId.WindowBody);
        WindowShell s = BuildWindowShell(win, null, UiText.Get("book.untitled"), ThemeRoleId.WindowBody, ThemeRoleId.DiegeticBookRow, true);
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

    /// <summary>
    /// A paged window's shell: title bar (keyed title, or a sample the
    /// controller rewrites), controls, the row list with its template and the
    /// footer; <paramref name="frameRole"/> colours the footer text, <paramref name="rowRole"/>
    /// the rows (whose label shrinks to fit when <paramref name="rowLabelFits"/>).
    /// </summary>
    private static WindowShell BuildWindowShell(Transform win, string titleKey, string titleSample, ThemeRoleId frameRole, ThemeRoleId rowRole, bool rowLabelFits)
    {
        // XP title bar (drag handle) with gloss highlight + window controls.
        Transform header = Panel(win, "Header", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -15f), new Vector2(0f, 30f), HeaderBar, ThemeRoleId.TitleBar);
        Panel(header, "Gloss", new Vector2(0f, 0.5f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, new Color(1f, 1f, 1f, 0.14f), ThemeRoleId.TitleGloss);
        DraggableWindow drag = header.GetComponent<DraggableWindow>() ?? header.gameObject.AddComponent<DraggableWindow>();
        var soDrag = new SerializedObject(drag);
        SetRef(soDrag, "windowRoot", (RectTransform)win);
        soDrag.ApplyModifiedProperties();
        TMP_Text title = Text(header, "TitleText", titleSample, 15, TextAlignmentOptions.Left, new Vector2(0.04f, 0f), new Vector2(0.76f, 1f), Color.white,
                              ThemeRoleId.TitleBar, titleKey, FontStyles.Bold, ThemeTextKind.Heading, titleKey != null);
        BuildWinControls(win, header);

        // Rows container (scroll-free vertical list)
        Transform rowsRoot = Panel(win, "Rows", new Vector2(0.04f, 0.12f), new Vector2(0.96f, 0.84f), Vector2.zero, Vector2.zero, null);
        AddVLayout(rowsRoot, 4f);
        if (rowsRoot.GetComponent<RectMask2D>() == null)
            rowsRoot.gameObject.AddComponent<RectMask2D>(); // clip any overflow inside the window

        // Row template
        GameObject rowTemplate = BuildRowTemplate(rowsRoot, rowRole, rowLabelFits);

        // Footer page controls
        Button prev = MakeButton(win, "PrevButton", null, new Vector2(0.04f, 0.02f), new Vector2(0.18f, 0.1f), null, ThemeRoleId.Button, "window.prev");
        TMP_Text page = Text(win, "PageText", UiText.Format("window.page", 1, 1), 18, TextAlignmentOptions.Center, new Vector2(0.2f, 0.02f), new Vector2(0.8f, 0.1f), Ink, frameRole);
        Button next = MakeButton(win, "NextButton", null, new Vector2(0.82f, 0.02f), new Vector2(0.96f, 0.1f), null, ThemeRoleId.Button, "window.next");

        return new WindowShell { title = title, page = page, prev = prev, next = next, rowsRoot = rowsRoot, rowTemplate = rowTemplate };
    }

    /// <summary>
    /// The transcript's row layout, re-applied on every build: the speaker in a
    /// fixed 150 px column that ellipsizes, the sentence in the rest, wrapping
    /// onto a second line and auto-sizing 12-18 pt inside the fixed 34 px row
    /// (its width never follows its text).
    /// </summary>
    private static void ApplyTranscriptRowLayout(GameObject row)
    {
        HorizontalLayoutGroup h = row.GetComponent<HorizontalLayoutGroup>();
        if (h != null)
            h.childForceExpandWidth = false;

        ConfigureTranscriptText(row.transform.Find("Label"), 150f, 0f, TextWrappingModes.NoWrap, TextOverflowModes.Ellipsis);
        ConfigureTranscriptText(row.transform.Find("Value"), 0f, 1f, TextWrappingModes.Normal, TextOverflowModes.Overflow);
    }

    /// <summary>Sizes one transcript row text: layout width, auto-size range, wrapping and overflow.</summary>
    private static void ConfigureTranscriptText(Transform t, float width, float flexibleWidth, TextWrappingModes wrapping, TextOverflowModes overflow)
    {
        if (t == null)
            return;

        LayoutElement le = t.GetComponent<LayoutElement>();
        if (le == null)
            le = t.gameObject.AddComponent<LayoutElement>();
        le.minWidth = width;
        le.preferredWidth = width;
        le.flexibleWidth = flexibleWidth;

        TMP_Text text = t.GetComponent<TMP_Text>();
        if (text == null)
            return;
        text.enableAutoSizing = true;
        text.fontSizeMin = 12f;
        text.fontSizeMax = 18f;
        text.textWrappingMode = wrapping;
        text.overflowMode = overflow;
    }

    /// <summary>A list's row template (existing-wins: an existing one only gets its theme tags).</summary>
    private static GameObject BuildRowTemplate(Transform parent, ThemeRoleId rowRole, bool labelFits)
    {
        Transform existing = parent.Find("RowTemplate");
        if (existing != null)
        {
            Tag(existing.GetComponent<Image>(), rowRole, ThemePart.Fill);
            Text(existing, "Label", "Label", 18, TextAlignmentOptions.Left, Vector2.zero, Vector2.one, Ink, rowRole, fit: labelFits);
            Text(existing, "Value", "Value", 18, TextAlignmentOptions.Left, Vector2.zero, Vector2.one, Ink, rowRole);
            return existing.gameObject;
        }

        var go = new GameObject("RowTemplate", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        img.color = RowBg;
        Tag(img, rowRole, ThemePart.Fill);
        go.AddComponent<Button>().targetGraphic = img;
        var le = go.AddComponent<LayoutElement>();
        le.minHeight = 34f;
        le.preferredHeight = 34f;
        AddHLayout(go.transform, 8f);

        Text(go.transform, "Label", "Label", 18, TextAlignmentOptions.Left, Vector2.zero, Vector2.one, Ink, rowRole, fit: labelFits);
        Text(go.transform, "Value", "Value", 18, TextAlignmentOptions.Left, Vector2.zero, Vector2.one, Ink, rowRole);

        go.SetActive(false);
        return go;
    }

    // -----------------------------
    // Primitive helpers
    // -----------------------------

    private static readonly Vector2 Center = new Vector2(0.5f, 0.5f);

    /// <summary>The desktop canvas's object name (the office overlay canvas is another Canvas).</summary>
    private const string DesktopCanvasName = "Canvas";

    /// <summary>
    /// The desktop canvas, found by its name: a World Space canvas of
    /// <see cref="DesktopSize"/> units (BuildMonitorScreen puts it on the CRT's
    /// glass), masked to its rect (dragged windows never draw over the bezel).
    /// A canvas scaler only serves a screen-space canvas, so it has none.
    /// </summary>
    private static Canvas EnsureCanvas()
    {
        Canvas canvas = null;
        foreach (Canvas c in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (c.gameObject.name == DesktopCanvasName)
            {
                canvas = c;
                break;
            }
        }
        if (canvas == null)
        {
            var go = new GameObject(DesktopCanvasName, typeof(RectTransform));
            canvas = go.AddComponent<Canvas>();
            go.AddComponent<GraphicRaycaster>();
        }
        canvas.renderMode = RenderMode.WorldSpace;
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler != null)
            Object.DestroyImmediate(scaler);
        if (canvas.GetComponent<GraphicRaycaster>() == null) canvas.gameObject.AddComponent<GraphicRaycaster>();
        if (canvas.GetComponent<RectMask2D>() == null) canvas.gameObject.AddComponent<RectMask2D>();

        var rt = (RectTransform)canvas.transform;
        rt.anchorMin = Center;
        rt.anchorMax = Center;
        rt.pivot = Center;
        rt.sizeDelta = DesktopSize;
        return canvas;
    }

    /// <summary>
    /// Scales the office overlay canvas (the newsletters, and the booth's
    /// overlay UI) with the screen from the reference resolution, and never
    /// below it: Expand keeps the canvas at least 1920x1080 in both dimensions
    /// (a screen wider than 16:9 gets more width, a narrower one more height),
    /// so a layout that fits at the reference size fits on every screen.
    /// </summary>
    private static void ConfigureScaler(CanvasScaler scaler)
    {
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = ReferenceResolution;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
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
        l.padding = new RectOffset(VLayoutPadding, VLayoutPadding, VLayoutPadding, VLayoutPadding);
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

    /// <summary>A rect (geometry re-applied every build) with an optional background image; with a role, its image gets that theme tag.</summary>
    private static Transform Panel(Transform parent, string name, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size, Color? bg, ThemeRoleId? role = null)
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
        if (role.HasValue && go.TryGetComponent(out Image themed))
            Tag(themed, role.Value, ThemePart.Fill);
        return go.transform;
    }

    /// <summary>
    /// Stamps (or re-stamps) a graphic's theme tag (piece 6): its role and part,
    /// and for a text its label key, built style, kind and shrink-to-fit.
    /// </summary>
    private static void Tag(Component graphic, ThemeRoleId role, ThemePart part, string labelKey = null, FontStyles style = FontStyles.Normal,
                            ThemeTextKind kind = ThemeTextKind.Body, bool fit = false)
    {
        if (graphic == null)
            return;
        ThemeTag tag = graphic.GetComponent<ThemeTag>();
        if (tag == null)
            tag = graphic.gameObject.AddComponent<ThemeTag>();
        tag.Configure(role, part, labelKey, style, kind, fit);
    }

    /// <summary>
    /// Logs an error for every graphic on the desktop and overlay canvases that
    /// carries no theme tag, so theming never silently misses one (TMP
    /// sub-meshes and the hidden legacy HUD excepted).
    /// </summary>
    private static void CheckThemeTags(params Canvas[] canvases)
    {
        foreach (Canvas canvas in canvases)
        {
            Transform legacyHud = canvas.transform.Find("HUD");
            foreach (Graphic g in canvas.GetComponentsInChildren<Graphic>(true))
            {
                if (g is TMP_SubMeshUI || g.GetComponent<ThemeTag>() != null || (legacyHud != null && g.transform.IsChildOf(legacyHud)))
                    continue;
                Debug.LogError($"[TimeDesk] '{PathOf(g.transform)}' has no ThemeTag; give it a role in OfficeSceneUIBuilder (piece 6).", g);
            }
        }
    }

    /// <summary>A transform's scene path.</summary>
    private static string PathOf(Transform t) => t.parent == null ? t.name : PathOf(t.parent) + "/" + t.name;

    /// <summary>The translucent strip behind texts on the wallpaper (verdict and idle lines) and the claim strip.</summary>
    private static readonly Color ScreenStripColor = new Color(0.06f, 0.18f, 0.42f, 0.8f);

    /// <summary>The dim behind the investigation desk.</summary>
    private static readonly Color DeskDimColor = new Color(0.06f, 0.07f, 0.1f, 0.55f);

    /// <summary>
    /// A text, created once (existing-wins); with a role it gets that theme
    /// tag. A new keyed text is baked with its English label; <paramref name="style"/>
    /// is its built style (new texts get it; the tag records it).
    /// </summary>
    private static TMP_Text Text(Transform parent, string name, string content, int size, TextAlignmentOptions align, Vector2 aMin, Vector2 aMax, Color color,
                                 ThemeRoleId? role = null, string labelKey = null, FontStyles style = FontStyles.Normal, ThemeTextKind kind = ThemeTextKind.Body, bool fit = false)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
        {
            var et = existing.GetComponent<TMP_Text>();
            if (et != null)
            {
                if (role.HasValue)
                    Tag(et, role.Value, ThemePart.Ink, labelKey, style, kind, fit);
                return et;
            }
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
        t.text = labelKey != null ? UiText.Get(labelKey) : content;
        t.fontSize = size;
        t.alignment = align;
        t.color = color;
        t.fontStyle = style;
        if (role.HasValue)
            Tag(t, role.Value, ThemePart.Ink, labelKey, style, kind, fit);
        return t;
    }

    /// <summary>
    /// A button (created once, existing-wins) with its label; with a role, its
    /// image and label get that theme tag, and a keyed label shrinks to fit
    /// (the label is baked in English from <paramref name="labelKey"/>, else <paramref name="label"/>).
    /// </summary>
    private static Button MakeButton(Transform parent, string name, string label, Vector2 aMin, Vector2 aMax, Color? color = null,
                                     ThemeRoleId? role = null, string labelKey = null)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
        {
            var eb = existing.GetComponent<Button>();
            if (eb != null)
            {
                if (role.HasValue)
                {
                    Tag(eb.GetComponent<Image>(), role.Value, ThemePart.Fill);
                    Transform existingLabel = eb.transform.Find("Label");
                    if (existingLabel != null)
                        Tag(existingLabel.GetComponent<TMP_Text>(), role.Value, ThemePart.Ink, labelKey, FontStyles.Normal, ThemeTextKind.Button, labelKey != null);
                }
                return eb;
            }
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
        if (role.HasValue)
            Tag(img, role.Value, ThemePart.Fill);
        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        Color labelColor = color.HasValue ? Color.white : Color.black;
        Text(go.transform, "Label", label, 22, TextAlignmentOptions.Center, Vector2.zero, Vector2.one, labelColor,
             role, labelKey, FontStyles.Normal, ThemeTextKind.Button, labelKey != null);
        return btn;
    }

    /// <summary>
    /// A decision button's glyph (piece 6 R8): a 32-unit container at the
    /// label's left holding two plain bars (no sprite, no raycast) in the
    /// button's ink, a tick for Accept and a cross for Deny; the label starts
    /// after it. Positions and glyphs never change with the culture.
    /// </summary>
    private static void BuildDecisionGlyph(Button button, ThemeRoleId role, bool tick)
    {
        Transform glyph = Panel(button.transform, "Glyph", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(30f, 0f), new Vector2(32f, 32f), null);
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

        Transform label = button.transform.Find("Label");
        if (label is RectTransform labelRt)
            labelRt.offsetMin = new Vector2(56f, labelRt.offsetMin.y);
    }

    /// <summary>One glyph bar: a plain rotated rect in the role's ink.</summary>
    private static void GlyphBar(Transform glyph, string name, Vector2 centre, Vector2 size, float angle, ThemeRoleId role)
    {
        Transform bar = Panel(glyph, name, Center, Center, centre, size, Color.white);
        bar.localRotation = Quaternion.Euler(0f, 0f, angle);
        Image image = bar.GetComponent<Image>();
        image.raycastTarget = false;
        Tag(image, role, ThemePart.Ink);
    }

    // ----------------------------- Windows XP theme -----------------------------

    /// <summary>
    /// The wallpaper (behind everything; it envelopes the 4:3 desktop at its own
    /// aspect, the overflow clipped by the canvas's mask; the neutral theme's,
    /// which Generate World ensures) and, right above it, the idle line shown
    /// between travellers (inactive; the investigation controller shows it).
    /// Returns the idle line's object.
    /// </summary>
    private static GameObject BuildDesktop(Transform root, ContentLibrarySO library)
    {
        Transform desk = Panel(root, "Desktop", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.23f, 0.45f, 0.74f, 1f), ThemeRoleId.Desktop);
        Image img = desk.GetComponent<Image>();
        Sprite wall = library != null && library.NeutralTheme != null ? library.NeutralTheme.wallpaper : null;
        if (wall == null)
            Debug.LogWarning("[TimeDesk] The content library has no neutral theme wallpaper, so the desktop shows its plain colour. Run Tools > TimeDesk > Generate World, then build again.");
        if (img != null && wall != null)
        {
            img.sprite = wall;
            img.type = Image.Type.Simple;
            img.preserveAspect = false;
            img.color = Color.white; // don't tint the wallpaper with the fallback color
        }
        AspectRatioFitter fitter = desk.GetComponent<AspectRatioFitter>();
        if (fitter == null)
            fitter = desk.gameObject.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fitter.aspectRatio = wall != null ? wall.rect.width / wall.rect.height : ReferenceResolution.x / ReferenceResolution.y;
        desk.SetAsFirstSibling();

        // Between travellers the live monitor reads this, large enough for the booth view, on
        // a strip so it never reads off the wallpaper (piece 6 R18; the idle line of older
        // builds moves onto the strip).
        Transform strip = Panel(root, "IdleScreen", new Vector2(0.05f, 0.38f), new Vector2(0.95f, 0.62f), Vector2.zero, Vector2.zero, ScreenStripColor, ThemeRoleId.ScreenStrip);
        strip.GetComponent<Image>().raycastTarget = false;
        Transform oldIdle = root.Find("IdleText");
        if (oldIdle != null)
            oldIdle.SetParent(strip, false);
        TMP_Text idle = Text(strip, "IdleText", null, 80, TextAlignmentOptions.Center, Vector2.zero, Vector2.one, Color.white,
                             ThemeRoleId.ScreenStrip, "idle.waiting", FontStyles.Bold, ThemeTextKind.Heading, true);
        SetAnchors(idle.transform, new Vector2(0.03f, 0f), new Vector2(0.97f, 1f));
        idle.raycastTarget = false;
        idle.gameObject.SetActive(true);
        strip.SetSiblingIndex(1);
        strip.gameObject.SetActive(false);
        return strip.gameObject;
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
    /// Overlay canvas for booth-view UI (the briefing/results newsletters),
    /// drawn over the booth and the live desktop, which is a World Space canvas
    /// on the CRT's glass.
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
        ConfigureScaler(scaler);

        if (go.GetComponent<GraphicRaycaster>() == null)
            go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    /// <summary>
    /// Builds a newsletter-styled panel (masthead + rule + dateline title + body
    /// + one action button) for the office view. Hidden by default.
    /// </summary>
    private static Transform BuildNewsletter(Transform parent, string name, string mastheadKey, string buttonKey,
        out TMP_Text title, out TMP_Text body, out Button action)
    {
        Transform panel = Panel(parent, name, Center, Center, Vector2.zero, new Vector2(700f, 780f), Ink, ThemeRoleId.NewsletterBorder);
        Transform paper = Panel(panel, "Paper", Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-10f, -10f), Paper, ThemeRoleId.Newsletter);

        Text(paper, "Masthead", null, 38, TextAlignmentOptions.Center, new Vector2(0.04f, 0.9f), new Vector2(0.96f, 0.99f), Ink,
             ThemeRoleId.Newsletter, mastheadKey, FontStyles.Bold, ThemeTextKind.Heading, true);
        Panel(paper, "Rule", new Vector2(0.06f, 0.885f), new Vector2(0.94f, 0.889f), Vector2.zero, Vector2.zero, Ink, ThemeRoleId.NewsletterBorder);

        title = Text(paper, "TitleText", "", 24, TextAlignmentOptions.Center, new Vector2(0.05f, 0.82f), new Vector2(0.95f, 0.875f), Ink,
                     ThemeRoleId.Newsletter, kind: ThemeTextKind.Heading, fit: true);
        body = Text(paper, "BodyText", "...", 21, TextAlignmentOptions.TopLeft, new Vector2(0.08f, 0.14f), new Vector2(0.92f, 0.8f), Ink, ThemeRoleId.Newsletter);
        action = MakeButton(paper, "ActionButton", null, new Vector2(0.3f, 0.03f), new Vector2(0.7f, 0.11f), new Color(0.16f, 0.15f, 0.13f, 1f),
                            ThemeRoleId.NewsletterButton, buttonKey);

        panel.gameObject.SetActive(false);
        return panel;
    }

    private static void BuildTaskbar(Transform root, out TMP_Text dayText, out TMP_Text moneyText, out TMP_Text stabilityText, out TMP_Text clockText)
    {
        Transform bar = Panel(root, "Taskbar", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 18f), new Vector2(0f, 36f), XpBlue, ThemeRoleId.Taskbar);
        Panel(bar, "TaskbarGloss", new Vector2(0f, 0.72f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, new Color(1f, 1f, 1f, 0.18f), ThemeRoleId.TaskbarGloss);

        Transform start = Panel(bar, "StartButton", new Vector2(0f, 0f), new Vector2(0.12f, 1f), Vector2.zero, Vector2.zero, XpGreen, ThemeRoleId.StartButton);
        Panel(start, "StartGloss", new Vector2(0f, 0.55f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, new Color(1f, 1f, 1f, 0.18f), ThemeRoleId.TaskbarGloss);
        Text(start, "Label", null, 20, TextAlignmentOptions.Center, Vector2.zero, Vector2.one, Color.white,
             ThemeRoleId.StartButton, "taskbar.start", FontStyles.Bold | FontStyles.Italic, ThemeTextKind.Button, true);

        // System tray: Day | Credits | Stability | Clock. Text() returns existing
        // objects unchanged, so the slot anchors are re-applied here (authoritative).
        Transform tray = Panel(bar, "Tray", new Vector2(0.64f, 0.12f), new Vector2(0.995f, 0.88f), Vector2.zero, Vector2.zero, new Color(0.1f, 0.32f, 0.78f, 1f), ThemeRoleId.Tray);
        dayText = Text(tray, "DayText", "Day 1", 18, TextAlignmentOptions.Center, new Vector2(0f, 0f), new Vector2(0.2f, 1f), Color.white, ThemeRoleId.Tray, fit: true);
        moneyText = Text(tray, "MoneyText", "Credits: 0", 18, TextAlignmentOptions.Center, new Vector2(0.2f, 0f), new Vector2(0.48f, 1f), Color.white, ThemeRoleId.Tray, fit: true);
        stabilityText = Text(tray, "StabilityText", "Stability: 100%", 18, TextAlignmentOptions.Center, new Vector2(0.48f, 0f), new Vector2(0.8f, 1f), Color.white, ThemeRoleId.Tray, fit: true);
        clockText = Text(tray, "ClockText", "09:00", 18, TextAlignmentOptions.Center, new Vector2(0.8f, 0f), new Vector2(1f, 1f), Color.white, ThemeRoleId.Tray);
        SetAnchors(dayText.transform, new Vector2(0f, 0f), new Vector2(0.2f, 1f));
        SetAnchors(moneyText.transform, new Vector2(0.2f, 0f), new Vector2(0.48f, 1f));
        SetAnchors(stabilityText.transform, new Vector2(0.48f, 0f), new Vector2(0.8f, 1f));
        SetAnchors(clockText.transform, new Vector2(0.8f, 0f), new Vector2(1f, 1f));

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

        Button minB = MakeButton(header, "MinBtn", null, new Vector2(0.79f, 0.16f), new Vector2(0.85f, 0.86f), null, ThemeRoleId.Button, "window.minimize");
        Button maxB = MakeButton(header, "MaxBtn", null, new Vector2(0.855f, 0.16f), new Vector2(0.915f, 0.86f), null, ThemeRoleId.Button, "window.maximize");
        Button closeB = MakeButton(header, "CloseBtn", null, new Vector2(0.925f, 0.16f), new Vector2(0.985f, 0.86f), XpRed, ThemeRoleId.CloseButton, "window.close");

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

    // ----------------------------- Office booth (world-space) -----------------------------

    /// <summary>
    /// Returns a flat-color placeholder Sprite at Assets/Art/Office/Placeholder/{name}.png,
    /// creating it if missing. Swap the PNG later for final art (same path/name).
    /// </summary>
    private static Sprite EnsureOfficeSprite(string name, Color color, int w, int h)
    {
        Color32 c32 = color;
        return EnsureOfficeShape(name, w, h, new Vector2(0.5f, 0.5f), (x, y) => c32);
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

    /// <summary>Where the office camera sits; the booth art is laid out for its view.</summary>
    private static readonly Vector3 OfficeCamPosition = new Vector3(0f, -1f, -10f);

    /// <summary>Orthographic half-height of the office view (y -7..5 around the camera).</summary>
    private const float OfficeOrthoSize = 6f;

    /// <summary>Centre of the largest 4:3 rectangle inside the CRT's glass, in the crt sprite's own units (the screen anchor and the monitor camera sit on it).</summary>
    private static readonly Vector2 CrtGlassCentre = new Vector2(-0.144f, 0.054f);

    /// <summary>Booth art beyond the drop-in placeholders: the deep desk, the calendar partition and the desk props.</summary>
    private const string BoothArtFolder = "Assets/Art/Office/Booth";

    /// <summary>
    /// Places a booth sprite <paramref name="width"/> world units wide (a uniform
    /// scale from the sprite's own size, so art of another resolution or pixels
    /// per unit keeps the layout), centred on <paramref name="centre"/>.
    /// </summary>
    private static SpriteRenderer PlaceSprite(Transform parent, string name, Sprite sprite, Vector3 centre, float width, int sortingOrder, bool flipX = false)
    {
        SpriteRenderer sr = EnsureSprite(parent, name, sprite, centre, sortingOrder);
        float scale = sprite != null ? width / sprite.bounds.size.x : 1f;
        sr.transform.localScale = new Vector3(scale, scale, 1f);
        sr.flipX = flipX;
        return sr;
    }

    /// <summary>
    /// The width at which a sprite, scaled uniformly and centred on the office
    /// camera, covers its whole view at the reference aspect (no black edges).
    /// </summary>
    private static float OfficeViewCoverWidth(Sprite sprite)
    {
        if (sprite == null)
            return 0f;
        float viewHeight = 2f * OfficeOrthoSize;
        float viewWidth = viewHeight * ReferenceResolution.x / ReferenceResolution.y;
        Vector2 size = sprite.bounds.size;
        return size.x * Mathf.Max(viewWidth / size.x, viewHeight / size.y);
    }

    /// <summary>
    /// Loads booth art from <see cref="BoothArtFolder"/> (no placeholder is made
    /// for it); warns and returns null when the file is missing, leaving that
    /// booth sprite empty.
    /// </summary>
    private static Sprite BoothArt(string file)
    {
        string path = $"{BoothArtFolder}/{file}.png";
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
            Debug.LogWarning($"[TimeDesk] Missing booth art {path}; its booth sprite is left empty.");
        return sprite;
    }

    /// <summary>
    /// Builds the world-space booth, two Cinemachine cameras, a Physics2DRaycaster,
    /// the live monitor (BuildMonitorScreen), the traveller's view, the desk
    /// slots and the desk (BuildDesk: its surface, the paper template, the
    /// scanner and its note), and wires OfficeViewController,
    /// the camera rig, the CRT's click (focus) and the focus exit zone; READY's
    /// saved focus call is cleared (READY only releases GameManager's gate). The
    /// painted art is laid out as in the art pass's 2D composition: the back
    /// wall and the deep desk cover the whole office view, partitions frame it,
    /// and the props sit on the desk and partitions. Idempotent.
    /// </summary>
    private static OfficeViewController BuildBooth(Canvas desktopCanvas, DeskConfigSO config, out MonitorScreen screen)
    {
        // Root for all booth world objects.
        GameObject root = GameObject.Find("OfficeRoot") ?? new GameObject("OfficeRoot");
        Transform booth = root.transform;

        // Set dressing. The left partition has the day calendar painted on it.
        Sprite backWall = EnsureOfficeSprite("backwall", new Color(0.17f, 0.17f, 0.22f), 400, 240);
        PlaceSprite(booth, "BackWall", backWall, new Vector3(0f, OfficeCamPosition.y, 5f), OfficeViewCoverWidth(backWall), -100);
        SpriteRenderer calendarPartition = PlaceSprite(booth, "LeftPartition", BoothArt("partition_calendar"), new Vector3(-9f, 0f, 5f), 3.8f, -50);
        PlaceSprite(booth, "RightPartition", EnsureOfficeSprite("partition", new Color(0.24f, 0.24f, 0.30f), 120, 240), new Vector3(9f, 0f, 5f), 3.4f, -50, flipX: true);
        Sprite desk = BoothArt("desk_deep");
        PlaceSprite(booth, "Desk", desk, new Vector3(0f, OfficeCamPosition.y, 0f), OfficeViewCoverWidth(desk), -10);

        // The traveller stands behind the desk, whose far edge hides the
        // placeholder's lower part; shown from presentation until the decision.
        SpriteRenderer traveller = PlaceSprite(booth, "Traveller", EnsureOfficeSprite("traveller", new Color(0.49f, 0.42f, 0.86f), 60, 110), new Vector3(0f, 0.5f, 2f), 2f, -20);
        BuildTravellerView(traveller);

        // Desk props (their clicks and reactions: BuildDeskInteraction). The plant
        // and the mug stand at their named slots (decoration hooks).
        Transform slots = BuildDeskSlots(booth);
        PlaceSprite(booth, "DeskIntercom", BoothArt("intercom"), new Vector3(-6.3f, -1.8f, 0f), 2.1f, 1);
        SpriteRenderer tray = PlaceSprite(booth, "ScannerTray", BoothArt("scanner_tray"), new Vector3(-4.6f, -3.1f, 0f), 3.8f, 2);
        PlaceSprite(booth, "DeskPlant", BoothArt("desk_plant"), booth.InverseTransformPoint(slots.Find("plant").position), 1.8f, 3);
        PlaceSprite(booth, "DeskMug", BoothArt("desk_mug"), booth.InverseTransformPoint(slots.Find("mug").position), 0.85f, 5);
        PlaceSprite(booth, "DeskStamp", BoothArt("desk_stamp"), new Vector3(-2.2f, -4.3f, 0f), 0.8f, 5);

        // Interactables: CRT (right of the desk) and READY sign (centre).
        SpriteRenderer crt = PlaceSprite(booth, "CRTMonitor", EnsureOfficeSprite("crt", new Color(0.85f, 0.81f, 0.65f), 150, 130), new Vector3(6f, -1.7f, 0f), 5.2f, 0);
        Clickable crtClick = EnsureClickable(crt);

        SpriteRenderer sign = PlaceSprite(booth, "ReadySign", EnsureOfficeSprite("sign", new Color(0.79f, 0.76f, 0.58f), 96, 50), new Vector3(0f, -2f, 0f), 2.3f, 2);
        Clickable signClick = EnsureClickable(sign);

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
        Physics2DRaycaster raycaster = main.GetComponent<Physics2DRaycaster>();
        if (raycaster == null)
            raycaster = main.gameObject.AddComponent<Physics2DRaycaster>();

        // A fixed hit buffer keeps the UI module's per-frame booth raycast allocation-free.
        raycaster.maxRayIntersections = BoothRaycastHits;

        // The live monitor: the desktop canvas on the glass, its power button and LED, the focus zones.
        screen = BuildMonitorScreen(crt, desktopCanvas, main, config);

        // Cameras: the booth view, and a push-in on the glass. The monitor camera
        // starts framed for the reference aspect; the rig reframes it for the
        // screen's aspect on every push-in (MonitorFraming, the fill knob).
        GameObject camsRoot = GameObject.Find("Cameras") ?? new GameObject("Cameras");
        CinemachineCamera officeCam = EnsureVcam(camsRoot.transform, "OfficeVCam", OfficeCamPosition, OfficeOrthoSize);
        Vector3 glass = screen.GlassCentre;
        Vector2 glassSize = screen.GlassWorldSize;
        float monitorOrtho = MonitorFraming.OrthoSize(glassSize.x, glassSize.y, ReferenceResolution.x / ReferenceResolution.y, config.monitorFill);
        CinemachineCamera monitorCam = EnsureVcam(camsRoot.transform, "MonitorVCam", new Vector3(glass.x, glass.y, OfficeCamPosition.z), monitorOrtho);

        // Camera rig + view controller on OfficeRoot.
        CinemachineCameraRig rig = root.GetComponent<CinemachineCameraRig>();
        if (rig == null)
            rig = root.AddComponent<CinemachineCameraRig>();
        var soRig = new SerializedObject(rig);
        SetRef(soRig, "officeCam", officeCam);
        SetRef(soRig, "monitorCam", monitorCam);
        SetRef(soRig, "brain", main.GetComponent<CinemachineBrain>());
        SetRef(soRig, "monitorScreen", screen);
        SetRef(soRig, "config", config);
        soRig.ApplyModifiedProperties();

        OfficeViewController view = root.GetComponent<OfficeViewController>();
        if (view == null)
            view = root.AddComponent<OfficeViewController>();
        var soView = new SerializedObject(view);
        SetRef(soView, "cameraRigBehaviour", rig);
        SetRef(soView, "desktopRoot", desktopCanvas.gameObject);
        SetRef(soView, "monitorScreen", screen);
        soView.ApplyModifiedProperties();

        // The CRT's click pushes in; a click outside the screen pulls back.
        // READY only releases GameManager's gate (no zoom): its saved focus call goes.
        WireClickToFocusMonitor(crtClick, view);
        WirePersistentVoid(crt.transform.Find("FocusExitZone").GetComponent<Clickable>(), "onClick", view, nameof(OfficeViewController.FocusOffice));
        ClearPersistentCalls(signClick, "onClick");

        // The desk: its surface, the paper template, the scanner and its note.
        BuildDesk(booth, tray, config);

        // Diegetic readouts + a timeline-reactive poster. The desktop's old
        // "< Office" button is replaced by the taskbar's "< Desk" (BuildDesktopShell).
        BuildReadouts(booth, calendarPartition.transform);
        BuildReactiveProp(booth);
        DestroyChildIfPresent(desktopCanvas.transform, "BackToOfficeButton");

        return view;
    }

    /// <summary>
    /// Builds the in-world Day calendar (painted on the given partition),
    /// Stability monitor, and Credits till, and wires an OfficeReadouts component
    /// on OfficeRoot. Each number auto-sizes inside the blank area of its art.
    /// Idempotent.
    /// </summary>
    private static void BuildReadouts(Transform root, Transform calendarPartition)
    {
        // Day — the calendar sheet painted on the left partition, drawn in
        // perspective, so the number tilts with it. Older builds hung a separate
        // calendar sprite (with the number) instead.
        DestroyChildIfPresent(root, "DayCalendar");
        TextMeshPro dayText = WorldText(calendarPartition, "DayNumber", "01", new Color(0.19f, 0.29f, 0.33f),
            new Vector2(0.38f, 2.79f), new Vector2(1.9f, 2f), -13f, 16f, -39);

        // Stability — TVA-style monitor on the right partition (the sprite is the lamp).
        SpriteRenderer lamp = PlaceSprite(root, "StabilityMonitor", EnsureOfficeSprite("stabilitymonitor", Color.white, 110, 80), new Vector3(8.5f, 2f, 5f), 2f, -40);
        TextMeshPro stabText = WorldText(lamp.transform, "StabilityPercent", "100%", new Color(0.19f, 0.29f, 0.33f),
            new Vector2(0f, 0.02f), new Vector2(0.6f, 0.36f), 0f, 3f, -39);

        // Credits — cash till on the desk below the CRT (with an AudioSource for the ding).
        SpriteRenderer till = PlaceSprite(root, "CreditsTill", EnsureOfficeSprite("till", new Color(0.55f, 0.5f, 0.42f), 110, 80), new Vector3(7f, -4.8f, 0f), 2.5f, 5);
        TextMeshPro creditsText = WorldText(till.transform, "CreditsNumber", "0", new Color(0.8f, 1f, 0.85f),
            new Vector2(0.05f, 0.19f), new Vector2(0.36f, 0.13f), -3f, 1.4f, 6);
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
    /// Ships with a default sprite (poster.png, shown while no mapped cue is
    /// active) and an empty mapping list for the designer.
    /// </summary>
    private static void BuildReactiveProp(Transform root)
    {
        // On the back wall's panel left of the traveller, clear of the desk's far edge.
        SpriteRenderer poster = PlaceSprite(root, "ReactivePoster", EnsureOfficeSprite("poster", new Color(0.5f, 0.45f, 0.6f), 80, 110), new Vector3(-5.6f, 0.08f, 5f), 1.1f, -40);
        TimelineReactiveSprite reactive = poster.GetComponent<TimelineReactiveSprite>();
        if (reactive == null)
            reactive = poster.gameObject.AddComponent<TimelineReactiveSprite>();
        var so = new SerializedObject(reactive);
        SetRef(so, "target", poster);
        SetRef(so, "defaultSprite", poster.sprite);
        so.ApplyModifiedProperties();
    }

    // ----------------------------- Shift clock (booth + tray) -----------------------------

    /// <summary>
    /// Hit buffer size for the booth's Physics2DRaycaster (non-allocating
    /// raycasts): one point can cross up to 8 stacked papers plus the scanner,
    /// a prop, the traveller zone and the exit zone, and hits are cut before
    /// they are sorted.
    /// </summary>
    private const int BoothRaycastHits = 16;

    /// <summary>Ink colour of the placeholder wall clock.</summary>
    private static readonly Color32 ClockInk = new Color32(30, 28, 26, 255);

    /// <summary>
    /// Builds the booth wall clock (face plus hour and minute hands; placeholder
    /// art until the real clock pieces land) and wires ShiftClockReadouts on
    /// OfficeRoot to the driver, the tray clock and the hands. Idempotent.
    /// </summary>
    private static void BuildShiftClockReadouts(ShiftClockDriver driver, TMP_Text trayClockText)
    {
        GameObject root = GameObject.Find("OfficeRoot");
        if (root == null)
        {
            Debug.LogWarning("[TimeDesk] No OfficeRoot, so the wall clock was not built (the booth is built first in Build()).");
            return;
        }

        // On the back wall's window mullion right of the traveller.
        SpriteRenderer face = PlaceSprite(root.transform, "WallClock",
            EnsureOfficeShape("clock_face", 100, 100, new Vector2(0.5f, 0.5f), ClockFacePixel),
            new Vector3(4f, 3.2f, 5f), 1.3f, -40);
        SpriteRenderer hourHand = EnsureSprite(face.transform, "HourHand",
            EnsureOfficeShape("clock_hand_hour", 8, 30, new Vector2(0.5f, 0.1f), (x, y) => ClockInk),
            new Vector3(0f, 0f, -0.01f), -39);
        SpriteRenderer minuteHand = EnsureSprite(face.transform, "MinuteHand",
            EnsureOfficeShape("clock_hand_minute", 6, 42, new Vector2(0.5f, 0.07f), (x, y) => ClockInk),
            new Vector3(0f, 0f, -0.02f), -38);

        ShiftClockReadouts readouts = root.GetComponent<ShiftClockReadouts>();
        if (readouts == null)
            readouts = root.AddComponent<ShiftClockReadouts>();
        var so = new SerializedObject(readouts);
        SetRef(so, "driver", driver);
        SetRef(so, "trayClockText", trayClockText);
        SetRef(so, "hourHand", hourHand.transform);
        SetRef(so, "minuteHand", minuteHand.transform);
        so.ApplyModifiedProperties();
    }

    /// <summary>Placeholder clock face: cream dial, dark rim, hour ticks, centre cap.</summary>
    private static Color32 ClockFacePixel(int x, int y)
    {
        float dx = x - 49.5f;
        float dy = y - 49.5f;
        float d = Mathf.Sqrt(dx * dx + dy * dy);
        if (d > 49f)
            return new Color32(0, 0, 0, 0);
        if (d > 45f)
            return ClockInk;
        float angle = Mathf.Repeat(Mathf.Atan2(dy, dx) * Mathf.Rad2Deg, 30f);
        if (d > 37f && (angle < 2.5f || angle > 27.5f))
            return ClockInk;
        if (d < 3f)
            return ClockInk;
        return new Color32(242, 237, 220, 255);
    }

    /// <summary>
    /// Returns a placeholder Sprite at Assets/Art/Office/Placeholder/{name}.png drawn
    /// by a pixel function, with a custom pivot (e.g. clock hands pivot at their
    /// base). Created once; swap the PNG for final art and keep the .meta (the pivot lives there).
    /// </summary>
    private static Sprite EnsureOfficeShape(string name, int w, int h, Vector2 pivot, System.Func<int, int, Color32> pixel)
    {
        string assetPath = $"Assets/Art/Office/Placeholder/{name}.png";
        Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        if (existing != null)
            return existing;

        PlaceholderPng.Write(assetPath, w, h, pixel);
        if (AssetImporter.GetAtPath(assetPath) is TextureImporter imp)
        {
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = 100f;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
            var settings = new TextureImporterSettings();
            imp.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = pivot;
            imp.SetTextureSettings(settings);
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
    }

    /// <summary>Sets a RectTransform's anchors and zeroes its offsets (stretch within the anchors).</summary>
    private static void SetAnchors(Transform t, Vector2 aMin, Vector2 aMax)
    {
        var rt = (RectTransform)t;
        rt.anchorMin = aMin;
        rt.anchorMax = aMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    // ----------------------------- Cursor + hover highlight -----------------------------

    /// <summary>Settings asset for the cursor and hover outline.</summary>
    private const string InteractionFeedbackPath = "Assets/Data/Config/InteractionFeedback_Default.asset";

    /// <summary>URP's unlit sprite material (outlines ignore 2D lighting).</summary>
    private const string UnlitSpriteMaterialPath = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";

    /// <summary>Where generated placeholder cursors live (never mistaken for final art).</summary>
    private const string PlaceholderCursorFolder = "Assets/Art/Generated/Cursors";

    /// <summary>Placeholder arrow outline, in top-left pixel coordinates of a 32x32 cursor.</summary>
    private static readonly Vector2[] ArrowCursorShape =
    {
        new Vector2(0, 0), new Vector2(0, 22), new Vector2(5, 17), new Vector2(9, 26),
        new Vector2(12, 25), new Vector2(8, 16), new Vector2(15, 16),
    };

    /// <summary>Placeholder pointing hand (fingertip at 12,1), top-left pixel coordinates.</summary>
    private static readonly Vector2[] HandCursorShape =
    {
        new Vector2(10, 1), new Vector2(13, 1), new Vector2(14, 2), new Vector2(14, 12),
        new Vector2(21, 13), new Vector2(23, 15), new Vector2(23, 25), new Vector2(19, 30),
        new Vector2(10, 30), new Vector2(6, 24), new Vector2(5, 18), new Vector2(7, 17),
        new Vector2(10, 19),
    };

    /// <summary>
    /// Ensures the interaction-feedback settings (cursor art by file name when
    /// present, placeholders otherwise, with click points derived from the art;
    /// unlit outline material) and assigns them to RunConfig, from which
    /// InteractionFeedbackBootstrap builds the one persistent HoverHighlighter
    /// used in every scene. Idempotent.
    /// </summary>
    private static void BuildInteractionFeedback()
    {
        InteractionFeedbackSO settings = AssetDatabase.LoadAssetAtPath<InteractionFeedbackSO>(InteractionFeedbackPath);
        if (settings == null)
        {
            PlaceholderPng.EnsureFolderTree("Assets/Data/Config");
            settings = ScriptableObject.CreateInstance<InteractionFeedbackSO>();
            AssetDatabase.CreateAsset(settings, InteractionFeedbackPath);
        }

        // Final art (by file name) replaces empty or placeholder cursors; a designer's own pick is kept.
        // Whenever a cursor texture is replaced, its click point is re-derived from the image.
        if (settings.arrowCursor == null || IsPlaceholderCursor(settings.arrowCursor))
        {
            Texture2D arrow = EnsureCursorTexture("cursor_arrow", ArrowCursorShape);
            if (arrow != settings.arrowCursor)
            {
                settings.arrowCursor = arrow;
                settings.arrowHotspot = DetectHotspot(arrow, CursorHotspot.Kind.Tip, settings.arrowHotspot);
            }
        }
        if (settings.handCursor == null || IsPlaceholderCursor(settings.handCursor))
        {
            Texture2D hand = EnsureCursorTexture("cursor_hand", HandCursorShape);
            if (hand != settings.handCursor)
            {
                settings.handCursor = hand;
                settings.handHotspot = DetectHotspot(hand, CursorHotspot.Kind.Fingertip, settings.handHotspot);
            }
        }
        if (settings.outlineMaterial == null)
            settings.outlineMaterial = AssetDatabase.LoadAssetAtPath<Material>(UnlitSpriteMaterialPath);
        EditorUtility.SetDirty(settings);

        RunConfigSO runConfig = FindAssetByName<RunConfigSO>("RunConfig");
        if (runConfig == null)
            Debug.LogWarning("[TimeDesk] No RunConfig asset, so the game cursor and hover outline are not active. Create Assets/Resources/RunConfig.asset and rebuild.");
        else if (runConfig.interactionFeedback != settings)
        {
            runConfig.interactionFeedback = settings;
            EditorUtility.SetDirty(runConfig);
        }

        AssetDatabase.SaveAssets();
    }

    /// <summary>Click point of a cursor texture from its alpha (falls back to the current value if unreadable).</summary>
    private static Vector2 DetectHotspot(Texture2D cursor, CursorHotspot.Kind kind, Vector2 fallback)
    {
        if (cursor == null || !cursor.isReadable)
            return fallback;

        Color32[] pixels = cursor.GetPixels32();
        var alpha = new byte[pixels.Length];
        for (int i = 0; i < pixels.Length; i++)
            alpha[i] = pixels[i].a;

        (int x, int y) = CursorHotspot.Find(alpha, cursor.width, cursor.height, kind);
        Debug.Log($"[TimeDesk] Cursor '{cursor.name}' click point set to ({x}, {y}).");
        return new Vector2(x, y);
    }

    /// <summary>True for a cursor generated by this builder.</summary>
    private static bool IsPlaceholderCursor(Texture2D texture) =>
        AssetDatabase.GetAssetPath(texture).StartsWith(PlaceholderCursorFolder);

    /// <summary>
    /// Returns the cursor texture named <paramref name="artName"/> (final art, anywhere
    /// under Assets) or a generated placeholder, with Cursor import settings applied.
    /// </summary>
    private static Texture2D EnsureCursorTexture(string artName, Vector2[] placeholderShape)
    {
        const int size = 32;
        Texture2D art = FindAssetByName<Texture2D>(artName);
        string path = art != null ? AssetDatabase.GetAssetPath(art) : $"{PlaceholderCursorFolder}/placeholder_{artName}.png";

        if (art == null && AssetDatabase.LoadAssetAtPath<Texture2D>(path) == null)
            PlaceholderPng.Write(path, size, size, (x, y) => CursorPixel(placeholderShape, x, y, size));

        if (AssetImporter.GetAtPath(path) is TextureImporter imp &&
            (imp.textureType != TextureImporterType.Cursor || !imp.isReadable || imp.mipmapEnabled))
        {
            imp.textureType = TextureImporterType.Cursor;
            imp.isReadable = true;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
            imp.npotScale = TextureImporterNPOTScale.None;
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }


    /// <summary>Placeholder cursor pixel: white inside the polygon, 1 px black edge, clear outside.</summary>
    private static Color32 CursorPixel(Vector2[] polygon, int x, int y, int size)
    {
        bool Inside(int px, int py) => InPolygon(polygon, px + 0.5f, (size - 1 - py) + 0.5f);

        if (!Inside(x, y))
            return new Color32(0, 0, 0, 0);
        bool edge = !Inside(x - 1, y) || !Inside(x + 1, y) || !Inside(x, y - 1) || !Inside(x, y + 1);
        return edge ? new Color32(0, 0, 0, 255) : new Color32(255, 255, 255, 255);
    }

    /// <summary>Even-odd point-in-polygon test.</summary>
    private static bool InPolygon(Vector2[] polygon, float px, float py)
    {
        bool inside = false;
        for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
        {
            if ((polygon[i].y > py) != (polygon[j].y > py) &&
                px < (polygon[j].x - polygon[i].x) * (py - polygon[i].y) / (polygon[j].y - polygon[i].y) + polygon[i].x)
                inside = !inside;
        }
        return inside;
    }

    /// <summary>
    /// Builds the fake-OS desktop shell: a left column of icons (some unlock-gated
    /// by a library upgrade id, reported when the library does not know it) that
    /// open placeholder windows with min/max/close chrome, a Start menu
    /// (Settings, Turn off screen and Quit game) wired to a DesktopShell on the
    /// canvas, and the taskbar's "&lt; Desk" button (FocusOffice; built here, after
    /// the view exists). Idempotent.
    /// </summary>
    private static void BuildDesktopShell(Canvas canvas, Transform iconGrid, Transform windowLayer, ContentLibrarySO library,
                                          OfficeViewController view, MonitorScreen screen)
    {
        Transform root = canvas.transform;

        // Remove the earlier redundant placeholder shell (parallel desktop) from
        // prior builds so it no longer overlaps the real investigation desk.
        DestroyChildIfPresent(root, "DesktopIcons");
        DestroyChildIfPresent(root, "DesktopWindowLayer");

        // Only genuinely-new apps get placeholder windows; existing
        // Passport/Permit documents, reference books, and Compare are launched by
        // the investigation icon grid (real windows with real data). Scanner,
        // Directives, Records and the interview transcript (Clue Log) are built
        // separately in Build() and wired to controllers. Upgrade ids are
        // UpgradeSO.id values.
        var apps = new (string name, string labelKey, string titleKey, string bodyKey, string upgrade)[]
        {
            ("IconInternet", "icon.internet", "window.internet", "body.internet", ""),
            ("IconLexicon",  "icon.lexicon",  "window.lexicon",  "body.lexicon", "archive_access"),
            ("IconDialect",  "icon.dialect",  "window.dialect",  "body.dialect", ""),
            ("IconMaterial", "icon.material", "window.material", "body.material", "adv_scanner"),
            ("IconNotes",    "icon.notes",    "window.notes",    "body.notes", ""),
        };

        // BuildOSWindow keeps an existing window's texts, so the renamed Dialect and Material windows are rebuilt.
        DestroyChildIfPresent(windowLayer, "IconDialectWindow");
        DestroyChildIfPresent(windowLayer, "IconMaterialWindow");

        foreach (var a in apps)
        {
            if (library != null && !string.IsNullOrEmpty(a.upgrade) && library.GetUpgradeById(a.upgrade) == null)
                Debug.LogError($"[TimeDesk] Desktop icon '{a.name}' requires unknown upgrade '{a.upgrade}' (not in the content library's upgrades); it could never unlock.");

            OSWindowChrome w = BuildOSWindow(windowLayer, a.name + "Window", a.titleKey, a.bodyKey, null);
            BuildDesktopIcon(iconGrid, a.name, a.labelKey, w, a.upgrade);
        }

        OSWindowChrome settings = BuildSettingsWindow(windowLayer);

        // Rebuilt from scratch each run: the entries need fixed LayoutElement
        // heights or the vertical layout collapses them on top of each other
        // (three 46-px entries, 4 px apart, 6 px padding: 158 px).
        DestroyChildIfPresent(root, "StartMenu");
        Transform startMenu = Panel(root, "StartMenu", new Vector2(0f, 0f), new Vector2(0.2f, 0f), new Vector2(0f, 119f), new Vector2(0f, 158f), new Color(0.1f, 0.12f, 0.18f, 0.97f), ThemeRoleId.StartMenu);
        AddVLayout(startMenu, 4f);
        Button settingsEntry = MakeButton(startMenu, "SettingsEntry", null, Vector2.zero, Vector2.one, new Color(0.2f, 0.25f, 0.35f, 1f), ThemeRoleId.MenuEntry, "startmenu.settings");
        SetLayoutHeight(settingsEntry, 46f);
        Button screenOffEntry = MakeButton(startMenu, "ScreenOffEntry", null, Vector2.zero, Vector2.one, new Color(0.2f, 0.25f, 0.35f, 1f), ThemeRoleId.MenuEntry, "startmenu.screenOff");
        SetLayoutHeight(screenOffEntry, 46f);
        Button quitEntry = MakeButton(startMenu, "QuitEntry", null, Vector2.zero, Vector2.one, new Color(0.5f, 0.2f, 0.2f, 1f), ThemeRoleId.QuitEntry, "startmenu.quit");
        SetLayoutHeight(quitEntry, 46f);
        startMenu.gameObject.SetActive(false);

        // The taskbar's way back to the booth, next to Start.
        Transform taskbar = root.Find("Taskbar");
        Button deskButton = MakeButton(taskbar, "DeskButton", null, new Vector2(0.125f, 0.1f), new Vector2(0.245f, 0.9f), new Color(0.2f, 0.3f, 0.5f, 0.95f),
                                       ThemeRoleId.DeskButton, "taskbar.desk");
        SetAnchors(deskButton.transform, new Vector2(0.125f, 0.1f), new Vector2(0.245f, 0.9f));
        WirePersistentVoid(deskButton, "m_OnClick", view, nameof(OfficeViewController.FocusOffice));

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
        SetRef(soShell, "quitButton", quitEntry);
        SetRef(soShell, "screenOffButton", screenOffEntry);
        SetRef(soShell, "monitorScreen", screen);
        SetRef(soShell, "settingsWindow", settings);
        soShell.ApplyModifiedProperties();
    }

    /// <summary>
    /// Builds a desktop window with a draggable title bar (its title keyed)
    /// and min/max/close chrome; its body is a keyed static text
    /// (<paramref name="bodyKey"/>) or a sample the controller rewrites.
    /// </summary>
    private static OSWindowChrome BuildOSWindow(Transform layer, string name, string titleKey, string bodyKey, string bodySample, Vector2? size = null,
                                                ThemeRoleId bodyRole = ThemeRoleId.WindowBody)
    {
        Transform win = Panel(layer, name, Center, Center, Vector2.zero, size ?? new Vector2(580f, 400f), Paper, bodyRole);

        Transform header = Panel(win, "Header", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -15f), new Vector2(0f, 30f), HeaderBar, ThemeRoleId.TitleBar);
        DraggableWindow drag = header.GetComponent<DraggableWindow>();
        if (drag == null)
            drag = header.gameObject.AddComponent<DraggableWindow>();
        var soDrag = new SerializedObject(drag);
        SetRef(soDrag, "windowRoot", (RectTransform)win);
        soDrag.ApplyModifiedProperties();

        Text(header, "TitleText", null, 15, TextAlignmentOptions.Left, new Vector2(0.04f, 0f), new Vector2(0.7f, 1f), Color.white,
             ThemeRoleId.TitleBar, titleKey, FontStyles.Bold, ThemeTextKind.Heading, true);

        TMP_Text bodyText = Text(win, "Body", bodySample, 20, TextAlignmentOptions.TopLeft, new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.82f), Ink, bodyRole, bodyKey);
        bodyText.textWrappingMode = TextWrappingModes.Normal;
        bodyText.overflowMode = TextOverflowModes.Truncate;
        if (win.GetComponent<RectMask2D>() == null)
            win.gameObject.AddComponent<RectMask2D>(); // nothing bleeds outside the window

        OSWindowChrome chrome = BuildWinControls(win, header);

        win.gameObject.SetActive(false); // opened by its icon
        return chrome;
    }

    /// <summary>
    /// The Settings window (piece 6 U12): "UI language" with the two choices
    /// (SettingsWindowController) and a note that a change applies at the next
    /// office load and that colours, fonts and the wallpaper follow history.
    /// </summary>
    private static OSWindowChrome BuildSettingsWindow(Transform windowLayer)
    {
        OSWindowChrome chrome = BuildOSWindow(windowLayer, "SettingsWindow", "window.settings", "settings.language", null);
        Transform win = chrome.transform;
        SetAnchors(win.Find("Body"), new Vector2(0.05f, 0.7f), new Vector2(0.95f, 0.82f));
        Button follow = MakeButton(win, "FollowHistoryButton", null, new Vector2(0.05f, 0.5f), new Vector2(0.48f, 0.66f), null, ThemeRoleId.Button, "settings.followHistory");
        Button english = MakeButton(win, "AlwaysEnglishButton", null, new Vector2(0.52f, 0.5f), new Vector2(0.95f, 0.66f), null, ThemeRoleId.Button, "settings.alwaysEnglish");
        TMP_Text note = Text(win, "NoteText", null, 17, TextAlignmentOptions.TopLeft, new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.44f), Ink,
                             ThemeRoleId.WindowBody, "settings.note");
        note.textWrappingMode = TextWrappingModes.Normal;

        SettingsWindowController controller = win.GetComponent<SettingsWindowController>();
        if (controller == null)
            controller = win.gameObject.AddComponent<SettingsWindowController>();
        var so = new SerializedObject(controller);
        SetRef(so, "followHistoryButton", follow);
        SetRef(so, "alwaysEnglishButton", english);
        so.ApplyModifiedProperties();
        return chrome;
    }

    /// <summary>Builds a TMP input field (box + masked viewport + keyed placeholder + text).</summary>
    private static TMP_InputField BuildInputField(Transform parent, string name, string placeholderKey, Vector2 aMin, Vector2 aMax)
    {
        DestroyChildIfPresent(parent, name);
        Transform box = Panel(parent, name, aMin, aMax, Vector2.zero, Vector2.zero, Color.white, ThemeRoleId.InputField);

        Transform area = Panel(box, "TextArea", Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-14f, -8f), null);
        area.gameObject.AddComponent<RectMask2D>();
        TMP_Text ph = Text(area, "Placeholder", null, 17, TextAlignmentOptions.Left, Vector2.zero, Vector2.one, new Color(0.45f, 0.45f, 0.45f, 0.8f),
                           ThemeRoleId.InputPlaceholder, placeholderKey, FontStyles.Italic, ThemeTextKind.Body, true);
        TMP_Text text = Text(area, "Text", "", 17, TextAlignmentOptions.Left, Vector2.zero, Vector2.one, Ink, ThemeRoleId.InputField);

        TMP_InputField input = box.gameObject.AddComponent<TMP_InputField>();
        input.textViewport = (RectTransform)area;
        input.textComponent = text;
        input.placeholder = ph;
        input.targetGraphic = box.GetComponent<Image>();
        return input;
    }

    /// <summary>Builds a compare-clickable label/value record row; returns row + value text.</summary>
    private static (GameObject row, TMP_Text value) BuildRecordRow(Transform parent, string name, string label, Vector2 aMin, Vector2 aMax)
    {
        DestroyChildIfPresent(parent, name);
        Transform row = Panel(parent, name, aMin, aMax, Vector2.zero, Vector2.zero, new Color(1f, 1f, 1f, 0.7f), ThemeRoleId.DiegeticRow);
        Button btn = row.gameObject.AddComponent<Button>();
        btn.targetGraphic = row.GetComponent<Image>();

        Text(row, "Label", label, 17, TextAlignmentOptions.Left, new Vector2(0.03f, 0f), new Vector2(0.3f, 1f), new Color(0.35f, 0.32f, 0.25f, 1f), ThemeRoleId.DiegeticLabel);
        TMP_Text value = Text(row, "Value", "", 17, TextAlignmentOptions.Left, new Vector2(0.33f, 0f), new Vector2(0.97f, 1f), Ink, ThemeRoleId.DiegeticRow);
        return (row.gameObject, value);
    }

    /// <summary>Builds a desktop icon button bound to a window (its label keyed), with optional unlock-gating.</summary>
    private static void BuildDesktopIcon(Transform grid, string name, string labelKey, OSWindowChrome window, string upgradeId)
    {
        Button btn = MakeButton(grid, name, null, Vector2.zero, Vector2.one, new Color(0.2f, 0.3f, 0.45f, 0.85f), ThemeRoleId.DesktopIcon, labelKey);
        Transform labelObject = btn.transform.Find("Label");
        TMP_Text labelText = labelObject != null ? labelObject.GetComponent<TMP_Text>() : null;
        if (labelText != null)
        {
            labelText.text = UiText.Get(labelKey); // an existing icon keeps its place in the grid; its label follows the builder
            // Icon labels wrap between words and shrink (FitIconLabel), not the one-line fit.
            Tag(labelText, ThemeRoleId.DesktopIcon, ThemePart.Ink, labelKey, FontStyles.Normal, ThemeTextKind.Button, false);
        }
        FitIconLabel(btn);
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

    /// <summary>Largest font size of a desktop icon label.</summary>
    private const float IconLabelMaxSize = 18f;

    /// <summary>Smallest font size an icon label shrinks to.</summary>
    private const float IconLabelMinSize = 10f;

    /// <summary>
    /// Lets a desktop icon's label shrink to fit its tile, wrapping only between
    /// words: auto-sizing shrinks a word that does not fit the tile's width
    /// instead of breaking it. Re-applied on every build (MakeButton keeps an
    /// existing label as it is; the label text is set by BuildDesktopIcon).
    /// </summary>
    private static void FitIconLabel(Button icon)
    {
        Transform label = icon.transform.Find("Label");
        TMP_Text text = label != null ? label.GetComponent<TMP_Text>() : null;
        if (text == null)
            return;
        text.enableAutoSizing = true;
        text.fontSizeMax = IconLabelMaxSize;
        text.fontSizeMin = IconLabelMinSize;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.margin = new Vector4(3f, 2f, 3f, 2f);
    }

    /// <summary>Makes a booth sprite clickable: a Clickable whose click box fits the visible art.</summary>
    private static Clickable EnsureClickable(SpriteRenderer sr)
    {
        BoxCollider2D box = sr.GetComponent<BoxCollider2D>();
        if (box == null)
            box = sr.gameObject.AddComponent<BoxCollider2D>();
        FitColliderToArt(box, sr.sprite);
        Clickable c = sr.GetComponent<Clickable>();
        if (c == null)
            c = sr.gameObject.AddComponent<Clickable>();
        return c;
    }

    /// <summary>
    /// Sizes a click box to the visible art: the bounds of the sprite's physics
    /// shape (traced from its alpha at import), or the whole sprite when it has
    /// none. Applied on every build, so swapped art never keeps an old hit area.
    /// </summary>
    private static void FitColliderToArt(BoxCollider2D box, Sprite sprite)
    {
        if (sprite == null)
            return;

        Bounds bounds = sprite.bounds;
        bool traced = false;
        var outline = new System.Collections.Generic.List<Vector2>();
        for (int i = 0; i < sprite.GetPhysicsShapeCount(); i++)
        {
            sprite.GetPhysicsShape(i, outline);
            foreach (Vector2 p in outline)
            {
                if (!traced)
                    bounds = new Bounds(p, Vector3.zero);
                bounds.Encapsulate(p);
                traced = true;
            }
        }
        box.offset = bounds.center;
        box.size = bounds.size;
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
    /// serialized property (e.g. Clickable "onClick" or Button "m_OnClick"),
    /// replacing the calls ClearPersistentCalls empties.
    /// </summary>
    private static void WirePersistentVoid(Object host, string eventProp, Object target, string method)
    {
        var so = new SerializedObject(host);
        SerializedProperty calls = ClearPersistentCalls(so, eventProp);
        if (calls == null)
            return;

        calls.InsertArrayElementAtIndex(0);
        SerializedProperty call = calls.GetArrayElementAtIndex(0);
        call.FindPropertyRelative("m_Target").objectReferenceValue = target;
        call.FindPropertyRelative("m_TargetAssemblyTypeName").stringValue = target.GetType().AssemblyQualifiedName;
        call.FindPropertyRelative("m_MethodName").stringValue = method;
        call.FindPropertyRelative("m_Mode").enumValueIndex = 1; // Void
        call.FindPropertyRelative("m_CallState").enumValueIndex = 2; // RuntimeOnly
        so.ApplyModifiedProperties();
    }

    /// <summary>
    /// Creates (or finds) a world-space TextMeshPro readout under a sprite, fitted
    /// to a blank area of its art: <paramref name="centre"/> and <paramref name="size"/>
    /// are in the sprite's own units, <paramref name="tilt"/> (degrees) follows the
    /// art's perspective, and the text auto-sizes on one line, never above
    /// <paramref name="maxFontSize"/>, so no value spills out of the art.
    /// </summary>
    private static TextMeshPro WorldText(Transform parent, string name, string content, Color color, Vector2 centre, Vector2 size, float tilt, float maxFontSize, int sortingOrder)
    {
        Transform existing = parent.Find(name);
        GameObject go = existing != null ? existing.gameObject : new GameObject(name, typeof(TextMeshPro));
        if (existing == null)
            go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(centre.x, centre.y, -0.1f);
        go.transform.localRotation = Quaternion.Euler(0f, 0f, tilt);

        TextMeshPro tmp = go.GetComponent<TextMeshPro>();
        if (tmp == null)
            tmp = go.AddComponent<TextMeshPro>();
        tmp.text = content;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMax = maxFontSize;
        tmp.fontSizeMin = maxFontSize * 0.1f;
        tmp.fontSize = maxFontSize;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
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
