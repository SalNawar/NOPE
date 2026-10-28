using NUnit.Framework;

/// <summary>
/// The office overlay's view plates and the guide's quiet plate under the
/// painted desk (Saleh 2026-10-28: "where is all the ui?": the CITY [A],
/// PC [Q] and STAMPS [TAB] plates show again in the office view), at both
/// shipped layouts: 1920x1080 (16:9) and 1280x1024 (5:4, where the Expand
/// canvas is 1920 by 1536). The quiet plate docked in the top-right corner
/// before, over the STAMPS tab's right part (canvas x 1392 to 1503, y 16 to 64);
/// it docks top-left now, clear of every plate, and every plate lies on the
/// canvas (ChromeLayout, which Build Office UI and GuidePrompt.Quiet read).
/// </summary>
public class ChromeLayoutTests
{
    [TestCase(1920f, 1080f, 1920f, 1080f)]
    [TestCase(1280f, 1024f, 1920f, 1536f)]
    [TestCase(2560f, 1080f, 2560f, 1080f)]
    public void TheCanvas_ExpandsFromTheReference(float screenWidth, float screenHeight, float width, float height)
    {
        (float w, float h) = ChromeLayout.Canvas(screenWidth, screenHeight);
        Assert.AreEqual(width, w, 0.01f);
        Assert.AreEqual(height, h, 0.01f);
    }

    [TestCase(1920f, 1080f)]
    [TestCase(1280f, 1024f)]
    public void TheQuietGuidePlate_CoversNoViewPlate(float screenWidth, float screenHeight)
    {
        (float width, float height) = ChromeLayout.Canvas(screenWidth, screenHeight);
        ChromeLayout.Box plate = ChromeLayout.QuietPlate();
        Assert.IsTrue(plate.Inside(width, height), $"the plate {plate} on the {width}x{height} canvas");
        Assert.IsFalse(plate.Overlaps(ChromeLayout.StampTab(width)), $"the plate {plate} over the STAMPS tab {ChromeLayout.StampTab(width)}");
        Assert.IsFalse(plate.Overlaps(ChromeLayout.CityTab(height)), $"the plate {plate} over the CITY tab {ChromeLayout.CityTab(height)}");
        Assert.IsFalse(plate.Overlaps(ChromeLayout.PcTab(height)), $"the plate {plate} over the PC tab {ChromeLayout.PcTab(height)}");
    }

    [TestCase(1920f, 1080f)]
    [TestCase(1280f, 1024f)]
    public void TheViewPlates_LieOnTheCanvas_ApartFromEachOther(float screenWidth, float screenHeight)
    {
        (float width, float height) = ChromeLayout.Canvas(screenWidth, screenHeight);
        ChromeLayout.Box stamps = ChromeLayout.StampTab(width), city = ChromeLayout.CityTab(height), pc = ChromeLayout.PcTab(height);
        Assert.IsTrue(stamps.Inside(width, height), $"STAMPS {stamps}");
        Assert.IsTrue(city.Inside(width, height), $"CITY {city}");
        Assert.IsTrue(pc.Inside(width, height), $"PC {pc}");
        Assert.IsFalse(city.Overlaps(pc), $"CITY {city} and PC {pc}");
        Assert.IsFalse(stamps.Overlaps(city), $"STAMPS {stamps} and CITY {city}");
    }

    [Test]
    public void ATopRightDock_WouldCoverTheStampsTab()
    {
        // The dock desk v3's round 5 used (anchored top right, the same margin and scale): the reason it moved.
        float w = ChromeLayout.GuidePlateWidth * ChromeLayout.QuietPlateScale, h = ChromeLayout.GuidePlateTall * ChromeLayout.QuietPlateScale;
        var topRight = new ChromeLayout.Box(1920f - ChromeLayout.QuietMargin - w, ChromeLayout.QuietMargin, 1920f - ChromeLayout.QuietMargin, ChromeLayout.QuietMargin + h);
        Assert.IsTrue(topRight.Overlaps(ChromeLayout.StampTab(1920f)));
    }
}
