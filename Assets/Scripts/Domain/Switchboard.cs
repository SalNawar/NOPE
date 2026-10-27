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
    FuseReset
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

    /// <summary>The power cord in its POWER socket, or -1.</summary>
    public int PowerCord { get; internal set; } = -1;

    /// <summary>The route MASTER locked in (valid while Committed or Open).</summary>
    public SwitchRoute Route { get; internal set; }

    /// <summary>Calibration seconds left (Committed); it counts only while powered.</summary>
    public float CalibrationLeft { get; internal set; }

    /// <summary>Seconds left on a popped fuse (Blown).</summary>
    internal float FuseLeft { get; set; }

    /// <summary>Travellers sent on its current route (the DONE lamp shows once one left).</summary>
    public int Sent { get; internal set; }

    /// <summary>True while a power cord is in it.</summary>
    public bool Powered => PowerCord >= 0;

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
        if (m.PowerCord >= 0)
            _powerCords[m.PowerCord] = -1;
        m.PowerCord = -1;
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

    /// <summary>True when <paramref name="module"/>'s PLACE cord may go into <paramref name="destination"/>'s place jack: the module is in service and the jack is free or already its own.</summary>
    public bool CanPlugPlace(int module, int destination)
    {
        if (!Live(module) || destination < 0 || destination >= Destinations)
            return false;
        int holder = PlaceJackHolder(destination);
        return holder < 0 || holder == module;
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

    /// <summary>True when power cord <paramref name="cord"/> may go into <paramref name="module"/>'s POWER socket: the module is in service and its socket is free or already this cord's.</summary>
    public bool CanPlugPower(int cord, int module)
    {
        if (cord < 0 || cord >= _powerCords.Length || !Live(module))
            return false;
        int inIt = _modules[module].PowerCord;
        return inIt < 0 || inIt == cord;
    }

    /// <summary>Moves power cord <paramref name="cord"/> into <paramref name="module"/> (out of the module it was in; false, and nothing changes, when it may not). Power keeps a route: a calibrating module counts on, an open one sends.</summary>
    public bool PlugPower(int cord, int module)
    {
        if (!CanPlugPower(cord, module))
            return false;
        UnplugPower(cord);
        _powerCords[cord] = module;
        _modules[module].PowerCord = cord;
        return true;
    }

    /// <summary>Parks power cord <paramref name="cord"/> on the power row: its module stops counting and sending, its route kept.</summary>
    public void UnplugPower(int cord)
    {
        if (cord < 0 || cord >= _powerCords.Length)
            return;
        int was = _powerCords[cord];
        if (was >= 0)
            _modules[was].PowerCord = -1;
        _powerCords[cord] = -1;
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

    /// <summary>True when a module is patched and waits for MASTER (its lamp asks for the pull).</summary>
    public bool MasterPending
    {
        get
        {
            foreach (SwitchModule m in _modules)
                if (m.State == SwitchModuleState.Patching && m.Patched)
                    return true;
            return false;
        }
    }

    /// <summary>
    /// MASTER: every patched module waiting for it locks its route in
    /// (Committed: it calibrates while powered), unless its period jack is
    /// another destination's or its destination is calling on other
    /// periods only: a wrong period, whose fuse pops (Blown for FuseSeconds:
    /// time only). A route to a destination nobody calls yet locks in
    /// (opened ahead). Modules already locked in keep their routes.
    /// </summary>
    public MasterPull PullMaster()
    {
        int committed = 0, fused = 0;
        foreach (SwitchModule m in _modules)
        {
            if (m.State != SwitchModuleState.Patching || !m.Patched)
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
    /// as one group (Departed, with the group bonus).
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
                    }
                    break;
            }
            if (m.State == SwitchModuleState.Open && m.Powered)
                Send(m);
        }
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

    /// <summary>A plug changed on <paramref name="m"/>: its route is unlocked (a popped fuse keeps cooling), and it is Free with no cord out.</summary>
    private void Repatched(SwitchModule m)
    {
        if (m.State == SwitchModuleState.Closed)
            return;
        if (m.State != SwitchModuleState.Blown)
            m.State = m.PlaceJack >= 0 || m.HasPeriod ? SwitchModuleState.Patching : SwitchModuleState.Free;
        ClearRoute(m);
    }

    private static void ClearRoute(SwitchModule m)
    {
        m.Route = default;
        m.CalibrationLeft = 0f;
        m.Sent = 0;
    }

    private bool ValidModule(int module) => module >= 0 && module < _modules.Count;

    private bool Live(int module) => ValidModule(module) && _modules[module].State != SwitchModuleState.Closed;

    private bool ValidRoute(SwitchRoute r) => r.Destination >= 0 && r.Destination < Destinations && r.Period >= 0 && r.Period < Periods;
}
