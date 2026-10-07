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
    /// The CRT's glitch (band shifts, chroma split) grows from nothing at the
    /// warning line (GameConfigSO.stabilityWarningMargin above the firing line)
    /// to this much at the firing line; the screen flickers in the critical
    /// band (StabilityRules.Band).
    /// </summary>
    public float glitchWild = 1f;

    /// <summary>A drop of this many points today (the shift's stability change) makes the river fully agitated: faster motes and a little glitch, on top of the damage.</summary>
    public float agitationFullDrop = 7.5f;

    /// <summary>The glitch a fully agitated day adds.</summary>
    public float agitationGlitch = 0.25f;

    /// <summary>The share of extra mote speed a fully agitated day adds.</summary>
    public float agitationFlow = 0.4f;

    /// <summary>How long a citation's red pulse takes to run the river's length (seconds).</summary>
    public float pulseSeconds = 1.2f;

    /// <summary>The river's speed under Reduced Motion (a share of full speed; 0 freezes it): the shape and the damage still show.</summary>
    public float reducedMotionSpeed = 0.1f;
}

/// <summary>What the river reads each frame (the hidden stability, today's change, the firing and warning lines, the player's motion choice).</summary>
public readonly struct HelixRiverInput
{
    /// <summary>Timeline stability now (0 to 100; never shown).</summary>
    public readonly float Stability;

    /// <summary>The firing line (GameConfigSO.firedAtStability): the river is collapsing at or under it.</summary>
    public readonly float FiredAt;

    /// <summary>The warning margin above the firing line (GameConfigSO.stabilityWarningMargin): the CRT starts glitching there.</summary>
    public readonly float WarningMargin;

    /// <summary>The critical margin above the firing line (GameConfigSO.stabilityCriticalMargin): the screen flickers there.</summary>
    public readonly float CriticalMargin;

    /// <summary>Stability's change today (the shift ledger's total; negative is a loss).</summary>
    public readonly float TodayChange;

    /// <summary>True when the player chose Reduced Motion.</summary>
    public readonly bool ReducedMotion;

    /// <summary>A frame's reading.</summary>
    public HelixRiverInput(float stability, float firedAt, float warningMargin, float criticalMargin, float todayChange, bool reducedMotion)
    {
        Stability = stability;
        FiredAt = firedAt;
        WarningMargin = warningMargin;
        CriticalMargin = criticalMargin;
        TodayChange = todayChange;
        ReducedMotion = reducedMotion;
    }
}

/// <summary>One frame of the river as the shader draws it (HelixRiverMonitor hands these to the material).</summary>
public struct HelixRiverFrame
{
    /// <summary>The shown calm: 1 steady, 0 at the firing line (smoothed; never a number on screen).</summary>
    public float Calm;

    /// <summary>The river's clock (seconds; slowed or frozen under Reduced Motion).</summary>
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

    /// <summary>The motes' speed downstream (reference px a second).</summary>
    public float Flow;

    /// <summary>How far the motes stray from their strand (reference px).</summary>
    public float Stray;

    /// <summary>How much the amber strand has reddened (0 to 1).</summary>
    public float Drift;

    /// <summary>The CRT's glitch (0 to 1).</summary>
    public float Glitch;

    /// <summary>The CRT's flicker (0 or 1; never under Reduced Motion).</summary>
    public float Flicker;

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
/// helix widens past its banks, rungs snap and mutate red, oxbows pinch off,
/// the leading end unzips and frays, and from the warning line the CRT
/// glitches (flickering in the critical band). A citation sends a red pulse
/// downstream. The shown calm follows stability with a time constant, so a
/// change never pops; the first frame starts where stability stands. Pure,
/// so every rule is tested headless (HelixRiverTests); the drawing is the
/// TimeDesk/HelixRiver shader's.
/// </summary>
public sealed class HelixRiver
{
    /// <summary>The river's reference screen height (px): every length in the knobs is in these pixels.</summary>
    public const float ScreenHeight = 380f;

    private readonly HelixRiverKnobs _knobs;
    private bool _started;
    private float _shownStability;
    private float _shownAgitation;
    private float _time;
    private float _pulseAge = float.PositiveInfinity;

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
        if (!_started)
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
        }

        _time += dt * (input.ReducedMotion ? Math.Max(0f, _knobs.reducedMotionSpeed) : 1f);
        if (!float.IsPositiveInfinity(_pulseAge))
            _pulseAge += dt;

        HelixRiverFrame frame = Shape(Calm(_shownStability, input.FiredAt), _knobs);
        frame.Time = _time;
        float warningLine = input.FiredAt + Math.Max(0f, input.WarningMargin);
        float warned = warningLine > input.FiredAt ? Clamp01((warningLine - _shownStability) / (warningLine - input.FiredAt)) : (_shownStability <= input.FiredAt ? 1f : 0f);
        frame.Glitch = Clamp01(warned * _knobs.glitchWild + _shownAgitation * _knobs.agitationGlitch);
        frame.Flow *= 1f + _shownAgitation * _knobs.agitationFlow;
        frame.Flicker = !input.ReducedMotion && StabilityRules.Band(_shownStability, input.FiredAt, input.WarningMargin, input.CriticalMargin) == StabilityBand.Critical ? 1f : 0f;

        float pulse = _knobs.pulseSeconds > 0f ? _pulseAge / _knobs.pulseSeconds : float.PositiveInfinity;
        bool pulsing = pulse < 1f;
        frame.Pulse = pulsing && !input.ReducedMotion ? pulse : -1f;
        frame.PulseGlow = pulsing && input.ReducedMotion ? 1f - pulse : 0f;
        return frame;
    }

    /// <summary>The calm of <paramref name="stability"/>: 0 at or under the firing line, 1 at or above HelixRiverKnobs.steadyAt, linear between.</summary>
    public float Calm(float stability, float firedAt)
    {
        float span = _knobs.steadyAt - firedAt;
        if (span <= 0f)
            return Finite(stability) > firedAt ? 1f : 0f;
        return Clamp01((Finite(stability) - firedAt) / span);
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
