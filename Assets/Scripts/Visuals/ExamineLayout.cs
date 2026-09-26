using System;

/// <summary>
/// The knobs of papers held in the hand (piece 10), held by DeskConfigSO.examine.
/// Sizes and places are in screen heights (x from the screen's centre, y from
/// its bottom), so one layout serves every aspect: the vertical field of view
/// is fixed, and a wider screen only adds room at the sides.
/// </summary>
[Serializable]
public sealed class ExamineTuning
{
    /// <summary>A held paper's height in the office, in screen heights.</summary>
    public float officeHeight = 0.44f;

    /// <summary>Each office slot's centre from the screen's centre (left -, right +), in screen heights.</summary>
    public float officeCentreX = 0.2f;

    /// <summary>The office slots' bottom edge above the screen's bottom, in screen heights.</summary>
    public float officeBottom = 0.02f;

    /// <summary>How far held papers drop while the wheel is open, in screen heights.</summary>
    public float wheelDip = 0.2f;

    /// <summary>The largest paper height beside the open PC frame, in screen heights.</summary>
    public float frameHeight = 0.62f;

    /// <summary>Below this height a paper does not fit beside the frame and waits behind it, in screen heights.</summary>
    public float frameMinHeight = 0.3f;

    /// <summary>The least distance from the screen's and the region's edges, in screen heights.</summary>
    public float margin = 0.02f;

    /// <summary>The gap between two papers, in screen heights.</summary>
    public float gap = 0.03f;

    /// <summary>A held paper's distance from the camera, in metres (before SafeDistance).</summary>
    public float distance = 0.6f;

    /// <summary>The least distance, in near-clip planes.</summary>
    public float nearMargin = 1.5f;

    /// <summary>The greatest distance, as a share of the depth where the paper's lower corners' rays meet the desk.</summary>
    public float deskMargin = 0.8f;

    /// <summary>A held paper's roll in degrees (the left slot rolls by -roll, the right by +roll).</summary>
    public float roll;

    /// <summary>Seconds a paper takes to rise into the hand and to go back.</summary>
    public float seconds = 0.18f;
}

/// <summary>A paper's box on the screen, in screen heights: its centre's x from the screen's centre and y from its bottom, and its height (its width is the height times the paper's aspect).</summary>
public readonly struct ScreenBox
{
    /// <summary>A box at (<paramref name="centreX"/>, <paramref name="centreY"/>) of <paramref name="height"/>.</summary>
    public ScreenBox(float centreX, float centreY, float height)
    {
        CentreX = centreX;
        CentreY = centreY;
        Height = height;
    }

    /// <summary>The centre's x from the screen's centre (left -), in screen heights.</summary>
    public float CentreX { get; }

    /// <summary>The centre's y from the screen's bottom, in screen heights.</summary>
    public float CentreY { get; }

    /// <summary>The box's height, in screen heights.</summary>
    public float Height { get; }
}

/// <summary>
/// Where papers held in the hand sit (piece 10 X1, X2, X9, X10): the two
/// office slots (low, flanking the screen's centre line, below the
/// traveller's face), their dip under the open wheel, the region beside the
/// open PC frame, the pose in the camera's space, the distance that keeps the
/// paper out of the near clip and above the desk, and the easing of the rise.
/// Engine-free, so every number is tested headless; PaperExaminer applies it.
/// </summary>
public static class ExamineLayout
{
    /// <summary>
    /// An office slot's box: officeHeight tall, its centre officeCentreX left
    /// or right of the screen's centre, its bottom officeBottom up (wheelDip
    /// lower when <paramref name="dipped"/>); pulled in so that it stays
    /// margin inside the screen's side (|x| + w/2 &lt;= aspect/2 - margin).
    /// </summary>
    public static ScreenBox OfficeSlot(bool right, float paperAspect, float screenAspect, bool dipped, ExamineTuning t)
    {
        float h = t.officeHeight;
        float halfWidth = h * paperAspect / 2f;
        float x = t.officeCentreX;
        float limit = screenAspect / 2f - t.margin - halfWidth;
        if (x > limit)
            x = Math.Max(limit, 0f);
        float y = t.officeBottom + h / 2f - (dipped ? t.wheelDip : 0f);
        return new ScreenBox(right ? x : -x, y, h);
    }

