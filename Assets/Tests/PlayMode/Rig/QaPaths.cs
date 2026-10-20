#if UNITY_EDITOR || TIMEDESK_QA
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>The player drag paths of FIX_PLAN's capture vocabulary (P-FAST ... P-ONTO), plus a plain eased drag.</summary>
public enum QaPathKind
{
    /// <summary>An eased drag at an everyday speed (about 600 px/s).</summary>
    Plain,
    /// <summary>P-FAST: at least 2,000 px/s.</summary>
    Fast,
    /// <summary>P-SLOW: at most 150 px/s.</summary>
    Slow,
    /// <summary>P-WOBBLE: ±30 px of sideways sine on the way.</summary>
    Wobble,
    /// <summary>P-DIAG: along the 45-degree diagonal through the start that is nearest the target.</summary>
    Diag,
    /// <summary>P-FLICK: released while still moving at about 1,500 px/s.</summary>
    Flick,
    /// <summary>P-EDGE: released 5 px inside the screen edge nearest the target.</summary>
    Edge,
    /// <summary>P-OFF: released outside the window, past the edge nearest the target.</summary>
    Off,
    /// <summary>P-ONTO: released onto a target (the folder, another paper, a machine, a prop); the caller passes its point.</summary>
    Onto,
}

/// <summary>One frame of a scripted pointer path: where the pointer is and whether the left button is held.</summary>
public readonly struct QaPathSample
{
    /// <summary>The pointer's screen position (pixels, origin bottom-left, as the Input System's).</summary>
    public readonly Vector2 Position;

    /// <summary>True while the left button is held on this frame.</summary>
    public readonly bool Down;

    /// <summary>A sample at <paramref name="position"/>, held or not.</summary>
    public QaPathSample(Vector2 position, bool down)
    {
        Position = position;
        Down = down;
    }
}

/// <summary>A whole scripted drag: one sample per frame at 30 fps, with the press and release frames named.</summary>
public sealed class QaPath
{
    /// <summary>The path's kind.</summary>
    public QaPathKind Kind { get; }

    /// <summary>One sample per frame: hover, press, move, (hold), release, settle.</summary>
    public IReadOnlyList<QaPathSample> Samples { get; }

    /// <summary>The index of the first sample with the button down.</summary>
    public int PressIndex { get; }

    /// <summary>The index of the first sample with the button up again (the release frame).</summary>
    public int ReleaseIndex { get; }

    /// <summary>Where the pointer is on the release frame: the end the drag is judged against.</summary>
    public Vector2 End => Samples[ReleaseIndex].Position;

    /// <summary>Where the button went down.</summary>
    public Vector2 Start => Samples[PressIndex].Position;

    /// <summary>A path of <paramref name="samples"/> (the press and release are found in them).</summary>
    public QaPath(QaPathKind kind, List<QaPathSample> samples)
    {
        Kind = kind;
        Samples = samples;
        PressIndex = samples.FindIndex(s => s.Down);
        ReleaseIndex = PressIndex < 0 ? -1 : samples.FindIndex(PressIndex, s => !s.Down);
        if (PressIndex < 0 || ReleaseIndex < 0)
            throw new ArgumentException("A drag path needs a press and a release.");
    }

    /// <summary>The pointer's speed in px/s between sample <paramref name="i"/> - 1 and <paramref name="i"/> (at <see cref="QaPaths.Fps"/>).</summary>
    public float SpeedAt(int i) => i <= 0 ? 0f : (Samples[i].Position - Samples[i - 1].Position).magnitude * QaPaths.Fps;
}

/// <summary>
/// Builds the P-* drag paths (FIX_PLAN "Player drag paths") as one sample per
/// frame at 30 fps, for the virtual mouse (QaInput) to play through the Input
/// System. Pure: no engine state is read, so the shapes are unit-tested
/// outside Unity (QaPathsTests). Every path hovers first, presses, holds two
/// frames (a real grab), moves, and releases; only P-FLICK and P-OFF release
/// without a still hold at the end.
/// </summary>
public static class QaPaths
{
    /// <summary>The capture rate the paths are timed for (FIX_PLAN SEQ capture).</summary>
    public const float Fps = 30f;

    /// <summary>Frames of hover before the press (the hover state shows before the grab).</summary>
    public const int HoverFrames = 3;

