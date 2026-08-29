using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Phase 6 developer overlay: cheat panel + live timeline inspector.
/// Toggle with the backtick/tilde key (KeyCode.BackQuote). Only draws in the
/// editor or development builds, so it's automatically absent from release
/// builds. RunManager.GetOrCreate() attaches this to its persistent
/// GameObject, so it's available from any scene with no scene wiring.
/// </summary>
public sealed class DebugPanelController : MonoBehaviour
{
    /// <summary>Overlay panel size in screen pixels.</summary>
    private const float PanelWidth = 440f;
    private const float PanelHeight = 480f;

    /// <summary>Tab labels for the toolbar.</summary>
    private static readonly string[] TabLabels = { "Cheats", "Timeline Inspector" };

    /// <summary>True while the overlay is visible.</summary>
    private bool _visible;

    /// <summary>Currently selected tab (0 = Cheats, 1 = Timeline Inspector).</summary>
    private int _tab;

    /// <summary>Scroll position for the active tab's content.</summary>
    private Vector2 _scroll;

    /// <summary>Text field contents for the flag set/clear cheat.</summary>
    private string _flagInput = string.Empty;

    /// <summary>Logs that the overlay has been attached and how to open it.</summary>
    private void Awake()
    {
        Debug.Log("[DebugPanelController] Attached to persistent RunManager object (press ~ to toggle the dev overlay).");
    }

