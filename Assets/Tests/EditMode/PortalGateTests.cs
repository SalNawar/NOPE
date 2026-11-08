using NUnit.Framework;

/// <summary>
/// The hall rings as gates (the night of 2026-11-07, DECISIONS D6; PortalGate,
/// PortalGateTrack): the chevrons light one per calibration step and hold, the
/// event horizon opens and closes on its times, a send's kawoosh waits for the
/// open horizon and the dwell, its envelope is smooth and peaks where its knob
/// says, the close fades in small steps, Reduced Motion cuts between steady
/// states, and a ring follows its switchboard module read only.
/// </summary>
public class PortalGateTests
{
    private const float Frame = 1f / 30f;

    // The night's knobs: 7 chevrons, the horizon opening in 0.7 s, the close in 0.8 s, a 1.2 s kawoosh peaking at 0.36 of it, 2 s between changes.
    private static readonly PortalGateTimes Times = new PortalGateTimes(7, 0.7f, 0.8f, 1.2f, 0.36f, 2f);

    private static PortalGateFeed Dialing(float calibrated) => new PortalGateFeed(false, true, calibrated, 20f, false);

    private static readonly PortalGateFeed Live = new PortalGateFeed(false, false, 0f, 20f, true);

    private static readonly PortalGateFeed Off = default;

    [Test]
    public void ChevronsLit_TheFirstAtTheLockIn_OnePerStep_AllOnceCalibrated()
    {
        Assert.AreEqual(1, PortalGate.ChevronsLit(0f, 20f, 7), "the lock-in lights the first");
        Assert.AreEqual(1, PortalGate.ChevronsLit(2.85f, 20f, 7));
        Assert.AreEqual(2, PortalGate.ChevronsLit(20f / 7f, 20f, 7), "one more each 20 / 7 s");
        Assert.AreEqual(7, PortalGate.ChevronsLit(6f * 20f / 7f, 20f, 7), "the last at 17.1 s");
        Assert.AreEqual(7, PortalGate.ChevronsLit(25f, 20f, 7), "never more than the ring has");
        Assert.AreEqual(7, PortalGate.ChevronsLit(0f, 0f, 7), "no calibration: all at once");
        Assert.AreEqual(1, PortalGate.ChevronsLit(-3f, 20f, 7));
    }

    [Test]
    public void ChevronAngle_FollowsTheDial_RightSideDownLeftSideDownTopLast()
    {
        float[] want = { 50f, 10f, -30f, 130f, 170f, 210f, 90f };
        for (int i = 0; i < 7; i++)
            Assert.AreEqual(want[i], PortalGate.ChevronAngle(i, 7, 40f), 1e-4f, "chevron " + (i + 1));
        Assert.AreEqual(90f, PortalGate.ChevronAngle(0, 1, 40f), "a single chevron is the top one");
        Assert.AreEqual(60f, PortalGate.ChevronAngle(0, 7, 30f), 1e-4f, "an arch's sites are closer");
    }

    [Test]
    public void Kawoosh_IsSmooth_PeaksAtItsKnob_HeldWithReducedMotion()
    {
        Assert.AreEqual(0f, PortalGate.Kawoosh(0f, 0.36f, false));
        Assert.AreEqual(1f, PortalGate.Kawoosh(0.36f, 0.36f, false), 1e-5f);
        Assert.AreEqual(0f, PortalGate.Kawoosh(1f, 0.36f, false));
        // 36 frames at 30 fps: the peak at frame 12, never more than 0.15 a frame.
        int peak = 0;
        float best = 0f, last = 0f, steepest = 0f;
        for (int f = 0; f <= 36; f++)
        {
            float e = PortalGate.Kawoosh(f / 36f, 0.36f, false);
            steepest = System.Math.Max(steepest, System.Math.Abs(e - last));
            last = e;
            if (e > best)
            {
                best = e;
                peak = f;
            }
        }
        Assert.AreEqual(13, peak, "the peak at frame 13 (the brief: 10 to 14)");
        Assert.LessOrEqual(steepest, 0.15f, "no step over 0.15 a frame");
        Assert.AreEqual(1f, PortalGate.Kawoosh(0.05f, 0.36f, true), "reduced motion: a held state");
        Assert.AreEqual(1f, PortalGate.Kawoosh(0.9f, 0.36f, true));
        Assert.AreEqual(0f, PortalGate.Kawoosh(1f, 0.36f, true), "then cut back");
    }

