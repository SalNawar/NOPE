using System;

/// <summary>
/// The desk's one light (BUG-14: the desk went dark at dusk with no source
/// changing, and papers, props, rack, traveller and photo each followed their
/// own rule). From the hour on the shared day cycle (HallDayCycle) it gives
/// one level and one colour that every desk shader multiplies by (DeskLight.hlsl,
/// published by DeskLightLevel): the room's light falling from the full day
/// (level 1, the desk as drawn) to its night floor (a cool tint), plus the
/// work lamp's warm share once the lamp is on. The lamp switches on in the
/// dusk (<see cref="LampOnHour"/>: the hour the hall's evening reaches the
/// lamp's knob; the sunset, 16:30, with the shipped knob, a quarter of an hour
/// before the plan's afternoon dip H3, because the unlit mat would fall under
/// the readable line by 16:45) and its share counts only while it warms up and burns,
/// so the switch-on reads as a warm pool coming up on the mat. Levels are
/// relative to the full day: 1 draws the desk as by day, so nothing changes
/// at noon, and the level never passes 1 (the lamp fills only what the room
/// has lost: no paper white above 1 for the bloom, no traveller tint clamped
/// while the photo's is not, no dusk brighter than the drawn day). The
/// readable rule is judged on the image the office camera shows: the finish's
/// exposure, Neutral tonemapping and contrast are modelled (<see cref="Finish"/>,
/// <see cref="GradedLuma"/>). Pure and allocation-free (read every frame).
/// </summary>
public static class DeskLighting
{
    /// <summary>The drawn mat's luma by day (the desk as drawn, the light's level 1).</summary>
    public const float DayMatLuma = 76f;

    /// <summary>The mat's luma at night below which it stops reading (FIX_PLAN LIGHT's pass line: "mat luma ≥55 at every hour").</summary>
    public const float ReadableMatLuma = 55f;

    /// <summary>The mat's night luma the plan aims for (FIX_PLAN LIGHT: "the desk at night stays readable (luma ≈60)").</summary>
    public const float TargetMatLuma = 60f;

    /// <summary>
    /// How far the modelled corners must clear <see cref="ReadableMatLuma"/>
    /// (luma): what the model leaves out (the grade's saturation and colour
    /// filter on a tinted light, the 32³ LUT's interpolation, the mat
    /// texture's spread around its mean of 76). Proposal (ours).
    /// </summary>
    public const float MarginLuma = 2f;

    /// <summary>
    /// The least luma step from the pool's centre to the mat's corners at
    /// night, after the finish, for the lamp to read as a pool and not as a
    /// tint (FIX_PLAN LIGHT: "a warm pool on the mat"). Proposal (ours); judged
    /// by eye with the reviewer at U4 and U18.
    /// </summary>
    public const float PoolContrastLuma = 8f;

    /// <summary>The least luma the pool's centre gains as the lamp switches on (LampOnHour), after the finish (FIX_PLAN LIGHT: "a visible lamp switch-on event at the dip"). Proposal (ours).</summary>
    public const float SwitchOnLuma = 5f;

    /// <summary>
    /// The light (its luminance, 1 = the full day) that keeps the mat at
    /// <see cref="ReadableMatLuma"/> before the finish: the sRGB-decoded ratio
    /// of 55 to the day's 76, about 0.529. The office finish costs the dim
    /// mat a little more (<see cref="GradedLuma"/>: 0.529 shows at about luma
    /// 52.5), so the readable rule is judged after the finish
    /// (<see cref="ReadableAfterFinish"/>); this is the unfinished line.
    /// </summary>
    public static readonly float ReadableNightLuminance = LuminanceForLuma(ReadableMatLuma);

    /// <summary>The light that puts the mat at <see cref="TargetMatLuma"/> (about 0.625): the pool's centre reaches it at night.</summary>
    public static readonly float TargetNightLuminance = LuminanceForLuma(TargetMatLuma);

