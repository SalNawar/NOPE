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

        // Default boot behavior: continue an existing run, otherwise start fresh.
        if (!mgr.ContinueRun())
            mgr.NewRun();

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

        Debug.Log($"[RunManager] New run started (day {World.day}, seed {World.runSeed}).");
    }

    /// <summary>
    /// Loads the saved run if one exists. Returns true on success.
    /// </summary>
    public bool ContinueRun()
    {
        WorldState loaded = SaveSystem.Load();

        if (loaded == null)
            return false;

        World = loaded;
        Debug.Log($"[RunManager] Continued run (day {World.day}).");
        return true;
    }

    /// <summary>Writes the current world state to disk.</summary>
    public void SaveNow()
    {
        if (World == null)
        {
            Debug.LogError("RunManager.SaveNow called with no active run.");
            return;
        }

        if (SaveSystem.Save(World))
            Debug.Log($"[RunManager] Saved (day {World.day}).");
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
        // Nightly resolve runs BEFORE day++ so trigger conditions read "today".
        TimelineService.NightlyResolve(World, Library, Config != null ? Config.gameConfig : null);

        World.day++;
        World.citationsToday = 0;

        SaveNow();
        LoadOfficeScene();
    }

    /// <summary>
    /// Call at end of shift: one-day "tomorrow modifiers" (set by yesterday's
    /// slot machine) have been consumed