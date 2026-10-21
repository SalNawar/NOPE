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
    public void TheGlasses_TipTowardTheChair_InProportionToTheLift()
    {
        DeskSlider glasses = S(DeskHomeMap.Glasses);
        Assert.AreEqual(7f, SlideCurve.Tip(glasses, 1f), 1e-6f);
        Assert.AreEqual(3.5f, SlideCurve.Tip(glasses, 0.5f), 1e-6f);
        Assert.AreEqual(0f, SlideCurve.Tip(glasses, -0.1f), 1e-6f);
        Assert.AreEqual(0f, SlideCurve.Tip(S(DeskHomeMap.UvTorch), 1f), 1e-6f, "only the glasses tip");
    }
}
