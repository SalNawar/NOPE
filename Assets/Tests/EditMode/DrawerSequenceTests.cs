using System.Collections.Generic;
using NUnit.Framework;

/// <summary>The heavy brass stamp drawer's motion (DrawerSequence, Track BR) and the prop contract's art-or-fallback rule (PropArt).</summary>
public sealed class DrawerSequenceTests
{
    private const float Dt = 1f / 120f;

    private static MotionKnobs Knobs => new MotionKnobs();

    /// <summary>Steps the drawer for <paramref name="seconds"/>, collecting each frame's travel, both raises and the events (with their time).</summary>
    private static List<(float t, float travel, float denied, float approved, DrawerEvents e)> Run(DrawerSequence d, bool open, float seconds, bool hold = false, float start = 0f)
    {
        var frames = new List<(float, float, float, float, DrawerEvents)>();
        MotionKnobs k = Knobs;
        for (float t = start; t < start + seconds; t += Dt)
        {
            DrawerEvents e = d.Step(Dt, open, k, false, hold);
            frames.Add((t + Dt, d.Travel, d.Raise(DrawerSequence.Denied), d.Raise(DrawerSequence.Approved), e));
        }
        return frames;
    }

    private static float FirstTime(List<(float t, float travel, float denied, float approved, DrawerEvents e)> frames, DrawerEvents flag)
    {
        foreach (var f in frames)
            if ((f.e & flag) != 0)
                return f.t;
        return -1f;
    }

    private static int Count(List<(float t, float travel, float denied, float approved, DrawerEvents e)> frames, DrawerEvents flag)
    {
        int n = 0;
        foreach (var f in frames)
            if ((f.e & flag) != 0)
                n++;
        return n;
    }

    [Test]
    public void Opening_SetsOffSlowly_Accelerates_AndHitsTheStopOnTime()
    {
        var d = new DrawerSequence();
        var frames = Run(d, true, 2f);
        MotionKnobs k = Knobs;
        Assert.AreEqual(Dt, FirstTime(frames, DrawerEvents.Carried), 1e-5f, "the carry (and its sound) sets off on the first frame");
        float quarter = 0f, half = 0f, late = 0f;
        foreach (var f in frames)
        {
            if (quarter == 0f && f.t >= k.drawerCarrySeconds * 0.25f) quarter = f.travel;
            if (half == 0f && f.t >= k.drawerCarrySeconds * 0.5f) half = f.travel;
            if (late == 0f && f.t >= k.drawerCarrySeconds * 0.75f) late = f.travel;
        }
        Assert.Less(quarter, 0.06f, "a slow start: a quarter of the time covers little of the way");
        Assert.Greater(late - half, half - quarter, "an accelerating carry: each stretch covers more than the last");
        Assert.AreEqual(k.drawerCarrySeconds, FirstTime(frames, DrawerEvents.Stopped), Dt * 1.5f, "the stop comes when the carry's time is up");
        Assert.AreEqual(1, Count(frames, DrawerEvents.Stopped), "one stop");
    }

    [Test]
    public void TheStop_OvershootsALittle_ThenSettlesOut()
    {
        var d = new DrawerSequence();
        var frames = Run(d, true, 2f);
        float most = 0f;
        foreach (var f in frames)
            most = System.Math.Max(most, f.travel);
        Assert.Greater(most, 1.005f, "the hard stop swings a little past out on the stiff spring");
        Assert.Less(most, 1.06f, "a small overshoot: a heavy drawer, not a jelly");
        Assert.AreEqual(1f, d.Travel, "it settles all the way out");
    }

    [Test]
    public void Cradles_StandDeniedThenApproved_EachLocksOnce_NeverPastUpright_AndBounce()
    {
        var d = new DrawerSequence();
        var frames = Run(d, true, 2f);
        float denied = FirstTime(frames, DrawerEvents.LockedDenied), approved = FirstTime(frames, DrawerEvents.LockedApproved);
        Assert.Greater(denied, 0f, "DENIED locks upright");
        Assert.Greater(approved, denied + Knobs.drawerRaiseStagger * 0.5f, "APPROVED locks after DENIED, staggered");
        Assert.AreEqual(1, Count(frames, DrawerEvents.LockedDenied));
        Assert.AreEqual(1, Count(frames, DrawerEvents.LockedApproved));
        Assert.Greater(denied, Knobs.drawerCarrySeconds * Knobs.drawerRaiseFrom, "they stand as the drawer reaches the end of its travel");
        bool dipped = false;
        foreach (var f in frames)
        {
            Assert.LessOrEqual(f.denied, 1f, "the upright stop holds");
            Assert.LessOrEqual(f.approved, 1f, "the upright stop holds");
            if (f.t > denied && f.denied < 0.995f)
                dipped = true;
        }
        Assert.IsTrue(dipped, "the lock bounces the cradle back a little");
        Assert.IsTrue(d.Locked(DrawerSequence.Denied) && d.Locked(DrawerSequence.Approved));
        Assert.AreEqual(1f, d.Raise(DrawerSequence.Denied));
        Assert.AreEqual(1f, d.Raise(DrawerSequence.Approved));
    }

