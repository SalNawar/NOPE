using System;

/// <summary>
/// The city view's parallax speed limit (BUG-69: "the panorama lurches with
/// the real mouse"; its check: "30 fps capture while moving the mouse, with
/// no jumps over 1 px per frame"; GAME_FEEL_BIBLE H1: "parallax ≤1
/// px/frame", check 7: nothing stopping dead): the painting's pan (its sweep
/// as it comes into view plus the pointer's look) follows its target on a
/// critically damped spring whose speed never moves the nearest layer (the
/// flying traffic, which pans further than the painting) more than
/// <see cref="MaxPixelsPerSecond"/> at 1080p: one pixel a frame at the QA
/// recorder's 30 fps, the same seconds at any frame rate, easing into its
/// target. The sweep is kept small enough that its own ease never needs the
/// limit (<see cref="SweepShare"/>). Pure; CityView applies it.
/// </summary>
public static class CityParallax
{
    /// <summary>The most the nearest layer may move in one frame of the QA recorder's 30 fps capture, in pixels at 1080p (BUG-69's check).</summary>
    public const float MaxPixelsPerFrame = 1f;

    /// <summary>The frame rate the limit is read at (the QA recorder's fixed step, SeqRecorder.Fps).</summary>
    public const float CheckFps = 30f;

    /// <summary>The most the nearest layer may move in a second, in pixels at 1080p: one pixel a frame at <see cref="CheckFps"/>, half a pixel a frame at 60.</summary>
    public const float MaxPixelsPerSecond = MaxPixelsPerFrame * CheckFps;

    /// <summary>How long the pan takes to settle onto a still target (seconds; the spring's smoothing time).</summary>
    public const float SmoothSeconds = 0.35f;

    /// <summary>The share of the speed limit a sweep's eased path may use at its fastest, so the spring under it never meets the limit.</summary>
    public const float SweepHeadroom = 0.8f;

    /// <summary>The steepest slope of the sweep's ease (CityLookTimeline's smoothstep peaks at 1.5 times the average speed).</summary>
    private const float EasePeak = 1.5f;

    /// <summary>
    /// The pan's speed limit (shares of the painting, the panorama's uvRect,
    /// a second) that moves the nearest layer
    /// <see cref="MaxPixelsPerSecond"/>: the painting shows
    /// <paramref name="span"/> of its width across
    /// <paramref name="shownWidthPx"/> screen pixels of a screen
    /// <paramref name="screenHeightPx"/> tall, and the nearest layer pans
    /// <paramref name="depth"/> times the painting (at least 1). 0 for an
    /// empty picture.
    /// </summary>
    public static float MaxSpeed(float span, float shownWidthPx, float screenHeightPx, float depth)
    {
        if (span <= 0f || shownWidthPx <= 0f || screenHeightPx <= 0f)
            return 0f;
        float pxAt1080PerShare = shownWidthPx / span * (1080f / screenHeightPx) * Math.Max(1f, depth);
        return MaxPixelsPerSecond / pxAt1080PerShare;
    }

    /// <summary>
    /// The sweep (shares of the painting) a city reveal may make over
    /// <paramref name="settleSeconds"/>: the <paramref name="requested"/> one
    /// (DeskConfigSO.cityParallax), made smaller where its eased path would
    /// outrun <see cref="SweepHeadroom"/> of <paramref name="maxSpeed"/>, so
    /// it reads as a settle, never a crawl at the limit.
    /// </summary>
    public static float SweepShare(float requested, float settleSeconds, float maxSpeed)
    {
        if (requested <= 0f || settleSeconds <= 0f || maxSpeed <= 0f)
            return 0f;
        return Math.Min(requested, SweepHeadroom * maxSpeed * settleSeconds / EasePeak);
    }

    /// <summary>
    /// One step of <paramref name="dt"/> seconds of the pan from
    /// <paramref name="shown"/> toward <paramref name="target"/> on a
    /// critically damped spring settling in about <see cref="SmoothSeconds"/>
    /// (<paramref name="velocity"/> carries its speed between frames), never
    /// faster than <paramref name="maxSpeed"/> a second nor, on a slow frame,
    /// more than a <see cref="CheckFps"/> frame's worth of it: it eases into
    /// a still target and lands on it, and takes the same seconds at any
    /// frame rate. A limit of 0 or less holds it.
    /// </summary>
    public static float Follow(float shown, ref float velocity, float target, float maxSpeed, float dt)
    {
        if (maxSpeed <= 0f || dt <= 0f)
        {
            velocity = 0f;
            return shown;
        }
        // The game-programming-gems critically damped step (as Mathf.SmoothDamp), with the speed limit on the change.
        float omega = 2f / SmoothSeconds;
        float x = omega * dt;
        float exp = 1f / (1f + x + 0.48f * x * x + 0.235f * x * x * x);
        float change = shown - target;
        float most = maxSpeed * SmoothSeconds;
        change = Math.Max(-most, Math.Min(most, change));
        float goal = shown - change;
        float temp = (velocity + omega * change) * dt;
        velocity = (velocity - omega * temp) * exp;
        float next = goal + (change + temp) * exp;
        // Never past the target.
        if ((target - shown > 0f) == (next > target))
        {
            next = target;
            velocity = 0f;
        }
        // Never faster than the limit, on a slow frame no more than a check frame's worth.
        float step = next - shown, cap = maxSpeed * Math.Min(dt, 1f / CheckFps);
        if (Math.Abs(step) > cap)
        {
            next = shown + Math.Sign(step) * cap;
            velocity = (next - shown) / dt;
        }
        return next;
    }
}
