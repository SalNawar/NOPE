using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// The screen transitions (Transit and SceneTransit, FLOW's; BUG-32a). In
/// play mode they ease over the kit's 0.2-0.3 s (TransitCurve, tested in
/// FlowRulesTests; the transitions' recordings are FLOW's QA recipes);
/// outside play mode they cut at once (the end state, then done) and load a
/// scene as SceneManager.LoadScene does, which these tests pin, as they pin
/// the scene change's shutter under the save notice. They also pin FLOW's
/// checks that read results (round 7): each registered check passes on its
/// exact results alone and is red with any one missing or (for flow.bp.load)
/// measured in the editor, and FLOW's recipes produce every result id, each
/// frozen one from a load recorded alone. Round 8: only flow.bp.load is
/// DemoOnly, so DemoGate's merge level runs every other FLOW check, and
/// flowRecLoad's sequence begins on the load's start mark and stops on its up
/// mark (FlowOps.LoadMarkStepFor). Tests in the "NeedsEditor"
/// category create Unity objects or load scenes and run only inside Unity;
/// the recipes' test reads the project (QA_PROJECT outside Unity).
/// </summary>
public class FlowSeamTests
{
    [Test]
    public void Transit_WithNoTarget_CallsDoneAtOnce()
    {
        int done = 0;
        Transit.Fade(null, 0f, 0.5f, () => done++);
        Transit.Slide(null, Vector2.one, 0.5f, () => done++);
        Assert.AreEqual(2, done);
        Assert.DoesNotThrow(() => Transit.Fade(null, 1f, 0.5f));
        Assert.DoesNotThrow(() => Transit.Slide(null, Vector2.zero, 0.5f));
    }

    [Test]
    public void TheSceneChangeShutter_SortsUnderTheSaveNotice()
    {
        // Round 4: at the same order the two overlay canvases fell back to hierarchy order, so a failed-save strip raised just before a load (RUN-SAFE's SaveNotice) could sit under the shutter.
        int veil = PrivateConstant(typeof(TransitRunner), "VeilOrder");
        int notice = PrivateConstant(typeof(SaveNotice), "SortingOrder");
        Assert.Less(veil, notice, "the shutter sorts under the save notice's strip");
    }

    /// <summary>
    /// FLOW's checks that read results, each with the exact results it requires
    /// (docs/pending/FLOW.md §1, Gate checks). The tests below run the
    /// registered checks themselves on given results (FlowChecks.Measured), so
    /// a check that goes back to a pattern or loses an id fails here, and they
    /// check that FLOW's recipes produce every id (round 7).
    /// </summary>
    private static readonly (string Check, bool BuiltPlayer, string[] Ids)[] ResultChecks =
    {
        ("flow.esc.office", false, new[] { "flow_pause.settings.office" }),
        ("flow.esc.pc", false, new[] { "flow_pause.settings.pcEsc" }),
        ("flow.esc.home", false, new[] { "flow_pause.settings.home" }),
        ("flow.esc.title", false, new[] { "flow_pause.settings.title" }),
        ("flow.focus", false, new[] { "flow_pause.focus" }),
        ("flow.payoff", false, new[] { "flow_transitions.payoff" }),
        ("flow.transitions.cuts", false, new[] { "flow_transitions.seq.dayend.cuts", "flow_transitions.seq.gohome.cuts", "flow_transitions.seq.sleep.cuts" }),
        ("flow.transit.shutter", false, new[] { "flow_shutter.transit.TitleScene", "flow_shutter.transit.office", "flow_shutter.transit.queue.titleOfficeTitle" }),
        ("flow.endings", false, new[] { "flow_endings.edition" }),
        ("flow.night.close", false, new[] { "flow_night.closed.midnight" }),
        ("flow.night.bell", false, new[] { "flow_night.bell.early" }),
        ("flow.skip.midshift", false, new[] { "flow_skip.same.midshift", "flow_skip.dayEnds.midshift", "flow_skip.same.doubleskip", "flow_skip.dayEnds.doubleskip" }),
        ("flow.bp.load", true, new[]
        {
            "bp.flow_load.load.title_office.frames", "bp.flow_load.load.office_home.frames", "bp.flow_load.load.home_office.frames",
            "bp.flow_load.load.title_office.time", "bp.flow_load.load.home_office.time",
            "bp.flow_load_seq.seq.title_office.frozen", "bp.flow_load_seq.seq.office_home.frozen", "bp.flow_load_seq.seq.home_office.frozen",
        }),
    };

