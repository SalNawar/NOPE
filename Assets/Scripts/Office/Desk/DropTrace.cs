using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The rests of things let go on the desk, kept for the QA rig only (FOLD-01,
/// Saleh's playtest of 2026-10-28, point 4: "You move the folder and it snaps
/// somewhere else"). The rig's restTrace op turns it on and writes each entry
/// to the run's rests.tsv. Off (the default, and always in play), the desk
/// records nothing and writes no log. Each entry holds what was let go, its
/// footprint at the release, the area it must lie in, every keep-out with its
/// source and the chosen rest, all in the office view's frame on the desk
/// (DeskController.InViewFrame: x right, y away from the camera, metres from
/// the desk's centre), with the frame's origin and axes so the rig can project
/// each rectangle onto the screen.
/// </summary>
public sealed class DropTrace
{
    /// <summary>True while the QA rig records the rests (restTrace "on").</summary>
    public static bool On;

    /// <summary>The rests recorded since the rig last read them.</summary>
    public static readonly List<DropTrace> Entries = new List<DropTrace>();

    /// <summary>What was let go ("folder" or "paper").</summary>
    public string What;

    /// <summary>The frame it was let go on (Time.frameCount).</summary>
    public int Frame;

    /// <summary>Its footprint at the release (its final size) and the area it must lie wholly inside.</summary>
    public DeskRect Footprint, Area;

    /// <summary>Where its footprint's centre rests.</summary>
    public float RestX, RestY;

    /// <summary>The frame's origin (the desk's centre, at the height the thing's root lies) and its level axes.</summary>
    public Vector3 Origin, Right, Forward;

    /// <summary>The desk surface's height (the plane the area's view corners are taken on).</summary>
    public float SurfaceY;

    /// <summary>The surface's clamp area (where a drag may take a thing's centre).</summary>
    public DeskRect Clamp;

    /// <summary>The reading view's corners on the desk as the area took them: near left, near right, far left, far right (all 0 when the area did not take the view).</summary>
    public readonly (float x, float y)[] View = new (float, float)[4];

    /// <summary>Every keep-out, with where it came from and whether it moved the rest (a blocker) or was only listed.</summary>
    public readonly List<(string source, DeskRect rect, bool blocks)> KeepOut = new List<(string, DeskRect, bool)>();

    /// <summary>The world point of (<paramref name="x"/>, <paramref name="y"/>) in the frame, on the desk's top.</summary>
    public Vector3 World(float x, float y) => Origin + Right * x + Forward * y;

    /// <summary>How far the rest lies from the release (metres).</summary>
    public float Move => Mathf.Sqrt((RestX - Footprint.CentreX) * (RestX - Footprint.CentreX) + (RestY - Footprint.CentreY) * (RestY - Footprint.CentreY));

    /// <summary>Forgets every entry when play starts (the editor without a domain reload keeps statics).</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        On = false;
        Entries.Clear();
    }
}
