using System;

/// <summary>
/// The living city's rules behind the hall windows and the city view (HALL-FX,
/// BUG-13: "no window, light or sprite in the hall or city ever toggles"), the
/// pure part of HallCityExterior and the NOPE/Hall Living City and Hall City
/// Traffic shaders (which mirror <see cref="WindowHash"/> and
/// <see cref="WindowLevel"/>):
/// <list type="bullet">
/// <item>the windows switch on one by one as the hall darkens: each window
/// cell's moment along the lit share (the global _HallCityLights.x, which the
/// hall's lights compute from the shift clock's hour, never from real time)
/// is its hash, which is never 0, so no cell is on before the first light;
/// once its moment passes, the window fades in once over a narrow band of the
/// lit share (<see cref="NextFadeWidth"/> sizes the band so the fade takes
/// about <see cref="FadeSeconds"/> at the clock's pace), and a frozen clock
/// freezes every window;</item>
/// <item>the flying traffic fades in and out at its lane's ends instead of
/// popping (<see cref="LaneEndFade"/>) and takes the hour's light
/// (<see cref="HourLight"/>, the light the city's painting is lit by).</item>
/// </list>
/// No UnityEngine, so every rule is tested headless (HallCityTests).
/// </summary>
public static class HallCity
{
    /// <summary>The least hash a cell gets (the shader's own floor): no window waits on a lit share of 0, so none shows before the first light.</summary>
    public const float HashFloor = 1f / 256f;

    /// <summary>The share of the lit range the hashes spread over (the last cell lights at 0.97, just before full night).</summary>
    public const float Spread = 0.97f;

    /// <summary>How long one window's fade-in takes at the clock's pace (Saleh's direction via FIX_PLAN: a single 0.3-0.5 s fade).</summary>
    public const float FadeSeconds = 0.4f;

    /// <summary>The narrowest and widest fade band (shares of the lit range): never a hard step, never a slow smear after a jump of the hour.</summary>
    public const float MinFadeWidth = 0.0005f, MaxFadeWidth = 0.05f;

    /// <summary>How long the fade band takes to follow a change of the clock's pace (seconds): a changed pace never pops a window.</summary>
    public const float WidthFollowSeconds = 1f;

    /// <summary>
    /// The window cell (<paramref name="cx"/>, <paramref name="cy"/>)'s
    /// moment, in [<see cref="HashFloor"/>, 1): an integer hash of the cell
    /// (a Wang hash, the shader's own, bit for bit), its low 24 bits as a
    /// share, lifted above the floor. The old float hash (frac of a product
    /// of fracs) was exactly 0 on 600 of the grid's 28,800 cells (BUG-13: they
    /// blinked at 6 Hz all day long) and gave hundreds of cells the same
    /// moment, so they lit together.
    /// </summary>
    public static float WindowHash(int cx, int cy)
    {
        unchecked
        {
            uint h = ((uint)cx * 73856093u) ^ ((uint)cy * 19349663u);
            h = (h ^ 61u) ^ (h >> 16);
            h *= 9u;
            h ^= h >> 4;
            h *= 0x27d4eb2du;
            h ^= h >> 15;
            return HashFloor + (1f - HashFloor) * ((h & 0xFFFFFFu) / 16777216f);
        }
    }

    /// <summary>
    /// A window's light (0 off to 1 lit) at the lit share <paramref name="lit"/>
    /// (0 by day to 1 when every window is lit) for its <paramref name="hash"/>,
    /// over a fade band <paramref name="fadeWidth"/> wide: off at a share of 0,
    /// then rising once from its moment (hash x <see cref="Spread"/>) to full
    /// across the band. A function of the share alone: no real time, so it
    /// never blinks, and holds still while the clock does.
    /// </summary>
    public static float WindowLevel(float lit, float hash, float fadeWidth)
    {
        if (!(lit > 0f))
            return 0f;
        float width = Math.Max(fadeWidth, 1e-5f);
        return Clamp01((lit - hash * Spread) / width);
    }

    /// <summary>
    /// The fade band for the next frame: the band the lit share crosses in
    /// <see cref="FadeSeconds"/> at its pace this frame (it moved from
    /// <paramref name="previousLit"/> to <paramref name="lit"/> in
    /// <paramref name="dt"/> seconds), clamped to the band's limits and
    /// followed smoothly from <paramref name="width"/>; while the share holds
    /// still (a frozen clock, a forced hour) the band holds too, so nothing on
    /// screen changes.
    /// </summary>
    public static float NextFadeWidth(float width, float previousLit, float lit, float dt)
    {
        float current = width > 0f ? Clamp(width, MinFadeWidth, MaxFadeWidth) : MinFadeWidth;
        float moved = Math.Abs(lit - previousLit);
        if (!(dt > 0f) || !(moved > 0f))
            return current;
        float target = Clamp(moved / dt * FadeSeconds, MinFadeWidth, MaxFadeWidth);
        float follow = 1f - (float)Math.Exp(-dt / WidthFollowSeconds);
        return current + (target - current) * follow;
    }

    /// <summary>A flyer's opacity at <paramref name="travel"/> (0 to 1 of its lane): it fades in over the first <paramref name="edge"/> of the lane and out over the last, so a lane's wrap never pops it.</summary>
    public static float LaneEndFade(float travel, float edge)
    {
        float t = travel - (float)Math.Floor(travel);
        if (!(edge > 0f))
            return 1f;
        return SmoothStep(0f, edge, t) * (1f - SmoothStep(1f - edge, 1f, t));
    }

    /// <summary>The hour's light on the city (and its traffic), from the painting's four state weights (HallBakedCycle.Weights): morning, noon, evening and night lights blended by weight (the living city shader's light), as red, green, blue.</summary>
    public static (float r, float g, float b) HourLight(HallBakedCycle.Weights4 w)
    {
        float total = w.x + w.y + w.z + w.w;
        if (!(total > 0f))
            return (1f, 1f, 1f);
        return ((0.95f * w.x + 1f * w.y + 0.75f * w.z + 0.22f * w.w) / total,
                (0.97f * w.x + 1f * w.y + 0.55f * w.z + 0.29f * w.w) / total,
                (1f * w.x + 1f * w.y + 0.55f * w.z + 0.43f * w.w) / total);
    }

    private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;

    private static float Clamp(float v, float lo, float hi) => v < lo ? lo : v > hi ? hi : v;

    private static float SmoothStep(float a, float b, float x)
    {
        float t = Clamp01((x - a) / (b - a));
        return t * t * (3f - 2f * t);
    }
}
