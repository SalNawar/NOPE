using System;

/// <summary>The kinds of camera shake on the game feel's shake table (GAME_FEEL_BIBLE §1.3). Append-only: the numbers are written into QA logs.</summary>
public enum CameraShakeMoment
{
    /// <summary>A dater lands on the passport.</summary>
    Stamp = 0,
    /// <summary>DETAIN pressed with a traveller present.</summary>
    Detain = 1,
    /// <summary>The switchboard's breaker snaps home (PORTALS).</summary>
    Breaker = 2,
    /// <summary>The CITED stamp on the slip.</summary>
    Cited = 3,
    /// <summary>A batch send of 3 or more (hall view only).</summary>
    BatchSend = 4,
    /// <summary>The Helix River breaches its warning band.</summary>
    Breach = 5,
    /// <summary>The timeline is destroyed (fired).</summary>
    Destroyed = 6,
}

/// <summary>One row of the shake table: the most the picture may move, for how long, which way, and whether it rumbles.</summary>
public readonly struct CameraShakeRow
{
    /// <summary>A row of <paramref name="moment"/>: at most <paramref name="capPx"/> pixels at 1080p for <paramref name="seconds"/>, along (<paramref name="x"/>, <paramref name="y"/>) on the screen, a rumble when <paramref name="rumble"/>.</summary>
    public CameraShakeRow(CameraShakeMoment moment, float capPx, float seconds, float x, float y, bool rumble)
    {
        Moment = moment;
        CapPx = capPx;
        Seconds = seconds;
        X = x;
        Y = y;
        Rumble = rumble;
    }

    /// <summary>The moment.</summary>
    public CameraShakeMoment Moment { get; }

    /// <summary>The most the picture may move from where it was, in pixels at 1080p (the QA camera logger's cap).</summary>
    public float CapPx { get; }

    /// <summary>The shake's length (unscaled seconds).</summary>
    public float Seconds { get; }

    /// <summary>The force's direction on the screen (camera space, unit length; down is (0, -1)).</summary>
    public float X { get; }

    /// <inheritdoc cref="X"/>
    public float Y { get; }

    /// <summary>True for a rumble (an oscillation), false for a single bump.</summary>
    public bool Rumble { get; }
}

/// <summary>
/// The knobs the shake calls that predate the table are built from
/// (MotionKnobs: the stamp's and the breach's force and seconds;
/// FeelDirector.Hit's strengths for the DETAIN press and the CITED slam), so
/// <see cref="CameraShakeTable.TryLegacy"/> can tell which moment a call is.
/// </summary>
public readonly struct CameraShakeKnobs
{
    /// <summary>The knobs as MotionKnobs holds them: shakeStamp, shakeStampSeconds, shakeBreach, shakeBreachSeconds, detainHit, citedHit.</summary>
    public CameraShakeKnobs(float stamp, float stampSeconds, float breach, float breachSeconds, float detainHit, float citedHit)
    {
        Stamp = stamp;
        StampSeconds = stampSeconds;
        Breach = breach;
        BreachSeconds = breachSeconds;
        DetainHit = detainHit;
        CitedHit = citedHit;
    }

    /// <summary>MotionKnobs.shakeStamp: FeelHit.Stamp's force.</summary>
    public float Stamp { get; }

    /// <summary>MotionKnobs.shakeStampSeconds: FeelHit.Stamp's seconds (FeelDirector.Hit's at strength 0).</summary>
    public float StampSeconds { get; }

    /// <summary>MotionKnobs.shakeBreach: FeelHit.Breach's force (FeelDirector.Hit's at strength 1).</summary>
    public float Breach { get; }

    /// <summary>MotionKnobs.shakeBreachSeconds: FeelHit.Breach's seconds (FeelDirector.Hit's at strength 1).</summary>
    public float BreachSeconds { get; }

    /// <summary>MotionKnobs.detainHit: the DETAIN press's FeelDirector.Hit strength.</summary>
    public float DetainHit { get; }

    /// <summary>MotionKnobs.citedHit: the CITED slam's FeelDirector.Hit strength.</summary>
    public float CitedHit { get; }
}

/// <summary>
/// The game feel's shake table (GAME_FEEL_BIBLE §1.3; CAM owns every camera
/// shake cell): tiny, directional, only on heavy impacts, each capped in
/// pixels at 1080p so the QA camera logger can hold it to its row; no other
/// moment shakes ("Everything else: 0"). A shake names its moment
/// (CameraFeel.Shake); the calls that predate the table are read by their
/// knobs (<see cref="TryLegacy"/>). The row sets the size, length and
/// direction, scaled by the Motion intensity, none under Reduced Motion
/// (CameraFeel). Pure.
/// </summary>
public static class CameraShakeTable
{
    /// <summary>The share of a row's cap a shake aims for, so the measured peak (the blend under it, a frame's rounding) stays under the cap.</summary>
    public const float Headroom = 0.8f;

    /// <summary>How deep the QA logger's reference points sit in front of the camera (metres; QaCameraMoves.ReferenceDepth): no shake is sized for anything deeper.</summary>
    public const float ReferenceDepth = 2f;

    /// <summary>
    /// The largest offset of Cinemachine 3's Bump impulse from its start, per
    /// unit of its velocity (the signal is the velocity times the shape's
    /// curve): its standard curve (keys (0, 0, -4.9), (0.2, 0, 8.25),
    /// (1, 0, -0.25), Hermite; CinemachineImpulseDefinition) dips 0.334
    /// against the direction at 11% of its length, then peaks at 0.9928 along
    /// it at 47%.
    /// </summary>
    public const float BumpPeak = 0.9928f;

