using UnityEngine;

/// <summary>
/// Runtime-only developer cheat state (Phase 6). Never serialized into
/// WorldState/SaveSystem — these are session-local overrides used by the
/// debug panel to influence generation/scoring for testing.
/// </summary>
public static class DevToolsState
{
    /// <summary>
    /// When true, the next slot that rolls a premade (CaseFactory.RollPremade)
    /// holds one from the day's pool (if any can roll) and this flag resets.
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