    /// <summary>The commit the given results were measured at, and HEAD for the checks.</summary>
    private const string Head = "c0ffee";

    /// <summary>FLOW's registered checks (read once).</summary>
    private static readonly Lazy<List<RegisteredCheck>> FlowRegistered =
        new Lazy<List<RegisteredCheck>>(() => GateChecks.All(new[] { typeof(FlowChecks).Assembly }).Where(c => c.Info.Owner == "FLOW").ToList());

    [Test]
    public void EveryFlowCheckThatReadsResults_IsListedWithItsResults_AndOnlyTheBuiltPlayersIsDemoOnly()
    {
        List<RegisteredCheck> reading = FlowRegistered.Value.Where(c => c.Info.Kind != GateKind.Static).ToList();
        CollectionAssert.AreEquivalent(ResultChecks.Select(c => c.Check), reading.Select(c => c.Info.Id));
        foreach (RegisteredCheck c in reading)
            Assert.AreEqual(c.Info.Kind == GateKind.BuiltPlayer, Spec(c.Info.Id).BuiltPlayer, $"{c.Info.Id}: a built-player check exactly when it requires a built player's results");
        // Round 8: DemoGate's merge level skips a DemoOnly check unread, so a check marked DemoOnly by mistake passes every merge.
        // Every FLOW check, the static ones included, is DemoOnly exactly when it is the built player's (flow.bp.load).
        Assert.IsTrue(FlowRegistered.Value.Any(c => c.Info.Kind == GateKind.Static), "FLOW's static checks are read too");
        foreach (RegisteredCheck c in FlowRegistered.Value)
            Assert.AreEqual(c.Info.Kind == GateKind.BuiltPlayer, c.Info.DemoOnly, $"{c.Info.Id}: DemoOnly exactly when it is a built-player check (only flow.bp.load waits for the demo gate)");
    }

    [Test]
    public void ALoadRecording_BeginsOnTheStartMark_AndStopsOnTheUpMark_ReadingNoOtherMark()
    {
        // Round 8: flowRecLoad's sequence is the load alone, from the start mark (the click's frame) to the up mark (the shutter off).
        // A sequence begun on the rise would hold only the rise (no lowered-shutter frame read), and one never stopped runs on over the settled scene.
        const string T = TransitRunner.TransitSignal;
        Assert.AreEqual("start:TitleScene .. up:TitleScene", Recorded((T, "start:TitleScene"), (T, "rise:TitleScene"), (T, "up:TitleScene")));
        // Another channel's marks, and nameless ones, are not the load's.
        Assert.AreEqual("none", Recorded(("other", "start:TitleScene"), (T, null), (T, string.Empty), ("other", "up:TitleScene")));
        Assert.AreEqual("start:TitleScene .. not stopped", Recorded((T, "start:TitleScene"), (T, "rise:TitleScene"), ("other", "up:TitleScene")));
        // A queued load (flowQueue's marks): the second start does not begin again, the sequence runs on to the first up mark, and the recording is disarmed after it.
        Assert.AreEqual("start:A .. up:B", Recorded((T, "start:A"), (T, "start:B"), (T, "rise:B"), (T, "up:B"), (T, "start:C"), (T, "up:C")));
        // Armed during a change: its rise and up begin nothing, and the next load's start does.
        Assert.AreEqual("start:B .. up:B", Recorded((T, "rise:A"), (T, "up:A"), (T, "start:B"), (T, "rise:B"), (T, "up:B")));
        Assert.AreEqual(FlowOps.LoadMarkStep.Ignore, FlowOps.LoadMarkStepFor(T, "rise:A", true), "the rise neither begins nor stops a sequence");
        Assert.AreEqual(FlowOps.LoadMarkStep.Ignore, FlowOps.LoadMarkStepFor(T, "up:A", false), "an up mark before any start stops nothing");
    }

