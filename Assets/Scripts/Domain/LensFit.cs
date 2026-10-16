using System;

/// <summary>
/// The office camera's lens at the screen's aspect (leftover 2026-10-08: at
/// 5:4 the office view cut off the PC and the phone). A field of view is
/// vertical, so a screen narrower than the art's 16:9 sees less of the desk's
/// width; there the lens widens toward the 16:9 width. The art is painted to
/// the 16:9 frame's top edge (the hall above it is bare), so the view keeps
/// that top edge and tilts down by as much as it widens, showing more of the
/// desk below; it widens no further than the desk reaches below the camera
/// (<c>bottom</c>: past it lies the floor under the desk). At 16:9 and wider
/// (21:9) the lens and the pose are the art's. Pure.
/// </summary>
public static class LensFit
{
    /// <summary>The aspect the art office is framed for (16:9).</summary>
    public const float DesignAspect = 16f / 9f;

    /// <summary>
    /// The vertical field of view (degrees) at <paramref name="aspect"/> and
    /// how far (degrees) the view tilts down from the art's: as wide as
    /// <paramref name="fov"/> shows at <paramref name="designAspect"/>, its
    /// top edge where the art's is, at most down to <paramref name="bottom"/>
    /// degrees below the art's forward; <paramref name="fov"/> and no tilt at
    /// that aspect or wider (or for no aspect).
    /// </summary>
    public static (float fov, float tilt) Fit(float fov, float designAspect, float aspect, float bottom)
    {
        float half = fov / 2f;
        if (aspect <= 0f || designAspect <= 0f || aspect >= designAspect)
            return (fov, 0f);
        float wide = (float)(Math.Atan(Math.Tan(half * Math.PI / 180.0) * designAspect / aspect) * 180.0 / Math.PI);
        float fitted = Math.Max(half, Math.Min(wide, (half + bottom) / 2f));
        return (fitted * 2f, fitted - half);
    }
}
