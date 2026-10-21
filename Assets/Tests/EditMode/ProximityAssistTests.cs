using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// The slide-in's decisions (desk v3; UNITY_PLAN 5.2 and the slide reviews):
/// only the nearest slider slides, a silhouette under the cursor wins at once
/// (a busy one too, keeping every other home), the one out keeps it by the
/// hysteresis; a hand-off retracts the loser at once and starts the winner
/// only once the loser is nearly seated, never later than winnerAfter, a
/// slider still going home hurried and never bouncing back up; out on the
/// trigger frame, home after the linger, arriving at speed with the seat
/// clack on the arrival frame; a busy machine stays as it is, stopping where
/// a paper finds it on its way home; a paper let go on a machine holds it
/// out while it lands; one taken in hand gives way, one gone from the desk is
/// home; the forced retract; Reduced Motion cuts; a track never passes its
/// felt gap; nothing pops; a step allocates nothing. Driven at 60 fps.
/// </summary>
public class ProximityAssistTests
{
    private const float Frame = 1f / 60f;
    private const string Scanner = DeskHomeMap.Scanner, Chip = DeskHomeMap.ChipReader, Torch = DeskHomeMap.UvTorch, Intercom = DeskHomeMap.Intercom, Glasses = DeskHomeMap.Glasses;

    private static SlideCandidate On(string id, int rank = 0) => new SlideCandidate(id, rank, false, 0f);

    private static SlideCandidate Foot(string id) => new SlideCandidate(id, -1, true, 0f);

    private static SlideCandidate Near(string id, float px) => new SlideCandidate(id, -1, false, px);

    /// <summary>The approved map with the intercom's home declaring its slide right (DESK-PLAN's open call), so its track and its mic can be driven.</summary>
    private static DeskHome[] IntercomSlides()
    {
        DeskHome[] homes = DeskHomeMap.Approved();
        homes.First(h => h.id == Intercom).slide = HomeSlide.Right;
        return homes;
    }

    /// <summary>A desk with its sliders attached (the scanner, the chip reader and the UV torch by default), what is busy, in hand or gone, a drag, and its clock.</summary>
    private sealed class Desk
    {
        public readonly ProximityAssist Assist;
        public readonly DeskSlides Slides;
        public readonly HashSet<string> Busy = new HashSet<string>(), Taken = new HashSet<string>(), Gone = new HashSet<string>();
        public readonly List<(int frame, SlideCue cue)> Heard = new List<(int, SlideCue)>();
        private readonly string[] _attached;
        public int FrameNo;
        public bool Still, Dragging;

        public Desk(DeskSlides slides = null, IReadOnlyList<DeskHome> homes = null, params string[] attached)
        {
            Slides = slides ?? DeskSlides.Approved();
            Assist = new ProximityAssist(Slides, homes ?? DeskHomeMap.Approved());
            _attached = attached.Length > 0 ? attached : new[] { Scanner, Chip, Torch };
        }

        /// <summary>One frame with the cursor finding <paramref name="found"/>; every attached slider is listed with its state.</summary>
        public void Step(params SlideCandidate[] found)
        {
            FrameNo++;
            var all = new List<SlideCandidate>();
            foreach (string id in _attached.Union(found.Select(f => f.Id)))
            {
                SlideCandidate c = found.FirstOrDefault(f => f.Id == id);
                bool busy = Busy.Contains(id), taken = Taken.Contains(id), present = !Gone.Contains(id);
                all.Add(c.Id != null ? new SlideCandidate(id, c.Over, c.OnFootprint, c.Distance, busy, taken, present) : SlideCandidate.Away(id, busy, taken, present));
            }
            Assist.Step(FrameNo * Frame, all, Dragging, Still);
            for (int i = 0; i < Assist.Cues.Count; i++)
                Heard.Add((FrameNo, Assist.Cues[i]));
        }

        /// <summary><paramref name="frames"/> frames with the cursor finding the same.</summary>
        public void Hold(int frames, params SlideCandidate[] found)
        {
            for (int i = 0; i < frames; i++)
                Step(found);
        }

        public int FirstFrame(string id, SlideCueKind kind) => Heard.Where(h => h.cue.Id == id && h.cue.Kind == kind).Select(h => h.frame).DefaultIfEmpty(-1).First();

        public float P(string id) => Assist.Pose(id).Position;

        public float Px(string id) => P(id) * Slides.Find(id).travelPx;
    }

    /// <summary>How many frames of <paramref name="a"/> and <paramref name="b"/> (screen px per frame) both move more than a quarter of a pixel.</summary>
    private static int BothMoving(IReadOnlyList<float> a, IReadOnlyList<float> b)
    {
        int both = 0;
        for (int i = 1; i < a.Count; i++)
            if (Math.Abs(a[i] - a[i - 1]) > 0.25f && Math.Abs(b[i] - b[i - 1]) > 0.25f)
                both++;
        return both;
    }

    [Test]
    public void Pick_ASilhouetteUnderTheCursor_WinsAtOnce_TheFrontMostFirst_WhateverIsOut()
    {
        var candidates = new[] { Foot(Scanner), On(Chip, 1), On(Torch, 0), Near(Glasses, 5f) };
        Assert.AreEqual(Torch, ProximityAssist.Pick(candidates, Scanner, 120f, 40f), "front-most silhouette, though the scanner is out and its footprint is under the cursor");
    }

