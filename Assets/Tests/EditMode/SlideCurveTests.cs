using System;
using NUnit.Framework;

/// <summary>
/// The shapes of the slide-in (desk v3, UNITY_PLAN 5.2 and the last slide
/// review): a track backs away before its spring and overshoots once, settled
/// from 250 ms; a lift never dips; the way home arrives at speed and the seat
/// bounce starts on that frame; the hand-off loser eases home gently in 130 ms
/// and is nearly seated before then; a lift squashes over the anticipation;
/// the intercom's mic sways at the stop; a lift's shadow stays at its rest
/// footprint, fading and widening; a slide out from where a slider stands
/// (behind home too) starts there, and every shadow carries on from the alpha
/// it had.
/// </summary>
public class SlideCurveTests
{
    private static readonly DeskSlides Slides = DeskSlides.Approved();

    private static DeskSlider S(string id) => Slides.Find(id);

    [Test]
    public void ATrack_BacksAwayFirst_ThenOvershootsOnce_AndSettlesFrom250ms()
    {
        foreach (DeskSlider s in Slides.sliders)
        {
            if (!s.Moves)
                continue;
            Assert.AreEqual(0f, SlideCurve.Out(Slides, s, 0f), 1e-6f, s.id + ": still on the trigger frame");
            float dip = SlideCurve.Out(Slides, s, Slides.anticipationSeconds * 0.99f);
            if (s.kind == SlideKind.Track)
                Assert.Less(dip, -0.5f * s.anticipation, s.id + ": the base backs away from the mat first");
            else
                Assert.AreEqual(0f, dip, 1e-6f, s.id + ": a lift never dips into the felt");
            Assert.AreEqual(s.Peak, SlideCurve.Out(Slides, s, s.peakSeconds), 1e-3f, s.id + ": one overshoot, at its peak time");
            float tolerance = Math.Max(0.02f, 0.5f / s.travelPx) + 0.002f;
            for (int ms = 250; ms < 650; ms++)
                Assert.AreEqual(1f, SlideCurve.Out(Slides, s, ms / 1000f), tolerance, $"{s.id} settled at {ms} ms (within max(2 %, 0.5 px))");
            float later = SlideCurve.Out(Slides, s, s.peakSeconds + 0.02f);
            Assert.Less(later, s.Peak, s.id + ": it comes back from the peak");
        }
    }

    [Test]
    public void TheWayHome_ArrivesAtSpeed_AndTheSeatBounceStartsOnTheArrivalFrame()
    {
        DeskSlider scanner = S(DeskHomeMap.Scanner);
        float tb = Slides.backSeconds, frame = 1f / 60f;
        float last = SlideCurve.Back(Slides, scanner, tb - frame, 1f) - SlideCurve.Back(Slides, scanner, tb, 1f);
        float first = SlideCurve.Back(Slides, scanner, 0f, 1f) - SlideCurve.Back(Slides, scanner, frame, 1f);
        Assert.Greater(last, 10f * first, "an ease-in: it leaves gently and arrives fast (rereview 5: the ease-in-out arrived at zero speed)");
        Assert.AreEqual(0f, SlideCurve.Back(Slides, scanner, tb, 1f), 1e-5f, "home on the arrival frame");
        Assert.Greater(SlideCurve.Back(Slides, scanner, tb + frame, 1f), 0.05f, "the bounce starts at once, no still frames");
        Assert.AreEqual(scanner.seat, SlideCurve.Back(Slides, scanner, tb + Slides.seatSeconds / 2f, 1f), 1e-5f, "the seat bounce's height");
        Assert.AreEqual(0f, SlideCurve.Back(Slides, scanner, tb + Slides.seatSeconds, 1f), 1e-5f);
        Assert.AreEqual(0f, SlideCurve.Back(Slides, scanner, 1f, 1f), 1e-6f);
        Assert.AreEqual(0.5f, SlideCurve.Back(Slides, scanner, 0f, 0.5f), 1e-6f, "from where it was");
    }