    /// <summary>The largest offset of Cinemachine 3's Rumble impulse per unit of its velocity: its standard curve's keys are flat, the highest 1 at half its length.</summary>
    public const float RumblePeak = 1f;

    /// <summary>Two knob values this close are the same (the callers scale float knobs).</summary>
    private const float Same = 1e-4f;

    private static readonly float Diagonal = (float)Math.Sqrt(0.5);

    /// <summary>The rows, by moment.</summary>
    public static readonly CameraShakeRow[] Rows =
    {
        new CameraShakeRow(CameraShakeMoment.Stamp, 1.5f, 0.12f, 0f, -1f, false),
        new CameraShakeRow(CameraShakeMoment.Detain, 2.5f, 0.16f, 0f, -1f, false),
        new CameraShakeRow(CameraShakeMoment.Breaker, 2f, 0.15f, Diagonal, -Diagonal, false),
        new CameraShakeRow(CameraShakeMoment.Cited, 3f, 0.25f, 0f, -1f, false),
        new CameraShakeRow(CameraShakeMoment.BatchSend, 1.5f, 0.3f, 0f, -1f, true),
        new CameraShakeRow(CameraShakeMoment.Breach, 6f, 0.8f, 0f, -1f, true),
        new CameraShakeRow(CameraShakeMoment.Destroyed, 8f, 0.9f, 0f, -1f, true),
    };

    /// <summary>The row of <paramref name="moment"/>.</summary>
    public static CameraShakeRow Row(CameraShakeMoment moment)
    {
        foreach (CameraShakeRow row in Rows)
            if (row.Moment == moment)
                return row;
        return Rows[0];
    }

    /// <summary>
    /// The table moment of a shake call that predates the table (a force,
    /// its seconds and whether it rumbles, as the callers build them from
    /// MotionKnobs), read by its knobs: FeelHit.Stamp's shake (the stamp's
    /// force and seconds) is the stamp; FeelDirector.Hit at the DETAIN
    /// press's strength (the breach's force times detainHit, its seconds from
    /// the stamp's toward the breach's by it) is DETAIN; at the CITED slam's
    /// (citedHit) the CITED stamp on the slip; FeelHit.Breach's rumble the
    /// breach. Anything else has no row and does not shake: the citation's
    /// own shake at the verdict (the slip's slam carries the CITED moment,
    /// once), the approval's hand-back, the chip reader's insert and fault,
    /// any other strength (GAME_FEEL_BIBLE §1.3: "Everything else: 0").
    /// </summary>
    public static bool TryLegacy(float force, float seconds, bool rumble, CameraShakeKnobs knobs, out CameraShakeMoment moment)
    {
        moment = CameraShakeMoment.Stamp;
        if (!rumble && Near(force, knobs.Stamp) && Near(seconds, knobs.StampSeconds))
            return true;
        if (IsHit(force, seconds, rumble, knobs, knobs.DetainHit))
        {
            moment = CameraShakeMoment.Detain;
            return true;
        }
        if (IsHit(force, seconds, rumble, knobs, knobs.CitedHit))
        {
            moment = CameraShakeMoment.Cited;
            return true;
        }
        if (rumble && Near(force, knobs.Breach) && Near(seconds, knobs.BreachSeconds))
        {
            moment = CameraShakeMoment.Breach;
            return true;
        }
        return false;
    }

    /// <summary>True when (<paramref name="force"/>, <paramref name="seconds"/>, <paramref name="rumble"/>) is FeelDirector.Hit at <paramref name="strength"/> (above 0): the breach's force times it, its seconds from the stamp's toward the breach's by it, a rumble from 0.75.</summary>
    private static bool IsHit(float force, float seconds, bool rumble, CameraShakeKnobs knobs, float strength)
    {
        if (strength <= 0f)
            return false;
        float s = Math.Min(1f, strength);
        return rumble == (s >= 0.75f) && Near(force, knobs.Breach * s) &&
               Near(seconds, knobs.StampSeconds + (knobs.BreachSeconds - knobs.StampSeconds) * s);
    }

    private static bool Near(float a, float b) => Math.Abs(a - b) <= Same;

    /// <summary>
    /// The camera's sideways move (metres) that shifts the picture of things
    /// <paramref name="depth"/> metres ahead (at most
    /// <see cref="ReferenceDepth"/>, at least a centimetre) by
    /// <paramref name="px"/> pixels at 1080p through a lens of vertical field
    /// of view <paramref name="fovDegrees"/>. Nearer things shift more for
    /// the same move, so a shake is sized at the nearest work's depth.
    /// </summary>
    public static float Metres(float px, float fovDegrees, float depth = ReferenceDepth)
    {
        double tan = Math.Tan(Math.Max(1f, Math.Min(179f, fovDegrees)) * Math.PI / 360.0);
        double d = Math.Max(0.01, Math.Min(ReferenceDepth, depth));
        return (float)(Math.Max(0f, px) / 540.0 * d * tan);
    }

    /// <summary>
    /// The impulse velocity (metres) that draws <paramref name="row"/>: its
    /// cap less the headroom, scaled by the Motion intensity
    /// <paramref name="share"/> (0..1), as a camera move at
    /// <paramref name="depth"/> through <paramref name="fovDegrees"/>
    /// (<see cref="Metres"/>), over its impulse shape's peak
    /// (<see cref="BumpPeak"/>, <see cref="RumblePeak"/>), so the largest
    /// offset is that move.
    /// </summary>
    public static float Velocity(CameraShakeRow row, float share, float fovDegrees, float depth)
    {
        float px = row.CapPx * Headroom * Math.Max(0f, Math.Min(1f, share));
        return Metres(px, fovDegrees, depth) / (row.Rumble ? RumblePeak : BumpPeak);
    }
}
