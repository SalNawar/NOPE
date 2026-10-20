using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// FLOW's pure rules: the transition kit's timing (BUG-32a), the Esc key's
/// "let go first" chain (D20), how a shift ends (BUG-28b, BUG-32a, BUG-56a),
/// the dater rubber's word (BUG-38b) and the seals' outlines resolved once
/// (BUG-31a).
/// </summary>
public class FlowRulesTests
{
    // --- SealOutlines.Resolve (BUG-31a: the name is looked up once per seal, not per sample) ---

    [Test]
    public void SealOutlines_EveryShapeResolves_AndInksExactlyAsByName()
    {
        foreach (SealShape shape in (SealShape[])System.Enum.GetValues(typeof(SealShape)))
        {
            string name = shape.ToString();
            SealOutlines.Outline outline = SealOutlines.Resolve(name);
            Assert.IsTrue(outline.Exists, name);
            SealOutlines.Outline lower = SealOutlines.Resolve(name.ToLowerInvariant());
            int inked = 0;
            for (int i = -20; i <= 20; i++)
                for (int j = -20; j <= 20; j++)
                {
                    float x = i / 20f, y = j / 20f;
                    bool ink = outline.Inked(x, y);
                    Assert.AreEqual(SealOutlines.Inked(name, x, y), ink, $"{name} at ({x}, {y})");
                    Assert.AreEqual(ink, lower.Inked(x, y), $"{name}: the name's case does not matter");
                    if (ink)
                        inked++;
                }
            Assert.Greater(inked, 0, $"{name} inks its band");
            Assert.IsFalse(outline.Inked(0f, 0f), $"{name}: the middle is left for the legend");
        }
    }

    [Test]
    public void SealOutlines_AnUnknownName_ResolvesToAnOutlineThatInksNothing()
    {
        foreach (string name in new[] { "Blob", "", null })
        {
            SealOutlines.Outline outline = SealOutlines.Resolve(name);
            Assert.IsFalse(outline.Exists, name ?? "null");
            Assert.IsFalse(outline.Inked(0.95f, 0f), name ?? "null");
        }
        Assert.IsFalse(default(SealOutlines.Outline).Inked(0.95f, 0f));
    }

    // --- TransitCurve (BUG-32a: one shared kit, 0.2-0.3 s, never a hard cut) ---

    [Test]
    public void TransitCurve_LeavesAndLandsAtRest_AndNeverGoesBack()
    {
        Assert.AreEqual(0f, TransitCurve.Ease(0f));
        Assert.AreEqual(1f, TransitCurve.Ease(1f));
        Assert.AreEqual(0.5f, TransitCurve.Ease(0.5f), 1e-6f);
        Assert.AreEqual(0f, TransitCurve.Ease(-1f));
        Assert.AreEqual(1f, TransitCurve.Ease(2f));
        Assert.AreEqual(0f, TransitCurve.Ease(float.NaN));
        float last = 0f;
        for (int i = 1; i <= 100; i++)
        {
            float v = TransitCurve.Ease(i / 100f);
            Assert.GreaterOrEqual(v, last, "monotonic");
            last = v;
        }
        Assert.Less(TransitCurve.Ease(0.05f), 0.05f, "eases out of rest (no pop at the start)");
        Assert.Greater(TransitCurve.Ease(0.95f), 0.95f, "eases into rest (no pop at the end)");
    }

    [Test]
    public void TransitCurve_At_IsTheShareOfTheLengthGone_AndANoLengthOneIsDone()
    {
        Assert.AreEqual(0f, TransitCurve.At(0f, 0.25f));
        Assert.AreEqual(0.5f, TransitCurve.At(0.125f, 0.25f), 1e-6f);
        Assert.AreEqual(1f, TransitCurve.At(0.3f, 0.25f));
        Assert.AreEqual(1f, TransitCurve.At(0f, 0f));
        Assert.AreEqual(1f, TransitCurve.At(0f, -1f));
    }

    [Test]
    public void TransitCurve_CutsAtOnceOnlyOutsidePlay_OrForNoLength()
    {
        Assert.IsTrue(TransitCurve.Instant(0.25f, playing: false), "edit mode and the EditMode tests: the end state at once");
        Assert.IsTrue(TransitCurve.Instant(0f, playing: true));
        Assert.IsFalse(TransitCurve.Instant(0.25f, playing: true));
    }

