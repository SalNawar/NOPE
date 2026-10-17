using System;

/// <summary>
/// The physical feel of a thing moved about the desk (Saleh 2026-10-17: "KEEP
/// IT TOP DOWN BUT GIVE IT PHYSICS"): a light spring body on top of where the
/// desk puts it (the desk decides the place, the drop sizing and the scanner's
/// snap; this only draws the way there). Held and dragged, it swings: it
/// turns a little against its sideways travel and its free edges sag with its
/// speed, as a sheet held in the middle lags at its corners. Let go, it keeps
/// its momentum: it slides on into its place and stops by friction (a
/// critically damped slide from behind the place, never back past it), spins
/// a little by its sideways momentum (a turn it keeps: a dropped paper lies
/// where its spin left it) and hops once (Bounce: the lift spring's kick).
/// A heavier body (the rulebook folder: <see cref="Weight"/>) swings and sags
/// less and takes longer to slide to rest. Every value is in the body's own
/// frame on the desk: x right, z away (metres), the turn about the desk's up
/// (degrees). Stepped by Spring (frame-rate independent); still under Reduced
/// Motion (Share 0: everything rests at once).
/// </summary>
public sealed class PaperPhysics
{
    /// <summary>How heavy the body is (1 a paper): its swing and sag divide by it, its slide's time grows with its square root.</summary>
    public float Weight { get; }

    private Spring _slideX, _slideZ, _yaw, _sag;
    private float _velX, _velZ;
    private float _lastX, _lastZ;
    private bool _placed, _wasHeld;

    /// <summary>A body of <paramref name="weight"/> (1 a paper, more for the folder), at rest.</summary>
    public PaperPhysics(float weight = 1f) => Weight = weight > 0.1f ? weight : 0.1f;

    /// <summary>Where it is drawn from its place on the desk (metres, its frame: x right, z away): its slide.</summary>
    public float OffsetX => _slideX.Value;

    /// <inheritdoc cref="OffsetX"/>
    public float OffsetZ => _slideZ.Value;

    /// <summary>Its swing about the desk's up while held (degrees), back to 0 at rest.</summary>
    public float Yaw => _yaw.Value;

    /// <summary>How far its free edges sag (metres, at its edge), and the way it travels (a unit vector in its frame) they sag across.</summary>
    public float Sag => _sag.Value;

    /// <inheritdoc cref="Sag"/>
    public float SagDirX { get; private set; } = 1f;

    /// <inheritdoc cref="Sag"/>
    public float SagDirZ { get; private set; }

    /// <summary>Its travel's speed now (metres per second, smoothed), in its frame.</summary>
    public float SpeedX => _velX;

    /// <inheritdoc cref="SpeedX"/>
    public float SpeedZ => _velZ;

    /// <summary>True while any of its motion still runs.</summary>
    public bool Moving => !(_slideX.AtRest && _slideZ.AtRest && _yaw.AtRest && _sag.AtRest);

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
                _velX = _velZ = 0f; // the desk's own moves (a slide, a snap) carry no momentum of their own
        }
        _lastX = x;
        _lastZ = z;

        if (_wasHeld && !held && share > 0f)
        {
            // let go: it slides on into its place, spins by its sideways momentum and hops once
            float slideSeconds = knobs.paperSlideSeconds * (float)Math.Sqrt(Weight);
            (float sx, float sz) = SlideStart(_releaseX, _releaseZ, slideSeconds, knobs.paperSlideMax * share);
            _slideX.Value = sx;
            _slideZ.Value = sz;
            _slideX.Velocity = -sx / slideSeconds;
            _slideZ.Velocity = -sz / slideSeconds;
            _slideX.Target = _slideZ.Target = 0f;
            spin = DropSpin(_releaseX, _releaseZ, knobs.paperDropSpinPerSpeed * share / Weight, knobs.paperDropSpinMax * share);
            hop = knobs.paperDropHop * share / (float)Math.Sqrt(Weight);
            _sag.Kick(-knobs.paperSagPerSpeed * share * 0.5f);
        }
        _wasHeld = held;
        if (held)
        {
            _releaseX = _velX;
            _releaseZ = _velZ;
        }

        if (held && share > 0f)
        {
            _yaw.Target = Sway(_velX, _velZ, knobs.paperSwayPerSpeed * share / Weight, knobs.paperSwayMax * share);
            float speed = (float)Math.Sqrt(_velX * _velX + _velZ * _velZ);
            _sag.Target = Math.Min(speed * knobs.paperSagPerSpeed * share / Weight, knobs.paperSagMax * share);
            if (speed > 1e-3f)
            {
                SagDirX = _velX / speed;
                SagDirZ = _velZ / speed;
            }
        }
        else
            _yaw.Target = _sag.Target = 0f;

        if (share <= 0f)
        {
            _slideX.Snap(0f);
            _slideZ.Snap(0f);
            _yaw.Snap(0f);
            _sag.Snap(0f);
            return (0f, 0f);
        }
        float slide = knobs.paperSlideSeconds * (float)Math.Sqrt(Weight);
        SpringTuning friction = SpringTuning.Critical(1f / (slide * slide));
        SpringTuning swing = knobs.Get(knobs.paperFeel);
        _slideX.Step(dt, friction, 1e-5f, 1e-4f);
        _slideZ.Step(dt, friction, 1e-5f, 1e-4f);
        _yaw.Step(dt, swing, knobs.settleValue * 10f, knobs.settleSpeed * 10f);
        _sag.Step(dt, swing, 1e-5f, 1e-4f);
        return (spin, hop);
    }

    private float _releaseX, _releaseZ;

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

    private static float Clamp(float v, float most) => v > most ? most : v < -most ? -most : v;
}
