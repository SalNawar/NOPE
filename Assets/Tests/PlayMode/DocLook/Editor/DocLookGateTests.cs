using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// DOC-LOOK's play checks that read results (DocLookChecks; the re-verify of
/// cbbe2e3): each registered check needs every result it names by its exact
/// id, so it is green on its scenario's results alone and red with any one of
/// them missing, red, stale or taken on uncommitted changes (a run that
/// stopped after doclook_print's first coverHits step left 'shut' alone, and
/// the old "doclook_print.coverHits.*" passed on it). Each check's list is its
/// scenario's own steps, so a renamed or an added step cannot drop out of the
/// gate. Pure: the QA editor runner runs it offline (QA_PROJECT set for the
/// scenarios).
/// </summary>
public class DocLookGateTests
{
    /// <summary>The commit the given results were measured at, and HEAD for the checks.</summary>
    private const string Head = "c0ffee";

    /// <summary>DOC-LOOK's checks that read results, each with the results it must need, written out here (not read from DocLookChecks), with its scenario and the op whose steps make them.</summary>
    private static readonly (string Check, string Scenario, string Op, string[] Ids)[] ResultChecks =
    {
        ("doclook.play.coverHits", "doclook_print", "coverHits", new[] { "doclook_print.coverHits.shut", "doclook_print.coverHits.folder_reshut", "doclook_print.coverHits.open" }),
        ("doclook.play.capHeight", "doclook_cap", "capHeight", new[] { "doclook_cap.capHeight.a1", "doclook_cap.capHeight.a2" }),
    };

    /// <summary>DOC-LOOK's registered checks (read once).</summary>
    private static readonly Lazy<List<RegisteredCheck>> Registered =
        new Lazy<List<RegisteredCheck>>(() => GateChecks.All(new[] { typeof(DocLookChecks).Assembly }).Where(c => c.Info.Owner == "DOC-LOOK").ToList());

    [Test]
    public void EveryDocLookCheckThatReadsResults_IsListedWithItsResults()
    {
        CollectionAssert.AreEquivalent(ResultChecks.Select(c => c.Check), Registered.Value.Where(c => c.Info.Kind != GateKind.Static).Select(c => c.Info.Id));
        CollectionAssert.AreEqual(Spec("doclook.play.coverHits").Ids, DocLookChecks.CoverHitsResults);
        CollectionAssert.AreEqual(Spec("doclook.play.capHeight").Ids, DocLookChecks.CapHeightResults);
    }

    [Test]
    public void EachResultCheck_PassesOnItsResults_AndReadsNoneItDoesNotName()
    {
        foreach ((string check, _, _, string[] ids) in ResultChecks)
        {
            List<QaGateResult> results = ids.Select(id => Result(id, true)).ToList();
            GateResult green = RunCheck(check, results);
            Assert.IsTrue(green.Pass, $"{check}: {green}");
            // a red result beside each id, which a pattern over the ids would read too: a check of exact ids leaves it
            List<QaGateResult> beside = results.Concat(ids.Select(id => Result(id.Substring(0, id.LastIndexOf('.') + 1) + "other", false)))
                                               .Concat(ids.Select(id => Result(id + ".more", false))).ToList();
            GateResult still = RunCheck(check, beside);
            Assert.IsTrue(still.Pass, $"{check} read a result it does not name (a pattern?): {still}");
        }
    }

    [Test]
    public void EachResultCheck_IsRed_WithAnyOneOfItsResultsMissing_Red_Stale_OrDirty()
    {
        foreach ((string check, _, _, string[] ids) in ResultChecks)
            foreach (string one in ids)
            {
                GateResult missing = RunCheck(check, ids.Where(id => id != one).Select(id => Result(id, true)));
                Assert.IsFalse(missing.Pass, $"{check} passed without {one}");
                StringAssert.Contains($"{one}: not measured", missing.Measured, check);

                GateResult red = RunCheck(check, ids.Select(id => Result(id, id != one)));
                Assert.IsFalse(red.Pass, $"{check} passed with {one} red");
                StringAssert.Contains($"FAIL {one}", red.Detail, check);

                GateResult stale = RunCheck(check, ids.Select(id => id == one ? Result(id, true, "beef") : Result(id, true)));
                Assert.IsFalse(stale.Pass, $"{check} passed with {one} from another commit");
                StringAssert.Contains($"STALE {one}", stale.Detail, check);

                GateResult dirty = RunCheck(check, ids.Select(id => id == one ? Result(id, true, dirty: true) : Result(id, true)));
                Assert.IsFalse(dirty.Pass, $"{check} passed with {one} taken on uncommitted changes");
                StringAssert.Contains($"DIRTY {one}", dirty.Detail, check);
            }
    }

