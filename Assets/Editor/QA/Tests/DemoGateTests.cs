using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

/// <summary>
/// DemoGate refuses (P0-B "a DemoGate refusal test"): any red result, a
/// broken check and an empty registry refuse and are named; the known-red
/// list spares only the merge gate; result files are judged by commit and
/// player. Pure; runs outside Unity.
/// </summary>
public class DemoGateTests
{
    [Test]
    public void AllGreen_Passes()
    {
        DemoGateReport r = DemoGate.Evaluate(GateLevel.Demo, new[] { GateResult.Ok("a.one", "QA-RIG", "1") }, 1);
        Assert.IsFalse(r.Refused);
        Assert.IsEmpty(r.Reasons);
    }

    [Test]
    public void OneRedResult_Refuses_AndNamesItAndItsOwner()
    {
        DemoGateReport r = DemoGate.Evaluate(GateLevel.Merge,
            new[] { GateResult.Ok("a.one", "QA-RIG", "1"), GateResult.Fail("doc.drops", "DOC-HANDLE", "3 of 40 off by >8 px", "ClearOfBlockers push") }, 2);
        Assert.IsTrue(r.Refused);
        Assert.AreEqual(1, r.Reasons.Count);
        StringAssert.Contains("doc.drops [DOC-HANDLE]", r.Reasons[0]);
        StringAssert.Contains("ClearOfBlockers", r.Reasons[0]);
        StringAssert.Contains("REFUSED", r.ToMarkdown());
    }

    [Test]
    public void NothingRegistered_Refuses()
    {
        DemoGateReport r = DemoGate.Evaluate(GateLevel.Demo, Array.Empty<GateResult>(), 0);
        Assert.IsTrue(r.Refused);
    }

    [Test]
    public void KnownRed_SparesTheMergeGate_ButNotTheDemoGate()
    {
        var known = DemoGate.ParseKnownRed("# header\nqa.scenes.liberation*  # BUG-3, TEXT-LANG: fixed at #12\n");
        var results = new[] { GateResult.Fail("qa.scenes.liberationSans", "TEXT-LANG", "4 texts", "...") };
        DemoGateReport merge = DemoGate.Evaluate(GateLevel.Merge, results, 1, known);
        Assert.IsFalse(merge.Refused);
        Assert.AreEqual(1, merge.KnownRed.Count);
        DemoGateReport demo = DemoGate.Evaluate(GateLevel.Demo, results, 1, known);
        Assert.IsTrue(demo.Refused);
        StringAssert.Contains("known red", demo.Reasons[0]);
    }

    [Test]
    public void KnownRedThatNowPasses_IsReportedStale()
    {
        var known = DemoGate.ParseKnownRed("qa.x  # BUG-1, CAM\n");
        DemoGateReport r = DemoGate.Evaluate(GateLevel.Merge, new[] { GateResult.Ok("qa.x", "CAM", "0") }, 1, known);
        Assert.AreEqual(1, r.StaleKnownRed.Count);
    }

    [Test]
    public void ParseKnownRed_SkipsCommentsAndBlanks()
    {
        Dictionary<string, string> k = DemoGate.ParseKnownRed("# a\n\n  qa.y   # why\nqa.z\n");
        CollectionAssert.AreEquivalent(new[] { "qa.y", "qa.z" }, k.Keys);
        Assert.AreEqual("why", k["qa.y"]);
    }

    [Test]
    public void Registry_HasNoFaults_AndQaRigsChecksAreThere()
    {
        List<RegisteredCheck> checks = GateChecks.All(new[] { typeof(QaRigChecks).Assembly });
        Assert.IsTrue(checks.Any(c => c.Info.Id == "qa.so.readers"));
        CollectionAssert.IsEmpty(GateChecks.Problems(checks).Select(p => p.ToString()));
    }

    [Test]
    public void KnownRedList_NamesOnlyRegisteredChecks()
    {
        string file = Path.Combine(SoFieldReadersTests.ProjectRoot(), "tools", "qa", "gate_known_red.txt");
        Dictionary<string, string> known = DemoGate.ParseKnownRed(File.ReadAllText(file));
        Assert.IsNotEmpty(known);
        List<string> ids = GateChecks.All(new[] { typeof(QaRigChecks).Assembly }).Select(c => c.Info.Id)
            .Concat(new[] { "qa.scenes.missingScripts", "qa.scenes.materials", "qa.scenes.retired", "qa.scenes.liberationSans" }).ToList();
        foreach (KeyValuePair<string, string> k in known)
        {
            Assert.IsTrue(ids.Any(id => GateResults.Matches(k.Key, id)), $"{k.Key} names no registered check");
            StringAssert.Contains("BUG-", k.Value, $"{k.Key} names its BUG part");
        }
    }

    private static GateResult Throws() => throw new InvalidOperationException("boom");

    private static GateResult NullResult() => null;

    private static IEnumerable<GateResult> Several()
    {
        yield return GateResult.Ok("x.a", null, "1");
        yield return new GateResult { Pass = true, Measured = "2" };
    }

    private static RegisteredCheck Check(string method) =>
        new RegisteredCheck(new GateCheckAttribute("t.check", "QA-RIG"), typeof(DemoGateTests).GetMethod(method, BindingFlags.NonPublic | BindingFlags.Static));

    [Test]
    public void ACheckThatThrows_FailsUnderItsId()
    {
        List<GateResult> r = Check(nameof(Throws)).Run();
        Assert.AreEqual(1, r.Count);
        Assert.IsFalse(r[0].Pass);
        Assert.AreEqual("t.check", r[0].Id);
        StringAssert.Contains("boom", r[0].Detail);
    }

