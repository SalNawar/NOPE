using System;

/// <summary>
/// The Helix River's tuning (HelixRiverSO holds one; Saleh edits it in the
/// Inspector, Generate World never touches it). Lengths are in the river's
/// reference pixels: its screen is <see cref="HelixRiver.ScreenHeight"/> tall
/// (the reference sheet's 900 x 380 glass), whatever size it is drawn at.
/// "Calm" is the shown stability read from the firing line (0) to
/// <see cref="steadyAt"/> (1).
/// </summary>
[Serializable]
public sealed class HelixRiverKnobs
{
    /// <summary>The stability at and above which the river is fully calm (the top of the scale).</summary>
    public float steadyAt = 100f;

    /// <summary>How long the shown river takes to follow a change of stability (seconds to close about two thirds of the gap): it never pops.</summary>
    public float settleSeconds = 1.2f;

    /// <summary>The centre line's meander when calm and when collapsing (reference px either side of the middle).</summary>
    public float meanderCalm = 6f, meanderWild = 118f;

    /// <summary>The meander's curve: above 1, the first losses bend the river less than the last ones.</summary>
    public float meanderCurve = 1.1f;

    /// <summary>The helix's radius (each strand's distance from the centre line) when calm and when collapsing (reference px).</summary>
    public float twistCalm = 26f, twistWild = 40f;

    /// <summary>How far the leading ("now", right) end's strands pull apart when collapsing (reference px), and the curve of that growth.</summary>
    public float unzipWild = 70f, unzipCurve = 1.2f;

    /// <summary>Where the unzip starts across the screen (share of its width) when collapsing and how much further right it starts when calm.</summary>
    public float unzipStartWild = 0.62f, unzipStartCalmShift = 0.35f;

    /// <summary>The share of rungs snapped when collapsing, and the curve of its growth.</summary>
    public float snapWild = 0.5f, snapCurve = 1.4f;

    /// <summary>The share of the snapped rungs that mutate red when collapsing.</summary>
    public float mutateWild = 0.5f;

    /// <summary>The calm under which oxbows (loops pinched off the helix) appear, and how many show when collapsing.</summary>
    public float oxbowsFrom = 0.75f, oxbowsWild = 7f;

    /// <summary>The calm under which the leading end frays, and how many loose fibres show when collapsing.</summary>
    public float frayFrom = 0.5f, frayWild = 24f;

    /// <summary>The speed the bright motes ride downstream when calm and when collapsing (reference px a second).</summary>
    public float flowCalm = 90f, flowWild = 150f;

    /// <summary>How far the motes stray from their strand when collapsing (reference px).</summary>
    public float strayWild = 30f;

    /// <summary>How much the amber strand reddens when collapsing (0 to 1).</summary>
    public float driftWild = 0.5f;

    /// <summary>
    /// The CRT's glitch on a drop of stability (HALL-FX, BUG-54: "one readable
    /// glitch on a stability drop, then it settles"): one burst this strong
    /// (band shifts that kink once and slide back, a chroma split for its first
    /// frames), fading out over <see cref="glitchSeconds"/>; then the glass is
    /// calm again, whatever the day's losses.
    /// </summary>
    public float glitchWild = 1f;

    /// <summary>How long a drop's glitch burst lasts (seconds; the feel bible's J4: 200 ms).</summary>
    public float glitchSeconds = 0.2f;

    /// <summary>How long the warning state takes to come on or go off (seconds to close about two thirds of the way): calm, never a strobe.</summary>
    public float warningSettleSeconds = 0.8f;

    /// <summary>How far (points) the shown stability must climb back above the warning line before the warning goes off, so it never flickers on the line.</summary>
    public float warningHysteresis = 0.5f;

    /// <summary>The fastest any of the river's details may cross the display (screen pixels a frame at <see cref="captureFps"/>): faster, a thin line hops pixels and strobes (BUG-54); a small display slows the river until it holds.</summary>
    public float maxPixelsPerFrame = 0.5f;

