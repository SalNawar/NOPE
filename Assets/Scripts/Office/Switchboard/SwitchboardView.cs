using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// The portal switchboard on the booth's right wall (LOOP_v5 on the v3 board
/// Saleh locked, docs/pending/SWITCHBOARD.md). From the office view a click
/// on the painted board, D (or the right arrow, the way A turns to the city)
/// or S turns the view to it: the board's plate (design\switchboard_v4\board,
/// the art slots' Switchboard/ folder) comes up from the painted board to
/// fill the screen; A, S, a right-click or Esc (ControlRules) turns back.
/// Over the plate it draws what lives: today's destinations and periods on
/// the blank plates (the day's open destinations first, then the portal
/// routes, then today's places: SwitchboardDirectory), the call lamps, the
/// modules' lamps and displays, the cords hanging between their plugs, the
/// two power cords and MASTER. An approved traveller (GameManager.Resolved,
/// after the verdict is scored, never changing it) becomes a call: their
/// destination's lamp blinks with a soft buzz the desk hears, and the
/// painted board in the office view glows with it. The player drags a free
/// module's PLACE and PERIOD plugs into the destination and its period,
/// moves a power cord into the module and pulls MASTER; the rules
/// (Switchboard) calibrate it for real seconds, send the group (its bonus
/// to the wallet and the shift's other money), pop a fuse on a wrong period.
/// Day 1 hangs a hint card on the board. It installs itself into the office's
/// gameplay scene at load (beside OfficeControls, which binds it) and builds
/// its parts at run time from the art slots, so the office scene needs no
/// rebuild. Reduced Motion cuts the turn, the swings and the shakes.
/// </summary>
public sealed partial class SwitchboardView : MonoBehaviour
{
    /// <summary>The switchboard of the running office (null elsewhere): the cheat menu reaches it.</summary>
    public static SwitchboardView Current { get; private set; }

    private SwitchboardConfigSO _config;
    private bool _ownConfig;
    private SwitchboardLayout _layout;
    private OfficeControls _controls;
    private GameManager _game;
    private Switchboard _board;
    private SwitchboardDirectory _directory;
    private int _day;
    private bool _open;
    private float _openT;
    private float _lastBuzz;
    private readonly List<SwitchEvent> _events = new List<SwitchEvent>();

    /// <summary>True while the view is turned to the board (or turning there).</summary>
    public bool IsOpen => _open;

    /// <summary>True while a plug or MASTER is in the hand.</summary>
    public bool IsDragging => _held != null || _leverHeld;

    // ------------------------------------------------------------------ install

    /// <summary>Hooks the install to every scene load (and installs at once into an office already loaded).</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Hook()
    {
        SceneManager.sceneLoaded -= Loaded;
        SceneManager.sceneLoaded += Loaded;
        Install(SceneManager.GetSceneByName(OfficeScenes.GameplaySceneName));
    }

    private static void Loaded(Scene scene, LoadSceneMode mode) => Install(scene);

