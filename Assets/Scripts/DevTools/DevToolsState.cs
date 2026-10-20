using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Runtime-only developer cheat state (Phase 6). Never serialized into
/// WorldState/SaveSystem — these are session-local overrides used by the
/// debug panel to influence generation/scoring for testing (force the next
/// legendary, force the timeline leader, force a costume error, force the
/// strandings, force a personality; the cheat menu's auto-decide, unlock
/// everything, forced appearance and time of day, Saleh 2026-10-06). The run
/// itself only remembers that it was cheated (WorldState.cheated).
/// Each switch has a scope (RUN-SAFE, BUG-27a): the day's switches
/// (<see cref="DayScoped"/>) are cleared at every morning, so a skip, jump
/// or restart lands on a true start of the day (ResetDay); the run's
/// (<see cref="RunScoped"/>) and the next generation's
/// (<see cref="NextGeneration"/>, which the jump or restart that applies them
/// must keep: a jump holds them aside while it plays the days it passes over,
/// HoldNextGeneration) stay until a new or continued run (ResetAll) and are
/// shown loudly on the cheat badge while on (<see cref="StickyLine"/>). One
/// switch lives elsewhere (<see cref="Elsewhere"/>): the cheat menu's language
/// lock (CultureThemeService.LockOverride, TEXT-LANG's), run-scoped here: on
/// the badge while set, cleared by ResetAll.
/// </summary>
public static class DevToolsState
{
    /// <summary>The switches cleared at every morning (ResetDay): the forced hour, auto-decide and the forced leader (the culture's day reset); the hall's overrides (HallSlotsDev) go with them.</summary>
    public static readonly string[] DayScoped = { nameof(ForcedHour), nameof(AutoDecide), nameof(ForcedLeaderId) };

    /// <summary>The switches kept for the run until a new or continued one (ResetAll), shown on the cheat badge while on.</summary>
    public static readonly string[] RunScoped = { nameof(UnlockEverything), nameof(ForceStrandings), nameof(ForcedStrandingFate), nameof(ForcedPersonality) };

    /// <summary>The switches the next generation consumes (kept through the jump or restart that applies them), shown on the cheat badge while waiting.</summary>
    public static readonly string[] NextGeneration = { nameof(ForceLegendaryNextCase), nameof(ForcedCostumeError), nameof(ForcedLie), nameof(ForcedAppearance) };

    /// <summary>The cheat switches kept outside this class, run-scoped (on the badge while set, cleared by ResetAll): the language lock (CultureThemeService.LockOverride, TEXT-LANG's; BUG-27a).</summary>
    public static readonly string[] Elsewhere = { "CultureThemeService.LockOverride" };

    /// <summary>The next generation's switches held aside (HoldNextGeneration), in <see cref="NextGeneration"/>'s order.</summary>
    public readonly struct HeldSwitches
    {
        /// <summary>Each switch's value when it was held, in NextGeneration's order (null: nothing held).</summary>
        public readonly object[] Values;

        public HeldSwitches(object[] values) => Values = values;
    }

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
    /// Clears the day's switches at a morning (RunManager's every morning: the
    /// night's turn, a skip, a jump, a restart; BUG-27a): the forced hour (the
    /// hall shows the clock's), auto-decide (the day is played by hand), the
    /// culture's day reset (CultureThemeService.ResetForNewDay: the forced
    /// leader) and the hall's overrides (HallSlotsDev, the stand-ins with
    /// them). The run's switches and the next generation's stay.
    /// </summary>
    public static void ResetDay()
    {
        if (AutoDecide || ForcedHour != null || ForcedLeaderId != null)
            Debug.Log($"[DevToolsState] ResetDay: clearing the day's switches (auto-decide {AutoDecide}, hour {ForcedHour}, leader '{ForcedLeaderId}') and the hall's overrides.");

        AutoDecide = false;
        ForcedHour = null;
        CultureThemeService.ResetForNewDay();
        HallSlotsDev.ShowAllStandIns = false;
        HallSlotsDev.Clear();
    }

