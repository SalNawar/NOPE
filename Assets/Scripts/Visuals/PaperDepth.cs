using System;
using System.Collections.Generic;

/// <summary>
/// A contact shadow's mesh (PaperDepth.Shadow): its corners (x, y on the
/// face's plane; z, metres under the face), a colour each and its triangles.
/// DeskPaperDepth keeps one and has PaperDepth.Shadow rebuild it in place, so
/// its lists keep their room and a rebuild allocates nothing (BUG-65g), then
/// copies it into a Unity mesh.
/// </summary>
public sealed class DepthMesh
{
    /// <summary>The corners: x and y on the face's plane, z metres under the face.</summary>
    public readonly List<(float x, float y, float z)> Vertices = new List<(float x, float y, float z)>();

    /// <summary>Each corner's colour (its alpha the shadow's darkness there).</summary>
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

    /// <summary>Adds a triangle once (the shadow's shader draws both sides: a second winding would darken it twice), corner by corner (no array, BUG-65g).</summary>
    public void Tri(int a, int b, int c)
    {
        Triangles.Add(a);
        Triangles.Add(b);
        Triangles.Add(c);
    }

    /// <summary>Empties the mesh for a rebuild (PaperDepth.Shadow): its lists keep their room, so a rebuild of an outline as long allocates nothing.</summary>
    public void Clear()
    {
        Vertices.Clear();
        Colours.Clear();
        Triangles.Clear();
    }
}

/// <summary>
/// The soft contact shadow under each thing lying on the desk (Saleh
/// 2026-10-17: "all documents need to feel 3d", then "use canva and blender to
/// make actual 3d documents": the documents are modelled, DeskPaperDepth; the
/// shadow under them is drawn, not lit, so no real-time shadow crosses their
/// text): a solid core under the outline fading to nothing outside it, in the
/// cel kit's ink, falling toward the bottom right of the reading view, away
/// from a light at its top left. Engine-free maths on outlines in the face's
/// unit space (the quad is scaled to the thing's size, width by height in
/// metres).
/// </summary>
public static class PaperDepth
{
    /// <summary>The way the shadow falls (unit vector on the desk, x right, y away from the camera): toward the bottom right of the reading view, away from the light at its top left.</summary>
    public const float LeanRight = 0.5f, LeanNear = 0.866f;

    /// <summary>The shadow's ink (#2B1C24, the cel kit's ink: a purple-brown, never grey).</summary>
    public static readonly Rgba Ink = new Rgba(0.169f, 0.11f, 0.141f, 1f);

    /// <summary>
    /// A soft contact shadow under <paramref name="outline"/> (unit space of a
    /// face <paramref name="width"/> by <paramref name="height"/> metres),
    /// <paramref name="depth"/> metres under the face: a solid core of
    /// <paramref name="alpha"/> inside the outline, fading to nothing
    /// <paramref name="spread"/> metres outside it, moved <paramref name="offsetX"/>
    /// right and <paramref name="offsetY"/> away (metres; negative: toward the
    /// camera). A convex outline. Built into <paramref name="into"/> when
    /// given (emptied first: DeskPaperDepth rebuilds its one mesh every frame
    /// a hold or a hover eases, and a rebuild into it allocates nothing,
    /// BUG-65g), else into a new mesh.
    /// </summary>
    public static DepthMesh Shadow(IReadOnlyList<(float x, float y)> outline, float width, float height, float depth,
                                   float spread, float offsetX, float offsetY, float alpha, DepthMesh into = null)
    {
        DepthMesh mesh = into ?? new DepthMesh();
        mesh.Clear();
        if (outline == null || outline.Count < 3 || width <= 0f || height <= 0f)
            return mesh;
        int n = outline.Count;
        float cx = 0f, cy = 0f, area = 0f;
        for (int i = 0; i < n; i++)
        {
            (float x, float y) p = At(outline, i, width, height);
            cx += p.x;
            cy += p.y;
        }
        cx /= n;
        cy /= n;
        for (int i = 0; i < n; i++)
        {
            (float x, float y) p = At(outline, i, width, height), q = At(outline, (i + 1) % n, width, height);
            area += p.x * q.y - q.x * p.y;
        }
        float turn = area >= 0f ? 1f : -1f;
        float core = spread * 0.35f;
        Rgba solid = Ink.WithAlpha(alpha), half = Ink.WithAlpha(alpha * 0.45f), none = Ink.WithAlpha(0f);
        int centre = mesh.Add((cx + offsetX) / width, (cy + offsetY) / height, depth, solid);
        int first = mesh.Vertices.Count;
        for (int i = 0; i < n; i++)
        {
            (float px, float py) = At(outline, i, width, height);
            (float nx, float ny) = Normal(outline, i, width, height, turn);
            float ox = offsetX, oy = offsetY;
            // the solid core's edge: toward the centre (never past it, so a round corner's points never cross)
            float toCentre = (float)Math.Sqrt((cx - px) * (cx - px) + (cy - py) * (cy - py));
            float t = toCentre > 1e-6f ? Math.Min(0.5f, core / toCentre) : 0f;
            mesh.Add((px + (cx - px) * t + ox) / width, (py + (cy - py) * t + oy) / height, depth, solid);
            mesh.Add((px + nx * spread * 0.35f + ox) / width, (py + ny * spread * 0.35f + oy) / height, depth, half);
            mesh.Add((px + nx * spread + ox) / width, (py + ny * spread + oy) / height, depth, none);
        }
        for (int i = 0; i < n; i++)
        {
            int a = first + i * 3, b = first + (i + 1) % n * 3;
            mesh.Tri(centre, a, b);
            for (int ring = 0; ring < 2; ring++)
            {
                mesh.Tri(a + ring, b + ring, b + ring + 1);
                mesh.Tri(a + ring, b + ring + 1, a + ring + 1);
            }
        }
        return mesh;
    }

