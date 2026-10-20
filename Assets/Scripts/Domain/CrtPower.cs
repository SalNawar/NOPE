using System;

/// <summary>
/// The CRT's power-on line and collapse (GAME_FEEL_BIBLE F1, BUG-32c,
/// UI-PC): opening, the picture grows from a bright horizontal line to full
/// height over <see cref="OpenSeconds"/> (OutCubic); closing, it squeezes to
/// a line over the first part of <see cref="CloseSeconds"/>, then the line
/// shrinks to a dot. Reduced Motion: no squeeze, a fade over
/// <see cref="ReducedSeconds"/> (GAME_FEEL_BIBLE G2). The frame's input is
/// live from the press frame; only the picture moves. Pure; PcFrame draws it.
/// </summary>
public static class CrtPower
{
    /// <summary>The power-on's length (seconds; F1: 150 ms OutCubic).</summary>
    public const float OpenSeconds = 0.15f;

    /// <summary>The collapse's length (seconds; F1: 120 ms).</summary>
    public const float CloseSeconds = 0.12f;

    /// <summary>The Reduced Motion fade's length (seconds; G2's 120 ms fade).</summary>
    public const float ReducedSeconds = 0.12f;

    /// <summary>The line's height while it is a line (a share of the picture's).</summary>
    public const float LineHeight = 0.02f;

    /// <summary>The share of the collapse the vertical squeeze takes (the rest shrinks the line to a dot).</summary>
    private const float SqueezeShare = 0.66f;

    /// <summary>How long the move takes: opening or closing, with Reduced Motion or not.</summary>
    public static float Seconds(bool opening, bool reduced) => reduced ? ReducedSeconds : opening ? OpenSeconds : CloseSeconds;

    /// <summary>
    /// The picture's scale (x, y) and alpha <paramref name="elapsed"/> seconds
    /// into opening or closing; at or past the end, opening is (1, 1, 1) and
    /// closing (0, 0, 0).
    /// </summary>
    public static (float x, float y, float alpha) At(float elapsed, bool opening, bool reduced)
    {
        float length = Seconds(opening, reduced);
        float t = length <= 0f ? 1f : Clamp01(elapsed / length);
        if (reduced)
            return opening ? (1f, 1f, t) : (1f, 1f, 1f - t);
        if (opening)
            return (1f, LineHeight + (1f - LineHeight) * OutCubic(t), 1f);
        if (t < SqueezeShare)
            return (1f, 1f - (1f - LineHeight) * OutCubic(t / SqueezeShare), 1f);
        float dot = (t - SqueezeShare) / (1f - SqueezeShare);
        return (1f - OutCubic(dot), LineHeight * (1f - dot), t >= 1f ? 0f : 1f);
    }

    private static float OutCubic(float t)
    {
        float u = 1f - Clamp01(t);
        return 1f - u * u * u;
    }

    private static float Clamp01(float v) => Math.Max(0f, Math.Min(1f, v));
}
