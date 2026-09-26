using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Connects DayOrchestrator -> CaseFactory -> OfficeUIController.
/// Generates cases at day start, displays the active case on UI,
/// validates the player's era choice, then advances the day.
/// </summary>
public sealed class GameManager : MonoBehaviour
{
    /// <summary>Controls day timeline (case slots + scheduled events).</summary>
    [SerializeField] private DayOrchestrator orchestrator;

    /// <summary>Content library used by CaseFactory (eras, clues, etc.).</summary>
    [SerializeField] private ContentLibrarySO contentLibrary;

    /// <summary>Day plan to run.</summary>
    [SerializeField] private DayPlanSO dayPlan;

    /// <summary>UI controller for HUD, citation slip, and verdict display.</summary>
    [SerializeField] private OfficeUIController officeUI;

    /// <summary>
    /// Investigation UI (documents/books/compare + Accept/Deny). When assigned,
    /// the office uses the accept/deny investigation loop; if null, it falls back
    /// to the legacy era-pick UI on OfficeUIController.
    /// </summary>
    [SerializeField] private InvestigationUIController investigationUI;

    /// <summary>Briefing + end-of-day panels (optional; flow skips if unassigned).</summary>
    [SerializeField] private DayFlowUIController dayFlowUI;

    /// <summary>Optional: pulls back to the booth and arms the READY sign per case.</summary>
    [SerializeField] private OfficeViewController officeView;

    /// <summary>Optional: the READY sign that releases the per-case gate.</summary>
    [SerializeField] private Clickable readySign;

    /// <summary>Scene clock for today's shift (optional: without it the day ends only when the queue is empty).</summary>
    [SerializeField] private ShiftClockDriver shiftClock;

    /// <summary>Optional: the booth figure (the traveller's layered look), from presentation until the decision.</summary>
    [SerializeField] private TravellerView travellerView;

    /// <summary>Optional: the booth's input and wake rules.</summary>
    [SerializeField] private BoothCoordinator booth;

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

    /// <summary>Verdict record for the current shift (results screen reads this).</summary>
    private ShiftLedger _ledger;

    /// <summary>Per-case readiness gate, released by the READY sign.</summary>
    private readonly ReadyGate _readyGate = new ReadyGate();

    /// <summary>True from presenting a traveller until the player's decision (closing-time rule).</summary>
    private bool _travellerAtDesk;

    /// <summary>Gameplay tuning, pulled from RunConfig (null-safe).</summary>
    private GameConfigSO _gameConfig;

    /// <summary>Character art for the booth figure and the passport photos (only the current traveller's is kept).</summary>
    private CharacterArt _characterArt;