    /// <summary>
    /// The span an armed load recording records over <paramref name="marks"/>,
    /// as FlowOps steps it (FlowOps.LoadMarkStepFor; the recording is disarmed
    /// once it stops): "&lt;begin mark&gt; .. &lt;stop mark&gt;", ".. not stopped", or "none".
    /// </summary>
    private static string Recorded(params (string Channel, string Name)[] marks)
    {
        string begin = null;
        foreach ((string channel, string name) in marks)
            switch (FlowOps.LoadMarkStepFor(channel, name, begin != null))
            {
                case FlowOps.LoadMarkStep.Begin:
                    begin = name;
                    break;
                case FlowOps.LoadMarkStep.Stop:
                    return $"{begin} .. {name}";
            }
        return begin == null ? "none" : $"{begin} .. not stopped";
    }

    [Test]
    public void EachResultCheck_PassesOnItsResults_AndReadsNoneItDoesNotName()
    {
        foreach ((string check, bool bp, string[] ids) in ResultChecks)
        {
            List<QaGateResult> results = ids.Select(id => Result(id, true, bp)).ToList();
            GateResult green = RunCheck(check, results);
            Assert.IsTrue(green.Pass, $"{check}: {green}");
            // A failing result beside each id, which a pattern over the ids would read too: a check of exact ids ignores it.
            List<QaGateResult> beside = results.Concat(ids.Select(id => Result(id.Substring(0, id.LastIndexOf('.') + 1) + "other", false, bp)))
                                               .Concat(ids.Select(id => Result(id + ".more", false, bp))).ToList();
            GateResult still = RunCheck(check, beside);
            Assert.IsTrue(still.Pass, $"{check} read a result it does not name (a pattern?): {still}");
        }
    }

    [Test]
    public void EachResultCheck_IsRed_WithAnyOneOfItsResultsMissing()
    {
        foreach ((string check, bool bp, string[] ids) in ResultChecks)
            foreach (string missing in ids)
            {
                GateResult g = RunCheck(check, ids.Where(id => id != missing).Select(id => Result(id, true, bp)));
                Assert.IsFalse(g.Pass, $"{check} passed without {missing}");
                StringAssert.Contains($"{missing}: not measured", g.Measured, check);
            }
    }

    [Test]
    public void SkipMidShift_WithOnlyTheMidshiftPair_IsNotMeasured_WithAllFour_Passes()
    {
        // Round 6: a flow_skip run that stopped in the double skip leaves only the midshift pair.
        var pair = new List<QaGateResult> { Result("flow_skip.same.midshift", true, false), Result("flow_skip.dayEnds.midshift", true, false) };
        GateResult stopped = RunCheck("flow.skip.midshift", pair);
        Assert.IsFalse(stopped.Pass);
        StringAssert.Contains("flow_skip.same.doubleskip: not measured", stopped.Measured);
        StringAssert.Contains("flow_skip.dayEnds.doubleskip: not measured", stopped.Measured);
        pair.Add(Result("flow_skip.same.doubleskip", true, false));
        pair.Add(Result("flow_skip.dayEnds.doubleskip", true, false));
        Assert.IsTrue(RunCheck("flow.skip.midshift", pair).Pass);
    }

    [Test]
    public void BpLoad_IsRed_WhenOneOfItsResultsWasMeasuredInTheEditor()
    {
        string[] ids = Spec("flow.bp.load").Ids;
        foreach (string inEditor in ids)
        {
            GateResult g = RunCheck("flow.bp.load", ids.Select(id => Result(id, true, id != inEditor)));
            Assert.IsFalse(g.Pass, inEditor);
            StringAssert.Contains($"NOT-BP {inEditor} was measured in the editor", g.Detail);
        }
    }

    [Test]
    public void Verdict_RefusesAPattern_AndANamelessCheck_AndJudgesByCommit()
    {
        var results = new[] { Result("flow_skip.same.midshift", true, false) };
        GateResult pattern = FlowChecks.Verdict("t.check", false, results, Head, "flow_skip.same.*");
        Assert.IsFalse(pattern.Pass, "a pattern is never read, even when results match it");
        StringAssert.Contains("not an exact result id", pattern.Detail);
        Assert.IsTrue(FlowChecks.Verdict("t.check", false, results, Head, "flow_skip.same.midshift").Pass);
        Assert.IsFalse(FlowChecks.Verdict("t.check", false, results, Head).Pass, "a check that names no result never passes");
        GateResult stale = FlowChecks.Verdict("t.check", false, results, "beef", "flow_skip.same.midshift");
        Assert.IsFalse(stale.Pass);
        StringAssert.Contains("STALE flow_skip.same.midshift", stale.Detail);
    }

