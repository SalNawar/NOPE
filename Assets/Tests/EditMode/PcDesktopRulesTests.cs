using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// UI-PC's pure rules (FIX_PLAN v1 §3 UI-PC "Tests", v2 delta): the PC's
/// back-out (Esc brings Settings forward, a right-click never leaves the PC),
/// one action per input (the Order debounce), the mid-shift quit warning,
/// the inbox without its automatic copies, the flash budget (G3), the CRT's
/// power-on line, and the tray clock's date (24:00 is the shift clock's own).
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

    // --- The flash budget (GAME_FEEL_BIBLE G3) ---

    [Test]
    public void Flash_AtMostThreeASecond()
    {
        var budget = new FlashBudget();
        Assert.AreEqual(FlashStyle.Flash, budget.Request(0f, false));
        Assert.AreEqual(FlashStyle.Flash, budget.Request(0.1f, false));
        Assert.AreEqual(FlashStyle.Flash, budget.Request(0.2f, false));
        Assert.AreEqual(FlashStyle.None, budget.Request(0.5f, false), "the fourth in one second draws nothing");
        Assert.AreEqual(FlashStyle.Flash, budget.Request(1.0f, false), "the first has left the second");
    }

    [Test]
    public void Flash_ReduceFlashing_IsAlwaysATint_CountedTheSame()
    {
        var budget = new FlashBudget();
        for (int i = 0; i < FlashBudget.PerSecond; i++)
            Assert.AreEqual(FlashStyle.Tint, budget.Request(i * 0.1f, true));
        Assert.AreEqual(FlashStyle.None, budget.Request(0.5f, true));
        Assert.AreEqual(0.2f, FlashBudget.TintSeconds, 1e-6f, "G3: a static tint held 200 ms");
    }

    // --- The CRT's power-on line (BUG-32c, F1) ---

    [Test]
    public void Crt_Opening_GrowsFromALineToTheFullPicture()
    {
        (float x0, float y0, float a0) = CrtPower.At(0f, true, false);
        Assert.AreEqual(1f, x0);
        Assert.AreEqual(CrtPower.LineHeight, y0, 1e-5f, "frame 0 is a line, not the full picture (no frame-0 pop)");
        Assert.AreEqual(1f, a0);
        (float _, float yMid, float _) = CrtPower.At(CrtPower.OpenSeconds / 2f, true, false);
        Assert.Greater(yMid, 0.5f, "OutCubic: most of the height early");
        Assert.AreEqual((1f, 1f, 1f), CrtPower.At(CrtPower.OpenSeconds, true, false));
        Assert.AreEqual(0.15f, CrtPower.OpenSeconds, 1e-6f, "F1: 150 ms");
    }

    [Test]
    public void Crt_Closing_SqueezesToALineThenADot()
    {
        Assert.AreEqual((1f, 1f, 1f), CrtPower.At(0f, false, false));
        (float xLine, float yLine, float _) = CrtPower.At(CrtPower.CloseSeconds * 0.66f, false, false);
        Assert.AreEqual(1f, xLine, 1e-4f, "a full-width line");
        Assert.AreEqual(CrtPower.LineHeight, yLine, 1e-3f);
        (float xEnd, float yEnd, float aEnd) = CrtPower.At(CrtPower.CloseSeconds, false, false);
        Assert.AreEqual(0f, xEnd, 1e-5f);
        Assert.AreEqual(0f, yEnd, 1e-5f);
        Assert.AreEqual(0f, aEnd);
    }

    [Test]
    public void Crt_ReducedMotion_FadesWithoutAnySqueeze()
    {
        for (float t = 0f; t <= CrtPower.ReducedSeconds; t += 0.02f)
        {
            (float x, float y, float _) = CrtPower.At(t, true, true);
            Assert.AreEqual((1f, 1f), (x, y), "no motion at " + t);
        }
        Assert.AreEqual(0f, CrtPower.At(0f, true, true).alpha);
        Assert.AreEqual(1f, CrtPower.At(CrtPower.ReducedSeconds, true, true).alpha);
        Assert.AreEqual(0f, CrtPower.At(CrtPower.ReducedSeconds, false, true).alpha);
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
