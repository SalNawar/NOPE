using System;

/// <summary>How a desk prop reacts to a click. Serialized on DeskReactionSO: append only.</summary>
public enum ReactionKind
{
    /// <summary>No movement (a tooltip-only prop).</summary>
    None,

    /// <summary>Wider and flatter, then back (a stamp's thunk).</summary>
    Squash,

    /// <summary>A damped rock from side to side.</summary>
    Wobble,

    /// <summary>A small dip down, then back.</summary>
    Nudge,

    /// <summary>Grows evenly, then back.</summary>
    Pulse
}

/// <summary>A prop's pose relative to its rest pose: scale factors, a turn and a vertical offset (local units).</summary>
public readonly struct ReactionPose
{
    /// <summary>Horizontal scale factor.</summary>
    public readonly float ScaleX;

    /// <summary>Vertical scale factor.</summary>
    public readonly float ScaleY;

    /// <summary>Turn about the view axis, degrees.</summary>
    public readonly float AngleDeg;

    /// <summary>Vertical offset, local units.</summary>
    public readonly float OffsetY;

    /// <summary>Creates a pose.</summary>
    public ReactionPose(float scaleX, float scaleY, float angleDeg, float offsetY)
    {
        ScaleX = scaleX;
        ScaleY = scaleY;
        AngleDeg = angleDeg;
        OffsetY = offsetY;
    }

    /// <summary>The rest pose.</summary>
    public static ReactionPose Identity => new ReactionPose(1f, 1f, 0f, 0f);
}

/// <summary>The click reactions' curves: every kind starts and ends at rest (DeskReaction plays them).</summary>
public static class ReactionCurve
{
    /// <summary>
    /// The pose at time <paramref name="t"/> (clamped to 0..1) with amplitude a,
    /// where s = sin(pi t): Squash (1 + a s, 1 - a s); Pulse (1 + a s, 1 + a s);
    /// Nudge an offset of -a s; Wobble a turn of a * 30 * sin(3 pi t) * (1 - t)
    /// degrees; None the rest pose.
    /// </summary>
    public static ReactionPose Evaluate(ReactionKind kind, float t, float amplitude)
    {
        double c = t < 0f ? 0.0 : t > 1f ? 1.0 : t;
        float s = (float)Math.Sin(Math.PI * c);

        switch (kind)
        {
            case ReactionKind.Squash:
                return new ReactionPose(1f + amplitude * s, 1f - amplitude * s, 0f, 0f);
            case ReactionKind.Pulse:
                return new ReactionPose(1f + amplitude * s, 1f + amplitude * s, 0f, 0f);
            case ReactionKind.Nudge:
                return new ReactionPose(1f, 1f, 0f, -amplitude * s);
            case ReactionKind.Wobble:
                return new ReactionPose(1f, 1f, (float)(amplitude * 30.0 * Math.Sin(3.0 * Math.PI * c) * (1.0 - c)), 0f);
            default:
                return ReactionPose.Identity;
        }
    }
}
