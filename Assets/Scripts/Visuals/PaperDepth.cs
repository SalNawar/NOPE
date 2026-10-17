using System;
using System.Collections.Generic;

/// <summary>
/// A mesh of the desk's depth cues (PaperDepth): its corners (x, y on the
/// face's plane; z, metres under the face), a colour each and its triangles.
/// The desk paper and the folder copy it into a Unity mesh.
/// </summary>
public sealed class DepthMesh
{
    /// <summary>The corners: x and y on the face's plane, z metres under the face.</summary>
    public readonly List<(float x, float y, float z)> Vertices = new List<(float x, float y, float z)>();

    /// <summary>Each corner's colour (its alpha the cue's strength).</summary>
    public readonly List<Rgba> Colours = new List<Rgba>();

    /// <summary>The triangles, three corner indices each.</summary>
    public readonly List<int> Triangles = new List<int>();

    /// <summary>Adds a corner and returns its index.</summary>
    public int Add(float x, float y, float z, Rgba colour)
    {
        Vertices.Add((x, y, z));
        Colours.Add(colour);
        return Vertices.Count - 1;
    }

    /// <summary>Adds a triangle wound both ways (it shows from either side, whatever way the outline turns).</summary>
    public void Both(int a, int b, int c) => Triangles.AddRange(new[] { a, b, c, a, c, b });

    /// <summary>Adds <paramref name="other"/>'s corners and triangles after this one's (drawn over them); returns this mesh.</summary>
    public DepthMesh Append(DepthMesh other)
    {
        if (other == null)
            return this;
        int offset = Vertices.Count;
        Vertices.AddRange(other.Vertices);
        Colours.AddRange(other.Colours);
        foreach (int t in other.Triangles)
            Triangles.Add(t + offset);
        return this;
    }
}

/// <summary>
/// The desk's documents as physical objects in an anime cel look (Saleh
/// 2026-10-17, after demo 1016a: "all documents need to feel 3d especially
/// the folder"), drawn without real-time shadows over the text (the papers
/// and the folder stay unlit, readable at every hour): the light comes from
/// the top left of the reading view, so each thing's edge leans out toward
/// the bottom right where the camera sees it (PaperEdge.Walls' lean: a
/// paper's sheet, a booklet's cover and pages, the folder's board), a soft
/// contact shadow lies under it (Shadow: tight at rest, wider, softer and
/// further when it is held up), a single sheet lifts one corner (CornerLift
/// widens its edge and its shadow there; Curl shades it), and a fold is a
/// band of shade (Strip: the folder's spine, a booklet's gutter). Engine-free
/// maths on outlines in the face's unit space (the quad is scaled to the
/// thing's size, width by height in metres).
/// </summary>
public static class PaperDepth
{
    /// <summary>The way an edge leans out under its face (unit vector on the desk, x right, y away from the camera): toward the bottom right of the reading view, away from the light at its top left.</summary>
    public const float LeanRight = 0.5f, LeanNear = 0.866f;

    /// <summary>The shadow's ink (#2B1C24, the cel kit's ink: a purple-brown, never grey).</summary>
    public static readonly Rgba Ink = new Rgba(0.169f, 0.11f, 0.141f, 1f);

    /// <summary>
    /// The lean of an edge <paramref name="amount"/> metres out per metre under
    /// the face, in the unit space of a face <paramref name="width"/> by
    /// <paramref name="height"/> metres: toward the bottom right (LeanRight, LeanNear).
    /// </summary>
    public static (float x, float y) Lean(float amount, float width, float height) =>
        width <= 0f || height <= 0f ? (0f, 0f) : (amount * LeanRight / width, -amount * LeanNear / height);

    /// <summary>The corners a single sheet may lift (the ones whose edge the camera sees): 0 bottom left, 1 bottom right, 2 top right.</summary>
    public static int CurlCorner(int seed) => (int)((uint)seed % 3u);

    /// <summary>A corner of the unit square (0 bottom left, 1 bottom right, 2 top right, 3 top left).</summary>
    private static (float x, float y) Corner(int corner) =>
        corner == 0 ? (-0.5f, -0.5f) : corner == 1 ? (0.5f, -0.5f) : corner == 2 ? (0.5f, 0.5f) : (-0.5f, 0.5f);

    /// <summary>
    /// How much more each corner of <paramref name="outline"/> (unit space)
    /// lifts when the sheet's <paramref name="corner"/> is raised: <paramref name="most"/>
    /// at that corner, falling straight to 1 a unit away (the far corners
    /// stay down). Null <paramref name="corner"/> (no curl): all 1.
    /// </summary>
    public static List<float> CornerLift(IReadOnlyList<(float x, float y)> outline, int? corner, float most)
    {
        var scale = new List<float>();
        if (outline == null)
            return scale;
        (float cx, float cy) = corner.HasValue ? Corner(corner.Value) : (0f, 0f);
        foreach ((float x, float y) in outline)
        {
            if (!corner.HasValue)
            {
                scale.Add(1f);
                continue;
            }
            float d = (float)Math.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
            scale.Add(1f + (most - 1f) * Math.Max(0f, 1f - d));
        }
        return scale;
    }

