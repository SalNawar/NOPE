using NUnit.Framework;

/// <summary>
/// The Helix River (Saleh 2026-10-07: stability shown as an animated helix that degrades, never as a number):
/// the mapping from the hidden stability to the river's look degrades monotonically, clamps at both ends,
/// follows a change smoothly, pulses on a citation and keeps its state readable under Reduced Motion.
/// Calm, readable motion (HALL-FX, BUG-54 and 56e): a drop glitches the CRT once and the glitch returns to 0,
/// the warning shows steadily near the destruction point, and no detail outruns half a pixel a frame.
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
            Assert.LessOrEqual(last.Warning, f.Warning, $"warning at {s}");
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
        Assert.AreEqual(0f, steady.Warning);

        HelixRiverFrame collapsed = First(FiredAt);
        Assert.AreEqual(0f, collapsed.Calm);
        Assert.AreEqual(knobs.meanderWild, collapsed.Meander, 1e-4f);
        Assert.AreEqual(knobs.unzipWild, collapsed.Unzip, 1e-4f);
        Assert.AreEqual(knobs.oxbowsWild, collapsed.Oxbows, 1e-4f);
        Assert.AreEqual(knobs.frayWild, collapsed.Fray, 1e-4f);
        Assert.AreEqual(0f, collapsed.Glitch, "no glitch without a drop: the damage is the shape's, the glass stays calm");
        Assert.AreEqual(1f, collapsed.Warning, "the warning shows at the line (it starts where stability stands)");
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
    public void ADrop_GlitchesOnce_AndTheGlitchReturnsToZero()
    {
        var knobs = new HelixRiverKnobs { glitchSeconds = 0.2f, glitchWild = 1f };
        var river = new HelixRiver(knobs);
        for (int i = 0; i < 10; i++)
            Assert.AreEqual(0f, river.Step(1f / 30f, At(90f)).Glitch, "no drop, no glitch");

        // A wrong call: 90 -> 87.5, with today's loss (the old gain kept the glitch up all day).
        HelixRiverFrame hit = river.Step(1f / 30f, At(87.5f, todayChange: -2.5f));
        Assert.Greater(hit.Glitch, 0.5f, "one readable glitch on the drop");
        float seed = hit.GlitchSeed, previous = hit.Glitch;
        HelixRiverFrame f = hit;
        for (int i = 0; i < 6; i++)
        {
            f = river.Step(1f / 30f, At(87.5f, todayChange: -2.5f));
            Assert.LessOrEqual(f.Glitch, previous, "it only settles");
            Assert.AreEqual(seed, f.GlitchSeed, "the same bands for the whole burst: it kinks once");
            previous = f.Glitch;
        }
        Assert.AreEqual(0f, f.Glitch, "gone after its 0.2 s");
        for (int i = 0; i < 300; i++)
            Assert.AreEqual(0f, river.Step(1f / 30f, At(87.5f, todayChange: -2.5f)).Glitch, "and calm for the rest of the shift");

        HelixRiverFrame next = river.Step(1f / 30f, At(85f, todayChange: -5f));
        Assert.Greater(next.Glitch, 0.5f, "the next drop glitches again");
        Assert.AreNotEqual(seed, next.GlitchSeed, "with other bands");
        for (int i = 0; i < 10; i++)
            river.Step(1f / 30f, At(85f, todayChange: -5f));
        HelixRiverFrame gain = river.Step(1f / 30f, At(86f, todayChange: -4f));
        Assert.AreEqual(0f, gain.Glitch, "a gain starts no burst");
        Assert.AreEqual(next.GlitchSeed, gain.GlitchSeed);
    }

    [Test]
    public void TheWarning_ShowsSteadilyNearTheDestructionPoint_AndCanBeHeld()
    {
        var knobs = new HelixRiverKnobs { warningSettleSeconds = 0.8f, warningHysteresis = 0.5f, settleSeconds = 0f };
        Assert.AreEqual(0f, new HelixRiver(knobs).Step(0.1f, At(FiredAt + Warning - 1f)).Warning, "the old 70% warning band is not the warning (D12)");
        Assert.AreEqual(0f, new HelixRiver(knobs).Step(0.1f, At(FiredAt + Critical + 0.01f)).Warning, "just above the critical line: off");
        Assert.AreEqual(1f, new HelixRiver(knobs).Step(0.1f, At(FiredAt + Critical)).Warning, "on the line: on");

        // Just past the line and back (the verification's cheat path): it eases on, holds, and eases off once clear of the hysteresis.
        var river = new HelixRiver(knobs);
        river.Step(0.1f, At(FiredAt + Critical + 2f));
        float w = 0f;
        for (int i = 0; i < 60; i++)
        {
            float next = river.Step(1f / 30f, At(FiredAt + Critical - 0.1f)).Warning;
            Assert.GreaterOrEqual(next, w, "it only rises: no strobe");
            Assert.Less(next - w, 0.05f, "eased");
            w = next;
        }
        Assert.Greater(w, 0.85f, "on within 2 s");
        Assert.Greater(river.Step(1f / 30f, At(FiredAt + Critical + 0.3f)).Warning, 0.85f, "inside the hysteresis it stays on");
        for (int i = 0; i < 90; i++)
            w = river.Step(1f / 30f, At(FiredAt + Critical + 1f)).Warning;
        Assert.Less(w, 0.1f, "past it, it goes off");

        var held = new HelixRiver(knobs);
        Assert.AreEqual(1f, held.Step(0.1f, new HelixRiverInput(95f, FiredAt, Warning, Critical, 0f, false, 0f, warningHeld: true)).Warning, "a held warning shows at any stability");
    }

    [Test]
    public void NoDetail_OutrunsHalfAPixelAFrame_OnAnyDisplay()
    {
        var knobs = new HelixRiverKnobs { maxPixelsPerFrame = 0.5f, captureFps = 30f };
        // A display 96 px tall showing 253 reference px (the desk monitor at 1080p): 2.6 reference px a screen pixel.
        float scale = 253f / 96f;
        float cap = HelixRiver.SpeedCap(scale, knobs);
        Assert.AreEqual(0.5f * 30f * scale, cap, 1e-4f);
        Assert.IsTrue(float.IsPositiveInfinity(HelixRiver.SpeedCap(0f, knobs)), "an unknown display: no cap");

        var river = new HelixRiver(knobs);
        HelixRiverFrame a = river.Step(1f / 30f, new HelixRiverInput(62f, FiredAt, Warning, Critical, -7.5f, false, scale));
        HelixRiverFrame b = river.Step(1f / 30f, new HelixRiverInput(62f, FiredAt, Warning, Critical, -7.5f, false, scale));
        float fastest = (b.Time - a.Time) * HelixRiver.FastestOnClock / scale;
        Assert.LessOrEqual(fastest, 0.5f + 1e-4f, "the clock's fastest detail: screen px a frame");
        Assert.LessOrEqual((b.MoteTravel - a.MoteTravel) / scale, 0.5f + 1e-4f, "the motes, even agitated");
        Assert.Greater(b.MoteTravel, a.MoteTravel, "they still flow");

        var big = new HelixRiver(knobs);
        HelixRiverFrame c = big.Step(1f / 30f, new HelixRiverInput(100f, FiredAt, Warning, Critical, 0f, false, 0.1f));
        HelixRiverFrame d = big.Step(1f / 30f, new HelixRiverInput(100f, FiredAt, Warning, Critical, 0f, false, 0.1f));
        Assert.LessOrEqual((d.Time - c.Time) * HelixRiver.FastestOnClock / 0.1f, 0.5f + 1e-4f, "a big display slows it too (thin lines strobe when they hop pixels)");
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
        Assert.AreEqual(0f, badDay.Glitch, "but the glass stays calm (a glitch is a drop's, once)");
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
        Assert.AreEqual(f.Warning, r.Warning, "and the warning");
        Assert.AreEqual(1f, r.Warning);
        Assert.AreEqual(0f, reduced.Step(0.1f, At(62f, reduced: true)).Glitch, "no glitch under Reduced Motion, even on a drop");

        reduced.Citation();
        HelixRiverFrame glow = reduced.Step(0.25f, At(62f, reduced: true));
        Assert.AreEqual(-1f, glow.Pulse, "no running pulse");
        Assert.AreEqual(0.75f, glow.PulseGlow, 1e-4f, "the river glows red and fades instead");

        var frozen = new HelixRiver(new HelixRiverKnobs { reducedMotionSpeed = 0f });
        frozen.Step(1f, At(90f, reduced: true));
        Assert.AreEqual(0f, frozen.Step(1f, At(90f, reduced: true)).Time, "0 freezes it");
    }
}
