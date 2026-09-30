using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Phase 6 developer overlay: cheat panel + live timeline inspector.
/// Toggle with the backtick/tilde key. RunManager.GetOrCreate() attaches this
/// to its persistent GameObject in the editor and development builds only, so
/// it's available from any scene with no scene wiring and absent from release
/// builds. The component is enabled only while the overlay is open: a closed
/// overlay runs no OnGUI, so IMGUI costs nothing per frame (audit R2-012,
/// R3-030); the key is an input action, heard while the component is off. A
/// cheat clicked in a GUI pass is queued and runs after the pass (audit
/// R2-003; DevCheats runs and logs it). An open overlay allocates nothing per
/// frame for what did not change: each line is rebuilt only when what it
/// shows changes, the layout options and the costume errors' names are made
/// once, and the scene's GameManager is found when a scene loads, not per
/// pass. The Timeline Inspector tab and the state dump are DebugInspector.
/// </summary>
public sealed class DebugPanelController : MonoBehaviour
{
    /// <summary>Overlay panel size in screen pixels.</summary>
    private const float PanelWidth = 440f;
    private const float PanelHeight = 480f;

    /// <summary>Tab labels for the toolbar.</summary>
    private static readonly string[] TabLabels = { "Cheats", "Timeline Inspector" };

    /// <summary>The costume error cheat's buttons, None first (one per variant).</summary>
    private static readonly CostumeError[] CostumeErrorChoices = (CostumeError[])Enum.GetValues(typeof(CostumeError));

    /// <summary>The costume error buttons' labels (the variants' names, made once).</summary>
    private static readonly string[] CostumeErrorNames = Array.ConvertAll(CostumeErrorChoices, e => e.ToString());

    /// <summary>The stranding fates the panel can force (null: the fate stream's draw; the endings and strandings spec §6), and their button names, built once.</summary>
    private static readonly StrandingFate?[] FateChoices = { null, StrandingFate.Forgotten, StrandingFate.News, StrandingFate.Carry, StrandingFate.Tremor, StrandingFate.Police };

    /// <summary>The stranding fate buttons' names (made once).</summary>
    private static readonly string[] FateNames = Array.ConvertAll(FateChoices, f => f.HasValue ? f.Value.ToString() : "Drawn");

    /// <summary>The layout options the panel uses, made once (GUILayout.Width and Height make a new option per call).</summary>
    private static readonly GUILayoutOption ScrollHeight = GUILayout.Height(PanelHeight - 90f), Width50 = GUILayout.Width(50f), Width60 = GUILayout.Width(60f),
                                            Width100 = GUILayout.Width(100f), Width120 = GUILayout.Width(120f), Width240 = GUILayout.Width(240f),
                                            Width260 = GUILayout.Width(260f), Width330 = GUILayout.Width(330f);

    /// <summary>The run line: day, money, stability, ending.</summary>
    private static readonly Func<(int day, int money, float stability, string ending), string> RunText =
        k => $"Day {k.day}   Money {k.money}   Stability {StabilityRules.Format(k.stability)}   Ending '{k.ending}'";

    /// <summary>The active traveller's line: who, and their voice (a premade's id, their personality, or the defaults).</summary>
    private static readonly Func<(CaseInstance active, string personality), string> TravellerText =
        k => k.active == null ? "Traveller: none"
            : $"Traveller: {k.active.visitorDisplayName}, voice {(k.active.legendarySource != null ? "premade " + k.active.legendarySource.id : string.IsNullOrEmpty(k.personality) ? "none (the defaults)" : k.personality)}";

    /// <summary>The forced personality's line.</summary>
    private static readonly Func<string, string> ForcedPersonalityText = id => $"Personality of every generated case: {id ?? "drawn"} (from the next generation)";

    /// <summary>The active flags' heading.</summary>
    private static readonly Func<int, string> FlagsText = count => $"Active flags ({count}):";

    /// <summary>The forced costume error's line.</summary>
    private static readonly Func<int, string> CostumeText = error => $"Costume error on the next generated case: {(CostumeError)error}";

