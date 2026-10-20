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
    /// <summary>
    /// The lowest night level that keeps the desk readable (FIX_PLAN LIGHT:
    /// the mat about luma 55-60 at night against 76 by day; in linear light
    /// (55/76)^2.2 is about 0.49). The knobs' night (floor plus lamp) must reach it.
    /// </summary>
    public const float ReadableNightLevel = 0.49f;

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

    /// <summary>The level at full night with the lamp full on: the knobs' floor plus the lamp.</summary>
    public static float NightLevel(in Knobs k) => Clamp01(k.NightFloor) + Math.Max(0f, k.LampLevel);

    /// <summary>
    /// The desk's light at <paramref name="hour"/> with the lamp at
    /// <paramref name="lampWarm"/> of its strength (0 off, 1 warm; the
    /// caller's HallDayCycle.WarmUp): the room's level eases from 1 by day to
    /// the night floor with the hall's daylight, its colour from white to the
    /// night tint, and the lamp's warm share is added on top.
    /// </summary>
    public static Light Evaluate(float hour, in HallDayCycle.Settings cycle, in Knobs k, float lampWarm)
    {
        float daylight = HallDayCycle.Daylight(hour, cycle);
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
}
