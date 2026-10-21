using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;

/// <summary>
/// UI-PC's play checks: each result is required by its own id, measured,
/// green and current, so one never written fails by name. A single wildcard
/// (GateResults.Judge) goes green on any subset of its matches: with seqtools
/// cuts run by hand on pc_open and pc_close only, uipc.transitions passed
/// while pc_reverse_open and pc_reverse_close were never judged (fix round
/// 6). Every [GateCheck] play method is run itself, as DemoGate runs it, on
/// given results (UiPcChecks.Source), against a table of the exact ids each
/// requires, so a method handed the wrong list or put back to a wildcard
/// fails (fix round 7). Also keeps the checks' recordings and ops in step
/// with the PC session (tools/qa/scenarios/ui_pc_session.json).
/// Pure: tools/qa/editor_runner runs it offline (QA_PROJECT for the session
/// file), Unity's EditMode suite too.
/// </summary>
public class UiPcChecksTests
{
    private const string Head = "abc";

    private static QaGateResult R(string id, bool pass = true, string commit = Head, bool dirty = false) =>
        new QaGateResult { id = id, owner = "UI-PC", pass = pass, measured = "m", commit = commit, dirty = dirty };

    private static QaGateResult[] Green(IEnumerable<string> ids) => ids.Select(i => R(i)).ToArray();

    /// <summary>Every id list a multi-result check requires, by check id.</summary>
    private static IEnumerable<(string check, string[] ids)> Lists() => new[]
    {
        ("uipc.transitions", UiPcChecks.TransitionIds),
        ("uipc.fullmotion", UiPcChecks.FullMotionIds),
        ("uipc.reverse", UiPcChecks.ReverseIds),
        ("uipc.poweron", UiPcChecks.PowerOnIds),
    };

    [Test]
    public void Transitions_OnlyThePlainMovesJudged_FailsNamingTheReversedOnes()
    {
        // Fix round 4's recipe 3: seqtools cuts by hand on pc_open and pc_close only.
        var results = new[] { R("ui_pc_session.seq.pc_open.cuts"), R("ui_pc_session.seq.pc_close.cuts") };
        Assert.IsTrue(GateResults.Judge("uipc.transitions", "UI-PC", "ui_pc_session.seq.pc_*.cuts", results, Head, false).Pass,
            "the old single wildcard goes green on the subset (the gap this closes)");

        GateResult g = UiPcChecks.JudgeEach("uipc.transitions", UiPcChecks.TransitionIds, results, Head);

        Assert.IsFalse(g.Pass);
        StringAssert.Contains("'ui_pc_session.seq.pc_reverse_open.cuts'", g.Detail);
        StringAssert.Contains("'ui_pc_session.seq.pc_reverse_close.cuts'", g.Detail);
        StringAssert.DoesNotContain("'ui_pc_session.seq.pc_open.cuts'", g.Detail, "a measured green result is not named as missing");
        StringAssert.Contains("ui_pc_session.seq.pc_open.cuts: 1 result(s): 1 pass", g.Measured);
        StringAssert.Contains("ui_pc_session.seq.pc_reverse_close.cuts: not measured", g.Measured);
    }

    [Test]
    public void TransitionsAndFullMotion_RequireEachOfTheSessionsFourRecordings()
    {
        CollectionAssert.AreEqual(new[] { "pc_open", "pc_close", "pc_reverse_open", "pc_reverse_close" }, UiPcChecks.Recordings);
        CollectionAssert.AreEqual(new[]
        {
            "ui_pc_session.seq.pc_open.cuts", "ui_pc_session.seq.pc_close.cuts",
            "ui_pc_session.seq.pc_reverse_open.cuts", "ui_pc_session.seq.pc_reverse_close.cuts",
        }, UiPcChecks.TransitionIds);
        CollectionAssert.AreEqual(new[]
        {
            "ui_pc_session.fullmotion.pc_open", "ui_pc_session.fullmotion.pc_close",
            "ui_pc_session.fullmotion.pc_reverse_open", "ui_pc_session.fullmotion.pc_reverse_close",
        }, UiPcChecks.FullMotionIds);
    }

    [Test]
    public void ReverseAndPowerOn_RequireBothOfTheirAsserts()
    {
        CollectionAssert.AreEqual(new[] { "ui_pc_session.reverse.open", "ui_pc_session.reverse.close" }, UiPcChecks.ReverseIds);
        CollectionAssert.AreEqual(new[] { "ui_pc_session.poweron.full", "ui_pc_session.poweron.reduced" }, UiPcChecks.PowerOnIds);
    }

    [Test]
    public void EachList_NamesItsIdsExactly_SoEveryOneMustBeWritten()
    {
        foreach ((string check, string[] ids) in Lists())
        {
            Assert.IsNotEmpty(ids, check);
            foreach (string id in ids)
                Assert.IsFalse(id.Contains("*"), $"{check}: '{id}' is a wildcard, which a subset of its matches satisfies");
            for (int missing = 0; missing < ids.Length; missing++)
            {
                QaGateResult[] results = Green(ids.Where((_, i) => i != missing));
                GateResult g = UiPcChecks.JudgeEach(check, ids, results, Head);
                Assert.IsFalse(g.Pass, $"{check} without {ids[missing]}");
                StringAssert.Contains($"'{ids[missing]}'", g.Detail, check);
                Assert.AreEqual(check, g.Id);
            }
        }
    }

