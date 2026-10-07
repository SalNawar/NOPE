using System;
using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// The booth's input table (the physical-desk spec section 1.9, as the office
/// move, piece 10 and Papers, Please's controls changed it, Saleh
/// 2026-10-06): one test per output, each over every row; the wheel's
/// details; the default context. Expected outputs are written in
/// this order: Desktop, CRT, Power, pRops, pApers, Wheel allowed, Traveller
/// live, then Stamps live, Inspect live, case hUd visible, then the desk
/// View allowed, the "▲ Back" control (B), the Normal view's own ways (N)
/// and the PC switch (Q) ('1' = true). The documents move by left-drag in
/// either view (the reading view comes by itself when one lands on the desk).
/// </summary>
public class BoothRulesTests
{
    private sealed class Row
    {
        public string Name;
        public BoothContext Context;
        public string Expected;
    }

    private static Row R(string name, bool focused, bool screenOn, BoothPhase phase, bool wheelOpen, bool deskView, string expected) =>
        new Row { Name = name, Context = new BoothContext(focused, screenOn, phase, wheelOpen, deskView), Expected = expected.Replace(" ", "") };

    /// <summary>The rows of the input table.</summary>
    private static readonly Row[] Rows =
    {
        //                                          focused screen phase                      wheel  desk    DCPRAWT SIU VBNQ
        R("newsletter, office view",                false,  true,  BoothPhase.Newsletter,      false, false, "0000000 000 0000"),
        R("newsletter, frame open",                 true,   true,  BoothPhase.Newsletter,      false, false, "0000000 000 0000"),
        R("office, no traveller",                   false,  true,  BoothPhase.NoTraveller,     false, false, "0111000 000 1011"),
        R("office, traveller at the desk",          false,  true,  BoothPhase.TravellerAtDesk, false, false, "0111111 111 1011"),
        R("wheel open",                             false,  true,  BoothPhase.TravellerAtDesk, true,  false, "0000010 001 0000"),
        R("frame open, screen on",                  true,   true,  BoothPhase.TravellerAtDesk, false, false, "1010000 000 0001"),
        R("frame open, screen off",                 true,   false, BoothPhase.TravellerAtDesk, false, false, "0010000 000 0001"),
        R("frame open, no traveller",               true,   true,  BoothPhase.NoTraveller,     false, false, "1010000 000 0001"),
        R("desk view, traveller at the desk",       false,  true,  BoothPhase.TravellerAtDesk, false, true,  "0111111 111 1101"),
        R("desk view, no traveller",                false,  true,  BoothPhase.NoTraveller,     false, true,  "0111000 000 1101"),
        R("desk view, wheel open",                  false,  true,  BoothPhase.TravellerAtDesk, true,  true,  "0000010 001 0000"),
        R("desk view, frame open",                  true,   true,  BoothPhase.TravellerAtDesk, false, true,  "1010000 000 0001"),
        R("desk view, newsletter",                  false,  true,  BoothPhase.Newsletter,      false, true,  "0000000 000 0000"),
    };

