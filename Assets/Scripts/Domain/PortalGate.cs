using System;

/// <summary>Where a hall ring is as a gate (PortalGateTrack; the night of 2026-11-07, DECISIONS D6). Runtime only, not serialized.</summary>
public enum PortalGateState
{
    /// <summary>A dark ring: nothing in it, no chevrons, no light.</summary>
    Offline,

    /// <summary>Its module is locked in and calibrating: the chevrons light one by one.</summary>
    LockedIn,

    /// <summary>The event horizon shows: a dialed route is live, or the day keeps the ring open (the Return Gate).</summary>
    Open,

    /// <summary>A group goes through: the kawoosh plays on the event horizon.</summary>
    Sending,

    /// <summary>The event horizon and the chevrons fade out.</summary>
    Closing
}

/// <summary>What drives one ring this frame (AnimeHallPortalLink: its switchboard module, or the day's look). Runtime only, not serialized.</summary>
public readonly struct PortalGateFeed
{
    /// <summary>The day's look keeps the event horizon open without a dial (the Return Gate in service; without a switchboard, an open route).</summary>
    public readonly bool Steady;

    /// <summary>The ring's module is locked in and calibrating (Switchboard: Committed).</summary>
    public readonly bool Dialing;

    /// <summary>Seconds of calibration done.</summary>
    public readonly float Calibrated;

    /// <summary>Seconds a whole calibration takes (SwitchboardRules.CalibrationSeconds).</summary>
    public readonly float CalibrationSeconds;

    /// <summary>The dialed route is live (Switchboard: Open): the event horizon with every chevron lit.</summary>
    public readonly bool Live;

    /// <summary>A feed.</summary>
    public PortalGateFeed(bool steady, bool dialing, float calibrated, float calibrationSeconds, bool live)
    {
        Steady = steady;
        Dialing = dialing;
        Calibrated = calibrated;
        CalibrationSeconds = calibrationSeconds;
        Live = live;
    }

    /// <summary>
    /// The feed of a ring dialed by <paramref name="module"/> (null: no module, the
    /// day's look alone) on a board whose calibration takes
    /// <paramref name="calibrationSeconds"/>: locked in while it is Committed (its
    /// calibration so far: the whole time less what is left), live while it is
    /// Open; <paramref name="steady"/> as the day's look says. Read only.
    /// </summary>
    public static PortalGateFeed Of(SwitchModule module, float calibrationSeconds, bool steady)
    {
        if (module == null)
            return new PortalGateFeed(steady, false, 0f, calibrationSeconds, false);
        bool dialing = module.State == SwitchModuleState.Committed;
        float done = dialing ? Math.Max(0f, calibrationSeconds - module.CalibrationLeft) : 0f;
        return new PortalGateFeed(steady, dialing, done, calibrationSeconds, module.State == SwitchModuleState.Open);
    }

    /// <summary>
    /// The feed of a ring whose look today is <paramref name="look"/>, dialed by
    /// <paramref name="module"/> (null: none) on a board whose calibration takes
    /// <paramref name="calibrationSeconds"/>. Only a departure ring on an open
    /// route (Glow) takes its module's dial (<see cref="TakesDial"/>). A closed
    /// ring (under maintenance, or CLOSED today) stays dark whatever its module
    /// does (Saleh 2026-09-29: "hide a closed portal's halo"). The Return Gate
    /// keeps its steady warm event horizon and takes no dial. Without a board
    /// (<paramref name="board"/> false) a Glow ring holds its event horizon
    /// open. The cheat (<paramref name="allOpen"/>) opens every ring. Read only.
    /// </summary>
    public static PortalGateFeed ForRing(PortalLook look, SwitchModule module, float calibrationSeconds, bool board, bool allOpen)
    {
        bool steady = allOpen || look == PortalLook.ReturnGate || !board && look == PortalLook.Glow;
        return Of(TakesDial(look) ? module : null, calibrationSeconds, steady);
    }

    /// <summary>True when a ring whose look today is <paramref name="look"/> shows its module's dial and sends: a departure ring on an open route (Glow) only.</summary>
    public static bool TakesDial(PortalLook look) => look == PortalLook.Glow;

    /// <summary>The switchboard module that dials ring <paramref name="portal"/>: the same number (module 01 dials ring 01, the front); -1 for none.</summary>
    public static int ModuleOf(int portal) => portal >= 1 ? portal - 1 : -1;

    /// <summary>
    /// True when <paramref name="module"/> sent a group since its count was
    /// <paramref name="seen"/> (SwitchModule.Sent; a lock-in sets it back to 0),
    /// which is brought up to date. Read only.
    /// </summary>
    public static bool SentSince(SwitchModule module, ref int seen)
    {
        int now = module != null ? module.Sent : 0;
        bool sent = now > seen;
        seen = now;
        return sent;
    }

    /// <summary>True when the event horizon should show.</summary>
    public bool Horizon => Steady || Live;

    /// <summary>True when the chevrons should show.</summary>
    public bool Chevrons => Live || Dialing;
}

