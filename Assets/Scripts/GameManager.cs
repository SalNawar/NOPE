using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Connects DayOrchestrator -> CaseFactory -> the investigation UI.
/// Generates cases at day start, displays the active case on UI,
/// resolves the player's Accept/Deny decision, then advances the day. The
/// cheat menu's entry points are in GameManager.Cheats.cs.
/// </summary>
public sealed partial class GameManager : MonoBehaviour
{
    /// <summary>Controls day timeline (case slots + scheduled events).</summary>
    [SerializeField] private DayOrchestrator orchestrator;

    /// <summary>Content library used by CaseFactory (eras, clues, etc.).</summary>
    [SerializeField] private ContentLibrarySO contentLibrary;

    /// <summary>Day plan to run.</summary>
    [SerializeField] private DayPlanSO dayPlan;

    /// <summary>UI controller for the HUD and the verdict line.</summary>
    [SerializeField] private OfficeUIController officeUI;

    /// <summary>
    /// Investigation UI (documents/books/compare + the stamps' verdict): the one case
    /// loop (audit R3-011: the legacy era-pick loop is gone). Without it the
    /// office logs an error and shows no case (audit R4-002).
    /// </summary>
    [SerializeField] private InvestigationUIController investigationUI;

    /// <summary>Briefing + end-of-day panels (optional; flow skips if unassigned).</summary>
    [SerializeField] private DayFlowUIController dayFlowUI;

    /// <summary>Optional: closes the PC frame when a traveller is called and for the shift report.</summary>
    [SerializeField] private OfficeViewController officeView;

    /// <summary>Optional: the AVAILABLE sign (the art's NEXT sign): a click turns the desk available or pauses it (DeskAvailability). Without it each traveller is shown as their slot starts.</summary>
    [SerializeField] private Clickable readySign;

    /// <summary>Scene clock for today's shift (optional: without it the day ends only when the queue is empty).</summary>
    [SerializeField] private ShiftClockDriver shiftClock;

    /// <summary>Optional: the booth figure (the traveller's layered look), from presentation until the decision.</summary>
    [SerializeField] private TravellerView travellerView;

    /// <summary>Optional: the booth's input and wake rules.</summary>
    [SerializeField] private BoothCoordinator booth;

    /// <summary>Optional: the desk's guide (day 1's FTUE, the guided days' new rule, the rulebook's GUIDE pages).</summary>
    [SerializeField] private GuideDirector guide;

    /// <summary>The desktop's knobs: how many morning papers the News site keeps (the builder wires it).</summary>
    [SerializeField] private DesktopConfigSO desktopConfig;

    /// <summary>Seed for deterministic day schedule randomness.</summary>
    [SerializeField] private int seed = 12345;

    /// <summary>Runtime world state for this session.</summary>
    private WorldState _worldState;

    /// <summary>Builds case runtime objects from ScriptableObjects.</summary>
    private CaseFactory _caseFactory;

    /// <summary>Today's places and facts, history applied: one snapshot for the factory and the books.</summary>
    private TodaysWorld _today;

    /// <summary>Generated cases for the day (0-based indexing).</summary>
    private List<CaseInstance> _dayCases;

    /// <summary>Currently active case slot (1-based).</summary>
    private int _activeCaseIndex1Based;

    /// <summary>The case of the active slot (the traveller at the desk or waiting for the AVAILABLE sign), or null between days; the debug panel shows its voice.</summary>
    public CaseInstance ActiveCase => _dayCases != null && _activeCaseIndex1Based >= 1 && _activeCaseIndex1Based <= _dayCases.Count ? _dayCases[_activeCaseIndex1Based - 1] : null;

    /// <summary>Verdict record for the current shift (results screen reads this).</summary>
    private ShiftLedger _ledger;

    /// <summary>The AVAILABLE sign's state: whether travellers are called one after another, and who waits (Saleh 2026-09-30).</summary>
    private readonly DeskAvailability _desk = new DeskAvailability();

    /// <summary>The desk's availability (the AVAILABLE sign lights up while it is on; the sign's link reads it).</summary>
    public DeskAvailability Desk => _desk;

    /// <summary>True from presenting a traveller until the player's decision (closing-time rule).</summary>
    private bool _travellerAtDesk;

    /// <summary>Gameplay tuning, pulled from RunConfig (null-safe).</summary>
    private GameConfigSO _gameConfig;

    /// <summary>Character art for the booth figure and the passport photos (only the current traveller's is kept).</summary>
    private CharacterArt _characterArt;

    /// <summary>Read-only access to the current shift's ledger.</summary>
    public ShiftLedger Ledger => _ledger;

    /// <summary>Today's portals (TodaysWorld.Portals, the portals spec v3 RT3), fixed at the day's start; none before it: the board, the rings and the Portals app read it.</summary>
    public PortalDay Portals => _today != null ? _today.Portals : PortalDay.None;

