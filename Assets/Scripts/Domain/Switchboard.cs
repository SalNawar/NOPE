using System;
using System.Collections.Generic;

/// <summary>
/// A pair of jacks on the switchboard: one destination's slot (its name plate,
/// its place jack and its call lamp) and one of the period jacks under it. A
/// call waits on a route; a portal module routes one. Not serialized.
/// </summary>
public readonly struct SwitchRoute : IEquatable<SwitchRoute>
{
    /// <summary>The destination's slot on the board (0 = the leftmost name plate).</summary>
    public readonly int Destination;

    /// <summary>The period row under that destination (0 = the top period jack).</summary>
    public readonly int Period;

    /// <summary>The route of <paramref name="destination"/>'s period row <paramref name="period"/>.</summary>
    public SwitchRoute(int destination, int period)
    {
        Destination = destination;
        Period = period;
    }

    /// <summary>True when both the destination and the period match.</summary>
    public bool Equals(SwitchRoute other) => Destination == other.Destination && Period == other.Period;

    /// <summary>True for an equal route.</summary>
    public override bool Equals(object obj) => obj is SwitchRoute other && Equals(other);

    /// <summary>A hash of both parts.</summary>
    public override int GetHashCode() => Destination * 397 ^ Period;

    /// <summary>The route as "destination/period" (the log names a route by it).</summary>
    public override string ToString() => Destination + "/" + Period;

    /// <summary>Equal routes.</summary>
    public static bool operator ==(SwitchRoute a, SwitchRoute b) => a.Equals(b);

    /// <summary>Different routes.</summary>
    public static bool operator !=(SwitchRoute a, SwitchRoute b) => !a.Equals(b);
}

/// <summary>Where a portal module is in its loop (Switchboard). Not serialized.</summary>
public enum SwitchModuleState
{
    /// <summary>Not in service today: it takes no cord (its LOCKED or OUT OF SERVICE placard, or not yet opened in the ramp).</summary>
    Closed,

    /// <summary>In service with both its PLACE and PERIOD cords stowed.</summary>
    Free,

    /// <summary>One or both of its cords are plugged and MASTER has not locked the route in.</summary>
    Patching,

    /// <summary>MASTER locked a sound route in: it calibrates while a power cord is in it.</summary>
    Committed,

    /// <summary>Calibrated: the route is live and sends its waiting travellers while powered.</summary>
    Open,

    /// <summary>MASTER found a wrong period: the fuse popped and the module is dark for a moment (time only).</summary>
    Blown
}

/// <summary>What a destination's call lamp shows (Switchboard.CallLamp). Not serialized.</summary>
public enum SwitchCallLamp
{
    /// <summary>Nothing waits and nothing was sent: dark.</summary>
    Dark,

    /// <summary>A traveller waits on a route no module has locked in: it blinks (faster the longer they wait).</summary>
    Calling,

    /// <summary>Every waiting traveller's route is locked in, or a route is open with nobody sent yet: steady.</summary>
    Connected,

    /// <summary>A module routed here has sent its group and still holds the route: DONE until it is cleared.</summary>
    Done
}

/// <summary>What happened on the board (Switchboard.TakeEvents): the view plays it. Not serialized.</summary>
public enum SwitchEventKind
{
    /// <summary>An approved traveller's call came in on a route.</summary>
    CallArrived,

    /// <summary>MASTER locked a module's route in (it calibrates while powered).</summary>
    Committed,

    /// <summary>A module finished calibrating: its route is live.</summary>
    Opened,

    /// <summary>A module sent every traveller waiting on its route as one group.</summary>
    Departed,

    /// <summary>MASTER found a module patched to a wrong period: its fuse popped.</summary>
    FusePopped,

    /// <summary>A popped fuse reset: the module can be pulled again.</summary>
    FuseReset,

    /// <summary>MASTER's breaker was closed and a patch waits for it: the breaker opened again (it trips), so it can be pulled for that patch.</summary>
    BreakerTripped
}

/// <summary>One thing that happened on the board, for the view to play (the lamps, the sounds, the flare). Not serialized.</summary>
public readonly struct SwitchEvent
{
    /// <summary>What happened.</summary>
    public readonly SwitchEventKind Kind;

    /// <summary>The module it happened to (-1 for a call).</summary>
    public readonly int Module;

    /// <summary>The route it concerns.</summary>
    public readonly SwitchRoute Route;

    /// <summary>For a departure: how many travellers left as one group.</summary>
    public readonly int Travellers;

    /// <summary>For a departure: the group bonus (Switchboard.GroupBonus).</summary>
    public readonly int Bonus;

    /// <summary>An event of <paramref name="kind"/>.</summary>
    public SwitchEvent(SwitchEventKind kind, int module, SwitchRoute route, int travellers = 0, int bonus = 0)
    {
        Kind = kind;
        Module = module;
        Route = route;
        Travellers = travellers;
        Bonus = bonus;
    }
}

