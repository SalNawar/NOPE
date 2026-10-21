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
/// rises), the secondary sway at the stop (px) and the contact shadows, each
/// shadow carrying on from the alpha it had when its phase began, so nothing
/// pops (Saleh: everything animated, nothing pops).</item>
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
    /// The position <paramref name="t"/> seconds after a slide out that
    /// starts from <paramref name="start"/> (where the slider stood when it
    /// was summoned): from home or on its way, the out spring over what is
    /// left of the travel; from behind home (a track caught in its
    /// anticipation, retracting from it), it eases from there into this out's
    /// anticipation over the anticipation's time, never deeper than the
    /// deeper of the two, then springs on as a fresh out; from beyond its
    /// slid spot (summoned again near its overshoot peak: on the frame its
    /// way home starts, or held there by a landing paper), its own spring
    /// let go from where it stands (Release): it settles back onto the slid
    /// spot and never goes further out than it already stood, so it never
    /// passes the peak the gate passed (no anticipation toward the mat).
    /// Equal to <paramref name="start"/> on its first frame: no pop.
    /// </summary>
    public static float OutFrom(DeskSlides slides, DeskSlider s, float t, float start)
    {
        if (start > 1f)
            return 1f + (start - 1f) * Release(slides, s, t);
        float o = Out(slides, s, t);
        if (start >= 0f)
            return start + (1f - start) * o;
        float ta = slides.anticipationSeconds;
        float w = t <= 0f ? 0f : t >= ta ? 1f : (1f - (float)Math.Cos(Math.PI * t / ta)) / 2f;
        return o + start * (1f - w);
    }

    /// <summary>
    /// <paramref name="s"/>'s own spring let go at rest from a displacement
    /// of 1, <paramref name="t"/> seconds later: 1 on its first frame,
    /// falling (it only ever moves toward its rest first), past it once by
    /// its overshoot and settling to 0; never above 1. A slider summoned
    /// beyond its slid spot settles from there (OutFrom).
    /// </summary>
    public static float Release(DeskSlides slides, DeskSlider s, float t)
    {
        if (t <= 0f)
            return 1f;
        double z = s.damping, wd = s.DampedFrequency(slides.anticipationSeconds);
        if (wd <= 0.0 || z <= 0.0 || z >= 1.0)
            return 0f;
        double root = Math.Sqrt(1.0 - z * z), wn = wd / root;
        return (float)(Math.Exp(-z * wn * t) * (Math.Cos(wd * t) + z / root * Math.Sin(wd * t)));
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
            return WayHome(start, t / tb);
        if (t < tb + seat)
            return s.seat * (float)Math.Sin(Math.PI * (t - tb) / seat);
        return 0f;
    }

    /// <summary>The way home's ease-in (cubic) from <paramref name="start"/> at <paramref name="u"/> of its way (0 to 1): it leaves gently and arrives at three times its mean speed.</summary>
    public static float WayHome(float start, float u)
    {
        u = Clamp01(u);
        return start * (1f - u * u * u);
    }

    /// <summary>The way home's speed (share of the travel a second, toward home negative) from <paramref name="start"/> at <paramref name="u"/> of its way, over <paramref name="backSeconds"/>.</summary>
    public static float WayHomeSpeed(float start, float u, float backSeconds)
    {
        u = Clamp01(u);
        return -3f * start * u * u / Math.Max(1e-4f, backSeconds);
    }

    /// <summary>
    /// A way home hurried by a summoned winner (a hand-off loser's way home,
    /// slides.json's hand-off: it arrives slowly, so no clack and no seat):
    /// <paramref name="t"/> seconds after it was hurried from
    /// <paramref name="start"/>, moving at <paramref name="speed"/> (share of
    /// the travel a second) then, home in <paramref name="over"/> seconds. A
    /// cubic that carries on at the speed it had (no stop and no jolt where it
    /// was hurried) and eases into home at rest (never a slam into the stop at
    /// full speed). A speed away from home counts as none. With HurryTime's
    /// time it never passes home.
    /// </summary>
    public static float Hurry(float start, float speed, float over, float t)
    {
        if (t <= 0f)
            return start;
        if (over <= 0f || t >= over)
            return 0f;
        if (speed * start > 0f)
            speed = 0f;
        float u = t / over, u2 = u * u, u3 = u2 * u;
        return (2f * u3 - 3f * u2 + 1f) * start + (u3 - 2f * u2 + u) * over * speed;
    }

    /// <summary>
    /// How long a way home hurried from <paramref name="start"/> at
    /// <paramref name="speed"/> takes (Hurry): within
    /// <paramref name="winnerAfter"/> (the winner never waits longer), and
    /// never so long that the speed it keeps would carry it past home (a cubic
    /// that arrives at rest stays on its side of home while over times the
    /// speed is at most three times the way left).
    /// </summary>
    public static float HurryTime(float start, float speed, float winnerAfter)
    {
        float toward = speed * start < 0f ? Math.Abs(speed) : 0f;
        float limit = toward > 1e-6f ? 3f * Math.Abs(start) / toward : float.PositiveInfinity;
        return Math.Max(1e-4f, Math.Min(winnerAfter, limit));
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

    /// <summary>A track's contact shadow <paramref name="t"/> seconds after its trigger, from the alpha it had then (<paramref name="from"/>; 0 from home): it leaves the well with the base as the anticipation ends, fading in to trackShadowAlpha over trackShadowIn.</summary>
    public static float TrackShadowOut(DeskSlides slides, float t, float from) =>
        from + (slides.trackShadowAlpha - from) * Clamp01((t - slides.anticipationSeconds) / Math.Max(1e-4f, slides.trackShadowIn));

    /// <summary>A track's contact shadow <paramref name="t"/> seconds into its way home, from the alpha it had then (<paramref name="from"/>): held until it arrives, then fading out over trackShadowOut as the base re-enters its well.</summary>
    public static float TrackShadowBack(DeskSlides slides, float t, float from) =>
        t < slides.backSeconds ? from : Fade(from, t - slides.backSeconds, slides.trackShadowOut);

    /// <summary>A track's contact shadow <paramref name="t"/> seconds into a hand-off retract, from the alpha it had then (<paramref name="from"/>): fading out over the retract.</summary>
    public static float TrackShadowRetract(DeskSlides slides, float t, float from) => Fade(from, t, slides.retractSeconds);

    /// <summary><paramref name="from"/> released to nothing linearly over <paramref name="over"/> seconds, <paramref name="t"/> seconds on (a squash, a sway or a shadow carried over a change of phase, so it never pops).</summary>
    public static float Fade(float from, float t, float over) => t <= 0f ? from : from * Math.Max(0f, 1f - t / Math.Max(1e-4f, over));

    /// <summary>A tip of <paramref name="s"/> at position <paramref name="f"/> (degrees): its full tip at the slid spot, in proportion along the way (none for a slider that does not tip).</summary>
    public static float Tip(DeskSlider s, float f) => s.tipDegrees * Math.Max(0f, f);

    /// <summary>
    /// Where <paramref name="s"/>'s slide puts it at position
    /// <paramref name="f"/> (MachineSlideIn writes it to its slide transform,
    /// or as a layer over its owner's pose): metres along the desk frame's
    /// right and up from its rest. A track moves along x the way its home
    /// declares (<paramref name="way"/>: DeskSlider.Way, -1 or +1; 0 keeps it
    /// put), never up; a lift rises, never sideways.
    /// </summary>
    public static (float right, float up) Offset(DeskSlider s, float way, float f) =>
        s.kind == SlideKind.Lift ? (0f, f * s.travel) : s.kind == SlideKind.Track ? (way * f * s.travel, 0f) : (0f, 0f);

    /// <summary>The height factor of a lift squashed by <paramref name="squashPx"/> (px at 1080p, <paramref name="metresPerPx"/> each) over its <paramref name="height"/> (metres): scaled by it about its base (SquashDrop), its top comes down by the squash, never below half its height; 1 with no squash or no measured height.</summary>
    public static float SquashScale(float squashPx, float metresPerPx, float height) =>
        squashPx > 0f && height > 1e-4f ? Math.Max(0.5f, 1f - squashPx * metresPerPx / height) : 1f;

    /// <summary>
    /// How far (metres) a lift's slide transform comes down while its height
    /// is scaled by <paramref name="scale"/> (SquashScale) about its pivot,
    /// <paramref name="pivotAboveBase"/> over its base (the UV torch's grip
    /// axis 3.4 cm up), so the squash is about its base: the base stays on
    /// its cradle and only the top comes down (UNITY_PLAN 5.1: "base down").
    /// 0 for a pivot at the base or no squash.
    /// </summary>
    public static float SquashDrop(float scale, float pivotAboveBase) => scale < 1f && pivotAboveBase > 0f ? (1f - scale) * pivotAboveBase : 0f;

    /// <summary>The centre of <paramref name="s"/>'s contact shadow on the desk (the desk frame's x, y) at position <paramref name="f"/>, its rest footprint centred at <paramref name="rest"/>: a lift's stays at its rest footprint, a track's travels with its base (Offset's x).</summary>
    public static (float x, float y) ShadowCentre(DeskSlider s, float way, float f, (float x, float y) rest) =>
        s.kind == SlideKind.Track ? (rest.x + Offset(s, way, f).right, rest.y) : rest;

    /// <summary>
    /// How much a contact shadow built for a rest footprint of
    /// <paramref name="size"/> metres with a soft edge of
    /// <paramref name="builtSpread"/> metres scales along x and y to show
    /// that footprint <paramref name="widen"/> times as wide with a soft edge
    /// of <paramref name="spread"/> metres (SlideShadow.Show).
    /// </summary>
    public static (float x, float y) ShadowScale((float x, float y) size, float builtSpread, float spread, float widen) =>
        ((size.x * widen + 2f * spread) / Math.Max(1e-5f, size.x + 2f * builtSpread), (size.y * widen + 2f * spread) / Math.Max(1e-5f, size.y + 2f * builtSpread));

    private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
}
