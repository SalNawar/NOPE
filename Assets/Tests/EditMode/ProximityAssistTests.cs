using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// The slide-in's decisions (desk v3; UNITY_PLAN 5.2 and the last slide
/// review): only the nearest slider slides, a silhouette under the cursor
/// wins at once, the one out keeps it by the hysteresis; a hand-off retracts
/// the loser at once and starts the winner only once the loser is nearly
/// seated, so one machine moves at a time and two are never both half out;
/// out on the trigger frame, home after the linger, arriving at speed with
/// the seat clack on the arrival frame; a busy machine stays as it is;
/// Reduced Motion cuts; a track never passes its felt gap. Driven at 60 fps.
/// </summary>
public class ProximityAssistTests
{
    private const float Frame = 1f / 60f;
    private const string Scanner = DeskHomeMap.Scanner, Chip = DeskHomeMap.ChipReader, Torch = DeskHomeMap.UvTorch;

    private static SlideCandidate On(string id, int rank = 0) => new SlideCandidate(id, rank, false, 0f);

    private static SlideCandidate Foot(string id) => new SlideCandidate(id, -1, true, 0f);

    private static SlideCandidate Near(string id, float px) => new SlideCandidate(id, -1, false, px);

    /// <summary>A desk with the scanner, the chip reader and the UV torch, and its clock.</summary>
    private sealed class Desk
    {
        public readonly ProximityAssist Assist = new ProximityAssist(DeskSlides.Approved());
        public readonly HashSet<string> Busy = new HashSet<string>();
        public readonly List<(int frame, SlideCue cue)> Heard = new List<(int, SlideCue)>();
        public int FrameNo;
        public bool Still;

        /// <summary>One frame with the cursor finding <paramref name="found"/>.</summary>
        public void Step(params SlideCandidate[] found)
        {
            FrameNo++;
            Assist.Step(FrameNo * Frame, found, Busy, Still);
            foreach (SlideCue cue in Assist.Cues)
                Heard.Add((FrameNo, cue));
        }

        /// <summary><paramref name="frames"/> frames with the cursor finding the same.</summary>
        public void Hold(int frames, params SlideCandidate[] found)
        {
            for (int i = 0; i < frames; i++)
                Step(found);
        }

        public int FirstFrame(string id, SlideCueKind kind) => Heard.Where(h => h.cue.Id == id && h.cue.Kind == kind).Select(h => h.frame).DefaultIfEmpty(-1).First();

        public float P(string id) => Assist.Position(id);
    }

