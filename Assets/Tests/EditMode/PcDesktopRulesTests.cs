using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// UI-PC's pure rules (FIX_PLAN v1 §3 UI-PC "Tests", v2 delta): the PC's
/// back-out (Esc brings Settings forward, a right-click never leaves the PC)
/// and what its keyboard polls (nothing while the InputGate is held), one action per input (the Order debounce), the mid-shift quit warning,
/// the inbox without its automatic copies, the flash budget (G3), the CRT's
/// power-on line, a scanned page's fit under the text zoom (CODE-111), Orders'
/// inputs, and the tray clock's date (24:00 is the shift clock's own).
/// </summary>
public class PcDesktopRulesTests
{
    // --- The back-out (BUG-50a, R3/R4) ---

    [Test]
    public void BackOut_TheChainFirst_OneStepPerPress()
    {
        foreach (DesktopEscape step in new[] { DesktopEscape.CloseMenu, DesktopEscape.CloseCard, DesktopEscape.LeaveField, DesktopEscape.CloseStartMenu, DesktopEscape.CancelDrag })
        {
            Assert.AreEqual(PcBackOutStep.Chain, PcBackOut.Resolve(step, true), step + " (Esc)");
            Assert.AreEqual(PcBackOutStep.Chain, PcBackOut.Resolve(step, false), step + " (right-click)");
        }
    }

    [Test]
    public void BackOut_EscWithNothingToClose_OpensSettings_NeverLeaves()
    {
        Assert.AreEqual(PcBackOutStep.OpenSettings, PcBackOut.Resolve(DesktopEscape.None, true));
    }

    [Test]
    public void BackOut_RightClickWithNothingToClose_IsSwallowed_NeverLeaves()
    {
        Assert.AreEqual(PcBackOutStep.Swallow, PcBackOut.Resolve(DesktopEscape.None, false));
    }

    // --- One action per input (BUG-58a: "places then cancels on a double-click") ---

    [Test]
    public void Debounce_DoubleClick_ActsOnce()
    {
        var debounce = new ActionDebounce(0.4f);
        Assert.IsTrue(debounce.TryAct(10f), "the first click orders");
        Assert.IsFalse(debounce.TryAct(10.25f), "the double-click's second click does not cancel it");
        Assert.IsTrue(debounce.TryAct(10.5f), "a deliberate later click acts");
    }

    [Test]
    public void Debounce_TheSameFrame_ActsOnce()
    {
        var debounce = new ActionDebounce(0.4f);
        Assert.IsTrue(debounce.TryAct(3f), "Enter via the desktop's keys");
        Assert.IsFalse(debounce.TryAct(3f), "the same Enter via the EventSystem's submit");
    }

    [Test]
    public void Debounce_ZeroOrNegative_ActsEveryTime_AndAClockGoingBackDoesNotBlock()
    {
        var none = new ActionDebounce(-1f);
        Assert.AreEqual(0f, none.Seconds);
        Assert.IsTrue(none.TryAct(1f));
        Assert.IsTrue(none.TryAct(1f));

        var debounce = new ActionDebounce(0.4f);
        Assert.IsTrue(debounce.TryAct(50f));
        Assert.IsTrue(debounce.TryAct(2f), "a new run's clock starts again");
    }

    // --- The mid-shift quit warning (BUG-35c) ---

    [Test]
    public void Quit_OutsideAShift_QuitsAtOnce()
    {
        var guard = new QuitGuard(4f);
        Assert.AreEqual(QuitStep.Quit, guard.Press(false, 1f));
    }

    [Test]
    public void Quit_DuringAShift_WarnsThenQuitsOnTheSecondPress()
    {
        var guard = new QuitGuard(4f);
        Assert.AreEqual(QuitStep.Warn, guard.Press(true, 1f));
        Assert.IsTrue(guard.Warning(2f));
        Assert.AreEqual(QuitStep.Quit, guard.Press(true, 2f));
    }

    [Test]
    public void Quit_TheWarningLapses_OrIsReset()
    {
        var guard = new QuitGuard(4f);
        Assert.AreEqual(QuitStep.Warn, guard.Press(true, 1f));
        Assert.IsFalse(guard.Warning(6f));
        Assert.AreEqual(QuitStep.Warn, guard.Press(true, 6f), "a lapsed warning warns again");
        guard.Reset();
        Assert.IsFalse(guard.Warning(6.5f));
        Assert.AreEqual(QuitStep.Warn, guard.Press(true, 6.5f), "the menu closed: the warning starts over");
    }

    // --- The inbox without its automatic copies (BUG-50a, UI-24) ---

    private static MailItem Mail(MailKind kind, int day, string id, params string[] body) =>
        new MailItem { Id = id, Day = day, Kind = kind, Body = body };