    /// <summary>True once the Departure Board is introduced (the desk-first ramp: the second destination, day 3; Feature.Board): before it, the board prints nothing and the rulebook alone names the open destination.</summary>
    public bool BoardIntroduced => contentLibrary != null && _worldState != null && contentLibrary.Introductions.Has(_worldState.day, Feature.Board);

    /// <summary>Raised when an accepted traveller leaves, with the portal they leave through (PortalDay.DepartureFor; the hall's rings pulse it, VX4).</summary>
    public event System.Action<int> Departed;

    /// <summary>Raised when a citation is issued (Mail's citation notice arrives then; redesign phase 25).</summary>
    public event System.Action<CaseVerdict> CitationIssued;

    /// <summary>Raised when a citation lands on the desk (the Helix River's red pulse runs then).</summary>
    public event System.Action<CaseVerdict> CitationLanded;

    /// <summary>Raised once a traveller is decided and scored (the game feel reacts: FeelDirector): the case, the verdict, whether they were accepted and the stability before the decision (after is the world's).</summary>
    public event System.Action<CaseInstance, CaseVerdict, bool, float> Resolved;

    /// <summary>The game's tuning (null when RunConfig has none).</summary>
    public GameConfigSO Config => _gameConfig;

    /// <summary>The run's world (stability, day).</summary>
    public WorldState World => _worldState;