    [TestCase(0.25f, 0.25f)]
    [TestCase(0.05f, 0.2f)]
    [TestCase(1f, 0.3f)]
    [TestCase(float.NaN, 0.25f)]
    public void TransitCurve_KitSeconds_StaysInTheKitsRange(float tuned, float expected)
    {
        Assert.AreEqual(expected, TransitCurve.KitSeconds(tuned), 1e-6f);
    }

    // --- EscapeChain (D20: Esc lets go of a hold first, then a track's step, then opens Settings) ---

    [Test]
    public void EscapeChain_TheFirstStepThatLetsGo_EndsThePress_InOrder()
    {
        var chain = new EscapeChain();
        var ran = new List<string>();
        bool holding = true, ringOpen = true;
        chain.Register("ring", EscapeChain.TrackOrder, () => { ran.Add("ring"); if (!ringOpen) return false; ringOpen = false; return true; });
        chain.Register("hold", EscapeChain.HoldOrder, () => { ran.Add("hold"); if (!holding) return false; holding = false; return true; });

        Assert.AreEqual("hold", chain.LetGo(), "the hold is cancelled first, whatever the registration order");
        CollectionAssert.AreEqual(new[] { "hold" }, ran);
        Assert.AreEqual("ring", chain.LetGo());
        Assert.IsNull(chain.LetGo(), "nothing left to let go of: Settings opens");
        Assert.AreEqual(2, chain.Count);
    }

    [Test]
    public void EscapeChain_TiesKeepTheirRegistrationOrder_AndARegisterAgainReplaces()
    {
        var chain = new EscapeChain();
        chain.Register("a", 5, () => true);
        chain.Register("b", 5, () => true);
        Assert.AreEqual("a", chain.LetGo());
        chain.Register("a", 5, () => false);
        Assert.AreEqual(2, chain.Count, "one step per name");
        Assert.AreEqual("b", chain.LetGo(), "the replaced step lets go of nothing");
        chain.Unregister("b");
        chain.Unregister("nobody");
        Assert.IsNull(chain.LetGo());
        Assert.IsTrue(chain.Has("a"));
        Assert.IsFalse(chain.Has("b"));
        chain.Clear();
        Assert.AreEqual(0, chain.Count);
        Assert.IsNotNull(EscapeChain.Shared);
        Assert.Throws<System.ArgumentException>(() => chain.Register("", 0, () => true));
        Assert.Throws<System.ArgumentNullException>(() => chain.Register("x", 0, null));
    }

    [Test]
    public void EscapeChain_EscNeverLeavesAMode_ItOpensSettings_RightClickStillBacksOut()
    {
        // R3: "Esc opens the same Settings window everywhere ... To leave the PC, press Q or click the bezel"; D20: Esc opens Settings.
        var modes = new[]
        {
            new ControlState(pcOpen: true),
            new ControlState(wheelOpen: true),
            new ControlState(valueHeld: true),
            new ControlState(inspecting: true),
            new ControlState(cityView: true),
            new ControlState(deskView: true),
            new ControlState(pcOpen: true, inspecting: true, deskView: true),
        };
        foreach (ControlState s in modes)
        {
            Assert.IsTrue(EscapeChain.KeepsTheOfficeOut(s, desktopTookIt: false, rightClick: false), $"{ControlRules.BackOut(s)}: Esc keeps the mode, Settings opens");
            Assert.IsFalse(EscapeChain.KeepsTheOfficeOut(s, desktopTookIt: false, rightClick: true), "a right-click still backs out");
            Assert.IsFalse(EscapeChain.KeepsTheOfficeOut(s, desktopTookIt: true, rightClick: false), "the PC's own chain took the press (a menu, a field)");
        }
        Assert.IsFalse(EscapeChain.KeepsTheOfficeOut(new ControlState(pcOpen: true, dragging: true), false, false), "a drag is a hold: the back-out cancels it first");
        Assert.IsFalse(EscapeChain.KeepsTheOfficeOut(new ControlState(), false, false), "nothing to leave");
        Assert.IsFalse(EscapeChain.KeepsTheOfficeOut(new ControlState(pcOpen: true, newsletter: true), false, false), "under a newsletter nothing backs out");
    }