    [Test]
    public void TheHandOffLoser_EasesHomeGently_In130ms_NearlySeatedBeforeThen()
    {
        DeskSlider scanner = S(DeskHomeMap.Scanner);
        float frame = 1f / 60f, px = scanner.travelPx;
        float firstFrame = (1f - SlideCurve.Retract(Slides, frame, 1f)) * px;
        Assert.AreEqual(0.6f, firstFrame, 0.05f, "rereview 4: 0.6 px in its first frame on the scanner, not a pop");
        float fastest = 0f, fastestOut = 0f;
        for (int i = 0; i < 9; i++)
            fastest = Math.Max(fastest, (SlideCurve.Retract(Slides, i * frame, 1f) - SlideCurve.Retract(Slides, (i + 1) * frame, 1f)) * px);
        for (int i = 0; i < 25; i++)
            fastestOut = Math.Max(fastestOut, Math.Abs(SlideCurve.Out(Slides, scanner, (i + 1) * frame) - SlideCurve.Out(Slides, scanner, i * frame)) * px);
        Assert.AreEqual(4.1f, fastest, 0.15f, "its fastest frame");
        Assert.Less(fastest, fastestOut, "never faster than the slide out");
        Assert.AreEqual(0f, SlideCurve.Retract(Slides, Slides.retractSeconds, 1f), 1e-6f, "home in 130 ms, no seat");
        float nearlySeated = 0f;
        for (float t = 0f; t < Slides.retractSeconds; t += 0.001f)
            if (SlideCurve.Retract(Slides, t, 1f) < Slides.winnerBelow)
            {
                nearlySeated = t;
                break;
            }
        Assert.AreEqual(0.101f, nearlySeated, 0.002f, "under 10 % of its travel about 101 ms in: the winner may start");
    }

    [Test]
    public void ALift_SquashesOverTheAnticipation_AndReleasesAsItRises()
    {
        Assert.AreEqual(0f, SlideCurve.Squash(Slides, 0f), 1e-6f);
        Assert.AreEqual(Slides.squashPx, SlideCurve.Squash(Slides, Slides.anticipationSeconds - 1e-4f), 0.01f, "the full squash as the anticipation ends");
        Assert.AreEqual(Slides.squashPx / 2f, SlideCurve.Squash(Slides, Slides.anticipationSeconds + Slides.squashRelease / 2f), 1e-4f, "released over 60 ms");
        Assert.AreEqual(0f, SlideCurve.Squash(Slides, Slides.anticipationSeconds + Slides.squashRelease), 1e-6f);
    }

    [Test]
    public void TheIntercomsMic_SwaysAtTheStop_AndSettles()
    {
        DeskSlider intercom = S(DeskHomeMap.Intercom);
        Assert.AreEqual(0f, SlideCurve.Sway(intercom, -0.06f), 1e-6f, "nothing well before the peak");
        float most = 0f;
        for (float t = -0.05f; t < 0.4f; t += 0.002f)
            most = Math.Max(most, Math.Abs(SlideCurve.Sway(intercom, t)));
        Assert.LessOrEqual(most, intercom.swayPx, "about 4 px at most");
        Assert.Greater(most, 0.5f * intercom.swayPx, "and visible");
        Assert.Less(Math.Abs(SlideCurve.Sway(intercom, 0.5f)), 0.2f, "settled in about 0.3 s");
        Assert.AreEqual(0f, SlideCurve.Sway(S(DeskHomeMap.Scanner), 0.05f), 1e-6f, "only the intercom has a mic");
    }

