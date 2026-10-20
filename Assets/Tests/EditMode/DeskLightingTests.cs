using NUnit.Framework;

/// <summary>
/// The desk's one light (DeskLighting, BUG-14): the full day draws the desk as
/// drawn and the level never passes it (the lamp fills only what the room has
/// lost); the night keeps the whole mat readable on the image shown (after
/// the office finish's exposure, Neutral tonemapping and contrast, the mat's
/// corners clear luma 55 with a margin and the pool's centre reaches 60) and
/// the lamp reads as a pool (a luma step and a hue that fall off from its
/// centre); the lamp switches on at the sunset (before the plan's afternoon dip,
/// H3), visibly, and only then
/// adds its warm share; the colour follows the room by day and the lamp at night.
/// </summary>
public class DeskLightingTests
{
    private const float Tolerance = 1e-4f;

    /// <summary>HallLightingSO's cycle defaults: sunrise 07:00, sunset 16:30, 1.5 h twilights.</summary>
    private static readonly HallDayCycle.Settings Cycle = new HallDayCycle.Settings(7f, 16.5f, 1.5f, 0.6f, 0.1f, 4f);

    /// <summary>HallLightingSO's desk defaults (HallLighting_Default.asset): night floor 0.55, cool night, lamp 0.45 warm, on at evening 0.5 (the sunset).</summary>
    private static readonly DeskLighting.Knobs Knobs = new DeskLighting.Knobs(
        0.55f, new Rgba(0.78f, 0.84f, 1f), 0.45f, new Rgba(1f, 0.82f, 0.6f), 0.5f);

    /// <summary>The lamp pool's radius (HallLighting_Default.deskLampRadius).</summary>
    private const float Radius = 1.35f;

    /// <summary>The first verified tune (round 1): floor 0.5, lamp 0.38, on at evening 0.1 (16:03), a 1.5 m pool.</summary>
    private static readonly DeskLighting.Knobs Round1 = new DeskLighting.Knobs(
        0.5f, new Rgba(0.78f, 0.84f, 1f), 0.38f, new Rgba(1f, 0.82f, 0.6f), 0.1f);

    /// <summary>The mat: the DeskSurface anchor, the blotter's 1.45 × 1.02 m (docs/SCENE_CONTRACT_GAMEPLAY.md).</summary>
    private const float MatWidth = 1.45f, MatDepth = 1.02f;

    private static readonly DeskLighting.Finish Office = DeskLighting.Finish.Office;

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
    public void TheFinish_IsModelled_AndCostsTheDimMat()
    {
        // No finish: the drawn image.
        foreach (float l in new[] { 0.3f, 0.529f, 0.8f, 1f })
            Assert.AreEqual(DeskLighting.LumaAt(l), DeskLighting.GradedLuma(l, DeskLighting.Finish.None), 0.05f, $"{l}");
        // The office finish leaves the day mat about as drawn (+0.1 EV and Neutral lift it, the contrast takes it back)...
        Assert.AreEqual(76f, DeskLighting.GradedLuma(1f, Office), 1f, "the day mat");
        // ...but pushes a dim mat further down: the unfinished line (luma 55) shows at about 52.5, so the rule is judged after it.
        float line = DeskLighting.GradedLuma(DeskLighting.ReadableNightLuminance, Office);
        Assert.AreEqual(52.5f, line, 0.5f);
        Assert.That(line, Is.LessThan(DeskLighting.ReadableMatLuma));
        // Both grading orders are modelled (LDR: the tonemap, then the contrast; HDR: the other way round); on the mat's
        // range they differ by under a luma, so the rule does not hang on the URP asset's grading mode.
        var hdr = new DeskLighting.Finish(0.1f, 12f, true, true);
        foreach (float l in new[] { 0.3f, 0.6f, 1f })
        {
            float gap = System.Math.Abs(DeskLighting.GradedLuma(l, hdr) - DeskLighting.GradedLuma(l, Office));
            Assert.That(gap, Is.GreaterThan(0f).And.LessThan(1f), $"{l}");
        }
        // Darker light never shows brighter.
        float last = -1f;
        for (float l = 0f; l <= 1f; l += 0.01f)
        {
            float g = DeskLighting.GradedLuma(l, Office);
            Assert.That(g, Is.GreaterThanOrEqualTo(last), $"{l}");
            last = g;
        }
    }

