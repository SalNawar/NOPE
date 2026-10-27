using System;

/// <summary>
/// The painted PC's CRT face (desk v3, round 12; the concept-vs-game review,
/// item 1: "the screen is a flat floating panel with a thick black border,
/// sized like a tablet, not a curved CRT inside the casing"): the geometry of
/// a curved glass with rounded corners set into the CRT's bezel, and the
/// recess plate that closes the casing's front opening (the casing of
/// Desk_PC.fbx is an open box whose front is a 46 x 40 cm hole with the 44 x
/// 37 cm bezel slab standing in it, so the hall showed through the 1 to 2 cm
/// gap: the review's "see-through housing"). Pure (no engine): positions in
/// the screen's own frame, metres, x to the viewer's right, y up, z away from
/// the chair (Unity's quad convention: the face looks down -z), origin at the
/// screen quad's centre (DeskPaintedImporter.LayScreen: the blockout's screen
/// face plus its 2 mm stand-off). Measured from Desk_PC.fbx (round 12):
/// casing front rim 2.5 cm behind the screen face, its opening x +-0.23,
/// y -0.205 to 0.195; the bezel slab 0.5 to 2.2 cm behind it, x +-0.22,
/// y +-0.185; the painted glass 36 x 29 cm.
/// </summary>
public static class CrtFace
{
    /// <summary>One vertex of the face: its position (metres, the screen's frame) and its UV (0 to 1 across the visible glass).</summary>
    public readonly struct Vertex
    {
        public readonly float X, Y, Z, U, V;

        public Vertex(float x, float y, float z, float u, float v)
        {
            X = x;
            Y = y;
            Z = z;
            U = u;
            V = v;
        }
    }

    /// <summary>The dome's shape.</summary>
    public readonly struct Shape
    {
        /// <summary>The visible glass's width and height (metres).</summary>
        public readonly float Width, Height;

        /// <summary>The rounded corners' radius (metres).</summary>
        public readonly float Corner;

        /// <summary>How far the glass's rim sits behind the screen face (metres, + is away from the chair): inside the bezel, so the bezel's face overlaps it.</summary>
        public readonly float RimDepth;

        /// <summary>How far the dome's centre stands toward the chair from its rim (metres).</summary>
        public readonly float Bulge;

        public Shape(float width, float height, float corner, float rimDepth, float bulge)
        {
            Width = width;
            Height = height;
            Corner = corner;
            RimDepth = rimDepth;
            Bulge = bulge;
        }
    }

    /// <summary>
    /// The painted PC's glass: its rim 0.9 cm behind the screen face, inside
    /// the bezel slab (whose face is 0.7 cm behind it), so the slab's face
    /// meets the dome 94% of the way out and the glass shows set into the
    /// casing; 38 x 30.6 cm to the rim, so the visible glass is the painting's
    /// 36 x 29 cm (the blockout's screen face, which the painting's dark glass
    /// was drawn over); corners of 2.8 cm; its centre 1.8 cm in front of the
    /// rim (a CRT's curve).
    /// </summary>
    public static readonly Shape Painted = new Shape(0.38f, 0.306f, 0.028f, 0.009f, 0.018f);

    /// <summary>The bezel slab's face, behind the screen face (metres, Desk_PC.fbx): where the dome disappears into the casing.</summary>
    public const float BezelFaceDepth = 0.007f;

    /// <summary>How far out from the dome's centre (0 to 1) the bezel's face meets it: the visible glass's share of the rim's size.</summary>
    public static float VisibleT(Shape shape) => shape.Bulge <= 0f ? 1f : (float)Math.Sqrt(Math.Max(0f, Math.Min(1f, 1f - (shape.RimDepth - BezelFaceDepth) / shape.Bulge)));

    /// <summary>The recess plate closing the casing's front opening: its half width and its bottom and top (metres), and its depth behind the screen face (behind the bezel slab's back at 2.4 cm, in front of the casing's rim at 2.7 cm).</summary>
    public const float RecessHalfWidth = 0.245f, RecessBottom = -0.215f, RecessTop = 0.205f, RecessDepth = 0.026f;

    /// <summary>The desktop's aspect (the canvas is 1440 x 1080).</summary>
    public const float DesktopAspect = 4f / 3f;

    /// <summary>
    /// The size the glass's bounds must report so the desktop's clone, fitted
    /// at 4:3 and scaled by <paramref name="fill"/> (PcScreenClone: the
    /// picture is <c>fill * Fit(bounds)</c>), covers the whole visible glass
    /// of <paramref name="shape"/> with <paramref name="overscan"/> to spare
    /// on its tighter side (a CRT's overscan): no black border inside the
    /// glass. 4:3, centred on the glass.
    /// </summary>
    public static (float width, float height) PictureBounds(Shape shape, float fill, float overscan)
    {
        if (fill <= 0f)
            fill = 1f;
        float scale = 1f + Math.Max(0f, overscan);
        // The picture must be at least the glass's width and height (times the overscan), at 4:3.
        float pictureHeight = Math.Max(shape.Height, shape.Width / DesktopAspect) * scale;
        float pictureWidth = pictureHeight * DesktopAspect;
        return (pictureWidth / fill, pictureHeight / fill);
    }

