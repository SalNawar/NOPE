using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// CAM's rig verdicts that need no Unity (CamOps; docs/pending/CAM.md,
/// rounds 4 and 5): a private collection is counted whatever its type (a
/// HashSet is no non-generic ICollection, so the chip read's "came back out"
/// wait never saw the reader's set); and a chrome leg's whole report, from
/// what its camchrome steps show (ShownBy) and what its watch counted
/// (LegLines): a plate the leg showed that was never drawn is CAM's red
/// line, never silence, a drawn plate is judged under its owner outside
/// CAM's chrome gate, and a half of the leg that was never seen (nothing
/// held, the reading view never at rest) fails CAM's line. Pure; in CAM's
/// own Editor folder (Assembly-CSharp-Editor, the editor runner's).
/// </summary>
public class CamRigTests
{
    private static readonly string[] None = Array.Empty<string>();

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
    public void ShownBy_ReadsTheLegsTarget_AndTheStampHintStep_AndRefusesAnUnknownKey()
    {
        CollectionAssert.AreEqual(new[] { "guide", "reading" }, CamOps.ShownBy(1, "guide, reading"), "the day-1 leg");
        CollectionAssert.AreEqual(new[] { CamOps.ShowsHeld, CamOps.ShowsReading }, CamOps.ShownBy(1, " held,reading "), "a day-12 leg");
        CollectionAssert.AreEqual(new[] { "stamphint" }, CamOps.ShownBy(2, null), "camchrome n = 2 shows the stamp hint, as the counter's bounce does");
        CollectionAssert.IsEmpty(CamOps.ShownBy(1, string.Empty));
        CollectionAssert.IsEmpty(CamOps.ShownBy(1, null));
        Assert.Throws<InvalidOperationException>(() => CamOps.ShownBy(1, "loupe"), "a typo must not drop the expectation silently");
        Assert.Throws<InvalidOperationException>(() => CamOps.ShownBy(3, null), "no such step");
        Assert.Throws<InvalidOperationException>(() => CamOps.ShownBy(0, null), "n = 0 ends a leg and shows nothing");
    }

    [Test]
    public void LegLines_ALegWhoseStampHintStepShowedThePlate_ButNeverDrew_ReportsARedCamLine()
    {
        // A day-12 leg: n = 1 "held, reading", then n = 2 (the stamp hint); the hint is found but drawn 0 frames.
        HashSet<string> shows = Shows((1, "held, reading"), (2, null));
        List<CamOps.ChromeLine> lines = CamOps.LegLines("a1", shows, Clean(), new[] { Seen("guide", 0, 0), Seen("stamphint", 0, 0) });

        CamOps.ChromeLine hint = Line(lines, "chrome.a1.stamphint");
        Assert.IsFalse(hint.Pass, "silence would let the stamp hint's gate pass by absence");
        Assert.AreEqual("CAM", hint.Owner, "nothing was measured: the rig's line, not the plate owner's");
        StringAssert.StartsWith("not measured", hint.Measured);
        StringAssert.Contains("shown 0 frames", hint.Measured);
        StringAssert.Contains("camchrome n = 2", hint.Detail);
        Assert.IsFalse(lines.Any(l => l.Id == "plate.a1.stamphint"), "no owner's verdict on a plate never drawn");
        Assert.IsFalse(lines.Any(l => l.Id.EndsWith(".guide", StringComparison.Ordinal)), "the guide plate was neither drawn nor shown: no line");
        Assert.IsTrue(Line(lines, "chrome.a1").Pass, "CAM's own chrome line stands apart");

        CamOps.ChromeLine missing = Line(CamOps.LegLines("a1", shows, Clean(), new[] { Seen("guide", 0, 0) }), "chrome.a1.stamphint");
        Assert.IsFalse(missing.Pass);
        Assert.AreEqual("CAM", missing.Owner);
        StringAssert.Contains("not found", missing.Measured, "a plate the watch could not find is not measured either");
    }

    [Test]
    public void LegLines_TheDay1LegsGuide_NeverDrawn_IsARedCamLine()
    {
        List<CamOps.ChromeLine> lines = CamOps.LegLines("a1_d1", Shows((1, "guide, reading")), Clean(holding: 0), new[] { Seen("guide", 0, 0), Seen("stamphint", 0, 0) });
        CamOps.ChromeLine guide = Line(lines, "chrome.a1_d1.guide");
        Assert.IsFalse(guide.Pass);
        Assert.AreEqual("CAM", guide.Owner);
        StringAssert.Contains("camchrome n = 1's target", guide.Detail);
        Assert.IsTrue(Line(lines, "chrome.a1_d1").Pass, "the day-1 leg shows no held thing, so 0 holding frames is not a gap");
    }

