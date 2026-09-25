/// <summary>
/// Draws the traveller wheel's placeholder icons: a white glyph on transparent
/// per icon name (DialogChoiceKinds.IconName), shapes only, never letters: a
/// dot (wheel_normal), a left arrow (wheel_back), a sheet of paper with a
/// folded corner (wheel_request), a speech balloon (wheel_question), an eye
/// (wheel_look) and two balloons (wheel_dialog). RGBA32, row 0 = bottom (like
/// Texture2D raw data), a clear border all round. Pure, so it is tested
/// headless; TravellerWheel turns the bytes into a sprite when a name has no
/// final art, and never writes them to disk.
/// </summary>
public static class WheelIconPlaceholder
{
    /// <summary>The icon's side in pixels (a placeholder resolution, not a gameplay knob).</summary>
    public const int Size = 32;

    /// <summary>The icon for <paramref name="iconName"/>, or null for a name with no glyph.</summary>
    public static byte[] Render(string iconName)
    {
        if (!Knows(iconName))
            return null;

        var rgba = new byte[Size * Size * 4];
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                if (!In(iconName, x + 0.5f, y + 0.5f))
                    continue;

                int i = (y * Size + x) * 4;
                rgba[i] = rgba[i + 1] = rgba[i + 2] = rgba[i + 3] = 255;
            }
        }

        return rgba;
    }

    /// <summary>True for the names this class draws.</summary>
    private static bool Knows(string iconName)
    {
        switch (iconName)
        {
            case "wheel_normal":
            case "wheel_back":
            case "wheel_request":
            case "wheel_question":
            case "wheel_look":
            case "wheel_dialog":
                return true;
            default:
                return false;
        }
    }

    /// <summary>Whether a point (pixels, y up from the bottom) lies in the glyph.</summary>
    private static bool In(string iconName, float px, float py)
    {
        switch (iconName)
        {
            case "wheel_normal":
                return PixelShapes.InEllipse(16f, 16f, 6f, 6f, px, py);

            case "wheel_back":
                return PixelShapes.InPolygon(new (float x, float y)[] { (5f, 16f), (15f, 26f), (15f, 6f) }, px, py) ||
                       (px >= 14f && px <= 27f && py >= 13f && py <= 19f);

            case "wheel_request":
                return PixelShapes.InPolygon(new (float x, float y)[] { (8f, 4f), (24f, 4f), (24f, 21f), (17f, 28f), (8f, 28f) }, px, py);

            case "wheel_question":
                return PixelShapes.InEllipse(16f, 18f, 12f, 9f, px, py) ||
                       PixelShapes.InPolygon(new (float x, float y)[] { (9f, 12f), (15f, 10f), (6f, 4f) }, px, py);

            case "wheel_look":
            {
                // An almond (two overlapping circles) with a clear ring round the iris.
                bool almond = PixelShapes.InEllipse(16f, 24f, 15f, 15f, px, py) && PixelShapes.InEllipse(16f, 8f, 15f, 15f, px, py);
                float dx = px - 16f, dy = py - 16f;
                float d2 = dx * dx + dy * dy;
                return almond && !(d2 > 2.5f * 2.5f && d2 <= 5f * 5f);
            }

            default: // wheel_dialog
            {
                // Two balloons, the front one (lower right) parted from the back one by a clear gap.
                bool front = PixelShapes.InEllipse(21f, 12f, 8f, 6f, px, py) ||
                             PixelShapes.InPolygon(new (float x, float y)[] { (22f, 8f), (26f, 8f), (28f, 3f) }, px, py);
                if (front)
                    return true;

                bool gap = PixelShapes.InEllipse(21f, 12f, 10f, 8f, px, py);
                bool back = PixelShapes.InEllipse(12f, 20f, 9f, 7f, px, py) ||
                            PixelShapes.InPolygon(new (float x, float y)[] { (7f, 16f), (11f, 15f), (4f, 10f) }, px, py);
                return back && !gap;
            }
        }
    }
}