    /// <summary>
    /// A soft contact shadow under <paramref name="outline"/> (unit space of a
    /// face <paramref name="width"/> by <paramref name="height"/> metres),
    /// <paramref name="depth"/> metres under the face: a solid core of
    /// <paramref name="alpha"/> inside the outline, fading to nothing
    /// <paramref name="spread"/> metres outside it, moved <paramref name="offsetX"/>
    /// right and <paramref name="offsetY"/> away (metres; negative: toward the
    /// camera). <paramref name="lift"/> (per outline corner, CornerLift) widens
    /// and moves it further where a corner is raised. A convex outline.
    /// </summary>
    public static DepthMesh Shadow(IReadOnlyList<(float x, float y)> outline, float width, float height, float depth,
                                   float spread, float offsetX, float offsetY, float alpha, IReadOnlyList<float> lift = null)
    {
        var mesh = new DepthMesh();
        if (outline == null || outline.Count < 3 || width <= 0f || height <= 0f)
            return mesh;
        int n = outline.Count;
        var p = new (float x, float y)[n];
        float cx = 0f, cy = 0f, area = 0f;
        for (int i = 0; i < n; i++)
        {
            p[i] = (outline[i].x * width, outline[i].y * height);
            cx += p[i].x;
            cy += p[i].y;
        }
        cx /= n;
        cy /= n;
        for (int i = 0; i < n; i++)
            area += p[i].x * p[(i + 1) % n].y - p[(i + 1) % n].x * p[i].y;
        float turn = area >= 0f ? 1f : -1f;
        float core = Math.Min(spread * 0.35f, 0.3f * Math.Min(width, height));
        Rgba solid = Ink.WithAlpha(alpha), half = Ink.WithAlpha(alpha * 0.45f), none = Ink.WithAlpha(0f);
        int centre = mesh.Add((cx + offsetX) / width, (cy + offsetY) / height, depth, solid);
        int first = mesh.Vertices.Count;
        for (int i = 0; i < n; i++)
        {
            (float nx, float ny) = Normal(p, i, turn);
            float s = lift != null && i < lift.Count ? lift[i] : 1f;
            float ox = offsetX * s, oy = offsetY * s;
            mesh.Add((p[i].x - nx * core + ox) / width, (p[i].y - ny * core + oy) / height, depth, solid);
            mesh.Add((p[i].x + nx * spread * 0.35f * s + ox) / width, (p[i].y + ny * spread * 0.35f * s + oy) / height, depth, half);
            mesh.Add((p[i].x + nx * spread * s + ox) / width, (p[i].y + ny * spread * s + oy) / height, depth, none);
        }
        for (int i = 0; i < n; i++)
        {
            int a = first + i * 3, b = first + (i + 1) % n * 3;
            mesh.Both(centre, a, b);
            for (int ring = 0; ring < 2; ring++)
            {
                mesh.Both(a + ring, b + ring, b + ring + 1);
                mesh.Both(a + ring, b + ring + 1, a + ring + 1);
            }
        }
        return mesh;
    }

    /// <summary>The outward normal at corner <paramref name="i"/> (the mean of its two sides' normals; <paramref name="turn"/> 1 for a counter-clockwise outline, -1 for clockwise).</summary>
    private static (float x, float y) Normal((float x, float y)[] p, int i, float turn)
    {
        int n = p.Length;
        (float x, float y) a = p[(i + n - 1) % n], b = p[i], c = p[(i + 1) % n];
        (float x, float y) n1 = Unit((b.y - a.y) * turn, -(b.x - a.x) * turn), n2 = Unit((c.y - b.y) * turn, -(c.x - b.x) * turn);
        return Unit(n1.x + n2.x, n1.y + n2.y);
    }

    private static (float x, float y) Unit(float x, float y)
    {
        float l = (float)Math.Sqrt(x * x + y * y);
        return l > 1e-9f ? (x / l, y / l) : (0f, 0f);
    }

    /// <summary>
    /// The shade of a lifted corner on a single sheet's face (cel: a soft
    /// wedge of shadow where the paper curls up): <paramref name="colour"/> at
    /// the outline's point nearest <paramref name="corner"/>, fading to nothing
    /// <paramref name="reach"/> metres along both sides (unit space of a face
    /// <paramref name="width"/> by <paramref name="height"/> metres), at
    /// <paramref name="z"/> (negative: over the face).
    /// </summary>
    public static DepthMesh Curl(IReadOnlyList<(float x, float y)> outline, int corner, float width, float height, float reach, float z, Rgba colour)
    {
        var mesh = new DepthMesh();
        if (outline == null || outline.Count < 3 || width <= 0f || height <= 0f)
            return mesh;
        (float x, float y) target = Corner(corner);
        (float x, float y) c = outline[0];
        float best = float.MaxValue;
        foreach ((float x, float y) v in outline)
        {
            float d = (v.x - target.x) * (v.x - target.x) + (v.y - target.y) * (v.y - target.y);
            if (d < best)
            {
                best = d;
                c = v;
            }
        }
        float rx = Math.Min(reach / width, 0.45f), ry = Math.Min(reach / height, 0.45f);
        int a = mesh.Add(c.x, c.y, z, colour);
        int b = mesh.Add(c.x - Math.Sign(target.x) * rx, c.y, z, colour.WithAlpha(0f));
        int d2 = mesh.Add(c.x, c.y - Math.Sign(target.y) * ry, z, colour.WithAlpha(0f));
        mesh.Both(a, b, d2);
        return mesh;
    }

