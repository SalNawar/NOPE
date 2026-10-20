using System;
using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// CAM's rig verdicts that need no Unity (CamOps; docs/pending/CAM.md, round
/// 4): a private collection is counted whatever its type (a HashSet is no
/// non-generic ICollection, so the chip read's "came back out" wait never saw
/// the reader's set), and each overlay plate's chrome line, where a plate the
/// leg showed but that was never drawn is a red line, never silence. Pure.
/// </summary>
public class CamRigTests
{
    [Test]
    public void Count_CountsAHashSet_AsWellAsAList()
    {
        Assert.AreEqual(2, CamOps.Count(new HashSet<int> { 3, 7 }), "DeskController._inReader is a HashSet<int>");
        Assert.AreEqual(3, CamOps.Count(new List<string> { "a", "b", "c" }), "TravellerWheel._said is a List");
        Assert.AreEqual(0, CamOps.Count(new HashSet<int>()));
        Assert.AreEqual(0, CamOps.Count(null));
        Assert.AreEqual(0, CamOps.Count(4), "not a collection");
    }

    [Test]
    public void PlateLine_ADrawnPlate_IsJudgedUnderItsOwner()
    {
        CamOps.PlateLine over = CamOps.PlateLine.Of("StampBar/Hint", "STAMP", true, true, 40, 12);
        Assert.IsTrue(over.Reported);
        Assert.IsFalse(over.Pass, "12 frames over the work");
        Assert.AreEqual("STAMP", over.Owner);
        StringAssert.Contains("shown 40 frames, 12 over the work", over.Measured);

        CamOps.PlateLine clear = CamOps.PlateLine.Of("GuidePrompt/Plate", "CONTENT", false, true, 40, 0);
        Assert.IsTrue(clear.Reported, "a plate drawn in a leg that did not ask for it is still judged");
        Assert.IsTrue(clear.Pass);
        Assert.AreEqual("CONTENT", clear.Owner);
    }

    [Test]
    public void PlateLine_APlateTheLegShowed_ButNeverDrawn_IsARedNotMeasuredLineUnderCam()
    {
        CamOps.PlateLine hidden = CamOps.PlateLine.Of("StampBar/Hint", "STAMP", true, true, 0, 0);
        Assert.IsTrue(hidden.Reported, "silence would let cam.play.chrome pass by absence");
        Assert.IsFalse(hidden.Pass);
        Assert.AreEqual("CAM", hidden.Owner, "nothing was measured: the rig's line, not the plate owner's");
        StringAssert.StartsWith("not measured", hidden.Measured);
        StringAssert.Contains("shown 0 frames", hidden.Measured);

        CamOps.PlateLine missing = CamOps.PlateLine.Of("GuidePrompt/Plate", "CONTENT", true, false, 0, 0);
        Assert.IsTrue(missing.Reported);
        Assert.IsFalse(missing.Pass);
        Assert.AreEqual("CAM", missing.Owner);
        StringAssert.Contains("not found", missing.Measured);
    }

    [Test]
    public void PlateLine_APlateNeitherDrawnNorShownByTheLeg_ReportsNothing()
    {
        Assert.IsFalse(CamOps.PlateLine.Of("GuidePrompt/Plate", "CONTENT", false, true, 0, 0).Reported);
        Assert.IsFalse(CamOps.PlateLine.Of("GuidePrompt/Plate", "CONTENT", false, false, 0, 0).Reported);
    }

    [Test]
    public void ExpectedPlates_ReadsTheStepsTarget_AndRefusesAnUnknownPlate()
    {
        CollectionAssert.AreEqual(new[] { "guide" }, CamOps.ExpectedPlates("guide"));
        CollectionAssert.AreEqual(new[] { "guide", "stamphint" }, CamOps.ExpectedPlates(" guide, stamphint "));
        CollectionAssert.IsEmpty(CamOps.ExpectedPlates(string.Empty));
        CollectionAssert.IsEmpty(CamOps.ExpectedPlates(null));
        Assert.Throws<InvalidOperationException>(() => CamOps.ExpectedPlates("loupe"), "a typo must not drop the expectation silently");
    }
}
