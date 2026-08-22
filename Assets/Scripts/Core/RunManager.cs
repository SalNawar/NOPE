using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Persistent run owner. Survives scene loads (Office <-> Home), owns the
/// WorldState, drives day progression and save/load.
/// Created lazily by GetOrCreate() so any scene can be played directly in the editor.
/// </summary>
public sealed class RunManager : MonoBehaviour
{
    /// <summary>Resources path of the RunConfigSO asset.</summary>
    private const string ConfigResourcePath = "RunConfig";

    /// <summary>Singleton instance (null until first GetOrCreate).</summary>
    public static RunManager Instance { get; private set; }

    /// <summary>True if a RunManager already exists.</summary>
    public static bool HasInstance => Instance != null;

    /// <summary>Boot configuration loaded from Resources.</summary>
    public RunConfigSO Config { get; private set; }

    /// <summary>The current run's world state.</summary>
    public WorldState World { get; private set; }

    /// <summary>Content library shortcut.</summary>
    public ContentLibrarySO Library => Config != null ? Config.contentLibrary : null;

    /// <summary>
    /// Returns the existing RunManager or creates one (loading config + save).
    /// Returns null if RunConfig.asset is missing from Resources.
    /// </summary>
    public static RunManager GetOrCreate()
    {
        if (Instance != null)
            return Instance;

        Debug.Log("[RunManager] >>> Entering GetOrCreate (no instance yet, bootstrapping).");

        var config = Resources.Load<RunConfigSO>(ConfigResourcePath);

        if (config == null)
        {
            Debug.LogError("RunManager could not load Resources/RunConfig.asset. Create one via Assets > Create > TimeDesk > Run Config and place it in a Resources folder.");
            return null;
        }

        var go = new GameObject("RunManager");
        DontDestroyOnLoad(go);

        var mgr = go.AddComponent<RunManager>();
        mgr.Config = config;
        Instance = mgr;

        // Phase 6: dev overlay (cheats + timeline inspector), toggled with '~'.
        // Editor/dev-build only — DebugPanelController.OnGUI() no-ops otherwise.
        go.AddComponent<DebugPanelController>();

        // Default boot behavior: continue an existing run, otherwise start fresh.
        if (!mgr.ContinueRun())
            mgr.NewRun();

        Debug.Log($"[RunManager] <<< Exiting GetOrCreate (day {mgr.World?.day}, money={mgr.World?.money}, stability={mgr.World?.timelineStability:0.#}).");

        return mgr;
    }

    /// <summary>Clears the singleton when destroyed (e.g., exiting play mode).</summary>
    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // -----------------------------
    // Run lifecycle
    // -----------------------------

    /// <summary>
    /// Starts a brand-new run: deletes the save and builds a fresh WorldState.
    /// </summary>
    public void NewRun()
    {
        Debug.Log("[RunManager] >>> Entering NewRun.");

        DevToolsState.ResetAll();
        SaveSystem.Delete();

        World = new WorldState
        {
            day = Config.startingDay,
            money = Config.startingMoney,
            timelineStability = Config.startingStability,
            runSeed = Config.fixedRunSeed != 0
                ? Config.fixedRunSeed
                : Random.Range(int.MinValue, int.MaxValue)
        };

        if (Config.startingFamilyMembers != null)
        {
            foreach (string name in Config.startingFamilyMembers)
            {
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                World.family.members.Add(new FamilyMemberData { name = name, condition = 0 });
            }
        }

        Debug.Log($"[RunManager] New run started (day {World.day}, seed {World.runSeed}).");
    }

    /// <summary>
    /// Loads the saved run if one exists. Returns true on success.
    /// </summary>
    public bool ContinueRun()
    {
        Debug.Log("[RunManager] >>> Entering ContinueRun.");

        WorldState loaded = SaveSystem.Load();

        if (loaded == null)
        {
            Debug.Log("[RunManager] <<< Exiting ContinueRun — no save found.");
            return false;
        }

        World = loaded;
        DevToolsState.ResetAll();
        Debug.Log($"[RunManager] Continued run (day {World.day}).");
        return true;
    }