    /// <summary>Puts a switchboard into <paramref name="scene"/> when it is the office's gameplay layer with its OfficeControls and none yet.</summary>
    private static void Install(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded || scene.name != OfficeScenes.GameplaySceneName)
            return;
        OfficeControls controls = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            controls = root.GetComponentInChildren<OfficeControls>(true);
            if (controls != null)
                break;
        }
        if (controls == null || controls.Switchboard != null)
            return;
        var host = new GameObject("Switchboard", typeof(RectTransform));
        SceneManager.MoveGameObjectToScene(host, scene);
        host.AddComponent<SwitchboardView>().Bind(controls);
    }

    /// <summary>Binds the view to the office's one input model (its keys and back-out reach the board through it).</summary>
    private void Bind(OfficeControls controls)
    {
        _controls = controls;
        if (controls != null)
            controls.BindSwitchboard(this);
    }

    // ------------------------------------------------------------------ lifecycle

    private void Awake()
    {
        Current = this;
        _config = Resources.Load<SwitchboardConfigSO>(SwitchboardConfigSO.ResourcePath);
        if (_config == null)
        {
            _config = ScriptableObject.CreateInstance<SwitchboardConfigSO>();
            _ownConfig = true;
        }
        TextAsset json = Resources.Load<TextAsset>(ArtSlots.SwitchboardLayout);
        if (json == null)
        {
            Debug.LogWarning($"[Switchboard] No layout at Resources/{ArtSlots.SwitchboardLayout}: the board stays off.");
            enabled = false;
            return;
        }
        try
        {
            _layout = SwitchboardLayout.Parse(json.text);
        }
        catch (System.FormatException e)
        {
            Debug.LogError("[Switchboard] " + e.Message + " The board stays off.");
            enabled = false;
            return;
        }
        BuildParts();
    }

    private void OnDestroy()
    {
        if (Current == this)
            Current = null;
        if (_game != null)
            _game.Resolved -= HandleResolved;
        if (_ownConfig && _config != null)
            Destroy(_config);
        DestroySounds();
    }

    /// <summary>Starts the day once the shift knows it, then runs the rules, the turn, the office view's glow, the hand and the drawing (unscaled time: a hit-stop never stops a calibration).</summary>
    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        if (_board == null)
            TryBeginDay();
        if (_board != null)
        {
            _board.Tick(dt);
            PlayEvents();
            Remind();
        }
        if (_open && _controls != null)
        {
            ControlState state = _controls.State();
            if (state.Newsletter || state.PcOpen)
                Close(); // the shift report or the PC came up: they need the screen
        }
        StepTurn(dt);
        DrawWall();
        if (_open && _openT >= 1f)
            Hand();
        Draw(dt);
    }

    // ------------------------------------------------------------------ the view

    /// <summary>Turns the view to the board (D, S, a click on the painted board: OfficeControls).</summary>
    public void Open()
    {
        if (_open || _layout == null)
            return;
        _open = true;
        if (MotionPreference.Reduced)
            _openT = 1f;
        Play(SwitchSound.PlugOut, 0.35f);
    }

    /// <summary>Turns back to the desk (A, S, a right-click, Esc: OfficeControls); a plug or MASTER in the hand goes back first, and a throw of MASTER not yet landed counts for nothing.</summary>
    public void Close()
    {
        if (!_open)
            return;
        CancelDrag();
        _leverThrow = false;
        _lever.Target = 0f;
        _open = false;
        if (_grab != null)
            _grab.gameObject.SetActive(false);
        if (MotionPreference.Reduced)
            _openT = 0f;
    }

    /// <summary>Lets go of what the hand holds without changing the board (a right-click or Esc while dragging): a plug goes back where it was, MASTER rises; false when the hand was empty.</summary>
    public bool CancelDrag()
    {
        if (_leverHeld)
        {
            _leverHeld = false;
            _lever.Target = 0f;
            return true;
        }
        if (_held == null)
            return false;
        ReturnHeld();
        return true;
    }

    // ------------------------------------------------------------------ the day

    /// <summary>Builds today's board once the shift has its world: the rules from the knobs, the modules the day opens, module 01 powered (LOOP_v5 day 1: "one free module already powered"), today's places on the plates, the approvals followed.</summary>
    private void TryBeginDay()
    {
        GameManager game = GameManager.Current;
        if (game == null || game.World == null || !RunManager.HasInstance)
            return;
        ContentLibrarySO library = RunManager.Instance.Library;
        DayPlanSO plan = RunManager.Instance.GetCurrentDayPlan();
        _day = game.World.day;
        SwitchboardRules rules = _config.Rules();
        _board = new Switchboard(rules, _layout.Modules.Count, _layout.Destinations.Count, _layout.PeriodRows);
        for (int m = 0; m < _layout.Modules.Count; m++)
            if (rules.ModuleOpen(m, _day) && !_layout.Modules[m].Placarded)
                _board.SetInService(m, true);
        if (_board.Modules.Count > 0 && _board.Modules[0].State != SwitchModuleState.Closed)
            _board.PlugPower(0, 0);
        _directory = SwitchboardDirectory.Build(TodaysPlaces(game, library, plan), _layout.Destinations.Count, _layout.PeriodRows);
        _game = game;
        _game.Resolved += HandleResolved;
        PrintLabels();
        ShowHint(_day <= _config.hintUntilDay);
        SyncCordsFromBoard();
        var listed = new List<string>();
        foreach (SwitchDestination d in _directory.Destinations)
        {
            var periods = new List<string>();
            foreach (SwitchPeriod p in d.Periods)
                periods.Add(p.Label);
            listed.Add(d.Label + " [" + string.Join(", ", periods) + "]");
        }
        Debug.Log($"[Switchboard] Day {_day}: {string.Join("; ", listed)}; modules in service: {InServiceList()}.");
    }

    /// <summary>Today's places in the board's priority: the open destinations (OpenDestinations rules), the portal routes, then today's places (book order).</summary>
    private static IEnumerable<SwitchPlace> TodaysPlaces(GameManager game, ContentLibrarySO library, DayPlanSO plan)
    {
        if (library == null)
            yield break;
        if (plan != null)
            foreach (TravelRuleSO rule in plan.ActiveTravelRules)
            {
                if (rule == null || rule.type != TravelRuleType.OpenDestinations)
                    continue;
                foreach (string key in rule.Directive.Open)
                {
                    int colon = key != null ? key.IndexOf(':') : -1;
                    if (colon > 0 && PlaceOf(library, library.GetProfile(new PlaceRef(key.Substring(0, colon), key.Substring(colon + 1))), out SwitchPlace open))
                        yield return open;
                }
            }
        foreach (PortalRoute route in game.Portals.Portals)
            if (route.Place.HasValue && PlaceOf(library, library.GetProfile(route.Place.Value), out SwitchPlace routed))
                yield return routed;
        if (plan != null)
            foreach (NationEraProfileSO profile in library.TodaysProfiles(plan))
                if (PlaceOf(library, profile, out SwitchPlace place))
                    yield return place;
    }

    /// <summary>A place as the board prints it: its nation's and era's names in capitals, its era's order and its year.</summary>
    private static bool PlaceOf(ContentLibrarySO library, NationEraProfileSO profile, out SwitchPlace place)
    {
        place = default;
        if (profile == null || profile.nation == null || profile.era == null)
            return false;
        place = new SwitchPlace(profile.nation.id, profile.era.id, Caps(profile.nation.displayName, profile.nation.id), Caps(profile.era.displayName, profile.era.id),
                                profile.era.order, profile.year);
        return true;
    }

    private static string Caps(string name, string id) => (string.IsNullOrWhiteSpace(name) ? id : name).ToUpperInvariant();

    /// <summary>A traveller decided at the desk: an approved one becomes a call on their destination's route (a place not printed takes a free jack); the verdict is the desk's and stays so.</summary>
    private void HandleResolved(CaseInstance inst, CaseVerdict verdict, bool accepted, float stabilityBefore)
    {
        if (!accepted || _board == null || inst == null || inst.claimedNation == null || inst.claimedEra == null)
            return;
        ContentLibrarySO library = RunManager.HasInstance ? RunManager.Instance.Library : null;
        NationEraProfileSO profile = library != null ? library.GetProfile(inst.claimedNation, inst.claimedEra) : null;
        SwitchPlace place = profile != null && PlaceOf(library, profile, out SwitchPlace found)
            ? found
            : new SwitchPlace(inst.claimedNation.id, inst.claimedEra.id, Caps(inst.claimedNation.displayName, inst.claimedNation.id),
                              Caps(inst.claimedEra.displayName, inst.claimedEra.id), inst.claimedEra.order, 0);
        if (!_directory.Place(place, out SwitchRoute route))
        {
            Debug.Log($"[Switchboard] {inst.visitorDisplayName} ({place.NationLabel}, {place.EraLabel}): no free jack on the board; they leave at once.");
            return;
        }
        PrintLabels();
        _board.AddCall(route);
        Debug.Log($"[Switchboard] Call: {inst.visitorDisplayName} for {place.NationLabel}, {place.EraLabel} (route {route}); {_board.Waiting(route)} waiting there.");
    }

    // ------------------------------------------------------------------ what happened

    /// <summary>Plays what the rules did since the last frame: the buzz, the clunk's aftermath, the open beep, the departure's flare and bonus, the fuse.</summary>
    private void PlayEvents()
    {
        _events.Clear();
        if (_board.TakeEvents(_events) == 0)
            return;
        foreach (SwitchEvent e in _events)
        {
            switch (e.Kind)
            {
                case SwitchEventKind.CallArrived:
                    Play(SwitchSound.Buzz, 0.55f);
                    _lastBuzz = Time.unscaledTime;
                    break;
                case SwitchEventKind.Committed:
                    Debug.Log($"[Switchboard] Module {_layout.Modules[e.Module].Id} locked in on route {e.Route}: calibrating {_board.Rules.CalibrationSeconds:0} s while powered.");
                    break;
                case SwitchEventKind.Opened:
                    Play(SwitchSound.Opened, 0.7f);
                    Flare(_layout.Modules[e.Module].Lamp.Centre, LampGreen, 2.6f);
                    break;
                case SwitchEventKind.Departed:
                    Departed(e);
                    break;
                case SwitchEventKind.FusePopped:
                    Play(SwitchSound.Fuse, 0.9f);
                    Sparks(_layout.Modules[e.Module].Lamp.Centre);
                    Shake(_config.fuseShake);
                    Debug.Log($"[Switchboard] Module {_layout.Modules[e.Module].Id}: wrong period, the fuse popped ({_board.Rules.FuseSeconds:0.#} s).");
                    break;
            }
        }
    }

    /// <summary>A group left: the chime, the flare at both lamps, the tally and the bonus into the wallet (and the shift's other money, so the report's net counts it).</summary>
    private void Departed(SwitchEvent e)
    {
        Play(SwitchSound.Departed, 0.8f);
        LayoutModule module = _layout.Modules[e.Module];
        Flare(_layout.Destinations[e.Route.Destination].Lamp.Centre, LampWarm, 5f);
        Flare(module.Lamp.Centre, LampGreen, 3f);
        BoardPoint rise = DisplayCentre(module);
        Rise(rise, Words(SwitchboardWords.Sent, e.Travellers), LampGreen);
        if (e.Bonus > 0 && _game != null && _game.World != null)
        {
            _game.World.money += e.Bonus;
            if (_game.Ledger != null)
                _game.Ledger.otherMoney += e.Bonus;
            WalletEvents.Raise();
            Rise(rise.Plus(0f, -34f), Words(SwitchboardWords.Bonus, e.Bonus, UiText.Currency(UiText.WalletForm.Short)), LampWarm, 0.35f);
        }
        SwitchDestination d = e.Route.Destination < _directory.Destinations.Count ? _directory.Destinations[e.Route.Destination] : null;
        string where = d != null && e.Route.Period < d.Periods.Count ? d.Label + ", " + d.Periods[e.Route.Period].Label : e.Route.ToString();
        Debug.Log($"[Switchboard] Module {module.Id} sent {e.Travellers} to {where} (group bonus {e.Bonus}).");
    }

    /// <summary>A soft buzz again while a call blinks unanswered (SwitchboardConfigSO.reminderBuzzSeconds).</summary>
    private void Remind()
    {
        if (_board.WaitingTotal == 0 || _config.reminderBuzzSeconds <= 0f || Time.unscaledTime - _lastBuzz < _config.reminderBuzzSeconds)
            return;
        for (int d = 0; d < _board.Destinations; d++)
            if (_board.CallLamp(d) == SwitchCallLamp.Calling)
            {
                Play(SwitchSound.Buzz, 0.35f);
                _lastBuzz = Time.unscaledTime;
                return;
            }
    }

    /// <summary>The printed words of <paramref name="key"/> with its placeholders filled; empty until Generate World has put the key in the tables (never the key itself).</summary>
    private static string Words(string key, params object[] args)
    {
        string text = UiText.Format(key, args);
        return text == key ? string.Empty : text;
    }

    /// <summary>The modules in service today, by number ("01, 02").</summary>
    private string InServiceList()
    {
        var open = new List<string>();
        foreach (SwitchModule m in _board.Modules)
            if (m.State != SwitchModuleState.Closed)
                open.Add(_layout.Modules[m.Index].Id);
        return open.Count > 0 ? string.Join(", ", open) : "none";
    }

    // ------------------------------------------------------------------ the cheat menu

    /// <summary>
    /// The cheat menu's review calls: modules 01 to 03 in service, then three
    /// calls on the printed board (two on the first destination's first
    /// period, one on the second destination's last period, or the first's
    /// second), as if three travellers were approved. Returns the overlay's
    /// line.
    /// </summary>
    public string AddTestCalls()
    {
        if (_board == null || _directory == null || _directory.Destinations.Count == 0)
            return "The switchboard has no day yet (start the shift first).";
        for (int m = 0; m < 3 && m < _board.Modules.Count; m++)
            if (!_layout.Modules[m].Placarded)
                _board.SetInService(m, true);
        SyncCordsFromBoard();
        SwitchDestination first = _directory.Destinations[0];
        var calls = new List<SwitchRoute> { new SwitchRoute(0, 0), new SwitchRoute(0, 0) };
        if (_directory.Destinations.Count > 1)
            calls.Add(new SwitchRoute(1, _directory.Destinations[1].Periods.Count - 1));
        else
            calls.Add(new SwitchRoute(0, Mathf.Min(1, first.Periods.Count - 1)));
        var said = new List<string>();
        foreach (SwitchRoute r in calls)
        {
            _board.AddCall(r);
            SwitchDestination d = _directory.Destinations[r.Destination];
            said.Add(d.Label + " " + d.Periods[r.Period].Label);
        }
        string line = "Switchboard: 3 test calls (" + string.Join(", ", said) + "); modules " + InServiceList() + " in service.";
        Debug.Log("[Switchboard] " + line);
        return line;
    }
}