/// <summary>Where a let-go MASTER goes (Switchboard.Throw): back where its breaker rests, or thrown to its other end. Not serialized.</summary>
public enum MasterThrow
{
    /// <summary>Not thrown far enough: back where the breaker rests; nothing changes.</summary>
    None,

    /// <summary>Thrown open (up): the breaker opens; no route changes.</summary>
    Open,

    /// <summary>Thrown closed (down): the pull (Switchboard.PullMaster).</summary>
    Closed
}

/// <summary>What one pull of MASTER did (Switchboard.PullMaster). Not serialized.</summary>
public readonly struct MasterPull
{
    /// <summary>How many modules' routes it locked in.</summary>
    public readonly int Committed;

    /// <summary>How many modules' fuses it popped (a wrong period).</summary>
    public readonly int Fused;

    /// <summary>A pull's outcome.</summary>
    public MasterPull(int committed, int fused)
    {
        Committed = committed;
        Fused = fused;
    }

    /// <summary>True when the pull changed nothing (no patched module waited for it).</summary>
    public bool Idle => Committed == 0 && Fused == 0;
}

/// <summary>The switchboard's knobs (SwitchboardConfigSO fills them; the defaults are LOOP_v5's and Saleh's numbers). Not serialized.</summary>
public sealed class SwitchboardRules
{
    /// <summary>Real seconds a locked-in route calibrates while powered before it opens (Saleh: "20s to calibrate", real seconds).</summary>
    public float CalibrationSeconds = 20f;

    /// <summary>Seconds a popped fuse keeps its module dark (Saleh: the fuse pop costs time only).</summary>
    public float FuseSeconds = 2f;

    /// <summary>Credits per extra traveller in one group sent (the portals spec: a send of k pays 5(k-1); "batch bonus small").</summary>
    public int BonusPerExtraTraveller = 5;

    /// <summary>The power cords the board has (Saleh: power is the cable you keep moving; 2 for 5 modules).</summary>
    public int PowerCords = 2;

    /// <summary>A fresh call's blink rate (blinks a second).</summary>
    public float BlinkHz = 1.2f;

    /// <summary>The fastest blink, reached after <see cref="BlinkRampSeconds"/> of waiting.</summary>
    public float BlinkHzMax = 4f;

    /// <summary>Seconds of waiting from the fresh blink to the fastest (LOOP_v5: calls blink faster the longer they wait).</summary>
    public float BlinkRampSeconds = 90f;

    /// <summary>The first day each module (01 first) is in service; 0 = never (the plate paints 04 LOCKED and 05 OUT OF SERVICE). Ours: 01 from day 1, 02 from day 3, 03 from day 5 (LOOP_v5's ramp: "Day 5: power is short").</summary>
    public int[] ModuleFromDay = { 1, 3, 5, 0, 0 };

    /// <summary>The bars a route of each period needs before its module counts as powered (index = the period; the Stargate dialer: the eras Ancient 3, Medieval 2, Early modern 2, Industrial 1, Modern 1, "Move the units"). Empty, or a period past its end: 1 bar (the Classic board's one power cord).</summary>
    public int[] PeriodBars = new int[0];

    /// <summary>How many power cords one module takes at once (the Classic board's POWER socket: 1; the dialer's cradle: 3 cells, one bar each).</summary>
    public int CellsPerModule = 1;

    /// <summary>True when MASTER engages only the patched modules whose power reaches their route's bars (the dialer: MASTER is the final lock); false: every patched module, powered or not (Classic).</summary>
    public bool MasterNeedsPower;

    /// <summary>True when a destination's place jack takes one module's PLACE plug (Classic); false lets several modules serve one destination, each on its own period (the dialer: one dial per route).</summary>
    public bool PlaceTakesOnePlug = true;

    /// <summary>The bars a route on <paramref name="period"/> needs (at least 1).</summary>
    public int BarsFor(int period) => PeriodBars != null && period >= 0 && period < PeriodBars.Length ? Math.Max(1, PeriodBars[period]) : 1;

