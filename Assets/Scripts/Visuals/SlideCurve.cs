using System;

/// <summary>
/// The shapes of the desk's slide-in (desk v3; render/slides.json and the
/// desk pipeline's layout.slide_out, slide_back, slide_retract, lift_squash
/// and secondary, so Unity plays the curves the offline gate passed), as a
/// share of a slider's travel (0 at home, 1 at its slid spot):
/// <list type="bullet">
/// <item>Out: a track first backs away from the mat (the anticipation, a half
/// cosine over DeskSlides.anticipationSeconds; a lift does not dip), then its
/// own damped spring to the slid spot with one overshoot, settled from 250 ms.</item>
/// <item>Back: an ease-in (cubic) home that arrives at full speed, and the seat
/// bounce off the home stop starting on that arrival frame.</item>
/// <item>Retract (the hand-off loser): an ease-in-out (quad) home, no seat.</item>
/// <item>A lift's squash (px of height over the anticipation, released as it
/// rises), the secondary sway at the stop (px), the contact shadows and the
/// lamp's pulse.</item>
/// </list>
/// Pure; ProximityAssist evaluates it per slider and phase.
/// </summary>
public static class SlideCurve
{
    /// <summary>The position <paramref name="t"/> seconds after the trigger frame of <paramref name="s"/>'s slide out.</summary>
    public static float Out(DeskSlides slides, DeskSlider s, float t)
    {
        if (t <= 0f)
            return 0f;
        float ta = slides.anticipationSeconds, a = s.kind == SlideKind.Lift ? 0f : s.anticipation;
        if (t < ta)
            return -a * (1f - (float)Math.Cos(Math.PI * t / ta)) / 2f;
        double z = s.damping, wd = s.DampedFrequency(ta);
        if (wd <= 0.0 || z <= 0.0 || z >= 1.0)
            return 1f;
        double root = Math.Sqrt(1.0 - z * z), wn = wd / root, u = t - ta;
        return (float)(1.0 - (1.0 + a) * Math.Exp(-z * wn * u) * (Math.Cos(wd * u) + z / root * Math.Sin(wd * u)));
    }

    /// <summary>
    /// The position <paramref name="t"/> seconds into <paramref name="s"/>'s way
    /// home from <paramref name="start"/>: an ease-in (cubic) that arrives at
    /// full speed (three times the mean) at DeskSlides.backSeconds, then the
    /// seat bounce (s.seat, a half sine over seatSeconds) off the home stop; 0 after.
    /// </summary>
    public static float Back(DeskSlides slides, DeskSlider s, float t, float start)
    {
        float tb = slides.backSeconds, seat = slides.seatSeconds;
        if (t <= 0f)
            return start;
        if (t < tb)
        {
            float u = t / tb;
            return start * (1f - u * u * u);
        }
        if (t < tb + seat)
            return s.seat * (float)Math.Sin(Math.PI * (t - tb) / seat);
        return 0f;
    }

    /// <summary>The hand-off loser's position <paramref name="t"/> seconds into its retract from <paramref name="start"/>: an ease-in-out (quad) home over DeskSlides.retractSeconds, no seat.</summary>
    public static float Retract(DeskSlides slides, float t, float start)
    {
        float T = slides.retractSeconds;
        if (t <= 0f)
            return start;
        if (t >= T)
            return 0f;
        float u = t / T;
        float e = u < 0.5f ? 2f * u * u : 1f - (2f - 2f * u) * (2f - 2f * u) / 2f;
        return start * (1f - e);
    }

    /// <summary>A lift's squash (px of height, its base down) <paramref name="t"/> seconds after its trigger: up to DeskSlides.squashPx over the anticipation, released over squashRelease as it rises.</summary>
    public static float Squash(DeskSlides slides, float t)
    {
        float ta = slides.anticipationSeconds, release = slides.squashRelease;
        if (t <= 0f || t >= ta + release)
            return 0f;
        if (t < ta)
            return slides.squashPx * (float)Math.Sin(Math.PI / 2.0 * t / ta);
        return slides.squashPx * (1f - (t - ta) / release);
    }

    /// <summary>The secondary sway (px toward the mat; the intercom's mic) <paramref name="sincePeak"/> seconds after the overshoot peak: it starts as the base decelerates into the peak (50 ms before it) and decays.</summary>
    public static float Sway(DeskSlider s, float sincePeak)
    {
        if (s.swayPx <= 0f || sincePeak < -0.05f || s.swayDecay <= 0f)
            return 0f;
        double u = sincePeak + 0.05;
        return (float)(s.swayPx * Math.Sin(2.0 * Math.PI * s.swayHz * u) * Math.Exp(-u / s.swayDecay));
    }

    /// <summary>
    /// A lift's contact shadow left at its rest footprint at position
    /// <paramref name="f"/>: its alpha (it appears over the first 2 px of
    /// lift and fades by liftShadowFade by the top), its blur (px) and how
    /// much wider it is (a factor); nothing at rest.
    /// </summary>
    public static (float alpha, float blurPx, float widen) LiftShadow(DeskSlides slides, DeskSlider s, float f)
    {
        float px = Math.Max(0f, f) * s.travelPx;
        if (px <= 0f)
            return (0f, 0f, 1f);
        float alpha = slides.liftShadowAlpha * Math.Min(1f, px / 2f) * (1f - slides.liftShadowFade * Math.Min(1f, px / Math.Max(1f, s.travelPx)));
        return (alpha, slides.liftShadowBlurPx + slides.liftShadowBlurPerPx * px, 1f + slides.liftShadowWidenPerPx * px);
    }

    /// <summary>A track's contact shadow <paramref name="t"/> seconds after its trigger: it leaves the well with the base as the anticipation ends, fading in to trackShadowAlpha over trackShadowIn.</summary>
    public static float TrackShadowOut(DeskSlides slides, float t) =>
        slides.trackShadowAlpha * Clamp01((t - slides.anticipationSeconds) / Math.Max(1e-4f, slides.trackShadowIn));

    /// <summary>A track's contact shadow <paramref name="t"/> seconds into its way home: full until it arrives, then fading out over trackShadowOut as the base re-enters its well.</summary>
    public static float TrackShadowBack(DeskSlides slides, float t) =>
        t < slides.backSeconds ? slides.trackShadowAlpha : slides.trackShadowAlpha * Math.Max(0f, 1f - (t - slides.backSeconds) / Math.Max(1e-4f, slides.trackShadowOut));

    /// <summary>A track's contact shadow <paramref name="t"/> seconds into a hand-off retract: fading out over the retract.</summary>
    public static float TrackShadowRetract(DeskSlides slides, float t) =>
        slides.trackShadowAlpha * Math.Max(0f, 1f - t / Math.Max(1e-4f, slides.retractSeconds));

    /// <summary>A tip of <paramref name="s"/> at position <paramref name="f"/> (degrees): its full tip at the slid spot, in proportion along the way (none for a slider that does not tip).</summary>
    public static float Tip(DeskSlider s, float f) => s.tipDegrees * Math.Max(0f, f);

    private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
}
