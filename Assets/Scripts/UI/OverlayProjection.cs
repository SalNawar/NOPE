using UnityEngine;

/// <summary>
/// Places an element of a screen-space overlay canvas over a world point: the
/// point through the given camera to the screen, then into the canvas, plus an
/// offset (canvas reference px), clamped inside the canvas. The target needs
/// anchors and pivot (0.5, 0.5) under a full-screen parent (the builder sets
/// both). Used by the traveller wheel and the overlay callouts, which resolve
/// their canvas once (CanvasRectOf) and get the office camera from the office
/// binder, so the per-frame placement looks nothing up. A point outside the
/// view is refused, or, for a caller that keeps on screen (the speech bubble
/// and the wheel's ring, whose traveller's head the desk view tilts above the
/// top), placed at the screen's edge.
/// </summary>
public static class OverlayProjection
{
    /// <summary>The rect of the root canvas above <paramref name="host"/> (resolved once, at a caller's Awake), or null when it has none.</summary>
    public static RectTransform CanvasRectOf(Component host)
    {
        Canvas canvas = host != null ? host.GetComponentInParent<Canvas>() : null;
        return canvas != null ? (RectTransform)canvas.rootCanvas.transform : null;
    }

    /// <summary>
    /// Places <paramref name="target"/> inside <paramref name="canvasRect"/>
    /// (its root canvas's rect); false (the target unmoved) when a reference is
    /// missing, the point is behind the camera, or it is outside the viewport
    /// and <paramref name="keepOnScreen"/> is false (with it true, the point is
    /// taken at the viewport's nearest edge).
    /// </summary>
    public static bool TryPlace(RectTransform target, RectTransform canvasRect, Camera camera, Vector3 world, Vector2 offset, bool keepOnScreen = false)
    {
        if (target == null || canvasRect == null || camera == null)
            return false;

        Vector3 viewport = camera.WorldToViewportPoint(world);
        if (viewport.z < 0f)
            return false;
        if (keepOnScreen)
        {
            viewport.x = Mathf.Clamp01(viewport.x);
            viewport.y = Mathf.Clamp01(viewport.y);
        }
        else if (viewport.x < 0f || viewport.x > 1f || viewport.y < 0f || viewport.y > 1f)
        {
            return false;
        }

        Vector2 screen = camera.ViewportToScreenPoint(viewport);
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out Vector2 local))
            return false;

        Vector2 at = local + offset;
        Rect bounds = canvasRect.rect;
        Vector2 half = target.rect.size * 0.5f;
        at.x += RectClamp.Shift(at.x - half.x, at.x + half.x, bounds.xMin, bounds.xMax, false);
        at.y += RectClamp.Shift(at.y - half.y, at.y + half.y, bounds.yMin, bounds.yMax, false);
        target.anchoredPosition = at;
        return true;
    }
}
