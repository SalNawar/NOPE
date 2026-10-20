/// <summary>
/// The desk's work mat (P0-A seam; owner DESK-PLAN): its rectangle in the
/// desk's plane (a DeskRect, metres) and whether a desk point lies on it. The
/// P0 stub holds the rectangle it is given; DESK-PLAN gives it the mat.
/// </summary>
public readonly struct DeskMat
{
    /// <summary>The mat on the desk's plane.</summary>
    public DeskMat(DeskRect rect) => Rect = rect;

    /// <summary>The mat's rectangle in the desk's plane (metres).</summary>
    public DeskRect Rect { get; }

    /// <summary>True when the desk point (<paramref name="x"/>, <paramref name="y"/>) lies on the mat (inside or on its edge).</summary>
    public bool IsOnMat(float x, float y) => Rect.Contains(x, y);
}