    [Test]
    public void ALiftsShadow_StaysAtTheRestFootprint_FadingAndWideningAsItRises()
    {
        DeskSlider torch = S(DeskHomeMap.UvTorch);
        Assert.AreEqual(0f, SlideCurve.LiftShadow(Slides, torch, 0f).alpha, 1e-6f, "at rest it is the object's own contact");
        (float alpha, float blur, float widen) top = SlideCurve.LiftShadow(Slides, torch, 1f);
        Assert.AreEqual(Slides.liftShadowAlpha * (1f - Slides.liftShadowFade), top.alpha, 1e-4f, "about 0.55 at the top");
        Assert.AreEqual(Slides.liftShadowBlurPx + Slides.liftShadowBlurPerPx * torch.travelPx, top.blur, 1e-4f);
        Assert.Greater(top.widen, 1f);
        Assert.Less(SlideCurve.LiftShadow(Slides, torch, 0.1f).alpha, top.alpha, "it shows as the gap opens");
    }

    [Test]
    public void ATracksShadow_LeavesTheWellWithTheBase_AndFadesAsItReentersIt_FromTheAlphaItHad()
    {
        Assert.AreEqual(0f, SlideCurve.TrackShadowOut(Slides, Slides.anticipationSeconds, 0f), 1e-6f, "the base still in its well");
        Assert.AreEqual(Slides.trackShadowAlpha, SlideCurve.TrackShadowOut(Slides, Slides.anticipationSeconds + Slides.trackShadowIn, 0f), 1e-6f);
        Assert.AreEqual(Slides.trackShadowAlpha, SlideCurve.TrackShadowBack(Slides, Slides.backSeconds - 0.01f, Slides.trackShadowAlpha), 1e-6f);
        Assert.AreEqual(0f, SlideCurve.TrackShadowBack(Slides, Slides.backSeconds + Slides.trackShadowOut, Slides.trackShadowAlpha), 1e-6f, "gone 60 ms after the seat");
        Assert.AreEqual(0f, SlideCurve.TrackShadowRetract(Slides, Slides.retractSeconds, Slides.trackShadowAlpha), 1e-6f);
        Assert.AreEqual(0.044f, SlideCurve.TrackShadowRetract(Slides, 0f, 0.044f), 1e-6f, "a hand-off on the fade-in's second frame starts from that frame's alpha (the review: 0.044 to 0.35)");
        Assert.AreEqual(Slides.trackShadowAlpha, SlideCurve.TrackShadowOut(Slides, 0f, Slides.trackShadowAlpha), 1e-6f, "summoned again on its way home: it stays (the review: 0.35 to 0)");
        Assert.AreEqual(0.5f, SlideCurve.Fade(1f, 0.03f, 0.06f), 1e-6f, "a carried channel releases linearly");
        Assert.AreEqual(0f, SlideCurve.Fade(1f, 0.07f, 0.06f), 1e-6f);
    }

    [Test]
    public void ASlideOut_StartsWhereTheSliderStands_AndFromBehindHomeNeverDipsDeeperThanItsAnticipation()
    {
        DeskSlider scanner = S(DeskHomeMap.Scanner);
        float frame = 1f / 60f;
        foreach (float start in new[] { 0f, 0.4f, -0.05f, -0.117f, -scanner.anticipation })
        {
            Assert.AreEqual(start, SlideCurve.OutFrom(Slides, scanner, 0f, start), 1e-6f, $"from {start}: no pop on its first frame");
            float deepest = 0f, step = 0f;
            for (int i = 1; i < 40; i++)
            {
                float a = SlideCurve.OutFrom(Slides, scanner, (i - 1) * frame, start), b = SlideCurve.OutFrom(Slides, scanner, i * frame, start);
                deepest = Math.Min(deepest, b);
                step = Math.Max(step, Math.Abs(b - a) * scanner.travelPx);
            }
            Assert.GreaterOrEqual(deepest, Math.Min(start, 0f) - scanner.anticipation - 1e-5f, $"from {start}");
            if (start < 0f)
                Assert.GreaterOrEqual(deepest, -scanner.anticipation - 1e-5f, $"from {start}: behind home it eases into this out's anticipation, never deeper");
            Assert.AreEqual(1f, SlideCurve.OutFrom(Slides, scanner, 0.6f, start), 0.02f, $"from {start}: out at its slid spot");
            Assert.Less(step, 7f, $"from {start}: no frame faster than the slide out's own");
        }
        Assert.AreEqual(SlideCurve.Out(Slides, scanner, 0.1f), SlideCurve.OutFrom(Slides, scanner, 0.1f, 0f), 1e-6f, "from home it is the slide out");
        Assert.AreEqual(1f, SlideCurve.WayHome(1f, 0f), 1e-6f);
        Assert.AreEqual(0f, SlideCurve.WayHome(1f, 1f), 1e-6f);
        Assert.AreEqual(1f - 0.125f, SlideCurve.WayHome(1f, 0.5f), 1e-6f, "the way home's ease-in (cubic)");
    }