    /// <summary>Frames the button is held still after the press, before moving.</summary>
    public const int GrabHoldFrames = 2;

    /// <summary>Frames held still at the end before the release (not for P-FLICK or P-OFF).</summary>
    public const int EndHoldFrames = 3;

    /// <summary>Frames sampled after the release (the drop settles on video).</summary>
    public const int SettleFrames = 3;

    /// <summary>P-FAST's speed, px/s (the vocabulary: ≥2,000).</summary>
    public const float FastSpeed = 2400f;

    /// <summary>P-SLOW's speed, px/s (the vocabulary: ≤150).</summary>
    public const float SlowSpeed = 120f;

    /// <summary>The plain drag's average speed, px/s.</summary>
    public const float PlainSpeed = 600f;

    /// <summary>P-FLICK's speed at the release, px/s (the vocabulary: ~1,500).</summary>
    public const float FlickSpeed = 1500f;

    /// <summary>P-WOBBLE's sideways amplitude, px (the vocabulary: ±30).</summary>
    public const float WobbleAmplitude = 30f;

    /// <summary>P-WOBBLE's sine cycles over the drag.</summary>
    public const float WobbleCycles = 1.5f;

    /// <summary>P-EDGE's release distance from the edge, px (the vocabulary: 5).</summary>
    public const float EdgeInset = 5f;

    /// <summary>How far past the edge P-OFF releases, px.</summary>
    public const float OffOvershoot = 40f;

    /// <summary>
    /// The drag of <paramref name="kind"/> from <paramref name="from"/>
    /// towards <paramref name="to"/> on a screen of <paramref name="screen"/>
    /// pixels. P-DIAG, P-EDGE and P-OFF move the end as their names say; the
    /// rest end exactly on <paramref name="to"/>.
    /// </summary>
    public static QaPath Drag(QaPathKind kind, Vector2 from, Vector2 to, Vector2 screen)
    {
        Vector2 end = EndFor(kind, from, to, screen);
        var samples = new List<QaPathSample>();
        for (int i = 0; i < HoverFrames; i++)
            samples.Add(new QaPathSample(from, false));
        for (int i = 0; i <= GrabHoldFrames; i++)
            samples.Add(new QaPathSample(from, true));

        float distance = (end - from).magnitude;
        switch (kind)
        {
            case QaPathKind.Fast:
                Linear(samples, from, end, Frames(distance, FastSpeed));
                break;
            case QaPathKind.Slow:
                Linear(samples, from, end, Frames(distance, SlowSpeed));
                break;
            case QaPathKind.Flick:
                Flick(samples, from, end);
                break;
            case QaPathKind.Wobble:
                Eased(samples, from, end, Frames(distance, PlainSpeed) + 6, WobbleAmplitude);
                break;
            default:
                Eased(samples, from, end, Frames(distance, PlainSpeed), 0f);
                break;
        }

        bool hold = kind != QaPathKind.Flick && kind != QaPathKind.Off;
        if (hold)
            for (int i = 0; i < EndHoldFrames; i++)
                samples.Add(new QaPathSample(end, true));
        for (int i = 0; i <= SettleFrames; i++)
            samples.Add(new QaPathSample(end, false));
        return new QaPath(kind, samples);
    }

    /// <summary>A click: hover, press, hold <paramref name="holdFrames"/>, release, settle, all at <paramref name="at"/>.</summary>
    public static QaPath Click(Vector2 at, int holdFrames = 2)
    {
        var samples = new List<QaPathSample>();
        for (int i = 0; i < HoverFrames; i++)
            samples.Add(new QaPathSample(at, false));
        for (int i = 0; i < Math.Max(1, holdFrames); i++)
            samples.Add(new QaPathSample(at, true));
        for (int i = 0; i <= SettleFrames; i++)
            samples.Add(new QaPathSample(at, false));
        return new QaPath(QaPathKind.Plain, samples);
    }

    /// <summary>A hover move with the button up from <paramref name="from"/> to <paramref name="to"/> over <paramref name="frames"/> frames (no press).</summary>
    public static List<Vector2> Move(Vector2 from, Vector2 to, int frames)
    {
        var points = new List<Vector2>();
        frames = Math.Max(1, frames);
        for (int i = 1; i <= frames; i++)
            points.Add(Vector2.Lerp(from, to, Smooth(i / (float)frames)));
        return points;
    }

