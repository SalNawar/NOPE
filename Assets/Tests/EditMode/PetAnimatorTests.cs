using NUnit.Framework;

/// <summary>The pets' puppet animation (Saleh 2026-10-17: a real animation, not a squash): the rig's bones bend only their own fur, the loops and reactions pose it, and a pose is the same for the same moment.</summary>
public class PetAnimatorTests
{
    private static float[] P(float x, float y) => new[] { x, y };

    /// <summary>A sitting dog's rig: the head over the body, an ear on the head, a tail at the bottom right.</summary>
    private static PetRig Rig() => new PetRig
    {
        kind = "Dog",
        mood = "Idle",
        head = new PetBone { pivot = P(0.43f, 0.42f), center = P(0.37f, 0.22f), radius = P(0.2f, 0.2f) },
        ears = new[] { new PetBone { pivot = P(0.3f, 0.15f), center = P(0.28f, 0.08f), radius = P(0.07f, 0.09f), tip = P(0.27f, 0.02f) } },
        tail = new PetBone { pivot = P(0.73f, 0.79f), center = P(0.79f, 0.87f), radius = P(0.13f, 0.11f), tip = P(0.85f, 0.9f) },
        body = new PetBone { pivot = P(0.48f, 0.95f), center = P(0.48f, 0.62f), radius = P(0.28f, 0.34f) },
        eyes = new[] { new PetEye { center = P(0.3f, 0.25f), radius = P(0.02f, 0.03f) } }
    };

    private static PetMoodMotion Idle() => new PetMoodMotion
    {
        mood = "Idle", breathPeriod = 2.2f, breathDepth = 0.012f, headSway = 2f, headSwayPeriod = 5.5f, tailSwing = 6f, tailPeriod = 1.8f,
        earFlickEvery = 5f, earFlick = 12f, blinkEvery = 3.6f, blinkLength = 0.16f
    };

    private static PetMoodMotion Happy() => new PetMoodMotion { mood = "Happy", breathPeriod = 1.1f, breathDepth = 0.016f, tailSwing = 18f, tailPeriod = 0.42f, hopEvery = 3.2f, hopHeight = 0.035f };

    private static void Moved(PetRig rig, PetPose pose, float x, float y, out float dx, out float dy)
    {
        PetAnimator.Deform(rig, pose, x, y, out float ox, out float oy);
        dx = ox - x;
        dy = oy - y;
    }

    [Test]
    public void Deform_AtRest_LeavesEveryPointWhereItIs()
    {
        var rest = new PetPose(0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, false);
        for (float x = 0f; x <= 1f; x += 0.125f)
            for (float y = 0f; y <= 1f; y += 0.125f)
            {
                Moved(Rig(), rest, x, y, out float dx, out float dy);
                Assert.AreEqual(0f, dx, 1e-6f);
                Assert.AreEqual(0f, dy, 1e-6f);
            }
    }

    [Test]
    public void Deform_TheHeadTurnsAboutItsPivot_AndLeavesTheFeet()
    {
        PetRig rig = Rig();
        var tilt = new PetPose(0f, 10f, 0f, 0f, 0f, 0f, 0f, 0f, false);
        PetAnimator.Deform(rig, tilt, 0.37f, 0.22f, out float hx, out float hy);
        float before = Dist(0.37f, 0.22f, 0.43f, 0.42f), after = Dist(hx, hy, 0.43f, 0.42f);
        Assert.AreEqual(before, after, 1e-4f, "the head's centre keeps its distance to the neck");
        Assert.Greater(Dist(hx, hy, 0.37f, 0.22f), 0.02f, "and moves");
        Moved(rig, tilt, 0.4f, 0.93f, out float fx, out float fy);
        Assert.AreEqual(0f, fx, 1e-6f, "the feet stay put");
        Assert.AreEqual(0f, fy, 1e-6f);
    }

    [Test]
    public void Deform_TheTailBendsMoreTowardsItsTip()
    {
        PetRig rig = Rig();
        var wag = new PetPose(0f, 0f, 0f, 0f, 0f, 15f, 0f, 0f, false);
        Moved(rig, wag, 0.73f, 0.79f, out float px, out float py);
        Assert.AreEqual(0f, px, 1e-6f, "the tail's root stays on the body");
        Assert.AreEqual(0f, py, 1e-6f);
        Moved(rig, wag, 0.76f, 0.82f, out float mx, out float my);
        Moved(rig, wag, 0.82f, 0.88f, out float tx, out float ty);
        Assert.Greater(Dist(tx, ty, 0f, 0f), Dist(mx, my, 0f, 0f), "the tip swings further than the middle");
        Moved(rig, wag, 0.37f, 0.22f, out float hx, out float hy);
        Assert.AreEqual(0f, hx, 1e-6f, "the head is not the tail's");
        Assert.AreEqual(0f, hy, 1e-6f);
    }

