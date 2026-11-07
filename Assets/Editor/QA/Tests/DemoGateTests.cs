using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
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

    /// <summary>
    /// The code review's RG-01 (2026-10-28): the merge gate spared a known-red
    /// check whole, so the entry for the Helix monitor's helix_before line hid
    /// helix_after_drop when it went red (69f23c4). An entry spares its check
    /// only when every result that made it red matches that entry's own run
    /// patterns; an entry with no pattern still spares its check whole.
    /// </summary>
    [Test]
    public void KnownRedEntry_SparesOnlyItsOwnRunPatterns_AnyOtherRedInTheCheckRefusesTheMerge()
    {
        const string text = "hallfx.helix.flicker  hall_fx_helix.seq.helix_before.flicker  # BUG-13, HALL-FX: helix_before only\nqa.scenes.liberationSans  # BUG-3, TEXT-LANG\n";
        Dictionary<string, string> known = DemoGate.ParseKnownRed(text);
        Dictionary<string, List<string>> own = DemoGate.KnownRedPatternsByCheck(text);
        CollectionAssert.AreEqual(new[] { "hall_fx_helix.seq.helix_before.flicker" }, own["hallfx.helix.flicker"]);
        CollectionAssert.IsEmpty(own["qa.scenes.liberationSans"]);

        GateResult beforeOnly = Helix(R("hall_fx_helix.seq.helix_before.flicker", false), R("hall_fx_helix.seq.helix_after_drop.flicker", true));
        DemoGateReport spared = DemoGate.Evaluate(GateLevel.Merge, new[] { beforeOnly }, 1, known, own);
        Assert.IsFalse(spared.Refused, "the entry's own line is red: spared");
        Assert.AreEqual(1, spared.KnownRed.Count);
        Assert.IsTrue(DemoGate.Evaluate(GateLevel.Demo, new[] { beforeOnly }, 1, known, own).Refused, "the demo gate refuses known red");

        GateResult afterDrop = Helix(R("hall_fx_helix.seq.helix_before.flicker", false), R("hall_fx_helix.seq.helix_after_drop.flicker", false));
        CollectionAssert.AreEquivalent(new[] { "hall_fx_helix.seq.helix_before.flicker", "hall_fx_helix.seq.helix_after_drop.flicker" }, afterDrop.Unmet);
        DemoGateReport refused = DemoGate.Evaluate(GateLevel.Merge, new[] { afterDrop }, 1, known, own);
        Assert.IsTrue(refused.Refused, "a red line outside the entry's patterns refuses the merge");
        StringAssert.Contains("helix_after_drop", refused.Reasons.Single());
        CollectionAssert.IsEmpty(refused.KnownRed);

        GateResult stale = Helix(R("hall_fx_helix.seq.helix_before.flicker", false), R("hall_fx_helix.seq.helix_after_drop.flicker", true, "old"));
        Assert.IsTrue(DemoGate.Evaluate(GateLevel.Merge, new[] { stale }, 1, known, own).Refused, "a stale line outside the patterns refuses too: it measured no commit");

        var whole = new[] { GateResult.Fail("qa.scenes.liberationSans", "TEXT-LANG", "4 texts", "...") };
        Assert.IsFalse(DemoGate.Evaluate(GateLevel.Merge, whole, 1, known, own).Refused, "an entry with no pattern spares its check whole");
    }

    /// <summary>RG-01 on the project's own list: the Helix entry and stamp.aim's edge tries spare only their lines; helix_after_drop and a pile refusal refuse the merge.</summary>
    [Test]
    public void TheProjectsKnownRedList_SparesEachCheckOnlyForItsEntrysLines()
    {
        string text = File.ReadAllText(Path.Combine(SoFieldReadersTests.ProjectRoot(), "tools", "qa", "gate_known_red.txt"));
        Dictionary<string, string> known = DemoGate.ParseKnownRed(text);
        Dictionary<string, List<string>> own = DemoGate.KnownRedPatternsByCheck(text);
        Assert.IsFalse(DemoGate.Evaluate(GateLevel.Merge, new[] { Helix(R("hall_fx_helix.seq.helix_before.flicker", false)) }, 1, known, own).Refused);
        Assert.IsTrue(DemoGate.Evaluate(GateLevel.Merge, new[] { Helix(R("hall_fx_helix.seq.helix_after_drop.flicker", false)) }, 1, known, own).Refused, "the other three helix lines stay strict");
        GateResult edge = GateResults.Judge("stamp.aim", "STAMP", "stamp_aim.aim.*", new[] { R("stamp_aim.aim.a6", false), R("stamp_aim.aim.c3", false), R("stamp_aim.aim.b4_1", true) }, "abc", false);
        Assert.IsFalse(DemoGate.Evaluate(GateLevel.Merge, new[] { edge }, 1, known, own).Refused, "the edge tries the entry names");
        GateResult pile = GateResults.Judge("stamp.aim", "STAMP", "stamp_aim.aim.*", new[] { R("stamp_aim.aim.a6", false), R("stamp_aim.aim.b9_1", false) }, "abc", false);
        Assert.IsTrue(DemoGate.Evaluate(GateLevel.Merge, new[] { pile }, 1, known, own).Refused, "the pile refusals stay strict");
    }

    /// <summary>
    /// The code review's RG-02 (2026-10-28): no check read the dater's carry
    /// lines, so the demo gate could not refuse QA-05. stamp.carry reads them;
    /// it is known red under STAMP (Saleh's open call), so the merge gate lists
    /// it and the demo gate refuses it; the let-go lines stay strict.
    /// </summary>
    [Test]
    public void TheCarryCheck_ReadsTheDatersCarryLines_KnownRedUnderStamp_TheDemoGateRefusesIt()
    {
        RegisteredCheck carry = GateChecks.All(new[] { typeof(StampChecks).Assembly }).Single(c => c.Info.Id == "stamp.carry");
        Assert.AreEqual("STAMP", carry.Info.Owner);
        Assert.AreEqual(GateKind.Play, carry.Info.Kind);
        Assert.IsFalse(carry.Info.DemoOnly, "the merge gate runs it too (and lists it as known red)");

        string text = File.ReadAllText(Path.Combine(SoFieldReadersTests.ProjectRoot(), "tools", "qa", "gate_known_red.txt"));
        Dictionary<string, string> known = DemoGate.ParseKnownRed(text);
        Dictionary<string, List<string>> own = DemoGate.KnownRedPatternsByCheck(text);
        Assert.IsTrue(known.ContainsKey("stamp.carry"));
        StringAssert.Contains("BUG-15c", known["stamp.carry"]);
        StringAssert.Contains("STAMP", known["stamp.carry"]);
        StringAssert.Contains("Saleh's open call: how the carried dater meets the taller painted machines", known["stamp.carry"]);

        GateResult red = GateResults.Judge("stamp.carry", "STAMP", "stamp_aim.letGoCarry.*", new[] { R("stamp_aim.letGoCarry.scanner1", false), R("stamp_aim.letGo.scanner1", false) }, "abc", false);
        CollectionAssert.AreEqual(new[] { "stamp_aim.letGoCarry.scanner1" }, red.Unmet, "the check reads the carry lines, not the let-go lines");
        Assert.IsFalse(DemoGate.Evaluate(GateLevel.Merge, new[] { red }, 1, known, own).Refused, "the merge gate lists it");
        Assert.IsTrue(DemoGate.Evaluate(GateLevel.Demo, new[] { red }, 1, known, own).Refused, "the demo gate refuses it");
        List<string> patterns = DemoGate.KnownRedRunPatterns(text);
        Assert.IsTrue(patterns.Any(p => GateResults.Matches(p, "stamp_aim.letGoCarry.rack1")), "a stamp_aim run reports its carry lines as known red");
        Assert.IsFalse(patterns.Any(p => GateResults.Matches(p, "stamp_aim.letGo.rack1")), "the let-go lines stay strict");
    }

    /// <summary>The Helix monitor's flicker check judged on <paramref name="results"/> at HEAD "abc" (HallFxChecks.HelixFlicker's pattern).</summary>
    private static GateResult Helix(params QaGateResult[] results) =>
        GateResults.Judge("hallfx.helix.flicker", "HALL-FX", "hall_fx_helix.seq.*.flicker", results, "abc", false);

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
            Assert.IsTrue(Regex.IsMatch(k.Value, @"\b(BUG-\d+|N-[a-z0-9-]+-\d+|[A-Z]{2,5}-\d+)"), $"{k.Key} names its bug (a BUG part, an audit id or a night row)");
        }
    }

    /// <summary>GATE-17 (the audit of 2026-10-28): "*.camera" and "*.seq.*.flicker" spared every run's camera and flicker lines. Each run pattern names its scenario (after "bp." for a built player's), never a leading wildcard.</summary>
    [Test]
    public void TheProjectsKnownRedList_NamesTheRunOfEveryPattern()
    {
        string text = File.ReadAllText(Path.Combine(SoFieldReadersTests.ProjectRoot(), "tools", "qa", "gate_known_red.txt"));
        foreach (string pattern in DemoGate.KnownRedRunPatterns(text))
        {
            string run = (pattern.StartsWith("bp.", StringComparison.Ordinal) ? pattern.Substring(3) : pattern).Split('.')[0];
            Assert.IsFalse(run.Length == 0 || run.Contains("*"), $"{pattern} names no run");
        }
    }

    /// <summary>GATE-18: an entry with run patterns spared a check that never ran ("not measured" named no result). A pattern with no result now refuses the merge; an entry with no pattern still spares its static check whole.</summary>
    [Test]
    public void KnownRedEntry_NeverSparesAResultThatWasNotMeasured()
    {
        const string text = "flow.esc.home  flow_pause.settings.home  # BUG-34b, HOME\nqa.scenes.liberationSans  # BUG-3, TEXT-LANG\n";
        Dictionary<string, string> known = DemoGate.ParseKnownRed(text);
        Dictionary<string, List<string>> own = DemoGate.KnownRedPatternsByCheck(text);
        GateResult never = GateResults.Judge("flow.esc.home", "FLOW", "flow_pause.settings.home", Array.Empty<QaGateResult>(), "abc", false);
        CollectionAssert.AreEqual(new[] { GateResults.NotMeasuredPrefix + "flow_pause.settings.home" }, never.Unmet);
        DemoGateReport merge = DemoGate.Evaluate(GateLevel.Merge, new[] { never }, 1, known, own);
        Assert.IsTrue(merge.Refused, "a known-red line that was never measured refuses the merge");
        StringAssert.Contains("not measured: flow_pause.settings.home", merge.Reasons.Single());

        GateResult measured = GateResults.Judge("flow.esc.home", "FLOW", "flow_pause.settings.home", new[] { R("flow_pause.settings.home", false) }, "abc", false);
        Assert.IsFalse(DemoGate.Evaluate(GateLevel.Merge, new[] { measured }, 1, known, own).Refused, "measured red, on the entry's own pattern: spared");

        var whole = new[] { GateResult.Fail("qa.scenes.liberationSans", "TEXT-LANG", "7 texts", "...") };
        Assert.IsFalse(DemoGate.Evaluate(GateLevel.Merge, whole, 1, known, own).Refused, "an entry with no pattern spares its static check whole");
    }

    /// <summary>GATE-18: a check judged over several patterns (CamChecks.Verdict, FlowChecks.Verdict, QaRigChecks.All ...) named no result, so its entry spared it whole, other lines too. It names its parts' lines now (GateResult.Naming), and a red check that names none refuses an entry with patterns.</summary>
    [Test]
    public void ACompositeCheck_NamesItsPartsLines_SoItsEntrySparesOnlyThose()
    {
        const string text = "cam.play.day12  cam_views.play.*  # BUG-17a, CAM\n";
        Dictionary<string, string> known = DemoGate.ParseKnownRed(text);
        Dictionary<string, List<string>> own = DemoGate.KnownRedPatternsByCheck(text);
        var results = new[] { R("cam_views.play.d12", false), R("cam_views.missingScripts.end", true) };
        GateResult onlyPlay = CamChecks.Verdict("cam.play.day12", "CAM", false, results, "abc", "cam_views.play.*", "cam_views.missingScripts.end");
        CollectionAssert.AreEqual(new[] { "cam_views.play.d12" }, onlyPlay.Unmet);
        Assert.IsFalse(DemoGate.Evaluate(GateLevel.Merge, new[] { onlyPlay }, 1, known, own).Refused, "the entry's own line: spared");

        var scriptRed = new[] { R("cam_views.play.d12", false), R("cam_views.missingScripts.end", false) };
        GateResult both = CamChecks.Verdict("cam.play.day12", "CAM", false, scriptRed, "abc", "cam_views.play.*", "cam_views.missingScripts.end");
        Assert.IsTrue(DemoGate.Evaluate(GateLevel.Merge, new[] { both }, 1, known, own).Refused, "a missing script in the same run is not the entry's");

        GateResult unnamed = GateResult.Fail("cam.play.day12", "CAM", "red", "a check that names no line");
        Assert.IsTrue(DemoGate.Evaluate(GateLevel.Merge, new[] { unnamed }, 1, known, own).Refused, "an entry with patterns cannot spare a red it cannot see");
    }

    /// <summary>GATE-01: a commit that touched only the docs or the known-red list made every result stale. MeasuredPaths names what a measurement reads: everything but the docs folder, Markdown outside Assets and the known-red list.</summary>
    [Test]
    public void MeasuredPaths_OnlyTheDocsAndTheKnownRedList_LeaveAResultCurrent()
    {
        foreach (string path in new[] { "docs/FEATURES.md", "docs/pending/QA-RIG.md", "tools/qa/gate_known_red.txt", "README.md", "tools/qa/NOTES.md", "" })
            Assert.IsFalse(MeasuredPaths.Affects(path), path);
        foreach (string path in new[] { "Assets/Scripts/GameManager.cs", "Assets/Art/UI/Kit/README.md", "Packages/manifest.json", "ProjectSettings/TagManager.asset", "tools/qa/scenarios/smoke_day1.json", "tools/qa/seqlib.py", ".gitattributes" })
            Assert.IsTrue(MeasuredPaths.Affects(path), path);
        Assert.IsTrue(MeasuredPaths.NoneAffected("docs/FEATURES.md\ntools/qa/gate_known_red.txt\n"));
        Assert.IsTrue(MeasuredPaths.NoneAffected(string.Empty), "the same tree");
        Assert.IsFalse(MeasuredPaths.NoneAffected("docs/FEATURES.md\nAssets/Editor/QA/DemoGate.cs\n"));
    }

    /// <summary>A check that picks results by commit itself (DeskPlanChecks.DecorVerdict) follows GateResults.IsCurrent, as Judge does: a carried line counts, and the check names it instead of throwing.</summary>
    [Test]
    public void TheDecorCheck_ReadsACarriedLine_AsJudgeDoes()
    {
        var line = new QaGateResult { id = DeskPlanChecks.DecorSpotsId, owner = "DESK-PLAN", pass = true, measured = "not applicable on the painted desk", commit = "old" };
        try
        {
            GateResults.SameMeasurement = (commit, head) => commit == "old" && head == "abc";
            GateResult r = DeskPlanChecks.DecorVerdict(new[] { line }, "abc");
            Assert.IsFalse(r.Pass, "the cut is red");
            CollectionAssert.AreEqual(new[] { DeskPlanChecks.DecorSpotsId }, r.Unmet, "the cut is named, so its known-red entry can spare it");
        }
        finally
        {
            GateResults.SameMeasurement = GateResults.ExactCommitOnly;
        }
    }

    /// <summary>A result measured at another commit counts only when GateResults.SameMeasurement carries it (DemoGate.Run sets MeasuredPaths' git rule); the default is the exact commit.</summary>
    [Test]
    public void Judge_AResultCarriedFromAnOlderCommit_IsCurrent_OnlyWhenTheRuleCarriesIt()
    {
        QaGateResult older = R("smoke_day1.console", true, "old");
        Assert.IsFalse(GateResults.Judge("c", "QA-RIG", "smoke_day1.*", new[] { older }, "abc", false).Pass, "the default: the exact commit only");
        try
        {
            GateResults.SameMeasurement = (commit, head) => commit == "old" && head == "abc";
            Assert.IsTrue(GateResults.Judge("c", "QA-RIG", "smoke_day1.*", new[] { older }, "abc", false).Pass);
            Assert.IsFalse(GateResults.Judge("c", "QA-RIG", "smoke_day1.*", new[] { R("smoke_day1.console", true, "other") }, "abc", false).Pass);
            QaGateResult dirty = R("smoke_day1.console", true, "old");
            dirty.dirty = true;
            Assert.IsFalse(GateResults.Judge("c", "QA-RIG", "smoke_day1.*", new[] { dirty }, "abc", false).Pass, "a carried result measured on uncommitted changes stays dirty");
        }
        finally
        {
            GateResults.SameMeasurement = GateResults.ExactCommitOnly;
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
        foreach (string id in new[] { "smoke_day1.camera", "smoke_day1.camera.handover", "smoke_day1.seq.handover.flicker", "bp.bp_smoke.seq.office_h2_idle.flicker" })
            Assert.IsTrue(patterns.Any(p => GateResults.Matches(p, id)), $"{id} is known red");
        foreach (string id in new[] { "smoke_day1.console", "smoke_day1.presses", "smoke_day1.quality", "smoke_day1.missingScripts.desk", "smoke_day1.materials.end", "smoke_day1.seq.handover.flash", "bp.bp_smoke.gc.steady", "bp.bp_smoke.seq.callnext.hitches",
                                      "smoke_day1.liberationSans.desk", "smoke_day1.seq.walk_in.flicker", "tools_torch.camera", "overlay_block.seq.overlay.flicker", "hall_fx_city.seq.office_a1_h2.flicker", "hall_fx_dusk.seq.dusk_office_a1.flicker" })
            Assert.IsFalse(patterns.Any(p => GateResults.Matches(p, id)), $"{id} is not known red");
    }

    /// <summary>The entries ruled at the six tracks' landing (2026-10-21, and ruling 6 of 2026-10-27: stamp.aim and stamp.rackClear under CAM) spare only the result their check reads: tools.micro's own assertions, stamp_aim's sixteen edge tries (a1 to a10 and the six (c) strokes at the rulebook's edge, two to three red each run) and CAM's stamp hint plate lines; desk.play.clicks and stamp.rackClear went at CAM's landing (2026-10-27), cam.play.guidePlate and qa.play.liberationSans at gate-hygiene (2026-11-07: they pass); qa.play.camera spares only the smoke day's and desk_plan_homes' camera lines; never QA-RIG's or STAMP's other assertions in those scenarios.</summary>
    [Test]
    public void TheRuledKnownRedEntries_SpareOnlyWhatTheirChecksRead()
    {
        string file = Path.Combine(SoFieldReadersTests.ProjectRoot(), "tools", "qa", "gate_known_red.txt");
        string text = File.ReadAllText(file);
        List<string> patterns = DemoGate.KnownRedRunPatterns(text);
        Dictionary<string, string> known = DemoGate.ParseKnownRed(text);
        foreach (string id in new[] { "tools.micro", "textlang.kitSymbols", "stamp.aim", "cam.play.stampHint", "cam.bp.gc", "qa.play.camera", "cam.play.day12", "tools.lens" })
            Assert.IsTrue(known.ContainsKey(id), $"{id} is on the list");
        Assert.IsFalse(known.ContainsKey("cam.play.guidePlate"), "cam.play.guidePlate went at gate-hygiene (its plate line passed: cam_chrome.plate.a1_d1.guide at ed6da29)");
        Assert.IsFalse(known.ContainsKey("qa.play.liberationSans"), "qa.play.liberationSans went at gate-hygiene (the smoke day's three lines passed at 370352f)");
        StringAssert.Contains("CONTENT", known["textlang.kitSymbols"]);
        StringAssert.Contains("UI-PC", known["tools.micro"]);
        StringAssert.Contains("STAMP", known["stamp.aim"]);
        StringAssert.Contains("DOC-HANDLE", known["qa.play.camera"]);
        Assert.IsFalse(known.ContainsKey("desk.play.clicks"), "desk.play.clicks went at CAM's landing (the intercom pressed in the reading view)");
        Assert.IsFalse(known.ContainsKey("stamp.rackClear"), "stamp.rackClear went at CAM's landing (0 rack px over the paper in the reading view)");
        StringAssert.Contains("STAMP", known["cam.play.stampHint"]);
        StringAssert.Contains("QA-RIG", known["cam.bp.gc"]);
        foreach (string id in new[] { "tools_microprint.tools.micro.read", "stamp_aim.aim.a6", "stamp_aim.aim.a9", "stamp_aim.aim.a1", "stamp_aim.aim.c3", "stamp_aim.aim.c9", "tools_lens.tools.lens.rulebook.revert", "cam_chrome.plate.a2.stamphint", "bp.cam_gc.gc.cam_cycle", "desk_plan_homes.camera.intercom_reading", "cam_views.play.d12" })
            Assert.IsTrue(patterns.Any(p => GateResults.Matches(p, id)), $"{id} is known red");
        foreach (string id in new[] { "cam_chrome.plate.a1_d1.guide", "cam_views.camera", "cam_views.camera.a1_city", "tools_torch.camera", "tools_microprint.console", "tools_microprint.presses", "tools_microprint.quality", "tools_microprint.missingScripts.desk", "tools_microprint.materials.end", "desk_plan_homes.console", "desk_plan_homes.presses", "desk_plan_homes.clickboxes.d1", "desk_plan_homes.visible.intercom", "desk_plan_decor.presses", "stamp_aim.console", "stamp_aim.presses", "stamp_aim.aim.b4_1", "stamp_aim.aim.b1_3", "stamp_aim.aim.c1", "stamp_aim.aim.c4", "tools_lens.tools.lens.newspaper.reach", "tools_lens.console", "stamp_aim.letGo.rack1", "stamp_case.console", "stamp_case.rackOverPaper.a1", "stamp_case.tab.a1.rackInFrame", "stamp_hold.hold.esc1", "cam_views.tilts.d12", "cam_views.frame.a1_d12_reading", "cam_chrome.console", "cam_chrome.chrome.a1", "cam_chrome.missingScripts.end", "cam_gc.cycle.cam_cycle", "bp.cam_gc.cycle.cam_cycle" })
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
        CollectionAssert.AreEqual(new[] { "ProjectSettings/InputManager.asset", "Packages/manifest.json" }, GitDirty.Filter(output));
        CollectionAssert.IsEmpty(GitDirty.Filter("projectsettings/qualitysettings.asset\nProjectSettings/EditorBuildSettings.asset\n"));
    }

    /// <summary>GATE-16: every run after a build was DIRTY, because a build rewrites the URP assets and the graphics, project, Shader Graph and connect settings. The night fleet's noise list (AGENT_RULES; unity_run.py NOISE, which the fleet never commits) is all churn; a real change beside it still makes the tree dirty.</summary>
    [Test]
    public void GitDirty_Filter_DropsEveryFileAPlayerBuildRewrites()
    {
        const string buildWrites = "Assets/DefaultVolumeProfile.asset\nAssets/Settings/UniversalRP.asset\nAssets/UniversalRenderPipelineGlobalSettings.asset\n"
                                 + "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset\n"
                                 + "ProjectSettings/GraphicsSettings.asset\nProjectSettings/ProjectSettings.asset\nProjectSettings/QualitySettings.asset\n"
                                 + "ProjectSettings/ShaderGraphSettings.asset\nProjectSettings/UnityConnectSettings.asset\nProjectSettings/EditorBuildSettings.asset\n"
                                 + "Assembly-CSharp-Editor.csproj\nTimeDesk.Domain.csproj\n";
        CollectionAssert.IsEmpty(GitDirty.Filter(buildWrites), "a run after a build is not dirty");
        CollectionAssert.AreEqual(new[] { "Assets/Settings/OfficeFinishProfile.asset" }, GitDirty.Filter(buildWrites + "Assets/Settings/OfficeFinishProfile.asset\n"));
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