    [Test]
    public void WhereTheSlidePutsIt_ATrackAlongItsHomesWay_ALiftUp_AndItsShadowFollowsATrackOnly()
    {
        DeskSlides slides = DeskSlides.Approved();
        DeskSlider scanner = slides.Find(DeskHomeMap.Scanner), torch = slides.Find(DeskHomeMap.UvTorch), detain = slides.Find(DeskHomeMap.Detain);
        float way = scanner.Way(DeskHomeMap.Find(DeskHomeMap.Approved(), DeskHomeMap.Scanner));
        Assert.AreEqual(-1f, way, "the scanner's home declares left, toward the mat");
        Close((-0.041f * 1.2f, 0f), SlideCurve.Offset(scanner, way, 1.2f), "a track along its home's way, never up");
        Close((0.041f * 1.2f, 0f), SlideCurve.Offset(scanner, 1f, 1.2f), "the other way when its home declares it");
        Close((0f, 0f), SlideCurve.Offset(scanner, 0f, 1f), "a home that declares no slide keeps it put");
        Close((0f, 0.015f * 0.5f), SlideCurve.Offset(torch, -1f, 0.5f), "a lift rises, never sideways");
        Close((0f, 0f), SlideCurve.Offset(detain, 1f, 1f), "a fixed object never moves");

        Close((0.3f - 0.041f, 0.2f), SlideCurve.ShadowCentre(scanner, way, 1f, (0.3f, 0.2f)), "a track's shadow travels with its base");
        Close((0.3f, 0.2f), SlideCurve.ShadowCentre(torch, way, 1f, (0.3f, 0.2f)), "a lift's stays at its rest footprint");
        Assert.AreEqual(0.041f / 17.98f, scanner.MetresPerPx, 1e-7f, "the travel's metres per px at 1080p");
        Assert.AreEqual(0f, new DeskSlider("x", null, SlideKind.Lift, 0.01f).MetresPerPx, "no measured px, no scale");
    }

    [Test]
    public void ALiftsSquash_BringsItsTopDown_NeverBelowHalf_AndTheShadowScalesToItsWideningAndBlur()
    {
        Assert.AreEqual(1f - 1.5f * 0.002f / 0.06f, SlideCurve.SquashScale(1.5f, 0.002f, 0.06f), 1e-6f, "1.5 px of a 6 cm tool's height");
        Assert.AreEqual(0.5f, SlideCurve.SquashScale(50f, 0.002f, 0.06f), 1e-6f, "never below half its height");
        Assert.AreEqual(1f, SlideCurve.SquashScale(0f, 0.002f, 0.06f), "no squash");
        Assert.AreEqual(1f, SlideCurve.SquashScale(1.5f, 0.002f, 0f), "no measured height: no squash");

        (float x, float y) same = SlideCurve.ShadowScale((0.1f, 0.2f), 0.002f, 0.002f, 1f);
        Assert.AreEqual(1f, same.x, 1e-6f, "as built");
        Assert.AreEqual(1f, same.y, 1e-6f);
        (float x, float y) wide = SlideCurve.ShadowScale((0.1f, 0.2f), 0.002f, 0.004f, 1.1f);
        Assert.AreEqual((0.1f * 1.1f + 0.008f) / 0.104f, wide.x, 1e-6f, "wider by its widening, its soft edge grown");
        Assert.AreEqual((0.2f * 1.1f + 0.008f) / 0.204f, wide.y, 1e-6f);
    }

