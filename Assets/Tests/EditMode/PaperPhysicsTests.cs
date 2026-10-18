using NUnit.Framework;

/// <summary>
/// The desk's physics feel (Saleh 2026-10-17: "KEEP IT TOP DOWN BUT GIVE IT
/// PHYSICS"; 1018a: "all documents are stiff"): PaperPhysics swings a held
/// body against its travel and bows it along it by its speed, twists its
/// corners as it swings and ripples it on a fast move; it slides a let-go body
/// on into its place without passing it, spins it once by its sideways
/// momentum, hops it and settles its bow through a wobble to flat, lighter
/// for a heavy body, still under Reduced Motion.
/// </summary>
public class PaperPhysicsTests
{
    private const float Dt = 1f / 60f;

    /// <summary>Holds a body and drags it at (vx, vz) metres per second for a quarter second; returns it and its place.</summary>
    private static (PaperPhysics body, float x, float z) Dragged(float vx, float vz, float weight = 1f, MotionAmount? amount = null)
    {
        var body = new PaperPhysics(weight);
        var knobs = new MotionKnobs();
        float x = 0f, z = 0f;
        for (int i = 0; i < 15; i++)
        {
            body.Step(Dt, x, z, true, knobs, amount ?? MotionAmount.Full);
            x += vx * Dt;
            z += vz * Dt;
        }
        return (body, x, z);
    }

    [Test]
    public void AHeldBody_SwingsAgainstItsSidewaysTravel_AndBowsAlongIt()
    {
        (PaperPhysics right, _, _) = Dragged(0.8f, 0f);
        (PaperPhysics left, _, _) = Dragged(-0.8f, 0f);
        Assert.Less(right.Yaw, 0f, "dragged right, it turns against it (its trailing corners lag)");
        Assert.Greater(left.Yaw, 0f);
        Assert.Greater(right.Bow, 0f, "its ends droop with its speed");
        Assert.AreEqual(1f, right.BowDirX, 1e-3f, "along its travel");
        Assert.LessOrEqual(right.Bow, new MotionKnobs().paperBowMax * 1.6f, "within its most (the spring's overshoot aside)");
        Assert.AreNotEqual(0f, right.Twist, "its corners twist as it swings");
        (PaperPhysics still, _, _) = Dragged(0f, 0f);
        Assert.AreEqual(0f, still.Yaw, 1e-4f, "held still, no swing");
        Assert.AreEqual(0f, still.Bow, 1e-5f);
        Assert.AreEqual(0f, still.Twist, 1e-5f);
    }

    [Test]
    public void AFastMove_Flutters_ASlowOneDoesNot()
    {
        var knobs = new MotionKnobs();
        (PaperPhysics slow, _, _) = Dragged(knobs.paperFlutterFrom * 0.5f, 0f);
        (PaperPhysics fast, _, _) = Dragged(knobs.paperFlutterFrom * 3f, 0f);
        Assert.AreEqual(0f, slow.Flutter, 1e-5f, "no ripple below its speed");
        Assert.Greater(fast.Flutter, 0f, "a fast move ripples");
        Assert.Greater(fast.FlutterPhase, 0f, "and the ripple runs");
        Assert.AreEqual(0f, PaperPhysics.FlutterHeight(0.2f, 0.5f, 1f, 1f));
        Assert.AreEqual(0.5f, PaperPhysics.FlutterHeight(2f, 0.5f, 1f, 0.5f), 1e-6f, "at most its most");
    }

    [Test]
    public void LetGo_ItsBowSettlesThroughAWobble_ToExactlyFlat()
    {
        var knobs = new MotionKnobs();
        (PaperPhysics body, float x, float z) = Dragged(0.9f, 0.2f);
        float most = 0f, least = 0f;
        body.Step(Dt, x, z, false, knobs, MotionAmount.Full);
        for (int i = 0; i < 600 && body.Moving; i++)
        {
            body.Step(Dt, x, z, false, knobs, MotionAmount.Full);
            most = System.Math.Max(most, body.Bow);
            least = System.Math.Min(least, body.Bow);
        }
        Assert.Less(least, 0f, "its ends curl up as it lands");
        Assert.Greater(most, 0f, "then droop again: a wobble, not a fade");
        Assert.IsFalse(body.Moving, "and it comes to rest");
        Assert.AreEqual(0f, body.Bow, "exactly flat: the text reads as printed");
        Assert.AreEqual(0f, body.Twist);
        Assert.AreEqual(0f, body.Flutter);
    }