    /// <summary>The share of the desktop's width and height the visible glass shows (inside the bezel's face, VisibleT; the rest is overscan or under the bezel): what a desk-view picture must keep its content inside.</summary>
    public static (float width, float height) VisibleShare(Shape shape, float fill, float overscan)
    {
        if (fill <= 0f)
            fill = 1f;
        (float w, float h) = PictureBounds(shape, fill, overscan);
        float t = VisibleT(shape);
        return (shape.Width * t / (w * fill), shape.Height * t / (h * fill));
    }

    /// <summary>
    /// The dome: <paramref name="around"/> points on the rounded rectangle's
    /// outline, <paramref name="rings"/> rings from the centre out to it; each
    /// point's depth follows a smooth cap (the rim at RimDepth, the centre
    /// Bulge in front of it). Triangles wound so the face looks down -z (the
    /// chair): Unity's front face.
    /// </summary>
    public static (Vertex[] vertices, int[] triangles) Dome(Shape shape, int around = 64, int rings = 8)
    {
        around = Math.Max(8, around - around % 4);
        rings = Math.Max(1, rings);
        var outline = Outline(shape, around);
        var vertices = new Vertex[1 + around * rings];
        vertices[0] = Make(shape, 0f, 0f, 0f);
        for (int r = 1; r <= rings; r++)
        {
            float t = (float)r / rings;
            for (int i = 0; i < around; i++)
                vertices[1 + (r - 1) * around + i] = Make(shape, outline[i].x * t, outline[i].y * t, t);
        }

        var triangles = new int[3 * (around + (rings - 1) * around * 2)];
        int k = 0;
        for (int i = 0; i < around; i++)
            k = Add(triangles, k, vertices, 0, 1 + i, 1 + (i + 1) % around);
        for (int r = 1; r < rings; r++)
        {
            int inner = 1 + (r - 1) * around, outer = 1 + r * around;
            for (int i = 0; i < around; i++)
            {
                int j = (i + 1) % around;
                k = Add(triangles, k, vertices, inner + i, outer + i, outer + j);
                k = Add(triangles, k, vertices, inner + i, outer + j, inner + j);
            }
        }
        return (vertices, triangles);
    }

    /// <summary>The depth (+ away from the chair) of a point <paramref name="t"/> of the way from the dome's centre to its rim.</summary>
    public static float Depth(Shape shape, float t) => shape.RimDepth - shape.Bulge * (1f - t * t);

    private static Vertex Make(Shape shape, float x, float y, float t) =>
        new Vertex(x, y, Depth(shape, t), 0.5f + x / shape.Width, 0.5f + y / shape.Height);

    /// <summary>Adds the triangle (a, b, c), wound to face -z.</summary>
    private static int Add(int[] triangles, int k, Vertex[] v, int a, int b, int c)
    {
        // Unity's front face: cross(b - a, c - a) points toward the viewer (-z).
        float abx = v[b].X - v[a].X, aby = v[b].Y - v[a].Y, acx = v[c].X - v[a].X, acy = v[c].Y - v[a].Y;
        float z = abx * acy - aby * acx;
        triangles[k] = a;
        triangles[k + 1] = z < 0f ? b : c;
        triangles[k + 2] = z < 0f ? c : b;
        return k + 3;
    }

    /// <summary><paramref name="count"/> points around the rounded rectangle, counter-clockwise from the right edge's middle.</summary>
    private static (float x, float y)[] Outline(Shape shape, int count)
    {
        float hw = shape.Width / 2f, hh = shape.Height / 2f;
        float r = Math.Min(shape.Corner, Math.Min(hw, hh));
        var points = new (float x, float y)[count];
        for (int i = 0; i < count; i++)
        {
            // A ray from the centre at this angle, met with the rounded rectangle (a superellipse-free exact hit).
            double a = 2.0 * Math.PI * i / count;
            float dx = (float)Math.Cos(a), dy = (float)Math.Sin(a);
            points[i] = Hit(hw, hh, r, dx, dy);
        }
        return points;
    }

    /// <summary>Where the ray from the centre along (dx, dy) leaves the rounded rectangle of half size (hw, hh) and corner radius r.</summary>
    private static (float x, float y) Hit(float hw, float hh, float r, float dx, float dy)
    {
        float ax = Math.Abs(dx), ay = Math.Abs(dy);
        // The plain rectangle's hit.
        float s = Math.Min(ax > 1e-6f ? hw / ax : float.MaxValue, ay > 1e-6f ? hh / ay : float.MaxValue);
        float x = ax * s, y = ay * s;
        float cx = hw - r, cy = hh - r;
        if (x > cx && y > cy)
        {
            // In the corner's square: meet the corner circle centred at (cx, cy).
            float b = ax * cx + ay * cy;
            float c = cx * cx + cy * cy - r * r;
            float t = b + (float)Math.Sqrt(Math.Max(0f, b * b - c));
            x = ax * t;
            y = ay * t;
        }
        return (Math.Sign(dx) * x, Math.Sign(dy) * y);
    }
}
