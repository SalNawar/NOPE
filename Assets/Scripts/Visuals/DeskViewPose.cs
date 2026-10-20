using System;
using System.Collections.Generic;

/// <summary>
/// The knobs of the reading view (piece 10 section 11, T2; the desk-first
/// redesign, Saleh 2026-10-05, item 5: "the tilt is 80 degrees"; D1,
/// 2026-10-20: "we are doing the camera tilt"; the big mat, "keep a big mat
/// size": the reading view backs out), held by DeskConfigSO.deskView: the
/// view looks down at a fixed pitch with its own field of view, and its
/// distance and aim are solved per aspect (LensFit.Reading) so the mat, the
/// stamp rack and the intercom whole, the inner strips of the other homes
/// beside the mat and the deal tray fit inside the screen's margins, below
/// the overlay's top band of chrome; it blends in and out over its seconds.
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

    /// <summary>The reading view's vertical field of view in degrees at rest, once its push-in has settled (47.5: the lens main's reading view rested at, 50 narrowed by MotionKnobs.readingPushIn, which the desk plan's measured camera used); the pose is solved at it (0 or less keeps the office camera's lens).</summary>
    public float fieldOfView = 47.5f;

    /// <summary>Seconds of the blend into the reading view and back (0 or less cuts).</summary>
    public float seconds = 0.5f;

    /// <summary>The overlay's top band no framed desk point may enter, in reference pixels of the overlay's 1080 rows (DeskViewPose.TopMargin turns it into a share of the screen at its aspect): the case HUD's compare and verdict strips (16..88) and the "▲ Back" control at the top right (16..60), and a gap (cam.chrome.band checks the builder keeps them in it).</summary>
    public float topBand = 96f;

    /// <summary>The screen kept clear below the framed desk (a share of the half screen).</summary>
    public float bottomMargin = 0.03f;

    /// <summary>The screen kept clear left and right of the framed desk (a share of the half screen; the pull tabs hide in the reading view, so no chrome stands in it).</summary>
    public float sideMargin = 0.04f;

    /// <summary>How much of a framed home beside the mat the reading view shows (metres, its strip along the edge nearest the mat; the desk plan's "≥ 8 x 8 cm of 2, 3, 7, 8 and 9"), unless it is framed whole.</summary>
    public float sideReach = 0.08f;

    /// <summary>How high above the desk the papers' stack is framed (metres).</summary>
    public float paperLift = 0.03f;

    /// <summary>The homes the reading view frames (DeskHomeMap ids): the rail's machines and the left column, by their inner strips beside the mat unless framed whole, and the deal tray whole.</summary>
    public string[] homes =
    {
        DeskHomeMap.ChipReader, DeskHomeMap.Scanner, DeskHomeMap.StampRack,
        DeskHomeMap.Intercom, DeskHomeMap.BabelTuner, DeskHomeMap.RulebookPocket, DeskHomeMap.UvTorch,
        DeskHomeMap.DealTray,
    };

    /// <summary>The framed homes shown whole even beside the mat (the desk plan's §6: "rack and intercom 100% in frame" in the reading view).</summary>
    public string[] whole = { DeskHomeMap.StampRack, DeskHomeMap.Intercom };
}

/// <summary>
/// The reading view's pose (piece 10 section 11, T2; CAM): the knobs' pitch,
/// what the solved pose must frame (<see cref="Targets"/>), the top margin
/// the overlay's chrome band takes at an aspect (<see cref="TopMargin"/>),
/// the fallback pose without a home map (<see cref="Offset"/>), the blend's
/// seconds (a cut when motion is still: Reduced Motion or no intensity) and
/// the controls' fade over a move (<see cref="ControlAlpha"/>). Engine-free,
/// so it is tested headless; DeskView applies it.
/// </summary>
public static class DeskViewPose
{
    /// <summary>The steepest pitch, in degrees below the horizon (straight down would lose the view's yaw).</summary>
    public const float MaxPitch = 89f;

    /// <summary>The share of a camera move after which the destination's controls start fading in (BUG-32b: "alpha stays 0 until 2/3 of the blend has elapsed").</summary>
    public const float ControlsFrom = 2f / 3f;

    /// <summary>The office overlay's reference resolution (OfficeSceneUIBuilder.ConfigureScaler: scaled with the screen, Expand; cam.chrome.band checks the builder's is this).</summary>
    public const float ReferenceWidth = 1920f, ReferenceHeight = 1080f;

    /// <summary>The old reading view the papers' on-screen size is measured against (main before CAM, at rest: 0.8 m from the aim point at a 47.5 degree lens; the desk plan's measured camera, homemap.py's READING).</summary>
    public const float OldDistance = 0.8f, OldFieldOfView = 47.5f;

    /// <summary>How big a paper shows (its height on the screen) in a pose <paramref name="distance"/> metres back at a <paramref name="fieldOfView"/> degree lens, against the old reading view (1: as big; the text must grow by its inverse to keep its pixels).</summary>
    public static float PaperShare(float distance, float fieldOfView)
    {
        double now = distance * Math.Tan(fieldOfView * Math.PI / 360.0);
        return now <= 0.0 ? 0f : (float)(OldDistance * Math.Tan(OldFieldOfView * Math.PI / 360.0) / now);
    }

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

    /// <summary>The blend's seconds: the knob's, or 0 (a cut) when motion is <paramref name="still"/> (Reduced Motion or a Motion intensity of 0: MotionAmount.Still, BUG-64b) or the knob is 0 or less.</summary>
    public static float Seconds(DeskViewTuning tuning, bool still) =>
        still || tuning.seconds <= 0f ? 0f : tuning.seconds;

    /// <summary>The overlay canvas' rows at <paramref name="aspect"/> (its scaler expands from the reference: at least 1080 rows, more on a screen narrower than 16:9).</summary>
    public static float CanvasRows(float aspect) =>
        aspect > 0f ? Math.Max(ReferenceHeight, ReferenceWidth / aspect) : ReferenceHeight;

    /// <summary>The reading solve's top margin at <paramref name="aspect"/>: the knob's top band of chrome as a share of the half screen (0..1).</summary>
    public static float TopMargin(DeskViewTuning tuning, float aspect) =>
        Math.Min(1f, Math.Max(0f, tuning.topBand) / (CanvasRows(aspect) / 2f));

    /// <summary>
    /// What the reading pose frames, as points in the desk's frame (x metres
    /// right, y metres away from the chair, z metres above the desk): the
    /// <paramref name="mat"/>'s corners on the desk and at the papers' lift;
    /// and each of the knobs' homes found in <paramref name="homes"/>, its
    /// footprint's top and bottom corners: a home beside the mat (wholly left
    /// or right of it) only by the strip <see cref="DeskViewTuning.sideReach"/>
    /// deep along its edge nearest the mat unless the knobs frame it whole
    /// (<see cref="DeskViewTuning.whole"/>), any other home whole.
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
            if (!IsWhole(tuning, id))
            {
                if (x0 >= matRight)
                    x1 = Math.Min(x1, x0 + reach);
                else if (x1 <= matLeft)
                    x0 = Math.Max(x0, x1 - reach);
            }
            Box(points, x0, x1, f.CentreY - f.Height / 2f, f.CentreY + f.Height / 2f, Math.Max(0f, home.height));
        }
        return points;
    }

    /// <summary>True when the knobs frame the home <paramref name="id"/> whole wherever it stands (<see cref="DeskViewTuning.whole"/>).</summary>
    public static bool IsWhole(DeskViewTuning tuning, string id) => tuning.whole != null && Array.IndexOf(tuning.whole, id) >= 0;

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
