using System;

/// <summary>
/// The daters' ink (the desk machine spec §1, Saleh 2026-10-07: retro
/// self-inking daters): the pad empties over the shift, so each print is a
/// little lighter than the last (DeskConfigSO's fade per print), but it
/// never runs out (the floor: a look, not a resource; Decision in the spec);
/// re-inking (a click on the dater's side button) or the morning's fresh pad
/// starts it full again. Each print's density also varies a little around
/// the pad's (the spread), and Hash01 gives the impression's noise (its edge
/// breaks), both values of the print, never a draw. Pure.
/// </summary>
public static class DaterInk
{
    /// <summary>The pad's density after <paramref name="printsSinceInking"/> prints: 1 less <paramref name="fadePerPrint"/> a print (a negative fade fades nothing), never below <paramref name="floor"/> (clamped to 0..1).</summary>
    public static float Density(int printsSinceInking, float fadePerPrint, float floor)
    {
        float least = Clamp01(floor);
        float fade = MathF.Max(0f, fadePerPrint) * MathF.Max(0, printsSinceInking);
        return MathF.Max(least, Clamp01(1f - fade));
    }

    /// <summary>The density of print number <paramref name="print"/> since inking: the pad's (Density) moved by up to ±<paramref name="spread"/> by the print's own value (Hash01 of it and <paramref name="seed"/>), clamped to 0..1. The same print always prints the same.</summary>
    public static float Print(int print, int seed, float fadePerPrint, float floor, float spread) =>
        Clamp01(Density(print, fadePerPrint, floor) + (Hash01(seed, print, 0x5EED) * 2f - 1f) * MathF.Max(0f, spread));

    /// <summary>A value in 0..1 for the three integers (a mixing hash): the same inputs always give the same value; neighbours give unrelated ones (the impression's noise).</summary>
    public static float Hash01(int a, int b, int c)
    {
        unchecked
        {
            uint h = (uint)a * 0x8DA6B343u ^ (uint)b * 0xD8163841u ^ (uint)c * 0xCB1AB31Fu;
            h ^= h >> 16;
            h *= 0x7FEB352Du;
            h ^= h >> 15;
            h *= 0x846CA68Bu;
            h ^= h >> 16;
            return (h & 0xFFFFFF) / (float)0xFFFFFF;
        }
    }

    private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
}