    private static void Close((float x, float y) expected, (float x, float y) actual, string message = null)
    {
        Assert.AreEqual(expected.x, actual.x, 1e-6f, message);
        Assert.AreEqual(expected.y, actual.y, 1e-6f, message);
    }

    [Test]
    public void TheGlasses_TipTowardTheChair_InProportionToTheLift()
    {
        DeskSlider glasses = S(DeskHomeMap.Glasses);
        Assert.AreEqual(7f, SlideCurve.Tip(glasses, 1f), 1e-6f);
        Assert.AreEqual(3.5f, SlideCurve.Tip(glasses, 0.5f), 1e-6f);
        Assert.AreEqual(0f, SlideCurve.Tip(glasses, -0.1f), 1e-6f);
        Assert.AreEqual(0f, SlideCurve.Tip(S(DeskHomeMap.UvTorch), 1f), 1e-6f, "the torch rises straight");
        // The daters' tip against slides.json's own words: DeskSlidesTests.TheTips_AreSlidesJsons_TheGlasses7Degrees_TheDaters5TowardTheHand.
    }

    [Test]
    public void ASlideOut_FromBeyondItsSlidSpot_SettlesBack_NeverPastWhereItStood_NorThePeakTheGatePassed()
    {
        // The code review (round 3, 1): summoned again at 1.1625 (on the frame its way home starts, or held there by a landing paper),
        // the old curve turned its anticipation toward the mat and peaked at 1.1875 against the gated 1.1665.
        float frame = 1f / 60f;
        foreach (DeskSlider s in Slides.sliders)
        {
            if (!s.Moves)
                continue;
            foreach (float start in new[] { 1.02f, 1f + s.overshoot * 0.7f, s.Peak })
            {
                string at = $"{s.id} from {start:0.0000}";
                Assert.AreEqual(start, SlideCurve.OutFrom(Slides, s, 0f, start), 1e-6f, at + ": no pop on its first frame");
                float before = start, highest = start;
                for (int i = 1; i < 60; i++)
                {
                    float f = SlideCurve.OutFrom(Slides, s, i * frame, start);
                    highest = Math.Max(highest, f);
                    if (i == 1)
                        Assert.LessOrEqual(f, start + 1e-6f, at + ": its first move is back toward its slid spot, never toward the mat");
                    Assert.Less(Math.Abs(f - before) * s.travelPx, 7f, at + ": no frame faster than the slide out's own");
                    before = f;
                }
                Assert.LessOrEqual(highest, start + 1e-6f, at + ": never further out than it stood");
                Assert.LessOrEqual(highest, s.Peak + 1e-6f, at + ": never past the peak the gate passed");
                Assert.AreEqual(1f, SlideCurve.OutFrom(Slides, s, 0.6f, start), Math.Max(0.02f, 0.5f / s.travelPx) + 0.002f, at + ": settled at its slid spot");
            }
        }
        Assert.AreEqual(1f, SlideCurve.Release(Slides, S(DeskHomeMap.Scanner), 0f), 1e-6f);
        Assert.AreEqual(1f, SlideCurve.OutFrom(Slides, S(DeskHomeMap.Scanner), 0.05f, 1f), 1e-6f, "at its slid spot it stays there");
    }

