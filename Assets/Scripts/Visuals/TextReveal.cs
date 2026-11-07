/// <summary>
/// How much of a line shows while it reveals (the speech bubble, D26): the
/// view lays the whole line out first (its box at its final size, its text at
/// its final rect) and then shows its first characters in place (TMP
/// maxVisibleCharacters), so nothing in the bubble moves while it reveals.
/// The count is the speed times the time, in whole characters, clamped to the
/// line; Reduced Motion shows the whole line at once.
/// </summary>
public static class TextReveal
{
    /// <summary>
    /// The characters shown <paramref name="elapsed"/> seconds into a line of
    /// <paramref name="length"/> characters revealed at
    /// <paramref name="charsPerSecond"/>: the speed times the time, rounded
    /// down, clamped to 0..length. The whole line when
    /// <paramref name="atOnce"/> (Reduced Motion) or the speed is 0 or less;
    /// none before the line starts (a negative or NaN time).
    /// </summary>
    public static int Count(float charsPerSecond, float elapsed, int length, bool atOnce = false)
    {
        if (length <= 0)
            return 0;
        if (atOnce || !(charsPerSecond > 0f))
            return length;
        if (!(elapsed > 0f))
            return 0;
        float n = elapsed * charsPerSecond;
        return n >= length ? length : (int)n;
    }

    /// <summary>Seconds a line of <paramref name="length"/> characters takes to reveal at <paramref name="charsPerSecond"/>: 0 when <paramref name="atOnce"/>, the speed is 0 or less, or the line is empty.</summary>
    public static float Seconds(float charsPerSecond, int length, bool atOnce = false) =>
        atOnce || !(charsPerSecond > 0f) || length <= 0 ? 0f : length / charsPerSecond;
}
