using System;
using NUnit.Framework;

/// <summary>
/// RUN-SAFE's save safety rules (BUG-35a): a save this build cannot continue
/// (unreadable, or from before its format) is set aside beside the slot
/// under a name of its own, never deleted or written over; a new run sets
/// the old one aside the same way; the problem a player is told about is
/// the worst one seen.
/// </summary>
public class SaveSafetyTests
{
    private static readonly DateTime Stamp = new DateTime(2026, 10, 20, 15, 30, 5);

    [Test]
    public void ASetAsideFile_KeepsItsNameAndExtension_WithTheReasonAndTheTime()
    {
        Assert.AreEqual("nope_save.unreadable-20261020-153005.json", SaveSafety.AsideName("nope_save.json", SaveSafety.Unreadable, Stamp));
        Assert.AreEqual("nope_save.replaced-20261020-153005.json", SaveSafety.AsideName("nope_save.json", SaveSafety.Replaced, Stamp));
    }

    [Test]
    public void ASecondSetAside_InTheSameSecond_GetsItsOwnName()
    {
        Assert.AreEqual("nope_save.unreadable-20261020-153005-2.json", SaveSafety.AsideName("nope_save.json", SaveSafety.Unreadable, Stamp, 2));
        Assert.AreNotEqual(SaveSafety.AsideName("nope_save.json", SaveSafety.Unreadable, Stamp, 1), SaveSafety.AsideName("nope_save.json", SaveSafety.Unreadable, Stamp, 2));
    }

    [Test]
    public void AFileThatCannotBeRead_OrIsTooOld_IsSetAside_AContinuableOneIsRead()
    {
        Assert.AreEqual(SaveProblem.Unreadable, SaveSafety.Check(version: -1, minVersion: 3));
        Assert.AreEqual(SaveProblem.TooOld, SaveSafety.Check(2, 3));
        Assert.AreEqual(SaveProblem.None, SaveSafety.Check(3, 3));
        Assert.AreEqual(SaveProblem.None, SaveSafety.Check(4, 3), "a newer version loads with defaults (SaveSystem warns)");
    }

    [Test]
    public void TheReasonInTheName_FollowsTheProblem()
    {
        Assert.AreEqual(SaveSafety.Unreadable, SaveSafety.ReasonOf(SaveProblem.Unreadable));
        Assert.AreEqual(SaveSafety.TooOldReason, SaveSafety.ReasonOf(SaveProblem.TooOld));
    }

    [Test]
    public void ThePlayerIsToldTheWorstProblem_AndAGoodSaveClearsOnlyAFailedWrite()
    {
        Assert.AreEqual(SaveProblem.WriteFailed, SaveSafety.Worse(SaveProblem.Recovered, SaveProblem.WriteFailed));
        Assert.AreEqual(SaveProblem.Unreadable, SaveSafety.Worse(SaveProblem.Unreadable, SaveProblem.Recovered));
        Assert.AreEqual(SaveProblem.TooOld, SaveSafety.Worse(SaveProblem.None, SaveProblem.TooOld));
        Assert.AreEqual(SaveProblem.None, SaveSafety.AfterGoodSave(SaveProblem.WriteFailed), "the write works again: the notice goes");
        Assert.AreEqual(SaveProblem.Unreadable, SaveSafety.AfterGoodSave(SaveProblem.Unreadable), "a set-aside file stays reported");
    }
}