    /// <summary>The frame rate the speed cap is measured at (the QA captures' 30 fps; a faster game only moves less a frame).</summary>
    public float captureFps = 30f;

    /// <summary>A drop of this many points today (the shift's stability change) makes the river fully agitated: faster motes, on top of the damage.</summary>
    public float agitationFullDrop = 7.5f;

    /// <summary>The share of extra mote speed a fully agitated day adds.</summary>
    public float agitationFlow = 0.4f;

    /// <summary>How long a citation's red pulse takes to run the river's length (seconds).</summary>
    public float pulseSeconds = 1.2f;

    /// <summary>The river's speed under Reduced Motion (a share of full speed; 0 freezes it): the shape and the damage still show.</summary>
    public float reducedMotionSpeed = 0.1f;
}

/// <summary>What the river reads each frame (the hidden stability, today's change, the firing and warning lines, the player's motion choice, the display's size and a held warning).</summary>
public readonly struct HelixRiverInput
{
    /// <summary>Timeline stability now (0 to 100; never shown).</summary>
    public readonly float Stability;

    /// <summary>The firing line (GameConfigSO.firedAtStability): the river is collapsing at or under it.</summary>
    public readonly float FiredAt;

    /// <summary>The warning margin above the firing line (GameConfigSO.stabilityWarningMargin): the river's state is Breaching under it (HallSlotPick reads the tier).</summary>
    public readonly float WarningMargin;

    /// <summary>The critical margin above the firing line (GameConfigSO.stabilityCriticalMargin): the warning state shows under it, near the destruction point (D12).</summary>
    public readonly float CriticalMargin;

    /// <summary>Stability's change today (the shift ledger's total; negative is a loss).</summary>
    public readonly float TodayChange;

    /// <summary>True when the player chose Reduced Motion.</summary>
    public readonly bool ReducedMotion;

    /// <summary>How many of the river's reference pixels one screen pixel of the display covers (its glass's reference height over its height on screen); 0 or less when unknown (no speed cap).</summary>
    public readonly float ScreenScale;

    /// <summary>True while a caller holds the warning on (HelixRiverMonitor.SetWarning), whatever the stability.</summary>
    public readonly bool WarningHeld;

    /// <summary>A frame's reading.</summary>
    public HelixRiverInput(float stability, float firedAt, float warningMargin, float criticalMargin, float todayChange, bool reducedMotion,
                           float screenScale = 0f, bool warningHeld = false)
    {
        Stability = stability;
        FiredAt = firedAt;
        WarningMargin = warningMargin;
        CriticalMargin = criticalMargin;
        TodayChange = todayChange;
        ReducedMotion = reducedMotion;
        ScreenScale = screenScale;
        WarningHeld = warningHeld;
    }
}

/// <summary>The Helix River's state in words (HelixRiver.Tier; the reference sheet's four panels): what the hall's art reacts to, never shown as a word or a number.</summary>
public enum StabilityTier
{
    /// <summary>Calm: no oxbow has pinched off.</summary>
    Steady,

    /// <summary>Oxbows pinch off (the calm under HelixRiverKnobs.oxbowsFrom), above the warning line.</summary>
    Strained,

    /// <summary>From the warning line (GameConfigSO.stabilityWarningMargin above the firing line).</summary>
    Breaching,

    /// <summary>The critical band (GameConfigSO.stabilityCriticalMargin above the firing line, or under it): the monitor shows its warning state.</summary>
    Collapsing
}

/// <summary>One frame of the river as the shader draws it (HelixRiverMonitor hands these to the material).</summary>
public struct HelixRiverFrame
{
    /// <summary>The shown calm: 1 steady, 0 at the firing line (smoothed; never a number on screen).</summary>
    public float Calm;

    /// <summary>The river's clock (seconds; slowed on a small display so no detail outruns the speed cap, slowed or frozen under Reduced Motion).</summary>
    public float Time;

    /// <summary>The centre line's meander (reference px).</summary>
    public float Meander;

    /// <summary>The helix's radius (reference px).</summary>
    public float Twist;