    [Test]
    public void Pick_OverNoSilhouette_AFootprintWins_TheOneOutKeepingIt()
    {
        Assert.AreEqual(Chip, ProximityAssist.Pick(new[] { Foot(Scanner), Foot(Chip) }, Chip, 120f, 40f), "the one out keeps it");
        Assert.AreEqual(Scanner, ProximityAssist.Pick(new[] { Foot(Scanner), Foot(Chip) }, null, 120f, 40f), "else the first listed");
        Assert.AreEqual(Scanner, ProximityAssist.Pick(new[] { Foot(Scanner), Near(Chip, 0f) }, Chip, 120f, 40f), "a footprint before any distance");
    }

    [Test]
    public void Pick_ElseTheNearestWithinTheTrigger_TheOneOutKeepingItByTheHysteresis()
    {
        Assert.AreEqual(Chip, ProximityAssist.Pick(new[] { Near(Scanner, 90f), Near(Chip, 60f) }, null, 120f, 40f), "the nearest");
        Assert.IsNull(ProximityAssist.Pick(new[] { Near(Scanner, 121f) }, null, 120f, 40f), "outside the radius");
        Assert.AreEqual(Scanner, ProximityAssist.Pick(new[] { Near(Scanner, 150f) }, Scanner, 120f, 40f), "the one out keeps it within radius plus hysteresis");
        Assert.IsNull(ProximityAssist.Pick(new[] { Near(Scanner, 161f) }, Scanner, 120f, 40f));
        Assert.AreEqual(Scanner, ProximityAssist.Pick(new[] { Near(Scanner, 90f), Near(Chip, 60f) }, Scanner, 120f, 40f), "another only 30 px nearer: the one out keeps it");
        Assert.AreEqual(Chip, ProximityAssist.Pick(new[] { Near(Scanner, 110f), Near(Chip, 60f) }, Scanner, 120f, 40f), "nearer by more than the hysteresis: it takes it");
        Assert.IsNull(ProximityAssist.Pick(Array.Empty<SlideCandidate>(), Scanner, 120f, 40f));
    }

    [Test]
    public void Out_StartsOnTheTriggerFrame_MovesTheNext_SoundsTheShushAndTheKnockOnTime()
    {
        var desk = new Desk();
        desk.Step(Near(Scanner, 100f));
        Assert.AreEqual(1, desk.FirstFrame(Scanner, SlideCueKind.Unlatch), "the unlatch tick on the trigger frame");
        Assert.AreEqual(0f, desk.P(Scanner), 1e-6f);
        desk.Step(Near(Scanner, 100f));
        Assert.Less(desk.P(Scanner), 0f, "it moves on the next frame: it backs away first");
        desk.Hold(30, Near(Scanner, 100f));
        Assert.AreEqual(1 + (int)Math.Ceiling(0.04f / Frame - 1e-4f), desk.FirstFrame(Scanner, SlideCueKind.Slide), "the shush as the base moves (40 ms)");
        int peak = desk.FirstFrame(Scanner, SlideCueKind.Peak);
        Assert.AreEqual(1 + (int)Math.Ceiling(DeskSlides.Approved().Find(Scanner).peakSeconds / Frame), peak, "the knock at its peak");
        Assert.AreEqual(1f, desk.P(Scanner), 0.03f, "settled at its slid spot");
        Assert.AreEqual(SlidePhase.Out, desk.Assist.Phase(Scanner));
        Assert.AreEqual(Scanner, desk.Assist.Picked);
        Assert.IsTrue(desk.Assist.SlideDone(Scanner), "its slide has finished");
    }

    [Test]
    public void Back_AfterTheLinger_ArrivesAtSpeed_WithTheSeatClackOnTheArrivalFrame()
    {
        var desk = new Desk();
        desk.Hold(40, Near(Scanner, 100f));
        desk.Step();
        Assert.AreEqual(SlidePhase.Linger, desk.Assist.Phase(Scanner), "the cursor left: it waits");
        int left = desk.FrameNo;
        desk.Hold(40);
        int back = desk.FirstFrame(Scanner, SlideCueKind.Back);
        Assert.That(back - left, Is.InRange(9, 10), "home after 150 ms outside the trigger");
        int seat = desk.FirstFrame(Scanner, SlideCueKind.Seat);
        Assert.That(seat - back, Is.InRange(18, 19), "the clack 300 ms later");
        var assist = new Desk();
        assist.Hold(40, Near(Scanner, 100f));
        assist.Step();
        assist.Hold(back - left - 1);
        var path = new List<float>();
        for (int i = 0; i < 30; i++)
        {
            path.Add(assist.P(Scanner));
            assist.Step();
        }
        int arrival = path.FindIndex(p => p <= 1e-4f);
        Assert.Greater(path[arrival - 1] - path[arrival], 0.1f, "it arrives fast, not at zero speed");
        Assert.Greater(path[arrival + 1], 0f, "the seat bounce starts on the next frame");
        Assert.AreEqual(SlidePhase.Rest, assist.Assist.Phase(Scanner));
        Assert.AreEqual(0f, assist.P(Scanner), 1e-6f);
    }