    /// <summary>True when module <paramref name="module"/> (0 = 01) is in service on <paramref name="day"/>.</summary>
    public bool ModuleOpen(int module, int day)
    {
        if (ModuleFromDay == null || module < 0 || module >= ModuleFromDay.Length)
            return false;
        int from = ModuleFromDay[module];
        return from > 0 && day >= from;
    }
}

/// <summary>One portal module of the board as the rules see it (read only outside the Switchboard). Not serialized.</summary>
public sealed class SwitchModule
{
    /// <summary>A module at <paramref name="index"/> (0 = 01), closed until the day opens it.</summary>
    public SwitchModule(int index) => Index = index;

    /// <summary>Its place on the board (0 = module 01).</summary>
    public int Index { get; }

    /// <summary>Where it is in its loop.</summary>
    public SwitchModuleState State { get; internal set; } = SwitchModuleState.Closed;

    /// <summary>The destination slot its PLACE cord is plugged into, or -1 (stowed).</summary>
    public int PlaceJack { get; internal set; } = -1;

    /// <summary>True while its PERIOD cord is plugged into a period jack (<see cref="PeriodJack"/>).</summary>
    public bool HasPeriod { get; internal set; }

    /// <summary>The period jack its PERIOD cord is plugged into (valid with <see cref="HasPeriod"/>).</summary>
    public SwitchRoute PeriodJack { get; internal set; }

    /// <summary>The power cord in its POWER socket (the lowest-numbered one when it takes several), or -1.</summary>
    public int PowerCord { get; internal set; } = -1;

    /// <summary>How many power cords (the dialer's cells) are in it: one bar each.</summary>
    public int Cells { get; internal set; }

    /// <summary>The bars its period needs (SwitchboardRules.BarsFor; 1 with no period plugged).</summary>
    public int Need { get; internal set; } = 1;

    /// <summary>The route MASTER locked in (valid while Committed or Open).</summary>
    public SwitchRoute Route { get; internal set; }

    /// <summary>Calibration seconds left (Committed); it counts only while powered.</summary>
    public float CalibrationLeft { get; internal set; }

    /// <summary>Seconds left on a popped fuse (Blown).</summary>
    internal float FuseLeft { get; set; }

    /// <summary>Travellers sent on its current route (the DONE lamp shows once one left).</summary>
    public int Sent { get; internal set; }

    /// <summary>True while its power reaches its period's bars (Classic: a power cord is in it).</summary>
    public bool Powered => Cells > 0 && Cells >= Need;

    /// <summary>True when both its PLACE and PERIOD cords are plugged.</summary>
    public bool Patched => PlaceJack >= 0 && HasPeriod;

    /// <summary>True while its route is locked in (calibrating or open).</summary>
    public bool Holding => State == SwitchModuleState.Committed || State == SwitchModuleState.Open;
}

/// <summary>
/// The portal switchboard's rules (LOOP_v5 on the v3 board Saleh locked; the
/// portals spec): an approved traveller becomes a call on their destination's
/// route and waits (AddCall); the player answers by plugging a free module's
/// PLACE cord into the destination's jack and its PERIOD cord into the
/// right period jack, putting one of the board's two power cords into that
/// module (Saleh: power is the cable you keep moving), then pulling MASTER
/// (PullMaster), which locks every patched module's route in at once; a
/// route whose period is not its destination's, or not one that is calling
/// there, pops the module's fuse (Saleh: time only). A locked-in route
/// calibrates for real seconds (Saleh: 20 s) only while powered (Tick), then
/// opens; an open, powered route sends every traveller waiting on it as one
/// group (the group bonus, small) and keeps the route until a cord is
/// pulled, so the next call on it leaves at once. Nothing here judges a
/// traveller: verdicts are the desk's. Pure, tested headless; the office's
/// SwitchboardView draws it and moves its cords.
/// </summary>
public sealed class Switchboard
{
    private readonly SwitchboardRules _rules;
    private readonly List<SwitchModule> _modules = new List<SwitchModule>();
    private readonly int[] _powerCords;
    private readonly Dictionary<SwitchRoute, int> _waiting = new Dictionary<SwitchRoute, int>();
    private readonly Dictionary<SwitchRoute, float> _waited = new Dictionary<SwitchRoute, float>();
    private readonly List<SwitchEvent> _events = new List<SwitchEvent>();
    private readonly List<SwitchRoute> _scratch = new List<SwitchRoute>();

