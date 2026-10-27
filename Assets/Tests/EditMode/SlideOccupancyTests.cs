using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// The scanner's occupancy as its outside shows it (DESK-HOMES P2, "busy or
/// occupied stays out"): busy while it scans, while its lid stands open for a
/// paper coming onto the bed (a queued or Auto-Fed paper's carry, the review's
/// fault: the sweep hides while the paper slides), until the lid has shut; a
/// lid opened for a paper the player still holds near it does not hold it.
/// The paper that rides a machine home in a forced retract is the docked one.
/// </summary>
public class SlideOccupancyTests
{
    [Test]
    public void TheScan_AndAnOpenLidWithNoPaperHeld_HoldTheScanner_UntilTheLidShuts()
    {
        var scanner = new SlideOccupancy();
        Assert.IsFalse(scanner.Step(false, false, false), "idle and shut");
        Assert.IsTrue(scanner.Step(true, false, false), "a scan runs (the lid shut over it)");
        Assert.IsTrue(scanner.Step(false, true, false), "the scan ended, the lid opens: the next queued paper may follow");
        Assert.IsTrue(scanner.Step(false, true, false), "a queued paper carried onto the bed: the sweep hides while it slides, the lid stays open");
        Assert.IsTrue(scanner.Step(true, false, false), "its scan");
        Assert.IsTrue(scanner.Step(false, true, true), "the player picks up a paper meanwhile: it stays held");
        Assert.IsFalse(scanner.Step(false, false, true), "the lid shut: empty");
    }

    [Test]
    public void TheRider_IsThePaperLyingStillNearestTheDockPoint_NeverOneBesideTheMachine()
    {
        // The review: the first still paper in the drop area rode, so a paper lying beside the reader could ride while the docked one stayed.
        var papers = new List<(float distance, bool inside, bool still)>
        {
            (0.06f, true, true),
            (0.004f, true, true),
            (0.001f, true, false),
            (0f, false, true),
        };
        Assert.AreEqual(1, SlideOccupancy.Rider(papers), "the docked one: nearest the pad, lying still inside");
        papers[1] = (0.004f, true, false);
        Assert.AreEqual(0, SlideOccupancy.Rider(papers), "the docked one on its own carry has that as its only mover");
        Assert.AreEqual(-1, SlideOccupancy.Rider(new List<(float, bool, bool)> { (0f, false, true), (0.01f, true, false) }), "none lies still inside");
        Assert.AreEqual(-1, SlideOccupancy.Rider(null));
    }

    [Test]
    public void ALidOpenedForAPaperHeldNearIt_DoesNotHoldIt_ItsDropDoes()
    {
        var scanner = new SlideOccupancy();
        Assert.IsFalse(scanner.Step(false, true, true), "a paper dragged near: the slide still comes to help the drop");
        Assert.IsTrue(scanner.Step(false, true, false), "let go on it: the lid stays open while it lands");
        Assert.IsTrue(scanner.Step(true, false, false));
        Assert.IsFalse(scanner.Step(false, false, false));
    }
}
