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

    [Test]
    public void ParseKnownRed_TakesTheFirstWordAsTheCheck_AndTheRestAsRunPatterns()
    {
        const string text = "# h\nqa.play.camera   *.camera *.camera.*  # BUG-17, CAM\nqa.bp.x  # BUG-2, QA-RIG\nqa.y bp.*.seq.*.flicker *.camera\n";
        CollectionAssert.AreEquivalent(new[] { "qa.play.camera", "qa.bp.x", "qa.y" }, DemoGate.ParseKnownRed(text).Keys);
        Assert.AreEqual("BUG-17, CAM", DemoGate.ParseKnownRed(text)["qa.play.camera"]);
        CollectionAssert.AreEqual(new[] { "*.camera", "*.camera.*", "bp.*.seq.*.flicker" }, DemoGate.KnownRedRunPatterns(text));
    }

    private static QaRunReport Run(bool ok, params QaAssertion[] assertions) => new QaRunReport
    {
        scenario = "smoke_day1",
        ok = ok,
        steps = new[] { new QaStepResult { i = 0, op = "title", ok = true } },
        assertions = assertions,
    };

    private static QaAssertion A(string id, bool pass) => new QaAssertion { id = id, owner = "QA-RIG", pass = pass, measured = "m", detail = "d" };

    [Test]
    public void JudgeRun_OnlyKnownRed_IsOk_AndSaysSo()
    {
        RunVerdict v = DemoGate.JudgeRun(Run(false, A("smoke_day1.console", true), A("smoke_day1.camera", false), A("smoke_day1.liberationSans.desk", false)),
            new[] { "*.camera", "*.liberationSans.*" });
        Assert.IsTrue(v.Ok);
        Assert.AreEqual(2, v.KnownRed.Count);
        StringAssert.StartsWith("ok (2 known red", v.Line);
        Assert.AreEqual("ok", DemoGate.JudgeRun(Run(true, A("smoke_day1.console", true)), new[] { "*.camera" }).Line);
    }

    [Test]
    public void JudgeRun_ARegressionOrAFailedStepOrAnUnfinishedRun_Fails()
    {
        RunVerdict red = DemoGate.JudgeRun(Run(false, A("smoke_day1.camera", false), A("smoke_day1.presses", false)), new[] { "*.camera" });
        Assert.IsFalse(red.Ok);
        StringAssert.StartsWith("failed (1 unexpected, 1 known red)", red.Line);
        StringAssert.Contains("smoke_day1.presses", red.Line);

        QaRunReport step = Run(false);
        step.steps = new[] { new QaStepResult { i = 3, op = "sign", ok = false, note = "timed out" } };
        StringAssert.Contains("step 3 sign: timed out", DemoGate.JudgeRun(step, new string[0]).Line);

        Assert.IsFalse(DemoGate.JudgeRun(Run(false), new string[0]).Ok, "not ok with nothing red: it stopped early");
        Assert.IsFalse(DemoGate.JudgeRun(null, new string[0]).Ok);
    }

    [Test]
    public void TheProjectsKnownRedList_SparesTheSmokeDaysOpenParts_AndNothingOfQaRigs()
    {
        string file = Path.Combine(SoFieldReadersTests.ProjectRoot(), "tools", "qa", "gate_known_red.txt");
        List<string> patterns = DemoGate.KnownRedRunPatterns(File.ReadAllText(file));
        foreach (string id in new[] { "smoke_day1.camera", "smoke_day1.camera.handover", "smoke_day1.liberationSans.desk", "smoke_day1.seq.handover.flicker", "bp.bp_smoke.seq.office_h2_idle.flicker" })
            Assert.IsTrue(patterns.Any(p => GateResults.Matches(p, id)), $"{id} is known red");
        foreach (string id in new[] { "smoke_day1.console", "smoke_day1.presses", "smoke_day1.quality", "smoke_day1.missingScripts.desk", "smoke_day1.materials.end", "smoke_day1.seq.handover.flash", "bp.bp_smoke.gc.steady", "bp.bp_smoke.seq.callnext.hitches" })
            Assert.IsFalse(patterns.Any(p => GateResults.Matches(p, id)), $"{id} is not known red");
    }

    [Test]
    public void Judge_AResultMeasuredOnUncommittedChanges_FailsAsDirty()
    {
        QaGateResult dirty = R("smoke_day1.console", true);
        dirty.dirty = true;
        GateResult g = GateResults.Judge("c", "QA-RIG", "smoke_day1.*", new[] { dirty }, "abc", false);
        Assert.IsFalse(g.Pass);
        StringAssert.Contains("DIRTY smoke_day1.console", g.Detail);
        StringAssert.Contains("1 dirty", g.Measured);
        Assert.IsTrue(GateResults.FromRun(Run(true, A("smoke_day1.console", true)), "dir", "abc", false, true).Single().dirty);
    }

    [Test]
    public void GitDirty_Filter_DropsUnitysChurn()
    {
        string output = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset\nAssets/Scripts/GameManager.cs\n\n";
        CollectionAssert.AreEqual(new[] { "Assets/Scripts/GameManager.cs" }, GitDirty.Filter(output));
        CollectionAssert.IsEmpty(GitDirty.Filter("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset\r\n"));
    }

    /// <summary>An editor rewrites the build scene list, the current quality level and the project files by itself: none of them makes a dirty tree; a real settings or package change still does.</summary>
    [Test]
    public void GitDirty_Filter_DropsUnitysProjectSettingsChurn()
    {
        string output = "ProjectSettings/EditorBuildSettings.asset\r\nProjectSettings/QualitySettings.asset\nAssembly-CSharp.csproj\nNOPE-t6.sln\n"
                      + "ProjectSettings/ProjectSettings.asset\nProjectSettings/InputManager.asset\nPackages/manifest.json\n";
        CollectionAssert.AreEqual(new[] { "ProjectSettings/ProjectSettings.asset", "ProjectSettings/InputManager.asset", "Packages/manifest.json" }, GitDirty.Filter(output));
        CollectionAssert.IsEmpty(GitDirty.Filter("projectsettings/qualitysettings.asset\nProjectSettings/EditorBuildSettings.asset\n"));
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
