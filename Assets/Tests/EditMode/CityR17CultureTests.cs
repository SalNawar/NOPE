using NUnit.Framework;

public sealed class CityR17CultureTests
{
    private const double MinW = 42, MinH = 18;   // the config's defaults

    [Test]
    public void Takes_NeedsBothSidesOfTheEmblem()
    {
        Assert.IsTrue(CityR17Culture.Takes(new CityR17Rect(0, 0, 44, 20), MinW, MinH));
        Assert.IsFalse(CityR17Culture.Takes(new CityR17Rect(0, 0, 41, 60), MinW, MinH));
        Assert.IsFalse(CityR17Culture.Takes(new CityR17Rect(0, 0, 90, 18), MinW, MinH));
    }

    [Test]
    public void Layout_BandAtFootEmblemAboveInsideTheBox()
    {
        var box = new CityR17Rect(100, 200, 80, 48);
        CityR17Culture.Layout(box, out CityR17Rect band, out CityR17Rect emblem);
        Assert.AreEqual(box.Y + box.H, band.Y + band.H, 1e-9);
        Assert.Greater(band.H, 0);
        Assert.Less(band.H, box.H * 0.2);
        Assert.LessOrEqual(emblem.Y + emblem.H, band.Y + 1e-9);
        Assert.GreaterOrEqual(emblem.X, box.X - 1e-9);
        Assert.LessOrEqual(emblem.X + emblem.W, box.X + box.W + 1e-9);
        Assert.GreaterOrEqual(emblem.Y, box.Y - 1e-9);
    }

    [Test]
    public void Layout_ATallBoxGetsASquareEmblem()
    {
        CityR17Culture.Layout(new CityR17Rect(0, 0, 56, 158), out _, out CityR17Rect emblem);
        Assert.AreEqual(56, emblem.W, 1e-9);
        Assert.AreEqual(56, emblem.H, 1e-9);
    }

    [Test]
    public void Crop_KeepsTheBoxAspectAroundThePlateCentre()
    {
        CityR17Rect c = CityR17Culture.Crop(new CityR17Rect(0, 0, 60, 30));
        Assert.AreEqual(2.0, c.W / c.H, 1e-9);
        Assert.AreEqual(CityR17Culture.PlateCenterX, c.X + c.W / 2, 1e-9);
        Assert.AreEqual(CityR17Culture.PlateCenterY, c.Y + c.H / 2, 1e-9);
    }

    [Test]
    public void SceneBoxes_StayInTheirRectAndNeverOverlap()
    {
        CityR17Screen[] s = CityR17Fixture.Scene.Ads.Screens;
        for (int i = 0; i < s.Length; i++)
        {
            CityR17Rect r = s[i].Rect, b = s[i].Box;
            Assert.IsTrue(b.X >= r.X && b.Y >= r.Y && b.X + b.W <= r.X + r.W && b.Y + b.H <= r.Y + r.H, s[i].Id + " box leaves its rect");
            for (int j = i + 1; j < s.Length; j++)
            {
                CityR17Rect c = s[j].Box;
                bool overlap = b.X < c.X + c.W && c.X < b.X + b.W && b.Y < c.Y + c.H && c.Y < b.Y + b.H;
                Assert.IsFalse(overlap, s[i].Id + " and " + s[j].Id + " boxes overlap");
            }
        }
    }

    [Test]
    public void Scene_ThirteenOrMoreScreensTakeTheCultureAtTheDefaults()
    {
        int n = 0;
        foreach (CityR17Screen s in CityR17Fixture.Scene.Ads.Screens)
            if (CityR17Culture.Takes(s.Box, MinW, MinH))
                n++;
        Assert.GreaterOrEqual(n, 14, "12 in the city view plus B5 and B6, which sit outside it");
    }
}
