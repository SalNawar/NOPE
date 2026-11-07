using NUnit.Framework;

/// <summary>
/// The bound a held thing's root obeys while it is dragged (HeldBound;
/// FOLD-01 round 4, the round 3 verifier's G2). Papers, Please: a paper
/// pressed on the counter follows the pointer one to one onto the desk, and
/// a paper that lies wholly on the desk is kept there. The bound is stepped
/// frame by frame through the counter's edge while the paper's size grows
/// under the cursor (the grab scale 1.58, 2.43, 3.01, 3.31, 3.39: the
/// verifier's o4, the passport pressed on its closed cover). The desk is the
/// one the fold_rest scenario measured (DropResolverTests): the clamp area
/// 1.9925 by 1.015 m, the back step's lip at y 0.54, the reading view's
/// corners.
/// </summary>
public class HeldBoundTests
{
    private const float Eps = 1e-5f;

    /// <summary>The desk surface's clamp area (metres, centred on the rest frame's origin).</summary>
    private const float ClampW = 1.9925f, ClampH = 1.015f;

    /// <summary>Where the back step's lip stands (DeskPaintedMap.BackStep): a thing on the desk ends there.</summary>
    private const float BackStep = DeskPaintedMap.BackStep;

    /// <summary>The reading view's corners on the desk (near left, near right, far left, far right).</summary>
    private static readonly (float x, float y)[] ViewCorners = { (-1.449f, -0.592f), (0.921f, -0.592f), (-1.639f, 0.840f), (1.112f, 0.840f) };

    /// <summary>The open passport at its desk size (metres), and the share of that it shows on the counter (it grows 3.39 times).</summary>
    private const float DeskW = 0.31f, DeskH = 0.4452f, Grows = 3.39f;

    /// <summary>The passport's x, its root on the counter, and its root's offset from the point pressed on its closed cover (at the counter's size).</summary>
    private const float X = -0.2f, CounterRoot = 0.4375f, Offset = 0.0325f;

    /// <summary>DeskController.RestArea's rule: the clamp area grown by the footprint; on the desk, short of the back step and in the reading view.</summary>
    private static DeskRect AreaFor(DeskRect footprint, bool onDesk)
    {
        var desk = new DeskRect(0f, 0f, ClampW + footprint.Width, ClampH + footprint.Height);
        return onDesk ? DropResolver.Area(desk, BackStep, ViewCorners) : desk;
    }

    private static DeskRect Counter(float y) => new DeskRect(X, y, DeskW / Grows, DeskH / Grows);

    private static DeskRect Desk(float y) => new DeskRect(X, y, DeskW, DeskH);

    /// <summary>One follow on the desk: the pointer at <paramref name="pointer"/>, the grab scale <paramref name="scale"/>; the root goes where the grown offset puts it, and still moves by the scale left to grow.</summary>
    private static (float x, float y) DeskFollow(HeldBound bound, float pointer, float scale, bool growthToCome = true) =>
        bound.Step(Desk(pointer + Offset * scale), 0f, growthToCome ? Offset * (Grows - scale) : 0f, true, Counter(CounterRoot), AreaFor);

    /// <summary>The drag's first follow (the EventSystem's): the pointer has already caught up the drag threshold, but the desk sizes the paper for the zone under it only after this follow, so it is still judged on the counter.</summary>
    private static (float x, float y) FirstFollow(HeldBound bound, float pointer) =>
        bound.Step(Counter(pointer + Offset), 0f, 0f, false, Counter(CounterRoot), AreaFor);

    [Test]
    public void APassportPulledFromTheCounter_FollowsThePointerOneToOne_WhileItsSizeGrowsUnderTheCursor()
    {
        var bound = new HeldBound();
        bound.PickUp();
        (float x, float y) first = FirstFollow(bound, 0.24f);
        Assert.AreEqual(0f, first.x, Eps);
        Assert.AreEqual(0f, first.y, Eps, "the first follow, still sized for the counter, goes where the pointer takes it");
        Assert.IsTrue(bound.Holding, "it lay wholly on the counter where it was picked up");

        // The counter's edge crossed: the paper grows over 5 frames while the pointer moves on toward the chair.
        float[] pointer = { 0.24f, 0.235f, 0.23f, 0.225f, 0.22f };
        float[] scale = { 1.58f, 2.43f, 3.01f, 3.31f, Grows };
        for (int frame = 0; frame < pointer.Length; frame++)
        {
            (float x, float y) move = DeskFollow(bound, pointer[frame], scale[frame]);
            Assert.AreEqual(0f, move.x, Eps, $"frame {frame}");
            Assert.AreEqual(0f, move.y, Eps, $"frame {frame}: never held back while it leaves the counter (round 3 held it 10 to 15 mm here)");
            Assert.IsFalse(bound.Holding, $"frame {frame}: grown, its top lies over the back step, so it is still free");
        }

        // On toward the chair: it is held from the first frame its grown footprint lies wholly short of the lip (0.20 + 0.110 + 0.223 < 0.54), with no move then.
        Assert.AreEqual(0f, DeskFollow(bound, 0.21f, Grows).y, Eps);
        Assert.IsFalse(bound.Holding, "0.21 + 0.110 + 0.223 lies past the lip");
        Assert.AreEqual(0f, DeskFollow(bound, 0.20f, Grows).y, Eps, "no jump the frame the bound takes it");
        Assert.IsTrue(bound.Holding);

        // Held from then on (round 2's F3): dragged back toward the counter, its top stops at the lip.
        (float x, float y) back = DeskFollow(bound, 0.26f, Grows);
        Assert.AreEqual(BackStep - DeskH / 2f - (0.26f + Offset * Grows), back.y, Eps, "its top at the lip");
    }

