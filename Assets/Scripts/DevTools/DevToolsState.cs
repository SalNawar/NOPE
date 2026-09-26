using UnityEngine;

/// <summary>
/// Runtime-only developer cheat state (Phase 6). Never serialized into
/// WorldState/SaveSystem — these are session-local overrides used by the
/// debug panel to influence generation/scoring for testing (force the next
/// legendary, force the timeline leader, force a costume error).
/// </summary>
public static class DevToolsState
{
    /// <summary>
    /// When true, the next slot that rolls a premade (CaseFactory.RollPremade)
    /// holds one from the day's pool (if any can roll) and this flag resets.
    /// </summary>
    public static bool ForceLegendaryNextCase;

    /// <summary>
    /// Session-only override of the timeline leader (debug panel "Force
    /// leader", HistoryService.ForceLeader): while set, the nightly leader step
    /// keeps it instead of deciding from influence. Null = no override.
    /// </summary>
    public static string ForcedLeaderId;

    /// <summary>
    /// When not None, the next generated traveller who is not a premade and
    /// whose destination is open wears this costume error (CaseFactory.PlanCostume;
    /// a planned fault, so they tell no lie), whatever their kind, and the
    /// flag resets. Cases are generated at the day's start, so it applies from
    /// the next day's generation.
    /// </summary>
    public static CostumeError ForcedCostumeError;

    /// <summary>
    /// Resets all dev cheat state. Called by RunManager.NewRun()/ContinueRun()
    /// so leftover toggles from a previous run don't bleed into a new one.
    /// </summary>
    public static void ResetAll()
    {
        if (ForceLegendaryNextCase)
            Debug.Log("[DevToolsState] ResetAll: clearing ForceLegendaryNextCase.");
        if (ForcedLeaderId != null)
            Debug.Log($"[DevToolsState] ResetAll: clearing ForcedLeaderId '{ForcedLeaderId}'.");
        if (ForcedCostumeError != CostumeError.None)
            Debug.Log($"[DevToolsState] ResetAll: clearing ForcedCostumeError '{ForcedCostumeError}'.");

        ForceLegendaryNextCase = false;
        ForcedLeaderId = null;
        ForcedCostumeError = CostumeError.None;
    }
}