    /// <summary>Writes the current world state to disk.</summary>
    public void SaveNow()
    {
        Debug.Log("[RunManager] >>> Entering SaveNow.");

        if (World == null)
        {
            Debug.LogError("RunManager.SaveNow called with no active run.");
            return;
        }

        if (SaveSystem.Save(World))
            Debug.Log($"[RunManager] Saved (day {World.day}).");
        else
            Debug.LogWarning("[RunManager] SaveNow: SaveSystem.Save reported failure.");
    }

    // -----------------------------
    // Day flow
    // -----------------------------

    /// <summary>
    /// Deterministic seed for the current day, derived from the run seed.
    /// Same run + same day = same schedule and cases.
    /// </summary>
    public int GetDaySeed()
    {
        unchecked
        {
            return World.runSeed * 397 ^ World.day * 7919;
        }
    }

    /// <summary>
    /// The DayPlan for the current day, from the content library.
    /// Returns null if no plan exists for this day (caller decides fallback).
    /// </summary>
    public DayPlanSO GetCurrentDayPlan() =>
        Library != null ? Library.GetDayPlan(World.day) : null;

    /// <summary>
    /// Advances to the next day: runs the nightly timeline resolve (dominance,
    /// triggers, effect expiry, tomorrow package), resets daily values, saves,
    /// and reloads the office.
    /// </summary>
    public void AdvanceToNextDay()
    {
        Debug.Log($"[RunManager] >>> Entering AdvanceToNextDay (day {World.day} -> {World.day + 1}).");

        // Nightly resolve runs BEFORE day++ so trigger conditions read "today".
        TimelineService.NightlyResolve(World, Library, Config != null ? Config.gameConfig : null);

        World.day++;
        World.citationsToday = 0;

        Debug.Log($"[RunManager] <<< Exiting AdvanceToNextDay (now day {World.day}, money={World.money}, stability={World.timelineStability:0.#}; saving and loading Office).");

        SaveNow();
        LoadOfficeScene();
    }

    /// <summary>
    /// Call at end of shift: one-day "tomorrow modifiers" (set by yesterday's
    /// slot machine) have been consumed by today's generation — reset them
    /// before the Home phase sets new ones for tomorrow.
    /// </summary>
    public void ResetTomorrowModifiers()
    {
        Debug.Log("[RunManager] ResetTomorrowModifiers (legendaryChanceBonus, forgeryChanceModifier reset to 0; payRateMultiplier reset to 1).");

        World.legendaryChanceBonus = 0f;
        World.forgeryChanceModifier = 0f;
        World.payRateMultiplier = 1f;
    }

    // -----------------------------
    // Scene transitions
    // -----------------------------

    /// <summary>Loads the office scene.</summary>
    public void LoadOfficeScene()
    {
        Debug.Log($"[RunManager] LoadOfficeScene -> '{Config.officeSceneName}'.");
        SceneManager.LoadScene(Config.officeSceneName);
    }

    /// <summary>Loads the home scene.</summary>
    public void LoadHomeScene()
    {
        Debug.Log($"[RunManager] LoadHomeScene -> '{Config.homeSceneName}'.");
        SceneManager.LoadScene(Config.homeSceneName);
    }

    /// <summary>Loads the title scene (run ended — ending display, Continue/New Run).</summary>
    public void LoadTitleScene()
    {
        Debug.Log($"[RunManager] LoadTitleScene -> '{Config.titleSceneName}'.");
        SceneManager.LoadScene(Config.titleSceneName);
    }

    /// <summary>
    /// End-of-shift handoff: goes to the Home scene if it's in Build Settings,
    /// otherwise (Home not built yet) advances straight to the next day so the
    /// core loop stays playable.
    /// </summary>
    public void GoHomeOrAdvance()
    {
        Debug.Log($"[RunManager] >>> Entering GoHomeOrAdvance (day {World.day}).");

        if (Application.CanStreamedLevelBeLoaded(Config.homeSceneName))
        {
            Debug.Log($"[RunManager] <<< Exiting GoHomeOrAdvance (going to Home scene '{Config.homeSceneName}').");

            // No save here: this is still the post-verdict evening state. The
            // morning save (AdvanceToNextDay, after sleep) is the resume point;
            // quitting mid-evening simply replays the day from its morning.
            LoadHomeScene();
        }
        else
        {
            Debug.LogWarning($"[RunManager] Scene '{Config.homeSceneName}' not in Build Settings — skipping Home phase and advancing to day {World.day + 1}.");
            AdvanceToNextDay();
        }
    }
}