    /// <summary>The scanner out, then a hand-off to the chip reader: both machines' screen px per frame, and the frames of the hand-off and of the winner's start.</summary>
    private static (List<float> scanner, List<float> chip, int handOff, int start, Desk desk) HandOff()
    {
        var desk = new Desk();
        desk.Hold(40, Near(Scanner, 50f), Near(Chip, 100f));
        var scanner = new List<float> { desk.Px(Scanner) };
        var chip = new List<float> { desk.Px(Chip) };
        int handOff = desk.FrameNo + 1;
        for (int i = 0; i < 40; i++)
        {
            desk.Step(Near(Scanner, 80f), On(Chip));
            scanner.Add(desk.Px(Scanner));
            chip.Add(desk.Px(Chip));
        }
        return (scanner, chip, handOff, desk.FirstFrame(Chip, SlideCueKind.Unlatch), desk);
    }

    [Test]
    public void HandOff_TheLoserRetractsAtOnce_TheWinnerStartsOnlyOnceTheLoserIsNearlySeated()
    {
        (List<float> scanner, List<float> chip, int handOff, int start, Desk desk) = HandOff();
        DeskSlides slides = DeskSlides.Approved();
        float travel = slides.Find(Scanner).travelPx;
        Assert.AreEqual(handOff, desk.FirstFrame(Scanner, SlideCueKind.HandOff), "the loser goes on the first frame the cursor touches the chip reader");
        Assert.Greater(start, handOff, "the winner waits");
        Assert.LessOrEqual((start - handOff) * Frame, slides.winnerAfter + 1e-4f, "never longer than the retract");
        Assert.Less(scanner[start - handOff + 1] / travel, slides.winnerBelow, "the loser is nearly seated when the winner starts");
        Assert.GreaterOrEqual(scanner[start - handOff] / travel, slides.winnerBelow, "and not before");
        for (int i = 0; i < scanner.Count; i++)
            Assert.IsFalse(scanner[i] / travel > 0.5f && chip[i] / slides.Find(Chip).travelPx > 0.5f, "two sliders are never both half out");
        Assert.AreEqual(SlidePhase.Rest, desk.Assist.Phase(Scanner));
        Assert.AreEqual(1f, desk.P(Chip), 0.03f, "the winner out");
        Assert.IsFalse(desk.Heard.Any(h => h.cue.Id == Scanner && h.cue.Kind == SlideCueKind.Seat), "no seat clack for the loser");
    }

    [Test]
    public void ASliderStillGoingHome_GivesWayAsAHandOffLoser_TheWinnerNeverWaitsLongerThanWinnerAfter_AndTheLoserNeverRisesAgain()
    {
        DeskSlides slides = DeskSlides.Approved();
        int handOffBoth = BothMoving(HandOff().scanner, HandOff().chip);
        int ceiling = (int)Math.Ceiling(slides.winnerAfter / Frame - 1e-4f);
        // The cursor reaches the chip reader at every frame of the scanner's way home: its ease-in from the first frame to the last, and its seat bounce.
        for (int backFrames = 0; backFrames <= 23; backFrames++)
        {
            var desk = new Desk();
            desk.Hold(40, Near(Scanner, 50f));
            desk.Hold(8);
            for (int i = 0; i < 3 && desk.Assist.Phase(Scanner) != SlidePhase.Back; i++)
                desk.Step();
            Assert.AreEqual(SlidePhase.Back, desk.Assist.Phase(Scanner), "the linger is over: it goes home");
            desk.Hold(backFrames);
            if (desk.Assist.Phase(Scanner) != SlidePhase.Back)
                continue;
            var scanner = new List<float> { desk.Px(Scanner) };
            var chip = new List<float> { desk.Px(Chip) };
            int summoned = desk.FrameNo + 1;
            for (int i = 0; i < 40; i++)
            {
                desk.Step(Near(Chip, 30f));
                scanner.Add(desk.Px(Scanner));
                chip.Add(desk.Px(Chip));
            }
            int start = desk.FirstFrame(Chip, SlideCueKind.Unlatch) - summoned;
            string at = $"the cursor at the chip reader {backFrames} frames into the scanner's way home";
            Assert.That(start, Is.InRange(0, ceiling), at + ": the winner starts within winnerAfter (DESK-HOMES 6: the first slide frame right after entry, the hand-off's wait at most)");
            Assert.Less(scanner[start + 1] / slides.Find(Scanner).travelPx, slides.winnerBelow, at + ": the loser is nearly seated (or at rest after its bounce) when the winner starts");
            for (int i = start + 2; i < scanner.Count; i++)
                Assert.LessOrEqual(scanner[i], scanner[i - 1] + 1e-4f, at + ": once the winner moves the loser never rises again (no seat bounce under it)");
            Assert.LessOrEqual(BothMoving(scanner, chip), Math.Max(1, handOffBoth), at + ": no more frames with both moving than a plain hand-off");
            for (int i = 0; i < scanner.Count; i++)
                Assert.IsFalse(scanner[i] / slides.Find(Scanner).travelPx > 0.5f && chip[i] / slides.Find(Chip).travelPx > 0.5f, at + ": two sliders are never both half out");
            Assert.IsTrue(desk.Heard.Any(h => h.cue.Id == Scanner && h.cue.Kind == SlideCueKind.Seat), at + ": the scanner still clacks home");
            Assert.AreEqual(1f, desk.P(Chip), 0.03f, at + ": the winner out");
        }
    }