    [Test]
    public void Mail_DropsThePaper_AndEveryMemoButToday()
    {
        var items = new List<MailItem>
        {
            Mail(MailKind.CitationNotice, 5, "cite:5:2", "Wrong era"),
            Mail(MailKind.DirectiveMemo, 5, "memo:5", "Rule A"),
            Mail(MailKind.TimesIssue, 5, "times:5", "Headline"),
            Mail(MailKind.Delivery, 5, "delivery:5", "Scanner"),
            Mail(MailKind.DirectiveMemo, 4, "memo:4", "Rule A"),
            Mail(MailKind.TimesIssue, 4, "times:4"),
            Mail(MailKind.Authored, 4, "mail:welcome", "Hello"),
            Mail(MailKind.StrandingReport, 3, "stranding:1"),
            Mail(MailKind.DirectiveMemo, 3, "memo:3", "Rule B"),
        };
        CollectionAssert.AreEqual(new[] { "cite:5:2", "memo:5", "delivery:5", "mail:welcome", "stranding:1" },
                                  MailDigest.Of(items, 5).Select(m => m.Id).ToArray());
    }

    [Test]
    public void Mail_AnEmptyMemo_IsNotMailed_AndNullIsEmpty()
    {
        var items = new List<MailItem> { Mail(MailKind.DirectiveMemo, 2, "memo:2") };
        Assert.IsEmpty(MailDigest.Of(items, 2));
        Assert.IsEmpty(MailDigest.Of(null, 2));
    }

    // --- The flash budget (GAME_FEEL_BIBLE G3); its knobs are the caller's (DesktopConfigSO) ---

    [Test]
    public void Flash_AtMostThreeASecond()
    {
        var budget = new FlashBudget(3, 0.2f);
        Assert.AreEqual(FlashStyle.Flash, budget.Request(0f, false));
        Assert.AreEqual(FlashStyle.Flash, budget.Request(0.1f, false));
        Assert.AreEqual(FlashStyle.Flash, budget.Request(0.2f, false));
        Assert.AreEqual(FlashStyle.None, budget.Request(0.5f, false), "the fourth in one second draws nothing");
        Assert.AreEqual(FlashStyle.Flash, budget.Request(1.0f, false), "the first has left the second");
    }

    [Test]
    public void Flash_ReduceFlashing_IsAlwaysATint_CountedTheSame()
    {
        var budget = new FlashBudget(3, 0.2f);
        for (int i = 0; i < budget.PerSecond; i++)
            Assert.AreEqual(FlashStyle.Tint, budget.Request(i * 0.1f, true));
        Assert.AreEqual(FlashStyle.None, budget.Request(0.5f, true));
        Assert.AreEqual(0.2f, budget.TintSeconds, 1e-6f, "G3: a static tint held 200 ms");
    }

    [Test]
    public void Flash_TheBudgetIsTheCallers_AndNeverUnderOne()
    {
        var one = new FlashBudget(1, 0.5f);
        Assert.AreEqual(FlashStyle.Flash, one.Request(0f, false));
        Assert.AreEqual(FlashStyle.None, one.Request(0.9f, false), "a budget of one a second");
        Assert.AreEqual(0.5f, one.TintSeconds, 1e-6f);
        var none = new FlashBudget(0, -1f);
        Assert.AreEqual(1, none.PerSecond, "a budget is at least one flash a second");
        Assert.AreEqual(0f, none.TintSeconds);
    }

    // --- The CRT's power-on line and collapse (BUG-32c, F1); its knobs are the caller's (DesktopConfigSO) ---

    /// <summary>F1's numbers: 150 ms on, 120 ms off, a 300 ms Reduced Motion fade, a 2 % line, the hum 80 ms ahead, 2 frames of afterglow, the frame's own 150 ms fade.</summary>
    private static CrtPower Crt() => new CrtPower(0.15f, 0.12f, 0.3f, 0.02f, 0.08f, 2, 0.15f);

    /// <summary>The QA rig's capture step (30 fps), the step seqtools' cuts compares.</summary>
    private const float CaptureStep = 1f / 30f;

    /// <summary>The most the frame (63-77 % of the screen at A1 to A4) may change its alpha in one captured frame and stay under seqtools' cut (40 luma over 60 % of the pixels) against a room up to 160 luma away.</summary>
    private const float CutStep = 0.25f;