    /// <summary>
    /// Initializes systems, generates cases once, and starts the day loop.
    /// </summary>
    private void Start()
    {
        Debug.Log("[GameManager] >>> Entering Start.");

        // The art office still holds a leftover copy of this manager from before
        // the gameplay moved into its own scene: that copy never runs a shift.
        if (OfficeScenes.IsArtOffice(gameObject.scene))
        {
            enabled = false;
            return;
        }

        if (orchestrator == null || contentLibrary == null || officeUI == null)
        {
            Debug.LogError("GameManager missing references (orchestrator/contentLibrary/officeUI).");
            return;
        }
        Current = this;

        // Acquire the run (creates RunManager + loads save/new run on first scene).
        RunManager run = RunManager.GetOrCreate();

        // The run exists now: theme the office for the present culture before the briefing shows.
        CultureThemeService.RefreshActive();

        if (run != null)
        {
            _worldState = run.World;

            // Prefer the library's plan for the current day (or its latest
            // earlier plan); keep the inspector value as a fallback so test
            // scenes still work.
            DayPlanSO planForToday = run.GetCurrentDayPlan();
            if (planForToday != null)
                dayPlan = planForToday;

            seed = run.GetDaySeed();

            if (_worldState.phase == RunPhase.Home)
                Debug.LogWarning($"[GameManager] The saved run already finished day {_worldState.day}'s shift; replaying it because the Office scene was opened directly. Continue from the Title resumes at Home.");
        }
        else
        {
            // No RunConfig in Resources: degrade to a local, session-only state.
            _worldState = new WorldState { day = dayPlan != null ? dayPlan.DayNumber : 1 };
        }

        if (dayPlan == null)
        {
            Debug.LogError($"GameManager has no DayPlan for day {_worldState.day} (none in ContentLibrary, none assigned in inspector).");
            return;
        }

        // Tuning config (citations/pay disabled gracefully if missing).
        _gameConfig = run != null && run.Config != null ? run.Config.gameConfig : null;

        if (_gameConfig == null)
            Debug.LogWarning("GameManager: no GameConfigSO assigned in RunConfig — pay/citations/stability will not be applied.");

        // Shift clock (Papers, Please-style closing time).
        if (shiftClock != null)
        {
            shiftClock.Configure(_gameConfig, dayPlan);
            shiftClock.Closed += HandleShiftClosed;
        }

        // The game feel's director (hit-stop, camera impulses, the shift's bells, the hall's ambience).
        FeelDirector.Attach(this, shiftClock);

        // A waiver signed from the desk's pad costs shift time (the endings and strandings spec §7.3, Q15 = A).
        if (investigationUI != null)
            investigationUI.WaiverSigned += HandleWaiverSigned;
        else
        {
            Debug.LogWarning("GameManager: no ShiftClockDriver wired, so the day ends only when the queue empties (no closing time). Run Tools > TimeDesk > Build Office UI.");
        }

        // Fresh ledger for this shift.
        _ledger = new ShiftLedger();

        // Today's world (places and facts, history applied): one snapshot shared
        // by the case factory and the books, so papers and reference books can
        // never disagree during the day.
        _today = contentLibrary.BuildToday(dayPlan, _worldState);
        _caseFactory = new CaseFactory(contentLibrary, _today);
        _characterArt = new CharacterArt(contentLibrary);

        // Today's interview, fixed at day start: the askable questions, which of
        // them may carry a spoken tell, and the offered dialogs.
        InterviewDay interview = BuildInterviewDay();

        // Generate all cases up-front (seeded: same run + same day + same met
        // premades + same interview wiring = same travellers). Where nothing
        // spoken can be read, no answer is computed and no tell is spoken;
        // where no garment can be looked at, no dress tell is generated.
        bool spoken = investigationUI != null && investigationUI.InterviewReachable;
        bool dress = investigationUI != null && investigationUI.AppearanceReachable;
        _dayCases = _caseFactory.GenerateDayCases(dayPlan, _worldState, seed, spoken ? interview : null, dress);
        Debug.Log($"[GameManager] Interview: spoken={spoken}, dress={dress}, askable=[{string.Join(", ", interview.AskableCategories)}], spoken tells may come from [{string.Join(", ", interview.AnswerTellCategories)}], dialogs offered={interview.OfferedDialogs(null).Count}.");

        // Investigation: surface today's travel directives (rules to deny), the
        // agency's citizen records for today's visitors, today's facts and interview.
        if (investigationUI != null)
        {
            investigationUI.SetDirectives(dayPlan.ActiveTravelRules);
            investigationUI.SetCitizenRegistry(BuildRegistry(), contentLibrary.Agency, _worldState.day);
            investigationUI.SetFacts(_today.Facts);
            investigationUI.SetInterviewDay(interview);
            investigationUI.SetCharacterArt(_characterArt);

            // Today's translation, fixed at day start like the interview (a translator bought tonight counts tomorrow).
            if (!contentLibrary.Translation.HasData)
                Debug.LogWarning("[GameManager] The content library has no translation data: every tongue reads as English. Run Tools > TimeDesk > Generate World.");
            investigationUI.SetTranslation(TimelineService.BuildTranslationDay(contentLibrary, _worldState), contentLibrary.Translation);
        }

        // Initial HUD state.
        officeUI.UpdateHud(_worldState);

        // Subscribe to orchestrator callbacks.
        orchestrator.OnCaseSlotStarted += HandleCaseSlotStarted;
        orchestrator.OnCaseSlotEnded += HandleCaseSlotEnded;
        orchestrator.OnDayCompleted += HandleDayCompleted;

        // The AVAILABLE sign toggles the desk (only meaningful when wired): while it is on, each waiting traveller is called as the desk frees up.
        // Its first press opens the shift: the clock starts then, not at Start Shift (Saleh 2026-10-07).
        _desk.Called += CallTraveller;
        _desk.Opened += OpenShift;
        if (readySign != null)
        {
            readySign.Interactable = false;
            readySign.onClick.AddListener(ToggleAvailable);
        }

        // The booth's day (its day-1 notes; the scanner upgrades fixed at day start, like the translation, and the scanner itself
        // only once it is introduced) and phase: the briefing comes first. The PC shows what the day has introduced.
        ScannerDay scanners = TimelineService.BuildScannerDay(_worldState, contentLibrary);
        if (booth != null)
            booth.BeginDay(_worldState.day, scanners);
        if (investigationUI != null)
            investigationUI.SetIntroductions(contentLibrary.Introductions, _worldState.day);

        Debug.Log($"[GameManager] Day {_worldState.day} starting: seed={seed}, money={_worldState.money}, stability={_worldState.timelineStability:0.00}, cases={_dayCases.Count}, places={_today.Places.Count}, leader='{_worldState.history.leaderId}'.");

        // The day's bulletin, with the new desk hours when they changed overnight (night shifts: the hours grow).
        string bulletin = TodaysBulletin();

        // The morning paper is printed: its lines go to the News site's back issues (the night rebuilds them, so they are kept now).
        // The paper is the world's: the clerk's bulletin is the Bureau memo's, never a back issue's.
        if (desktopConfig != null)
            NewsArchive.Record(_worldState.newsArchive, _worldState.day, _worldState.tomorrow.briefingLines, _worldState.tomorrow.newsLines, desktopConfig.newsArchiveIssues,
                               _worldState.tomorrow.deskLines);
        else
            Debug.LogWarning("[GameManager] No DesktopConfigSO wired: today's paper is not kept for the News site. Run Tools > TimeDesk > Build Office UI.");

        // Morning briefing first (if wired), then the day loop.
        if (dayFlowUI != null)
        {
            DayPlanSO planToRun = dayPlan;
            int seedToUse = seed;
            Debug.Log("[GameManager] <<< Exiting Start (showing morning briefing before day loop).");
            if (booth != null)
                booth.SetPhase(BoothPhase.Newsletter);
            dayFlowUI.ShowBriefing(_worldState, bulletin, () => BeginShift(planToRun, seedToUse));
        }
        else
        {
            Debug.Log("[GameManager] <<< Exiting Start (starting day loop directly).");
            BeginShift(dayPlan, seed);
        }
    }

