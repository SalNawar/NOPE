using NUnit.Framework;

/// <summary>
/// The game feel's motion core (Saleh 2026-10-07: "every single button, every
/// single action to feel this satisfying"): the damped spring converges and
/// settles, never overshoots when critically damped, overshoots as much as
/// its damping ratio says, lands on the same curve at 30 and 144 frames a
/// second, and a kick swings out exactly as far as asked; squash and stretch
/// keep the area; a control lifts, squashes, springs back past its rest,
/// pops and says "no" as MotionKnobs say, and stays still under Reduced
/// Motion; a popup grows in, fades only under Reduced Motion and waits out
/// its stagger.
/// </summary>
public class MotionTests
{
    private static readonly MotionKnobs Knobs = new MotionKnobs();

    /// <summary>Runs <paramref name="spring"/> toward 1 from 0 for <paramref name="seconds"/> at <paramref name="fps"/>, returning the highest value seen.</summary>
    private static float Run(ref Spring spring, SpringTuning tuning, float seconds, float fps)
    {
        float peak = spring.Value;
        int frames = (int)System.Math.Round(seconds * fps);
        for (int i = 0; i < frames; i++)
        {
            spring.Step(1f / fps, tuning, 1e-6f, 1e-6f);
            if (spring.Value > peak)
                peak = spring.Value;
        }
        return peak;
    }

    private static Spring From0To1() => new Spring { Value = 0f, Target = 1f };

    [Test]
    public void Spring_EveryFeel_ConvergesAndSettles()
    {
        foreach (MotionFeel feel in System.Enum.GetValues(typeof(MotionFeel)))
        {
            Spring s = From0To1();
            Run(ref s, Knobs.Get(feel), 4f, 60f);
            Assert.AreEqual(1f, s.Value, 1e-3f, $"{feel} converges");
            bool moving = true;
            for (int i = 0; i < 600 && moving; i++)
                moving = s.Step(1f / 60f, Knobs.Get(feel), Knobs.settleValue, Knobs.settleSpeed);
            Assert.IsFalse(moving, $"{feel} settles");
            Assert.IsTrue(s.AtRest, $"{feel} rests on its target");
            Assert.AreEqual(1f, s.Value, $"{feel} snaps onto its target");
        }
    }

    [Test]
    public void Spring_CriticallyDamped_NeverOvershoots()
    {
        foreach (float k in new[] { 50f, 300f, 900f })
        {
            Spring s = From0To1();
            float peak = Run(ref s, SpringTuning.Critical(k), 3f, 60f);
            Assert.LessOrEqual(peak, 1f + 1e-5f, $"stiffness {k}");
            Assert.AreEqual(0f, SpringTuning.Critical(k).Overshoot);
            Assert.AreEqual(1f, SpringTuning.Critical(k).Ratio, 1e-5f);
        }
    }

    [Test]
    public void Spring_Elastic_OvershootsAsItsRatioSays()
    {
        SpringTuning elastic = Knobs.elastic;
        Spring s = From0To1();
        float peak = Run(ref s, elastic, 1.5f, 144f);
        Assert.AreEqual(elastic.Overshoot, peak - 1f, 0.02f, "the swing past the target is exp(-ζπ/√(1-ζ²))");
        Assert.Greater(peak - 1f, 0.25f, "Elastic is jelly: a big overshoot");
    }

    [Test]
    public void Spring_Feels_OvershootFromFirmToElastic()
    {
        Assert.Less(Knobs.firm.Overshoot, Knobs.balanced.Overshoot);
        Assert.Less(Knobs.balanced.Overshoot, Knobs.elastic.Overshoot);
        Assert.Less(Knobs.heavy.Overshoot, Knobs.balanced.Overshoot);
        Assert.Less(Knobs.firm.Overshoot, 0.05f, "Firm barely overshoots");
    }

    [Test]
    public void Spring_30And144Fps_ReachTheSameState()
    {
        foreach (MotionFeel feel in System.Enum.GetValues(typeof(MotionFeel)))
            foreach (float t in new[] { 1f / 6f, 1f / 3f, 0.5f, 1f }) // whole frames at both rates
            {
                Spring a = From0To1(), b = From0To1();
                Run(ref a, Knobs.Get(feel), t, 30f);
                Run(ref b, Knobs.Get(feel), t, 144f);
                Assert.AreEqual(a.Value, b.Value, 0.01f, $"{feel} at {t}s: value");
                Assert.AreEqual(a.Velocity, b.Velocity, 0.15f, $"{feel} at {t}s: velocity");
            }
    }

    [Test]
    public void Spring_LongHitch_IsClampedNotExploded()
    {
        Spring s = From0To1();
        s.Step(5f, Knobs.elastic);
        Assert.IsFalse(float.IsNaN(s.Value));
        Assert.Less(System.Math.Abs(s.Value), 2f, "a 5 s hitch is taken as MaxFrame");
    }