    /// <summary>The sheet model's lifted corner (tools/props/documents/docs_models.py, sheet(): lift_max) and its reach (as a share of the face from the bottom right corner).</summary>
    public const float CornerLift = 0.0007f, CornerReach = 0.28f;

    /// <summary>The sheet model's edge curl (its height at the left and right edges) and its reach (a share of the width).</summary>
    public const float EdgeCurl = 0.0002f, EdgeReach = 0.06f;

    /// <summary>
    /// How high the sheet model's face stands over its plane at
    /// (<paramref name="u"/>, <paramref name="v"/>) (0 to 1 from its bottom
    /// left; metres toward the viewer): its lifted bottom right corner and its
    /// slight edge curl, as docs_models.py builds it. A printed part over the
    /// corner lifts by it (ReliefOver), so the corner never hides print
    /// (BUG-12a: "corner relief hides print in ~13% of the corner").
    /// </summary>
    public static float SheetRelief(float u, float v)
    {
        double d = Math.Sqrt((1 - u) * (1 - u) + v * v);
        double corner = CornerLift * Math.Pow(Math.Max(0.0, 1 - d / CornerReach), 2);
        double curl = EdgeCurl * (Math.Pow(Math.Max(0.0, 1 - u / EdgeReach), 2) + Math.Pow(Math.Max(0.0, 1 - (1 - u) / EdgeReach), 2));
        return (float)(corner + curl);
    }

    /// <summary>The most the sheet model stands over its plane under a part centred at (<paramref name="u"/>, <paramref name="v"/>), <paramref name="halfU"/> by <paramref name="halfV"/> of the face either way (metres).</summary>
    public static float ReliefOver(float u, float v, float halfU, float halfV)
    {
        float most = 0f;
        const int steps = 4;
        for (int i = 0; i <= steps; i++)
            for (int j = 0; j <= steps; j++)
            {
                float su = Clamp01(u + halfU * (-1f + 2f * i / steps)), sv = Clamp01(v + halfV * (-1f + 2f * j / steps));
                most = Math.Max(most, SheetRelief(su, sv));
            }
        return most;
    }

    /// <summary>The contact shadow's darkness per place up the papers' stack, and its most (BUG-40c: the top paper's shadow the darkest, so the stack order reads at a glance; GAME_FEEL_BIBLE A9).</summary>
    public const float RankStep = 0.25f, MostRankStrength = 1.75f;

    /// <summary>The share of its darkness a paper's contact shadow takes at <paramref name="rank"/> in the papers' stack (0 the bottom): 1 at the bottom, darker each place up, at most MostRankStrength.</summary>
    public static float RankStrength(int rank) => Math.Min(MostRankStrength, 1f + RankStep * Math.Max(0, rank));

    private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;

    /// <summary>Corner <paramref name="i"/> of <paramref name="outline"/> in metres (its unit space scaled to <paramref name="width"/> by <paramref name="height"/>), read where it lies: no scaled copy of the outline is made (BUG-65g).</summary>
    private static (float x, float y) At(IReadOnlyList<(float x, float y)> outline, int i, float width, float height) =>
        (outline[i].x * width, outline[i].y * height);

    /// <summary>The outward normal at corner <paramref name="i"/> of <paramref name="outline"/> in metres (the mean of its two sides' normals; <paramref name="turn"/> 1 for a counter-clockwise outline, -1 for clockwise).</summary>
    private static (float x, float y) Normal(IReadOnlyList<(float x, float y)> outline, int i, float width, float height, float turn)
    {
        int n = outline.Count;
        (float x, float y) a = At(outline, (i + n - 1) % n, width, height), b = At(outline, i, width, height), c = At(outline, (i + 1) % n, width, height);
        (float x, float y) n1 = Unit((b.y - a.y) * turn, -(b.x - a.x) * turn), n2 = Unit((c.y - b.y) * turn, -(c.x - b.x) * turn);
        return Unit(n1.x + n2.x, n1.y + n2.y);
    }

    private static (float x, float y) Unit(float x, float y)
    {
        float l = (float)Math.Sqrt(x * x + y * y);
        return l > 1e-9f ? (x / l, y / l) : (0f, 0f);
    }
}
