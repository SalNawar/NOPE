using System.Collections.Generic;

/// <summary>
/// Concrete per-day schedule where all randomness is already resolved.
/// Lookups are fast at runtime: trigger + caseIndex -> list of events.
/// </summary>
public sealed class ResolvedDaySchedule
{
    /// <summary>
    /// Internal mapping: trigger -> (caseIndex1Based -> events list).
    /// </summary>
    private readonly Dictionary<DayEventTrigger, Dictionary<int, List<DayEventSO>>> _map = new();

    /// <summary>
    /// Adds an event to the schedule for a specific trigger and case slot (1-based).
    /// </summary>
    public void Add(DayEventTrigger trigger, int caseIndex1Based, DayEventSO dayEvent)
    {
        if (dayEvent == null)
            return;

        if (!_map.TryGetValue(trigger, out Dictionary<int, List<DayEventSO>> byIndex))
        {
            byIndex = new Dictionary<int, List<DayEventSO>>();
            _map.Add(trigger, byIndex);
        }

        if (!byIndex.TryGetValue(caseIndex1Based, out List<DayEventSO> list))
        {
            list = new List<DayEventSO>();
            byIndex.Add(caseIndex1Based, list);
        }

        list.Add(dayEvent);
    }

    /// <summary>
    /// Gets the list of events for a trigger + case slot (1-based).
    /// Returns null if none exist.
    /// </summary>
    public List<DayEventSO> Get(DayEventTrigger trigger, int caseIndex1Based)
    {
        return _map.TryGetValue(trigger, out Dictionary<int, List<DayEventSO>> byIndex) &&
               byIndex.TryGetValue(caseIndex1Based, out List<DayEventSO> list)
            ? list
            : null;
    }
}