    /// <summary>An upgrade's row label.</summary>
    private static readonly Func<UpgradeSO, string> UpgradeText = upgrade => $"  {upgrade.displayName} ({upgrade.id})";

    /// <summary>A GUI line rebuilt only when what it shows (its key) changes.</summary>
    private sealed class CachedLine<TKey>
    {
        private TKey _key;
        private string _text;

        /// <summary>The line for <paramref name="key"/>: the last one while the key is the same, else made anew.</summary>
        public string Get(TKey key, Func<TKey, string> make)
        {
            if (_text == null || !EqualityComparer<TKey>.Default.Equals(key, _key))
            {
                _key = key;
                _text = make(key);
            }
            return _text;
        }
    }

    private readonly CachedLine<(int, int, float, string)> _runLine = new CachedLine<(int, int, float, string)>();
    private readonly CachedLine<(CaseInstance, string)> _travellerLine = new CachedLine<(CaseInstance, string)>();
    private readonly CachedLine<string> _personalityLine = new CachedLine<string>();
    private readonly CachedLine<int> _flagsLine = new CachedLine<int>();
    private readonly CachedLine<int> _costumeLine = new CachedLine<int>();

    /// <summary>Each upgrade's row label, made the first time it is drawn.</summary>
    private readonly Dictionary<UpgradeSO, string> _upgradeLabels = new Dictionary<UpgradeSO, string>();

    /// <summary>The personality buttons (Drawn first, then each personality's id) for <see cref="_choicesFor"/>.</summary>
    private readonly List<string> _choices = new List<string>();

    /// <summary>The library the personality buttons were listed from.</summary>
    private ContentLibrarySO _choicesFor;

    /// <summary>Currently selected tab (0 = Cheats, 1 = Timeline Inspector).</summary>
    private int _tab;

    /// <summary>Scroll position for the active tab's content.</summary>
    private Vector2 _scroll;

    /// <summary>Text field contents for the flag set/clear cheat.</summary>
    private string _flagInput = string.Empty;

    /// <summary>The toggle key (backtick/tilde), heard while the component is disabled.</summary>
    private InputAction _toggle;

    /// <summary>The scene's GameManager (the active traveller), found when the overlay opens and when a scene loads.</summary>
    private GameManager _game;

    /// <summary>
    /// What a click in this GUI pass changes (a cheat and its argument, or
    /// the tab), run once the pass has drawn everything: changing what is
    /// drawn mid-pass (a new flag row, the other tab) would draw more controls
    /// than IMGUI's layout pass counted, and GUILayout throws. A pass carries
    /// one click.
    /// </summary>
    private DevCheat _cheat;
    private int _cheatNumber;
    private float _cheatAmount;
    private string _cheatText;
    private int _pendingTab = -1;

    /// <summary>Listens for the toggle key and for scene loads, and starts closed (disabled: no OnGUI).</summary>
    private void Awake()
    {
        _toggle = new InputAction("DevOverlay", InputActionType.Button, "<Keyboard>/backquote");
        _toggle.performed += OnToggle;
        _toggle.Enable();
        SceneManager.sceneLoaded += OnSceneLoaded;
        enabled = false;
        Debug.Log("[DebugPanelController] Attached to persistent RunManager object (press ~ to toggle the dev overlay).");
    }