    /// <summary>The light (relative luminance, 1 = the full day) that takes the day mat's luma to <paramref name="luma"/> (0-255): the ratio of their sRGB-decoded values.</summary>
    public static float LuminanceForLuma(float luma) => SrgbToLinear(luma / 255f) / SrgbToLinear(DayMatLuma / 255f);

    /// <summary>The luma (0-255) the day mat reads at under a light of relative <paramref name="luminance"/>: the inverse of <see cref="LuminanceForLuma"/>.</summary>
    public static float LumaAt(float luminance) => 255f * LinearToSrgb(SrgbToLinear(DayMatLuma / 255f) * Math.Max(0f, luminance));

    /// <summary>A linear colour's relative luminance (Rec. 709).</summary>
    public static float Luminance(float r, float g, float b) => 0.2126f * r + 0.7152f * g + 0.0722f * b;

    /// <summary>
    /// The lamp pool's weight at <paramref name="distance"/> metres from its
    /// centre (DeskLight.hlsl's DeskLampPool, the same maths): 1 out to a
    /// quarter of <paramref name="radius"/>, easing to 0 at the radius.
    /// </summary>
    public static float PoolWeight(float distance, float radius)
    {
        float r = Math.Max(radius, 1e-3f);
        return 1f - Smooth(0.25f * r, r, Math.Abs(distance));
    }

    /// <summary>
    /// The light's luminance at a desk point whose pool weight is
    /// <paramref name="poolWeight"/> (DeskLight.hlsl's DeskLightAt, the same
    /// maths: the light, its colour times its level, less the lamp's colour
    /// times its share outside the pool, so the hue falls off with the level:
    /// warm under the lamp, the room's cool at the mat's corners).
    /// </summary>
    public static float LuminanceAt(in Light l, float poolWeight) =>
        Luminance(l.R, l.G, l.B) * l.Level - Luminance(l.LampR, l.LampG, l.LampB) * l.LampShare * (1f - Clamp01(poolWeight));

    /// <summary>The light's colour times its level at a desk point whose pool weight is <paramref name="poolWeight"/> (DeskLightAt's value; each channel 0..1).</summary>
    public static Rgba LightAt(in Light l, float poolWeight)
    {
        float off = l.LampShare * (1f - Clamp01(poolWeight));
        return new Rgba(l.R * l.Level - l.LampR * off, l.G * l.Level - l.LampG * off, l.B * l.Level - l.LampB * off);
    }

    /// <summary>
    /// The darkest point of a <paramref name="matWidth"/> × <paramref name="matDepth"/>
    /// metre mat centred under the pool (its corners): the luminance there
    /// under <paramref name="l"/> with a pool of <paramref name="radius"/>.
    /// </summary>
    public static float DarkestOnMat(in Light l, float radius, float matWidth, float matDepth) =>
        LuminanceAt(l, PoolWeight(0.5f * (float)Math.Sqrt(matWidth * matWidth + matDepth * matDepth), radius));

    /// <summary>The light at full night with the lamp full on (the readable rule's worst hour).</summary>
    public static Light FullNight(in Knobs k) => AtDaylight(0f, k, 1f);