    /// <summary>
    /// A band of shade across a face (a fold: the folder's spine, a booklet's
    /// gutter): through <paramref name="stops"/> (a place on the face, unit
    /// space, and its colour) along x (<paramref name="alongX"/>, the band
    /// spanning y from <paramref name="from"/> to <paramref name="to"/>) or
    /// along y (spanning x), at <paramref name="z"/> (negative: over the face).
    /// The stops in order.
    /// </summary>
    public static DepthMesh Strip(IReadOnlyList<(float at, Rgba colour)> stops, bool alongX, float from, float to, float z)
    {
        var mesh = new DepthMesh();
        if (stops == null || stops.Count < 2)
            return mesh;
        for (int i = 0; i < stops.Count; i++)
        {
            (float at, Rgba colour) = stops[i];
            if (alongX)
            {
                mesh.Add(at, from, z, colour);
                mesh.Add(at, to, z, colour);
            }
            else
            {
                mesh.Add(from, at, z, colour);
                mesh.Add(to, at, z, colour);
            }
            if (i == 0)
                continue;
            int a = (i - 1) * 2, b = i * 2;
            mesh.Both(a, b, b + 1);
            mesh.Both(a, b + 1, a + 1);
        }
        return mesh;
    }

    /// <summary>
    /// The page block's edges seen at the open side of a stack of pages
    /// lying on a face (the folder's right sheet): <paramref name="pages"/>
    /// steps from <paramref name="sheet"/>'s bottom and right sides (x0, y0,
    /// x1, y1 in unit space), each <paramref name="step"/> metres further out
    /// toward the bottom right (LeanRight, LeanNear) in a face
    /// <paramref name="width"/> by <paramref name="height"/> metres, the
    /// lowest page first: a light page edge then a thin ink line under it,
    /// alternating <paramref name="light"/> and <paramref name="dark"/> pages
    /// (an L-shaped band per page along the bottom and the right side, the
    /// right one only above <paramref name="rightFrom"/>: what lies over the
    /// pages' lower right, a pocket's flap, hides them there), at
    /// <paramref name="z"/>.
    /// </summary>
    public static DepthMesh PageBlock((float x0, float y0, float x1, float y1) sheet, int pages, float step, float width, float height,
                                      float z, Rgba light, Rgba dark, Rgba ink, float rightFrom = float.MinValue)
    {
        var mesh = new DepthMesh();
        if (pages <= 0 || width <= 0f || height <= 0f)
            return mesh;
        float sx = step * LeanRight / width, sy = step * LeanNear / height;
        for (int k = pages; k >= 1; k--)
        {
            // Page k lies k steps out; the lower pages are drawn first and the upper ones over them.
            float ox = sx * k, oy = sy * k, line = 0.3f;
            Rgba face = k % 2 == 0 ? dark : light;
            Band(mesh, sheet, ox, oy, z, ink, rightFrom);
            Band(mesh, sheet, ox * (1f - line), oy * (1f - line), z, face, rightFrom);
        }
        return mesh;
    }

    /// <summary>One L-shaped page edge: the sheet moved (ox, -oy), the part of it outside the sheet (its bottom and right sides; the right side from <paramref name="rightFrom"/> up).</summary>
    private static void Band(DepthMesh mesh, (float x0, float y0, float x1, float y1) s, float ox, float oy, float z, Rgba colour, float rightFrom)
    {
        // The bottom strip, under the sheet, from its left (moved) to its right (moved).
        Quad(mesh, s.x0 + ox, s.y0 - oy, s.x1 + ox, s.y0, z, colour);
        // The right strip, beside the sheet, from its bottom (moved) to its top (moved).
        float bottom = Math.Max(s.y0 - oy, rightFrom);
        if (bottom < s.y1 - oy)
            Quad(mesh, s.x1, bottom, s.x1 + ox, s.y1 - oy, z, colour);
    }

    private static void Quad(DepthMesh mesh, float x0, float y0, float x1, float y1, float z, Rgba colour)
    {
        int a = mesh.Add(x0, y0, z, colour), b = mesh.Add(x1, y0, z, colour), c = mesh.Add(x1, y1, z, colour), d = mesh.Add(x0, y1, z, colour);
        mesh.Both(a, b, c);
        mesh.Both(a, c, d);
    }
}