    [Test]
    public void EachResultTheChecksRequire_IsProducedByFlowsRecipes_EveryFrozenOneOverALoadAlone()
    {
        // The result ids each step of FLOW's recipes produces (Produced); a built player's run adds "bp." (run_player).
        string folder = Path.Combine(SoFieldReadersTests.ProjectRoot(), "tools", "qa", "scenarios");
        var produced = new HashSet<string>(StringComparer.Ordinal);
        foreach (string file in Directory.GetFiles(folder, "flow_*.json"))
            produced.UnionWith(Produced(ContentJson.Parse(File.ReadAllText(file))));
        Assert.IsNotEmpty(produced, "FLOW's recipes are read");
        foreach ((string check, bool bp, string[] ids) in ResultChecks)
            foreach (string id in ids)
            {
                Assert.AreEqual(bp, id.StartsWith("bp.", StringComparison.Ordinal), $"{check}: {id} is a built player's result exactly when the check is a built-player one");
                Assert.IsTrue(produced.Contains(bp ? id.Substring(3) : id),
                    $"{check} requires {id}, which no step of FLOW's recipes produces (a frozen result comes only from a flowRecLoad sequence: the load alone)");
            }
    }

    /// <summary>
    /// The result ids a FLOW recipe produces: each FLOW op's assertion as
    /// FlowOps names it, prefixed by QaRunner with the scenario's name; and for
    /// each recorded sequence the seqtools results its recipe writes
    /// (default_id "&lt;scenario&gt;.seq.&lt;name&gt;.&lt;analysis&gt;"): cuts for every
    /// sequence, frozen only for a load recorded alone (flowRecLoad, round 7),
    /// since a recStart sequence keeps a lead before the click and a tail on
    /// the settled scene that may hold still.
    /// </summary>
    private static IEnumerable<string> Produced(ContentNode scenario)
    {
        string name = Text(scenario, "name");
        foreach (ContentNode step in scenario.Get("steps").Items)
        {
            string n = Text(step, "name");
            switch (Text(step, "op").ToLowerInvariant())
            {
                case "flowsettings": yield return $"{name}.settings.{n}"; break;
                case "flowfocus": yield return $"{name}.focus"; break;
                case "flowpayoff": yield return $"{name}.payoff"; break;
                case "flowedition": yield return $"{name}.edition"; break;
                case "flowclosed": yield return $"{name}.closed.{n}"; break;
                case "flowearlyfinish": yield return $"{name}.bell.{n}"; break;
                case "flowtransit": yield return $"{name}.transit.{n}"; break;
                case "flowqueue": yield return $"{name}.transit.queue.{n}"; break;
                case "flowloadtime":
                    yield return $"{name}.load.{n}.frames";
                    yield return $"{name}.load.{n}.time";
                    break;
                case "flowrunstate":
                    // Only a record compared with an earlier one asserts.
                    if (Text(step, "text").Length > 0)
                        yield return $"{name}.same.{n}";
                    break;
                case "flowdayends": yield return $"{name}.dayEnds.{n}"; break;
                case "recstart":
                case "record":
                    yield return $"{name}.seq.{n}.cuts";
                    break;
                case "flowrecload":
                    yield return $"{name}.seq.{n}.cuts";
                    yield return $"{name}.seq.{n}.frozen";
                    break;
            }
        }
    }

    /// <summary>The string member <paramref name="key"/> of a scenario's JSON object, or "".</summary>
    private static string Text(ContentNode node, string key)
    {
        ContentNode value = node.Get(key);
        return value != null && value.Kind == ContentNodeKind.String ? value.Text : string.Empty;
    }

    /// <summary>The listed check <paramref name="check"/>.</summary>
    private static (string Check, bool BuiltPlayer, string[] Ids) Spec(string check) => ResultChecks.Single(c => c.Check == check);

    /// <summary>A result measured at <see cref="Head"/> on a clean tree.</summary>
    private static QaGateResult Result(string id, bool pass, bool builtPlayer) =>
        new QaGateResult { id = id, owner = "FLOW", pass = pass, measured = pass ? "ok" : "red", commit = Head, builtPlayer = builtPlayer };

