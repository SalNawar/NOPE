using System;

/// <summary>
/// The physical feel of a thing moved about the desk (Saleh 2026-10-17: "KEEP
/// IT TOP DOWN BUT GIVE IT PHYSICS"; his 1018a playtest: "all documents are
/// stiff"): a light spring body on top of where the desk puts it (the desk
/// decides the place, the drop sizing and the machines' docks; this only draws
/// the way there). Held and dragged, it is alive: it swings, turning against
/// its sideways travel with an overshoot (its corners lag), it bows along its
/// travel by its speed, the trailing end drooping most, behind the hand on a
/// loose spring, its corners twist as it swings, and a fast move sets a ripple
/// running back along it (the flutter). Let go, it keeps its momentum: it
/// slides on into its place and stops by friction (a critically damped slide
/// from behind the place, never back past it), spins a little by its sideways
/// momentum (a turn it keeps: a dropped paper lies where its spin left it),
/// hops once (Bounce: the lift spring's kick), and its bow settles through a
/// soft wobble (MotionKnobs.paperWobble: its ends curl up as it lands, then
/// droop, then lie flat), the ripple dying away. A heavier body (the rulebook
/// folder: <see cref="Weight"/>) swings and bends less and takes longer to
/// slide to rest. Every value is in the body's own frame on the desk: x right,
/// z away (metres), the turn about the desk's up (degrees). Stepped by Spring
/// (frame-rate independent); still under Reduced Motion (Share 0: everything
/// rests at once). What the paper's look makes of the bow, the twist and the
/// ripple is PaperBend's.
/// </summary>
public sealed class PaperPhysics
{
    /// <summary>How heavy the body is (1 a paper): its swing and bend divide by it, its slide's time grows with its square root.</summary>
    public float Weight { get; }

    private Spring _slideX, _slideZ, _yaw, _bow, _twist, _flutter;
    private float _velX, _velZ;
    private float _lastX, _lastZ;
    private bool _placed, _wasHeld, _settling;
    private float _releaseX, _releaseZ;

    /// <summary>A body of <paramref name="weight"/> (1 a paper, more for the folder), at rest.</summary>
    public PaperPhysics(float weight = 1f) => Weight = weight > 0.1f ? weight : 0.1f;

    /// <summary>Where it is drawn from its place on the desk (metres, its frame: x right, z away): its slide.</summary>
    public float OffsetX => _slideX.Value;

    /// <inheritdoc cref="OffsetX"/>
    public float OffsetZ => _slideZ.Value;

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

    /// <summary>True while any of its motion still runs.</summary>
    public bool Moving => !(_slideX.AtRest && _slideZ.AtRest && _yaw.AtRest && _bow.AtRest && _twist.AtRest && _flutter.AtRest);

