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

    /// <summary>F1's numbers: 150 ms on, 120 ms off, a 120 ms fade, a 2 % line, the hum 80 ms ahead, 2 frames of afterglow.</summary>
    private static CrtPower Crt() => new CrtPower(0.15f, 0.12f, 0.12f, 0.02f, 0.08f, 2);

    [Test]
    public void Crt_Opening_TheHumLeads_TheGlassStaysDark_ThenALineGrowsToTheFullPicture()
    {
        CrtPower crt = Crt();
        CrtFrame f0 = crt.At(0f, true, false);
        Assert.AreEqual(1f, f0.Veil, "frame 0: the dark glass while the degauss hum begins (F1: 80 ms before the picture)");
        Assert.IsTrue(f0.Picture);
        Assert.AreEqual(1f, f0.Bezel, "the frame itself is there on the press frame (input follows at once)");
        Assert.AreEqual(1f, crt.At(0.079f, true, false).Veil, "still dark just before the lead ends");
        Assert.IsFalse(crt.PictureStarted(0.079f, true, false));
        CrtFrame line = crt.At(0.08f, true, false);
        Assert.AreEqual(0f, line.Veil, "the picture starts after the lead");
        Assert.IsTrue(crt.PictureStarted(0.08f, true, false));
        Assert.AreEqual(1f, line.X);
        Assert.AreEqual(0.02f, line.Y, 1e-5f, "it starts as a full-width line, not the full picture (no frame-0 pop)");
        Assert.Greater(crt.At(0.08f + 0.075f, true, false).Y, 0.5f, "OutCubic: most of the height early");
        CrtFrame end = crt.At(0.23f, true, false);
        Assert.AreEqual((1f, 1f, 0f), (end.X, end.Y, end.Veil));
        Assert.AreEqual(0.23f, crt.Seconds(true, false), 1e-6f, "the lead and the 150 ms power-on");
    }

    [Test]
    public void Crt_Closing_SqueezesToALineThenADot_ThenTwoFramesOfAfterglow()
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
        Assert.AreEqual(0.12f, crt.Seconds(false, false), 1e-6f);
        Assert.Greater(crt.Afterglow(0), crt.Afterglow(1), "the phosphor dot fades");
        Assert.Greater(crt.Afterglow(1), 0f);
        Assert.AreEqual(0f, crt.Afterglow(2), "F1: an afterglow of 2 frames");
        Assert.AreEqual(2, crt.AfterglowFrames);
    }

    [Test]
    public void Crt_ReducedMotion_FadesThePictureWithTheFrame_NeverACut()
    {
        CrtPower crt = Crt();
        float fade = crt.Seconds(true, true);
        Assert.AreEqual(0.12f, fade, 1e-6f);
        Assert.AreEqual(fade, crt.Seconds(false, true), 1e-6f);

        CrtFrame open0 = crt.At(0f, true, true);
        Assert.AreEqual(0f, open0.Bezel, "frame 0: nothing yet, the room shows");
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
            for (float t = 0.005f; t <= fade + 1e-4f; t += 0.005f)
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
        var slow = new CrtPower(0.3f, 0.2f, 0.4f, 0.05f, 0f, 0);
        Assert.AreEqual(0.3f, slow.Seconds(true, false), 1e-6f, "no lead");
        Assert.AreEqual(0.05f, slow.At(0f, true, false).Y, 1e-6f, "its own line height, at once");
        Assert.AreEqual(0f, slow.At(0f, true, false).Veil);
        Assert.AreEqual(0.4f, slow.Seconds(false, true), 1e-6f);
        Assert.AreEqual(0f, slow.Afterglow(0), "no afterglow frames");
        var odd = new CrtPower(-1f, -1f, -1f, 2f, -1f, -3);
        Assert.AreEqual(0f, odd.Seconds(true, false));
        Assert.AreEqual((1f, 1f), (odd.At(0f, true, false).X, odd.At(0f, true, false).Y), "a zero-length move is at its end");
        Assert.AreEqual(0, odd.AfterglowFrames);
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
