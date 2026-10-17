using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// The contact shadow under each modelled document (Saleh 2026-10-17: "all
/// documents need to feel 3d especially the folder"): PaperDepth.Shadow.
/// </summary>
public class PaperDepthTests
{
    private const float Eps = 1e-5f;

    [Test]
    public void Shadow_IsSolidInside_AndFadesToNothing_SpreadOutside_MovedByItsOffset()
    {
        List<(float x, float y)> square = PaperEdge.Rectangle(1f, 1f);
        DepthMesh shadow = PaperDepth.Shadow(square, 0.2f, 0.3f, 0.002f, 0.01f, 0.003f, -0.004f, 0.4f);
        Assert.AreEqual(1 + 3 * square.Count, shadow.Vertices.Count, "its centre and three rings");
        Assert.AreEqual(0, shadow.Triangles.Count % 3);
        float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
        for (int i = 0; i < shadow.Vertices.Count; i++)
        {
            (float x, float y, float z) v = shadow.Vertices[i];
            Assert.AreEqual(0.002f, v.z, Eps, "every corner at its depth under the face");
            if (shadow.Colours[i].A > 0f)
                continue;
            minX = System.Math.Min(minX, v.x);
            maxX = System.Math.Max(maxX, v.x);
            minY = System.Math.Min(minY, v.y);
            maxY = System.Math.Max(maxY, v.y);
        }
        Assert.AreEqual(0.4f, shadow.Colours[0].A, Eps, "solid at its centre");
        Assert.AreEqual(0.5f + (0.01f / 1.41421f + 0.003f) / 0.2f, maxX, 1e-3f, "its faded rim lies spread outside, moved right");
        Assert.AreEqual(-0.5f - (0.01f / 1.41421f + 0.004f) / 0.3f, minY, 1e-3f, "and toward the camera");
        Assert.Less(maxY, 0.5f + 0.01f / 0.3f, "less far out on the far side");
        Assert.Greater(minX, -0.5f - 0.01f / 0.2f);
        foreach (Rgba c in shadow.Colours)
            Assert.AreEqual(PaperDepth.Ink.R, c.R, Eps, "the cel kit's ink, never grey");
    }

    [Test]
    public void Shadow_HeldUp_ReachesWiderAndFurther()
    {
        List<(float x, float y)> square = PaperEdge.Rectangle(1f, 1f);
        float Reach(DepthMesh m)
        {
            float most = 0f;
            foreach ((float x, float y, float z) v in m.Vertices)
                most = System.Math.Max(most, v.x);
            return most;
        }
        float rest = Reach(PaperDepth.Shadow(square, 0.2f, 0.2f, 0.001f, 0.008f, 0.002f, -0.002f, 0.5f));
        float held = Reach(PaperDepth.Shadow(square, 0.2f, 0.2f, 0.001f, 0.026f, 0.01f, -0.017f, 0.3f));
        Assert.Greater(held, rest, "held up, the shadow lies wider and further");
        Assert.AreEqual(0, PaperDepth.Shadow(null, 1f, 1f, 0f, 0.01f, 0f, 0f, 1f).Vertices.Count);
    }

    [Test]
    public void Shadow_WorksWhicheverWayTheOutlineTurns()
    {
        List<(float x, float y)> ccw = PaperEdge.Rectangle(1f, 1f);
        var cw = new List<(float x, float y)>(ccw);
        cw.Reverse();
        DepthMesh a = PaperDepth.Shadow(ccw, 0.2f, 0.2f, 0f, 0.01f, 0f, 0f, 0.4f), b = PaperDepth.Shadow(cw, 0.2f, 0.2f, 0f, 0.01f, 0f, 0f, 0.4f);
        float Reach(DepthMesh m)
        {
            float most = 0f;
            foreach ((float x, float y, float z) v in m.Vertices)
                most = System.Math.Max(most, v.x);
            return most;
        }
        Assert.AreEqual(Reach(a), Reach(b), Eps, "the rim lies outside both ways");
        Assert.Greater(Reach(b), 0.5f);
    }

    [Test]
    public void Shadow_ARoundCornersSolidCore_NeverCrossesItself_EvenWide()
    {
        // A card's round corner (a 3 mm radius) under a held paper's wide shadow (22 mm): before, its core points
        // passed the corner's centre and their triangles folded over, darkening wedges at the corners.
        List<(float x, float y)> outline = PaperSilhouette.Outline(0.14f, 0.22f, 0.003f).ConvertAll(q => (q.x / 0.14f, q.y / 0.22f));
        DepthMesh shadow = PaperDepth.Shadow(outline, 0.14f, 0.22f, 0f, 0.022f, 0f, 0f, 0.4f);
        for (int i = 0; i < outline.Count; i++)
        {
            (float x, float y, float z) core = shadow.Vertices[1 + i * 3];
            Assert.LessOrEqual(System.Math.Abs(core.x), System.Math.Abs(outline[i].x) + 1e-5f, "each core point lies between its outline point and the centre");
            Assert.LessOrEqual(System.Math.Abs(core.y), System.Math.Abs(outline[i].y) + 1e-5f);
        }
        Assert.AreEqual(3 * (outline.Count + 4 * outline.Count), shadow.Triangles.Count, "each triangle once (the shader draws both sides)");
    }
}