    /// <summary>
    /// Today's bulletin: the plan's line, then, when today's desk hours differ
    /// from yesterday's (ShiftHours.Announces; the night shifts' days 8 and 12),
    /// the line naming the new hours (UI string briefing.newHours).
    /// </summary>
    private string TodaysBulletin()
    {
        string bulletin = dayPlan.Bulletin.Trim();
        ShiftHours today = dayPlan.Shift(_gameConfig);
        DayPlanSO yesterdayPlan = _worldState.day > 1 ? contentLibrary.GetDayPlan(_worldState.day - 1) : null;
        if (!ShiftHours.Announces(yesterdayPlan != null ? yesterdayPlan.Shift(_gameConfig) : (ShiftHours?)null, today))
            return bulletin;
        string hours = UiText.Format("briefing.newHours", today.Open, today.Close);
        return string.IsNullOrEmpty(bulletin) ? hours : bulletin + " " + hours;
    }

    /// <summary>
    /// The day's Citizen Records (traveller types R1): each traveller's record
    /// (CaseFactory.BuildRegistry), then the clerk's own account as the Citizen
    /// Account app shows it this morning (AccountRecords.Clerk over
    /// ClerkAccountSource: no row is evidence), found by TMW-773.
    /// </summary>
    private CitizenRegistry BuildRegistry()
    {
        CitizenRegistry registry = CaseFactory.BuildRegistry(_dayCases, contentLibrary.Introductions.Has(_worldState.day, Feature.Standing), _worldState.day);
        var clerk = new ClerkAccountSource(_worldState, contentLibrary);
        registry.Add(AccountRecords.Clerk(clerk.Profile, Account.ExtractRows(clerk, UiText.Get, AccountMaker.Credits)));
        return registry;
    }

    /// <summary>
    /// Today's interview from the content library and the day-start world
    /// (TimelineService.BuildInterviewDay; InterviewDay decides what is askable
    /// and offered). Logs every structurally broken dialog as an error.
    /// </summary>
    private InterviewDay BuildInterviewDay()
    {
        InterviewDay interview = TimelineService.BuildInterviewDay(contentLibrary, _worldState, _ledger);
        foreach (string problem in interview.ContentProblems)
            Debug.LogError($"[GameManager] {problem} Run Tools > TimeDesk > Generate World, then Validate Content Library.");
        return interview;
    }

    /// <summary>A waiver signed from the desk's pad: the shift clock spends its minutes (GameConfigSO.waiverSignMinutes; time only, no money).</summary>
    private void HandleWaiverSigned()
    {
        float minutes = _gameConfig != null ? _gameConfig.waiverSignMinutes : 0f;
        if (shiftClock != null)
            shiftClock.Spend(minutes);
        Debug.Log($"[GameManager] A waiver was signed from the desk's pad and filed: the shift spends {minutes:0.#} minute(s).");
    }

    /// <summary>
    /// Unsubscribes to prevent event leaks on scene unload / play mode exit.
    /// </summary>
    private void OnDestroy()
    {
        if (Current == this)
            Current = null;
        if (shiftClock != null)
            shiftClock.Closed -= HandleShiftClosed;
        if (investigationUI != null)
            investigationUI.WaiverSigned -= HandleWaiverSigned;

        _characterArt?.Dispose();

        if (orchestrator == null)
            return;

        orchestrator.OnCaseSlotStarted -= HandleCaseSlotStarted;
        orchestrator.OnCaseSlotEnded -= HandleCaseSlotEnded;
        orchestrator.OnDayCompleted -= HandleDayCompleted;
    }

