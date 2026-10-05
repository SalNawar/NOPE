using System;
using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// The booth's input table (the physical-desk spec section 1.9, as the office
/// move changed it: the PC frame opens at once, so there is no blend row; and
/// as piece 10 grew it: the stamp tray and papers held in the hand): one test
/// per output, each over every row; the wheel, stamp and citation details; the
/// default context. Expected outputs are written in this order: Desktop, CRT,
/// Power, pRops, pApers, Wheel allowed, Traveller live, then (piece 10) Held
/// papers live, desk catcher (K), Escape puts papers back, Stamps live
/// (the desk-first redesign: the physical stamps), case hUd visible, then (the desk view, spec 11) the Mat toggles
/// the desk view, Escape and the right-click return from it (X), the desk
/// View allowed, then (the readability fix) the "▲ Back" control and the wheel
/// rolled up (B), the wheel rolled down over the mat (I), then (audit R5-001)
/// a held paper's drag Out of the hand (O) ('1' = true). The desk flow
/// (Saleh 2026-09-30): the papers on the desk move only while tilted (when
/// there is a desk view), and the wheel or the PC frame opening returns the
/// desk view.
/// </summary>
public class BoothRulesTests
{
    private sealed class Row
    {
        public string Name;
        public BoothContext Context;
        public string Expected;
    }

    private static Row R(string name, bool focused, bool screenOn, BoothPhase phase, bool wheelOpen, bool stampHeld, bool papersHeld, string expected) =>
        R(name, focused, screenOn, phase, wheelOpen, stampHeld, papersHeld, false, expected);

    private static Row R(string name, bool focused, bool screenOn, BoothPhase phase, bool wheelOpen, bool stampHeld, bool papersHeld, bool deskView, string expected) =>
        R(name, focused, screenOn, phase, wheelOpen, stampHeld, papersHeld, deskView, true, expected);

    private static Row R(string name, bool focused, bool screenOn, BoothPhase phase, bool wheelOpen, bool stampHeld, bool papersHeld, bool deskView, bool bound, string expected) =>
        new Row { Name = name, Context = new BoothContext(focused, screenOn, phase, wheelOpen, false, stampHeld, papersHeld, deskView, bound), Expected = expected.Replace(" ", "") };

    /// <summary>The rows of the input table (the citation row is tested separately).</summary>
    private static readonly Row[] Rows =
    {
        //                                          focused screen phase                      wheel  stamp  held          DCPRAWT HKESU MXV BI O
        R("newsletter, office view",                false,  true,  BoothPhase.Newsletter,      false, false, false,        "0000000 00000 000 00 0"),
        R("newsletter, frame open",                 true,   true,  BoothPhase.Newsletter,      false, false, false,        "0000000 00000 000 00 0"),
        R("office, no traveller",                   false,  true,  BoothPhase.NoTraveller,     false, false, false,        "0111000 00000 101 01 0"),
        R("office, traveller at the desk",          false,  true,  BoothPhase.TravellerAtDesk, false, false, false,        "0111011 10011 101 01 0"),
        R("wheel open",                             false,  true,  BoothPhase.TravellerAtDesk, true,  false, false,        "0000010 00001 000 00 0"),
        R("frame open, screen on",                  true,   true,  BoothPhase.TravellerAtDesk, false, false, false,        "1010000 10000 000 00 0"),
        R("frame open, screen off",                 true,   false, BoothPhase.TravellerAtDesk, false, false, false,        "0010000 10000 000 00 0"),
        R("frame open, no traveller",               true,   true,  BoothPhase.NoTraveller,     false, false, false,        "1010000 00000 000 00 0"),
        R("office, papers held",                    false,  true,  BoothPhase.TravellerAtDesk, false, false, true,         "0111011 11111 001 00 0"),
        R("stamp held",                             false,  true,  BoothPhase.TravellerAtDesk, false, true,  false,        "0010010 10011 001 00 0"),
        R("stamp held, papers held",                false,  true,  BoothPhase.TravellerAtDesk, false, true,  true,         "0010010 10011 001 00 0"),
        R("wheel open, papers held",                false,  true,  BoothPhase.TravellerAtDesk, true,  false, true,         "0000010 00001 000 00 0"),
        R("frame open, papers held",                true,   true,  BoothPhase.TravellerAtDesk, false, false, true,         "1010000 10000 000 00 0"),
        R("office, no traveller, papers held",      false,  true,  BoothPhase.NoTraveller,     false, false, true,         "0111000 00000 001 00 0"),
        //                                          focused screen phase                      wheel  stamp  held   desk   DCPRAWT HKESU MXV BI O
        R("desk view, traveller at the desk",       false,  true,  BoothPhase.TravellerAtDesk, false, false, false, true,  "0111111 10011 111 10 1"),
        R("desk view, no traveller",                false,  true,  BoothPhase.NoTraveller,     false, false, false, true,  "0111000 00000 111 10 0"),
        R("desk view, papers held",                 false,  true,  BoothPhase.TravellerAtDesk, false, false, true,  true,  "0111111 11111 001 10 1"),
        R("desk view, wheel open",                  false,  true,  BoothPhase.TravellerAtDesk, true,  false, false, true,  "0000010 00001 000 00 0"),
        R("desk view, stamp held",                  false,  true,  BoothPhase.TravellerAtDesk, false, true,  false, true,  "0010110 10011 001 00 1"),
        R("desk view, frame open",                  true,   true,  BoothPhase.TravellerAtDesk, false, false, false, true,  "1010000 10000 000 00 0"),
        R("desk view, newsletter",                  false,  true,  BoothPhase.Newsletter,      false, false, false, true,  "0000000 00000 000 00 0"),
        //                                          focused screen phase                      wheel  stamp  held   desk   bound  DCPRAWT HKESU MXV BI O
        R("no desk view, traveller at the desk",    false,  true,  BoothPhase.TravellerAtDesk, false, false, false, false, false, "0111111 10011 101 01 1"),
        R("no desk view, papers held",              false,  true,  BoothPhase.TravellerAtDesk, false, false, true,  false, false, "0111111 11111 001 00 1"),
    };

