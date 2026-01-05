// ReSharper disable InconsistentNaming
using System.Collections;
using UnityEngine;

/// <summary>
/// Base class for all day events (cutscenes, rule changes, forced case injection, etc.).
/// Implement Execute() to perform event logic while pausing the day flow.
/// </summary>
public abstract class DayEventSO : ScriptableObject
{
    /// <summary>
    /// Executes the event. The day loop will wait until this coroutine completes.
    /// </summary>
    public abstract IEnumerator Execute(DayEventContext ctx);
}

/// <summary>
/// Runtime context passed to day events so they can affect the game.
/// Keep this minimal and stable to avoid tight coupling.
/// </summary>
public sealed class DayEventContext
{
    /// <summary>The orchestrator currently running the day loop.</summary>
    public DayOrchestrator DayOrchestrator { get; }

    /// <summary>Persistent simulation state for the run/session.</summary>
    public WorldState WorldState { get; }

    /// <summary>
    /// Creates a context container used by events during execution.
    /// </summary>
    public DayEventContext(DayOrchestrator orchestrator, WorldState worldState)
    {
        DayOrchestrator = orchestrator;
        WorldState = worldState;
    }
}