    [Test]
    public void Crt_Opening_TheFrameFadesIn_TheHumLeadsThePictureBy80ms_ThenALineGrowsToTheFullPicture()
    {
        CrtPower crt = Crt();
        CrtFrame f0 = crt.At(0f, true, false);
        Assert.AreEqual(CrtPower.FrameFirstStep, f0.Bezel, 1e-6f, "the press frame answers, but the frame is not there whole (F1: no frame-0 UI pop)");
        Assert.Greater(f0.Bezel, 0f);
        Assert.AreEqual(1f, f0.Veil, "the glass is dark while the frame comes");
        Assert.IsFalse(f0.Picture, "no picture under a glass that is not yet opaque: its camera's black clear would cut in");
        Assert.AreEqual(1f, crt.At(0.15f - 0.001f, true, false).Veil, "still dark just before the picture");
        Assert.IsFalse(crt.PictureStarted(0.149f, true, false));
        Assert.IsFalse(crt.HumStarted(0.069f, false), "the hum waits until 80 ms before the picture");
        Assert.IsTrue(crt.HumStarted(0.0701f, false), "F1: the degauss hum begins 80 ms before the picture");
        CrtFrame line = crt.At(0.15f, true, false);
        Assert.IsTrue(crt.PictureStarted(0.15f, true, false));
        Assert.AreEqual((1f, 0f, true), (line.Bezel, line.Veil, line.Picture), "the picture comes on as the dark glass clears: the tube's black is the glass's black");
        Assert.AreEqual(1f, line.X);
        Assert.AreEqual(0.02f, line.Y, 1e-5f, "a full-width line, not the full picture");
        Assert.Greater(crt.At(0.15f + 0.075f, true, false).Y, 0.5f, "OutCubic: most of the height early");
        CrtFrame end = crt.At(0.3f, true, false);
        Assert.AreEqual((1f, 1f, 0f), (end.X, end.Y, end.Veil));
        Assert.AreEqual(0.3f, crt.Seconds(true, false), 1e-6f, "the frame's fade, then the 150 ms power-on");
    }

    [Test]
    public void Crt_TheHumIsAlways80msAheadOfThePicture_EvenWhenTheFrameIsQuickerThanTheLead()
    {
        var quick = new CrtPower(0.15f, 0.12f, 0.3f, 0.02f, 0.08f, 2, 0.05f);
        Assert.IsTrue(quick.HumStarted(0f, false), "the lead is the longer: the hum starts on the press frame");
        Assert.IsFalse(quick.PictureStarted(0.079f, true, false));
        Assert.IsTrue(quick.PictureStarted(0.08f, true, false));
        Assert.AreEqual(1f, quick.At(0.06f, true, false).Bezel, "the frame is whole; the glass waits dark for the lead");
        Assert.AreEqual(1f, quick.At(0.06f, true, false).Veil);
        CrtPower reduced = Crt();
        Assert.IsTrue(reduced.HumStarted(0.15f - 0.08f, true), "Reduced Motion: 80 ms before the picture comes on at the fade's half");
        Assert.IsFalse(reduced.HumStarted(0.069f, true));
    }

    [Test]
    public void Crt_Closing_SqueezesToALineThenADot_TwoFramesOfAfterglowOnADarkGlass_ThenTheFrameFadesOut()
    {
        CrtPower crt = Crt();
        CrtFrame start = crt.At(0f, false, false);
        Assert.AreEqual((1f, 1f, 0f, true), (start.X, start.Y, start.Veil, start.Picture));
        CrtFrame line = crt.At(0.12f * 0.66f, false, false);
        Assert.AreEqual(1f, line.X, 1e-4f, "a full-width line");
        Assert.AreEqual(0.02f, line.Y, 1e-3f);
        CrtFrame end = crt.At(0.12f, false, false);
        Assert.AreEqual(0f, end.X, 1e-5f);
        Assert.AreEqual(0f, end.Y, 1e-5f);
        Assert.IsFalse(end.Picture, "the picture is gone at the end");
        Assert.AreEqual(1f, end.Veil, "the glass is dark when its camera goes off (never the room through the hole)");
        Assert.AreEqual(1f, end.Bezel);
        Assert.AreEqual(0.12f, crt.Seconds(false, false), 1e-6f);
        Assert.Greater(crt.Afterglow(0), crt.Afterglow(1), "the phosphor dot fades");
        Assert.Greater(crt.Afterglow(1), 0f);
        Assert.AreEqual(0f, crt.Afterglow(2), "F1: an afterglow of 2 frames");
        Assert.AreEqual(2, crt.AfterglowFrames);

        Assert.AreEqual(0.15f, crt.LeaveSeconds, 1e-6f, "then the frame fades out (F1: no last-frame pop)");
        CrtFrame leave0 = crt.Leaving(0f);
        Assert.AreEqual((1f, 1f, false), (leave0.Bezel, leave0.Veil, leave0.Picture));
        Assert.AreEqual(0.5f, crt.Leaving(0.075f).Bezel, 1e-5f);
        Assert.AreEqual(0f, crt.Leaving(0.15f).Bezel);
    }

