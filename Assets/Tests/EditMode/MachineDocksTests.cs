using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// Docking papers into the desk machines (Saleh's 1018a playtest: "when you
/// move a document to a machine it should move to the correct elevation and
/// orintation - if is the wrong document it shouldnt go through the
/// machience"): each machine takes only its documents (MachineIntake), and a
/// paper's way to, into, out of or past a machine never passes through its
/// casing (DockPath). DOC-HANDLE (BUG-24): a passport fits the chip reader's
/// pad (DockFit); the dock state is each paper's, reset with the case, and no
/// queue takes a paper out of a machine (MachineOccupancy).
/// </summary>
public class MachineDocksTests
{
    private static readonly MachineFeed Passport = new MachineFeed(true, true, false);
    private static readonly MachineFeed TransponderCard = new MachineFeed(true, true, true);
    private static readonly MachineFeed Ticket = new MachineFeed(true, false, false);
    private static readonly MachineFeed PlainCard = new MachineFeed(true, false, true);

    // a machine 0.3 wide (x) and 0.2 deep (z) at the origin, its top 0.1 up; its front faces -z
    private static readonly MachineBox Box = new MachineBox(0f, 0f, 1f, 0f, 0.15f, 0.1f, 0.1f);
    private static readonly IReadOnlyList<MachineBox> Boxes = new[] { Box };
    private const float Grow = 0.05f, Clear = 0.02f;

    [Test]
    public void TheScanner_TakesAnyCasePaperOnItsBed_AndRefusesTheFolder()
    {
        foreach (MachineFeed paper in new[] { Passport, TransponderCard, Ticket, PlainCard })
            Assert.AreEqual(MachineDock.Bed, MachineIntake.Dock(DeskMachine.Scanner, paper));
        Assert.AreEqual(MachineDock.Refused, MachineIntake.Dock(DeskMachine.Scanner, MachineFeed.Folder));
        Assert.IsFalse(MachineIntake.Accepts(DeskMachine.Scanner, MachineFeed.Folder));
    }

    [Test]
    public void TheChipReader_TakesAChipCardInItsSlot_AndAPassportOnItsPad_AndRefusesPapersWithNoChip()
    {
        Assert.AreEqual(MachineDock.Slot, MachineIntake.Dock(DeskMachine.ChipReader, TransponderCard));
        Assert.AreEqual(MachineDock.Pad, MachineIntake.Dock(DeskMachine.ChipReader, Passport));
        Assert.AreEqual(MachineDock.Refused, MachineIntake.Dock(DeskMachine.ChipReader, Ticket), "a sheet with no chip bounces back");
        Assert.AreEqual(MachineDock.Refused, MachineIntake.Dock(DeskMachine.ChipReader, PlainCard), "a card with no chip too");
        Assert.AreEqual(MachineDock.Refused, MachineIntake.Dock(DeskMachine.ChipReader, MachineFeed.Folder));
    }

    /// <summary>Every point of <paramref name="way"/> (sampled finely) over the footprint grown by Grow is at least the top plus the clearance, but for the given share at its end (a dock).</summary>
    private static void NeverThroughTheCasing(List<(float x, float y, float z)> way, float exemptFromShare = 2f)
    {
        for (int i = 0; i <= 400; i++)
        {
            float share = i / 400f;
            if (share >= exemptFromShare)
                break;
            (float x, float y, float z) = DockPath.At(way, share);
            if (Box.Over(x, z, Grow))
                Assert.GreaterOrEqual(y, Box.Top + Clear - 1e-4f, $"at {share:0.000} the paper is inside the casing ({x:0.000}, {y:0.000}, {z:0.000})");
        }
    }

    [Test]
    public void AWayAcrossAMachine_RisesOverIt_AndComesBackDownBeyondIt()
    {
        var way = DockPath.Over((-0.6f, 0f, 0.05f), (0.6f, 0f, -0.02f), Boxes, Grow, Clear);
        Assert.Greater(way.Count, 2, "it does not go straight through");
        Assert.AreEqual(0f, way[way.Count - 1].y, "it ends on the desk");
        NeverThroughTheCasing(way);
        var clear = DockPath.Over((-0.6f, 0f, 0.6f), (0.6f, 0f, 0.6f), Boxes, Grow, Clear);
        Assert.AreEqual(2, clear.Count, "a way that never comes over the machine goes straight");
    }

