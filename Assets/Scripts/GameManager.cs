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

    /// <summary>Seed for deterministic day schedule randomness.</summary>
    [SerializeField] private int seed = 12345;

    /// <summary>Runtime world state for this session.</summary>
    private WorldState _worldState;

    /// <summary>Builds case runtime objects from ScriptableObjects.</summary>
    private CaseFactory _caseFactory;

    /// <summary>Generated cases for the day (0-based indexing).</summary>
    private List<CaseInstance> _dayCases;

    /// <summary>Currently active case slot (1-based).</summary>
    private int _activeCaseIndex1Based;

    /// <summary>Verdict record for the current shift (results screen reads this).</summary>
    private ShiftLedger _ledger;

    /// <summary>Per-case readiness gate, released by the READY sign.</summary>
    private readonly ReadyGate _readyGate = new ReadyGate();

    /// <summary>Gameplay tuning, pulled from RunConfig (null-safe).</summary>
    private GameConfigSO _gameConfig;

    /// <summary>Read-only access to the current shift's ledger.</summary>
    public ShiftLedger Ledger => _ledger;

    /// <summary>
    /// Initializes systems, generates cases once, and starts the day loop.
    /// </summary>
    private void Start()
    {
        Debug.Log("[GameManager] >>> Entering Start.");

        if (orchestrator == null || contentLibrary == null || officeUI == null)
        {
            Debug.LogError("GameManager missing references (orchestrator/contentLibrary/officeUI).");
            return;
        }

        // Acquire the run (creates RunManager + loads save/new run on first scene).
        RunManager run = RunManager.GetOrCreate();

        if (run != null)
        {
            _worldState = run.World;

            // Prefer the library's plan for the current day; keep the inspector
            // value as a fallback so test scenes still work.
            DayPlanSO planForToday = run.GetCurrentDayPlan();
            if (planForToday != null)
                dayPlan = planForToday;

            seed = run.GetDaySeed();
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

        // Fresh ledger for this shift.
        _ledger = new ShiftLedger();

        // Create the case factory from the content library.
        _caseFactory = new CaseFactory(contentLibrary);

        // Generate all cases up-front.
        _dayCases = _caseFactory.GenerateDayCases(dayPlan, _worldState);

        // Investigation: surface today's travel directives (rules to deny).
        if (investigationUI != null)
            investigationUI.SetDirectives(dayPlan.ActiveTravelRules);

        // Initial HUD state.
        officeUI.UpdateHud(_worldState);

        // Subscribe to orchestrator callbacks.
        orchestrator.OnCaseSlotStarted += HandleCaseSlotStarted;
        orchestrator.OnCaseSlotEnded += HandleCaseSlotEnded;
        orchestrator.OnDayCompleted += HandleDayCompleted;

        // READY sign releases the per-case gate (only meaningful when wired).
        if (readySign != null)
            readySign.onClick.AddListener(() => _readyGate.Release());

        Debug.Log($"[GameManager] Day {_worldState.day} starting: seed={seed}, money={_worldState.money}, stability={_worldState.timelineStability:0.#}, cases={_dayCases.Count}.");

        // Morning briefing first (if wired), then the day loop.
        if (dayFlowUI != null)
        {
            DayPlanSO planToRun = dayPlan;
            int seedToUse = seed;
            Debug.Log("[GameManager] <<< Exiting Start (showing morning briefing before day loop).");
            dayFlowUI.ShowBriefing(_worldState, () => orchestrator.StartDay(_worldState, planToRun, seedToUse));
        }
        else
        {
            Debug.Log("[GameManager] <<< Exiting Start (starting day loop directly).");
            orchestrator.StartDay(_worldState, dayPlan, seed);
        }
    }

    /// <summary>
    /// Unsubscribes to prevent event leaks on scene unload / play mode exit.
    /// </summary>
    private void OnDestroy()
    {
        if (orchestrator == null)
            return;

        orchestrator.OnCaseSlotStarted -= HandleCaseSlotStarted;
        orchestrator.OnCaseSlotEnded -= HandleCaseSlotEnded;
        orchestrator.OnDayCompleted -= HandleDayCompleted;
    }

    /// <summary>
    /// Called when the last case of the day resolves. Saves the run.
    /// Phase 3 replaces the log with the results screen; Phase 4 leads into the Home scene.
    /// </summary>
    private void HandleDayCompleted()
    {
        Debug.Log($"[GameManager] >>> Entering HandleDayCompleted (day {_worldState.day}).");

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

        if (RunManager.HasInstance)
        {
            // Yesterday's slot modifiers were consumed by today's shift.
            RunManager.Instance.ResetTomorrowModifiers();
            RunManager.Instance.SaveNow();
        }

        // Show the shift report, then hand off to the home phase.
        if (dayFlowUI != null)
        {
            Debug.Log("[GameManager] <<< Exiting HandleDayCompleted (showing results panel, then Home).");
            dayFlowUI.ShowResults(_worldState, _ledger, HandleGoHome);
        }
        else
        {
            if (officeUI != null)
                officeUI.SetResultText($"Day {_worldState.day} complete.");

            Debug.Log("[GameManager] <<< Exiting HandleDayCompleted (no results panel, going Home directly).");
            HandleGoHome();
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

        Debug.Log($"[GameManager] Case {caseIndex1Based}: visitor='{inst.visitorDisplayName}', trueEra='{inst.trueEra?.id}', archetype='{inst.archetype?.displayName}', nation='{inst.nation?.displayName}', legendary={inst.isLegendary}, documents={inst.documents.Count}, clues={inst.usedClues.Count}.");

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

    /// <summary>Presents a case via the investigation UI (or legacy era UI).</summary>
    private void ShowActiveCase(CaseInstance inst)
    {
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
            officeUI.SetResultText(simpleCorrect ? "✅ Correct" : "❌ Wrong");
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
        EndingSO ending = EndingService.Evaluate(_worldState, contentLibrary, _gameConfig);

        if (ending != null)
        {
            Debug.Log($"[GameManager] Ending check: matched '{ending.id}' ({ending.displayName}).");

            _worldState.endingId = ending.id;

            if (RunManager.HasInstance)
                RunManager.Instance.SaveNow();

            Debug.Log($"[GameManager] <<< Exiting HandlePlayerChoseEra (run ending '{ending.id}' — showing verdict then title scene).");

            // Show the verdict, then hand off to the title scene instead of continuing the day.
            officeUI.ShowVerdict(verdict, HandleEndingReached);
            return;
        }

        Debug.Log("[GameManager] Ending check: no ending matched, run continues.");

        if (verdict.firedNow)
            Debug.LogWarning("[GameManager] Stability hit firing threshold, but no matching EndingSO is authored yet — run continues.");

        Debug.Log($"[GameManager] <<< Exiting HandlePlayerChoseEra (slot {_activeCaseIndex1Based} resolved, showing verdict then advancing).");

        // Show the verdict (citation slip pauses the day if wired), then advance.
        officeUI.ShowVerdict(verdict, () => orchestrator.MarkCaseResolved());
    }

    /// <summary>
    /// Resolves the player's Accept/Deny decision (investigation loop): scores it,
    /// dispatches timeline impacts only on accept, checks for an ending, then
    /// shows the verdict and advances the day.
    /// </summary>
    private void HandleDecision(bool accepted)
    {
        Debug.Log($"[GameManager] >>> Entering HandleDecision (slot {_activeCaseIndex1Based}, accepted={accepted}).");

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
                officeUI.SetResultText(simpleCorrect ? "✅ Correct" : "❌ Wrong");
            Debug.Log($"[GameManager] <<< Exiting HandleDecision (no GameConfig, simpleCorrect={simpleCorrect}).");
            orchestrator.MarkCaseResolved();
            return;
        }

        float stabilityBefore = _worldState.timelineStability;
        int moneyBefore = _worldState.money;

        CaseVerdict verdict = ShiftScoring.ResolveDecision(inst, accepted, _activeCaseIndex1Based, _worldState, _gameConfig, contentLibrary);
        _ledger.verdicts.Add(verdict);

        // The traveler is only dispatched (and the timeline moved) when accepted.
        if (accepted)
            TimelineService.ApplyVerdictImpacts(inst, inst.claimedEra, verdict.correct, _worldState, contentLibrary);

        if (officeUI != null)
            officeUI.UpdateHud(_worldState);

        Debug.Log($"[Result] Case {_activeCaseIndex1Based}: accepted={accepted}, shouldAccept={inst.ShouldAccept}, forged={inst.isForged}, claimAllowed={inst.claimAllowedByRules}, correct={verdict.correct}, pay={verdict.payAwarded}, penalty={verdict.moneyPenalty}, money {moneyBefore}->{_worldState.money}, stability {stabilityBefore:0.#}->{_worldState.timelineStability:0.#}, firedNow={verdict.firedNow}.");

        EndingSO ending = EndingService.Evaluate(_worldState, contentLibrary, _gameConfig);

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

    /// <summary>Shows the verdict slip if a UI is wired, then runs the continuation.</summary>
    private void ShowVerdictThen(CaseVerdict verdict, System.Action onContinue)
    {
        if (officeUI != null)
            officeUI.ShowVerdict(verdict, onContinue);
        else
            onContinue?.Invoke();
    }

    /// <summary>
    /// Called once the verdict for a run-ending case has been shown.
    /// Loads the title scene to display the ending (WorldState.endingId is
    /// already set and saved).
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