    /// <summary>
    /// Holds the next generation's switches aside, each back off (RUN-SAFE,
    /// BUG-27a/28a): a cheat jump plays the days it passes over headless
    /// (DayCycle.PlayShiftCorrectly), whose generation would use them up on the
    /// wrong day; CheatRoute holds them for each such day and puts them back
    /// (RestoreNextGeneration), so the day the jump lands on generates with
    /// them. By name over <see cref="NextGeneration"/>, so a switch added there
    /// is held too.
    /// </summary>
    public static HeldSwitches HoldNextGeneration()
    {
        var values = new object[NextGeneration.Length];
        for (int i = 0; i < NextGeneration.Length; i++)
        {
            FieldInfo field = Switch(NextGeneration[i]);
            values[i] = field.GetValue(null);
            field.SetValue(null, field.FieldType.IsValueType ? Activator.CreateInstance(field.FieldType) : null);
        }
        return new HeldSwitches(values);
    }

    /// <summary>Puts the next generation's switches held by HoldNextGeneration back as they were.</summary>
    public static void RestoreNextGeneration(HeldSwitches held)
    {
        if (held.Values == null)
            return;
        for (int i = 0; i < NextGeneration.Length && i < held.Values.Length; i++)
            Switch(NextGeneration[i]).SetValue(null, held.Values[i]);
    }

    /// <summary>The switch field called <paramref name="name"/>.</summary>
    private static FieldInfo Switch(string name) => typeof(DevToolsState).GetField(name, BindingFlags.Public | BindingFlags.Static);

    /// <summary>A number that changes when the badge's line would (the run's switches, the next generation's, the language lock, <paramref name="cheated"/>): the badge rebuilds its line only then, so it allocates nothing per frame.</summary>
    public static int StickyKey(bool cheated)
    {
        unchecked
        {
            int key = cheated ? 1 : 0;
            key = key * 31 + (UnlockEverything ? 1 : 0);
            key = key * 31 + (ForceStrandings ? 1 : 0);
            key = key * 31 + (ForcedStrandingFate.HasValue ? (int)ForcedStrandingFate.Value + 1 : 0);
            key = key * 31 + (ForcedPersonality != null ? ForcedPersonality.GetHashCode() : 0);
            key = key * 31 + (ForceLegendaryNextCase ? 1 : 0);
            key = key * 31 + (int)ForcedCostumeError;
            key = key * 31 + (ForcedLie.HasValue ? (int)ForcedLie.Value + 1 : 0);
            key = key * 31 + (ForcedAppearance != null ? System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(ForcedAppearance) : 0);
            key = key * 31 + (CultureThemeService.LockOverride.HasValue ? (CultureThemeService.LockOverride.Value ? 2 : 1) : 0);
            return key;
        }
    }

    /// <summary>The cheat badge's line (a cheated run's "CHEATS ON", then each run switch, waiting next-generation switch and the language lock that is on, loudly), or "" for a run no cheat touched with nothing on.</summary>
    public static string StickyLine(bool cheated)
    {
        var on = new List<string>();
        if (UnlockEverything)
            on.Add("Unlock everything");
        if (ForceStrandings)
            on.Add("Strandings forced");
        if (ForcedStrandingFate.HasValue)
            on.Add("Stranding fate " + ForcedStrandingFate.Value);
        if (ForcedPersonality != null)
            on.Add("Personality " + ForcedPersonality);
        if (ForceLegendaryNextCase)
            on.Add("Premade next");
        if (ForcedCostumeError != CostumeError.None)
            on.Add("Costume error next " + ForcedCostumeError);
        if (ForcedLie.HasValue)
            on.Add("Lie next " + ForcedLie.Value);
        if (ForcedAppearance != null)
            on.Add("Appearance next " + ForcedAppearance.id);
        if (CultureThemeService.LockOverride.HasValue)
            on.Add(CultureThemeService.LockOverride.Value ? "Language locked" : "Language unlocked");
        if (!cheated && on.Count == 0)
            return string.Empty;
        return on.Count == 0 ? "CHEATS ON" : "CHEATS ON: " + string.Join(" · ", on);
    }

    /// <summary>
    /// Resets all dev cheat state, the day's included (ResetDay), and the
    /// language lock kept elsewhere (CultureThemeService.LockOverride). Called by
    /// RunManager.NewRun()/ContinueRun() so leftover toggles from a previous
    /// run don't bleed into a new one.
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
        if (CultureThemeService.LockOverride != null)
            Debug.Log($"[DevToolsState] ResetAll: clearing the language lock ({CultureThemeService.LockOverride}): the language follows the ramp.");
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
        CultureThemeService.LockOverride = null;
        ResetDay();
    }
}