    [Test]
    public void Spring_Kick_SwingsOutAsFarAsAsked()
    {
        foreach (SpringTuning tuning in new[] { Knobs.balanced, Knobs.elastic, Knobs.firm, SpringTuning.Critical(400f) })
        {
            Spring s = Spring.At(0f);
            s.Kick(tuning.KickFor(0.08f));
            float peak = Run(ref s, tuning, 1f, 240f);
            Assert.AreEqual(0.08f, peak, 0.004f, $"ratio {tuning.Ratio}");
        }
    }

    [Test]
    public void SquashStretch_KeepsTheArea()
    {
        foreach (float a in new[] { 0.5f, 0.9f, 1f, 1.2f, 1.6f })
        {
            Stretch s = SquashStretch.Preserve(a);
            Assert.AreEqual(1f, s.Along * s.Across, 1e-5f, $"stretch {a}");
        }
        Stretch squash = SquashStretch.Squash(0.2f);
        Assert.AreEqual(0.8f, squash.Along, 1e-5f);
        Assert.AreEqual(1f, squash.Along * squash.Across, 1e-5f);
        Stretch fast = SquashStretch.FromSpeed(-1e6f, 0.001f, 0.3f);
        Assert.AreEqual(1.3f, fast.Along, 1e-5f, "capped at 1 + max, whichever way it travels");
        Assert.AreEqual(1f, fast.Along * fast.Across, 1e-5f);
        Assert.AreEqual(1f, SquashStretch.FromSpeed(0f, 0.001f, 0.3f).Along);
        Assert.AreEqual(1f, SquashStretch.Preserve(0f).Along, "a non-positive stretch is none");
    }

    /// <summary>Steps <paramref name="m"/> for <paramref name="seconds"/> at 120 fps, returning the extremes of ScaleX, ScaleY and OffsetX.</summary>
    private static (float minX, float maxX, float minY, float maxY, float minO, float maxO) Play(ControlMotion m, float seconds)
    {
        float minX = m.ScaleX, maxX = m.ScaleX, minY = m.ScaleY, maxY = m.ScaleY, minO = m.OffsetX, maxO = m.OffsetX;
        for (int i = 0; i < seconds * 120f; i++)
        {
            m.Step(1f / 120f, Knobs);
            minX = System.Math.Min(minX, m.ScaleX);
            maxX = System.Math.Max(maxX, m.ScaleX);
            minY = System.Math.Min(minY, m.ScaleY);
            maxY = System.Math.Max(maxY, m.ScaleY);
            minO = System.Math.Min(minO, m.OffsetX);
            maxO = System.Math.Max(maxO, m.OffsetX);
        }
        return (minX, maxX, minY, maxY, minO, maxO);
    }

    [Test]
    public void Control_HoverPressRelease_LiftsSquashesAndSpringsBackPastRest()
    {
        var m = new ControlMotion();
        m.Hover(true, Knobs, MotionAmount.Full);
        Play(m, 2f);
        Assert.AreEqual(Knobs.hoverScale, m.ScaleX, 1e-3f, "hover lifts");
        Assert.IsFalse(m.Moving, "and settles");

        m.Press(Knobs, MotionAmount.Full);
        Play(m, 1f);
        Assert.AreEqual(Knobs.pressScaleX, m.ScaleX, 1e-3f, "pressed: wider");
        Assert.AreEqual(Knobs.pressScaleY, m.ScaleY, 1e-3f, "pressed: shorter");

        m.Release(Knobs, MotionAmount.Full);
        var r = Play(m, 2f);
        Assert.Greater(r.maxY, Knobs.hoverScale + 0.02f, "the release springs up past the hover lift");
        Assert.Less(r.minX, Knobs.hoverScale - 0.01f, "and narrows past it: jelly");
        Assert.AreEqual(Knobs.hoverScale, m.ScaleY, 1e-3f, "then settles on the lift");
        Assert.IsFalse(m.Moving);
    }

    [Test]
    public void Control_ReleaseAfterThePointerLeft_SettlesAtRest()
    {
        var m = new ControlMotion();
        m.Hover(true, Knobs, MotionAmount.Full);
        m.Press(Knobs, MotionAmount.Full);
        m.Hover(false, Knobs, MotionAmount.Full);
        Play(m, 0.5f);
        Assert.AreEqual(Knobs.pressScaleY, m.ScaleY, 1e-3f, "still squashed while held");
        m.Release(Knobs, MotionAmount.Full);
        Play(m, 2f);
        Assert.AreEqual(1f, m.ScaleX, 1e-3f);
        Assert.AreEqual(1f, m.ScaleY, 1e-3f);
    }

    [Test]
    public void Control_Confirm_PopsToItsAmountAndBack()
    {
        var m = new ControlMotion();
        m.Confirm(Knobs, MotionAmount.Full);
        var r = Play(m, 2f);
        Assert.AreEqual(1f + Knobs.popAmount, r.maxX, 0.005f, "1.0 to 1.08");
        Assert.AreEqual(r.maxX, r.maxY, 1e-6f, "evenly");
        Assert.AreEqual(1f, m.ScaleX, 1e-3f, "and back");
        Assert.AreEqual(0f, r.maxO, "no shake");
    }