    /// <summary>
    /// Called when the last case of the day resolves: closes the desk and ends
    /// the last traveller's reaction (no bubble over the report), applies the narrative
    /// dialogs' consequences (then refreshes the HUD and checks endings), saves
    /// the run, shows the shift report and leads into the Home scene (or the
    /// title scene when an ending was reached).
    /// </summary>
    private void HandleDayCompleted()
    {
        Debug.Log($"[GameManager] >>> Entering HandleDayCompleted (day {_worldState.day}).");

        // The booth is shut: freeze the clock (the queue may have run out before closing) and close the desk; an FTUE still open is over (saved below).
        _travellerAtDesk = false;
        _shiftRunning = false;
        if (guide != null)
            guide.EndShift();
        CloseDesk();

        // The last traveller's reaction ends as the booth shuts: the figure leaves and their bubble hides, so it never sits over the shift report.
        if (travellerView != null)
            travellerView.EndLinger();
        EndReaction();
        if (shiftClock != null)
            shiftClock.StopShift();
        _characterArt?.Retain(null);

        int correctCount = 0;
        int totalPay = 0;
        int totalPenalty = 0;

        if (_ledger != null)
        {
            foreach (CaseVerdict v in _ledger.verdicts)
            {
                if (v.correct)
                    correctCount++;

                totalPay += v.payAwarded;
                totalPenalty += v.moneyPenalty;
            }
        }

        int totalCases = _ledger != null ? _ledger.verdicts.Count : 0;
        Debug.Log($"[GameManager] Day {_worldState.day} shift complete: {correctCount}/{totalCases} correct, totalPay={totalPay}, totalPenalty={totalPenalty}, money={_worldState.money}, stability={_worldState.timelineStability:0.00}.");

        // The strandings among the accepted travellers (their fates: carries,
        // tremors, the paper's lines and the stranding fine where no valid signed
        // waiver was on file; the endings and strandings spec §6-§7), the
        // clerk's Debt Relief instalment out of the shift's pay (phase 13) and
        // the narrative dialogs' consequences all apply before the save
        // (DayCycle.CloseShift), so a Continue replay of this day can never apply
        // them twice. A stranding's fine or tremor, the instalment and the dialogs
        // may move the wallet or stability, so the ending check runs after them.
        EndingSO ending = null;
        if (DayCycle.CloseShift(_worldState, _ledger, _dayCases, _today, contentLibrary, _gameConfig))
        {
            if (officeUI != null)
                officeUI.UpdateHud(_worldState);

            if (_gameConfig != null)
            {
                ending = EndingService.Evaluate(_worldState, contentLibrary, _gameConfig, EndingMoment.Immediate);
                if (ending != null)
                {
                    Debug.Log($"[GameManager] Ending check after the instalment and dialog consequences: matched '{ending.id}' ({ending.displayName}).");
                    _worldState.endingId = ending.id;
                }
            }
        }

        // The clerk's statement gets the day's row, the instalment included (the Citizen Account; redesign phases 25 and 13).
        ClerkAccountSource.RecordShift(_worldState, _ledger, contentLibrary, _gameConfig);

        // Continue from this save resumes at Home, never replaying this shift.
        _worldState.phase = RunPhase.Home;

        if (RunManager.HasInstance)
        {
            // Yesterday's slot modifiers were consumed by today's shift.
            RunManager.Instance.ResetTomorrowModifiers();
            RunManager.Instance.SaveNow();
        }

        System.Action next = ending != null ? new System.Action(HandleEndingReached) : HandleGoHome;

        // Show the shift report, then hand off to the home phase (or the title
        // scene after an ending). The report is read in the booth (like the
        // morning briefing), so pull back first.
        if (dayFlowUI != null)
        {
            if (officeView != null)
                officeView.FocusOffice();

            Debug.Log($"[GameManager] <<< Exiting HandleDayCompleted (showing results panel, then {(ending != null ? "the title scene" : "Home")}).");
            if (booth != null)
                booth.SetPhase(BoothPhase.Newsletter);
            // The report's money at a glance (lesson 5): tonight's bills are Home's own fixed bill (HomeEconomy.DailyExpenses; a break-in is
            // never foretold) and the pet's essentials (food, heating, electricity: HomeEconomy.EssentialsPrice; the Home pet spec PS9).
            int bills = HomeEconomy.DailyExpenses(_worldState, contentLibrary, _gameConfig, 0).total + HomeEconomy.EssentialsPrice(_worldState, contentLibrary);
            dayFlowUI.ShowResults(_worldState, _ledger, ShiftReport.From(_ledger, _dayCases != null ? _dayCases.Count : 0, _worldState.money, bills),
                                  shiftClock != null ? shiftClock.Hours : dayPlan.Shift(_gameConfig), next);
        }
        else
        {
            if (officeUI != null)
                officeUI.SetResultText(UiText.Format("day.complete", _worldState.day));

            Debug.Log($"[GameManager] <<< Exiting HandleDayCompleted (no results panel, going to {(ending != null ? "the title scene" : "Home")} directly).");
            next();
        }
    }

    /// <summary>
    /// Leaves the office: Home scene if available, otherwise next day directly.
    /// Without a RunManager (standalone test scene), the day simply ends.
    /// </summary>
    private void HandleGoHome()
    {
        Debug.Log("[GameManager] >>> Entering HandleGoHome.");

        if (RunManager.HasInstance)
        {
            Debug.Log("[GameManager] <<< Exiting HandleGoHome (RunManager.GoHomeOrAdvance).");
            RunManager.Instance.GoHomeOrAdvance();
        }
        else
        {
            Debug.Log("[GameManager] <<< Exiting HandleGoHome — no RunManager, day ended (standalone test scene).");
        }
    }

