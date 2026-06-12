using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Drives the daily loop:
/// - BeforeCase events
/// - Notify GameManager a case slot started
/// - Wait until GameManager resolves the case
/// - AfterCase events
/// - Next case slot
/// This class has NO UI dependencies.
/// </summary>
public sealed class DayOrchestrator : MonoBehaviour
{
    /// <summary>Runs scripted events (cutscenes, rule changes, etc.).</summary>
    [SerializeField] private DayEventDirector eventDirector;

    /// <summary>Designer-authored plan for the current day.</summary>
    [SerializeField] private DayPlanSO dayPlan;

    /// <summary>Resolved event schedule (randomness removed) for fast lookups.</summary>
    private ResolvedDaySchedule _resolvedSchedule;

    /// <summary>World state for the current run.</summary>
    private WorldState _worldState;

    /// <summary>1-based case index in the current day.</summary>
    private int _caseIndex1Based;

    /// <summary>True while the orchestrator is waiting for a case to be resolved.</summary>
    private bool _waitingForCaseResolution;

    /// <summary>Handle for the currently running day loop coroutine.</summary>
    private Coroutine _dayLoopRoutine;

    /// <summary>
    /// Fired when a case slot begins (1-based case index).
    /// Your GameManager should build/show the case here.
    /// </summary>
    public event Action<int> OnCaseSlotStarted;

    /// <summary>
    /// Fired when a case slot ends (1-based case index).
    /// </summary>
    public event Action<int> OnCaseSlotEnded;

    /// <summary>
    /// Fired once after the last case slot of the day resolves.
    /// GameManager uses this to start the end-of-day flow.
    /// </summary>
    public event Action OnDayCompleted;

    /// <summary>
    /// Starts a day using a plan and world state.
    /// Provide a seed to make random events deterministic.
    /// </summary>
    public void StartDay(WorldState worldState, DayPlanSO plan, int seed)
    {
        // Stop an earlier day loop if this orchestrator is reused.
        if (_dayLoopRoutine != null)
        {
            StopCoroutine(_dayLoopRoutine);
            _dayLoopRoutine = null;
        }

        _worldState = worldState;
        dayPlan = plan;

        // Validate required inputs early to avoid silent deadlocks.
        if (dayPlan == null)
        {
            Debug.LogError("DayOrchestrator.StartDay called with null DayPlanSO.");
            return;
        }

        if (_worldState == null)
        {
            Debug.LogError("DayOrchestrator.StartDay called with null WorldState.");
            return;
        }

        if (eventDirector == null)
        {
            Debug.LogError("DayOrchestrator is missing a DayEventDirector reference.");
            return;
        }

        _caseIndex1Based = 1;

        // Resolve random placements once. This keeps the runtime loop simple and debuggable.
        _resolvedSchedule = dayPlan != null ? dayPlan.ResolveSchedule(seed) : null;

        // Initialize event context for all events.
        if (eventDirector != null && _worldState != null)
            eventDirector.Init(new DayEventContext(this, _worldState));

        // Start the day loop.
        _dayLoopRoutine = StartCoroutine(DayLoop());
    }

    /// <summary>
    /// Called by your GameManager to resume the day flow after the current case is finished.
    /// </summary>
    public void MarkCaseResolved()
    {
        _waitingForCaseResolution = false;
    }

    /// <summary>
    /// Main loop: for each case slot, run before-events, wait for external case resolution,
    /// then run after-events.
    /// </summary>
    private IEnumerator DayLoop()
    {
        if (dayPlan == null)
            yield break;

        int total = Mathf.Max(1, dayPlan.VisitorsCount);

        while (_caseIndex1Based <= total)
        {
            // 1) BeforeCase events
            yield return RunScheduledEvents(DayEventTrigger.BeforeCase, _caseIndex1Based);

            // 2) Notify gameplay layer to start this case slot.
            _waitingForCaseResolution = true;
            OnCaseSlotStarted?.Invoke(_caseIndex1Based);

            // 3) Wait until gameplay layer resolves the case.
            // If you forget to call MarkCaseResolved(), the day will pause here indefinitely.
#if UNITY_EDITOR
            float waitStart = Time.realtimeSinceStartup;
#endif
            yield return new WaitUntil(() =>
            {
#if UNITY_EDITOR
                if (_waitingForCaseResolution && Time.realtimeSinceStartup - waitStart > 30f)
                {
                    Debug.LogWarning($"DayOrchestrator is still waiting for MarkCaseResolved() after 30 seconds (case slot {_caseIndex1Based}).");
                    waitStart = float.PositiveInfinity; // Warn once.
                }
#endif
                return _waitingForCaseResolution == false;
            });

            // 4) Notify slot ended.
            OnCaseSlotEnded?.Invoke(_caseIndex1Based);

            // 5) AfterCase events
            yield return RunScheduledEvents(DayEventTrigger.AfterCase, _caseIndex1Based);

            // 6) Advance
            _caseIndex1Based++;
        }

        // All case slots resolved: the shift is over.
        OnDayCompleted?.Invoke();
    }

    /// <summary>
    /// Runs scheduled events for the given trigger + case slot, if any.
    /// </summary>
    private IEnumerator RunScheduledEvents(DayEventTrigger trigger, int slotIndex1Based)
    {
       