    [Test]
    public void Front_RunsOutToItsReachByThePeak_ThenHolds()
    {
        Assert.AreEqual(0.2f, PortalGate.Front(0f, 0.36f, 0.2f, 1.15f), 1e-5f);
        Assert.AreEqual(0.675f, PortalGate.Front(0.18f, 0.36f, 0.2f, 1.15f), 1e-5f, "a steady speed out");
        Assert.AreEqual(1.15f, PortalGate.Front(0.36f, 0.36f, 0.2f, 1.15f), 1e-5f);
        Assert.AreEqual(1.15f, PortalGate.Front(0.8f, 0.36f, 0.2f, 1.15f), 1e-5f, "held while it settles");
    }

    [Test]
    public void Track_ADial_ChevronsStepOnePerCalibrationStep_NeverTwoChangesWithinTheDwell()
    {
        var track = new PortalGateTrack();
        int lastLit = 0, changes = 0;
        float lastChange = -10f, closest = float.MaxValue;
        // 20 s of calibration at 30 fps, then the route live and its group sent in the same frame (the board's Tick does both).
        for (int f = 0; f < 20 * 30; f++)
        {
            track.Step(Dialing(f * Frame), Times, Frame, false);
            Assert.AreEqual(PortalGateState.LockedIn, track.State);
            if (track.Lit != lastLit)
            {
                Assert.AreEqual(lastLit + 1, track.Lit, "one chevron at a time");
                closest = System.Math.Min(closest, f * Frame - lastChange);
                lastChange = f * Frame;
                lastLit = track.Lit;
                changes++;
            }
            Assert.AreEqual(1f, track.Chevrons, "the chevrons cut in and hold");
            Assert.AreEqual(0f, track.Pool, "no event horizon while locked in");
        }
        Assert.AreEqual(7, changes);
        Assert.AreEqual(7, track.Lit);

        track.Send();
        float opened = 20f, kawoosh = -1f;
        for (int f = 0; f < 5 * 30; f++)
        {
            track.Step(Live, Times, Frame, false);
            if (track.BurstStarted)
                kawoosh = 20f + (f + 1) * Frame;
        }
        Assert.GreaterOrEqual(closest, 2f, "the chevrons step 20 / 7 s apart");
        Assert.GreaterOrEqual(opened - lastChange, 2f, "the open comes a step after the last chevron");
        Assert.Greater(kawoosh, 0f, "the send plays its kawoosh");
        Assert.GreaterOrEqual(kawoosh - opened, Times.DwellSeconds - 1e-3f, "the kawoosh waits the dwell after the open");
        Assert.AreEqual(PortalGateState.Open, track.State, "the kawoosh settles back into the event horizon");
        Assert.AreEqual(1f, track.Pool, 1e-5f);
        Assert.AreEqual(1f, track.Iris, 1e-5f);
        Assert.AreEqual(7, track.Lit, "every chevron stays lit while open");
    }

    [Test]
    public void Track_TheKawoosh_PlaysOnce_InItsTime_OnlyOnAnOpenHorizon()
    {
        var track = new PortalGateTrack();
        track.Send();
        track.Step(Off, Times, Frame, false);
        Assert.IsFalse(track.Pending, "a dark ring drops a send");

        for (int f = 0; f < 4 * 30; f++)
            track.Step(Live, Times, Frame, false);
        track.Send();
        int frames = 0, starts = 0;
        for (int f = 0; f < 3 * 30; f++)
        {
            track.Step(Live, Times, Frame, false);
            if (track.BurstStarted)
                starts++;
            if (track.State == PortalGateState.Sending)
                frames++;
        }
        Assert.AreEqual(1, starts);
        Assert.That(frames, Is.InRange(35, 37), "the kawoosh lasts 36 frames");
    }

