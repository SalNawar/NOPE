using System;

/// <summary>
/// The named feels of the game's motion (Saleh 2026-10-07: "every single
/// button, every single action to feel this satisfying"; the reel's toggle in
/// three tunings, Balanced / Firm / Elastic). Each names one spring tuning in
/// MotionKnobs. Serialized on MotionTuningSO: append only.
/// </summary>
public enum MotionFeel
{
    /// <summary>Quick and nearly flat: a press going down, a stamp's slam.</summary>
    Firm,

    /// <summary>The default: a visible overshoot that settles in two swings.</summary>
    Balanced,

    /// <summary>Jelly: a big overshoot and a wobble (a release, a pop, a "no" shake).</summary>
    Elastic,

    /// <summary>Slow and soft: a paper lifting, settling or drifting in.</summary>
    Paper,

    /// <summary>Weighty and barely overshooting: a panel, a window, the camera.</summary>
    Heavy
}

/// <summary>
/// One damped spring's tuning: stiffness k, damping c and mass m (the force
/// on a displacement x moving at v is -k·x - c·v). The damping ratio
/// c / (2·√(k·m)) says how it settles: 1 is critically damped (the fastest
/// with no overshoot), below 1 it overshoots and wobbles, above 1 it creeps.
/// </summary>
[Serializable]
public struct SpringTuning
{
    /// <summary>How hard the spring pulls toward its target (per unit of displacement).</summary>
    public float stiffness;

    /// <summary>How hard it resists speed (per unit of velocity).</summary>
    public float damping;

    /// <summary>The moving mass (heavier is slower and swings longer at the same stiffness).</summary>
    public float mass;

    /// <summary>A tuning from its three constants.</summary>
    public SpringTuning(float stiffness, float damping, float mass)
    {
        this.stiffness = stiffness;
        this.damping = damping;
        this.mass = mass;
    }

    /// <summary>A tuning of <paramref name="stiffness"/> and <paramref name="mass"/> whose damping gives the damping ratio <paramref name="ratio"/> (1: critical).</summary>
    public static SpringTuning WithRatio(float stiffness, float ratio, float mass = 1f) =>
        new SpringTuning(stiffness, 2f * ratio * MathF.Sqrt(MathF.Max(0f, stiffness) * MathF.Max(1e-6f, mass)), mass);

    /// <summary>The critically damped tuning of <paramref name="stiffness"/> and <paramref name="mass"/>: the fastest settle that never overshoots.</summary>
    public static SpringTuning Critical(float stiffness, float mass = 1f) => WithRatio(stiffness, 1f, mass);

    /// <summary>The mass used by the integrator (never zero).</summary>
    public float SafeMass => mass > 1e-6f ? mass : 1e-6f;

    /// <summary>The undamped angular frequency √(k/m) (radians per second).</summary>
    public float Omega => MathF.Sqrt(MathF.Max(0f, stiffness) / SafeMass);

    /// <summary>The damping ratio c / (2·√(k·m)) (1: critical; below: it overshoots).</summary>
    public float Ratio
    {
        get
        {
            float critical = 2f * MathF.Sqrt(MathF.Max(0f, stiffness) * SafeMass);
            return critical > 0f ? damping / critical : float.PositiveInfinity;
        }
    }

    /// <summary>
    /// How far past its target the spring swings when it is let go from rest
    /// (a share of the distance travelled): exp(-ζπ/√(1-ζ²)) below critical
    /// damping, 0 at or above it.
    /// </summary>
    public float Overshoot
    {
        get
        {
            float z = Ratio;
            return z >= 1f ? 0f : MathF.Exp(-z * MathF.PI / MathF.Sqrt(1f - z * z));
        }
    }