    [Test]
    public void EachList_AllMeasuredGreenAtHead_Passes()
    {
        foreach ((string check, string[] ids) in Lists())
        {
            GateResult g = UiPcChecks.JudgeEach(check, ids, Green(ids).Append(R("other_run.console", false)), Head);
            Assert.IsTrue(g.Pass, $"{check}: {g.Measured} {g.Detail}");
            Assert.AreEqual(check, g.Id);
            Assert.AreEqual("UI-PC", g.Owner);
            foreach (string id in ids)
                StringAssert.Contains(id + ": 1 result(s): 1 pass", g.Measured);
        }
    }

    [Test]
    public void JudgeEach_ARedStaleOrDirtyResult_FailsNamingIt()
    {
        string[] ids = UiPcChecks.ReverseIds;
        foreach (QaGateResult bad in new[] { R(ids[1], pass: false), R(ids[1], commit: "old"), R(ids[1], dirty: true) })
        {
            GateResult g = UiPcChecks.JudgeEach("uipc.reverse", ids, new[] { R(ids[0]), bad }, Head);
            Assert.IsFalse(g.Pass, $"pass {bad.pass}, commit {bad.commit}, dirty {bad.dirty}");
            StringAssert.Contains(ids[1], g.Detail);
            StringAssert.DoesNotContain(ids[0] + " ", g.Detail, "the green one is not named");
        }
    }

    /// <summary>
    /// The session's op that writes each play check's results, and the exact
    /// results the check requires, by check id: the spec every [GateCheck]
    /// method is run against. The review of 2003df8 put Transitions() back to
    /// its old wildcard, and in a second copy handed it ReverseIds, and every
    /// test stayed green: they proved JudgeEach and the lists, never which
    /// list each method passes.
    /// </summary>
    private static readonly Dictionary<string, (string op, string[] ids)> Required = new Dictionary<string, (string op, string[] ids)>
    {
        ["uipc.esc"] = ("uipcEsc", new[] { "ui_pc_session.esc" }),
        ["uipc.escdark"] = ("uipcEscDark", new[] { "ui_pc_session.escdark" }),
        ["uipc.room"] = ("uipcRoom", new[] { "ui_pc_session.room" }),
        ["uipc.windows"] = ("uipcWindows", new[] { "ui_pc_session.windows" }),
        ["uipc.enter"] = ("uipcEnter", new[] { "ui_pc_session.enter" }),
        ["uipc.zoom"] = ("uipcZoom", new[] { "ui_pc_session.zoom" }),
        ["uipc.balance"] = ("uipcBalance", new[] { "ui_pc_session.balance" }),
        ["uipc.bezel"] = ("uipcBezel", new[] { "ui_pc_session.bezel" }),
        ["uipc.tray"] = ("uipcTray", new[] { "ui_pc_session.tray" }),
        ["uipc.order"] = ("uipcOrder", new[] { "ui_pc_session.order" }),
        ["uipc.restore"] = ("uipcRestore", new[] { "ui_pc_session.restore" }),
        ["uipc.gate"] = ("uipcGate", new[] { "ui_pc_session.gate" }),
        ["uipc.poweron"] = ("uipcPowerOn", new[] { "ui_pc_session.poweron.full", "ui_pc_session.poweron.reduced" }),
        ["uipc.reverse"] = ("uipcReverseOpen", new[] { "ui_pc_session.reverse.open", "ui_pc_session.reverse.close" }),
        ["uipc.fullmotion"] = ("uipcFullMotion", new[]
        {
            "ui_pc_session.fullmotion.pc_open", "ui_pc_session.fullmotion.pc_close",
            "ui_pc_session.fullmotion.pc_reverse_open", "ui_pc_session.fullmotion.pc_reverse_close",
        }),
        ["uipc.transitions"] = ("recStart", new[]
        {
            "ui_pc_session.seq.pc_open.cuts", "ui_pc_session.seq.pc_close.cuts",
            "ui_pc_session.seq.pc_reverse_open.cuts", "ui_pc_session.seq.pc_reverse_close.cuts",
        }),
    };

    /// <summary>Every [GateCheck] method of UiPcChecks that reads the play results, with its attribute.</summary>
    private static List<(GateCheckAttribute check, MethodInfo method)> PlayChecks() =>
        typeof(UiPcChecks).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                          .Select(m => (check: m.GetCustomAttribute<GateCheckAttribute>(), method: m))
                          .Where(c => c.check != null && c.check.Kind == GateKind.Play).ToList();

