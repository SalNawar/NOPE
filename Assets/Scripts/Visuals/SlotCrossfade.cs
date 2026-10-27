/// <summary>
/// The hall slots' crossfade (HALL-FX, BUG-71: fading both faces with
/// Fade and 1 - Fade let about a quarter of the painting show through at
/// mid-fade): over the first half the incoming face fades in while the
/// outgoing one stays opaque; over the second half the outgoing face fades
/// out while the incoming one stays opaque. Wherever both faces cover, one
/// of them is opaque at every moment, so the painting never shows through,
/// whichever of the two draws on top (with the incoming on top the change
/// happens in the first half, with the outgoing on top in the second); where
/// only one covers, it fades alone. The pure part of HallSlotsLink, tested
/// headless (SlotCrossfadeTests).
/// </summary>
public static class SlotCrossfade
{
    /// <summary>The incoming face's opacity at <paramref name="fade"/> (0 start to 1 done).</summary>
    public static float Incoming(float fade) => Clamp01(2f * fade);

    /// <summary>The outgoing face's opacity at <paramref name="fade"/> (0 start to 1 done).</summary>
    public static float Outgoing(float fade) => Clamp01(2f - 2f * fade);

    private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : float.IsNaN(v) ? 1f : v;
}