    // --- ShiftEnd (BUG-28b: never abandon a decided slot; BUG-32a: payoff first; BUG-56a: the ending at the day's end) ---

    [Test]
    public void ShiftEnd_ClosingFinishesTheSlot_WhileATravellerStands_OrADecidedOnesVerdictShows()
    {
        Assert.AreEqual(ClosingAction.FinishCurrent, ShiftEnd.OnClosing(travellerAtDesk: true, verdictShowing: false));
        Assert.AreEqual(ClosingAction.FinishCurrent, ShiftEnd.OnClosing(travellerAtDesk: false, verdictShowing: true), "the citation slip is never covered by the report");
        Assert.AreEqual(ClosingAction.CloseNow, ShiftEnd.OnClosing(travellerAtDesk: false, verdictShowing: false));
    }

    [Test]
    public void ShiftEnd_TheReportWaitsForTheLastTravellersPayoff_UpToTheCap()
    {
        Assert.IsTrue(ShiftEnd.PayoffDone(departing: false, 0f, 8f), "nobody leaving: at once");
        Assert.IsFalse(ShiftEnd.PayoffDone(departing: true, 1.5f, 8f), "the reaction and walk-off play first");
        Assert.IsTrue(ShiftEnd.PayoffDone(departing: true, 8f, 8f), "never stuck: the cap");
        Assert.IsTrue(ShiftEnd.PayoffDone(departing: true, 0f, 0f));
        Assert.IsTrue(ShiftEnd.PayoffDone(departing: true, 0f, float.NaN), "a cap that is not a number never holds the report");
    }

    [Test]
    public void ShiftEnd_ARunWithAnEnding_EndsTheDayInIt_NotAtHome()
    {
        Assert.IsTrue(ShiftEnd.EndsTheRun("fired"));
        Assert.IsFalse(ShiftEnd.EndsTheRun(string.Empty));
        Assert.IsFalse(ShiftEnd.EndsTheRun(null));
    }

    [Test]
    public void ShiftEnd_AShiftClosedElsewhere_OrAnOfficeBeingReplaced_ClosesNothingAgain()
    {
        Assert.IsFalse(ShiftEnd.LeftBehind(shiftRunning: true, sceneChanging: false), "a running shift closes and ends its own day");
        Assert.IsTrue(ShiftEnd.LeftBehind(shiftRunning: false, sceneChanging: false), "a cheat's route closed it (or its ending showed at once): no second close on the landing morning");
        Assert.IsTrue(ShiftEnd.LeftBehind(shiftRunning: true, sceneChanging: true), "a restart or a jump back replaces the office behind the shutter: its day loop must not save over the new morning");
        Assert.IsTrue(ShiftEnd.LeftBehind(shiftRunning: false, sceneChanging: true));
    }

    // --- SceneChangeQueue (round 4: a scene asked for during a change is never dropped; the latest wins) ---

    [Test]
    public void SceneChangeQueue_ARequestDuringAChange_Waits_AndTheLatestWins()
    {
        var q = new SceneChangeQueue();
        Assert.IsFalse(q.Changing);
        Assert.IsTrue(q.Request("OfficeScene"), "nothing runs: the change starts now");
        Assert.IsTrue(q.Changing);
        Assert.IsFalse(q.Request("HomeScene"), "one runs: it waits");
        Assert.IsFalse(q.Request("OfficeScene"), "a later request replaces the waiting one");
        Assert.AreEqual("OfficeScene", q.Queued);

        string scene = "first";
        Assert.IsTrue(q.TakeQueued(ref scene));
        Assert.AreEqual("OfficeScene", scene, "the run's world matches the last request: that scene loads");
        Assert.IsNull(q.Queued);
        Assert.IsTrue(q.Changing, "the change goes on with the scene taken");
    }

    [Test]
    public void SceneChangeQueue_NothingWaiting_KeepsTheScene_AndAFinishedChangeTakesANewRequestAtOnce()
    {
        var q = new SceneChangeQueue();
        Assert.IsTrue(q.Request("TitleScene"));
        string scene = "TitleScene";
        Assert.IsFalse(q.TakeQueued(ref scene));
        Assert.AreEqual("TitleScene", scene);
        q.Finish();
        Assert.IsFalse(q.Changing);
        Assert.IsTrue(q.Request("OfficeScene"), "after the shutter is off, the next request starts its own change");
    }

