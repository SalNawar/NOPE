using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>A cheat of the dev overlay (DebugPanelController), queued by a click in a GUI pass and run after it.</summary>
public enum DevCheat
{
    /// <summary>Nothing queued.</summary>
    None,
    /// <summary>Skip the day (through Sleep: endings, the nightly resolve, the advance).</summary>
    SkipDay,
    /// <summary>Force a nation's leader (the text: its id; none: no leader).</summary>
    ForceLeader,
    /// <summary>Add the number to the wallet.</summary>
    AddMoney,
    /// <summary>Add the amount to the timeline stability (a flat step, not the compounding rule).</summary>
    AddStability,
    /// <summary>Set the timeline stability to the amount.</summary>
    SetStability,
    /// <summary>Set the text as a flag (the flag field's Set).</summary>
    SetFlag,
    /// <summary>Clear the text's flag (the flag field's Clear).</summary>
    ClearFlag,
    /// <summary>Clear the text's flag (its row in the active flags).</summary>
    ClearListedFlag,
    /// <summary>Force every generated case's personality to the text (none: drawn again).</summary>
    ForcePersonality,
    /// <summary>Force the costume error (the number) on the next generated case.</summary>
    ForceCostumeError,
    /// <summary>Remove the text's upgrade from the owned ones.</summary>
    LockUpgrade,
    /// <summary>Own the text's upgrade.</summary>
    UnlockUpgrade,
    /// <summary>The cheat menu: the start of day N's shift (the number; RunManager.JumpToDay).</summary>
    JumpToDay,
    /// <summary>The cheat menu: today's shift from its briefing (RunManager.RestartShift).</summary>
    RestartShift,
    /// <summary>The cheat menu: the traveller decided correctly (GameManager.CheatDecideCorrectly).</summary>
    DecideCorrectly,
    /// <summary>The cheat menu: every traveller decided correctly as they arrive (the number: 1 on, 0 off).</summary>
    AutoDecide,
    /// <summary>The cheat menu: closing time now (GameManager.CheatEndShift).</summary>
    EndShift,
    /// <summary>The cheat menu: the traveller's faults named and their boxes marked (GameManager.CheatRevealFaults).</summary>
    RevealFaults,
    /// <summary>The cheat menu: every introduction in force (the number: 1 on, 0 off; a running shift restarts with it).</summary>
    UnlockEverything,
    /// <summary>The cheat menu: every Orders and House item owned (a running shift restarts with them).</summary>
    GiveAllItems,
    /// <summary>The cheat menu: the desk tutorial skipped (the number 0) or replayed (1).</summary>
    Tutorial,
    /// <summary>The cheat menu: the text's appearance (an AppearanceChoices key) in slot 1 of the next shift (a running shift restarts with it).</summary>
    ForceAppearance,
    /// <summary>The cheat menu: the hall's time of day, the next of TimesOfDay.</summary>
    TimeOfDay,
    /// <summary>Another track's cheat (the text: its Register label).</summary>
    Extra
}

/// <summary>
/// The dev overlay's cheats (DebugPanelController: the editor, development
/// builds and the Windows demo while DemoBuild.DemoCheats is on): each changes
/// the run, the shift (GameManager's cheat entry points) or the dev switches
/// (DevToolsState) and logs what it did. Every cheat marks the run cheated
/// (WorldState.cheated, saved with it). The overlay queues a click's cheat
/// during its GUI pass and runs it here after the pass (audit R2-003), so the
/// drawing itself builds no log line. Another track adds its own cheats
/// through Register (the translation lens's abilities, Saleh 2026-10-06).
/// </summary>
public static class DevCheats
{
    /// <summary>The cheat menu's time-of-day choices in toggle order (null: the shift clock's hour), and their names.</summary>
    public static readonly (float? hour, string name)[] TimesOfDay = { (null, "Clock"), (9f, "Morning"), (18.5f, "Dusk"), (22f, "Night") };

    /// <summary>What the last cheat said (the overlay shows it under the buttons).</summary>
    public static string LastResult { get; private set; } = string.Empty;

    /// <summary>The other tracks' cheats by label, in registration order (Register).</summary>
    private static readonly List<(string label, Action<RunManager> run)> Extras = new List<(string, Action<RunManager>)>();

    /// <summary>The other tracks' cheats' labels, in registration order (the overlay draws a button for each).</summary>
    public static IEnumerable<string> ExtraLabels => Extras.Select(e => e.label);

