#if UNITY_EDITOR || TIMEDESK_QA
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>One logged frame of the watched camera: when, and how far the picture moved since the previous frame.</summary>
public readonly struct QaCameraFrame
{
    /// <summary>Time.frameCount of the frame.</summary>
    public readonly int Frame;

    /// <summary>Unscaled time of the frame, seconds (the tags' clock).</summary>
    public readonly float Time;

    /// <summary>How far the picture moved since the previous frame, in pixels at 1080p (reference points at the desk's depth).</summary>
    public readonly float StepPx;

    /// <summary>A frame at <paramref name="frame"/> and <paramref name="time"/> whose picture moved <paramref name="stepPx"/>.</summary>
    public QaCameraFrame(int frame, float time, float stepPx)
    {
        Frame = frame;
        Time = time;
        StepPx = stepPx;
    }
}

/// <summary>One cause window: <see cref="QaSignals.TagCamera"/>'s cause, from its call to its end, and its cap.</summary>
public sealed class QaCameraTag
{
    /// <summary>The tagged cause.</summary>
    public CameraMoveCause Cause;

    /// <summary>Unscaled time of the tag.</summary>
    public float Start;

    /// <summary>Unscaled time the window ends.</summary>
    public float End;

    /// <summary>The cap on the move, px at 1080p from the pose at <see cref="Start"/> (0: none).</summary>
    public float CapPx;

    /// <summary>The largest offset measured from the pose at <see cref="Start"/> while the window was open, px at 1080p.</summary>
    public float MaxOffsetPx;
}

/// <summary>What the camera logger found: every move attributed to a cause, or not.</summary>
public sealed class QaCameraReport
{
    /// <summary>Frames whose picture moved.</summary>
    public int MovedFrames;

    /// <summary>The moving frames no cause covered (Time.frameCount): each is a gate failure.</summary>
    public readonly List<int> UntaggedFrames = new List<int>();

    /// <summary>Moving frames per cause (a frame under two causes counts for both).</summary>
    public readonly Dictionary<CameraMoveCause, int> FramesByCause = new Dictionary<CameraMoveCause, int>();

    /// <summary>Tags whose measured offset went over their cap.</summary>
    public readonly List<QaCameraTag> OverCap = new List<QaCameraTag>();

    /// <summary>True when every move had a cause and no capped move went over its cap.</summary>
    public bool Pass => UntaggedFrames.Count == 0 && OverCap.Count == 0;

    /// <summary>One line: the counts the gate reports.</summary>
    public string Summary() =>
        $"moved {MovedFrames} frames; untagged {UntaggedFrames.Count}" +
        (UntaggedFrames.Count > 0 ? $" (first at frame {UntaggedFrames[0]})" : string.Empty) +
        "; by cause " + string.Join(", ", FramesByCause.OrderBy(p => p.Key).Select(p => $"{p.Key} {p.Value}")) +
        $"; over cap {OverCap.Count}" + string.Concat(OverCap.Select(t => $" [{t.Cause} {t.MaxOffsetPx:0.0} px > {t.CapPx:0.0} px at {t.Start:0.00} s]"));
}

/// <summary>
/// The camera logger's rule (GAME_FEEL_BIBLE G9, FIX_PLAN_v2 P0-B delta):
/// the camera moves only for a tagged cause (a view key, the D1 drop tilt,
/// the D6 slide-in focus, an impulse shake), and an impulse stays under its
/// pixel cap. Pure, so the rule is unit-tested outside Unity
/// (QaCameraMovesTests); CameraMoveLog measures and feeds it.
/// </summary>
public static class QaCameraMoves
{
    /// <summary>A frame moved when its picture shifted more than this, px at 1080p (below it is float noise).</summary>
    public const float MoveThresholdPx = 0.1f;

    /// <summary>A window covers this long past its end (a blend's last settle frame), seconds.</summary>
    public const float GraceSeconds = 0.1f;

    /// <summary>Attributes each moving frame of <paramref name="frames"/> to the <paramref name="tags"/> open at its time.</summary>
    public static QaCameraReport Classify(IEnumerable<QaCameraFrame> frames, IReadOnlyList<QaCameraTag> tags)
    {
        var report = new QaCameraReport();
        foreach (QaCameraFrame f in frames)
        {
            if (f.StepPx <= MoveThresholdPx)
                continue;
            report.MovedFrames++;
            bool any = false;
            foreach (CameraMoveCause cause in tags.Where(t => Covers(t, f.Time)).Select(t => t.Cause).Distinct())
            {
                any = true;
                report.FramesByCause[cause] = report.FramesByCause.TryGetValue(cause, out int n) ? n + 1 : 1;
            }
            if (!any)
                report.UntaggedFrames.Add(f.Frame);
        }
        foreach (QaCameraTag t in tags)
            if (t.CapPx > 0f && t.MaxOffsetPx > t.CapPx)
                report.OverCap.Add(t);
        return report;
    }

