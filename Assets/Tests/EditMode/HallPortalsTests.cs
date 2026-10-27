using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using NUnit.Framework;

/// <summary>
/// The hall portals' one source (HALL-FX, BUG-26a / 73d): HallPortals.asset's
/// openings are the moment fits of the painted-aperture masks in
/// HallPortalMasks.png, re-fitted here from the PNG's pixels (a data test,
/// not a comparison of the config with itself, as the old circular probe
/// was); each ring's hole lies inside its crop and inside the effect's quad,
/// what stands in front of a hole and what is seen through the glass lie on
/// the hole, the crops sit apart inside the atlas, and the asset wires that
/// atlas.
/// </summary>
public class HallPortalsTests
{
    private const string AssetPath = "Assets/Data/Config/HallPortals.asset";
    private const string MaskPath = "Assets/Data/Config/HallPortalMasks.png";

    /// <summary>One ring as the asset stores it.</summary>
    private sealed class Ring
    {
        public int Portal;
        public float Cx, Cy, Rx, Ry, Reach;
        public float CropX, CropY, CropW, CropH;
        public float U, V, Uw, Vh;
    }

    private static string FromProject(string path, [CallerFilePath] string here = "") =>
        File.Exists(path) ? path : Path.Combine(Path.GetDirectoryName(here), "..", "..", "..", path);

    private static float F(string s) => float.Parse(s, CultureInfo.InvariantCulture);

