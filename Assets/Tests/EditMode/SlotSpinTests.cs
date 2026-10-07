using NUnit.Framework;

/// <summary>
/// The Night Slots machine's spin (SlotSpinSchedule): every reel lands on the
/// face it was given (the predetermined result), left to right one after
/// another, with an overshoot the Motion intensity scales; Reduced Motion cuts
/// straight to the result; the same inputs draw the same motion. And its lever
/// (SlotLever): a full drag pulls it and fires once, through the ratchet's
/// notches; let go it springs back; a click pulls it all the way by itself.
/// </summary>
public class SlotSpinTests
{
    private static readonly MotionKnobs Motion = new MotionKnobs();
    private static SlotSpinKnobs Knobs => Motion.slots;
    private static SpringTuning Land => Motion.Get(Knobs.landFeel);

    private static SlotSpinSchedule Schedule(float[] start, int[] faces, int symbols, MotionAmount amount) =>
        new SlotSpinSchedule(start, faces, symbols, Knobs, Land, amount);

    [Test]
    public void EveryReel_LandsOnItsFace_AndStaysThere()
    {
        for (int outcome = 0; outcome < 5; outcome++)
            foreach (bool win in new[] { true, false })
            {
                int[] faces = SlotReels.Faces(outcome, win, 5);
                var start = new float[] { 3, 0, 4 };
                SlotSpinSchedule s = Schedule(start, faces, 5, MotionAmount.Full);
                for (int reel = 0; reel < SlotReels.Count; reel++)
                {
                    Assert.AreEqual(faces[reel], SlotSpinSchedule.FaceAt(s.Position(reel, s.LandTime(reel)), 5), $"outcome {outcome} win {win} reel {reel} at its landing");
                    for (float t = s.LandTime(reel); t <= s.Duration + 0.5f; t += 0.01f)
                        Assert.AreEqual(faces[reel], SlotSpinSchedule.FaceAt(s.Position(reel, t), 5), $"reel {reel} keeps its face at {t:0.00} s");
                    float settled = s.Position(reel, s.Duration + 1f);
                    Assert.AreEqual(System.MathF.Round(settled), settled, 2e-3f, "settled on the payline");
                }
            }
    }

    [Test]
    public void TheReels_StopLeftToRight_AfterTravellingAtLeastAWholeTurn()
    {
        SlotSpinSchedule s = Schedule(new float[] { 0, 1, 2 }, new[] { 0, 1, 2 }, 5, MotionAmount.Full);
        Assert.Less(s.LandTime(0), s.LandTime(1));
        Assert.Less(s.LandTime(1), s.LandTime(2));
        Assert.LessOrEqual(s.LandTime(2), s.Duration);
        for (int reel = 0; reel < 3; reel++)
        {
            Assert.GreaterOrEqual(s.Position(reel, s.LandTime(reel)) - reel, 5f, "a reel landing where it started still spins a full turn");
            Assert.Greater(s.Speed(reel, s.LandTime(reel) - 0.05f), Knobs.blurSpeed, "fast enough to blur just before it lands");
            Assert.Less(s.Speed(reel, s.Duration + 0.5f), 0.01f, "still once settled");
        }
    }

    [Test]
    public void ALanding_OvershootsThePayline_ByTheKnob_ScaledByTheMotionIntensity()
    {
        SlotSpinSchedule full = Schedule(new float[] { 0, 0, 0 }, new[] { 2, 2, 2 }, 5, MotionAmount.Full);
        SlotSpinSchedule half = Schedule(new float[] { 0, 0, 0 }, new[] { 2, 2, 2 }, 5, new MotionAmount(0.5f, false));
        float Peak(SlotSpinSchedule s)
        {
            float landed = s.Position(0, s.LandTime(0)), peak = 0f;
            for (float t = s.LandTime(0); t < s.Duration; t += 0.002f)
                peak = System.Math.Max(peak, s.Position(0, t) - landed);
            return peak;
        }
        Assert.AreEqual(Knobs.landOvershoot, Peak(full), 0.01f);
        Assert.AreEqual(Knobs.landOvershoot * 0.5f, Peak(half), 0.01f);
        Assert.Less(Knobs.landOvershoot, 0.5f, "an overshoot never shows the next symbol on the payline");
    }

