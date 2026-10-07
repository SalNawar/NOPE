using System;

/// <summary>A squash or a stretch as two scale factors: along the motion (or the press) and across it.</summary>
public readonly struct Stretch
{
    /// <summary>The scale along the axis of travel (above 1 stretched, below squashed).</summary>
    public readonly float Along;

    /// <summary>The scale across it (the other way, so the area stays: Along · Across = 1).</summary>
    public readonly float Across;

    /// <summary>A stretch from its two factors.</summary>
    public Stretch(float along, float across)
    {
        Along = along;
        Across = across;
    }

    /// <summary>No squash, no stretch.</summary>
    public static Stretch None => new Stretch(1f, 1f);
}

/// <summary>
/// Squash and stretch that keeps the area (x·y = 1): a cartoon jelly never
/// grows or shrinks while it deforms. The selection pill stretching across to
/// its new item, a stamp squashing on the paper and a paper settling on the
/// desk all use it.
/// </summary>
public static class SquashStretch
{
    /// <summary>Stretched to <paramref name="along"/> along the axis, the other axis scaled by its inverse (a non-positive factor is taken as no stretch).</summary>
    public static Stretch Preserve(float along) => along > 0f ? new Stretch(along, 1f / along) : Stretch.None;

    /// <summary>Squashed by <paramref name="amount"/> along the axis of a press (0.2 is 20 % shorter), wider across it by the area's rule.</summary>
    public static Stretch Squash(float amount) => Preserve(1f - Clamp(amount, 0f, 0.9f));

    /// <summary>Stretched along the travel by the speed: 1 + |<paramref name="speed"/>| · <paramref name="perSpeed"/>, at most 1 + <paramref name="max"/>.</summary>
    public static Stretch FromSpeed(float speed, float perSpeed, float max) =>
        Preserve(1f + Clamp(MathF.Abs(speed) * MathF.Max(0f, perSpeed), 0f, MathF.Max(0f, max)));

    /// <summary><paramref name="value"/> held between <paramref name="min"/> and <paramref name="max"/>.</summary>
    private static float Clamp(float value, float min, float max) => value < min ? min : value > max ? max : value;
}