    [Test]
    public void Crt_EveryCapturedFrame_StepsTheFrameLessThanACut()
    {
        CrtPower crt = Crt();
        foreach (bool reduced in new[] { false, true })
        {
            float last = 0f; // the frame is hidden before the press
            for (float t = 0f; t <= crt.Seconds(true, reduced) + CaptureStep; t += CaptureStep)
            {
                float bezel = crt.At(t, true, reduced).Bezel;
                Assert.LessOrEqual(bezel - last, CutStep, (reduced ? "RM" : "full") + " opening at " + t);
                last = bezel;
            }
            Assert.AreEqual(1f, last, "open: the frame is whole");
        }

        float shown = 1f;
        for (float t = 0f; t < crt.LeaveSeconds; t += CaptureStep)
        {
            float bezel = crt.Leaving(t).Bezel;
            Assert.LessOrEqual(shown - bezel, CutStep, "leaving at " + t);
            shown = bezel;
        }
        Assert.LessOrEqual(shown, CutStep, "the last frame drawn before the frame hides is nearly gone");

        shown = 1f;
        float fade = crt.Seconds(false, true);
        for (float t = 0f; t < fade; t += CaptureStep)
        {
            float bezel = crt.At(t, false, true).Bezel;
            Assert.LessOrEqual(shown - bezel, CutStep, "RM closing at " + t);
            shown = bezel;
        }
        Assert.LessOrEqual(shown, CutStep, "RM: the last frame drawn before the frame hides is nearly gone");
    }

    [Test]
    public void Crt_ReducedMotion_FadesThePictureWithTheFrame_NeverACut()
    {
        CrtPower crt = Crt();
        float fade = crt.Seconds(true, true);
        Assert.AreEqual(0.3f, fade, 1e-6f);
        Assert.AreEqual(fade, crt.Seconds(false, true), 1e-6f);

        CrtFrame open0 = crt.At(0f, true, true);
        Assert.AreEqual(CrtPower.FrameFirstStep, open0.Bezel, 1e-6f, "the press frame answers with the frame's first step");
        Assert.IsFalse(open0.Picture, "the picture never shows before the dark glass covers it");
        Assert.IsFalse(crt.At(fade / 2f - 0.001f, true, true).Picture);
        CrtFrame half = crt.At(fade / 2f, true, true);
        Assert.IsTrue(half.Picture);
        Assert.AreEqual((1f, 1f), (half.Bezel, half.Veil), "the picture comes on under a fully dark glass");
        CrtFrame open1 = crt.At(fade, true, true);
        Assert.AreEqual((1f, 0f, true), (open1.Bezel, open1.Veil, open1.Picture));

        CrtFrame close0 = crt.At(0f, false, true);
        Assert.AreEqual((1f, 0f, true), (close0.Bezel, close0.Veil, close0.Picture));
        CrtFrame closeHalf = crt.At(fade / 2f, false, true);
        Assert.IsFalse(closeHalf.Picture, "the picture goes only under a fully dark glass");
        Assert.AreEqual((1f, 1f), (closeHalf.Bezel, closeHalf.Veil));
        Assert.AreEqual(0f, crt.At(fade, false, true).Bezel);

        foreach (bool opening in new[] { true, false })
        {
            float last = Seen(crt.At(0f, opening, true)), lastFrame = crt.At(0f, opening, true).Bezel;
            for (float t = 0.005f; t < fade; t += 0.005f)
            {
                CrtFrame f = crt.At(t, opening, true);
                Assert.AreEqual((1f, 1f), (f.X, f.Y), "no motion at " + t);
                Assert.Less(System.Math.Abs(Seen(f) - last), 0.2f, "the picture fades, never cuts (" + (opening ? "open" : "close") + " at " + t + ")");
                Assert.Less(System.Math.Abs(f.Bezel - lastFrame), 0.2f, "the frame fades, never cuts at " + t);
                last = Seen(f);
                lastFrame = f.Bezel;
            }
        }
    }

    /// <summary>How much of the desktop's picture the eye sees: the picture under the dark glass (which fades with the frame).</summary>
    private static float Seen(CrtFrame f) => f.Picture ? 1f - f.Bezel * f.Veil : 0f;

    [Test]
    public void Crt_TheKnobsAreTheCallers()
    {
        var slow = new CrtPower(0.3f, 0.2f, 0.4f, 0.05f, 0f, 0, 0f);
        Assert.AreEqual(0.3f, slow.Seconds(true, false), 1e-6f, "no lead, no frame fade");
        Assert.AreEqual(0.05f, slow.At(0f, true, false).Y, 1e-6f, "its own line height, at once");
        Assert.AreEqual(0f, slow.At(0f, true, false).Veil);
        Assert.AreEqual(1f, slow.At(0f, true, false).Bezel, "no frame fade: the frame is whole at once");
        Assert.AreEqual(0.4f, slow.Seconds(false, true), 1e-6f);
        Assert.AreEqual(0f, slow.Afterglow(0), "no afterglow frames");
        Assert.AreEqual(0f, slow.LeaveSeconds);
        Assert.AreEqual(0f, slow.Leaving(0f).Bezel, "no frame fade: gone at once");
        var odd = new CrtPower(-1f, -1f, -1f, 2f, -1f, -3, -1f);
        Assert.AreEqual(0f, odd.Seconds(true, false));
        Assert.AreEqual((1f, 1f), (odd.At(0f, true, false).X, odd.At(0f, true, false).Y), "a zero-length move is at its end");
        Assert.AreEqual(0, odd.AfterglowFrames);
    }