    [Test]
    public void LegLines_ADrawnPlate_IsJudgedUnderItsOwner_OutsideCamsChromeLines()
    {
        var notes = new[] { "frame 12: StampBar/Hint over the reading view's work" };
        List<CamOps.ChromeLine> lines = CamOps.LegLines("a1", Shows((1, "held, reading"), (2, null)), Clean(), new[] { Seen("guide", 40, 0), new CamOps.PlateSeen("stamphint", 40, 12, notes) });

        CamOps.ChromeLine hint = Line(lines, "plate.a1.stamphint");
        Assert.IsFalse(hint.Pass, "12 frames over the work");
        Assert.AreEqual("STAMP", hint.Owner);
        StringAssert.Contains("shown 40 frames, 12 over the work", hint.Measured);
        StringAssert.Contains(notes[0], hint.Detail);

        CamOps.ChromeLine guide = Line(lines, "plate.a1.guide");
        Assert.IsTrue(guide.Pass, "a plate drawn in a leg that did not show it is still judged");
        Assert.AreEqual("CONTENT", guide.Owner);

        Assert.IsTrue(lines.All(l => !l.Id.StartsWith("chrome.a1.", StringComparison.Ordinal)), "both plates were measured: no CAM plate line");
        CamOps.ChromeLine chrome = Line(lines, "chrome.a1");
        Assert.IsTrue(chrome.Pass, "the plates' lines are their owners', so CAM's chrome line stays green while they are red");
        StringAssert.Contains("plates: GuidePrompt/Plate shown 40 frames, StampBar/Hint shown 40 frames", chrome.Measured);
    }

    [Test]
    public void LegLines_CamsChromeLine_FailsOnChromeOverTheWork_AndOnAHalfTheLegNeverSaw()
    {
        HashSet<string> day12 = Shows((1, "held, reading"));
        Assert.IsTrue(Line(CamOps.LegLines("a1", day12, Clean(), null), "chrome.a1").Pass);

        CamOps.ChromeTally over = Clean();
        over.Over = 3;
        over.Violations = new[] { "frame 9: PcTab over the held Decor_form_sorter" };
        CamOps.ChromeLine covered = Line(CamOps.LegLines("a1", day12, over, null), "chrome.a1");
        Assert.IsFalse(covered.Pass);
        StringAssert.Contains("PcTab over the held", covered.Detail);

        CamOps.ChromeLine heldNothing = Line(CamOps.LegLines("a1", day12, Clean(holding: 0), null), "chrome.a1");
        Assert.IsFalse(heldNothing.Pass, "a day-12 leg whose drag held nothing tested only half of BUG-59");
        StringAssert.Contains("held nothing", heldNothing.Detail);

        CamOps.ChromeLine neverRested = Line(CamOps.LegLines("a1", day12, Clean(reading: 0), null), "chrome.a1");
        Assert.IsFalse(neverRested.Pass);
        StringAssert.Contains("reading view never came to rest", neverRested.Detail);

        CamOps.ChromeLine unwatched = Line(CamOps.LegLines("a1", new HashSet<string>(), Clean(frames: 0, holding: 0, reading: 0), null), "chrome.a1");
        Assert.IsFalse(unwatched.Pass, "a leg that watched no frame measured nothing");
        Assert.AreEqual("CAM", unwatched.Owner);

        CamOps.ChromeTally noTab = Clean();
        noTab.Missing = "StampBar/Tab";
        StringAssert.Contains("not found: StampBar/Tab", Line(CamOps.LegLines("a1", day12, noTab, null), "chrome.a1").Measured);
    }

    /// <summary>What a leg shows after its camchrome steps (n, target), gathered as the op gathers it.</summary>
    private static HashSet<string> Shows(params (int n, string target)[] steps)
    {
        var shows = new HashSet<string>();
        foreach ((int n, string target) in steps)
            shows.UnionWith(CamOps.ShownBy(n, target));
        return shows;
    }

    /// <summary>A clean tally: frames watched, some holding, some in the reading view at rest, nothing over the work.</summary>
    private static CamOps.ChromeTally Clean(int frames = 120, int holding = 30, int reading = 50) =>
        new CamOps.ChromeTally { Frames = frames, HoldingFrames = holding, ReadingFrames = reading, Over = 0, Missing = string.Empty, ScreenSize = "1920x1080", Violations = None };

    private static CamOps.PlateSeen Seen(string key, int shown, int over) => new CamOps.PlateSeen(key, shown, over, None);

    private static CamOps.ChromeLine Line(List<CamOps.ChromeLine> lines, string id)
    {
        List<CamOps.ChromeLine> found = lines.Where(l => l.Id == id).ToList();
        Assert.AreEqual(1, found.Count, $"exactly one line {id}; the lines: {string.Join(", ", lines.Select(l => l.Id))}");
        return found[0];
    }
}
