using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Persistent run owner. Survives scene loads (Office <-> Home), owns the
/// WorldState, drives day progression and save/load.
/// Created lazily by GetOrCreate() so any scene can be played directly in the editor.
/// Every morning is saved, day 1's included, and kept as the day's morning
/// snapshot, which the cheat menu's restart restores (RUN-SAFE, BUG-28a,
/// BUG-35a); the cheat menu's skips, jumps and restarts run the same day
/// steps as play (RunRoutes over DayCycle) and land at a morning with the
/// day's cheat switches cleared (BUG-27a).
/// </summary>
public sealed class RunManager : MonoBehaviour
{
    /// <summary>Resources path of the RunConfigSO asset (shared with other boot-time loaders).</summary>
    public const string ConfigResourcePath = "RunConfig";

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
    /// How many mornings this session has opened (a new run, a night's turn,
    /// a skip, a jump, a restart): the cheat panel closes when a cheat it ran
    /// opened one (the day starts at its briefing, nothing left open; BUG-27).
    /// </summary>
    public int Mornings { get; private set; }

    /// <summary>The run's tuning (null when RunConfig names none).</summary>
    private GameConfigSO GameConfig => Config != null ? Config.gameConfig : null;

    /// <summary>The run's last day (ContentLibrarySO.LastDay; 0 without a library).</summary>
    private int LastDay => Library != null ? Library.LastDay : 0;

    /// <summary>True once the run has ended (WorldState.endingId).</summary>
    private bool Ended => World != null && !string.IsNullOrEmpty(World.endingId);

    /// <summary>Today's morning snapshot (the world as the morning opened, as save JSON) and its day: a restart restores it.</summary>
    private string _morning;
    private int _morningDay;

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

        // The save slot's notice (a failed write, a save set aside): shown wherever the player is (BUG-35a).
        go.AddComponent<SaveNotice>();

#if UNITY_EDITOR || DEVELOPMENT_BUILD || DEMO_CHEATS
        // Phase 6: dev overlay (the cheat menu + timeline inspector), toggled
        // with F9 or '~'. Editor and development builds, and the Windows demo
        // while DemoBuild.DemoCheats defines DEMO_CHEATS (Saleh 2026-10-06:
        // "a cheat menu available in the Windows demo build too"; D18 2026-10-20:
        // "visible, as now"); any other release build never carries it (audit
        // R2-012, R3-030).
        go.AddComponent<DebugPanelController>();
#endif

        // Default boot behaviour: continue an existing run, otherwise a fresh
        // run in memory that writes nothing until it is saved (a save that
        // could not be continued was set aside by the load, never deleted).
        if (!mgr.ContinueRun())
            mgr.StartUnsavedRun();

        Debug.Log($"[RunManager] <<< Exiting GetOrCreate (day {mgr.World?.day}, money={mgr.World?.money}, stability={mgr.World?.timelineStability:0.00}).");

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
    /// Raised when the running world's effects change: an effect activated,
    /// removed or expired (TimelineService), or a new or continued run. Cue
    /// receivers listen to it, so an effect that lands mid-scene (a Home
    /// purchase, a dev cheat) shows at once, not at the next scene's start
    /// (audit R3-040).
    /// </summary>
    public static event Action EffectsChanged;

    /// <summary>Raises <see cref="EffectsChanged"/> (TimelineService after it changes a world's effects; NewRun, ContinueRun and every morning a cheat opens).</summary>
    public static void NotifyEffectsChanged() => EffectsChanged?.Invoke();

    /// <summary>
    /// Starts a brand-new run with the run config's default pet: the old
    /// run's files are set aside (kept beside the slot, never deleted;
    /// SaveSystem.SetAsideForNewRun) and day 1's morning is saved at once, so
    /// a quit on day 1 can be continued (BUG-35a). The Title asks before
    /// replacing a run (HOME, through SaveSystem.HasSave).
    /// </summary>
    public void NewRun() => StartRun(null);

    /// <summary>Starts a brand-new run (NewRun) with the pet the Title's adoption chose, its kind, name and coat (DayCycle.Adopt: a refused name falls back to the kind's suggested one, an unlisted coat to the kind's first), adopted before day 1's morning save.</summary>
    public void NewRun(PetKind kind, string petName, string coat) =>
        StartRun(world => DayCycle.Adopt(world, Library, kind, petName, coat));

    /// <summary>A new run: the switches reset, the old run set aside, a fresh world (<paramref name="adopt"/> picks its pet), day 1's morning saved.</summary>
    private void StartRun(Action<WorldState> adopt)
    {
        Debug.Log("[RunManager] >>> Entering NewRun.");

        DevToolsState.ResetAll();
        SaveSystem.SetAsideForNewRun();
        World = FreshWorld();
        adopt?.Invoke(World);

        Debug.Log($"[RunManager] New run started (day {World.day}, seed {World.runSeed}).");
        NotifyEffectsChanged();
        SaveMorning();
    }

