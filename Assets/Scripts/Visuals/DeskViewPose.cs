using System;

/// <summary>
/// The knobs of the desk view (piece 10 section 11, T2; the desk-first
/// redesign, Saleh 2026-10-05, item 5: "the tilt on the desk zooms more and
/// the tilt is 80 degrees"), held by DeskConfigSO.deskView: the view looks
/// down at a fixed pitch onto an aim point near the mat's centre from a
/// distance, with its own field of view, and blends in and out over its
/// seconds.
/// </summary>
[Serializable]
public sealed class DeskViewTuning
{
    /// <summary>Degrees the desk view looks below the horizon (80: nearly straight down onto the papers; kept between level and DeskViewPose.MaxPitch).</summary>
    public float pitch = 80f;

    /// <summary>Metres from the aim point back to the camera along its view (smaller zooms closer).</summary>
    public float distance = 0.95f;

    /// <summary>Metres the aim point lies right of the mat's centre, along the office view's level right (toward the scanner and the stamps).</summary>
    public float aimRight = 0.08f;

    /// <summary>Metres the aim point lies ahead of the mat's centre, along the office view's level forward (negative: toward the chair).</summary>
    public float aimForward = -0.04f;

    /// <summary>The desk view's vertical field of view in degrees (0 or less keeps the office camera's lens).</summary>
    public float fieldOfView = 50f;

    /// <summary>Seconds of the blend into the desk view and back (0 or less cuts).</summary>
    public float seconds = 0.5f;
}

/// <summary>
/// The desk view's pose (piece 10 section 11, T2; the desk-first redesign,
/// item 5): the camera sits <see cref="DeskViewTuning.distance"/> back from
/// the aim point along a view pitched <see cref="DeskViewTuning.pitch"/>
/// below the horizon, keeping the office view's yaw; the blend's seconds, a
/// cut under Reduced Motion. Engine-free, so it is tested headless; DeskView
/// applies it.
/// </summary>
public static class DeskViewPose
{
    /// <summary>The steepest pitch, in degrees below the horizon (straight down would lose the view's yaw).</summary>
    public const float MaxPitch = 89f;

    /// <summary>The knob's pitch kept within 0..MaxPitch.</summary>
    public static float Pitch(DeskViewTuning tuning) => Math.Max(0f, Math.Min(MaxPitch, tuning.pitch));

    /// <summary>
    /// Where the camera sits from the aim point, in metres: <c>back</c> along
    /// the office view's level forward (positive: toward the chair) and
    /// <c>up</c> above it, so the view along the pitch passes through the aim
    /// point at the knob's distance (never negative).
    /// </summary>
    public static (float back, float up) Offset(DeskViewTuning tuning)
    {
        double radians = Pitch(tuning) * Math.PI / 180.0;
        float distance = Math.Max(0f, tuning.distance);
        return ((float)(distance * Math.Cos(radians)), (float)(distance * Math.Sin(radians)));
    }

    /// <summary>The blend's seconds: the knob's, or 0 (a cut) under Reduced Motion or a knob of 0 or less.</summary>
    public static float Seconds(DeskViewTuning tuning, bool reducedMotion) =>
        reducedMotion || tuning.seconds <= 0f ? 0f : tuning.seconds;
}