    /// <summary>
    /// The speed to give a spring at rest on its target so that it swings out
    /// to <paramref name="amplitude"/> and back (a pop, a shake): the peak of
    /// x(t) = (v/ωd)·e^(-ζωt)·sin(ωd·t) solved for v (at or above critical
    /// damping, the critical peak v/(ω·e)).
    /// </summary>
    public float KickFor(float amplitude)
    {
        float w = Omega;
        if (w <= 0f)
            return 0f;
        float z = Ratio;
        if (z >= 1f)
            return amplitude * w * MathF.E;
        float s = MathF.Sqrt(1f - z * z);
        float peak = MathF.Exp(-z / s * MathF.Atan2(s, z));
        return amplitude * w / peak;
    }
}

/// <summary>
/// One damped spring's state: where it is, how fast it moves and where it is
/// pulled to. Stepped by semi-implicit Euler (velocity first, then position
/// from the new velocity) in sub-steps no longer than <see cref="MaxSubStep"/>,
/// so a frame of any length lands on (nearly) the same curve: 30 and 144
/// frames a second reach the same state. Settles (snaps onto its target and
/// stops) once it is within <see cref="Settle"/>'s tolerances.
/// </summary>
[Serializable]
public struct Spring
{
    /// <summary>The longest sub-step the integrator takes (seconds): stable for every tuning the game uses and short enough that frame rates agree.</summary>
    public const float MaxSubStep = 1f / 1000f;

    /// <summary>The longest frame a step accepts (seconds): a hitch longer than this is taken as this long, so the spring never jumps across a long pause.</summary>
    public const float MaxFrame = 0.1f;

    /// <summary>The current value.</summary>
    public float Value;

    /// <summary>The current speed (units per second).</summary>
    public float Velocity;

    /// <summary>The value it is pulled toward.</summary>
    public float Target;

    /// <summary>A spring at rest at <paramref name="value"/> (its target too).</summary>
    public static Spring At(float value) => new Spring { Value = value, Target = value };

    /// <summary>True when it sits on its target and does not move.</summary>
    public bool AtRest => Value == Target && Velocity == 0f;

    /// <summary>Puts it at rest at <paramref name="value"/> (a cut: Reduced Motion, a reset).</summary>
    public void Snap(float value)
    {
        Value = Target = value;
        Velocity = 0f;
    }

    /// <summary>Adds <paramref name="speed"/> to its velocity (a kick: SpringTuning.KickFor gives the speed for a swing of a given size).</summary>
    public void Kick(float speed) => Velocity += speed;

    /// <summary>
    /// Advances it by <paramref name="dt"/> seconds with <paramref name="tuning"/>
    /// (clamped to <see cref="MaxFrame"/>, in equal sub-steps of at most
    /// <see cref="MaxSubStep"/>), then settles it when within
    /// <paramref name="valueTolerance"/> of its target and slower than
    /// <paramref name="speedTolerance"/>. Returns true while it still moves.
    /// </summary>
    public bool Step(float dt, SpringTuning tuning, float valueTolerance = 1e-4f, float speedTolerance = 1e-3f)
    {
        if (AtRest)
            return false;
        if (dt > 0f)
        {
            dt = MathF.Min(dt, MaxFrame);
            int steps = (int)MathF.Ceiling(dt / MaxSubStep);
            float h = dt / steps;
            float invMass = 1f / tuning.SafeMass;
            for (int i = 0; i < steps; i++)
            {
                float force = -tuning.stiffness * (Value - Target) - tuning.damping * Velocity;
                Velocity += force * invMass * h;
                Value += Velocity * h;
            }
        }
        if (Settle(valueTolerance, speedTolerance))
            return false;
        return true;
    }

    /// <summary>Snaps onto the target and stops when within <paramref name="valueTolerance"/> of it and slower than <paramref name="speedTolerance"/>; true when it did (or already rested).</summary>
    public bool Settle(float valueTolerance, float speedTolerance)
    {
        if (MathF.Abs(Value - Target) > valueTolerance || MathF.Abs(Velocity) > speedTolerance)
            return false;
        Value = Target;
        Velocity = 0f;
        return true;
    }
}