    /// <summary>Runs the registered check <paramref name="check"/> on <paramref name="results"/> at <see cref="Head"/> (FlowChecks.Measured swapped in for the call).</summary>
    private static GateResult RunCheck(string check, IEnumerable<QaGateResult> results)
    {
        RegisteredCheck registered = FlowRegistered.Value.Single(c => c.Info.Id == check);
        List<QaGateResult> given = results.ToList();
        Func<(IEnumerable<QaGateResult> Results, string Head)> measured = FlowChecks.Measured;
        FlowChecks.Measured = () => (given, Head);
        try
        {
            return registered.Run().Single();
        }
        finally
        {
            FlowChecks.Measured = measured;
        }
    }

    [Test, Category("NeedsEditor")]
    public void Transit_OutsidePlay_WaitsNoTime_AndTheKitLengthIsInRange()
    {
        int done = 0;
        Transit.Wait(2f, () => done++);
        Assert.AreEqual(1, done);
        Assert.That(Transit.Seconds, Is.InRange(TransitCurve.MinSeconds, TransitCurve.MaxSeconds));
    }

    [Test, Category("NeedsEditor")]
    public void Transit_OutsidePlay_CutsToTheEndState_ThenCallsDone()
    {
        var go = new GameObject("FlowSeamTests", typeof(RectTransform), typeof(CanvasGroup));
        try
        {
            var group = go.GetComponent<CanvasGroup>();
            var rect = go.GetComponent<RectTransform>();
            float alphaAtDone = -1f;
            Vector2 positionAtDone = Vector2.zero;
            Transit.Fade(group, 0.25f, 1f, () => alphaAtDone = group.alpha);
            Transit.Slide(rect, new Vector2(12f, -7f), 1f, () => positionAtDone = rect.anchoredPosition);
            Assert.AreEqual(0.25f, alphaAtDone, 1e-5f);
            Assert.AreEqual(new Vector2(12f, -7f), positionAtDone);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(go);
        }
    }

    [Test, Category("NeedsEditor")]
    public void SceneTransit_OutsidePlay_LoadsAsSceneManagerDoes()
    {
        const string scene = "__FlowSeamTests_NoSuchScene__";
        bool ignored = LogAssert.ignoreFailingMessages;
        LogAssert.ignoreFailingMessages = true;
        try
        {
            Assert.AreEqual(Outcome(() => SceneManager.LoadScene(scene)), Outcome(() => SceneTransit.Load(scene)));
        }
        finally
        {
            LogAssert.ignoreFailingMessages = ignored;
        }
    }

    [Test, Category("NeedsEditor")]
    public void SealArt_PaintsEachOutlineOnce_TheSecondRequestIsTheSameTexture()
    {
        // BUG-31a: the seals are painted once a session (the office's load spent most of its time painting them).
        foreach (SealShape shape in (SealShape[])Enum.GetValues(typeof(SealShape)))
        {
            Texture2D first = SealArt.Texture(shape);
            Assert.IsNotNull(first, shape.ToString());
            Assert.AreSame(first, SealArt.Texture(shape), $"{shape}: no repaint on the second request");
        }
    }

    [Test, Category("NeedsEditor")]
    public void DaterImpressionArt_AWordTheRubberCannotPrint_PrintsTheEnglishVerdict()
    {
        // BUG-38b: a Greek (or Arabic, Japanese, Chinese) verdict word printed blank; the bureau's rubber prints its English word.
        Texture2D greek = DaterImpressionArt.Paint("ΕΓΚΡΙΘΗΚΕ", DaterImpressionArt.ApprovedInk, "", "", null, 1f, 7, false);
        Texture2D english = DaterImpressionArt.Paint("APPROVED", DaterImpressionArt.ApprovedInk, "", "", null, 1f, 7, false);
        Texture2D arabic = DaterImpressionArt.Paint("مرفوض", DaterImpressionArt.DeniedInk, "", "", null, 1f, 7, false);
        Texture2D denied = DaterImpressionArt.Paint("DENIED", DaterImpressionArt.DeniedInk, "", "", null, 1f, 7, false);
        try
        {
            CollectionAssert.AreEqual(english.GetPixels32(), greek.GetPixels32(), "the Greek word prints APPROVED");
            CollectionAssert.AreEqual(denied.GetPixels32(), arabic.GetPixels32(), "the Arabic word prints DENIED");
            Assert.IsTrue(Array.Exists(english.GetPixels32(), p => p.a > 0), "the word is inked");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(greek);
            UnityEngine.Object.DestroyImmediate(english);
            UnityEngine.Object.DestroyImmediate(arabic);
            UnityEngine.Object.DestroyImmediate(denied);
        }
    }