    // --- ShutterRoll (the bible's G3/G4: the booth shutter's 1.2 s roll; J5/BUG-31a: the veil's light never stops) ---

    [Test]
    public void ShutterRoll_UpEasesOut_DownEasesIn_OverTheBiblesLength()
    {
        Assert.AreEqual(1.2f, ShutterRoll.DefaultSeconds, 1e-6f, "G3/G4: 1.2 s");
        float s = ShutterRoll.DefaultSeconds;
        Assert.AreEqual(0f, ShutterRoll.At(0f, true, 0f, s), 1e-6f);
        Assert.AreEqual(1f, ShutterRoll.At(0f, true, s, s), 1e-6f);
        Assert.AreEqual(1f, ShutterRoll.At(1f, false, 0f, s), 1e-6f);
        Assert.AreEqual(0f, ShutterRoll.At(1f, false, s, s), 1e-6f);
        Assert.Greater(ShutterRoll.At(0f, true, s / 2f, s), 0.85f, "OutCubic: up fast, then settles (0.875 at half time)");
        Assert.Less(ShutterRoll.At(1f, false, s / 2f, s), 0.9f, "InCubic: down slow at first");
        Assert.Greater(ShutterRoll.At(1f, false, s / 2f, s), 0.85f, "InCubic: 0.875 still open at half time, then gathers speed into the clang");
        float last = 0f;
        for (int i = 1; i <= 36; i++)
        {
            float open = ShutterRoll.At(0f, true, i * s / 36f, s);
            Assert.GreaterOrEqual(open, last, "never goes back");
            last = open;
        }
        Assert.AreEqual(0.5f + 0.5f * 0.875f, ShutterRoll.At(0.5f, true, s / 2f, s), 1e-5f, "a roll from part-way covers the rest");
        Assert.AreEqual(1f, ShutterRoll.At(0f, true, 0f, 0f), "no length: the end at once");
        Assert.IsTrue(ShutterRoll.Done(s, s));
        Assert.IsFalse(ShutterRoll.Done(s / 2f, s));
        Assert.IsTrue(ShutterRoll.Done(0f, 0f));
    }

    [Test]
    public void ShutterRoll_TheVeilsLightMovesEveryFrame_AndWraps()
    {
        // The [BP] pass "no frame repeated without motion > 0.25 s": at 30 fps every frame moves the light on, Reduced Motion too.
        foreach (bool reduced in new[] { false, true })
        {
            float phase = 0f;
            for (int frame = 0; frame < 300; frame++)
            {
                float next = ShutterRoll.Sweep(phase, 1f / 30f, reduced);
                Assert.That(next, Is.InRange(0f, 1f));
                float moved = next >= phase ? next - phase : next + 1f - phase;
                Assert.Greater(moved, 0.01f, $"frame {frame}: the light moved (reduced {reduced})");
                phase = next;
            }
        }
        Assert.AreEqual(0.25f, ShutterRoll.Sweep(0.9f, 0.35f / ShutterRoll.SweepPerSecond, false), 1e-4f, "wraps past a screen height");
        Assert.AreEqual(0.5f, ShutterRoll.Sweep(0.5f, -1f, false), 1e-6f, "no time: stays");
    }

    // --- DaterRubber (BUG-38b: every impression carries its verdict word) ---

    [Test]
    public void DaterRubber_PrintsAWordItCanPrint_AsGiven_InCapitals()
    {
        System.Func<char, bool> latin = c => c >= 'A' && c <= 'Z';
        Assert.AreEqual("APPROVED", DaterRubber.Word("approved", true, latin));
        Assert.AreEqual("ADMITTED", DaterRubber.Word("Admitted", true, latin), "an era's own English word stays");
        Assert.AreEqual("NOT ADMITTED", DaterRubber.Word("not admitted", false, latin), "spaces always print");
    }

