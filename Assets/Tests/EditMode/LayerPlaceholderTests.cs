using System;
using System.Linq;
using NUnit.Framework;

/// <summary>Placeholder character layers: size, shape inside the art contract's safe area, colours, marks, determinism.</summary>
public class LayerPlaceholderTests
{
    /// <summary>The art contract's safe area (canvas px): x 120..904, y 10..the soles.</summary>
    private const int SafeXMin = 120, SafeXMax = 904, SafeYMin = 10;

    private static readonly (byte, byte, byte) Fill = (200, 30, 40);
    private static readonly (byte, byte, byte) Accent = (10, 220, 90);

    private static byte[] Render(PlaceholderRegion region, PlaceholderMark mark = PlaceholderMark.None) =>
        LayerPlaceholder.Render(region, Fill, Accent, mark);

    private static bool Opaque(byte[] rgba, int x, int y) => rgba[(y * LayerPlaceholder.Width + x) * 4 + 3] == 255;

    [Test]
    public void TheCanvasAtQuarterSize()
    {
        Assert.AreEqual(256, LayerPlaceholder.Width);
        Assert.AreEqual(384, LayerPlaceholder.Height);
        Assert.AreEqual(256 * 384 * 4, Render(PlaceholderRegion.Body).Length);
    }

    [Test]
    public void EveryRegion_HasOpaquePixels_OnlyInsideTheSafeArea()
    {
        foreach (PlaceholderRegion region in Enum.GetValues(typeof(PlaceholderRegion)))
        {
            byte[] rgba = Render(region);
            int opaque = 0;
            for (int y = 0; y < LayerPlaceholder.Height; y++)
            {
                for (int x = 0; x < LayerPlaceholder.Width; x++)
                {
                    int i = (y * LayerPlaceholder.Width + x) * 4;
                    if (rgba[i + 3] == 0)
                    {
                        Assert.AreEqual(0, rgba[i] | rgba[i + 1] | rgba[i + 2], $"{region}: a transparent pixel is clear");
                        continue;
                    }

                    opaque++;
                    int canvasX = x * 4 + 2;
                    int canvasYFromTop = (LayerPlaceholder.Height - 1 - y) * 4 + 2;
                    Assert.IsTrue(canvasX >= SafeXMin && canvasX <= SafeXMax, $"{region}: x {canvasX}");
                    Assert.IsTrue(canvasYFromTop >= SafeYMin && canvasYFromTop <= LookCanvas.Feet, $"{region}: y {canvasYFromTop}");
                }
            }

            Assert.Greater(opaque, 200, region.ToString());
        }
    }

    [Test]
    public void TheCornersAreTransparent()
    {
        byte[] rgba = Render(PlaceholderRegion.WholeFigure);
        foreach ((int x, int y) in new[] { (0, 0), (255, 0), (0, 383), (255, 383) })
            Assert.IsFalse(Opaque(rgba, x, y));
    }

    [Test]
    public void TheFillAndTheAccentBothAppear()
    {
        foreach (PlaceholderRegion region in Enum.GetValues(typeof(PlaceholderRegion)))
        {
            byte[] rgba = Render(region);
            bool fill = false, accent = false;
            for (int i = 0; i < rgba.Length; i += 4)
            {
                if (rgba[i + 3] == 0)
                    continue;
                fill |= (rgba[i], rgba[i + 1], rgba[i + 2]) == Fill;
                accent |= (rgba[i], rgba[i + 1], rgba[i + 2]) == Accent;
            }

            Assert.IsTrue(fill && accent, region.ToString());
        }
    }

    [Test]
    public void EachMark_ChangesTheHead_AndNoMarkChangesNothing()
    {
        byte[] plain = Render(PlaceholderRegion.WholeFigure);
        var marked = new[] { PlaceholderMark.Neutral, PlaceholderMark.Happy, PlaceholderMark.Angry, PlaceholderMark.Worried }
            .Select(m => Render(PlaceholderRegion.WholeFigure, m)).ToArray();

        foreach (byte[] m in marked)
            Assert.IsFalse(plain.SequenceEqual(m), "a mark draws something");
        for (int a = 0; a < marked.Length; a++)
            for (int b = a + 1; b < marked.Length; b++)
                Assert.IsFalse(marked[a].SequenceEqual(marked[b]), $"marks {a} and {b} differ");

        // Every changed pixel is on the head (rows 260..424 from the top).
        byte[] happy = marked[1];
        for (int y = 0; y < LayerPlaceholder.Height; y++)
            for (int x = 0; x < LayerPlaceholder.Width; x++)
            {
                int i = (y * LayerPlaceholder.Width + x) * 4;
                if (plain[i] != happy[i] || plain[i + 3] != happy[i + 3])
                {
                    int canvasY = (LayerPlaceholder.Height - 1 - y) * 4;
                    Assert.IsTrue(canvasY >= LookCanvas.HeadTop && canvasY <= LookCanvas.Chin, $"changed pixel at canvas y {canvasY}");
                }
            }
    }

    [Test]
    public void Deterministic()
    {
        CollectionAssert.AreEqual(Render(PlaceholderRegion.Hat, PlaceholderMark.Angry), Render(PlaceholderRegion.Hat, PlaceholderMark.Angry));
    }
}
