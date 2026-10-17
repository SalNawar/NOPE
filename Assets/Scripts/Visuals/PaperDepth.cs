using System;
using System.Collections.Generic;

/// <summary>
/// A contact shadow's mesh (PaperDepth.Shadow): its corners (x, y on the
/// face's plane; z, metres under the face), a colour each and its triangles.
/// DeskPaperDepth copies it into a Unity mesh.
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

    /// <summary>Adds a triangle wound both ways (it shows from either side, whatever way the outline turns).</summary>
    public void Both(int a, int b, int c) => Triangles.AddRange(new[] { a, b, c, a, c, b });
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
    /// camera). A convex outline.
    /// </summary>
    public static DepthMesh Shadow(IReadOnlyList<(float x, float y)> outline, float width, float height, float depth,
                                   float spread, float offsetX, float offsetY, float alpha)
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
            float ox = offsetX, oy = offsetY;
            mesh.Add((p[i].x - nx * core + ox) / width, (p[i].y - ny * core + oy) / height, depth, solid);
            mesh.Add((p[i].x + nx * spread * 0.35f + ox) / width, (p[i].y + ny * spread * 0.35f + oy) / height, depth, half);
            mesh.Add((p[i].x + nx * spread + ox) / width, (p[i].y + ny * spread + oy) / height, depth, none);
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
}