    [Test]
    public void TheIntercomGoingHome_GivesWayToTheGlasses_OneMachineMovingAtATime()
    {
        DeskSlides slides = DeskSlides.Approved();
        var desk = new Desk(null, IntercomSlides(), Intercom, Glasses);
        desk.Hold(30, Near(Intercom, 40f));
        desk.Hold(10);
        desk.Hold(18);
        Assert.AreEqual(SlidePhase.Back, desk.Assist.Phase(Intercom), "on its way home: the end of its ease-in or its seat bounce (the review: it rose to 0.197 of its travel under the glasses)");
        var intercom = new List<float> { desk.Px(Intercom) };
        var glasses = new List<float> { desk.Px(Glasses) };
        int summoned = desk.FrameNo + 1;
        for (int i = 0; i < 30; i++)
        {
            desk.Step(On(Glasses));
            intercom.Add(desk.Px(Intercom));
            glasses.Add(desk.Px(Glasses));
        }
        int start = desk.FirstFrame(Glasses, SlideCueKind.Unlatch) - summoned;
        Assert.That(start, Is.InRange(0, (int)Math.Ceiling(slides.winnerAfter / Frame - 1e-4f)), "within winnerAfter");
        for (int i = start + 2; i < intercom.Count; i++)
            Assert.AreEqual(0f, intercom[i], 1e-4f, "the intercom rests before the glasses rise");
        Assert.AreEqual(0, BothMoving(intercom, glasses), "never both moving");
    }

    [Test]
    public void ACursorPassingBy_StillHearsTheKnock_WhenTheSpringPeaksDuringTheLinger()
    {
        var desk = new Desk();
        desk.Hold(3, Near(Scanner, 50f));
        desk.Hold(20);
        Assert.Greater(desk.FirstFrame(Scanner, SlideCueKind.Slide), 3, "the shush after the cursor left");
        Assert.Greater(desk.FirstFrame(Scanner, SlideCueKind.Peak), 3, "and the knock at the peak");
        Assert.AreEqual(SlidePhase.Back, desk.Assist.Phase(Scanner), "then home after the linger");
    }

    [Test]
    public void ReEnteringDuringTheLinger_KeepsItOut_WithNoNewUnlatch()
    {
        var desk = new Desk();
        desk.Hold(30, Near(Scanner, 50f));
        desk.Hold(5);
        desk.Hold(5, Near(Scanner, 50f));
        Assert.AreEqual(SlidePhase.Out, desk.Assist.Phase(Scanner));
        Assert.AreEqual(1, desk.Heard.Count(h => h.cue.Kind == SlideCueKind.Unlatch));
        Assert.AreEqual(1f, desk.P(Scanner), 0.03f);
    }

    [Test]
    public void ABusyMachine_StaysOutUntilEmpty_NeverLosesAHandOff_AndIsNotSummonedAtHome()
    {
        var desk = new Desk();
        desk.Hold(30, Near(Chip, 50f));
        desk.Busy.Add(Chip);
        desk.Hold(30);
        Assert.AreEqual(SlidePhase.Out, desk.Assist.Phase(Chip), "a passport in it: it stays out after the cursor left");
        desk.Hold(30, On(Scanner));
        Assert.AreEqual(SlidePhase.Out, desk.Assist.Phase(Chip), "it never retracts for another");
        Assert.AreEqual(SlidePhase.Out, desk.Assist.Phase(Scanner), "the other slides at once: only empty machines take turns");
        desk.Busy.Remove(Chip);
        desk.Hold(60, On(Scanner));
        Assert.AreEqual(SlidePhase.Rest, desk.Assist.Phase(Chip), "empty again and the cursor gone: home");

        var home = new Desk();
        home.Busy.Add(Scanner);
        home.Hold(20, On(Scanner));
        Assert.AreEqual(SlidePhase.Rest, home.Assist.Phase(Scanner), "a busy machine at home is not summoned");
    }

