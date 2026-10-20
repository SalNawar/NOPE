using System;

/// <summary>
/// The shape a moving paper flexes into (Saleh's 1018a playtest: "all
/// documents are stiff"; more swing and lag on the corners, a real bend along
/// the drag, a flutter on fast moves, a settle with a soft wobble). A smooth
/// height field over the paper's face, from four motions PaperPhysics drives:
/// a bow along its travel (its ends droop away from the viewer, below the
/// middle the hand holds, the trailing end more than the leading one by
/// <see cref="Lag"/>; negative, they curl up toward the viewer, as a dropped
/// sheet's wobble does), a twist (opposite corners flap opposite ways as it
/// swings), and a ripple running from the leading edge back along it on a
/// fast move (<see cref="Flutter"/>, strongest at the trailing edge). One
/// formula with its slopes: the cel shader's PaperBend.hlsl draws the face and
/// its sides with it (vertex by vertex, its tone from the bent normal), and
/// the paper moves each printed part (a text, the photo, the seal, a stamp) to
/// the bent face under its centre, turned to its slope; the paper rises by
/// <see cref="Lowest"/> so nothing bends through what lies under it. Flat at
/// rest, exactly, so the text reads as printed. Engine-free.
/// <para>
/// Coordinates: X and Y run -1 to 1 across the face (X right, Y up the
/// page), for a face <c>width</c> by <c>height</c> metres; depths are metres,
/// positive away from the viewer (into the desk).
/// </para>
/// </summary>
public readonly struct PaperBend : IEquatable<PaperBend>
{
    /// <summary>The way the paper travels in its face's plane (a unit vector: X right, Y up the page).</summary>
    public readonly float DirX, DirY;

    /// <summary>The bow along the travel (metres at the ends: positive droops away from the viewer, negative curls up).</summary>
    public readonly float Bow;

    /// <summary>How much more the trailing end bows than the leading one (0 even, 1 the leading end flat).</summary>
    public readonly float Lag;

    /// <summary>The twist (metres at the corners): the corners ahead-right and behind-left go one way, the other two the other.</summary>
    public readonly float Twist;

    /// <summary>The ripple's height at the trailing edge (metres), running back along the travel.</summary>
    public readonly float Flutter;

    /// <summary>The ripple's phase (radians: it runs as the phase grows).</summary>
    public readonly float Phase;

    /// <summary>How many half waves the ripple lays along the paper.</summary>
    public readonly float Waves;

    /// <summary>A bend: the travel (normalised here; none: across X), its bow, lag, twist and ripple.</summary>
    public PaperBend(float dirX, float dirY, float bow, float lag, float twist, float flutter, float phase, float waves)
    {
        float length = (float)Math.Sqrt(dirX * dirX + dirY * dirY);
        DirX = length > 1e-6f ? dirX / length : 1f;
        DirY = length > 1e-6f ? dirY / length : 0f;
        Bow = bow;
        Lag = lag < 0f ? 0f : lag > 1f ? 1f : lag;
        Twist = twist;
        Flutter = flutter;
        Phase = phase;
        Waves = waves;
    }

    /// <summary>No bend: the paper lies flat.</summary>
    public static PaperBend Flat => new PaperBend(1f, 0f, 0f, 0f, 0f, 0f, 0f, 0f);

    /// <summary>True when nothing bends (the printed parts lie where they were printed).</summary>
    public bool IsFlat => Bow == 0f && Twist == 0f && Flutter == 0f;

    /// <summary>
    /// Where (<paramref name="x"/>, <paramref name="y"/>) lies along the
    /// travel (s, -1 the trailing end to 1 the leading one) and across it (t),
    /// on a face of <paramref name="width"/> by <paramref name="height"/>,
    /// and how s and t change with X and Y.
    /// </summary>
    private void Frame(float x, float y, float width, float height, out float s, out float t, out float sx, out float sy, out float tx, out float ty)
    {
        float along = Math.Abs(DirX) * width + Math.Abs(DirY) * height;
        float across = Math.Abs(DirY) * width + Math.Abs(DirX) * height;
        along = along > 1e-6f ? along : 1e-6f;
        across = across > 1e-6f ? across : 1e-6f;
        sx = DirX * width / along;
        sy = DirY * height / along;
        tx = -DirY * width / across;
        ty = DirX * height / across;
        s = x * sx + y * sy;
        t = x * tx + y * ty;
    }

    /// <summary>How far the face lies from flat at (<paramref name="x"/>, <paramref name="y"/>) (metres, positive away from the viewer).</summary>
    public float Depth(float x, float y, float width, float height)
    {
        if (IsFlat)
            return 0f;
        Frame(x, y, width, height, out float s, out float t, out _, out _, out _, out _);
        float w = (1f - s) * 0.5f;
        return Bow * s * s * (1f - Lag * s) + Twist * s * t + Flutter * w * w * (float)Math.Sin(Math.PI * Waves * s - Phase);
    }

    /// <summary>The face's slopes at (<paramref name="x"/>, <paramref name="y"/>): metres of depth per metre across (along the face's x) and up (along its y).</summary>
    public (float dx, float dy) Slope(float x, float y, float width, float height)
    {
        if (IsFlat)
            return (0f, 0f);
        Frame(x, y, width, height, out float s, out float t, out float sx, out float sy, out float tx, out float ty);
        float w = (1f - s) * 0.5f;
        float angle = (float)(Math.PI * Waves * s - Phase);
        float ds = Bow * (2f * s - 3f * Lag * s * s) + Twist * t
                   + Flutter * (-w * (float)Math.Sin(angle) + w * w * (float)(Math.Cos(angle) * Math.PI * Waves));
        float dt = Twist * s;
        // X runs -1 to 1 over the width: a metre across is 2 / width of X
        float dX = ds * sx + dt * tx, dY = ds * sy + dt * ty;
        return (dX * 2f / Math.Max(width, 1e-6f), dY * 2f / Math.Max(height, 1e-6f));
    }

    /// <summary>
    /// How far the bent face rises toward the viewer past the plane a flat
    /// printed part lies in (metres, at least 0): the part centred at
    /// (<paramref name="x"/>, <paramref name="y"/>) (-1 to 1 across the face)
    /// and <paramref name="halfWidth"/> by <paramref name="halfHeight"/>
    /// metres, laid on the face's tangent plane under its centre. The paper
    /// lifts the part by it, so the curling face never covers its print
    /// (BUG-12a: rule lines vanished under the landing curl).
    /// </summary>
    public float Bulge(float x, float y, float halfWidth, float halfHeight, float width, float height)
    {
        if (IsFlat || width <= 0f || height <= 0f)
            return 0f;
        float centre = Depth(x, y, width, height);
        (float dx, float dy) = Slope(x, y, width, height);
        float most = 0f;
        const int steps = 4;
        for (int i = 0; i <= steps; i++)
            for (int j = 0; j <= steps; j++)
            {
                float ox = halfWidth * (-1f + 2f * i / steps), oy = halfHeight * (-1f + 2f * j / steps);
                float sx = Clamp(x + ox * 2f / width), sy = Clamp(y + oy * 2f / height);
                float plane = centre + dx * ox + dy * oy;
                most = Math.Max(most, plane - Depth(sx, sy, width, height));
            }
        return most;
    }

    /// <summary>How much a paper of <paramref name="kind"/> bends (its share of the physics' bend): a sheet's <paramref name="sheetFlex"/>, a booklet's <paramref name="bookletFlex"/>, and a card none at all, whatever any knob says (Saleh's D3: "other documents other than solid id cards should feel a bit like paper").</summary>
    public static float FlexOf(PaperKind kind, float sheetFlex, float bookletFlex) =>
        kind == PaperKind.Card ? 0f : kind == PaperKind.Booklet ? bookletFlex : sheetFlex;

    private static float Clamp(float v) => v < -1f ? -1f : v > 1f ? 1f : v;

    /// <summary>The most the face goes away from the viewer anywhere (metres, at least 0): the paper rises by it so its bend never passes through what lies under it.</summary>
    public float Lowest(float width, float height)
    {
        if (IsFlat)
            return 0f;
        float most = 0f;
        const int steps = 8;
        for (int i = 0; i <= steps; i++)
            for (int j = 0; j <= steps; j++)
                most = Math.Max(most, Depth(-1f + 2f * i / steps, -1f + 2f * j / steps, width, height));
        return most;
    }

    /// <inheritdoc />
    public bool Equals(PaperBend other) => DirX == other.DirX && DirY == other.DirY && Bow == other.Bow && Lag == other.Lag &&
                                           Twist == other.Twist && Flutter == other.Flutter && Phase == other.Phase && Waves == other.Waves;

    /// <inheritdoc />
    public override bool Equals(object obj) => obj is PaperBend other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(DirX, DirY, Bow, Lag, Twist, Flutter, Phase, Waves);
}