    /// <summary>How far the leading end's strands pull apart (reference px).</summary>
    public float Unzip;

    /// <summary>Where the unzip starts (share of the screen's width).</summary>
    public float UnzipStart;

    /// <summary>The share of rungs snapped.</summary>
    public float Snap;

    /// <summary>The share of snapped rungs that mutate red.</summary>
    public float Mutate;

    /// <summary>How many oxbows show (fractional: the last one fades in).</summary>
    public float Oxbows;

    /// <summary>How many loose fibres fray the leading end (fractional).</summary>
    public float Fray;

    /// <summary>The motes' speed downstream (reference px a second, before the speed cap).</summary>
    public float Flow;

    /// <summary>How far the motes have ridden downstream (reference px): their speed capped and integrated, so a change of speed never jumps them.</summary>
    public float MoteTravel;

    /// <summary>How far the motes stray from their strand (reference px).</summary>
    public float Stray;

    /// <summary>How much the amber strand has reddened (0 to 1).</summary>
    public float Drift;

    /// <summary>The CRT's glitch (0 to 1): one burst on a drop of stability, back to 0 within HelixRiverKnobs.glitchSeconds; never under Reduced Motion.</summary>
    public float Glitch;

    /// <summary>Which glitch burst this is (counts the drops): the shader slips the same bands for a whole burst, so it kinks once.</summary>
    public float GlitchSeed;

    /// <summary>The warning state (0 off to 1 on, eased; HALL-FX, BUG-56e): near the destruction point, or while a caller holds it.</summary>
    public float Warning;

    /// <summary>A citation's pulse along the river: its head's place (0 at the source, 1 past the leading end), or -1 when none runs.</summary>
    public float Pulse;

    /// <summary>A citation's glow under Reduced Motion (the whole river reddens and fades instead of a running pulse; 0 when none).</summary>
    public float PulseGlow;
}

/// <summary>
/// The Helix River (stability without a number; Saleh 2026-10-07: "I want
/// something animated like a dna helix ... that you see degrade and change
/// with time but you dont really see a value for"): maps the hidden
/// stability, today's change and citations to the river's look, smoothly.
/// As stability falls towards the firing line the centre line meanders, the
/// helix widens past its banks, rungs snap and mutate red, oxbows pinch off
/// and the leading end unzips and frays. Calm, readable motion (HALL-FX,
/// BUG-54): a drop of stability glitches the CRT once, then it settles; in the
/// critical band, near the destruction point, the monitor eases into a steady
/// warning state (BUG-56e; D12: "you are fired if you destroy the timeline");
/// and no detail crosses the display faster than half a pixel a frame (the
/// river's clock slows on a small display). A citation sends a red pulse
/// downstream. The shown calm follows stability with a time constant, so a
/// change never pops; the first frame starts where stability stands. Pure,
/// so every rule is tested headless (HelixRiverTests); the drawing is the
/// TimeDesk/HelixRiver shader's.
/// </summary>
public sealed class HelixRiver
{
    /// <summary>The river's reference screen height (px): every length in the knobs is in these pixels.</summary>
    public const float ScreenHeight = 380f;

    /// <summary>The fastest detail on the river's clock (reference px a second, in HelixRiver.shader): the current streaks (up to 80), the rungs and the twist (70.4). The speed cap slows the clock by it.</summary>
    public const float FastestOnClock = 80f;

    /// <summary>The least fall of the hidden stability that counts as a drop (points; it is kept in hundredths).</summary>
    private const float DropEpsilon = 0.005f;

    private readonly HelixRiverKnobs _knobs;
    private bool _started;
    private float _shownStability;
    private float _shownAgitation;
    private float _lastStability;
    private float _time;
    private float _moteTravel;
    private float _pulseAge = float.PositiveInfinity;
    private float _glitchAge = float.PositiveInfinity;
    private int _glitches;
    private bool _warned;
    private float _warning;

    /// <summary>A river on <paramref name="knobs"/> (null: the defaults).</summary>
    public HelixRiver(HelixRiverKnobs knobs) => _knobs = knobs ?? new HelixRiverKnobs();