    [Test]
    public void ACursorOnABusyMachine_KeepsEveryOtherOneHome_OnlyTheNearestMayMove()
    {
        // The review's harness: the scanner busy, the cursor on its silhouette and 60 px from the chip reader's.
        var atHome = new Desk();
        atHome.Busy.Add(Scanner);
        atHome.Hold(30, On(Scanner), Near(Chip, 60f));
        Assert.AreEqual(Scanner, atHome.Assist.Picked, "the busy scanner is the cursor's");
        Assert.AreEqual(SlidePhase.Rest, atHome.Assist.Phase(Chip), "the chip reader stays home");
        Assert.AreEqual(SlidePhase.Rest, atHome.Assist.Phase(Scanner), "and the busy scanner is not summoned");

        var scanning = new Desk();
        scanning.Hold(30, Near(Scanner, 20f));
        scanning.Busy.Add(Scanner);
        scanning.Hold(60, On(Scanner), Near(Chip, 60f));
        Assert.AreEqual(SlidePhase.Rest, scanning.Assist.Phase(Chip), "a scan watched with the cursor on the scanner never brings the chip reader out");
        Assert.AreEqual(SlidePhase.Out, scanning.Assist.Phase(Scanner));
        Assert.AreEqual(-1, scanning.FirstFrame(Chip, SlideCueKind.Unlatch));

        var handOff = new Desk();
        handOff.Hold(30, Near(Chip, 40f));
        handOff.Busy.Add(Scanner);
        handOff.Hold(20, On(Scanner), Near(Chip, 60f));
        Assert.AreEqual(SlidePhase.Rest, handOff.Assist.Phase(Chip), "the chip reader out gives way to the cursor on the busy scanner");
        Assert.Greater(handOff.FirstFrame(Chip, SlideCueKind.HandOff), 0);
    }

    [Test]
    public void ABusyMachineOnItsWayHome_StopsWhereItStands_UntilEmpty_ThenGoesHome()
    {
        DeskSlides slides = DeskSlides.Approved();
        var desk = new Desk();
        desk.Hold(30, Near(Scanner, 50f));
        desk.Hold(10);
        desk.Hold(8);
        Assert.AreEqual(SlidePhase.Back, desk.Assist.Phase(Scanner));
        float shown = desk.P(Scanner);
        desk.Busy.Add(Scanner);
        desk.Step();
        Assert.AreEqual(SlidePhase.Held, desk.Assist.Phase(Scanner), "a paper on its way onto the bed: it stops");
        Assert.AreEqual(shown, desk.P(Scanner), 1e-6f, "where it was shown last frame (where the bed point was taken)");
        desk.Hold(90);
        Assert.AreEqual(shown, desk.P(Scanner), 1e-6f, "it stays while busy");
        desk.Busy.Remove(Scanner);
        desk.Step();
        Assert.AreEqual(SlidePhase.Back, desk.Assist.Phase(Scanner), "empty: home from there");
        Assert.AreEqual(shown, desk.P(Scanner), 0.02f, "no jump");
        desk.Hold(30);
        Assert.AreEqual(SlidePhase.Rest, desk.Assist.Phase(Scanner));
        Assert.Greater(desk.Heard.Count(h => h.cue.Id == Scanner && h.cue.Kind == SlideCueKind.Seat), 0, "it clacks home");

        var retracting = new Desk();
        retracting.Hold(30, Near(Scanner, 50f));
        retracting.Hold(3, On(Chip));
        Assert.AreEqual(SlidePhase.Retract, retracting.Assist.Phase(Scanner));
        retracting.Busy.Add(Scanner);
        retracting.Step(On(Chip));
        Assert.AreEqual(SlidePhase.Held, retracting.Assist.Phase(Scanner), "a hand-off loser a paper lands on stops too");
        Assert.Greater(retracting.P(Scanner), slides.winnerBelow);
    }

    [Test]
    public void APaperLetGoOnAMachine_HoldsItOutWhileItLands_ThenItGoesHome()
    {
        DeskSlides slides = DeskSlides.Approved();
        int hold = (int)Math.Ceiling(slides.dropHoldSeconds / Frame);
        var desk = new Desk { Dragging = true };
        desk.Hold(30, On(Scanner));
        desk.Dragging = false;
        desk.Step();
        desk.Hold(hold - 3);
        Assert.AreEqual(SlidePhase.Out, desk.Assist.Phase(Scanner), "drop and go: it stays out while the paper lands");
        desk.Hold(20);
        Assert.AreNotEqual(SlidePhase.Out, desk.Assist.Phase(Scanner), "then it lingers and goes home");

        var missed = new Desk { Dragging = true };
        missed.Hold(30, Near(Scanner, 50f));
        missed.Dragging = false;
        missed.Step();
        missed.Hold(12);
        Assert.AreEqual(SlidePhase.Back, missed.Assist.Phase(Scanner), "a paper let go beside it (not on it) holds nothing");

    }

    [Test]
    public void ALiftTakenInHand_GivesWayAtOnce_Smoothly_AndIsNotSummonedWhileHeld()
    {
        var desk = new Desk();
        desk.Hold(30, On(Torch));
        float before = desk.P(Torch);
        desk.Taken.Add(Torch);
        desk.Step(On(Torch));
        Assert.AreEqual(SlidePhase.Retract, desk.Assist.Phase(Torch), "it gives way as a hand-off loser does");
        Assert.AreEqual(before, desk.P(Torch), 0.05f, "no jump on the first frame");
        desk.Hold(10, On(Torch));
        Assert.AreEqual(SlidePhase.Rest, desk.Assist.Phase(Torch), "home, and not summoned while in hand");
        Assert.AreEqual(1, desk.Heard.Count(h => h.cue.Id == Torch && h.cue.Kind == SlideCueKind.Unlatch));
        desk.Taken.Remove(Torch);
        desk.Hold(3, On(Torch));
        Assert.AreEqual(SlidePhase.Out, desk.Assist.Phase(Torch), "put back: it slides again");
    }