    [Test]
    public void TheVignette_IsUrpsFrame()
    {
        Assert.AreEqual(1f, DeskLighting.VignetteAt(0f, 0f, 0.2f, 0.65f), Tolerance, "none at the centre");
        Assert.AreEqual(0.525f, DeskLighting.VignetteAt(0.5f, 0.5f, 0.2f, 0.65f), 0.005f, "the screen's corner: (1 - 0.18)^3.25");
        Assert.AreEqual(0.804f, DeskLighting.VignetteAt(0.3f, 0.3f, 0.2f, 0.65f), 0.005f);
        Assert.AreEqual(1f, DeskLighting.VignetteAt(0.5f, 0.5f, 0f, 0.65f), Tolerance, "off at intensity 0");
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
    public void TheNight_KeepsTheWholeMatReadable_AfterTheFinish_ItsCornersIncluded()
    {
        Assert.IsTrue(DeskLighting.ReadableAfterFinish(Knobs, Radius, MatWidth, MatDepth, Office, out float centre, out float corners),
                      $"centre {centre:0.0}, corners {corners:0.0}");
        Assert.That(corners, Is.GreaterThanOrEqualTo(DeskLighting.ReadableMatLuma + DeskLighting.MarginLuma), "the mat's corners, with the margin");
        Assert.That(centre, Is.GreaterThanOrEqualTo(DeskLighting.TargetMatLuma), "the pool's centre");
        DeskLighting.Light night = DeskLighting.FullNight(Knobs);
        Assert.That(DeskLighting.DarkestOnMat(night, Radius, MatWidth, MatDepth), Is.GreaterThanOrEqualTo(DeskLighting.ReadableNightLuminance), "before the finish too");
        foreach (float hour in new[] { 20f, 22f, 23f, 23.99f })
        {
            DeskLighting.Light l = DeskLighting.Evaluate(hour, Cycle, Knobs, 1f);
            Assert.AreEqual(night.Level, l.Level, Tolerance, $"{hour}: full night");
            Assert.AreEqual(DeskLighting.DarkestOnMat(night, Radius, MatWidth, MatDepth), DeskLighting.DarkestOnMat(l, Radius, MatWidth, MatDepth), 1e-4f, $"{hour}");
        }
    }

    [Test]
    public void ThePool_ReadsAsAPool_NotATint()
    {
        DeskLighting.Light night = DeskLighting.FullNight(Knobs);
        DeskLighting.MatLumas(night, Radius, MatWidth, MatDepth, Office, out float centre, out float corners);
        Assert.That(centre - corners, Is.GreaterThanOrEqualTo(DeskLighting.PoolContrastLuma), $"centre {centre:0.0} to corners {corners:0.0}");
        // The hue falls off with the level: warm under the lamp, the room's cool at the corners.
        float cornerWeight = DeskLighting.PoolWeight(0.5f * (float)System.Math.Sqrt(MatWidth * MatWidth + MatDepth * MatDepth), Radius);
        Rgba atCentre = DeskLighting.LightAt(night, 1f), atCorner = DeskLighting.LightAt(night, cornerWeight);
        Assert.That(atCorner.R / atCorner.B, Is.LessThan(atCentre.R / atCentre.B - 0.05f), "cooler at the corners");
        Assert.AreEqual(DeskLighting.LuminanceAt(night, cornerWeight), DeskLighting.Luminance(atCorner.R, atCorner.G, atCorner.B), 1e-4f, "LightAt and LuminanceAt agree");
        // Round 1's flat pool (a 1.5 m radius, the hue global) did not reach the step.
        DeskLighting.MatLumas(DeskLighting.FullNight(Round1), 1.5f, MatWidth, MatDepth, Office, out float c1, out float k1);
        Assert.That(c1 - k1, Is.LessThan(DeskLighting.PoolContrastLuma), $"round 1: {c1:0.0} to {k1:0.0}");
    }

    [Test]
    public void TheOldKnobs_FailTheRule()
    {
        // The first LIGHT tune (floor 0.35, lamp 0.25, a 0.45 m pool) left the mat's corners at about luma 40 and the centre at about 54.
        var old = new DeskLighting.Knobs(0.35f, new Rgba(0.78f, 0.84f, 1f), 0.25f, new Rgba(1f, 0.82f, 0.6f), 0.1f);
        Assert.IsFalse(DeskLighting.ReadableAfterFinish(old, 0.45f, MatWidth, MatDepth, Office, out _, out _));
        // Round 1 passed the unfinished rule (corners 58.8) but not the finished one with its margin (the verifier's 55.8 to 57.6).
        Assert.IsFalse(DeskLighting.ReadableAfterFinish(Round1, 1.5f, MatWidth, MatDepth, Office, out _, out float corners));
        Assert.That(corners, Is.LessThan(DeskLighting.ReadableMatLuma + DeskLighting.MarginLuma), $"round 1's corners after the finish: {corners:0.0}");
    }

    [Test]
    public void TheMatNeverFallsBelowTheReadableLine_AtAnyMinute()
    {
        for (float hour = 0f; hour < 24f; hour += 1f / 60f)
        {
            float warm = DeskLighting.LampOn(hour, Cycle, Knobs) ? 1f : 0f;
            DeskLighting.Light l = DeskLighting.Evaluate(hour, Cycle, Knobs, warm);
            float corners = DeskLighting.GradedLuma(DeskLighting.DarkestOnMat(l, Radius, MatWidth, MatDepth), Office);
            Assert.That(corners, Is.GreaterThanOrEqualTo(DeskLighting.ReadableMatLuma + DeskLighting.MarginLuma), $"{hour:0.00}");
        }
    }

    [Test]
    public void TheLevel_NeverPassesTheFullDay_AtAnyMinute()
    {
        // BUG-14c and BUG-51: round 1 put the level at 1.33 at 16:03 (daylight 0.9, the lamp's 0.38 on top). The lamp now
        // fills only what the room has lost, for any knobs, at any warm-up share: no paper white above 1 for the bloom, no
        // traveller tint clamped on its sprite while the photo's multiply is not, no dusk brighter than the drawn desk.
        var strong = new DeskLighting.Knobs(0.9f, new Rgba(1f, 1f, 1f), 1f, new Rgba(1f, 1f, 1f), 0.05f);
        foreach (DeskLighting.Knobs k in new[] { Knobs, Round1, strong })
            for (float hour = 0f; hour < 24f; hour += 1f / 60f)
                foreach (float warm in new[] { 0f, 0.2f, 0.35f, 0.5f, 1f })
                {
                    DeskLighting.Light l = DeskLighting.Evaluate(hour, Cycle, k, warm);
                    Assert.That(l.Level, Is.LessThanOrEqualTo(1f + 1e-5f), $"{hour:0.00} warm {warm}");
                    Assert.That(l.R * l.Level, Is.LessThanOrEqualTo(1f + 1e-5f), $"{hour:0.00} red");
                    Assert.That(l.G * l.Level, Is.LessThanOrEqualTo(1f + 1e-5f), $"{hour:0.00} green");
                    Assert.That(l.B * l.Level, Is.LessThanOrEqualTo(1f + 1e-5f), $"{hour:0.00} blue");
                    Assert.That(DeskLighting.LuminanceAt(l, 1f), Is.LessThanOrEqualTo(1f + 1e-5f), $"{hour:0.00} the pool's centre");
                }
        DeskLighting.Light round1At1603 = DeskLighting.Evaluate(16f + 3f / 60f, Cycle, Round1, 1f);
        Assert.That(round1At1603.Level, Is.LessThanOrEqualTo(1f + 1e-5f), "the verifier's 16:03");
    }

    [Test]
    public void TheLampOnHour_IsTheSunset_ByThePlansDip()
    {
        float dip = DeskLighting.LampOnHour(Cycle, Knobs);
        Assert.That(dip, Is.InRange(15.75f, 16.75f), "within the dusk, by the plan's afternoon dip (H3, 16:45)");
        Assert.AreEqual(16.5f, dip, 1f / 60f, "the sunset, with the shipped knob: a quarter of an hour before H3 (a flagged deviation)");
        Assert.AreEqual(Knobs.LampOnAtEvening, HallDayCycle.Evening(dip, Cycle), 1e-3f, "where the hall's evening reaches the lamp's knob");
        Assert.IsFalse(DeskLighting.LampOn(dip - 1f / 60f, Cycle, Knobs), "off a minute before the switch-on");
        Assert.IsTrue(DeskLighting.LampOn(dip + 1f / 60f, Cycle, Knobs), "on a minute after it");
        for (float hour = 9f; hour < dip - 1f / 60f; hour += 1f / 60f)
            Assert.IsFalse(DeskLighting.LampOn(hour, Cycle, Knobs), $"{hour:0.00}: never earlier");
    }

    [Test]
    public void ALampAtThePlansDip_WouldLeaveTheUnlitMatUnderTheLine()
    {
        // Why the lamp does not wait for H3 (16:45, the plan's afternoon dip): with the lamp switching on there, the room
        // alone takes the mat under the readable line first (about luma 56 after the finish at 16:44), and again at dawn,
        // where the lamp goes off as the evening falls back past the same knob.
        const float h3 = 16.75f;
        var atH3 = new DeskLighting.Knobs(Knobs.NightFloor, Knobs.NightTint, Knobs.LampLevel, Knobs.LampTint, HallDayCycle.Evening(h3, Cycle));
        Assert.AreEqual(h3, DeskLighting.LampOnHour(Cycle, atH3), 1f / 60f, "this knob switches the lamp on at H3");
        float before = h3 - 1f / 60f;
        Assert.IsFalse(DeskLighting.LampOn(before, Cycle, atH3));
        float unlit = DeskLighting.GradedLuma(DeskLighting.DarkestOnMat(DeskLighting.Evaluate(before, Cycle, atH3, 0f), Radius, MatWidth, MatDepth), Office);
        Assert.That(unlit, Is.LessThan(DeskLighting.ReadableMatLuma + DeskLighting.MarginLuma), $"16:44 unlit: {unlit:0.0}");
        // The shipped knob (the sunset) keeps the mat over the line at that minute: the lamp is on by then.
        float shipped = DeskLighting.GradedLuma(DeskLighting.DarkestOnMat(DeskLighting.Evaluate(before, Cycle, Knobs, 1f), Radius, MatWidth, MatDepth), Office);
        Assert.That(shipped, Is.GreaterThanOrEqualTo(DeskLighting.ReadableMatLuma + DeskLighting.MarginLuma), $"16:44 with the lamp on since 16:30: {shipped:0.0}");
    }

    [Test]
    public void TheSwitchOn_IsVisible()
    {
        // The lamp fills only what the room has lost, so it switches on once the room has lost enough to show it.
        float dip = DeskLighting.LampOnHour(Cycle, Knobs) + 1f / 60f;
        DeskLighting.Light off = DeskLighting.Evaluate(dip, Cycle, Knobs, 0f), on = DeskLighting.Evaluate(dip, Cycle, Knobs, 1f);
        float before = DeskLighting.GradedLuma(DeskLighting.LuminanceAt(off, 1f), Office);
        float after = DeskLighting.GradedLuma(DeskLighting.LuminanceAt(on, 1f), Office);
        Assert.That(after - before, Is.GreaterThanOrEqualTo(DeskLighting.SwitchOnLuma), $"the pool's centre {before:0.0} -> {after:0.0}");
        Assert.That(on.R / on.B, Is.GreaterThan(off.R / off.B), "and it warms");
        // Round 1's switch-on at 16:03 would show almost nothing under the cap: why the knob moved to the sunset.
        float early = DeskLighting.LampOnHour(Cycle, Round1) + 1f / 60f;
        float gain = DeskLighting.GradedLuma(DeskLighting.LuminanceAt(DeskLighting.Evaluate(early, Cycle, Round1, 1f), 1f), Office)
                   - DeskLighting.GradedLuma(DeskLighting.LuminanceAt(DeskLighting.Evaluate(early, Cycle, Round1, 0f), 1f), Office);
        Assert.That(gain, Is.LessThan(DeskLighting.SwitchOnLuma), $"16:03 under the cap: +{gain:0.0}");
    }

    [Test]
    public void TheLamp_AddsItsWarmShare_AsItWarms_UpToTheRoomsDeficit()
    {
        float hour = DeskLighting.LampOnHour(Cycle, Knobs) + 0.1f;
        DeskLighting.Light cold = DeskLighting.Evaluate(hour, Cycle, Knobs, 0f);
        DeskLighting.Light half = DeskLighting.Evaluate(hour, Cycle, Knobs, 0.5f);
        DeskLighting.Light warm = DeskLighting.Evaluate(hour, Cycle, Knobs, 1f);
        float deficit = 1f - cold.Level;
        Assert.AreEqual(0f, cold.LampShare, Tolerance);
        Assert.AreEqual(System.Math.Min(Knobs.LampLevel * 0.5f, deficit), half.LampShare, Tolerance);
        Assert.AreEqual(System.Math.Min(Knobs.LampLevel, deficit), warm.LampShare, Tolerance);
        Assert.AreEqual(System.Math.Min(1f, cold.Level + Knobs.LampLevel), warm.Level, Tolerance, "the switch-on brightens the pool, up to the drawn desk");
        Assert.That(warm.Level, Is.GreaterThan(cold.Level));
        Assert.That(warm.B, Is.LessThan(cold.B), "and warms it");
        Assert.AreEqual(1f, warm.LampR, Tolerance); Assert.AreEqual(0.6f, warm.LampB, Tolerance);
    }

    [Test]
    public void TheNight_IsCoolWithoutTheLamp_AndWarmerWithIt()
    {
        DeskLighting.Light dark = DeskLighting.Evaluate(23f, Cycle, Knobs, 0f);
        Assert.AreEqual(0.55f, dark.Level, Tolerance);
        Assert.AreEqual(0.78f, dark.R, Tolerance); Assert.AreEqual(1f, dark.B, Tolerance);
        DeskLighting.Light lit = DeskLighting.Evaluate(23f, Cycle, Knobs, 1f);
        Assert.AreEqual(1f, lit.Level, Tolerance, "floor + lamp: the pool's centre at the drawn level");
        Assert.That(lit.R / lit.B, Is.GreaterThan(dark.R / dark.B), "the lamp warms the night");
    }

    [Test]
    public void OddKnobs_AreRepaired()
    {
        var odd = new DeskLighting.Knobs(-1f, new Rgba(2f, -1f, 0.5f), -3f, new Rgba(5f, 5f, 5f), float.NaN);
        DeskLighting.Light l = DeskLighting.Evaluate(23f, Cycle, odd, 1f);
        Assert.That(l.Level, Is.InRange(0f, 1f));
        Assert.That(l.R, Is.InRange(0f, 1f)); Assert.That(l.G, Is.InRange(0f, 1f)); Assert.That(l.B, Is.InRange(0f, 1f));
        Assert.DoesNotThrow(() => DeskLighting.LampOnHour(Cycle, odd));
        Assert.AreEqual(0f, DeskLighting.GradedLuma(float.NaN, Office), 0.5f, "no light: black");
    }
}