    [Test]
    public void Closing_FoldsApprovedThenDenied_ThenShoves_AndSeatsStillMoving()
    {
        var d = new DrawerSequence();
        Run(d, true, 2f);
        var frames = Run(d, false, 2f, false, 2f);
        float approved = FirstTime(frames, DrawerEvents.FoldedApproved), denied = FirstTime(frames, DrawerEvents.FoldedDenied);
        float shove = FirstTime(frames, DrawerEvents.Shoved), seated = FirstTime(frames, DrawerEvents.Seated);
        Assert.Greater(approved, 0f);
        Assert.Greater(denied, approved, "the reverse order: APPROVED folds first");
        Assert.GreaterOrEqual(shove, denied, "the drawer slides in only once both daters lie flat");
        foreach (var f in frames)
            if (f.t < shove)
                Assert.AreEqual(1f, f.travel, "it stays out while they fold");
        Assert.AreEqual(Knobs.drawerShoveSeconds, seated - shove, Dt * 1.5f, "the shove takes its time");
        Assert.IsFalse(d.Locked(DrawerSequence.Denied) || d.Locked(DrawerSequence.Approved), "folded daters take no input");
        Assert.IsTrue(d.Closed);
        Assert.AreEqual(0f, d.Travel);
    }

    [Test]
    public void TheShove_IsFastAtFirst_AndStillMovingAsItSeats()
    {
        float kick = Knobs.drawerShoveKick;
        float start = DrawerSequence.ShoveCurve(0.05f, kick) / 0.05f, end = (1f - DrawerSequence.ShoveCurve(0.95f, kick)) / 0.05f;
        Assert.Greater(start, 1.4f, "a shove: most speed at the start");
        Assert.Greater(end, 0.2f, "it seats still moving (the thud)");
        Assert.Less(end, start);
        Assert.AreEqual(0f, DrawerSequence.ShoveCurve(0f, kick));
        Assert.AreEqual(1f, DrawerSequence.ShoveCurve(1f, kick), 1e-6f);
        for (float u = 0f; u < 1f; u += 0.05f)
            Assert.LessOrEqual(DrawerSequence.ShoveCurve(u, 1f), DrawerSequence.ShoveCurve(u + 0.05f, 1f), "it never runs back, even at the most kick");
    }

    [Test]
    public void ADaterStillAway_HoldsTheFold_AndTheDrawerOut()
    {
        var d = new DrawerSequence();
        Run(d, true, 2f);
        var held = Run(d, false, 1f, true, 2f);
        Assert.AreEqual(0, Count(held, DrawerEvents.FoldedApproved | DrawerEvents.FoldedDenied | DrawerEvents.Shoved));
        Assert.AreEqual(1f, d.Travel);
        var free = Run(d, false, 2f, false, 3f);
        Assert.AreEqual(1, Count(free, DrawerEvents.Seated), "once the dater is back, it folds and seats");
    }

    [Test]
    public void ReducedMotion_Snaps_AndStillSaysWhatHappened()
    {
        var d = new DrawerSequence();
        DrawerEvents open = d.Step(Dt, true, Knobs, true, false);
        Assert.AreEqual(1f, d.Travel);
        Assert.IsTrue(d.Locked(DrawerSequence.Denied) && d.Locked(DrawerSequence.Approved), "usable at once");
        Assert.AreEqual(DrawerEvents.Carried | DrawerEvents.Stopped | DrawerEvents.LockedDenied | DrawerEvents.LockedApproved, open);
        Assert.AreEqual(DrawerEvents.None, d.Step(Dt, true, Knobs, true, false), "nothing more while it stays out");
        DrawerEvents close = d.Step(Dt, false, Knobs, true, false);
        Assert.IsTrue(d.Closed);
        Assert.AreEqual(DrawerEvents.FoldedDenied | DrawerEvents.FoldedApproved | DrawerEvents.Shoved | DrawerEvents.Seated, close);
    }

    [Test]
    public void AReversal_PicksUpFromWhereItIs()
    {
        var d = new DrawerSequence();
        Run(d, true, 0.2f);
        float mid = d.Travel;
        Assert.Greater(mid, 0f);
        Assert.Less(mid, 1f);
        var back = Run(d, false, 2f, false, 0.2f);
        Assert.AreEqual(1, Count(back, DrawerEvents.Seated), "a close mid-carry stops it and shoves it home");
        Assert.LessOrEqual(back[0].travel, mid + 1e-5f, "it never jumps out further");
        var again = Run(d, true, 2f, false, 2.2f);
        Assert.AreEqual(1, Count(again, DrawerEvents.Stopped));
        Assert.AreEqual(1f, d.Travel);
    }

    [Test]
    public void PropArt_UsesTheModel_OnlyWhenItCarriesEveryPart()
    {
        var all = new HashSet<string>(PropArt.BrassDrawer);
        Assert.IsTrue(PropArt.UseArt(true, PropArt.BrassDrawer, all.Contains), "a complete model is used");
        Assert.IsFalse(PropArt.UseArt(false, PropArt.BrassDrawer, all.Contains), "no model: the built rack");
        all.Remove("LeverApproved");
        Assert.IsFalse(PropArt.UseArt(true, PropArt.BrassDrawer, all.Contains), "one part missing: the built rack");
        CollectionAssert.AreEqual(new[] { "LeverApproved" }, PropArt.Missing(PropArt.BrassDrawer, all.Contains), "and the missing part is named");
        CollectionAssert.AreEqual(PropArt.BrassDrawer, PropArt.Missing(PropArt.BrassDrawer, null), "nothing to look in: all missing");
    }
}
