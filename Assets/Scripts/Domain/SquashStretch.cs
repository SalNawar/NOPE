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

    /// <summary>
    /// A travelling piece's shape (Saleh 2026-10-07, round 2: "no stretch in
    /// motion"): stretched along its travel by its <paramref name="speed"/>
    /// (· <paramref name="perSpeed"/>) and squashed by its
    /// <paramref name="acceleration"/> (· <paramref name="perAccel"/>: the
    /// launch's anticipation, the arrival, each turn of a wobble), the two
    /// summed and held within 1 ± <paramref name="max"/>; the area keeps.
    /// </summary>
    public static Stretch FromMotion(float speed, float acceleration, float perSpeed, float perAccel, float max)
    {
        float m = Clamp(max, 0f, 0.9f);
        float along = 1f + MathF.Abs(speed) * MathF.Max(0f, perSpeed) - MathF.Abs(acceleration) * MathF.Max(0f, perAccel);
        return Preserve(Clamp(along, 1f - m, 1f + m));
    }

    /// <summary><paramref name="value"/> held between <paramref name="min"/> and <paramref name="max"/>.</summary>
    private static float Clamp(float value, float min, float max) => value < min ? min : value > max ? max : value;
}
