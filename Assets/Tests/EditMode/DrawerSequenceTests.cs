using System.Collections.Generic;
using NUnit.Framework;

/// <summary>The fixed stamp rack's cradles (DrawerSequence: the brass drawer's mechanism, with no slide since BUG-73b) and the prop contract's art-or-fallback rule (PropArt).</summary>
public sealed class DrawerSequenceTests
{
    private const float Dt = 1f / 120f;

    private static MotionKnobs Knobs => new MotionKnobs();

    /// <summary>Steps the cradles for <paramref name="seconds"/>, collecting each frame's raises and events (with their time).</summary>
    private static List<(float t, float denied, float approved, DrawerEvents e)> Run(DrawerSequence d, bool open, float seconds, bool hold = false, float start = 0f)
    {
        var frames = new List<(float, float, float, DrawerEvents)>();
        MotionKnobs k = Knobs;
        for (float t = start; t < start + seconds; t += Dt)
        {
            DrawerEvents e = d.Step(Dt, open, k, false, hold);
            frames.Add((t + Dt, d.Raise(DrawerSequence.Denied), d.Raise(DrawerSequence.Approved), e));
        }
        return frames;
    }

    private static float FirstTime(List<(float t, float denied, float approved, DrawerEvents e)> frames, DrawerEvents flag)
    {
        foreach (var f in frames)
            if ((f.e & flag) != 0)
                return f.t;
        return -1f;
    }

    private static int Count(List<(float t, float denied, float approved, DrawerEvents e)> frames, DrawerEvents flag)
    {
        int n = 0;
        foreach (var f in frames)
            if ((f.e & flag) != 0)
                n++;
        return n;
    }

    [Test]
    public void ACaseOpening_StandsTheDeniedCradle_OnItsFirstFrame_NoIdleDelay()
    {
        // BUG-73b: the drawer's invisible slide held the cradles back about 0.34 s every case.
        var d = new DrawerSequence();
        d.Step(Dt, true, Knobs, false, false);
        Assert.Greater(d.Raise(DrawerSequence.Denied), 0f, "the DENIED cradle starts to stand on the case's first frame");
        var frames = Run(d, true, 1f, false, Dt);
        float denied = FirstTime(frames, DrawerEvents.LockedDenied), approved = FirstTime(frames, DrawerEvents.LockedApproved);
        Assert.Greater(denied, 0f);
        Assert.Less(denied, 0.3f, "DENIED locks after its quarter turn only (the Cradle spring), not after a slide");
        Assert.Less(approved, Knobs.drawerRaiseStagger + 0.3f, "APPROVED follows one stagger later");
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
    public void Closing_FoldsApprovedThenDenied_AndTheyTakeNoInput()
    {
        var d = new DrawerSequence();
        Run(d, true, 2f);
        var frames = Run(d, false, 2f, false, 2f);
        float approved = FirstTime(frames, DrawerEvents.FoldedApproved), denied = FirstTime(frames, DrawerEvents.FoldedDenied);
        Assert.Greater(approved, 0f);
        Assert.Greater(denied, approved, "the reverse order: APPROVED folds first");
        Assert.IsFalse(d.Locked(DrawerSequence.Denied) || d.Locked(DrawerSequence.Approved), "folded daters take no input");
        Assert.IsTrue(d.Closed);
        Assert.AreEqual(0f, d.Raise(DrawerSequence.Denied));
        Assert.AreEqual(0f, d.Raise(DrawerSequence.Approved));
    }

    [Test]
    public void ADaterStillAway_HoldsTheFold()
    {
        var d = new DrawerSequence();
        Run(d, true, 2f);
        var held = Run(d, false, 1f, true, 2f);
        Assert.AreEqual(0, Count(held, DrawerEvents.FoldedApproved | DrawerEvents.FoldedDenied));
        Assert.IsTrue(d.Locked(DrawerSequence.Approved), "a dater on its way back still finds its cradle up");
        var free = Run(d, false, 2f, false, 3f);
        Assert.AreEqual(1, Count(free, DrawerEvents.FoldedDenied), "once the dater is back, they fold");
        Assert.IsTrue(d.Closed);
    }

    [Test]
    public void ReducedMotion_Snaps_AndStillSaysWhatHappened()
    {
        var d = new DrawerSequence();
        DrawerEvents open = d.Step(Dt, true, Knobs, true, false);
        Assert.IsTrue(d.Locked(DrawerSequence.Denied) && d.Locked(DrawerSequence.Approved), "usable at once");
        Assert.AreEqual(DrawerEvents.LockedDenied | DrawerEvents.LockedApproved, open);
        Assert.AreEqual(DrawerEvents.None, d.Step(Dt, true, Knobs, true, false), "nothing more while the case stays open");
        DrawerEvents close = d.Step(Dt, false, Knobs, true, false);
        Assert.IsTrue(d.Closed);
        Assert.AreEqual(DrawerEvents.FoldedDenied | DrawerEvents.FoldedApproved, close);
    }

    [Test]
    public void AReversal_PicksUpFromWhereItIs()
    {
        var d = new DrawerSequence();
        Run(d, true, 0.05f);
        float mid = d.Raise(DrawerSequence.Denied);
        Assert.Greater(mid, 0f);
        Assert.Less(mid, 1f);
        var back = Run(d, false, 2f, false, 0.05f);
        Assert.LessOrEqual(back[0].denied, mid + 0.15f, "it carries on from where it was (its swing's momentum only), never jumps");
        Assert.AreEqual(0, Count(back, DrawerEvents.LockedDenied), "turned back mid-rise, it never reaches the lock");
        Assert.AreEqual(1, Count(back, DrawerEvents.FoldedDenied), "a close mid-rise folds it back down");
        var again = Run(d, true, 2f, false, 2.05f);
        Assert.AreEqual(1, Count(again, DrawerEvents.LockedDenied));
        Assert.IsTrue(d.Locked(DrawerSequence.Denied) && d.Locked(DrawerSequence.Approved));
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
