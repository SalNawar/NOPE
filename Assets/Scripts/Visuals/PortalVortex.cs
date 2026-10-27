using System;

/// <summary>
/// The portal rings' vortex (Saleh's demo 1018a: "the vfx for portals looks
/// bad and is not even alligned"; the portals spec v3 VX1, VX4, VX5; HALL-FX
/// BUG-26a): the pure part of PortalEffect and its shader TimeDesk/PortalGlow.
/// A ring's opening is an ellipse on the hall drawing's source canvas (pixels,
/// top-left origin: HallPortalsSO's rings, fitted to each ring's painted-aperture
/// mask); the effect is a quad <see cref="Margin"/> times the opening's radii
/// each way around its centre, so an arch's hole, which reaches past its
/// ellipse, still fits on it. This class maps the opening to the drawing's
/// local space and the quad to its ring's crop of the mask atlas, and holds the
/// effect's curves: the iris that opens and closes it, the departure flare,
/// the night surge and the turning of its swirl. No UnityEngine, so it is
/// tested headless (PortalVortexTests).
/// </summary>
public static class PortalVortex
{
    /// <summary>The quad's half-size as a multiple of the opening's radii (a hole reaches at most this far: HallPortalRing.reach).</summary>
    public const float Margin = 1.5f;

    /// <summary>A rectangle (x, y the lower-left corner; w, h its size) in a drawing's local units or texture coordinates.</summary>
    public readonly struct Box
    {
        /// <summary>The lower-left corner's x.</summary>
        public readonly float X;

        /// <summary>The lower-left corner's y.</summary>
        public readonly float Y;

        /// <summary>The width.</summary>
        public readonly float W;

        /// <summary>The height.</summary>
        public readonly float H;

        /// <summary>A box.</summary>
        public Box(float x, float y, float w, float h)
        {
            X = x;
            Y = y;
            W = w;
            H = h;
        }

        /// <summary>The centre's x.</summary>
        public float CentreX => X + W * 0.5f;

        /// <summary>The centre's y.</summary>
        public float CentreY => Y + H * 0.5f;
    }

    /// <summary>
    /// The effect's quad in the drawing sprite's local units: the opening
    /// centred at canvas pixel (<paramref name="cx"/>, <paramref name="cy"/>,
    /// y down) with radii <paramref name="rx"/> and <paramref name="ry"/>,
    /// grown by <see cref="Margin"/>; the sprite is <paramref name="width"/> x
    /// <paramref name="height"/> pixels with its pivot at
    /// (<paramref name="pivotX"/>, <paramref name="pivotY"/>) pixels from its
    /// lower-left corner and <paramref name="pixelsPerUnit"/> pixels a unit.
    /// </summary>
    public static Box LocalQuad(float cx, float cy, float rx, float ry, float width, float height, float pivotX, float pivotY, float pixelsPerUnit)
    {
        float ppu = pixelsPerUnit > 0f ? pixelsPerUnit : 100f;
        float w = 2f * Margin * rx / ppu, h = 2f * Margin * ry / ppu;
        float x = (cx - pivotX) / ppu, y = (height - cy - pivotY) / ppu;
        return new Box(x - w * 0.5f, y - h * 0.5f, w, h);
    }

    /// <summary>
    /// The map from the quad's texture coordinates (0 to 1, y up) to the mask
    /// atlas's, for the opening centred at canvas (<paramref name="cx"/>,
    /// <paramref name="cy"/>, y down) with radii <paramref name="rx"/> and
    /// <paramref name="ry"/>, whose mask crop covers <paramref name="crop"/> on
    /// the canvas (x, y its top-left corner, y down) and <paramref name="atlas"/>
    /// in the atlas (x, y its lower-left corner, y up): the shader reads the
    /// mask at uv * (W, H) + (X, Y).
    /// </summary>
    public static Box MaskMap(float cx, float cy, float rx, float ry, Box crop, Box atlas)
    {
        float cw = crop.W > 0f ? crop.W : 1f, ch = crop.H > 0f ? crop.H : 1f;
        // u: canvas x = cx - M rx + u 2 M rx; v: canvas y = cy + M ry - v 2 M ry (rows run down, v up).
        float scaleU = atlas.W * 2f * Margin * rx / cw;
        float offsetU = atlas.X + atlas.W * (cx - Margin * rx - crop.X) / cw;
        float scaleV = atlas.H * 2f * Margin * ry / ch;
        float offsetV = atlas.Y + atlas.H * (1f - (cy + Margin * ry - crop.Y) / ch);
        return new Box(offsetU, offsetV, scaleU, scaleV);
    }

    /// <summary>The iris after <paramref name="dt"/> seconds from <paramref name="current"/> toward open (1) or shut (0), over <paramref name="seconds"/>; at once with <paramref name="reduced"/> motion or no time.</summary>
    public static float Reveal(float current, bool open, float dt, float seconds, bool reduced)
    {
        float target = open ? 1f : 0f;
        if (reduced || seconds <= 0f)
            return target;
        float step = Math.Max(0f, dt) / seconds;
        return current < target ? Math.Min(target, current + step) : Math.Max(target, current - step);
    }

    /// <summary>The iris's radius (a share of the opening) at <paramref name="reveal"/>: it springs open past its rim and settles (an ease-out with a small overshoot), 0 shut, 1 open.</summary>
    public static float Iris(float reveal)
    {
        float t = Clamp01(reveal);
        if (t <= 0f || t >= 1f)
            return t;
        const float back = 1.6f;
        float u = t - 1f;
        return 1f + u * u * ((back + 1f) * u + back);
    }

    /// <summary>
    /// The departure flare at <paramref name="progress"/> (0 to 1 of its time):
    /// 0 at both ends, 1 at its middle, eased; with <paramref name="reduced"/>
    /// motion one step: 1 until its time is up, then 0.
    /// </summary>
    public static float Pulse(float progress, bool reduced)
    {
        if (progress <= 0f || progress >= 1f)
            return 0f;
        return reduced ? 1f : (float)Math.Sin(Math.PI * progress);
    }

    /// <summary>The swirl's angle (radians, kept in 0 to 2 pi) after <paramref name="dt"/> seconds turning <paramref name="degreesPerSecond"/>; still with <paramref name="reduced"/> motion.</summary>
    public static float Turn(float angle, float degreesPerSecond, float dt, bool reduced)
    {
        if (reduced)
            return angle;
        double a = angle + degreesPerSecond * Math.PI / 180.0 * Math.Max(0f, dt);
        double twoPi = 2.0 * Math.PI;
        a -= Math.Floor(a / twoPi) * twoPi;
        return (float)a;
    }

    private static float Clamp01(float x) => x < 0f ? 0f : x > 1f ? 1f : float.IsNaN(x) ? 0f : x;
}