    /// <summary>A board of <paramref name="modules"/> closed modules, <paramref name="destinations"/> destination slots of <paramref name="periods"/> period jacks each, and the rules' power cords, all parked.</summary>
    public Switchboard(SwitchboardRules rules, int modules, int destinations, int periods)
    {
        _rules = rules ?? new SwitchboardRules();
        for (int i = 0; i < Math.Max(0, modules); i++)
            _modules.Add(new SwitchModule(i));
        Destinations = Math.Max(0, destinations);
        Periods = Math.Max(0, periods);
        _powerCords = new int[Math.Max(0, _rules.PowerCords)];
        for (int i = 0; i < _powerCords.Length; i++)
            _powerCords[i] = -1;
    }

    /// <summary>The knobs this board runs on.</summary>
    public SwitchboardRules Rules => _rules;

    /// <summary>A module's locked-in route finished calibrating and is live (the module, its route): the gate stands open. For the hall's portals; additive to TakeEvents' Opened.</summary>
    public event Action<int, SwitchRoute> RouteOpened;

    /// <summary>A live route sends a group (the module, its route, how many travellers): they leave through it now. Additive to TakeEvents' Departed.</summary>
    public event Action<int, SwitchRoute, int> RouteSending;

    /// <summary>A locked-in route (calibrating or live) was dropped (the module, the route it held): a plug or the ring moved, or the module closed for the day. The gate shuts.</summary>
    public event Action<int, SwitchRoute> RouteClosed;

    /// <summary>The portal modules, 01 first.</summary>
    public IReadOnlyList<SwitchModule> Modules => _modules;

    /// <summary>How many destination slots the board has.</summary>
    public int Destinations { get; }

    /// <summary>How many period jacks each destination has.</summary>
    public int Periods { get; }

    /// <summary>How many power cords the board has.</summary>
    public int PowerCords => _powerCords.Length;

    /// <summary>The module power cord <paramref name="cord"/> is plugged into, or -1 (parked on the power row).</summary>
    public int PowerCordModule(int cord) => cord >= 0 && cord < _powerCords.Length ? _powerCords[cord] : -1;

    /// <summary>The group bonus for <paramref name="travellers"/> sent at once: BonusPerExtraTraveller for each beyond the first.</summary>
    public int GroupBonus(int travellers) => travellers > 1 ? (travellers - 1) * Math.Max(0, _rules.BonusPerExtraTraveller) : 0;

    // ------------------------------------------------------------------ service

    /// <summary>Opens or closes <paramref name="module"/> for today; closing drops its cords (stowed, its power cord parked) and its route.</summary>
    public void SetInService(int module, bool inService)
    {
        if (!ValidModule(module))
            return;
        SwitchModule m = _modules[module];
        if (inService)
        {
            if (m.State == SwitchModuleState.Closed)
                m.State = SwitchModuleState.Free;
            return;
        }
        m.PlaceJack = -1;
        m.HasPeriod = false;
        for (int c = 0; c < _powerCords.Length; c++)
            if (_powerCords[c] == module)
                _powerCords[c] = -1;
        m.PowerCord = -1;
        m.Cells = 0;
        m.Need = 1;
        ClearRoute(m);
        m.State = SwitchModuleState.Closed;
    }

    // ------------------------------------------------------------------ calls

    /// <summary>One approved traveller's call on <paramref name="route"/> (a route off the board is ignored): they wait there until an open, powered module sends them.</summary>
    public void AddCall(SwitchRoute route)
    {
        if (!ValidRoute(route))
            return;
        _waiting.TryGetValue(route, out int n);
        _waiting[route] = n + 1;
        if (n == 0)
            _waited[route] = 0f;
        _events.Add(new SwitchEvent(SwitchEventKind.CallArrived, -1, route, n + 1));
    }

    /// <summary>Travellers waiting on <paramref name="route"/>.</summary>
    public int Waiting(SwitchRoute route) => _waiting.TryGetValue(route, out int n) ? n : 0;

    /// <summary>Travellers waiting on any period of <paramref name="destination"/>.</summary>
    public int WaitingAt(int destination)
    {
        int total = 0;
        foreach (KeyValuePair<SwitchRoute, int> w in _waiting)
            if (w.Key.Destination == destination)
                total += w.Value;
        return total;
    }

    /// <summary>Travellers waiting anywhere on the board.</summary>
    public int WaitingTotal
    {
        get
        {
            int total = 0;
            foreach (int n in _waiting.Values)
                total += n;
            return total;
        }
    }

    /// <summary>The longest any call at <paramref name="destination"/> has waited (seconds; 0 with none).</summary>
    public float OldestWait(int destination)
    {
        float oldest = 0f;
        foreach (KeyValuePair<SwitchRoute, float> w in _waited)
            if (w.Key.Destination == destination && w.Value > oldest)
                oldest = w.Value;
        return oldest;
    }

