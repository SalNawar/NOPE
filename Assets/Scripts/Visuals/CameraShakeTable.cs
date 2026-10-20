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
/// The game feel's shake table (GAME_FEEL_BIBLE §1.3; CAM owns every camera
/// shake cell): tiny, directional, only on heavy impacts, each capped in
/// pixels at 1080p so the QA camera logger can hold it to its row. The
/// callers' knobs (MotionKnobs.shakeStamp, shakeCitation, shakeBreach) pick
/// the row (<see cref="Pick"/>); the row sets the size, length and
/// direction, scaled by the Motion intensity, none under Reduced Motion
/// (CameraFeel). Pure.
/// </summary>
public static class CameraShakeTable
{
    /// <summary>The share of a row's cap a shake aims for, so the measured peak (the shape's overshoot, the blend under it) stays under the cap.</summary>
    public const float Headroom = 0.8f;

    /// <summary>How deep the QA logger's reference points sit in front of the camera (metres; QaCameraMoves.ReferenceDepth): the size of a move in pixels is measured there.</summary>
    public const float ReferenceDepth = 2f;

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
    /// The row a shake of <paramref name="force"/> asks for, read against the
    /// callers' knobs: a rumble is the breach, or the destroyed timeline above
    /// the breach's force; a bump is the stamp up to halfway to the
    /// citation's force, the CITED slam above it.
    /// </summary>
    public static CameraShakeRow Pick(float force, bool rumble, float stampForce, float citationForce, float breachForce)
    {
        if (rumble)
            return Row(force > breachForce * 1.001f ? CameraShakeMoment.Destroyed : CameraShakeMoment.Breach);
        return Row(force <= (stampForce + citationForce) / 2f ? CameraShakeMoment.Stamp : CameraShakeMoment.Cited);
    }

    /// <summary>
    /// The camera's sideways move (metres) that shifts the picture of things
    /// <see cref="ReferenceDepth"/> ahead by <paramref name="px"/> pixels at
    /// 1080p through a lens of vertical field of view
    /// <paramref name="fovDegrees"/>.
    /// </summary>
    public static float Metres(float px, float fovDegrees)
    {
        double tan = Math.Tan(Math.Max(1f, Math.Min(179f, fovDegrees)) * Math.PI / 360.0);
        return (float)(Math.Max(0f, px) / 540.0 * ReferenceDepth * tan);
    }
}