    /// <summary>The boot's run when no save can be continued: a fresh world in memory, nothing written or removed (the player's New Run saves it; a save that could not be read was set aside by the load).</summary>
    private void StartUnsavedRun()
    {
        DevToolsState.ResetAll();
        World = FreshWorld();
        _morning = null;
        Debug.Log($"[RunManager] No save to continue: a fresh run in memory (day {World.day}, seed {World.runSeed}), unsaved until it is started or played.");
        NotifyEffectsChanged();
    }

    /// <summary>A new world from the run config (DayCycle.NewWorld) on its fixed seed, else a random one.</summary>
    private WorldState FreshWorld() =>
        DayCycle.NewWorld(Config, Library, Config.fixedRunSeed != 0 ? Config.fixedRunSeed : UnityEngine.Random.Range(int.MinValue, int.MaxValue));

    /// <summary>
    /// Loads the saved run from disk if one exists (the Title's Continue reads
    /// it again, so it resumes the saved run and never the one in memory;
    /// BUG-35a). Returns true on success. A morning save is kept as the day's
    /// morning snapshot (or the stored one for its day).
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

        // A save from before the pet (the Home pet spec) adopts the run config's default;
        // one from before the coats (PS11) gives its pet the kind's first coat.
        if (World.pet == null || !World.pet.Adopted)
            DayCycle.Adopt(World, Library, Config.startingPetKind, null, null);
        else if (Library != null)
            World.pet.coat = Library.Home.pet.CoatOf(World.pet.kind, World.pet.coat);

        // The present culture's cue from the saved history, idempotently (an
        // older save may lack it; piece 6 Z5).
        if (Library != null)
        {
            HistoryService.RebuildLeaderEffect(World, Library, World.day);
            WorldOutcomeService.SeedOlderSave(World, Library);
        }