    /// <summary>True when <paramref name="tag"/>'s window (plus the grace) holds <paramref name="time"/>.</summary>
    public static bool Covers(QaCameraTag tag, float time) => time >= tag.Start - 1e-4f && time <= tag.End + GraceSeconds;

    /// <summary><paramref name="px"/> measured on a screen <paramref name="screenHeight"/> pixels tall, in pixels at 1080p.</summary>
    public static float To1080(float px, float screenHeight) => screenHeight > 0f ? px * 1080f / screenHeight : px;

    /// <summary>The depth of the reference points a picture shift is measured at, metres (about the desk's distance).</summary>
    public const float ReferenceDepth = 2f;

    /// <summary>The reference points, in normalised screen coordinates (-1..1): the centre and four points 60% out.</summary>
    private static readonly Vector2[] ReferencePoints =
        { Vector2.zero, new Vector2(-0.6f, -0.6f), new Vector2(0.6f, -0.6f), new Vector2(-0.6f, 0.6f), new Vector2(0.6f, 0.6f) };

    /// <summary>
    /// How far the picture moves from pose <paramref name="from"/> to pose
    /// <paramref name="to"/>, in pixels at 1080p: the mean screen distance of
    /// five points at <see cref="ReferenceDepth"/> in front of
    /// <paramref name="from"/> (the centre and four points 60% out), seen
    /// through both. A rotation, a move and a zoom all count.
    /// </summary>
    public static float ShiftPx(QaCameraPose from, QaCameraPose to)
    {
        float sum = 0f;
        foreach (Vector2 ndc in ReferencePoints)
        {
            Vector3 world = from.Unproject(ndc, ReferenceDepth);
            Vector2 a = from.To1080(ndc);
            Vector2 b = to.To1080(to.Project(world));
            sum += (b - a).magnitude;
        }
        return sum / ReferencePoints.Length;
    }
}

/// <summary>A camera's pose and lens, enough to project points (pure; see <see cref="QaCameraMoves.ShiftPx"/>).</summary>
public readonly struct QaCameraPose
{
    /// <summary>World position.</summary>
    public readonly Vector3 Position;

    /// <summary>World rotation.</summary>
    public readonly Quaternion Rotation;

    /// <summary>Vertical field of view, degrees (perspective).</summary>
    public readonly float FieldOfView;

    /// <summary>Width over height.</summary>
    public readonly float Aspect;

    /// <summary>True for an orthographic camera.</summary>
    public readonly bool Orthographic;

    /// <summary>Half the view's height in world units (orthographic).</summary>
    public readonly float OrthographicSize;

    /// <summary>A pose from its parts.</summary>
    public QaCameraPose(Vector3 position, Quaternion rotation, float fieldOfView, float aspect, bool orthographic = false, float orthographicSize = 5f)
    {
        Position = position;
        Rotation = rotation;
        FieldOfView = fieldOfView;
        Aspect = aspect <= 0f ? 1f : aspect;
        Orthographic = orthographic;
        OrthographicSize = orthographicSize;
    }

    private float HalfHeightAt(float depth) => Orthographic ? OrthographicSize : depth * (float)Math.Tan(FieldOfView * 0.5f * Math.PI / 180.0);

    /// <summary>The world point at normalised screen <paramref name="ndc"/> and <paramref name="depth"/> metres in front.</summary>
    public Vector3 Unproject(Vector2 ndc, float depth)
    {
        float h = HalfHeightAt(depth);
        return Position + Rotation * new Vector3(ndc.x * h * Aspect, ndc.y * h, depth);
    }

    /// <summary>The normalised screen position (-1..1) of <paramref name="world"/> (behind the camera: far off screen).</summary>
    public Vector2 Project(Vector3 world)
    {
        // The conjugate is the inverse of a unit rotation (and needs no engine call).
        Vector3 local = new Quaternion(-Rotation.x, -Rotation.y, -Rotation.z, Rotation.w) * (world - Position);
        if (local.z <= 1e-4f)
            return new Vector2(1e4f, 1e4f);
        float h = HalfHeightAt(local.z);
        return new Vector2(local.x / (h * Aspect), local.y / h);
    }

    /// <summary><paramref name="ndc"/> in pixels from the centre of a 1080-pixel-tall picture of this aspect.</summary>
    public Vector2 To1080(Vector2 ndc) => new Vector2(ndc.x * 540f * Aspect, ndc.y * 540f);
}
#endif
