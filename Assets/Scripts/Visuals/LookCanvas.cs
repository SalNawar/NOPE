/// <summary>
/// The character canvas every layer and premade image shares (the art
/// contract, make_guide_v2.py and coverage.json): 1024 x 1536 px, landmarks
/// in pixels from the top (the safe area, x 120..904, y 10..1490, is the art
/// contract's). The figure stands with its soles on
/// <see cref="Feet"/>; sprites pivot there and are one unit tall, so a
/// traveller's world height is its transform scale. Pure, so the geometry is tested.
/// </summary>
public static class LookCanvas
{
    /// <summary>Canvas width in pixels.</summary>
    public const int Width = 1024;

    /// <summary>Canvas height in pixels.</summary>
    public const int Height = 1536;

    /// <summary>The figure's vertical centre line.</summary>
    public const int CenterX = 512;

    /// <summary>Top of the head (hats and buns may rise above it, into the headroom).</summary>
    public const int HeadTop = 260;

    /// <summary>The chin.</summary>
    public const int Chin = 424;

    /// <summary>The shoulder line.</summary>
    public const int Shoulders = 500;

    /// <summary>The waist.</summary>
    public const int Waist = 760;

    /// <summary>The hips.</summary>
    public const int Hips = 900;

    /// <summary>The knees.</summary>
    public const int Knees = 1170;

    /// <summary>The soles of the feet (the floor).</summary>
    public const int Feet = 1490;

    /// <summary>How far the arms reach from the centre line (the guide's hands).</summary>
    public const int ArmReach = 232;

    /// <summary>The passport photo crop, left edge.</summary>
    public const int PhotoLeft = 362;

    /// <summary>The passport photo crop, top edge.</summary>
    public const int PhotoTop = 215;

    /// <summary>The passport photo crop, right edge.</summary>
    public const int PhotoRight = 662;

    /// <summary>The passport photo crop, bottom edge.</summary>
    public const int PhotoBottom = 590;

    /// <summary>The sprite pivot's height as a fraction of the canvas, from the bottom (the soles).</summary>
    public const float FeetPivotY = (Height - Feet) / (float)Height;

    /// <summary>The photo's width over its height (4:5).</summary>
    public const float PhotoAspect = (PhotoRight - PhotoLeft) / (float)(PhotoBottom - PhotoTop);

    /// <summary>The photo crop as fractions of the canvas, bottom-left origin: (x, y, width, height).</summary>
    public static (float x, float y, float width, float height) PhotoRect =>
        (PhotoLeft / (float)Width, (Height - PhotoBottom) / (float)Height,
         (PhotoRight - PhotoLeft) / (float)Width, (PhotoBottom - PhotoTop) / (float)Height);

    /// <summary>A canvas row (pixels from the top) as a height above the feet, in canvas heights (the sprite's local units).</summary>
    public static float LocalY(int yFromTop) => (Feet - yFromTop) / (float)Height;

    /// <summary>A canvas column as a distance from the centre line, in canvas heights (the sprite's local units).</summary>
    public static float LocalX(int x) => (x - CenterX) / (float)Height;
}