    /// <summary>
    /// One frame of <paramref name="dt"/> seconds: the body's place now
    /// (<paramref name="x"/>, <paramref name="z"/>, metres, in its frame) and
    /// whether it is <paramref name="held"/>. Returns what the drop left it:
    /// the turn it keeps (degrees, 0 but on the frame it is let go) and how
    /// high it hops (metres, the same: the caller kicks its lift spring by it).
    /// </summary>
    public (float spin, float hop) Step(float dt, float x, float z, bool held, MotionKnobs knobs, MotionAmount amount)
    {
        float spin = 0f, hop = 0f;
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

        if (_wasHeld && !held && share > 0f)
        {
            // let go: it slides on into its place, spins by its sideways momentum, hops once, and its ends curl up as it lands (the wobble)
            float slideSeconds = knobs.paperSlideSeconds * (float)Math.Sqrt(Weight);
            (float sx, float sz) = SlideStart(_releaseX, _releaseZ, slideSeconds, knobs.paperSlideMax * share);
            _slideX.Value = sx;
            _slideZ.Value = sz;
            _slideX.Velocity = -sx / slideSeconds;
            _slideZ.Velocity = -sz / slideSeconds;
            _slideX.Target = _slideZ.Target = 0f;
            spin = DropSpin(_releaseX, _releaseZ, knobs.paperDropSpinPerSpeed * share / Weight, knobs.paperDropSpinMax * share);
            hop = knobs.paperDropHop * share / (float)Math.Sqrt(Weight);
            _bow.Kick(-knobs.paperLandKick * share / Weight);
            _settling = true;
        }
        _wasHeld = held;
        if (held)
        {
            _releaseX = _velX;
            _releaseZ = _velZ;
            _settling = false;
        }

        float speed = (float)Math.Sqrt(_velX * _velX + _velZ * _velZ);
        if (held && share > 0f)
        {
            _yaw.Target = Sway(_velX, _velZ, knobs.paperSwayPerSpeed * share / Weight, knobs.paperSwayMax * share);
            _bow.Target = Math.Min(speed * knobs.paperBowPerSpeed * share, knobs.paperBowMax * share) / Weight;
            _flutter.Target = FlutterHeight(speed, knobs.paperFlutterFrom, knobs.paperFlutterPerSpeed * share, knobs.paperFlutterMax * share) / Weight;
            if (speed > 1e-3f)
            {
                BowDirX = _velX / speed;
                BowDirZ = _velZ / speed;
            }
        }
        else
            _yaw.Target = _bow.Target = _flutter.Target = 0f;
        // the corners flap as it swings: the twist follows the swing's speed (degrees per second), against it
        _twist.Target = share > 0f ? Clamp(-_yaw.Velocity * knobs.paperTwistPerSwing * share / Weight, knobs.paperTwistMax * share) : 0f;

        if (share <= 0f)
        {
            _slideX.Snap(0f);
            _slideZ.Snap(0f);
            _yaw.Snap(0f);
            _bow.Snap(0f);
            _twist.Snap(0f);
            _flutter.Snap(0f);
            FlutterPhase = 0f;
            _settling = false;
            return (0f, 0f);
        }
        float slide = knobs.paperSlideSeconds * (float)Math.Sqrt(Weight);
        SpringTuning friction = SpringTuning.Critical(1f / (slide * slide));
        _slideX.Step(dt, friction, 1e-5f, 1e-4f);
        _slideZ.Step(dt, friction, 1e-5f, 1e-4f);
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
        return (spin, hop);
    }

    /// <summary>
    /// Where a body let go at speed (<paramref name="vx"/>, <paramref name="vz"/>)
    /// starts its slide from (metres from its place, behind it along its
    /// travel): as far as its momentum carries it in <paramref name="seconds"/>,
    /// at most <paramref name="most"/>. A critically damped slide from there
    /// with that speed comes to rest on the place without passing it.
    /// </summary>
    public static (float x, float z) SlideStart(float vx, float vz, float seconds, float most)
    {
        float x = -vx * seconds, z = -vz * seconds;
        float d = (float)Math.Sqrt(x * x + z * z);
        if (d > most && d > 0f)
        {
            x *= most / d;
            z *= most / d;
        }
        return (x, z);
    }

    /// <summary>A held body's swing against its travel (degrees about the desk's up): turned against its sideways travel and with its forward travel the other way (its corners lag), <paramref name="perSpeed"/> degrees per metre per second, at most <paramref name="most"/>.</summary>
    public static float Sway(float vx, float vz, float perSpeed, float most) => Clamp(-(vx - vz * 0.5f) * perSpeed, most);

    /// <summary>The turn a body keeps from being let go at (<paramref name="vx"/>, <paramref name="vz"/>): its sideways momentum's spin, <paramref name="perSpeed"/> degrees per metre per second, at most <paramref name="most"/>.</summary>
    public static float DropSpin(float vx, float vz, float perSpeed, float most) => Clamp((vx + vz * 0.35f) * perSpeed, most);

    /// <summary>The ripple's height on a move of <paramref name="speed"/> metres per second: none below <paramref name="from"/>, then <paramref name="perSpeed"/> metres per metre per second above it, at most <paramref name="most"/>.</summary>
    public static float FlutterHeight(float speed, float from, float perSpeed, float most) => speed <= from ? 0f : Math.Min((speed - from) * perSpeed, most);

    private static float Clamp(float v, float most) => v > most ? most : v < -most ? -most : v;
}