    [Test]
    public void ACheckWithNoResult_Fails()
    {
        Assert.IsFalse(Check(nameof(NullResult)).Run().Single().Pass);
    }

    [Test]
    public void ResultsWithoutIdOrOwner_GetTheRegistrations()
    {
        List<GateResult> r = Check(nameof(Several)).Run();
        Assert.AreEqual(2, r.Count);
        Assert.AreEqual("QA-RIG", r[0].Owner);
        Assert.AreEqual("t.check", r[1].Id);
    }

    [Test]
    public void Problems_FlagDuplicateIdsAndBadShapes()
    {
        RegisteredCheck a = Check(nameof(Throws)), b = Check(nameof(NullResult));
        var bad = new RegisteredCheck(new GateCheckAttribute("t.bad", ""), typeof(DemoGateTests).GetMethod(nameof(Check), BindingFlags.NonPublic | BindingFlags.Static));
        List<GateResult> p = GateChecks.Problems(new[] { a, b, bad });
        Assert.IsTrue(p.Any(x => x.Id == "t.check" && x.Measured == "duplicate id"));
        Assert.IsTrue(p.Any(x => x.Id == "t.bad" && x.Detail.Contains("takes parameters")));
        Assert.IsTrue(p.Any(x => x.Id == "t.bad" && x.Detail.Contains("names no owner")));
    }

    private static QaGateResult R(string id, bool pass, string commit = "abc", bool bp = false) =>
        new QaGateResult { id = id, owner = "QA-RIG", pass = pass, measured = "m", commit = commit, builtPlayer = bp };

    [Test]
    public void Judge_NoMatchingResult_FailsAsNotMeasured()
    {
        GateResult g = GateResults.Judge("c", "QA-RIG", "smoke_day1.*", new[] { R("other.x", true) }, "abc", false);
        Assert.IsFalse(g.Pass);
        Assert.AreEqual("not measured", g.Measured);
    }

    [Test]
    public void Judge_AllGreenAtHead_Passes_ARedOrStaleOne_Fails()
    {
        Assert.IsTrue(GateResults.Judge("c", "QA-RIG", "smoke_day1.*", new[] { R("smoke_day1.console", true), R("smoke_day1.camera", true) }, "abc", false).Pass);
        Assert.IsFalse(GateResults.Judge("c", "QA-RIG", "smoke_day1.*", new[] { R("smoke_day1.console", false) }, "abc", false).Pass);
        GateResult stale = GateResults.Judge("c", "QA-RIG", "smoke_day1.*", new[] { R("smoke_day1.console", true, "old") }, "abc", false);
        Assert.IsFalse(stale.Pass);
        StringAssert.Contains("STALE", stale.Detail);
    }

    [Test]
    public void Judge_BuiltPlayerCheck_RefusesEditorResults()
    {
        Assert.IsFalse(GateResults.Judge("c", "QA-RIG", "bp.*", new[] { R("bp.smoke.flicker", true) }, "abc", true).Pass);
        Assert.IsTrue(GateResults.Judge("c", "QA-RIG", "bp.*", new[] { R("bp.smoke.flicker", true, "abc", true) }, "abc", true).Pass);
    }

    [Test]
    public void Matches_IsAWildcardOnTheWholeId()
    {
        Assert.IsTrue(GateResults.Matches("bp.bp_smoke.*", "bp.bp_smoke.seq.office.flicker"));
        Assert.IsFalse(GateResults.Matches("smoke_day1.*", "bp.smoke_day1.console"));
        Assert.IsTrue(GateResults.Matches("perf.gcsites", "PERF.GCSITES"));
    }

    [Test]
    public void FromRun_PrefixesBuiltPlayerResults_AndStampsTheCommit()
    {
        var report = new QaRunReport { assertions = new[] { new QaAssertion { id = "bp_smoke.console", owner = "QA-RIG", pass = true, measured = "errors 0" } } };
        QaGateResult r = GateResults.FromRun(report, "dir", "abc", true).Single();
        Assert.AreEqual("bp.bp_smoke.console", r.id);
        Assert.AreEqual("abc", r.commit);
        Assert.IsTrue(r.builtPlayer);
    }

    [Test]
    public void GitHead_ReadsAWorktreesCommit()
    {
        string root = Path.Combine(Path.GetTempPath(), "qarig_githead_" + Guid.NewGuid().ToString("N"));
        try
        {
            string common = Path.Combine(root, "repo", ".git");
            string wtGit = Path.Combine(common, "worktrees", "wt");
            string wt = Path.Combine(root, "wt");
            Directory.CreateDirectory(wtGit);
            Directory.CreateDirectory(Path.Combine(common, "refs", "heads", "fix"));
            Directory.CreateDirectory(wt);
            File.WriteAllText(Path.Combine(wt, ".git"), "gitdir: " + wtGit + "\n");
            File.WriteAllText(Path.Combine(wtGit, "HEAD"), "ref: refs/heads/fix/qa\n");
            File.WriteAllText(Path.Combine(wtGit, "commondir"), "../..\n");
            File.WriteAllText(Path.Combine(common, "refs", "heads", "fix", "qa"), "0123456789abcdef\n");
            Assert.AreEqual("0123456789abcdef", GitHead.Read(wt));
            File.Delete(Path.Combine(common, "refs", "heads", "fix", "qa"));
            File.WriteAllText(Path.Combine(common, "packed-refs"), "# pack-refs\nfedcba9876543210 refs/heads/fix/qa\n");
            Assert.AreEqual("fedcba9876543210", GitHead.Read(wt));
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, true);
        }
    }
}