/// <summary>The gate's times (HallPortalsSO.gate, with HallLightingSO.portalFadeSeconds for the close). Runtime only.</summary>
public readonly struct PortalGateTimes
{
    /// <summary>How many chevrons a ring has (one lights per CalibrationSeconds / Chevrons).</summary>
    public readonly int Chevrons;

    /// <summary>Seconds the event horizon takes to open.</summary>
    public readonly float OpenSeconds;

    /// <summary>Seconds the event horizon and the chevrons take to fade out.</summary>
    public readonly float FadeSeconds;

    /// <summary>Seconds a kawoosh takes: its burst out, then its settle.</summary>
    public readonly float KawooshSeconds;

    /// <summary>Where the kawoosh peaks, as a share of its time.</summary>
    public readonly float KawooshPeak;

    /// <summary>The least seconds between two of the ring's changes (a chevron, the opening, a kawoosh): a queued kawoosh waits for it.</summary>
    public readonly float DwellSeconds;

    /// <summary>The times.</summary>
    public PortalGateTimes(int chevrons, float openSeconds, float fadeSeconds, float kawooshSeconds, float kawooshPeak, float dwellSeconds)
    {
        Chevrons = Math.Max(1, chevrons);
        OpenSeconds = Math.Max(0f, openSeconds);
        FadeSeconds = Math.Max(0f, fadeSeconds);
        KawooshSeconds = Math.Max(0.05f, kawooshSeconds);
        KawooshPeak = Math.Min(0.9f, Math.Max(0.1f, kawooshPeak));
        DwellSeconds = Math.Max(0f, dwellSeconds);
    }
}

/// <summary>
/// One hall ring as a gate (the night of 2026-11-07, DECISIONS D6; Saleh:
/// "portals", "a switchboard that is intresting ( my friends suggested
/// something based on stargate)", "we dont need art for travellers going to
/// the portal we just need to fake it"): the pure part of PortalEffect.
/// Offline it is a dark ring. Locked in, its chevrons light one by one over
/// the calibration, each in one step and held (<see cref="PortalGate.ChevronsLit"/>).
/// Open, the event horizon opens (an eased iris, no overshoot) and every
/// chevron stays lit. A send (<see cref="Send"/>) plays the kawoosh once the
/// horizon is open and the dwell since the ring's last change has passed
/// (<see cref="PortalGateTimes.DwellSeconds"/>: never two changes closer than
/// it). Closing, the horizon and the chevrons fade out over the fade time.
/// Reduced motion keeps steady states and cuts between them: the horizon
/// opens and closes at once and the kawoosh is a held bright state.
/// No UnityEngine, so it is tested headless (PortalGateTests).
/// </summary>
public sealed class PortalGateTrack
{
    private float _open;
    private float _pool;
    private float _chevrons;
    private bool _pending;
    private float _since = float.MaxValue;

    /// <summary>Where the ring is now.</summary>
    public PortalGateState State { get; private set; }

    /// <summary>How many chevrons are lit.</summary>
    public int Lit { get; private set; }

    /// <summary>The chevrons' opacity (0 to 1, eased: they cut in, and fade out with the close).</summary>
    public float Chevrons => PortalGate.Smooth(_chevrons);