    /// <summary>Runs check <paramref name="method"/> as DemoGate does (no arguments) on <paramref name="results"/> at <see cref="Head"/>, then puts the checks' source back.</summary>
    private static GateResult Run(MethodInfo method, IEnumerable<QaGateResult> results)
    {
        var live = UiPcChecks.Source;
        List<QaGateResult> given = results.ToList();
        UiPcChecks.Source = () => (given, Head);
        try
        {
            return (GateResult)method.Invoke(null, null);
        }
        finally
        {
            UiPcChecks.Source = live;
        }
    }

    /// <summary>The PC session's steps, in order: each op and its name (tools/qa/scenarios/ui_pc_session.json; QA_PROJECT).</summary>
    private static List<(string op, string name)> SessionSteps()
    {
        string path = Path.Combine(SoFieldReadersTests.ProjectRoot(), "tools", "qa", "scenarios", "ui_pc_session.json");
        return Regex.Matches(File.ReadAllText(path), "\\{\\s*\"op\"\\s*:\\s*\"(\\w+)\"(?:\\s*,\\s*\"name\"\\s*:\\s*\"(\\w+)\")?")
                    .Cast<Match>().Select(m => (op: m.Groups[1].Value, name: m.Groups[2].Value)).ToList();
    }

    [Test]
    public void EveryPlayCheck_IsInTheTable_UiPcs_AndRunAtEveryMerge()
    {
        List<(GateCheckAttribute check, MethodInfo method)> checks = PlayChecks();
        CollectionAssert.AreEquivalent(Required.Keys, checks.Select(c => c.check.Id), "each of UI-PC's play checks, and only those, is specified here");
        foreach ((GateCheckAttribute check, MethodInfo method) in checks)
        {
            Assert.AreEqual("UI-PC", check.Owner, check.Id);
            Assert.IsFalse(check.DemoOnly, $"{check.Id}: a merge gate skips a DemoOnly check");
            Assert.IsEmpty(method.GetParameters(), $"{check.Id}: DemoGate calls a check with no arguments");
            Assert.AreEqual(typeof(GateResult), method.ReturnType, check.Id);
        }
    }

    [Test]
    public void EachPlayCheck_AllItsIdsGreenAtHead_PassesNamingEach_AndNoOtherResultCounts()
    {
        foreach ((GateCheckAttribute check, MethodInfo method) in PlayChecks())
        {
            string[] ids = Required[check.Id].ids;
            // Red results of other ids, one inside each old wildcard's reach: none of them is this check's.
            QaGateResult[] others = { R("ui_pc_session.decoy", false), R("ui_pc_session.seq.pc_decoy.cuts", false), R(ids[0] + "_decoy", false) };
            GateResult g = Run(method, Green(ids).Concat(others));
            Assert.IsTrue(g.Pass, $"{check.Id}: {g.Measured} {g.Detail}");
            Assert.AreEqual(check.Id, g.Id, "the result is the check's own");
            Assert.AreEqual("UI-PC", g.Owner, check.Id);
            foreach (string id in ids)
                StringAssert.Contains(id + ": 1 result(s): 1 pass", g.Measured, $"{check.Id} judges {id} by its own id");
        }
    }

    [Test]
    public void EachPlayCheck_AnyOneOfItsIdsMissing_FailsNamingIt()
    {
        foreach ((GateCheckAttribute check, MethodInfo method) in PlayChecks())
        {
            string[] ids = Required[check.Id].ids;
            for (int missing = 0; missing < ids.Length; missing++)
            {
                GateResult g = Run(method, Green(ids.Where((_, i) => i != missing)));
                Assert.IsFalse(g.Pass, $"{check.Id} without {ids[missing]}");
                StringAssert.Contains($"'{ids[missing]}'", g.Detail, check.Id);
            }
        }
    }

    [Test]
    public void EachPlayCheck_ItsResultsAreWrittenByAnOpTheSessionPlays()
    {
        List<string> ops = SessionSteps().Select(s => s.op).ToList();
        foreach (KeyValuePair<string, (string op, string[] ids)> check in Required)
            CollectionAssert.Contains(ops, check.Value.op, $"{check.Key}: ui_pc_session.json plays {check.Value.op}, which writes its results");
    }

    [Test]
    public void Recordings_AreTheSessionsRecordings_EachAfterItsFullMotionCheck()
    {
        List<(string op, string name)> steps = SessionSteps();
        var recordings = new List<string>();
        for (int i = 0; i < steps.Count; i++)
        {
            if (steps[i].op != "recStart")
                continue;
            recordings.Add(steps[i].name);
            Assert.Greater(i, 0, "a recording opens the session");
            Assert.AreEqual(("uipcFullMotion", steps[i].name), steps[i - 1], $"uipcFullMotion runs right before recording {steps[i].name}");
        }
        CollectionAssert.AreEqual(UiPcChecks.Recordings, recordings, "uipc.transitions and uipc.fullmotion require each recording the session makes, and only those");
        CollectionAssert.AreEqual(UiPcChecks.Recordings, steps.Where(s => s.op == "uipcFullMotion").Select(s => s.name).ToArray());
    }
}
