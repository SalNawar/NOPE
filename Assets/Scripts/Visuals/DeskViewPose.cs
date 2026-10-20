using System;
using System.Collections.Generic;

/// <summary>
/// The knobs of the reading view (piece 10 section 11, T2; the desk-first
/// redesign, Saleh 2026-10-05, item 5: "the tilt is 80 degrees"; D1,
/// 2026-10-20: "we are doing the camera tilt"; the big mat, "keep a big mat
/// size": the reading view backs out), held by DeskConfigSO.deskView: the
/// view looks down at a fixed pitch with its own field of view, and its
/// distance and aim are solved per aspect (LensFit.Reading) so the mat, the
/// inner strips of the homes beside it and the deal tray fit inside the
/// screen's margins; it blends in and out over its seconds.
/// </summary>
[Serializable]
public sealed class DeskViewTuning
{
    /// <summary>Degrees the reading view looks below the horizon (80: nearly straight down onto the papers; kept between level and DeskViewPose.MaxPitch).</summary>
    public float pitch = 80f;

    /// <summary>The fallback pose's distance (metres from the aim point back along the view) when there is no desk home map to frame (an art office without it); the solved pose replaces it.</summary>
    public float distance = 0.8f;

    /// <summary>The fallback pose's aim (metres right of the mat's centre, along the office view's level right) when there is no desk home map to frame.</summary>
    public float aimRight = 0f;

    /// <summary>The fallback pose's aim (metres ahead of the mat's centre, along the office view's level forward) when there is no desk home map to frame.</summary>
    public float aimForward = 0.06f;

    /// <summary>The reading view's vertical field of view in degrees at rest, pushed in (0 or less keeps the office camera's lens).</summary>
    public float fieldOfView = 50f;

    /// <summary>Seconds of the blend into the reading view and back (0 or less cuts).</summary>
    public float seconds = 0.5f;

    /// <summary>The screen kept clear above the framed desk (a share of the half screen: the case HUD's strip and the "▲ Back" control).</summary>
    public float topMargin = 0.08f;

    /// <summary>The screen kept clear below the framed desk (a share of the half screen).</summary>
    public float bottomMargin = 0.03f;

    /// <summary>The screen kept clear left and right of the framed desk (a share of the half screen).</summary>
    public float sideMargin = 0.04f;

    /// <summary>How much of each side home's inner edge the reading view shows (metres; DESK-PLAN's hand-off: "≥ 8 x 8 cm of each"), so a tool by the mat is always in reach.</summary>
    public float sideReach = 0.08f;

    /// <summary>How high above the desk the papers' stack is framed (metres).</summary>
    public float paperLift = 0.03f;

    /// <summary>The homes the reading view frames (DeskHomeMap ids): the rail's machines and the left column by their inner strips (sideReach), the deal tray whole.</summary>
    public string[] homes =
    {
        DeskHomeMap.ChipReader, DeskHomeMap.Scanner, DeskHomeMap.StampRack,
        DeskHomeMap.Intercom, DeskHomeMap.BabelTuner, DeskHomeMap.RulebookPocket, DeskHomeMap.UvTorch,
        DeskHomeMap.DealTray,
    };
}

/// <summary>
/// The reading view's pose (piece 10 section 11, T2; CAM): the knobs' pitch,
/// what the solved pose must frame (<see cref="Targets"/>), the fallback pose
/// without a home map (<see cref="Offset"/>), the blend's seconds (a cut under
/// Reduced Motion) and the controls' fade over a move
/// (<see cref="ControlAlpha"/>). Engine-free, so it is tested headless;
/// DeskView applies it.
/// </summary>
public static class DeskViewPose
{
    /// <summary>The steepest pitch, in degrees below the horizon (straight down would lose the view's yaw).</summary>
    public const float MaxPitch = 89f;

    /// <summary>The share of a camera move after which the destination's controls start fading in (BUG-32b: "alpha stays 0 until 2/3 of the blend has elapsed").</summary>
    public const float ControlsFrom = 2f / 3f;

    /// <summary>The knob's pitch kept within 0..MaxPitch.</summary>
    public static float Pitch(DeskViewTuning tuning) => Math.Max(0f, Math.Min(MaxPitch, tuning.pitch));

    /// <summary>
    /// The fallback pose (no desk home map to frame): where the camera sits
    /// from the aim point, in metres: <c>back</c> along the office view's
    /// level forward (positive: toward the chair) and <c>up</c> above it, so
    /// the view along the pitch passes through the aim point at the knob's
    /// distance (never negative).
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

    /// <summary>
    /// What the reading pose frames, as points in the desk's frame (x metres
    /// right, y metres away from the chair, z metres above the desk): the
    /// <paramref name="mat"/>'s corners on the desk and at the papers' lift;
    /// and each of the knobs' homes found in <paramref name="homes"/>, its
    /// footprint's top and bottom corners: a home beside the mat (wholly left
    /// or right of it) only by the strip <see cref="DeskViewTuning.sideReach"/>
    /// deep along its edge nearest the mat, any other home whole.
    /// </summary>
    public static List<(float x, float y, float z)> Targets(DeskViewTuning tuning, DeskRect mat, IReadOnlyList<DeskHome> homes)
    {
        var points = new List<(float x, float y, float z)>();
        float matLeft = mat.CentreX - mat.Width / 2f, matRight = mat.CentreX + mat.Width / 2f;
        Box(points, matLeft, matRight, mat.CentreY - mat.Height / 2f, mat.CentreY + mat.Height / 2f, Math.Max(0f, tuning.paperLift));
        if (homes == null || tuning.homes == null)
            return points;
        float reach = Math.Max(0f, tuning.sideReach);
        foreach (string id in tuning.homes)
        {
            DeskHome home = DeskHomeMap.Find(homes, id);
            if (home == null)
                continue;
            DeskRect f = home.Footprint;
            float x0 = f.CentreX - f.Width / 2f, x1 = f.CentreX + f.Width / 2f;
            if (x0 >= matRight)
                x1 = Math.Min(x1, x0 + reach);
            else if (x1 <= matLeft)
                x0 = Math.Max(x0, x1 - reach);
            Box(points, x0, x1, f.CentreY - f.Height / 2f, f.CentreY + f.Height / 2f, Math.Max(0f, home.height));
        }
        return points;
    }

    /// <summary>
    /// A control's opacity <paramref name="elapsed"/> seconds into a camera
    /// move of <paramref name="seconds"/> that brings its view (BUG-32b,
    /// VIS-26): 0 until <see cref="ControlsFrom"/> of the move, then eased up
    /// to 1 at its end; 1 for a cut (no seconds).
    /// </summary>
    public static float ControlAlpha(float elapsed, float seconds)
    {
        if (seconds <= 0f)
            return 1f;
        float t = (elapsed / seconds - ControlsFrom) / (1f - ControlsFrom);
        t = Math.Min(1f, Math.Max(0f, t));
        return t * t * (3f - 2f * t);
    }

    private static void Box(List<(float x, float y, float z)> points, float x0, float x1, float y0, float y1, float height)
    {
        foreach (float x in new[] { x0, x1 })
            foreach (float y in new[] { y0, y1 })
            {
                points.Add((x, y, 0f));
                if (height > 0f)
                    points.Add((x, y, height));
            }
    }
}
