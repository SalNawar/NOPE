using System;

/// <summary>
/// The physical feel of a thing moved about the desk (Saleh 2026-10-17: "KEEP
/// IT TOP DOWN BUT GIVE IT PHYSICS"; his 1018a playtest: "all documents are
/// stiff"): a light spring body on top of where the desk puts it (the desk
/// decides the place, the size and the machines' docks; this only draws the
/// way there). Held and dragged, it is alive: it swings, turning against its
/// sideways travel with an overshoot (its corners lag; the desk pivots the
/// swing on the held point, so the point under the cursor never moves:
/// <see cref="PivotOffset"/>), it bows along its travel by its speed, the
/// trailing end drooping most, behind the hand on a loose spring, its corners
/// twist as it swings, and a fast move sets a ripple running back along it
/// (the flutter). Let go, it stays exactly where it was released (DOC-HANDLE,
/// BUG-6, BUG-11: no slide on, no spin, no squash; Papers, Please's honesty):
/// its swing springs back on the held point, it hops a little and its bow
/// settles through a soft wobble (MotionKnobs.paperWobble: its ends curl up
/// as it lands, then droop, then lie flat), all scaled by how fast it was let
/// go (<see cref="SettleShare"/>: a slow drop barely settles, a fast one
/// settles visibly but briefly). A <see cref="Rigid"/> body (an ID card,
/// Saleh's D3: "documents other than solid id cards should feel a bit like
/// paper") never bows, twists or ripples and hops at most
/// MotionKnobs.cardDropHop. A heavier body (the rulebook folder:
/// <see cref="Weight"/>) swings and bends less. A refusal shakes it in place
/// (<see cref="Shake"/>: side to side, a few cycles, never flying back). Every
/// value is in the body's own frame on the desk: x right, z away (metres),
/// the turn about the desk's up (degrees). Stepped by Spring (frame-rate
/// independent); still under Reduced Motion (Share 0: everything rests at
/// once). What the paper's look makes of the bow, the twist and the ripple is
/// PaperBend's.
/// </summary>
public sealed class PaperPhysics
{
    /// <summary>How heavy the body is (1 a paper): its swing and bend divide by it.</summary>
    public float Weight { get; }

    /// <summary>True for a rigid body (an ID card): no bow, twist or ripple, a hop of at most MotionKnobs.cardDropHop.</summary>
    public bool Rigid { get; set; }

    private Spring _yaw, _bow, _twist, _flutter;
    private float _velX, _velZ;
    private float _lastX, _lastZ;
    private bool _placed, _wasHeld, _settling;
    private float _releaseX, _releaseZ;
    private float _shakeAmplitude, _shakeSeconds, _shakeElapsed;
    private int _shakeCycles;

    /// <summary>A body of <paramref name="weight"/> (1 a paper, more for the folder), at rest.</summary>
    public PaperPhysics(float weight = 1f) => Weight = weight > 0.1f ? weight : 0.1f;

    /// <summary>Where it is drawn from its place on the desk along its own x (metres): a refusal's shake in place, 0 at rest.</summary>
    public float ShakeX { get; private set; }

    /// <summary>Its swing about the desk's up while held (degrees), back to 0 at rest.</summary>
    public float Yaw => _yaw.Value;

    /// <summary>Its bow along its travel (metres at its ends: positive, they droop away from the viewer; negative, they curl up as it lands), and the way it travels (a unit vector in its frame) it bows along.</summary>
    public float Bow => _bow.Value;

    /// <inheritdoc cref="Bow"/>
    public float BowDirX { get; private set; } = 1f;

    /// <inheritdoc cref="Bow"/>
    public float BowDirZ { get; private set; }

    /// <summary>Its corners' twist (metres at the corners) as it swings.</summary>
    public float Twist => _twist.Value;

    /// <summary>The ripple's height at its trailing edge (metres) and its phase (radians, growing as it runs).</summary>
    public float Flutter => _flutter.Value;

    /// <inheritdoc cref="Flutter"/>
    public float FlutterPhase { get; private set; }

    /// <summary>Its travel's speed now (metres per second, smoothed), in its frame.</summary>
    public float SpeedX => _velX;

    /// <inheritdoc cref="SpeedX"/>
    public float SpeedZ => _velZ;

    /// <summary>How fast it was last let go (metres per second): what its settle was scaled by.</summary>
    public float ReleaseSpeed { get; private set; }

    /// <summary>True while any of its motion still runs.</summary>
    public bool Moving => _shakeSeconds > 0f || !(_yaw.AtRest && _bow.AtRest && _twist.AtRest && _flutter.AtRest);