    /// <summary>
    /// The hook for another track's cheat (Saleh 2026-10-06: "translation
    /// abilities (coordinate: just expose a hook the other track can fill)"):
    /// <paramref name="run"/> runs, as the overlay's other cheats do (queued
    /// after the GUI pass, the run marked cheated), when its button
    /// <paramref name="label"/> is clicked under "More" in the Play tab. A
    /// second registration of a label replaces the first. Call it once, from
    /// the owning system's start (a [RuntimeInitializeOnLoadMethod] works).
    /// </summary>
    public static void Register(string label, Action<RunManager> run)
    {
        if (string.IsNullOrWhiteSpace(label) || run == null)
            return;
        Extras.RemoveAll(e => e.label == label);
        Extras.Add((label, run));
    }

    /// <summary>
    /// The cheat menu's forced appearances: each premade of the library (the
    /// famous travellers and the story characters) as "premade:id", then each
    /// day's authored story beat (a forced slot of a day plan with a premade
    /// or an id) as "beat:day:index", with a label to draw. Forced, it stands
    /// first in the shift of its day (ForceDay): a premade today when today's
    /// plan lists it, else on the first day a plan lists it (their place is in
    /// that day's world, so their papers print it); a story beat on its own
    /// day.
    /// </summary>
    public static List<(string key, string label)> AppearanceChoices(ContentLibrarySO lib)
    {
        var choices = new List<(string, string)>();
        if (lib == null)
            return choices;
        foreach (LegendarySO premade in lib.Legendaries)
            if (premade != null && !string.IsNullOrWhiteSpace(premade.id))
                choices.Add(("premade:" + premade.id, premade.displayName));
        foreach (DayPlanSO plan in lib.DayPlans.Where(p => p != null).OrderBy(p => p.DayNumber))
            for (int i = 0; i < plan.ForcedCases.Count; i++)
                if (plan.ForcedCases[i] != null && (plan.ForcedCases[i].legendary != null || !string.IsNullOrWhiteSpace(plan.ForcedCases[i].id)))
                    choices.Add(($"beat:{plan.DayNumber}:{i}", $"D{plan.DayNumber} {CaseFactory.Describe(plan.ForcedCases[i])}"));
        return choices;
    }

    /// <summary>
    /// The appearance an AppearanceChoices key names, standing in slot 1 (a
    /// copy: the day plan's own entry is untouched), with a blueprint of its
    /// premade's kind when it names none (a day whose kinds lack it still
    /// draws one); null for an unknown key.
    /// </summary>
    public static ForcedCaseSlot Appearance(ContentLibrarySO lib, string key)
    {
        if (lib == null || string.IsNullOrEmpty(key))
            return null;
        string[] parts = key.Split(':');
        ForcedCaseSlot slot = null;
        if (parts[0] == "premade" && parts.Length == 2)
        {
            LegendarySO premade = lib.Legendaries.FirstOrDefault(l => l != null && l.id == parts[1]);
            slot = premade == null ? null : new ForcedCaseSlot { caseIndex1Based = 1, id = "cheat:" + premade.id, legendary = premade };
        }
        else if (parts[0] == "beat" && parts.Length == 3 && int.TryParse(parts[1], out int day) && int.TryParse(parts[2], out int index))
        {
            DayPlanSO plan = lib.DayPlans.FirstOrDefault(p => p != null && p.DayNumber == day);
            ForcedCaseSlot beat = plan != null && index >= 0 && index < plan.ForcedCases.Count ? plan.ForcedCases[index] : null;
            slot = beat == null ? null : new ForcedCaseSlot
            {
                caseIndex1Based = 1, id = beat.id, caseBlueprint = beat.caseBlueprint, legendary = beat.legendary, hasLie = beat.hasLie, lie = beat.lie,
                directive = beat.directive, dialogId = beat.dialogId, introLine = beat.introLine
            };
        }
        if (slot != null && slot.caseBlueprint == null && slot.legendary != null)
            slot.caseBlueprint = lib.DayPlans.Where(p => p != null).SelectMany(p => p.Kinds)
                .Where(k => k != null && k.blueprint != null && k.blueprint.Kind == slot.legendary.kind).Select(k => k.blueprint).FirstOrDefault();
        return slot;
    }