    [Test]
    public void Track_TheClose_FadesTheHorizonAndTheChevrons_InStepsUnderATenth()
    {
        var track = new PortalGateTrack();
        for (int f = 0; f < 3 * 30; f++)
            track.Step(Live, Times, Frame, false);
        float pool = track.Pool, chevrons = track.Chevrons, steepest = 0f;
        int frames = 0;
        while (track.State != PortalGateState.Offline && frames < 100)
        {
            track.Step(Off, Times, Frame, false);
            steepest = System.Math.Max(steepest, System.Math.Max(pool - track.Pool, chevrons - track.Chevrons));
            pool = track.Pool;
            chevrons = track.Chevrons;
            frames++;
            if (track.State != PortalGateState.Offline)
                Assert.AreEqual(PortalGateState.Closing, track.State);
        }
        Assert.That(frames, Is.InRange(23, 25), "0.8 s: 24 frames");
        Assert.LessOrEqual(steepest, 0.1f, "no step over 0.1 a frame");
        Assert.AreEqual(0, track.Lit, "the chevrons are dark again");
        Assert.AreEqual(0f, track.Iris, "the iris resets once faded");
    }

    [Test]
    public void Track_ReducedMotion_CutsBetweenSteadyStates()
    {
        var track = new PortalGateTrack();
        track.Step(Live, Times, Frame, true);
        Assert.AreEqual(1f, track.Pool, "the horizon cuts open");
        Assert.AreEqual(1f, track.Iris);
        for (int f = 0; f < 3 * 30; f++)
            track.Step(Live, Times, Frame, true);
        track.Send();
        track.Step(Live, Times, Frame, true);
        Assert.IsTrue(track.BurstStarted);
        track.Step(Live, Times, Frame, true);
        Assert.AreEqual(1f, track.Envelope(Times, true), "the kawoosh is a held bright state");
        track.Step(Off, Times, Frame, true);
        Assert.AreEqual(PortalGateState.Offline, track.State, "the close cuts");
        Assert.AreEqual(0f, track.Pool);
        Assert.AreEqual(0f, track.Chevrons);
    }

    [Test]
    public void Track_TheReturnGate_StaysOpen_AndADialLightsItsChevronsOverIt()
    {
        var track = new PortalGateTrack();
        var steady = new PortalGateFeed(true, false, 0f, 20f, false);
        for (int f = 0; f < 30; f++)
            track.Step(steady, Times, Frame, false);
        Assert.AreEqual(PortalGateState.Open, track.State);
        Assert.AreEqual(0, track.Lit, "no chevrons without a dial");
        track.Step(new PortalGateFeed(true, true, 5f, 20f, false), Times, Frame, false);
        Assert.AreEqual(2, track.Lit);
        Assert.AreEqual(1f, track.Pool, 1e-5f, "the warm horizon stays");
    }

    [Test]
    public void Feed_FollowsItsModule_ReadOnly()
    {
        var board = new Switchboard(new SwitchboardRules(), 5, 4, 3);
        board.SetInService(0, true);
        board.PlugPower(0, 0);
        var route = new SwitchRoute(0, 0);
        board.AddCall(route);
        board.AddCall(route);
        SwitchModule m = board.Modules[PortalGateFeed.ModuleOf(1)];
        Assert.AreEqual(0, m.Index, "ring 01 is module 01");
        Assert.AreEqual(-1, PortalGateFeed.ModuleOf(0));

        PortalGateFeed idle = PortalGateFeed.Of(m, 20f, false);
        Assert.IsFalse(idle.Horizon || idle.Chevrons, "a free module: a dark ring");
        Assert.IsTrue(PortalGateFeed.Of(null, 20f, true).Horizon, "no module: the day's look");

        board.PlugPlace(0, 0);
        board.PlugPeriod(0, route);
        board.PullMaster();
        board.Tick(5f);
        PortalGateFeed dialing = PortalGateFeed.Of(m, 20f, false);
        Assert.IsTrue(dialing.Dialing);
        Assert.AreEqual(5f, dialing.Calibrated, 1e-4f);
        Assert.AreEqual(SwitchModuleState.Committed, m.State, "reading changes nothing");

        int seen = 0;
        Assert.IsFalse(PortalGateFeed.SentSince(m, ref seen));
        board.Tick(15f);
        Assert.IsTrue(PortalGateFeed.Of(m, 20f, false).Live);
        Assert.IsTrue(PortalGateFeed.SentSince(m, ref seen), "the group of two went: one kawoosh");
        Assert.AreEqual(2, seen);
        Assert.IsFalse(PortalGateFeed.SentSince(m, ref seen), "once");

        board.UnplugPlace(0);
        Assert.IsFalse(PortalGateFeed.Of(m, 20f, false).Horizon, "a cleared route closes the ring");
    }

