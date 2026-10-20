using NUnit.Framework;

/// <summary>
/// The desk's one light (DeskLighting, BUG-14): the full day draws the desk as
/// drawn, the night keeps the whole mat readable (luminance, with the lamp
/// pool's falloff, at the mat's corners at least ReadableNightLuminance: luma
/// 55; at the pool's centre TargetNightLuminance: luma 60), the lamp switches
/// on at the afternoon dip and only then adds its warm share, and the colour
/// follows the room by day and the lamp at night.
/// </summary>
public class DeskLightingTests
{
    private const float Tolerance = 1e-4f;

    /// <summary>HallLightingSO's cycle defaults: sunrise 07:00, sunset 16:30, 1.5 h twilights.</summary>
    private static readonly HallDayCycle.Settings Cycle = new HallDayCycle.Settings(7f, 16.5f, 1.5f, 0.6f, 0.1f, 4f);

    /// <summary>HallLightingSO's desk defaults (HallLighting_Default.asset): night floor 0.5, cool night, lamp 0.38 warm, on at evening 0.1.</summary>
    private static readonly DeskLighting.Knobs Knobs = new DeskLighting.Knobs(
        0.5f, new Rgba(0.78f, 0.84f, 1f), 0.38f, new Rgba(1f, 0.82f, 0.6f), 0.1f);

    /// <summary>The lamp pool's radius (HallLighting_Default.deskLampRadius).</summary>
    private const float Radius = 1.5f;

    /// <summary>The mat: the DeskSurface anchor, the blotter's 1.45 × 1.02 m (docs/SCENE_CONTRACT_GAMEPLAY.md).</summary>
    private const float MatWidth = 1.45f, MatDepth = 1.02f;

    [Test]
    public void TheFullDay_DrawsTheDeskAsDrawn()
    {
        DeskLighting.Light noon = DeskLighting.Evaluate(12f, Cycle, Knobs, 0f);
        Assert.AreEqual(1f, noon.Level, Tolerance);
        Assert.AreEqual(1f, noon.R, Tolerance); Assert.AreEqual(1f, noon.G, Tolerance); Assert.AreEqual(1f, noon.B, Tolerance);
        Assert.AreEqual(0f, noon.LampShare, Tolerance);
        Assert.IsFalse(DeskLighting.LampOn(12f, Cycle, Knobs), "no lamp at noon");
    }

    [Test]
    public void TheReadableLine_IsLuma55_AndTheTargetLuma60()
    {
        // The plan's lines are lumas on screen; the light is linear: the sRGB-decoded ratios to the day mat's 76.
        Assert.AreEqual(0.529f, DeskLighting.ReadableNightLuminance, 1e-3f);
        Assert.AreEqual(0.625f, DeskLighting.TargetNightLuminance, 1e-3f);
        Assert.AreEqual(55f, DeskLighting.LumaAt(DeskLighting.ReadableNightLuminance), 0.01f);
        Assert.AreEqual(76f, DeskLighting.LumaAt(1f), 0.01f);
        Assert.AreEqual(1f, DeskLighting.Luminance(1f, 1f, 1f), 1e-5f);
    }

    [Test]
    public void ThePoolWeight_IsTheShadersFalloff()
    {
        Assert.AreEqual(1f, DeskLighting.PoolWeight(0f, Radius), Tolerance, "full at the centre");
        Assert.AreEqual(1f, DeskLighting.PoolWeight(0.25f * Radius, Radius), Tolerance, "full out to a quarter of the radius");
        Assert.AreEqual(0.5f, DeskLighting.PoolWeight(0.625f * Radius, Radius), Tolerance, "half way through the falloff");
        Assert.AreEqual(0f, DeskLighting.PoolWeight(Radius, Radius), Tolerance, "none at the radius");
        Assert.AreEqual(0f, DeskLighting.PoolWeight(3f, Radius), Tolerance);
    }

    [Test]
    public void TheNight_KeepsTheWholeMatReadable_ItsCornersIncluded()
    {
        DeskLighting.Light night = DeskLighting.FullNight(Knobs);
        float centre = DeskLighting.LuminanceAt(night, 1f);
        float corner = DeskLighting.DarkestOnMat(night, Radius, MatWidth, MatDepth);
        Assert.That(centre, Is.GreaterThanOrEqualTo(DeskLighting.TargetNightLuminance), $"the pool's centre: luma {DeskLighting.LumaAt(centre):0.0}");
        Assert.That(corner, Is.GreaterThanOrEqualTo(DeskLighting.ReadableNightLuminance), $"the mat's corners: luma {DeskLighting.LumaAt(corner):0.0}");
        Assert.That(corner, Is.LessThan(centre), "a pool: brighter under the lamp than at the mat's corners");
        foreach (float hour in new[] { 20f, 22f, 23f, 23.99f })
        {
            DeskLighting.Light l = DeskLighting.Evaluate(hour, Cycle, Knobs, 1f);
            Assert.AreEqual(night.Level, l.Level, Tolerance, $"{hour}: full night");
            Assert.AreEqual(corner, DeskLighting.DarkestOnMat(l, Radius, MatWidth, MatDepth), 1e-4f, $"{hour}");
        }
    }

