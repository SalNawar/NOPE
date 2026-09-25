using System;
using NUnit.Framework;

/// <summary>The text-free placeholder wallpaper painter (piece 6 U13).</summary>
public class CulturePlaceholdersTests
{
    private const int W = 96, H = 540;
    private static readonly Rgba SkyTop = new Rgba(0.2f, 0.43f, 0.76f);
    private static readonly Rgba SkyBottom = new Rgba(0.78f, 0.9f, 1f);
    private static readonly Rgba GroundLow = new Rgba(0.27f, 0.45f, 0.15f);
    private static readonly Rgba GroundHigh = new Rgba(0.49f, 0.69f, 0.26f);
    private static readonly Rgba Cloud = new Rgba(1f, 1f, 1f);

    private static byte[] Paint() => CulturePlaceholders.Wallpaper(W, H, SkyTop, SkyBottom, GroundLow, GroundHigh, Cloud);

    private static int At(double fx, double fy) => ((int)(fy * H) * W + (int)(fx * W)) * 4;

    private static int Brightness(byte[] p, int i) => p[i] + p[i + 1] + p[i + 2];

    [Test]
    public void Wallpaper_IsOpaqueRgba_AndDeterministic()
    {
        byte[] a = Paint();
        Assert.AreEqual(W * H * 4, a.Length);
        for (int i = 3; i < a.Length; i += 4)
            Assert.AreEqual(255, a[i]);
        CollectionAssert.AreEqual(a, Paint());
    }

    [Test]
    public void Wallpaper_GroundAtTheBottom_SkyAtTheTop_ACloudOnTheSky()
    {
        byte[] p = Paint();
        Assert.AreEqual((byte)Math.Round(GroundLow.R * 255), p[0], "bottom-left is the ground's low colour");
        Assert.AreEqual((byte)Math.Round(GroundLow.G * 255), p[1]);
        int top = ((H - 1) * W + (int)(0.4 * W)) * 4;
        Assert.AreEqual(SkyTop.R * 255, p[top], 2.0);
        Assert.AreEqual(SkyTop.B * 255, p[top + 2], 2.0);
        Assert.Greater(Brightness(p, At(0.6, 0.9)), Brightness(p, At(0.95, 0.9)), "the second cloud is lighter than the open sky");
    }

    [TestCase(0, 10)]
    [TestCase(10, 0)]
    [TestCase(-1, 10)]
    public void Wallpaper_NonPositiveSize_Throws(int w, int h)
    {
        Assert.Throws<ArgumentException>(() => CulturePlaceholders.Wallpaper(w, h, SkyTop, SkyBottom, GroundLow, GroundHigh, Cloud));
    }
}
