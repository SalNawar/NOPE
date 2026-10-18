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

    // The office's edge pull tabs keep clear of the calendar (Saleh 2026-10-18: "the pc ui label is over the calander").

    [Test]
    public void ClearShift_ASpanClearOfTheObstacle_StaysPut()
    {
        Assert.AreEqual(0f, ScreenMapping.ClearShift(364f, 500f, 520f, 600f, 10f, 0f, 1080f));
        Assert.AreEqual(0f, ScreenMapping.ClearShift(700f, 836f, 476f, 571f, 10f, 0f, 1080f));
    }

    [Test]
    public void ClearShift_ASpanBelowTheObstaclesCentre_MovesDownJustClearOfIt()
    {
        // The PC tab (364-500) under the calendar (476-571) at 1920x1080: its top goes to 476 - 10.
        float d = ScreenMapping.ClearShift(364f, 500f, 476f, 571f, 10f, 0f, 1080f);
        Assert.AreEqual(466f - 500f, d, 1e-4f);
    }

    [Test]
    public void ClearShift_ASpanAboveTheObstaclesCentre_MovesUpJustClearOfIt()
    {
        float d = ScreenMapping.ClearShift(560f, 696f, 476f, 571f, 10f, 0f, 1080f);
        Assert.AreEqual(581f - 560f, d, 1e-4f);
    }

    [Test]
    public void ClearShift_BlockedByTheScreensEdge_GoesTheOtherWay()
    {
        // Below the obstacle's centre, but no room under it: it goes above.
        float d = ScreenMapping.ClearShift(20f, 120f, 60f, 200f, 10f, 0f, 1080f);
        Assert.AreEqual(210f - 20f, d, 1e-4f);
    }

    [TestCase(0.66f, 1f, true)]  // the STAMPS tab on the top edge (the desk layout's fixed x)
    [TestCase(0.5f, 0f, true)]   // a tab on the bottom edge
    [TestCase(0f, 0.55f, false)] // the PC tab on the left edge
    [TestCase(1f, 0.5f, false)]  // a tab on the right edge
    [TestCase(0f, 1f, false)]    // a corner: up or down
    public void SlidesAlongX_TopAndBottomEdgeTabsSlideSideways(float ax, float ay, bool expected)
    {
        Assert.AreEqual(expected, ScreenMapping.SlidesAlongX(ax, ay));
    }

    [Test]
    public void ClearShift_WithNoRoomEitherWay_StaysOnTheScreen()
    {
        float d = ScreenMapping.ClearShift(100f, 200f, 0f, 300f, 10f, 0f, 300f);
        Assert.GreaterOrEqual(100f + d, 0f);
        Assert.LessOrEqual(200f + d, 300f);
    }
}