    /// <summary>Watches for the toggle key.</summary>
    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.backquoteKey.wasPressedThisFrame)
        {
            _visible = !_visible;
            _scroll = Vector2.zero;
            Debug.Log($"[DebugPanelController] Overlay {(_visible ? "opened" : "closed")} (~ pressed).");
        }
    }

    /// <summary>Draws the overlay (editor / development builds only).</summary>
    private void OnGUI()
    {
        if (!Application.isEditor && !Debug.isDebugBuild)
            return;

        if (!_visible)
            return;

        RunManager run = RunManager.HasInstance ? RunManager.Instance : null;
        WorldState world = run != null ? run.World : null;
        ContentLibrarySO lib = run != null ? run.Library : null;

        var rect = new Rect(10f, 10f, PanelWidth, PanelHeight);
        GUILayout.BeginArea(rect, GUI.skin.box);

        GUILayout.Label("NOPE Dev Tools (~ to toggle)");

        if (run == null || world == null)
        {
            GUILayout.Label("No active run yet (RunManager not ready).");
            GUILayout.EndArea();
            return;
        }

        GUILayout.Label($"Day {world.day}   Money {world.money}   Stability {world.timelineStability:0.#}   Ending '{world.endingId}'");

        _tab = GUILayout.Toolbar(_tab, TabLabels);
        GUILayout.Space(4f);

        _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.Height(PanelHeight - 90f));

        if (_tab == 0)
            DrawCheatsTab(run, world, lib);
        else
            DrawInspectorTab(world, lib);

        GUILayout.EndScrollView();

        GUILayout.EndArea();
    }

    /// <summary>Cheats tab: day skip, money/stability adjust, flags, force legendary, upgrades.</summary>
    private void DrawCheatsTab(RunManager run, WorldState world, ContentLibrarySO lib)
    {
        GUILayout.Label("Day flow");

        if (GUILayout.Button("Skip Day (nightly resolve + advance)"))
        {
            Debug.Log("[DebugPanelController] Cheat: Skip Day requested.");
            run.AdvanceToNextDay();
        }

        GUILayout.Space(6f);
        GUILayout.Label("Money");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("+10")) AddMoney(world, 10);
        if (GUILayout.Button("+50")) AddMoney(world, 50);
        if (GUILayout.Button("+100")) AddMoney(world, 100);
        if (GUILayout.Button("-50")) AddMoney(world, -50);
        GUILayout.EndHorizontal();

        GUILayout.Space(6f);
        GUILayout.Label("Timeline stability");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("+10")) AddStability(world, 10f);
        if (GUILayout.Button("-10")) AddStability(world, -10f);
        if (GUILayout.Button("Set 0 (fire test)")) SetStability(world, 0f);
        if (GUILayout.Button("Set 100")) SetStability(world, 100f);
        GUILayout.EndHorizontal();

        GUILayout.Space(6f);
        GUILayout.Label("Flags");
        GUILayout.BeginHorizontal();
        _flagInput = GUILayout.TextField(_flagInput, GUILayout.Width(240f));

        if (GUILayout.Button("Set", GUILayout.Width(50f)) && !string.IsNullOrWhiteSpace(_flagInput))
        {
            Debug.Log($"[DebugPanelController] Cheat: SetFlag('{_flagInput.Trim()}').");
            world.SetFlag(_flagInput.Trim());
        }

        if (GUILayout.Button("Clear", GUILayout.Width(50f)) && !string.IsNullOrWhiteSpace(_flagInput))
        {
            Debug.Log($"[DebugPanelController] Cheat: ClearFlag('{_flagInput.Trim()}').");
            world.ClearFlag(_flagInput.Trim());
        }

        GUILayout.EndHorizontal();

        GUILayout.Label($"Active flags ({world.flags.Count}):");

        for (int i = world.flags.Count - 1; i >= 0; i--)
        {
            string flag = world.flags[i];

            GUILayout.BeginHorizontal();
            GUILayout.Label("  " + flag);

            if (GUILayout.Button("Clear", GUILayout.Width(50f)))
            {
                Debug.Log($"[DebugPanelController] Cheat: ClearFlag('{flag}') (from active list).");
                world.ClearFlag(flag);
            }

            GUILayout.EndHorizontal();
        }

        GUILayout.Space(6f);

        bool forced = DevToolsState.ForceLegendaryNextCase;
        bool newForced = GUILayout.Toggle(forced, "Force legendary on next generated case");

        if (newForced != forced)
        {
            Debug.Log($"[DebugPanelController] Cheat: ForceLegendaryNextCase set to {newForced}.");
            DevToolsState.ForceLegendaryNextCase = newForced;
        }

        GUILayout.Space(6f);
        GUILayout.Label("Upgrades");

        if (lib == null)
        {
            GUILayout.Label("  No ContentLibrary available.");
            return;
        }

        foreach (UpgradeSO upgrade in lib.Upgrades)
        {
            if (upgrade == null)
                continue;

            bool owned = world.HasUpgrade(upgrade.id);

            GUILayout.BeginHorizontal();
            GUILayout.Label($"  {upgrade.displayName} ({upgrade.id})", GUILayout.Width(260f));

            if (owned)
            {
                if (GUILayout.Button("Lock", GUILayout.Width(60f)))
                {
                    Debug.Log($"[DebugPanelController] Cheat: removing upgrade '{upgrade.id}'.");
                    world.unlockedUpgradeIds.Remove(upgrade.id);
                }
            }
            else
            {
                if (GUILayout.Button("Unlock", GUILayout.Width(60f)))
                {
                    Debug.Log($"[DebugPanelController] Cheat: unlocking upgrade '{upgrade.id}'.");
                    world.UnlockUpgrade(upgrade.id);
                }
            }

            GUILayout.EndHorizontal();
        }
    }

    /// <summary>Timeline Inspector tab: live scores, dominance tiers, active effects.</summary>
    private void DrawInspectorTab(WorldState world, ContentLibrarySO lib)
    {
        if (GUILayout.Button("Dump full state to console"))
            DumpStateToConsole(world, lib);

        GUILayout.Space(6f);
        GUILayout.Label($"Scores ({world.timeline.scores.Count}):");
        GUILayout.Label(TimelineScoreDisplay.FormatGrouped(world, lib));

        GUILayout.Space(6f);
        GUILayout.Label($"Dominant keys ({world.timeline.dominantKeys.Count}):");

        foreach (string key in world.timeline.dominantKeys)
            GUILayout.Label("  " + key);

        GUILayout.Space(6f);
        GUILayout.Label($"Supporting keys ({world.timeline.supportingKeys.Count}):");

        foreach (string key in world.timeline.supportingKeys)
            GUILayout.Label("  " + key);

        GUILayout.Space(6f);
        GUILayout.Label($"Active effects ({world.timeline.activeEffects.Count}):");

        foreach (ActiveEffectEntry entry in world.timeline.activeEffects)
        {
            EffectSO effect = lib != null ? lib.GetEffectByAssetName(entry.effectId) : null;
            string name = effect != null ? effect.displayName : entry.effectId;

            string remaining = entry.durationDays < 0
                ? "permanent"
                : $"{Mathf.Max(0, entry.startDay + entry.durationDays - world.day)}d left";

            GUILayout.Label($"  {name} — {entry.sourceLabel} (started day {entry.startDay}, {remaining})");
        }
    }

    /// <summary>Adds to world.money and logs the change.</summary>
    private static void AddMoney(WorldState world, int delta)
    {
        int before = world.money;
        world.money += delta;
        Debug.Log($"[DebugPanelController] Cheat: money {before} -> {world.money} ({delta:+0;-0}).");
    }

    /// <summary>Adds to world.timelineStability (clamped 0..100) and logs the change.</summary>
    private static void AddStability(WorldState world, float delta)
    {
        float before = world.timelineStability;
        world.timelineStability = Mathf.Clamp(world.timelineStability + delta, 0f, 100f);
        Debug.Log($"[DebugPanelController] Cheat: stability {before:0.#} -> {world.timelineStability:0.#} ({delta:+0.#;-0.#}).");
    }

    /// <summary>Sets world.timelineStability (clamped 0..100) and logs the change.</summary>
    private static void SetStability(WorldState world, float value)
    {
        float before = world.timelineStability;
        world.timelineStability = Mathf.Clamp(value, 0f, 100f);
        Debug.Log($"[DebugPanelController] Cheat: stability {before:0.#} -> {world.timelineStability:0.#} (set).");
    }

    /// <summary>Logs a full snapshot of WorldState + timeline data to the console.</summary>
    private static void DumpStateToConsole(WorldState world, ContentLibrarySO lib)
    {
        var sb = new StringBuilder();

        sb.AppendLine("[DebugPanelController] ---- State dump ----");
        sb.AppendLine($"Day {world.day}, money={world.money}, stability={world.timelineStability:0.#}, endingId='{world.endingId}'.");
        sb.AppendLine($"legendaryChanceBonus={world.legendaryChanceBonus:0.##}, forgeryChanceModifier={world.forgeryChanceModifier:0.##}, payRateMultiplier={world.payRateMultiplier:0.##}.");
        sb.AppendLine($"Flags ({world.flags.Count}): {string.Join(", ", world.flags)}");
        sb.AppendLine($"Unlocked upgrades ({world.unlockedUpgradeIds.Count}): {string.Join(", ", world.unlockedUpgradeIds)}");

        sb.AppendLine($"Counters ({world.counters.Count}):");
        foreach (CounterEntry c in world.counters)
            sb.AppendLine($"  {c.key} = {c.value}");

        sb.AppendLine($"Scores ({world.timeline.scores.Count}):");
        sb.AppendLine(TimelineScoreDisplay.FormatGrouped(world, lib));

        sb.AppendLine($"Dominant: {string.Join(", ", world.timeline.dominantKeys)}");
        sb.AppendLine($"Supporting: {string.Join(", ", world.timeline.supportingKeys)}");

        sb.AppendLine($"Active effects ({world.timeline.activeEffects.Count}):");
        foreach (ActiveEffectEntry entry in world.timeline.activeEffects)
        {
            EffectSO effect = lib != null ? lib.GetEffectByAssetName(entry.effectId) : null;
            string name = effect != null ? effect.displayName : entry.effectId;
            sb.AppendLine($"  {name} — {entry.sourceLabel} (startDay={entry.startDay}, durationDays={entry.durationDays})");
        }

        Debug.Log(sb.ToString());
    }
}
