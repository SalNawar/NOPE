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
/// papers live, desk catcher (K), Escape puts papers back, Stamp tray
/// allowed, case hUd visible ('1' = true).
/// </summary>
public class BoothRulesTests
{
    private sealed class Row
    {
        public string Name;
        public BoothContext Context;
        public string Expected;
    }

    private static Row R(string name, bool focused, bool screenOn, BoothPhase phase, bool wheelOpen, bool stampOpen, bool papersHeld, string expected) =>
        new Row { Name = name, Context = new BoothContext(focused, screenOn, phase, wheelOpen, false, stampOpen, papersHeld), Expected = expected.Replace(" ", "") };

    /// <summary>The rows of the input table (the citation row is tested separately).</summary>
    private static readonly Row[] Rows =
    {
        //                                          focused screen phase                      wheel  stamp  held   DCPRAWT HKESU
        R("newsletter, office view",                false,  true,  BoothPhase.Newsletter,      false, false, false, "0000000 00000"),
        R("newsletter, frame open",                 true,   true,  BoothPhase.Newsletter,      false, false, false, "0000000 00000"),
        R("office, no traveller",                   false,  true,  BoothPhase.NoTraveller,     false, false, false, "0111000 00000"),
        R("office, traveller at the desk",          false,  true,  BoothPhase.TravellerAtDesk, false, false, false, "0111111 10011"),
        R("wheel open",                             false,  true,  BoothPhase.TravellerAtDesk, true,  false, false, "0000010 00011"),
        R("frame open, screen on",                  true,   true,  BoothPhase.TravellerAtDesk, false, false, false, "1010000 10000"),
        R("frame open, screen off",                 true,   false, BoothPhase.TravellerAtDesk, false, false, false, "0010000 10000"),
        R("frame open, no traveller",               true,   true,  BoothPhase.NoTraveller,     false, false, false, "1010000 00000"),
        R("office, papers held",                    false,  true,  BoothPhase.TravellerAtDesk, false, false, true,  "0111111 11111"),
        R("stamp tray open",                        false,  true,  BoothPhase.TravellerAtDesk, false, true,  false, "0000010 00011"),
        R("stamp tray open, papers held",           false,  true,  BoothPhase.TravellerAtDesk, false, true,  true,  "0000010 00011"),
        R("wheel open, papers held",                false,  true,  BoothPhase.TravellerAtDesk, true,  false, true,  "0000010 00011"),
        R("frame open, papers held",                true,   true,  BoothPhase.TravellerAtDesk, false, false, true,  "1010000 10000"),
        R("office, no traveller, papers held",      false,  true,  BoothPhase.NoTraveller,     false, false, true,  "0111000 00000"),
    };