    /// <summary>
    /// Called when the orchestrator starts case slot #N (1-based).
    /// Displays the generated case in the UI.
    /// </summary>
    private void HandleCaseSlotStarted(int caseIndex1Based)
    {
        Debug.Log($"[GameManager] >>> Entering HandleCaseSlotStarted (slot {caseIndex1Based}/{_dayCases?.Count ?? 0}).");

        _activeCaseIndex1Based = caseIndex1Based;

        int idx = caseIndex1Based - 1;

        if (_dayCases == null || idx < 0 || idx >= _dayCases.Count)
        {
            Debug.LogError($"GameManager could not find case for slot {caseIndex1Based}.");
            orchestrator.MarkCaseResolved();
            return;
        }

        CaseInstance inst = _dayCases[idx];

        string claimedEraId = inst.claimedEra != null ? inst.claimedEra.id : string.Empty;
        string archetypeName = inst.archetype != null ? inst.archetype.displayName : string.Empty;
        string nationName = inst.claimedNation != null ? inst.claimedNation.displayName : string.Empty;
        Debug.Log($"[GameManager] Case {caseIndex1Based}: visitor='{inst.visitorDisplayName}', claim='{inst.originLabel}', claimedEra='{claimedEraId}', archetype='{archetypeName}', nation='{nationName}', legendary={inst.isLegendary}, liar={inst.IsLiar}, home='{inst.HomeLabel}', gender={inst.gender}, documents={inst.documents.Count}.");

        // Clear the previous case's verdict line before showing the new case.
        if (officeUI != null)
            officeUI.SetResultText(string.Empty);

        // The traveller waits in the queue: called at once while the desk is
        // available and free, else when the AVAILABLE sign is clicked (or the
        // last traveller has left). With no sign wired, show immediately.
        if (readySign != null)
            _desk.Arm();
        else
            ShowActiveCase(inst);

        Debug.Log($"[GameManager] <<< Exiting HandleCaseSlotStarted (slot {caseIndex1Based}, awaiting player decision).");
    }

    /// <summary>Starts the day loop (after the briefing); the AVAILABLE sign takes clicks from here (the desk starts paused) and its first press starts the shift clock (OpenShift; Saleh 2026-10-07: "shift should not start until you press AVAILABLE"); without a sign the clock starts at once.</summary>
    private void BeginShift(DayPlanSO plan, int daySeed)
    {
        if (booth != null)
            booth.SetPhase(BoothPhase.NoTraveller);
        if (readySign != null)
            readySign.Interactable = true;

        orchestrator.StartDay(_worldState, plan, daySeed, _dayCases);

        if (readySign == null)
            OpenShift();

        // The desk's guide: day 1's FTUE, a guided day's new page opened in the rulebook, the GUIDE's pages so far.
        if (guide != null)
            guide.BeginShift(_worldState.day, _worldState.guide, contentLibrary);

        // The cheat menu's "Auto-decide" keeps the queue coming from the shift's start.
        _shiftRunning = true;
        StartAutoDecide();
    }

    /// <summary>
    /// Closing time: the desk closes (the AVAILABLE sign goes off and inert, and
    /// a traveller still waiting in the queue is never called); a traveller
    /// already at the desk may be finished, otherwise the booth closes at once.
    /// </summary>
    private void HandleShiftClosed()
    {
        ClosingAction action = ShiftFlow.OnClosing(_travellerAtDesk);
        Debug.Log($"[GameManager] Closing time (travellerAtDesk={_travellerAtDesk}, waiting={_desk.IsWaiting}) -> {action}.");
        CloseDesk();

        if (action == ClosingAction.FinishCurrent)
            orchestrator.CloseAfterCurrentSlot();
        else
            orchestrator.CloseNow();
    }

    /// <summary>The shift opens (the AVAILABLE sign's first press, DeskAvailability.Opened; at once without a sign): the shift clock starts from the opening hour, and with it what it times (the hall's light, closing time, the last-hour alarm).</summary>
    private void OpenShift()
    {
        if (shiftClock != null)
            shiftClock.StartShift();
        Debug.Log($"[GameManager] Shift started at {ShiftClock.Format(shiftClock != null ? shiftClock.MinuteOfDay : 0f)} on AVAILABLE (day {_worldState.day}).");
    }

    /// <summary>The AVAILABLE sign's click: turns the desk available (the waiting traveller is called once the desk is free) or pauses it (the traveller at the desk is finished normally; the shift clock keeps running).</summary>
    private void ToggleAvailable()
    {
        _desk.Toggle();
        Debug.Log($"[GameManager] Desk {(_desk.IsAvailable ? "available" : "paused")} (waiting={_desk.IsWaiting}, departing={_desk.IsDeparting}, travellerAtDesk={_travellerAtDesk}).");
    }

    /// <summary>The shift is over (closing time or the day's end): nobody else is called and the AVAILABLE sign goes off and takes no clicks.</summary>
    private void CloseDesk()
    {
        _desk.Close();
        if (readySign != null)
            readySign.Interactable = false;
    }

    /// <summary>The desk calls the waiting traveller (DeskAvailability.Called): the PC frame closes, so the arrival is seen, and the active slot's case is shown.</summary>
    private void CallTraveller()
    {
        int idx = _activeCaseIndex1Based - 1;
        if (_dayCases == null || idx < 0 || idx >= _dayCases.Count)
            return;

        if (officeView != null)
            officeView.FocusOffice();
        ShowActiveCase(_dayCases[idx]);
    }