    /// <summary>
    /// The office camera's finish as the readable rule sees it
    /// (OfficeFinishProfile.asset through HallLightingSO.finishProfile, and the
    /// URP asset's grading mode): Color Adjustments' post exposure (EV) and
    /// contrast, and Neutral tonemapping. Saturation and the colour filter
    /// keep a grey's luminance and are left out (<see cref="MarginLuma"/>).
    /// The vignette is not part of it: it is a frame that darkens the same
    /// screen point by day as by night (<see cref="VignetteAt"/>).
    /// </summary>
    public readonly struct Finish
    {
        /// <summary>Color Adjustments' post exposure (EV): the light times 2^EV before the grade.</summary>
        public readonly float PostExposure;

        /// <summary>Color Adjustments' contrast (-100..100), applied in LogC around ACEScc's mid grey.</summary>
        public readonly float Contrast;

        /// <summary>True when Neutral tonemapping is on.</summary>
        public readonly bool Neutral;

        /// <summary>True for URP's HDR grading mode (contrast, then the tonemap, in one LogC LUT); false for LDR (the tonemap, then the LUT's contrast).</summary>
        public readonly bool HdrGrading;

        /// <summary>Creates the finish.</summary>
        public Finish(float postExposure, float contrast, bool neutral, bool hdrGrading)
        {
            PostExposure = postExposure;
            Contrast = contrast;
            Neutral = neutral;
            HdrGrading = hdrGrading;
        }

        /// <summary>No finish: the image as drawn.</summary>
        public static Finish None => new Finish(0f, 0f, false, false);

        /// <summary>The shipped office finish (OfficeFinishProfile.asset: exposure +0.1 EV, contrast +12, Neutral; UniversalRP.asset: LDR grading). LightSeamTests holds them equal to the assets.</summary>
        public static Finish Office => new Finish(0.1f, 12f, true, false);
    }

    /// <summary>
    /// The luma (0-255) the day mat shows under a light of relative
    /// <paramref name="luminance"/> after <paramref name="finish"/> (URP's
    /// UberPost and LUT maths for a grey: the exposure, then the tonemap and
    /// the contrast in the grading mode's order). With <see cref="Finish.None"/>
    /// it is <see cref="LumaAt"/>.
    /// </summary>
    public static float GradedLuma(float luminance, in Finish finish)
    {
        double x = SrgbToLinear(DayMatLuma / 255f) * (luminance > 0f ? luminance : 0f) * Math.Pow(2.0, finish.PostExposure);
        if (finish.HdrGrading)
        {
            x = ContrastLogC(x, finish.Contrast);
            if (finish.Neutral) x = NeutralTonemap(x);
        }
        else
        {
            if (finish.Neutral) x = NeutralTonemap(x);
            x = ContrastLogC(Math.Min(1.0, Math.Max(0.0, x)), finish.Contrast);
        }
        return 255f * LinearToSrgb((float)Math.Min(1.0, Math.Max(0.0, x)));
    }

    /// <summary>
    /// URP's vignette multiplier at a screen point <paramref name="dx"/>,
    /// <paramref name="dy"/> from its centre (uv units, -0.5..0.5) for the
    /// profile's <paramref name="intensity"/> and <paramref name="smoothness"/>
    /// (not rounded): (1 - |d·3·intensity|²)^(5·smoothness); 1 at the centre.
    /// </summary>
    public static float VignetteAt(float dx, float dy, float intensity, float smoothness)
    {
        float x = dx * intensity * 3f, y = dy * intensity * 3f;
        return (float)Math.Pow(Clamp01(1f - (x * x + y * y)), Math.Max(0f, smoothness) * 5f);
    }

    /// <summary>The pool's centre and the mat's corners after <paramref name="finish"/> (luma 0-255) under <paramref name="l"/>.</summary>
    public static void MatLumas(in Light l, float radius, float matWidth, float matDepth, in Finish finish, out float centre, out float corners)
    {
        centre = GradedLuma(LuminanceAt(l, 1f), finish);
        corners = GradedLuma(DarkestOnMat(l, radius, matWidth, matDepth), finish);
    }

    /// <summary>
    /// The readable rule on the image shown (FIX_PLAN LIGHT: mat luma ≥ 55 at
    /// every hour, ≈ 60 at night), at full night with the lamp on: after the
    /// finish the mat's corners clear 55 by <see cref="MarginLuma"/>, the
    /// pool's centre reaches 60, and the pool reads (centre to corners at
    /// least <see cref="PoolContrastLuma"/>).
    /// </summary>
    public static bool ReadableAfterFinish(in Knobs k, float radius, float matWidth, float matDepth, in Finish finish, out float centre, out float corners)
    {
        MatLumas(FullNight(k), radius, matWidth, matDepth, finish, out centre, out corners);
        return corners >= ReadableMatLuma + MarginLuma && centre >= TargetMatLuma && centre - corners >= PoolContrastLuma;
    }

