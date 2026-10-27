/// <summary>
/// The hover outline's fade (the review of the desk v3 capture: the outline
/// popped on and off; Saleh's standing rule, nothing pops): the rim of the
/// object under the pointer fades in, and the rim of the one it left fades
/// out from where it stood, over the same short time each way
/// (InteractionFeedbackSO.outlineFadeSeconds, 70 ms: about four frames at
/// 60 fps), and is a cut under Reduced Motion or with no fade time.
/// HoverHighlighter draws it. Pure.
/// </summary>
public static class HoverFade
{
    /// <summary>
    /// How far a rim at <paramref name="shown"/> (0 gone, 1 full) has faded
    /// toward <paramref name="target"/> after <paramref name="dt"/> seconds of
    /// a fade lasting <paramref name="seconds"/> from end to end; straight to
    /// the target when motion is <paramref name="still"/> (Reduced Motion or
    /// Motion intensity 0) or the fade takes no time. Always within 0 to 1.
    /// </summary>
    public static float Step(float shown, float target, float dt, float seconds, bool still)
    {
        target = Clamp01(target);
        if (still || seconds <= 0f)
            return target;
        float step = (dt > 0f ? dt : 0f) / seconds;
        float next = shown < target ? shown + step : shown - step;
        return Clamp01(shown < target ? (next > target ? target : next) : (next < target ? target : next));
    }

    private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
}