    [Test]
    public void LetGo_ItSlidesOnIntoItsPlace_FromBehind_NeverPastIt_AndStops()
    {
        var knobs = new MotionKnobs();
        (PaperPhysics body, float x, float z) = Dragged(1f, 0f);
        (float spin, float hop) = body.Step(Dt, x, z, false, knobs, MotionAmount.Full);
        Assert.Less(body.OffsetX, 0f, "it starts behind its place, along its travel");
        Assert.GreaterOrEqual(-body.OffsetX, 0f);
        Assert.LessOrEqual(-body.OffsetX, knobs.paperSlideMax + 1e-5f, "no further than the slide's most");
        Assert.Greater(hop, 0f, "it hops once as it lands");
        float last = body.OffsetX;
        for (int i = 0; i < 120; i++)
        {
            (float s, float h) = body.Step(Dt, x, z, false, knobs, MotionAmount.Full);
            Assert.AreEqual(0f, s, "the spin comes once, on the drop");
            Assert.AreEqual(0f, h);
            Assert.GreaterOrEqual(body.OffsetX, last - 1e-6f, "it keeps moving on toward its place");
            Assert.LessOrEqual(body.OffsetX, 1e-6f, "never past it");
            last = body.OffsetX;
        }
        for (int i = 0; i < 600 && body.Moving; i++)
            body.Step(Dt, x, z, false, knobs, MotionAmount.Full); // its bow's wobble settles a little longer
        Assert.IsFalse(body.Moving, "and comes to rest");
        Assert.AreEqual(0f, body.OffsetX);
        Assert.AreEqual(0f, body.Yaw);
    }

    [Test]
    public void LetGo_ItSpinsOnceByItsSidewaysMomentum_WithinItsMost()
    {
        var knobs = new MotionKnobs();
        (PaperPhysics right, float xr, float zr) = Dragged(1.2f, 0f);
        (PaperPhysics left, float xl, float zl) = Dragged(-1.2f, 0f);
        float spinRight = right.Step(Dt, xr, zr, false, knobs, MotionAmount.Full).spin;
        float spinLeft = left.Step(Dt, xl, zl, false, knobs, MotionAmount.Full).spin;
        Assert.Greater(spinRight, 0f);
        Assert.Less(spinLeft, 0f, "the other way for the other way");
        (PaperPhysics fast, float xf, float zf) = Dragged(40f, 0f);
        Assert.AreEqual(knobs.paperDropSpinMax, fast.Step(Dt, xf, zf, false, knobs, MotionAmount.Full).spin, 1e-4f, "never more than its most");
        Assert.AreEqual(-1f, PaperPhysics.SlideStart(1f, 0f, 1f, 2f).x, 1e-6f);
        Assert.AreEqual(-0.03f, PaperPhysics.SlideStart(10f, 0f, 1f, 0.03f).x, 1e-6f, "clamped to the slide's most");
    }

    [Test]
    public void AHeavierBody_SwingsLess_AndTakesLongerToSlideToRest()
    {
        (PaperPhysics paper, _, _) = Dragged(0.6f, 0f);
        (PaperPhysics folder, _, _) = Dragged(0.6f, 0f, 2.5f);
        Assert.Less(System.Math.Abs(folder.Yaw), System.Math.Abs(paper.Yaw));
        Assert.Less(folder.Bow, paper.Bow);
        var knobs = new MotionKnobs();
        int Frames(float weight)
        {
            (PaperPhysics b, float x, float z) = Dragged(0.4f, 0f, weight);
            int n = 0;
            b.Step(Dt, x, z, false, knobs, MotionAmount.Full);
            while (System.Math.Abs(b.OffsetX) > 1e-5f && n < 1000)
            {
                b.Step(Dt, x, z, false, knobs, MotionAmount.Full);
                n++;
            }
            return n;
        }
        Assert.Greater(Frames(2.5f), Frames(1f), "the folder slides to rest slower: it has weight");
    }

    [Test]
    public void UnderReducedMotion_NothingMoves()
    {
        var reduced = new MotionAmount(1f, true);
        (PaperPhysics body, float x, float z) = Dragged(1f, 0.5f, 1f, reduced);
        Assert.AreEqual(0f, body.Yaw);
        Assert.AreEqual(0f, body.Bow);
        Assert.AreEqual(0f, body.Twist);
        Assert.AreEqual(0f, body.Flutter);
        (float spin, float hop) = body.Step(Dt, x, z, false, new MotionKnobs(), reduced);
        Assert.AreEqual(0f, spin);
        Assert.AreEqual(0f, hop);
        Assert.AreEqual(0f, body.OffsetX);
        Assert.IsFalse(body.Moving);
    }
}