    /// <summary>The outputs in the order of the expected strings.</summary>
    private static bool[] Outputs(BoothInput o) => new[]
    {
        o.DesktopInteractive, o.CrtFocusable, o.PowerButtonLive,
        o.PropsLive, o.PapersLive, o.WheelAllowed, o.TravellerLive,
        o.HeldPapersLive, o.DeskCatcherLive, o.ExamineEscapeLive, o.StampsLive, o.CaseHudVisible,
        o.DeskViewToggleLive, o.DeskViewReturnLive, o.DeskViewAllowed,
        o.DeskViewBackLive, o.DeskViewScrollInLive,
        o.HeldDragOutLive
    };

    private static void CheckColumn(int column, string output)
    {
        var wrong = new List<string>();
        foreach (Row row in Rows)
        {
            bool expected = row.Expected[column] == '1';
            if (Outputs(BoothRules.Evaluate(row.Context))[column] != expected)
                wrong.Add($"{row.Name}: expected {expected}");
        }

        Assert.IsEmpty(wrong, $"{output}: {string.Join("; ", wrong)}");
    }

    [Test]
    public void DesktopInteractive_OnlyWithTheFrameOpenAndTheScreenLit_NeverUnderANewsletter() => CheckColumn(0, "DesktopInteractive");

    [Test]
    public void CrtFocusable_InTheOfficeViewWithoutANewsletterOrAnOpenWheel() => CheckColumn(1, "CrtFocusable");

    [Test]
    public void PowerButtonLive_InEitherView_WithoutANewsletterOrAnOpenWheel() => CheckColumn(2, "PowerButtonLive");

    [Test]
    public void PropsLive_InTheOfficeView_WithoutANewsletterOrAnOpenWheel() => CheckColumn(3, "PropsLive");

    [Test]
    public void PapersLive_LikeProps_WhileATravellerIsAtTheDesk_OnlyTilted() => CheckColumn(4, "PapersLive");

    /// <summary>Saleh 2026-09-30: "moving papers should only be possible when camera is tilted": in the normal view no paper on the desk drags or takes a click (a click reaches the mat under it and tilts in), nor does a held paper drag out onto the desk; tilted, they work as the props do; with no desk view at all they work in the normal view.</summary>
    [Test]
    public void PapersOnTheDesk_MoveOnlyWhileTilted_OrWithoutADeskView()
    {
        foreach (BoothContext c in AllContexts())
        {
            BoothInput o = BoothRules.Evaluate(c);
            string name = $"{c.Phase}, focused {c.Focused}, wheel {c.WheelOpen}, stamp {c.StampHeld}, held {c.PapersHeld}, desk view {c.DeskView}, bound {c.DeskViewBound}";
            bool reach = c.DeskView || !c.DeskViewBound;
            bool desk = !c.Focused && c.Phase != BoothPhase.Newsletter && !c.WheelOpen;
            Assert.AreEqual(desk && c.Phase == BoothPhase.TravellerAtDesk && reach, o.PapersLive, name);
            if (!c.StampHeld)
                Assert.AreEqual(o.PropsLive && c.Phase == BoothPhase.TravellerAtDesk && reach, o.PapersLive, name);
            if (!reach)
                Assert.IsFalse(o.PapersLive || o.HeldDragOutLive, $"nothing moves on the desk in the normal view: {name}");
        }
    }