    [Test]
    public void AWayOntoTheBed_ComesDownFromStraightAbove()
    {
        (float x, float y, float z) bed = (0.02f, 0.07f, 0.01f);
        var way = DockPath.OnTo((-0.5f, 0.03f, -0.3f), bed, Boxes, Grow, Clear);
        Assert.AreEqual(bed, way[way.Count - 1], "it ends on the bed, at the bed's height");
        (float x, float y, float z) above = way[way.Count - 2];
        Assert.AreEqual(bed.x, above.x, 1e-5f);
        Assert.AreEqual(bed.z, above.z, 1e-5f);
        Assert.GreaterOrEqual(above.y, Box.Top + Clear - 1e-5f, "straight down from over the casing");
        float lastLeg = 1f - (above.y - bed.y) / DockPath.Length(way);
        NeverThroughTheCasing(way, lastLeg - 1e-3f);
        // dropped right over it (held up at its drag's lift), it rises first, never cutting through the casing
        var held = DockPath.OnTo((0.1f, 0.02f, 0.05f), bed, Boxes, Grow, Clear);
        Assert.AreEqual(0.1f, held[1].x, 1e-5f);
        Assert.GreaterOrEqual(held[1].y, Box.Top + Clear - 1e-5f, "it rises where it is before it moves over");
    }

    [Test]
    public void AWayIntoTheSlot_ComesDownInFrontOfTheMouth_AtItsHeight_ThenSlidesIn()
    {
        (float x, float y, float z) mouth = (0f, 0.02f, -0.1f); // in the front face, 2 cm up; in along +z (the paper held over the machine: the carry keeps it over the top)
        const float half = 0.1f, depth = 0.08f;
        var way = DockPath.Into((0.05f, 0.13f, 0.02f), mouth, 0f, 1f, half, depth, Box, Boxes, Grow, Clear);
        (float x, float y, float z) inside = way[way.Count - 1], front = way[way.Count - 2];
        Assert.AreEqual(mouth.y, front.y, 1e-5f, "at the slot's height");
        Assert.AreEqual(mouth.y, inside.y, 1e-5f, "it slides in level");
        Assert.AreEqual(mouth.x, inside.x, 1e-5f, "straight along the slot");
        Assert.AreEqual(mouth.z + depth - half, inside.z, 1e-5f, "its leading edge the slot's depth in, the rest out of the mouth");
        Assert.IsFalse(Box.Over(front.x, front.z, Grow), "it comes down clear of the casing");
        float stroke = (inside.z - front.z) / DockPath.Length(way);
        NeverThroughTheCasing(way, 1f - stroke - 1e-3f);
        var back = DockPath.OutOf(mouth, 0f, 1f, half, depth, Box, (0.5f, 0f, 0.3f), Boxes, Grow, Clear);
        Assert.AreEqual(inside, back[0], "out the way it went in");
        Assert.AreEqual(front, back[1]);
        Assert.AreEqual((0.5f, 0f, 0.3f), back[back.Count - 1]);
    }

    [Test]
    public void APassport_IsSizedToFitThePad()
    {
        // The pad 0.12 wide and 0.092 deep; a portrait passport 0.144 by 0.218 at scale 1, its normal docked scale 0.6.
        float s = DockFit.Scale(0.144f, 0.218f, 0.12f, 0.092f, 0.6f, 0.08f);
        Assert.LessOrEqual(0.218f * s, 0.092f * 1.08f + 1e-6f, "no deeper than the pad (and its overhang)");
        Assert.LessOrEqual(0.144f * s, 0.12f * 1.08f + 1e-6f, "no wider");
        Assert.Less(s, 0.6f, "smaller than its normal docked size: it would overhang the casing");
        Assert.AreEqual(0.3f, DockFit.Scale(0.1f, 0.1f, 0.5f, 0.5f, 0.3f, 0f), 1e-6f, "a paper that fits keeps its normal docked size");
        Assert.AreEqual(0.6f, DockFit.Scale(0.144f, 0.218f, 0f, 0.092f, 0.6f, 0.08f), "a pad of no size changes nothing");
    }

    [Test]
    public void TheDockState_IsEachPapers_AndResetsWithTheCase()
    {
        var docks = new MachineOccupancy();
        Assert.IsTrue(docks.Dock(0, DeskMachine.ChipReader));
        Assert.IsTrue(docks.IsDocked(0));
        Assert.IsTrue(docks.Busy(DeskMachine.ChipReader));
        Assert.IsFalse(docks.IsDocked(1), "paper 1 is not paper 0");
        Assert.IsFalse(docks.Dock(1, DeskMachine.ChipReader), "the reader holds one paper at a time");
        Assert.IsTrue(docks.Dock(1, DeskMachine.Scanner));
        Assert.IsFalse(docks.Dock(0, DeskMachine.Scanner), "a paper lies in one machine");
        Assert.IsTrue(docks.Busy(DeskMachine.Scanner));
        Assert.IsTrue(docks.Leave(1));
        Assert.IsFalse(docks.Busy(DeskMachine.Scanner));
        Assert.IsFalse(docks.Leave(1), "it already left");

        docks.Clear(); // the case ended mid-read
        Assert.IsFalse(docks.IsDocked(0), "the next case's paper 0 is not taken for docked (it lay at desk height and clipped)");
        Assert.IsFalse(docks.Busy(DeskMachine.ChipReader));
    }

