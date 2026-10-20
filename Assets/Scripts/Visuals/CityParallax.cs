using System;

/// <summary>
/// The city view's parallax speed limit (BUG-69: "the panorama lurches with
/// the real mouse"; GAME_FEEL_BIBLE H1: "parallax ≤1 px/frame"): the
/// painting's pan (its sweep as it comes into view plus the pointer's look)
/// follows its target by at most one pixel at 1080p a frame on its nearest
/// layer (the flying traffic, which pans further than the painting). Pure;
/// CityView applies it.
/// </summary>
public static class CityParallax
{
    /// <summary>The most the nearest layer may move in one frame, in pixels at 1080p.</summary>
    public const float MaxPixelsPerFrame = 1f;

    /// <summary>
    /// The largest pan step (in shares of the painting, the panorama's
    /// uvRect) that moves the nearest layer <see cref="MaxPixelsPerFrame"/>:
    /// the painting shows <paramref name="span"/> of its width across
    /// <paramref name="shownWidthPx"/> screen pixels of a screen
    /// <paramref name="screenHeightPx"/> tall, and the nearest layer pans
    /// <paramref name="depth"/> times the painting (at least 1). 0 for an
    /// empty picture.
    /// </summary>
    public static float MaxStep(float span, float shownWidthPx, float screenHeightPx, float depth)
    {
        if (span <= 0f || shownWidthPx <= 0f || screenHeightPx <= 0f)
            return 0f;
        float pxAt1080PerShare = shownWidthPx / span * (1080f / screenHeightPx) * Math.Max(1f, depth);
        return MaxPixelsPerFrame / pxAt1080PerShare;
    }

    /// <summary>Moves <paramref name="shown"/> toward <paramref name="target"/> by at most <paramref name="maxStep"/> (0 or less holds it).</summary>
    public static float Step(float shown, float target, float maxStep)
    {
        if (maxStep <= 0f)
            return shown;
        float delta = target - shown;
        return Math.Abs(delta) <= maxStep ? target : shown + Math.Sign(delta) * maxStep;
    }
}
