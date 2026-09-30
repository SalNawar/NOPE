using NUnit.Framework;

/// <summary>
/// The anime hall's day-night cycle (HallDayCycle, read by HallLightingRig):
/// the hour from the shift clock's minute, the daylight through dawn, day,
/// dusk and night, the solar position the colour gradients are read at, the
/// sun's arc the window shafts turn along, the interior fixtures switching on
/// at dusk with a stagger, and their switch-on flicker (none with reduced motion).
/// </summary>
public class HallDayCycleTests
{
    private const float Tolerance = 1e-4f;

    /// <summary>The defaults of HallLightingSO: sunrise 07:00, sunset 16:30, 1.5 h twilights, fixtures on below 0.6 daylight over a 0.1 band, 4 minutes apart.</summary>
    private static readonly HallDayCycle.Settings Defaults = new HallDayCycle.Settings(7f, 16.5f, 1.5f, 0.6f, 0.1f, 4f);

    [Test]
    public void TheHour_ComesFromTheClockMinute_WrappedIntoTheDay()
    {
        Assert.AreEqual(9f, HallDayCycle.Hour(540f), Tolerance);
        Assert.AreEqual(17f, HallDayCycle.Hour(1020f), Tolerance);
        Assert.AreEqual(0f, HallDayCycle.Hour(1440f), Tolerance);
        Assert.AreEqual(23f, HallDayCycle.Hour(-60f), Tolerance);
        Assert.AreEqual(12f, HallDayCycle.Hour(float.NaN), Tolerance, "a clock that is not a number reads as noon");
    }

    [Test]
    public void Daylight_IsFullByDay_NoneAtNight_HalfAtSunriseAndSunset()
    {
        Assert.AreEqual(1f, HallDayCycle.Daylight(12f, Defaults), Tolerance);
        Assert.AreEqual(1f, HallDayCycle.Daylight(9f, Defaults), Tolerance, "the shift opens in full day");
        Assert.AreEqual(0f, HallDayCycle.Daylight(2f, Defaults), Tolerance);
        Assert.AreEqual(0f, HallDayCycle.Daylight(22f, Defaults), Tolerance);
        Assert.AreEqual(0.5f, HallDayCycle.Daylight(7f, Defaults), Tolerance);
        Assert.AreEqual(0.5f, HallDayCycle.Daylight(16.5f, Defaults), Tolerance);
    }

    [Test]
    public void Daylight_RisesThroughDawn_AndFallsThroughDusk()
    {
        float early = HallDayCycle.Daylight(6.5f, Defaults), late = HallDayCycle.Daylight(7.5f, Defaults);
        Assert.That(early, Is.GreaterThan(0f).And.LessThan(0.5f));
        Assert.That(late, Is.GreaterThan(0.5f).And.LessThan(1f));
        Assert.That(HallDayCycle.Daylight(16f, Defaults), Is.GreaterThan(HallDayCycle.Daylight(17f, Defaults)));
        Assert.AreEqual(0f, HallDayCycle.Daylight(17.25f, Defaults), Tolerance, "night falls a half twilight after sunset");
    }

    [Test]
    public void TheEvening_IsTheDaylightsComplement()
    {
        Assert.AreEqual(0f, HallDayCycle.Evening(12f, Defaults), Tolerance);
        Assert.AreEqual(1f, HallDayCycle.Evening(23f, Defaults), Tolerance);
        Assert.AreEqual(0.5f, HallDayCycle.Evening(16.5f, Defaults), Tolerance);
    }

    [Test]
    public void TheSolarPosition_MapsMidnightSunriseNoonAndSunset_ToQuarters()
    {
        Assert.AreEqual(0.25f, HallDayCycle.SolarPosition(7f, Defaults), Tolerance);
        Assert.AreEqual(0.75f, HallDayCycle.SolarPosition(16.5f, Defaults), Tolerance);
        Assert.AreEqual(0.5f, HallDayCycle.SolarPosition(11.75f, Defaults), Tolerance, "solar noon halfway between sunrise and sunset");
        float midnight = HallDayCycle.SolarPosition(23.75f, Defaults);
        Assert.That(midnight < 0.001f || midnight > 0.999f, $"solar midnight halfway through the night, got {midnight}");
        Assert.That(HallDayCycle.SolarPosition(18f, Defaults), Is.GreaterThan(0.75f).And.LessThan(1f));
        Assert.That(HallDayCycle.SolarPosition(3f, Defaults), Is.GreaterThan(0f).And.LessThan(0.25f));
    }