    [Test]
    public void TheAutoFeedChain_DocksEachPaper_WhileTheLastIsCarriedOut()
    {
        // BUG-24 verifier finding 4: the scan of paper 0 ends and its carry-out starts (DeskController.CarryOut hands it off);
        // the next frame the Auto-Feed puts paper 1 on the bed (StartScan) before paper 0's carry-out has landed.
        var docs = new[]
        {
            new CaseDocument { name = "Passport", handOver = DocumentHandOver.OnArrival },
            new CaseDocument { name = "Permit", handOver = DocumentHandOver.OnArrival },
        };
        var papers = new DeskPapers(docs, 0.5f, 0.5f, new ScannerDay(true, false));
        papers.HandOver(0);
        papers.HandOver(1);
        var docks = new MachineOccupancy();
        papers.FeedStack();
        Assert.AreEqual(0, papers.FeedNext(docks.FreeToFeed));
        Assert.IsTrue(docks.Dock(0, DeskMachine.Scanner), "paper 0 on the bed");
        Assert.AreEqual(0, papers.Tick(1f), "its scan ends");

        Assert.IsTrue(docks.HandOff(0), "its carry-out starts: handed off");
        Assert.IsTrue(docks.IsDocked(0), "still the scanner's until it lands clear (its docked size, no input)");
        Assert.IsFalse(docks.FreeToFeed(0), "no queue takes it on its way out");
        Assert.IsFalse(docks.Busy(DeskMachine.Scanner), "but the bed is free");

        Assert.AreEqual(1, papers.FeedNext(docks.FreeToFeed), "the next frame the Auto-Feed takes paper 1");
        Assert.IsTrue(docks.Dock(1, DeskMachine.Scanner), "and paper 1 is recorded on the bed (it used to be refused and never recorded)");
        Assert.IsTrue(docks.IsDocked(1));
        Assert.IsFalse(docks.FreeToFeed(1));
        Assert.IsTrue(docks.Busy(DeskMachine.Scanner));
        Assert.IsFalse(docks.Dock(0, DeskMachine.Scanner), "a paper on its way out does not go back in");

        Assert.IsTrue(docks.Leave(0), "paper 0's carry-out lands");
        Assert.IsFalse(docks.IsDocked(0));
        Assert.IsTrue(docks.Busy(DeskMachine.Scanner), "paper 1 still fills the bed");
        Assert.IsTrue(docks.HandOff(1));
        Assert.IsTrue(docks.Leave(1), "paper 1's own carry-out lands: its Leave finds it");
        Assert.IsFalse(docks.Busy(DeskMachine.Scanner));
        Assert.IsFalse(docks.HandOff(1), "it is in no machine now");
    }

    [Test]
    public void AMachine_HoldsOnePaper_UntilItIsHandedOff()
    {
        var docks = new MachineOccupancy();
        Assert.IsTrue(docks.Dock(0, DeskMachine.Scanner));
        Assert.IsFalse(docks.Dock(1, DeskMachine.Scanner), "the bed holds one paper");
        Assert.IsFalse(docks.IsDocked(1), "a refused dock records nothing");
        docks.HandOff(0);
        docks.Clear();
        Assert.IsFalse(docks.IsDocked(0), "a case's end clears the hand-offs too");
        Assert.IsTrue(docks.Dock(0, DeskMachine.Scanner), "and the next case's paper 0 goes in");
    }

    [Test]
    public void NoQueue_TakesAPaperOutOfAMachine()
    {
        var docs = new[]
        {
            new CaseDocument { name = "Passport", handOver = DocumentHandOver.OnArrival },
            new CaseDocument { name = "Permit", handOver = DocumentHandOver.OnArrival },
        };
        var papers = new DeskPapers(docs, 0.5f, 0.5f, new ScannerDay(true, false));
        papers.HandOver(0);
        papers.HandOver(1);
        var docks = new MachineOccupancy();
        docks.Dock(0, DeskMachine.ChipReader); // the passport lies on the reader's pad, mid-read
        Assert.AreEqual(2, papers.FeedStack().Count, "the Auto-Feed takes the stack");
        int fed = papers.FeedNext(docks.FreeToFeed);
        Assert.AreEqual(1, fed, "it skips the paper in the reader and feeds the next");
        Assert.IsTrue(papers.IsQueued(0), "the passport keeps its place in the feed");
    }

    [Test]
    public void AWay_IsWalkedByLength()
    {
        var way = new List<(float x, float y, float z)> { (0f, 0f, 0f), (1f, 0f, 0f), (1f, 1f, 0f) };
        Assert.AreEqual(2f, DockPath.Length(way), 1e-6f);
        Assert.AreEqual((1f, 0f, 0f), DockPath.At(way, 0.5f));
        Assert.AreEqual((1f, 0.5f, 0f), DockPath.At(way, 0.75f));
        Assert.AreEqual((1f, 1f, 0f), DockPath.At(way, 1f));
    }
}
