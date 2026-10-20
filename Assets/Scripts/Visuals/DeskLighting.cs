using System;

/// <summary>
/// The desk's one light (BUG-14: the desk went dark at dusk with no source
/// changing, and papers, props, rack, traveller and photo each followed their
/// own rule). From the hour on the shared day cycle (HallDayCycle) it gives
/// one level and one colour that every desk shader multiplies by (DeskLight.hlsl,
/// published by DeskLightLevel): the room's light falling from the full day
/// (level 1, the desk as drawn) to its night floor (a cool tint), plus the
/// work lamp's warm share once the lamp is on. The lamp switches on at the
/// afternoon dip (<see cref="LampOnHour"/>: the hour the hall's evening reaches
/// the lamp's knob) and its share counts only while it warms up and burns,
/// so the switch-on reads as a warm pool coming up on the mat. Levels are
/// relative to the full day: 1 draws the desk as by day, so nothing changes
/// at noon. Pure and allocation-free (read every frame).
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
    /// The light (its luminance, 1 = the full day) that keeps the mat at
    /// <see cref="ReadableMatLuma"/>: the sRGB-decoded ratio of 55 to the
    /// day's 76, about 0.529. Every point of the mat must reach it at night,
    /// the corners (the farthest from the lamp's pool) included.
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
    /// maths: the light's colour times its level less the lamp's share
    /// outside the pool).
    /// </summary>
    public static float LuminanceAt(in Light l, float poolWeight) =>
        Luminance(l.R, l.G, l.B) * (l.Level - l.LampShare * (1f - Clamp01(poolWeight)));

    /// <summary>
    /// The darkest point of a <paramref name="matWidth"/> × <paramref name="matDepth"/>
    /// metre mat centred under the pool (its corners): the luminance there
    /// under <paramref name="l"/> with a pool of <paramref name="radius"/>.
    /// </summary>
    public static float DarkestOnMat(in Light l, float radius, float matWidth, float matDepth) =>
        LuminanceAt(l, PoolWeight(0.5f * (float)Math.Sqrt(matWidth * matWidth + matDepth * matDepth), radius));

    /// <summary>The light at full night with the lamp full on (the readable rule's worst hour).</summary>
    public static Light FullNight(in Knobs k) => AtDaylight(0f, k, 1f);

    /// <summary>The desk light's knobs (HallLightingSO's "The desk" header), repaired on use.</summary>
    public readonly struct Knobs
    {
        /// <summary>The room's light on the desk at night as a share of the full day's (0..1), without the lamp.</summary>
        public readonly float NightFloor;

        /// <summary>The room's colour on the desk at night (by day it is white).</summary>
        public readonly Rgba NightTint;

        /// <summary>What the lamp adds to the level, warm and full, at the mat's centre.</summary>
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

        /// <summary>The level, 1 = the full day (the desk as drawn); the lamp's part included.</summary>
        public readonly float Level;

        /// <summary>The lamp's part of <see cref="Level"/> (0 while it is off): the desk shaders let it fall off away from the pool.</summary>
        public readonly float LampShare;

        /// <summary>Creates the light.</summary>
        public Light(float r, float g, float b, float level, float lampShare)
        {
            R = r;
            G = g;
            B = b;
            Level = level;
            LampShare = lampShare;
        }
    }

    /// <summary>True when the lamp is switched on at <paramref name="hour"/>: the hall's evening has reached the lamp's knob.</summary>
    public static bool LampOn(float hour, in HallDayCycle.Settings cycle, in Knobs k) =>
        HallDayCycle.SwitchedOn(hour, Clamp01(k.LampOnAtEvening), 0, 0f, cycle);

    /// <summary>The afternoon dip: the hour the lamp switches on (the hall's evening reaches the lamp's knob).</summary>
    public static float LampOnHour(in HallDayCycle.Settings cycle, in Knobs k) => HallDayCycle.HourAtEvening(Clamp01(k.LampOnAtEvening), cycle);

    /// <summary>
    /// The desk's light at <paramref name="hour"/> with the lamp at
    /// <paramref name="lampWarm"/> of its strength (0 off, 1 warm; the
    /// caller's HallDayCycle.WarmUp): the room's level eases from 1 by day to
    /// the night floor with the hall's daylight, its colour from white to the
    /// night tint, and the lamp's warm share is added on top.
    /// </summary>
    public static Light Evaluate(float hour, in HallDayCycle.Settings cycle, in Knobs k, float lampWarm) =>
        AtDaylight(HallDayCycle.Daylight(hour, cycle), k, lampWarm);

    /// <summary>The desk's light under the hall's <paramref name="daylight"/> (1 by day, 0 at night) with the lamp at <paramref name="lampWarm"/> of its strength.</summary>
    public static Light AtDaylight(float daylight, in Knobs k, float lampWarm)
    {
        daylight = Clamp01(daylight);
        float floor = Clamp01(k.NightFloor);
        float room = floor + (1f - floor) * daylight;
        float lamp = Math.Max(0f, k.LampLevel) * Clamp01(lampWarm);
        float level = room + lamp;
        float r = Mix(k.NightTint.R, daylight), g = Mix(k.NightTint.G, daylight), b = Mix(k.NightTint.B, daylight);
        if (level > 1e-4f)
        {
            r = (r * room + Clamp01(k.LampTint.R) * lamp) / level;
            g = (g * room + Clamp01(k.LampTint.G) * lamp) / level;
            b = (b * room + Clamp01(k.LampTint.B) * lamp) / level;
        }
        return new Light(r, g, b, level, lamp);
    }

    /// <summary>The room's colour channel: the night tint's at night, white by day.</summary>
    private static float Mix(float night, float daylight) => Clamp01(night) + (1f - Clamp01(night)) * daylight;

    private static float Clamp01(float x) => x < 0f ? 0f : x > 1f ? 1f : float.IsNaN(x) ? 0f : x;

    private static float Smooth(float from, float to, float x)
    {
        float t = Clamp01((x - from) / (to - from));
        return t * t * (3f - 2f * t);
    }

    private static float SrgbToLinear(float v) => v <= 0.04045f ? v / 12.92f : (float)Math.Pow((v + 0.055f) / 1.055f, 2.4f);

    private static float LinearToSrgb(float v) => v <= 0.0031308f ? v * 12.92f : 1.055f * (float)Math.Pow(v, 1f / 2.4f) - 0.055f;
}
