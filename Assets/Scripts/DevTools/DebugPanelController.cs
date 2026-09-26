using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Phase 6 developer overlay: cheat panel + live timeline inspector.
/// Toggle with the backtick/tilde key. RunManager.GetOrCreate() attaches this
/// to its persistent GameObject in the editor and development builds only, so
/// it's available from any scene with no scene wiring and absent from release
/// builds. The component is enabled only while the overlay is open: a closed
/// overlay runs no OnGUI, so IMGUI costs nothing per frame (audit R2-012,
/// R3-030); the key is an input action, heard while the component is off. A
/// cheat clicked in a GUI pass runs after the pass (audit R2-003). The
/// Timeline Inspector tab and the state dump are DebugInspector.
/// </summary>
public sealed class DebugPanelController : MonoBehaviour
{
    /// <summary>Overlay panel size in screen pixels.</summary>
    private const float PanelWidth = 440f;
    private const float PanelHeight = 480f;

    /// <summary>Tab labels for the toolbar.</summary>
    private static readonly string[] TabLabels = { "Cheats", "Timeline Inspector" };

    /// <summary>Currently selected tab (0 = Cheats, 1 = Timeline Inspector).</summary>
    private int _tab;

    /// <summary>Scroll position for the active tab's content.</summary>
    private Vector2 _scroll;

    /// <summary>Text field contents for the flag set/clear cheat.</summary>
    private string _flagInput = string.Empty;

    /// <summary>The toggle key (backtick/tilde), heard while the component is disabled.</summary>
    private InputAction _toggle;

    /// <summary>
    /// What a click in this GUI pass changes (a cheat, the tab), run once the
    /// pass has drawn everything: changing what is drawn mid-pass (a new flag
    /// row, the other tab) would draw more controls than IMGUI's layout pass
    /// counted, and GUILayout throws.
    /// </summary>
    private Action _afterPass;

    /// <summary>Listens for the toggle key and starts closed (disabled: no OnGUI).</summary>
    private void Awake()
    {
        _toggle = new InputAction("DevOverlay", InputActionType.Button, "<Keyboard>/backquote");
        _toggle.performed += OnToggle;
        _toggle.Enable();
        enabled = false;
        Debug.Log("[DebugPanelController] Attached to persistent RunManager object (press ~ to toggle the dev overlay).");
    }

    /// <summary>Stops listening for the toggle key.</summary>
    private void OnDestroy()
    {
        if (_toggle == null)
            return;
        _toggle.performed -= OnToggle;
        _toggle.Dispose();
        _toggle = null;
    }

    /// <summary>Opens or closes the overlay (editor / development builds only).</summary>
    private void OnToggle(InputAction.CallbackContext _)
    {
        if (!Application.isEditor && !Debug.isDebugBuild)
            return;

        enabled = !enabled;
        _scroll = Vector2.zero;
        _afterPass = null;
        Debug.Log($"[DebugPanelController] Overlay {(enabled ? "opened" : "closed")} (~ pressed).");
    }

    /// <summary>Draws the open overlay, then applies what a click in this pass changed.</summary>
    private void OnGUI()
    {
        Draw();

        Action change = _afterPass;
        _afterPass = null;
        change?.Invoke();
    }

    /// <summary>Queues a change to run after this GUI pass (a pass carries one click).</summary>
    private void AfterPass(Action change) => _afterPass = change;

    /// <summary>Draws the overlay's panel and the selected tab.</summary>
    private void Draw()
    {
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

        int tab = GUILayout.Toolbar(_tab, TabLabels);
        if (tab != _tab)
            AfterPass(() => _tab = tab);
        GUILayout.Space(4f);

        _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.Height(PanelHeight - 90f));

        if (_tab == 0)
            DrawCheatsTab(run, world, lib);
        else
            DebugInspector.Draw(world, lib);

        GUILayout.EndScrollView();