    /// <summary>
    /// The one presence transition: whether a traveller is at the desk (and
    /// how they look) feeds the closing-time rule, the booth figure and the
    /// booth's input phase.
    /// </summary>
    private void SetTravellerAtDesk(bool at, TravellerLook look = null, bool keepFigure = false)
    {
        _travellerAtDesk = at;

        if (travellerView != null)
        {
            if (at)
                travellerView.Show(look, _characterArt);
            else if (!keepFigure)
                travellerView.Clear();
        }

        if (booth != null)
            booth.SetPhase(at ? BoothPhase.TravellerAtDesk : BoothPhase.NoTraveller);
    }

    /// <summary>
    /// Presents a case via the investigation UI: keeps only this traveller's
    /// art, shows them in the booth, and marks a once-per-run premade as met
    /// (DayCycle.Present: they never come back this run). Without the
    /// investigation UI no case can be shown: an error, and the slot resolves.
    /// </summary>
    private void ShowActiveCase(CaseInstance inst)
    {
        // Calling the next traveller ends the last one's linger at once (R4).
        if (travellerView != null)
            travellerView.EndLinger();
        EndReaction();
        _characterArt?.Retain(inst.look != null ? inst.look.Keys : null);
        SetTravellerAtDesk(true, inst.look);

        DayCycle.Present(_worldState, inst);

        if (investigationUI == null)
        {
            Debug.LogError("[GameManager] No InvestigationUIController is wired, so no case can be shown. Run Tools > TimeDesk > Build Office UI.");
            SetTravellerAtDesk(false);
            orchestrator.MarkCaseResolved();
            return;
        }

        investigationUI.ShowCase(inst, contentLibrary, HandleDecision);
        if (guide != null)
            guide.CaseShown(inst);
        DecideOnArrivalIfCheated();
    }

    /// <summary>
    /// Called when the orchestrator ends case slot #N (1-based).
    /// </summary>
    private void HandleCaseSlotEnded(int caseIndex1Based)
    {
        Debug.Log($"[GameManager] >>> Entering HandleCaseSlotEnded (slot {caseIndex1Based}).");
        Debug.Log($"[GameManager] <<< Exiting HandleCaseSlotEnded (slot {caseIndex1Based}).");
    }

    /// <summary>
    /// Resolves the player's decision, one of the three verdicts committed by
    /// the desk's hardware (the one decision handler, audit R3-017; the desk
    /// machine spec §2): scores it, dispatches timeline impacts only on
    /// accept, checks for an ending, then shows the verdict and advances the day.
    /// </summary>
    private void HandleDecision(DeskStamp decision)
    {
        bool accepted = decision == DeskStamp.Approved;
        Debug.Log($"[GameManager] >>> Entering HandleDecision (slot {_activeCaseIndex1Based}, verdict={decision}).");
        // The booth has no traveller from here (the wheel cannot open); the figure stays for their reaction (R4). The papers are handed back: the guide hears it.
        SetTravellerAtDesk(false, keepFigure: true);
        if (guide != null)
            guide.CaseDecided();

        int idx = _activeCaseIndex1Based - 1;

        if (_dayCases == null || idx < 0 || idx >= _dayCases.Count)
        {
            if (travellerView != null)
                travellerView.Clear();
            Debug.LogError("Player decided but the active case index is invalid.");
            orchestrator.MarkCaseResolved();
            return;
        }

        CaseInstance inst = _dayCases[idx];

        // No tuning config: keep a simple correct/wrong readout.
        if (_gameConfig == null)
        {
            bool simpleCorrect = accepted == inst.ShouldAccept;
            if (officeUI != null)
                officeUI.SetResultText(UiText.Get(simpleCorrect ? "verdict.simpleCorrect" : "verdict.simpleWrong"));
            React(inst, accepted);
            Debug.Log($"[GameManager] <<< Exiting HandleDecision (no GameConfig, simpleCorrect={simpleCorrect}).");
            orchestrator.MarkCaseResolved();
            return;
        }

        float stabilityBefore = _worldState.timelineStability;
        int moneyBefore = _worldState.money;

        // Evidence documented in the scanner gates deny decisions (-1 = the
        // evidence system is not active in this scene, gate skipped).
        int evidenceCount = investigationUI != null && investigationUI.EvidenceSystemActive
            ? investigationUI.EvidenceCount
            : -1;
        if (_cheatEvidence && evidenceCount == 0)
            evidenceCount = 1; // the cheat menu's "decide correctly" denies with the evidence a denial needs

        // The verdict onto the ledger; the traveler is only dispatched (and the
        // timeline moved) when accepted: an accepted liar also carries their true
        // home's fact into the claim, and an accepted costume error causes a
        // panic there (tomorrow's news). DayCycle holds the step, so the balance
        // simulation plays the same one.
        CaseVerdict verdict = DayCycle.Decide(inst, decision, _activeCaseIndex1Based, evidenceCount, _worldState, _today, _ledger, contentLibrary, _gameConfig);
        if (accepted)
            AnnounceDeparture(inst);

        if (officeUI != null)
            officeUI.UpdateHud(_worldState);

        Debug.Log($"[Result] Case {_activeCaseIndex1Based}: verdict={decision}, shouldAccept={inst.ShouldAccept}, fault='{inst.FaultReason}', home='{inst.HomeLabel}', directive={inst.directiveFault}, correct={verdict.correct}, pay={verdict.payAwarded}, penalty={verdict.moneyPenalty}, money {moneyBefore}->{_worldState.money}, stability {stabilityBefore:0.00}->{_worldState.timelineStability:0.00}, firedNow={verdict.firedNow}.");

        // The reaction (the personalities spec's R1-R5): presentation only, after the scoring, never changing it.
        React(inst, accepted);
        Resolved?.Invoke(inst, verdict, accepted, stabilityBefore);

        EndingSO ending = EndingService.Evaluate(_worldState, contentLibrary, _gameConfig, EndingMoment.Immediate);

        if (ending != null)
        {
            Debug.Log($"[GameManager] Ending check: matched '{ending.id}' ({ending.displayName}).");
            _worldState.endingId = ending.id;

            if (RunManager.HasInstance)
                RunManager.Instance.SaveNow();

            ShowVerdictThen(verdict, HandleEndingReached);
            return;
        }

        ShowVerdictThen(verdict, () => orchestrator.MarkCaseResolved());
    }