    // --- A reversed move (Q or the bezel during the power-on, a click on the PC during the collapse): it picks up where the glass is, never a pop (F1) ---

    [Test]
    public void Crt_QDuringThePowerOn_ClosesFromWhereTheGlassIs()
    {
        CrtPower crt = Crt();
        for (float t = 0f; t < crt.Seconds(true, false); t += 0.005f)
        {
            CrtFrame now = crt.At(t, true, false);
            float from = crt.Resume(now, false, false, out bool leaving);
            Assert.AreEqual(!now.Picture, leaving, "no picture yet: nothing to collapse, so the frame fades out from its alpha (at " + t + ")");
            AssertSameGlass(now, leaving ? crt.Leaving(from) : crt.At(from, false, false), "closing from the power-on at " + t);
        }
        float whole = crt.Resume(crt.At(crt.Seconds(true, false), true, false), false, false, out bool fromWhole);
        Assert.IsFalse(fromWhole);
        Assert.AreEqual(0f, whole, 1e-4f, "from the whole picture: the whole collapse");
    }

    [Test]
    public void Crt_APressDuringTheCollapse_OpensFromWhereTheGlassIs()
    {
        CrtPower crt = Crt();
        for (float t = 0f; t < crt.Seconds(false, false) * 0.66f; t += 0.004f)
        {
            CrtFrame now = crt.At(t, false, false);
            float from = crt.Resume(now, true, false, out bool leaving);
            Assert.IsFalse(leaving, "opening never fades out");
            AssertSameGlass(now, crt.At(from, true, false), "the power-on from the collapse's line at " + t);
            Assert.IsTrue(crt.HumStarted(from, false), "the picture is on the glass: the hum answers the press at once");
        }

        CrtFrame dot = crt.At(crt.Seconds(false, false) * 0.9f, false, false);
        CrtFrame line = crt.At(crt.Resume(dot, true, false, out _), true, false);
        Assert.AreEqual((1f, 1f, 0f, true), (line.X, line.Bezel, line.Veil, line.Picture), "the dot opens from the power-on's full-width line");
        Assert.AreEqual(0.02f, line.Y, 1e-4f);
        Assert.LessOrEqual(line.Y - dot.Y, 0.02f, "the change stays inside a line of the glass");

        CrtFrame afterglow = crt.At(crt.Seconds(false, false), false, false);
        float lit = crt.Resume(afterglow, true, false, out _);
        CrtFrame power = crt.At(lit, true, false);
        Assert.AreEqual(1f, power.Bezel, "the whole frame stays whole");
        Assert.IsTrue(crt.PictureStarted(lit, true, false), "the dark glass powers on at once, with its line");
        Assert.AreEqual((1f, 0.02f), (power.X, power.Y), "the line, as a fresh power-on's");
        Assert.IsTrue(crt.HumStarted(lit, false));

        for (float t = 0.005f; t < crt.LeaveSeconds; t += 0.005f) // from t = 0 the frame is still whole: the afterglow's case above
        {
            CrtFrame now = crt.Leaving(t);
            CrtFrame back = crt.At(crt.Resume(now, true, false, out _), true, false);
            if (now.Bezel >= CrtPower.FrameFirstStep)
                AssertSameGlass(now, back, "the frame fades back in from its alpha at " + t);
            else
                Assert.AreEqual(CrtPower.FrameFirstStep, back.Bezel, 1e-4f, "fainter than the first step: the press frame's answer, never fainter");
        }
    }

    [Test]
    public void Crt_ReducedMotion_AReversedFadeTurnsBackFromWhereItIs()
    {
        CrtPower crt = Crt();
        float fade = crt.Seconds(true, true);
        foreach (bool opening in new[] { true, false })
            for (float t = 0f; t < fade; t += 0.005f)
            {
                CrtFrame now = crt.At(t, opening, true);
                CrtFrame back = crt.At(crt.Resume(now, !opening, true, out bool leaving), !opening, true);
                Assert.IsFalse(leaving, "Reduced Motion's close ends with its own fade");
                if (!opening && now.Bezel < CrtPower.FrameFirstStep)
                    Assert.AreEqual(CrtPower.FrameFirstStep, back.Bezel, 1e-4f, "fainter than the first step: the press frame's answer");
                else
                    AssertSameGlass(now, back, (opening ? "closing from the fade in" : "opening from the fade out") + " at " + t);
            }
    }