    [Test]
    public void AHurriedWayHome_CarriesOnAtItsSpeed_EasesIntoHomeAtRest_NeverPastIt_WithinWinnerAfter()
    {
        // The code review (round 3, 3): hurried, the old way home stopped dead at full speed, 4.74 px in its last frame, with no seat
        // bounce and no clack. slides.json's hand-off loser arrives slowly: a hurried one now carries on at the speed it had and eases in.
        DeskSlider scanner = S(DeskHomeMap.Scanner);
        float tb = Slides.backSeconds, frame = 1f / 60f, fastest = 4.1f + 0.05f;
        for (float u = 0f; u < 1f; u += 0.05f)
        {
            float start = SlideCurve.WayHome(1f, u), speed = SlideCurve.WayHomeSpeed(1f, u, tb);
            float over = SlideCurve.HurryTime(start, speed, Slides.winnerAfter);
            string at = $"hurried {u:0.00} of the way home";
            Assert.LessOrEqual(over, Slides.winnerAfter + 1e-6f, at + ": home within winnerAfter");
            Assert.AreEqual(start, SlideCurve.Hurry(start, speed, over, 0f), 1e-6f, at + ": no pop where it was hurried");
            float early = (SlideCurve.Hurry(start, speed, over, 1e-4f) - start) / 1e-4f;
            Assert.AreEqual(speed, early, Math.Abs(speed) * 0.02f + 0.05f, at + ": it carries on at the speed it had (no jolt)");
            float late = (SlideCurve.Hurry(start, speed, over, over) - SlideCurve.Hurry(start, speed, over, over - 1e-4f)) / 1e-4f;
            Assert.AreEqual(0f, late, 0.05f, at + ": it arrives at rest (no slam into the stop, no clack)");
            float before = start;
            for (float t = frame; t < over + frame; t += frame)
            {
                float f = SlideCurve.Hurry(start, speed, over, t);
                Assert.GreaterOrEqual(f, -1e-6f, at + ": never past home");
                Assert.LessOrEqual(f, before + 1e-6f, at + ": only ever toward home");
                Assert.LessOrEqual((before - f) * scanner.travelPx, fastest, at + ": no frame faster than the hand-off retract's fastest (slides.json: 4.1 px)");
                before = f;
            }
            Assert.AreEqual(0f, SlideCurve.Hurry(start, speed, over, over), 1e-6f, at + ": home");
        }
        Assert.AreEqual(0f, SlideCurve.Hurry(0.5f, 2f, 0.1f, 0.1f), 1e-6f, "a speed away from home counts as none");
        Assert.LessOrEqual(SlideCurve.Hurry(0.5f, 2f, 0.1f, 0.01f), 0.5f + 1e-6f, "and never takes it further out");
    }

    [Test]
    public void ALiftsSquash_IsAboutItsBase_TheBaseStaysDown_AndTheTopComesDownBySquashPx()
    {
        // The code review (round 3, 4): the UV torch's pivot is its grip axis, 3.4 cm up, so a squash about it lifted its underside off
        // the cradle by about 0.75 px and brought its top down only about 0.75 px. UNITY_PLAN 5.1: "base down", the top 1.5 px down.
        DeskSlider torch = S(DeskHomeMap.UvTorch);
        float metresPerPx = torch.MetresPerPx, height = 0.068f, pivotAboveBase = 0.034f, pivot = 1f;
        float scale = SlideCurve.SquashScale(Slides.squashPx, metresPerPx, height);
        float drop = SlideCurve.SquashDrop(scale, pivotAboveBase);
        float baseNow = pivot - drop - scale * pivotAboveBase, topNow = pivot - drop + scale * (height - pivotAboveBase);
        Assert.AreEqual(pivot - pivotAboveBase, baseNow, 1e-6f, "the base stays on its cradle");
        Assert.AreEqual(Slides.squashPx, (pivot - pivotAboveBase + height - topNow) / metresPerPx, 1e-3f, "the top comes down by the squash's px");
        Assert.AreEqual(0f, SlideCurve.SquashDrop(scale, 0f), "a pivot at the base needs no drop");
        Assert.AreEqual(0f, SlideCurve.SquashDrop(1f, pivotAboveBase), "no squash, no drop");
    }
}