    [Test]
    public void ASliderGoneFromTheDesk_IsHomeAtOnce()
    {
        var desk = new Desk();
        desk.Hold(30, Near(Scanner, 50f));
        desk.Gone.Add(Scanner);
        desk.Step(Near(Scanner, 50f));
        Assert.AreEqual(SlidePhase.Rest, desk.Assist.Phase(Scanner));
        Assert.AreEqual(0f, desk.Assist.Pose(Scanner).Shadow, 1e-6f, "nothing left behind");
    }

    [Test]
    public void AForcedRetract_TakesEveryEmptySliderHome_ABusyOneOnlyWithItsPaperRiding()
    {
        var desk = new Desk();
        desk.Hold(30, Near(Chip, 30f));
        desk.Assist.Retire(Chip, false);
        Assert.AreEqual(SlidePhase.Retract, desk.Assist.Phase(Chip), "closing time: it retracts at once, no linger");
        desk.Hold(10);
        Assert.AreEqual(SlidePhase.Rest, desk.Assist.Phase(Chip));
        Assert.IsFalse(desk.Heard.Any(h => h.cue.Id == Chip && h.cue.Kind == SlideCueKind.Seat), "no seat");

        var busy = new Desk();
        busy.Hold(30, Near(Chip, 30f));
        busy.Busy.Add(Chip);
        busy.Step();
        busy.Assist.Retire(Chip, false);
        busy.Hold(20);
        Assert.AreEqual(SlidePhase.Out, busy.Assist.Phase(Chip), "a passport docked that cannot ride: it stays until empty");
        busy.Assist.Retire(Chip, true);
        Assert.AreEqual(SlidePhase.Retract, busy.Assist.Phase(Chip), "its docked passport rides it home: one mover");
        busy.Hold(10);
        Assert.AreEqual(SlidePhase.Rest, busy.Assist.Phase(Chip), "busy all the way, it is never stopped on its way");
        busy.Assist.Retire(Torch, false);
        Assert.AreEqual(SlidePhase.Rest, busy.Assist.Phase(Torch), "one at home stays home");
    }

    [Test]
    public void ReducedMotion_CutsBothWays_OnTheTriggerFrame_AndAfterTheLinger_CuesStillSound()
    {
        var desk = new Desk { Still = true };
        desk.Step(Near(Scanner, 50f));
        Assert.AreEqual(1f, desk.P(Scanner), 1e-6f, "a one-frame cut to the full spot (the travel never scales)");
        Assert.AreEqual(1, desk.FirstFrame(Scanner, SlideCueKind.Unlatch));
        Assert.AreEqual(1, desk.FirstFrame(Scanner, SlideCueKind.Peak), "the arrival's knock on the cut");
        SlidePose pose = desk.Assist.Pose(Scanner);
        Assert.AreEqual(0f, pose.Squash + pose.Sway, 1e-6f, "no squash, no sway");
        Assert.IsTrue(desk.Assist.SlideDone(Scanner), "done at once");
        desk.Step(On(Chip));
        Assert.AreEqual(0f, desk.P(Scanner), 1e-6f, "the hand-off loser cuts home");
        Assert.AreEqual(1f, desk.P(Chip), 1e-6f, "and the winner cuts out on the same frame");
        desk.Step();
        Assert.AreEqual(1f, desk.P(Chip), 1e-6f, "it lingers");
        desk.Hold(12);
        Assert.AreEqual(0f, desk.P(Chip), 1e-6f, "home after the linger, at once");
        Assert.AreEqual(SlidePhase.Rest, desk.Assist.Phase(Chip));
        Assert.IsTrue(desk.Heard.Any(h => h.cue.Id == Chip && h.cue.Kind == SlideCueKind.Seat), "the clack still sounds");
    }

    [Test]
    public void ATrack_NeverPassesItsFeltGap_EvenWithAPlantedOvershoot()
    {
        DeskSlides slides = DeskSlides.Approved();
        DeskSlider scanner = slides.Find(Scanner);
        scanner.overshoot = 0.6f;
        scanner.damping = 0.16f;
        var assist = new ProximityAssist(slides, DeskHomeMap.Approved());
        float most = 0f;
        for (int i = 1; i < 40; i++)
        {
            assist.Step(i * Frame, new[] { Near(Scanner, 50f) }, false, false);
            most = Math.Max(most, assist.Pose(Scanner).Position * scanner.travel);
        }
        Assert.AreEqual(scanner.Reach(slides.feltGap), most, 1e-5f, "it stops 0.4 cm short of its inner lip");
    }

