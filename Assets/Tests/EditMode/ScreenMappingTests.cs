using NUnit.Framework;

/// <summary>The desktop on the CRT glass and in the PC frame (ScreenMapping).</summary>
public class ScreenMappingTests
{
    private const float Desktop = 4f / 3f;

    [Test]
    public void Fit_OnAWiderGlass_PillarBoxes_KeepingTheHeight()
    {
        // The Blender CRT's glass: 0.395 x 0.247 (1.6:1).
        (float w, float h) = ScreenMapping.Fit(0.395f, 0.247f, Desktop);
        Assert.AreEqual(1f, h, 1e-5f);
        Assert.AreEqual(Desktop / (0.395f / 0.247f), w, 1e-5f);
        Assert.Less(w, 1f);
    }

    [Test]
    public void Fit_OnATallerGlass_LetterBoxes_KeepingTheWidth()
    {
        (float w, float h) = ScreenMapping.Fit(1f, 1f, Desktop);
        Assert.AreEqual(1f, w, 1e-5f);
        Assert.AreEqual(0.75f, h, 1e-5f);
    }

    [Test]
    public void Fit_AtTheContentsAspect_FillsTheGlass()
    {
        (float w, float h) = ScreenMapping.Fit(1.2f, 0.9f, Desktop);
        Assert.AreEqual(1f, w, 1e-5f);
        Assert.AreEqual(1f, h, 1e-5f);
    }

    [TestCase(0f, 1f, 1.33f)]
    [TestCase(1f, -1f, 1.33f)]
    [TestCase(1f, 1f, 0f)]
    public void Fit_WithNoSizeOrAspect_FillsTheGlass(float gw, float gh, float aspect)
    {
        Assert.AreEqual((1f, 1f), ScreenMapping.Fit(gw, gh, aspect));
    }

    [Test]
    public void Viewport_NormalizesARectangleOfPixels()
    {
        (float x, float y, float w, float h) = ScreenMapping.Viewport(96f, 108f, 960f, 720f, 1920f, 1080f);
        Assert.AreEqual(0.05f, x, 1e-5f);
        Assert.AreEqual(0.1f, y, 1e-5f);
        Assert.AreEqual(0.5f, w, 1e-5f);
        Assert.AreEqual(720f / 1080f, h, 1e-5f);
    }

    [Test]
    public void Viewport_ClipsARectangleCrossingTheScreensEdge()
    {
        (float x, float y, float w, float h) = ScreenMapping.Viewport(-192f, 540f, 384f, 1080f, 1920f, 1080f);
        Assert.AreEqual(0f, x, 1e-5f);
        Assert.AreEqual(0.5f, y, 1e-5f);
        Assert.AreEqual(0.1f, w, 1e-5f);
        Assert.AreEqual(0.5f, h, 1e-5f);
    }

    [TestCase(2000f, 0f, 100f, 100f, 1920f, 1080f)]
    [TestCase(0f, 0f, 0f, 100f, 1920f, 1080f)]
    [TestCase(0f, 0f, 100f, 100f, 0f, 1080f)]
    public void Viewport_OffScreenOrEmpty_IsAllZero(float x0, float y0, float w0, float h0, float sw, float sh)
    {
        Assert.AreEqual((0f, 0f, 0f, 0f), ScreenMapping.Viewport(x0, y0, w0, h0, sw, sh));
    }
}
