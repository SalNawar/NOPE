using System.Collections.Generic;
using NUnit.Framework;

/// <summary>Where a dater lands (StampTarget, BUG-40a): the top-most thing under its aim, a folder's edge passed over for the paper under it, the stroke stopping on the surface it meets, and the ink a stroke owes (StampInk).</summary>
public sealed class StampTargetTests
{
    private const float Band = 0.02f;

    private static StampCandidate Doc(float distance) => new StampCandidate(distance, StampTargetKind.Document, 0.05f);

    private static StampCandidate Folder(float distance, float edge) => new StampCandidate(distance, StampTargetKind.Folder, edge);

    [Test]
    public void Pick_TakesTheTopMost_TheNearestAlongTheView()
    {
        var under = new List<StampCandidate> { Doc(1.2f), Doc(1.1f), Doc(1.3f) };
        Assert.AreEqual(1, StampTarget.Pick(under, Band), "the paper the player sees on top of the stack, whatever the list's order");
    }

    [Test]
    public void Pick_PassesOverAFolderEdge_ForThePaperUnderIt()
    {
        // Day 12 (BUG-40): the folder lies over the passport's corner; the dater aimed at the passport meets the folder's edge first.
        var under = new List<StampCandidate> { Folder(1.0f, 0.004f), Doc(1.01f) };
        Assert.AreEqual(1, StampTarget.Pick(under, Band), "a folder's edge does not swallow the stamp aimed at the paper under it");
    }

    [Test]
    public void Pick_TakesTheFolder_WhenAimedWellInsideIt()
    {
        var under = new List<StampCandidate> { Folder(1.0f, 0.06f), Doc(1.01f) };
        Assert.AreEqual(0, StampTarget.Pick(under, Band), "aimed at the folder's middle, the folder is what the stamp meets (it is refused there)");
    }

    [Test]
    public void Pick_TakesAFolderEdge_WithNothingUnderIt()
    {
        Assert.AreEqual(0, StampTarget.Pick(new List<StampCandidate> { Folder(1.0f, 0.004f) }, Band), "with no paper under the edge, the edge is what the stamp meets");
        var under = new List<StampCandidate> { Folder(1.0f, 0.004f), Doc(0.9f) };
        Assert.AreEqual(1, StampTarget.Pick(under, Band), "a paper lying on the folder's edge is on top: it takes the stamp");
    }

    [Test]
    public void Pick_OverTheBareDesk_IsNothing()
    {
        Assert.AreEqual(-1, StampTarget.Pick(new List<StampCandidate>(), Band));
        Assert.AreEqual(-1, StampTarget.Pick(null, Band));
    }

    [Test]
    public void StrokeDrop_StopsTheDieOnTheSurfaceItMeets()
    {
        const float die = 0.0300f;
        foreach (float surface in new[] { 0.0004f, 0.0042f, 0.0080f })
        {
            float drop = StampTarget.StrokeDrop(die, surface);
            Assert.AreEqual(surface, die - drop, 0.0005f, "the die's foot ends within 0.5 mm of the surface: no sink into a stack or the folder");
        }
        Assert.AreEqual(0f, StampTarget.StrokeDrop(0.002f, 0.004f), "a die already at or under the surface does not drop");
    }

    [Test]
    public void StampInk_AnOwedPrintLandsExactlyOnce_EvenWhenTheStrokeIsCut()
    {
        var ink = new StampInk();
        Assert.IsFalse(ink.Land(), "nothing owed, nothing lands (a press over the bare desk)");
        ink.Owe();
        Assert.IsTrue(ink.Owed);
        Assert.IsTrue(ink.Land(), "the stroke bottoms out: the print lands");
        Assert.IsFalse(ink.Land(), "once");
        ink.Owe();
        Assert.IsTrue(ink.Land(), "a stroke cut before it bottoms out still prints: the ink matches the recorded verdict");
        Assert.IsFalse(ink.Owed);
    }
}