    /// <summary>The outputs in the order of the expected strings.</summary>
    private static bool[] Outputs(BoothInput o) => new[]
    {
        o.DesktopInteractive, o.CrtFocusable, o.PowerButtonLive,
        o.PropsLive, o.PapersLive, o.WheelAllowed, o.TravellerLive,
        o.HeldPapersLive, o.DeskCatcherLive, o.ExamineEscapeLive, o.StampTrayAllowed, o.CaseHudVisible
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
    public void PapersLive_LikeProps_WhileATravellerIsAtTheDesk() => CheckColumn(4, "PapersLive");

    [Test]
    public void WheelAllowed_InTheOfficeView_WhileATravellerIsAtTheDesk() => CheckColumn(5, "WheelAllowed");

    [Test]
    public void TravellerLive_WhenTheWheelIsAllowedButClosed() => CheckColumn(6, "TravellerLive");

    [Test]
    public void StampOpen_MakesCrtPowerPropsPapersTravellerInert()
    {
        foreach (bool held in new[] { false, true })
        {
            BoothInput open = BoothRules.Evaluate(new BoothContext(false, true, BoothPhase.TravellerAtDesk, false, false, true, held));
            Assert.IsFalse(open.CrtFocusable, "the PC");
            Assert.IsFalse(open.PowerButtonLive, "the power buttons");
            Assert.IsFalse(open.PropsLive, "the props");
            Assert.IsFalse(open.PapersLive, "the papers on the desk");
            Assert.IsFalse(open.HeldPapersLive, "the papers in the hand");
            Assert.IsFalse(open.TravellerLive, "the traveller");
            Assert.IsTrue(open.StampTrayAllowed, "an open tray stays allowed, so opening it never closes it");
        }
    }

    [Test]
    public void HeldPapersLive_InBothViews_NotWithWheelStampOrNewsletter()
    {
        CheckColumn(7, "HeldPapersLive");
        Assert.IsTrue(BoothRules.Evaluate(new BoothContext(true, false, BoothPhase.TravellerAtDesk, false, false, false, true)).HeldPapersLive,
            "beside the open frame, even on a dark screen");
    }

    [Test]
    public void DeskCatcherAndEscape_NeedHeldPapersInOfficeView()
    {
        CheckColumn(8, "DeskCatcherLive");
        CheckColumn(9, "ExamineEscapeLive");
        foreach (Row row in Rows)
        {
            BoothInput o = BoothRules.Evaluate(row.Context);
            Assert.AreEqual(o.PapersLive && row.Context.PapersHeld, o.DeskCatcherLive, row.Name);
            Assert.AreEqual(o.DeskCatcherLive, o.ExamineEscapeLive, row.Name);
        }
    }

    [Test]
    public void StampTrayAllowed_OfficeViewTravellerAtDesk() => CheckColumn(10, "StampTrayAllowed");

    [Test]
    public void CaseHudVisible_NotWhileFocused() => CheckColumn(11, "CaseHudVisible");

    [Test]
    public void AnOpenWheel_StaysAllowed_SoOpeningItNeverClosesIt_ButTheTravellerGoesInert()
    {
        BoothInput open = BoothRules.Evaluate(new BoothContext(false, true, BoothPhase.TravellerAtDesk, true, false, false, false));
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
            BoothInput held = BoothRules.Evaluate(new BoothContext(c.Focused, c.ScreenOn, c.Phase, c.WheelOpen, true, c.StampOpen, c.PapersHeld));

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
    /// the desk, PC click, wheel, traveller, desk catcher, stamp tray or case
    /// HUD in any context (the citation, the stamp and held papers included).
    /// Only papers held in the hand stay live, beside the frame (piece 10 X10).
    /// </summary>
    [Test]
    public void WithTheFrameOpen_NothingInTheOfficeTakesInput_InAnyContext()
    {
        var wrong = new List<string>();
        foreach (BoothPhase phase in (BoothPhase[])Enum.GetValues(typeof(BoothPhase)))
            for (int bits = 0; bits < 32; bits++)
            {
                var c = new BoothContext(true, (bits & 1) != 0, phase, (bits & 2) != 0, (bits & 4) != 0, (bits & 8) != 0, (bits & 16) != 0);
                BoothInput o = BoothRules.Evaluate(c);
                if (o.CrtFocusable || o.PropsLive || o.PapersLive || o.WheelAllowed || o.TravellerLive ||
                    o.DeskCatcherLive || o.ExamineEscapeLive || o.StampTrayAllowed || o.CaseHudVisible)
                    wrong.Add($"{phase}, screen {c.ScreenOn}, wheel {c.WheelOpen}, citation {c.CitationPending}, stamp {c.StampOpen}, held {c.PapersHeld}");
            }

        Assert.IsEmpty(wrong, $"Something behind the open frame takes input: {string.Join("; ", wrong)}");
    }

    [Test]
    public void TheDefaultContext_IsTheOfficeViewWithNoTraveller()
    {
        BoothContext c = default;
        Assert.AreEqual(BoothPhase.NoTraveller, c.Phase, "NoTraveller is the first phase, the default before Start");
        CollectionAssert.AreEqual(new[] { false, true, true, true, false, false, false, false, false, false, false, false }, Outputs(BoothRules.Evaluate(c)));
    }
}