    /// <summary>The glass shows the same: the frame's alpha, the dark glass, the picture the eye sees, and its line when drawn.</summary>
    private static void AssertSameGlass(CrtFrame expected, CrtFrame actual, string at)
    {
        Assert.AreEqual(expected.Bezel, actual.Bezel, 1e-3f, "the frame's alpha, " + at);
        Assert.AreEqual(expected.Veil, actual.Veil, 1e-3f, "the dark glass, " + at);
        Assert.AreEqual(Seen(expected), Seen(actual), 1e-3f, "the picture seen, " + at);
        if (expected.Picture && actual.Picture)
        {
            Assert.AreEqual(expected.X, actual.X, 1e-3f, "the line's width, " + at);
            Assert.AreEqual(expected.Y, actual.Y, 1e-3f, "the line's height, " + at);
        }
    }

    // --- A scanned page's fit and the pane's text zoom (CODE-111): the zoom is never cancelled, and a page wider than its pane scrolls sideways instead of being cut ---

    [Test]
    public void PageFit_AtOneHundredPercent_FitsThePaneWidth()
    {
        PageFitResult fit = PageFit.Of(600f, 900f, 1f, 600f, 0.75f, 1.5f);
        Assert.AreEqual(1f, fit.Scale, 1e-5f);
        Assert.AreEqual(600f, fit.Width, 1e-3f);
        Assert.AreEqual(900f, fit.Height, 1e-3f);
        Assert.IsFalse(fit.Sideways);
        PageFitResult wide = PageFit.Of(840f, 900f, 1f, 600f, 0.75f, 1.5f);
        Assert.AreEqual(1.4f, wide.Scale, 1e-5f, "a wide pane shows the page larger");
        Assert.IsFalse(wide.Sideways);
    }

    [Test]
    public void PageFit_TheTextZoomIsNotCancelled_ThePageScrollsSideways()
    {
        // The pane zoom lays its content out at the pane's width over the zoom (PaneZoom), so the page's parent is 400 wide at 150 %.
        PageFitResult zoomed = PageFit.Of(400f, 600f, 1.5f, 600f, 0.75f, 1.5f);
        Assert.AreEqual(1f, zoomed.Scale, 1e-5f, "the fit is the pane's own width (600), not its zoomed layout width (400)");
        Assert.AreEqual(1.5f, zoomed.Scale * 1.5f, 1e-5f, "on screen the page is 150 % of its 100 % size (CODE-111: the two scales used to cancel)");
        Assert.AreEqual(400f, zoomed.Width, 1e-3f, "the page's box fills its parent, never past it (no cut edge)");
        Assert.IsTrue(zoomed.Sideways, "the page is wider than its box: it scrolls sideways");
    }

    [Test]
    public void PageFit_UnderTheFloor_ThePageScrollsSideways_NeverCut()
    {
        PageFitResult narrow = PageFit.Of(300f, 600f, 1f, 600f, 0.75f, 1.5f);
        Assert.AreEqual(0.75f, narrow.Scale, 1e-5f, "the floor holds");
        Assert.AreEqual(400f, narrow.Width, 1e-3f, "the box is the parent's width at that scale");
        Assert.IsTrue(narrow.Sideways);
        PageFitResult none = PageFit.Of(0f, 0f, 0f, 0f, 0f, 0f);
        Assert.Greater(none.Scale, 0f, "never a zero scale");
    }

    // --- Orders' inputs (BUG-58a; the review of the fix round: a card click that ordered refused the next Enter) ---

    [Test]
    public void OrderInputs_ACardClick_NeverBlocksTheButtonOrEnter()
    {
        var gate = new OrderInputs(0.4f);
        Assert.IsTrue(gate.TryAct(OrderInput.Card, 10f), "a click on an orderable card orders");
        Assert.IsTrue(gate.TryAct(OrderInput.Key, 10.1f), "Enter right after it is the clerk's own second act");
        Assert.IsTrue(gate.TryAct(OrderInput.Button, 10.2f), "so is the button");
    }

