using NUnit.Framework;

/// <summary>
/// The desk's physics feel (Saleh 2026-10-17: "KEEP IT TOP DOWN BUT GIVE IT
/// PHYSICS"; 1018a: "all documents are stiff"): PaperPhysics swings a held
/// body against its travel and bows it along it by its speed, twists its
/// corners as it swings and ripples it on a fast move. Let go, it stays
/// exactly where it was let go (DOC-HANDLE, BUG-6 and BUG-11: no slide on, no
/// spin; the old slide-on test pinned the back-slide bug and is replaced
/// here), hops and settles its bow through a wobble to flat, as much as it was
/// moving (a slow drop barely settles). A rigid card never bends (Saleh's D3);
/// a refusal shakes in place; the swing pivots on the held point; a heavy body
/// swings less; nothing moves under Reduced Motion.
/// </summary>
public class PaperPhysicsTests
{
    private const float Dt = 1f / 60f;

    /// <summary>Holds a body and drags it at (vx, vz) metres per second for a quarter second; returns it and its place.</summary>
    private static (PaperPhysics body, float x, float z) Dragged(float vx, float vz, float weight = 1f, MotionAmount? amount = null, bool rigid = false)
    {
        var body = new PaperPhysics(weight) { Rigid = rigid };
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
        (PaperPhysics body, float x, float z) = Dragged(1.2f, 0.2f);
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
    public void LetGo_ItStaysExactlyWhereItWasLetGo_NoSlideNoSpin()
    {
        // Replaces the old "slides on into its place from behind" test, which pinned BUG-6's back-slide (SlideStart = -v * 0.16 s).
        var knobs = new MotionKnobs();
        (PaperPhysics body, float x, float z) = Dragged(1.5f, 0.4f);
        float hop = body.Step(Dt, x, z, false, knobs, MotionAmount.Full);
        Assert.Greater(hop, 0f, "it hops once as it lands");
        for (int i = 0; i < 600 && body.Moving; i++)
        {
            Assert.AreEqual(0f, body.Step(Dt, x, z, false, knobs, MotionAmount.Full), "the hop comes once, on the drop");
            Assert.AreEqual(0f, body.ShakeX, "nothing moves its place: no slide on, no slide back");
        }
        Assert.IsFalse(body.Moving, "and it comes to rest");
        Assert.AreEqual(0f, body.Yaw, "its swing springs back to square: it keeps no spin");
    }

    [Test]
    public void LetGo_ItSettlesAsMuchAsItWasMoving()
    {
        var knobs = new MotionKnobs();
        Assert.AreEqual(0f, PaperPhysics.SettleShare(0f, 1f), "let go still: no settle");
        Assert.AreEqual(0.1f, PaperPhysics.SettleShare(0.1f, 1f), 1e-6f, "in proportion to its speed");
        Assert.AreEqual(1f, PaperPhysics.SettleShare(3f, 1f), "all of it from the full speed");
        Assert.AreEqual(1f, PaperPhysics.SettleShare(0f, 0f), "with no full speed set, all of it");

        float Hop(float speed)
        {
            (PaperPhysics b, float x, float z) = Dragged(speed, 0f);
            return b.Step(Dt, x, z, false, knobs, MotionAmount.Full);
        }
        float slow = Hop(0.1f), fast = Hop(knobs.paperSettleSpeed * 2f);
        Assert.AreEqual(knobs.paperDropHop, fast, 1e-6f, "a fast drop hops its full hop");
        Assert.Less(slow, fast * 0.2f, "a slow drop barely hops (the settle is scaled by speed, BUG-11)");

        float Curl(float speed)
        {
            (PaperPhysics b, float x, float z) = Dragged(speed, 0f);
            float least = 0f;
            for (int i = 0; i < 600 && (i == 0 || b.Moving); i++)
            {
                b.Step(Dt, x, z, false, knobs, MotionAmount.Full);
                least = System.Math.Min(least, b.Bow);
            }
            return -least;
        }
        Assert.Less(Curl(0.1f), Curl(knobs.paperSettleSpeed * 2f), "a slow drop's ends curl up less than a fast one's");
    }

    [Test]
    public void ARigidCard_NeverBends_AndHopsAtMostItsCardHop()
    {
        var knobs = new MotionKnobs();
        (PaperPhysics card, float x, float z) = Dragged(knobs.paperFlutterFrom * 4f, 0.3f, rigid: true);
        Assert.AreEqual(0f, card.Bow, "no bow while held (Saleh's D3: ID cards stay rigid)");
        Assert.AreEqual(0f, card.Twist);
        Assert.AreEqual(0f, card.Flutter);
        float hop = card.Step(Dt, x, z, false, knobs, MotionAmount.Full);
        Assert.LessOrEqual(hop, knobs.cardDropHop + 1e-7f, "a short card hop");
        for (int i = 0; i < 300; i++)
        {
            card.Step(Dt, x, z, false, knobs, MotionAmount.Full);
            Assert.AreEqual(0f, card.Bow, "no landing curl either");
        }
        Assert.AreEqual(0f, knobs.cardFlex, "the cards' flex knob is 0 too");
        var landed = new PaperPhysics { Rigid = true };
        Assert.LessOrEqual(landed.Land(1f, knobs), knobs.cardDropHop + 1e-7f);
        Assert.AreEqual(0f, landed.Bow);
    }

    [Test]
    public void ALanding_CurlsAndHops_LikeAFullDrop()
    {
        var knobs = new MotionKnobs();
        var body = new PaperPhysics();
        body.Step(Dt, 0f, 0f, false, knobs, MotionAmount.Full);
        Assert.AreEqual(knobs.paperDropHop, body.Land(1f, knobs), 1e-6f);
        Assert.AreEqual(0f, body.Land(0f, knobs), "no landing, no hop");
        float least = 0f;
        for (int i = 0; i < 600 && (i == 0 || body.Moving); i++)
        {
            body.Step(Dt, 0f, 0f, false, knobs, MotionAmount.Full);
            least = System.Math.Min(least, body.Bow);
        }
        Assert.Less(least, 0f, "the hand-over's slap: its ends curl up as it lands");
        Assert.IsFalse(body.Moving);
    }

    [Test]
    public void ARefusal_ShakesInPlace_SideToSide_ThenLiesStill()
    {
        var knobs = new MotionKnobs();
        var body = new PaperPhysics();
        body.Step(Dt, 0f, 0f, false, knobs, MotionAmount.Full);
        body.Shake(0.004f, 0.2f, 3);
        Assert.IsTrue(body.Moving);
        float most = 0f, least = 0f;
        int flips = 0;
        float last = 0f;
        for (int i = 0; i < 60 && body.Moving; i++)
        {
            body.Step(Dt, 0f, 0f, false, knobs, MotionAmount.Full);
            most = System.Math.Max(most, body.ShakeX);
            least = System.Math.Min(least, body.ShakeX);
            if (last != 0f && body.ShakeX != 0f && System.Math.Sign(last) != System.Math.Sign(body.ShakeX))
                flips++;
            if (body.ShakeX != 0f)
                last = body.ShakeX;
            Assert.LessOrEqual(System.Math.Abs(body.ShakeX), 0.004f + 1e-7f, "within its amplitude: it shakes in place, never flies");
        }
        Assert.Greater(most, 0f);
        Assert.Less(least, 0f, "side to side");
        Assert.GreaterOrEqual(flips, 3, "a few cycles");
        Assert.IsFalse(body.Moving, "and it is over in its seconds");
        Assert.AreEqual(0f, body.ShakeX, "lying exactly where it was");
        Assert.AreEqual(0f, PaperPhysics.ShakeOffset(0.2f, 1f, 0.2f, 3), "nothing at its end");
        Assert.AreEqual(0f, PaperPhysics.ShakeOffset(0f, 1f, 0.2f, 3), "nor at its start");
        Assert.AreEqual(0f, PaperPhysics.ShakeOffset(0.1f, 1f, 0f, 3), "nor with no seconds");
    }

    [Test]
    public void ASwing_PivotsOnTheHeldPoint()
    {
        // Held by its top right corner (0.1, 0.15 from its centre), a swing of 30 degrees leaves that corner where it was.
        const float px = 0.1f, pz = 0.15f, yaw = 30f;
        (float cx, float cz) = PaperPhysics.PivotOffset(px, pz, yaw);
        double a = yaw * System.Math.PI / 180.0;
        // the held point, turned with the body about its new centre: x' = x cos + z sin, z' = -x sin + z cos
        float hx = cx + (float)(px * System.Math.Cos(a) + pz * System.Math.Sin(a));
        float hz = cz + (float)(-px * System.Math.Sin(a) + pz * System.Math.Cos(a));
        Assert.AreEqual(px, hx, 1e-5f, "the held point stays under the cursor");
        Assert.AreEqual(pz, hz, 1e-5f);
        Assert.AreEqual((0f, 0f), PaperPhysics.PivotOffset(px, pz, 0f), "no swing, no offset");
        Assert.AreEqual((0f, 0f), PaperPhysics.PivotOffset(0f, 0f, yaw), "held at its centre, it turns in place");
    }

    [Test]
    public void AHeavierBody_SwingsAndBendsLess()
    {
        (PaperPhysics paper, _, _) = Dragged(0.6f, 0f);
        (PaperPhysics folder, _, _) = Dragged(0.6f, 0f, 2.5f);
        Assert.Less(System.Math.Abs(folder.Yaw), System.Math.Abs(paper.Yaw));
        Assert.Less(folder.Bow, paper.Bow);
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
        float hop = body.Step(Dt, x, z, false, new MotionKnobs(), reduced);
        Assert.AreEqual(0f, hop);
        body.Shake(0.004f, 0.2f, 3);
        body.Step(Dt, x, z, false, new MotionKnobs(), reduced);
        Assert.AreEqual(0f, body.ShakeX, "no shake either");
        Assert.IsFalse(body.Moving);
    }
}