    /// <summary>The outputs in the order of the expected strings.</summary>
    private static bool[] Outputs(BoothInput o) => new[]
    {
        o.DesktopInteractive, o.CrtFocusable, o.PowerButtonLive,
        o.PropsLive, o.PapersLive, o.WheelAllowed, o.TravellerLive,
        o.StampsLive, o.InspectLive, o.CaseHudVisible,
        o.DeskViewAllowed, o.DeskViewBackLive, o.NormalViewLive, o.PcSwitchLive
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
    public void PapersLive_LikeProps_WhileATravellerIsAtTheDesk_InEitherView() => CheckColumn(4, "PapersLive");

    [Test]
    public void WheelAllowed_InTheOfficeView_WhileATravellerIsAtTheDesk() => CheckColumn(5, "WheelAllowed");

    [Test]
    public void TravellerLive_WhenTheWheelIsAllowedButClosed() => CheckColumn(6, "TravellerLive");

    [Test]
    public void StampsLive_LikeThePapers() => CheckColumn(7, "StampsLive");

    [Test]
    public void InspectLive_LikeThePapers() => CheckColumn(8, "InspectLive");

    [Test]
    public void CaseHudVisible_NotWhileFocused() => CheckColumn(9, "CaseHudVisible");

    [Test]
    public void DeskViewAllowed_UnlessANewsletterTheWheelOrTheFrameIsUp() => CheckColumn(10, "DeskViewAllowed");

    [Test]
    public void DeskViewBackLive_InTheDeskView_WhenThePropsAreLive() => CheckColumn(11, "DeskViewBackLive");

    [Test]
    public void NormalViewLive_InTheNormalView_WhenThePropsAreLive() => CheckColumn(12, "NormalViewLive");

    [Test]
    public void PcSwitchLive_BackFromThePc_OrToItWithTheWheelClosed() => CheckColumn(13, "PcSwitchLive");

    /// <summary>The documents, the stamps and inspect mode are live in the same states (one input model: none of them waits for another).</summary>
    [Test]
    public void ThePapersTheStampsAndInspect_AreLiveTogether_InAnyContext()
    {
        foreach (BoothContext c in AllContexts())
        {
            BoothInput o = BoothRules.Evaluate(c);
            Assert.AreEqual(o.PapersLive, o.StampsLive, c.ToString());
            Assert.AreEqual(o.PapersLive, o.InspectLive, c.ToString());
        }
    }

    /// <summary>The reading view changes only its own ways (the Back control and the normal view's ways), in any context.</summary>
    [Test]
    public void TheDeskView_ChangesOnlyItsOwnWays_InAnyContext()
    {
        foreach (BoothContext c in AllContexts())
        {
            bool[] off = Outputs(BoothRules.Evaluate(new BoothContext(c.Focused, c.ScreenOn, c.Phase, c.WheelOpen, false)));
            bool[] on = Outputs(BoothRules.Evaluate(new BoothContext(c.Focused, c.ScreenOn, c.Phase, c.WheelOpen, true)));
            for (int i = 0; i < off.Length; i++)
                if (i != 11 && i != 12)
                    Assert.AreEqual(off[i], on[i], $"{c.Phase}: output {i} must not depend on the reading view");
        }
    }

    private static IEnumerable<BoothContext> AllContexts()
    {
        foreach (BoothPhase phase in (BoothPhase[])Enum.GetValues(typeof(BoothPhase)))
            for (int bits = 0; bits < 16; bits++)
                yield return new BoothContext((bits & 1) != 0, (bits & 2) != 0, phase, (bits & 4) != 0, (bits & 8) != 0);
    }

    [Test]
    public void AnOpenWheel_StaysAllowed_SoOpeningItNeverClosesIt_ButTheTravellerGoesInert()
    {
        BoothInput open = BoothRules.Evaluate(new BoothContext(false, true, BoothPhase.TravellerAtDesk, true, false));
        Assert.IsTrue(open.WheelAllowed);
        Assert.IsFalse(open.TravellerLive);
    }

    /// <summary>The frame covers the left of the office and its exits sit around it, so nothing behind it may take input while it is open: no prop, document, PC click, wheel, traveller, stamp bar, inspect mode, case HUD, mat or reading view; only the PC switch (back to the desk) stays.</summary>
    [Test]
    public void WithTheFrameOpen_NothingInTheOfficeTakesInput_InAnyContext()
    {
        var wrong = new List<string>();
        foreach (BoothContext c in AllContexts())
        {
            if (!c.Focused)
                continue;
            BoothInput o = BoothRules.Evaluate(c);
            if (o.CrtFocusable || o.PropsLive || o.PapersLive || o.WheelAllowed || o.TravellerLive || o.StampsLive || o.InspectLive ||
                o.CaseHudVisible || o.DeskViewAllowed || o.DeskViewBackLive || o.NormalViewLive)
                wrong.Add($"{c.Phase}, screen {c.ScreenOn}, wheel {c.WheelOpen}, desk view {c.DeskView}");
            Assert.AreEqual(c.Phase != BoothPhase.Newsletter, o.PcSwitchLive, "the PC switch goes back to the desk");
        }

        Assert.IsEmpty(wrong, $"Something behind the open frame takes input: {string.Join("; ", wrong)}");
    }

    [Test]
    public void TheDefaultContext_IsTheOfficeViewWithNoTraveller()
    {
        BoothContext c = default;
        Assert.AreEqual(BoothPhase.NoTraveller, c.Phase, "NoTraveller is the first phase, the default before Start");
        Assert.IsFalse(c.DeskView, "the normal view is the default");
        CollectionAssert.AreEqual(new[] { false, true, true, true, false, false, false, false, false, false, true, false, true, true }, Outputs(BoothRules.Evaluate(c)));
    }
}
