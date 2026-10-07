using System;
using System.Collections.Generic;
using System.Linq;
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
        Stretch fast = SquashStretch.FromMotion(-1e6f, 0f, 0.001f, 0f, 0.3f);
        Assert.AreEqual(1.3f, fast.Along, 1e-5f, "capped at 1 + max, whichever way it travels");
        Assert.AreEqual(1f, fast.Along * fast.Across, 1e-5f);
        Assert.AreEqual(1f, SquashStretch.Preserve(0f).Along, "a non-positive stretch is none");
    }

    private const float W = 300f, H = 44f;

    /// <summary>A control with the kit's room on every side.</summary>
    private static ControlMotion Control()
    {
        var m = new ControlMotion();
        m.SetRoom(Knobs.faceRoom, Knobs.faceRoom, Knobs.faceRoom, Knobs.faceRoom);
        return m;
    }

    /// <summary>Steps <paramref name="m"/> for <paramref name="seconds"/> at 240 fps, the face's edges each frame (time, edges).</summary>
    private static List<(float t, FaceEdges e)> Play(ControlMotion m, float seconds, float w = W, float h = H)
    {
        var frames = new List<(float, FaceEdges)>();
        for (int i = 1; i <= seconds * 240f; i++)
        {
            m.Step(1f / 240f, Knobs);
            frames.Add((i / 240f, m.Edges(w, h, Knobs)));
        }
        return frames;
    }

    [Test]
    public void Control_Hover_LiftsTheFaceAPixelOrTwo_NoGrowth()
    {
        var m = Control();
        m.Hover(true, Knobs, MotionAmount.Full);
        var f = Play(m, 1f);
        FaceEdges e = f[f.Count - 1].e;
        Assert.AreEqual(Knobs.hoverLift, e.Top, 0.05f, "the face's top lifts");
        Assert.AreEqual(0f, e.Bottom, 1e-4f, "its bottom stays");
        Assert.LessOrEqual(e.Left, 0f, "no sideways growth: a taller face is narrower by the area's rule");
        Assert.LessOrEqual(Knobs.hoverLift, 2f);
        Assert.GreaterOrEqual(Knobs.hoverLift, 1f);
    }

    [Test]
    public void Control_Press_GoesDownIntoTheBezelInAbout90ms_AndDarkens()
    {
        var m = Control();
        m.Press(Knobs, MotionAmount.Full);
        var f = Play(m, 0.5f);
        float at90 = f.Find(x => x.t >= 0.09f).e.Top;
        Assert.LessOrEqual(at90, -0.9f * Knobs.pressDepth, "most of the way down by 90 ms");
        float at45 = f.Find(x => x.t >= 0.045f).e.Top;
        Assert.Greater(at45, -0.95f * Knobs.pressDepth, "not a snap: still going at 45 ms");
        FaceEdges e = f[f.Count - 1].e;
        Assert.AreEqual(-Knobs.pressDepth, e.Top, 0.05f, "its top pushed down 2-3 px");
        Assert.AreEqual(0f, e.Bottom, 1e-4f, "its bottom stays in the bezel");
        Assert.AreEqual(Knobs.pressDarken, e.Darken, 0.01f, "darkened");
        Assert.LessOrEqual(e.Right, Knobs.faceRoom + 1e-4f);
    }

    [Test]
    public void Control_Release_SpringsBackPastRest_InTwoOrThreeDecayingWobbles_Over350To450ms()
    {
        var m = Control();
        m.Press(Knobs, MotionAmount.Full);
        Play(m, 0.5f);
        m.Release(Knobs, MotionAmount.Full);
        var f = Play(m, 1.2f);
        // Wobbles: the top's crossings of rest, and their peaks, while visible (a tenth of a pixel or more).
        int crossings = 0;
        float lastPeak = float.MaxValue;
        bool decaying = true;
        float sign = Math.Sign(f[0].e.Top), peak = 0f, settledAt = 0f;
        foreach ((float t, FaceEdges e) in f)
        {
            if (Math.Abs(e.Top) >= 0.1f)
                settledAt = t;
            if (Math.Sign(e.Top) != sign && Math.Sign(e.Top) != 0)
            {
                if (peak >= 0.1f)
                {
                    crossings++;
                    decaying &= peak < lastPeak;
                    lastPeak = peak;
                }
                sign = Math.Sign(e.Top);
                peak = 0f;
            }
            peak = Math.Max(peak, Math.Abs(e.Top));
        }
        Assert.GreaterOrEqual(crossings, 2, "at least two visible wobbles");
        Assert.LessOrEqual(crossings, 4, "and not an endless jelly");
        Assert.IsTrue(decaying, "each smaller than the last");
        Assert.Greater(settledAt, 0.3f, "not a quick snap");
        Assert.Less(settledAt, 0.5f, "settled (within a tenth of a pixel) by about 400 ms");
        Assert.LessOrEqual(f.Max(x => x.e.Top), Knobs.faceRoom + 1e-4f, "the overshoot stays in the room");
        Assert.AreEqual(0f, f[f.Count - 1].e.Darken, 1e-3f, "the darkening goes");
    }

    [Test]
    public void Control_Confirm_PopsTheTopUpAndBack_NoSidewaysGrowthPastTheRoom()
    {
        var m = Control();
        m.Confirm(Knobs, MotionAmount.Full);
        var f = Play(m, 1.5f);
        Assert.AreEqual(Math.Min(Knobs.popLift, Knobs.faceRoom), f.Max(x => x.e.Top), 0.15f, "up by the pop");
        Assert.LessOrEqual(f.Max(x => Math.Max(x.e.Left, x.e.Right)), Knobs.faceRoom + 1e-4f);
        Assert.AreEqual(0f, f[f.Count - 1].e.Top, 0.01f, "and back");
        Assert.IsFalse(m.Moving);
    }

    [Test]
    public void Control_Refuse_ShakesInsideItsRoom()
    {
        var m = new ControlMotion();
        m.SetRoom(Knobs.faceRoom, 0f, Knobs.faceRoom, Knobs.faceRoom); // a neighbour touching its right
        m.Refuse(Knobs, MotionAmount.Full);
        var f = Play(m, 1.5f);
        Assert.Greater(f.Max(x => x.e.Left), 1f, "it shakes out to the left");
        Assert.LessOrEqual(f.Max(x => x.e.Right), 0f, "never into the neighbour on its right");
        Assert.AreEqual(0f, f.Max(x => Math.Abs(x.e.Top)), 1e-4f, "no pop");
    }

    [Test]
    public void Control_ReducedMotion_TheFaceStaysPut_ThePressStillDarkens()
    {
        var m = Control();
        var reduced = new MotionAmount(1f, true);
        m.Hover(true, Knobs, reduced);
        m.Press(Knobs, reduced);
        m.Confirm(Knobs, reduced);
        m.Refuse(Knobs, reduced);
        FaceEdges e = m.Edges(W, H, Knobs);
        Assert.AreEqual((0f, 0f, 0f, 0f), (e.Left, e.Right, e.Bottom, e.Top));
        Assert.AreEqual(Knobs.pressDarken, e.Darken, 1e-4f);
        Assert.IsFalse(m.Moving);
        m.Release(Knobs, reduced);
        Assert.IsTrue(m.AtRestPose);
    }

    [Test]
    public void Control_Intensity_ScalesTheAmplitude()
    {
        var m = Control();
        m.Press(Knobs, new MotionAmount(0.5f, false));
        var f = Play(m, 1f);
        Assert.AreEqual(-Knobs.pressDepth * 0.5f, f[f.Count - 1].e.Top, 0.05f);
    }

    [Test]
    public void PullTab_SlidesOut_StretchedAlongItsTravel()
    {
        var m = new ControlMotion();
        m.SetRoom(0f, Knobs.pullHover + Knobs.pullPop + Knobs.faceRoom, Knobs.faceRoom, Knobs.faceRoom); // on the screen's left edge
        m.SetPull(1, 0);
        m.Hover(true, Knobs, MotionAmount.Full);
        var f = Play(m, 1f, 128f, 136f);
        Assert.AreEqual(Knobs.pullHover, f[f.Count - 1].e.Right, 1f, "out by the hover's slide (less the lifted face's narrowing)");
        Assert.LessOrEqual(f.Max(x => x.e.Left), 0f, "never past the screen's edge");
        bool stretched = f.Exists(x => x.e.Right + x.e.Left > 4f && x.e.Top + x.e.Bottom < -4f);
        Assert.IsTrue(stretched, "on the way: longer along its travel, thinner across");
    }

    [Test]
    public void SquashStretch_FromMotion_StretchesBySpeed_SquashesByAcceleration()
    {
        Assert.AreEqual(1f, SquashStretch.FromMotion(0f, 0f, 0.001f, 0.001f, 0.25f).Along);
        Assert.Greater(SquashStretch.FromMotion(200f, 0f, 0.001f, 0.001f, 0.25f).Along, 1.1f, "speed stretches");
        Assert.Less(SquashStretch.FromMotion(0f, 200f, 0.001f, 0.001f, 0.25f).Along, 0.9f, "acceleration squashes");
        Stretch capped = SquashStretch.FromMotion(1e6f, 0f, 0.001f, 0.001f, 0.25f);
        Assert.AreEqual(1.25f, capped.Along, 1e-5f);
        Assert.AreEqual(1f, capped.Along * capped.Across, 1e-5f, "the area keeps");
        Assert.AreEqual(0.75f, SquashStretch.FromMotion(0f, -1e6f, 0.001f, 0.001f, 0.25f).Along, 1e-5f);
    }

    /// <summary>The layouts the kit's controls stand in (canvas px, y up): rows and columns at every spacing the office, the PC and Home use (0 for the bills' Paying / Skip pair), plates, mini plates and segments.</summary>
    private static IEnumerable<(string name, List<FaceRect> rects)> Layouts()
    {
        foreach (float gap in new[] { 0f, 2f, 4f, 6f, 8f, 12f, 16f })
            foreach ((float w, float h) in new[] { (300f, 44f), (240f, 44f), (160f, 48f), (110f, 40f) })
            {
                var row = new List<FaceRect>();
                for (int i = 0; i < 4; i++)
                    row.Add(new FaceRect(i * (w + gap), 0f, i * (w + gap) + w, h));
                yield return ($"row {w}x{h} gap {gap}", row);
                var grid = new List<FaceRect>();
                for (int r = 0; r < 3; r++)
                    for (int c = 0; c < 3; c++)
                        grid.Add(new FaceRect(c * (w + gap), r * (h + gap), c * (w + gap) + w, r * (h + gap) + h));
                yield return ($"grid {w}x{h} gap {gap}", grid);
            }
    }

    [Test]
    public void NoAnimatedFace_EverOverlapsANeighboursRestRect()
    {
        foreach ((string name, List<FaceRect> rects) in Layouts())
            for (int i = 0; i < rects.Count; i++)
            {
                FaceRect rest = rects[i];
                var others = new List<FaceRect>(rects);
                others.RemoveAt(i);
                ControlRoom.Of(rest, others, Knobs.faceRoom, Knobs.faceRoom, Knobs.faceRoom, Knobs.faceRoom, out float l, out float r, out float b, out float t);
                foreach (int script in new[] { 0, 1, 2 })
                {
                    var m = new ControlMotion();
                    m.SetRoom(l, r, b, t);
                    m.Hover(true, Knobs, MotionAmount.Full);
                    if (script == 0)
                        m.Press(Knobs, MotionAmount.Full);
                    if (script == 1)
                        m.Refuse(Knobs, MotionAmount.Full);
                    for (int k = 0; k < 240; k++)
                    {
                        if (script == 0 && k == 30)
                        {
                            m.Release(Knobs, MotionAmount.Full);
                            m.Confirm(Knobs, MotionAmount.Full);
                        }
                        if (script == 2 && k == 10)
                            m.Confirm(Knobs, MotionAmount.Full);
                        m.Step(1f / 240f, Knobs);
                        FaceEdges e = m.Edges(rest.Width, rest.Height, Knobs);
                        var face = new FaceRect(rest.XMin - e.Left, rest.YMin - e.Bottom, rest.XMax + e.Right, rest.YMax + e.Top);
                        Assert.IsTrue(e.Left <= Knobs.faceRoom + 1e-4f && e.Right <= Knobs.faceRoom + 1e-4f && e.Bottom <= Knobs.faceRoom + 1e-4f && e.Top <= Knobs.faceRoom + 1e-4f,
                                      $"{name} #{i}: within the kit's border inset");
                        foreach (FaceRect n in others)
                            Assert.IsFalse(BubbleLayout.Overlap(face, n, 0f) && !BubbleLayout.Overlap(rest, n, 0f),
                                           $"{name} #{i} script {script} frame {k}: the face reaches into a neighbour's rest rect");
                    }
                }
            }
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

    [Test]
    public void SpringCurve_StartsAt0_LandsExactlyOn1_AndKeepsTheSpringsShape()
    {
        foreach (MotionFeel feel in System.Enum.GetValues(typeof(MotionFeel)))
        {
            SpringTuning tuning = Knobs.Get(feel);
            Assert.AreEqual(0f, SpringCurve.Ease(0f, tuning, 0.3f), $"{feel} starts at 0");
            Assert.AreEqual(1f, SpringCurve.Ease(1f, tuning, 0.3f), $"{feel} lands on 1");
            Assert.AreEqual(1f, SpringCurve.Ease(0.5f, tuning, 0f), "no length: a cut");
        }
        float peak = 0f;
        for (int i = 0; i <= 100; i++)
            peak = System.Math.Max(peak, SpringCurve.Ease(i / 100f, Knobs.elastic, 0.6f));
        Assert.Greater(peak, 1.05f, "an elastic curve swings past its end before it lands");
        for (int i = 1; i <= 100; i++)
            Assert.LessOrEqual(SpringCurve.Ease(i / 100f, SpringTuning.Critical(300f), 0.5f), 1f + 1e-5f, "a critical curve never does");
    }

    [Test]
    public void SpringCurve_Response_MatchesTheIntegrator()
    {
        foreach (SpringTuning tuning in new[] { Knobs.elastic, Knobs.balanced, SpringTuning.Critical(300f), SpringTuning.WithRatio(300f, 1.6f) })
        {
            Spring s = From0To1();
            Run(ref s, tuning, 0.25f, 240f);
            Assert.AreEqual(SpringCurve.Response(0.25f, tuning), s.Value, 0.01f, $"ratio {tuning.Ratio}");
        }
    }
}