    [Test]
    public void TheOldKnobs_FailTheRule()
    {
        // The first LIGHT tune (floor 0.35, lamp 0.25, a 0.45 m pool) passed a level-only rule but left the mat's corners
        // at about luma 40 and the centre at about 54: the rule must catch it.
        var old = new DeskLighting.Knobs(0.35f, new Rgba(0.78f, 0.84f, 1f), 0.25f, new Rgba(1f, 0.82f, 0.6f), 0.1f);
        DeskLighting.Light night = DeskLighting.FullNight(old);
        Assert.That(DeskLighting.DarkestOnMat(night, 0.45f, MatWidth, MatDepth), Is.LessThan(DeskLighting.ReadableNightLuminance));
        Assert.That(DeskLighting.LuminanceAt(night, 1f), Is.LessThan(DeskLighting.TargetNightLuminance));
    }

    [Test]
    public void TheMatNeverFallsBelowTheReadableLine_OnceTheLampIsOn()
    {
        for (float hour = 0f; hour < 24f; hour += 1f / 60f)
        {
            float warm = DeskLighting.LampOn(hour, Cycle, Knobs) ? 1f : 0f;
            DeskLighting.Light l = DeskLighting.Evaluate(hour, Cycle, Knobs, warm);
            Assert.That(DeskLighting.DarkestOnMat(l, Radius, MatWidth, MatDepth), Is.GreaterThanOrEqualTo(DeskLighting.ReadableNightLuminance), $"{hour:0.00}");
        }
    }

    [Test]
    public void TheLampOnHour_IsTheAfternoonDip()
    {
        float dip = DeskLighting.LampOnHour(Cycle, Knobs);
        Assert.That(dip, Is.InRange(15.75f, 16.5f), "within the dusk, before the sunset");
        Assert.AreEqual(Knobs.LampOnAtEvening, HallDayCycle.Evening(dip, Cycle), 1e-3f, "where the hall's evening reaches the lamp's knob");
        Assert.IsFalse(DeskLighting.LampOn(dip - 1f / 60f, Cycle, Knobs), "off a minute before the dip");
        Assert.IsTrue(DeskLighting.LampOn(dip + 1f / 60f, Cycle, Knobs), "on a minute after it");
        for (float hour = 9f; hour < dip - 1f / 60f; hour += 1f / 60f)
            Assert.IsFalse(DeskLighting.LampOn(hour, Cycle, Knobs), $"{hour:0.00}: never earlier");
    }

    [Test]
    public void TheLamp_AddsItsWarmShare_AsItWarms()
    {
        float hour = DeskLighting.LampOnHour(Cycle, Knobs) + 0.1f;
        DeskLighting.Light cold = DeskLighting.Evaluate(hour, Cycle, Knobs, 0f);
        DeskLighting.Light half = DeskLighting.Evaluate(hour, Cycle, Knobs, 0.5f);
        DeskLighting.Light warm = DeskLighting.Evaluate(hour, Cycle, Knobs, 1f);
        Assert.AreEqual(0f, cold.LampShare, Tolerance);
        Assert.AreEqual(Knobs.LampLevel * 0.5f, half.LampShare, Tolerance);
        Assert.AreEqual(cold.Level + Knobs.LampLevel, warm.Level, Tolerance, "the switch-on brightens the pool");
        Assert.That(warm.B, Is.LessThan(cold.B), "and warms it");
    }

    [Test]
    public void TheNight_IsCoolWithoutTheLamp_AndWarmerWithIt()
    {
        DeskLighting.Light dark = DeskLighting.Evaluate(23f, Cycle, Knobs, 0f);
        Assert.AreEqual(0.5f, dark.Level, Tolerance);
        Assert.AreEqual(0.78f, dark.R, Tolerance); Assert.AreEqual(1f, dark.B, Tolerance);
        DeskLighting.Light lit = DeskLighting.Evaluate(23f, Cycle, Knobs, 1f);
        Assert.That(lit.R / lit.B, Is.GreaterThan(dark.R / dark.B), "the lamp warms the night");
    }

    [Test]
    public void OddKnobs_AreRepaired()
    {
        var odd = new DeskLighting.Knobs(-1f, new Rgba(2f, -1f, 0.5f), -3f, new Rgba(5f, 5f, 5f), float.NaN);
        DeskLighting.Light l = DeskLighting.Evaluate(23f, Cycle, odd, 1f);
        Assert.That(l.Level, Is.InRange(0f, 2f));
        Assert.That(l.R, Is.InRange(0f, 1f)); Assert.That(l.G, Is.InRange(0f, 1f)); Assert.That(l.B, Is.InRange(0f, 1f));
        Assert.DoesNotThrow(() => DeskLighting.LampOnHour(Cycle, odd));
    }
}
