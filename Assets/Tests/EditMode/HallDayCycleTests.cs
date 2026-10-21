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
    public void TheNightStages_SwitchOnInTheirOrderAsItDarkens()
    {
        // Saleh's demo 1018a: "need more lights to activate as it gets darker". The evening at 16:30 is a half.
        float[] stages = { 0.1f, 0.4f, 0.55f, 0.7f };
        int Lit(float hour) { int n = 0; foreach (float at in stages) if (HallDayCycle.SwitchedOn(hour, at, 0, 0f, Defaults)) n++; return n; }
        Assert.AreEqual(0, Lit(12f), "noon: none of them");
        int last = 0;
        for (float hour = 15f; hour <= 18.5f; hour += 0.1f)
        {
            int lit = Lit(hour);
            Assert.GreaterOrEqual(lit, last, $"{hour:0.0}: once on, a stage stays on as the evening deepens");
            last = lit;
        }
        Assert.AreEqual(stages.Length, Lit(18.5f), "night: all of them");
        Assert.AreEqual(2, Lit(16.5f), "16:30 (evening 0.5): the stages at 0.1 and 0.4");
    }

    [Test]
    public void ANightStage_StaggersItsLights_AndSwitchesOffInTheSameOrderAtDawn()
    {
        float onAt = 0.5f;
        Assert.IsTrue(HallDayCycle.SwitchedOn(16.6f, onAt, 0, 6f, Defaults));
        Assert.IsFalse(HallDayCycle.SwitchedOn(16.6f, onAt, 3, 6f, Defaults), "the fourth light comes on 18 minutes later");
        Assert.IsTrue(HallDayCycle.SwitchedOn(17f, onAt, 3, 6f, Defaults));
        Assert.IsTrue(HallDayCycle.SwitchedOn(12f, 0f, 0, 0f, Defaults), "a stage at 0 is always on");
        Assert.IsFalse(HallDayCycle.SwitchedOn(23f, 1.5f, 0, 0f, Defaults), "a stage past 1 never comes");
        Assert.IsFalse(HallDayCycle.SwitchedOn(7.2f, onAt, 0, 6f, Defaults), "after sunrise the first is off");
        Assert.IsTrue(HallDayCycle.SwitchedOn(7.2f, onAt, 3, 6f, Defaults), "the fourth goes off 18 minutes later");
    }

    [Test]
    public void TheWarmUps_StartDark_EndFull_AndAreFullAtOnceWithReducedMotion()
    {
        Assert.AreEqual(0f, HallDayCycle.WarmUp(-1f, 0.6f, false), "off");
        Assert.Less(HallDayCycle.WarmUp(0.02f, 0.6f, false), 0.5f, "a dim blink first");
        Assert.Less(HallDayCycle.WarmUp(0.2f, 0.6f, false), HallDayCycle.WarmUp(0.4f, 0.6f, false), "then easing up");
        Assert.AreEqual(1f, HallDayCycle.WarmUp(0.6f, 0.6f, false));
        Assert.AreEqual(1f, HallDayCycle.WarmUp(0.01f, 0.6f, true));

        Assert.AreEqual(0f, HallDayCycle.Neon(-1f, false));
        Assert.AreEqual(0f, HallDayCycle.Neon(0.08f, false), "the neon stutters off");
        Assert.AreEqual(1f, HallDayCycle.Neon(HallDayCycle.NeonSeconds, false));
        Assert.AreEqual(1f, HallDayCycle.Neon(0.08f, true));

        Assert.AreEqual(0f, HallDayCycle.Surge(-1f, 1.2f, false));
        Assert.AreEqual(HallDayCycle.SurgePeak, HallDayCycle.Surge(0.48f, 1.2f, false), Tolerance, "the portal surges past full");
        Assert.AreEqual(1f, HallDayCycle.Surge(1.2f, 1.2f, false), Tolerance, "and settles");
        Assert.AreEqual(1f, HallDayCycle.Surge(0.1f, 1.2f, true));
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

    [Test]
    public void DuskHour_IsTheCheatMenusDusk()
    {
        Assert.AreEqual(18.5f, HallDayCycle.DuskHour);
    }

    [Test]
    public void TheDefaults_AreTheCyclesOneSource()
    {
        // HallLightingSO's field defaults and the cycle before a rig publishes read HallDayCycle.Defaults (no copy by hand).
        Assert.AreEqual(7f, HallDayCycle.Defaults.SunriseHour);
        Assert.AreEqual(16.5f, HallDayCycle.Defaults.SunsetHour);
        Assert.AreEqual(1.5f, HallDayCycle.Defaults.TwilightHours);
        Assert.AreEqual(0.6f, HallDayCycle.Defaults.FixturesOnBelow);
        Assert.AreEqual(0.1f, HallDayCycle.Defaults.FixtureFadeBand);
        Assert.AreEqual(4f, HallDayCycle.Defaults.FixtureStaggerMinutes);
    }

    [Test]
    public void HourAtEvening_IsTheDusksInverse()
    {
        Assert.AreEqual(15.75f, HallDayCycle.HourAtEvening(0f, Defaults), 1e-3f, "the dusk starts half a twilight before sunset");
        Assert.AreEqual(16.5f, HallDayCycle.HourAtEvening(0.5f, Defaults), 1e-3f);
        Assert.AreEqual(17.25f, HallDayCycle.HourAtEvening(1f, Defaults), 1e-3f, "night falls half a twilight after it");
        for (float e = 0.05f; e < 1f; e += 0.05f)
            Assert.AreEqual(e, HallDayCycle.Evening(HallDayCycle.HourAtEvening(e, Defaults), Defaults), 1e-3f, $"evening {e:0.00}");
        Assert.AreEqual(17.25f, HallDayCycle.HourAtEvening(5f, Defaults), 1e-3f, "clamped");
    }

    [Test]
    public void TheShownHour_IsNeverNoonBeforeTheClockExists()
    {
        // BUG-25c: a 16:00 shift's office load showed a noon hall for a frame (the edit-mode hour before the clock).
        Assert.AreEqual(16f, HallDayCycle.ShownHour(null, false, 22f, null, 960f, 12f), Tolerance, "today's opening before the clock");
        Assert.AreEqual(16.5f, HallDayCycle.ShownHour(null, false, 22f, 990f, 960f, 12f), Tolerance, "the clock once it runs");
        Assert.AreEqual(22f, HallDayCycle.ShownHour(null, true, 22f, 990f, 960f, 12f), Tolerance, "the preview over the clock");
        Assert.AreEqual(18.5f, HallDayCycle.ShownHour(18.5f, true, 22f, 990f, 960f, 12f), Tolerance, "the cheat's forced hour over all");
        Assert.AreEqual(12f, HallDayCycle.ShownHour(null, false, 22f, null, null, 12f), Tolerance, "edit mode without a run: the knob's hour");
    }

    [Test]
    public void OneCycle_GivesTheHallLampsWindowsAndCalendarTheSameHour()
    {
        // The painting's night share (HallBakedCycle), the lamps' and city windows' switching (SwitchedOn of the evening)
        // and the calendar ink (AnimeHallShiftLink reads Evening) all read the evening of the same hour on the same knobs.
        for (float h = 0f; h < 24f; h += 1f / 12f)
        {
            float evening = HallDayCycle.Evening(h, Defaults);
            Assert.AreEqual(evening, HallBakedCycle.Weights(h, Defaults).w, 1e-5f, $"{h:0.00}: the painting");
            foreach (float stage in new[] { 0.1f, 0.4f, 0.55f, 0.7f })
                Assert.AreEqual(evening >= stage, HallDayCycle.SwitchedOn(h, stage, 0, 0f, Defaults), $"{h:0.00}: the stage at {stage}");
        }
    }
}