    [Test]
    public void OrderInputs_EachInputActsOncePerDoubleClickTime()
    {
        var gate = new OrderInputs(0.4f);
        Assert.IsTrue(gate.TryAct(OrderInput.Button, 1f));
        Assert.IsFalse(gate.TryAct(OrderInput.Button, 1.2f), "a double-click on the button acts once (order, never then cancel)");
        Assert.IsTrue(gate.TryAct(OrderInput.Key, 1.2f), "another input is its own");
        Assert.IsFalse(gate.TryAct(OrderInput.Key, 1.3f), "two quick Enters act once");
        Assert.IsTrue(gate.TryAct(OrderInput.Key, 1.7f));
        Assert.IsTrue(gate.TryAct(OrderInput.Card, 1.71f));
        Assert.IsTrue(gate.TryAct(OrderInput.Card, 1.72f), "a card click acts only on an orderable card, so its double-click cannot undo it (OrdersWindow)");
        Assert.IsFalse(gate.TryAct(OrderInput.Button, 1.39f), "the button's own clock still runs from its action at 1 s");
        Assert.IsTrue(gate.TryAct(OrderInput.Button, 1.41f), "past its double-click time the button acts again");
    }

    // --- The back-out with the screen off (R3/R4: "Esc opens the same Settings window everywhere") ---

    [Test]
    public void BackOut_ScreenOff_EscWakesTheScreenAndOpensSettings_NeverLeaves()
    {
        Assert.AreEqual(PcBackOutStep.WakeAndOpenSettings, PcBackOut.ScreenOff(true));
    }

    [Test]
    public void BackOut_ScreenOff_ARightClickIsSwallowed_NeverLeaves()
    {
        Assert.AreEqual(PcBackOutStep.Swallow, PcBackOut.ScreenOff(false));
    }

    // --- What the PC's keyboard polls in a frame (RUN-SAFE's BUG-68a contract: while the cheat panel holds the InputGate, "the PC's input handlers stand still") ---

    [Test]
    public void Poller_GateHeld_NothingRuns_InEveryPcState()
    {
        foreach (bool interactive in new[] { false, true })
        foreach (bool open in new[] { false, true })
        foreach (bool lit in new[] { false, true })
            Assert.AreEqual(PcPoller.None, PcBackOut.Poller(true, interactive, open, lit),
                $"interactive {interactive}, open {open}, screen on {lit}: an Esc under the cheat panel must not bring Settings forward behind it");
    }

    [Test]
    public void Poller_GateFree_TheDesktopWhileItTakesInput_TheScreenOffBackOutWhileDark_ElseNothing()
    {
        Assert.AreEqual(PcPoller.Desktop, PcBackOut.Poller(false, true, true, true), "the desktop takes input (from the press frame)");
        Assert.AreEqual(PcPoller.ScreenOff, PcBackOut.Poller(false, false, true, false), "the PC open with its screen off");
        Assert.AreEqual(PcPoller.None, PcBackOut.Poller(false, false, true, true), "open and lit but taking no input (under a newsletter)");
        Assert.AreEqual(PcPoller.None, PcBackOut.Poller(false, false, false, false), "the PC closed, its screen off");
        Assert.AreEqual(PcPoller.None, PcBackOut.Poller(false, false, false, true), "the PC closed");
    }

    // --- One press, one sound (BUG-21c, F2), and a switch's own cue (F3: "ui_toggle on each switch") ---

    [Test]
    public void Press_OnASwitch_IsTheToggleCue_OtherwiseTheMouseClick()
    {
        Assert.AreEqual(SoundCues.UiToggle, PcPressCue.For(true), "a Settings switch: its toggle, not a second click");
        Assert.AreEqual(SoundCues.MouseClick, PcPressCue.For(false), "anything else on the PC: the desktop's one mouse click");
    }

    // --- The Orders card the pointer passes over (UI-25: "the detail on hover") never moves what the action acts on ---

    [Test]
    public void OrderPick_HoverShowsACard_TheActionStaysOnTheChosenOne()
    {
        var pick = new OrderPick();
        Assert.IsTrue(pick.Choose("scanner"));
        Assert.AreEqual("scanner", pick.Target);
        Assert.IsTrue(pick.Hover("lamp"), "the detail card changes");
        Assert.AreEqual("lamp", pick.Shown, "the detail card shows the card under the pointer");
        Assert.AreEqual("scanner", pick.Target, "the action still acts on the chosen card");
        Assert.IsTrue(pick.Hover("crate"));
        Assert.IsTrue(pick.Leave("crate"), "the pointer left the cards on its way to the action button");
        Assert.AreEqual("scanner", pick.Shown, "the detail card is the chosen card again under the action button");
        Assert.AreEqual("scanner", pick.Target);
    }

    [Test]
    public void OrderPick_ALeaveOfAnotherCard_OrNothingNew_ChangesNothing()
    {
        var pick = new OrderPick();
        Assert.IsNull(pick.Shown);
        Assert.IsFalse(pick.Leave("lamp"), "nothing hovered");
        Assert.IsTrue(pick.Hover("lamp"));
        Assert.IsFalse(pick.Hover("lamp"), "the same card again");
        Assert.IsFalse(pick.Leave("crate"), "a late leave of a card the pointer already left");
        Assert.AreEqual("lamp", pick.Shown);
        Assert.IsNull(pick.Target, "hovering chooses nothing");
        Assert.IsTrue(pick.Choose("lamp"), "a click chooses");
        Assert.IsFalse(pick.Choose("lamp"));
        Assert.AreEqual("lamp", pick.Target);
    }