    /// <summary>A citation was issued: a red pulse starts at the river's source (a running pulse restarts).</summary>
    public void Citation() => _pulseAge = 0f;

    /// <summary>
    /// Advances the river by <paramref name="deltaSeconds"/> towards
    /// <paramref name="input"/> and returns the frame to draw. The first step
    /// starts at the input's stability (no transition from a default).
    /// </summary>
    public HelixRiverFrame Step(float deltaSeconds, in HelixRiverInput input)
    {
        float dt = Math.Max(0f, Finite(deltaSeconds));
        float stability = Finite(input.Stability);
        float agitation = Clamp01(-Finite(input.TodayChange) / Math.Max(0.0001f, _knobs.agitationFullDrop));
        bool first = !_started;
        if (first)
        {
            _shownStability = stability;
            _shownAgitation = agitation;
            _started = true;
        }
        else
        {
            float follow = _knobs.settleSeconds > 0f ? 1f - (float)Math.Exp(-dt / _knobs.settleSeconds) : 1f;
            _shownStability += (stability - _shownStability) * follow;
            _shownAgitation += (agitation - _shownAgitation) * follow;
            // A drop of the hidden stability starts one glitch burst (a new drop restarts it).
            if (stability < _lastStability - DropEpsilon)
            {
                _glitchAge = 0f;
                _glitches++;
            }
        }
        _lastStability = stability;

        HelixRiverFrame frame = Shape(Calm(_shownStability, input.FiredAt), _knobs);
        frame.Flow *= 1f + _shownAgitation * _knobs.agitationFlow;

        // The clock: slowed so no detail outruns the speed cap on this display, and by Reduced Motion.
        float motion = input.ReducedMotion ? Math.Max(0f, _knobs.reducedMotionSpeed) : 1f;
        float cap = SpeedCap(input.ScreenScale, _knobs);
        _time += dt * motion * Math.Min(1f, cap / FastestOnClock);
        _moteTravel += dt * motion * Math.Min(frame.Flow, cap);
        frame.Time = _time;
        frame.MoteTravel = _moteTravel;

        if (!float.IsPositiveInfinity(_pulseAge))
            _pulseAge += dt;
        if (!float.IsPositiveInfinity(_glitchAge))
            _glitchAge += dt;
        float burst = _knobs.glitchSeconds > 0f ? _glitchAge / _knobs.glitchSeconds : float.PositiveInfinity;
        frame.Glitch = !input.ReducedMotion && burst < 1f ? Clamp01(_knobs.glitchWild * (1f - burst) * (1f - burst)) : 0f;
        frame.GlitchSeed = _glitches;

        // The warning: in the critical band (near the destruction point), off only once the shown stability climbs back past it, or held.
        float line = input.FiredAt + Math.Max(0f, input.CriticalMargin);
        _warned = _warned ? _shownStability <= line + Math.Max(0f, _knobs.warningHysteresis) : _shownStability <= line;
        float target = _warned || input.WarningHeld ? 1f : 0f;
        _warning = first || _knobs.warningSettleSeconds <= 0f ? target : _warning + (target - _warning) * (1f - (float)Math.Exp(-dt / _knobs.warningSettleSeconds));
        frame.Warning = _warning;

        float pulse = _knobs.pulseSeconds > 0f ? _pulseAge / _knobs.pulseSeconds : float.PositiveInfinity;
        bool pulsing = pulse < 1f;
        frame.Pulse = pulsing && !input.ReducedMotion ? pulse : -1f;
        frame.PulseGlow = pulsing && input.ReducedMotion ? 1f - pulse : 0f;
        return frame;
    }

    /// <summary>The calm of <paramref name="stability"/>: 0 at or under the firing line, 1 at or above HelixRiverKnobs.steadyAt, linear between.</summary>
    public float Calm(float stability, float firedAt) => CalmOf(stability, firedAt, _knobs);

