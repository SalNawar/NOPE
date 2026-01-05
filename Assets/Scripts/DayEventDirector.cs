using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runs day events sequentially and provides a single place to control
/// "pause day flow while event runs".
/// </summary>
public sealed class DayEventDirector : MonoBehaviour
{
    /// <summary>Context passed to every event execution.</summary>
    private DayEventContext _ctx;

    /// <summary>True while at least one event coroutine is running.</summary>
    private bool _isRunningEvent;

    /// <summary>
    /// Initializes the director with the runtime context for this day.
    /// Must be called once at day start.
    /// </summary>
    public void Init(DayEventContext context)
    {
        if (context == null)
        {
            Debug.LogError("DayEventDirector.Init called with null context.");
            _ctx = null;
            return;
        }

        _ctx = context;
    }

    /// <summary>
    /// Runs events sequentially and yields until all are complete.
    /// </summary>
    public IEnumerator RunEvents(List<DayEventSO> events)
    {
        if (_ctx == null || events == null || events.Count == 0)
            yield break;

        _isRunningEvent = true;

        foreach (DayEventSO ev in events)
        {
            if (ev == null)
                continue;

            yield return ev.Execute(_ctx);
        }

        _isRunningEvent = false;
    }

    /// <summary>
    /// Returns true while the director is executing events.
    /// </summary>
    public bool IsRunningEvent()
    {
        return _isRunningEvent;
    }
}