    [Test]
    public void Deform_AnEarFlicksAlone_AndRidesOnTheHead()
    {
        PetRig rig = Rig();
        Moved(rig, new PetPose(0f, 0f, 0f, 12f, 0f, 0f, 0f, 0f, false), 0.28f, 0.06f, out float ex, out float ey);
        Assert.Greater(Dist(ex, ey, 0f, 0f), 0.01f, "the ear's tip flicks");
        Moved(rig, new PetPose(0f, 0f, 0f, 12f, 0f, 0f, 0f, 0f, false), 0.4f, 0.3f, out float fx, out float fy);
        Assert.AreEqual(0f, Dist(fx, fy, 0f, 0f), 1e-6f, "the face does not");
        Moved(rig, new PetPose(0f, 0f, 0.02f, 0f, 0f, 0f, 0f, 0f, false), 0.28f, 0.06f, out _, out float dropped);
        Assert.AreEqual(0.02f, dropped, 1e-4f, "the ear sinks with the head");
    }

    [Test]
    public void Deform_TheBreathLiftsTheChest_TheHeadRidesOnIt_TheGroundStays()
    {
        PetRig rig = Rig();
        var breath = new PetPose(0.02f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, false);
        Moved(rig, breath, 0.48f, 0.6f, out _, out float chest);
        Assert.Less(chest, 0f, "the chest rises");
        Moved(rig, breath, 0.48f, 0.95f, out _, out float ground);
        Assert.AreEqual(0f, ground, 1e-6f, "the ground under it stays");
        Moved(rig, breath, 0.37f, 0.22f, out float hx, out float hy);
        Moved(rig, breath, 0.43f, 0.42f, out float nx, out float ny);
        Assert.AreEqual(nx, hx, 1e-5f, "the head moves as its pivot does");
        Assert.AreEqual(ny, hy, 1e-5f);
    }

    [Test]
    public void Deform_TheHopLiftsEverything()
    {
        Moved(Rig(), new PetPose(0f, 0f, 0f, 0f, 0f, 0f, 0.05f, 0f, false), 0.1f, 0.9f, out float dx, out float dy);
        Assert.AreEqual(0f, dx, 1e-6f);
        Assert.AreEqual(-0.05f, dy, 1e-6f);
    }

    [Test]
    public void Deform_NoRig_NoMove()
    {
        PetAnimator.Deform(null, new PetPose(0.1f, 30f, 0.1f, 9f, 9f, 9f, 0.1f, 0.1f, true), 0.3f, 0.7f, out float x, out float y);
        Assert.AreEqual(0.3f, x);
        Assert.AreEqual(0.7f, y);
    }

    [Test]
    public void Pose_TheSameMoment_TheSamePose()
    {
        PetPose a = PetAnimator.Pose(Idle(), null, -1f, 12.34f, 7), b = PetAnimator.Pose(Idle(), null, -1f, 12.34f, 7);
        Assert.AreEqual(a.Breath, b.Breath);
        Assert.AreEqual(a.HeadAngle, b.HeadAngle);
        Assert.AreEqual(a.TailAngle, b.TailAngle);
        Assert.AreEqual(a.Blink, b.Blink);
    }

    [Test]
    public void Pose_IdleBreathesWagsAndBlinks_AboutAsOftenAsItsKnobsSay_ButNeverHops()
    {
        PetMoodMotion idle = Idle();
        int shut = 0, frames = 0, blinks = 0, flicks = 0;
        bool was = false;
        float maxBreath = 0f, maxTail = 0f;
        for (float t = 0f; t < 72f; t += 0.01f, frames++)
        {
            PetPose p = PetAnimator.Pose(idle, null, -1f, t, 3);
            if (p.Blink) shut++;
            if (p.Blink && !was) blinks++;
            was = p.Blink;
            if (p.Ear0 != 0f || p.Ear1 != 0f) flicks++;
            maxBreath = System.Math.Max(maxBreath, System.Math.Abs(p.Breath));
            maxTail = System.Math.Max(maxTail, System.Math.Abs(p.TailAngle));
            Assert.AreEqual(0f, p.Hop, "an idle pet never hops");
        }
        Assert.AreEqual(20, blinks, "one blink in each 3.6 s window");
        Assert.AreEqual(idle.blinkLength / idle.blinkEvery, shut / (float)frames, 0.01f, "each as long as its knob");
        Assert.Greater(flicks, 0, "the ears flick");
        Assert.AreEqual(idle.breathDepth, maxBreath, 0.0005f);
        Assert.AreEqual(idle.tailSwing, maxTail, 0.05f);
    }

