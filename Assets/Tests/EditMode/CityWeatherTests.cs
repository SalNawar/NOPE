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
}
