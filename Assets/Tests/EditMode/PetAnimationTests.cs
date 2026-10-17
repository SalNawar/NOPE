using NUnit.Framework;

/// <summary>The pets' frame-by-frame animation (Saleh 2026-10-17: "just make normal animations"): each step held its ticks, the loop over and over, a reaction once, a missing frame resting.</summary>
public class PetAnimationTests
{
    private static PetFrameHold S(string frame, int hold) => new PetFrameHold { frame = frame, hold = hold };

    private static readonly PetFrameHold[] Idle = { S("rest", 3), S("blink", 1), S("rest", 2), S("tail_a", 2) };

    [Test]
    public void FrameAt_HoldsEachStep_AndLoops()
    {
        string[] want = { "rest", "rest", "rest", "blink", "rest", "rest", "tail_a", "tail_a", "rest", "rest" };
        for (int tick = 0; tick < want.Length; tick++)
            Assert.AreEqual(want[tick], PetAnimation.FrameAt(Idle, tick, true), $"tick {tick}");
        Assert.AreEqual(8, PetAnimation.Length(Idle));
        Assert.AreEqual("blink", PetAnimation.FrameAt(Idle, 8 * 1000 + 3, true), "a thousand loops on");
    }

    [Test]
    public void FrameAt_AReactionPlaysOnce()
    {
        PetFrameHold[] pat = { S("blink", 2), S("tail_a", 1) };
        Assert.AreEqual("blink", PetAnimation.FrameAt(pat, 1, false));
        Assert.AreEqual("tail_a", PetAnimation.FrameAt(pat, 2, false));
        Assert.IsNull(PetAnimation.FrameAt(pat, 3, false), "then it is over");
    }

    [Test]
    public void FrameAt_EmptyOrOddSteps_Rest()
    {
        Assert.AreEqual(PetAnimation.Rest, PetAnimation.FrameAt(new PetFrameHold[0], 5, true));
        Assert.IsNull(PetAnimation.FrameAt(null, 0, false));
        Assert.AreEqual(PetAnimation.Rest, PetAnimation.FrameAt(Idle, -1, true));
        Assert.AreEqual("x", PetAnimation.FrameAt(new[] { S("x", 0) }, 0, true), "a step held 0 ticks still shows for one");
        Assert.AreEqual(PetAnimation.Rest, PetAnimation.FrameAt(new[] { S("", 2) }, 1, true), "a step with no frame rests");
    }

    [Test]
    public void Animations_FindTheLoopTheReactionAndEveryFrame()
    {
        var a = new PetAnimations
        {
            fps = 10,
            loops = new[] { new PetLoop { kind = "Dog", mood = "Idle", frames = Idle }, new PetLoop { kind = "Cat", mood = "Sick", frames = new[] { S("rest", 5), S("ear", 1) } } },
            reactions = new[] { new PetReactionLoop { reaction = "Pat", frames = new[] { S("blink", 4), S("tail_b", 2) } } }
        };
        Assert.AreSame(Idle, a.Loop(PetKind.Dog, PetLook.Idle));
        Assert.IsEmpty(a.Loop(PetKind.Dog, PetLook.Sick));
        Assert.AreEqual(2, a.Reaction(PetReaction.Pat).Length);
        Assert.IsEmpty(a.Reaction(PetReaction.Play));
        CollectionAssert.AreEqual(new[] { "rest", "blink", "tail_a", "tail_b" }, a.FrameNames(PetKind.Dog, PetLook.Idle));
        CollectionAssert.AreEqual(new[] { "rest", "ear", "blink", "tail_b" }, a.FrameNames(PetKind.Cat, PetLook.Sick));
    }
}
