using NUnit.Framework;

/// <summary>
/// The desk's one light (DeskLighting, BUG-14): the full day draws the desk as
/// drawn, the night keeps it readable (floor plus lamp at least
/// ReadableNightLevel), the lamp switches on at the afternoon dip and only
/// then adds its warm share, and the colour follows the room by day and the
/// lamp at night.
/// </summary>
public class DeskLightingTests
{
    private const float Tolerance = 1e-4f;

    /// <summary>HallLightingSO's cycle defaults: sunrise 07:00, sunset 16:30, 1.5 h twilights.</summary>
    private static readonly HallDayCycle.Settings Cycle = new HallDayCycle.Settings(7f, 16.5f, 1.5f, 0.6f, 0.1f, 4f);

    /// <summary>HallLightingSO's desk defaults (HallLighting_Default.asset): night floor 0.35, cool night, lamp 0.25 warm, on at evening 0.1.</summary>
    private static readonly DeskLighting.Knobs Knobs = new DeskLighting.Knobs(
        0.35f, new Rgba(0.78f, 0.84f, 1f), 0.25f, new Rgba(1f, 0.82f, 0.6f), 0.1f);

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
    public void TheNightFloor_KeepsTheDeskReadable()
    {
        Assert.That(DeskLighting.NightLevel(Knobs), Is.GreaterThanOrEqualTo(DeskLighting.ReadableNightLevel), "floor plus lamp");
        foreach (float hour in new[] { 20f, 22f, 23f, 23.99f })
        {
            DeskLighting.Light night = DeskLighting.Evaluate(hour, Cycle, Knobs, 1f);
            Assert.That(night.Level, Is.GreaterThanOrEqualTo(DeskLighting.ReadableNightLevel), $"{hour}");
            Assert.AreEqual(DeskLighting.NightLevel(Knobs), night.Level, Tolerance, $"{hour}: the floor plus the warm lamp");
        }
    }

    [Test]
    public void TheDeskNeverFallsBelowTheReadableLevel_OnceTheLampIsOn()
    {
        for (float hour = 9f; hour < 24f; hour += 1f / 60f)
        {
            float warm = DeskLighting.LampOn(hour, Cycle, Knobs) ? 1f : 0f;
            Assert.That(DeskLighting.Evaluate(hour, Cycle, Knobs, warm).Level, Is.GreaterThanOrEqualTo(DeskLighting.ReadableNightLevel), $"{hour:0.00}");
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
        Assert.AreEqual(0.35f, dark.Level, Tolerance);
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
