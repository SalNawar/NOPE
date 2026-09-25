/// <summary>Point-in-shape tests for generated placeholder art (cursors, character layers). Pure, so they are tested headless.</summary>
public static class PixelShapes
{
    /// <summary>Even-odd point-in-polygon test (the polygon's points in order, any winding).</summary>
    public static bool InPolygon((float x, float y)[] polygon, float px, float py)
    {
        bool inside = false;
        for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
        {
            if ((polygon[i].y > py) != (polygon[j].y > py) &&
                px < (polygon[j].x - polygon[i].x) * (py - polygon[i].y) / (polygon[j].y - polygon[i].y) + polygon[i].x)
                inside = !inside;
        }
        return inside;
    }

    /// <summary>True when (px, py) lies inside or on the axis-aligned ellipse centred at (cx, cy) with radii rx and ry (both positive).</summary>
    public static bool InEllipse(float cx, float cy, float rx, float ry, float px, float py)
    {
        if (rx <= 0f || ry <= 0f)
            return false;

        float dx = (px - cx) / rx;
        float dy = (py - cy) / ry;
        return dx * dx + dy * dy <= 1f;
    }
}