    /// <summary>Stops listening for the toggle key and scene loads.</summary>
    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (_toggle == null)
            return;
        _toggle.performed -= OnToggle;
        _toggle.Dispose();
        _toggle = null;
    }

    /// <summary>The overlay opens: finds the scene's GameManager.</summary>
    private void OnEnable() => _game = FindAnyObjectByType<GameManager>();

    /// <summary>A scene loaded (the office, Home): its GameManager, if any, is the one the overlay reads.</summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => _game = FindAnyObjectByType<GameManager>();

    /// <summary>Opens or closes the overlay (editor / development builds only).</summary>
    private void OnToggle(InputAction.CallbackContext _)
    {
        if (!Application.isEditor && !Debug.isDebugBuild)
            return;

        enabled = !enabled;
        _scroll = Vector2.zero;
        _cheat = DevCheat.None;
        _pendingTab = -1;
        Debug.Log($"[DebugPanelController] Overlay {(enabled ? "opened" : "closed")} (~ pressed).");
    }

    /// <summary>Draws the open overlay, then applies what a click in this pass changed.</summary>
    private void OnGUI()
    {
        Draw();

        if (_pendingTab >= 0)
            _tab = _pendingTab;
        _pendingTab = -1;
        DevCheat cheat = _cheat;
        _cheat = DevCheat.None;
        if (cheat != DevCheat.None && RunManager.HasInstance)
            DevCheats.Run(cheat, RunManager.Instance, _cheatNumber, _cheatAmount, _cheatText);
    }

    /// <summary>Queues a cheat to run after this GUI pass (a pass carries one click).</summary>
    private void Queue(DevCheat cheat, int number = 0, float amount = 0f, string text = null)
    {
        _cheat = cheat;
        _cheatNumber = number;
        _cheatAmount = amount;
        _cheatText = text;
    }

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

        GUILayout.Label(_runLine.Get((world.day, world.money, world.timelineStability, world.endingId), RunText));

        int tab = GUILayout.Toolbar(_tab, TabLabels);
        if (tab != _tab)
            _pendingTab = tab;
        GUILayout.Space(4f);

        _scroll = GUILayout.BeginScrollView(_scroll, ScrollHeight);

        if (_tab == 0)
            DrawCheatsTab(world, lib);
        else
            DebugInspector.Draw(world, lib);

        GUILayout.EndScrollView();

        GUILayout.EndArea();
    }

    /// <summary>
    /// The voice (the personalities spec's PS4): the active traveller's
    /// personality, or the premade they are (shown here only, never in the
    /// game), and the force every generated traveller's personality takes
    /// from the next generation ("Drawn" lifts it).
    /// </summary>
    private void DrawPersonality(ContentLibrarySO lib)
    {
        CaseInstance active = _game != null ? _game.ActiveCase : null;
        GUILayout.Label(_travellerLine.Get((active, active != null ? active.personality : null), TravellerText));
        GUILayout.Label(_personalityLine.Get(DevToolsState.ForcedPersonality, ForcedPersonalityText));

        if (lib != _choicesFor || _choices.Count == 0)
        {
            _choicesFor = lib;
            _choices.Clear();
            _choices.Add(null);
            if (lib != null)
                foreach (Personality p in lib.Personalities)
                    if (p != null && !string.IsNullOrWhiteSpace(p.id))
                        _choices.Add(p.id);
        }
        for (int i = 0; i < _choices.Count; i += 4)
        {
            GUILayout.BeginHorizontal();
            for (int j = i; j < i + 4 && j < _choices.Count; j++)
            {
                string id = _choices[j];
                if (GUILayout.Button(id ?? "Drawn", Width100) && DevToolsState.ForcedPersonality != id)
                    Queue(DevCheat.ForcePersonality, text: id);
            }
            GUILayout.EndHorizontal();
        }
    }

    /// <summary>Cheats tab: day skip, history (force leader), money/stability adjust, flags, force legendary, the voice, upgrades.</summary>
    private void DrawCheatsTab(WorldState world, ContentLibrarySO lib)
    {
        GUILayout.Label("Day flow");

        if (GUILayout.Button("Skip Day (sleep: endings + nightly resolve + advance)"))
            Queue(DevCheat.SkipDay);

        if (lib != null)
        {
            GUILayout.Space(6f);
            GUILayout.Label("History (force leader)");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("No leader"))
                Queue(DevCheat.ForceLeader);

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
                    Queue(DevCheat.ForceLeader, text: nation.id);
            }
            GUILayout.EndHorizontal();
            GUILayout.Label("Kept every night this session; the Future follows at the next office day.");
        }

        GUILayout.Space(6f);
        GUILayout.Label("Money");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("+10")) Queue(DevCheat.AddMoney, 10);
        if (GUILayout.Button("+50")) Queue(DevCheat.AddMoney, 50);
        if (GUILayout.Button("+100")) Queue(DevCheat.AddMoney, 100);
        if (GUILayout.Button("-50")) Queue(DevCheat.AddMoney, -50);
        GUILayout.EndHorizontal();

        GUILayout.Space(6f);
        GUILayout.Label("Timeline stability");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("+10")) Queue(DevCheat.AddStability, amount: 10f);
        if (GUILayout.Button("-10")) Queue(DevCheat.AddStability, amount: -10f);
        if (GUILayout.Button("Set 0 (fire test)")) Queue(DevCheat.SetStability, amount: 0f);
        if (GUILayout.Button("Set 100")) Queue(DevCheat.SetStability, amount: 100f);
        GUILayout.EndHorizontal();

        GUILayout.Space(6f);
        GUILayout.Label("Flags");
        GUILayout.BeginHorizontal();
        _flagInput = GUILayout.TextField(_flagInput, Width240);

        if (GUILayout.Button("Set", Width50) && !string.IsNullOrWhiteSpace(_flagInput))
            Queue(DevCheat.SetFlag, text: _flagInput.Trim());

        if (GUILayout.Button("Clear", Width50) && !string.IsNullOrWhiteSpace(_flagInput))
            Queue(DevCheat.ClearFlag, text: _flagInput.Trim());

        GUILayout.EndHorizontal();

        GUILayout.Label(_flagsLine.Get(world.flags.Count, FlagsText));

        for (int i = world.flags.Count - 1; i >= 0; i--)
        {
            string flag = world.flags[i];

            GUILayout.BeginHorizontal();
            GUILayout.Space(8f);
            GUILayout.Label(flag);

            if (GUILayout.Button("Clear", Width50))
                Queue(DevCheat.ClearListedFlag, text: flag);

            GUILayout.EndHorizontal();
        }

        GUILayout.Space(6f);

        bool forced = DevToolsState.ForceLegendaryNextCase;
        bool newForced = GUILayout.Toggle(forced, "Force legendary on next generated case");
        if (newForced != forced)
            DevCheats.SetForceLegendary(newForced);

        GUILayout.BeginHorizontal();
        GUILayout.Label(_costumeLine.Get((int)DevToolsState.ForcedCostumeError, CostumeText), Width330);
        for (int i = 0; i < CostumeErrorChoices.Length; i++)
            if (GUILayout.Button(CostumeErrorNames[i], Width120) && DevToolsState.ForcedCostumeError != CostumeErrorChoices[i])
                Queue(DevCheat.ForceCostumeError, (int)CostumeErrorChoices[i]);
        GUILayout.EndHorizontal();

        DrawPersonality(lib);

        bool forceStrandings = DevToolsState.ForceStrandings;
        bool newForceStrandings = GUILayout.Toggle(forceStrandings, "Force strandings (every accepted Economy transponder fails at the shift's end)");
        if (newForceStrandings != forceStrandings)
            DevCheats.SetForceStrandings(newForceStrandings);

        GUILayout.BeginHorizontal();
        GUILayout.Label("Stranding fate:", Width100);
        for (int i = 0; i < FateChoices.Length; i++)
            if (GUILayout.Button(FateNames[i]) && DevToolsState.ForcedStrandingFate != FateChoices[i])
                DevCheats.ForceStrandingFate(FateChoices[i]);
        GUILayout.EndHorizontal();

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

            if (!_upgradeLabels.TryGetValue(upgrade, out string label))
                _upgradeLabels[upgrade] = label = UpgradeText(upgrade);

            GUILayout.BeginHorizontal();
            GUILayout.Label(label, Width260);

            if (world.HasUpgrade(upgrade.id))
            {
                if (GUILayout.Button("Lock", Width60))
                    Queue(DevCheat.LockUpgrade, text: upgrade.id);
            }
            else if (GUILayout.Button("Unlock", Width60))
                Queue(DevCheat.UnlockUpgrade, text: upgrade.id);

            GUILayout.EndHorizontal();
        }
    }
}
