using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// The booth's input table (spec section 1.9): one test per output, each over
/// every row; the wheel, blend and citation details; the default context.
/// Expected outputs are written in this order: Desktop, CRT, Power, eXit,
/// pRops, pApers, Wheel allowed, Traveller live ('1' = true).
/// </summary>
public class BoothRulesTests
{
    private sealed class Row
    {
        public string Name;
        public BoothContext Context;
        public string Expected;
    }

    private static Row R(string name, bool focused, bool settled, bool screenOn, BoothPhase phase, bool wheelOpen, string expected) =>
        new Row { Name = name, Context = new BoothContext(focused, settled, screenOn, phase, wheelOpen, false), Expected = expected };

    /// <summary>The rows of spec section 1.9 (the citation row is tested separately).</summary>
    private static readonly Row[] Rows =
    {
        //                                         focused settled screen phase                      wheel   DCPXRAWT
        R("newsletter, booth view",                false,  true,   true,  BoothPhase.Newsletter,      false, "00000000"),
        R("newsletter, monitor view",              true,   true,   true,  BoothPhase.Newsletter,      false, "00010000"),
        R("booth, settled, no traveller",          false,  true,   true,  BoothPhase.NoTraveller,     false, "01101000"),
        R("booth, settled, traveller at the desk", false,  true,   true,  BoothPhase.TravellerAtDesk, false, "01101111"),
        R("wheel open",                            false,  true,   true,  BoothPhase.TravellerAtDesk, true,  "00000010"),
        R("blending to the booth",                 false,  false,  true,  BoothPhase.TravellerAtDesk, false, "01000000"),
        R("blending to the monitor",               true,   false,  true,  BoothPhase.TravellerAtDesk, false, "00000000"),
        R("focused, settled, screen on",           true,   true,   true,  BoothPhase.TravellerAtDesk, false, "10110000"),
        R("focused, settled, screen off",          true,   true,   false, BoothPhase.TravellerAtDesk, false, "00110000"),
    };

    /// <summary>The outputs in the order of the expected strings.</summary>
    private static bool[] Outputs(BoothInput o) => new[]
    {
        o.DesktopInteractive, o.CrtFocusable, o.PowerButtonLive, o.FocusExitLive,
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
    public void DesktopInteractive_OnlyFocusedSettledAndLit_NeverUnderANewsletter() => CheckColumn(0, "DesktopInteractive");

    [Test]
    public void CrtFocusable_InTheBoothWithoutANewsletterOrAnOpenWheel() => CheckColumn(1, "CrtFocusable");

    [Test]
    public void PowerButtonLive_WhenSettled_WithoutANewsletterOrAnOpenWheel() => CheckColumn(2, "PowerButtonLive");

    [Test]
    public void FocusExitLive_OnlyFocusedAndSettled() => CheckColumn(3, "FocusExitLive");

    [Test]
    public void PropsLive_InTheSettledBooth_WithoutANewsletterOrAnOpenWheel() => CheckColumn(4, "PropsLive");

    [Test]
    public void PapersLive_LikeProps_WhileATravellerIsAtTheDesk() => CheckColumn(5, "PapersLive");

    [Test]
    public void WheelAllowed_InTheSettledBooth_WhileATravellerIsAtTheDesk() => CheckColumn(6, "WheelAllowed");

    [Test]
    public void TravellerLive_WhenTheWheelIsAllowedButClosed() => CheckColumn(7, "TravellerLive");

    [Test]
    public void AnOpenWheel_StaysAllowed_SoOpeningItNeverClosesIt_ButTheTravellerGoesInert()
    {
        BoothInput open = BoothRules.Evaluate(new BoothContext(false, true, true, BoothPhase.TravellerAtDesk, true, false));
        Assert.IsTrue(open.WheelAllowed);
        Assert.IsFalse(open.TravellerLive);
    }

    [Test]
    public void TheCrt_CanBeClickedWhileTheCameraBlendsBackToTheBooth_NotWhileItPushesIn()
    {
        Assert.IsTrue(BoothRules.Evaluate(new BoothContext(false, false, true, BoothPhase.NoTraveller, false, false)).CrtFocusable);
        Assert.IsFalse(BoothRules.Evaluate(new BoothContext(true, false, true, BoothPhase.NoTraveller, false, false)).CrtFocusable);
    }

    [Test]
    public void APendingCitation_OnlyMakesThePowerButtonInert_InEveryRow()
    {
        foreach (Row row in Rows)
        {
            BoothContext c = row.Context;
            BoothInput plain = BoothRules.Evaluate(c);
            BoothInput held = BoothRules.Evaluate(new BoothContext(c.Focused, c.Settled, c.ScreenOn, c.Phase, c.WheelOpen, true));

            Assert.IsFalse(held.PowerButtonLive, row.Name);
            bool[] a = Outputs(plain), b = Outputs(held);
            for (int i = 0; i < a.Length; i++)
                if (i != 2)
                    Assert.AreEqual(a[i], b[i], $"{row.Name}: output {i} must not depend on the citation");
        }
    }

    [Test]
    public void TheDefaultContext_OnlyLetsTheCrtBeClicked()
    {
        BoothContext c = default;
        Assert.AreEqual(BoothPhase.NoTraveller, c.Phase, "NoTraveller is the first phase, the default before Start");
        CollectionAssert.AreEqual(new[] { false, true, false, false, false, false, false, false }, Outputs(BoothRules.Evaluate(c)));
    }
}