    [Test]
    public void Pose_HappyHops_AndAMoodWithoutBlinksNeverShutsItsEyes()
    {
        PetMoodMotion happy = Happy();
        float highest = 0f;
        for (float t = 0f; t < 30f; t += 0.01f)
        {
            PetPose p = PetAnimator.Pose(happy, null, -1f, t, 3);
            highest = System.Math.Max(highest, p.Hop);
            Assert.IsFalse(p.Blink);
        }
        Assert.AreEqual(happy.hopHeight, highest, 0.001f);
    }

    [Test]
    public void Pose_APat_TiltsTheHeadShutsTheEyesAndWags_ThenTheLoopAgain()
    {
        var pat = new PetReactionMotion { reaction = "Pat", length = 1.3f, headTilt = 9f, tailSwing = 20f, tailPeriod = 0.32f, eyesShut = true };
        PetPose mid = PetAnimator.Pose(null, pat, 0.65f, 100f, 3);
        Assert.AreEqual(9f, mid.HeadAngle, 0.01f, "the tilt peaks halfway");
        Assert.IsTrue(mid.Blink, "eyes shut while petted");
        Assert.Greater(System.Math.Abs(PetAnimator.Pose(null, pat, 0.4f, 100f, 3).TailAngle), 1f, "the tail goes");
        PetPose after = PetAnimator.Pose(null, pat, 1.4f, 101.4f, 3);
        Assert.AreEqual(0f, after.HeadAngle);
        Assert.IsFalse(after.Blink);
    }

    [Test]
    public void Pose_FedBobsTheHeadDown_PlayHops()
    {
        var fed = new PetReactionMotion { reaction = "Fed", length = 2.2f, headTilt = -6f, headDip = 0.025f, bobs = 4 };
        float lowest = 0f;
        for (float s = 0f; s < 2.2f; s += 0.01f)
            lowest = System.Math.Max(lowest, PetAnimator.Pose(null, fed, s, s, 3).HeadDrop);
        Assert.Greater(lowest, 0.025f, "the head goes down to the bowl and bobs");
        var play = new PetReactionMotion { reaction = "Play", length = 1.4f, hops = 2, hopHeight = 0.06f };
        Assert.AreEqual(0.06f, PetAnimator.Pose(null, play, 0.35f, 0.35f, 3).Hop, 0.001f, "the first hop's top");
        Assert.AreEqual(0f, PetAnimator.Pose(null, play, 0.7f, 0.7f, 3).Hop, 0.001f, "down between hops");
    }

    [Test]
    public void Rigs_FindByKindAndLook()
    {
        var rigs = new PetRigs
        {
            rigs = new[] { Rig(), new PetRig { kind = "Cat", mood = "Sad" } },
            moods = new[] { Idle(), Happy() },
            reactions = new[] { new PetReactionMotion { reaction = "Fed", length = 2f } }
        };
        Assert.AreSame(rigs.rigs[0], rigs.Rig(PetKind.Dog, PetLook.Idle));
        Assert.AreSame(rigs.rigs[1], rigs.Rig(PetKind.Cat, PetLook.Sad));
        Assert.IsNull(rigs.Rig(PetKind.Cat, PetLook.Idle));
        Assert.AreEqual("Happy", rigs.Mood(PetLook.Happy).mood);
        Assert.IsNull(rigs.Mood(PetLook.Sick));
        Assert.AreEqual(2f, rigs.Reaction(PetReaction.Fed).length);
        Assert.IsNull(rigs.Reaction(PetReaction.Pat));
        Assert.IsTrue(Rig().CanBlink);
        Assert.IsFalse(new PetRig().CanBlink);
        Assert.IsFalse(new PetBone().Present, "a rig's missing tail");
    }

    private static float Dist(float ax, float ay, float bx, float by) => (float)System.Math.Sqrt((ax - bx) * (ax - bx) + (ay - by) * (ay - by));
}