    [Test]
    public void RoundThreesRule_HeldThePassportAtTheLip_JudgedAtTheCountersEdgeWhileItsSizeStillGrew()
    {
        // Round 3 (DeskController.Inward): at the zone change, "it lies wholly on the desk" was judged where the frame's first follow had
        // put it (the smaller offset of the counter's size) but at the full desk size, so the bound turned on at once.
        DeskRect area = AreaFor(Desk(0f), true);
        bool holding = DropResolver.Held(Desk(0.24f + Offset), area, false).holding;
        Assert.IsTrue(holding, "round 3: judged at 0.2725 at the desk size, it lay wholly on the desk");
        (_, float y, _) = DropResolver.Held(Desk(0.23f + Offset * 3.01f), area, holding);
        Assert.Less(y, 0.23f + Offset * 3.01f - 0.01f, "round 3: the grown offset asked for 0.328, the bound held it 10 mm back (the verifier's G2)");

        // Judged at its size now but without the growth still to come, it is held all the same: the size grows on after the bound took it.
        var bound = new HeldBound();
        bound.PickUp();
        FirstFollow(bound, 0.24f);
        Assert.AreEqual(0f, DeskFollow(bound, 0.24f, 1.58f, false).y, Eps);
        Assert.IsTrue(bound.Holding, "its footprint at 0.291 lay wholly on the desk");
        Assert.Less(DeskFollow(bound, 0.23f, 3.01f, false).y, -0.01f, "and the grown offset then pushed it past the lip: held back");
    }

    [Test]
    public void UnderReducedMotion_ThePassportTakesItsDeskSizeAtOnce_AndIsNotHeldBackEither()
    {
        // Reduced Motion: the size changes at once (DeskDocument's resize falls back to a cut), so the grab scale jumps to 3.39 and no growth is to come.
        var bound = new HeldBound();
        bound.PickUp();
        FirstFollow(bound, 0.24f);
        (float x, float y) move = DeskFollow(bound, 0.24f, Grows);
        Assert.AreEqual(0f, move.y, Eps, "it goes where the pointer takes it");
        Assert.IsFalse(bound.Holding);

        DeskRect area = AreaFor(Desk(0f), true);
        bool roundThree = DropResolver.Held(Desk(0.24f + Offset), area, false).holding;
        (_, float y, _) = DropResolver.Held(Desk(0.24f + Offset * Grows), area, roundThree);
        Assert.Less(y, 0.24f + Offset * Grows - 0.03f, "round 3 held it 33 mm back here");
    }

    [Test]
    public void ADeskPaperPickedUpWhollyOnTheDesk_IsHeldFromItsFirstFrame()
    {
        // Round 3's case (the round 2 verifier's b06): a passport 2 cm short of the lip whose first follow catches up the drag
        // threshold and goes 5 cm on. It lay wholly on the desk at the pick-up, so the bound keeps it there from that first frame.
        float lying = BackStep - DeskH / 2f - 0.02f;
        var bound = new HeldBound();
        bound.PickUp();
        (float x, float y) move = bound.Step(Desk(lying + 0.05f), 0f, 0f, true, Desk(lying), AreaFor);
        Assert.IsTrue(bound.Holding);
        Assert.AreEqual(BackStep - DeskH / 2f - (lying + 0.05f), move.y, Eps, "its top at the lip from the first frame");
    }

    [Test]
    public void TheFolder_IsKeptWhollyInside_FromItsFirstFrame()
    {
        const float w = 0.2249f, h = 0.2588f;
        var bound = new HeldBound();
        bound.PickUp();
        (float x, float y) move = bound.Step(new DeskRect(0f, 0.45f, w, h), 0f, 0f, true, null, AreaFor);
        Assert.AreEqual(BackStep - h / 2f - 0.45f, move.y, Eps, "the folder's top at the lip at once (round 2's F3)");
    }

    [Test]
    public void APaperDraggedOntoTheCounterAndBack_StartsFreeInEachZone()
    {
        // A desk paper held on the desk, then (the verdict in) over the counter at its counter's size, then back over the desk:
        // a zone change is not a pick-up, so on the desk again it is free until it lies wholly there.
        float lying = 0.0f;
        var bound = new HeldBound();
        bound.PickUp();
        bound.Step(Desk(lying), 0f, 0f, true, Desk(lying), AreaFor);
        Assert.IsTrue(bound.Holding, "picked up wholly on the desk");
        bound.Step(Counter(0.47f), 0f, 0f, false, Desk(lying), AreaFor);
        Assert.IsTrue(bound.Holding, "on the counter it lies wholly where a counter paper may lie");
        (float x, float y) move = DeskFollow(bound, 0.44f, 1.58f);
        Assert.AreEqual(0f, move.y, Eps, "back over the desk, growing: it follows the pointer");
        Assert.IsFalse(bound.Holding);
    }
}
