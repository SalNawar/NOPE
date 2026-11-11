using NUnit.Framework;

/// <summary>The day's weather (CITY-WEATHER): the day's seed draws Clear or Rain against the chance, and a forced state wins.</summary>
public class CityWeatherTests
{
    [Test]
    public void ChanceZero_NeverRains_ChanceOne_AlwaysRains()
    {
        for (int day = 1; day <= 200; day++)
        {
            int seed = Seeds.Day(12345, day);
            Assert.IsFalse(CityWeather.Rains(seed, 0f, null), "day " + day);
            Assert.IsTrue(CityWeather.Rains(seed, 1f, null), "day " + day);
        }
    }

    [Test]
    public void SameSeed_SameWeather_AndTheRateFollowsTheChance()
    {
        int rainy = 0;
        for (int day = 1; day <= 1000; day++)
        {
            int seed = Seeds.Day(777, day);
            bool a = CityWeather.Rains(seed, 0.3f, null);
            Assert.AreEqual(a, CityWeather.Rains(seed, 0.3f, null), "the same seed draws the same weather");
            if (a)
                rainy++;
        }
        Assert.That(rainy, Is.InRange(240, 360), "about 30 percent of 1000 days rain");
    }

    [Test]
    public void ForcedState_WinsOverTheDraw()
    {
        Assert.IsTrue(CityWeather.Rains(1, 0f, true));
        Assert.IsFalse(CityWeather.Rains(1, 1f, false));
    }

    [Test]
    public void RainSource_FallsDownTheScreenAlongTheSlant_RepeatsSeamlessly_AndHoldsUnderReducedMotion()
    {
        (float x0, float y0) = CityWeather.RainSource(1.0, 900f, 256, false);
        (float x1, float y1) = CityWeather.RainSource(1.05, 900f, 256, false);
        Assert.AreEqual(-45f, ((y1 - y0 + 1280f + 640f) % 1280f) - 640f, 0.01f, "45 px up the tile in 0.05 s: the pattern falls 45 px");
        Assert.AreEqual(9f, ((x1 - x0 + 256f + 128f) % 256f) - 128f, 0.01f, "and 9 px right: the pattern slides left, along the slant");
        (float xa, float ya) = CityWeather.RainSource(1280.0 / 900.0 - 1e-6, 900f, 256, false);
        (float xb, float yb) = CityWeather.RainSource(1280.0 / 900.0 + 1e-6, 900f, 256, false);
        Assert.AreEqual(0f, (xb - xa + 128f + 256f) % 256f - 128f, 0.1f, "one period on: the same tile position in x (mod 256)");
        Assert.AreEqual(0f, (yb - ya + 128f + 256f) % 256f - 128f, 0.1f, "and in y (mod 256)");
        Assert.AreEqual((0f, 0f), CityWeather.RainSource(5.0, 900f, 256, true));
    }
}