    /// <summary>The desk light's knobs (HallLightingSO's "The desk" header), repaired on use.</summary>
    public readonly struct Knobs
    {
        /// <summary>The room's light on the desk at night as a share of the full day's (0..1), without the lamp.</summary>
        public readonly float NightFloor;

        /// <summary>The room's colour on the desk at night (by day it is white).</summary>
        public readonly Rgba NightTint;

        /// <summary>What the lamp adds to the level, warm and full, at the mat's centre (at most what the room has lost: the level never passes 1).</summary>
        public readonly float LampLevel;

        /// <summary>The lamp's colour (a warm filament).</summary>
        public readonly Rgba LampTint;

        /// <summary>The hall's evening (0 by day, 1 at night) at which the lamp switches on (HallDayCycle.SwitchedOn).</summary>
        public readonly float LampOnAtEvening;

        /// <summary>Creates the knobs.</summary>
        public Knobs(float nightFloor, Rgba nightTint, float lampLevel, Rgba lampTint, float lampOnAtEvening)
        {
            NightFloor = nightFloor;
            NightTint = nightTint;
            LampLevel = lampLevel;
            LampTint = lampTint;
            LampOnAtEvening = lampOnAtEvening;
        }
    }

    /// <summary>The desk's light at one moment: its colour (R, G, B), its level, and the lamp's part of the level.</summary>
    public readonly struct Light
    {
        /// <summary>The light's colour, red (the room's and the lamp's, mixed by their parts).</summary>
        public readonly float R;

        /// <summary>The light's colour, green.</summary>
        public readonly float G;

        /// <summary>The light's colour, blue.</summary>
        public readonly float B;

        /// <summary>The level, 1 = the full day (the desk as drawn); the lamp's part included; never above 1.</summary>
        public readonly float Level;

        /// <summary>The lamp's part of <see cref="Level"/> (0 while it is off): the desk shaders let it fall off away from the pool.</summary>
        public readonly float LampShare;

        /// <summary>The lamp's colour, red (what falls off with its share away from the pool).</summary>
        public readonly float LampR;

        /// <summary>The lamp's colour, green.</summary>
        public readonly float LampG;

        /// <summary>The lamp's colour, blue.</summary>
        public readonly float LampB;

        /// <summary>Creates the light.</summary>
        public Light(float r, float g, float b, float level, float lampShare, float lampR, float lampG, float lampB)
        {
            R = r;
            G = g;
            B = b;
            Level = level;
            LampShare = lampShare;
            LampR = lampR;
            LampG = lampG;
            LampB = lampB;
        }
    }

    /// <summary>True when the lamp is switched on at <paramref name="hour"/>: the hall's evening has reached the lamp's knob.</summary>
    public static bool LampOn(float hour, in HallDayCycle.Settings cycle, in Knobs k) =>
        HallDayCycle.SwitchedOn(hour, Clamp01(k.LampOnAtEvening), 0, 0f, cycle);

    /// <summary>The hour the lamp switches on: the hall's evening reaches the lamp's knob (the sunset, 16:30, with the shipped knob: a quarter of an hour before the plan's afternoon dip, H3 16:45).</summary>
    public static float LampOnHour(in HallDayCycle.Settings cycle, in Knobs k) => HallDayCycle.HourAtEvening(Clamp01(k.LampOnAtEvening), cycle);