        KeepMorningOf(World);
        Debug.Log($"[RunManager] Continued run (day {World.day}).");
        NotifyEffectsChanged();
        return true;
    }

    /// <summary>The day's morning snapshot after a load: the stored morning when it is of the loaded day, else the loaded world itself when it is a morning save (the office's, no ending), else none.</summary>
    private void KeepMorningOf(WorldState loaded)
    {
        WorldState stored = SaveSystem.LoadMorning();
        if (stored != null && stored.day == loaded.day)
            Keep(JsonUtility.ToJson(stored), stored.day);
        else if (loaded.phase == RunPhase.Office && string.IsNullOrEmpty(loaded.endingId))
            Keep(JsonUtility.ToJson(loaded), loaded.day);
        else
            _morning = null;
    }

    /// <summary>Writes the current world state to disk (a failure is reported to the player by SaveSystem and SaveNotice; the run goes on).</summary>
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

    /// <summary>The morning save (every morning, day 1's included; BUG-35a): the world saved and kept, in memory and on disk, as the day's morning snapshot.</summary>
    private void SaveMorning()
    {
        Keep(JsonUtility.ToJson(World), World.day);
        SaveSystem.SaveMorning(World);
        SaveNow();
    }

    /// <summary>Keeps <paramref name="json"/> as day <paramref name="day"/>'s morning snapshot.</summary>
    private void Keep(string json, int day)
    {
        _morning = json;
        _morningDay = day;
    }

    /// <summary>Today's morning as it opened (a fresh copy of the snapshot), or null when none is kept for today.</summary>
    private WorldState TodaysMorning() =>
        World != null && _morning != null && _morningDay == World.day ? JsonUtility.FromJson<WorldState>(_morning) : null;

    // -----------------------------
    // Day flow
    // -----------------------------

    /// <summary>
    /// Deterministic seed for the current day, derived from the run seed.
    /// Same run + same day = same schedule and cases (see Seeds).
    /// </summary>
    public int GetDaySeed() => Seeds.Day(World.runSeed, World.day);

    /// <summary>
    /// The DayPlan for the current day, from the content library (its own or
    /// the latest earlier plan, ContentLibrarySO.GetDayPlan).
    /// Returns null if no plan exists for this day (caller decides fallback).
    /// </summary>
    public DayPlanSO GetCurrentDayPlan() =>
        Library != null ? Library.GetDayPlan(World.day) : null;

    /// <summary>
    /// Opens the scene the saved run resumes in: Home after the end-of-shift
    /// save (GoHomeOrAdvance), otherwise the Office. A run never saved yet
    /// (the boot's, played without the Title's New Run) saves its morning first.
    /// </summary>
    public void ResumeRun()
    {
        Debug.Log($"[RunManager] ResumeRun (day {World.day}, phase {World.phase}).");

        if (World.phase == RunPhase.Home)
            GoHomeOrAdvance();
        else
        {
            if (_morning == null)
                SaveMorning();
            LoadOfficeScene();
        }
    }

    /// <summary>
    /// End of the Home phase: Sleep's night (DayCycle.Sleep: the day-boundary
    /// ending check, the nightly resolve, then the failures again on the world
    /// the night left; BUG-56b). An ending is saved and shown on the title
    /// scene; otherwise the next morning opens (OpenMorning). Home's Sleep and
    /// the no-Home path of GoHomeOrAdvance come here.
    /// </summary>
    public void Sleep()
    {
        Debug.Log($"[RunManager] >>> Entering Sleep (day {World.day}, money={World.money}, stability={World.timelineStability:0.00}).");

        EndingSO ending = DayCycle.Sleep(World, Library, GameConfig);
        if (ending != null)
        {
            Debug.Log($"[RunManager] <<< Exiting Sleep (ending '{ending.id}' ({ending.displayName}); saving and loading the title scene).");
            EndRunAt(ending);
            return;
        }

        Debug.Log($"[RunManager] <<< Exiting Sleep (now day {World.day}, money={World.money}, stability={World.timelineStability:0.00}; the morning opens).");
        OpenMorning();
    }

    /// <summary>
    /// The cheat menu's "Skip day" (RunRoutes.Skip): today's shift closes as
    /// played (the live shift on its ledger; unless the run is at Home,
    /// its shift closed), Home's night on the default care, Sleep, and the next
    /// morning at the briefing; an ending on the way ends the run there. False
    /// (nothing done) once the run has ended.
    /// </summary>
    public bool SkipDay()
    {
        if (World == null || RefuseEnded("skip the day"))
            return false;
        return Run(RunRoutes.Skip(World.phase == RunPhase.Home), null);
    }

    /// <summary>
    /// The cheat menu's "Jump to day N" (Saleh 2026-10-06; RunRoutes.Jump):
    /// the start of day <paramref name="day"/>'s shift (1 to the run's last
    /// day). Forward, today closes as played and has its night, as a skip
    /// does, then every day before the target is played with its whole queue
    /// decided correctly (as the cheat menu's Auto-decide plays it, so the run
    /// earns its pay) with Home's night on the default care and Sleep; back, a
    /// new world with the run's seed and pet is played the same way from day
    /// 1; today restarts (RestartShift). The run is marked cheated, its morning saved and the
    /// office opens at the briefing. False when nothing was done (no run, or
    /// a jump forward from a run that has ended).
    /// </summary>
    public bool JumpToDay(int day)
    {
        if (World == null)
            return false;
        int target = RunRoutes.Clamp(day, LastDay);
        if (target > World.day && RefuseEnded("jump forward"))
            return false;
        Debug.Log($"[RunManager] Cheat: jump from day {World.day} to day {target}.");
        return Run(RunRoutes.Jump(World.day, target, LastDay, World.phase == RunPhase.Home), null);
    }

    /// <summary>
    /// The cheat menu's "Restart shift" (and its unlock-everything, give-all
    /// and force switches, which apply from a shift's start): today's morning
    /// snapshot comes back (what today's decided travellers did, the pay, the
    /// citations and the premades met today are undone; an ending reached
    /// today is lifted), then <paramref name="reapply"/> (the cheat's own
    /// change, which the morning did not have) and the morning opens: saved,
    /// marked cheated, at the briefing.
    /// </summary>
    public bool RestartShift(Action<WorldState> reapply = null)
    {
        if (World == null)
            return false;
        return Run(RunRoutes.Restart(), reapply);
    }

    /// <summary>What a restart of today loses, for the cheat menu's confirm (RewindWarnings.Restart): today's decisions, the money since the morning, today's citations.</summary>
    public string RestartWarning()
    {
        GameManager game = GameManager.Current;
        int decisions = game != null && game.World == World && game.Ledger != null ? game.Ledger.verdicts.Count : 0;
        WorldState morning = TodaysMorning();
        return RewindWarnings.Restart(World.day, decisions, morning != null ? World.money - morning.money : 0, World.citationsToday);
    }

    /// <summary>What a jump back to <paramref name="day"/> loses (RewindWarnings.JumpBack), or today's restart warning for today.</summary>
    public string JumpWarning(int day)
    {
        int target = RunRoutes.Clamp(day, LastDay);
        return target == World.day ? RestartWarning() : RewindWarnings.JumpBack(World.day, target);
    }

    /// <summary>True (and logged) when the run has ended, so <paramref name="what"/> cannot be done (a restart or a jump back still can).</summary>
    private bool RefuseEnded(string what)
    {
        if (!Ended)
            return false;
        Debug.LogWarning($"[RunManager] Cheat: cannot {what}: the run has ended ('{World.endingId}'). Restart the day or jump back instead.");
        return true;
    }

    /// <summary>
    /// Runs a day-changing cheat's route (CheatRoute.Play: play's own day
    /// steps, the live shift closed on its ledger, a route from Home starting
    /// from the end-of-shift save, a restart from today's morning snapshot).
    /// An ending reached on the way ends the run there (saved, the title
    /// scene); otherwise <paramref name="reapply"/> runs on the world the route
    /// lands on and its morning opens. True when the route ran.
    /// </summary>
    private bool Run(List<RunStep> route, Action<WorldState> reapply)
    {
        Debug.Log($"[RunManager] Cheat route from day {World.day} ({World.phase}): {string.Join(", ", route)}.");
        GameManager live = GameManager.Current != null && GameManager.Current.World == World ? GameManager.Current : null;
        WorldState world = World;
        EndingSO ending = CheatRoute.Play(ref world, route, Config, live != null ? live.CloseShiftForCheat : (Func<bool>)null, TodaysMorning, SaveSystem.Load);
        World = world;

        if (ending != null)
        {
            EndRunAt(ending);
            return true;
        }

        reapply?.Invoke(World);
        World.cheated = true;
        OpenMorning();
        return true;
    }

    /// <summary>
    /// A morning opens (the night's turn, or a cheat's landing): the day's
    /// cheat switches are cleared (DevToolsState.ResetDay: the forced hour,
    /// auto-decide, the hall overrides, the culture's day reset; BUG-27a),
    /// the morning is saved and kept as the day's snapshot, and the office
    /// opens at the briefing.
    /// </summary>
    private void OpenMorning()
    {
        DevToolsState.ResetDay();
        SaveMorning();
        Mornings++;
        NotifyEffectsChanged();
        LoadOfficeScene();
    }

    /// <summary>The run ends on <paramref name="ending"/>: saved with it, and the title scene shows it.</summary>
    private void EndRunAt(EndingSO ending)
    {
        World.endingId = ending.id;
        SaveNow();
        LoadTitleScene();
    }

    /// <summary>
    /// Call at end of shift: one-day "tomorrow modifiers" (set by yesterday's
    /// slot machine) have been consumed by today's generation — reset them
    /// before the Home phase sets new ones for tomorrow (DayCycle.ResetTomorrowModifiers).
    /// </summary>
    public void ResetTomorrowModifiers()
    {
        Debug.Log("[RunManager] ResetTomorrowModifiers (legendaryChanceBonus, forgeryChanceModifier reset to 0; payRateMultiplier reset to 1).");
        DayCycle.ResetTomorrowModifiers(World);
    }

    // -----------------------------
    // Scene transitions
    // -----------------------------

    /// <summary>Loads the art office; its load brings the gameplay layer on top (OfficeScenes). Through SceneTransit (FLOW's transition).</summary>
    public void LoadOfficeScene()
    {
        Debug.Log($"[RunManager] LoadOfficeScene -> '{Config.officeSceneName}' (+ '{Config.officeGameplaySceneName}' on top).");
        SceneTransit.Load(Config.officeSceneName);
    }

    /// <summary>Loads the home scene (through SceneTransit).</summary>
    public void LoadHomeScene()
    {
        Debug.Log($"[RunManager] LoadHomeScene -> '{Config.homeSceneName}'.");
        SceneTransit.Load(Config.homeSceneName);
    }

    /// <summary>Loads the title scene (run ended — ending display, Continue/New Run; through SceneTransit).</summary>
    public void LoadTitleScene()
    {
        Debug.Log($"[RunManager] LoadTitleScene -> '{Config.titleSceneName}'.");
        SceneTransit.Load(Config.titleSceneName);
    }

    /// <summary>
    /// End-of-shift handoff (and Continue after the end-of-shift save): goes to
    /// the Home scene if it's in Build Settings, otherwise (Home not built yet)
    /// sleeps at once (day-boundary endings, then the next day) so the core
    /// loop stays playable.
    /// </summary>
    public void GoHomeOrAdvance()
    {
        Debug.Log($"[RunManager] >>> Entering GoHomeOrAdvance (day {World.day}).");

        if (Application.CanStreamedLevelBeLoaded(Config.homeSceneName))
        {
            Debug.Log($"[RunManager] <<< Exiting GoHomeOrAdvance (going to Home scene '{Config.homeSceneName}').");

            SaveNow();
            LoadHomeScene();
        }
        else
        {
            Debug.LogWarning($"[RunManager] Scene '{Config.homeSceneName}' not in Build Settings — skipping Home phase and sleeping (day {World.day}).");
            Sleep();
        }
    }
}
