using UnityEngine;

/// <summary>
/// Runtime-only developer cheat state (Phase 6). Never serialized into
/// WorldState/SaveSystem — these are session-local overrides used by the
/// debug panel to influence generation/scoring for testing (force the next
/// legendary, force the timeline leader, force a costume error, force the
/// strandings, force a personality).
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
    /// While true, every accepted traveller on an Economy transponder is
    /// stranded at the shift's end (ShiftStrandings: the chance reads 1), so a
    /// stranding, its fate, its paper line, its Mail report and its fine can
    /// be seen at will. The draws still run on the day's stream.
    /// </summary>
    public static bool ForceStrandings;

    /// <summary>
    /// While set, every stranded traveller meets this fate instead of the
    /// fate stream's draw (ShiftStrandings; the endings and strandings spec
    /// §6), so each fate can be seen at will. The fate stream still draws.
    /// Null = no force.
    /// </summary>
    public static StrandingFate? ForcedStrandingFate;

    /// <summary>
    /// While set (a Personality.id), every generated traveller who is not a
    /// premade speaks as this personality: CaseFactory draws the personality
    /// as ever (no stream moves) and this overrides it (the personalities
    /// spec's PS4: for testing, never saved). Cases are generated at the
    /// day's start, so it applies from the next generation. Null = no force.
    /// </summary>
    public static string ForcedPersonality;

    /// <summary>
    /// Resets all dev cheat state. Called by RunManager.NewRun()/ContinueRun()
    /// so leftover toggles from a previous run don't bleed into a new one.
    /// </summary>
    public static void ResetAll()
    {
        if (ForceStrandings)
            Debug.Log("[DevToolsState] ResetAll: clearing ForceStrandings.");
        if (ForceLegendaryNextCase)
            Debug.Log("[DevToolsState] ResetAll: clearing ForceLegendaryNextCase.");
        if (ForcedLeaderId != null)
            Debug.Log($"[DevToolsState] ResetAll: clearing ForcedLeaderId '{ForcedLeaderId}'.");
        if (ForcedCostumeError != CostumeError.None)
            Debug.Log($"[DevToolsState] ResetAll: clearing ForcedCostumeError '{ForcedCostumeError}'.");
        if (ForcedPersonality != null)
            Debug.Log($"[DevToolsState] ResetAll: clearing ForcedPersonality '{ForcedPersonality}'.");
        if (ForcedStrandingFate != null)
            Debug.Log($"[DevToolsState] ResetAll: clearing ForcedStrandingFate '{ForcedStrandingFate}'.");

        ForceLegendaryNextCase = false;
        ForcedLeaderId = null;
        ForcedCostumeError = CostumeError.None;
        ForceStrandings = false;
        ForcedPersonality = null;
        ForcedStrandingFate = null;
    }
}