    private static List<Ring> Rings(out string text)
    {
        text = File.ReadAllText(FromProject(AssetPath)).Replace("\r\n", "\n");
        var rings = new List<Ring>();
        const string Num = @"(-?[\d.eE+-]+)";
        string rect(string name) => name + @":\n\s+serializedVersion: 2\n\s+x: " + Num + @"\n\s+y: " + Num + @"\n\s+width: " + Num + @"\n\s+height: " + Num;
        var block = new Regex(@"  - portal: (\d+)\n(?:    .*\n|      .*\n)+?    opening: \{x: " + Num + ", y: " + Num + ", z: " + Num + ", w: " + Num + @"\}\n    reach: " + Num + @"\n    " + rect("maskCanvas") + @"\n    " + rect("maskRect"));
        foreach (Match m in block.Matches(text))
        {
            rings.Add(new Ring
            {
                Portal = int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture),
                Cx = F(m.Groups[2].Value), Cy = F(m.Groups[3].Value), Rx = F(m.Groups[4].Value), Ry = F(m.Groups[5].Value), Reach = F(m.Groups[6].Value),
                CropX = F(m.Groups[7].Value), CropY = F(m.Groups[8].Value), CropW = F(m.Groups[9].Value), CropH = F(m.Groups[10].Value),
                U = F(m.Groups[11].Value), V = F(m.Groups[12].Value), Uw = F(m.Groups[13].Value), Vh = F(m.Groups[14].Value),
            });
        }
        return rings;
    }

    [Test]
    public void TheAsset_HoldsFiveRings_AndWiresTheMaskAtlas()
    {
        List<Ring> rings = Rings(out string text);
        CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5 }, rings.ConvertAll(r => r.Portal), "the hall's five rings, in order");
        string meta = File.ReadAllText(FromProject(MaskPath + ".meta"));
        string guid = Regex.Match(meta, @"guid: ([0-9a-f]{32})").Groups[1].Value;
        StringAssert.Contains("masks: {fileID: 2800000, guid: " + guid + ", type: 3}", text, "the asset's masks are HallPortalMasks.png");
        StringAssert.Contains("sRGBTexture: 0", meta, "the masks are data, read linear");
        StringAssert.Contains("enableMipMap: 0", meta, "no mips: a low mip would bleed between the crops");
    }

    [Test]
    public void EachOpening_IsTheMomentFitOfItsMask()
    {
        Png atlas = Png.Read(FromProject(MaskPath));
        foreach (Ring r in Rings(out _))
        {
            int x0 = (int)Math.Round(r.U * atlas.Width), w = (int)Math.Round(r.Uw * atlas.Width);
            int h = (int)Math.Round(r.Vh * atlas.Height), y0 = atlas.Height - (int)Math.Round(r.V * atlas.Height) - h;
            float scale = w / r.CropW;
            Assert.AreEqual(scale, h / r.CropH, 1e-4, $"portal {r.Portal}: one scale across and up");

            double sum = 0, sx = 0, sy = 0;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    double c = atlas.At(x0 + x, y0 + y, 0) / 255.0;
                    sum += c;
                    sx += c * (r.CropX + (x + 0.5) / scale);
                    sy += c * (r.CropY + (y + 0.5) / scale);
                }
            Assert.Greater(sum, 0, $"portal {r.Portal}: its mask has a hole");
            double ex = sx / sum, ey = sy / sum, vx = 0, vy = 0;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    double c = atlas.At(x0 + x, y0 + y, 0) / 255.0;
                    vx += c * Math.Pow(r.CropX + (x + 0.5) / scale - ex, 2);
                    vy += c * Math.Pow(r.CropY + (y + 0.5) / scale - ey, 2);
                }
            // The PNG holds the coverage in 8 bits and the asset two decimals: a tenth of a canvas pixel.
            Assert.AreEqual(ex, r.Cx, 0.1, $"portal {r.Portal}: centre x");
            Assert.AreEqual(ey, r.Cy, 0.1, $"portal {r.Portal}: centre y");
            Assert.AreEqual(2.0 * Math.Sqrt(vx / sum), r.Rx, 0.1, $"portal {r.Portal}: radius across");
            Assert.AreEqual(2.0 * Math.Sqrt(vy / sum), r.Ry, 0.1, $"portal {r.Portal}: radius up");
        }
    }

    [Test]
    public void EachHole_LiesInItsCropAndTheEffectsQuad_WithItsLayersOnIt()
    {
        Png atlas = Png.Read(FromProject(MaskPath));
        foreach (Ring r in Rings(out _))
        {
            int x0 = (int)Math.Round(r.U * atlas.Width), w = (int)Math.Round(r.Uw * atlas.Width);
            int h = (int)Math.Round(r.Vh * atlas.Height), y0 = atlas.Height - (int)Math.Round(r.V * atlas.Height) - h;
            float scale = w / r.CropW;
            double reach = 0;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int hole = atlas.At(x0 + x, y0 + y, 0);
                    if (x == 0 || y == 0 || x == w - 1 || y == h - 1)
                        Assert.AreEqual(0, hole, $"portal {r.Portal}: the hole never touches its crop's edge (at {x},{y})");
                    if (hole == 0)
                    {
                        Assert.AreEqual(0, atlas.At(x0 + x, y0 + y, 1), $"portal {r.Portal}: an occluder off the hole (at {x},{y})");
                        Assert.AreEqual(0, atlas.At(x0 + x, y0 + y, 2), $"portal {r.Portal}: glass off the hole (at {x},{y})");
                    }
                    if (hole > 127)
                    {
                        double dx = (r.CropX + (x + 0.5) / scale - r.Cx) / r.Rx, dy = (r.CropY + (y + 0.5) / scale - r.Cy) / r.Ry;
                        reach = Math.Max(reach, Math.Sqrt(dx * dx + dy * dy));
                    }
                }
            Assert.AreEqual(reach, r.Reach, 0.02, $"portal {r.Portal}: the iris opens as far as the hole reaches");
            Assert.That(r.Reach, Is.InRange(1f, PortalVortex.Margin), $"portal {r.Portal}: the hole fits in the effect's quad");
        }
    }

    [Test]
    public void TheCrops_SitApartInsideTheAtlas()
    {
        List<Ring> rings = Rings(out _);
        foreach (Ring r in rings)
        {
            Assert.That(r.U, Is.GreaterThanOrEqualTo(0f));
            Assert.That(r.V, Is.GreaterThanOrEqualTo(0f));
            Assert.That(r.U + r.Uw, Is.LessThanOrEqualTo(1f));
            Assert.That(r.V + r.Vh, Is.LessThanOrEqualTo(1f));
        }
        for (int i = 0; i < rings.Count; i++)
            for (int j = i + 1; j < rings.Count; j++)
            {
                Ring a = rings[i], b = rings[j];
                bool apart = a.U + a.Uw <= b.U || b.U + b.Uw <= a.U || a.V + a.Vh <= b.V || b.V + b.Vh <= a.V;
                Assert.IsTrue(apart, $"portals {a.Portal} and {b.Portal} share atlas pixels");
            }
    }

    /// <summary>A minimal reader of the 8-bit RGBA, non-interlaced PNGs the mask tool writes.</summary>
    private sealed class Png
    {
        public int Width, Height;
        private byte[] _rgba;

        /// <summary>Channel <paramref name="c"/> (0 R .. 3 A) of the pixel at column <paramref name="x"/>, row <paramref name="y"/> from the top.</summary>
        public int At(int x, int y, int c) => _rgba[(y * Width + x) * 4 + c];

        public static Png Read(string path)
        {
            byte[] file = File.ReadAllBytes(path);
            // A checkout without its LFS objects holds a text pointer: the mask cannot be read there.
            if (file.Length > 0 && file[0] == (byte)'v')
                Assert.Ignore($"{path} is a Git LFS pointer in this checkout (git lfs pull fetches the mask).");
            Assert.IsTrue(file.Length > 8 && file[1] == (byte)'P' && file[2] == (byte)'N' && file[3] == (byte)'G', $"{path} is not a PNG");
            var png = new Png();
            var idat = new MemoryStream();
            int pos = 8;
            while (pos < file.Length)
            {
                int length = (file[pos] << 24) | (file[pos + 1] << 16) | (file[pos + 2] << 8) | file[pos + 3];
                string type = System.Text.Encoding.ASCII.GetString(file, pos + 4, 4);
                if (type == "IHDR")
                {
                    png.Width = (file[pos + 8] << 24) | (file[pos + 9] << 16) | (file[pos + 10] << 8) | file[pos + 11];
                    png.Height = (file[pos + 12] << 24) | (file[pos + 13] << 16) | (file[pos + 14] << 8) | file[pos + 15];
                    Assert.AreEqual(8, file[pos + 16], "8 bits a channel");
                    Assert.AreEqual(6, file[pos + 17], "RGBA");
                    Assert.AreEqual(0, file[pos + 20], "not interlaced");
                }
                else if (type == "IDAT")
                    idat.Write(file, pos + 8, length);
                pos += 12 + length;
            }

            idat.Position = 2; // the zlib header
            var raw = new MemoryStream();
            using (var inflate = new DeflateStream(idat, CompressionMode.Decompress))
                inflate.CopyTo(raw);
            byte[] data = raw.ToArray();
            int stride = png.Width * 4;
            png._rgba = new byte[stride * png.Height];
            for (int y = 0; y < png.Height; y++)
            {
                int filter = data[y * (stride + 1)];
                for (int i = 0; i < stride; i++)
                {
                    int x = data[y * (stride + 1) + 1 + i];
                    int a = i >= 4 ? png._rgba[y * stride + i - 4] : 0;
                    int b = y > 0 ? png._rgba[(y - 1) * stride + i] : 0;
                    int c = i >= 4 && y > 0 ? png._rgba[(y - 1) * stride + i - 4] : 0;
                    int p = a + b - c, pa = Math.Abs(p - a), pb = Math.Abs(p - b), pc = Math.Abs(p - c);
                    int predicted = filter == 0 ? 0 : filter == 1 ? a : filter == 2 ? b : filter == 3 ? (a + b) / 2
                        : pa <= pb && pa <= pc ? a : pb <= pc ? b : c;
                    png._rgba[y * stride + i] = (byte)(x + predicted);
                }
            }
            return png;
        }
    }
}
