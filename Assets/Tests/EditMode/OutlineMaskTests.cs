using System;
using NUnit.Framework;

public class OutlineMaskTests
{
    private static byte At(byte[] grid, int width, int x, int y) => grid[y * width + x];

    [Test]
    public void OutputGrid_AddsMarginOnEverySide()
    {
        OutlineMask.BuildRing(new byte[4 * 3], 4, 3, 3, out int w, out int h);
        Assert.AreEqual(4 + 2 * 4, w);
        Assert.AreEqual(3 + 2 * 4, h);
    }

    [Test]
    public void SinglePixel_OrthogonalNeighboursFull_DiagonalsAntiAliased_FarEmpty()
    {
        byte[] ring = OutlineMask.BuildRing(new byte[] { 255 }, 1, 1, 1, out int w, out int _);
        Assert.AreEqual(5, w);
        Assert.AreEqual(0, At(ring, w, 2, 2), "inside stays empty");
        Assert.AreEqual(255, At(ring, w, 1, 2));
        Assert.AreEqual(255, At(ring, w, 3, 2));
        Assert.AreEqual(255, At(ring, w, 2, 1));
        Assert.AreEqual(255, At(ring, w, 2, 3));
        Assert.AreEqual(149, At(ring, w, 1, 1), "diagonal is anti-aliased");
        Assert.AreEqual(0, At(ring, w, 0, 2));
    }

    [Test]
    public void WideRing_ReachesItsWidth_AndStopsThere()
    {
        byte[] ring = OutlineMask.BuildRing(new byte[] { 255 }, 1, 1, 3, out int w, out int _);
        Assert.AreEqual(255, At(ring, w, 1, 4));
        Assert.AreEqual(0, At(ring, w, 0, 4));
    }

    [Test]
    public void SolidBlock_HasNoRingInside_AndARingAtItsEdge()
    {
        var alpha = new byte[9];
        for (int i = 0; i < alpha.Length; i++)
            alpha[i] = 255;
        byte[] ring = OutlineMask.BuildRing(alpha, 3, 3, 2, out int w, out int _);
        int m = OutlineMask.Margin(2);
        for (int y = 0; y < 3; y++)
            for (int x = 0; x < 3; x++)
                Assert.AreEqual(0, At(ring, w, x + m, y + m));
        Assert.AreEqual(255, At(ring, w, m - 1, m + 1));
    }

    [Test]
    public void TransparentOrBelowThreshold_GivesNoRing()
    {
        byte[] ring = OutlineMask.BuildRing(new byte[] { 0, 100, 127, 0 }, 2, 2, 2, out _, out _);
        foreach (byte a in ring)
            Assert.AreEqual(0, a);
    }

    [Test]
    public void ShiftPivot_AddsTheMargin()
    {
        (float x, float y) = OutlineMask.ShiftPivot(10f, 5f, 2);
        Assert.AreEqual(13f, x);
        Assert.AreEqual(8f, y);
    }

    [Test]
    public void BadArguments_Throw()
    {
        Assert.Throws<ArgumentException>(() => OutlineMask.BuildRing(new byte[3], 2, 2, 1, out _, out _));
        Assert.Throws<ArgumentException>(() => OutlineMask.BuildRing(null, 1, 1, 1, out _, out _));
        Assert.Throws<ArgumentOutOfRangeException>(() => OutlineMask.BuildRing(new byte[1], 1, 1, 0, out _, out _));
        Assert.Throws<ArgumentOutOfRangeException>(() => OutlineMask.BuildRing(new byte[0], 0, 1, 1, out _, out _));
    }
}
