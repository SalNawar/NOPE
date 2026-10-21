using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>How the game's window shows (Settings, BUG-62). Stored by its number: append-only.</summary>
public enum WindowMode
{
    /// <summary>A window on the desktop.</summary>
    Windowed,

    /// <summary>A borderless window over the whole screen (FullScreenWindow).</summary>
    Borderless,

    /// <summary>Exclusive fullscreen.</summary>
    Fullscreen
}

/// <summary>The volumes Settings offers (BUG-62), each a slider. Stored by its number: append-only.</summary>
public enum SoundChannel
{
    /// <summary>Everything (the mixer's Master group).</summary>
    Master,

    /// <summary>The music bus.</summary>
    Music,

    /// <summary>The sound effects: the UI, the desk and the ambience buses.</summary>
    Effects
}

/// <summary>A screen size in pixels.</summary>
public readonly struct ScreenSize : IEquatable<ScreenSize>
{
    /// <summary>A size.</summary>
    public ScreenSize(int width, int height)
    {
        Width = width;
        Height = height;
    }

    /// <summary>Its width.</summary>
    public int Width { get; }

    /// <summary>Its height.</summary>
    public int Height { get; }

    /// <summary>Its pixel count.</summary>
    public long Pixels => (long)Width * Height;

    /// <summary>True for the same width and height.</summary>
    public bool Equals(ScreenSize other) => Width == other.Width && Height == other.Height;

    /// <summary>True for a ScreenSize of the same width and height.</summary>
    public override bool Equals(object obj) => obj is ScreenSize s && Equals(s);

    /// <summary>A hash of the width and height.</summary>
    public override int GetHashCode() => Width * 397 ^ Height;

    /// <summary>"1920x1080".</summary>
    public override string ToString() => Width + "x" + Height;
}

/// <summary>
/// The PC basics' rules (BUG-62, CODE-104; Settings: volume, window mode,
/// resolution, vsync, brightness and reduce flashing): a volume slider's
/// share in decibels for the mixer, the resolutions offered and stepping
/// through them, and brightness as an exposure. Pure, so the numbers are
/// tested headless (SettingsRulesTests).
/// </summary>
public static class SettingsRules
{
    /// <summary>The mixer's silence (dB): a volume of 0, and the floor of every other.</summary>
    public const float SilentDecibels = -80f;

    /// <summary>The smallest height a resolution is offered at (the game's layouts are checked from 720p up).</summary>
    public const int SmallestHeight = 720;

    /// <summary>The exposure (EV) the brightness slider reaches at either end: 0 is -1 EV, the middle 0, the top +1.</summary>
    public const float BrightnessRange = 1f;

    /// <summary>A volume slider's share (0 to 1) as the mixer's attenuation: 20·log10(share) dB, never under <see cref="SilentDecibels"/>, 0 dB at full.</summary>
    public static float Decibels(float share)
    {
        if (float.IsNaN(share) || share <= 0f)
            return SilentDecibels;
        return (float)Math.Max(SilentDecibels, Math.Min(0.0, 20.0 * Math.Log10(Math.Min(1f, share))));
    }

    /// <summary>
    /// The resolutions Settings offers: the screen's sizes at least
    /// <see cref="SmallestHeight"/> tall (refresh rates merged), smallest
    /// first, with <paramref name="current"/> always among them.
    /// </summary>
    public static List<ScreenSize> Resolutions(IEnumerable<ScreenSize> offered, ScreenSize current)
    {
        var sizes = new HashSet<ScreenSize>((offered ?? Array.Empty<ScreenSize>()).Where(s => s.Width > 0 && s.Height >= SmallestHeight));
        if (current.Width > 0 && current.Height > 0)
            sizes.Add(current);
        return sizes.OrderBy(s => s.Pixels).ThenBy(s => s.Width).ToList();
    }

    /// <summary>The index of <paramref name="size"/> in <paramref name="sizes"/>, else of the nearest by pixel count (0 for an empty list).</summary>
    public static int IndexOf(IReadOnlyList<ScreenSize> sizes, ScreenSize size)
    {
        if (sizes == null || sizes.Count == 0)
            return 0;
        int best = 0;
        for (int i = 0; i < sizes.Count; i++)
        {
            if (sizes[i].Equals(size))
                return i;
            if (Math.Abs(sizes[i].Pixels - size.Pixels) < Math.Abs(sizes[best].Pixels - size.Pixels))
                best = i;
        }
        return best;
    }

    /// <summary>The index <paramref name="step"/> places on from <paramref name="index"/> in a list of <paramref name="count"/>, held at its ends.</summary>
    public static int Step(int index, int step, int count) => count <= 0 ? 0 : Math.Max(0, Math.Min(count - 1, index + step));

    /// <summary>The brightness slider's share (0 to 1; the middle is the game as painted) as an exposure in EV, from -<see cref="BrightnessRange"/> to +<see cref="BrightnessRange"/>.</summary>
    public static float Exposure(float brightness)
    {
        float share = float.IsNaN(brightness) ? 0.5f : Math.Max(0f, Math.Min(1f, brightness));
        return (share - 0.5f) * 2f * BrightnessRange;
    }

    /// <summary>A share (0 to 1) as the whole percent a slider and its label show.</summary>
    public static int Percent(float share) => (int)Math.Round(Math.Max(0f, Math.Min(1f, float.IsNaN(share) ? 0f : share)) * 100f, MidpointRounding.AwayFromZero);
}
