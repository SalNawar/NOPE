using System;

/// <summary>
/// The knobs of the desk view (piece 10 section 11, T2), held by
/// DeskConfigSO.deskView: how far the camera moves from the art office's view
/// (along its level forward, and up), how much further it pitches than aiming
/// at the mat's centre, and the blend's seconds.
/// </summary>
[Serializable]
public sealed class DeskViewTuning
{
    /// <summary>Metres the desk view moves along the normal view's level forward.</summary>
    public float forward = 0.6f;

    /// <summary>Metres the desk view rises above the normal view (negative lowers it).</summary>
    public float rise = 0.3f;

    /// <summary>Degrees the desk view pitches past aiming at the mat's centre (positive looks further down; the default -4 keeps the floor under the desk's front edge out of the view's bottom).</summary>
    public float pitch = -4f;

    /// <summary>Seconds of the blend into the desk view and back (0 or less cuts).</summary>
    public float seconds = 0.35f;
}

/// <summary>
/// The desk view's pose (piece 10 section 11, T2): from the normal view moved
/// by the knobs, the pitch that aims at the mat's centre plus the pitch knob,
/// kept between level and straight down; the blend's seconds, a cut under
/// Reduced Motion. Engine-free, so it is tested headless; DeskView applies it.
/// </summary>
public static class DeskViewPose
{
    /// <summary>The steepest pitch, in degrees below the horizon (straight down would lose the view's yaw).</summary>
    public const float MaxPitch = 89f;

    /// <summary>
    /// The desk view's pitch in degrees below the horizon, from the normal
    /// view's height above the mat's centre and the mat's centre's distance
    /// ahead along the view's level forward (metres), once the view has moved
    /// by the knobs; kept within 0..MaxPitch.
    /// </summary>
    public static float Pitch(float heightAboveMat, float depthToMat, DeskViewTuning tuning)
    {
        float height = heightAboveMat + tuning.rise;
        float depth = depthToMat - tuning.forward;
        float aim = (float)(Math.Atan2(height, depth) * 180.0 / Math.PI);
        return Math.Max(0f, Math.Min(MaxPitch, aim + tuning.pitch));
    }

    /// <summary>The blend's seconds: the knob's, or 0 (a cut) under Reduced Motion or a knob of 0 or less.</summary>
    public static float Seconds(DeskViewTuning tuning, bool reducedMotion) =>
        reducedMotion || tuning.seconds <= 0f ? 0f : tuning.seconds;
}