    /// <summary>Where a drag of <paramref name="kind"/> is released, given its start, its target and the screen.</summary>
    public static Vector2 EndFor(QaPathKind kind, Vector2 from, Vector2 to, Vector2 screen)
    {
        switch (kind)
        {
            case QaPathKind.Diag:
            {
                Vector2 d = to - from;
                float length = (Math.Abs(d.x) + Math.Abs(d.y)) * 0.5f;
                return from + new Vector2(d.x < 0f ? -length : length, d.y < 0f ? -length : length);
            }
            case QaPathKind.Edge:
                return ToEdge(to, screen, EdgeInset);
            case QaPathKind.Off:
                return ToEdge(to, screen, -OffOvershoot);
            default:
                return to;
        }
    }

    /// <summary><paramref name="p"/> moved onto the screen edge nearest it, <paramref name="inset"/> px inside (negative: outside).</summary>
    private static Vector2 ToEdge(Vector2 p, Vector2 screen, float inset)
    {
        float left = p.x, right = screen.x - p.x, bottom = p.y, top = screen.y - p.y;
        float nearest = Math.Min(Math.Min(left, right), Math.Min(bottom, top));
        if (nearest == left) return new Vector2(inset, Clamp(p.y, screen.y));
        if (nearest == right) return new Vector2(screen.x - inset, Clamp(p.y, screen.y));
        if (nearest == bottom) return new Vector2(Clamp(p.x, screen.x), inset);
        return new Vector2(Clamp(p.x, screen.x), screen.y - inset);
    }

    private static float Clamp(float v, float max) => Math.Max(0f, Math.Min(max, v));

    /// <summary>The frames needed to cover <paramref name="distance"/> px at <paramref name="speed"/> px/s (at least 2).</summary>
    private static int Frames(float distance, float speed) => Math.Max(2, (int)Math.Ceiling(distance / (speed / Fps)));

    /// <summary>Constant speed from <paramref name="from"/> to <paramref name="to"/> in <paramref name="frames"/> steps, button held.</summary>
    private static void Linear(List<QaPathSample> samples, Vector2 from, Vector2 to, int frames)
    {
        for (int i = 1; i <= frames; i++)
            samples.Add(new QaPathSample(Vector2.Lerp(from, to, i / (float)frames), true));
    }

    /// <summary>A smoothstep path with an optional sideways sine (ending on the line), button held.</summary>
    private static void Eased(List<QaPathSample> samples, Vector2 from, Vector2 to, int frames, float wobble)
    {
        Vector2 d = to - from;
        Vector2 side = d.sqrMagnitude > 1e-6f ? new Vector2(-d.y, d.x).normalized : Vector2.up;
        for (int i = 1; i <= frames; i++)
        {
            float t = i / (float)frames;
            float sway = wobble * (float)Math.Sin(t * Math.PI * 2.0 * WobbleCycles);
            samples.Add(new QaPathSample(Vector2.Lerp(from, to, Smooth(t)) + side * sway, true));
        }
    }

    /// <summary>
    /// Accelerates from rest to <see cref="FlickSpeed"/> and stops one
    /// <see cref="FlickSpeed"/> step short of <paramref name="to"/>, so the
    /// release frame (added by the caller at <paramref name="to"/>) still
    /// moves at full speed.
    /// </summary>
    private static void Flick(List<QaPathSample> samples, Vector2 from, Vector2 to)
    {
        float distance = (to - from).magnitude;
        float step = FlickSpeed / Fps;
        float ramp = Math.Max(0f, distance - step);
        Vector2 dir = distance > 1e-4f ? (to - from) / distance : Vector2.right;
        // s(t) = ramp * t^2 over n frames: its last step is about 2 * ramp / n, which is `step` for n = 2 * ramp / step.
        int frames = Math.Max(2, (int)Math.Round(2.0 * ramp / step));
        for (int i = 1; i <= frames; i++)
        {
            float t = i / (float)frames;
            samples.Add(new QaPathSample(from + dir * (ramp * t * t), true));
        }
    }

    /// <summary>Smoothstep.</summary>
    private static float Smooth(float t) => t * t * (3f - 2f * t);
}
#endif
