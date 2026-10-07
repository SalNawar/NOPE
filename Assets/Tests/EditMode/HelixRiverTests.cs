using NUnit.Framework;

/// <summary>
/// The Helix River (Saleh 2026-10-07: stability shown as an animated helix that degrades, never as a number):
/// the mapping from the hidden stability to the river's look degrades monotonically, clamps at both ends,
/// follows a change smoothly, pulses on a citation and keeps its state readable under Reduced Motion.
/// </summary>
public class HelixRiverTests
{
    private const float FiredAt = 60f, Warning = 10f, Critical = 3f;

    private static HelixRiverInput At(float stability, float todayChange = 0f, bool reduced = false) =>
        new HelixRiverInput(stability, FiredAt, Warning, Critical, todayChange, reduced);

    /// <summary>A fresh river's first frame at <paramref name="stability"/> (it starts where stability stands).</summary>
    private static HelixRiverFrame First(float stability, float todayChange = 0f, bool reduced = false) =>
        new HelixRiver(new HelixRiverKnobs()).Step(0.016f, At(stability, todayChange, reduced));

    [Test]
    public void Damage_GrowsAsStabilityFalls_NeverShrinks()
    {
        HelixRiverFrame last = First(100f);
        for (float s = 99.5f; s >= 40f; s -= 0.5f)
        {
            HelixRiverFrame f = First(s);
            Assert.GreaterOrEqual(last.Calm, f.Calm, $"calm at {s}");
            Assert.LessOrEqual(last.Meander, f.Meander, $"meander at {s}");
            Assert.LessOrEqual(last.Twist, f.Twist, $"twist at {s}");
            Assert.LessOrEqual(last.Unzip, f.Unzip, $"unzip at {s}");
            Assert.GreaterOrEqual(last.UnzipStart, f.UnzipStart, $"the unzip starts further left at {s}");
            Assert.LessOrEqual(last.Snap, f.Snap, $"snapped rungs at {s}");
            Assert.LessOrEqual(last.Mutate, f.Mutate, $"mutations at {s}");
            Assert.LessOrEqual(last.Oxbows, f.Oxbows, $"oxbows at {s}");
            Assert.LessOrEqual(last.Fray, f.Fray, $"fray at {s}");
            Assert.LessOrEqual(last.Glitch, f.Glitch, $"glitch at {s}");
            Assert.LessOrEqual(last.Drift, f.Drift, $"drift at {s}");
            Assert.LessOrEqual(last.Flow, f.Flow, $"flow at {s}");
            last = f;
        }
    }

    [Test]
    public void Steady_HasNoDamage_AndCollapse_HasItAll()
    {
        var knobs = new HelixRiverKnobs();
        HelixRiverFrame steady = First(100f);
        Assert.AreEqual(1f, steady.Calm);
        Assert.AreEqual(knobs.meanderCalm, steady.Meander, 1e-4f);
        Assert.AreEqual(0f, steady.Unzip);
        Assert.AreEqual(0f, steady.Snap);
        Assert.AreEqual(0f, steady.Oxbows);
        Assert.AreEqual(0f, steady.Fray);
        Assert.AreEqual(0f, steady.Glitch);
        Assert.AreEqual(0f, steady.Flicker);

        HelixRiverFrame collapsed = First(FiredAt);
        Assert.AreEqual(0f, collapsed.Calm);
        Assert.AreEqual(knobs.meanderWild, collapsed.Meander, 1e-4f);
        Assert.AreEqual(knobs.unzipWild, collapsed.Unzip, 1e-4f);
        Assert.AreEqual(knobs.oxbowsWild, collapsed.Oxbows, 1e-4f);
        Assert.AreEqual(knobs.frayWild, collapsed.Fray, 1e-4f);
        Assert.AreEqual(1f, collapsed.Glitch, 1e-4f);
        Assert.AreEqual(1f, collapsed.Flicker, "the critical band flickers");
    }

    [TestCase(100f, 1f)]
    [TestCase(150f, 1f, Description = "above the top: as steady")]
    [TestCase(80f, 0.5f, Description = "halfway between the firing line and the top")]
    [TestCase(60f, 0f)]
    [TestCase(20f, 0f, Description = "under the firing line: as collapsed")]
    [TestCase(float.NaN, 0f, Description = "an unreadable value shows the worst, never throws")]
    public void Calm_IsTheShareFromTheFiringLineToTheTop_Clamped(float stability, float calm)
    {
        Assert.AreEqual(calm, First(stability).Calm, 1e-4f);
    }