    /// <summary>The day an AppearanceChoices key stands on when forced (see AppearanceChoices): <paramref name="today"/> for a premade today's plan lists (its pool or a forced slot), else the first day a plan lists it (today when none does); a story beat's own day.</summary>
    public static int ForceDay(ContentLibrarySO lib, string key, int today)
    {
        string[] parts = (key ?? string.Empty).Split(':');
        if (parts[0] == "beat" && parts.Length == 3 && int.TryParse(parts[1], out int day))
            return day;
        if (lib == null || parts[0] != "premade" || parts.Length != 2)
            return today;
        bool Lists(DayPlanSO p) => p != null && ((p.AvailableLegendaries ?? Array.Empty<LegendarySO>()).Any(l => l != null && l.id == parts[1])
                                                  || p.ForcedCases.Any(f => f != null && f.legendary != null && f.legendary.id == parts[1]));
        if (Lists(lib.GetDayPlan(today)))
            return today;
        return lib.DayPlans.Where(Lists).Select(p => p.DayNumber).DefaultIfEmpty(today).Min();
    }

    /// <summary>Runs <paramref name="cheat"/> on the run with its argument (<paramref name="number"/>, <paramref name="amount"/> or <paramref name="text"/>, as the cheat reads it), marking the run cheated; LastResult says what it did.</summary>
    public static void Run(DevCheat cheat, RunManager run, int number, float amount, string text)
    {
        WorldState world = run != null ? run.World : null;
        if (world == null || cheat == DevCheat.None)
            return;
        world.cheated = true;
        ContentLibrarySO lib = run.Library;
        GameManager game = GameManager.Current;
        LastResult = string.Empty;
        switch (cheat)
        {
            case DevCheat.SkipDay:
                Say("Skip Day requested (through Sleep).");
                run.Sleep();
                break;
            case DevCheat.ForceLeader:
                Say(text == null ? "No leader." : $"Force leader '{text}'.");
                HistoryService.ForceLeader(world, lib, text);
                break;
            case DevCheat.AddMoney:
                int before = world.money;
                world.money += number;
                Say($"money {before} -> {world.money} ({number:+0;-0}).");
                if (game != null)
                    game.CheatRefreshHud();
                break;
            case DevCheat.AddStability:
                float was = world.timelineStability;
                world.timelineStability = StabilityRules.Round(world.timelineStability + amount);
                Say($"stability {StabilityRules.Format(was)} -> {StabilityRules.Format(world.timelineStability)} ({StabilityRules.FormatChange(amount)}).");
                if (game != null)
                    game.CheatRefreshHud();
                break;
            case DevCheat.SetStability:
                float old = world.timelineStability;
                world.timelineStability = StabilityRules.Round(amount);
                Say($"stability {StabilityRules.Format(old)} -> {StabilityRules.Format(world.timelineStability)} (set).");
                if (game != null)
                    game.CheatRefreshHud();
                break;
            case DevCheat.SetFlag:
                Say($"SetFlag('{text}').");
                world.SetFlag(text);
                break;
            case DevCheat.ClearFlag:
                Say($"ClearFlag('{text}').");
                world.ClearFlag(text);
                break;
            case DevCheat.ClearListedFlag:
                Say($"ClearFlag('{text}') (from active list).");
                world.ClearFlag(text);
                break;
            case DevCheat.ForcePersonality:
                Say($"ForcedPersonality set to '{text ?? "drawn"}' (from the next generation).");
                DevToolsState.ForcedPersonality = text;
                break;
            case DevCheat.ForceCostumeError:
                var error = (CostumeError)number;
                Say($"ForcedCostumeError set to {error} (from the next day's generation).");
                DevToolsState.ForcedCostumeError = error;
                break;
            case DevCheat.LockUpgrade:
                Say($"removing upgrade '{text}'.");
                world.unlockedUpgradeIds.Remove(text);
                break;
            case DevCheat.UnlockUpgrade:
                Say($"unlocking upgrade '{text}'.");
                world.UnlockUpgrade(text);
                break;
            case DevCheat.JumpToDay:
                Say($"Day {number}: the shift starts.");
                run.JumpToDay(number);
                break;
            case DevCheat.RestartShift:
                Say($"Day {world.day}: the shift starts over.");
                run.RestartShift();
                break;
            case DevCheat.DecideCorrectly:
                Say(game != null ? game.CheatDecideCorrectly() : "Not in the office.");
                break;
            case DevCheat.AutoDecide:
                if (game != null)
                    Say(game.CheatSetAutoDecide(number != 0));
                else
                {
                    DevToolsState.AutoDecide = number != 0;
                    Say($"Auto-decide {(DevToolsState.AutoDecide ? "on, from the next shift" : "off")}.");
                }
                break;
            case DevCheat.EndShift:
                Say(game != null ? game.CheatEndShift() : "Not in the office.");
                break;
            case DevCheat.RevealFaults:
                Say(game != null ? game.CheatRevealFaults() : "Not in the office.");
                break;
            case DevCheat.UnlockEverything:
                DevToolsState.UnlockEverything = number != 0;
                Say($"Unlock everything {(DevToolsState.UnlockEverything ? "on" : "off")}{(game != null ? ": today's shift starts over with it." : ", from the next shift.")}");
                RestartIfInShift(run, game);
                break;
            case DevCheat.GiveAllItems:
                int granted = lib != null ? lib.Upgrades.Count(u => u != null && OrderBook.Grant(world, u)) : 0;
                Say($"{granted} Orders and House item(s) given{(game != null ? ": today's shift starts over with them." : ", in force from the next shift.")}");
                RestartIfInShift(run, game);
                break;
            case DevCheat.Tutorial:
                Say(game != null ? game.CheatTutorial(number != 0) : "Not in the office.");
                break;
            case DevCheat.ForceAppearance:
                ForcedCaseSlot appearance = Appearance(lib, text);
                if (appearance == null)
                {
                    Say($"No appearance '{text}'.");
                    break;
                }
                DevToolsState.ForcedAppearance = appearance;
                int on = ForceDay(lib, text, world.day);
                if (on != world.day)
                {
                    Say($"'{CaseFactory.Describe(appearance)}' stands first on day {on}, their day.");
                    run.JumpToDay(on);
                    break;
                }
                Say($"'{CaseFactory.Describe(appearance)}' stands first{(game != null ? ": today's shift starts over with them." : " in the next shift.")}");
                RestartIfInShift(run, game);
                break;
            case DevCheat.TimeOfDay:
                int next = (Array.FindIndex(TimesOfDay, t => Nullable.Equals(t.hour, DevToolsState.ForcedHour)) + 1) % TimesOfDay.Length;
                DevToolsState.ForcedHour = TimesOfDay[next].hour;
                Say($"Time of day: {TimesOfDay[next].name}.");
                break;
            case DevCheat.Extra:
                (string label, Action<RunManager> extra) = Extras.FirstOrDefault(e => e.label == text);
                if (extra == null)
                {
                    Say($"No cheat '{text}'.");
                    break;
                }
                extra(run);
                Say($"{label}: done.");
                break;
        }
    }

    /// <summary>A switch that applies from a shift's start: a running shift starts over with it (RunManager.RestartShift).</summary>
    private static void RestartIfInShift(RunManager run, GameManager game)
    {
        if (game != null)
            run.RestartShift();
    }

    /// <summary>Logs a cheat's line and keeps it for the overlay.</summary>
    private static void Say(string line)
    {
        LastResult = line ?? string.Empty;
        Debug.Log("[DebugPanelController] Cheat: " + LastResult);
    }

    /// <summary>A dev switch flipped outside Run: the run is marked cheated.</summary>
    private static void MarkCheated()
    {
        if (RunManager.HasInstance && RunManager.Instance.World != null)
            RunManager.Instance.World.cheated = true;
    }

    /// <summary>The "Force legendary on next generated case" switch, logged (the run is marked cheated).</summary>
    public static void SetForceLegendary(bool on)
    {
        MarkCheated();
        Debug.Log($"[DebugPanelController] Cheat: ForceLegendaryNextCase set to {on}.");
        DevToolsState.ForceLegendaryNextCase = on;
    }

    /// <summary>The "Force strandings" switch, logged (the run is marked cheated).</summary>
    public static void SetForceStrandings(bool on)
    {
        MarkCheated();
        Debug.Log($"[DebugPanelController] Cheat: ForceStrandings set to {on}.");
        DevToolsState.ForceStrandings = on;
    }

    /// <summary>The "Stranding fate" choice (null: the fate stream's draw), logged (the run is marked cheated).</summary>
    public static void ForceStrandingFate(StrandingFate? fate)
    {
        MarkCheated();
        Debug.Log($"[DebugPanelController] Cheat: ForcedStrandingFate set to {(fate.HasValue ? fate.Value.ToString() : "drawn")}.");
        DevToolsState.ForcedStrandingFate = fate;
    }
}