    /// <summary>Saleh 2026-09-30: from the tilted view the intercom, the traveller or the PC is one click away: the wheel or the PC frame opening returns the desk view (the camera blends straight up), and the desk view is never allowed beside them.</summary>
    [Test]
    public void TheWheelOrThePcFrame_ReturnsTheDeskView()
    {
        foreach (BoothContext c in AllContexts())
        {
            BoothInput o = BoothRules.Evaluate(c);
            Assert.AreEqual(c.Phase != BoothPhase.Newsletter && !c.WheelOpen && !c.Focused, o.DeskViewAllowed,
                $"{c.Phase}, focused {c.Focused}, wheel {c.WheelOpen}, stamp {c.StampHeld}, held {c.PapersHeld}, desk view {c.DeskView}");
        }
    }

    [Test]
    public void WheelAllowed_InTheOfficeView_WhileATravellerIsAtTheDesk() => CheckColumn(5, "WheelAllowed");

    [Test]
    public void TravellerLive_WhenTheWheelIsAllowedButClosed() => CheckColumn(6, "TravellerLive");

    /// <summary>The desk-first redesign, item 12: a stamp in the hand is a tool, not a modal tray: the papers on the desk stay live (it presses on them) while the PC, the props, the traveller and the mat wait until it is put down; the stamps stay live, so picking one up never puts it down.</summary>
    [Test]
    public void AStampInTheHand_PressesOnThePapers_TheRestWaits()
    {
        foreach (bool held in new[] { false, true })
        {
            BoothInput stamp = BoothRules.Evaluate(new BoothContext(false, true, BoothPhase.TravellerAtDesk, false, false, true, held, true, true));
            Assert.IsFalse(stamp.CrtFocusable, "the PC");
            Assert.IsFalse(stamp.PropsLive, "the props");
            Assert.IsTrue(stamp.PapersLive, "the papers on the desk take the press");
            Assert.IsTrue(stamp.HeldPapersLive, "the papers in the hand");
            Assert.IsFalse(stamp.TravellerLive, "the traveller");
            Assert.IsFalse(stamp.DeskViewToggleLive, "the mat");
            Assert.IsFalse(stamp.DeskViewReturnLive, "Escape puts the stamp down first");
            Assert.IsTrue(stamp.StampsLive, "the stamps stay live while one is held");
        }
    }

    [Test]
    public void HeldPapersLive_InBothViews_NotWithWheelStampOrNewsletter()
    {
        CheckColumn(7, "HeldPapersLive");
        Assert.IsTrue(BoothRules.Evaluate(new BoothContext(true, false, BoothPhase.TravellerAtDesk, false, false, false, true, false, true)).HeldPapersLive,
            "beside the open frame, even on a dark screen");
    }

    /// <summary>
    /// A held paper dragged out of the hand drops onto the desk, so the drag
    /// needs the desk's papers live: never beside the open frame, where held
    /// papers still take clicks but a press-and-move on one starts no drag and
    /// it stays held (audit R5-001: that drag disabled the paper mid-drag and
    /// stranded it, lifted and inert, until the decision).
    /// </summary>
    [Test]
    public void HeldDragOutLive_OnlyWhileTheDeskPapersAreLive_NeverBesideTheOpenFrame()
    {
        CheckColumn(17, "HeldDragOutLive");
        foreach (BoothContext c in AllContexts())
        {
            BoothInput o = BoothRules.Evaluate(c);
            string name = $"{c.Phase}, focused {c.Focused}, wheel {c.WheelOpen}, stamp {c.StampHeld}, held {c.PapersHeld}, desk view {c.DeskView}";
            Assert.AreEqual(o.HeldPapersLive && o.PapersLive, o.HeldDragOutLive, name);
            if (c.Focused)
                Assert.IsFalse(o.HeldDragOutLive, $"beside the open frame: {name}");
        }
    }

    [Test]
    public void DeskCatcherAndEscape_NeedHeldPapersInOfficeView_InEitherView()
    {
        CheckColumn(8, "DeskCatcherLive");
        CheckColumn(9, "ExamineEscapeLive");
        foreach (BoothContext c in AllContexts())
        {
            BoothInput o = BoothRules.Evaluate(c);
            string name = $"{c.Phase}, focused {c.Focused}, wheel {c.WheelOpen}, stamp {c.StampHeld}, held {c.PapersHeld}, desk view {c.DeskView}, bound {c.DeskViewBound}";
            Assert.AreEqual(o.PropsLive && c.Phase == BoothPhase.TravellerAtDesk && c.PapersHeld, o.DeskCatcherLive, name);
            Assert.AreEqual(o.DeskCatcherLive, o.ExamineEscapeLive, name);
        }
    }

