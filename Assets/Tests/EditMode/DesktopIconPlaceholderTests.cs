using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>The desktop's placeholder icon glyphs (the PC redesign DK2): a white glyph on transparent per app id, each its own, nothing for an unknown id.</summary>
public class DesktopIconPlaceholderTests
{
    private static bool Opaque(byte[] rgba, int x, int y) => rgba[(y * DesktopIconPlaceholder.Size + x) * 4 + 3] != 0;

    [Test]
    public void EveryApp_IsASquareOfWhiteOnTransparent_WithAClearBorder()
    {
        int size = DesktopIconPlaceholder.Size;
        Assert.AreEqual(64, size);
        foreach (string id in DesktopAppIds.DefaultOrder)
        {
            byte[] rgba = DesktopIconPlaceholder.Render(id);
            Assert.IsNotNull(rgba, id);
            Assert.AreEqual(size * size * 4, rgba.Length, id);

            int opaque = 0;
            for (int i = 0; i < rgba.Length; i += 4)
            {
                bool white = rgba[i] == 255 && rgba[i + 1] == 255 && rgba[i + 2] == 255 && rgba[i + 3] == 255;
                bool clear = rgba[i] == 0 && rgba[i + 1] == 0 && rgba[i + 2] == 0 && rgba[i + 3] == 0;
                Assert.IsTrue(white || clear, $"{id}: pixel {i / 4} is white or clear");
                if (white)
                    opaque++;
            }

            Assert.Greater(opaque, size * size / 12, $"{id} draws a glyph");
            Assert.Less(opaque, size * size * 3 / 4, $"{id} leaves room around it");
            for (int k = 0; k < size; k++)
                Assert.IsFalse(Opaque(rgba, k, 0) || Opaque(rgba, k, size - 1) || Opaque(rgba, 0, k) || Opaque(rgba, size - 1, k), $"{id}: the edge stays clear");
        }
    }

    [Test]
    public void EveryApp_IsItsOwnGlyph()
    {
        var drawn = new List<byte[]>();
        foreach (string id in DesktopAppIds.DefaultOrder)
        {
            byte[] rgba = DesktopIconPlaceholder.Render(id);
            Assert.IsFalse(drawn.Any(d => d.SequenceEqual(rgba)), id);
            drawn.Add(rgba);
        }
    }

    [Test]
    public void AnUnknownId_HasNoGlyph()
    {
        Assert.IsNull(DesktopIconPlaceholder.Render("lexicon"));
        Assert.IsNull(DesktopIconPlaceholder.Render(null));
    }
}