    [Test]
    public void ReducedMotion_CutsStraightToTheResult()
    {
        SlotSpinSchedule s = Schedule(new float[] { 1, 2, 3 }, new[] { 4, 0, 2 }, 5, new MotionAmount(1f, true));
        Assert.IsTrue(s.Cut);
        Assert.AreEqual(0f, s.Duration);
        Assert.AreEqual(4f, s.Position(0, 0f));
        Assert.AreEqual(0f, s.Position(1, 0f));
        Assert.AreEqual(2f, s.Position(2, 0f));
    }

    [Test]
    public void TheSameInputs_DrawTheSameMotion()
    {
        SlotSpinSchedule a = Schedule(new float[] { 2, 4, 1 }, new[] { 3, 3, 3 }, 5, MotionAmount.Full);
        SlotSpinSchedule b = Schedule(new float[] { 2, 4, 1 }, new[] { 3, 3, 3 }, 5, MotionAmount.Full);
        Assert.AreEqual(a.Duration, b.Duration);
        for (float t = -0.1f; t < a.Duration + 0.2f; t += 0.0137f)
            for (int reel = 0; reel < 3; reel++)
                Assert.AreEqual(a.Position(reel, t), b.Position(reel, t), $"reel {reel} at {t}");
    }

    [Test]
    public void FaceAt_WrapsAnyPosition()
    {
        Assert.AreEqual(0, SlotSpinSchedule.FaceAt(10f, 5));
        Assert.AreEqual(4, SlotSpinSchedule.FaceAt(-1f, 5));
        Assert.AreEqual(3, SlotSpinSchedule.FaceAt(7.8f, 5));
        Assert.AreEqual(0, SlotSpinSchedule.FaceAt(3f, 0), "no symbols: face 0");
    }

    [Test]
    public void AFullDrag_PullsTheLever_ThroughItsNotches_AndFiresOnce()
    {
        var lever = new SlotLever();
        int notches = 0, fired = 0;
        for (float px = 0f; px <= Knobs.leverTravel * 1.2f; px += 5f)
        {
            SlotLever.Move move = lever.Drag(px, Knobs);
            notches += move.Notches;
            fired += move.Fire ? 1 : 0;
        }
        Assert.AreEqual(1, fired, "one spin per pull");
        Assert.AreEqual(Knobs.leverNotches, notches, "a click per notch");
        Assert.AreEqual(1f, lever.Pull, 1e-4f);
    }

    [Test]
    public void TheRatchet_HoldsTheLeverBehindTheHand_BetweenNotches()
    {
        var lever = new SlotLever();
        float notch = Knobs.leverTravel / Knobs.leverNotches;
        lever.Drag(notch * 1.5f, Knobs);
        Assert.Less(lever.Pull, 1.5f / Knobs.leverNotches, "it lags between notches");
        Assert.Greater(lever.Pull, 1f / Knobs.leverNotches, "past the notch it caught");
    }

    [Test]
    public void AShortDrag_DoesNotFire_AndLetGo_TheLeverSpringsBack()
    {
        var lever = new SlotLever();
        Assert.IsFalse(lever.Drag(Knobs.leverTravel * 0.5f, Knobs).Fire);
        lever.Release(MotionAmount.Full);
        for (int i = 0; i < 400; i++)
            lever.Step(1f / 60f, Knobs, Motion, MotionAmount.Full);
        Assert.AreEqual(0f, lever.Pull, 1e-3f);
        Assert.IsFalse(lever.Moving);
    }

    [Test]
    public void AClick_PullsTheLeverAllTheWay_FiresOnce_AndComesBack()
    {
        var lever = new SlotLever();
        lever.PullAll(MotionAmount.Full);
        int fired = 0;
        float deepest = 0f;
        for (int i = 0; i < 600; i++)
        {
            fired += lever.Step(1f / 60f, Knobs, Motion, MotionAmount.Full).Fire ? 1 : 0;
            deepest = System.Math.Max(deepest, lever.Pull);
        }
        Assert.AreEqual(1, fired);
        Assert.GreaterOrEqual(deepest, Knobs.leverFireAt);
        Assert.AreEqual(0f, lever.Pull, 1e-3f);
    }

    [Test]
    public void UnderReducedMotion_AClickFiresAtOnce_AndTheLeverStaysUp()
    {
        var lever = new SlotLever();
        lever.PullAll(new MotionAmount(1f, true));
        SlotLever.Move move = lever.Step(1f / 60f, Knobs, Motion, new MotionAmount(1f, true));
        Assert.IsTrue(move.Fire);
        Assert.AreEqual(0f, lever.Pull);
    }
}
