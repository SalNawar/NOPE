using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// One-click builder of the office's gameplay layer,
/// Assets/Scenes/OfficeGameplay.unity, which loads additively on top of the
/// art office (Assets/Scenes/OfficeScene.unity, the art side's; never opened
/// or written here). Creates and wires everything needed for a playable shift:
/// - The PC desktop: a World Space canvas (1440 x 1080 units at 4:3) on its
///   own layer far from the office, drawn by the frame camera into the PC
///   frame (a ReStory-style monitor over the office, opened by clicking the
///   PC) and by the clone camera onto the office PC's glass, with screen
///   power [MonitorScreen, PcScreenClone, PcFrame, OfficeViewController];
///   EventSystem (new Input System)
/// - HUD (day/money/stability) on the desktop's taskbar; the citation slip and
///   the verdict line on the office overlay (piece 10)  [OfficeUIController]
/// - Morning briefing + shift report panels  [DayFlowUIController]
/// - Investigation desk: claim banner, directives, draggable/multi-page document
///   windows, reference-book windows, the interview transcript (their tiles in
///   the interim Investigation window), a visual compare bar, and Accept/Deny
///   buttons, laid out for the 4:3 desktop  [InvestigationUIController +
///   CompareController]
/// - The desktop's six icons, their context menu, the Start menu and the one
///   OpenApp(id) entry point (OfficeSceneUIBuilder.Desktop.cs)
///   [DesktopIcons, DesktopIconView, DesktopContextMenu, DesktopShell, DesktopApps]
/// - The traveller wheel (the interview's choices around the traveller), the
///   speech bubble (its answer pickable, above the wheel), the desk tooltip,
///   the fallback HUD, the office case HUD (the claim tag and the office
///   compare strip), the desk view's "▲ Back" control and the stamp tray
///   (Accept and Deny at the desk) on the office overlay canvas
///   [TravellerWheel, OverlayCallout, SpeechBubbleInput, OfficeCaseHud,
///   HoverHint, StampTray]
/// - The Office root: click boxes for the art's props, the physical desk
///   (papers, the scanner, the mat's click), the desk view's camera, the
///   traveller, the READY sign, the readouts and the office's input rules,
///   all put on the art office at load by the binder through the scene
///   contract  [OfficeSceneBinder, DeskController, DeskView, DeskReaction,
///   TravellerView, BoothCoordinator]
/// - GameManager + DaySystem (DayOrchestrator + DayEventDirector), auto-wired to
///   ContentLibrary_Main and a Day Plan
/// - The Tier-2 images' art slots: each shows its file from
///   Assets/Art/UI/Resources/ when delivered, else today's look
///   (OfficeSceneUIBuilder.Art.cs)  [ArtSlotImage]
/// Safe to re-run: finds existing pieces by name and only fills gaps (some
/// overlay and window parts are rebuilt each run). Every UI graphic it makes
/// gets a ThemeTag (piece 6: role, label key, style, fit), which
/// CultureThemeService applies at runtime; an untagged graphic on the two
/// canvases is logged as an error, and so is a text that does not reach its
/// contrast minimum on what it is drawn on in any theme (UiContrastCheck).
/// It opens the gameplay layer alone, builds
/// it, saves it and keeps the build settings in boot order (the title, the
/// art office, this layer, Home: BuildScenes.Order); it
/// refuses while any open scene has unsaved changes. The Office root and the
/// desktop's place are in OfficeSceneUIBuilder.Desk.cs.
/// </summary>
public static partial class OfficeSceneUIBuilder
{
    // Windows XP "Luna" palette
    private static readonly Color XpBlue = new Color(0.13f, 0.34f, 0.86f, 1f);    // taskbar / title-bar base
    private static readonly Color XpGreen = new Color(0.18f, 0.49f, 0.2f, 1f);    // Start button (#2E7D32: white reads on it)
    private static readonly Color XpFace = new Color(0.925f, 0.913f, 0.847f, 1f); // #ECE9D8 control face
    private static readonly Color XpRed = new Color(0.77f, 0.235f, 0.17f, 1f);    // close button (#C43C2C: white reads on it)
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

    /// <summary>The one scene this builder writes: the office's gameplay layer.</summary>
    private const string GameplayScenePath = "Assets/Scenes/OfficeGameplay.unity";

    /// <summary>The gameplay layer's scene name (RunConfig.officeGameplaySceneName).</summary>
    private const string GameplaySceneName = "OfficeGameplay";

    /// <summary>The art office the gameplay layer loads on (the art side's scene; the builder never opens it).</summary>
    private const string ArtScenePath = "Assets/Scenes/OfficeScene.unity";

    /// <summary>The title scene, which a player build boots (first in the build list).</summary>
    private const string TitleScenePath = "Assets/Scenes/TitleScene.unity";

    /// <summary>The Home scene (last of the shipped scenes in the build list).</summary>
    private const string HomeScenePath = "Assets/Scenes/HomeScene.unity";

