using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>The traveller wheel's placeholder icons: a white glyph on transparent per icon name, nothing for an unknown name.</summary>
public class WheelIconPlaceholderTests
{
    private static readonly string[] Names = { "wheel_normal", "wheel_back", "wheel_request", "wheel_question", "wheel_look", "wheel_dialog" };

    private static bool Opaque(byte[] rgba, int x, int y) => rgba[(y * WheelIconPlaceholder.Size + x) * 4 + 3] != 0;

    [Test]
    public void EveryIcon_IsASquareOfWhiteOnTransparent_WithAClearBorder()
    {
        int size = WheelIconPlaceholder.Size;
        Assert.AreEqual(32, size);
        foreach (string name in Names)
        {
            byte[] rgba = WheelIconPlaceholder.Render(name);
            Assert.AreEqual(size * size * 4, rgba.Length, name);

            int opaque = 0;
            for (int i = 0; i < rgba.Length; i += 4)
            {
                bool white = rgba[i] == 255 && rgba[i + 1] == 255 && rgba[i + 2] == 255 && rgba[i + 3] == 255;
                bool clear = rgba[i] == 0 && rgba[i + 1] == 0 && rgba[i + 2] == 0 && rgba[i + 3] == 0;
                Assert.IsTrue(white || clear, $"{name}: pixel {i / 4} is white or clear");
                if (white)
                    opaque++;
            }

            Assert.Greater(opaque, size * size / 20, $"{name} draws a glyph");
            Assert.Less(opaque, size * size * 3 / 4, $"{name} leaves room around it");
            for (int k = 0; k < size; k++)
                Assert.IsFalse(Opaque(rgba, k, 0) || Opaque(rgba, k, size - 1) || Opaque(rgba, 0, k) || Opaque(rgba, size - 1, k), $"{name}: the edge stays clear");
        }
    }

    [Test]
    public void EveryIcon_IsItsOwnGlyph()
    {
        var drawn = new List<byte[]>();
        foreach (string name in Names)
        {
            byte[] rgba = WheelIconPlaceholder.Render(name);
            Assert.IsFalse(drawn.Any(d => d.SequenceEqual(rgba)), name);
            drawn.Add(rgba);
        }
    }

    [Test]
    public void TheBackArrow_PointsLeft()
    {
        byte[] rgba = WheelIconPlaceholder.Render("wheel_back");
        int size = WheelIconPlaceholder.Size;
        int TipColumn(bool fromLeft)
        {
            for (int k = 0; k < size; k++)
            {
                int x = fromLeft ? k : size - 1 - k;
                for (int y = 0; y < size; y++)
                    if (Opaque(rgba, x, y))
                        return x;
            }
            return -1;
        }

        int left = TipColumn(true), right = TipColumn(false);
        int Height(int x) => Enumerable.Range(0, size).Count(y => Opaque(rgba, x, y));
        Assert.Less(Height(left), Height(right - 6), "a point on the left, the body towards the right");
    }

    [Test]
    public void UnknownNames_DrawNothing()
    {
        Assert.IsNull(WheelIconPlaceholder.Render("wheel_shrug"));
        Assert.IsNull(WheelIconPlaceholder.Render(""));
        Assert.IsNull(WheelIconPlaceholder.Render(null));
    }
}