    // ------------------------------------------------------------------ cords

    /// <summary>True when <paramref name="module"/>'s PLACE cord may go into <paramref name="destination"/>'s place jack: the module is in service and the jack is free or already its own (or, without PlaceTakesOnePlug, any jack).</summary>
    public bool CanPlugPlace(int module, int destination)
    {
        if (!Live(module) || destination < 0 || destination >= Destinations)
            return false;
        int holder = PlaceJackHolder(destination);
        return holder < 0 || holder == module || !_rules.PlaceTakesOnePlug;
    }

    /// <summary>Plugs <paramref name="module"/>'s PLACE cord into <paramref name="destination"/> (false, and nothing changes, when it may not): a changed plug unlocks its route.</summary>
    public bool PlugPlace(int module, int destination)
    {
        if (!CanPlugPlace(module, destination))
            return false;
        SwitchModule m = _modules[module];
        if (m.PlaceJack == destination)
            return true;
        m.PlaceJack = destination;
        Repatched(m);
        return true;
    }

    /// <summary>Stows <paramref name="module"/>'s PLACE cord: its route is unlocked.</summary>
    public void UnplugPlace(int module)
    {
        if (!ValidModule(module) || _modules[module].PlaceJack < 0)
            return;
        _modules[module].PlaceJack = -1;
        Repatched(_modules[module]);
    }

    /// <summary>True when <paramref name="module"/>'s PERIOD cord may go into the period jack <paramref name="jack"/>: the module is in service and the jack is free or already its own.</summary>
    public bool CanPlugPeriod(int module, SwitchRoute jack)
    {
        if (!Live(module) || !ValidRoute(jack))
            return false;
        int holder = PeriodJackHolder(jack);
        return holder < 0 || holder == module;
    }

    /// <summary>Plugs <paramref name="module"/>'s PERIOD cord into <paramref name="jack"/> (false, and nothing changes, when it may not): a changed plug unlocks its route.</summary>
    public bool PlugPeriod(int module, SwitchRoute jack)
    {
        if (!CanPlugPeriod(module, jack))
            return false;
        SwitchModule m = _modules[module];
        if (m.HasPeriod && m.PeriodJack == jack)
            return true;
        m.PeriodJack = jack;
        m.HasPeriod = true;
        Repatched(m);
        return true;
    }

    /// <summary>Stows <paramref name="module"/>'s PERIOD cord: its route is unlocked.</summary>
    public void UnplugPeriod(int module)
    {
        if (!ValidModule(module) || !_modules[module].HasPeriod)
            return;
        _modules[module].HasPeriod = false;
        Repatched(_modules[module]);
    }

    /// <summary>True when power cord <paramref name="cord"/> may go into <paramref name="module"/>: the module is in service and the cord is in it already, or it holds fewer than CellsPerModule cords (Classic: its one POWER socket is free).</summary>
    public bool CanPlugPower(int cord, int module)
    {
        if (cord < 0 || cord >= _powerCords.Length || !Live(module))
            return false;
        return _powerCords[cord] == module || _modules[module].Cells < Math.Max(1, _rules.CellsPerModule);
    }

    /// <summary>Moves power cord <paramref name="cord"/> into <paramref name="module"/> (out of the module it was in; false, and nothing changes, when it may not). Power keeps a route: a calibrating module counts on, an open one sends, once its cords reach its period's bars.</summary>
    public bool PlugPower(int cord, int module)
    {
        if (!CanPlugPower(cord, module))
            return false;
        if (_powerCords[cord] == module)
            return true;
        UnplugPower(cord);
        _powerCords[cord] = module;
        Recount(_modules[module]);
        return true;
    }

    /// <summary>Parks power cord <paramref name="cord"/> on the power row (the dialer: the cell rack): its module stops counting and sending once its cords fall short of its bars, its route kept.</summary>
    public void UnplugPower(int cord)
    {
        if (cord < 0 || cord >= _powerCords.Length)
            return;
        int was = _powerCords[cord];
        _powerCords[cord] = -1;
        if (was >= 0)
            Recount(_modules[was]);
    }

    /// <summary>How many power cords are in <paramref name="m"/>, and the lowest-numbered of them.</summary>
    private void Recount(SwitchModule m)
    {
        m.Cells = 0;
        m.PowerCord = -1;
        for (int c = 0; c < _powerCords.Length; c++)
            if (_powerCords[c] == m.Index)
            {
                m.Cells++;
                if (m.PowerCord < 0)
                    m.PowerCord = c;
            }
    }