    /// <summary>
    /// Opens the gameplay layer alone (creating it on the first run), builds and
    /// wires it, saves it and lists it after the art office in the build
    /// settings. Refuses (with an error, changing nothing) in play mode or while
    /// an open scene has unsaved changes.
    /// </summary>
    [MenuItem("Tools/TimeDesk/Build Office UI (HUD + Panels)")]
    public static void Build()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("[TimeDesk] Build Office UI does not run in play mode. Nothing was changed.");
            return;
        }
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            if (SceneManager.GetSceneAt(i).isDirty)
            {
                Debug.LogError($"[TimeDesk] Save or discard the changes in '{SceneManager.GetSceneAt(i).path}' first: Build Office UI opens {GameplayScenePath} alone. Nothing was changed.");
                return;
            }
        }

        EnsureLayer(OfficeLayers.Interactable);
        EnsureLayer(OfficeLayers.PcDesktop);
        Scene scene = File.Exists(GameplayScenePath)
            ? EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Single)
            : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        Canvas canvas = EnsureCanvas();
        Transform root = canvas.transform;
        EnsureEventSystem();
        ContentLibrarySO library = FindAssetByName<ContentLibrarySO>("ContentLibrary_Main") ?? FindFirstAsset<ContentLibrarySO>();

        // --- OfficeUIController (HUD + citation + verdict only; case display is the investigation desk) ---
        OfficeUIController officeUI = Object.FindFirstObjectByType<OfficeUIController>();
        if (officeUI == null)
        {
            Transform existing = root.Find("OfficeUI");
            if (existing != null)
                officeUI = GetOrAdd<OfficeUIController>(existing.gameObject);
            else
            {
                var go = new GameObject("OfficeUI", typeof(RectTransform));
                go.transform.SetParent(root, false);
                FullStretch((RectTransform)go.transform);
                officeUI = go.AddComponent<OfficeUIController>();
            }
        }

        // XP desktop wallpaper (behind everything), the idle line between
        // travellers + taskbar with system-tray HUD.
        GameObject idleScreen = BuildDesktop(root, library);
        BuildTaskbar(root, out TMP_Text dayText, out TMP_Text moneyText, out TMP_Text stabilityText, out TMP_Text trayClockText);

        // The verdict line and the citation slip moved to the office overlay (piece 10): the desktop keeps no copy.
        DestroyChildIfPresent(root, "VerdictStrip");
        DestroyChildIfPresent(root, "VerdictText");
        DestroyChildIfPresent(root, "CitationPanel");

        // Briefing + Results: newsletter panels on the office overlay canvas, so
        // they read over the office, not on the PC's desktop.
        Canvas officeCanvas = EnsureOfficeOverlayCanvas();
        DestroyChildIfPresent(officeCanvas.transform, "BriefingPanel");
        DestroyChildIfPresent(officeCanvas.transform, "ResultsPanel");

        Transform briefing = BuildNewsletter(officeCanvas.transform, "BriefingPanel", "briefing.masthead",
            "briefing.start", out TMP_Text briefingTitle, out TMP_Text briefingBody, out Button startShift);
        Transform results = BuildNewsletter(officeCanvas.transform, "ResultsPanel", "results.masthead",
            "results.goHome", out TMP_Text resultsTitle, out TMP_Text resultsBody, out Button goHome);

        // The desk tuning and the scene contract (created once): screen power, the
        // clone, the papers, the traveller, the wheel; where the art's places are.
        DeskConfigSO deskConfig = EnsureDeskConfig();
        OfficeSceneContractSO contract = EnsureOfficeContract();

        // The office root and the desktop's own place (its cameras and screen).
        OfficeViewController officeView = EnsureOfficeRoot();
        MonitorScreen monitorScreen = BuildPcDesktop(canvas, deskConfig, out Camera frameCamera);

        // Office overlays, rebuilt each run with always-active hosts, above the
        // newsletters, bottom to top: the fallback HUD, the office case HUD (the
        // claim tag and the office compare strip), the desk view's "▲ Back"
        // control (under the case HUD, shown while tilted), the PC frame, the traveller
        // wheel (the interview's choices), the traveller's speech bubble (above
        // the wheel, its answer pickable), the desk props' tooltip, the stamp
        // tray, the verdict line and the citation slip.
        FallbackHud fallbackHud = BuildFallbackHud(officeCanvas.transform);
        OfficeCaseHud caseHud = BuildOfficeCaseHud(officeCanvas.transform, out GameObject officeCompareStrip, out TMP_Text officeCompareText);
        Button deskViewBack = BuildDeskViewBack(officeCanvas.transform);
        PcFrame pcFrame = BuildPcFrame(officeCanvas.transform, frameCamera, officeView, out Image powerLed, out Button framePower);
        OverlayCallout speechBubble = BuildOverlayCallout(officeCanvas.transform, "SpeechBubble", new Vector2(420f, 110f), new Color(0.98f, 0.97f, 0.93f, 0.97f), ThemeRoleId.DiegeticBubble, true);
        TravellerWheel wheel = BuildTravellerWheel(officeCanvas.transform, deskConfig, speechBubble);
        BuildBubbleInput(speechBubble, wheel);
        InteractionPanelController interaction = wheel.transform.Find("Catcher/Ring").GetComponent<InteractionPanelController>();
        OverlayCallout deskTooltip = BuildOverlayCallout(officeCanvas.transform, "DeskTooltip", new Vector2(360f, 60f), Tooltip, ThemeRoleId.Tooltip, false);
        StampTray stampTray = BuildStampTray(officeCanvas.transform, deskConfig);

        // Verdict line (result text) on a strip that shows only while the line has text (piece 6 R18): top centre, the claim tag's place (they never show together).
        DestroyChildIfPresent(officeCanvas.transform, "VerdictStrip");
        Transform verdictStrip = Panel(officeCanvas.transform, "VerdictStrip", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -ClaimStripTop - ClaimStripSize.y / 2f),
                                       ClaimStripSize, ScreenStripColor, ThemeRoleId.ScreenStrip);
        ((RectTransform)verdictStrip).pivot = Center;
        verdictStrip.GetComponent<Image>().raycastTarget = false;
        TMP_Text verdictText = Text(verdictStrip, "VerdictText", "", 26, TextAlignmentOptions.Center, new Vector2(0.02f, 0.04f), new Vector2(0.98f, 0.96f), Color.white,
                                    ThemeRoleId.ScreenStrip, fit: true);
        verdictText.raycastTarget = false;
        verdictStrip.gameObject.SetActive(false);

        // Citation slip (over the office and the frame; it still holds the day until Acknowledge).
        DestroyChildIfPresent(officeCanvas.transform, "CitationPanel");
        Transform citation = Panel(officeCanvas.transform, "CitationPanel", Center, Center, Vector2.zero, new Vector2(560f, 320f), new Color(0.85f, 0.2f, 0.15f, 0.96f), ThemeRoleId.Alert);
        TMP_Text citationText = Text(citation, "CitationText", UiText.Get("citation.title"), 24, TextAlignmentOptions.Center, new Vector2(0.05f, 0.28f), new Vector2(0.95f, 0.95f), Color.white,
                                     ThemeRoleId.Alert);
        Button citationContinue = MakeButton(citation, "ContinueButton", null, new Vector2(0.3f, 0.06f), new Vector2(0.7f, 0.24f), null, ThemeRoleId.Button, "citation.acknowledge");
        citation.gameObject.SetActive(false);

        var soView = new SerializedObject(officeView);
        SetRef(soView, "frame", pcFrame);
        soView.ApplyModifiedProperties();
        var soScreen = new SerializedObject(monitorScreen);
        SetRef(soScreen, "powerLed", powerLed);
        soScreen.ApplyModifiedProperties();

        DayFlowUIController dayFlow = Object.FindFirstObjectByType<DayFlowUIController>();
        if (dayFlow == null)
        {
            var go = new GameObject("DayFlowUI");
            go.transform.SetParent(root, false);
            dayFlow = go.AddComponent<DayFlowUIController>();
        }

        // --- Investigation desk ---
        // Persistent host (never toggled) holds the controllers; InvestigationRoot is the toggled case overlay. The
        // window layer sits on the host above it, never toggled, so a window opened between travellers (an app from
        // its desktop icon) shows too; the compare dock goes above the layer (BuildCompareDock).
        Transform investHost = Panel(root, "InvestigationUI", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        InvestigationUIController invest = GetOrAdd<InvestigationUIController>(investHost.gameObject);
        CompareController compare = GetOrAdd<CompareController>(investHost.gameObject);

        Transform investRoot = EnsureCaseRoot(investHost);
        Transform windowLayer = EnsureWindowLayer(investHost, investRoot);

        // Claim on a translucent XP-blue strip, clear of the desktop's default icon column.
        Panel(investRoot, "ClaimStrip", new Vector2(0.12f, 0.87f), new Vector2(0.94f, 1f), Vector2.zero, Vector2.zero, ScreenStripColor, ThemeRoleId.ClaimStrip);
        TMP_Text claimText = Text(investRoot, "ClaimBanner", "Visitor", 26, TextAlignmentOptions.Center, new Vector2(0.13f, 0.88f), new Vector2(0.93f, 0.99f), Color.white, ThemeRoleId.ClaimStrip);
        SetAnchors(claimText.transform, new Vector2(0.13f, 0.88f), new Vector2(0.93f, 0.99f));

        // The case tiles moved off the desktop (the PC redesign DK7) into the
        // interim Investigation window the Investigation icon opens, until the
        // Investigation app (phase 16): the day's windows, then the reference
        // books and each scanned document the controller adds at runtime.
        DestroyChildIfPresent(investRoot, "BookShelf");
        Transform caseTiles = BuildInvestigationWindow(windowLayer, out DesktopWindow investigationWindow, out Button caseTileTemplate);

        // Directives: a window (closed by default) opened from its case tile.
        DesktopWindow directivesWindow = BuildOSWindow(windowLayer, "DirectivesWindow", "window.directives", null,
            UiText.Get("directives.none"), new Vector2(430f, 360f));
        TMP_Text directivesText = directivesWindow.transform.Find("Body").GetComponent<TMP_Text>();
        BuildCaseTile(caseTiles, "TileDirectives", "icon.directives", directivesWindow);

        // The Deviation Report: its body lists the discrepancies the player has
        // documented for the current case (drives deny gating). Its object names
        // keep "Scanner"; the desk device is the scanner.
        DestroyChildIfPresent(windowLayer, "IconScannerWindow");
        DesktopWindow scannerWindow = BuildOSWindow(windowLayer, "IconScannerWindow", "window.scanner", null,
            UiText.Get("scanner.idle"), new Vector2(560f, 420f));
        TMP_Text scannerText = scannerWindow.transform.Find("Body").GetComponent<TMP_Text>();
        BuildCaseTile(caseTiles, "TileScanner", "icon.scanner", scannerWindow);

        // Citizen Records app: the agency's master record of every (fake)
        // human, its rows listed group by group (OfficeSceneUIBuilder.Records).
        // Registry content is injected per day by GameManager.
        CitizenRecordsWindowController records = BuildRecordsWindow(windowLayer, caseTiles, compare);

        // Case Notes: Interview — the current traveller's transcript. Rebuilt
        // fresh each run (like Records), so its row template always has the
        // transcript layout and the window layer keeps a stable order. Opened by
        // every interview choice but a document request; answer rows are
        // compare-clickable.
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
        DesktopWindow transcriptChrome = transcriptWin.GetComponent<DesktopWindow>();
        transcriptWin.gameObject.SetActive(false);
        BuildCaseTile(caseTiles, "TileClueLog", "icon.clueLog", transcriptChrome);

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

        // The compare dock above the taskbar (the PC redesign DK9): the window
        // layer moves over the claim, the icons and Accept/Deny, the dock over it.
        Transform compareBar = BuildCompareDock(investHost, investRoot, windowLayer, compare, out TMP_Text compareText, out GameObject compareDock);

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
        if (dayPlan == null) Debug.LogWarning("[TimeDesk] No DayPlanSO found — generate content first (Tools > TimeDesk).");

        DayOrchestrator orchestrator = Object.FindFirstObjectByType<DayOrchestrator>();
        if (orchestrator == null) orchestrator = new GameObject("DaySystem").AddComponent<DayOrchestrator>();
        DayEventDirector eventDirector = GetOrAdd<DayEventDirector>(orchestrator.gameObject);

        GameManager gameManager = Object.FindFirstObjectByType<GameManager>();
        if (gameManager == null) gameManager = new GameObject("GameManager").AddComponent<GameManager>();

        // Fake-OS desktop shell: the six apps' windows, their icons, the context
        // menu and the Start menu, and the taskbar's way back to the office.
        BuildDesktopShell(canvas, windowLayer, library, officeView, monitorScreen, investigationWindow, officeView.transform.Find("Desk").GetComponent<DeskController>(),
                          gameManager, directivesWindow);

        // The window stack (every window built above), the taskbar's window buttons and the frame's Escape stamp (the PC redesign WN1-WN3).
        BuildWindowManager(canvas, officeView);

        // Cursor + hover outline settings (a persistent highlighter uses them in every scene).
        BuildInteractionFeedback();

        // Shift clock: driver beside the GameManager (its readouts are on the Office root).
        ShiftClockDriver shiftClock = gameManager.GetComponent<ShiftClockDriver>();
        if (shiftClock == null)
            shiftClock = gameManager.gameObject.AddComponent<ShiftClockDriver>();

        // The Office root: every click box, the desk, the traveller, the readouts,
        // the input rules and the binder that puts them on the art office at load.
        BoothCoordinator booth = BuildOffice(officeView, monitorScreen, framePower, deskConfig, contract, wheel,
                                             new[] { speechBubble, deskTooltip }, deskTooltip, trayClockText, shiftClock, library,
                                             fallbackHud, pcFrame, stampTray, caseHud, deskViewBack, out Clickable readySign);

        // The Tier-2 images' art slots (OfficeSceneUIBuilder.Art.cs, redesign phase 27), before the desktop's layer is applied to its covers.
        BuildArtSlots(officeCanvas.transform, speechBubble, caseTileTemplate, bookTemplate, officeView);

        // The desktop's own layer covers everything under its place (the canvas's windows and templates included).
        SetLayer(monitorScreen.transform, OfficeLayers.PcDesktopLayer);

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
        SetRef(soCompare, "officeBar", officeCompareStrip);
        SetRef(soCompare, "officeText", officeCompareText);
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
        SetRef(soInvest, "compareDock", compareDock);
        SetRef(soInvest, "windowLayer", windowLayer);
        SetRef(soInvest, "documentWindowTemplate", docTemplate);
        SetRef(soInvest, "bookWindowTemplate", bookTemplate);
        SetRef(soInvest, "bookShelfRoot", caseTiles);
        SetRef(soInvest, "bookShelfButtonTemplate", caseTileTemplate);
        SetRef(soInvest, "scannerText", scannerText);
        SetRef(soInvest, "scannerWindow", scannerWindow);
        SetRef(soInvest, "interactionPanel", interaction);
        SetRef(soInvest, "recordsWindow", records);
        SetRef(soInvest, "transcriptWindow", transcript);
        SetRef(soInvest, "transcriptChrome", transcriptChrome);
        SetRef(soInvest, "desk", officeView.transform.Find("Desk").GetComponent<DeskController>());
        SetRef(soInvest, "hud", caseHud);
        SetRef(soInvest, "stampTray", stampTray);
        SetRef(soInvest, "wheel", wheel);
        SetRef(soInvest, "idleScreen", idleScreen);
        // The 4:3 desktop: documents cascade on the left, clear of the icon
        // column; books open in two staggered rows.
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
        SetRef(soGm, "readySign", readySign);
        SetRef(soGm, "shiftClock", shiftClock);
        SetRef(soGm, "travellerView", officeView.transform.Find("Traveller").GetComponent<TravellerView>());
        SetRef(soGm, "booth", booth);
        SetRef(soGm, "desktopConfig", EnsureDesktopConfig());
        soGm.ApplyModifiedProperties();

        OrderDesktopLayers(root);
        CheckThemeTags(canvas, officeCanvas);
        CheckLabelKeysAndRoles(library, canvas, officeCanvas);
        CheckContrast(library, canvas, officeCanvas);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, GameplayScenePath);
        EnsureBuildSettings();
        AssetDatabase.SaveAssets();
        Debug.Log($"[TimeDesk] {GameplayScenePath} built, wired and saved (every UI graphic theme-tagged, the PC frame and the desktop's clone on the office PC with screen power, the desk with papers (their whole face, the passport photo; examined in the hand), scanner and reacting props, the layered traveller + wheel + speech bubble (answers pickable), the office case HUD and the stamp tray, the office's input rules, the binder and its scene contract, HUD, citation and verdict line over the office, briefing/results, claim, document (passport photo) + book windows, interview transcript, compare (the PC dock + office strip), the window manager and the taskbar's window buttons, the six desktop icons, their context menu and the Start menu, Accept/Deny, GameManager, DaySystem). It loads on {ArtScenePath}.");
    }

    // -----------------------------
    // Window builders
    // -----------------------------

    private static DocumentWindowController BuildDocumentWindow(Transform layer)
    {
        // Rebuilt fresh each run: visitor papers read as SCANNED documents —
        // a white page with a photo corner (the traveller's photo on a photo
        // document) on a dark scanner backing — so they never look like just
        // another OS window.
        DestroyChildIfPresent(layer, "DocumentWindowTemplate");
        Transform win = Panel(layer, "DocumentWindowTemplate", Center, Center, Vector2.zero, new Vector2(540f, 440f), new Color(0.13f, 0.14f, 0.17f, 1f), ThemeRoleId.DiegeticBacking);
        WindowShell s = BuildWindowShell(win, null, UiText.Get("document.untitled"), ThemeRoleId.DiegeticBacking, ThemeRoleId.DiegeticRow, false);

        Transform page = Panel(win, "ScanPage", new Vector2(0.025f, 0.115f), new Vector2(0.975f, 0.85f), Vector2.zero, Vector2.zero, new Color(0.97f, 0.96f, 0.92f, 1f), ThemeRoleId.DiegeticPaper);
        page.SetSiblingIndex(1); // render after the header, behind the rows
        Transform photo = Panel(page, "PhotoBox", new Vector2(0.76f, 0.66f), new Vector2(0.96f, 0.96f), Vector2.zero, Vector2.zero, new Color(0.55f, 0.56f, 0.58f, 1f), ThemeRoleId.DiegeticPhoto);
        TravellerPortraitView portrait = BuildPortrait(photo);

        // Footer page label needs light ink on the dark backing.
        s.page.color = new Color(0.85f, 0.86f, 0.88f, 1f);

        DocumentWindowController c = GetOrAdd<DocumentWindowController>(win.gameObject);
        var so = new SerializedObject(c);
        SetRef(so, "titleText", s.title);
        SetRef(so, "pageText", s.page);
        SetRef(so, "prevButton", s.prev);
        SetRef(so, "nextButton", s.next);
        SetRef(so, "fieldRowsRoot", s.rowsRoot);
        SetRef(so, "fieldRowTemplate", s.rowTemplate);
        SetRef(so, "photoBox", photo.gameObject);
        SetRef(so, "photo", portrait);
        so.FindProperty("photoInset").floatValue = PhotoRowInset;
        so.ApplyModifiedProperties();
        win.gameObject.SetActive(false);
        return c;
    }

    /// <summary>Extra right padding of a photo page's rows (px), so none runs under the photo box.</summary>
    private const float PhotoRowInset = 120f;

    /// <summary>
    /// The scanned page's photo: a 4:5 Portrait fitted inside the box, holding
    /// one full-size, non-raycast Image per LookLayer in stack order, wired to
    /// its TravellerPortraitView.
    /// </summary>
    private static TravellerPortraitView BuildPortrait(Transform box)
    {
        Transform portrait = Panel(box, "Portrait", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        AspectRatioFitter fitter = GetOrAdd<AspectRatioFitter>(portrait.gameObject);
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fitter.aspectRatio = LookCanvas.PhotoAspect;
        fitter.enabled = false; // re-enabled: the fitter sizes the rect now (the size Panel reset), as on a fresh build
        fitter.enabled = true;

        var layers = new List<Object>();
        foreach (LookLayer layer in System.Enum.GetValues(typeof(LookLayer)))
        {
            Image image = Panel(portrait, layer.ToString(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.white, ThemeRoleId.DiegeticPhoto).GetComponent<Image>();
            image.raycastTarget = false;
            image.preserveAspect = false;
            image.enabled = false;
            layers.Add(image);
        }

        TravellerPortraitView view = GetOrAdd<TravellerPortraitView>(portrait.gameObject);
        var so = new SerializedObject(view);
        SerializedArrays.Set(so, "layers", layers);
        so.ApplyModifiedProperties();
        return view;
    }

    private static ReferenceBookWindowController BuildBookWindow(Transform layer)
    {
        Transform win = Panel(layer, "BookWindowTemplate", Center, Center, Vector2.zero, new Vector2(540f, 440f), Paper, ThemeRoleId.WindowBody);
        WindowShell s = BuildWindowShell(win, null, UiText.Get("book.untitled"), ThemeRoleId.WindowBody, ThemeRoleId.DiegeticBookRow, true);
        ReferenceBookWindowController c = GetOrAdd<ReferenceBookWindowController>(win.gameObject);
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
        Transform header = Panel(win, "Header", new Vector2(0f, 1f), new Vector2(1f, 1f), TitleBarPos, TitleBarSize, HeaderBar, ThemeRoleId.TitleBar);
        Panel(header, "Gloss", new Vector2(0f, 0.5f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, new Color(1f, 1f, 1f, 0.14f), ThemeRoleId.TitleGloss);
        WindowDrag drag = GetOrAdd<WindowDrag>(header.gameObject);
        var soDrag = new SerializedObject(drag);
        SetRef(soDrag, "windowRoot", (RectTransform)win);
        soDrag.ApplyModifiedProperties();
        TMP_Text title = Text(header, "TitleText", titleSample, TitleFontSize, TextAlignmentOptions.Left, new Vector2(0.04f, 0f), new Vector2(0.76f, 1f), Color.white,
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
    /// <see cref="DesktopSize"/> units (BuildPcDesktop puts it on its own layer,
    /// away from the office), masked to its rect (dragged windows never leave the screen).
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
    /// Scales the office overlay canvas (the newsletters, the PC frame, the
    /// wheel and the callouts) with the screen from the reference resolution, and never
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
        var l = GetOrAdd<VerticalLayoutGroup>(t.gameObject);
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
        var l = GetOrAdd<HorizontalLayoutGroup>(t.gameObject);
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

    /// <summary>
    /// The readability check (UiContrastCheck): every text on the desktop and
    /// the overlay against what it is drawn on, in the neutral theme and each
    /// culture's; the desktop is drawn at the glass's height on a 1080p screen.
    /// </summary>
    private static void CheckContrast(ContentLibrarySO library, Canvas desktop, Canvas overlay)
    {
        if (library == null || library.NeutralTheme == null)
        {
            Debug.LogWarning("[TimeDesk] No content library themes: the readability check is skipped. Run Tools > TimeDesk > Generate World, then build again.");
            return;
        }

        var themes = new List<ThemeSO> { library.NeutralTheme };
        foreach (ThemeSO theme in library.Themes)
            if (theme != null)
                themes.Add(theme);
        UiContrastCheck.Check(desktop, FrameGlass.height / DesktopSize.y, themes, library.CultureUi);
        UiContrastCheck.Check(overlay, 1f, themes, library.CultureUi);
    }

    /// <summary>A transform's scene path.</summary>
    private static string PathOf(Transform t) => t.parent == null ? t.name : PathOf(t.parent) + "/" + t.name;

    /// <summary>The translucent strip behind texts on the wallpaper (verdict and idle lines) and the claim strip.</summary>
    private static readonly Color ScreenStripColor = new Color(0.06f, 0.18f, 0.42f, 0.8f);

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

        // Between travellers the desktop reads this, large enough for the office PC's
        // clone, on a strip so it never reads off the wallpaper (piece 6 R18).
        Transform strip = Panel(root, "IdleScreen", new Vector2(0.05f, 0.38f), new Vector2(0.95f, 0.62f), Vector2.zero, Vector2.zero, ScreenStripColor, ThemeRoleId.ScreenStrip);
        strip.GetComponent<Image>().raycastTarget = false;
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
    /// Overlay canvas for the office's UI (the briefing/results newsletters, the
    /// PC frame, the wheel, the callouts), drawn over the office and the PC
    /// frame's desktop.
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
        Transform bar = Panel(root, "Taskbar", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, TaskbarHeight / 2f), new Vector2(0f, TaskbarHeight), XpBlue, ThemeRoleId.Taskbar);
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
    /// DesktopWindow on the window root. Shared by every window type so the
    /// chrome behaves identically everywhere. Destroys older decorative
    /// controls (pre-chrome builds used plain panels) before rebuilding.
    /// </summary>
    private static DesktopWindow BuildWinControls(Transform win, Transform header)
    {
        DestroyChildIfPresent(header, "MinBtn");
        DestroyChildIfPresent(header, "MaxBtn");
        DestroyChildIfPresent(header, "CloseBtn");

        Button minB = MakeButton(header, "MinBtn", null, new Vector2(0.79f, 0.16f), new Vector2(0.85f, 0.86f), null, ThemeRoleId.Button, "window.minimize");
        Button maxB = MakeButton(header, "MaxBtn", null, new Vector2(0.855f, 0.16f), new Vector2(0.915f, 0.86f), null, ThemeRoleId.Button, "window.maximize");
        Button closeB = MakeButton(header, "CloseBtn", null, new Vector2(0.925f, 0.16f), new Vector2(0.985f, 0.86f), XpRed, ThemeRoleId.CloseButton, "window.close");

        DesktopWindow chrome = win.GetComponent<DesktopWindow>();
        if (chrome == null)
            chrome = win.gameObject.AddComponent<DesktopWindow>();
        var so = new SerializedObject(chrome);
        SetRef(so, "window", (RectTransform)win);
        SetRef(so, "minimizeButton", minB);
        SetRef(so, "maximizeButton", maxB);
        SetRef(so, "closeButton", closeB);
        so.ApplyModifiedProperties();
        return chrome;
    }

    // ----------------------------- Placeholder art -----------------------------

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

    /// <summary>
    /// Returns a placeholder Sprite at Assets/Art/Office/Placeholder/{name}.png drawn
    /// by a pixel function, with a custom pivot. Created once; swap the PNG for final art and keep the .meta (the pivot lives there).
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

    /// <summary>Where generated placeholder cursors live (never mistaken for final art).</summary>
    private const string PlaceholderCursorFolder = "Assets/Art/Generated/Cursors";

    /// <summary>Placeholder arrow outline, in top-left pixel coordinates of a 32x32 cursor.</summary>
    private static readonly (float x, float y)[] ArrowCursorShape =
    {
        (0, 0), (0, 22), (5, 17), (9, 26), (12, 25), (8, 16), (15, 16),
    };

    /// <summary>Placeholder pointing hand (fingertip at 12,1), top-left pixel coordinates.</summary>
    private static readonly (float x, float y)[] HandCursorShape =
    {
        (10, 1), (13, 1), (14, 2), (14, 12), (21, 13), (23, 15), (23, 25), (19, 30),
        (10, 30), (6, 24), (5, 18), (7, 17), (10, 19),
    };

    /// <summary>
    /// Ensures the interaction-feedback settings (cursor art by file name when
    /// present, placeholders otherwise, with click points derived from the art;
    /// the office objects' outline hull material) and assigns them to RunConfig, from which
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
        // The 3D hover outline: the hull material (an older build's sprite material is replaced).
        Material hull = EnsureMaterial("HoverHull", "TimeDesk/HoverHull", null);
        if (settings.outlineMaterial == null || settings.outlineMaterial.shader == null || settings.outlineMaterial.shader.name != "TimeDesk/HoverHull")
            settings.outlineMaterial = hull;
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
    private static Texture2D EnsureCursorTexture(string artName, (float x, float y)[] placeholderShape)
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
    private static Color32 CursorPixel((float x, float y)[] polygon, int x, int y, int size)
    {
        bool Inside(int px, int py) => PixelShapes.InPolygon(polygon, px + 0.5f, (size - 1 - py) + 0.5f);

        if (!Inside(x, y))
            return new Color32(0, 0, 0, 0);
        bool edge = !Inside(x - 1, y) || !Inside(x + 1, y) || !Inside(x, y - 1) || !Inside(x, y + 1);
        return edge ? new Color32(0, 0, 0, 255) : new Color32(255, 255, 255, 255);
    }

    /// <summary>
    /// Builds the fake-OS desktop shell: the six apps' windows, registered in
    /// phase 25's DesktopApps by their DesktopAppIds id (the interim
    /// Investigation window, the Internet browser, Mail with its feed, the
    /// Citizen Account, Notes: OfficeSceneUIBuilder.Apps.cs; Settings in
    /// sections), their icons and the context menu
    /// (OfficeSceneUIBuilder.Desktop.cs), a Start menu (the six apps, Arrange
    /// icons, Turn off screen and Quit game) wired to a DesktopShell on the
    /// canvas, and the taskbar's "&lt; Desk" button (FocusOffice; built here,
    /// after the view exists). The retired Lexicon, Dialect and Material
    /// placeholders and their icons go. <paramref name="rulesWindow"/> is what
    /// Mail's Rules link opens. Idempotent.
    /// </summary>
    private static void BuildDesktopShell(Canvas canvas, Transform windowLayer, ContentLibrarySO library, OfficeViewController view, MonitorScreen screen,
                                          DesktopWindow investigationWindow, DeskController desk, GameManager game, DesktopWindow rulesWindow)
    {
        Transform root = canvas.transform;
        DesktopConfigSO config = EnsureDesktopConfig();

        // The earlier parallel desktop and the retired placeholder apps (the PC redesign DK7) go.
        DestroyChildIfPresent(root, "DesktopWindowLayer");
        DestroyChildIfPresent(windowLayer, "IconLexiconWindow");
        DestroyChildIfPresent(windowLayer, "IconDialectWindow");
        DestroyChildIfPresent(windowLayer, "IconMaterialWindow");
        DestroyChildIfPresent(windowLayer, "IconNotesWindow");

        // Each app's window by its id (DesktopApps.OpenApp): an app's builder returns its window and it is registered here.
        DesktopApps apps = GetOrAdd<DesktopApps>(root.gameObject);
        MailFeed feed = GetOrAdd<MailFeed>(root.gameObject);
        DesktopWindow internet = BuildInternetWindow(windowLayer, library);
        var windows = new Dictionary<string, DesktopWindow>
        {
            { DesktopAppIds.Investigation, investigationWindow },
            { DesktopAppIds.Internet, internet },
            { DesktopAppIds.Mail, BuildMailWindow(windowLayer, config, feed, apps, internet.GetComponent<BrowserWindow>(), rulesWindow, out TMP_Text mailTitle) },
            { DesktopAppIds.CitizenAccount, BuildAccountWindow(windowLayer, config) },
            { DesktopAppIds.Notes, BuildNotesWindow(windowLayer, config) },
            { DesktopAppIds.Settings, BuildSettingsWindow(windowLayer) },
        };
        DesktopIcons icons = BuildDesktopIcons(canvas, windows, desk, feed, out DesktopContextMenu contextMenu);
        WireIconSettings(windows[DesktopAppIds.Settings], icons);

        Transform startMenu = BuildStartMenu(root, apps, out TMP_Text mailEntry, out Button arrangeEntry, out Button screenOffEntry, out Button quitEntry);
        var soFeed = new SerializedObject(feed);
        SetRef(soFeed, "game", game);
        SetRef(soFeed, "startEntryLabel", mailEntry);
        SetRef(soFeed, "windowTitle", mailTitle);
        soFeed.ApplyModifiedProperties();

        // The taskbar's way back to the office (closes the PC frame), next to Start.
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
        SetRef(soShell, "arrangeButton", arrangeEntry);
        SetRef(soShell, "icons", icons);
        SetRef(soShell, "quitButton", quitEntry);
        SetRef(soShell, "screenOffButton", screenOffEntry);
        SetRef(soShell, "monitorScreen", screen);
        soShell.ApplyModifiedProperties();

        // The window manager closes the context menu on a press outside it and on Escape; a press on an icon or the icon layer leaves no window focused.
        var soManager = new SerializedObject(GetOrAdd<DesktopWindowManager>(root.gameObject));
        SetRef(soManager, "shell", shell);
        SetRef(soManager, "contextMenu", contextMenu);
        SerializedArrays.Set(soManager, "emptyDesktop", new Object[] { root.Find("Desktop").GetComponent<Image>(), icons.GetComponent<Image>() });
        soManager.ApplyModifiedProperties();
    }

    /// <summary>
    /// Builds a desktop window with a draggable title bar (its title keyed)
    /// and min/max/close chrome; its body is a keyed static text
    /// (<paramref name="bodyKey"/>) or a sample the controller rewrites.
    /// </summary>
    private static DesktopWindow BuildOSWindow(Transform layer, string name, string titleKey, string bodyKey, string bodySample, Vector2? size = null,
                                                ThemeRoleId bodyRole = ThemeRoleId.WindowBody)
    {
        Transform win = Panel(layer, name, Center, Center, Vector2.zero, size ?? new Vector2(580f, 400f), Paper, bodyRole);

        Transform header = Panel(win, "Header", new Vector2(0f, 1f), new Vector2(1f, 1f), TitleBarPos, TitleBarSize, HeaderBar, ThemeRoleId.TitleBar);
        WindowDrag drag = header.GetComponent<WindowDrag>();
        if (drag == null)
            drag = header.gameObject.AddComponent<WindowDrag>();
        var soDrag = new SerializedObject(drag);
        SetRef(soDrag, "windowRoot", (RectTransform)win);
        soDrag.ApplyModifiedProperties();

        Text(header, "TitleText", null, TitleFontSize, TextAlignmentOptions.Left, new Vector2(0.04f, 0f), new Vector2(0.7f, 1f), Color.white,
             ThemeRoleId.TitleBar, titleKey, FontStyles.Bold, ThemeTextKind.Heading, true);

        TMP_Text bodyText = Text(win, "Body", bodySample, 20, TextAlignmentOptions.TopLeft, new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.82f), Ink, bodyRole, bodyKey);
        bodyText.textWrappingMode = TextWrappingModes.Normal;
        bodyText.overflowMode = TextOverflowModes.Truncate;
        if (win.GetComponent<RectMask2D>() == null)
            win.gameObject.AddComponent<RectMask2D>(); // nothing bleeds outside the window

        DesktopWindow chrome = BuildWinControls(win, header);

        win.gameObject.SetActive(false); // opened by its icon
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

    /// <summary>Largest font size of a case tile's label.</summary>
    private const float IconLabelMaxSize = 18f;

    /// <summary>Smallest font size a desktop icon's or a case tile's label shrinks to.</summary>
    private const float IconLabelMinSize = 10f;

    /// <summary>
    /// Lets a case tile's label shrink to fit its tile, wrapping only between
    /// words: auto-sizing shrinks a word that does not fit the tile's width
    /// instead of breaking it. Re-applied on every build (MakeButton keeps an
    /// existing label as it is; the label text is set by BuildCaseTile).
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