    /// <summary>
    /// The desk's light at <paramref name="hour"/> with the lamp at
    /// <paramref name="lampWarm"/> of its strength (0 off, 1 warm; the
    /// caller's HallDayCycle.WarmUp): the room's level eases from 1 by day to
    /// the night floor with the hall's daylight, its colour from white to the
    /// night tint, and the lamp's warm share fills what the room has lost.
    /// </summary>
    public static Light Evaluate(float hour, in HallDayCycle.Settings cycle, in Knobs k, float lampWarm) =>
        AtDaylight(HallDayCycle.Daylight(hour, cycle), k, lampWarm);

    /// <summary>
    /// The desk's light under the hall's <paramref name="daylight"/> (1 by
    /// day, 0 at night) with the lamp at <paramref name="lampWarm"/> of its
    /// strength. The lamp's share is capped at the room's deficit (1 less the
    /// room's level), so the level never passes the full day's 1 (BUG-14c,
    /// BUG-51: no paper white over the bloom's threshold, no traveller tint
    /// clamped while the photo's is not, no dusk brighter than the drawn desk).
    /// </summary>
    public static Light AtDaylight(float daylight, in Knobs k, float lampWarm)
    {
        daylight = Clamp01(daylight);
        float floor = Clamp01(k.NightFloor);
        float room = floor + (1f - floor) * daylight;
        float lamp = Math.Min(Math.Max(0f, k.LampLevel) * Clamp01(lampWarm), Math.Max(0f, 1f - room));
        float level = room + lamp;
        float lr = Clamp01(k.LampTint.R), lg = Clamp01(k.LampTint.G), lb = Clamp01(k.LampTint.B);
        float r = Mix(k.NightTint.R, daylight), g = Mix(k.NightTint.G, daylight), b = Mix(k.NightTint.B, daylight);
        if (level > 1e-4f)
        {
            r = (r * room + lr * lamp) / level;
            g = (g * room + lg * lamp) / level;
            b = (b * room + lb * lamp) / level;
        }
        return new Light(r, g, b, level, lamp, lr, lg, lb);
    }

    /// <summary>The room's colour channel: the night tint's at night, white by day.</summary>
    private static float Mix(float night, float daylight) => Clamp01(night) + (1f - Clamp01(night)) * daylight;

    private static float Clamp01(float x) => x < 0f ? 0f : x > 1f ? 1f : float.IsNaN(x) ? 0f : x;

    private static float Smooth(float from, float to, float x)
    {
        float t = Clamp01((x - from) / (to - from));
        return t * t * (3f - 2f * t);
    }

    /// <summary>URP's Neutral tonemap (Color.hlsl NeutralTonemap) on one channel.</summary>
    private static double NeutralTonemap(double x)
    {
        double whiteScale = 1.0 / NeutralCurve(5.3);
        return NeutralCurve(x * whiteScale) * whiteScale;
    }

    private static double NeutralCurve(double x)
    {
        const double a = 0.2, b = 0.29, c = 0.24, d = 0.272, e = 0.02, f = 0.3;
        return (x * (a * x + c * b) + d * e) / (x * (a * x + b) + d * f) - e / f;
    }

    /// <summary>URP's contrast (ColorGrade): in LogC around ACEScc's mid grey, scaled by 1 + contrast/100.</summary>
    private static double ContrastLogC(double x, float contrast)
    {
        const double midGrey = 0.4135884, ca = 5.555556, cb = 0.047996, cc = 0.244161, cd = 0.386036;
        double log = cc * Math.Log10(ca * Math.Max(0.0, x) + cb) + cd;
        log = (log - midGrey) * (1.0 + contrast / 100.0) + midGrey;
        return Math.Max(0.0, (Math.Pow(10.0, (log - cd) / cc) - cb) / ca);
    }

    private static float SrgbToLinear(float v) => v <= 0.04045f ? v / 12.92f : (float)Math.Pow((v + 0.055f) / 1.055f, 2.4f);

    private static float LinearToSrgb(float v) => v <= 0.0031308f ? v * 12.92f : 1.055f * (float)Math.Pow(v, 1f / 2.4f) - 0.055f;
}
