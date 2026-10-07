using System;

/// <summary>Where a delivered paper is at one moment of its flight (PaperFlight.At): along the way, up off the desk, its twist and tumble, its squash and its shadow.</summary>
public readonly struct FlightPose
{
    /// <summary>A pose from its parts.</summary>
    public FlightPose(float along, float lift, float twist, float tumble, float squash, float shadow, bool landed)
    {
        Along = along;
        Lift = lift;
        Twist = twist;
        Tumble = tumble;
        Squash = squash;
        Shadow = shadow;
        Landed = landed;
    }

    /// <summary>How far from the start to the spot (0 to 1; a little past 1 in the springy overshoot).</summary>
    public float Along { get; }

    /// <summary>The height over its arc, as a share of the arc's top (0 on the desk).</summary>
    public float Lift { get; }

    /// <summary>Its turn about the vertical, in degrees (0 when it lies square).</summary>
    public float Twist { get; }

    /// <summary>Its tumble about its long axis, in degrees (0 flat).</summary>
    public float Tumble { get; }

    /// <summary>Its size as a share of its own (1, a little less in the landing's squash).</summary>
    public float Squash { get; }

    /// <summary>Its shadow's strength on the desk (0 none to 1 sharp, as it lands).</summary>
    public float Shadow { get; }

    /// <summary>True once it has touched the desk (the thud).</summary>
    public bool Landed { get; }
}

/// <summary>
/// A delivered paper's flight onto the desk (Saleh 2026-10-07, the citation:
/// "when it shows on your screen it should slide and twist, then become a
/// document on the desk"): from the screen's edge it slides along a slight
/// arc, twists about the vertical and tumbles about its long axis, settles
/// with a damped spring's overshoot, lands at LandAt of its time with a soft
/// squash, and its shadow sharpens as it comes down. Reduced Motion: a short
/// straight slide, no twist, no tumble, no overshoot. Pure: PaperArrival
/// drives a paper with it.
/// </summary>
public static class PaperFlight
{
    /// <summary>The flight's length (seconds) and Reduced Motion's slide.</summary>
    public const float Seconds = 1.05f, ReducedSeconds = 0.25f;

    /// <summary>The share of the flight at which it touches the desk; the rest settles.</summary>
    public const float LandAt = 0.72f;

    /// <summary>The damped spring: its damping ratio and its angular frequency over the flight (so it overshoots once and settles).</summary>
    public const float Damping = 0.6f, Frequency = 7.5f;

    /// <summary>The squash at the landing (a share of its size) and the shadow's strength high up.</summary>
    public const float SquashDepth = 0.035f, FarShadow = 0.15f;

    /// <summary>
    /// The pose at <paramref name="t"/> (0 to 1 of the flight) starting
    /// twisted by <paramref name="twist"/> and tumbled by
    /// <paramref name="tumble"/> degrees; at 0 it is at the start, at 1 exactly
    /// on its spot, square, flat and its own size.
    /// </summary>
    public static FlightPose At(float t, float twist, float tumble, bool reduced)
    {
        t = Math.Max(0f, Math.Min(1f, t));
        if (reduced)
        {
            float e = 1f - (1f - t) * (1f - t);
            return new FlightPose(e, 0f, 0f, 0f, 1f, 1f, t >= 1f);
        }
        float s = Spring(t);
        float rest = 1f - s;
        float lift = t < LandAt ? (float)Math.Sin(Math.PI * t / LandAt) : 0f;
        float squash = 1f;
        if (t >= LandAt)
        {
            float k = (t - LandAt) / (1f - LandAt);
            squash = 1f - SquashDepth * (float)Math.Sin(Math.PI * Math.Min(1f, k * 2f));
        }
        float shadow = FarShadow + (1f - FarShadow) * (1f - lift);
        float wobble = (float)(Math.Sin(t * Math.PI * 3.0) * (1.0 - t));
        return new FlightPose(s, lift, twist * rest, tumble * rest * (0.6f + 0.4f * wobble), squash, shadow, t >= LandAt);
    }

    /// <summary>The flight's length for the motion preference.</summary>
    public static float Length(bool reduced) => reduced ? ReducedSeconds : Seconds;

    /// <summary>The damped spring's step response at <paramref name="t"/>, eased to exactly 1 at the end.</summary>
    private static float Spring(float t)
    {
        double w = Frequency, z = Damping, wd = w * Math.Sqrt(1 - z * z);
        double s = 1 - Math.Exp(-z * w * t) * (Math.Cos(wd * t) + z * w / wd * Math.Sin(wd * t));
        double end = Math.Pow(t, 8);
        return (float)(s * (1 - end) + end);
    }
}
