using System;

/// <summary>The shape a magnifier shows on the desk: the loupe's round glass, or a rectangle (the bar magnifier, the Fresnel sheet).</summary>
public enum LensShape
{
    /// <summary>A circle of the lens's half width.</summary>
    Round,

    /// <summary>A rectangle of the lens's half width and half height.</summary>
    Rectangle
}

/// <summary>
/// Reading a paper's microprint under the lens stand's magnifiers (Saleh
/// 2026-10-18, demo 1018a note 14: "pick up the loupe, bar or Fresnel and use
/// it over a paper to magnify microprint that has a meaning, such as
/// microtext that must match"; the desk machines spec §10). A microprint line
/// is a segment along a paper's foot; a magnifier shows a shape on the desk
/// (in its own frame: centred at the origin, x along its width). The line is
/// read once the shape has shown at least <see cref="ReadLength"/> of it (one
/// repeat of the Bureau's mark, the form and the Citizen ID; all of a shorter
/// line) for <see cref="UvCone.SeenSeconds"/> in a row. Pure: the magnifiers'
/// MonoBehaviours fed it the line's ends in each lens's frame. The magnifiers
/// were cut by P0-C, 2026-10-20: until the scanner shows the microprint, FIX_PLAN_v2 #17, nothing reads it;
/// it stays with TOOLS (FIX_PLAN_v2 65c), through DeskHardware.TrackLens.
/// </summary>
public static class MicroLens
{
    /// <summary>How much of a microprint line a magnifier must show to read it (metres of the desk: one repeat of its text).</summary>
    public const float ReadLength = 0.03f;

    /// <summary>
    /// How long a stretch of the segment from (<paramref name="ax"/>, <paramref name="ay"/>)
    /// to (<paramref name="bx"/>, <paramref name="by"/>), in the lens's frame,
    /// lies inside its <paramref name="shape"/> of half width
    /// <paramref name="halfWidth"/> (a circle's radius) and half height
    /// <paramref name="halfHeight"/> (a rectangle's; a circle ignores it).
    /// </summary>
    public static float Shown(float ax, float ay, float bx, float by, LensShape shape, float halfWidth, float halfHeight)
    {
        float dx = bx - ax, dy = by - ay;
        float length = (float)Math.Sqrt(dx * dx + dy * dy);
        if (length < 1e-7f || halfWidth <= 0f)
            return 0f;
        float t0 = 0f, t1 = 1f;
        if (shape == LensShape.Round)
        {
            // |a + t d|^2 <= r^2: a quadratic in t.
            float A = dx * dx + dy * dy, B = 2f * (ax * dx + ay * dy), C = ax * ax + ay * ay - halfWidth * halfWidth;
            float disc = B * B - 4f * A * C;
            if (disc <= 0f)
                return 0f;
            float root = (float)Math.Sqrt(disc);
            t0 = Math.Max(0f, (-B - root) / (2f * A));
            t1 = Math.Min(1f, (-B + root) / (2f * A));
        }
        else
        {
            if (halfHeight <= 0f)
                return 0f;
            // Liang-Barsky clip against |x| <= halfWidth, |y| <= halfHeight.
            float[] p = { -dx, dx, -dy, dy };
            float[] q = { ax + halfWidth, halfWidth - ax, ay + halfHeight, halfHeight - ay };
            for (int i = 0; i < 4; i++)
            {
                if (Math.Abs(p[i]) < 1e-9f)
                {
                    if (q[i] < 0f)
                        return 0f;
                    continue;
                }
                float r = q[i] / p[i];
                if (p[i] < 0f)
                    t0 = Math.Max(t0, r);
                else
                    t1 = Math.Min(t1, r);
            }
        }
        return t1 > t0 ? (t1 - t0) * length : 0f;
    }

    /// <summary>True when <paramref name="shown"/> metres of a line <paramref name="lineLength"/> long are enough to read it: ReadLength, or nearly all of a shorter line.</summary>
    public static bool Readable(float shown, float lineLength) => shown >= Math.Min(ReadLength, 0.9f * Math.Max(0f, lineLength)) && shown > 0f;

    /// <summary>A line's time under a lens after a frame of <paramref name="dt"/>: it grows while it is readable there and falls back to 0 otherwise; it is read once UvCone.IsSeen.</summary>
    public static float Held(float seconds, bool readable, float dt) => readable ? seconds + Math.Max(0f, dt) : 0f;
}
