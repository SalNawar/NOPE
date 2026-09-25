using System;

/// <summary>How close the monitor camera frames the CRT's glass (the ReStory-style push-in).</summary>
public static class MonitorFraming
{
    /// <summary>
    /// The orthographic half-height at which a glass of <paramref name="width"/>
    /// x <paramref name="height"/> world units fills <paramref name="fill"/> of the
    /// view's height, or of its width when the screen is narrower than the
    /// glass: max(height, width / aspect) / (2 * fill). The fill is clamped to
    /// [0.05, 1]; an aspect of 0 or less counts as 1.
    /// </summary>
    public static float OrthoSize(float width, float height, float aspect, float fill)
    {
        float f = Math.Min(Math.Max(fill, 0.05f), 1f);
        float a = aspect > 0f ? aspect : 1f;
        return Math.Max(height, width / a) / (2f * f);
    }
}
