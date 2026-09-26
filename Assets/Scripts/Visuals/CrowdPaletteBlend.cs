/// <summary>
/// How far the art office's hall crowds have gone from their morning colours (0)
/// to their evening colours (1), from the shift clock's progress (0 at opening,
/// 1 at closing). OfficeHallCrowdPalette tints the crowds with it.
/// </summary>
public static class CrowdPaletteBlend
{
    /// <summary>
    /// 0 up to <paramref name="eveningStartsAt"/>, 1 from <paramref name="eveningFullAt"/>,
    /// eased between (smoothstep); all three are shift progress, 0..1. A full point at
    /// or before the start counts as the start plus 0.001 (an almost instant switch);
    /// a progress that is not a number counts as morning.
    /// </summary>
    public static float Evening(float shiftProgress01, float eveningStartsAt, float eveningFullAt)
    {
        float end = eveningFullAt > eveningStartsAt + 0.001f ? eveningFullAt : eveningStartsAt + 0.001f;
        float t = (shiftProgress01 - eveningStartsAt) / (end - eveningStartsAt);
        if (!(t > 0f))
            return 0f;
        if (t >= 1f)
            return 1f;
        return t * t * (3f - 2f * t);
    }
}