    [Test]
    public void StampsLive_OfficeViewTravellerAtDesk_WheelClosed() => CheckColumn(10, "StampsLive");

    [Test]
    public void CaseHudVisible_NotWhileFocused() => CheckColumn(11, "CaseHudVisible");

    [Test]
    public void DeskViewToggleLive_WhenThePropsAreLiveAndNoPaperIsHeld()
    {
        CheckColumn(12, "DeskViewToggleLive");
        foreach (Row row in Rows)
        {
            BoothInput o = BoothRules.Evaluate(row.Context);
            Assert.AreEqual(o.PropsLive && !row.Context.PapersHeld, o.DeskViewToggleLive, row.Name);
        }
    }

    [Test]
    public void DeskViewReturnLive_OnlyInTheDeskView_WhenTheToggleIsLive()
    {
        CheckColumn(13, "DeskViewReturnLive");
        foreach (Row row in Rows)
        {
            BoothInput o = BoothRules.Evaluate(row.Context);
            Assert.AreEqual(row.Context.DeskView && o.DeskViewToggleLive, o.DeskViewReturnLive, row.Name);
        }
    }

    [Test]
    public void DeskViewAllowed_UnlessANewsletterTheWheelOrTheFrameIsUp() => CheckColumn(14, "DeskViewAllowed");

    /// <summary>The visible way out: the "▲ Back" control shows (and the wheel rolled up returns) while tilted and the office takes input, papers held or not.</summary>
    [Test]
    public void DeskViewBackLive_InTheDeskView_WhenThePropsAreLive_PapersHeldOrNot()
    {
        CheckColumn(15, "DeskViewBackLive");
        foreach (BoothContext c in AllContexts())
        {
            BoothInput o = BoothRules.Evaluate(c);
            Assert.AreEqual(c.DeskView && o.PropsLive, o.DeskViewBackLive, $"{c.Phase}, focused {c.Focused}, wheel {c.WheelOpen}, stamp {c.StampHeld}, held {c.PapersHeld}, desk view {c.DeskView}");
        }
    }

    /// <summary>The wheel rolled down over the empty mat tilts in: the mat's toggle is live and the view is normal (no frame, wheel or tray; the pointer check is the view's).</summary>
    [Test]
    public void DeskViewScrollInLive_InTheNormalView_WhenTheMatIsLive()
    {
        CheckColumn(16, "DeskViewScrollInLive");
        foreach (BoothContext c in AllContexts())
        {
            BoothInput o = BoothRules.Evaluate(c);
            Assert.AreEqual(!c.DeskView && o.DeskViewToggleLive, o.DeskViewScrollInLive, $"{c.Phase}, focused {c.Focused}, wheel {c.WheelOpen}, stamp {c.StampHeld}, held {c.PapersHeld}, desk view {c.DeskView}");
            Assert.IsFalse(o.DeskViewBackLive && o.DeskViewScrollInLive, "the wheel never both tilts in and back");
        }
    }

    /// <summary>A click on the desk has one meaning: with papers held it puts them back (the desk catcher), else it toggles the desk view (the mat).</summary>
    [Test]
    public void TheDeskCatcherAndTheMat_AreNeverLiveTogether()
    {
        foreach (BoothContext c in AllContexts())
        {
            BoothInput o = BoothRules.Evaluate(c);
            Assert.IsFalse(o.DeskCatcherLive && o.DeskViewToggleLive, $"{c.Phase}, focused {c.Focused}, held {c.PapersHeld}");
        }
    }

    /// <summary>The desk view changes no output but the papers on the desk (and so a held paper's drag out onto it) and its own ways out and in (the return, the Back control, the wheel); with no desk view, not even the papers.</summary>
    [Test]
    public void TheDeskView_ChangesOnlyThePapersAndItsOwnWays_InAnyContext()
    {
        foreach (BoothContext c in AllContexts())
        {
            bool[] a = Outputs(BoothRules.Evaluate(new BoothContext(c.Focused, c.ScreenOn, c.Phase, c.WheelOpen, c.CitationPending, c.StampHeld, c.PapersHeld, false, c.DeskViewBound)));
            bool[] b = Outputs(BoothRules.Evaluate(new BoothContext(c.Focused, c.ScreenOn, c.Phase, c.WheelOpen, c.CitationPending, c.StampHeld, c.PapersHeld, true, c.DeskViewBound)));
            for (int i = 0; i < a.Length; i++)
                if (i != 13 && i != 15 && i != 16 && (!c.DeskViewBound || (i != 4 && i != 17)))
                    Assert.AreEqual(a[i], b[i], $"{c.Phase}, focused {c.Focused}, wheel {c.WheelOpen}, stamp {c.StampHeld}, held {c.PapersHeld}, bound {c.DeskViewBound}: output {i} must not depend on the desk view");
        }
    }