    /// <summary>
    /// One frame of <paramref name="dt"/> seconds: the body's place now
    /// (<paramref name="x"/>, <paramref name="z"/>, metres, in its frame) and
    /// whether it is <paramref name="held"/>. Returns how high the drop makes
    /// it hop (metres, 0 but on the frame it is let go: the caller kicks its
    /// lift spring by it), scaled by how fast it was let go.
    /// </summary>
    public float Step(float dt, float x, float z, bool held, MotionKnobs knobs, MotionAmount amount)
    {
        float hop = 0f;
        float share = amount.Share;
        if (!_placed)
        {
            _placed = true;
            _lastX = x;
            _lastZ = z;
            _wasHeld = held;
        }
        if (dt > 0f)
        {
            // the travel's speed, smoothed over a few frames (the pointer's steps are uneven)
            float k = Math.Min(1f, dt / Math.Max(1e-4f, knobs.paperSpeedSmoothing));
            _velX += ((x - _lastX) / dt - _velX) * k;
            _velZ += ((z - _lastZ) / dt - _velZ) * k;
            if (!held)
                _velX = _velZ = 0f; // the desk's own moves (a slide, a dock) carry no momentum of their own
        }
        _lastX = x;
        _lastZ = z;

        if (_wasHeld && !held)
        {
            // let go: it stays where it was let go; it hops and its ends curl up as it lands, as much as it was moving
            ReleaseSpeed = (float)Math.Sqrt(_releaseX * _releaseX + _releaseZ * _releaseZ);
            hop = Land(share * SettleShare(ReleaseSpeed, knobs.paperSettleSpeed), knobs);
        }
        _wasHeld = held;
        if (held)
        {
            _releaseX = _velX;
            _releaseZ = _velZ;
            _settling = false;
        }

        float speed = (float)Math.Sqrt(_velX * _velX + _velZ * _velZ);
        bool bends = !Rigid && share > 0f;
        if (held && share > 0f)
        {
            _yaw.Target = Sway(_velX, _velZ, knobs.paperSwayPerSpeed * share / Weight, knobs.paperSwayMax * share);
            _bow.Target = bends ? Math.Min(speed * knobs.paperBowPerSpeed * share, knobs.paperBowMax * share) / Weight : 0f;
            _flutter.Target = bends ? FlutterHeight(speed, knobs.paperFlutterFrom, knobs.paperFlutterPerSpeed * share, knobs.paperFlutterMax * share) / Weight : 0f;
            if (speed > 1e-3f)
            {
                BowDirX = _velX / speed;
                BowDirZ = _velZ / speed;
            }
        }
        else
            _yaw.Target = _bow.Target = _flutter.Target = 0f;
        // the corners flap as it swings: the twist follows the swing's speed (degrees per second), against it
        _twist.Target = bends ? Clamp(-_yaw.Velocity * knobs.paperTwistPerSwing * share / Weight, knobs.paperTwistMax * share) : 0f;

        StepShake(dt, share);
        if (share <= 0f)
        {
            _yaw.Snap(0f);
            _bow.Snap(0f);
            _twist.Snap(0f);
            _flutter.Snap(0f);
            FlutterPhase = 0f;
            _settling = false;
            return 0f;
        }
        if (Rigid)
        {
            _bow.Snap(0f);
            _twist.Snap(0f);
            _flutter.Snap(0f);
        }
        _yaw.Step(dt, knobs.paperSwing, knobs.settleValue * 10f, knobs.settleSpeed * 10f);
        _bow.Step(dt, _settling ? knobs.paperWobble : knobs.paperBend, 2e-5f, 2e-4f);
        _twist.Step(dt, knobs.paperBend, 2e-5f, 2e-4f);
        _flutter.Step(dt, knobs.paperBend, 2e-5f, 2e-4f);
        if (_flutter.AtRest && _flutter.Value == 0f)
            FlutterPhase = 0f;
        else
            FlutterPhase = (FlutterPhase + 2f * (float)Math.PI * knobs.paperFlutterHz * dt) % (float)(2.0 * Math.PI * 64.0);
        if (_bow.AtRest)
            _settling = false;
        return hop;
    }

