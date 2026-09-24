using NUnit.Framework;

public class CursorHotspotTests
{
    /// <summary>Builds a bottom-up alpha grid from rows written top-down ('#' solid, '.' clear, ':' faint).</summary>
    private static byte[] Grid(params string[] topDownRows)
    {
        int h = topDownRows.Length;
        int w = topDownRows[0].Length;
        var alpha = new byte[w * h];
        for (int row = 0; row < h; row++)
        {
            int y = h - 1 - row;
            for (int x = 0; x < w; x++)
                alpha[y * w + x] = topDownRows[row][x] == '#' ? (byte)255 : topDownRows[row][x] == ':' ? (byte)5 : (byte)0;
        }
        return alpha;
    }

    [Test]
    public void Tip_IsTheLeftmostSolidPixelOfTheTopSolidRow()
    {
        byte[] alpha = Grid(
            "......",
            "..#...",
            "..##..",
            "..###.");
        (int x, int y) = CursorHotspot.Find(alpha, 6, 4, CursorHotspot.Kind.Tip);
        Assert.AreEqual(2, x);
        Assert.AreEqual(1, y);
    }

    [Test]
    public void Fingertip_IsTheCentreOfTheTopSolidRun()
    {
        byte[] alpha = Grid(
            "........",
            "...###..",
            "...###..",
            "..#####.");
        (int x, int y) = CursorHotspot.Find(alpha, 8, 4, CursorHotspot.Kind.Fingertip);
        Assert.AreEqual(4, x);
        Assert.AreEqual(1, y);
    }

    [Test]
    public void FaintAntiAliasedPixels_AreIgnored()
    {
        byte[] alpha = Grid(
            "::::",
            ".#..",
            ".##.");
        (int x, int y) = CursorHotspot.Find(alpha, 4, 3, CursorHotspot.Kind.Tip);
        Assert.AreEqual(1, x);
        Assert.AreEqual(1, y);
    }

    [Test]
    public void EmptyImage_FallsBackToTopLeft()
    {
        (int x, int y) = CursorHotspot.Find(new byte[4], 2, 2, CursorHotspot.Kind.Fingertip);
        Assert.AreEqual(0, x);
        Assert.AreEqual(0, y);
    }
}