    // --- Proofs and hints (BUG-47b): only proofs look like proofs ---

    [Test]
    public void Shade_AProvedDifference_IsAProof()
    {
        Assert.AreEqual(FindingShade.Proof, FindingShades.Of(FindingKind.Differs, true));
        Assert.AreEqual(FindingShade.Proof, FindingShades.Of(FindingKind.Elsewhere, true));
        Assert.AreEqual(FindingShade.Proof, FindingShades.Of(FindingKind.SealIncorrect, true));
    }

    [Test]
    public void Shade_DirectiveEvidence_IsAProof_WithoutADeviation()
    {
        foreach (FindingKind kind in new[] { FindingKind.RuleBroken, FindingKind.NotToday, FindingKind.Expired, FindingKind.PaperMissing })
            Assert.AreEqual(FindingShade.Proof, FindingShades.Of(kind, false), kind.ToString());
    }

    [Test]
    public void Shade_AnUnprovedDifference_IsOnlyAHint()
    {
        Assert.AreEqual(FindingShade.Hint, FindingShades.Of(FindingKind.Differs, false), "'Data differs' that proves nothing is not red");
        Assert.AreEqual(FindingShade.Hint, FindingShades.Of(FindingKind.NoRecord, false), "NO RECORD is never a proof");
        Assert.AreEqual(FindingShade.Hint, FindingShades.Of(FindingKind.SealIncorrect, false));
    }

    [Test]
    public void Shade_MatchesAndNotes_KeepTheirLooks()
    {
        Assert.AreEqual(FindingShade.Match, FindingShades.Of(FindingKind.Match, false));
        Assert.AreEqual(FindingShade.Match, FindingShades.Of(FindingKind.StillValid, false));
        Assert.AreEqual(FindingShade.Info, FindingShades.Of(FindingKind.DifferentDetails, false));
        Assert.AreEqual(FindingShade.Info, FindingShades.Of(FindingKind.TwoTruths, true));
    }

    // --- The case board's overlay keeps the pair the clerk chose across a new scan (CODE-157) ---

    private static List<OverlaySource> Sources(params string[] titles) => titles.Select(t => new OverlaySource(t, null)).ToList();

    [Test]
    public void OverlayPick_ANewScanShiftsTheSources_TheChosenPairIsFoundByItsTitle()
    {
        List<OverlaySource> before = Sources("Passport", "Record", "Face");
        List<OverlaySource> after = Sources("Passport", "Work permit", "Record", "Face");
        (string title, int nth) record = OverlayPick.Key(before, 1);
        (string title, int nth) face = OverlayPick.Key(before, 2);
        Assert.AreEqual(2, OverlayPick.Find(after, record), "the record moved one place down; it is still the record");
        Assert.AreEqual(3, OverlayPick.Find(after, face));
    }

    [Test]
    public void OverlayPick_TwoPapersOfOneTitle_KeepTheirOrder_AndAGoneSourceIsNone()
    {
        List<OverlaySource> before = Sources("ID card", "ID card", "Record");
        (string title, int nth) second = OverlayPick.Key(before, 1);
        Assert.AreEqual(("ID card", 1), second);
        Assert.AreEqual(2, OverlayPick.Find(Sources("Passport", "ID card", "ID card"), second), "the second of its title");
        Assert.AreEqual(-1, OverlayPick.Find(Sources("ID card", "Record"), second), "no second ID card any more");
        Assert.AreEqual(-1, OverlayPick.Find(Sources("Record"), OverlayPick.Key(before, 0)));
        Assert.AreEqual(((string)null, 0), OverlayPick.Key(before, -1), "nothing chosen");
        Assert.AreEqual(-1, OverlayPick.Find(before, (null, 0)));
    }

    // --- The tray clock (BUG-20a) ---

    [Test]
    public void TrayClock_TheDateInCapitals_AndTheShiftClocksMidnight()
    {
        Assert.AreEqual("14 MAR 2150", TrayClock.Date("14 Mar 2150", 1));
        Assert.AreEqual("1 APR 2150", TrayClock.Date("14 Mar 2150", 19));
        Assert.IsNull(TrayClock.Date("not a date", 1));
        Assert.IsNull(TrayClock.Date("14 Mar 2150", 0));
        Assert.AreEqual("00:00", ShiftClock.Format(24f * 60f), "a night shift closing at 24:00 reads as the shift clock reads it");
        Assert.AreEqual("23:59", ShiftClock.Format(24f * 60f - 0.5f));
    }
}
