using System;
using System.Collections;
using System.Linq;
using UnityEngine;

/// <summary>
/// Drives the daily loop:
/// - BeforeCase events
/// - Notify GameManager a case slot started
/// - Wait until GameManager resolves the case
/// - AfterCase events
/// - Next case slot, until the queue is empty or the booth closes
/// Slot sequencing around closing time lives in the pure DaySlotSequencer.
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

    /// <summary>Per-slot state for today (null until StartDay).</summary>
    private DaySlotSequencer _slots;

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
    /// Fired once when the day is over: after the last slot resolves, or when
    /// the booth closes. GameManager uses this to start the end-of-day flow.
    /// </summary>
    public event Action OnDayCompleted;

    /// <summary>
    /// Starts a day using a plan and world state.
    /// Provide a seed to make random events deterministic.
    /// </summary>
    public void StartDay(WorldState worldState, DayPlanSO plan, int seed)
    {
        Debug.Log($"[DayOrchestrator] >>> Entering StartDay (day {worldState?.day}, plan='{plan?.name}', seed={seed}).");

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

        _slots = new DaySlotSequencer(dayPlan.VisitorsCount);

        // Resolve random placements once. This keeps the runtime loop simple and debuggable.
        _resolvedSchedule = dayPlan != null ? dayPlan.ResolveSchedule(seed) : null;

        // Initialize event context for all events.
        if (eventDirector != null && _worldState != null)
            eventDirector.Init(new DayEventContext(this, _worldState));

        Debug.Log($"[DayOrchestrator] <<< Exiting StartDay (starting day loop with a queue of {_slots.TotalSlots}).");

        // Start the day loop.
        _dayLoopRoutine = StartCoroutine(DayLoop());
    }

    /// <summary>
    /// Called by your GameManager to resume the day flow after the current case is finished.
    /// </summary>
    public void MarkCaseResolved()
    {
        _slots?.MarkResolved();
    }

    /// <summary>
    /// Closing time with a traveller at the desk: finish the current case slot
    /// normally, then end the day instead of starting the next one.
    /// </summary>
    public void CloseAfterCurrentSlot()
    {
        _slots?.CloseAfterCurrentSlot();
    }

    /// <summary>
    /// Closing time with nobody at the desk: end the day at once. If the loop is
    /// waiting on a slot whose traveller was never called in, that slot is
    /// abandoned (no slot-ended or after-case events).
    /// </summary>
    public void CloseNow()
    {
        _slots?.CloseNow();
    }

    /// <summary>
    /// Main loop: for each case slot, run before-events, wait for external case resolution,
    /// then run after-events.
    /// </summary>
    private IEnumerator DayLoop()
    {
        if (dayPlan == null || _slots == null)
            yield break;

        int total = _slots.TotalSlots;

        Debug.Log($"[DayOrchestrator] >>> Entering DayLoop (day {_worldState?.day}, {total} case slot(s)).");

        // True once the current slot's before-case events ran (for the closing report).
        bool beforeEventsRan = false;

        while (_slots.CanStartSlot)
        {
            int slot = _slots.CurrentSlot;
            Debug.Log($"[DayOrchestrator] >>> Entering case slot {slot}/{total}.");

            // 1) BeforeCase events
            yield return RunScheduledEvents(DayEventTrigger.BeforeCase, slot);
            beforeEventsRan = true;

            // The booth may have closed while those events ran.
            if (_slots.CloseRequested)
                break;

            // 2) Notify gameplay layer to start this case slot. The wait begins first
            // so a CloseNow() raised synchronously by a listener is honoured.
            _slots.BeginWaiting();
            OnCaseSlotStarted?.Invoke(slot);

            // 3) Wait until gameplay layer resolves the case (or the booth closes).
            // If you forget to call MarkCaseResolved(), the day will pause here indefinitely.
#if UNITY_EDITOR
            float waitStart = Time.realtimeSinceStartup;
#endif
            yield return new WaitUntil(() =>
            {
#if UNITY_EDITOR
                if (_slots.IsWaiting && Time.realtimeSinceStartup - waitStart > 30f)
                {
                    Debug.LogWarning($"DayOrchestrator is still waiting for MarkCaseResolved() after 30 seconds (case slot {slot}).");
                    waitStart = float.PositiveInfinity; // Warn once.
                }
#endif
                return !_slots.IsWaiting;
            });

            // Closed before this traveller was called in: skip the slot's end and after-case events.
            if (_slots.SlotAbandoned)
            {
                Debug.Log($"[DayOrchestrator] Case slot {slot} abandoned at closing time.");
                break;
            }

            // 4) Notify slot ended.
            OnCaseSlotEnded?.Invoke(slot);

            // 5) AfterCase events
            yield return RunScheduledEvents(DayEventTrigger.AfterCase, slot);

            Debug.Log($"[DayOrchestrator] <<< Exiting case slot {slot}/{total}.");

            // 6) Advance
            _slots.Advance();
            beforeEventsRan = false;
        }

        if (_slots.CloseRequested)
            WarnAboutUnreachedContent(_slots.CurrentSlot, beforeEventsRan, total);

        Debug.Log($"[DayOrchestrator] <<< Exiting DayLoop (day {_worldState?.day} complete, closedEarly={_slots.CloseRequested}, invoking OnDayCompleted).");

        // Queue done or booth closed: the shift is over.
        OnDayCompleted?.Invoke();
    }

    /// <summary>
    /// Closing time cut the queue short: warns (for designers) about scheduled
    /// events and forced cases placed in slots the player never reached.
    /// </summary>
    private void WarnAboutUnreachedContent(int firstUnreachedSlot, bool firstSlotBeforeEventsRan, int total)
    {
        var missed = new System.Collections.Generic.List<string>();
        for (int s = Mathf.Max(1, firstUnreachedSlot); s <= total; s++)
        {
            foreach (DayEventTrigger trigger in new[] { DayEventTrigger.BeforeCase, DayEventTrigger.AfterCase })
            {
                if (s == firstUnreachedSlot && firstSlotBeforeEventsRan && trigger == DayEventTrigger.BeforeCase)
                    continue;

                var events = _resolvedSchedule?.Get(trigger, s);
                if (events != null)
                    missed.AddRange(events.Where(e => e != null).Select(e => $"{e.name} ({trigger}, slot {s})"));
            }

            if (dayPlan != null && dayPlan.TryGetForcedCase(s, out CaseBlueprintSO forced) && forced != null)
                missed.Add($"forced case {forced.name} (slot {s})");
        }

        if (missed.Count > 0)
            Debug.LogWarning($"[DayOrchestrator] The booth closed before slot {firstUnreachedSlot}; never reached: {string.Join(", ", missed)}. Place scheduled content in earlier slots or shorten the queue.");
    }

    /// <summary>
    /// Runs scheduled events for the given trigger + case slot, if any.
    /// </summary>
    private IEnumerator RunScheduledEvents(DayEventTrigger trigger, int slotIndex1Based)
    {
        if (_resolvedSchedule == null || eventDirector == null)
            yield break;

        var events = _resolvedSchedule.Get(trigger, slotIndex1Based);

        if (events == null || events.Count == 0)
            yield break;

        Debug.Log($"[DayOrchestrator] Running {events.Count} scheduled event(s) for trigger={trigger}, slot={slotIndex1Based}: {string.Join(", ", events.Select(e => e != null ? e.name : "<null>"))}.");

        yield return eventDirector.RunEvents(events);
    }
}