    /// <summary>The event horizon's opacity (0 to 1, eased).</summary>
    public float Pool => PortalGate.Smooth(_pool);

    /// <summary>The event horizon's radius as a share of its hole (0 to 1, an ease-out with no overshoot; held at 1 while it fades).</summary>
    public float Iris => PortalGate.EaseOut(_open);

    /// <summary>The kawoosh's progress (0 to 1 of its time), or -1 when none plays.</summary>
    public float Burst { get; private set; } = -1f;

    /// <summary>True on the step a kawoosh started (its sound plays then).</summary>
    public bool BurstStarted { get; private set; }

    /// <summary>True while a send waits for its kawoosh.</summary>
    public bool Pending => _pending;

    /// <summary>A group goes through: the kawoosh plays once the event horizon is open and the dwell has passed (a close first drops it).</summary>
    public void Send() => _pending = true;

    /// <summary>Advances the ring by <paramref name="dt"/> seconds toward what <paramref name="feed"/> asks, on <paramref name="times"/>; with <paramref name="reduced"/> motion it cuts between steady states.</summary>
    public void Step(in PortalGateFeed feed, in PortalGateTimes times, float dt, bool reduced)
    {
        dt = Math.Max(0f, dt);
        BurstStarted = false;
        _since += dt;

        // The chevrons: they cut in at the lock-in, step up one at a time and hold; they fade out with the close.
        if (feed.Chevrons)
        {
            int lit = feed.Live ? times.Chevrons : PortalGate.ChevronsLit(feed.Calibrated, feed.CalibrationSeconds, times.Chevrons);
            if (_chevrons < 1f || lit != Lit)
                _since = 0f;
            _chevrons = 1f;
            Lit = lit;
        }
        else if (_chevrons > 0f)
        {
            _chevrons = PortalGate.Toward(_chevrons, 0f, dt, times.FadeSeconds, reduced);
            if (_chevrons <= 0f)
                Lit = 0;
        }

        // The event horizon: an eased iris and a quicker fade in; a fade out with the iris held.
        if (feed.Horizon)
        {
            if (_pool <= 0f && _open <= 0f)
                _since = 0f;
            _open = PortalGate.Toward(_open, 1f, dt, times.OpenSeconds, reduced);
            _pool = PortalGate.Toward(_pool, 1f, dt, times.OpenSeconds * 0.5f, reduced);
        }
        else if (_pool > 0f)
        {
            _pool = PortalGate.Toward(_pool, 0f, dt, times.FadeSeconds, reduced);
            if (_pool <= 0f)
                _open = 0f;
        }
        else
            _open = 0f;

        // The kawoosh: one at a time, on an open horizon, the dwell after the last change; a close drops a queued one.
        if (Burst >= 0f)
        {
            Burst += dt / times.KawooshSeconds;
            if (Burst >= 1f)
                Burst = -1f;
        }
        if (!feed.Horizon && _pool <= 0f)
            _pending = false;
        if (_pending && Burst < 0f && feed.Horizon && _open >= 1f && _since >= times.DwellSeconds)
        {
            _pending = false;
            Burst = 0f;
            BurstStarted = true;
            _since = 0f;
        }

        State = Burst >= 0f && feed.Horizon ? PortalGateState.Sending
            : feed.Horizon ? PortalGateState.Open
            : feed.Dialing ? PortalGateState.LockedIn
            : _pool > 0f || _chevrons > 0f ? PortalGateState.Closing
            : PortalGateState.Offline;
    }

    /// <summary>The kawoosh's envelope now (PortalGate.Kawoosh), 0 when none plays.</summary>
    public float Envelope(in PortalGateTimes times, bool reduced) => Burst < 0f ? 0f : PortalGate.Kawoosh(Burst, times.KawooshPeak, reduced);
}