    /// <summary>Read-only access to the current shift's ledger.</summary>
    public ShiftLedger Ledger => _ledger;

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
            shiftClock.Configure(_gameConfig);
            shiftClock.Closed += HandleShiftClosed;
        }
        else
        {
            Debug.LogWarning("GameManager: no ShiftClockDriver wired, so the day ends only when the queue empties (no closing time). Run Tools > TimeDesk > Build Office UI.");
        }

        // Fresh ledger for this shift.
        _ledger = new ShiftLedger();

        // Today's world (places and facts, history applied): one snapshot shared
        // by the case factory and the books, so papers and reference books can
        // never disagree during the day.
        _today = contentLibrary.BuildToday(dayPlan, _worldState.history);
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
        _dayCases = _caseFactory.GenerateDayCases(dayPlan, _worldState, seed,
            spoken ? interview.AskableCategories : System.Array.Empty<ClueCategory>(),
            spoken ? interview.AnswerTellCategories : System.Array.Empty<ClueCategory>(),
            dress);
        Debug.Log($"[GameManager] Interview: spoken={spoken}, dress={dress}, askable=[{string.Join(", ", interview.AskableCategories)}], spoken tells may come from [{string.Join(", ", interview.AnswerTellCategories)}], dialogs offered={interview.OfferedDialogs(null).Count}.");

        // Investigation: surface today's travel directives (rules to deny), the
        // agency's citizen records for today's visitors, today's facts and interview.
        if (investigationUI != null)
        {
            investigationUI.SetDirectives(dayPlan.ActiveTravelRules);
            investigationUI.SetCitizenRegistry(CaseFactory.BuildRegistry(_dayCases));
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

        // READY sign releases the per-case gate (only meaningful when wired).
        if (readySign != null)
            readySign.onClick.AddListener(() => _readyGate.Release());

        // The booth's day (its day-1 notes) and phase: the briefing comes first.
        if (booth != null)
            booth.BeginDay(_worldState.day);

        Debug.Log($"[GameManager] Day {_worldState.day} starting: seed={seed}, money={_worldState.money}, stability={_worldState.timelineStability:0.#}, cases={_dayCases.Count}, places={_today.Places.Count}, leader='{_worldState.history.leaderId}'.");

        // The morning paper is printed: its lines go to the News site's back issues (the night rebuilds them, so they are kept now).
        if (desktopConfig != null)
            NewsArchive.Record(_worldState.newsArchive, _worldState.day, _worldState.tomorrow.briefingLines, _worldState.tomorrow.newsLines, desktopConfig.newsArchiveIssues);
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
            dayFlowUI.ShowBriefing(_worldState, () => BeginShift(planToRun, seedToUse));
        }
        else
        {
            Debug.Log("[GameManager] <<< Exiting Start (starting day loop directly).");
            BeginShift(dayPlan, seed);
        }
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

    /// <summary>
    /// Unsubscribes to prevent event leaks on scene unload / play mode exit.
    /// </summary>
    private void OnDestroy()
    {
        if (shiftClock != null)
            shiftClock.Closed -= HandleShiftClosed;

        _characterArt?.Dispose();

        if (orchestrator == null)
            return;

        orchestrator.OnCaseSlotStarted -= HandleCaseSlotStarted;
        orchestrator.OnCaseSlotEnded -= HandleCaseSlotEnded;
        orchestrator.OnDayCompleted -= HandleDayCompleted;
    }

    /// <summary>
    /// Called when the last case of the day resolves: applies the narrative
    /// dialogs' consequences (then refreshes the HUD and checks endings), saves
    /// the run, shows the shift report and leads into the Home scene (or the
    /// title scene when an ending was reached).
    /// </summary>
    private void HandleDayCompleted()
    {
        Debug.Log($"[GameManager] >>> Entering HandleDayCompleted (day {_worldState.day}).");

        // The booth is shut: freeze the clock (the queue may have run out before closing).
        _travellerAtDesk = false;
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
        Debug.Log($"[GameManager] Day {_worldState.day} shift complete: {correctCount}/{totalCases} correct, totalPay={totalPay}, totalPenalty={totalPenalty}, money={_worldState.money}, stability={_worldState.timelineStability:0.#}.");

        // Narrative dialogs' consequences apply now, before the save, so a
        // Continue replay of this day can never apply them twice.
        EndingSO ending = null;
        if (ApplyDialogOutcomes())
        {
            if (officeUI != null)
                officeUI.UpdateHud(_worldState);

            if (_gameConfig != null)
            {
                ending = EndingService.Evaluate(_worldState, contentLibrary, _gameConfig, EndingMoment.Immediate);
                if (ending != null)
                {
                    Debug.Log($"[GameManager] Ending check after dialog consequences: matched '{ending.id}' ({ending.displayName}).");
                    _worldState.endingId = ending.id;
                }
            }
        }

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
            dayFlowUI.ShowResults(_worldState, _ledger, next);
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
    /// Applies what the shift's completed dialogs decided (DialogOutcomes):
    /// sets each one-shot dialog's done flag, and activates each named effect
    /// once, with its instant ops now and a start day of tomorrow (so its
    /// briefing and news lines reach the next morning's paper). Returns true
    /// when any effect was applied.
    /// </summary>
    private bool ApplyDialogOutcomes()
    {
        if (_ledger == null || _worldState == null)
            return false;

        foreach (string flag in DialogOutcomes.FlagsToSet(_ledger.dialogOutcomes))
            _worldState.SetFlag(flag);

        bool applied = false;
        foreach (DialogOutcome outcome in DialogOutcomes.EffectsToApply(_ledger.dialogOutcomes))
        {
            EffectSO fx = contentLibrary.GetEffectByAssetName(outcome.effectName);
            if (fx == null)
            {
                Debug.LogWarning($"[GameManager] Dialog '{outcome.dialogId}' names effect '{outcome.effectName}', which ContentLibrary_Main does not list; add it to the library's effects.");
                continue;
            }

            TimelineService.ActivateEffect(_worldState, fx, $"Dialog: {outcome.dialogId}", _worldState.day + 1, fx.defaultDurationDays, applyInstantOps: true);
            applied = true;
        }

        return applied;
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

        string claimedEraId = inst.trueEra != null ? inst.trueEra.id : string.Empty;
        string archetypeName = inst.archetype != null ? inst.archetype.displayName : string.Empty;
        string nationName = inst.nation != null ? inst.nation.displayName : string.Empty;
        Debug.Log($"[GameManager] Case {caseIndex1Based}: visitor='{inst.visitorDisplayName}', claim='{inst.originLabel}', claimedEra='{claimedEraId}', archetype='{archetypeName}', nation='{nationName}', legendary={inst.isLegendary}, liar={inst.IsLiar}, home='{inst.HomeLabel}', gender={inst.gender}, documents={inst.documents.Count}, clues={inst.usedClues.Count}.");

        // Clear the previous case's verdict line before showing the new case.
        if (officeUI != null)
            officeUI.SetResultText(string.Empty);

        // Return to the booth and wait for the player to tap READY before
        // presenting the visitor. With no view/sign wired, show immediately.
        if (officeView != null && readySign != null)
        {
            officeView.FocusOffice();
            readySign.Interactable = true;
            _readyGate.Arm();
            _readyGate.Released += ShowActiveCaseOnce;
        }
        else
        {
            ShowActiveCase(inst);
        }

        Debug.Log($"[GameManager] <<< Exiting HandleCaseSlotStarted (slot {caseIndex1Based}, awaiting player decision).");
    }

    /// <summary>Starts the day loop and the shift clock together (after the briefing).</summary>
    private void BeginShift(DayPlanSO plan, int daySeed)
    {
        if (booth != null)
            booth.SetPhase(BoothPhase.NoTraveller);

        orchestrator.StartDay(_worldState, plan, daySeed);

        if (shiftClock != null)
            shiftClock.StartShift();
    }

    /// <summary>
    /// Closing time: a traveller already at the desk may be finished; otherwise
    /// the booth closes at once, and a traveller still behind READY is never called.
    /// </summary>
    private void HandleShiftClosed()
    {
        ClosingAction action = ShiftFlow.OnClosing(_travellerAtDesk);
        Debug.Log($"[GameManager] Closing time (travellerAtDesk={_travellerAtDesk}) -> {action}.");

        if (action == ClosingAction.FinishCurrent)
        {
            orchestrator.CloseAfterCurrentSlot();
            return;
        }

        if (_readyGate.IsArmed)
        {
            _readyGate.Released -= ShowActiveCaseOnce;
            _readyGate.Disarm();
            if (readySign != null)
                readySign.Interactable = false;
        }

        orchestrator.CloseNow();
    }

    /// <summary>One-shot handler so the gate shows the case a single time.</summary>
    private void ShowActiveCaseOnce()
    {
        _readyGate.Released -= ShowActiveCaseOnce;

        int idx = _activeCaseIndex1Based - 1;
        if (_dayCases == null || idx < 0 || idx >= _dayCases.Count)
            return;

        if (readySign != null)
            readySign.Interactable = false;

        ShowActiveCase(_dayCases[idx]);
    }

    /// <summary>
    /// The one presence transition: whether a traveller is at the desk (and
    /// how they look) feeds the closing-time rule, the booth figure and the
    /// booth's input phase.
    /// </summary>
    private void SetTravellerAtDesk(bool at, TravellerLook look = null)
    {
        _travellerAtDesk = at;

        if (travellerView != null)
        {
            if (at)
                travellerView.Show(look, _characterArt);
            else
                travellerView.Clear();
        }

        if (booth != null)
            booth.SetPhase(at ? BoothPhase.TravellerAtDesk : BoothPhase.NoTraveller);
    }

    /// <summary>
    /// Presents a case via the investigation UI (or legacy era UI): keeps only
    /// this traveller's art, shows them in the booth, and marks a once-per-run
    /// premade as met (FlagKeys.PremadeMet: they never come back this run).
    /// </summary>
    private void ShowActiveCase(CaseInstance inst)
    {
        _characterArt?.Retain(inst.look != null ? inst.look.Keys : null);
        SetTravellerAtDesk(true, inst.look);

        if (inst.isLegendary && inst.legendarySource != null && inst.legendarySource.oncePerRun)
            _worldState.SetFlag(FlagKeys.PremadeMet(inst.legendarySource.id));

        if (investigationUI != null)
            investigationUI.ShowCase(inst, contentLibrary, HandleDecision);
        else
            officeUI.ShowCase(inst, contentLibrary.Eras, HandlePlayerChoseEra);
    }

    /// <summary>
    /// Called when the orchestrator ends case slot #N (1-based).
    /// </summary>
    private void HandleCaseSlotEnded(int caseIndex1Based)
    {
        Debug.Log($"[GameManager] >>> Entering HandleCaseSlotEnded (slot {caseIndex1Based}).");

        // Optional: hide UI between cases, or keep it visible and overwrite contents.
        // officeUI.Hide();

        Debug.Log($"[GameManager] <<< Exiting HandleCaseSlotEnded (slot {caseIndex1Based}).");
    }

    /// <summary>
    /// Validates the player's choice, shows result, then advances the day.
    /// </summary>
    private void HandlePlayerChoseEra(EraSO chosenEra)
    {
        Debug.Log($"[GameManager] >>> Entering HandlePlayerChoseEra (slot {_activeCaseIndex1Based}, chosenEra='{chosenEra?.id}').");
        SetTravellerAtDesk(false);

        int idx = _activeCaseIndex1Based - 1;

        if (_dayCases == null || idx < 0 || idx >= _dayCases.Count)
        {
            Debug.LogError("Player chose an era but the active case index is invalid.");
            orchestrator.MarkCaseResolved();
            return;
        }

        CaseInstance inst = _dayCases[idx];

        // No tuning config: keep the old simple correct/wrong behavior.
        if (_gameConfig == null)
        {
            bool simpleCorrect = inst.trueEra == chosenEra;
            officeUI.SetResultText(UiText.Get(simpleCorrect ? "verdict.simpleCorrect" : "verdict.simpleWrong"));
            Debug.Log($"[GameManager] <<< Exiting HandlePlayerChoseEra (no GameConfig, simpleCorrect={simpleCorrect}).");
            orchestrator.MarkCaseResolved();
            return;
        }

        // Full economy path: resolve verdict, apply consequences, record it.
        float stabilityBefore = _worldState.timelineStability;
        int moneyBefore = _worldState.money;

        CaseVerdict verdict = ShiftScoring.Resolve(inst, chosenEra, _activeCaseIndex1Based, _worldState, _gameConfig, contentLibrary);
        _ledger.verdicts.Add(verdict);

        // Timeline impacts: every send moves attribute/nation scores.
        TimelineService.ApplyVerdictImpacts(inst, chosenEra, verdict.correct, _worldState, contentLibrary);

        officeUI.UpdateHud(_worldState);

        Debug.Log($"[Result] Case {_activeCaseIndex1Based}: chose '{verdict.chosenEraId}', true='{verdict.trueEraId}', correct={verdict.correct}, pay={verdict.payAwarded}, penalty={verdict.moneyPenalty}, citation={verdict.citationIssued} (freeWarning={verdict.wasFreeWarning}), money {moneyBefore}->{_worldState.money}, stability {stabilityBefore:0.#}->{_worldState.timelineStability:0.#}, firedNow={verdict.firedNow}.");

        // Check for a game-over ending (e.g., fired from hitting the stability floor).
        EndingSO ending = EndingService.Evaluate(_worldState, contentLibrary, _gameConfig, EndingMoment.Immediate);

        if (ending != null)
        {
            Debug.Log($"[GameManager] Ending check: matched '{ending.id}' ({ending.displayName}).");

            _worldState.endingId = ending.id;

            if (RunManager.HasInstance)
                RunManager.Instance.SaveNow();

            Debug.Log($"[GameManager] <<< Exiting HandlePlayerChoseEra (run ending '{ending.id}' — showing verdict then title scene).");

            // Show the verdict, then hand off to the title scene instead of continuing the day.
            ShowVerdictThen(verdict, HandleEndingReached);
            return;
        }

        Debug.Log("[GameManager] Ending check: no ending matched, run continues.");

        if (verdict.firedNow)
            Debug.LogWarning("[GameManager] Stability hit firing threshold, but no matching EndingSO is authored yet — run continues.");

        Debug.Log($"[GameManager] <<< Exiting HandlePlayerChoseEra (slot {_activeCaseIndex1Based} resolved, showing verdict then advancing).");

        // Show the verdict (citation slip pauses the day if wired), then advance.
        ShowVerdictThen(verdict, () => orchestrator.MarkCaseResolved());
    }

    /// <summary>
    /// Resolves the player's Accept/Deny decision (investigation loop): scores it,
    /// dispatches timeline impacts only on accept, checks for an ending, then
    /// shows the verdict and advances the day.
    /// </summary>
    private void HandleDecision(bool accepted)
    {
        Debug.Log($"[GameManager] >>> Entering HandleDecision (slot {_activeCaseIndex1Based}, accepted={accepted}).");
        SetTravellerAtDesk(false);

        int idx = _activeCaseIndex1Based - 1;

        if (_dayCases == null || idx < 0 || idx >= _dayCases.Count)
        {
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

        CaseVerdict verdict = ShiftScoring.ResolveDecision(inst, accepted, _activeCaseIndex1Based, _worldState, _gameConfig, contentLibrary, evidenceCount);
        _ledger.verdicts.Add(verdict);

        // The traveler is only dispatched (and the timeline moved) when accepted;
        // an accepted liar also carries their true home's fact into the claim.
        if (accepted)
        {
            TimelineService.ApplyVerdictImpacts(inst, inst.claimedEra, verdict.correct, _worldState, contentLibrary);
            HistoryService.RecordCarry(_worldState, inst, _today.Facts, _gameConfig);
        }

        if (officeUI != null)
            officeUI.UpdateHud(_worldState);

        Debug.Log($"[Result] Case {_activeCaseIndex1Based}: accepted={accepted}, shouldAccept={inst.ShouldAccept}, liar={verdict.wasLiar}, home='{verdict.trueHomeLabel}', claimAllowed={inst.claimAllowedByRules}, correct={verdict.correct}, pay={verdict.payAwarded}, penalty={verdict.moneyPenalty}, money {moneyBefore}->{_worldState.money}, stability {stabilityBefore:0.#}->{_worldState.timelineStability:0.#}, firedNow={verdict.firedNow}.");

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

    /// <summary>
    /// Shows the verdict slip if a UI is wired (pausing the shift clock while a
    /// citation slip is up, and holding the PC screen on so the slip can never
    /// sit on a dark screen), then runs the continuation.
    /// </summary>
    private void ShowVerdictThen(CaseVerdict verdict, System.Action onContinue)
    {
        if (officeUI == null)
        {
            onContinue?.Invoke();
            return;
        }

        // A citation slip holds the day, the shift clock and the screen until acknowledged.
        bool citation = verdict != null && verdict.citationIssued;
        bool holdsClock = shiftClock != null && citation;
        if (holdsClock)
            shiftClock.Pause();
        if (citation && booth != null)
            booth.SetCitationPending(true);

        officeUI.ShowVerdict(verdict, () =>
        {
            if (citation && booth != null)
                booth.SetCitationPending(false);
            if (holdsClock)
                shiftClock.Resume();
            onContinue?.Invoke();
        });
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
