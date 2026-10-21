using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

/// <summary>
/// UI-PC's play checks that judge more than one result (uipc.transitions,
/// uipc.fullmotion, uipc.reverse, uipc.poweron): each result is required by
/// its own id, measured, green and current, so one never written fails by
/// name. A single wildcard (GateResults.Judge) goes green on any subset of
/// its matches: with seqtools cuts run by hand on pc_open and pc_close only,
/// uipc.transitions passed while pc_reverse_open and pc_reverse_close were
/// never judged (fix round 6). Also keeps the checks' recordings in step with
/// the PC session (tools/qa/scenarios/ui_pc_session.json).
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
        ("uipc.transitions", UiPcChecks.TransitionIds.ToArray()),
        ("uipc.fullmotion", UiPcChecks.FullMotionIds.ToArray()),
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
        }, UiPcChecks.TransitionIds.ToArray());
        CollectionAssert.AreEqual(new[]
        {
            "ui_pc_session.fullmotion.pc_open", "ui_pc_session.fullmotion.pc_close",
            "ui_pc_session.fullmotion.pc_reverse_open", "ui_pc_session.fullmotion.pc_reverse_close",
        }, UiPcChecks.FullMotionIds.ToArray());
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

    [Test]
    public void Recordings_AreTheSessionsRecordings_EachAfterItsFullMotionCheck()
    {
        string path = Path.Combine(SoFieldReadersTests.ProjectRoot(), "tools", "qa", "scenarios", "ui_pc_session.json");
        var steps = Regex.Matches(File.ReadAllText(path), "\\{\\s*\"op\"\\s*:\\s*\"(\\w+)\"(?:\\s*,\\s*\"name\"\\s*:\\s*\"(\\w+)\")?")
                         .Cast<Match>().Select(m => (op: m.Groups[1].Value, name: m.Groups[2].Value)).ToList();
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
