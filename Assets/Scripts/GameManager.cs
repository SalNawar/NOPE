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

    /// <summary>
    /// Initializes systems, generates cases once, and starts the day loop.
    /// </summary>
    private void Start()
    {
        if (orchestrator == null || contentLibrary == null || dayPlan == null || officeUI == null)
        {
            Debug.LogError("GameManager missing references (orchestrator/contentLibrary/dayPlan/officeUI).");
            return;
        }

        // Create world state for this run.
        _worldState = new WorldState { day = dayPlan.DayNumber };

        // Create the case factory from the content library.
        _caseFactory = new CaseFactory(contentLibrary);

        // Generate all cases up-front.
        _dayCases = _caseFactory.GenerateDayCases(dayPlan, _worldState);

        // Subscribe to orchestrator callbacks.
        orchestrator.OnCaseSlotStarted += HandleCaseSlotStarted;
        orchestrator.OnCaseSlotEnded += HandleCaseSlotEnded;

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
    }

    /// <summary>
    /// Called when the orchestrator starts case slot #N (1-based).
    /// Displays the generated case in the UI.
    /// </summary>
    private void HandleCaseSlotStarted(int caseIndex1Based)
    {
        _activeCaseIndex1Based = caseIndex1Based;

        int idx = caseIndex1Based - 1;

        if (_dayCases == null || idx < 0 || idx >= _dayCases.Count)
        {
            Debug.LogError($"GameManager could not find case for slot {caseIndex1Based}.");
            orchestrator.MarkCaseResolved();
            return;
        }

        CaseInstance inst = _dayCases[idx];

        // Show UI and wait for the player's selection.
        officeUI.ShowCase(inst, contentLibrary.Eras, HandlePlayerChoseEra);
    }

    /// <summary>
    /// Called when the orchestrator ends case slot #N (1-based).
    /// </summary>
    private void HandleCaseSlotEnded(int caseIndex1Based)
    {
        // Optional: hide UI between cases, or keep it visible and overwrite contents.
        // officeUI.Hide();
    }

    /// <summary>
    /// Validates the player's choice, shows result, then advances the day.
    /// </summary>
    private void HandlePlayerChoseEra(EraSO chosenEra)
    {
        int idx = _activeCaseIndex1Based - 1;

        if (_dayCases == null || idx < 0 || idx >= _dayCases.Count)
        {
            Debug.LogError("Player chose an era but the active case index is invalid.");
            orchestrator.MarkCaseResolved();
            return;
        }

        CaseInstance inst = _dayCases[idx];

        bool correct = inst.trueEra == chosenEra;

        officeUI.SetResultText(correct ? "✅ Correct" : "❌ Wrong");

        Debug.Log($"[Result] Case {_activeCaseIndex1Based}: chose '{chosenEra.displayName}', true = '{inst.trueEra.displayName}', correct={correct}");
        Debug.Log("Resolving case and moving to next subject...");
        orchestrator.MarkCaseResolved();

    }

    /// <summary>
    /// Marks the current case as resolved so the orchestrator advances to the next slot.
    /// </summary>
    private void ResolveCurrentCase()
    {
        orchestrator.MarkCaseResolved();
    }
}
