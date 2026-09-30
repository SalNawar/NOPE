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
    UnlockUpgrade
}

/// <summary>
/// The dev overlay's cheats (DebugPanelController, editor and development
/// builds only): each changes the run or the dev switches (DevToolsState) and
/// logs what it did. The overlay queues a click's cheat during its GUI pass
/// and runs it here after the pass (audit R2-003), so the drawing itself
/// builds no log line.
/// </summary>
public static class DevCheats
{
    /// <summary>Runs <paramref name="cheat"/> on the run with its argument (<paramref name="number"/>, <paramref name="amount"/> or <paramref name="text"/>, as the cheat reads it).</summary>
    public static void Run(DevCheat cheat, RunManager run, int number, float amount, string text)
    {
        WorldState world = run != null ? run.World : null;
        if (world == null)
            return;
        ContentLibrarySO lib = run.Library;
        switch (cheat)
        {
            case DevCheat.SkipDay:
                Debug.Log("[DebugPanelController] Cheat: Skip Day requested (through Sleep).");
                run.Sleep();
                break;
            case DevCheat.ForceLeader:
                Debug.Log(text == null ? "[DebugPanelController] Cheat: No leader." : $"[DebugPanelController] Cheat: Force leader '{text}'.");
                HistoryService.ForceLeader(world, lib, text);
                break;
            case DevCheat.AddMoney:
                int before = world.money;
                world.money += number;
                Debug.Log($"[DebugPanelController] Cheat: money {before} -> {world.money} ({number:+0;-0}).");
                break;
            case DevCheat.AddStability:
                float was = world.timelineStability;
                world.timelineStability = StabilityRules.Round(world.timelineStability + amount);
                Debug.Log($"[DebugPanelController] Cheat: stability {StabilityRules.Format(was)} -> {StabilityRules.Format(world.timelineStability)} ({StabilityRules.FormatChange(amount)}).");
                break;
            case DevCheat.SetStability:
                float old = world.timelineStability;
                world.timelineStability = StabilityRules.Round(amount);
                Debug.Log($"[DebugPanelController] Cheat: stability {StabilityRules.Format(old)} -> {StabilityRules.Format(world.timelineStability)} (set).");
                break;
            case DevCheat.SetFlag:
                Debug.Log($"[DebugPanelController] Cheat: SetFlag('{text}').");
                world.SetFlag(text);
                break;
            case DevCheat.ClearFlag:
                Debug.Log($"[DebugPanelController] Cheat: ClearFlag('{text}').");
                world.ClearFlag(text);
                break;
            case DevCheat.ClearListedFlag:
                Debug.Log($"[DebugPanelController] Cheat: ClearFlag('{text}') (from active list).");
                world.ClearFlag(text);
                break;
            case DevCheat.ForcePersonality:
                Debug.Log($"[DebugPanelController] Cheat: ForcedPersonality set to '{text ?? "drawn"}' (from the next generation).");
                DevToolsState.ForcedPersonality = text;
                break;
            case DevCheat.ForceCostumeError:
                var error = (CostumeError)number;
                Debug.Log($"[DebugPanelController] Cheat: ForcedCostumeError set to {error} (from the next day's generation).");
                DevToolsState.ForcedCostumeError = error;
                break;
            case DevCheat.LockUpgrade:
                Debug.Log($"[DebugPanelController] Cheat: removing upgrade '{text}'.");
                world.unlockedUpgradeIds.Remove(text);
                break;
            case DevCheat.UnlockUpgrade:
                Debug.Log($"[DebugPanelController] Cheat: unlocking upgrade '{text}'.");
                world.UnlockUpgrade(text);
                break;
        }
    }

    /// <summary>The "Force legendary on next generated case" switch, logged.</summary>
    public static void SetForceLegendary(bool on)
    {
        Debug.Log($"[DebugPanelController] Cheat: ForceLegendaryNextCase set to {on}.");
        DevToolsState.ForceLegendaryNextCase = on;
    }

    /// <summary>The "Force strandings" switch, logged.</summary>
    public static void SetForceStrandings(bool on)
    {
        Debug.Log($"[DebugPanelController] Cheat: ForceStrandings set to {on}.");
        DevToolsState.ForceStrandings = on;
    }

    /// <summary>The "Stranding fate" choice (null: the fate stream's draw), logged.</summary>
    public static void ForceStrandingFate(StrandingFate? fate)
    {
        Debug.Log($"[DebugPanelController] Cheat: ForcedStrandingFate set to {(fate.HasValue ? fate.Value.ToString() : "drawn")}.");
        DevToolsState.ForcedStrandingFate = fate;
    }
}