        GUILayout.EndArea();
    }

    /// <summary>Cheats tab: day skip, history (force leader), money/stability adjust, flags, force legendary, upgrades.</summary>
    private void DrawCheatsTab(RunManager run, WorldState world, ContentLibrarySO lib)
    {
        GUILayout.Label("Day flow");

        if (GUILayout.Button("Skip Day (sleep: endings + nightly resolve + advance)"))
        {
            AfterPass(() =>
            {
                Debug.Log("[DebugPanelController] Cheat: Skip Day requested (through Sleep).");
                run.Sleep();
            });
        }

        if (lib != null)
        {
            GUILayout.Space(6f);
            GUILayout.Label("History (force leader)");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("No leader"))
            {
                AfterPass(() =>
                {
                    Debug.Log("[DebugPanelController] Cheat: No leader.");
                    HistoryService.ForceLeader(world, lib, null);
                });
            }

            int shown = 1;
            foreach (NationSO nation in lib.Nations)
            {
                if (nation == null)
                    continue;
                if (shown++ % 5 == 0)
                {
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                }
                if (GUILayout.Button(nation.displayName))
                {
                    string nationId = nation.id;
                    AfterPass(() =>
                    {
                        Debug.Log($"[DebugPanelController] Cheat: Force leader '{nationId}'.");
                        HistoryService.ForceLeader(world, lib, nationId);
                    });
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.Label("Kept every night this session; the Future follows at the next office day.");
        }

        GUILayout.Space(6f);
        GUILayout.Label("Money");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("+10")) AfterPass(() => AddMoney(world, 10));
        if (GUILayout.Button("+50")) AfterPass(() => AddMoney(world, 50));
        if (GUILayout.Button("+100")) AfterPass(() => AddMoney(world, 100));
        if (GUILayout.Button("-50")) AfterPass(() => AddMoney(world, -50));
        GUILayout.EndHorizontal();

        GUILayout.Space(6f);
        GUILayout.Label("Timeline stability");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("+10")) AfterPass(() => AddStability(world, 10f));
        if (GUILayout.Button("-10")) AfterPass(() => AddStability(world, -10f));
        if (GUILayout.Button("Set 0 (fire test)")) AfterPass(() => SetStability(world, 0f));
        if (GUILayout.Button("Set 100")) AfterPass(() => SetStability(world, 100f));
        GUILayout.EndHorizontal();

        GUILayout.Space(6f);
        GUILayout.Label("Flags");
        GUILayout.BeginHorizontal();
        _flagInput = GUILayout.TextField(_flagInput, GUILayout.Width(240f));

        if (GUILayout.Button("Set", GUILayout.Width(50f)) && !string.IsNullOrWhiteSpace(_flagInput))
        {
            string flag = _flagInput.Trim();
            AfterPass(() =>
            {
                Debug.Log($"[DebugPanelController] Cheat: SetFlag('{flag}').");
                world.SetFlag(flag);
            });
        }

        if (GUILayout.Button("Clear", GUILayout.Width(50f)) && !string.IsNullOrWhiteSpace(_flagInput))
        {
            string flag = _flagInput.Trim();
            AfterPass(() =>
            {
                Debug.Log($"[DebugPanelController] Cheat: ClearFlag('{flag}').");
                world.ClearFlag(flag);
            });
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
                AfterPass(() =>
                {
                    Debug.Log($"[DebugPanelController] Cheat: ClearFlag('{flag}') (from active list).");
                    world.ClearFlag(flag);
                });
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
                    string id = upgrade.id;
                    AfterPass(() =>
                    {
                        Debug.Log($"[DebugPanelController] Cheat: removing upgrade '{id}'.");
                        world.unlockedUpgradeIds.Remove(id);
                    });
                }
            }
            else
            {
                if (GUILayout.Button("Unlock", GUILayout.Width(60f)))
                {
                    string id = upgrade.id;
                    AfterPass(() =>
                    {
                        Debug.Log($"[DebugPanelController] Cheat: unlocking upgrade '{id}'.");
                        world.UnlockUpgrade(id);
                    });
                }
            }

            GUILayout.EndHorizontal();
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
}
