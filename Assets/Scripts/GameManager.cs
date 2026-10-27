using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Connects DayOrchestrator -> CaseFactory -> the investigation UI.
/// Generates cases at day start, displays the active case on UI,
/// resolves the player's Accept/Deny decision, then advances the day. The
/// cheat menu's entry points are in GameManager.Cheats.cs.
/// P0-A carve (docs/pending/CARVE.md): today's bulletin is in
/// GameManager.Bulletin.cs.
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

    /// <summary>The upgrades' objects on the desk (the desk machines spec §4): each day starts them with the world; the shift's opening opens the supply crate; a departure flashes the RING SERVICE panel.</summary>
    [SerializeField] private DeskUpgrades deskUpgrades;

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

    /// <summary>True while a decided traveller's verdict (the ribbon, a citation slip) shows and their slot is not yet resolved: closing finishes the slot first (ShiftEnd.OnClosing; BUG-28b).</summary>
    private bool _verdictShowing;

    /// <summary>True once today's closing was handled (closing time, End Shift, CloseNow, or the day's end): it is handled once.</summary>
    private bool _closingHandled;

    /// <summary>
    /// True once this office's shift is no longer its own to close: it is not
    /// running (never begun, its day already ended, a cheat's route closed it:
    /// CloseShiftForCheat ended the day on the run's world, which the route
    /// then moved on and saved; or End Shift showed its ending at once), or a
    /// scene change is under way (SceneTransit.Loading: a cheat's landing, a
    /// restart or the Title replaces this office, which stays alive behind
    /// the shutter until the new scene activates). Its clock's Closed and its
    /// day loop's end are then ignored, so a day is never closed twice and
    /// the landing morning's save is never overwritten by the office left
    /// behind (FLOW round 4: a mid-shift Skip day closed day N again on day
    /// N+1's world).
    /// </summary>
    private bool ShiftLeftBehind => ShiftEnd.LeftBehind(_shiftRunning, SceneTransit.Loading);

    /// <summary>The day this office runs (its plan's), for the log; the run's world may already be on another day after a cheat's route.</summary>
    private int DayNumber => dayPlan != null ? dayPlan.DayNumber : 0;

    /// <summary>The booth's roll shutter (the bible's G3 and G4): up as the shift opens, down after the last payoff, before the report.</summary>
    private BoothShutter _shutter;

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

    /// <summary>Raised when the player acknowledges a citation slip (Mail's citation notice arrives then, redesign phase 25; the Helix River's red pulse runs then).</summary>
    public event System.Action<CaseVerdict> CitationAcknowledged;

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

        // The office a scene change is replacing is never the cheat menu's live shift (FLOW round 5): not one that starts
        // while a newer load waits to follow its own, and not this one once a load is asked for (LeaveCurrent).
        if (SceneTransit.NextWaiting)
            Debug.Log("[GameManager] This office starts while a newer scene change waits to replace it: it is not the live shift (GameManager.Current).");
        else
            Current = this;
        SceneTransit.Asked += LeaveCurrent;

        // Acquire the run (creates RunManager + loads save/new run on first scene).
        bool runExisted = RunManager.HasInstance;
        RunManager run = RunManager.GetOrCreate();

        // Each scene is themed for the run's culture as it loads (CultureThemeService, sceneLoaded), so the office is
        // themed once (BUG-31a); only an office opened on its own, whose run is made here, is themed again for that run.
        if (!runExisted)
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
        else
            Debug.LogWarning("GameManager: no ShiftClockDriver wired, so the day ends only when the queue empties (no closing time). Run Tools > TimeDesk > Build Office UI.");

        // The game feel's director (hit-stop, camera impulses, the shift's bells, the hall's ambience).
        FeelDirector.Attach(this, shiftClock);

        // The booth's roll shutter, shut until the shift opens.
        _shutter = BoothShutter.Attach(this);

        // A waiver signed from the desk's pad costs shift time (the endings and strandings spec §7.3, Q15 = A).
        if (investigationUI != null)
            investigationUI.WaiverSigned += HandleWaiverSigned;

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
        if (deskUpgrades != null)
        {
            deskUpgrades.Bind(investigationUI);
            deskUpgrades.BeginDay(_worldState, contentLibrary, Portals, _dayCases, seed);
            _desk.Opened += deskUpgrades.ShiftOpened;
            Departed += deskUpgrades.Departed;
        }

        Debug.Log($"[GameManager] Day {_worldState.day} starting: seed={seed}, money={_worldState.money}, stability={_worldState.timelineStability:0.00}, cases={_dayCases.Count}, places={_today.Places.Count}, leader='{_worldState.history.leaderId}'.");

        // The day's bulletin, with the new desk hours when they changed overnight (night shifts: the hours grow).
        string bulletin = TodaysBulletin();

        // The morning paper is printed: its lines go to the News site's back issues (GameManager.News.cs).
        RecordTodaysPaper();

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
        SceneTransit.Asked -= LeaveCurrent;
        // A citation slip still up as the office goes (a cheat's jump, quitting to the title) holds no clock in the next scene.
        PauseState.Shared.Release(PauseState.Citation);
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
    /// A scene change was asked for (SceneTransit.Asked): this office is being
    /// replaced, though it stays alive behind the shutter until the new scene
    /// activates, so it stops being the cheat menu's live shift
    /// (GameManager.Current) at once. RunManager's routes take the live shift
    /// from Current, so a second skip or jump under the shutter closes the
    /// run's landing day on an empty ledger, as from its briefing, and never
    /// closes this office's day again on the landing world (FLOW round 5: a
    /// Skip day, then another before the shutter lifted, re-applied the first
    /// day's strandings, instalment, dialog outcomes and statement row).
    /// </summary>
    private void LeaveCurrent()
    {
        if (Current != this)
            return;
        Current = null;
        Debug.Log($"[GameManager] A scene change replaces day {DayNumber}'s office: it is no longer the live shift (GameManager.Current).");
    }

    /// <summary>
    /// Called when the last case of the day resolves (or the booth closed):
    /// closes the desk and stops the clock (an early finish rings the closing
    /// bell, BUG-72), applies the narrative dialogs' consequences (then
    /// refreshes the HUD and checks endings) and saves the run; then, once the
    /// last traveller's reaction and walk-off have played (BUG-32a), shows the
    /// shift report and leads into the Home scene, or into the run's ending
    /// when one was reached during the shift or at its close (the title scene;
    /// BUG-28b, BUG-56a). Nothing when the shift is no longer this office's
    /// to close (ShiftLeftBehind): a cheat's route has already closed the day.
    /// </summary>
    private void HandleDayCompleted()
    {
        if (ShiftLeftBehind)
        {
            Debug.Log($"[GameManager] The day loop of day {DayNumber} ended after its shift was closed elsewhere (a cheat's route, an ending shown at once) or while a scene change replaces this office: nothing is closed again (the run is on day {_worldState.day}).");
            return;
        }

        Debug.Log($"[GameManager] >>> Entering HandleDayCompleted (day {_worldState.day}).");

        // The booth is shut: closing is handled, the clock stops (the queue may have run out before closing: the bell rings) and the desk closes; an FTUE still open is over (saved below).
        _travellerAtDesk = false;
        _shiftRunning = false;
        _closingHandled = true;
        if (guide != null)
            guide.EndShift();
        CloseDesk();
        if (shiftClock != null)
            shiftClock.StopShift();

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

        // A run that reached an ending during the shift (a verdict's CloseNow(endingId)) or at its close ends here: the report, then the ending, never Home.
        bool ends = ShiftEnd.EndsTheRun(_worldState.endingId);
        System.Action next = ends ? new System.Action(HandleEndingReached) : HandleGoHome;

        Debug.Log($"[GameManager] <<< Exiting HandleDayCompleted (the report once the last traveller's payoff has played, then {(ends ? "the ending" : "Home")}).");
        StartCoroutine(AfterThePayoff(() => ShowDayReport(next, ends)));
    }

    /// <summary>
    /// Waits until the last traveller's reaction and walk-off have played
    /// (DeskAvailability.IsDeparting; at most MotionTuningSO.payoffWaitSeconds
    /// of real time: ShiftEnd.PayoffDone), then ends their linger and bubble,
    /// so neither sits over the report, frees their art, rolls the booth's
    /// shutter down (the bible's G4; at once without one) and runs
    /// <paramref name="then"/> (BUG-32a: the report wiped the payoff).
    /// </summary>
    private IEnumerator AfterThePayoff(System.Action then)
    {
        float max = UiMotion.Tuning != null ? UiMotion.Tuning.payoffWaitSeconds : DefaultPayoffWaitSeconds;
        float waited = 0f;
        while (!ShiftEnd.PayoffDone(_desk.IsDeparting, waited, max))
        {
            yield return null;
            waited += Time.unscaledDeltaTime;
        }
        if (travellerView != null)
            travellerView.EndLinger();
        EndReaction();
        _characterArt?.Retain(null);
        if (_shutter != null)
            _shutter.Close(then);
        else
            then();
    }

    /// <summary>The payoff's longest wait without a MotionTuningSO (its default).</summary>
    private const float DefaultPayoffWaitSeconds = 8f;

    /// <summary>
    /// Shows the shift report, then hands off to <paramref name="next"/> (Home,
    /// or the ending when <paramref name="ends"/>). The report is read in the
    /// booth (like the morning briefing), so the view pulls back first; without
    /// a report panel, the day's line and <paramref name="next"/> at once.
    /// </summary>
    private void ShowDayReport(System.Action next, bool ends)
    {
        if (dayFlowUI != null)
        {
            if (officeView != null)
                officeView.FocusOffice();

            Debug.Log($"[GameManager] The shift report, then {(ends ? "the ending" : "Home")}.");
            if (booth != null)
                booth.SetPhase(BoothPhase.Newsletter);
            // The last verdict's ribbon goes with the shift (QA2 2026-10-16: CORRECT stayed above the report).
            if (officeUI != null)
                officeUI.SetResultText(string.Empty);
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

            Debug.Log($"[GameManager] No results panel: going to {(ends ? "the ending" : "Home")} directly.");
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

    /// <summary>Starts the day loop (after the briefing); the AVAILABLE sign takes clicks from here (the desk starts paused) and its first press starts the shift clock (OpenShift; Saleh 2026-10-07: "shift should not start until you press AVAILABLE"); without a sign the clock starts at once.</summary>
    private void BeginShift(DayPlanSO plan, int daySeed)
    {
        if (booth != null)
            booth.SetPhase(BoothPhase.NoTraveller);
        if (readySign != null)
            readySign.Interactable = true;

        // The shift runs from here, before the day loop starts: closing time and the day's end are handled only for a running shift (ShiftLeftBehind).
        _shiftRunning = true;

        orchestrator.StartDay(_worldState, plan, daySeed, _dayCases);

        if (readySign == null)
            OpenShift();

        // The desk's guide: day 1's FTUE, a guided day's new page opened in the rulebook, the GUIDE's pages so far.
        if (guide != null)
            guide.BeginShift(_worldState.day, _worldState.guide, contentLibrary);

        // The cheat menu's "Auto-decide" keeps the queue coming from the shift's start.
        StartAutoDecide();
    }

    /// <summary>
    /// Closing time (once a day: closing time, End Shift, CloseNow): the desk
    /// closes (the AVAILABLE sign goes off and inert, and a traveller still
    /// waiting in the queue is never called); a traveller at the desk, or a
    /// decided one whose verdict still shows (a citation slip), is finished
    /// first (ShiftEnd.OnClosing: the report never covers the slip; BUG-28b),
    /// otherwise the booth closes at once. Nothing when the shift is no longer
    /// this office's to close (ShiftLeftBehind): the clock's Closed raised by a
    /// cheat's close (CloseShiftForCheat finishes the clock after clearing
    /// _shiftRunning) must not release the day loop into a second day's end.
    /// </summary>
    private void HandleShiftClosed()
    {
        if (_closingHandled)
            return;
        if (ShiftLeftBehind)
        {
            Debug.Log($"[GameManager] Closing time of day {DayNumber} ignored: its shift was closed by a cheat's route or its ending, or a scene change is replacing this office.");
            return;
        }
        _closingHandled = true;
        ClosingAction action = ShiftEnd.OnClosing(_travellerAtDesk, _verdictShowing);
        Debug.Log($"[GameManager] Closing time (travellerAtDesk={_travellerAtDesk}, verdictShowing={_verdictShowing}, waiting={_desk.IsWaiting}) -> {action}.");
        CloseDesk();

        if (action == ClosingAction.FinishCurrent)
            orchestrator.CloseAfterCurrentSlot();
        else
            orchestrator.CloseNow();
    }

    /// <summary>
    /// The run ends at this day's end (TIMELINE-FAIL's Q4 = A; BUG-28b,
    /// BUG-56a: failure plays out at the day's end, never the instant a
    /// verdict lands): <paramref name="endingId"/> is set (blank keeps the one
    /// set already) and, while the shift runs, it closes now: the AVAILABLE
    /// sign goes off, nobody else is called, a traveller at the desk or a
    /// decided one's verdict is finished first, the closing bell rings; then
    /// the last payoff, the report and the ending (never Home). Before the
    /// shift starts, the ending still waits for the day's end.
    /// </summary>
    public void CloseNow(string endingId)
    {
        if (_worldState == null)
            return;
        if (!string.IsNullOrEmpty(endingId))
            _worldState.endingId = endingId;
        if (!_shiftRunning)
            return;
        Debug.Log($"[GameManager] CloseNow('{_worldState.endingId}'): the shift closes now; the run ends at the day's end.");
        if (shiftClock != null)
            shiftClock.StopShift(); // rings the bell and raises Closed: HandleShiftClosed
        HandleShiftClosed(); // when the clock never ran (handled once either way)
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
    /// Marks the current case as resolved so the orchestrator advances to the next slot.
    /// </summary>
    private void ResolveCurrentCase()
    {
        orchestrator.MarkCaseResolved();
    }
}
