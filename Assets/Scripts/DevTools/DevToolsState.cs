using UnityEngine;

/// <summary>
/// Runtime-only developer cheat state (Phase 6). Never serialized into
/// WorldState/SaveSystem — these are session-local overrides used by the
/// debug panel to influence generation/scoring for testing (force the next
/// legendary, force the timeline leader, force a costume error, force the
/// strandings, force a personality; the cheat menu's auto-decide, unlock
/// everything, forced appearance and time of day, Saleh 2026-10-06). The run
/// itself only remembers that it was cheated (WorldState.cheated).
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
    /// When set, the next generated traveller who is not a premade, has no
    /// directive fault and carries papers tells this lie (CaseFactory.RollLie;
    /// the cheat menu's "Law-breaker next": someone else's photo, so DETAIN has a
    /// law-breaker to take; the desk machine spec §2), and the flag resets.
    /// Cases are generated at the day's start, so it applies from the next
    /// generation (a restarted shift).
    /// </summary>
    public static LieKind? ForcedLie;

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
    /// While true, every traveller is decided correctly as they reach the desk
    /// (GameManager: the cheat menu's "Auto-decide"; the desk is made
    /// available so the queue keeps coming), with the evidence a denial needs.
    /// </summary>
    public static bool AutoDecide;

    /// <summary>
    /// While true, every feature some day introduces counts as introduced on
    /// any day (ContentLibrarySO.Introductions hands out
    /// Introductions.WithEverything: every paper's field, rule, tool, app, the
    /// scanner, the books), from the next shift start (the cheat menu restarts
    /// a running shift at once).
    /// </summary>
    public static bool UnlockEverything;

    /// <summary>
    /// When set, the next day's generation puts this appearance in slot 1 (a
    /// famous traveller or another day's story beat, the cheat menu's "Force";
    /// CaseFactory's appearances, whatever its conditions or the premade being
    /// met) and the slot is cleared. Null = none.
    /// </summary>
    public static ForcedCaseSlot ForcedAppearance;

    /// <summary>
    /// When set, the anime hall shows this hour (0-24) instead of the shift
    /// clock's (HallLightingRig.Hour: the cheat menu's "Time of day" toggle:
    /// morning, dusk, night); the clock itself runs on. Null = the clock's.
    /// </summary>
    public static float? ForcedHour;

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
        if (ForcedLie != null)
            Debug.Log($"[DevToolsState] ResetAll: clearing ForcedLie '{ForcedLie}'.");
        if (ForcedPersonality != null)
            Debug.Log($"[DevToolsState] ResetAll: clearing ForcedPersonality '{ForcedPersonality}'.");
        if (ForcedStrandingFate != null)
            Debug.Log($"[DevToolsState] ResetAll: clearing ForcedStrandingFate '{ForcedStrandingFate}'.");
        if (AutoDecide || UnlockEverything || ForcedAppearance != null || ForcedHour != null)
            Debug.Log($"[DevToolsState] ResetAll: clearing the cheat menu's switches (auto-decide {AutoDecide}, unlock everything {UnlockEverything}, forced appearance '{ForcedAppearance?.id}', hour {ForcedHour}).");

        ForceLegendaryNextCase = false;
        ForcedLeaderId = null;
        ForcedCostumeError = CostumeError.None;
        ForcedLie = null;
        ForceStrandings = false;
        ForcedPersonality = null;
        ForcedStrandingFate = null;
        AutoDecide = false;
        UnlockEverything = false;
        ForcedAppearance = null;
        ForcedHour = null;
    }
}