    [Test]
    public void TheSunsArc_RunsFromSunriseToSunset_AndHoldsItsEndsAtNight()
    {
        Assert.AreEqual(0f, HallDayCycle.SunArc(7f, Defaults), Tolerance);
        Assert.AreEqual(1f, HallDayCycle.SunArc(16.5f, Defaults), Tolerance);
        Assert.AreEqual(0.5f, HallDayCycle.SunArc(11.75f, Defaults), Tolerance);
        Assert.AreEqual(0f, HallDayCycle.SunArc(3f, Defaults), Tolerance);
        Assert.AreEqual(1f, HallDayCycle.SunArc(20f, Defaults), Tolerance);
    }

    [Test]
    public void TheFixtures_AreOffByDay_OnAtNight()
    {
        for (int i = 0; i < 6; i++)
        {
            Assert.AreEqual(0f, HallDayCycle.FixtureLevel(12f, i, Defaults), Tolerance, $"fixture {i} at noon");
            Assert.AreEqual(1f, HallDayCycle.FixtureLevel(22f, i, Defaults), Tolerance, $"fixture {i} at night");
        }
    }

    [Test]
    public void AtDusk_TheFixturesSwitchOnOneAfterAnother()
    {
        // Find the first minute fixture 0 is fully on; the later ones follow in order, the stagger apart.
        float first = -1f;
        for (float h = 15f; h < 18f; h += 1f / 60f)
            if (HallDayCycle.FixtureLevel(h, 0, Defaults) >= 1f) { first = h; break; }
        Assert.That(first, Is.GreaterThan(15f), "fixture 0 switches on during the dusk");
        Assert.AreEqual(1f, HallDayCycle.FixtureLevel(first, 0, Defaults), Tolerance);
        Assert.That(HallDayCycle.FixtureLevel(first, 1, Defaults), Is.LessThan(1f), "fixture 1 is not yet on");
        Assert.AreEqual(1f, HallDayCycle.FixtureLevel(first + 4f / 60f, 1, Defaults), Tolerance, "fixture 1 is on the stagger later");
        Assert.That(HallDayCycle.FixtureLevel(first + 4f / 60f, 3, Defaults), Is.LessThan(1f), "fixture 3 still waits");
    }

    [Test]
    public void AtDawn_TheFixturesSwitchOffInTheSameOrder()
    {
        float off = -1f;
        for (float h = 5f; h < 9f; h += 1f / 60f)
            if (HallDayCycle.FixtureLevel(h, 0, Defaults) <= 0f) { off = h; break; }
        Assert.That(off, Is.GreaterThan(5f));
        Assert.That(HallDayCycle.FixtureLevel(off, 2, Defaults), Is.GreaterThan(0f), "a later fixture is still on");
    }

    [Test]
    public void TheSwitchOnFlicker_BlinksBriefly_ThenStaysOn_AndNeverWithReducedMotion()
    {
        bool dark = false;
        for (float t = 0f; t < HallDayCycle.FlickerSeconds; t += 0.01f)
            dark |= HallDayCycle.Flicker(t, reduced: false) < 0.5f;
        Assert.IsTrue(dark, "the fixture blinks while it strikes");
        Assert.AreEqual(1f, HallDayCycle.Flicker(HallDayCycle.FlickerSeconds + 0.01f, reduced: false), Tolerance);
        Assert.AreEqual(1f, HallDayCycle.Flicker(10f, reduced: false), Tolerance);
        for (float t = 0f; t < HallDayCycle.FlickerSeconds; t += 0.01f)
            Assert.AreEqual(1f, HallDayCycle.Flicker(t, reduced: true), Tolerance, "no flicker with reduced motion");
        Assert.AreEqual(1f, HallDayCycle.Flicker(-1f, reduced: false), Tolerance, "no flicker before a switch-on");
    }

    [Test]
    public void InvalidSettings_AreRepaired_SoTheCycleStillRuns()
    {
        var odd = new HallDayCycle.Settings(18f, 6f, -1f, 2f, 0f, -3f);
        Assert.DoesNotThrow(() => HallDayCycle.Daylight(12f, odd));
        float d = HallDayCycle.Daylight(12f, odd);
        Assert.That(d, Is.InRange(0f, 1f));
        Assert.That(HallDayCycle.FixtureLevel(12f, 2, odd), Is.InRange(0f, 1f));
        Assert.That(HallDayCycle.SolarPosition(12f, odd), Is.InRange(0f, 1f));
    }
}