    /// <summary>An accepted traveller leaves through today's portal for them (the displaced by the Return Gate once repaired, else 01; a citizen by the open route to their destination, else 01): the portal is announced for the hall's pulse; none when the hall has no portals.</summary>
    private void AnnounceDeparture(CaseInstance inst)
    {
        if (_today == null || inst.claimedNation == null || inst.claimedEra == null)
            return;
        int portal = _today.Portals.DepartureFor(new PlaceRef(inst.claimedNation.id, inst.claimedEra.id), inst.kind == TravellerKind.Displaced);
        if (portal <= 0)
            return;
        Debug.Log($"[GameManager] Case {_activeCaseIndex1Based} leaves through portal {PortalText.Number(portal)}.");
        Departed?.Invoke(portal);
    }

    /// <summary>
    /// The decided traveller's reaction (the personalities spec's R1-R4): the
    /// investigation UI says it and returns the linger; the figure leaves
    /// after it (at once for 0), and their bubble hides as they go. The desk
    /// calls nobody new until they have left.
    /// </summary>
    private void React(CaseInstance inst, bool accepted)
    {
        float linger = investigationUI != null ? investigationUI.React(inst, accepted) : 0f;
        if (travellerView == null)
        {
            EndReaction();
            return;
        }

        _desk.SetDeparting(true);
        travellerView.Leave(linger, () =>
        {
            EndReaction();
            _desk.SetDeparting(false);
        });
    }

    private void EndReaction()
    {
        if (investigationUI != null)
            investigationUI.EndReaction();
    }

    /// <summary>
    /// Shows the verdict line and, for a citation, prints it and flies it
    /// onto the desk (BoothCoordinator.Cite; the Citation replaces the slip,
    /// Saleh 2026-10-07: Papers, Please's way, nothing waits for it):
    /// CitationIssued at once (Mail's copy), CitationLanded when it lies on
    /// the desk (the Helix River's pulse); then the continuation, at once.
    /// </summary>
    private void ShowVerdictThen(CaseVerdict verdict, System.Action onContinue)
    {
        if (officeUI != null)
            officeUI.ShowVerdict(verdict);
        if (verdict != null && verdict.citationIssued)
        {
            CitationIssued?.Invoke(verdict);
            if (booth == null || !booth.Cite(verdict.ticket, () => CitationLanded?.Invoke(verdict)))
                CitationLanded?.Invoke(verdict);
        }
        onContinue?.Invoke();
    }

    /// <summary>
    /// Called once the verdict for a run-ending case, or the shift report of a
    /// day whose dialog consequences reached an ending, has been shown. Loads
    /// the title scene to display the ending (WorldState.endingId is already
    /// set and saved).
    /// </summary>
    private void HandleEndingReached()
    {
        Debug.Log($"[GameManager] >>> Entering HandleEndingReached (endingId='{_worldState.endingId}').");

        if (RunManager.HasInstance)
        {
            Debug.Log("[GameManager] <<< Exiting HandleEndingReached (loading title scene).");
            RunManager.Instance.LoadTitleScene();
        }
        else
        {
            Debug.LogError("[GameManager] Ending reached but no RunManager instance — cannot load title scene.");
        }
    }

    /// <summary>
    /// Marks the current case as resolved so the orchestrator advances to the next slot.
    /// </summary>
    private void ResolveCurrentCase()
    {
        orchestrator.MarkCaseResolved();
    }
}