    [Test]
    public void Control_Refuse_ShakesSidewaysWithoutPopping()
    {
        var m = new ControlMotion();
        m.Refuse(Knobs, MotionAmount.Full);
        var r = Play(m, 2f);
        Assert.AreEqual(Knobs.refuseShake, r.maxO, 0.4f, "out to the shake's size");
        Assert.Less(r.minO, -1f, "and back past the middle: a no");
        Assert.AreEqual(1f, r.maxX, 1e-6f, "no pop");
        Assert.AreEqual(1f, r.maxY, 1e-6f);
        Assert.AreEqual(0f, m.OffsetX, 0.05f, "settled in the middle");
    }

    [Test]
    public void Control_ReducedMotion_NeverMovesTheControl()
    {
        var m = new ControlMotion();
        var reduced = new MotionAmount(1f, true);
        m.Hover(true, Knobs, reduced);
        m.Press(Knobs, reduced);
        m.Release(Knobs, reduced);
        m.Confirm(Knobs, reduced);
        m.Refuse(Knobs, reduced);
        Assert.IsFalse(m.Moving);
        var r = Play(m, 1f);
        Assert.AreEqual(1f, r.minX);
        Assert.AreEqual(1f, r.maxY);
        Assert.AreEqual(0f, r.maxO);
        Assert.AreEqual(0f, r.minO);
    }

    [Test]
    public void Control_Intensity_ScalesTheAmplitude()
    {
        var m = new ControlMotion();
        m.Hover(true, Knobs, new MotionAmount(0.5f, false));
        Play(m, 2f);
        Assert.AreEqual(1f + (Knobs.hoverScale - 1f) * 0.5f, m.ScaleX, 1e-3f);
        var pop = new ControlMotion();
        pop.Confirm(Knobs, new MotionAmount(0.5f, false));
        Assert.AreEqual(1f + Knobs.popAmount * 0.5f, Play(pop, 2f).maxX, 0.005f);
        var none = new ControlMotion();
        none.Press(Knobs, new MotionAmount(0f, false));
        Assert.IsFalse(none.Moving, "intensity 0 cuts");
        Assert.AreEqual(1f, none.ScaleY);
    }

    [Test]
    public void Appear_GrowsInPastFullAndSettles_ThenGoes()
    {
        var a = new AppearMotion();
        a.Snap(false);
        a.Show(true, MotionAmount.Full);
        float peak = 0f;
        for (int i = 0; i < 240; i++)
        {
            a.Step(1f / 120f, Knobs.elastic, Knobs.reducedFadeSeconds, Knobs.settleValue, Knobs.settleSpeed);
            peak = System.Math.Max(peak, a.Scale(Knobs.appearFromScale, MotionAmount.Full));
        }
        Assert.Greater(peak, 1.02f, "it grows past full size");
        Assert.AreEqual(1f, a.Scale(Knobs.appearFromScale, MotionAmount.Full), 1e-3f);
        Assert.AreEqual(1f, a.Alpha);
        Assert.IsFalse(a.Moving);

        a.Show(false, MotionAmount.Full);
        for (int i = 0; i < 360 && !a.Gone; i++)
            a.Step(1f / 120f, Knobs.heavy, Knobs.reducedFadeSeconds, Knobs.settleValue, Knobs.settleSpeed);
        Assert.IsTrue(a.Gone);
        Assert.AreEqual(0f, a.Alpha);
    }

    [Test]
    public void Appear_ReducedMotion_OnlyFades()
    {
        var a = new AppearMotion();
        var reduced = new MotionAmount(1f, true);
        a.Snap(false);
        a.Show(true, reduced, 0.5f);
        Assert.AreEqual(0f, a.Offset(80f, reduced));
        float half = Knobs.reducedFadeSeconds / 2f;
        a.Step(half, Knobs.elastic, Knobs.reducedFadeSeconds, Knobs.settleValue, Knobs.settleSpeed);
        Assert.AreEqual(0.5f, a.Alpha, 0.02f, "a linear fade, with no stagger wait under Reduced Motion");
        Assert.AreEqual(1f, a.Scale(Knobs.appearFromScale, reduced), "no scale");
        a.Step(half, Knobs.elastic, Knobs.reducedFadeSeconds, Knobs.settleValue, Knobs.settleSpeed);
        Assert.AreEqual(1f, a.Alpha);
        Assert.IsFalse(a.Moving);
    }

    [Test]
    public void Appear_Stagger_WaitsBeforeItMoves()
    {
        var a = new AppearMotion();
        a.Snap(false);
        a.Show(true, MotionAmount.Full, 0.1f);
        a.Step(0.05f, Knobs.balanced, Knobs.reducedFadeSeconds, Knobs.settleValue, Knobs.settleSpeed);
        Assert.AreEqual(0f, a.Presence, "still waiting");
        Assert.IsTrue(a.Moving);
        a.Step(0.1f, Knobs.balanced, Knobs.reducedFadeSeconds, Knobs.settleValue, Knobs.settleSpeed);
        Assert.Greater(a.Presence, 0f, "then grows");
    }
}