    [Test]
    public void Pick_ASilhouetteUnderTheCursor_WinsAtOnce_TheFrontMostFirst_WhateverIsOut()
    {
        var candidates = new[] { Foot(Scanner), On(Chip, 1), On(Torch, 0), Near("glasses", 5f) };
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

    [Test]
    public void HandOff_TheLoserRetractsAtOnce_TheWinnerStartsOnlyOnceTheLoserIsNearlySeated()
    {
        var desk = new Desk();
        desk.Hold(40, Near(Scanner, 50f), Near(Chip, 100f));
        int handOff = desk.FrameNo + 1;
        var both = new List<(float scanner, float chip)>();
        for (int i = 0; i < 40; i++)
        {
            desk.Step(Near(Scanner, 80f), On(Chip));
            both.Add((desk.P(Scanner), desk.P(Chip)));
        }
        Assert.AreEqual(handOff, desk.FirstFrame(Scanner, SlideCueKind.HandOff), "the loser goes on the first frame the cursor touches the chip reader");
        int start = desk.FirstFrame(Chip, SlideCueKind.Unlatch);
        Assert.Greater(start, handOff, "the winner waits");
        Assert.LessOrEqual((start - handOff) * Frame, DeskSlides.Approved().winnerAfter + 1e-4f, "never longer than the retract");
        float loserAtStart = both[start - handOff].scanner;
        Assert.Less(loserAtStart, DeskSlides.Approved().winnerBelow, "the loser is nearly seated when the winner starts");
        Assert.GreaterOrEqual(both[start - handOff - 1].scanner, DeskSlides.Approved().winnerBelow, "and not before");
        foreach ((float scanner, float chip) in both)
            Assert.IsFalse(scanner > 0.5f && chip > 0.5f, "two sliders are never both half out");
        Assert.AreEqual(SlidePhase.Rest, desk.Assist.Phase(Scanner));
        Assert.AreEqual(1f, desk.P(Chip), 0.03f, "the winner out");
        Assert.IsFalse(desk.Heard.Any(h => h.cue.Id == Scanner && h.cue.Kind == SlideCueKind.Seat), "no seat clack for the loser");
    }

    [Test]
    public void ASliderStillGoingHome_HoldsTheNextOneBack_UntilItIsNearlySeated()
    {
        var desk = new Desk();
        desk.Hold(40, Near(Scanner, 50f));
        desk.Hold(12);
        Assert.AreEqual(SlidePhase.Back, desk.Assist.Phase(Scanner), "going home");
        int summoned = desk.FrameNo + 1;
        var scanner = new List<float>();
        var chip = new List<float>();
        for (int i = 0; i < 40; i++)
        {
            desk.Step(Near(Chip, 30f));
            scanner.Add(desk.P(Scanner));
            chip.Add(desk.P(Chip));
        }
        int start = desk.FirstFrame(Chip, SlideCueKind.Unlatch) - summoned;
        float below = DeskSlides.Approved().winnerBelow;
        Assert.Greater(start, 0, "it waited");
        Assert.Less(scanner[start], below, "until the scanner was nearly seated");
        Assert.GreaterOrEqual(scanner[start - 1], below, "and not a frame longer");
        for (int i = 0; i < scanner.Count; i++)
            Assert.IsFalse(scanner[i] > 0.5f && chip[i] > 0.5f, "two sliders are never both half out");
        Assert.IsTrue(desk.Heard.Any(h => h.cue.Id == Scanner && h.cue.Kind == SlideCueKind.Seat), "the scanner kept its own way home and its clack");
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
    public void ReducedMotion_CutsBothWays_OnTheTriggerFrame_AndAfterTheLinger_CuesStillSound()
    {
        var desk = new Desk { Still = true };
        desk.Step(Near(Scanner, 50f));
        Assert.AreEqual(1f, desk.P(Scanner), 1e-6f, "a one-frame cut to the full spot (the travel never scales)");
        Assert.AreEqual(1, desk.FirstFrame(Scanner, SlideCueKind.Unlatch));
        Assert.AreEqual(1, desk.FirstFrame(Scanner, SlideCueKind.Peak), "the arrival's knock and lamp pulse on the cut");
        SlidePose pose = desk.Assist.Pose(Scanner, true);
        Assert.AreEqual(0f, pose.Squash + pose.Sway, 1e-6f, "no squash, no sway");
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
        var assist = new ProximityAssist(slides);
        float most = 0f;
        for (int i = 1; i < 40; i++)
        {
            assist.Step(i * Frame, new[] { Near(Scanner, 50f) }, null, false);
            most = Math.Max(most, assist.Position(Scanner) * scanner.travel);
        }
        Assert.AreEqual(scanner.Reach(slides.feltGap), most, 1e-5f, "it stops 0.4 cm short of its inner lip");
    }

    [Test]
    public void APose_SquashesALift_PulsesALitLampAfterThePeak_AndCarriesTheShadows()
    {
        var desk = new Desk();
        desk.Step(On(Torch));
        desk.Step(On(Torch));
        desk.Step(On(Torch));
        Assert.Greater(desk.Assist.Pose(Torch, false).Squash, 0f, "a lift squashes over its anticipation");
        Assert.AreEqual(0f, desk.Assist.Pose(Torch, false).Shadow, 1e-6f, "its shadow is its own contact until it rises");
        desk.Hold(10, On(Torch));
        int peak = desk.FirstFrame(Torch, SlideCueKind.Peak);
        Assert.Greater(peak, 0);
        Assert.AreEqual(DeskSlides.Approved().lampPulse, desk.Assist.Pose(Torch, true).Lamp, 1e-6f, "a lit lamp pulses just after the peak");
        Assert.AreEqual(1f, desk.Assist.Pose(Torch, false).Lamp, 1e-6f, "an unlit lamp stays unlit");
        desk.Hold(10, On(Torch));
        Assert.AreEqual(1f, desk.Assist.Pose(Torch, true).Lamp, 1e-6f, "100 ms only");
        Assert.Greater(desk.Assist.Pose(Torch, false).Shadow, 0.4f, "the rest-footprint shadow shows under the lifted torch");
        Assert.AreEqual(0f, desk.Assist.Pose(Torch, false).Squash, 1e-6f, "released as it rose");

        var track = new Desk();
        track.Hold(3, Near(Scanner, 50f));
        Assert.AreEqual(0f, track.Assist.Pose(Scanner, false).Shadow, 1e-6f, "in its well during the anticipation");
        track.Hold(10, Near(Scanner, 50f));
        Assert.AreEqual(DeskSlides.Approved().trackShadowAlpha, track.Assist.Pose(Scanner, false).Shadow, 1e-6f, "it travels with the base");
    }

    [Test]
    public void Home_CutsASliderHomeAtOnce_AndAWinnerWaitingOnItStartsNext()
    {
        var desk = new Desk();
        desk.Hold(30, Near(Scanner, 50f));
        desk.Step(On(Chip));
        Assert.AreEqual(SlidePhase.Wait, desk.Assist.Phase(Chip));
        desk.Assist.Home(Scanner);
        Assert.AreEqual(0f, desk.P(Scanner), 1e-6f);
        desk.Step(On(Chip));
        Assert.AreEqual(desk.FrameNo, desk.FirstFrame(Chip, SlideCueKind.Unlatch), "its loser is home: it starts");
        desk.Assist.Home(null);
        Assert.AreEqual(SlidePhase.Rest, desk.Assist.Phase(Chip), "every slider home (a load, a new day)");
    }

    [Test]
    public void ALiftTakenInHand_GivesWayAtOnce_Smoothly()
    {
        var desk = new Desk();
        desk.Hold(30, On(Torch));
        float before = desk.P(Torch);
        desk.Assist.Release(Torch);
        Assert.AreEqual(SlidePhase.Retract, desk.Assist.Phase(Torch), "it gives way as a hand-off loser does");
        desk.Step();
        Assert.AreEqual(before, desk.P(Torch), 0.05f, "no jump on the first frame");
        desk.Hold(10);
        Assert.AreEqual(SlidePhase.Rest, desk.Assist.Phase(Torch));
        desk.Assist.Release(Torch);
        Assert.AreEqual(SlidePhase.Rest, desk.Assist.Phase(Torch), "at home it stays home");
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
    public void FixedObjects_AndUnknownIds_NeverMove()
    {
        var desk = new Desk();
        desk.Hold(20, On(DeskHomeMap.Detain), On("nothing", 1));
        Assert.IsEmpty(desk.Heard);
        Assert.AreEqual(0f, desk.P(DeskHomeMap.Detain), 1e-6f);
        Assert.AreEqual(SlidePhase.Rest, desk.Assist.Pose(DeskHomeMap.Bell, true).Phase);
    }
}