    [Test]
    public void APose_SquashesALift_PressesATracksShadowAtThePeak_AndCarriesTheShadows()
    {
        DeskSlides slides = DeskSlides.Approved();
        var desk = new Desk();
        desk.Step(On(Torch));
        desk.Step(On(Torch));
        desk.Step(On(Torch));
        Assert.Greater(desk.Assist.Pose(Torch).Squash, 0f, "a lift squashes over its anticipation");
        Assert.AreEqual(0f, desk.Assist.Pose(Torch).Shadow, 1e-6f, "its shadow is its own contact until it rises");
        desk.Hold(20, On(Torch));
        Assert.Greater(desk.Assist.Pose(Torch).Shadow, 0.4f, "the rest-footprint shadow shows under the lifted torch");
        Assert.Greater(desk.Assist.Pose(Torch).ShadowWiden, 1f, "wider as it rises");
        Assert.AreEqual(0f, desk.Assist.Pose(Torch).Squash, 1e-6f, "released as it rose");

        var track = new Desk();
        track.Hold(3, Near(Scanner, 50f));
        Assert.AreEqual(0f, track.Assist.Pose(Scanner).Shadow, 1e-6f, "in its well during the anticipation");
        int peak = (int)Math.Round(slides.Find(Scanner).peakSeconds / Frame);
        track.Hold(peak - 3, Near(Scanner, 50f));
        SlidePose pressed = track.Assist.Pose(Scanner);
        Assert.AreEqual(slides.trackShadowBlurPx * slides.peakTightBlur, pressed.ShadowBlur, 1e-5f, "at the peak the base presses the felt: the blur tightens 30 %");
        Assert.AreEqual(slides.trackShadowAlpha * slides.peakTightDarken, pressed.Shadow, 1e-5f, "and 10 % darker");
        track.Hold(10, Near(Scanner, 50f));
        Assert.AreEqual(slides.trackShadowAlpha, track.Assist.Pose(Scanner).Shadow, 1e-6f, "it travels with the base");
        Assert.AreEqual(slides.trackShadowBlurPx, track.Assist.Pose(Scanner).ShadowBlur, 1e-6f);
    }

    [Test]
    public void NothingPops_AReSummonDuringTheAnticipation_CarriesOnFromWhereItStands()
    {
        // The review: a scanner retracting from its anticipation, summoned again, jumped from -2.10 px to 0 in one frame, then dipped again.
        DeskSlides slides = DeskSlides.Approved();
        DeskSlider scanner = slides.Find(Scanner);
        var desk = new Desk();
        var path = new List<float> { 0f };
        desk.Step(Near(Scanner, 50f));
        path.Add(desk.Px(Scanner));
        desk.Step(Near(Scanner, 50f));
        path.Add(desk.Px(Scanner));
        desk.Step(On(Chip));
        path.Add(desk.Px(Scanner));
        Assert.Less(path[3], 0f, "it retracts from behind home");
        int summon = path.Count;
        for (int i = 0; i < 30; i++)
        {
            desk.Step(Near(Scanner, 20f));
            path.Add(desk.Px(Scanner));
        }
        float before = Math.Abs(path[summon - 1] - path[summon - 2]);
        Assert.LessOrEqual(Math.Abs(path[summon] - path[summon - 1]), before + 0.5f, "the summon frame moves no more than the frame before it");
        Assert.GreaterOrEqual(path.Min(), -scanner.anticipation * scanner.travelPx - 1e-3f, "never deeper than its own anticipation");
        Assert.AreEqual(scanner.travelPx, path[path.Count - 1], 0.03f * scanner.travelPx, "then out as ever");
    }

    [Test]
    public void NothingPops_TheSquashTheSwayAndTheShadow_CarryOverEveryChangeOfPhase()
    {
        DeskSlides slides = DeskSlides.Approved();
        float squashStep = slides.squashPx * Frame / slides.squashRelease + 0.02f;

        // A lift handed off at 50 ms, mid-squash: the squash releases at its own rate, it never vanishes in a frame.
        var torch = new Desk();
        torch.Hold(3, On(Torch));
        float squash = torch.Assist.Pose(Torch).Squash;
        Assert.Greater(squash, 1f);
        var squashes = new List<float> { squash };
        for (int i = 0; i < 10; i++)
        {
            torch.Step(On(Chip));
            squashes.Add(torch.Assist.Pose(Torch).Squash);
        }
        Assert.AreEqual(SlidePhase.Rest, torch.Assist.Phase(Torch));
        for (int i = 1; i < squashes.Count; i++)
            Assert.LessOrEqual(Math.Abs(squashes[i] - squashes[i - 1]), squashStep, $"squash frame {i}: released, not cut");

        // The intercom's mic handed off mid-sway, and brushed past (its first frame home): the frame of the change shows the sway the
        // undisturbed mic shows then, and from there it only settles (the review: it snapped from 2.54 px to 0).
        foreach (bool brush in new[] { false, true })
        {
            var desk = new Desk(null, IntercomSlides(), Intercom, Glasses);
            var twin = new Desk(null, IntercomSlides(), Intercom, Glasses);
            desk.Hold(10, Near(Intercom, 40f));
            twin.Hold(10, Near(Intercom, 40f));
            Assert.Greater(Math.Abs(desk.Assist.Pose(Intercom).Sway), 0.5f, "mid-sway");
            while (brush && desk.Assist.Phase(Intercom) != SlidePhase.Back)
            {
                desk.Step();
                twin.Step(Near(Intercom, 40f));
            }
            if (!brush)
            {
                desk.Step(On(Glasses));
                twin.Step(Near(Intercom, 40f));
                Assert.AreEqual(SlidePhase.Retract, desk.Assist.Phase(Intercom));
            }
            string how = brush ? "brushed past" : "handed off";
            float sway = desk.Assist.Pose(Intercom).Sway;
            Assert.AreEqual(twin.Assist.Pose(Intercom).Sway, sway, 1e-4f, how + ": the change's frame keeps the sway");
            Assert.Greater(Math.Abs(sway), 0.1f, how + ": not cut");
            for (int i = 0; i < 12; i++)
            {
                desk.Step(brush ? Array.Empty<SlideCandidate>() : new[] { On(Glasses) });
                float next = desk.Assist.Pose(Intercom).Sway;
                Assert.LessOrEqual(Math.Abs(next), Math.Abs(sway) + 1e-5f, how + ": then it only settles");
                Assert.IsTrue(next == 0f || Math.Sign(next) == Math.Sign(sway), how + ": the same way");
                sway = next;
            }
            Assert.AreEqual(0f, sway, 1e-6f, how + ": settled within its decay");
        }

        // A track's shadow over a hand-off at its fade-in, a re-summon on its way home and the arrival: never faster than its own fades.
        float shadowStep = slides.trackShadowAlpha * Frame / Math.Min(slides.trackShadowIn, slides.trackShadowOut) + 1e-4f;
        var scanner = new Desk();
        var alphas = new List<float> { 0f };
        void Watch(int n, params SlideCandidate[] found)
        {
            for (int i = 0; i < n; i++)
            {
                scanner.Step(found);
                alphas.Add(scanner.Assist.Pose(Scanner).Shadow);
            }
        }
        Watch(4, Near(Scanner, 30f));
        Watch(12, On(Chip));
        Watch(30, Near(Scanner, 30f));
        Watch(16);
        Watch(20, Near(Scanner, 30f));
        Watch(40);
        for (int i = 1; i < alphas.Count; i++)
            Assert.LessOrEqual(Math.Abs(alphas[i] - alphas[i - 1]), shadowStep * slides.peakTightDarken + 0.04f, $"shadow frame {i}: {alphas[i - 1]:0.000} to {alphas[i]:0.000}");
    }