/// <summary>The gate's curves (PortalGateTrack, PortalEffect and the shader TimeDesk/PortalGlow). Pure.</summary>
public static class PortalGate
{
    /// <summary>
    /// How many of <paramref name="chevrons"/> are lit after <paramref name="calibrated"/>
    /// of <paramref name="calibrationSeconds"/>: the first at the lock-in, then
    /// one more each calibrationSeconds / chevrons (20 / 7 s: the last at 17.1 s,
    /// the route live at 20 s, each change one step apart); all of them once
    /// the calibration is done, or with no calibration time.
    /// </summary>
    public static int ChevronsLit(float calibrated, float calibrationSeconds, int chevrons)
    {
        chevrons = Math.Max(1, chevrons);
        if (calibrationSeconds <= 0f || float.IsNaN(calibrated))
            return chevrons;
        double step = calibrationSeconds / (double)chevrons;
        // A hair of slack so a calibration counted in float frames lights its chevron on the step it reaches.
        int lit = 1 + (int)Math.Floor(Math.Max(0.0, calibrated) / step + 1e-4);
        return Math.Min(chevrons, Math.Max(1, lit));
    }

    /// <summary>
    /// The kawoosh's envelope at <paramref name="progress"/> (0 to 1 of its time):
    /// 0 at both ends, 1 at <paramref name="peak"/>, a smoothstep up (the burst
    /// out) and a smoothstep down (the settle), so it never moves more than
    /// 1.5 / (frames to the peak) in a frame; with <paramref name="reduced"/>
    /// motion a held state: 1 until its time is up, then 0.
    /// </summary>
    public static float Kawoosh(float progress, float peak, bool reduced)
    {
        if (!(progress > 0f) || progress >= 1f)
            return 0f;
        if (reduced)
            return 1f;
        peak = Math.Min(0.9f, Math.Max(0.1f, peak));
        return progress <= peak ? Smooth(progress / peak) : Smooth((1f - progress) / (1f - peak));
    }

    /// <summary>The kawoosh plume's reach from the core at <paramref name="progress"/> (opening radii): from <paramref name="from"/> out to <paramref name="to"/> at a steady speed by the peak (the burst out; its envelope eases it in), then held while it settles.</summary>
    public static float Front(float progress, float peak, float from, float to)
    {
        peak = Math.Min(0.9f, Math.Max(0.1f, peak));
        return from + (to - from) * Clamp01(progress / peak);
    }

    /// <summary>A value moved from <paramref name="current"/> toward <paramref name="target"/> by <paramref name="dt"/> / <paramref name="seconds"/>; at once with <paramref name="reduced"/> motion or no time.</summary>
    public static float Toward(float current, float target, float dt, float seconds, bool reduced)
    {
        if (reduced || seconds <= 0f)
            return target;
        float step = Math.Max(0f, dt) / seconds;
        return current < target ? Math.Min(target, current + step) : Math.Max(target, current - step);
    }

    /// <summary>The smoothstep of <paramref name="t"/> (clamped to 0 to 1): its steepest slope is 1.5.</summary>
    public static float Smooth(float t)
    {
        t = Clamp01(t);
        return t * t * (3f - 2f * t);
    }

    /// <summary>A cubic ease-out of <paramref name="t"/> (clamped to 0 to 1): fast, then settling at 1 with no overshoot.</summary>
    public static float EaseOut(float t)
    {
        float u = 1f - Clamp01(t);
        return 1f - u * u * u;
    }

    /// <summary>
    /// The angle (degrees, 0 to the right, 90 up) of chevron <paramref name="index"/>
    /// (0 lights first) on a ring whose sites are <paramref name="step"/> degrees
    /// apart, in the order of the Stargate's dial: three down the right side, three
    /// down the left side, the top one last (90 - step, 90 - 2 step, 90 - 3 step,
    /// 90 + step, 90 + 2 step, 90 + 3 step, 90); a ring of more chevrons goes on
    /// down each side in turn.
    /// </summary>
    public static float ChevronAngle(int index, int chevrons, float step)
    {
        chevrons = Math.Max(1, chevrons);
        index = Math.Min(Math.Max(0, index), chevrons - 1);
        if (index == chevrons - 1)
            return 90f;
        int sides = chevrons - 1, right = (sides + 1) / 2;
        return index < right ? 90f - step * (index + 1) : 90f + step * (index - right + 1);
    }

    private static float Clamp01(float x) => x < 0f ? 0f : x > 1f ? 1f : float.IsNaN(x) ? 0f : x;
}