    /// <summary>The module whose PLACE cord is in <paramref name="destination"/>'s jack, or -1.</summary>
    public int PlaceJackHolder(int destination)
    {
        foreach (SwitchModule m in _modules)
            if (m.PlaceJack == destination && destination >= 0)
                return m.Index;
        return -1;
    }

    /// <summary>The module whose PERIOD cord is in the period jack <paramref name="jack"/>, or -1.</summary>
    public int PeriodJackHolder(SwitchRoute jack)
    {
        foreach (SwitchModule m in _modules)
            if (m.HasPeriod && m.PeriodJack == jack)
                return m.Index;
        return -1;
    }

    // ------------------------------------------------------------------ MASTER

    /// <summary>
    /// MASTER is one breaker (Saleh: "one breaker to the side", "a breaker
    /// that is satisfying to pull to lock in the changes"; 2026-10-28:
    /// "Master break is pulled down instead of being a break"): it stays
    /// where it is thrown. Open (up, the ready position) each morning; a pull
    /// closes it (PullMaster) and it stays closed. When a patch waits for it
    /// while it is closed, it trips open again (Tick: BreakerTripped), so the
    /// next pull locks that patch in. The player may also throw it open by
    /// hand (OpenBreaker). Opening it changes no route: each locked-in route
    /// keeps its own lock.
    /// </summary>
    public bool BreakerClosed { get; private set; }

    /// <summary>
    /// Where MASTER goes when the hand lets go of it (its handle at
    /// <paramref name="pull"/>, 0 up to 1 down, or where the hand last pulled
    /// it, <paramref name="hand"/>): from open it throws closed once either is
    /// past <paramref name="throwAt"/> (the portals spec: before 80 % nothing
    /// happens); from closed it throws open once either is past
    /// <paramref name="throwAt"/> of the way up; else it goes back.
    /// </summary>
    public static MasterThrow Throw(bool closed, float pull, float hand, float throwAt)
    {
        if (!closed)
            return Math.Max(pull, hand) >= throwAt ? MasterThrow.Closed : MasterThrow.None;
        return Math.Min(pull, hand) <= 1f - throwAt ? MasterThrow.Open : MasterThrow.None;
    }

    /// <summary>The player throws MASTER's breaker open (up): nothing else changes; false when it was open already.</summary>
    public bool OpenBreaker()
    {
        if (!BreakerClosed)
            return false;
        BreakerClosed = false;
        return true;
    }

    /// <summary>True when a module is patched (and, with MasterNeedsPower, powered to its bars) and waits for MASTER (its lamp asks for the pull).</summary>
    public bool MasterPending
    {
        get
        {
            foreach (SwitchModule m in _modules)
                if (Ready(m))
                    return true;
            return false;
        }
    }

    /// <summary>
    /// MASTER: every patched module waiting for it (with MasterNeedsPower,
    /// only those powered to their bars: the dialer's final lock) locks its
    /// route in (Committed: it calibrates while powered), unless its period jack is
    /// another destination's or its destination is calling on other
    /// periods only: a wrong period, whose fuse pops (Blown for FuseSeconds:
    /// time only). A route to a destination nobody calls yet locks in
    /// (opened ahead). Modules already locked in keep their routes. The
    /// breaker closes and stays closed (BreakerClosed), even when nothing
    /// waited for it.
    /// </summary>
    public MasterPull PullMaster()
    {
        BreakerClosed = true;
        int committed = 0, fused = 0;
        foreach (SwitchModule m in _modules)
        {
            if (!Ready(m))
                continue;
            var route = new SwitchRoute(m.PlaceJack, m.PeriodJack.Period);
            if (m.PeriodJack.Destination != m.PlaceJack || WrongPeriod(route, m.Index))
            {
                m.State = SwitchModuleState.Blown;
                m.FuseLeft = Math.Max(0f, _rules.FuseSeconds);
                _events.Add(new SwitchEvent(SwitchEventKind.FusePopped, m.Index, route));
                fused++;
                continue;
            }
            m.Route = route;
            m.State = SwitchModuleState.Committed;
            m.CalibrationLeft = Math.Max(0f, _rules.CalibrationSeconds);
            m.Sent = 0;
            _events.Add(new SwitchEvent(SwitchEventKind.Committed, m.Index, route));
            committed++;
        }
        return new MasterPull(committed, fused);
    }

