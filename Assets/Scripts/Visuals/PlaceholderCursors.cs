/// <summary>
/// The placeholder cursors' outlines, in top-left pixel coordinates of a
/// 32 x 32 cursor: the arrow and the pointing hand that Build Office UI draws
/// (PixelShapes.InPolygon) until the cursor art lands. Here, not in the
/// builder, so the tests check the shapes the builder draws, not a copy
/// (audit R6-023).
/// </summary>
public static class PlaceholderCursors
{
    /// <summary>The arrow, its tip at (0, 0).</summary>
    public static readonly (float x, float y)[] Arrow =
    {
        (0, 0), (0, 22), (5, 17), (9, 26), (12, 25), (8, 16), (15, 16),
    };

    /// <summary>The pointing hand, its fingertip at (12, 1).</summary>
    public static readonly (float x, float y)[] Hand =
    {
        (10, 1), (13, 1), (14, 2), (14, 12), (21, 13), (23, 15), (23, 25), (19, 30),
        (10, 30), (6, 24), (5, 18), (7, 17), (10, 19),
    };
}