    /// <summary>
    /// The <paramref name="index"/>-th of <paramref name="count"/> (1 or 2)
    /// held papers in the region between <paramref name="left"/> and
    /// <paramref name="right"/> (screen heights from the centre; the full
    /// screen height), margin inside it: one paper centred, two side by side
    /// or stacked, whichever is taller, capped at frameHeight. <paramref name="fits"/>
    /// is false below frameMinHeight (the papers then wait behind the frame).
    /// </summary>
    public static ScreenBox InRegion(int index, int count, float left, float right, float paperAspect, ExamineTuning t, out bool fits)
    {
        float width = right - left - 2f * t.margin;
        float height = 1f - 2f * t.margin;
        float centreX = (left + right) / 2f;
        const float centreY = 0.5f;

        if (count < 2)
        {
            float h = Math.Max(0f, Math.Min(t.frameHeight, Math.Min(height, width / paperAspect)));
            fits = h >= t.frameMinHeight;
            return new ScreenBox(centreX, centreY, h);
        }

        float side = Math.Max(0f, Math.Min(t.frameHeight, Math.Min(height, (width - t.gap) / (2f * paperAspect))));
        float stacked = Math.Max(0f, Math.Min(t.frameHeight, Math.Min((height - t.gap) / 2f, width / paperAspect)));
        int i = index < 1 ? 0 : 1;
        if (side >= stacked)
        {
            fits = side >= t.frameMinHeight;
            float w = side * paperAspect;
            float start = centreX - (2f * w + t.gap) / 2f;
            return new ScreenBox(start + w / 2f + i * (w + t.gap), centreY, side);
        }

        fits = stacked >= t.frameMinHeight;
        float step = (stacked + t.gap) / 2f;
        return new ScreenBox(centreX, i == 0 ? centreY + step : centreY - step, stacked);
    }

    /// <summary>
    /// A box's pose at depth <paramref name="distance"/> (X2): with the view's
    /// height there h_d = 2 d tan(fov/2), the camera-space centre
    /// (x h_d, (y - 1/2) h_d, d) and the paper's world height (box height x h_d);
    /// the paper faces the camera.
    /// </summary>
    public static (float x, float y, float z, float height) Pose(ScreenBox box, float verticalFovDegrees, float distance)
    {
        float hd = 2f * distance * (float)Math.Tan(verticalFovDegrees * Math.PI / 360.0);
        return (box.CentreX * hd, (box.CentreY - 0.5f) * hd, distance, box.Height * hd);
    }

    /// <summary>
    /// The distance to use (X2): the wanted distance, lowered to deskMargin x
    /// the depth at which the box's lower corners' rays meet the desk
    /// (DeskDepth; no limit when they never meet it), then raised to at least
    /// nearMargin x the near clip (a clipped paper shows nothing, so the near
    /// clip wins).
    /// </summary>
    public static float SafeDistance(ScreenBox box, float paperAspect, float verticalFovDegrees, float nearClip,
                                     float heightAboveDesk, float normalDotRight, float normalDotUp, float normalDotForward, ExamineTuning t)
    {
        float d = t.distance;
        float meet = DeskDepth(box, paperAspect, verticalFovDegrees, heightAboveDesk, normalDotRight, normalDotUp, normalDotForward);
        if (!float.IsPositiveInfinity(meet))
            d = Math.Min(d, t.deskMargin * meet);
        return Math.Max(d, t.nearMargin * nearClip);
    }

    /// <summary>
    /// The camera-space depth at which the rays through the box's lower corners
    /// first meet the desk plane: the camera sits <paramref name="heightAboveDesk"/>
    /// above it, and the plane's normal has these dots with the camera's right,
    /// up and forward. Positive infinity when neither ray goes down to it.
    /// </summary>
    public static float DeskDepth(ScreenBox box, float paperAspect, float verticalFovDegrees,
                                  float heightAboveDesk, float normalDotRight, float normalDotUp, float normalDotForward)
    {
        float k = 2f * (float)Math.Tan(verticalFovDegrees * Math.PI / 360.0);
        float halfWidth = box.Height * paperAspect / 2f;
        float bottom = box.CentreY - box.Height / 2f;
        float best = float.PositiveInfinity;
        foreach (float x in new[] { box.CentreX - halfWidth, box.CentreX + halfWidth })
        {
            // A point at depth z on the ray sits heightAboveDesk + z * rate above the plane.
            float rate = normalDotRight * x * k + normalDotUp * (bottom - 0.5f) * k + normalDotForward;
            if (rate >= 0f)
                continue;
            float z = -heightAboveDesk / rate;
            if (z > 0f && z < best)
                best = z;
        }
        return best;
    }

    /// <summary>Smoothstep over [0, 1] (clamped): the rise and the return.</summary>
    public static float Ease(float t)
    {
        t = t < 0f ? 0f : t > 1f ? 1f : t;
        return t * t * (3f - 2f * t);
    }
}