    [Test, Category("NeedsEditor")]
    public void DaterImpressionArt_TheVerdictNotTheInk_ChoosesTheWordARubberCannotPrintFallsBackTo()
    {
        // A tinted ink with the verdict passed prints that verdict's English word; an ink that is neither verdict's is refused, never read as DENIED.
        var tint = new Color32(60, 140, 200, 255);
        Texture2D tinted = DaterImpressionArt.Paint("ΕΓΚΡΙΘΗΚΕ", true, tint, "", "", null, 1f, 7, false);
        Texture2D approved = DaterImpressionArt.Paint("APPROVED", true, DaterImpressionArt.ApprovedInk, "", "", null, 1f, 7, false);
        try
        {
            Color32[] a = tinted.GetPixels32(), b = approved.GetPixels32();
            Assert.AreEqual(b.Length, a.Length);
            for (int i = 0; i < a.Length; i++)
                Assert.AreEqual(b[i].a, a[i].a, $"pixel {i}: the tinted approval prints APPROVED's outline");
            Assert.Throws<ArgumentException>(() => DaterImpressionArt.Paint("APPROVED", tint, "", "", null, 1f, 7, false));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(tinted);
            UnityEngine.Object.DestroyImmediate(approved);
        }
    }

    [Test, Category("NeedsEditor")]
    public void DaterImpressionArt_TheSecondPrintOfTheSameTexts_ReusesItsLayers_AndANewDateReusesTheWord()
    {
        // BUG-31a: a print's clean layers are painted once per texts, and the word's outline (the costly layer) once per word.
        IDictionary layers = PrivateCache("Cache"), words = PrivateCache("Words");
        layers.Clear();
        words.Clear();
        var made = new List<Texture2D>();
        try
        {
            made.Add(DaterImpressionArt.Paint("APPROVED", true, DaterImpressionArt.ApprovedInk, "20 OCT 2150", "BY: 7", null, 1f, 1));
            Assert.AreEqual(1, layers.Count);
            Assert.AreEqual(1, words.Count);
            object first = Single(layers);
            made.Add(DaterImpressionArt.Paint("APPROVED", true, DaterImpressionArt.ApprovedInk, "20 OCT 2150", "BY: 7", null, 0.6f, 2));
            Assert.AreEqual(1, layers.Count, "the same texts: no repaint");
            Assert.AreSame(first, Single(layers));
            object word = Single(words);
            made.Add(DaterImpressionArt.Paint("APPROVED", true, DaterImpressionArt.ApprovedInk, "21 OCT 2150", "BY: 7", null, 1f, 3));
            Assert.AreEqual(2, layers.Count, "a new date paints its own layers");
            Assert.AreEqual(1, words.Count, "the word is not painted again");
            Assert.AreSame(word, Single(words));
        }
        finally
        {
            foreach (Texture2D t in made)
                UnityEngine.Object.DestroyImmediate(t);
        }
    }

    /// <summary>DaterImpressionArt's private static cache <paramref name="name"/> (the tests read it; nothing else does).</summary>
    private static IDictionary PrivateCache(string name)
    {
        FieldInfo field = typeof(DaterImpressionArt).GetField(name, BindingFlags.NonPublic | BindingFlags.Static);
        Assert.IsNotNull(field, name);
        return (IDictionary)field.GetValue(null);
    }

    /// <summary>The private int constant <paramref name="name"/> of <paramref name="type"/> (the tests read it; nothing else does).</summary>
    private static int PrivateConstant(Type type, string name)
    {
        FieldInfo field = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Static);
        Assert.IsNotNull(field, $"{type.Name}.{name}");
        Assert.IsTrue(field.IsLiteral, $"{type.Name}.{name} is a constant");
        return (int)field.GetRawConstantValue();
    }

    /// <summary>The only value of <paramref name="cache"/>.</summary>
    private static object Single(IDictionary cache)
    {
        Assert.AreEqual(1, cache.Count);
        foreach (DictionaryEntry entry in cache)
            return entry.Value;
        return null;
    }

    /// <summary>What a call does, as text: the exception it throws (type and message), or "" when it returns.</summary>
    private static string Outcome(Action call)
    {
        try
        {
            call();
            return string.Empty;
        }
        catch (Exception e)
        {
            return $"{e.GetType().FullName}: {e.Message}";
        }
    }
}