    [Test]
    public void Glitch_StartsAtTheWarningLine_FlickerInTheCriticalBand()
    {
        Assert.AreEqual(0f, First(FiredAt + Warning + 0.01f).Glitch, "above the warning line");
        Assert.Greater(First(FiredAt + Warning - 1f).Glitch, 0f, "inside the warning band");
        Assert.AreEqual(0f, First(FiredAt + Critical + 0.5f).Flicker, "warning, not critical");
        Assert.AreEqual(1f, First(FiredAt + Critical).Flicker, "critical");
    }

    [Test]
    public void AChange_IsFollowedSmoothly_NeverPops()
    {
        var river = new HelixRiver(new HelixRiverKnobs { settleSeconds = 1f });
        Assert.AreEqual(1f, river.Step(0.016f, At(100f)).Calm, "starts where stability stands");

        // A wrong decision: 100 -> 97.5 (calm 1 -> 0.9375).
        float calm = river.Step(0.016f, At(97.5f)).Calm;
        Assert.Greater(calm, 0.99f, "one frame later it has only begun to move");
        float previous = calm;
        for (int i = 0; i < 300; i++)
        {
            calm = river.Step(0.016f, At(97.5f)).Calm;
            Assert.LessOrEqual(calm, previous + 1e-6f, "it moves one way only");
            Assert.Less(previous - calm, 0.002f, "in small steps");
            previous = calm;
        }
        Assert.AreEqual(0.9375f, calm, 0.001f, "and settles where stability stands");
    }

    [Test]
    public void TodaysLoss_AgitatesTheRiver_WithoutChangingItsShape()
    {
        HelixRiverFrame calmDay = First(80f, todayChange: 0f);
        HelixRiverFrame badDay = First(80f, todayChange: -7.5f);
        Assert.Greater(badDay.Flow, calmDay.Flow, "the motes race");
        Assert.Greater(badDay.Glitch, calmDay.Glitch, "the glass twitches");
        Assert.AreEqual(calmDay.Meander, badDay.Meander, "the shape is stability's alone");
        Assert.AreEqual(calmDay.Snap, badDay.Snap);
        Assert.AreEqual(calmDay.Flow, First(80f, todayChange: 2f).Flow, "a gain does not agitate");
    }

    [Test]
    public void Citation_RunsAPulseDownstream_ForItsDuration()
    {
        var river = new HelixRiver(new HelixRiverKnobs { pulseSeconds = 1f });
        Assert.AreEqual(-1f, river.Step(0.1f, At(90f)).Pulse, "no pulse before a citation");

        river.Citation();
        Assert.AreEqual(0.25f, river.Step(0.25f, At(90f)).Pulse, 1e-4f, "a quarter of the way after a quarter of its time");
        Assert.AreEqual(0.75f, river.Step(0.5f, At(90f)).Pulse, 1e-4f);
        Assert.AreEqual(-1f, river.Step(0.3f, At(90f)).Pulse, "gone once it has run the river");

        river.Citation();
        river.Step(0.5f, At(90f));
        river.Citation();
        Assert.AreEqual(0.1f, river.Step(0.1f, At(90f)).Pulse, 1e-4f, "a new citation restarts the pulse");
    }

    [Test]
    public void ReducedMotion_SlowsTheClock_KeepsTheState_AndGlowsInsteadOfRunning()
    {
        var knobs = new HelixRiverKnobs { reducedMotionSpeed = 0.1f, pulseSeconds = 1f };
        var full = new HelixRiver(knobs);
        var reduced = new HelixRiver(knobs);
        HelixRiverFrame f = default, r = default;
        for (int i = 0; i < 10; i++)
        {
            f = full.Step(0.1f, At(63f));
            r = reduced.Step(0.1f, At(63f, reduced: true));
        }
        Assert.AreEqual(1f, f.Time, 1e-4f);
        Assert.AreEqual(0.1f, r.Time, 1e-4f, "a tenth of the speed");
        Assert.AreEqual(f.Meander, r.Meander, "the damage still shows");
        Assert.AreEqual(f.Snap, r.Snap);
        Assert.AreEqual(f.Glitch, r.Glitch);
        Assert.AreEqual(1f, f.Flicker);
        Assert.AreEqual(0f, r.Flicker, "no flicker under Reduced Motion");

        reduced.Citation();
        HelixRiverFrame glow = reduced.Step(0.25f, At(63f, reduced: true));
        Assert.AreEqual(-1f, glow.Pulse, "no running pulse");
        Assert.AreEqual(0.75f, glow.PulseGlow, 1e-4f, "the river glows red and fades instead");

        var frozen = new HelixRiver(new HelixRiverKnobs { reducedMotionSpeed = 0f });
        frozen.Step(1f, At(90f, reduced: true));
        Assert.AreEqual(0f, frozen.Step(1f, At(90f, reduced: true)).Time, "0 freezes it");
    }
}
