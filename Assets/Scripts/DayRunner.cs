using UnityEngine;

/// <summary>
/// Minimal harness that starts a day and auto-resolves each case slot
/// so you can confirm the timeline + events work before UI.
/// </summary>
public sealed class DayRunner : MonoBehaviour
{
    [SerializeField] private DayOrchestrator orchestrator;
    [SerializeField] private DayPlanSO dayPlan;
    [SerializeField] private int seed = 12345;

    /// <summary>Runtime world state for this test run.</summary>
    private WorldState _worldState;

    /// <summary>
    /// Subscribes to case slot callbacks and starts the day.
    /// </summary>
    private void Start()
    {
        Debug.Log("[DayRunner] >>> Entering Start (standalone test harness).");

        if (orchestrator == null || dayPlan == null)
        {
            Debug.LogError("DayRunner missing references (orchestrator/dayPlan).");
            return;
        }

        _worldState = new WorldState { day = dayPlan.DayNumber };

        orchestrator.OnCaseSlotStarted += HandleCaseStarted;
        orchestrator.OnCaseSlotEnded += HandleCaseEnded;

        Debug.Log($"[DayRunner] <<< Exiting Start (day {_worldState.day}, seed={seed}, starting day loop).");

        orchestrator.StartDay(_worldState, dayPlan, seed);
    }

    /// <summary>
    /// Unsubscribes to prevent leaks when exiting play mode.
    /// </summary>
    private void OnDestroy()
    {
        if (orchestrator == null)
            return;

        orchestrator.OnCaseSlotStarted -= HandleCaseStarted;
        orchestrator.OnCaseSlotEnded -= HandleCaseEnded;
    }

    /// <summary>
    /// Called when a case slot begins. For now, we auto-resolve after a short delay.
    /// Later: build/show the real CaseInstance + UI.
    /// </summary>
    private void HandleCaseStarted(int caseIndex1Based)
    {
        Debug.Log($"[DayRunner] Case slot started: {caseIndex1Based}");
        Invoke(nameof(ResolveCurrentCase), 0.5f);
    }

    /// <summary>
    /// Called when a case slot ends.
    /// </summary>
    private void HandleCaseEnded(int caseIndex1Based)
    {
        Debug.Log($"[DayRunner] Case slot ended: {caseIndex1Based}");
    }

    /// <summary>
    /// Marks the current case as resolved so the orchestrator advances.
    /// </summary>
    private void ResolveCurrentCase()
    {
        orchestrator.MarkCaseResolved();
    }
}