    /// <summary>True when <paramref name="m"/> waits for MASTER: patched, not locked in, and (with MasterNeedsPower) powered to its bars.</summary>
    private bool Ready(SwitchModule m) => m.State == SwitchModuleState.Patching && m.Patched && (!_rules.MasterNeedsPower || m.Powered);

    /// <summary>True when <paramref name="route"/>'s destination has calls no locked-in module serves and none of them is on <paramref name="route"/>'s period.</summary>
    private bool WrongPeriod(SwitchRoute route, int module)
    {
        bool calling = false;
        foreach (KeyValuePair<SwitchRoute, int> w in _waiting)
        {
            if (w.Key.Destination != route.Destination || w.Value <= 0 || Served(w.Key, module))
                continue;
            if (w.Key.Period == route.Period)
                return false;
            calling = true;
        }
        return calling;
    }

    /// <summary>True when a locked-in module other than <paramref name="except"/> holds <paramref name="route"/>.</summary>
    private bool Served(SwitchRoute route, int except = -1)
    {
        foreach (SwitchModule m in _modules)
            if (m.Index != except && m.Holding && m.Route == route)
                return true;
        return false;
    }

    // ------------------------------------------------------------------ time

    /// <summary>
    /// Advances the board by <paramref name="seconds"/>: calls wait longer,
    /// popped fuses cool and reset, powered locked-in routes calibrate and
    /// open, and every open, powered route sends all travellers waiting on it
    /// as one group (Departed, with the group bonus); a closed MASTER breaker
    /// with a patch waiting for it trips open (BreakerTripped).
    /// </summary>
    public void Tick(float seconds)
    {
        if (seconds < 0f)
            seconds = 0f;
        _scratch.Clear();
        _scratch.AddRange(_waited.Keys);
        foreach (SwitchRoute r in _scratch)
            _waited[r] += seconds;

        foreach (SwitchModule m in _modules)
        {
            switch (m.State)
            {
                case SwitchModuleState.Blown:
                    m.FuseLeft -= seconds;
                    if (m.FuseLeft <= 0f)
                    {
                        m.FuseLeft = 0f;
                        m.State = m.PlaceJack >= 0 || m.HasPeriod ? SwitchModuleState.Patching : SwitchModuleState.Free;
                        _events.Add(new SwitchEvent(SwitchEventKind.FuseReset, m.Index, m.Route));
                    }
                    break;
                case SwitchModuleState.Committed:
                    if (!m.Powered)
                        break;
                    m.CalibrationLeft -= seconds;
                    if (m.CalibrationLeft <= 0f)
                    {
                        m.CalibrationLeft = 0f;
                        m.State = SwitchModuleState.Open;
                        _events.Add(new SwitchEvent(SwitchEventKind.Opened, m.Index, m.Route));
                        RouteOpened?.Invoke(m.Index, m.Route);
                    }
                    break;
            }
            if (m.State == SwitchModuleState.Open && m.Powered)
                Send(m);
        }
        Trip();
    }

    /// <summary>MASTER's breaker trips open when it is closed and a patch waits for it (a new patch, or a fuse that reset with its wrong period still in).</summary>
    private void Trip()
    {
        if (!BreakerClosed || !MasterPending)
            return;
        BreakerClosed = false;
        _events.Add(new SwitchEvent(SwitchEventKind.BreakerTripped, -1, default));
    }

    /// <summary>Sends everyone waiting on <paramref name="m"/>'s route as one group.</summary>
    private void Send(SwitchModule m)
    {
        int k = Waiting(m.Route);
        if (k <= 0)
            return;
        _waiting.Remove(m.Route);
        _waited.Remove(m.Route);
        m.Sent += k;
        _events.Add(new SwitchEvent(SwitchEventKind.Departed, m.Index, m.Route, k, GroupBonus(k)));
        RouteSending?.Invoke(m.Index, m.Route, k);
    }

    // ------------------------------------------------------------------ lamps

    /// <summary>
    /// <paramref name="destination"/>'s call lamp: Calling while a traveller
    /// waits on a route no module has locked in; Connected while every
    /// waiting route is locked in, or a route there is locked in with nobody
    /// sent yet; Done once a module routed there has sent and still holds the
    /// route; else Dark.
    /// </summary>
    public SwitchCallLamp CallLamp(int destination)
    {
        bool waiting = false;
        foreach (KeyValuePair<SwitchRoute, int> w in _waiting)
        {
            if (w.Key.Destination != destination || w.Value <= 0)
                continue;
            waiting = true;
            if (!Served(w.Key))
                return SwitchCallLamp.Calling;
        }
        if (waiting)
            return SwitchCallLamp.Connected;
        bool held = false;
        foreach (SwitchModule m in _modules)
        {
            if (!m.Holding || m.Route.Destination != destination)
                continue;
            if (m.State == SwitchModuleState.Open && m.Sent > 0)
                return SwitchCallLamp.Done;
            held = true;
        }
        return held ? SwitchCallLamp.Connected : SwitchCallLamp.Dark;
    }