    [Test]
    public void Feed_OnlyAnOpenDepartureRingTakesItsDial()
    {
        var board = new Switchboard(new SwitchboardRules(), 5, 4, 3);
        board.SetInService(0, true);
        board.PlugPower(0, 0);
        var route = new SwitchRoute(0, 0);
        board.AddCall(route);
        board.PlugPlace(0, 0);
        board.PlugPeriod(0, route);
        board.PullMaster();
        board.Tick(5f);
        SwitchModule m = board.Modules[0];
        Assert.AreEqual(SwitchModuleState.Committed, m.State);

        Assert.IsTrue(PortalGateFeed.TakesDial(PortalLook.Glow));
        Assert.IsFalse(PortalGateFeed.TakesDial(PortalLook.Empty));
        Assert.IsFalse(PortalGateFeed.TakesDial(PortalLook.Dimmed));
        Assert.IsFalse(PortalGateFeed.TakesDial(PortalLook.ReturnGate));

        PortalGateFeed glow = PortalGateFeed.ForRing(PortalLook.Glow, m, 20f, true, false);
        Assert.IsTrue(glow.Dialing && glow.Chevrons, "an open departure ring lights its chevrons");
        Assert.IsFalse(glow.Horizon, "with a board it waits for the route to go live");

        foreach (PortalLook closed in new[] { PortalLook.Empty, PortalLook.Dimmed })
        {
            PortalGateFeed f = PortalGateFeed.ForRing(closed, m, 20f, true, false);
            Assert.IsFalse(f.Dialing || f.Chevrons || f.Horizon, closed + ": a closed ring stays dark while its module dials");
        }
        PortalGateFeed ret = PortalGateFeed.ForRing(PortalLook.ReturnGate, m, 20f, true, false);
        Assert.IsTrue(ret.Horizon && ret.Steady, "the Return Gate keeps its steady horizon");
        Assert.IsFalse(ret.Dialing || ret.Chevrons, "the Return Gate takes no dial");

        board.Tick(15f);
        Assert.AreEqual(SwitchModuleState.Open, m.State);
        Assert.IsTrue(PortalGateFeed.ForRing(PortalLook.Glow, m, 20f, true, false).Live);
        Assert.IsFalse(PortalGateFeed.ForRing(PortalLook.Dimmed, m, 20f, true, false).Horizon, "a live module does not open a ring under maintenance");
        Assert.IsFalse(PortalGateFeed.ForRing(PortalLook.Empty, m, 20f, true, false).Horizon, "a live module does not open a CLOSED ring");

        Assert.IsTrue(PortalGateFeed.ForRing(PortalLook.Glow, null, 20f, false, false).Horizon, "no board: an open route holds its horizon");
        Assert.IsFalse(PortalGateFeed.ForRing(PortalLook.Glow, null, 20f, true, false).Horizon, "a board: an idle ring is dark");
        Assert.IsTrue(PortalGateFeed.ForRing(PortalLook.Empty, m, 20f, true, true).Horizon, "the cheat opens every ring");
    }
}
