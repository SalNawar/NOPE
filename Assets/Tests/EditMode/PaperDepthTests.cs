using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// The desk's documents read as physical objects (Saleh 2026-10-17: "all
/// documents need to feel 3d especially the folder"): PaperDepth's contact
/// shadow, a sheet's lifted corner, the folds' bands and the folder's page
/// edges, and PaperLayers' shade layer.
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
    public void Shadow_ReachesFurtherAtALiftedCorner_AndAWiderSpreadReachesFurther()
    {
        List<(float x, float y)> square = PaperEdge.Rectangle(1f, 1f);
        float Reach(DepthMesh m)
        {
            float most = 0f;
            foreach ((float x, float y, float z) v in m.Vertices)
                most = System.Math.Max(most, v.x);
            return most;
        }
        float rest = Reach(PaperDepth.Shadow(square, 0.2f, 0.2f, 0.001f, 0.006f, 0.002f, -0.002f, 0.4f));
        float held = Reach(PaperDepth.Shadow(square, 0.2f, 0.2f, 0.001f, 0.026f, 0.02f, -0.02f, 0.24f));
        float curled = Reach(PaperDepth.Shadow(square, 0.2f, 0.2f, 0.001f, 0.006f, 0.002f, -0.002f, 0.4f, PaperDepth.CornerLift(square, 1, 2.5f)));
        Assert.Greater(held, rest, "held up, the shadow lies wider and further");
        Assert.Greater(curled, rest, "a lifted corner throws its shadow further");
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
    public void CurlCorner_IsOneTheCameraSees_AndCurl_ShadesItFadingAlongBothSides()
    {
        for (int seed = 0; seed < 50; seed++)
            Assert.That(PaperDepth.CurlCorner(seed), Is.InRange(0, 2), "bottom left, bottom right or top right: never the hidden top left");
        List<(float x, float y)> square = PaperEdge.Rectangle(1f, 1f);
        DepthMesh curl = PaperDepth.Curl(square, 1, 0.2f, 0.3f, 0.06f, -PaperLayers.Shade, new Rgba(0.3f, 0.2f, 0.3f, 0.16f));
        Assert.AreEqual(3, curl.Vertices.Count);
        Assert.AreEqual((0.5f, -0.5f), (curl.Vertices[0].x, curl.Vertices[0].y), "dark at the corner");
        Assert.AreEqual(0.16f, curl.Colours[0].A, Eps);
        Assert.AreEqual(0f, curl.Colours[1].A, Eps, "fading to nothing along the bottom");
        Assert.AreEqual(0.5f - 0.3f, curl.Vertices[1].x, Eps);
        Assert.AreEqual(-0.5f + 0.2f, curl.Vertices[2].y, Eps, "and up the side");
        Assert.AreEqual(-PaperLayers.Shade, curl.Vertices[0].z, Eps, "over the face, at the shade's layer");
    }

    [Test]
    public void Strip_RunsThroughItsStops_AcrossTheFace()
    {
        var stops = new List<(float at, Rgba colour)> { (-0.1f, new Rgba(0f, 0f, 0f, 0f)), (0f, new Rgba(0f, 0f, 0f, 0.3f)), (0.1f, new Rgba(0f, 0f, 0f, 0f)) };
        DepthMesh along = PaperDepth.Strip(stops, true, -0.4f, 0.4f, -0.0002f);
        Assert.AreEqual(6, along.Vertices.Count);
        Assert.AreEqual(2 * 2 * 6, along.Triangles.Count, "two triangles, both ways, between each pair of stops");
        Assert.AreEqual((0f, -0.4f), (along.Vertices[2].x, along.Vertices[2].y));
        Assert.AreEqual((0f, 0.4f), (along.Vertices[3].x, along.Vertices[3].y));
        DepthMesh across = PaperDepth.Strip(stops, false, -0.5f, 0.5f, 0f);
        Assert.AreEqual((-0.5f, 0f), (across.Vertices[2].x, across.Vertices[2].y), "along y: the stops are heights");
        Assert.AreEqual(0, PaperDepth.Strip(stops.GetRange(0, 1), true, 0f, 1f, 0f).Vertices.Count);
    }

    [Test]
    public void PageBlock_StepsEachPageOutBelowAndRight_TheRightSideOnlyAboveTheFlap()
    {
        var sheet = (x0: 0f, y0: -0.4f, x1: 0.4f, y1: 0.3f);
        var light = new Rgba(1f, 1f, 1f, 1f);
        var dark = new Rgba(0.8f, 0.8f, 0.8f, 1f);
        var ink = new Rgba(0.3f, 0.2f, 0.2f, 1f);
        DepthMesh block = PaperDepth.PageBlock(sheet, 3, 0.001f, 0.46f, 0.26f, -0.0001f, light, dark, ink);
        float right = float.MinValue, low = float.MaxValue;
        foreach ((float x, float y, float z) v in block.Vertices)
        {
            right = System.Math.Max(right, v.x);
            low = System.Math.Min(low, v.y);
            Assert.IsTrue(v.x >= sheet.x0 - Eps && v.y <= sheet.y1 + Eps, "only out to the bottom right");
        }
        Assert.AreEqual(sheet.x1 + 3 * 0.001f * PaperDepth.LeanRight / 0.46f, right, Eps, "three pages out to the right");
        Assert.AreEqual(sheet.y0 - 3 * 0.001f * PaperDepth.LeanNear / 0.26f, low, Eps, "and below");
        DepthMesh flapped = PaperDepth.PageBlock(sheet, 3, 0.001f, 0.46f, 0.26f, 0f, light, dark, ink, 0.1f);
        foreach ((float x, float y, float z) v in flapped.Vertices)
            if (v.x > sheet.x1 + Eps)
                Assert.IsTrue(v.y >= 0.1f - Eps || v.y <= sheet.y0 + Eps, "beside the sheet only above the flap (or in the bottom band's corner)");
        Assert.Less(PaperDepth.PageBlock(sheet, 3, 0.001f, 0.46f, 0.26f, 0f, light, dark, ink, 1f).Vertices.Count, block.Vertices.Count, "a flap over the whole side: no right side at all");
    }

    [Test]
    public void Append_KeepsBothMeshesTriangles()
    {
        var a = new DepthMesh();
        a.Both(a.Add(0f, 0f, 0f, new Rgba(0f, 0f, 0f)), a.Add(1f, 0f, 0f, new Rgba(0f, 0f, 0f)), a.Add(0f, 1f, 0f, new Rgba(0f, 0f, 0f)));
        var b = new DepthMesh();
        b.Both(b.Add(0f, 0f, 1f, new Rgba(0f, 0f, 0f)), b.Add(1f, 0f, 1f, new Rgba(0f, 0f, 0f)), b.Add(0f, 1f, 1f, new Rgba(0f, 0f, 0f)));
        a.Append(b);
        Assert.AreEqual(6, a.Vertices.Count);
        Assert.AreEqual(12, a.Triangles.Count);
        Assert.AreEqual(3, a.Triangles[6], "the appended triangles point at their own corners");
    }

    [Test]
    public void Shade_LiesOverThePrintAndThePhoto_UnderTheTexts()
    {
        Assert.Greater(PaperLayers.Shade, PaperLayers.Photo + PaperLayers.PhotoInset);
        Assert.Less(PaperLayers.Shade, PaperLayers.Text);
        Assert.Less(PaperLayers.Shade, PaperLayers.PartsDepth);
    }
}