    [TestCase("ΕΓΚΡΙΘΗΚΕ", true, "APPROVED")]
    [TestCase("مرفوض", false, "DENIED")]
    [TestCase("承認", true, "APPROVED")]
    [TestCase("拒否", false, "DENIED")]
    [TestCase("APPROUVÉ", true, "APPROVED")]
    [TestCase("", false, "DENIED")]
    [TestCase("   ", true, "APPROVED")]
    [TestCase(null, true, "APPROVED")]
    public void DaterRubber_AWordItCannotPrint_FallsBackToTheBureausEnglish(string word, bool approved, string expected)
    {
        System.Func<char, bool> latin = c => c >= 'A' && c <= 'Z';
        Assert.AreEqual(expected, DaterRubber.Word(word, approved, latin));
        Assert.AreEqual(approved ? DaterRubber.Approved : DaterRubber.Denied, DaterRubber.Word("x", approved, null), "no font: the English word");
    }

    // --- DaterWordOutline (BUG-31a: each pixel measures only the letters that reach it, and prints the same word) ---

    /// <summary>The dater impression's word row (DaterImpressionArt's size, row, stroke and tracking).</summary>
    private const int DaterWidth = 480, DaterHeight = 216;
    private const float WordTop = 0.05f, WordBottom = 0.53f, Line = 0.034f, Tracking = 0.09f;

    [TestCase("APPROVED")]
    [TestCase("DENIED")]
    [TestCase("ADEINOPRV")]
    [TestCase("VAVAV")]
    [TestCase("IOIDI")]
    [TestCase("NO DEAN")]
    [TestCase("i")]
    public void DaterWordOutline_MeasuringOnlyTheLettersThatReach_PaintsExactlyWhatMeasuringEveryLetterPaints(string word)
    {
        foreach ((int w, int h) in new[] { (DaterWidth, DaterHeight), (160, 72), (97, 211) })
        {
            float[] fast = DaterWordOutline.Cover(word, w, h, WordTop, WordBottom, Line, Tracking);
            float[] every = DaterWordOutline.Cover(word, w, h, WordTop, WordBottom, Line, Tracking, float.PositiveInfinity);
            Assert.AreEqual(every.Length, fast.Length);
            int inked = 0;
            for (int i = 0; i < every.Length; i++)
            {
                if (every[i] != fast[i])
                    Assert.Fail($"{word} at {w}x{h}: pixel ({i % w}, {i / w}) is {fast[i]} measuring the reaching letters, {every[i]} measuring them all");
                if (every[i] > 0f)
                    inked++;
            }
            Assert.Greater(inked, w * h / 200, $"{word} at {w}x{h}: the outline is inked");
        }
    }

    [Test]
    public void DaterWordOutline_TheReachCoversEveryLettersFarthestSerif()
    {
        // The reach's claim: past it, a letter is farther from a pixel than the stroke inks, so skipping it changes nothing.
        foreach (char c in "ADEINOPRV")
            for (float y = -0.2f; y <= 1.2f; y += 0.01f)
            {
                float outside = DaterLetters.Width(c) + DaterWordOutline.DefaultReach;
                Assert.Greater(DaterLetters.Distance(c, outside, y), Line + 0.5f / 50f, $"{c} right of its box at y {y}");
                Assert.Greater(DaterLetters.Distance(c, -DaterWordOutline.DefaultReach, y), Line + 0.5f / 50f, $"{c} left of its box at y {y}");
            }
    }

    [Test]
    public void DaterWordOutline_AWordWithALetterItDoesNotDraw_IsNotDrawn_AndInksNothing()
    {
        Assert.IsTrue(DaterWordOutline.Drawn("APPROVED"));
        Assert.IsTrue(DaterWordOutline.Drawn("denied"), "any case");
        Assert.IsFalse(DaterWordOutline.Drawn("ADMITTED"), "M and T are the font's");
        Assert.IsFalse(DaterWordOutline.Drawn("ΕΓΚΡΙΘΗΚΕ"));
        Assert.IsFalse(DaterWordOutline.Drawn("   "));
        Assert.IsFalse(DaterWordOutline.Drawn(null));
        Assert.IsTrue(System.Array.TrueForAll(DaterWordOutline.Cover("ADMITTED", 64, 32, WordTop, WordBottom, Line, Tracking), c => c == 0f));
    }
}
