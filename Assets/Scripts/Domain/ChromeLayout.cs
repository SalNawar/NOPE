using System;

/// <summary>
/// Where the office overlay's edge chrome lies: the view plates (the PC and
/// CITY pull tabs on the left edge, the STAMPS tab on the top edge) as Build
/// Office UI lays them, and the guide's plate, as the builder lays it and
/// where its quiet look docks under the painted desk (GuidePrompt.Quiet). One
/// source for the builder, the guide and the layout test. The overlay's
/// canvas scales from 1920x1080 with Expand (OfficeSceneUIBuilder.ConfigureScaler):
/// a screen wider than 16:9 gets more width, a narrower one (5:4) more height,
/// so the left and top edges keep their sizes in canvas units. A
/// <see cref="Box"/> is in canvas units from the top-left corner. Pure.
/// </summary>
public static class ChromeLayout
{
    /// <summary>The overlay canvas's reference size (CanvasScaler.referenceResolution).</summary>
    public const float ReferenceWidth = 1920f, ReferenceHeight = 1080f;

    /// <summary>The PC pull tab on the left edge: its centre at this share of the canvas's height from the bottom, its width and height.</summary>
    public const float PcTabHeight = 0.4f, PcTabWidth = 96f, PcTabTall = 96f;

    /// <summary>The CITY tab on the left edge (the city view's look control; its back control mirrors it on the right edge): its centre's share of the height from the bottom, its width, height and inset from the edge.</summary>
    public const float CityTabHeight = 0.74f, CityTabWidth = 170f, CityTabTall = 44f, CityTabInset = 12f;

    /// <summary>The STAMPS tab on the top edge: its left side at this share of the canvas's width, its width and height.</summary>
    public const float StampTabX = 0.66f, StampTabWidth = 236f, StampTabTall = 64f;

    /// <summary>The guide's plate as the builder lays it: its width and height, and its margin from the top-left corner.</summary>
    public const float GuidePlateWidth = 640f, GuidePlateTall = 124f, GuidePlateMargin = 16f;

    /// <summary>The guide's quiet plate under the painted desk: its scale and its margin from the top-left corner (clear of the STAMPS tab on the top edge and the CITY tab on the left edge).</summary>
    public const float QuietPlateScale = 0.8f, QuietMargin = 16f;

    /// <summary>A rectangle in canvas units from the top-left corner (x right, y down).</summary>
    public readonly struct Box
    {
        public readonly float Left, Top, Right, Bottom;

        public Box(float left, float top, float right, float bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        /// <summary>True when the two rectangles share an area (touching edges do not count).</summary>
        public bool Overlaps(Box other) => Left < other.Right && other.Left < Right && Top < other.Bottom && other.Top < Bottom;

        /// <summary>True when the rectangle lies wholly inside a canvas of <paramref name="width"/> by <paramref name="height"/>.</summary>
        public bool Inside(float width, float height) => Left >= 0f && Top >= 0f && Right <= width && Bottom <= height;

        /// <summary>"(left, top)-(right, bottom)" for test messages.</summary>
        public override string ToString() => $"({Left:0}, {Top:0})-({Right:0}, {Bottom:0})";
    }

    /// <summary>The overlay canvas's size on a <paramref name="screenWidth"/> by <paramref name="screenHeight"/> screen (Expand: the smaller of the two scales, so the canvas is never smaller than the reference).</summary>
    public static (float width, float height) Canvas(float screenWidth, float screenHeight)
    {
        if (!(screenWidth > 0f) || !(screenHeight > 0f))
            return (ReferenceWidth, ReferenceHeight);
        float scale = Math.Min(screenWidth / ReferenceWidth, screenHeight / ReferenceHeight);
        return (screenWidth / scale, screenHeight / scale);
    }

    /// <summary>The PC pull tab on a canvas <paramref name="canvasHeight"/> tall.</summary>
    public static Box PcTab(float canvasHeight) => LeftEdge(PcTabHeight, 0f, PcTabWidth, PcTabTall, canvasHeight);

    /// <summary>The CITY tab on a canvas <paramref name="canvasHeight"/> tall.</summary>
    public static Box CityTab(float canvasHeight) => LeftEdge(CityTabHeight, CityTabInset, CityTabWidth, CityTabTall, canvasHeight);

    /// <summary>The STAMPS tab on a canvas <paramref name="canvasWidth"/> wide.</summary>
    public static Box StampTab(float canvasWidth) => new Box(StampTabX * canvasWidth, 0f, StampTabX * canvasWidth + StampTabWidth, StampTabTall);

    /// <summary>The guide's quiet plate: docked in the top-left corner at <see cref="QuietMargin"/>, <see cref="QuietPlateScale"/> of the builder's plate.</summary>
    public static Box QuietPlate() =>
        new Box(QuietMargin, QuietMargin, QuietMargin + GuidePlateWidth * QuietPlateScale, QuietMargin + GuidePlateTall * QuietPlateScale);

    /// <summary>A tab on the left edge: its centre at <paramref name="share"/> of the height from the bottom, <paramref name="inset"/> from the edge.</summary>
    private static Box LeftEdge(float share, float inset, float width, float tall, float canvasHeight)
    {
        float centre = (1f - share) * canvasHeight;
        return new Box(inset, centre - tall / 2f, inset + width, centre + tall / 2f);
    }
}
