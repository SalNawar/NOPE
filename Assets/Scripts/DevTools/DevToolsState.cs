using UnityEngine;

/// <summary>
/// Runtime-only developer cheat state (Phase 6). Never serialized into
/// WorldState/SaveSystem — these are session-local overrides used by the
/// debug panel to influence generation/scoring for testing.
/// </summary>
public static class DevToolsState
{
    /// <summary>
    /// When true, CaseFactory.TryRollLegendary forces the next case to be a
    /// legendary (if any legendary is available for the current day) and
    /// then resets this flag back to false.
    /// </summary>
    public static bool ForceLegendaryNextCase;

    /// <summary>
    /// Resets all dev cheat state. Called by RunManager.NewRun()/ContinueRun()
    /// so leftover toggles from a previous run don't bleed into a new one.
    /// </summary>
    public static void ResetAll()
    {
        if (ForceLegendaryNextCase)
            Debug.Log("[DevToolsState] ResetAll: clearing ForceLegendaryNextCase.");

        ForceLegendaryNextCase = false;
    }
}
