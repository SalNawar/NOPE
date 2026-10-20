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
}