    [Test]
    public void TheCursorLeavingAWaitingWinner_LeavesItHome()
    {
        var desk = new Desk();
        desk.Hold(30, Near(Scanner, 50f));
        desk.Step(On(Chip));
        desk.Hold(20);
        Assert.AreEqual(-1, desk.FirstFrame(Chip, SlideCueKind.Unlatch), "it never started");
        Assert.AreEqual(SlidePhase.Rest, desk.Assist.Phase(Chip));
    }

    [Test]
    public void FixedObjects_TracksWhoseHomeDeclaresNoSlide_AndUnknownIds_NeverMove()
    {
        var desk = new Desk(null, null, Scanner, DeskHomeMap.Detain, Intercom);
        desk.Hold(20, On(DeskHomeMap.Detain), On("nothing", 1), Near(Intercom, 10f));
        Assert.IsEmpty(desk.Heard);
        Assert.AreEqual(0f, desk.P(DeskHomeMap.Detain), 1e-6f);
        Assert.AreEqual(0f, desk.P(Intercom), 1e-6f, "the intercom's home declares no slide on today's map (DESK-PLAN's open call): it waits for the map");
        Assert.AreEqual(SlidePhase.Rest, desk.Assist.Pose(DeskHomeMap.Bell).Phase);
    }

    [Test]
    public void AStep_AllocatesNothing_EveryFrameOfTheOffice()
    {
        // MERGE_CRITERIA: no per-frame allocations (the [BP] gate bp_smoke gc.steady: 0 B). The desk's own loop: Step, the cues, each pose.
        var assist = new ProximityAssist(DeskSlides.Approved(), DeskHomeMap.Approved());
        string[] ids = { Scanner, Chip, Torch, Glasses, DeskHomeMap.Rolodex, DeskHomeMap.ProtocolRing };
        var frame = new List<SlideCandidate>(ids.Length);
        foreach (string id in ids)
            frame.Add(SlideCandidate.Away(id));
        int n = 0;
        float sink = 0f;
        void Run(int frames)
        {
            for (int f = 0; f < frames; f++, n++)
            {
                int phase = n / 40 % 6;
                for (int i = 0; i < ids.Length; i++)
                    frame[i] = phase == 5 ? SlideCandidate.Away(ids[i]) : i == phase % 3 ? new SlideCandidate(ids[i], 0, false, 0f, n % 97 < 10) : new SlideCandidate(ids[i], -1, false, 60f + 10f * i);
                assist.Step(n * Frame, frame, n % 50 < 25, false);
                IReadOnlyList<SlideCue> cues = assist.Cues;
                for (int c = 0; c < cues.Count; c++)
                    sink += (int)cues[c].Kind;
                for (int i = 0; i < ids.Length; i++)
                    sink += assist.Pose(ids[i]).Position;
                sink += ProximityAssist.Pick(frame, ids[0], 120f, 40f) != null ? 1f : 0f;
            }
        }
        Run(1200);
        long start = GC.GetAllocatedBytesForCurrentThread();
        Run(2400);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - start;
        Assert.AreEqual(0L, allocated, $"2400 frames of the slide-in allocated {allocated} B (sink {sink})");
    }
}
