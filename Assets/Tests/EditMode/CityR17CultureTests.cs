using NUnit.Framework;

public sealed class CityR17CultureTests
{
    [Test]
    public void Takes_NeedsBothSides()
    {
        Assert.IsTrue(CityR17Culture.Takes(new CityR17Rect(0, 0, 64, 48), 64, 48));
        Assert.IsFalse(CityR17Culture.Takes(new CityR17Rect(0, 0, 48, 56), 64, 48));
        Assert.IsFalse(CityR17Culture.Takes(new CityR17Rect(0, 0, 64, 40), 64, 48));
    }

    [Test]
    public void Layout_BandAtFootEmblemAboveInside()
    {
        var screen = new CityR17Rect(100, 200, 80, 48);
        CityR17Culture.Layout(screen, out CityR17Rect band, out CityR17Rect emblem);
        Assert.AreEqual(screen.Y + screen.H, band.Y + band.H, 1e-9);
        Assert.Greater(band.H, 0);
        Assert.LessOrEqual(emblem.Y + emblem.H, band.Y);
        Assert.GreaterOrEqual(emblem.X, screen.X);
        Assert.LessOrEqual(emblem.X + emblem.W, screen.X + screen.W);
    }

    [Test]
    public void Crop_KeepsTheBoxAspectAroundThePlateCentre()
    {
        CityR17Rect c = CityR17Culture.Crop(new CityR17Rect(0, 0, 60, 30));
        Assert.AreEqual(2.0, c.W / c.H, 1e-9);
        Assert.AreEqual(CityR17Culture.PlateCenterX, c.X + c.W / 2, 1e-9);
        Assert.AreEqual(CityR17Culture.PlateCenterY, c.Y + c.H / 2, 1e-9);
    }
}
