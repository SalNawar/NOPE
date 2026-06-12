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

    /// <summary>UI controller for showing the current case.</summary>
    [SerializeField] private OfficeUIController officeUI;

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

    /// <summary>Gameplay tuning, pulled from RunConfig (null-safe).</summary>
    private GameConfigSO _gameConfig;

    /// <summary>Read-only access to the current shift's ledger.</summary>
    public ShiftLedger Ledger => _ledger;

    /// <summary>
    /// Initializes systems, generates cases once, and starts the day loop.
    /// </summary>
    private void Start()
    {
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

        // Initial HUD state.
        officeUI.UpdateHud(_worldState);

        // Subscribe to orchestrator callbacks.
        orchestrator.OnCaseSlotStarted += HandleCaseSlotStarted;
        orchestrator.OnCaseSlotEnded += HandleCaseSlotEnded;
        orchestrator.OnDayCompleted += HandleDayCompleted;

        // Start the day loop.
        orchestrator.StartDay(_worldState, dayPlan, seed);
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
        Debug.Log($"[GameManager] Day {_worldState.day} shift complete.");

        if (officeUI != null)
            officeUI.SetRes