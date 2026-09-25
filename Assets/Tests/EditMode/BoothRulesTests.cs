using System;
using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// The booth's input table (the physical-desk spec section 1.9, as the office
/// move changed it: the PC frame opens at once, so there is no blend row): one
/// test per output, each over every row; the wheel and citation details; the
/// default context. Expected outputs are written in this order: Desktop, CRT,
/// Power, pRops, pApers, Wheel allowed, Traveller live ('1' = true).
/// </summary>
public class BoothRulesTests
{
    private sealed class Row
    {
        public string Name;
        public BoothContext Context;
        public string Expected;
    }

    private static Row R(string name, bool focused, bool screenOn, BoothPhase phase, bool wheelOpen, string expected) =>
        new Row { Name = name, Context = new BoothContext(focused, screenOn, phase, wheelOpen, false), Expected = expected };

    /// <summary>The rows of the input table (the citation row is tested separately).</summary>
    private static readonly Row[] Rows =
    {
        //                                  focused screen phase                      wheel   DCPRAWT
        R("newsletter, office view",        false,  true,  BoothPhase.Newsletter,      false, "0000000"),
        R("newsletter, frame open",         true,   true,  BoothPhase.Newsletter,      false, "0000000"),
        R("office, no traveller",           false,  true,  BoothPhase.NoTraveller,     false, "0111000"),
        R("office, traveller at the desk",  false,  true,  BoothPhase.TravellerAtDesk, false, "0111111"),
        R("wheel open",                     false,  true,  BoothPhase.TravellerAtDesk, true,  "0000010"),
        R("frame open, screen on",          true,   true,  BoothPhase.TravellerAtDesk, false, "1010000"),
        R("frame open, screen off",         true,   false, BoothPhase.TravellerAtDesk, false, "0010000"),
        R("frame open, no traveller",       true,   true,  BoothPhase.NoTraveller,     false, "1010000"),
    };

    /// <summary>The outputs in the order of the expected strings.</summary>
    private static bool[] Outputs(BoothInput o) => new[]
    {
        o.DesktopInteractive, o.CrtFocusable, o.PowerButtonLive,
        o.PropsLive, o.PapersLive, o.WheelAllowed, o.TravellerLive
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
    public void AnOpenWheel_StaysAllowed_SoOpeningItNeverClosesIt_ButTheTravellerGoesInert()
    {
        BoothInput open = BoothRules.Evaluate(new BoothContext(false, true, BoothPhase.TravellerAtDesk, true, false));
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
            BoothInput held = BoothRules.Evaluate(new BoothContext(c.Focused, c.ScreenOn, c.Phase, c.WheelOpen, true));

            Assert.IsFalse(held.PowerButtonLive, row.Name);
            bool[] a = Outputs(plain), b = Outputs(held);
            for (int i = 0; i < a.Length; i++)
                if (i != 2)
                    Assert.AreEqual(a[i], b[i], $"{row.Name}: output {i} must not depend on the citation");
        }
    }

    /// <summary>
    /// The frame covers the left of the office and its exits sit around it, so
    /// nothing behind it may take input while it is open: no prop, paper, PC
    /// click, wheel or traveller in any context (the citation included).
    /// </summary>
    [Test]
    public void WithTheFrameOpen_NothingInTheOfficeTakesInput_InAnyContext()
    {
        var wrong = new List<string>();
        foreach (BoothPhase phase in (BoothPhase[])Enum.GetValues(typeof(BoothPhase)))
            for (int bits = 0; bits < 8; bits++)
            {
                var c = new BoothContext(true, (bits & 1) != 0, phase, (bits & 2) != 0, (bits & 4) != 0);
                BoothInput o = BoothRules.Evaluate(c);
                if (o.CrtFocusable || o.PropsLive || o.PapersLive || o.WheelAllowed || o.TravellerLive)
                    wrong.Add($"{phase}, screen {c.ScreenOn}, wheel {c.WheelOpen}, citation {c.CitationPending}");
            }

        Assert.IsEmpty(wrong, $"Something behind the open frame takes input: {string.Join("; ", wrong)}");
    }

    [Test]
    public void TheDefaultContext_IsTheOfficeViewWithNoTraveller()
    {
        BoothContext c = default;
        Assert.AreEqual(BoothPhase.NoTraveller, c.Phase, "NoTraveller is the first phase, the default before Start");
        CollectionAssert.AreEqual(new[] { false, true, true, true, false, false, false }, Outputs(BoothRules.Evaluate(c)));
    }
}