    /// <summary>How fast <paramref name="destination"/>'s call lamp blinks (blinks a second): from BlinkHz for a fresh call to BlinkHzMax after BlinkRampSeconds of waiting.</summary>
    public float BlinkHz(int destination)
    {
        float ramp = _rules.BlinkRampSeconds > 0f ? Math.Min(1f, OldestWait(destination) / _rules.BlinkRampSeconds) : 1f;
        return _rules.BlinkHz + (_rules.BlinkHzMax - _rules.BlinkHz) * ramp;
    }

    // ------------------------------------------------------------------ events

    /// <summary>Moves what happened since the last call into <paramref name="into"/> (oldest first) and returns how many.</summary>
    public int TakeEvents(List<SwitchEvent> into)
    {
        int n = _events.Count;
        into?.AddRange(_events);
        _events.Clear();
        return n;
    }

    // ------------------------------------------------------------------ helpers

    /// <summary>A plug changed on <paramref name="m"/>: its route is unlocked (a popped fuse keeps cooling), its bars follow its period, and it is Free with no cord out.</summary>
    private void Repatched(SwitchModule m)
    {
        if (m.State == SwitchModuleState.Closed)
            return;
        m.Need = m.HasPeriod ? _rules.BarsFor(m.PeriodJack.Period) : 1;
        ClearRoute(m);
        if (m.State != SwitchModuleState.Blown)
            m.State = m.PlaceJack >= 0 || m.HasPeriod ? SwitchModuleState.Patching : SwitchModuleState.Free;
    }

    /// <summary>Drops <paramref name="m"/>'s route (RouteClosed when it held one).</summary>
    private void ClearRoute(SwitchModule m)
    {
        bool held = m.Holding;
        SwitchRoute route = m.Route;
        m.Route = default;
        m.CalibrationLeft = 0f;
        m.Sent = 0;
        if (held)
            RouteClosed?.Invoke(m.Index, route);
    }

    private bool ValidModule(int module) => module >= 0 && module < _modules.Count;

    private bool Live(int module) => ValidModule(module) && _modules[module].State != SwitchModuleState.Closed;

    private bool ValidRoute(SwitchRoute r) => r.Destination >= 0 && r.Destination < Destinations && r.Period >= 0 && r.Period < Periods;
}

/// <summary>What the hand on the switchboard does this frame (SwitchHand.Step). Not serialized.</summary>
public enum SwitchHandStep
{
    /// <summary>Nothing: the board is not turned to, or the hand may not act now.</summary>
    None,

    /// <summary>Let go of what the hand holds with nothing changed: a plug back where it was, MASTER back where its breaker rests (SwitchboardView.CancelDrag).</summary>
    Cancel,

    /// <summary>The hand acts: it holds, lets go (a plug seats, MASTER throws), or picks up.</summary>
    Hand
}

/// <summary>
/// When the hand on the switchboard may act (the night's code review, F1;
/// BUG-55: "let go without being committed"). The Input System sends a
/// button release when the game loses the focus (Alt+Tab, another window),
/// and a let-go plug seats or a let-go MASTER throws; so without the focus a
/// held plug or MASTER is let go with nothing changed, and nothing is picked
/// up. The same while the input gate is held (the code review, F2: the
/// cheat panel or a dialog over the open board; InputGate.Blocked), so a
/// click there never picks up a plug or pulls MASTER. Pure.
/// </summary>
public static class SwitchHand
{
    /// <summary>The step: <paramref name="turned"/> the board fully turned to, <paramref name="focused"/> the game window has the focus, <paramref name="blocked"/> the input gate is held (InputGate.Blocked), <paramref name="holding"/> a plug or MASTER is in the hand.</summary>
    public static SwitchHandStep Step(bool turned, bool focused, bool blocked, bool holding)
    {
        if (!focused || blocked)
            return holding ? SwitchHandStep.Cancel : SwitchHandStep.None;
        return turned ? SwitchHandStep.Hand : SwitchHandStep.None;
    }
}