    /// <summary>Every context: each phase, and every combination of the six flags, the desk view and whether there is one.</summary>
    private static IEnumerable<BoothContext> AllContexts()
    {
        foreach (BoothPhase phase in (BoothPhase[])Enum.GetValues(typeof(BoothPhase)))
            for (int bits = 0; bits < 256; bits++)
                yield return new BoothContext((bits & 1) != 0, (bits & 2) != 0, phase, (bits & 4) != 0, (bits & 8) != 0, (bits & 16) != 0, (bits & 32) != 0, (bits & 64) != 0, (bits & 128) != 0);
    }

    [Test]
    public void AnOpenWheel_StaysAllowed_SoOpeningItNeverClosesIt_ButTheTravellerGoesInert()
    {
        BoothInput open = BoothRules.Evaluate(new BoothContext(false, true, BoothPhase.TravellerAtDesk, true, false, false, false, false, true));
        Assert.IsTrue(open.WheelAllowed);
        Assert.IsFalse(open.TravellerLive);
    }

    [Test]
    public void APendingCitation_OnlyMakesThePowerButtonInert_InEveryRow()
    {
        foreach (Row row in Rows)
        {
            BoothContext c = row.Context;
            BoothInput plain = BoothRules.Evaluate(c);
            BoothInput held = BoothRules.Evaluate(new BoothContext(c.Focused, c.ScreenOn, c.Phase, c.WheelOpen, true, c.StampHeld, c.PapersHeld, c.DeskView, c.DeskViewBound));

            Assert.IsFalse(held.PowerButtonLive, row.Name);
            bool[] a = Outputs(plain), b = Outputs(held);
            for (int i = 0; i < a.Length; i++)
                if (i != 2)
                    Assert.AreEqual(a[i], b[i], $"{row.Name}: output {i} must not depend on the citation");
        }
    }

    /// <summary>
    /// The frame covers the left of the office and its exits sit around it, so
    /// nothing behind it may take input while it is open: no prop, paper on
    /// the desk, PC click, wheel, traveller, desk catcher, stamp tray, case
    /// HUD, mat or desk view return in any context (the citation, the stamp,
    /// held papers and the desk view included). Only papers held in the hand
    /// stay live, beside the frame (piece 10 X10), for clicks: never for a drag
    /// out of the hand (audit R5-001).
    /// </summary>
    [Test]
    public void WithTheFrameOpen_NothingInTheOfficeTakesInput_InAnyContext()
    {
        var wrong = new List<string>();
        foreach (BoothContext c in AllContexts())
        {
            if (!c.Focused)
                continue;
            BoothInput o = BoothRules.Evaluate(c);
            if (o.CrtFocusable || o.PropsLive || o.PapersLive || o.WheelAllowed || o.TravellerLive ||
                o.DeskCatcherLive || o.ExamineEscapeLive || o.StampsLive || o.CaseHudVisible ||
                o.DeskViewToggleLive || o.DeskViewReturnLive || o.DeskViewBackLive || o.DeskViewScrollInLive ||
                o.HeldDragOutLive)
                wrong.Add($"{c.Phase}, screen {c.ScreenOn}, wheel {c.WheelOpen}, citation {c.CitationPending}, stamp {c.StampHeld}, held {c.PapersHeld}, desk view {c.DeskView}");
        }

        Assert.IsEmpty(wrong, $"Something behind the open frame takes input: {string.Join("; ", wrong)}");
    }

    [Test]
    public void TheDefaultContext_IsTheOfficeViewWithNoTraveller()
    {
        BoothContext c = default;
        Assert.AreEqual(BoothPhase.NoTraveller, c.Phase, "NoTraveller is the first phase, the default before Start");
        Assert.IsFalse(c.DeskView, "the normal view is the default");
        Assert.IsFalse(c.DeskViewBound, "no desk view is the default (the papers then move in the normal view)");
        CollectionAssert.AreEqual(new[] { false, true, true, true, false, false, false, false, false, false, false, false, true, false, true, false, true, false }, Outputs(BoothRules.Evaluate(c)));
    }
}