    /// <summary>
    /// It lands (a drop let go, or a hand-over's slap at the counter) with
    /// <paramref name="settle"/> of a full landing (0 to 1; SettleShare): its
    /// ends curl up and settle through the wobble (not a rigid card) and it
    /// hops; returns how high (metres: the caller kicks its lift spring by it;
    /// a card at most MotionKnobs.cardDropHop). Nothing for 0.
    /// </summary>
    public float Land(float settle, MotionKnobs knobs)
    {
        if (!(settle > 0f))
            return 0f;
        settle = Math.Min(settle, 1f);
        float hop = knobs.paperDropHop * settle / (float)Math.Sqrt(Weight);
        if (Rigid)
            return Math.Min(hop, knobs.cardDropHop);
        _bow.Kick(-knobs.paperLandKick * settle / Weight);
        _settling = true;
        return hop;
    }

    /// <summary>
    /// A refusal's shake in place (a machine's "no", the counter before the
    /// verdict; the feel bible's A11): side to side along its own x,
    /// <paramref name="amplitude"/> metres, <paramref name="cycles"/> times in
    /// <paramref name="seconds"/>, dying away (ShakeOffset); nothing under
    /// Reduced Motion (Step's share 0 ends it at once).
    /// </summary>
    public void Shake(float amplitude, float seconds, int cycles)
    {
        _shakeAmplitude = amplitude;
        _shakeSeconds = seconds > 0f ? seconds : 0f;
        _shakeCycles = cycles > 0 ? cycles : 1;
        _shakeElapsed = 0f;
    }

    /// <summary>Advances the shake (ShakeX); 0 once it has run its seconds, or at once with no motion.</summary>
    private void StepShake(float dt, float share)
    {
        if (_shakeSeconds <= 0f)
        {
            ShakeX = 0f;
            return;
        }
        _shakeElapsed += Math.Max(0f, dt);
        if (share <= 0f || _shakeElapsed >= _shakeSeconds)
        {
            _shakeSeconds = 0f;
            ShakeX = 0f;
            return;
        }
        ShakeX = ShakeOffset(_shakeElapsed, _shakeAmplitude * share, _shakeSeconds, _shakeCycles);
    }

    /// <summary>A shake's offset at <paramref name="t"/> seconds: <paramref name="cycles"/> full side-to-side cycles over <paramref name="seconds"/>, <paramref name="amplitude"/> at first and dying linearly to 0 (0 outside the shake).</summary>
    public static float ShakeOffset(float t, float amplitude, float seconds, int cycles)
    {
        if (seconds <= 0f || t <= 0f || t >= seconds)
            return 0f;
        float u = t / seconds;
        return amplitude * (1f - u) * (float)Math.Sin(2.0 * Math.PI * Math.Max(1, cycles) * u);
    }

    /// <summary>The share of a drop's settle (hop, landing curl) that plays for a body let go at <paramref name="speed"/> metres per second: in proportion to it, all of it from <paramref name="fullAt"/> (none at rest; all of it with no full speed set).</summary>
    public static float SettleShare(float speed, float fullAt)
    {
        if (fullAt <= 0f)
            return 1f;
        float s = speed / fullAt;
        return s <= 0f ? 0f : s >= 1f ? 1f : s;
    }

    /// <summary>
    /// Where a body's centre goes, from where it lies, when it swings by
    /// <paramref name="yawDegrees"/> about the desk's up on the held point
    /// (<paramref name="px"/>, <paramref name="pz"/>: from its centre, its
    /// frame, x right, z away): the held point stays put (Unity's turn about
    /// up: x' = x cos + z sin, z' = -x sin + z cos).
    /// </summary>
    public static (float x, float z) PivotOffset(float px, float pz, float yawDegrees)
    {
        double a = yawDegrees * Math.PI / 180.0;
        float c = (float)Math.Cos(a), s = (float)Math.Sin(a);
        // the centre lies at -p from the held point; turned, at R(-p) from it
        float rx = -px * c - pz * s, rz = px * s - pz * c;
        return (px + rx, pz + rz);
    }

    /// <summary>A held body's swing against its travel (degrees about the desk's up): turned against its sideways travel and with its forward travel the other way (its corners lag), <paramref name="perSpeed"/> degrees per metre per second, at most <paramref name="most"/>.</summary>
    public static float Sway(float vx, float vz, float perSpeed, float most) => Clamp(-(vx - vz * 0.5f) * perSpeed, most);

    /// <summary>The ripple's height on a move of <paramref name="speed"/> metres per second: none below <paramref name="from"/>, then <paramref name="perSpeed"/> metres per metre per second above it, at most <paramref name="most"/>.</summary>
    public static float FlutterHeight(float speed, float from, float perSpeed, float most) => speed <= from ? 0f : Math.Min((speed - from) * perSpeed, most);

    private static float Clamp(float v, float most) => v > most ? most : v < -most ? -most : v;
}
