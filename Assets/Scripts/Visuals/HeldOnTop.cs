using System;

/// <summary>
/// A held thing drawn above everything on the desk (DOC-HANDLE, BUG-8;
/// Papers, Please: the paper in your hand is always on top) without a change
/// on screen: the held thing is moved toward the camera along the camera's
/// own rays and shrunk by the same ratio (a homothety centred on the camera's
/// eye), which leaves its picture exactly as it was (same place, same size)
/// while putting it physically in front of every prop. A ray from the eye
/// down to a desk point is at height (1 - s) times the eye's height where it
/// has gone s of the way, and a prop of height H meets that ray only where
/// the ray is lower than H, so a thing at s &lt; 1 - H / eyeHeight is in
/// front of every prop up to H. Let go, the ratio is 1 again: no change on
/// screen either. Pure, so it is tested headless; DeskDocument and the
/// rulebook folder apply it.
/// </summary>
public static class HeldOnTop
{
    /// <summary>
    /// The ratio (0 to 1: 1 leaves it where it is) for a held thing under an
    /// eye <paramref name="eyeHeight"/> metres over the desk to be in front of
    /// every prop up to <paramref name="clearHeight"/> metres, never under
    /// <paramref name="least"/> (the near plane's room); 1 when the eye is no
    /// higher than the props (nothing helps) or nothing needs clearing.
    /// </summary>
    public static float Ratio(float eyeHeight, float clearHeight, float least)
    {
        if (clearHeight <= 0f || eyeHeight <= clearHeight)
            return 1f;
        float s = 1f - clearHeight / eyeHeight;
        float floor = Math.Min(Math.Max(least, 0.01f), 1f);
        return Math.Min(1f, Math.Max(floor, s));
    }

    /// <summary>Where point (<paramref name="x"/>, <paramref name="y"/>, <paramref name="z"/>) goes under the ratio <paramref name="s"/> toward the eye at (<paramref name="ex"/>, <paramref name="ey"/>, <paramref name="ez"/>): on the same ray from the eye, <paramref name="s"/> of the way out.</summary>
    public static (float x, float y, float z) Toward(float x, float y, float z, float ex, float ey, float ez, float s) =>
        (ex + (x - ex) * s, ey + (y - ey) * s, ez + (z - ez) * s);
}
