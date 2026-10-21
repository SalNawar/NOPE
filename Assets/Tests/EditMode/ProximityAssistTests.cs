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
/// home; the forced retract once the shift is over; Reduced Motion cuts; a
/// track never passes its felt gap; nothing pops; a cursor wandering the desk
/// never moves two at once for more than a frame; the frame's facts and the
/// silhouette's rank; a step allocates nothing. Driven at 60 fps.
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
        public readonly HashSet<string> Busy = new HashSet<string>(), Taken = new HashSet<string>(), Gone = new HashSet<string>(), Rides = new HashSet<string>();
        public readonly List<(int frame, SlideCue cue)> Heard = new List<(int, SlideCue)>();
        private readonly string[] _attached;
        public int FrameNo;
        public bool Still, Dragging;

        /// <summary>How fast the cursor moves this frame (px a second at 1080p: the drive-by guard; 0 unknown).</summary>
        public float Speed;

        public Desk(DeskSlides slides = null, IReadOnlyList<DeskHome> homes = null, params string[] attached)
        {
            Slides = slides ?? DeskSlides.Approved();
            Assist = new ProximityAssist(Slides, homes ?? DeskHomeMap.Approved());
            _attached = attached.Length > 0 ? attached : new[] { Scanner, Chip, Torch };
        }

        /// <summary>One frame with the cursor finding <paramref name="found"/>; every attached slider is listed with its state (busy, in hand, gone, its docked paper riding it).</summary>
        public void Step(params SlideCandidate[] found)
        {
            FrameNo++;
            var all = new List<SlideCandidate>();
            foreach (string id in _attached.Union(found.Select(f => f.Id)))
            {
                SlideCandidate c = found.FirstOrDefault(f => f.Id == id);
                bool busy = Busy.Contains(id), taken = Taken.Contains(id), present = !Gone.Contains(id), rides = Rides.Contains(id);
                all.Add(c.Id != null ? new SlideCandidate(id, c.Over, c.OnFootprint, c.Distance, busy, taken, present, rides) : SlideCandidate.Away(id, busy, taken, present, rides));
            }
            Assist.Step(FrameNo * Frame, all, Dragging, Still, Speed);
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
        Assert.AreEqual(SlidePhase.Out, desk.Assist.Pose(Scanner).Phase);
    }

    [Test]
    public void Back_AfterTheLinger_ArrivesAtSpeed_WithTheSeatClackOnTheArrivalFrame()
    {
        var desk = new Desk();
        desk.Hold(40, Near(Scanner, 100f));
        desk.Step();
        Assert.AreEqual(SlidePhase.Linger, desk.Assist.Pose(Scanner).Phase, "the cursor left: it waits");
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
        Assert.AreEqual(SlidePhase.Rest, assist.Assist.Pose(Scanner).Phase);
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
        Assert.AreEqual(SlidePhase.Rest, desk.Assist.Pose(Scanner).Phase);
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
            for (int i = 0; i < 3 && desk.Assist.Pose(Scanner).Phase != SlidePhase.Back; i++)
                desk.Step();
            Assert.AreEqual(SlidePhase.Back, desk.Assist.Pose(Scanner).Phase, "the linger is over: it goes home");
            desk.Hold(backFrames);
            if (desk.Assist.Pose(Scanner).Phase != SlidePhase.Back)
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
            if ((backFrames + 1) * Frame < slides.backSeconds - Frame / 2f)
                Assert.IsFalse(desk.Heard.Any(h => h.cue.Id == Scanner && h.cue.Kind == SlideCueKind.Seat && h.frame >= summoned),
                               at + ": summoned during its ease-in, the scanner was hurried: a loser's way home, no clack (slides.json's hand-off)");
            else
                Assert.AreEqual(1, desk.Heard.Count(h => h.cue.Id == Scanner && h.cue.Kind == SlideCueKind.Seat), at + ": it arrived on its own way home: its one clack");
            Assert.AreEqual(1f, desk.P(Chip), 0.03f, at + ": the winner out");
        }
    }

    [Test]
    public void HandOff_FromALoserCaughtBehindHomeInItsAnticipation_TheWinnerWaitsUntilItIsNearlySeated_OneMachineMovingAtATime()
    {
        // The review's harness: the chip reader summoned 2 frames earlier (backing away, about -3 px), the cursor onto the scanner. The
        // signed test counted it seated: the scanner sprang out while the chip reader retracted, both moving more than 0.25 px for 6 frames.
        DeskSlides slides = DeskSlides.Approved();
        int plain = BothMoving(HandOff().scanner, HandOff().chip);
        for (int caught = 1; caught <= 2; caught++)
        {
            var desk = new Desk();
            desk.Hold(caught, Near(Chip, 30f));
            desk.Step(Near(Chip, 30f));
            Assert.Less(desk.P(Chip), 0f, "caught behind home in its anticipation");
            var chip = new List<float> { desk.Px(Chip) };
            var scanner = new List<float> { desk.Px(Scanner) };
            int summoned = desk.FrameNo + 1;
            for (int i = 0; i < 40; i++)
            {
                desk.Step(On(Scanner));
                chip.Add(desk.Px(Chip));
                scanner.Add(desk.Px(Scanner));
            }
            int start = desk.FirstFrame(Scanner, SlideCueKind.Unlatch) - summoned;
            Assert.That(start, Is.InRange(0, (int)Math.Ceiling(slides.winnerAfter / Frame - 1e-4f)), "the winner waits, never longer than winnerAfter");
            float travel = slides.Find(Chip).travelPx;
            Assert.Less(Math.Abs(chip[start + 1]) / travel, slides.winnerBelow, $"caught {caught} frames in: the loser is nearly seated (by its distance from home) when the winner starts");
            Assert.AreEqual(0f, chip[start + 2], 1e-4f, "and home on the winner's first move");
            if (caught == 2)
                Assert.GreaterOrEqual(Math.Abs(chip[1]) / travel, slides.winnerBelow, "the review's case: further behind home than winnerBelow, so it is not seated at the hand-off");
            for (int i = 2; i < chip.Count; i++)
                Assert.LessOrEqual(Math.Abs(chip[i] - chip[i - 1]), slides.winnerBelow * travel + 1e-3f, $"frame {i}: no step home larger than its last stretch (under winnerBelow of its travel): nothing pops");
            Assert.LessOrEqual(BothMoving(scanner, chip), Math.Max(1, plain), $"caught {caught} frames in: no more frames with both moving than a plain hand-off");
            Assert.AreEqual(1f, desk.P(Scanner), 0.03f, "the winner out");
        }
    }

    [Test]
    public void ALingeringMachineThatTurnsBusy_GoesBackOut_AndNeverGoesHomeWhileBusy()
    {
        // The review's surviving fault: a lingering machine that turns busy stayed in Linger and went home under its paper.
        var desk = new Desk();
        desk.Hold(30, Near(Scanner, 30f));
        desk.Step();
        Assert.AreEqual(SlidePhase.Linger, desk.Assist.Pose(Scanner).Phase, "the cursor left: it lingers");
        desk.Busy.Add(Scanner);
        desk.Step();
        Assert.AreEqual(SlidePhase.Out, desk.Assist.Pose(Scanner).Phase, "a paper came onto it: out again");
        desk.Hold(60);
        Assert.AreEqual(SlidePhase.Out, desk.Assist.Pose(Scanner).Phase, "it never goes home while busy");
        Assert.AreEqual(-1, desk.FirstFrame(Scanner, SlideCueKind.Back), "no way home started");
        Assert.AreEqual(1f, desk.P(Scanner), 0.03f);
        desk.Busy.Remove(Scanner);
        desk.Hold(40);
        Assert.AreEqual(SlidePhase.Rest, desk.Assist.Pose(Scanner).Phase, "empty: home after the linger");
    }

    [Test]
    public void ACursorWanderingTheDesk_NeverMovesTwoSlidersAtOnceForMoreThanAFrame_NorHasTwoHalfOut()
    {
        // The review's cursor-only fuzz (3 x 20,000 frames, no busy or drag) found both-moving runs of 2 to 6 frames; a plain hand-off
        // has 1. The chip reader's silhouette overlaps the scanner's home footprint, as on the desk; the cursor wanders, darts and leaves.
        string[] ids = { Chip, Scanner, Torch };
        float[] centre = { 300f, 430f, 700f }, half = { 45f, 60f, 30f };
        DeskSlides slides = DeskSlides.Approved();
        int worst = 0, episodes = 0;
        for (int run = 0; run < 3; run++)
        {
            var desk = new Desk(null, null, ids);
            uint seed = 2463534242u + (uint)run * 7919u;
            float Next()
            {
                seed ^= seed << 13;
                seed ^= seed >> 17;
                seed ^= seed << 5;
                return (seed & 0xFFFFFF) / (float)0x1000000;
            }
            float x = 500f, v = 0f;
            var px = new float[ids.Length];
            int both = 0;
            for (int f = 0; f < 20000; f++)
            {
                float r = Next();
                v = r < 0.02f ? (Next() - 0.5f) * 120f : r < 0.04f ? 0f : v + (Next() - 0.5f) * 4f;
                x = Math.Max(0f, Math.Min(1000f, x + v));
                if (Next() < 0.002f)
                    x = Next() * 1000f;
                var found = new List<SlideCandidate>();
                int rank = 0;
                for (int i = 0; i < ids.Length; i++)
                {
                    float d = Math.Max(0f, Math.Abs(x - centre[i]) - half[i]);
                    bool onScannerHome = ids[i] == Scanner && Math.Abs(x - 400f) < 70f;
                    found.Add(new SlideCandidate(ids[i], d <= 0f ? rank++ : -1, onScannerHome, d));
                }
                desk.Step(found.ToArray());
                int moving = 0, halfOut = 0;
                for (int i = 0; i < ids.Length; i++)
                {
                    float now = desk.Px(ids[i]);
                    if (Math.Abs(now - px[i]) > 0.25f)
                        moving++;
                    if (desk.P(ids[i]) > 0.5f)
                        halfOut++;
                    px[i] = now;
                }
                Assert.LessOrEqual(halfOut, 1, $"run {run}, frame {f}: two sliders half out");
                if (moving >= 2)
                {
                    if (both == 0)
                        episodes++;
                    both++;
                    worst = Math.Max(worst, both);
                }
                else
                {
                    both = 0;
                }
            }
        }
        Assert.Greater(episodes, 20, "the walk hands off often");
        Assert.LessOrEqual(worst, 1, $"never two sliders moving more than 0.25 px for more than 1 frame ({episodes} episodes)");
    }

    [Test]
    public void TheFramesFacts_AreTheDesksRule_AndTheRankCountsEachSliderOnceFromTheFront()
    {
        SlideCandidate gone = SlideCandidate.Found(Chip, false, true, true, true, true, 0, true, 0f);
        Assert.IsFalse(gone.Present);
        Assert.IsFalse(gone.Busy || gone.Taken || gone.Rides, "gone from the desk: nothing else counts");
        SlideCandidate away = SlideCandidate.Found(Chip, true, true, false, true, false, 0, true, 0f);
        Assert.AreEqual(-1, away.Over, "the PC open: the cursor is nowhere near");
        Assert.IsFalse(away.OnFootprint);
        Assert.AreEqual(float.PositiveInfinity, away.Distance);
        Assert.IsTrue(away.Busy && away.Rides, "its state still counts");
        SlideCandidate held = SlideCandidate.Found(Torch, true, false, true, false, true, 0, true, 0f);
        Assert.IsTrue(held.Taken);
        Assert.AreEqual(-1, held.Over, "in hand: never under the cursor");
        SlideCandidate near = SlideCandidate.Found(Scanner, true, false, false, false, true, 1, true, 42f);
        Assert.AreEqual(1, near.Over);
        Assert.IsTrue(near.OnFootprint);
        Assert.AreEqual(42f, near.Distance);

        string[] hits = { null, Chip, Chip, null, Scanner, Chip, Torch };
        Assert.AreEqual(0, ProximityAssist.Rank(hits, hits.Length, Chip), "the front-most slider, whatever box of no slider lies before it");
        Assert.AreEqual(1, ProximityAssist.Rank(hits, hits.Length, Scanner), "the chip reader's two boxes count once");
        Assert.AreEqual(2, ProximityAssist.Rank(hits, hits.Length, Torch), "and its third, behind the scanner, still once");
        Assert.AreEqual(-1, ProximityAssist.Rank(hits, 4, Scanner), "only the hits there are");
        Assert.AreEqual(-1, ProximityAssist.Rank(hits, hits.Length, Glasses));

        DeskSlides slides = DeskSlides.Approved();
        Assert.AreEqual(120f, slides.ToReference(240f, 2160f), 1e-4f, "240 px at 4K is the trigger's 120 px at 1080p");
        Assert.AreEqual(160f, slides.ToReference(80f, 540f), 1e-4f);
        Assert.AreEqual(slides.referenceHeight * 5f, slides.ToReference(5f, 0f), 1e-4f, "no screen: one px high, never a division by nothing");
    }

    [Test]
    public void TheIntercomGoingHome_GivesWayToTheGlasses_OneMachineMovingAtATime()
    {
        DeskSlides slides = DeskSlides.Approved();
        var desk = new Desk(null, IntercomSlides(), Intercom, Glasses);
        desk.Hold(30, Near(Intercom, 40f));
        desk.Hold(10);
        desk.Hold(18);
        Assert.AreEqual(SlidePhase.Back, desk.Assist.Pose(Intercom).Phase, "on its way home: the end of its ease-in or its seat bounce (the review: it rose to 0.197 of its travel under the glasses)");
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
        Assert.AreEqual(SlidePhase.Back, desk.Assist.Pose(Scanner).Phase, "then home after the linger");
    }

    [Test]
    public void ReEnteringDuringTheLinger_KeepsItOut_WithNoNewUnlatch()
    {
        var desk = new Desk();
        desk.Hold(30, Near(Scanner, 50f));
        desk.Hold(5);
        desk.Hold(5, Near(Scanner, 50f));
        Assert.AreEqual(SlidePhase.Out, desk.Assist.Pose(Scanner).Phase);
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
        Assert.AreEqual(SlidePhase.Out, desk.Assist.Pose(Chip).Phase, "a passport in it: it stays out after the cursor left");
        desk.Hold(30, On(Scanner));
        Assert.AreEqual(SlidePhase.Out, desk.Assist.Pose(Chip).Phase, "it never retracts for another");
        Assert.AreEqual(SlidePhase.Out, desk.Assist.Pose(Scanner).Phase, "the other slides at once: only empty machines take turns");
        desk.Busy.Remove(Chip);
        desk.Hold(60, On(Scanner));
        Assert.AreEqual(SlidePhase.Rest, desk.Assist.Pose(Chip).Phase, "empty again and the cursor gone: home");

        var home = new Desk();
        home.Busy.Add(Scanner);
        home.Hold(20, On(Scanner));
        Assert.AreEqual(SlidePhase.Rest, home.Assist.Pose(Scanner).Phase, "a busy machine at home is not summoned");
    }

    [Test]
    public void ACursorOnABusyMachine_KeepsEveryOtherOneHome_OnlyTheNearestMayMove()
    {
        // The review's harness: the scanner busy, the cursor on its silhouette and 60 px from the chip reader's.
        var atHome = new Desk();
        atHome.Busy.Add(Scanner);
        atHome.Hold(30, On(Scanner), Near(Chip, 60f));
        Assert.AreEqual(SlidePhase.Rest, atHome.Assist.Pose(Chip).Phase, "the chip reader stays home");
        Assert.AreEqual(SlidePhase.Rest, atHome.Assist.Pose(Scanner).Phase, "and the busy scanner is not summoned");

        var scanning = new Desk();
        scanning.Hold(30, Near(Scanner, 20f));
        scanning.Busy.Add(Scanner);
        scanning.Hold(60, On(Scanner), Near(Chip, 60f));
        Assert.AreEqual(SlidePhase.Rest, scanning.Assist.Pose(Chip).Phase, "a scan watched with the cursor on the scanner never brings the chip reader out");
        Assert.AreEqual(SlidePhase.Out, scanning.Assist.Pose(Scanner).Phase);
        Assert.AreEqual(-1, scanning.FirstFrame(Chip, SlideCueKind.Unlatch));

        var handOff = new Desk();
        handOff.Hold(30, Near(Chip, 40f));
        handOff.Busy.Add(Scanner);
        handOff.Hold(20, On(Scanner), Near(Chip, 60f));
        Assert.AreEqual(SlidePhase.Rest, handOff.Assist.Pose(Chip).Phase, "the chip reader out gives way to the cursor on the busy scanner");
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
        Assert.AreEqual(SlidePhase.Back, desk.Assist.Pose(Scanner).Phase);
        float shown = desk.P(Scanner);
        desk.Busy.Add(Scanner);
        desk.Step();
        Assert.AreEqual(SlidePhase.Held, desk.Assist.Pose(Scanner).Phase, "a paper on its way onto the bed: it stops");
        Assert.AreEqual(shown, desk.P(Scanner), 1e-6f, "where it was shown last frame (where the bed point was taken)");
        desk.Hold(90);
        Assert.AreEqual(shown, desk.P(Scanner), 1e-6f, "it stays while busy");
        desk.Busy.Remove(Scanner);
        desk.Step();
        Assert.AreEqual(SlidePhase.Back, desk.Assist.Pose(Scanner).Phase, "empty: home from there");
        Assert.AreEqual(shown, desk.P(Scanner), 0.02f, "no jump");
        desk.Hold(30);
        Assert.AreEqual(SlidePhase.Rest, desk.Assist.Pose(Scanner).Phase);
        Assert.Greater(desk.Heard.Count(h => h.cue.Id == Scanner && h.cue.Kind == SlideCueKind.Seat), 0, "it clacks home");

        var retracting = new Desk();
        retracting.Hold(30, Near(Scanner, 50f));
        retracting.Hold(3, On(Chip));
        Assert.AreEqual(SlidePhase.Retract, retracting.Assist.Pose(Scanner).Phase);
        retracting.Busy.Add(Scanner);
        retracting.Step(On(Chip));
        Assert.AreEqual(SlidePhase.Held, retracting.Assist.Pose(Scanner).Phase, "a hand-off loser a paper lands on stops too");
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
        Assert.AreEqual(SlidePhase.Out, desk.Assist.Pose(Scanner).Phase, "drop and go: it stays out while the paper lands");
        desk.Hold(20);
        Assert.AreNotEqual(SlidePhase.Out, desk.Assist.Pose(Scanner).Phase, "then it lingers and goes home");

        var missed = new Desk { Dragging = true };
        missed.Hold(30, Near(Scanner, 50f));
        missed.Dragging = false;
        missed.Step();
        missed.Hold(12);
        Assert.AreEqual(SlidePhase.Back, missed.Assist.Pose(Scanner).Phase, "a paper let go beside it (not on it) holds nothing");

    }

    [Test]
    public void ALiftTakenInHand_GivesWayAtOnce_Smoothly_AndIsNotSummonedWhileHeld()
    {
        var desk = new Desk();
        desk.Hold(30, On(Torch));
        float before = desk.P(Torch);
        desk.Taken.Add(Torch);
        desk.Step(On(Torch));
        Assert.AreEqual(SlidePhase.Retract, desk.Assist.Pose(Torch).Phase, "it gives way as a hand-off loser does");
        Assert.AreEqual(before, desk.P(Torch), 0.05f, "no jump on the first frame");
        desk.Hold(10, On(Torch));
        Assert.AreEqual(SlidePhase.Rest, desk.Assist.Pose(Torch).Phase, "home, and not summoned while in hand");
        Assert.AreEqual(1, desk.Heard.Count(h => h.cue.Id == Torch && h.cue.Kind == SlideCueKind.Unlatch));
        desk.Taken.Remove(Torch);
        desk.Hold(3, On(Torch));
        Assert.AreEqual(SlidePhase.Out, desk.Assist.Pose(Torch).Phase, "put back: it slides again");
    }

    [Test]
    public void ASliderGoneFromTheDesk_IsHomeAtOnce()
    {
        var desk = new Desk();
        desk.Hold(30, Near(Scanner, 50f));
        desk.Gone.Add(Scanner);
        desk.Step(Near(Scanner, 50f));
        Assert.AreEqual(SlidePhase.Rest, desk.Assist.Pose(Scanner).Phase);
        Assert.AreEqual(0f, desk.Assist.Pose(Scanner).Shadow, 1e-6f, "nothing left behind");
    }

    [Test]
    public void AForcedRetract_TakesEverySliderHomeOnTheNextStep_WithItsSound_AndNothingSlidesAgain_WithTheCursorStillOnIt()
    {
        // The review: Retire then a Step in the same frame summoned it again with the cursor still on it (a 0.65 px dip, an unlatch,
        // the shush with nothing sliding), and Step cleared the hand-off cue Retire raised, so the retract was silent.
        var desk = new Desk();
        desk.Hold(30, On(Chip));
        desk.Assist.Close();
        Assert.AreEqual(SlidePhase.Out, desk.Assist.Pose(Chip).Phase, "nothing moves before the next step");
        desk.Step(On(Chip));
        int closed = desk.FrameNo;
        Assert.AreEqual(SlidePhase.Retract, desk.Assist.Pose(Chip).Phase, "the shift is over: it retracts at once, the cursor still on it");
        Assert.AreEqual(closed, desk.FirstFrame(Chip, SlideCueKind.HandOff), "with the hand-off's reversed shush, on that step");
        float was = desk.P(Chip);
        for (int i = 0; i < 90; i++)
        {
            desk.Step(On(Chip), Near(Scanner, 10f), On(Torch, 1));
            Assert.LessOrEqual(desk.P(Chip), was + 1e-6f, $"frame {i}: it never comes back out");
            was = desk.P(Chip);
        }
        Assert.AreEqual(SlidePhase.Rest, desk.Assist.Pose(Chip).Phase);
        Assert.AreEqual(0f, desk.P(Chip), 1e-6f);
        Assert.IsFalse(desk.Heard.Any(h => h.frame >= closed && (h.cue.Kind == SlideCueKind.Unlatch || h.cue.Kind == SlideCueKind.Slide)), "no unlatch, no shush: nothing slides once the shift is over");
        Assert.IsFalse(desk.Heard.Any(h => h.cue.Id == Chip && h.cue.Kind == SlideCueKind.Seat), "a loser's way home: no seat clack");
        Assert.AreEqual(SlidePhase.Rest, desk.Assist.Pose(Scanner).Phase, "a slider at home stays home");
        Assert.AreEqual(SlidePhase.Rest, desk.Assist.Pose(Torch).Phase);
    }

    [Test]
    public void AForcedRetract_TakesABusyOneOnlyWithItsPaperRiding_ElseAsSoonAsItIsEmpty_AndOneHeldForADropOnceItLanded()
    {
        var riding = new Desk();
        riding.Hold(30, Near(Chip, 30f));
        riding.Busy.Add(Chip);
        riding.Step();
        riding.Assist.Close();
        riding.Hold(20);
        Assert.AreEqual(SlidePhase.Out, riding.Assist.Pose(Chip).Phase, "a passport docked that cannot ride: it stays until empty");
        riding.Rides.Add(Chip);
        riding.Step();
        Assert.AreEqual(SlidePhase.Retract, riding.Assist.Pose(Chip).Phase, "its docked passport rides it home: one mover");
        riding.Hold(10);
        Assert.AreEqual(SlidePhase.Rest, riding.Assist.Pose(Chip).Phase, "busy all the way, it is never stopped on its way");

        var emptied = new Desk();
        emptied.Hold(30, Near(Chip, 30f));
        emptied.Busy.Add(Chip);
        emptied.Assist.Close();
        emptied.Hold(20, Near(Chip, 30f));
        Assert.AreEqual(SlidePhase.Out, emptied.Assist.Pose(Chip).Phase);
        emptied.Busy.Remove(Chip);
        emptied.Step(Near(Chip, 30f));
        Assert.AreEqual(SlidePhase.Retract, emptied.Assist.Pose(Chip).Phase, "empty: it goes at once, no linger, the cursor still near");

        // The review: a forced retract during the 1 s drop hold did nothing and was never tried again.
        var dropped = new Desk { Dragging = true };
        dropped.Hold(30, On(Scanner));
        dropped.Dragging = false;
        dropped.Step(On(Scanner));
        dropped.Assist.Close();
        dropped.Hold(20, On(Scanner));
        Assert.AreEqual(SlidePhase.Out, dropped.Assist.Pose(Scanner).Phase, "the paper still lands: it stays");
        dropped.Hold((int)Math.Ceiling(dropped.Slides.dropHoldSeconds / Frame), On(Scanner));
        Assert.AreNotEqual(SlidePhase.Out, dropped.Assist.Pose(Scanner).Phase, "the drop hold over: it goes home");
        dropped.Hold(10, On(Scanner));
        Assert.AreEqual(SlidePhase.Rest, dropped.Assist.Pose(Scanner).Phase);

        var still = new Desk { Still = true };
        still.Hold(5, Near(Scanner, 20f));
        still.Assist.Close();
        still.Step(Near(Scanner, 20f));
        Assert.AreEqual(0f, still.P(Scanner), 1e-6f, "under Reduced Motion: a cut home");
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
        desk.Step(On(Chip));
        Assert.AreEqual(0f, desk.P(Scanner), 1e-6f, "the hand-off loser cuts home");
        Assert.AreEqual(1f, desk.P(Chip), 1e-6f, "and the winner cuts out on the same frame");
        desk.Step();
        Assert.AreEqual(1f, desk.P(Chip), 1e-6f, "it lingers");
        desk.Hold(12);
        Assert.AreEqual(0f, desk.P(Chip), 1e-6f, "home after the linger, at once");
        Assert.AreEqual(SlidePhase.Rest, desk.Assist.Pose(Chip).Phase);
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
        var pressedFrames = new List<int>();
        for (int i = 0; i < 20; i++)
        {
            track.Step(Near(Scanner, 50f));
            SlidePose pose = track.Assist.Pose(Scanner);
            if (Math.Abs(pose.ShadowBlur - slides.trackShadowBlurPx * slides.peakTightBlur) < 1e-5f)
            {
                pressedFrames.Add(track.FrameNo);
                Assert.AreEqual(slides.trackShadowAlpha * slides.peakTightDarken, pose.Shadow, 1e-5f, "pressed: the blur tightens 30 % and it is 10 % darker");
            }
        }
        Assert.AreEqual(2, pressedFrames.Count, "slides.json: the press lasts 2 frames (at 60 fps)");
        Assert.AreEqual(track.FirstFrame(Scanner, SlideCueKind.Peak), pressedFrames[0], "from the peak's frame, with the knock");
        Assert.AreEqual(pressedFrames[0] + 1, pressedFrames[1]);
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
        Assert.AreEqual(SlidePhase.Rest, torch.Assist.Pose(Torch).Phase);
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
            while (brush && desk.Assist.Pose(Intercom).Phase != SlidePhase.Back)
            {
                desk.Step();
                twin.Step(Near(Intercom, 40f));
            }
            if (!brush)
            {
                desk.Step(On(Glasses));
                twin.Step(Near(Intercom, 40f));
                Assert.AreEqual(SlidePhase.Retract, desk.Assist.Pose(Intercom).Phase);
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
        Assert.AreEqual(SlidePhase.Rest, desk.Assist.Pose(Chip).Phase);
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

    // ---------------- Round 5: the code review of round 3 (max) and the review of the Unity capture ----------------

    /// <summary>Steps <paramref name="desk"/> with the cursor away until <paramref name="id"/>'s way home has started (the frame it begins).</summary>
    private static void AwayUntilItsWayHome(Desk desk, string id)
    {
        for (int i = 0; i < 90 && desk.Assist.Pose(id).Phase != SlidePhase.Back; i++)
            desk.Step();
        Assert.AreEqual(SlidePhase.Back, desk.Assist.Pose(id).Phase, id + ": the linger is over, its way home has started");
    }

    [Test]
    public void ASliderSummonedAgainBeyondItsSlidSpot_NeverGoesFurtherOut_NorPastThePeakTheGatePassed()
    {
        // The code review (round 3, 1): the cursor near the chip reader, gone for the linger, back on the frame its way home starts
        // (position about 1.16), or a paper landing on it there and then the cursor: the new slide out turned its anticipation
        // toward the mat (to 1.1875 against the gated 1.1665: 0.42 px nearer lamp_chip, 0.42 px into the scanner's felt).
        DeskSlides slides = DeskSlides.Approved();
        DeskSlider chip = slides.Find(Chip);
        foreach (bool held in new[] { false, true })
        {
            string how = held ? "held near its peak by a landing paper, then summoned" : "summoned on the frame its way home starts";
            var desk = new Desk();
            desk.Hold(3, Near(Chip, 60f));
            AwayUntilItsWayHome(desk, Chip);
            if (held)
            {
                desk.Busy.Add(Chip);
                desk.Hold(5);
                Assert.AreEqual(SlidePhase.Held, desk.Assist.Pose(Chip).Phase, how + ": it stopped where the paper found it");
                desk.Busy.Remove(Chip);
            }
            float stood = desk.P(Chip);
            Assert.Greater(stood, 1f, how + ": beyond its slid spot");
            float highest = float.NegativeInfinity;
            for (int i = 0; i < 40; i++)
            {
                desk.Step(Near(Chip, 60f));
                highest = Math.Max(highest, desk.P(Chip));
            }
            Assert.LessOrEqual(highest, stood + 1e-5f, how + ": never further out than it stood (no anticipation toward the mat)");
            Assert.LessOrEqual(highest, chip.Peak + 1e-5f, how + ": never past the peak the gate passed (lamp_chip 3 px clear)");
            Assert.AreEqual(1f, desk.P(Chip), 0.03f, how + ": settled at its slid spot");
            Assert.AreEqual(SlidePhase.Out, desk.Assist.Pose(Chip).Phase, how);
        }
    }

    [Test]
    public void AHandOff_TakesTheEmptySliderThatIsOut_NeverABusyOne_SoTheOneOutAlwaysGivesWay()
    {
        // The code review (round 3, 2): with the busy chip reader out, a pick that took the first slider out (busy or not) left the
        // UV torch raised while the scanner sprang out under the cursor: 19 frames with both more than half out. The chip reader
        // comes before the torch in the data, so only the preference for the empty one keeps the torch the hand-off's loser.
        var desk = new Desk();
        desk.Hold(30, On(Chip));
        desk.Busy.Add(Chip);
        desk.Hold(30, On(Torch));
        Assert.AreEqual(SlidePhase.Out, desk.Assist.Pose(Chip).Phase, "the busy chip reader stays out");
        Assert.AreEqual(1f, desk.P(Torch), 0.03f, "the torch raised beside it");
        int handOff = desk.FrameNo + 1, both = 0;
        for (int i = 0; i < 40; i++)
        {
            desk.Step(On(Scanner));
            if (desk.P(Torch) > 0.5f && desk.P(Scanner) > 0.5f)
                both++;
        }
        Assert.AreEqual(0, both, "never the torch and the scanner both half out");
        Assert.AreEqual(handOff, desk.FirstFrame(Torch, SlideCueKind.HandOff), "the torch, the empty one out, gives way at once");
        Assert.AreEqual(1f, desk.P(Scanner), 0.03f, "the scanner out");
        Assert.AreEqual(SlidePhase.Out, desk.Assist.Pose(Chip).Phase, "the busy chip reader still out");
    }

    [Test]
    public void AWaitingWinnerThatTurnsBusy_StaysWhereItStood_AndComesOutOnlyOnceEmpty()
    {
        // The code review (round 3, 2a): without the busy wait-to-held rule, a winner waiting on its loser that turned busy slid out
        // anyway once the loser was seated.
        var desk = new Desk();
        desk.Hold(30, On(Scanner));
        desk.Step(On(Chip));
        Assert.AreEqual(SlidePhase.Wait, desk.Assist.Pose(Chip).Phase, "the winner waits for the scanner to be nearly seated");
        desk.Busy.Add(Chip);
        for (int i = 0; i < 30; i++)
        {
            desk.Step(On(Chip));
            Assert.AreEqual(0f, desk.P(Chip), 1e-6f, $"busy frame {i}: it stays where it stood, at home");
        }
        Assert.AreEqual(-1, desk.FirstFrame(Chip, SlideCueKind.Unlatch), "never summoned while busy");
        int empty = desk.FrameNo + 1;
        desk.Busy.Remove(Chip);
        desk.Hold(30, On(Chip));
        Assert.AreEqual(empty, desk.FirstFrame(Chip, SlideCueKind.Unlatch), "empty again with the cursor on it: out at once");
        Assert.AreEqual(1f, desk.P(Chip), 0.03f);
    }

    [Test]
    public void AHeldSliderThatEmptiesAfterTheShiftEnds_GoesHomeAsTheForcedRetract_WithNoClack()
    {
        // The code review (round 3, 2b): without Held in the forced retract, it went home on a clacking way home.
        var desk = new Desk();
        desk.Hold(30, Near(Scanner, 50f));
        AwayUntilItsWayHome(desk, Scanner);
        desk.Hold(3);
        desk.Busy.Add(Scanner);
        desk.Step();
        Assert.AreEqual(SlidePhase.Held, desk.Assist.Pose(Scanner).Phase, "a paper found it on its way home");
        Assert.Greater(desk.P(Scanner), 0.1f);
        desk.Assist.Close();
        desk.Hold(10);
        Assert.AreEqual(SlidePhase.Held, desk.Assist.Pose(Scanner).Phase, "busy with no paper riding it: it stays");
        int empty = desk.FrameNo + 1;
        desk.Busy.Remove(Scanner);
        desk.Hold(20);
        Assert.AreEqual(empty, desk.Heard.Where(h => h.cue.Id == Scanner && h.cue.Kind == SlideCueKind.HandOff && h.frame >= empty).Select(h => h.frame).DefaultIfEmpty(-1).First(),
                        "emptied: the forced retract at once, with the hand-off's sound");
        Assert.IsFalse(desk.Heard.Any(h => h.cue.Id == Scanner && h.frame >= empty && (h.cue.Kind == SlideCueKind.Back || h.cue.Kind == SlideCueKind.Seat)), "no way home of its own, no clack");
        Assert.AreEqual(SlidePhase.Rest, desk.Assist.Pose(Scanner).Phase);
    }

    [Test]
    public void AWayHomeHurriedByAWinner_EasesIntoHome_NeverSlammingIntoItsStop()
    {
        // The code review (round 3, 3): the cursor reaching the chip reader just as the scanner's way home starts; the hurried way home
        // moved 4.74 px in its last frame (its own way home 2.83 px) and stopped dead, silently. Now it eases in, as a hand-off loser.
        DeskSlides slides = DeskSlides.Approved();
        float travel = slides.Find(Scanner).travelPx;
        var desk = new Desk();
        desk.Hold(40, Near(Scanner, 50f));
        AwayUntilItsWayHome(desk, Scanner);
        int summoned = desk.FrameNo + 1;
        float before = desk.Px(Scanner);
        var steps = new List<float>();
        for (int i = 0; i < 30 && before > 0f; i++)
        {
            desk.Step(On(Chip));
            float now = desk.Px(Scanner);
            steps.Add(before - now);
            before = now;
        }
        Assert.AreEqual(0f, before, 1e-4f, "home");
        Assert.LessOrEqual(steps.Max(), 4.1f + 0.05f, "no frame faster than the hand-off retract's fastest (slides.json: 4.1 px)");
        Assert.LessOrEqual(steps[steps.Count - 1], slides.winnerBelow * travel + 1e-3f, "its frame into home is at most its last stretch");
        Assert.Less(steps[steps.Count - 1], steps.Max(), "it arrives slowly: its last frame is not its fastest");
        for (int i = 0; i < steps.Count; i++)
            Assert.GreaterOrEqual(steps[i], -1e-4f, $"frame {i}: only ever toward home");
        Assert.IsFalse(desk.Heard.Any(h => h.cue.Id == Scanner && h.cue.Kind == SlideCueKind.Seat && h.frame >= summoned), "a loser's way home: no clack");
        desk.Hold(30, On(Chip));
        Assert.AreEqual(1f, desk.P(Chip), 0.03f, "the winner out");
    }

    [Test]
    public void ACursorLeavingDuringTheAnticipation_SendsItStraightHome_NoLingerNoOvershootNoKnock()
    {
        // The review of the Unity capture (S2, 10): a cursor brushing past a machine let it run its whole spring.
        var desk = new Desk();
        desk.Hold(2, Near(Scanner, 40f));
        Assert.Less(desk.P(Scanner), 0f, "it began to back away");
        var path = new List<float>();
        for (int i = 0; i < 20; i++)
        {
            desk.Step();
            path.Add(desk.P(Scanner));
        }
        Assert.AreEqual(-1, desk.FirstFrame(Scanner, SlideCueKind.Slide), "its base never left its well: no shush");
        Assert.AreEqual(-1, desk.FirstFrame(Scanner, SlideCueKind.Peak), "no overshoot, no knock");
        Assert.AreEqual(-1, desk.FirstFrame(Scanner, SlideCueKind.Back), "no linger, no way home of its own");
        Assert.LessOrEqual(path.Max(), 1e-6f, "never toward the mat");
        for (int i = 1; i < path.Count; i++)
            Assert.GreaterOrEqual(path[i], path[i - 1] - 1e-6f, $"frame {i}: straight home");
        Assert.AreEqual(SlidePhase.Rest, desk.Assist.Pose(Scanner).Phase);

        var lift = new Desk();
        lift.Hold(1, On(Torch));
        lift.Hold(10);
        Assert.AreEqual(SlidePhase.Rest, lift.Assist.Pose(Torch).Phase, "a lift brushed past is home at once");
        Assert.AreEqual(0f, lift.P(Torch), 1e-6f);
    }

    [Test]
    public void ADriveBy_ACursorSweepingPast_SummonsNothingByNearness_ButOnASilhouetteItWinsAtOnce()
    {
        // The review of the Unity capture (S2, 10): one sweep from the chip reader to the intercom slid the scanner, then BABEL.
        var sweep = new Desk { Speed = 2000f };
        sweep.Hold(8, Near(Scanner, 40f));
        Assert.AreEqual(-1, sweep.FirstFrame(Scanner, SlideCueKind.Unlatch), "a sweep past it, faster than sweepPxPerSecond, summons nothing");
        Assert.AreEqual(0f, sweep.P(Scanner), 1e-6f);
        sweep.Step(On(Scanner));
        Assert.AreEqual(sweep.FrameNo, sweep.FirstFrame(Scanner, SlideCueKind.Unlatch), "on its silhouette it wins at once, however fast");

        var slow = new Desk { Speed = 300f };
        slow.Step(Near(Scanner, 40f));
        Assert.AreEqual(1, slow.FirstFrame(Scanner, SlideCueKind.Unlatch), "from a quiet desk a slow cursor near it starts it on the trigger frame (DESK-HOMES 6)");
    }

    [Test]
    public void AHandOffByNearness_WaitsForTheCursorToDwell_TheOneOutLingersMeanwhile_ASweepNeverTakesIt()
    {
        DeskSlides slides = DeskSlides.Approved();
        int dwell = (int)Math.Ceiling(slides.dwellSeconds / Frame - 1e-4f);
        var desk = new Desk();
        desk.Hold(30, On(Scanner));
        int near = desk.FrameNo + 1;
        desk.Hold(dwell - 1, Near(Chip, 30f));
        Assert.AreEqual(-1, desk.FirstFrame(Scanner, SlideCueKind.HandOff), "the cursor only passing near the chip reader: no hand-off yet");
        Assert.AreEqual(SlidePhase.Linger, desk.Assist.Pose(Scanner).Phase, "the scanner lingers meanwhile");
        desk.Hold(30, Near(Chip, 30f));
        Assert.AreEqual(near + dwell, desk.FirstFrame(Scanner, SlideCueKind.HandOff), "the hand-off once the cursor dwelt near it");
        Assert.AreEqual(1f, desk.P(Chip), 0.03f, "the chip reader out");

        var sweep = new Desk();
        sweep.Hold(30, On(Scanner));
        sweep.Speed = 2000f;
        sweep.Hold(40, Near(Chip, 30f));
        Assert.AreEqual(-1, sweep.FirstFrame(Chip, SlideCueKind.Unlatch), "a sweep near the chip reader never takes it");
        Assert.AreEqual(SlidePhase.Rest, sweep.Assist.Pose(Scanner).Phase, "the scanner went home after its linger, on its own way");
        sweep.Speed = 0f;
        int slowed = sweep.FrameNo + 1;
        sweep.Hold(30, Near(Chip, 30f));
        Assert.AreEqual(slowed, sweep.FirstFrame(Chip, SlideCueKind.Unlatch), "the cursor slowing near it on a quiet desk: out at once");
    }
}