    [Test]
    public void CoverHits_ARunThatStoppedAfterItsFirstStep_IsRed_AllThreeAreGreen()
    {
        // The re-verify of cbbe2e3: a click whose target is gone throws, the runner stops, and only 'shut' is imported.
        var shutAlone = new List<QaGateResult> { Result("doclook_print.coverHits.shut", true) };
        GateResult stopped = RunCheck("doclook.play.coverHits", shutAlone);
        Assert.IsFalse(stopped.Pass, stopped.ToString());
        StringAssert.Contains("doclook_print.coverHits.folder_reshut: not measured", stopped.Measured);
        StringAssert.Contains("doclook_print.coverHits.open: not measured", stopped.Measured);
        shutAlone.Add(Result("doclook_print.coverHits.folder_reshut", true));
        Assert.IsFalse(RunCheck("doclook.play.coverHits", shutAlone).Pass, "two of three");
        shutAlone.Add(Result("doclook_print.coverHits.open", true));
        Assert.IsTrue(RunCheck("doclook.play.coverHits", shutAlone).Pass, "all three");
        Assert.IsFalse(RunCheck("doclook.play.capHeight", new[] { Result("doclook_cap.capHeight.a1", true) }).Pass, "the cap height at A1 alone");
    }

    [Test]
    public void Verdict_RefusesAPattern_AndACheckThatNamesNoResult()
    {
        var results = new[] { Result("doclook_print.coverHits.shut", true) };
        GateResult pattern = DocLookChecks.Verdict("t.check", results, Head, new[] { "doclook_print.coverHits.*" });
        Assert.IsFalse(pattern.Pass, "a pattern is never read, even when results match it");
        StringAssert.Contains("not an exact result id", pattern.Detail);
        Assert.IsTrue(DocLookChecks.Verdict("t.check", results, Head, new[] { "doclook_print.coverHits.shut" }).Pass);
        Assert.IsFalse(DocLookChecks.Verdict("t.check", results, Head, new string[0]).Pass, "a check that names no result never passes");
    }

    [Test]
    public void EachCheck_NeedsExactlyItsScenariosSteps_InOrder()
    {
        // A step renamed or added in the scenario changes the results its run writes: the check's list must follow, or this fails.
        foreach ((string check, string scenario, string op, string[] ids) in ResultChecks)
        {
            string path = Path.Combine(SoFieldReadersTests.ProjectRoot(), "tools", "qa", "scenarios", scenario + ".json");
            ContentNode root = ContentJson.Parse(File.ReadAllText(path));
            Assert.AreEqual(scenario, Text(root, "name"), path);
            List<string> made = root.Get("steps").Items.Where(s => string.Equals(Text(s, "op"), op, StringComparison.OrdinalIgnoreCase))
                                    .Select(s => $"{scenario}.{op}.{Text(s, "name")}").ToList();
            CollectionAssert.AreEqual(made, ids, $"{check}: the results {scenario}'s {op} steps write");
            CollectionAssert.AllItemsAreUnique(made, $"{scenario}: two {op} steps of one name write one result");
        }
    }

    /// <summary>The listed check <paramref name="check"/>.</summary>
    private static (string Check, string Scenario, string Op, string[] Ids) Spec(string check) => ResultChecks.Single(c => c.Check == check);

    /// <summary>The string member <paramref name="key"/> of a scenario's JSON object, or "".</summary>
    private static string Text(ContentNode node, string key)
    {
        ContentNode value = node.Get(key);
        return value != null && value.Kind == ContentNodeKind.String ? value.Text : string.Empty;
    }

    /// <summary>A result measured at <paramref name="commit"/> (default <see cref="Head"/>), on a clean tree unless <paramref name="dirty"/>.</summary>
    private static QaGateResult Result(string id, bool pass, string commit = Head, bool dirty = false) =>
        new QaGateResult { id = id, owner = "DOC-LOOK", pass = pass, measured = pass ? "ok" : "red", commit = commit, dirty = dirty };

    /// <summary>Runs the registered check <paramref name="check"/> on <paramref name="results"/> at <see cref="Head"/> (DocLookChecks.Measured swapped in for the call).</summary>
    private static GateResult RunCheck(string check, IEnumerable<QaGateResult> results)
    {
        RegisteredCheck registered = Registered.Value.Single(c => c.Info.Id == check);
        List<QaGateResult> given = results.ToList();
        Func<(IEnumerable<QaGateResult> Results, string Head)> measured = DocLookChecks.Measured;
        DocLookChecks.Measured = () => (given, Head);
        try
        {
            return registered.Run().Single();
        }
        finally
        {
            DocLookChecks.Measured = measured;
        }
    }
}
