/// <summary>
/// An axis-aligned rectangle by its edges. A placed form (FormLayout) uses it
/// in form space: the page's top-left is the origin and y grows down, in the
/// caller's units. What was piece 10's PaperFace layout retired into
/// FormLayout (redesign phase 4); this rectangle stays.
/// </summary>
public readonly struct FaceRect
{
    /// <summary>A rectangle from its edges.</summary>
    public FaceRect(float xMin, float yMin, float xMax, float yMax)
    {
        XMin = xMin;
        YMin = yMin;
        XMax = xMax;
        YMax = yMax;
    }

    /// <summary>The left edge.</summary>
    public float XMin { get; }

    /// <summary>The lower edge in value (the top edge in form space, where y grows down).</summary>
    public float YMin { get; }

    /// <summary>The right edge.</summary>
    public float XMax { get; }

    /// <summary>The higher edge in value (the bottom edge in form space).</summary>
    public float YMax { get; }

    /// <summary>The width.</summary>
    public float Width => XMax - XMin;

    /// <summary>The height.</summary>
    public float Height => YMax - YMin;

    /// <summary>The centre's x.</summary>
    public float CentreX => (XMin + XMax) / 2f;

    /// <summary>The centre's y.</summary>
    public float CentreY => (YMin + YMax) / 2f;

    /// <summary>True when the point is inside (edges included).</summary>
    public bool Contains(float x, float y) => x >= XMin && x <= XMax && y >= YMin && y <= YMax;

    /// <summary>A rectangle from its left and top edges and its size (form space: y grows down).</summary>
    public static FaceRect FromTop(float x, float y, float width, float height) => new FaceRect(x, y, x + width, y + height);
}
