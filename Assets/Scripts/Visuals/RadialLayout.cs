using System;

/// <summary>
/// The traveller wheel's ring: items on an ellipse, item 0 at the top, then
/// clockwise; how many items of a size fit without overlapping each other or
/// the centre box; and the box the ring can cover. Engine-free, so the layout
/// RadialLayoutGroup applies and the fit the builder checks are tested headless.
/// </summary>
public static class RadialLayout
{
    /// <summary>Where item <paramref name="i"/> of <paramref name="n"/> sits, relative to the ring's centre (angle 90 - i * 360 / n degrees on the ellipse); (0, 0) when n is 0 or less.</summary>
    public static (float x, float y) Point(int i, int n, float radiusX, float radiusY)
    {
        if (n <= 0)
            return (0f, 0f);

        double angle = (90.0 - i * 360.0 / n) * Math.PI / 180.0;
        return ((float)(radiusX * Math.Cos(angle)), (float)(radiusY * Math.Sin(angle)));
    }

    /// <summary>
    /// The largest m up to <paramref name="limit"/> such that every count from
    /// 1 to m fits (the wheel may show any number of choices up to its
    /// capacity); 0 when even one item does not fit.
    /// </summary>
    public static int MaxFit(float radiusX, float radiusY, float itemW, float itemH, float centreW, float centreH, float gap, int limit)
    {
        int fit = 0;
        for (int n = 1; n <= limit; n++)
        {
            if (!Fits(n, radiusX, radiusY, itemW, itemH, centreW, centreH, gap))
                break;
            fit = n;
        }

        return fit;
    }

    /// <summary>The size of the box around every item the ring can place: the ellipse plus one item each way.</summary>
    public static (float width, float height) Extent(float radiusX, float radiusY, float itemW, float itemH) =>
        (2f * radiusX + itemW, 2f * radiusY + itemH);

    /// <summary>True when n items overlap neither each other nor the centre box, at least <paramref name="gap"/> apart.</summary>
    private static bool Fits(int n, float radiusX, float radiusY, float itemW, float itemH, float centreW, float centreH, float gap)
    {
        var points = new (float x, float y)[n];
        for (int i = 0; i < n; i++)
            points[i] = Point(i, n, radiusX, radiusY);

        for (int i = 0; i < n; i++)
        {
            if (!Apart(points[i].x, points[i].y, itemW, itemH, 0f, 0f, centreW, centreH, gap))
                return false;

            for (int j = i + 1; j < n; j++)
                if (!Apart(points[i].x, points[i].y, itemW, itemH, points[j].x, points[j].y, itemW, itemH, gap))
                    return false;
        }

        return true;
    }

    /// <summary>Two centred boxes are apart when they are separated by the gap on at least one axis.</summary>
    private static bool Apart(float ax, float ay, float aw, float ah, float bx, float by, float bw, float bh, float gap) =>
        Math.Abs(ax - bx) >= (aw + bw) / 2f + gap || Math.Abs(ay - by) >= (ah + bh) / 2f + gap;
}