    /// <summary>
    /// The fastest a detail may cross a display of <paramref name="screenScale"/>
    /// reference px per screen px (reference px a second): the knobs' pixels a
    /// frame at their capture rate; unbounded when the display's size is unknown.
    /// </summary>
    public static float SpeedCap(float screenScale, HelixRiverKnobs knobs)
    {
        knobs = knobs ?? new HelixRiverKnobs();
        if (!(screenScale > 0f) || float.IsInfinity(screenScale))
            return float.PositiveInfinity;
        return Math.Max(0f, knobs.maxPixelsPerFrame) * Math.Max(1f, knobs.captureFps) * screenScale;
    }

    /// <summary>The calm of <paramref name="stability"/> on <paramref name="knobs"/> (null: the defaults): 0 at or under the firing line, 1 at or above HelixRiverKnobs.steadyAt, linear between.</summary>
    public static float CalmOf(float stability, float firedAt, HelixRiverKnobs knobs)
    {
        float span = (knobs ?? new HelixRiverKnobs()).steadyAt - firedAt;
        if (span <= 0f)
            return Finite(stability) > firedAt ? 1f : 0f;
        return Clamp01((Finite(stability) - firedAt) / span);
    }

    /// <summary>
    /// The river's state in words, for the systems that pick art by it (the
    /// hall's slots, HallSlotPick; never shown to the player): Collapsing in
    /// the critical band (where the monitor shows its warning), Breaching from
    /// the warning line, Strained once the calm falls
    /// under HelixRiverKnobs.oxbowsFrom (where oxbows pinch off), else Steady.
    /// The river's own thresholds, so the hall and the river always agree.
    /// </summary>
    public static StabilityTier Tier(float stability, float firedAt, float warningMargin, float criticalMargin, HelixRiverKnobs knobs)
    {
        knobs = knobs ?? new HelixRiverKnobs();
        switch (StabilityRules.Band(Finite(stability), firedAt, warningMargin, criticalMargin))
        {
            case StabilityBand.Critical:
                return StabilityTier.Collapsing;
            case StabilityBand.Warning:
                return StabilityTier.Breaching;
            default:
                return CalmOf(stability, firedAt, knobs) < knobs.oxbowsFrom ? StabilityTier.Strained : StabilityTier.Steady;
        }
    }

    /// <summary>The river's shape at <paramref name="calm"/> (every damage grows as calm falls; no clock, pulse or glitch).</summary>
    public static HelixRiverFrame Shape(float calm, HelixRiverKnobs knobs)
    {
        knobs = knobs ?? new HelixRiverKnobs();
        float c = Clamp01(calm), wild = 1f - c;
        return new HelixRiverFrame
        {
            Calm = c,
            Meander = Lerp(knobs.meanderCalm, knobs.meanderWild, Pow(wild, knobs.meanderCurve)),
            Twist = Lerp(knobs.twistCalm, knobs.twistWild, wild),
            Unzip = knobs.unzipWild * Pow(wild, knobs.unzipCurve),
            UnzipStart = knobs.unzipStartWild + knobs.unzipStartCalmShift * c,
            Snap = knobs.snapWild * Pow(wild, knobs.snapCurve),
            Mutate = knobs.mutateWild * wild,
            Oxbows = knobs.oxbowsFrom > 0f ? knobs.oxbowsWild * Clamp01((knobs.oxbowsFrom - c) / knobs.oxbowsFrom) : 0f,
            Fray = knobs.frayFrom > 0f ? knobs.frayWild * Clamp01((knobs.frayFrom - c) / knobs.frayFrom) : 0f,
            Flow = Lerp(knobs.flowCalm, knobs.flowWild, wild),
            Stray = knobs.strayWild * wild,
            Drift = knobs.driftWild * wild,
            Pulse = -1f,
        };
    }

    private static float Finite(float v) => float.IsNaN(v) || float.IsInfinity(v) ? 0f : v;

    private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;

    private static float Lerp(float a, float b, float t) => a + (b - a) * t;

    private static float Pow(float v, float p) => (float)Math.Pow(Math.Max(0f, v), Math.Max(0.0001f, p));
}
